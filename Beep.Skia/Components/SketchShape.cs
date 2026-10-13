using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Beep.Skia.Model;
using Beep.Skia.Text;
using SkiaSharp;

namespace Beep.Skia.Components;

/// <summary>Sketch kinds. A frame is a named rectangular region: a crisp border with its title above it.</summary>
public enum SketchShapeKind { Rectangle, Ellipse, Diamond, Line, Arrow, Freehand, Text, Image, Frame }
public enum SketchStrokeStyle { Solid, Dashed, Dotted }
public enum SketchArrowhead { None, Arrow, Triangle, Dot, Bar, OutlinedTriangle, OutlinedDot, Diamond, OutlinedDiamond, CrowsFoot }
public enum SketchTextAlign { Left, Center, Right }
/// <summary>The corners of a rectangle or diamond.</summary>
public enum SketchEdges { Sharp, Round }
/// <summary>How a closed shape is filled: solid, or with lines of its fill colour, in one direction or two.</summary>
public enum SketchFillStyle { Solid, Hatched, CrossHatched }
/// <summary>How a freehand stroke's width is drawn: following its pressures (from a pen, or simulated from the drawing speed),
/// or even along its length.</summary>
public enum SketchInkWidth { Pressure, Even }
/// <summary>An arrow or line end attached to another object: its identity and a point of its unrotated bounds, each
/// coordinate from 0 to 1. The end follows that point when the object moves, resizes or rotates.</summary>
public readonly record struct SketchBinding(Guid Target, float U, float V)
{
    public override string ToString() => Target.ToString("D") + ";" + U.ToString("R", CultureInfo.InvariantCulture) + ";" + V.ToString("R", CultureInfo.InvariantCulture);
    /// <summary>Parses the stored form; empty means unbound. Anything else that is not exact is refused.</summary>
    public static bool TryParse(string text, out SketchBinding? binding)
    {
        binding = null;
        if (text == null) return false;
        if (text.Length == 0) return true;
        var parts = text.Split(';');
        if (parts.Length != 3 || !Guid.TryParseExact(parts[0], "D", out var target) || target == Guid.Empty
            || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var u)
            || !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
            || !float.IsFinite(u) || !float.IsFinite(v) || u < 0 || u > 1 || v < 0 || v > 1) return false;
        binding = new SketchBinding(target, u, v);
        return binding.ToString() == text;
    }
}
/// <summary>How an image's pixels are mirrored about its centre.</summary>
public enum SketchImageFlip { None, Horizontal, Vertical, Both }

// Render-cache key only. The component remains the scene/document authority.
internal readonly record struct SketchVisualState(SketchShapeKind Kind, float X, float Y, float Width, float Height,
    string Stroke, string Fill, float StrokeWidth, float Opacity, float Roughness, int Seed, string Path,
    string Text, float TextSize, bool Renderable, bool Static, SketchStrokeStyle StrokeStyle,
    SketchArrowhead StartArrowhead, SketchArrowhead EndArrowhead, SketchTextAlign TextAlign, string FontFamily, float Rotation,
    string ImageAsset, uint ImageId, SketchImageFlip ImageFlip, SketchBinding? StartBinding, SketchBinding? EndBinding, SKRect? Clip,
    LineRoutingMode Routing, SKRect? RouteStart, SKRect? RouteEnd, SketchEdges Edges, SketchFillStyle FillStyle,
    string Pressures, SketchInkWidth InkWidth, SKRect? Crop, LineFlow Flow, LineFlowSpeed FlowSpeed, LineFlowDirection FlowDirection,
    float LabelPosition, int RouteSignature, int Jumps, double FlowTime = 0);

/// <summary>Lightweight sketch geometry on the existing scene and property contracts.</summary>
public sealed class SketchShape : SkiaComponent
{
    private SketchShapeKind _kind;
    private string _stroke = "#334155", _fill = "#00000000";
    private float _strokeWidth = 2;
    private SKPoint[] _points = Array.Empty<SKPoint>();
    private string _pathData = "";
    private float _roughness;
    private int _sketchSeed;
    private SketchStrokeStyle _strokeStyle;
    private SketchArrowhead _startArrowhead, _endArrowhead;
    private SketchTextAlign _textAlign;
    private string _fontFamily = TextFontFamily;
    private float _rotation;
    private LineRoutingMode _routing;
    private SketchEdges _edges;
    private string _link = "";
    private SketchFillStyle _fillStyle;
    private SKRect? _routeStart, _routeEnd;
    private (SketchShapeKind Kind, float Width, float Height, string Path, float Roughness, int Seed, (LineRoutingMode, SKRect?, SKRect?, int) Route,
        float X, float Y, SketchArrowhead Start, SketchArrowhead End, SketchEdges Edges)? _roughKey;
    private SKPoint[] _roughPoints = Array.Empty<SKPoint>();
    private (SketchShapeKind Kind, float X, float Y, float Width, float Height, string Path, float Roughness, int Seed, float StrokeWidth,
        SketchStrokeStyle Style, SketchArrowhead Start, SketchArrowhead End, (LineRoutingMode, SKRect?, SKRect?, int) Route, SketchEdges Edges, SketchFillStyle FillStyle,
        LineFlow Flow, int Jumps)? _drawKey;
    private SKPath _drawPath, _headStrokePath, _headFillPath, _hatchPath;
    private (string Fill, string Stroke, float Width, SketchStrokeStyle Style, float Opacity)? _paintKey;
    private SKPaint _fillPaint, _strokePaint, _solidStrokePaint, _headFillPaint, _hatchPaint;
    private SKPathEffect _dashEffect;
    private (SketchShapeKind Kind, float X, float Y, float Width, float Height, string Text, float Size, string Stroke, SketchTextAlign Align, string Family, float Opacity, string Path,
        (LineRoutingMode, SKRect?, SKRect?, int) Route, float LabelPosition)? _textKey;
    private SKPicture _textPicture;
    private SKRect _textInkBounds;
    private bool _textHasInkBounds;
    internal SketchVisualState VisualState => new(Kind, X, Y, Width, Height, Stroke, Fill, StrokeWidth, Opacity,
        Roughness, SketchSeed, PathData, DisplayText, TextFontSize,
        IsVisible && State != ComponentState.Disposing && State != ComponentState.Inactive, IsStatic,
        StrokeStyle, StartArrowhead, EndArrowhead, TextAlign, FontFamily, Rotation, ImageAsset, Image?.UniqueId ?? 0, ImageFlip, StartBinding, EndBinding, ClipBounds,
        _routing, _routeStart, _routeEnd, _edges, _fillStyle, _pressureData, _inkWidth, _crop, _flow, _flowSpeed, _flowDirection, _labelPosition,
        RouteSignature, LineJumps.Signature(_jumpStyle, _jumps));
    /// <summary>What a line's route depends on besides its points: the routing and the attached objects' bounds.</summary>
    /// <summary>What a line's drawn route depends on besides its points: the routing, the attached objects' bounds and,
    /// for a route that goes round other objects, the route itself (its signature).</summary>
    private (LineRoutingMode, SKRect?, SKRect?, int) RouteKey => (_routing, _routeStart, _routeEnd, RouteSignature);
    public const int MaxPathPoints = 4096;
    public SketchShape()
    {
        Width = 120; Height = 80;
        Kind = SketchShapeKind.Rectangle;
        Stroke = _stroke; Fill = _fill; StrokeWidth = _strokeWidth;
        PathData = "";
        DisplayText = ""; TextFontSize = 24; GroupId = Guid.Empty; Roughness = 0; SketchSeed = Id.GetHashCode() & int.MaxValue;
        OpacityPercent = 100; StrokeStyle = SketchStrokeStyle.Solid; StartArrowhead = SketchArrowhead.None; EndArrowhead = SketchArrowhead.None;
        TextAlign = SketchTextAlign.Left; FontFamily = TextFontFamily; Rotation = 0; ImageAsset = ""; ImageFlip = SketchImageFlip.None; StartBinding = null; EndBinding = null;
        Routing = LineRoutingMode.Straight; Edges = SketchEdges.Round; FillStyle = SketchFillStyle.Solid; // A rectangle's defaults.
        Link = ""; Crop = null; PressureData = ""; InkWidth = SketchInkWidth.Pressure;
        Flow = LineFlow.None; FlowSpeed = LineFlowSpeed.Normal; FlowDirection = LineFlowDirection.Forward; LabelPosition = 0.5f;
    }
    public SketchShape(Guid id) : this() { RestoreIdentity(id); }
    /// <summary>The original bundled family: the default for new text and for files written before font choice existed.</summary>
    public const string TextFontFamily = "Kalam";
    public const int MaxTextLength = 4096;
    public new string DisplayText
    {
        get => base.DisplayText;
        set
        {
            if (value == null || value.Length > MaxTextLength || value.Count(c => c == '\n') >= 64
                || value.Split('\n').Any(line => line.Length > 512) || value.Any(c => char.IsControl(c) && c != '\n'))
                throw new ArgumentException("Text permits 4096 characters, 64 lines and 512 characters per line.", nameof(value));
            base.DisplayText = value; UpsertNodeProperty(nameof(DisplayText), typeof(string), currentValue: value);
        }
    }
    public new float TextFontSize
    {
        get => base.TextFontSize;
        set
        {
            if (!float.IsFinite(value) || value < 8 || value > 120) throw new ArgumentOutOfRangeException(nameof(value));
            base.TextFontSize = value; UpsertNodeProperty(nameof(TextFontSize), typeof(string), currentValue: value.ToString("R", CultureInfo.InvariantCulture));
        }
    }
    public float Roughness
    {
        get => _roughness;
        set
        {
            if (!float.IsFinite(value) || value < 0 || value > 2) throw new ArgumentOutOfRangeException(nameof(value));
            _roughness = value; UpsertNodeProperty(nameof(Roughness), typeof(string), currentValue: value.ToString("R", CultureInfo.InvariantCulture));
        }
    }
    public int SketchSeed
    {
        get => _sketchSeed;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            _sketchSeed = value; UpsertNodeProperty(nameof(SketchSeed), typeof(string), currentValue: value.ToString(CultureInfo.InvariantCulture));
        }
    }
    /// <summary>Whole-object opacity from 0 (invisible) to 1. The component's opacity is the one owner; the document
    /// stores it as a whole percent ("Opacity"), kept in step by <see cref="OnOpacityChanged"/>.</summary>
    public new float Opacity
    {
        get => base.Opacity;
        set
        {
            if (!float.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value));
            base.Opacity = value;
        }
    }
    /// <summary>Opacity as a whole percent, 0 to 100.</summary>
    public int OpacityPercent
    {
        get => (int)MathF.Round(base.Opacity * 100);
        set
        {
            if (value < 0 || value > 100) throw new ArgumentOutOfRangeException(nameof(value));
            base.Opacity = value / 100f;
        }
    }
    protected override void OnOpacityChanged()
    {
        UpsertNodeProperty("Opacity", typeof(string), currentValue: OpacityPercent.ToString(CultureInfo.InvariantCulture));
        base.OnOpacityChanged();
    }
    /// <summary>Rectangles and diamonds: sharp or round corners (persisted as "Edges"). Setting <see cref="Kind"/> applies
    /// that kind's default: round for rectangles, sharp otherwise.</summary>
    public SketchEdges Edges
    {
        get => _edges;
        set
        {
            if (!Enum.IsDefined(typeof(SketchEdges), value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (value == SketchEdges.Round && Kind is not (SketchShapeKind.Rectangle or SketchShapeKind.Diamond))
                throw new ArgumentException("Only rectangles and diamonds have round edges.", nameof(value));
            _edges = value; UpsertNodeProperty(nameof(Edges), typeof(string), currentValue: value.ToString());
        }
    }
    /// <summary>Rectangles, ellipses and diamonds: a solid fill or lines of the fill colour (persisted as "FillStyle"),
    /// drawn the same way on every load from <see cref="SketchSeed"/>.</summary>
    public SketchFillStyle FillStyle
    {
        get => _fillStyle;
        set
        {
            if (!Enum.IsDefined(typeof(SketchFillStyle), value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (value != SketchFillStyle.Solid && !Fillable) throw new ArgumentException("Only rectangles, ellipses and diamonds have hatched fills.", nameof(value));
            _fillStyle = value; UpsertNodeProperty(nameof(FillStyle), typeof(string), currentValue: value.ToString());
        }
    }
    /// <summary>Where the object links to (persisted as "Link"): empty for none. The host decides which addresses and
    /// in-drawing targets it accepts; this only keeps the text to one line of at most 2,048 characters.</summary>
    public string Link
    {
        get => _link;
        set
        {
            if (value == null || value.Length > 2048 || value.Any(char.IsControl)) throw new ArgumentException("A link is one line of at most 2048 characters.", nameof(value));
            _link = value; UpsertNodeProperty(nameof(Link), typeof(string), currentValue: value);
        }
    }
    private bool Fillable => Kind is SketchShapeKind.Rectangle or SketchShapeKind.Ellipse or SketchShapeKind.Diamond;
    public SketchStrokeStyle StrokeStyle
    {
        get => _strokeStyle;
        set
        {
            if (!Enum.IsDefined(typeof(SketchStrokeStyle), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _strokeStyle = value; UpsertNodeProperty(nameof(StrokeStyle), typeof(string), currentValue: value.ToString());
        }
    }
    /// <summary>Arrowhead at the first point; arrows only. Setting <see cref="Kind"/> applies that kind's defaults.</summary>
    public SketchArrowhead StartArrowhead
    {
        get => _startArrowhead;
        set { ValidateArrowhead(value); _startArrowhead = value; UpsertNodeProperty(nameof(StartArrowhead), typeof(string), currentValue: value.ToString()); }
    }
    /// <summary>Arrowhead at the last point; arrows only. Setting <see cref="Kind"/> applies that kind's defaults.</summary>
    public SketchArrowhead EndArrowhead
    {
        get => _endArrowhead;
        set { ValidateArrowhead(value); _endArrowhead = value; UpsertNodeProperty(nameof(EndArrowhead), typeof(string), currentValue: value.ToString()); }
    }
    private void ValidateArrowhead(SketchArrowhead value)
    {
        if (!Enum.IsDefined(typeof(SketchArrowhead), value)) throw new ArgumentOutOfRangeException(nameof(value));
        if (value != SketchArrowhead.None && Kind != SketchShapeKind.Arrow) throw new ArgumentException("Only arrows have arrowheads.", nameof(value));
    }
    public SketchTextAlign TextAlign
    {
        get => _textAlign;
        set
        {
            if (!Enum.IsDefined(typeof(SketchTextAlign), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _textAlign = value; UpsertNodeProperty(nameof(TextAlign), typeof(string), currentValue: value.ToString());
        }
    }
    /// <summary>A family registered with <see cref="TypefaceCache.Register"/>; drawing and measuring require it.</summary>
    public string FontFamily
    {
        get => _fontFamily;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 64 || value != value.Trim()) throw new ArgumentException("Choose a bundled font family.", nameof(value));
            _fontFamily = value; UpsertNodeProperty(nameof(FontFamily), typeof(string), currentValue: value);
        }
    }
    /// <summary>Clockwise rotation in degrees about the centre, in [0, 360). Lines, arrows and freehand strokes keep 0:
    /// rotating them rotates their points instead, so endpoints and arrowheads stay exact.</summary>
    public float Rotation
    {
        get => _rotation;
        set
        {
            if (!float.IsFinite(value) || value < 0 || value >= 360) throw new ArgumentOutOfRangeException(nameof(value), "Use an angle from 0 up to 360 degrees.");
            _rotation = value; UpsertNodeProperty(nameof(Rotation), typeof(string), currentValue: value.ToString("R", CultureInfo.InvariantCulture));
            UpdateBounds();
        }
    }
    /// <summary>Normalises any finite angle to [0, 360).</summary>
    public static float NormalizeAngle(float degrees)
    {
        if (!float.IsFinite(degrees)) throw new ArgumentOutOfRangeException(nameof(degrees));
        var angle = degrees % 360; if (angle < 0) angle += 360;
        return angle >= 360 ? 0 : angle;
    }
    private string _imageAsset = "";
    /// <summary>Images only: the lowercase hex SHA-256 of the encoded image the drawing stores; empty for every other kind.</summary>
    public string ImageAsset
    {
        get => _imageAsset;
        set
        {
            if (value == null || value.Length != 0 && (value.Length != 64 || value.Any(c => !(c is >= '0' and <= '9' or >= 'a' and <= 'f'))))
                throw new ArgumentException("An image asset is a lowercase hex SHA-256.", nameof(value));
            _imageAsset = value; UpsertNodeProperty(nameof(ImageAsset), typeof(string), currentValue: value);
        }
    }
    /// <summary>Images only: the decoded image for <see cref="ImageAsset"/>. The host owns and disposes it; it is runtime
    /// state, never serialized. A shape prepared only for validation has none and draws nothing.</summary>
    public SKImage Image { get; set; }
    private SketchImageFlip _imageFlip;
    /// <summary>Images only: mirrors the pixels about the centre; every other kind keeps None.</summary>
    public SketchImageFlip ImageFlip
    {
        get => _imageFlip;
        set
        {
            if (!Enum.IsDefined(typeof(SketchImageFlip), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _imageFlip = value; UpsertNodeProperty(nameof(ImageFlip), typeof(string), currentValue: value.ToString());
        }
    }
    private SKRect? _crop;
    /// <summary>Images only: the part of the picture shown, as fractions of its width and height (left, top, right, bottom,
    /// at least 1% each way); null shows the whole picture. The shape's bounds show that part, so cropping never discards
    /// pixels: resetting the crop brings the rest back.</summary>
    public SKRect? Crop
    {
        get => _crop;
        set
        {
            if (value is { } c && !(float.IsFinite(c.Left) && float.IsFinite(c.Top) && float.IsFinite(c.Right) && float.IsFinite(c.Bottom)
                && c.Left >= 0 && c.Top >= 0 && c.Right <= 1 && c.Bottom <= 1 && c.Width >= MinCrop && c.Height >= MinCrop))
                throw new ArgumentException("A crop is a part of the picture, at least 1% of it each way.", nameof(value));
            _crop = value; UpsertNodeProperty(nameof(CropData), typeof(string), currentValue: CropData);
        }
    }
    public const float MinCrop = 0.01f;
    /// <summary><see cref="Crop"/> written "left,top,right,bottom" in invariant form, or empty for the whole picture.</summary>
    public string CropData
    {
        get => _crop is { } c ? string.Join(",", new[] { c.Left, c.Top, c.Right, c.Bottom }.Select(v => v.ToString("R", CultureInfo.InvariantCulture))) : "";
        set
        {
            if (value == null) throw new ArgumentException("Invalid crop.", nameof(value));
            if (value.Length == 0) { Crop = null; return; }
            var parts = value.Split(',');
            var numbers = new float[4];
            if (parts.Length != 4 || parts.Where((part, i) => !float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out numbers[i])).Any())
                throw new ArgumentException("A crop is written left,top,right,bottom.", nameof(value));
            Crop = new SKRect(numbers[0], numbers[1], numbers[2], numbers[3]);
        }
    }
    private float[] _pressures = Array.Empty<float>();
    private string _pressureData = "";
    /// <summary>Freehand strokes only: the pressure (0 to 1) at each path point, from a pen or simulated from the drawing
    /// speed; empty when the stroke has none. A change to the number of points clears them.</summary>
    public IReadOnlyList<float> Pressures => Array.AsReadOnly(_pressures);
    /// <summary><see cref="Pressures"/> written "p;p;…" in invariant form.</summary>
    public string PressureData
    {
        get => _pressureData;
        set
        {
            if (value == null || value.Length > MaxPathPoints * 8) throw new ArgumentException("Invalid pressure data.", nameof(value));
            var parts = value.Length == 0 ? Array.Empty<string>() : value.Split(';');
            if (parts.Length > MaxPathPoints) throw new ArgumentException("Too many pressures.", nameof(value));
            var pressures = new float[parts.Length];
            for (var i = 0; i < parts.Length; i++)
                if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out pressures[i]) || !float.IsFinite(pressures[i])
                    || pressures[i] < 0 || pressures[i] > 1)
                    throw new ArgumentException("Pressures are numbers from 0 to 1.", nameof(value));
            _pressures = pressures; _pressureData = value;
            UpsertNodeProperty(nameof(PressureData), typeof(string), currentValue: value);
        }
    }
    /// <summary>Pressures in the stored form: each rounded to three decimals.</summary>
    public static string FormatPressures(IEnumerable<float> pressures) =>
        string.Join(";", pressures.Select(p => MathF.Round(Math.Clamp(p, 0, 1), 3).ToString("R", CultureInfo.InvariantCulture)));
    private SketchInkWidth _inkWidth;
    /// <summary>Freehand strokes: whether their width follows their pressures (the default) or stays even.</summary>
    public SketchInkWidth InkWidth
    {
        get => _inkWidth;
        set
        {
            if (!Enum.IsDefined(typeof(SketchInkWidth), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _inkWidth = value; UpsertNodeProperty(nameof(InkWidth), typeof(string), currentValue: value.ToString());
        }
    }
    private LineFlow _flow;
    private LineFlowSpeed _flowSpeed;
    private LineFlowDirection _flowDirection;
    /// <summary>Lines and arrows: how the line shows movement along it (persisted as "Flow"). None by default; flowing
    /// dashes move the line's own pattern (a solid line flows as dashes); dots travel over the line.</summary>
    public LineFlow Flow
    {
        get => _flow;
        set
        {
            if (!Enum.IsDefined(typeof(LineFlow), value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (value != LineFlow.None && !Bindable) throw new ArgumentException("Only lines and arrows flow.", nameof(value));
            _flow = value; UpsertNodeProperty(nameof(Flow), typeof(string), currentValue: value.ToString());
            UpdateBounds();
        }
    }
    /// <summary>How fast the flow moves (persisted as "FlowSpeed").</summary>
    public LineFlowSpeed FlowSpeed
    {
        get => _flowSpeed;
        set
        {
            if (!Enum.IsDefined(typeof(LineFlowSpeed), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _flowSpeed = value; UpsertNodeProperty(nameof(FlowSpeed), typeof(string), currentValue: value.ToString());
        }
    }
    /// <summary>Which way the flow moves along the line (persisted as "FlowDirection").</summary>
    public LineFlowDirection FlowDirection
    {
        get => _flowDirection;
        set
        {
            if (!Enum.IsDefined(typeof(LineFlowDirection), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _flowDirection = value; UpsertNodeProperty(nameof(FlowDirection), typeof(string), currentValue: value.ToString());
        }
    }
    private float _labelPosition = 0.5f;
    /// <summary>Lines and arrows: where the label sits along the line, from 0 (its start) to 1 (its end); the middle by
    /// default (persisted as "LabelPosition").</summary>
    public float LabelPosition
    {
        get => _labelPosition;
        set
        {
            if (!float.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value), "Place the label from 0 (the start) to 1 (the end).");
            _labelPosition = value; UpsertNodeProperty(nameof(LabelPosition), typeof(string), currentValue: value.ToString("R", CultureInfo.InvariantCulture));
            UpdateBounds();
        }
    }
    /// <summary>Whether this is drawn as a stroke whose width follows its pressures: a solid freehand stroke with a pressure
    /// for each point.</summary>
    public bool DrawsPressure => Kind == SketchShapeKind.Freehand && _inkWidth == SketchInkWidth.Pressure && _pressures.Length >= 2
        && _pressures.Length == _points.Length && StrokeStyle == SketchStrokeStyle.Solid;
    private SketchBinding? _startBinding, _endBinding;
    /// <summary>Lines and arrows: the object the first point is attached to, if any (persisted as "StartBinding").</summary>
    public SketchBinding? StartBinding
    {
        get => _startBinding;
        set { ValidateBinding(value); _startBinding = value; UpsertNodeProperty(nameof(StartBinding), typeof(string), currentValue: value?.ToString() ?? ""); }
    }
    /// <summary>Lines and arrows: the object the last point is attached to, if any (persisted as "EndBinding").</summary>
    public SketchBinding? EndBinding
    {
        get => _endBinding;
        set { ValidateBinding(value); _endBinding = value; UpsertNodeProperty(nameof(EndBinding), typeof(string), currentValue: value?.ToString() ?? ""); }
    }
    private void ValidateBinding(SketchBinding? value)
    {
        if (value is not { } binding) return;
        if (!Bindable) throw new ArgumentException("Only lines and arrows attach to other objects.", nameof(value));
        if (binding.Target == Id || binding.Target == Guid.Empty || !float.IsFinite(binding.U) || !float.IsFinite(binding.V)
            || binding.U < 0 || binding.U > 1 || binding.V < 0 || binding.V > 1)
            throw new ArgumentException("Attach to another object, at a point within its bounds.", nameof(value));
    }
    /// <summary>Lines and arrows attach their ends to objects and can carry a label at their midpoint.</summary>
    public bool Bindable => Kind is SketchShapeKind.Line or SketchShapeKind.Arrow;
    /// <summary>
    /// Lines and arrows: how the line runs through its points (persisted as "Routing"). Straight joins them; orthogonal
    /// (elbow) runs at right angles, leaving and entering attached objects away from them; curved bends smoothly through
    /// them. Corners and curves are derived from the points and attachments (<see cref="Route"/>), never stored.
    /// </summary>
    public LineRoutingMode Routing
    {
        get => _routing;
        set
        {
            if (!Enum.IsDefined(typeof(LineRoutingMode), value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (value != LineRoutingMode.Straight && !Bindable) throw new ArgumentException("Only lines and arrows are routed.", nameof(value));
            _routing = value; UpsertNodeProperty(nameof(Routing), typeof(string), currentValue: value.ToString());
            UpdateBounds();
        }
    }
    /// <summary>The bounds of the objects the start and end are attached to, which an elbow route leaves and enters away
    /// from and goes round. The host supplies them from the attachments; they are derived, never stored.</summary>
    public (SKRect? Start, SKRect? End) RouteEnds
    {
        get => (_routeStart, _routeEnd);
        set { _routeStart = value.Start; _routeEnd = value.End; UpdateBounds(); }
    }
    /// <summary>The drawn line in world coordinates: the points themselves when straight, otherwise their route
    /// (<see cref="ConnectionRoute.Through"/>). Drawing, hit testing, arrowheads, the label and bounds all use it.</summary>
    public IReadOnlyList<SKPoint> Route()
    {
        var key = (_pathData, X, Y, Width, Height, _routing, _routeStart, _routeEnd, _obstacles, _startBinding?.Target, _endBinding?.Target, Kind);
        if (_routeKey == key && _route != null) return _route;
        var points = new SKPoint[_points.Length];
        for (int i = 0; i < points.Length; i++) points[i] = WorldPoint(i);
        // An attached two-point elbow line goes round the page's other objects; points a person placed are kept as they are.
        _route = !Bindable || _routing == LineRoutingMode.Straight || points.Length < 2 ? points
            : points.Length == 2 && _obstacles != null
                ? ConnectionRoute.Points(_routing, points[0], _routeStart, points[1], _routeEnd, _obstacles, _startBinding?.Target ?? Guid.Empty, _endBinding?.Target ?? Guid.Empty)
                : ConnectionRoute.Through(_routing, points, _routeStart, _routeEnd);
        _routeKey = key;
        var hash = new HashCode();
        foreach (var p in _route) hash.Add(p);
        _routeSignature = _obstacles == null ? 0 : hash.ToHashCode();
        return _route;
    }
    private SKPoint[] _route;
    private (string, float, float, float, float, LineRoutingMode, SKRect?, SKRect?, RouteObstacles, Guid?, Guid?, SketchShapeKind)? _routeKey;
    private int _routeSignature;
    /// <summary>Distinguishes routes that went round different objects; zero while no obstacles are given.</summary>
    private int RouteSignature { get { if (Bindable && _obstacles != null) Route(); return Bindable && _obstacles != null ? _routeSignature : 0; } }
    private RouteObstacles _obstacles;
    /// <summary>The page's other objects, which an attached two-point elbow route goes round. The host supplies them (one
    /// shared snapshot for the page); they are derived, never stored. Null keeps the route round the two attached objects only.</summary>
    public RouteObstacles Obstacles
    {
        get => _obstacles;
        set { if (ReferenceEquals(_obstacles, value)) return; _obstacles = value; UpdateBounds(); }
    }
    private LineJumpStyle _jumpStyle;
    private float[] _jumps = Array.Empty<float>();
    /// <summary>Where this line jumps over lines drawn before it (distances along its route, ascending) and how. The host
    /// supplies them from <see cref="LineJumps.Find"/>; they are derived, never stored. Rough lines draw no jumps.</summary>
    public (LineJumpStyle Style, IReadOnlyList<float> At) Jumps
    {
        get => (_jumpStyle, _jumps);
        set
        {
            if (!Enum.IsDefined(typeof(LineJumpStyle), value.Style)) throw new ArgumentOutOfRangeException(nameof(value));
            _jumpStyle = value.Style; _jumps = value.At == null ? Array.Empty<float>() : value.At.ToArray();
        }
    }
    /// <summary>The route as drawn: an outlined arrowhead's shaft stops at the head, so the outline stays clear.</summary>
    private IReadOnlyList<SKPoint> DrawnRoute()
    {
        var route = Route();
        if (Kind != SketchShapeKind.Arrow || route.Count < 2) return route;
        var drawn = route.ToArray();
        foreach (var (head, start) in new[] { (StartArrowhead, true), (EndArrowhead, false) })
        {
            var inset = HeadInset(head, HeadFrame(start));
            if (inset <= 0) continue;
            var f = HeadFrame(start); var direction = f.Tip - new SKPoint((f.Left.X + f.Right.X) / 2, (f.Left.Y + f.Right.Y) / 2);
            if (direction.Length <= 0) continue;
            var unit = new SKPoint(direction.X / direction.Length, direction.Y / direction.Length);
            drawn[start ? 0 : drawn.Length - 1] = f.Tip - new SKPoint(unit.X * inset, unit.Y * inset);
        }
        return drawn;
    }
    /// <summary>How far the shaft stops short of the tip under each arrowhead: only outlined heads keep their inside clear.</summary>
    private static float HeadInset(SketchArrowhead head, (SKPoint Tip, SKPoint Left, SKPoint Right, float Size) f) =>
        Arrowheads.Inset(head, new(f.Tip, f.Left, f.Right, f.Size));
    /// <summary>Rectangles, ellipses and diamonds can hold a wrapped label in <see cref="DisplayText"/>.</summary>
    public bool IsContainer => Kind is SketchShapeKind.Rectangle or SketchShapeKind.Ellipse or SketchShapeKind.Diamond;
    private bool Rotated => _rotation != 0 && !IsPath;
    private SKPoint Center => new(X + Width / 2, Y + Height / 2);
    public static SKPoint RotatePoint(SKPoint point, SKPoint center, float degrees)
    {
        if (degrees == 0) return point;
        var radians = degrees * MathF.PI / 180; var cos = MathF.Cos(radians); var sin = MathF.Sin(radians);
        var dx = point.X - center.X; var dy = point.Y - center.Y;
        return new(center.X + dx * cos - dy * sin, center.Y + dx * sin + dy * cos);
    }
    /// <summary>Maps a world point into the shape's unrotated frame.</summary>
    public SKPoint ToLocal(SKPoint world) => Rotated ? RotatePoint(world, Center, -_rotation) : world;
    /// <summary>Maps a point of the unrotated frame into the world.</summary>
    public SKPoint ToWorld(SKPoint local) => Rotated ? RotatePoint(local, Center, _rotation) : local;
    /// <summary>The rotated corners of the shape's bounds: top-left, top-right, bottom-right, bottom-left.</summary>
    public SKPoint[] WorldCorners() => new[] { ToWorld(new SKPoint(X, Y)), ToWorld(new SKPoint(X + Width, Y)),
        ToWorld(new SKPoint(X + Width, Y + Height)), ToWorld(new SKPoint(X, Y + Height)) };
    private SKRect RotatedBounds(SKRect local)
    {
        if (!Rotated || local.IsEmpty) return local;
        var corners = new[] { ToWorld(new SKPoint(local.Left, local.Top)), ToWorld(new SKPoint(local.Right, local.Top)),
            ToWorld(new SKPoint(local.Right, local.Bottom)), ToWorld(new SKPoint(local.Left, local.Bottom)) };
        return new SKRect(corners.Min(p => p.X), corners.Min(p => p.Y), corners.Max(p => p.X), corners.Max(p => p.Y));
    }
    /// <summary>Bounds are the axis-aligned footprint of the rotated shape, so selection, marquee and culling follow rotation;
    /// a routed line's bounds hold its whole route, which can leave the box of its points.</summary>
    protected override void UpdateBounds()
    {
        var bounds = RotatedBounds(new SKRect(X, Y, X + Width, Y + Height));
        if (Bindable && _routing != LineRoutingMode.Straight && _points.Length >= 2)
            foreach (var p in Route()) bounds = SKRect.Union(bounds, new SKRect(p.X, p.Y, p.X, p.Y));
        Bounds = bounds;
    }

    /// <summary>Text objects only: the size the text needs, in the chosen font.</summary>
    public SKSize MeasureText()
    {
        using var font = CreateTextFont();
        var lines = Layout(font, out _);
        return new(Math.Max(1, lines.Max(line => line.Width)) + 8, lines.Count * TextFontSize * 1.4f + 8);
    }
    /// <summary>Containers only: the smallest height at <paramref name="width"/> that shows every line of the label.</summary>
    public float LabelHeight(float width)
    {
        if (!IsContainer) throw new InvalidOperationException("Only rectangles, ellipses and diamonds hold labels.");
        if (DisplayText.Length == 0) return 0;
        using var font = CreateTextFont();
        var box = LabelBox(X, Y, width, Height);
        var text = TextBlock.Lines(FontFamily, font, DisplayText, Math.Max(1, box.Width)).Count * TextFontSize * 1.4f;
        return Kind switch
        {
            SketchShapeKind.Rectangle => text + 2 * RectangleLabelPadding,
            SketchShapeKind.Ellipse => (text + 2 * InnerLabelPadding) * MathF.Sqrt(2),
            SketchShapeKind.Diamond => (text + 2 * InnerLabelPadding) * 2,
            SketchShapeKind.Line or SketchShapeKind.Arrow or SketchShapeKind.Freehand or SketchShapeKind.Text or SketchShapeKind.Image
                or SketchShapeKind.Frame => throw new InvalidOperationException("Only rectangles, ellipses and diamonds hold labels."),
        };
    }
    private const float RectangleLabelPadding = 8, InnerLabelPadding = 4;
    /// <summary>The unrotated area a label wraps in: inside the rectangle, or the rectangle inscribed in an ellipse or diamond.</summary>
    private SKRect LabelBox(float x, float y, float width, float height)
    {
        float w, h, inset;
        if (Kind == SketchShapeKind.Rectangle) { w = width; h = height; inset = RectangleLabelPadding; }
        else if (Kind == SketchShapeKind.Ellipse) { w = width / MathF.Sqrt(2); h = height / MathF.Sqrt(2); inset = InnerLabelPadding; }
        else { w = width / 2; h = height / 2; inset = InnerLabelPadding; }
        var cx = x + width / 2; var cy = y + height / 2;
        var halfW = Math.Max(0.5f, w / 2 - inset); var halfH = Math.Max(0.5f, h / 2 - inset);
        return new SKRect(cx - halfW, cy - halfH, cx + halfW, cy + halfH);
    }
    private List<GlyphLine> Layout(SKFont font, out SKRect box)
    {
        List<GlyphLine> lines;
        if (Kind == SketchShapeKind.Frame)
        {
            // A frame's title sits on one line above its top-left corner.
            lines = TextBlock.Lines(FontFamily, font, DisplayText);
            var titleWidth = Math.Max(1, lines.Count == 0 ? 0 : lines.Max(l => l.Width)); var titleHeight = Math.Max(1, lines.Count) * TextFontSize * 1.4f;
            box = new SKRect(X, Y - FrameTitleGap - titleHeight, X + titleWidth, Y - FrameTitleGap);
            return lines;
        }
        if (Bindable)
        {
            // A line or arrow label is centred on its place along the line: the middle (of its two points, or halfway along
            // its route) unless it was moved.
            lines = TextBlock.Lines(FontFamily, font, DisplayText);
            var mid = _labelPosition != 0.5f ? ConnectionRoute.At(Route(), _labelPosition)
                : _points.Length == 2 && _routing == LineRoutingMode.Straight
                ? new SKPoint((WorldPoint(0).X + WorldPoint(_points.Length - 1).X) / 2, (WorldPoint(0).Y + WorldPoint(_points.Length - 1).Y) / 2)
                : ConnectionRoute.Middle(Route());
            var w = Math.Max(1, lines.Count == 0 ? 0 : lines.Max(l => l.Width)); var h = Math.Max(1, lines.Count) * TextFontSize * 1.4f;
            box = new SKRect(mid.X - w / 2, mid.Y - h / 2, mid.X + w / 2, mid.Y + h / 2);
            _pathLabelBox = box;
            return lines;
        }
        if (Kind == SketchShapeKind.Text)
        {
            box = new SKRect(X + 4, Y + 4, X + Width - 4, Y + Height - 4);
            return TextBlock.Lines(FontFamily, font, DisplayText);
        }
        box = LabelBox(X, Y, Width, Height);
        return TextBlock.Lines(FontFamily, font, DisplayText, Math.Max(1, box.Width));
    }
    protected override void DrawText(SKCanvas canvas, DrawingContext context)
    {
        if (!HasText) return;
        EnsureTextPicture();
        canvas.DrawPicture(_textPicture);
    }
    private bool HasText => DisplayText.Length != 0 && (Kind is SketchShapeKind.Text or SketchShapeKind.Frame || IsContainer || Bindable && _points.Length >= 2);
    /// <summary>Space between a frame's border and its title, in drawing units.</summary>
    public const float FrameTitleGap = 4;
    /// <summary>Frames only: the world area the title covers above the frame.</summary>
    public SKRect? FrameTitleBounds
    {
        get
        {
            if (Kind != SketchShapeKind.Frame || DisplayText.Length == 0) return null;
            using var font = CreateTextFont(); Layout(font, out var box); return box;
        }
    }
    private SKRect _pathLabelBox;
    /// <summary>Lines and arrows with a label: the world area the label covers, centred on the line, where the line is not drawn.</summary>
    public SKRect? LineLabelBounds
    {
        get
        {
            if (!Bindable || !HasText) return null;
            EnsureTextPicture();
            var box = _pathLabelBox; box.Inflate(4, 2); return box;
        }
    }
    private void EnsureTextPicture(bool captureBounds = false)
    {
        var key = (Kind, X, Y, Width, Height, DisplayText, TextFontSize, Stroke, TextAlign, FontFamily, base.Opacity, PathData, RouteKey, _labelPosition);
        if (_textKey != key || _textPicture == null || captureBounds && !_textHasInkBounds)
        {
            using var recorder = new SKPictureRecorder();
            var cull = new SKRect(X - TextFontSize * 2, Y - TextFontSize * 2, X + Width + TextFontSize * 2, Y + Height + TextFontSize * 2);
            var recorded = recorder.BeginRecording(cull);
            _textInkBounds = DrawTextBlock(recorded, captureBounds);
            _textHasInkBounds = captureBounds;
            var picture = recorder.EndRecording();
            _textPicture?.Dispose(); _textPicture = picture; _textKey = key;
        }
    }
    private SKRect DrawTextBlock(SKCanvas canvas, bool captureBounds)
    {
        using var font = CreateTextFont();
        using var paint = new SKPaint { IsAntialias = true, Color = Faded(Stroke) };
        var lines = Layout(font, out var box);
        var lineHeight = TextFontSize * 1.4f;
        // Text objects start at their top padding; labels are centred vertically in their box.
        var baseline = (Kind == SketchShapeKind.Text ? Y + 4 : Bindable || Kind == SketchShapeKind.Frame ? box.Top : box.MidY - lines.Count * lineHeight / 2) - font.Metrics.Ascent;
        return TextBlock.Draw(canvas, font, lines, box, TextAlign, baseline, lineHeight, paint, captureBounds);
    }
    /// <summary>Conservative bounds of this leaf's actual world-space drawing commands.
    /// Returns false when child content makes the footprint unknown. Empty means no ink.
    /// Transform the result and add at least two device pixels for antialias coverage;
    /// this contract does not by itself track damage, connections or host overlays.</summary>
    public bool TryGetInkBounds(out SKRect bounds)
    {
        bounds = SKRect.Empty;
        if (Children.Count != 0) return false;
        if (!IsVisible || State == ComponentState.Disposing || State == ComponentState.Inactive) return true;
        bounds = RotatedBounds(LocalInkBounds());
        // Nothing is painted outside the area the host clips this shape to.
        if (ClipBounds is { } clip && !bounds.IsEmpty) bounds = bounds.IntersectsWith(clip) ? SKRect.Intersect(bounds, clip) : SKRect.Empty;
        return true;
    }
    private SKRect LocalInkBounds()
    {
        var bounds = SKRect.Empty;
        void Add(SKRect r, float inflate)
        {
            if (r.IsEmpty) return;
            r.Inflate(inflate, inflate); bounds = bounds.IsEmpty ? r : SKRect.Union(bounds, r);
        }
        if (Kind == SketchShapeKind.Image) Add(new SKRect(X, Y, X + Width, Y + Height), 0);
        else if (Kind != SketchShapeKind.Text)
        {
            var path = GetDrawPath();
            if (DrawsPressure) Add(InkOutline().Bounds, 1);
            else if (path != null && !path.IsEmpty) Add(path.Bounds, Bindable ? FlowAnimation.Reach(_flow, StrokeWidth) : StrokeWidth / 2);
            if (_headStrokePath != null && !_headStrokePath.IsEmpty) Add(_headStrokePath.Bounds, StrokeWidth / 2);
            if (_headFillPath != null && !_headFillPath.IsEmpty) Add(_headFillPath.Bounds, StrokeWidth / 2);
        }
        if (HasText) { EnsureTextPicture(true); Add(_textInkBounds, 0); }
        return bounds;
    }
    private SKFont CreateTextFont() => TextBlock.Font(FontFamily, TextFontSize);
    /// <summary>Cached local sketch outline. Fixed xorshift algorithm preserves seed behavior across runtimes.</summary>
    public IReadOnlyList<SKPoint> SketchOutline()
    {
        // A routed line, or one whose shaft stops at an outlined head, follows its drawn route; other paths their points.
        var routed = Bindable && (_routing != LineRoutingMode.Straight || DrawnRouteDiffers());
        var key = (Kind, Width, Height, PathData, Roughness, SketchSeed, RouteKey, routed ? X : 0, routed ? Y : 0, StartArrowhead, EndArrowhead, Edges);
        if (_roughKey == key) return Array.AsReadOnly(_roughPoints);
        var points = new List<SKPoint>();
        if (IsPath && routed)
        {
            foreach (var p in DrawnRoute()) points.Add(new(p.X - X, p.Y - Y));
            if (points.Count == 2) points.Insert(1, new((points[0].X + points[1].X) / 2, (points[0].Y + points[1].Y) / 2));
        }
        else if (IsPath)
        {
            for (int i = 0; i < _points.Length; i++) points.Add(new(_points[i].X * Width, _points[i].Y * Height));
            if (points.Count == 2) points.Insert(1, new((points[0].X + points[1].X) / 2, (points[0].Y + points[1].Y) / 2));
        }
        else if (Kind == SketchShapeKind.Ellipse)
        {
            for (int i = 0; i <= 64; i++) { var angle = i * Math.PI / 32; points.Add(new((float)(Width / 2 * (1 + Math.Cos(angle))), (float)(Height / 2 * (1 + Math.Sin(angle))))); }
        }
        else if (Edges == SketchEdges.Round && Kind is SketchShapeKind.Rectangle or SketchShapeKind.Diamond)
        {
            points.AddRange(Outline(new SKRect(0, 0, Width, Height)));
            points.Add(points[0]);
        }
        else if (Kind is not (SketchShapeKind.Text or SketchShapeKind.Image))
        {
            SKPoint[] corners = Kind == SketchShapeKind.Diamond
                ? new[] { new SKPoint(Width / 2, 0), new SKPoint(Width, Height / 2), new SKPoint(Width / 2, Height), new SKPoint(0, Height / 2) }
                : new[] { new SKPoint(0, 0), new SKPoint(Width, 0), new SKPoint(Width, Height), new SKPoint(0, Height) };
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 8; j++) { var a = corners[i]; var b = corners[(i + 1) % 4]; points.Add(new(a.X + (b.X - a.X) * j / 8, a.Y + (b.Y - a.Y) * j / 8)); }
            points.Add(points[0]);
        }
        uint state = (uint)SketchSeed | 1u;
        float Noise() { state ^= state << 13; state ^= state >> 17; state ^= state << 5; return (state / (float)uint.MaxValue * 2 - 1) * Roughness; }
        // A curve is drawn smooth: noise on its many flattened points would make it wobble.
        if (!(routed && _routing == LineRoutingMode.Curved))
            for (int i = IsPath ? 1 : 0; i < points.Count - 1; i++) points[i] += new SKPoint(Noise(), Noise());
        if (!IsPath && points.Count > 0) points[^1] = points[0];
        _roughPoints = points.ToArray(); _roughKey = key; return Array.AsReadOnly(_roughPoints);
    }
    /// <summary>Ordered points normalized to the shape bounds; direction survives resizing and serialization.</summary>
    public IReadOnlyList<SKPoint> PathPoints => Array.AsReadOnly(_points);
    public string PathData
    {
        get => _pathData;
        set
        {
            if (value == null || value.Length > 120_000) throw new ArgumentException("Invalid path data.", nameof(value));
            var parts = value.Length == 0 ? Array.Empty<string>() : value.Split(';');
            if (parts.Length > MaxPathPoints) throw new ArgumentException("Too many path points.", nameof(value));
            var points = new SKPoint[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                var xy = parts[i].Split(',');
                if (xy.Length != 2 || !float.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                    || !float.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y)
                    || !float.IsFinite(x) || !float.IsFinite(y) || x < 0 || x > 1 || y < 0 || y > 1)
                    throw new ArgumentException("Path points must be finite normalized coordinates.", nameof(value));
                points[i] = new SKPoint(x, y);
            }
            _points = points; _pathData = value;
            UpsertNodeProperty(nameof(PathData), typeof(string), currentValue: value);
            if (_pressures.Length != 0 && _pressures.Length != points.Length) PressureData = "";
            if (_routing != LineRoutingMode.Straight) UpdateBounds(); // The route moves with the points even in the same box.
        }
    }
    public bool IsPath => Kind is SketchShapeKind.Line or SketchShapeKind.Arrow or SketchShapeKind.Freehand;
    public void SetWorldPath(IReadOnlyList<SKPoint> points)
    {
        if (points == null || points.Count < 2 || points.Count > MaxPathPoints
            || points.Any(p => !float.IsFinite(p.X) || !float.IsFinite(p.Y)))
            throw new ArgumentException("A path needs 2 to 4096 finite points.", nameof(points));
        var left = points.Min(p => p.X); var top = points.Min(p => p.Y);
        var width = Math.Max(0.01f, points.Max(p => p.X) - left);
        var height = Math.Max(0.01f, points.Max(p => p.Y) - top);
        PathData = string.Join(";", points.Select(p =>
            ((p.X - left) / width).ToString("R", CultureInfo.InvariantCulture) + "," +
            ((p.Y - top) / height).ToString("R", CultureInfo.InvariantCulture)));
        X = left; Y = top; Width = width; Height = height;
    }
    /// <summary>Sets a freehand stroke's points and the pressure at each.</summary>
    public void SetWorldPath(IReadOnlyList<SKPoint> points, IReadOnlyList<float> pressures)
    {
        if (pressures == null || pressures.Count != points?.Count) throw new ArgumentException("A stroke needs one pressure for each point.", nameof(pressures));
        SetWorldPath(points);
        PressureData = FormatPressures(pressures);
    }
    public SKPoint WorldPoint(int index) => new(X + _points[index].X * Width, Y + _points[index].Y * Height);
    public static float DistanceToSegment(SKPoint p, SKPoint a, SKPoint b)
    {
        var d = b - a; var length = d.X * d.X + d.Y * d.Y;
        var t = length == 0 ? 0 : Math.Clamp(((p.X - a.X) * d.X + (p.Y - a.Y) * d.Y) / length, 0, 1);
        return (p - (a + new SKPoint(d.X * t, d.Y * t))).Length;
    }
    private static float SegmentDistance(SKPoint a, SKPoint b, SKPoint c, SKPoint d)
    {
        var r = b - a; var s = d - c;
        static float Cross(SKPoint x, SKPoint y) => x.X * y.Y - x.Y * y.X;
        var denominator = Cross(r, s);
        if (denominator != 0)
        {
            var t = Cross(c - a, s) / denominator; var u = Cross(c - a, r) / denominator;
            if (t >= 0 && t <= 1 && u >= 0 && u <= 1) return 0;
        }
        return Math.Min(Math.Min(DistanceToSegment(a, c, d), DistanceToSegment(b, c, d)),
            Math.Min(DistanceToSegment(c, a, b), DistanceToSegment(d, a, b)));
    }
    /// <summary>Tests the whole eraser sweep, including sparse or fast pointer movement.</summary>
    public bool IntersectsSegment(SKPoint a, SKPoint b, float tolerance)
    {
        var distance = Math.Max(0, tolerance) + StrokeWidth / 2;
        if (IsPath)
        {
            var route = Route();
            for (int i = 1; i < route.Count; i++)
                if (SegmentDistance(a, b, route[i - 1], route[i]) <= distance) return true;
            foreach (var head in HeadShapes())
            {
                if (head.Fill && (ContainsInPolygon(a, head.Points) || ContainsInPolygon(b, head.Points))) return true;
                for (int i = 1; i < head.Points.Length; i++)
                    if (SegmentDistance(a, b, head.Points[i - 1], head.Points[i]) <= distance) return true;
            }
            return false;
        }
        if (Width <= 0 || Height <= 0) return false;
        a = ToLocal(a); b = ToLocal(b);
        if (ContainsLocal(a, tolerance) || ContainsLocal(b, tolerance)) return true;
        var cx = X + Width / 2; var cy = Y + Height / 2;
        if (Kind == SketchShapeKind.Ellipse)
        {
            var rx = Width / 2 + distance; var ry = Height / 2 + distance;
            return DistanceToSegment(SKPoint.Empty, new((a.X - cx) / rx, (a.Y - cy) / ry),
                new((b.X - cx) / rx, (b.Y - cy) / ry)) <= 1;
        }
        SKPoint[] corners = Kind == SketchShapeKind.Diamond
            ? new[] { new SKPoint(cx, Y), new SKPoint(X + Width, cy), new SKPoint(cx, Y + Height), new SKPoint(X, cy) }
            : new[] { new SKPoint(X, Y), new SKPoint(X + Width, Y), new SKPoint(X + Width, Y + Height), new SKPoint(X, Y + Height) };
        for (int i = 0; i < corners.Length; i++)
            if (SegmentDistance(a, b, corners[i], corners[(i + 1) % corners.Length]) <= distance) return true;
        return false;
    }
    /// <summary>The two base points of the end arrowhead (also used to size export margins).</summary>
    public (SKPoint Left, SKPoint Right) ArrowHead()
    {
        var head = HeadFrame(start: false);
        return (head.Left, head.Right);
    }
    private (SKPoint Tip, SKPoint Left, SKPoint Right, float Size) HeadFrame(bool start)
    {
        var route = Route();
        var tip = start ? route[0] : route[route.Count - 1];
        // A curve is flattened into short pieces: its head points along the last 12 units and is sized by its length.
        var previous = _routing == LineRoutingMode.Curved && Bindable ? Behind(route, start, 12) : start ? route[1] : route[route.Count - 2];
        var d = tip - previous; var length = _routing == LineRoutingMode.Curved && Bindable ? RouteLength(route) : d.Length;
        // The head points along d; a curve is sized by its whole length, a straight piece by its own.
        var along = d.Length;
        if (length <= 0 || along <= 0) return (tip, tip, tip, 0);
        var size = Math.Min(length * 0.45f, Math.Max(10, StrokeWidth * 4));
        var ux = d.X / along; var uy = d.Y / along;
        return (tip, new(tip.X - ux * size - uy * size * .5f, tip.Y - uy * size + ux * size * .5f),
            new(tip.X - ux * size + uy * size * .5f, tip.Y - uy * size - ux * size * .5f), size);
    }
    /// <summary>Arrowhead outlines in world space: open polylines are stroked; filled ones are closed polygons.</summary>
    private IEnumerable<(SKPoint[] Points, bool Fill)> HeadShapes()
    {
        if (Kind != SketchShapeKind.Arrow || _points.Length < 2) yield break;
        foreach (var (head, start) in new[] { (StartArrowhead, true), (EndArrowhead, false) })
        {
            var f = HeadFrame(start);
            foreach (var shape in Arrowheads.Shapes(head, new(f.Tip, f.Left, f.Right, f.Size))) yield return shape;
        }
    }
    /// <summary>The point <paramref name="distance"/> back along the route from its start or end (or its other end).</summary>
    private static SKPoint Behind(IReadOnlyList<SKPoint> route, bool start, float distance)
    {
        var walked = 0f;
        for (int k = 1; k < route.Count; k++)
        {
            var a = route[start ? k - 1 : route.Count - k]; var b = route[start ? k : route.Count - 1 - k];
            var length = SKPoint.Distance(a, b);
            if (walked + length >= distance && length > 0) { var t = (distance - walked) / length; return new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t); }
            walked += length;
        }
        return start ? route[route.Count - 1] : route[0];
    }
    private static float RouteLength(IReadOnlyList<SKPoint> route)
    {
        float total = 0;
        for (int i = 1; i < route.Count; i++) total += SKPoint.Distance(route[i - 1], route[i]);
        return total;
    }
    /// <summary>True when an outlined arrowhead shortens the drawn shaft.</summary>
    private bool DrawnRouteDiffers() => Kind == SketchShapeKind.Arrow && _points.Length >= 2
        && (HeadInset(StartArrowhead, HeadFrame(true)) > 0 || HeadInset(EndArrowhead, HeadFrame(false)) > 0);
    private static bool ContainsInPolygon(SKPoint p, SKPoint[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            if (polygon[i].Y > p.Y != polygon[j].Y > p.Y
                && p.X < (polygon[j].X - polygon[i].X) * (p.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X) inside = !inside;
        return inside;
    }
    public SketchShapeKind Kind
    {
        get => _kind;
        set
        {
            if (!Enum.IsDefined(typeof(SketchShapeKind), value)) throw new ArgumentOutOfRangeException(nameof(value));
            var previous = _kind; _kind = value;
            UpsertNodeProperty(nameof(Kind), typeof(string), currentValue: value.ToString());
            // Changing kind applies that kind's arrowheads: an arrow points at its end; other kinds have none.
            if (previous != value || value != SketchShapeKind.Arrow && (_startArrowhead != SketchArrowhead.None || _endArrowhead != SketchArrowhead.None))
            {
                StartArrowhead = SketchArrowhead.None;
                EndArrowhead = value == SketchShapeKind.Arrow ? SketchArrowhead.Arrow : SketchArrowhead.None;
            }
            // Only lines and arrows attach their ends.
            if (!Bindable && (_startBinding != null || _endBinding != null)) { StartBinding = null; EndBinding = null; }
            // Only lines and arrows are routed.
            if (!Bindable && _routing != LineRoutingMode.Straight) Routing = LineRoutingMode.Straight;
            // Only lines and arrows flow.
            if (!Bindable && _flow != LineFlow.None) Flow = LineFlow.None;
            // A new kind takes its own corners; only closed shapes keep a hatched fill.
            if (previous != value) Edges = value == SketchShapeKind.Rectangle ? SketchEdges.Round : SketchEdges.Sharp;
            if (!Fillable && _fillStyle != SketchFillStyle.Solid) FillStyle = SketchFillStyle.Solid;
            UpdateBounds();
        }
    }
    public string Stroke
    {
        get => _stroke;
        set { ValidateColor(value); _stroke = value; UpsertNodeProperty(nameof(Stroke), typeof(string), currentValue: value); }
    }
    public string Fill
    {
        get => _fill;
        set { ValidateColor(value); _fill = value; UpsertNodeProperty(nameof(Fill), typeof(string), currentValue: value); }
    }
    public float StrokeWidth
    {
        get => _strokeWidth;
        set
        {
            if (!float.IsFinite(value) || value < 0.5f || value > 20) throw new ArgumentOutOfRangeException(nameof(value));
            _strokeWidth = value;
            UpsertNodeProperty(nameof(StrokeWidth), typeof(string), currentValue: value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }
    /// <summary>A color with its alpha scaled by the shape's opacity.</summary>
    private SKColor Faded(string color)
    {
        var parsed = SKColor.Parse(color);
        return base.Opacity >= 1 ? parsed : parsed.WithAlpha((byte)Math.Round(parsed.Alpha * base.Opacity));
    }
    private static void ValidateColor(string value)
    {
        if (value == null || (value.Length != 7 && value.Length != 9) || value[0] != '#' || !SKColor.TryParse(value, out _))
            throw new ArgumentException("Use a #RRGGBB or #AARRGGBB color.", nameof(value));
    }
    private SKPath GetDrawPath()
    {
        var key = (Kind, X, Y, Width, Height, PathData, Roughness, SketchSeed, StrokeWidth, StrokeStyle, StartArrowhead, EndArrowhead, RouteKey, Edges, FillStyle, _flow,
            LineJumps.Signature(_jumpStyle, _jumps));
        if (_drawKey == key) return _drawPath;
        _drawPath?.Dispose(); _drawPath = null; _headStrokePath?.Dispose(); _headStrokePath = null; _hatchPath?.Dispose(); _hatchPath = null;
        _headFillPath?.Dispose(); _headFillPath = null; _drawKey = key;
        if (Kind is SketchShapeKind.Text or SketchShapeKind.Image || IsPath && _points.Length < 2) return null;
        using var builder = new SKPathBuilder();
        var bounds = new SKRect(X, Y, X + Width, Y + Height);
        switch (Kind)
        {
            case SketchShapeKind.Line:
            case SketchShapeKind.Arrow:
                var drawn = DrawnRoute();
                if (_jumpStyle != LineJumpStyle.None && _jumps.Length > 0 && Roughness == 0)
                {
                    // Jump distances are along the route; an outlined start head moves the drawn start on along it.
                    var inset = SKPoint.Distance(Route()[0], drawn[0]);
                    var at = new float[_jumps.Length];
                    for (int i = 0; i < at.Length; i++) at[i] = _jumps[i] - inset;
                    LineJumps.Build(builder, drawn, at, _jumpStyle, StrokeWidth);
                }
                else
                {
                    builder.MoveTo(drawn[0]);
                    for (int i = 1; i < drawn.Count; i++) builder.LineTo(drawn[i]);
                }
                break;
            case SketchShapeKind.Freehand:
                builder.MoveTo(WorldPoint(0));
                for (int i = 1; i < _points.Length; i++) builder.LineTo(WorldPoint(i));
                break;
            case SketchShapeKind.Rectangle:
                if (Edges == SketchEdges.Round) builder.AddRoundRect(bounds, 8, 8); else builder.AddRect(bounds);
                break;
            case SketchShapeKind.Frame: builder.AddRect(bounds); break;
            case SketchShapeKind.Ellipse: builder.AddOval(bounds); break;
            case SketchShapeKind.Diamond when Edges == SketchEdges.Round:
                var rounded = Outline(bounds); builder.MoveTo(rounded[0]);
                for (int i = 1; i < rounded.Length; i++) builder.LineTo(rounded[i]);
                builder.Close(); break;
            case SketchShapeKind.Diamond:
                builder.MoveTo(bounds.MidX, bounds.Top); builder.LineTo(bounds.Right, bounds.MidY);
                builder.LineTo(bounds.MidX, bounds.Bottom); builder.LineTo(bounds.Left, bounds.MidY); builder.Close(); break;
            case SketchShapeKind.Text: case SketchShapeKind.Image: break;
        }
        // A frame's border is always crisp.
        if (Roughness > 0 && Kind != SketchShapeKind.Frame)
        {
            var outline = SketchOutline(); builder.Reset();
            if (outline.Count == 0) return null;
            builder.MoveTo(X + outline[0].X, Y + outline[0].Y);
            for (int i = 1; i < outline.Count; i++) builder.LineTo(X + outline[i].X, Y + outline[i].Y);
            if (!IsPath) builder.Close();
        }
        // Open arrowheads share the shaft's stroke when it is solid and still (one stroke, as arrows always drew); a dashed,
        // dotted or flowing shaft keeps its arrowheads solid in their own path. Filled arrowheads are always separate.
        using var headStrokes = new SKPathBuilder(); using var headFills = new SKPathBuilder();
        foreach (var (points, fill) in HeadShapes())
        {
            var target = fill ? headFills : StrokeStyle == SketchStrokeStyle.Solid && _flow == LineFlow.None ? builder : headStrokes;
            target.MoveTo(points[0]);
            for (int i = 1; i < points.Length; i++) target.LineTo(points[i]);
            if (fill) target.Close();
        }
        _headStrokePath = headStrokes.Detach(); _headFillPath = headFills.Detach();
        if (Fillable && FillStyle != SketchFillStyle.Solid) _hatchPath = Hatch(Outline(bounds));
        return _drawPath = builder.Detach();
    }
    /// <summary>A closed shape's outline as a convex polygon in the given box: rounded corners are sampled.</summary>
    private SKPoint[] Outline(SKRect box)
    {
        if (Kind == SketchShapeKind.Ellipse)
        {
            var ellipse = new SKPoint[64];
            for (int i = 0; i < ellipse.Length; i++) { var a = i * MathF.PI * 2 / ellipse.Length; ellipse[i] = new(box.MidX + box.Width / 2 * MathF.Cos(a), box.MidY + box.Height / 2 * MathF.Sin(a)); }
            return ellipse;
        }
        var corners = Kind == SketchShapeKind.Diamond
            ? new[] { new SKPoint(box.MidX, box.Top), new SKPoint(box.Right, box.MidY), new SKPoint(box.MidX, box.Bottom), new SKPoint(box.Left, box.MidY) }
            : new[] { new SKPoint(box.Left, box.Top), new SKPoint(box.Right, box.Top), new SKPoint(box.Right, box.Bottom), new SKPoint(box.Left, box.Bottom) };
        if (Edges == SketchEdges.Sharp) return corners;
        // Each corner becomes an arc of the rectangle's 8-unit radius (a quarter of the shorter side at most), sampled.
        var result = new List<SKPoint>(corners.Length * 7);
        for (int i = 0; i < corners.Length; i++)
        {
            var c = corners[i]; var before = corners[(i + corners.Length - 1) % corners.Length]; var after = corners[(i + 1) % corners.Length];
            var r = Math.Min(8, Math.Min(SKPoint.Distance(c, before), SKPoint.Distance(c, after)) / 4);
            var p0 = c + Scaled(before - c, r); var p1 = c + Scaled(after - c, r);
            for (int s = 0; s <= 6; s++)
            {
                float t = s / 6f, u = 1 - t; // A quadratic curve through the corner, tangent to both sides.
                result.Add(new SKPoint(u * u * p0.X + 2 * u * t * c.X + t * t * p1.X, u * u * p0.Y + 2 * u * t * c.Y + t * t * p1.Y));
            }
        }
        return result.ToArray();
    }
    private static SKPoint Scaled(SKPoint v, float length) { var l = v.Length; return l <= 0 ? SKPoint.Empty : new(v.X / l * length, v.Y / l * length); }
    /// <summary>Lines of the fill colour inside a convex outline: one direction (hatched) or two (cross-hatched). A rough
    /// shape's lines are jittered from its seed, so the same shape draws the same lines on every load.</summary>
    private SKPath Hatch(SKPoint[] outline)
    {
        var gap = Math.Max(6f, StrokeWidth * 4);
        uint state = ((uint)SketchSeed * 2654435761u) | 1u;
        float Jitter() { state ^= state << 13; state ^= state >> 17; state ^= state << 5; return (state / (float)uint.MaxValue * 2 - 1) * Roughness; }
        using var builder = new SKPathBuilder();
        foreach (var degrees in FillStyle == SketchFillStyle.CrossHatched ? new[] { -45f, 45f } : new[] { -45f })
        {
            var angle = degrees * MathF.PI / 180; var direction = new SKPoint(MathF.Cos(angle), MathF.Sin(angle)); var normal = new SKPoint(-direction.Y, direction.X);
            float Along(SKPoint p) => p.X * normal.X + p.Y * normal.Y;
            var low = outline.Min(Along); var high = outline.Max(Along);
            for (var offset = low + gap / 2; offset < high; offset += gap)
            {
                var origin = new SKPoint(normal.X * offset, normal.Y * offset);
                // The line meets a convex outline at its nearest and farthest crossing.
                float near = float.MaxValue, far = float.MinValue;
                for (int i = 0; i < outline.Length; i++)
                {
                    var a = outline[i]; var b = outline[(i + 1) % outline.Length]; var edge = b - a;
                    var denominator = direction.X * edge.Y - direction.Y * edge.X;
                    if (Math.Abs(denominator) < 1e-6f) continue;
                    var w = a - origin;
                    var t = (w.X * edge.Y - w.Y * edge.X) / denominator; var s = (w.X * direction.Y - w.Y * direction.X) / denominator;
                    if (s < 0 || s > 1) continue;
                    near = Math.Min(near, t); far = Math.Max(far, t);
                }
                if (far - near < 1) continue;
                builder.MoveTo(origin.X + direction.X * near + Jitter(), origin.Y + direction.Y * near + Jitter());
                builder.LineTo(origin.X + direction.X * far + Jitter(), origin.Y + direction.Y * far + Jitter());
            }
        }
        return builder.Detach();
    }
    public override void Draw(SKCanvas canvas, DrawingContext context)
    {
        if (!IsVisible || State == ComponentState.Disposing || State == ComponentState.Inactive || base.Opacity <= 0) return;
        State = ComponentState.Rendering;
        var saved = canvas.Save();
        try
        {
            if (Rotated) canvas.RotateDegrees(_rotation, X + Width / 2, Y + Height / 2);
            // Opacity scales each paint's alpha (as a canvas globalAlpha does), never an isolated layer: SVG export
            // drops layer content, and per-paint alpha becomes fill-opacity and stroke-opacity there.
            DrawContent(canvas, context);
            DrawChildren(canvas, context);
            DrawText(canvas, context);
        }
        finally { canvas.RestoreToCount(saved); State = ComponentState.Active; }
    }
    protected override void DrawContent(SKCanvas canvas, DrawingContext context)
    {
        if (Kind == SketchShapeKind.Image)
        {
            if (Image == null) return;
            var (mirrorX, mirrorY) = ImageFlip switch
            {
                SketchImageFlip.None => (1f, 1f), SketchImageFlip.Horizontal => (-1f, 1f),
                SketchImageFlip.Vertical => (1f, -1f), SketchImageFlip.Both => (-1f, -1f),
            };
            if (mirrorX != 1 || mirrorY != 1) canvas.Scale(mirrorX, mirrorY, X + Width / 2, Y + Height / 2);
            // On an inverted (dark) display the picture is pre-compensated so it shows its true colours.
            using var imagePaint = new SKPaint { IsAntialias = true, Color = SKColors.Black.WithAlpha((byte)Math.Round(255 * base.Opacity)),
                ColorFilter = context.InvertedDisplay ? DisplayFilters.ImageCompensation : null };
            var shown = _crop is { } crop ? new SKRect(crop.Left * Image.Width, crop.Top * Image.Height, crop.Right * Image.Width, crop.Bottom * Image.Height)
                : new SKRect(0, 0, Image.Width, Image.Height);
            canvas.DrawImage(Image, shown, new SKRect(X, Y, X + Width, Y + Height), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear), imagePaint);
            return;
        }
        var path = GetDrawPath(); if (path == null) return;
        var key = (Fill, Stroke, StrokeWidth, StrokeStyle, base.Opacity);
        if (_paintKey != key || _fillPaint == null || _strokePaint == null)
        {
            _fillPaint?.Dispose(); _strokePaint?.Dispose(); _solidStrokePaint?.Dispose(); _headFillPaint?.Dispose(); _dashEffect?.Dispose();
            _fillPaint = new SKPaint { IsAntialias = true, Color = Faded(Fill), Style = SKPaintStyle.Fill };
            _solidStrokePaint = new SKPaint { IsAntialias = true, Color = Faded(Stroke), Style = SKPaintStyle.Stroke,
                StrokeWidth = StrokeWidth, StrokeJoin = SKStrokeJoin.Round, StrokeCap = SKStrokeCap.Round };
            _strokePaint = _solidStrokePaint.Clone();
            _strokePaint.PathEffect = _dashEffect = StrokeStyle switch
            {
                SketchStrokeStyle.Solid => null,
                SketchStrokeStyle.Dashed => SKPathEffect.CreateDash(new[] { StrokeWidth * 4 + 4, StrokeWidth * 3 + 4 }, 0),
                // Zero-length dashes with round caps draw as dots.
                SketchStrokeStyle.Dotted => SKPathEffect.CreateDash(new[] { 0f, StrokeWidth * 2 + 4 }, 0),
            };
            _headFillPaint = new SKPaint { IsAntialias = true, Color = Faded(Stroke), Style = SKPaintStyle.Fill };
            _hatchPaint?.Dispose();
            _hatchPaint = new SKPaint { IsAntialias = true, Color = Faded(Fill), Style = SKPaintStyle.Stroke, StrokeWidth = Math.Max(1, StrokeWidth / 2), StrokeCap = SKStrokeCap.Round };
            _paintKey = key;
        }
        // A frame never covers its members; a hatched shape draws lines of its fill colour instead of a solid fill.
        if (!IsPath && Kind != SketchShapeKind.Frame)
        {
            if (_hatchPath is { IsEmpty: false }) canvas.DrawPath(_hatchPath, _hatchPaint);
            else if (FillStyle == SketchFillStyle.Solid) canvas.DrawPath(path, _fillPaint);
        }
        if (DrawsPressure) { canvas.DrawPath(InkOutline(), _headFillPaint); return; }
        // A labelled line breaks around its label, so the text stays readable on every background and in exports.
        var label = LineLabelBounds;
        if (label is { } gap) { canvas.Save(); canvas.ClipRect(gap, SKClipOperation.Difference); }
        DrawStroke(canvas, path, context?.AnimationTime ?? 0);
        if (label != null) canvas.Restore();
        if (!_headStrokePath.IsEmpty) canvas.DrawPath(_headStrokePath, _solidStrokePaint);
        if (!_headFillPath.IsEmpty) { canvas.DrawPath(_headFillPath, _headFillPaint); canvas.DrawPath(_headFillPath, _solidStrokePaint); }
    }
    /// <summary>The line's stroke at a moment: still, with its own pattern flowing, or with dots travelling over it.</summary>
    private void DrawStroke(SKCanvas canvas, SKPath path, double seconds)
    {
        switch (_flow)
        {
            case LineFlow.None:
                canvas.DrawPath(path, _strokePaint);
                break;
            case LineFlow.Dashes:
                // A dotted line moves its dots; a solid or dashed one flows as dashes.
                var pattern = StrokeStyle == SketchStrokeStyle.Dotted ? FlowAnimation.DotPattern(StrokeWidth) : FlowAnimation.DashPattern(StrokeWidth);
                using (var effect = FlowAnimation.Effect(pattern, _flowSpeed, _flowDirection, seconds))
                using (var paint = _solidStrokePaint.Clone())
                {
                    paint.PathEffect = effect;
                    canvas.DrawPath(path, paint);
                }
                break;
            case LineFlow.Dots:
                canvas.DrawPath(path, _strokePaint);
                FlowAnimation.DrawTravellingDots(canvas, path, Faded(Stroke), StrokeWidth, _flowSpeed, _flowDirection, seconds);
                break;
        }
    }
    private (string Path, string Pressures, float X, float Y, float Width, float Height, float StrokeWidth)? _inkKey;
    private SKPath _inkPath;
    /// <summary>The filled outline of a stroke whose width follows its pressures: at each point the width is the stroke
    /// width times 0.5 + pressure, so 0.5 draws the plain width. It is a pure function of the points, pressures, bounds and
    /// stroke width, so a reloaded drawing looks the same.</summary>
    private SKPath InkOutline()
    {
        var key = (_pathData, _pressureData, X, Y, Width, Height, StrokeWidth);
        if (_inkKey == key && _inkPath != null) return _inkPath;
        _inkPath?.Dispose();
        var points = new List<SKPoint>(_points.Length); var radii = new List<float>(_points.Length);
        for (var i = 0; i < _points.Length; i++)
        {
            var point = WorldPoint(i);
            if (points.Count > 0 && (point - points[^1]).Length < 0.01f) continue;
            points.Add(point); radii.Add(StrokeWidth * (0.5f + _pressures[i]) / 2);
        }
        using var builder = new SKPathBuilder();
        if (points.Count == 1) builder.AddCircle(points[0].X, points[0].Y, radii[0]);
        else
        {
            var left = new SKPoint[points.Count]; var right = new SKPoint[points.Count];
            for (var i = 0; i < points.Count; i++)
            {
                var direction = points[Math.Min(i + 1, points.Count - 1)] - points[Math.Max(i - 1, 0)];
                var length = direction.Length;
                var normal = length == 0 ? new SKPoint(0, 0) : new SKPoint(-direction.Y / length, direction.X / length);
                left[i] = points[i] + new SKPoint(normal.X * radii[i], normal.Y * radii[i]);
                right[i] = points[i] - new SKPoint(normal.X * radii[i], normal.Y * radii[i]);
            }
            builder.MoveTo(left[0]);
            for (var i = 1; i < points.Count; i++) builder.LineTo(left[i]);
            for (var i = points.Count - 1; i >= 0; i--) builder.LineTo(right[i]);
            builder.Close();
            // Round ends, and round joins where the stroke turns sharply, so the outline never shows a notch.
            builder.AddCircle(points[0].X, points[0].Y, radii[0]);
            builder.AddCircle(points[^1].X, points[^1].Y, radii[^1]);
            for (var i = 1; i < points.Count - 1; i++)
            {
                SKPoint a = points[i] - points[i - 1], b = points[i + 1] - points[i];
                var la = a.Length; var lb = b.Length;
                if (la > 0 && lb > 0 && (a.X * b.X + a.Y * b.Y) / (la * lb) < 0.5f) builder.AddCircle(points[i].X, points[i].Y, radii[i]);
            }
        }
        _inkPath = builder.Detach();
        _inkPath.FillType = SKPathFillType.Winding;
        _inkKey = key;
        return _inkPath;
    }
    protected override void DisposeUnmanagedResources()
    {
        _drawPath?.Dispose(); _drawPath = null; _headStrokePath?.Dispose(); _headStrokePath = null;
        _headFillPath?.Dispose(); _headFillPath = null; _hatchPath?.Dispose(); _hatchPath = null; _drawKey = null;
        _inkPath?.Dispose(); _inkPath = null; _inkKey = null;
        _hatchPaint?.Dispose(); _hatchPaint = null;
        _fillPaint?.Dispose(); _fillPaint = null; _strokePaint?.Dispose(); _strokePaint = null;
        _solidStrokePaint?.Dispose(); _solidStrokePaint = null; _headFillPaint?.Dispose(); _headFillPaint = null;
        _dashEffect?.Dispose(); _dashEffect = null; _paintKey = null;
        _textPicture?.Dispose(); _textPicture = null; _textKey = null;
        base.DisposeUnmanagedResources();
    }
    public override bool ContainsPoint(SKPoint point)
    {
        return ContainsPoint(point, 5);
    }
    public bool ContainsPoint(SKPoint point, float tolerance)
    {
        if (IsPath)
        {
            var distance = Math.Max(0, tolerance) + StrokeWidth / 2;
            var route = Route();
            for (int i = 1; i < route.Count; i++)
                if (DistanceToSegment(point, route[i - 1], route[i]) <= distance) return true;
            foreach (var head in HeadShapes())
            {
                if (head.Fill && ContainsInPolygon(point, head.Points)) return true;
                for (int i = 1; i < head.Points.Length; i++)
                    if (DistanceToSegment(point, head.Points[i - 1], head.Points[i]) <= distance) return true;
            }
            return LineLabelBounds is { } label && label.Contains(point);
        }
        // A frame is picked by its border or its title; its inside belongs to the objects in it.
        if (Kind == SketchShapeKind.Frame)
        {
            if (FrameTitleBounds is { } title && title.Contains(point)) return true;
            var edge = Math.Max(0, tolerance) + StrokeWidth / 2;
            var outer = new SKRect(X - edge, Y - edge, X + Width + edge, Y + Height + edge);
            var inner = new SKRect(X + edge, Y + edge, X + Width - edge, Y + Height - edge);
            return outer.Contains(point) && !(inner.Width > 0 && inner.Height > 0 && inner.Contains(point));
        }
        return ContainsLocal(ToLocal(point), tolerance);
    }
    private bool ContainsLocal(SKPoint point, float tolerance)
    {
        if (Width <= 0 || Height <= 0) return false;
        var nx = (point.X - X - Width / 2) / (Width / 2);
        var ny = (point.Y - Y - Height / 2) / (Height / 2);
        return Kind switch
        {
            SketchShapeKind.Ellipse => nx * nx + ny * ny <= 1,
            SketchShapeKind.Diamond => Math.Abs(nx) + Math.Abs(ny) <= 1,
            SketchShapeKind.Rectangle or SketchShapeKind.Text or SketchShapeKind.Image => Math.Abs(nx) <= 1 && Math.Abs(ny) <= 1,
            // Paths are tested along their segments above; a frame along its border.
            SketchShapeKind.Line or SketchShapeKind.Arrow or SketchShapeKind.Freehand or SketchShapeKind.Frame => false,
        };
    }
}

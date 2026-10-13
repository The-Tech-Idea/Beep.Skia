using SkiaSharp;
using System.Timers;
using Beep.Skia.Model;
using System.Collections.Generic;
using System;
using System.Linq;
using Beep.Skia.Components;
namespace Beep.Skia
{
    /// <summary>
    /// Represents a visual connection line between two connection points that can be drawn on a Skia canvas.
    /// This class implements the IConnectionLine interface and provides functionality for drawing lines,
    /// arrows, labels, animations, and user interaction handling.
    /// </summary>
    public class ConnectionLine : IConnectionLine, IDisposable
    {
        private readonly Action invalidateVisualAction;
        private SKPaint _textPaint;
        private SKFont _textFont;
        private string _resolvedRequiredLabelFamily;

        /// <summary>Trusted host configuration for portable label outlines. Null keeps platform text rendering.</summary>
        public string RequiredLabelFontFamily { get; set; }

        // Reused across frames: lines are redrawn every frame, so creating paints/path effects per
        // draw would allocate a managed wrapper and a native Skia object for every line.
        private SKPaint _strokePaint;
        private SKPathEffect _dashEffect;
        private float[] _dashEffectPattern;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionLine"/> class with an invalidate visual action.
        /// </summary>
        /// <param name="invalidateVisualAction">The action to call when the visual needs to be invalidated.</param>
        public ConnectionLine(Action invalidateVisualAction)
        {
            this.invalidateVisualAction = invalidateVisualAction;
            InitializePaint();
        }

        /// <summary>
        /// Initializes paint objects used for drawing the connection line and text.
        /// </summary>
        private void InitializePaint()
        {
            Paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };
            _textPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = SKColors.Black
            };
            _textFont = new SKFont
            {
                Size = 14
            };
        }

        /// <summary>
        /// Gets or sets the starting connection point of this line.
        /// </summary>
        public IConnectionPoint Start { get; set; }

        /// <summary>
        /// Gets or sets the ending connection point of this line.
        /// </summary>
        public IConnectionPoint End { get; set; }

        /// <summary>
        /// Gets or sets the end point coordinates when not connected to an end connection point.
        /// </summary>
        public SKPoint EndPoint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this connection line is connected.
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this connection line is selected.
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// Gets or sets whether this line is visible. Hidden lines are skipped by the renderer.
        /// </summary>
        public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Indicates the line is currently hovered by the mouse (set by interaction layer).
    /// </summary>
    public bool IsHovered { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this connection line should be animated.
        /// </summary>
        public bool IsAnimated { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether data flow animation is enabled.
        /// </summary>
        public bool IsDataFlowAnimated { get; set; }

        /// <summary>
        /// Gets or sets the speed of the data flow animation (pixels per second).
        /// </summary>
        public float DataFlowSpeed { get; set; } = 100f;

        /// <summary>
        /// Gets or sets the size of the data flow particles.
        /// </summary>
        public float DataFlowParticleSize { get; set; } = 4f;

        /// <summary>
        /// Gets or sets the color of the data flow particles.
        /// </summary>
        public SKColor DataFlowColor { get; set; } = SKColors.Blue;

        /// <summary>
        /// Gets or sets a value indicating whether to show an arrow at the start of the line.
        /// </summary>
        public bool ShowStartArrow { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show an arrow at the end of the line.
        /// </summary>
        public bool ShowEndArrow { get; set; } = true;

        /// <summary>
        /// Gets or sets the paint object used for drawing the connection line.
        /// </summary>
        public SKPaint Paint { get; set; }

        /// <summary>
        /// Gets or sets the first label text to display on the connection line.
        /// </summary>
        public string Label1 { get; set; }

        /// <summary>
        /// Gets or sets the second label text to display on the connection line.
        /// </summary>
        public string Label2 { get; set; }

        /// <summary>
        /// Gets or sets the third label text to display on the connection line.
        /// </summary>
        public string Label3 { get; set; }

        /// <summary>
        /// Gets or sets the data type label to display on the connection line.
        /// If not set, will automatically use the data type from the start connection point.
        /// </summary>
        public string DataTypeLabel { get; set; }

        /// <summary>
        /// Optional guard condition evaluated before a state-machine transition fires.
        /// </summary>
        public string GuardCondition { get; set; }

        /// <summary>
        /// Optional action executed when a state-machine transition fires.
        /// </summary>
        public string TransitionAction { get; set; }

        /// <summary>
        /// Optional trigger event name that causes the transition to fire.
        /// </summary>
        public string TriggerEvent { get; set; }

    // --- Schema/row-level semantics ---
    /// <summary>
    /// Optional source row/column identifier when connecting ERD rows or similar row-based ports.
    /// </summary>
    public System.Guid? SourceRowId { get; set; }

    /// <summary>
    /// Optional target row/column identifier when connecting ERD rows or similar row-based ports.
    /// </summary>
    public System.Guid? TargetRowId { get; set; }

    /// <summary>
    /// Optional JSON payload for schema flowing across this line (e.g., ETL OutputSchema).
    /// </summary>
    public string SchemaJson { get; set; }

    /// <summary>
    /// Optional JSON payload for an expected schema used to validate the actual SchemaJson (e.g., ETLTarget.ExpectedSchema).
    /// When provided, tooltip will show concrete differences on hover.
    /// </summary>
    public string ExpectedSchemaJson { get; set; }

    /// <summary>
    /// Size of arrowheads in pixels.
    /// </summary>
    public float ArrowSize { get; set; } = 10.0f;

    /// <summary>
    /// Optional dash pattern (pairs of on/off lengths). Null/empty for solid.
    /// </summary>
    public float[] DashPattern { get; set; }

    /// <summary>
    /// Routing mode for how the line is drawn.
    /// </summary>
    public LineRoutingMode RoutingMode { get; set; } = LineRoutingMode.Straight;

    /// <summary>
    /// Data flow animation direction.
    /// </summary>
    public DataFlowDirection FlowDirection { get; set; } = DataFlowDirection.None;

    /// <summary>
    /// Animation style for data flow visualization (infographic style).
    /// </summary>
    public FlowAnimationStyle AnimationStyle { get; set; } = FlowAnimationStyle.Dots;

    private LineFlow _flow;
    private LineFlowSpeed _flowSpeed = LineFlowSpeed.Normal;
    private LineFlowDirection _flowMovement;
    private SketchArrowhead _startArrowhead, _endArrowhead;
    /// <summary>How the line shows movement along it: its own dashes flowing, or dots travelling over it (the same flow
    /// sketch lines have, painted by <see cref="FlowAnimation"/>). None by default.</summary>
    public LineFlow Flow
    {
        get => _flow;
        set { if (!Enum.IsDefined(typeof(LineFlow), value)) throw new ArgumentOutOfRangeException(nameof(value)); _flow = value; }
    }
    /// <summary>How fast the flow moves.</summary>
    public LineFlowSpeed FlowSpeed
    {
        get => _flowSpeed;
        set { if (!Enum.IsDefined(typeof(LineFlowSpeed), value)) throw new ArgumentOutOfRangeException(nameof(value)); _flowSpeed = value; }
    }
    /// <summary>Which way the flow moves: from the start port to the end port, or back.</summary>
    public LineFlowDirection FlowMovement
    {
        get => _flowMovement;
        set { if (!Enum.IsDefined(typeof(LineFlowDirection), value)) throw new ArgumentOutOfRangeException(nameof(value)); _flowMovement = value; }
    }
    /// <summary>The head at the start port, from the shared <see cref="Arrowheads"/> set. None leaves
    /// <see cref="ShowStartArrow"/> and the ERD marks in charge.</summary>
    public SketchArrowhead StartArrowhead
    {
        get => _startArrowhead;
        set { if (!Enum.IsDefined(typeof(SketchArrowhead), value)) throw new ArgumentOutOfRangeException(nameof(value)); _startArrowhead = value; }
    }
    /// <summary>The head at the end port, from the shared <see cref="Arrowheads"/> set.</summary>
    public SketchArrowhead EndArrowhead
    {
        get => _endArrowhead;
        set { if (!Enum.IsDefined(typeof(SketchArrowhead), value)) throw new ArgumentOutOfRangeException(nameof(value)); _endArrowhead = value; }
    }
    /// <summary>The scene's animation clock at the moment of drawing, in seconds; the renderer sets it from
    /// <see cref="DrawingManager.AnimationTime"/> before each draw.</summary>
    public double AnimationTime { get; set; }
    /// <summary>The page's other objects, which an elbow route goes round (one snapshot shared by the page). The host
    /// supplies them; they are derived, never stored. Null keeps the route round its two components only.</summary>
    public RouteObstacles Obstacles { get; set; }
    private LineJumpStyle _jumpStyle;
    private float[] _jumps = Array.Empty<float>();
    /// <summary>Where this line jumps over lines drawn before it (distances along its route, ascending) and how. The host
    /// supplies them from <see cref="LineJumps.Find"/>; they are derived, never stored.</summary>
    public (LineJumpStyle Style, IReadOnlyList<float> At) Jumps
    {
        get => (_jumpStyle, _jumps);
        set
        {
            if (!Enum.IsDefined(typeof(LineJumpStyle), value.Style)) throw new ArgumentOutOfRangeException(nameof(value));
            _jumpStyle = value.Style; _jumps = value.At == null ? Array.Empty<float>() : value.At.ToArray();
        }
    }

    /// <summary>
    /// Gets or sets the label1 placement.
    /// </summary>
    public LabelPlacement Label1Placement { get; set; } = LabelPlacement.Above;
    /// <summary>
    /// Gets or sets the label2 placement.
    /// </summary>
    public LabelPlacement Label2Placement { get; set; } = LabelPlacement.Above;
    /// <summary>
    /// Gets or sets the label3 placement.
    /// </summary>
    public LabelPlacement Label3Placement { get; set; } = LabelPlacement.Over;
    /// <summary>
    /// Gets or sets the data label placement.
    /// </summary>
    public LabelPlacement DataLabelPlacement { get; set; } = LabelPlacement.Below;

    /// <summary>
    /// Gets or sets the show status indicator.
    /// </summary>
    public bool ShowStatusIndicator { get; set; } = false;
    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public LineStatus Status { get; set; } = LineStatus.None;
    /// <summary>
    /// Gets or sets the status color.
    /// </summary>
    public SKColor StatusColor { get; set; } = SKColors.DimGray;

    // ERD multiplicity markers
    /// <summary>
    /// Gets or sets the start multiplicity.
    /// </summary>
    public ERDMultiplicity StartMultiplicity { get; set; } = ERDMultiplicity.Unspecified;
    /// <summary>
    /// Gets or sets the end multiplicity.
    /// </summary>
    public ERDMultiplicity EndMultiplicity { get; set; } = ERDMultiplicity.Unspecified;

    // Optional styling for ERD crow's foot markers
    // Spread angle in degrees between the center prong and each outer prong
    /// <summary>
    /// Gets or sets the crow foot spread degrees.
    /// </summary>
    public float CrowFootSpreadDegrees { get; set; } = 18f; // ~PI/10
    // Length scale (multiplier on unit) for the crow's foot prongs
    /// <summary>
    /// Gets or sets the crow foot length.
    /// </summary>
    public float CrowFootLength { get; set; } = 1.0f;

        /// <summary>
        /// The threshold distance for detecting line clicks.
        /// </summary>
        private const float LineClickThreshold = 5.0f;

        private System.Timers.Timer animationTimer;

        /// <summary>
        /// Animation state for data flow particles.
        /// </summary>
        private float dataFlowOffset = 0f;
        private float dataFlowSpacing = 30f; // Distance between particles

        /// <summary>
        /// Occurs when the start arrow of the connection line is clicked.
        /// </summary>
        public event EventHandler<ConnectionArgs> StartArrowClicked;

        /// <summary>
        /// Occurs when the end arrow of the connection line is clicked.
        /// </summary>
        public event EventHandler<ConnectionArgs> EndArrowClicked;

        /// <summary>
        /// Occurs when the connection line itself is clicked.
        /// </summary>
        public event EventHandler<LineArgs> LineClicked;

        /// <summary>
        /// Gets or sets the color of the connection line.
        /// </summary>
        public SKColor LineColor { get; set; } = SKColors.Black;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionLine"/> class with start and end connection points.
        /// </summary>
        /// <param name="start">The starting connection point.</param>
        /// <param name="end">The ending connection point.</param>
        /// <param name="invalidateVisualAction">The action to call when the visual needs to be invalidated.</param>
        /// <exception cref="ArgumentNullException">Thrown when start, end, or invalidateVisualAction is null.</exception>
        public ConnectionLine(IConnectionPoint start, IConnectionPoint end, Action invalidateVisualAction)
        {
            if (start == null)
                throw new ArgumentNullException(nameof(start), "Start connection point cannot be null.");
            if (end == null)
                throw new ArgumentNullException(nameof(end), "End connection point cannot be null.");
            if (invalidateVisualAction == null)
                throw new ArgumentNullException(nameof(invalidateVisualAction), "Invalidate visual action cannot be null.");

            Start = start;
            End = end;
            this.invalidateVisualAction = invalidateVisualAction;
            InitializePaint();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionLine"/> class with a start connection point and end coordinates.
        /// </summary>
        /// <param name="start">The starting connection point.</param>
        /// <param name="end">The ending point coordinates.</param>
        /// <param name="invalidateVisualAction">The action to call when the visual needs to be invalidated.</param>
        /// <exception cref="ArgumentNullException">Thrown when start or invalidateVisualAction is null.</exception>
        public ConnectionLine(IConnectionPoint start, SKPoint end, Action invalidateVisualAction)
        {
            if (start == null)
                throw new ArgumentNullException(nameof(start), "Start connection point cannot be null.");
            if (invalidateVisualAction == null)
                throw new ArgumentNullException(nameof(invalidateVisualAction), "Invalidate visual action cannot be null.");

            Start = start;
            EndPoint = end;
            this.invalidateVisualAction = invalidateVisualAction;
            InitializePaint();
        }

        /// <summary>
        /// Draws the connection line on the specified Skia canvas.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        public void Draw(SKCanvas canvas)
        {
            // Calculate line coordinates and draw the line
            if (Start == null && End == null)
            {
                // Nothing to draw
                return;
            }

            // Prefer actual connection points; fall back to EndPoint when End is not connected (preview/drag)
            var lineStart = Start != null ? Start.Position : EndPoint;
            var lineEnd = (End != null) ? End.Position : EndPoint;
            var route = Route();
            ResolveLabelFont();
            Paint.Color = LineColor;

            // Determine if this is an identifying relationship (check endpoints)
            bool isIdentifying = true; // default to solid (identifying)
            try
            {
                // Check if either endpoint component has IsIdentifying NodeProperty set to false
                var startComp = Start?.Component as SkiaComponent;
                var endComp = End?.Component as SkiaComponent;
                if (startComp?.NodeProperties != null && startComp.NodeProperties.TryGetValue("IsIdentifying", out var p1) && p1?.ParameterCurrentValue is bool b1)
                    isIdentifying = b1;
                else if (endComp?.NodeProperties != null && endComp.NodeProperties.TryGetValue("IsIdentifying", out var p2) && p2?.ParameterCurrentValue is bool b2)
                    isIdentifying = b2;
            }
            catch { }

            // Configure dash pattern if provided OR if non-identifying ERD relationship
            var stroke = EnsureStrokePaint();
            stroke.Color = Paint.Color;
            stroke.Style = Paint.Style;
            stroke.StrokeWidth = Paint.StrokeWidth;
            stroke.IsAntialias = Paint.IsAntialias;
            stroke.StrokeCap = Paint.StrokeCap;
            stroke.StrokeJoin = Paint.StrokeJoin;

            float[] dash = null;
            if (DashPattern != null && DashPattern.Length >= 2) dash = DashPattern;
            else if (!isIdentifying && (Start?.Component?.GetType()?.Namespace == "Beep.Skia.ERD" || End?.Component?.GetType()?.Namespace == "Beep.Skia.ERD"))
                dash = ErdDashPattern; // Non-identifying ERD relationships: use dashed line
            // Flowing dashes move the line's own pattern; a solid line flows as dashes.
            using var flowing = _flow == LineFlow.Dashes
                ? FlowAnimation.Effect(dash ?? FlowAnimation.DashPattern(Paint.StrokeWidth), _flowSpeed, _flowMovement, AnimationTime) : null;
            stroke.PathEffect = flowing ?? (dash == null ? null : GetDashEffect(dash));

            // Arrowheads point along the route's first and last segments.
            var startToward = route.Length > 1 ? route[1] : lineEnd; var endToward = route.Length > 1 ? route[route.Length - 2] : lineStart;
            if (RoutingMode == LineRoutingMode.Curved)
                (startToward, endToward) = ConnectionRoute.CurveControls(lineStart, lineEnd);
            if (startToward == lineStart) startToward = lineEnd;
            if (endToward == lineEnd) endToward = lineStart;
            var startHead = Start != null && StartMultiplicity == ERDMultiplicity.Unspecified ? _startArrowhead : SketchArrowhead.None;
            var endHead = EndMultiplicity == ERDMultiplicity.Unspecified ? _endArrowhead : SketchArrowhead.None;

            // One path for the whole route, so dashes run on round corners and a flow moves along every segment.
            using (var path = RoutePath(route, lineStart, lineEnd, startHead, startToward, endHead, endToward))
            {
                // A label sitting on the middle of the line breaks the line there, so its text stays readable.
                var gap = MiddleLabelGap(route);
                if (gap is { } cut) { canvas.Save(); canvas.ClipRect(cut, SKClipOperation.Difference); }
                try
                {
                    canvas.DrawPath(path, stroke);
                    if (_flow == LineFlow.Dots)
                        FlowAnimation.DrawTravellingDots(canvas, path, LineColor, Paint.StrokeWidth, _flowSpeed, _flowMovement, AnimationTime);
                }
                finally
                {
                    if (gap != null) canvas.Restore();
                    stroke.PathEffect = null; // The flowing effect is disposed with this draw.
                }
            }

            // Draw ERD multiplicity markers or arrows
            if (Start != null && StartMultiplicity != ERDMultiplicity.Unspecified)
                DrawErdMultiplicity(canvas, stroke, atPoint: lineStart, toward: startToward, StartMultiplicity);
            else if (startHead != SketchArrowhead.None)
                DrawHead(canvas, startHead, lineStart, startToward);
            else if (ShowStartArrow && Start != null)
                DrawArrow(canvas, Paint, lineStart, startToward, ArrowSize);

            if (EndMultiplicity != ERDMultiplicity.Unspecified)
                DrawErdMultiplicity(canvas, stroke, atPoint: lineEnd, toward: endToward, EndMultiplicity);
            else if (endHead != SketchArrowhead.None)
                DrawHead(canvas, endHead, lineEnd, endToward);
            else if (ShowEndArrow)
                DrawArrow(canvas, Paint, lineEnd, endToward, ArrowSize);

            // Draw optional labels
            if (!string.IsNullOrEmpty(Label1))
            {
                var labelPosition = GetLabelPosition(route, Label1Placement, atStart: true);
                DrawLineLabel(canvas, Label1, labelPosition);
            }

            if (!string.IsNullOrEmpty(Label2))
            {
                var labelPosition = GetLabelPosition(route, Label2Placement, atStart: false);
                DrawLineLabel(canvas, Label2, labelPosition);
            }

            if (!string.IsNullOrEmpty(Label3))
            {
                var labelPosition = GetMidLabelPosition(route, Label3Placement, Label3);
                DrawLineLabel(canvas, Label3, labelPosition);
            }

            // Draw data type label
            string dataTypeText = DataTypeLabel ?? Start?.DataType;
            if (!string.IsNullOrEmpty(dataTypeText))
            {
                var labelPosition = GetMidLabelPosition(route, DataLabelPlacement, dataTypeText);
                DrawLineLabel(canvas, dataTypeText, labelPosition);
            }

            // Draw data flow animation particles
            if (IsDataFlowAnimated && FlowDirection != DataFlowDirection.None)
            {
                DrawDataFlowParticles(canvas, lineStart, lineEnd, FlowDirection);
            }

            // Status indicator
            if (ShowStatusIndicator && Status != LineStatus.None)
            {
                DrawStatusIndicator(canvas, lineStart, lineEnd);
            }

            // Schema-derived label and tooltip
            TryDrawSchemaLabelAndTooltip(canvas, lineStart, lineEnd);
            // Hover badge (small "i") to hint details availability
            TryDrawInfoBadge(canvas, lineStart, lineEnd);

        }

        private void TryDrawSchemaLabelAndTooltip(SKCanvas canvas, SKPoint lineStart, SKPoint lineEnd)
        {
            if (string.IsNullOrWhiteSpace(SchemaJson)) return;
            List<ColumnDefinition> cols = null;
            try { cols = System.Text.Json.JsonSerializer.Deserialize<List<ColumnDefinition>>(SchemaJson); } catch { }
            if (cols == null) return;

            int n = cols.Count;
            if (n > 0)
            {
                string small = $"cols: {n}";
                var pos = GetMidLabelPosition(Route(), LabelPlacement.Above, small);
                using var paint = new SKPaint { IsAntialias = true, Color = SKColors.DimGray, Style = SKPaintStyle.Fill };
                canvas.DrawText(small, pos.X, pos.Y, SKTextAlign.Center, _textFont, paint);
            }

            // Show tooltip when hovered
            if (IsHovered && n > 0)
            {
                var lines = new List<string>(16);

                // If an expected schema is available, compute concrete differences and prepend them to the tooltip
                try
                {
                    if (!string.IsNullOrWhiteSpace(ExpectedSchemaJson))
                    {
                        var expected = System.Text.Json.JsonSerializer.Deserialize<List<ColumnDefinition>>(ExpectedSchemaJson) ?? new();
                        var diff = Beep.Skia.Model.SchemaDiffUtil.Compute(expected, cols);
                        if (diff.HasDifferences())
                        {
                            lines.Add("Schema differences:");
                            int maxShow = 5;
                            foreach (var m in diff.MissingColumns.Take(maxShow)) lines.Add($"- Missing: {m}");
                            if (diff.MissingColumns.Count > maxShow) lines.Add($"  … +{diff.MissingColumns.Count - maxShow} more missing");
                            foreach (var td in diff.TypeDifferences.Take(maxShow)) lines.Add($"- Type: {td.Name} expected {td.ExpectedType}, actual {td.ActualType}");
                            if (diff.TypeDifferences.Count > maxShow) lines.Add($"  … +{diff.TypeDifferences.Count - maxShow} more type differences");
                            foreach (var nd in diff.NullabilityDifferences.Take(maxShow))
                                lines.Add($"- Nullability: {nd.Name} expected {(nd.ExpectedNullable ? "nullable" : "not nullable")}, actual {(nd.ActualNullable ? "nullable" : "not nullable")}");
                            if (diff.NullabilityDifferences.Count > maxShow) lines.Add($"  … +{diff.NullabilityDifferences.Count - maxShow} more nullability differences");
                            foreach (var dd in diff.DefaultDifferences.Take(maxShow))
                                lines.Add($"- Default: {dd.Name} expected '{dd.ExpectedDefault}', actual '{dd.ActualDefault}'");
                            if (diff.DefaultDifferences.Count > maxShow) lines.Add($"  … +{diff.DefaultDifferences.Count - maxShow} more default differences");
                            lines.Add(" "); // spacer before listing columns
                        }
                    }
                }
                catch { }

                // Append a compact columns preview: up to 8 columns (Name:Type), ellipsis if more
                int max = Math.Min(8, n);
                for (int i = 0; i < max; i++)
                {
                    var c = cols[i];
                    var name = c?.Name ?? "";
                    var type = c?.DataType ?? "";
                    var flags = (c?.IsPrimaryKey == true ? " [PK]" : "") + (c?.IsForeignKey == true ? " [FK]" : "");
                    lines.Add(string.IsNullOrWhiteSpace(type) ? name + flags : $"{name}: {type}{flags}");
                }
                if (n > max) lines.Add($"… +{n - max} more");

                // Measure tooltip box
                float padding = 6f;
                float lineH = _textFont.Size + 4;
                float width = 0f;
                using var tp = new SKPaint { IsAntialias = true, Color = SKColors.Black, Style = SKPaintStyle.Fill };
                foreach (var s in lines)
                {
                    width = Math.Max(width, _textFont.MeasureText(s));
                }
                float height = lineH * lines.Count + padding * 2;
                width += padding * 2;
                var mid = new SKPoint((lineStart.X + lineEnd.X) / 2, (lineStart.Y + lineEnd.Y) / 2);
                var box = new SKRect(mid.X - width / 2, mid.Y - height - 24, mid.X + width / 2, mid.Y - 24);

                // Draw background and border
                using var bg = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(230), Style = SKPaintStyle.Fill };
                using var border = new SKPaint { IsAntialias = true, Color = SKColors.Gray, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
                canvas.DrawRoundRect(box, 6, 6, bg);
                canvas.DrawRoundRect(box, 6, 6, border);

                // Draw text lines
                float y = box.Top + padding + _textFont.Size;
                foreach (var s in lines)
                {
                    canvas.DrawText(s, box.Left + padding, y, SKTextAlign.Left, _textFont, tp);
                    y += lineH;
                }
            }
        }

        private void TryDrawInfoBadge(SKCanvas canvas, SKPoint lineStart, SKPoint lineEnd)
        {
            // Draw a subtle info badge near midpoint if schema exists
            if (string.IsNullOrWhiteSpace(SchemaJson)) return;
            var mid = new SKPoint((lineStart.X + lineEnd.X) / 2, (lineStart.Y + lineEnd.Y) / 2);
            var center = new SKPoint(mid.X + 16, mid.Y - 16);
            float r = 7f;
            using var fill = new SKPaint { IsAntialias = true, Color = SKColors.SkyBlue.WithAlpha(200), Style = SKPaintStyle.Fill };
            using var stroke = new SKPaint { IsAntialias = true, Color = SKColors.DarkSlateBlue, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };
            using var txt = new SKPaint { IsAntialias = true, Color = SKColors.White, Style = SKPaintStyle.Fill };
            canvas.DrawCircle(center, r, fill);
            canvas.DrawCircle(center, r, stroke);
            // Draw a tiny 'i'
            var font = _textFont ?? new SKFont { Size = 12 };
            canvas.DrawText("i", center.X - font.MeasureText("i") / 2, center.Y + font.Size * 0.35f, SKTextAlign.Left, font, txt);
        }

        /// <summary>How long a shared head is: the line's arrow size, or four times its width when that is larger, and never
        /// more than about half the segment it sits on.</summary>
        private float HeadSize(SKPoint tip, SKPoint toward) =>
            Math.Min(SKPoint.Distance(tip, toward) * 0.45f, Math.Max(ArrowSize, Paint.StrokeWidth * 4));

        /// <summary>Draws one of the shared heads. The plain Arrow keeps the connector's own open chevron, so connectors
        /// drawn before the shared heads existed look the same.</summary>
        private void DrawHead(SKCanvas canvas, SketchArrowhead head, SKPoint tip, SKPoint toward)
        {
            switch (head)
            {
                case SketchArrowhead.None:
                    return;
                case SketchArrowhead.Arrow:
                    DrawArrow(canvas, Paint, tip, toward, Math.Max(ArrowSize, Paint.StrokeWidth * 4));
                    return;
                case SketchArrowhead.Triangle: case SketchArrowhead.Dot: case SketchArrowhead.Bar: case SketchArrowhead.OutlinedTriangle:
                case SketchArrowhead.OutlinedDot: case SketchArrowhead.Diamond: case SketchArrowhead.OutlinedDiamond: case SketchArrowhead.CrowsFoot:
                    using (var fill = new SKPaint { IsAntialias = true, Color = LineColor, Style = SKPaintStyle.Fill })
                    using (var outline = new SKPaint { IsAntialias = true, Color = LineColor, Style = SKPaintStyle.Stroke, StrokeWidth = Paint.StrokeWidth,
                        StrokeJoin = SKStrokeJoin.Round, StrokeCap = SKStrokeCap.Round })
                        foreach (var (points, filled) in Arrowheads.Shapes(head, Arrowheads.At(tip, toward, HeadSize(tip, toward))))
                        {
                            using var builder = new SKPathBuilder();
                            builder.MoveTo(points[0]);
                            for (int i = 1; i < points.Length; i++) builder.LineTo(points[i]);
                            if (filled) builder.Close();
                            using var shape = builder.Detach();
                            if (filled) canvas.DrawPath(shape, fill);
                            canvas.DrawPath(shape, outline);
                        }
                    return;
            }
        }

        /// <summary>The route as one path. An outlined head keeps its inside clear, so the line stops at the head.</summary>
        private SKPath RoutePath(SKPoint[] route, SKPoint lineStart, SKPoint lineEnd, SketchArrowhead startHead, SKPoint startToward,
            SketchArrowhead endHead, SKPoint endToward)
        {
            var startInset = Arrowheads.Inset(startHead, Arrowheads.At(lineStart, startToward, HeadSize(lineStart, startToward)));
            var endInset = Arrowheads.Inset(endHead, Arrowheads.At(lineEnd, endToward, HeadSize(lineEnd, endToward)));
            using var builder = new SKPathBuilder();
            if (RoutingMode == LineRoutingMode.Curved && startInset == 0 && endInset == 0)
            {
                builder.MoveTo(lineStart);
                var (c1, c2) = ConnectionRoute.CurveControls(lineStart, lineEnd);
                builder.CubicTo(c1, c2, lineEnd);
                return builder.Detach();
            }
            var points = (SKPoint[])route.Clone();
            if (points.Length >= 2)
            {
                points[0] = Toward(points[0], points[1], startInset);
                points[points.Length - 1] = Toward(points[points.Length - 1], points[points.Length - 2], endInset);
            }
            if (_jumpStyle != LineJumpStyle.None && _jumps.Length > 0 && RoutingMode != LineRoutingMode.Curved)
            {
                // Jump distances are along the route; an outlined start head moves the drawn start on along it.
                var at = new float[_jumps.Length];
                for (int i = 0; i < at.Length; i++) at[i] = _jumps[i] - startInset;
                LineJumps.Build(builder, points, at, _jumpStyle, Paint.StrokeWidth);
                return builder.Detach();
            }
            builder.MoveTo(points[0]);
            for (int i = 1; i < points.Length; i++) builder.LineTo(points[i]);
            return builder.Detach();
        }

        /// <summary>A point moved <paramref name="distance"/> from <paramref name="from"/> toward <paramref name="to"/>, never past it.</summary>
        private static SKPoint Toward(SKPoint from, SKPoint to, float distance)
        {
            var d = to - from; var length = d.Length;
            if (distance <= 0 || length <= 0) return from;
            var t = Math.Min(1, distance / length);
            return new SKPoint(from.X + d.X * t, from.Y + d.Y * t);
        }

        /// <summary>The area the middle label covers when it sits on the line, where the line is not drawn; null otherwise.</summary>
        private SKRect? MiddleLabelGap(SKPoint[] route)
        {
            if (string.IsNullOrEmpty(Label3) || Label3Placement != LabelPlacement.Over) return null;
            var baseline = GetMidLabelPosition(route, Label3Placement, Label3);
            var advance = RequiredLabelFontFamily == null ? _textFont.MeasureText(Label3) : Beep.Skia.Text.TextLayout.MeasureLine(RequiredLabelFontFamily, _textFont, Label3, out _);
            var metrics = _textFont.Metrics;
            var gap = new SKRect(baseline.X - advance / 2, baseline.Y + metrics.Ascent, baseline.X + advance / 2, baseline.Y + metrics.Descent);
            gap.Inflate(4, 2);
            return gap;
        }

        /// <summary>
        /// Draws an arrow on the canvas at the specified position.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="paint">The paint to use for drawing.</param>
        /// <param name="arrowPosition">The position of the arrow.</param>
        /// <param name="oppositePoint">The opposite point to calculate the arrow direction.</param>
        /// <param name="size">How long the two strokes of the arrow are.</param>
        private void DrawArrow(SKCanvas canvas, SKPaint paint, SKPoint arrowPosition, SKPoint oppositePoint, float size)
        {
            var angle = (float)Math.Atan2(arrowPosition.Y - oppositePoint.Y, arrowPosition.X - oppositePoint.X);
            var arrowEndPoint1 = new SKPoint(
                arrowPosition.X - size * (float)Math.Cos(angle + Math.PI / 6),
                arrowPosition.Y - size * (float)Math.Sin(angle + Math.PI / 6));
            var arrowEndPoint2 = new SKPoint(
                arrowPosition.X - size * (float)Math.Cos(angle - Math.PI / 6),
                         arrowPosition.Y - size * (float)Math.Sin(angle - Math.PI / 6));
            canvas.DrawLine(arrowPosition, arrowEndPoint1, paint);
            canvas.DrawLine(arrowPosition, arrowEndPoint2, paint);
        }

        /// <summary>
        /// Draw ERD multiplicity marks at a line end: combinations of small circle, single/double bar, and crow's foot.
        /// Orientation is computed from the vector atPoint -> toward so symbols sit outside the endpoint.
        /// </summary>
        private void DrawErdMultiplicity(SKCanvas canvas, SKPaint paint, SKPoint atPoint, SKPoint toward, ERDMultiplicity mult)
        {
            // Geometry parameters (relative to ArrowSize/StrokeWidth)
            float unit = Math.Max(6f, ArrowSize * 0.75f);
            float barLen = unit * 1.2f;
            float gap = Math.Max(2f, paint.StrokeWidth);
            float circleR = unit * 0.45f;
            float footLen = unit * Math.Max(0.1f, CrowFootLength);
            float footSpread = (float)(Math.PI * CrowFootSpreadDegrees / 180.0); // angle offset for the outer prongs

            // Direction from end outward
            var vx = atPoint.X - toward.X;
            var vy = atPoint.Y - toward.Y;
            float len = (float)Math.Sqrt(vx * vx + vy * vy);
            if (len < 0.001f) len = 0.001f;
            vx /= len; vy /= len; // normalized outward

            // Build a local right vector for perpendiculars
            var rx = -vy; var ry = vx;

            // Helper to shift a point along v or r
            SKPoint Along(SKPoint p, float s) => new SKPoint(p.X + vx * s, p.Y + vy * s);
            SKPoint Right(SKPoint p, float s) => new SKPoint(p.X + rx * s, p.Y + ry * s);

            // Start a little off the endpoint so symbols don’t overlap the node outline
            var origin = Along(atPoint, gap + paint.StrokeWidth);

            // Rendering order: near endpoint to far (so circle is closest, then bars, then foot at farthest)
            bool needsCircle = mult == ERDMultiplicity.ZeroOrOne || mult == ERDMultiplicity.ZeroOrMany;
            bool needsBar = mult == ERDMultiplicity.ZeroOrOne || mult == ERDMultiplicity.OneOnly || mult == ERDMultiplicity.OneOrMany || mult == ERDMultiplicity.One;
            bool needsDoubleBar = mult == ERDMultiplicity.OneOnly; // exactly one often shown as double bar
            bool needsFoot = mult == ERDMultiplicity.OneOrMany || mult == ERDMultiplicity.ZeroOrMany || mult == ERDMultiplicity.Many;

            using var symbolPaint = new SKPaint
            {
                Color = paint.Color,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = paint.StrokeWidth,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Butt
            };

            float cursor = 0f;
            if (needsCircle)
            {
                var c = Along(origin, cursor + circleR);
                canvas.DrawCircle(c, circleR, symbolPaint);
                cursor += circleR * 2 + gap;
            }

            if (needsBar)
            {
                var p1 = Right(Along(origin, cursor), -barLen / 2);
                var p2 = Right(Along(origin, cursor), +barLen / 2);
                canvas.DrawLine(p1, p2, symbolPaint);
                cursor += gap * 1.2f;
            }

            if (needsDoubleBar)
            {
                var p1 = Right(Along(origin, cursor), -barLen / 2);
                var p2 = Right(Along(origin, cursor), +barLen / 2);
                canvas.DrawLine(p1, p2, symbolPaint);
                cursor += gap * 1.2f;
            }

            if (needsFoot)
            {
                // Crow’s foot with three prongs from a base point
                var baseP = Along(origin, cursor);
                // central prong straight out
                var mid = Along(baseP, footLen);
                canvas.DrawLine(baseP, mid, symbolPaint);

                // two outer prongs at small angles
                float ang = (float)Math.Atan2(vy, vx);
                var v1 = new SKPoint((float)Math.Cos(ang + footSpread), (float)Math.Sin(ang + footSpread));
                var v2 = new SKPoint((float)Math.Cos(ang - footSpread), (float)Math.Sin(ang - footSpread));
                var pOut1 = new SKPoint(baseP.X + v1.X * footLen, baseP.Y + v1.Y * footLen);
                var pOut2 = new SKPoint(baseP.X + v2.X * footLen, baseP.Y + v2.Y * footLen);
                canvas.DrawLine(baseP, pOut1, symbolPaint);
                canvas.DrawLine(baseP, pOut2, symbolPaint);
            }
        }

        /// <summary>
        /// Draws animated data flow particles along the connection line.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="lineStart">The start point of the line.</param>
        /// <param name="lineEnd">The end point of the line.</param>
        /// <param name="directionMode">The configured flow direction.</param>
        private void DrawDataFlowParticles(SKCanvas canvas, SKPoint lineStart, SKPoint lineEnd, DataFlowDirection directionMode)
        {
            // Calculate line direction and length
            var direction = lineEnd - lineStart;
            var length = (float)Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);

            if (length == 0) return;

            // Normalize direction
            direction = new SKPoint(direction.X / length, direction.Y / length);

            // Possibly reverse or draw both directions
            bool drawForward = directionMode == DataFlowDirection.Forward || directionMode == DataFlowDirection.Bidirectional;
            bool drawBackward = directionMode == DataFlowDirection.Backward || directionMode == DataFlowDirection.Bidirectional;

            // Draw based on selected animation style
            switch (AnimationStyle)
            {
                case FlowAnimationStyle.Dots:
                    DrawDotsAnimation(canvas, lineStart, lineEnd, direction, length, drawForward, drawBackward);
                    break;
                case FlowAnimationStyle.Dashes:
                    DrawDashesAnimation(canvas, lineStart, lineEnd, direction, length, drawForward, drawBackward);
                    break;
                case FlowAnimationStyle.Wave:
                    DrawWaveAnimation(canvas, lineStart, lineEnd, direction, length, drawForward, drawBackward);
                    break;
                case FlowAnimationStyle.Gradient:
                    DrawGradientAnimation(canvas, lineStart, lineEnd, direction, length, drawForward, drawBackward);
                    break;
                case FlowAnimationStyle.Particles:
                    DrawParticlesAnimation(canvas, lineStart, lineEnd, direction, length, drawForward, drawBackward);
                    break;
                case FlowAnimationStyle.Pulse:
                    DrawPulseAnimation(canvas, lineStart, lineEnd, direction, length, drawForward, drawBackward);
                    break;
                case FlowAnimationStyle.Arrows:
                    DrawArrowsAnimation(canvas, lineStart, lineEnd, direction, length, drawForward, drawBackward);
                    break;
                default:
                    DrawDotsAnimation(canvas, lineStart, lineEnd, direction, length, drawForward, drawBackward);
                    break;
            }
        }

        private void DrawDotsAnimation(SKCanvas canvas, SKPoint lineStart, SKPoint lineEnd, SKPoint direction, float length, bool drawForward, bool drawBackward)
        {
            using var particlePaint = new SKPaint { Style = SKPaintStyle.Fill, Color = DataFlowColor, IsAntialias = true };

            void drawAlong(SKPoint s, SKPoint e, bool reverse)
            {
                var dir = reverse ? new SKPoint(-direction.X, -direction.Y) : direction;
                float currentOffset = -dataFlowOffset;
                while (currentOffset < length)
                {
                    if (currentOffset >= 0)
                    {
                        var particlePos = new SKPoint(s.X + dir.X * currentOffset, s.Y + dir.Y * currentOffset);
                        canvas.DrawCircle(particlePos, DataFlowParticleSize, particlePaint);
                        using var glowPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = DataFlowColor.WithAlpha(100), IsAntialias = true };
                        canvas.DrawCircle(particlePos, DataFlowParticleSize * 2, glowPaint);
                    }
                    currentOffset += dataFlowSpacing;
                }
            }

            if (drawForward) drawAlong(lineStart, lineEnd, reverse: false);
            if (drawBackward) drawAlong(lineEnd, lineStart, reverse: true);
        }

        private void DrawDashesAnimation(SKCanvas canvas, SKPoint lineStart, SKPoint lineEnd, SKPoint direction, float length, bool drawForward, bool drawBackward)
        {
            using var dashPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = DataFlowColor, IsAntialias = true, StrokeWidth = 3, StrokeCap = SKStrokeCap.Round };

            void drawAlong(SKPoint s, SKPoint e, bool reverse)
            {
                var dir = reverse ? new SKPoint(-direction.X, -direction.Y) : direction;
                float currentOffset = -dataFlowOffset;
                float dashLength = 15f;
                while (currentOffset < length)
                {
                    if (currentOffset >= 0 && currentOffset + dashLength <= length)
                    {
                        var dashStart = new SKPoint(s.X + dir.X * currentOffset, s.Y + dir.Y * currentOffset);
                        var dashEnd = new SKPoint(s.X + dir.X * (currentOffset + dashLength), s.Y + dir.Y * (currentOffset + dashLength));
                        canvas.DrawLine(dashStart, dashEnd, dashPaint);
                    }
                    currentOffset += dataFlowSpacing;
                }
            }

            if (drawForward) drawAlong(lineStart, lineEnd, reverse: false);
            if (drawBackward) drawAlong(lineEnd, lineStart, reverse: true);
        }

        private void DrawWaveAnimation(SKCanvas canvas, SKPoint lineStart, SKPoint lineEnd, SKPoint direction, float length, bool drawForward, bool drawBackward)
        {
            using var wavePaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = DataFlowColor, IsAntialias = true, StrokeWidth = 2 };

            void drawAlong(SKPoint s, SKPoint e, bool reverse)
            {
                var dir = reverse ? new SKPoint(-direction.X, -direction.Y) : direction;
                var perpDir = new SKPoint(-dir.Y, dir.X); // Perpendicular for wave
                float currentOffset = -dataFlowOffset;
                float waveAmplitude = 6f;
                float waveFrequency = 20f;

                while (currentOffset < length)
                {
                    if (currentOffset >= 0 && currentOffset + waveFrequency <= length)
                    {
                        using var pathBuilder = new SKPathBuilder();
                        bool first = true;
                        for (float t = 0; t <= waveFrequency; t += 2f)
                        {
                            float offset = currentOffset + t;
                            float wave = waveAmplitude * (float)Math.Sin((offset / waveFrequency) * Math.PI * 2);
                            var pos = new SKPoint(
                                s.X + dir.X * offset + perpDir.X * wave,
                                s.Y + dir.Y * offset + perpDir.Y * wave
                            );
                            if (first) { pathBuilder.MoveTo(pos); first = false; } else { pathBuilder.LineTo(pos); }
                        }
                        using var path = pathBuilder.Detach();
                        canvas.DrawPath(path, wavePaint);
                    }
                    currentOffset += dataFlowSpacing * 2;
                }
            }

            if (drawForward) drawAlong(lineStart, lineEnd, reverse: false);
            if (drawBackward) drawAlong(lineEnd, lineStart, reverse: true);
        }

        private void DrawGradientAnimation(SKCanvas canvas, SKPoint lineStart, SKPoint lineEnd, SKPoint direction, float length, bool drawForward, bool drawBackward)
        {
            void drawAlong(SKPoint s, SKPoint e, bool reverse)
            {
                var dir = reverse ? new SKPoint(-direction.X, -direction.Y) : direction;
                float gradientLength = 40f;
                float currentOffset = -dataFlowOffset;

                while (currentOffset < length)
                {
                    if (currentOffset >= 0 && currentOffset + gradientLength <= length)
                    {
                        var gradStart = new SKPoint(s.X + dir.X * currentOffset, s.Y + dir.Y * currentOffset);
                        var gradEnd = new SKPoint(s.X + dir.X * (currentOffset + gradientLength), s.Y + dir.Y * (currentOffset + gradientLength));
                        
                        using var shader = SKShader.CreateLinearGradient(
                            gradStart, gradEnd,
                            new[] { SKColors.Transparent, DataFlowColor, SKColors.Transparent },
                            new[] { 0f, 0.5f, 1f },
                            SKShaderTileMode.Clamp
                        );
                        using var gradPaint = new SKPaint { Style = SKPaintStyle.Stroke, Shader = shader, IsAntialias = true, StrokeWidth = 4, StrokeCap = SKStrokeCap.Round };
                        canvas.DrawLine(gradStart, gradEnd, gradPaint);
                    }
                    currentOffset += dataFlowSpacing * 2;
                }
            }

            if (drawForward) drawAlong(lineStart, lineEnd, reverse: false);
            if (drawBackward) drawAlong(lineEnd, lineStart, reverse: true);
        }

        private void DrawParticlesAnimation(SKCanvas canvas, SKPoint lineStart, SKPoint lineEnd, SKPoint direction, float length, bool drawForward, bool drawBackward)
        {
            using var particlePaint = new SKPaint { Style = SKPaintStyle.Fill, Color = DataFlowColor, IsAntialias = true };

            void drawAlong(SKPoint s, SKPoint e, bool reverse)
            {
                var dir = reverse ? new SKPoint(-direction.X, -direction.Y) : direction;
                float currentOffset = -dataFlowOffset;
                var random = new Random((int)dataFlowOffset);

                while (currentOffset < length)
                {
                    if (currentOffset >= 0)
                    {
                        // Main particle
                        var particlePos = new SKPoint(s.X + dir.X * currentOffset, s.Y + dir.Y * currentOffset);
                        canvas.DrawCircle(particlePos, DataFlowParticleSize, particlePaint);

                        // Trail particles
                        for (int i = 1; i <= 3; i++)
                        {
                            float trailOffset = currentOffset - (i * 5);
                            if (trailOffset >= 0)
                            {
                                var trailPos = new SKPoint(s.X + dir.X * trailOffset, s.Y + dir.Y * trailOffset);
                                byte alpha = (byte)(255 - (i * 70));
                                using var trailPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = DataFlowColor.WithAlpha(alpha), IsAntialias = true };
                                canvas.DrawCircle(trailPos, DataFlowParticleSize * (1 - i * 0.2f), trailPaint);
                            }
                        }
                    }
                    currentOffset += dataFlowSpacing;
                }
            }

            if (drawForward) drawAlong(lineStart, lineEnd, reverse: false);
            if (drawBackward) drawAlong(lineEnd, lineStart, reverse: true);
        }

        private void DrawPulseAnimation(SKCanvas canvas, SKPoint lineStart, SKPoint lineEnd, SKPoint direction, float length, bool drawForward, bool drawBackward)
        {
            void drawAlong(SKPoint s, SKPoint e, bool reverse)
            {
                var dir = reverse ? new SKPoint(-direction.X, -direction.Y) : direction;
                float currentOffset = -dataFlowOffset;

                while (currentOffset < length)
                {
                    if (currentOffset >= 0)
                    {
                        var pulseCenter = new SKPoint(s.X + dir.X * currentOffset, s.Y + dir.Y * currentOffset);
                        
                        // Draw expanding rings
                        for (int ring = 0; ring < 3; ring++)
                        {
                            float ringRadius = DataFlowParticleSize * (1 + ring * 1.5f);
                            byte alpha = (byte)(200 - ring * 60);
                            using var pulsePaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = DataFlowColor.WithAlpha(alpha), IsAntialias = true, StrokeWidth = 2 };
                            canvas.DrawCircle(pulseCenter, ringRadius, pulsePaint);
                        }
                    }
                    currentOffset += dataFlowSpacing * 2;
                }
            }

            if (drawForward) drawAlong(lineStart, lineEnd, reverse: false);
            if (drawBackward) drawAlong(lineEnd, lineStart, reverse: true);
        }

        private void DrawArrowsAnimation(SKCanvas canvas, SKPoint lineStart, SKPoint lineEnd, SKPoint direction, float length, bool drawForward, bool drawBackward)
        {
            using var arrowPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = DataFlowColor, IsAntialias = true };

            void drawAlong(SKPoint s, SKPoint e, bool reverse)
            {
                var dir = reverse ? new SKPoint(-direction.X, -direction.Y) : direction;
                float currentOffset = -dataFlowOffset;
                float arrowSize = DataFlowParticleSize * 2;

                while (currentOffset < length)
                {
                    if (currentOffset >= 0)
                    {
                        var arrowPos = new SKPoint(s.X + dir.X * currentOffset, s.Y + dir.Y * currentOffset);
                        
                        using var pathBuilder = new SKPathBuilder();
                        // Arrow pointing in movement direction
                        pathBuilder.MoveTo(arrowPos.X + dir.X * arrowSize, arrowPos.Y + dir.Y * arrowSize);
                        pathBuilder.LineTo(arrowPos.X - dir.Y * arrowSize * 0.5f, arrowPos.Y + dir.X * arrowSize * 0.5f);
                        pathBuilder.LineTo(arrowPos.X + dir.Y * arrowSize * 0.5f, arrowPos.Y - dir.X * arrowSize * 0.5f);
                        pathBuilder.Close();
                        using var path = pathBuilder.Detach();
                        canvas.DrawPath(path, arrowPaint);
                    }
                    currentOffset += dataFlowSpacing;
                }
            }

            if (drawForward) drawAlong(lineStart, lineEnd, reverse: false);
            if (drawBackward) drawAlong(lineEnd, lineStart, reverse: true);
        }

        /// <summary>
        /// Determines whether the specified point lies on the connection line within the click threshold.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>true if the point is on the line; otherwise, false.</returns>
        public bool LineContainsPoint(SKPoint point)
        {
            // Guard against missing endpoints; use EndPoint when End is null (preview state)
            if (Start == null && End == null)
                return false;

            // Within the click threshold of any segment of the drawn route.
            return ConnectionRoute.Distance(Route(), point) <= LineClickThreshold;
        }

        /// <summary>
        /// Determines whether the specified point lies within an arrow of the connection line.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <param name="startArrow">true to test the start arrow; false to test the end arrow.</param>
        /// <returns>true if the point is within the arrow; otherwise, false.</returns>
        public bool ArrowContainsPoint(SKPoint point, bool startArrow)
        {
            // Check if the point is within the bounding box of the arrow
            if (startArrow && ShowStartArrow && Start != null)
            {
                var arrowBoundingBox = CalculateArrowBoundingBox(Start.Position, (End != null ? End.Position : EndPoint));
                return arrowBoundingBox.Contains(point);
            }
            else if (!startArrow && ShowEndArrow)
            {
                var tip = (End != null ? End.Position : EndPoint);
                var tail = (Start != null ? Start.Position : EndPoint);
                var arrowBoundingBox = CalculateArrowBoundingBox(tip, tail);
                return arrowBoundingBox.Contains(point);
            }
            return false;
        }

        /// <summary>
        /// Calculates the bounding box of an arrow for hit testing purposes.
        /// </summary>
        /// <param name="arrowPosition">The position of the arrow.</param>
        /// <param name="oppositePoint">The opposite point to calculate the arrow direction.</param>
        /// <returns>The bounding rectangle of the arrow.</returns>
        private SKRect CalculateArrowBoundingBox(SKPoint arrowPosition, SKPoint oppositePoint)
        {
            var angle = (float)Math.Atan2(arrowPosition.Y - oppositePoint.Y, arrowPosition.X - oppositePoint.X);
            var arrowEndPoint1 = new SKPoint(
                arrowPosition.X - ArrowSize * (float)Math.Cos(angle + Math.PI / 6),
                arrowPosition.Y - ArrowSize * (float)Math.Sin(angle + Math.PI / 6));
            var arrowEndPoint2 = new SKPoint(
                arrowPosition.X - ArrowSize * (float)Math.Cos(angle - Math.PI / 6),
                arrowPosition.Y - ArrowSize * (float)Math.Sin(angle - Math.PI / 6));
            var left = Math.Min(arrowPosition.X, Math.Min(arrowEndPoint1.X, arrowEndPoint2.X));
            var top = Math.Min(arrowPosition.Y, Math.Min(arrowEndPoint1.Y, arrowEndPoint2.Y));
            var right = Math.Max(arrowPosition.X, Math.Max(arrowEndPoint1.X, arrowEndPoint2.X));
            var bottom = Math.Max(arrowPosition.Y, Math.Max(arrowEndPoint1.Y, arrowEndPoint2.Y));
            return new SKRect(left, top, right, bottom);
        }

        /// <summary>
        /// Handles click events on the connection line, determining whether an arrow or the line itself was clicked.
        /// </summary>
        /// <param name="clickPoint">The point where the click occurred.</param>
        public void OnClick(SKPoint clickPoint)
        {
            try
            {
                if (ArrowContainsPoint(clickPoint, true))
                {
                    StartArrowClicked?.Invoke(this, new ConnectionArgs(Start));
                }
                else if (ArrowContainsPoint(clickPoint, false))
                {
                    EndArrowClicked?.Invoke(this, new ConnectionArgs(End));
                }
                else if (LineContainsPoint(clickPoint))
                {
                    LineClicked?.Invoke(this, new LineArgs(Start, End, this));
                }
            }
            catch (Exception)
            {
                // Log error or handle appropriately
                // For now, we'll silently handle to prevent crashes
                invalidateVisualAction?.Invoke();
            }
        }

        /// <summary>
        /// Starts the animation of the connection line if it is set to be animated.
        /// </summary>
        public void StartAnimation()
        {
            if (IsAnimated)
            {
                animationTimer = new System.Timers.Timer(16); // Approximately 60 frames per second
                animationTimer.Elapsed += Animate;
                animationTimer.AutoReset = true;
                animationTimer.Start();
            }
        }

        /// <summary>
        /// Stops the animation of the connection line.
        /// </summary>
        public void StopAnimation()
        {
            animationTimer?.Stop();
        }

        /// <summary>
        /// Animates the connection line by changing its color over time.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void Animate(object sender, ElapsedEventArgs e)
        {
            bool needsRedraw = false;

            if (IsAnimated)
            {
                // Update the LineColor property based on the elapsed time
                var (hue, saturation, luminosity) = LineColor.ToHsl();
                hue = (hue + 1) % 360;
                LineColor = SkiaUtil.FromHsl(hue, saturation, luminosity);
                needsRedraw = true;
            }

            if (IsDataFlowAnimated)
            {
                // Update data flow animation
                dataFlowOffset += DataFlowSpeed * 0.016f; // 16ms per frame approximately

                // Reset offset when it exceeds the spacing
                if (dataFlowOffset > dataFlowSpacing)
                {
                    dataFlowOffset = 0f;
                }

                needsRedraw = true;
            }

            if (ShowStatusIndicator && Status is LineStatus.Working or LineStatus.Running)
            {
                // Rotate spinner implicitly by advancing a phase via color or offset
                // We can reuse dataFlowOffset to animate spinner rotation
                needsRedraw = true;
            }

            if (needsRedraw)
            {
                // Trigger a redraw of the scene
                invalidateVisualAction?.Invoke();
            }
        }

        /// <summary>Where a start or end label sits: a short way along the route from its port (so it clears the node),
        /// above, on or below the line.</summary>
        private SKPoint GetLabelPosition(IReadOnlyList<SKPoint> route, LabelPlacement placement, bool atStart)
        {
            float length = 0;
            for (int i = 1; i < route.Count; i++) length += SKPoint.Distance(route[i - 1], route[i]);
            var along = length <= 0 ? 0 : Math.Min(0.3f, EndLabelDistance / length);
            var pos = ConnectionRoute.At(route, atStart ? along : 1 - along);
            float yOffset = placement switch
            {
                LabelPlacement.Above => -10,
                LabelPlacement.Below => 20,
                LabelPlacement.Over => 4,
            };
            return pos + new SKPoint(0, yOffset);
        }
        /// <summary>How far along the route from its port a start or end label sits.</summary>
        private const float EndLabelDistance = 28;

        private SKPoint GetMidLabelPosition(IReadOnlyList<SKPoint> route, LabelPlacement placement, string text)
        {
            var mid = ConnectionRoute.Middle(route);
            float yOffset = placement switch
            {
                LabelPlacement.Above => -10,
                LabelPlacement.Below => 20,
                LabelPlacement.Over => 4,
            };
            return mid + new SKPoint(0, yOffset);
        }

        private void ResolveLabelFont()
        {
            if (_resolvedRequiredLabelFamily == RequiredLabelFontFamily) return;
            _textFont.Typeface = RequiredLabelFontFamily == null ? SKTypeface.Default : TypefaceCache.GetRequired(RequiredLabelFontFamily);
            _resolvedRequiredLabelFamily = RequiredLabelFontFamily;
        }

        private void DrawLineLabel(SKCanvas canvas, string text, SKPoint centerBaseline)
        {
            if (RequiredLabelFontFamily == null)
                canvas.DrawText(text, centerBaseline.X, centerBaseline.Y, SKTextAlign.Center, _textFont, _textPaint);
            else // Laid out by the family's registration: nominal as before, shaped (Arabic) with bidi.
                Beep.Skia.Text.TextLayout.DrawCenteredLine(canvas, RequiredLabelFontFamily, _textFont, text, centerBaseline, _textPaint);
        }

        /// <summary>The drawn route as a polyline, from the start port (or preview point) to the end port: what drawing, hit
        /// testing, labels, bounds and line jumps all use.</summary>
        public SKPoint[] Route()
        {
            var start = Start != null ? Start.Position : EndPoint;
            var end = End != null ? End.Position : EndPoint;
            var source = Start?.Component as SkiaComponent; var target = End?.Component as SkiaComponent;
            var key = (RoutingMode, start, end, source?.Bounds, target?.Bounds, Obstacles);
            if (_routeKey == key && _route != null) return _route;
            _route = Obstacles == null
                ? ConnectionRoute.Points(RoutingMode, start, source?.Bounds, end, target?.Bounds)
                : ConnectionRoute.Points(RoutingMode, start, source?.Bounds, end, target?.Bounds, Obstacles, source?.Id ?? Guid.Empty, target?.Id ?? Guid.Empty);
            _routeKey = key;
            return _route;
        }
        // The route is worked out once per change of its ends, routing or obstacles; callers never change it.
        private SKPoint[] _route;
        private (LineRoutingMode, SKPoint, SKPoint, SKRect?, SKRect?, RouteObstacles)? _routeKey;

        /// <summary>Bounds of the routed stroke, arrows, and four ordinary labels. Does not include ERD markers,
        /// animated particles, status indicators, schema badges, or hover tooltips.</summary>
        public SKRect GetPathAndLabelBounds()
        {
            ResolveLabelFont();
            var start = Start?.Position ?? EndPoint; var end = End?.Position ?? EndPoint;
            var route = Route();
            var bounds = new SKRect(start.X, start.Y, start.X, start.Y);
            foreach (var point in route) bounds = SKRect.Union(bounds, new SKRect(point.X, point.Y, point.X, point.Y));
            // Every route point is inside; a curve stays inside its control polygon's box, which its end and control
            // points span. ArrowSize conservatively contains the two arrow strokes for any orientation, with paint
            // width and antialiasing margin.
            var heads = _startArrowhead != SketchArrowhead.None || _endArrowhead != SketchArrowhead.None ? Math.Max(ArrowSize, Paint.StrokeWidth * 4)
                : ShowStartArrow || ShowEndArrow ? ArrowSize : 0;
            var reach = Math.Max(heads, 0) + FlowAnimation.Reach(_flow, Paint.StrokeWidth) + 2;
            bounds.Inflate(reach, reach);
            void Include(string text, SKPoint centerBaseline)
            {
                if (string.IsNullOrEmpty(text)) return;
                SKRect ink;
                var advance = RequiredLabelFontFamily == null ? _textFont.MeasureText(text, out ink) : Beep.Skia.Text.TextLayout.MeasureLine(RequiredLabelFontFamily, _textFont, text, out ink);
                ink.Offset(centerBaseline.X - advance / 2, centerBaseline.Y); ink.Inflate(2, 2);
                bounds = SKRect.Union(bounds, ink);
            }
            Include(Label1, GetLabelPosition(route, Label1Placement, true));
            Include(Label2, GetLabelPosition(route, Label2Placement, false));
            Include(Label3, GetMidLabelPosition(route, Label3Placement, Label3));
            var data = DataTypeLabel ?? Start?.DataType;
            Include(data, GetMidLabelPosition(route, DataLabelPlacement, data));
            return bounds;
        }

        private void DrawStatusIndicator(SKCanvas canvas, SKPoint start, SKPoint end)
        {
            var mid = new SKPoint((start.X + end.X) / 2, (start.Y + end.Y) / 2);
            using var paint = new SKPaint { IsAntialias = true, Color = StatusColor, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            float r = 8f;

            if (Status is LineStatus.Working or LineStatus.Running)
            {
                // Simple spinner arc that appears to rotate as Animate updates
                float sweep = 270f;
                float angle = (dataFlowOffset * 360f / Math.Max(1f, dataFlowSpacing)) % 360f;
                using var pathBuilder = new SKPathBuilder();
                var rect = new SKRect(mid.X - r, mid.Y - r, mid.X + r, mid.Y + r);
                pathBuilder.AddArc(rect, angle, sweep);
                using var path = pathBuilder.Detach();
                canvas.DrawPath(path, paint);
            }
            else if (Status == LineStatus.Warning)
            {
                canvas.DrawCircle(mid, r, paint);
                using var fill = new SKPaint { IsAntialias = true, Color = StatusColor.WithAlpha(60), Style = SKPaintStyle.Fill };
                canvas.DrawCircle(mid, r - 2, fill);
            }
            else if (Status == LineStatus.Error)
            {
                canvas.DrawCircle(mid, r, paint);
                using var cross = new SKPaint { IsAntialias = true, Color = StatusColor, StrokeWidth = 2, Style = SKPaintStyle.Stroke };
                canvas.DrawLine(mid.X - r / 2, mid.Y - r / 2, mid.X + r / 2, mid.Y + r / 2, cross);
                canvas.DrawLine(mid.X - r / 2, mid.Y + r / 2, mid.X + r / 2, mid.Y - r / 2, cross);
            }
        }

        /// <summary>Dashed pattern used for non-identifying ERD relationships.</summary>
        private static readonly float[] ErdDashPattern = { 8f, 4f };

        /// <summary>Gets the reused stroke paint, creating it on first use.</summary>
        private SKPaint EnsureStrokePaint() => _strokePaint ??= new SKPaint();

        /// <summary>
        /// Gets a dash path effect for the pattern, reusing the cached effect while the pattern is
        /// unchanged (the pattern changes per frame for zoom-scaled and preview lines).
        /// </summary>
        private SKPathEffect GetDashEffect(float[] pattern)
        {
            if (_dashEffect != null && _dashEffectPattern != null &&
                _dashEffectPattern.Length == pattern.Length)
            {
                var same = true;
                for (var i = 0; i < pattern.Length; i++)
                {
                    if (_dashEffectPattern[i] != pattern[i]) { same = false; break; }
                }
                if (same) return _dashEffect;
            }

            _dashEffect?.Dispose();
            _dashEffect = SKPathEffect.CreateDash(pattern, 0);
            _dashEffectPattern = (float[])pattern.Clone();
            return _dashEffect;
        }

        /// <summary>
        /// Releases all resources used by the connection line.
        /// </summary>
        public void Dispose()
        {
            Paint?.Dispose();
            _textPaint?.Dispose();
            _strokePaint?.Dispose();
            _dashEffect?.Dispose();
            animationTimer?.Dispose();
        }
    }
}

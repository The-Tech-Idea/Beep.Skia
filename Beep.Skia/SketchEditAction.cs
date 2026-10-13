using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia.Components;

namespace Beep.Skia;

/// <summary>One history entry for an already-applied edit of existing sketch components.</summary>
public sealed class SketchEditAction : DrawingAction
{
    public readonly record struct State(float X, float Y, float Width, float Height, string Name,
        SketchShapeKind Kind, string Stroke, string Fill, float StrokeWidth, string PathData,
        string DisplayText, float TextFontSize, Guid GroupId, float Roughness, int SketchSeed,
        int OpacityPercent, SketchStrokeStyle StrokeStyle, SketchArrowhead StartArrowhead, SketchArrowhead EndArrowhead,
        SketchTextAlign TextAlign, string FontFamily, float Rotation, string ImageAsset, SkiaSharp.SKImage Image, SketchImageFlip ImageFlip,
        SketchBinding? StartBinding, SketchBinding? EndBinding, Beep.Skia.Model.LineRoutingMode Routing, SketchEdges Edges, SketchFillStyle FillStyle, string Link,
        string PressureData, SketchInkWidth InkWidth, SkiaSharp.SKRect? Crop, LineFlow Flow, LineFlowSpeed FlowSpeed, LineFlowDirection FlowDirection,
        float LabelPosition)
    {
        public static State Capture(SketchShape s) => new(s.X, s.Y, s.Width, s.Height, s.Name,
            s.Kind, s.Stroke, s.Fill, s.StrokeWidth, s.PathData, s.DisplayText, s.TextFontSize, s.GroupId, s.Roughness, s.SketchSeed,
            s.OpacityPercent, s.StrokeStyle, s.StartArrowhead, s.EndArrowhead, s.TextAlign, s.FontFamily, s.Rotation, s.ImageAsset, s.Image, s.ImageFlip, s.StartBinding, s.EndBinding, s.Routing, s.Edges, s.FillStyle, s.Link,
            s.PressureData, s.InkWidth, s.Crop, s.Flow, s.FlowSpeed, s.FlowDirection, s.LabelPosition);
        public void Apply(SketchShape s)
        {
            // Kind first: it resets arrowheads to the kind's defaults, which the captured values then replace.
            s.Kind = Kind; s.Stroke = Stroke; s.Fill = Fill; s.StrokeWidth = StrokeWidth;
            s.PathData = PathData; s.PressureData = PressureData; s.InkWidth = InkWidth; s.DisplayText = DisplayText; s.TextFontSize = TextFontSize;
            s.GroupId = GroupId; s.Roughness = Roughness; s.SketchSeed = SketchSeed;
            s.OpacityPercent = OpacityPercent; s.StrokeStyle = StrokeStyle; s.StartArrowhead = StartArrowhead; s.EndArrowhead = EndArrowhead;
            s.TextAlign = TextAlign; s.FontFamily = FontFamily; s.Rotation = Rotation;
            // The decoded image is a host-owned reference that travels with its asset hash.
            s.ImageAsset = ImageAsset; s.Image = Image; s.ImageFlip = ImageFlip; s.Crop = Crop;
            s.StartBinding = StartBinding; s.EndBinding = EndBinding; s.Routing = Routing; s.Edges = Edges; s.FillStyle = FillStyle; s.Link = Link;
            s.Flow = Flow; s.FlowSpeed = FlowSpeed; s.FlowDirection = FlowDirection; s.LabelPosition = LabelPosition;
            s.Name = Name; s.X = X; s.Y = Y; s.Width = Width; s.Height = Height;
        }
    }
    private readonly SketchShape[] _shapes;
    private readonly State[] _before, _after;
    public SketchEditAction(IEnumerable<SketchShape> shapes, IEnumerable<State> before)
    {
        _shapes = shapes.ToArray(); _before = before.ToArray();
        if (_shapes.Length != _before.Length) throw new ArgumentException("Each shape requires its previous state.");
        _after = _shapes.Select(State.Capture).ToArray();
    }
    public override void Execute() { for (int i = 0; i < _shapes.Length; i++) _after[i].Apply(_shapes[i]); }
    public override void Undo() { for (int i = 0; i < _shapes.Length; i++) _before[i].Apply(_shapes[i]); }
}

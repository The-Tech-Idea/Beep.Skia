using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Decision node: a diamond whose ports sit on its tips (see <see cref="LayoutTips"/>). Left to right, the default like
    /// the rest of the flowchart family, the input is at the left tip and the outputs "yes" (port 0) and "no" (port 1) at the
    /// right and bottom tips; with <see cref="DirectedFlowchartControl.ShowTopBottomPorts"/> the flow runs top to bottom: the input at the top tip,
    /// "yes" at the bottom and "no" at the right.
    /// </summary>
    public class DecisionNode : DirectedFlowchartControl
    {
        private string _label = "Decision";
        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        public string Label
        {
            get => _label;
            set
            {
                var v = value ?? string.Empty;
                if (!string.Equals(_label, v, System.StringComparison.Ordinal))
                {
                    _label = v;
                    if (NodeProperties.TryGetValue("Label", out var pi))
                        pi.ParameterCurrentValue = _label;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Text shown inside the diamond.
        /// </summary>
        public DecisionNode()
        {
            Name = "Flowchart Decision";
            Width = 140;
            Height = 100;
            EnsurePortCounts(1, 2);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Text shown inside the diamond."
            };
        }

        /// <summary>Creates a detached decision from validated component and ordered port identities.</summary>
        public DecisionNode(System.Guid id, System.Collections.Generic.IReadOnlyList<System.Guid> inputs,
            System.Collections.Generic.IReadOnlyList<System.Guid> outputs) : this() => RestoreDefinitionIdentity(id, inputs, outputs);

        /// <summary>
        /// Places the ports on the diamond's tips, as Visio glues a decision's branches to its tips and draw.io offers a
        /// rhombus's tips as its connection points. Left to right, one input sits at the left tip; one output at the right
        /// tip; two outputs at the right tip ("yes") and the bottom tip ("no"); three or more run from the top tip over the
        /// right edges to the bottom tip. Further inputs spread over the two edges round the input tip. Top to bottom turns
        /// the same layout a quarter turn: input at the top, "yes" at the bottom, "no" at the right. Ports run in index order
        /// across the flow (down the side left to right, along it top to bottom), so links attached in that order do not
        /// cross at the node; each port sits just outside the outline.
        /// </summary>
        protected override void LayoutSidePorts() => LayoutTips();

        /// <summary>The same tips, turned a quarter: see <see cref="LayoutTips"/>.</summary>
        protected override void LayoutTopBottomPorts() => LayoutTips();

        private void LayoutTips()
        {
            var b = Bounds;
            var top = new SKPoint(b.MidX, b.Top);
            var right = new SKPoint(b.Right, b.MidY);
            var bottom = new SKPoint(b.MidX, b.Bottom);
            var left = new SKPoint(b.Left, b.MidY);
            // Each side's two edges, from the tip where the cross axis starts, through the side's own tip, to where it ends.
            var (inputSide, outputSide) = ShowTopBottomPorts
                ? (new[] { left, top, right }, new[] { left, bottom, right })
                : (new[] { top, left, bottom }, new[] { top, right, bottom });
            var ins = InConnectionPoints;
            for (int i = 0; i < ins.Count; i++) Place(ins[i], i, inputSide, (i + 1f) / (ins.Count + 1f), b);
            var outs = OutConnectionPoints;
            for (int i = 0; i < outs.Count; i++)
            {
                var at = outs.Count switch { 1 => 0.5f, 2 => i == 0 ? 0.5f : 1f, _ => i / (outs.Count - 1f) };
                Place(outs[i], i, outputSide, at, b);
            }
        }

        /// <summary>
        /// Sets a port at <paramref name="at"/> (0 to 1) along the path over two diamond edges of equal length, just outside
        /// the outline: away from the centre at a tip, along the edge's outward normal between tips.
        /// </summary>
        private void Place(IConnectionPoint port, int index, SKPoint[] path, float at, SKRect bounds)
        {
            var centre = new SKPoint(bounds.MidX, bounds.MidY);
            var along = at * 2; // Measured in edges: 0 is the first tip, 1 the side's own tip, 2 the last.
            var nearestTip = System.MathF.Round(along);
            SKPoint point, outward;
            if (System.MathF.Abs(along - nearestTip) < 1e-4f)
            {
                point = path[(int)nearestTip];
                outward = point - centre;
            }
            else
            {
                var edge = (int)System.MathF.Floor(along);
                var a = path[edge]; var c = path[edge + 1]; var t = along - edge;
                point = new SKPoint(a.X + (c.X - a.X) * t, a.Y + (c.Y - a.Y) * t);
                outward = new SKPoint(-(c.Y - a.Y), c.X - a.X);
                if (outward.X * (point.X - centre.X) + outward.Y * (point.Y - centre.Y) < 0) outward = new SKPoint(-outward.X, -outward.Y);
            }
            var length = outward.Length;
            var offset = (PortRadius + 1f) / System.MathF.Max(length, 1e-3f);
            SetPort(port, length < 1e-3f ? point : new SKPoint(point.X + outward.X * offset, point.Y + outward.Y * offset), index);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var b = Bounds;
            var pTop = new SKPoint(b.MidX, b.Top);
            var pRight = new SKPoint(b.Right, b.MidY);
            var pBottom = new SKPoint(b.MidX, b.Bottom);
            var pLeft = new SKPoint(b.Left, b.MidY);

            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xE3, 0xF2, 0xFD), IsAntialias = true };
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x19, 0x76, 0xD2), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = NodeStrokeWidth };
            using var pathBuilder = new SKPathBuilder();

            pathBuilder.MoveTo(pTop);
            pathBuilder.LineTo(pRight);
            pathBuilder.LineTo(pBottom);
            pathBuilder.LineTo(pLeft);
            pathBuilder.Close();

            using var path = pathBuilder.Detach();
            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            DrawCenteredLabel(canvas, b, Label);

            DrawPorts(canvas);
        }
    }
}

using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Start/End node (terminator): pill-shaped rounded rectangle with a label.
    /// </summary>
    public class StartEndNode : DirectedFlowchartControl
    {
        private string _label = "Start/End";
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
        /// Text shown inside the terminator pill.
        /// </summary>
        public StartEndNode()
        {
            Name = "Flowchart Terminator";
            Width = 140;
            Height = 56;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Text shown inside the terminator pill."
            };
        }

        /// <summary>Creates a detached terminator from validated component and ordered port identities.</summary>
        public StartEndNode(System.Guid id, System.Collections.Generic.IReadOnlyList<System.Guid> inputs,
            System.Collections.Generic.IReadOnlyList<System.Guid> outputs) : this() => RestoreDefinitionIdentity(id, inputs, outputs);

        /// <summary>The corner radius of the terminator's outline.</summary>
        private float Rounding => Math.Min(Bounds.Height / 2f, CornerRadius * 2f);

        /// <summary>Inputs down the left edge and outputs down the right edge.</summary>
        protected override void LayoutSidePorts()
        {
            var r = Bounds;
            PlacePortsAlongVerticalEdge(InConnectionPoints, r.Left, r.Top + 6f, r.Bottom - 6f, outwardSign: -1f);
            PlacePortsAlongVerticalEdge(OutConnectionPoints, r.Right, r.Top + 6f, r.Bottom - 6f, outwardSign: +1f);
        }

        /// <summary>Inputs along the top edge and outputs along the bottom edge, left to right, within the straight part
        /// between the rounded ends.</summary>
        protected override void LayoutTopBottomPorts()
        {
            var r = Bounds;
            var straight = Math.Min(Rounding, r.Width / 2f);
            PlaceAlongEdge(InConnectionPoints, r.Left + straight, r.Right - straight, _ => r.Top, -1f);
            PlaceAlongEdge(OutConnectionPoints, r.Left + straight, r.Right - straight, _ => r.Bottom, +1f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            var radius = Rounding;

            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xE8, 0xF5, 0xE9), IsAntialias = true };
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x2E, 0x7D, 0x32), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = NodeStrokeWidth };

            canvas.DrawRoundRect(r, radius, radius, fill);
            canvas.DrawRoundRect(r, radius, radius, stroke);

            DrawCenteredLabel(canvas, r, Label);

            DrawPorts(canvas);
        }
    }
}

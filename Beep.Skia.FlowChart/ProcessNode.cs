using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Process node: rectangle with optional rounded corners and centered label.
    /// </summary>
    public class ProcessNode : DirectedFlowchartControl
    {
        private string _label = "Process";
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
                    // sync editor metadata
                    if (NodeProperties.TryGetValue("Label", out var pi))
                        pi.ParameterCurrentValue = _label;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Text shown inside the process rectangle.
        /// </summary>
        public ProcessNode()
        {
            Name = "Flowchart Process";
            Width = 160;
            Height = 70;
            EnsurePortCounts(2, 2);

            // Seed node-specific metadata for property editor
            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Text shown inside the process rectangle."
            };
        }

        /// <summary>Creates a detached process from validated component and ordered port identities.</summary>
        public ProcessNode(System.Guid id, System.Collections.Generic.IReadOnlyList<System.Guid> inputs,
            System.Collections.Generic.IReadOnlyList<System.Guid> outputs) : this() => RestoreDefinitionIdentity(id, inputs, outputs);

        /// <summary>Inputs down the left edge and outputs down the right edge; the top-to-bottom layout is the base class's.</summary>
        protected override void LayoutSidePorts()
        {
            var r = Bounds;
            PlacePortsAlongVerticalEdge(InConnectionPoints, r.Left, r.Top + 8f, r.Bottom - 8f, outwardSign: -1f);
            PlacePortsAlongVerticalEdge(OutConnectionPoints, r.Right, r.Top + 8f, r.Bottom - 8f, outwardSign: +1f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xFF, 0xF3, 0xE0), IsAntialias = true };
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0xFB, 0x8C, 0x00), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = NodeStrokeWidth };

            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, fill);
            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, stroke);

            DrawCenteredLabel(canvas, r, Label);

            DrawPorts(canvas);
        }
    }
}

using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Start/End node (terminator): pill-shaped rounded rectangle with a label.
    /// </summary>
    public class StartEndNode : FlowchartControl
    {
        private string _label = "Start/End";
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
        private bool _showTopBottomPorts = false;
        public bool ShowTopBottomPorts
        {
            get => _showTopBottomPorts;
            set
            {
                if (_showTopBottomPorts != value)
                {
                    _showTopBottomPorts = value;
                    if (NodeProperties.TryGetValue("ShowTopBottomPorts", out var pi))
                        pi.ParameterCurrentValue = value;
                    // Defer layout to draw; just mark ports dirty and notify
                    MarkPortsDirty();
                    try { OnBoundsChanged(Bounds); } catch { }
                    InvalidateVisual();
                }
            }
        }

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
            NodeProperties["ShowTopBottomPorts"] = new ParameterInfo
            {
                ParameterName = "ShowTopBottomPorts",
                ParameterType = typeof(bool),
                DefaultParameterValue = _showTopBottomPorts,
                ParameterCurrentValue = _showTopBottomPorts,
                Description = "If true, extra ports are distributed along top/bottom edges."
            };
        }

        protected override void LayoutPorts()
        {
            var r = Bounds;
            // In ports along left rounded edge, out ports along right rounded edge
            PlacePortsAlongVerticalEdge(InConnectionPoints, r.Left, r.Top + 6f, r.Bottom - 6f, outwardSign: -1f);
            PlacePortsAlongVerticalEdge(OutConnectionPoints, r.Right, r.Top + 6f, r.Bottom - 6f, outwardSign: +1f);

            if (ShowTopBottomPorts)
            {
                // For Start/End, distribute any extras beyond two side ports across the horizontal edges
                if (InConnectionPoints.Count > 1)
                {
                    var extras = InConnectionPoints.Count - 1; // keep first on side, move remaining to top
                    if (extras > 0)
                    {
                        var list = new System.Collections.Generic.List<IConnectionPoint>();
                        for (int i = 0; i < extras; i++) list.Add(InConnectionPoints[InConnectionPoints.Count - 1 - i]);
                        PlacePortsAlongHorizontalEdge(list, r.Top, r.Left + 12f, r.Right - 12f, outwardSign: -1f);
                    }
                }
                if (OutConnectionPoints.Count > 1)
                {
                    var extras = OutConnectionPoints.Count - 1;
                    if (extras > 0)
                    {
                        var list = new System.Collections.Generic.List<IConnectionPoint>();
                        for (int i = 0; i < extras; i++) list.Add(OutConnectionPoints[OutConnectionPoints.Count - 1 - i]);
                        PlacePortsAlongHorizontalEdge(list, r.Bottom, r.Left + 12f, r.Right - 12f, outwardSign: +1f);
                    }
                }
            }
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            float radius = Math.Min(r.Height / 2f, CornerRadius * 2f);

            using var fill = new SKPaint { Color = new SKColor(0xE8, 0xF5, 0xE9), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0x2E, 0x7D, 0x32), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var text = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);

            canvas.DrawRoundRect(r, radius, radius, fill);
            canvas.DrawRoundRect(r, radius, radius, stroke);

            var tx = r.MidX - font.MeasureText(Label, text) / 2;
            var ty = r.MidY + 5;
            canvas.DrawText(Label, tx, ty, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}

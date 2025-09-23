using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Process node: rectangle with optional rounded corners and centered label.
    /// </summary>
    public class ProcessNode : FlowchartControl
    {
        public string Label { get; set; } = "Process";
        public bool ShowTopBottomPorts { get; set; } = false;

        public ProcessNode()
        {
            Name = "Flowchart Process";
            Width = 160;
            Height = 70;
            EnsurePortCounts(2, 2);
        }

        protected override void LayoutPorts()
        {
            var r = Bounds;
            // Default: distribute on left/right vertical edges
            PlacePortsAlongVerticalEdge(InConnectionPoints, r.Left, r.Top + 8f, r.Bottom - 8f, outwardSign: -1f);
            PlacePortsAlongVerticalEdge(OutConnectionPoints, r.Right, r.Top + 8f, r.Bottom - 8f, outwardSign: +1f);

            if (ShowTopBottomPorts)
            {
                // If there are more ports than can be evenly placed on the sides, put the last ones on top/bottom edges
                if (InConnectionPoints.Count > 2)
                {
                    // Place the extra input port(s) along the top edge, offset outward
                    var extras = InConnectionPoints.Count - 2;
                    var list = new System.Collections.Generic.List<IConnectionPoint>();
                    for (int i = 0; i < extras; i++) list.Add(InConnectionPoints[InConnectionPoints.Count - 1 - i]);
                    // Ensure left->right placement near the middle segment of top
                    PlacePortsAlongHorizontalEdge(list, r.Top, r.Left + 12f, r.Right - 12f, outwardSign: -1f);
                }
                if (OutConnectionPoints.Count > 2)
                {
                    var extras = OutConnectionPoints.Count - 2;
                    var list = new System.Collections.Generic.List<IConnectionPoint>();
                    for (int i = 0; i < extras; i++) list.Add(OutConnectionPoints[OutConnectionPoints.Count - 1 - i]);
                    PlacePortsAlongHorizontalEdge(list, r.Bottom, r.Left + 12f, r.Right - 12f, outwardSign: +1f);
                }
            }
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;
            LayoutPorts();

            var r = Bounds;
            using var fill = new SKPaint { Color = new SKColor(0xFF, 0xF3, 0xE0), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0xFB, 0x8C, 0x00), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var text = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);

            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, fill);
            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, stroke);

            var tx = r.MidX - font.MeasureText(Label, text) / 2;
            var ty = r.MidY + 5;
            canvas.DrawText(Label, tx, ty, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}

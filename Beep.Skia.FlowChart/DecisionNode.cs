using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Decision node: diamond shape with Yes/No ports (left/right) plus optional top/bottom ports.
    /// </summary>
    public class DecisionNode : FlowchartControl
    {
        private string _label = "Decision";
        public string Label
        {
            get => _label;
            set
            {
                var v = value ?? string.Empty;
                if (!string.Equals(_label, v, System.StringComparison.Ordinal))
                {
                    _label = v;
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
                    LayoutPorts();
                    InvalidateVisual();
                }
            }
        }

        public DecisionNode()
        {
            Name = "Flowchart Decision";
            Width = 140;
            Height = 100;
            EnsurePortCounts(1, 2);
        }

        protected override void LayoutPorts()
        {
            var b = Bounds;
            var pTop = new SKPoint(b.MidX, b.Top);
            var pRight = new SKPoint(b.Right, b.MidY);
            var pBottom = new SKPoint(b.MidX, b.Bottom);
            var pLeft = new SKPoint(b.Left, b.MidY);

            // In ports on left diamond edge(s) – split across segments (bottom->left and left->top)
            PlacePortsAcrossTwoSegments(InConnectionPoints, pBottom, pLeft, pTop, outwardSign: -1f);
            // Out ports on right diamond edge(s)
            PlacePortsAcrossTwoSegments(OutConnectionPoints, pTop, pRight, pBottom, outwardSign: +1f);

            if (ShowTopBottomPorts)
            {
                // If extra ports exist, place them slightly outside top/bottom tips
                if (InConnectionPoints.Count > 2)
                {
                    var cp = InConnectionPoints[InConnectionPoints.Count - 1];
                    SetPort(cp, new SKPoint(pTop.X, pTop.Y - (PortRadius + 2)), cp.Index);
                }
                if (OutConnectionPoints.Count > 2)
                {
                    var cp = OutConnectionPoints[OutConnectionPoints.Count - 1];
                    SetPort(cp, new SKPoint(pBottom.X, pBottom.Y + (PortRadius + 2)), cp.Index);
                }
            }
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;
            LayoutPorts();

            var b = Bounds;
            var pTop = new SKPoint(b.MidX, b.Top);
            var pRight = new SKPoint(b.Right, b.MidY);
            var pBottom = new SKPoint(b.MidX, b.Bottom);
            var pLeft = new SKPoint(b.Left, b.MidY);

            using var fill = new SKPaint { Color = new SKColor(0xE3, 0xF2, 0xFD), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0x19, 0x76, 0xD2), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var text = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);
            using var path = new SKPath();

            path.MoveTo(pTop);
            path.LineTo(pRight);
            path.LineTo(pBottom);
            path.LineTo(pLeft);
            path.Close();

            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            var tx = b.MidX - font.MeasureText(Label, text) / 2;
            var ty = b.MidY + 5;
            canvas.DrawText(Label, tx, ty, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}

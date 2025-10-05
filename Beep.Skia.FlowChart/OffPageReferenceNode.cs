using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Off-Page Reference node: upside-down house shape (pentagon) for page references.
    /// Alternative to ConnectorNode with a different visual style.
    /// </summary>
    public class OffPageReferenceNode : FlowchartControl
    {
        private string _referenceId = "";
        public string ReferenceId
        {
            get => _referenceId;
            set
            {
                var v = value ?? "";
                if (!string.Equals(_referenceId, v, System.StringComparison.Ordinal))
                {
                    _referenceId = v;
                    if (NodeProperties.TryGetValue("ReferenceId", out var pi))
                        pi.ParameterCurrentValue = _referenceId;
                    InvalidateVisual();
                }
            }
        }

        private string _pageNumber = "";
        public string PageNumber
        {
            get => _pageNumber;
            set
            {
                var v = value ?? "";
                if (!string.Equals(_pageNumber, v, System.StringComparison.Ordinal))
                {
                    _pageNumber = v;
                    if (NodeProperties.TryGetValue("PageNumber", out var pi))
                        pi.ParameterCurrentValue = _pageNumber;
                    InvalidateVisual();
                }
            }
        }

        public OffPageReferenceNode()
        {
            Name = "Flowchart Off-Page Reference";
            Width = 100;
            Height = 80;
            EnsurePortCounts(1, 1);

            NodeProperties["ReferenceId"] = new ParameterInfo
            {
                ParameterName = "ReferenceId",
                ParameterType = typeof(string),
                DefaultParameterValue = _referenceId,
                ParameterCurrentValue = _referenceId,
                Description = "Unique identifier for matching references."
            };
            NodeProperties["PageNumber"] = new ParameterInfo
            {
                ParameterName = "PageNumber",
                ParameterType = typeof(string),
                DefaultParameterValue = _pageNumber,
                ParameterCurrentValue = _pageNumber,
                Description = "Target page number or diagram name."
            };
        }

        protected override void LayoutPorts()
        {
            var r = Bounds;

            // Input port at top
            if (InConnectionPoints.Count > 0)
            {
                var inPt = InConnectionPoints[0];
                inPt.Center = new SKPoint(r.MidX, r.Top);
                inPt.Position = new SKPoint(r.MidX, r.Top - PortRadius);
                inPt.Bounds = new SKRect(
                    inPt.Center.X - PortRadius,
                    inPt.Center.Y - PortRadius,
                    inPt.Center.X + PortRadius,
                    inPt.Center.Y + PortRadius
                );
                inPt.Rect = inPt.Bounds;
            }

            // Output port at bottom point
            if (OutConnectionPoints.Count > 0)
            {
                var outPt = OutConnectionPoints[0];
                outPt.Center = new SKPoint(r.MidX, r.Bottom);
                outPt.Position = new SKPoint(r.MidX, r.Bottom + PortRadius);
                outPt.Bounds = new SKRect(
                    outPt.Center.X - PortRadius,
                    outPt.Center.Y - PortRadius,
                    outPt.Center.X + PortRadius,
                    outPt.Center.Y + PortRadius
                );
                outPt.Rect = outPt.Bounds;
            }
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            float pointHeight = r.Height * 0.3f;

            // Pentagon (house shape pointing down)
            var points = new SKPoint[]
            {
                new SKPoint(r.Left, r.Top),
                new SKPoint(r.Right, r.Top),
                new SKPoint(r.Right, r.Bottom - pointHeight),
                new SKPoint(r.MidX, r.Bottom),
                new SKPoint(r.Left, r.Bottom - pointHeight)
            };

            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xE8, 0xF5, 0xE9), IsAntialias = true }; // Light green
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x43, 0xA0, 0x47), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Green
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);
            using var path = new SKPath();

            path.MoveTo(points[0]);
            for (int i = 1; i < points.Length; i++)
                path.LineTo(points[i]);
            path.Close();

            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            // Draw reference ID in bold
            if (!string.IsNullOrWhiteSpace(ReferenceId))
            {
                using var boldFont = new SKFont(SKTypeface.Default, 14) { Embolden = true };
                float refWidth = boldFont.MeasureText(ReferenceId, text);
                canvas.DrawText(ReferenceId, r.MidX - refWidth / 2, r.Top + 25, SKTextAlign.Left, boldFont, text);
            }

            // Draw page number below
            if (!string.IsNullOrWhiteSpace(PageNumber))
            {
                using var smallFont = new SKFont(SKTypeface.Default, 10);
                using var grayText = new SKPaint { Color = new SKColor(0x60, 0x60, 0x60), IsAntialias = true };
                float pageWidth = smallFont.MeasureText($"→ {PageNumber}", grayText);
                canvas.DrawText($"→ {PageNumber}", r.MidX - pageWidth / 2, r.Top + 45, SKTextAlign.Left, smallFont, grayText);
            }

            DrawPorts(canvas);
        }
    }
}

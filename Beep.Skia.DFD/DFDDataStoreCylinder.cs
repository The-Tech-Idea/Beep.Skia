using SkiaSharp;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// DFD Data Store (Database): cylindrical shape with top ellipse.
    /// </summary>
    public class DFDDataStoreCylinder : DFDControl
    {
        public DFDDataStoreCylinder()
        {
            Name = "Data Store (Cylinder)";
            DisplayText = "Database";
            TextPosition = Beep.Skia.TextPosition.Below;
            EnsurePortCounts(1, 1);
        }

        protected override void LayoutPorts()
        {
            // Put ports on left/right mid vertical range; avoid very top/bottom caps
            float capInset = Bounds.Height * 0.2f;
            LayoutPortsOnEllipse(topInset: capInset, bottomInset: capInset, outwardOffset: 2f);
        }

    protected override void DrawDFDContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            float capHeight = r.Height * 0.25f;

            using var fill = new SKPaint { Color = MaterialColors.Surface, IsAntialias = true };
            using var stroke = new SKPaint { Color = MaterialColors.Outline, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };

            // Body
            canvas.DrawRect(new SKRect(r.Left, r.Top + capHeight / 2, r.Right, r.Bottom - capHeight / 2), fill);

            // Top ellipse
            var topRect = new SKRect(r.Left, r.Top, r.Right, r.Top + capHeight);
            canvas.DrawOval(topRect, fill);
            canvas.DrawOval(topRect, stroke);

            // Bottom ellipse (implied with arcs/lines)
            var bottomRect = new SKRect(r.Left, r.Bottom - capHeight, r.Right, r.Bottom);
            // Suggest the bottom edge with stroke only (to avoid overfill dark band)
            canvas.DrawOval(bottomRect, stroke);

            // Side strokes
            canvas.DrawLine(r.Left, r.Top + capHeight / 2, r.Left, r.Bottom - capHeight / 2, stroke);
            canvas.DrawLine(r.Right, r.Top + capHeight / 2, r.Right, r.Bottom - capHeight / 2, stroke);

            DrawPorts(canvas);
        }
    }
}

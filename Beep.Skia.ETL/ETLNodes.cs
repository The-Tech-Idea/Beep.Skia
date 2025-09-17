using SkiaSharp;

namespace Beep.Skia.ETL
{
    public class ETLSource : ETLControl
    {
        public ETLSource()
        {
            Title = "Source";
            HeaderColor = new SKColor(0x1A, 0x73, 0xE8);
            Width = 140; Height = 72;
            EnsurePortCounts(inCount: 0, outCount: 1);
        }

        protected override void DrawShape(SKCanvas canvas)
        {
            // Cylinder shape for data source
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            using (var fill = new SKPaint { Color = Background, Style = SKPaintStyle.Fill, IsAntialias = true })
            {
                // Draw cylinder body
                canvas.DrawRect(new SKRect(rect.Left, rect.Top + 8, rect.Right, rect.Bottom - 8), fill);
                
                // Draw top ellipse
                canvas.DrawOval(new SKRect(rect.Left, rect.Top, rect.Right, rect.Top + 16), fill);
                
                // Draw bottom ellipse
                canvas.DrawOval(new SKRect(rect.Left, rect.Bottom - 16, rect.Right, rect.Bottom), fill);
            }
            
            using (var border = new SKPaint { Color = Stroke, Style = SKPaintStyle.Stroke, StrokeWidth = 1.25f, IsAntialias = true })
            {
                // Draw cylinder outline
                canvas.DrawLine(rect.Left, rect.Top + 8, rect.Left, rect.Bottom - 8, border);
                canvas.DrawLine(rect.Right, rect.Top + 8, rect.Right, rect.Bottom - 8, border);
                
                // Draw top ellipse outline
                canvas.DrawOval(new SKRect(rect.Left, rect.Top, rect.Right, rect.Top + 16), border);
                
                // Draw bottom ellipse outline
                canvas.DrawOval(new SKRect(rect.Left, rect.Bottom - 16, rect.Right, rect.Bottom), border);
            }
        }
    }

    public class ETLDestination : ETLControl
    {
        public ETLDestination()
        {
            Title = "Destination";
            HeaderColor = new SKColor(0xE5, 0x39, 0x35);
            Width = 160; Height = 72;
            EnsurePortCounts(inCount: 1, outCount: 0);
        }

        protected override void DrawShape(SKCanvas canvas)
        {
            // Funnel shape for data destination
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            using (var path = new SKPath())
            {
                // Create funnel shape (wider at top, narrower at bottom)
                path.MoveTo(rect.Left + 10, rect.Top);
                path.LineTo(rect.Right - 10, rect.Top);
                path.LineTo(rect.Right - 30, rect.Bottom);
                path.LineTo(rect.Left + 30, rect.Bottom);
                path.Close();

                using (var fill = new SKPaint { Color = Background, Style = SKPaintStyle.Fill, IsAntialias = true })
                    canvas.DrawPath(path, fill);
                using (var border = new SKPaint { Color = Stroke, Style = SKPaintStyle.Stroke, StrokeWidth = 1.25f, IsAntialias = true })
                    canvas.DrawPath(path, border);
            }
        }
    }

    public class ETLTransform : ETLControl
    {
        public ETLTransform()
        {
            Title = "Transform";
            HeaderColor = new SKColor(0x43, 0xA0, 0x47);
            Width = 160; Height = 84;
            EnsurePortCounts(inCount: 1, outCount: 1);
        }

        protected override void DrawShape(SKCanvas canvas)
        {
            // Hexagon shape for transformation
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            using (var path = new SKPath())
            {
                float indent = 20f;
                // Create hexagon
                path.MoveTo(rect.Left + indent, rect.Top);
                path.LineTo(rect.Right - indent, rect.Top);
                path.LineTo(rect.Right, rect.MidY);
                path.LineTo(rect.Right - indent, rect.Bottom);
                path.LineTo(rect.Left + indent, rect.Bottom);
                path.LineTo(rect.Left, rect.MidY);
                path.Close();

                using (var fill = new SKPaint { Color = Background, Style = SKPaintStyle.Fill, IsAntialias = true })
                    canvas.DrawPath(path, fill);
                using (var border = new SKPaint { Color = Stroke, Style = SKPaintStyle.Stroke, StrokeWidth = 1.25f, IsAntialias = true })
                    canvas.DrawPath(path, border);
            }
        }
    }

    public class ETLFilter : ETLControl
    {
        public ETLFilter()
        {
            Title = "Filter";
            HeaderColor = new SKColor(0xFB, 0xBC, 0x05);
            Width = 140; Height = 72;
            EnsurePortCounts(inCount: 1, outCount: 1);
        }

        protected override void DrawShape(SKCanvas canvas)
        {
            // Diamond shape for filtering
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            using (var path = new SKPath())
            {
                // Create diamond
                path.MoveTo(rect.MidX, rect.Top);
                path.LineTo(rect.Right, rect.MidY);
                path.LineTo(rect.MidX, rect.Bottom);
                path.LineTo(rect.Left, rect.MidY);
                path.Close();

                using (var fill = new SKPaint { Color = Background, Style = SKPaintStyle.Fill, IsAntialias = true })
                    canvas.DrawPath(path, fill);
                using (var border = new SKPaint { Color = Stroke, Style = SKPaintStyle.Stroke, StrokeWidth = 1.25f, IsAntialias = true })
                    canvas.DrawPath(path, border);
            }
        }
    }

    public class ETLJoin : ETLControl
    {
        public ETLJoin()
        {
            Title = "Join";
            HeaderColor = new SKColor(0x8E, 0x24, 0xAA);
            Width = 180; Height = 96;
            EnsurePortCounts(inCount: 2, outCount: 1);
        }

        protected override void DrawShape(SKCanvas canvas)
        {
            // Triangle pointing right for join operation
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            using (var path = new SKPath())
            {
                // Create right-pointing triangle with flat left side
                path.MoveTo(rect.Left, rect.Top + 10);
                path.LineTo(rect.Right - 20, rect.MidY);
                path.LineTo(rect.Left, rect.Bottom - 10);
                path.LineTo(rect.Left + 20, rect.Bottom - 10);
                path.LineTo(rect.Left + 20, rect.Top + 10);
                path.Close();

                using (var fill = new SKPaint { Color = Background, Style = SKPaintStyle.Fill, IsAntialias = true })
                    canvas.DrawPath(path, fill);
                using (var border = new SKPaint { Color = Stroke, Style = SKPaintStyle.Stroke, StrokeWidth = 1.25f, IsAntialias = true })
                    canvas.DrawPath(path, border);
            }
        }
    }

    public class ETLAggregate : ETLControl
    {
        public ETLAggregate()
        {
            Title = "Aggregate";
            HeaderColor = new SKColor(0x00, 0x95, 0x88);
            Width = 160; Height = 96;
            EnsurePortCounts(inCount: 1, outCount: 1);
        }

        protected override void DrawShape(SKCanvas canvas)
        {
            // Octagon shape for aggregation
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            using (var path = new SKPath())
            {
                float corner = 15f;
                // Create octagon (rectangle with cut corners)
                path.MoveTo(rect.Left + corner, rect.Top);
                path.LineTo(rect.Right - corner, rect.Top);
                path.LineTo(rect.Right, rect.Top + corner);
                path.LineTo(rect.Right, rect.Bottom - corner);
                path.LineTo(rect.Right - corner, rect.Bottom);
                path.LineTo(rect.Left + corner, rect.Bottom);
                path.LineTo(rect.Left, rect.Bottom - corner);
                path.LineTo(rect.Left, rect.Top + corner);
                path.Close();

                using (var fill = new SKPaint { Color = Background, Style = SKPaintStyle.Fill, IsAntialias = true })
                    canvas.DrawPath(path, fill);
                using (var border = new SKPaint { Color = Stroke, Style = SKPaintStyle.Stroke, StrokeWidth = 1.25f, IsAntialias = true })
                    canvas.DrawPath(path, border);
            }
        }
    }
}

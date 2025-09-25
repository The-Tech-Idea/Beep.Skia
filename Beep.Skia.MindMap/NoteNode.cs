using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.MindMap
{
    public class NoteNode : MindMapControl
    {
        public string Title { get; set; } = "Note";
        public string? Notes { get; set; }

        public NoteNode()
        {
            Width = 160; Height = 80;
            BackgroundColor = MaterialColors.TertiaryContainer;
            BorderColor = MaterialColors.Tertiary;
            TextColor = MaterialColors.OnTertiaryContainer;
            EnsurePortCounts(1, 0);
        }

        protected override void LayoutPorts()
        {
            // Avoid placing an output in the folded top-right area; if outputs are added later,
            // increase the top inset a bit so ports sit below the fold height.
            float fold = Math.Min(Width, Height) * 0.2f;
            // If there are outputs, keep them below the fold; inputs still centered on left.
            if (OutConnectionPoints.Count > 0)
                LayoutPortsRightEdge(topInset: fold + 6f, bottomInset: 6f);
            else
                LayoutPortsRightEdge(topInset: 6f, bottomInset: 6f);
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = BorderThickness, IsAntialias = true };

            float r = 8f;
            float fold = Math.Min(Width, Height) * 0.2f;
            using var path = new SKPath();
            path.MoveTo(X + r, Y);
            path.LineTo(X + Width - fold - r, Y);
            path.ArcTo(new SKRect(X + Width - fold - 2 * r, Y, X + Width - fold, Y + 2 * r), 270, 90, false);
            path.LineTo(X + Width - fold, Y + fold);
            path.LineTo(X + Width, Y + fold);
            path.LineTo(X + Width, Y + Height - r);
            path.ArcTo(new SKRect(X + Width - 2 * r, Y + Height - 2 * r, X + Width, Y + Height), 0, 90, false);
            path.LineTo(X + r, Y + Height);
            path.ArcTo(new SKRect(X, Y + Height - 2 * r, X + 2 * r, Y + Height), 90, 90, false);
            path.LineTo(X, Y + r);
            path.ArcTo(new SKRect(X, Y, X + 2 * r, Y + 2 * r), 180, 90, false);
            path.Close();

            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            using var font = new SKFont(SKTypeface.Default, 12) { Embolden = true };
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            canvas.DrawText(Title ?? Name ?? string.Empty, X + Width / 2f, Y + 22, SKTextAlign.Center, font, text);

            if (!string.IsNullOrWhiteSpace(Notes))
            {
                using var font2 = new SKFont(SKTypeface.Default, 11);
                using var t2 = new SKPaint { Color = MaterialColors.OnSurfaceVariant, IsAntialias = true };
                canvas.DrawText(Notes!.Length > 140 ? Notes!.Substring(0, 140) + "…" : Notes!, X + 10, Y + 42, font2, t2);
            }

            DrawConnectionPoints(canvas);
        }
    }
}

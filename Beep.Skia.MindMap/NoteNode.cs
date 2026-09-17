using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.MindMap
{
    public class NoteNode : MindMapControl
    {
        private string _title = "Note";
        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title
        {
            get => _title;
            set
            {
                var v = value ?? string.Empty;
                if (_title == v) return;
                _title = v;
                if (NodeProperties.TryGetValue("Title", out var pi)) pi.ParameterCurrentValue = _title;
                InvalidateVisual();
            }
        }

        private string? _notes;
        /// <summary>
        /// Gets or sets the notes.
        /// </summary>
        public string? Notes
        {
            get => _notes;
            set
            {
                var v = value ?? string.Empty;
                if (_notes == v) return;
                _notes = v;
                if (NodeProperties.TryGetValue("Notes", out var pi)) pi.ParameterCurrentValue = _notes;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Note title
        /// </summary>
        public NoteNode()
        {
            Width = 160; Height = 80;
            BackgroundColor = MaterialColors.TertiaryContainer;
            BorderColor = MaterialColors.Tertiary;
            TextColor = MaterialColors.OnTertiaryContainer;
            if (NodeProperties.TryGetValue("TextColor", out var piTxt)) piTxt.ParameterCurrentValue = TextColor;
            EnsurePortCounts(1, 0);

            NodeProperties["Title"] = new ParameterInfo { ParameterName = "Title", ParameterType = typeof(string), DefaultParameterValue = _title, ParameterCurrentValue = _title, Description = "Note title" };
            NodeProperties["Notes"] = new ParameterInfo { ParameterName = "Notes", ParameterType = typeof(string), DefaultParameterValue = _notes ?? string.Empty, ParameterCurrentValue = _notes ?? string.Empty, Description = "Notes content" };
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

        protected override void DrawMindMapContent(SKCanvas canvas, DrawingContext context)
        {
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = BorderThickness, IsAntialias = true };

            float r = 8f;
            float fold = Math.Min(Width, Height) * 0.2f;
            using var pathBuilder = new SKPathBuilder();
            pathBuilder.MoveTo(X + r, Y);
            pathBuilder.LineTo(X + Width - fold - r, Y);
            pathBuilder.ArcTo(new SKRect(X + Width - fold - 2 * r, Y, X + Width - fold, Y + 2 * r), 270, 90, false);
            pathBuilder.LineTo(X + Width - fold, Y + fold);
            pathBuilder.LineTo(X + Width, Y + fold);
            pathBuilder.LineTo(X + Width, Y + Height - r);
            pathBuilder.ArcTo(new SKRect(X + Width - 2 * r, Y + Height - 2 * r, X + Width, Y + Height), 0, 90, false);
            pathBuilder.LineTo(X + r, Y + Height);
            pathBuilder.ArcTo(new SKRect(X, Y + Height - 2 * r, X + 2 * r, Y + Height), 90, 90, false);
            pathBuilder.LineTo(X, Y + r);
            pathBuilder.ArcTo(new SKRect(X, Y, X + 2 * r, Y + 2 * r), 180, 90, false);
            pathBuilder.Close();

            using var path = pathBuilder.Detach();
            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            using var font = new SKFont(SKTypeface.Default, 12) { Embolden = true };
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            canvas.DrawText(Title ?? Name ?? string.Empty, X + Width / 2f, Y + 22, SKTextAlign.Center, font, text);

            if (!string.IsNullOrWhiteSpace(Notes))
            {
                MindMapRichText.Draw(
                    canvas,
                    Notes!,
                    new SKRect(X + 8f, Y + 30f, X + Width - 8f, Y + Height - 8f),
                    MaterialColors.OnSurfaceVariant,
                    fontSize: 10f,
                    lineHeight: 13f);
            }

            DrawConnectionPoints(canvas);
        }
    }
}

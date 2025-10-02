using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.MindMap
{
    public class SubTopicNode : MindMapControl
    {
        private string _title = "SubTopic";
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

        public SubTopicNode()
        {
            Width = 140; Height = 56;
            BackgroundColor = MaterialColors.SurfaceVariant;
            BorderColor = MaterialColors.OutlineVariant;
            TextColor = MaterialColors.OnSurfaceVariant;
            if (NodeProperties.TryGetValue("TextColor", out var piTxt)) piTxt.ParameterCurrentValue = TextColor;
            EnsurePortCounts(1, 2);

            NodeProperties["Title"] = new ParameterInfo { ParameterName = "Title", ParameterType = typeof(string), DefaultParameterValue = _title, ParameterCurrentValue = _title, Description = "Topic title" };
            NodeProperties["Notes"] = new ParameterInfo { ParameterName = "Notes", ParameterType = typeof(string), DefaultParameterValue = _notes ?? string.Empty, ParameterCurrentValue = _notes ?? string.Empty, Description = "Optional notes" };
        }

        protected override void LayoutPorts()
        {
            // Use default right-edge layout for rectangular subtopics
            base.LayoutPorts();
        }

        protected override void DrawMindMapContent(SKCanvas canvas, DrawingContext context)
        {
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = BorderThickness, IsAntialias = true };
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(rect, 8, 8, fill);
            canvas.DrawRoundRect(rect, 8, 8, stroke);

            using var font = new SKFont(SKTypeface.Default, 12) { Embolden = true };
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            canvas.DrawText(Title ?? Name ?? string.Empty, X + Width / 2f, Y + Height / 2f + 4, SKTextAlign.Center, font, text);
            DrawConnectionPoints(canvas);
        }
    }
}

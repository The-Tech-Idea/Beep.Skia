using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;
using System.Collections.Generic;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a text annotation for documenting processes.
    /// Displayed as a text box with optional border and background.
    /// </summary>
    public class Annotation : BusinessControl
    {
        public string AnnotationText { get; set; } = "Annotation text";
        public AnnotationType AnnotationType { get; set; } = AnnotationType.Note;
        public bool ShowBorder { get; set; } = true;
        public bool ShowBackground { get; set; } = true;

        public Annotation()
        {
            Width = 120;
            Height = 60;
            Name = "Annotation";
            ComponentType = BusinessComponentType.Task;
            BackgroundColor = SKColors.LightYellow;
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            if (ShowBackground)
            {
                using var fillPaint = new SKPaint
                {
                    Color = BackgroundColor,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };

                var rect = new SKRect(X, Y, X + Width, Y + Height);
                canvas.DrawRoundRect(rect, 6, 6, fillPaint);
            }

            if (ShowBorder)
            {
                using var borderPaint = new SKPaint
                {
                    Color = BorderColor,
                    StrokeWidth = 1,
                    Style = SKPaintStyle.Stroke,
                    IsAntialias = true
                };

                var rect = new SKRect(X, Y, X + Width, Y + Height);
                canvas.DrawRoundRect(rect, 6, 6, borderPaint);
            }

            // Draw annotation type indicator
            DrawAnnotationIndicator(canvas);
        }

        private void DrawAnnotationIndicator(SKCanvas canvas)
        {
            using var indicatorPaint = new SKPaint
            {
                Color = BorderColor,
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            float indicatorX = X + 6;
            float indicatorY = Y + 6;
            float size = 10;

            switch (AnnotationType)
            {
                case AnnotationType.Note:
                    // Draw note icon (page with lines)
                    canvas.DrawRect(indicatorX, indicatorY, indicatorX + size * 0.8f, indicatorY + size, indicatorPaint);
                    // Lines
                    for (int i = 1; i < 3; i++)
                    {
                        float lineY = indicatorY + (i * size / 3);
                        canvas.DrawLine(indicatorX + 2, lineY, indicatorX + size * 0.6f, lineY, indicatorPaint);
                    }
                    break;

                case AnnotationType.Warning:
                    // Draw warning triangle
                    using (var path = new SKPath())
                    {
                        path.MoveTo(indicatorX + size / 2, indicatorY);
                        path.LineTo(indicatorX, indicatorY + size);
                        path.LineTo(indicatorX + size, indicatorY + size);
                        path.Close();
                        canvas.DrawPath(path, indicatorPaint);
                    }
                    // Exclamation mark
                    canvas.DrawLine(indicatorX + size / 2, indicatorY + 3, indicatorX + size / 2, indicatorY + 6, indicatorPaint);
                    canvas.DrawCircle(indicatorX + size / 2, indicatorY + 8, 1, indicatorPaint);
                    break;

                case AnnotationType.Comment:
                    // Draw speech bubble
                    canvas.DrawOval(new SKRect(indicatorX, indicatorY, indicatorX + size, indicatorY + size * 0.8f), indicatorPaint);
                    // Bubble tail
                    using (var tailPath = new SKPath())
                    {
                        tailPath.MoveTo(indicatorX + size * 0.3f, indicatorY + size * 0.8f);
                        tailPath.LineTo(indicatorX + size * 0.5f, indicatorY + size);
                        tailPath.LineTo(indicatorX + size * 0.7f, indicatorY + size * 0.8f);
                        canvas.DrawPath(tailPath, indicatorPaint);
                    }
                    break;
            }
        }

        protected override void DrawComponentText(SKCanvas canvas)
        {
            if (string.IsNullOrEmpty(AnnotationText))
                return;

            using var font = new SKFont(SKTypeface.Default, 9);
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            // Word wrap the text
            var words = AnnotationText.Split(' ');
            var lines = new List<string>();
            var currentLine = "";

            foreach (var word in words)
            {
                var testLine = currentLine + (currentLine.Length > 0 ? " " : "") + word;
                var textWidth = font.MeasureText(testLine);

                if (textWidth > Width - 20)
                {
                    if (currentLine.Length > 0)
                    {
                        lines.Add(currentLine);
                        currentLine = word;
                    }
                    else
                    {
                        lines.Add(word);
                        currentLine = "";
                    }
                }
                else
                {
                    currentLine = testLine;
                }
            }

            if (currentLine.Length > 0)
            {
                lines.Add(currentLine);
            }

            // Draw lines
            float lineHeight = 12;
            float startY = Y + 20;

            for (int i = 0; i < Math.Min(lines.Count, 3); i++) // Max 3 lines
            {
                canvas.DrawText(lines[i], X + 10, startY + (i * lineHeight), SKTextAlign.Left, font, paint);
            }

            // Draw "..." if text was truncated
            if (lines.Count > 3)
            {
                canvas.DrawText("...", X + Width - 15, startY + (2 * lineHeight), SKTextAlign.Left, font, paint);
            }
        }
    }
}
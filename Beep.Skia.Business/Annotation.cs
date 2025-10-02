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
        private string _annotationText = "Annotation text";
        private AnnotationType _annotationType = AnnotationType.Note;
        private bool _showBorder = true;
        private bool _showBackground = true;
        public string AnnotationText
        {
            get => _annotationText;
            set
            {
                var v = value ?? string.Empty;
                if (_annotationText != v)
                {
                    _annotationText = v;
                    if (NodeProperties.TryGetValue("AnnotationText", out var p)) p.ParameterCurrentValue = _annotationText; else NodeProperties["AnnotationText"] = new ParameterInfo { ParameterName = "AnnotationText", ParameterType = typeof(string), DefaultParameterValue = _annotationText, ParameterCurrentValue = _annotationText, Description = "Annotation text" };
                    InvalidateVisual();
                }
            }
        }
        public AnnotationType AnnotationType
        {
            get => _annotationType;
            set
            {
                if (_annotationType != value)
                {
                    _annotationType = value;
                    if (NodeProperties.TryGetValue("AnnotationType", out var p)) p.ParameterCurrentValue = _annotationType; else NodeProperties["AnnotationType"] = new ParameterInfo { ParameterName = "AnnotationType", ParameterType = typeof(AnnotationType), DefaultParameterValue = _annotationType, ParameterCurrentValue = _annotationType, Description = "Annotation type", Choices = Enum.GetNames(typeof(AnnotationType)) };
                    InvalidateVisual();
                }
            }
        }
        public bool ShowBorder
        {
            get => _showBorder;
            set
            {
                if (_showBorder != value)
                {
                    _showBorder = value;
                    if (NodeProperties.TryGetValue("ShowBorder", out var p)) p.ParameterCurrentValue = _showBorder; else NodeProperties["ShowBorder"] = new ParameterInfo { ParameterName = "ShowBorder", ParameterType = typeof(bool), DefaultParameterValue = _showBorder, ParameterCurrentValue = _showBorder, Description = "Show border" };
                    InvalidateVisual();
                }
            }
        }
        public bool ShowBackground
        {
            get => _showBackground;
            set
            {
                if (_showBackground != value)
                {
                    _showBackground = value;
                    if (NodeProperties.TryGetValue("ShowBackground", out var p)) p.ParameterCurrentValue = _showBackground; else NodeProperties["ShowBackground"] = new ParameterInfo { ParameterName = "ShowBackground", ParameterType = typeof(bool), DefaultParameterValue = _showBackground, ParameterCurrentValue = _showBackground, Description = "Show background" };
                    InvalidateVisual();
                }
            }
        }

        public Annotation()
        {
            Width = 120;
            Height = 60;
            Name = "Annotation";
            ComponentType = BusinessComponentType.Task;
            BackgroundColor = MaterialColors.SurfaceContainer;
            NodeProperties["AnnotationText"] = new ParameterInfo { ParameterName = "AnnotationText", ParameterType = typeof(string), DefaultParameterValue = _annotationText, ParameterCurrentValue = _annotationText, Description = "Annotation text" };
            NodeProperties["AnnotationType"] = new ParameterInfo { ParameterName = "AnnotationType", ParameterType = typeof(AnnotationType), DefaultParameterValue = _annotationType, ParameterCurrentValue = _annotationType, Description = "Annotation type", Choices = Enum.GetNames(typeof(AnnotationType)) };
            NodeProperties["ShowBorder"] = new ParameterInfo { ParameterName = "ShowBorder", ParameterType = typeof(bool), DefaultParameterValue = _showBorder, ParameterCurrentValue = _showBorder, Description = "Show border" };
            NodeProperties["ShowBackground"] = new ParameterInfo { ParameterName = "ShowBackground", ParameterType = typeof(bool), DefaultParameterValue = _showBackground, ParameterCurrentValue = _showBackground, Description = "Show background" };
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

        protected override void LayoutPorts()
        {
            // Non-interrupting box: default 0 in / 0 out unless used as connector; keep 1/1 for flexibility
            EnsurePortCounts(1, 1);
            LayoutPortsVerticalSegments(topInset: 4f, bottomInset: 4f);
        }
    }
}
using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a data object that flows through the process.
    /// Displayed as a rectangle with a data icon and type indicator.
    /// </summary>
    public class DataObject : BusinessControl
    {
        public string DataName { get; set; } = "Data Object";
        public DataType DataType { get; set; } = DataType.Document;
        public string DataFormat { get; set; } = "";
        public bool IsCollection { get; set; } = false;

        public DataObject()
        {
            Width = 100;
            Height = 70;
            Name = "Data Object";
            ComponentType = BusinessComponentType.Document;
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            using var fillPaint = new SKPaint
            {
                Color = BackgroundColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var borderPaint = new SKPaint
            {
                Color = BorderColor,
                StrokeWidth = BorderThickness,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            var rect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(rect, 8, 8, fillPaint);
            canvas.DrawRoundRect(rect, 8, 8, borderPaint);

            // Draw data type icon
            DrawDataIcon(canvas);

            // Draw collection indicator
            if (IsCollection)
            {
                DrawCollectionIndicator(canvas);
            }
        }

        private void DrawDataIcon(SKCanvas canvas)
        {
            using var iconPaint = new SKPaint
            {
                Color = BorderColor,
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            float iconX = X + 8;
            float iconY = Y + 8;
            float iconSize = 16;

            switch (DataType)
            {
                case DataType.Document:
                    {
                        // Draw document with fold
                        canvas.DrawRect(iconX, iconY, iconX + iconSize, iconY + iconSize, iconPaint);
                        using (var foldPath = new SKPath())
                        {
                            foldPath.MoveTo(iconX + iconSize - 4, iconY);
                            foldPath.LineTo(iconX + iconSize, iconY + 4);
                            foldPath.LineTo(iconX + iconSize, iconY + iconSize);
                            canvas.DrawPath(foldPath, iconPaint);
                        }
                    }
                    break;

                case DataType.Database:
                    {
                        // Draw cylinder
                        canvas.DrawArc(new SKRect(iconX, iconY, iconX + iconSize, iconY + 6), 0, 180, false, iconPaint);
                        canvas.DrawArc(new SKRect(iconX, iconY + iconSize - 6, iconX + iconSize, iconY + iconSize), 180, 180, false, iconPaint);
                        canvas.DrawLine(iconX, iconY + 3, iconX, iconY + iconSize - 3, iconPaint);
                        canvas.DrawLine(iconX + iconSize, iconY + 3, iconX + iconSize, iconY + iconSize - 3, iconPaint);
                    }
                    break;

                case DataType.Email:
                    {
                        // Draw envelope
                        using (var envelopePath = new SKPath())
                        {
                            envelopePath.MoveTo(iconX, iconY + iconSize);
                            envelopePath.LineTo(iconX + iconSize / 2, iconY);
                            envelopePath.LineTo(iconX + iconSize, iconY + iconSize);
                            envelopePath.LineTo(iconX, iconY + iconSize);
                            envelopePath.MoveTo(iconX, iconY + iconSize);
                            envelopePath.LineTo(iconX + iconSize / 2, iconY + iconSize / 2);
                            envelopePath.LineTo(iconX + iconSize, iconY + iconSize);
                            canvas.DrawPath(envelopePath, iconPaint);
                        }
                    }
                    break;

                case DataType.XML:
                    {
                        // Draw XML brackets
                        using var xmlPaint = new SKPaint
                        {
                            Color = BorderColor,
                            IsAntialias = true
                        };
                        using var font = new SKFont(SKTypeface.Default, 8);
                        canvas.DrawText("<>", iconX, iconY + 12, SKTextAlign.Left, font, xmlPaint);
                    }
                    break;
            }
        }

        private void DrawCollectionIndicator(SKCanvas canvas)
        {
            using var indicatorPaint = new SKPaint
            {
                Color = MaterialColors.Tertiary,
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            // Draw three stacked rectangles
            float indicatorX = X + Width - 15;
            float indicatorY = Y + Height - 20;
            float rectWidth = 8;
            float rectHeight = 3;

            for (int i = 0; i < 3; i++)
            {
                var rect = new SKRect(
                    indicatorX, indicatorY + (i * 4),
                    indicatorX + rectWidth, indicatorY + (i * 4) + rectHeight);
                canvas.DrawRect(rect, indicatorPaint);
            }
        }

        protected override void DrawComponentText(SKCanvas canvas)
        {
            using var nameFont = new SKFont(SKTypeface.Default, 9) { Embolden = true };
            using var typeFont = new SKFont(SKTypeface.Default, 7);
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float nameY = Y + Height + 12;
            float typeY = nameY + 10;

            canvas.DrawText(DataName, centerX, nameY, SKTextAlign.Center, nameFont, paint);

            if (!string.IsNullOrEmpty(DataFormat))
            {
                canvas.DrawText(DataFormat, centerX, typeY, SKTextAlign.Center, typeFont, paint);
            }
        }
    }
}
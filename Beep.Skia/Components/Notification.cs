using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A notification component for displaying messages.
    /// </summary>
    public class Notification : MaterialControl
    {
        private string _text = "";
        private NotificationType _type = NotificationType.Information;
        private NotificationPosition _position = NotificationPosition.TopRight;
        private NotificationStyle _style = NotificationStyle.Standard;
        private bool _autoClose = true;
        private int _duration = 3000; // milliseconds

        /// <summary>
        /// Gets or sets the notification text.
        /// </summary>
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value ?? "";
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the notification type.
        /// </summary>
        public NotificationType Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the notification position.
        /// </summary>
        public NotificationPosition Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the notification style.
        /// </summary>
        public NotificationStyle Style
        {
            get => _style;
            set
            {
                if (_style != value)
                {
                    _style = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the notification should auto-close.
        /// </summary>
        public bool AutoClose
        {
            get => _autoClose;
            set => _autoClose = value;
        }

        /// <summary>
        /// Gets or sets the duration in milliseconds before auto-closing.
        /// </summary>
        public int Duration
        {
            get => _duration;
            set => _duration = value;
        }

        /// <summary>
        /// Initializes a new instance of the Notification class.
        /// </summary>
        public Notification()
        {
            Width = 300;
            Height = 60;
        }

        /// <summary>
        /// Draws the notification content.
        /// </summary>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Draw background with type-specific color
            SKColor backgroundColor = GetBackgroundColor();
            using (var paint = new SKPaint())
            {
                paint.Color = backgroundColor;
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawRoundRect(new SKRect(X, Y, X + Width, Y + Height), 8, 8, paint);

                // Draw border
                paint.Color = GetBorderColor();
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = 1;
                canvas.DrawRoundRect(new SKRect(X, Y, X + Width, Y + Height), 8, 8, paint);
            }

            // Draw icon based on type
            DrawTypeIcon(canvas);

            // Draw text
            if (!string.IsNullOrEmpty(_text))
            {
                using (var paint = new SKPaint())
                {
                    paint.Color = MaterialColors.OnSurface;
                    paint.Style = SKPaintStyle.Fill;

                    using (var font = new SKFont())
                    {
                        font.Size = 14;
                        canvas.DrawText(_text, 50, 25, SKTextAlign.Left, font, paint);
                    }
                }
            }
        }

        private SKColor GetBackgroundColor()
        {
            switch (_type)
            {
                case NotificationType.Success:
                    return new SKColor(0xE8, 0xF5, 0xE8); // Light green
                case NotificationType.Warning:
                    return new SKColor(0xFF, 0xF8, 0xE1); // Light yellow
                case NotificationType.Error:
                    return new SKColor(0xFF, 0xEB, 0xEE); // Light red
                default:
                    return new SKColor(0xE3, 0xF2, 0xFD); // Light blue
            }
        }

        private SKColor GetBorderColor()
        {
            switch (_type)
            {
                case NotificationType.Success:
                    return new SKColor(0x4C, 0xAF, 0x50); // Green
                case NotificationType.Warning:
                    return new SKColor(0xFF, 0x98, 0x00); // Orange
                case NotificationType.Error:
                    return new SKColor(0xF4, 0x43, 0x36); // Red
                default:
                    return new SKColor(0x21, 0x96, 0xF3); // Blue
            }
        }

        private void DrawTypeIcon(SKCanvas canvas)
        {
            using (var paint = new SKPaint())
            {
                paint.Color = GetBorderColor();
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = 2;

                float centerX = 20;
                float centerY = 20;

                switch (_type)
                {
                    case NotificationType.Success:
                        // Check mark
                        canvas.DrawLine(centerX - 8, centerY, centerX - 2, centerY + 6, paint);
                        canvas.DrawLine(centerX - 2, centerY + 6, centerX + 8, centerY - 4, paint);
                        break;
                    case NotificationType.Warning:
                        // Exclamation mark
                        canvas.DrawLine(centerX, centerY - 8, centerX, centerY + 4, paint);
                        canvas.DrawCircle(centerX, centerY + 6, 1, paint);
                        break;
                    case NotificationType.Error:
                        // X mark
                        canvas.DrawLine(centerX - 6, centerY - 6, centerX + 6, centerY + 6, paint);
                        canvas.DrawLine(centerX + 6, centerY - 6, centerX - 6, centerY + 6, paint);
                        break;
                    default:
                        // Info icon
                        canvas.DrawCircle(centerX, centerY - 4, 1, paint);
                        canvas.DrawLine(centerX, centerY, centerX, centerY + 6, paint);
                        break;
                }
            }
        }

        /// <summary>
        /// Shows the notification.
        /// </summary>
        public void Show()
        {
            IsVisible = true;
            InvalidateVisual();

            if (_autoClose)
            {
                // In a real implementation, you'd use a timer here
                // For now, we'll just set a flag
            }
        }

        /// <summary>
        /// Hides the notification.
        /// </summary>
        public void Hide()
        {
            IsVisible = false;
            InvalidateVisual();
        }
    }
}

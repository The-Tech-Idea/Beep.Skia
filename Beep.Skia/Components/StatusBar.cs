using System;
using SkiaSharp;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 status bar component
    /// </summary>
    public class StatusBar : MaterialControl
    {
        private StatusBarItemCollection _items;
        private SKColor _backgroundColor = MaterialDesignColors.SurfaceVariant;
        private SKColor _borderColor = MaterialDesignColors.OutlineVariant;
        private float _borderWidth = 1;
        private float _itemSpacing = 8;
        private bool _showBorder = true;

        /// <summary>
        /// Gets the collection of status bar items
        /// </summary>
        public StatusBarItemCollection Items
        {
            get
            {
                if (_items == null)
                {
                    _items = new StatusBarItemCollection(this);
                }
                return _items;
            }
        }

        /// <summary>
        /// Gets or sets the background color
        /// </summary>
        public SKColor BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the border color
        /// </summary>
        public SKColor BorderColor
        {
            get => _borderColor;
            set
            {
                if (_borderColor != value)
                {
                    _borderColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the border width
        /// </summary>
        public float BorderWidth
        {
            get => _borderWidth;
            set
            {
                if (_borderWidth != value)
                {
                    _borderWidth = Math.Max(0, value);
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the spacing between items
        /// </summary>
        public float ItemSpacing
        {
            get => _itemSpacing;
            set
            {
                if (_itemSpacing != value)
                {
                    _itemSpacing = Math.Max(0, value);
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show the border
        /// </summary>
        public bool ShowBorder
        {
            get => _showBorder;
            set
            {
                if (_showBorder != value)
                {
                    _showBorder = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the StatusBar class
        /// </summary>
        public StatusBar()
        {
            Height = 24;
            Width = 400;
        }

        /// <summary>
        /// Draws the status bar content
        /// </summary>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds))
                return;

            // Draw background
            using (var backgroundPaint = new SKPaint { Color = _backgroundColor })
            {
                canvas.DrawRect(X, Y, Width, Height, backgroundPaint);
            }

            // Draw border
            if (_showBorder && _borderWidth > 0)
            {
                using (var borderPaint = new SKPaint
                {
                    Color = _borderColor,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = _borderWidth,
                    IsAntialias = true
                })
                {
                    canvas.DrawLine(X, Y, X + Width, Y, borderPaint);
                }
            }

            // Draw items
            DrawItems(canvas, context);
        }

        private void DrawItems(SKCanvas canvas, DrawingContext context)
        {
            float currentX = X + _itemSpacing;

            foreach (var item in Items)
            {
                if (!item.IsVisible)
                    continue;

                // Check if item fits in remaining space
                if (currentX + item.Width > X + Width - _itemSpacing)
                    break;

                // Draw item background if not transparent
                if (item.BackgroundColor.Alpha > 0)
                {
                    using (var itemBackgroundPaint = new SKPaint { Color = item.BackgroundColor })
                    {
                        canvas.DrawRect(currentX, Y, item.Width, Height, itemBackgroundPaint);
                    }
                }

                // Draw item text
                if (!string.IsNullOrEmpty(item.Text))
                {
                    using (var font = new SKFont())
                    {
                        font.Size = 12;
                        font.Typeface = SKTypeface.Default;

                        using (var textPaint = new SKPaint(font)
                        {
                            Color = item.TextColor,
                            IsAntialias = true
                        })
                        {
                            var textBounds = new SKRect();
                            textPaint.MeasureText(item.Text, ref textBounds);

                            float textX = GetTextX(item, currentX, textBounds.Width);
                            float textY = Y + Height / 2 + textBounds.Height / 2;

                            canvas.DrawText(item.Text, textX, textY, textPaint);
                        }
                    }
                }

                currentX += item.Width + _itemSpacing;
            }
        }

        private float GetTextX(StatusBarItem item, float itemX, float textWidth)
        {
            switch (item.TextAlignment)
            {
                case TextAlignment.Center:
                    return itemX + (item.Width - textWidth) / 2;
                case TextAlignment.Right:
                    return itemX + item.Width - textWidth - 4;
                case TextAlignment.Left:
                default:
                    return itemX + 4;
            }
        }

        /// <summary>
        /// Adds a status bar item with the specified text
        /// </summary>
        public StatusBarItem AddItem(string text)
        {
            var item = new StatusBarItem(text);
            Items.Add(item);
            return item;
        }

        /// <summary>
        /// Adds a status bar item with the specified text and width
        /// </summary>
        public StatusBarItem AddItem(string text, float width)
        {
            var item = new StatusBarItem(text) { Width = width };
            Items.Add(item);
            return item;
        }

        /// <summary>
        /// Removes a status bar item
        /// </summary>
        public void RemoveItem(StatusBarItem item)
        {
            if (item != null)
            {
                Items.Remove(item);
            }
        }

        /// <summary>
        /// Clears all items from the status bar
        /// </summary>
        public void ClearItems()
        {
            Items.Clear();
        }
    }
}

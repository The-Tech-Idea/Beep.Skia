using System;
using SkiaSharp;
using Beep.Skia.Model;
namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 tool bar component
    /// </summary>
    public class ToolBar : MaterialControl
    {
        private ToolBarItemCollection _items;
        private SKColor _backgroundColor = MaterialDesignColors.Surface;
        private SKColor _borderColor = MaterialDesignColors.OutlineVariant;
        private float _borderWidth = 1;
        private float _itemSpacing = 4;
        private bool _showBorder = true;
        private ToolBarItem _hoveredItem;
        private ToolBarItem _pressedItem;

        /// <summary>
        /// Gets the collection of tool bar items
        /// </summary>
        public ToolBarItemCollection Items
        {
            get
            {
                if (_items == null)
                {
                    _items = new ToolBarItemCollection(this);
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
        /// Initializes a new instance of the ToolBar class
        /// </summary>
        public ToolBar()
        {
            Height = 40;
            Width = 400;
        }

        /// <summary>
        /// Draws the tool bar content
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
                    canvas.DrawLine(X, Y + Height - _borderWidth / 2, X + Width, Y + Height - _borderWidth / 2, borderPaint);
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

                // Draw item background
                var itemBackgroundColor = item.BackgroundColor;
                if (item.IsPressed && item.IsEnabled)
                {
                    // Apply pressed state
                    itemBackgroundColor = ApplyStateLayer(item.BackgroundColor, StateLayerOpacity.Press);
                }
                else if (item.IsHovered && item.IsEnabled)
                {
                    // Apply hover state
                    itemBackgroundColor = item.HoverBackgroundColor;
                }

                if (itemBackgroundColor.Alpha > 0)
                {
                    using (var itemBackgroundPaint = new SKPaint { Color = itemBackgroundColor })
                    {
                        canvas.DrawRect(currentX, Y, item.Width, Height, itemBackgroundPaint);
                    }
                }

                // Draw item text (modern SKFont baseline centering)
                if (!string.IsNullOrEmpty(item.Text))
                {
                    using (var font = new SKFont(SKTypeface.Default, 14))
                    {
                        var textColor = item.IsEnabled ? item.TextColor : MaterialDesignColors.OnSurfaceVariant;
                        using (var textPaint = new SKPaint
                        {
                            Color = textColor,
                            IsAntialias = true
                        })
                        {
                            // Measure width for horizontal centering
                            float textWidth = font.MeasureText(item.Text);
                            float textX = currentX + (item.Width - textWidth) / 2f; // centered width

                            // Vertical centering using cap height for a visually balanced baseline
                            var metrics = font.Metrics; // ascent negative, descent positive
                            float capHeight = metrics.CapHeight; // positive
                            float baseline = Y + (Height + capHeight) / 2f; // center cap height visually

                            canvas.DrawText(item.Text, textX, baseline, SKTextAlign.Left, font, textPaint);
                        }
                    }
                }

                currentX += item.Width + _itemSpacing;
            }
        }

        private SKColor ApplyStateLayer(SKColor baseColor, float opacity)
        {
            var stateColor = MaterialDesignColors.Primary;
            return baseColor.WithAlpha((byte)(baseColor.Alpha * (1 - opacity) + (stateColor.Alpha * opacity)));
        }

        /// <summary>
        /// Handles mouse down events
        /// </summary>
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            base.OnMouseDown(point, context);

            var item = GetItemAtPoint(point);
            if (item != null && item.IsEnabled)
            {
                _pressedItem = item;
                item.IsPressed = true;
            }
            return true;
        }

        /// <summary>
        /// Handles mouse up events
        /// </summary>
        protected override bool OnMouseUp(SKPoint point, InteractionContext context)
        {
            base.OnMouseUp(point, context);

            if (_pressedItem != null)
            {
                var item = GetItemAtPoint(point);
                if (item == _pressedItem)
                {
                    _pressedItem.PerformClick();
                }

                _pressedItem.IsPressed = false;
                _pressedItem = null;
            }
            return true;
        }

        /// <summary>
        /// Handles mouse move events
        /// </summary>
        protected override bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            base.OnMouseMove(point, context);

            var item = GetItemAtPoint(point);

            // Update hover state
            if (_hoveredItem != item)
            {
                if (_hoveredItem != null)
                {
                    _hoveredItem.IsHovered = false;
                }

                _hoveredItem = item;

                if (_hoveredItem != null)
                {
                    _hoveredItem.IsHovered = true;
                }
            }
            return true;
        }

        private ToolBarItem GetItemAtPoint(SKPoint point)
        {
            if (!Bounds.Contains(point))
                return null;

            float currentX = X + _itemSpacing;

            foreach (var item in Items)
            {
                if (!item.IsVisible)
                    continue;

                var itemRect = new SKRect(currentX, Y, currentX + item.Width, Y + Height);
                if (itemRect.Contains(point))
                {
                    return item;
                }

                currentX += item.Width + _itemSpacing;
            }

            return null;
        }

        /// <summary>
        /// Adds a tool bar item with the specified text
        /// </summary>
        public ToolBarItem AddItem(string text)
        {
            var item = new ToolBarItem(text);
            Items.Add(item);
            return item;
        }

        /// <summary>
        /// Adds a tool bar item with the specified text and width
        /// </summary>
        public ToolBarItem AddItem(string text, float width)
        {
            var item = new ToolBarItem(text) { Width = width };
            Items.Add(item);
            return item;
        }

        /// <summary>
        /// Removes a tool bar item
        /// </summary>
        public void RemoveItem(ToolBarItem item)
        {
            if (item != null)
            {
                if (item == _hoveredItem)
                    _hoveredItem = null;
                if (item == _pressedItem)
                    _pressedItem = null;

                Items.Remove(item);
            }
        }

        /// <summary>
        /// Clears all items from the tool bar
        /// </summary>
        public void ClearItems()
        {
            _hoveredItem = null;
            _pressedItem = null;
            Items.Clear();
        }
    }
}

using SkiaSharp;
using System;
using System.Collections.Generic;
using Beep.Skia.Model;
namespace Beep.Skia.Components
{
    /// <summary>
    /// A collection of check box items.
    /// </summary>
    public class CheckBoxGroupItemCollection : List<CheckBoxGroupItem>
    {
    }

    /// <summary>
    /// Represents an item in a check box group.
    /// </summary>
    public class CheckBoxGroupItem
    {
        private string _text = "";
        private bool _checked = false;
        private object _tag;

        /// <summary>
        /// Gets or sets the text of the item.
        /// </summary>
        public string Text
        {
            get => _text;
            set => _text = value ?? "";
        }

        /// <summary>
        /// Gets or sets whether the item is checked.
        /// </summary>
        public bool Checked
        {
            get => _checked;
            set => _checked = value;
        }

        /// <summary>
        /// Gets or sets the tag associated with the item.
        /// </summary>
        public object Tag
        {
            get => _tag;
            set => _tag = value;
        }

        /// <summary>
        /// Initializes a new instance of the CheckBoxGroupItem class.
        /// </summary>
        public CheckBoxGroupItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the CheckBoxGroupItem class with the specified text.
        /// </summary>
        public CheckBoxGroupItem(string text)
        {
            _text = text ?? "";
        }

        /// <summary>
        /// Initializes a new instance of the CheckBoxGroupItem class with the specified text and checked state.
        /// </summary>
        public CheckBoxGroupItem(string text, bool isChecked)
        {
            _text = text ?? "";
            _checked = isChecked;
        }
    }

    /// <summary>
    /// A group of check box controls.
    /// </summary>
    public class CheckBoxGroup : MaterialControl
    {
        private CheckBoxGroupItemCollection _items = new CheckBoxGroupItemCollection();
        private CheckBoxGroupStyle _style = CheckBoxGroupStyle.Standard;
        private Orientation _orientation = Orientation.Vertical;
        private BorderStyle _borderStyle = BorderStyle.Single;
        private int _itemHeight = 24;
        private int _spacing = 4;

        /// <summary>
        /// Gets the collection of items in the check box group.
        /// </summary>
        public CheckBoxGroupItemCollection Items => _items;

        /// <summary>
        /// Gets or sets the style of the check box group.
        /// </summary>
        public CheckBoxGroupStyle Style
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
        /// Gets or sets the orientation of the check box group.
        /// </summary>
        public Orientation Orientation
        {
            get => _orientation;
            set
            {
                if (_orientation != value)
                {
                    _orientation = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the border style of the check box group.
        /// </summary>
        public BorderStyle BorderStyle
        {
            get => _borderStyle;
            set
            {
                if (_borderStyle != value)
                {
                    _borderStyle = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the height of each item.
        /// </summary>
        public int ItemHeight
        {
            get => _itemHeight;
            set
            {
                if (_itemHeight != value)
                {
                    _itemHeight = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the spacing between items.
        /// </summary>
        public int Spacing
        {
            get => _spacing;
            set
            {
                if (_spacing != value)
                {
                    _spacing = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the CheckBoxGroup class.
        /// </summary>
        public CheckBoxGroup()
        {
            Width = 200;
            Height = 100;
        }

        /// <summary>
        /// Draws the check box group content.
        /// </summary>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Draw background
            using (var paint = new SKPaint())
            {
                paint.Color = MaterialColors.Surface;
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawRect(new SKRect(X, Y, X + Width, Y + Height), paint);

                // Draw border if needed
                if (_borderStyle != BorderStyle.None)
                {
                    paint.Color = MaterialColors.OnSurfaceVariant;
                    paint.Style = SKPaintStyle.Stroke;
                    paint.StrokeWidth = 1;
                    canvas.DrawRect(new SKRect(X, Y, X + Width, Y + Height), paint);
                }
            }

            // Draw items
            float currentX = 8;
            float currentY = 8;

            foreach (var item in _items)
            {
                DrawCheckBoxItem(canvas, item, currentX, currentY);

                if (_orientation == Orientation.Vertical)
                {
                    currentY += _itemHeight + _spacing;
                    if (currentY + _itemHeight > Height) break;
                }
                else
                {
                    currentX += 100 + _spacing; // Approximate item width
                    if (currentX + 100 > Width)
                    {
                        currentX = 8;
                        currentY += _itemHeight + _spacing;
                        if (currentY + _itemHeight > Height) break;
                    }
                }
            }
        }

        private void DrawCheckBoxItem(SKCanvas canvas, CheckBoxGroupItem item, float x, float y)
        {
            // Draw check box
            using (var paint = new SKPaint())
            {
                // Draw box
                paint.Color = MaterialColors.SurfaceVariant;
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawRect(x, y, 16, 16, paint);

                paint.Color = MaterialColors.OnSurfaceVariant;
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = 1;
                canvas.DrawRect(x, y, 16, 16, paint);

                // Draw check mark if checked
                if (item.Checked)
                {
                    paint.Color = MaterialColors.Primary;
                    paint.Style = SKPaintStyle.Stroke;
                    paint.StrokeWidth = 2;

                    // Simple check mark
                    canvas.DrawLine(x + 3, y + 8, x + 7, y + 12, paint);
                    canvas.DrawLine(x + 7, y + 12, x + 13, y + 6, paint);
                }

                // Draw text
                paint.Color = MaterialColors.OnSurface;
                paint.Style = SKPaintStyle.Fill;

                using (var font = new SKFont())
                {
                    font.Size = 14;
                    canvas.DrawText(item.Text, x + 24, y + 14, SKTextAlign.Left, font, paint);
                }
            }
        }

        /// <summary>
        /// Handles mouse down events.
        /// </summary>
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            // Check if click is on a check box
            float currentX = 8;
            float currentY = 8;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                SKRect itemRect = new SKRect(currentX, currentY, currentX + 16, currentY + 16);

                if (itemRect.Contains(point.X, point.Y))
                {
                    item.Checked = !item.Checked;
                    InvalidateVisual();
                    return true; // Event handled
                }

                if (_orientation == Orientation.Vertical)
                {
                    currentY += _itemHeight + _spacing;
                }
                else
                {
                    currentX += 100 + _spacing;
                    if (currentX + 100 > Width)
                    {
                        currentX = 8;
                        currentY += _itemHeight + _spacing;
                    }
                }
            }

            return base.OnMouseDown(point, context);
        }
    }
}

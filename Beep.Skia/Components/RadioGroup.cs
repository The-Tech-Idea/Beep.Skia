using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A collection of radio button items.
    /// </summary>
    public class RadioGroupItemCollection : List<RadioGroupItem>
    {
    }

    /// <summary>
    /// Represents an item in a radio group.
    /// </summary>
    public class RadioGroupItem
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
        /// Initializes a new instance of the RadioGroupItem class.
        /// </summary>
        public RadioGroupItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the RadioGroupItem class with the specified text.
        /// </summary>
        public RadioGroupItem(string text)
        {
            _text = text ?? "";
        }

        /// <summary>
        /// Initializes a new instance of the RadioGroupItem class with the specified text and checked state.
        /// </summary>
        public RadioGroupItem(string text, bool isChecked)
        {
            _text = text ?? "";
            _checked = isChecked;
        }
    }

    /// <summary>
    /// A group of radio button controls where only one can be selected at a time.
    /// </summary>
    public class RadioGroup : MaterialControl
    {
        private RadioGroupItemCollection _items = new RadioGroupItemCollection();
        private RadioGroupStyle _style = RadioGroupStyle.Standard;
        private Orientation _orientation = Orientation.Vertical;
        private BorderStyle _borderStyle = BorderStyle.Single;
        private int _itemHeight = 24;
        private int _spacing = 4;

        /// <summary>
        /// Gets the collection of items in the radio group.
        /// </summary>
        public RadioGroupItemCollection Items => _items;

        /// <summary>
        /// Gets or sets the style of the radio group.
        /// </summary>
        public RadioGroupStyle Style
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
        /// Gets or sets the orientation of the radio group.
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
        /// Gets or sets the border style of the radio group.
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
        /// Gets or sets the selected item.
        /// </summary>
        public RadioGroupItem SelectedItem
        {
            get
            {
                foreach (var item in _items)
                {
                    if (item.Checked) return item;
                }
                return null;
            }
            set
            {
                foreach (var item in _items)
                {
                    item.Checked = (item == value);
                }
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Initializes a new instance of the RadioGroup class.
        /// </summary>
        public RadioGroup()
        {
            Width = 200;
            Height = 100;
        }

        /// <summary>
        /// Draws the radio group content.
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
                DrawRadioButtonItem(canvas, item, currentX, currentY);

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

        private void DrawRadioButtonItem(SKCanvas canvas, RadioGroupItem item, float x, float y)
        {
            // Draw radio button
            using (var paint = new SKPaint())
            {
                // Draw outer circle
                paint.Color = MaterialColors.OnSurfaceVariant;
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = 2;
                canvas.DrawCircle(x + 8, y + 8, 8, paint);

                // Draw inner circle if checked
                if (item.Checked)
                {
                    paint.Color = MaterialColors.Primary;
                    paint.Style = SKPaintStyle.Fill;
                    canvas.DrawCircle(x + 8, y + 8, 4, paint);
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
            // Check if click is on a radio button
            float currentX = 8;
            float currentY = 8;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                SKRect itemRect = new SKRect(currentX, currentY, currentX + 16, currentY + 16);

                if (itemRect.Contains(point.X, point.Y))
                {
                    // Uncheck all items first
                    foreach (var otherItem in _items)
                    {
                        otherItem.Checked = false;
                    }

                    // Check the clicked item
                    item.Checked = true;
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

using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia.Model;
namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 dropdown/combobox component.
    /// </summary>
    public class Dropdown : MaterialControl
    {
        private List<DropdownItem> _items = new List<DropdownItem>();
        private DropdownItem _selectedItem;
        private string _label = string.Empty;
        private string _placeholder = "Select an option";
        private string _errorMessage = string.Empty;
        private DropdownVariant _variant = DropdownVariant.Filled;
        private ItemDisplayStyle _itemDisplayStyle = ItemDisplayStyle.TextOnly;
        private bool _isExpanded = false;
        private bool _isEnabled = true;
        private float _cornerRadius = 8.0f;
        private SKColor? _textColor;
        private SKColor? _labelColor;
        private SKColor? _borderColor;
        private SKColor? _backgroundColor;
        private float _fontSize = 14.0f;
        private float _dropdownHeight = 200.0f;
        private int _maxVisibleItems = 5;

        /// <summary>
        /// Represents an item in the dropdown list.
        /// </summary>
        public class DropdownItem
        {
            public string Text { get; set; }
            public string Value { get; set; }
            public object Tag { get; set; }
            public string IconPath { get; set; }
            public SKColor? IconColor { get; set; }

            public DropdownItem(string text, string value = null, object tag = null, string iconPath = null, SKColor? iconColor = null)
            {
                Text = text;
                Value = value ?? text;
                Tag = tag;
                IconPath = iconPath;
                IconColor = iconColor;
            }

            public override string ToString() => Text;
        }

        /// <summary>
        /// Material Design 3.0 dropdown variants.
        /// </summary>
        public enum DropdownVariant
        {
            Filled,
            Outlined
        }

        /// <summary>
        /// Dropdown item display styles.
        /// </summary>
        public enum ItemDisplayStyle
        {
            TextOnly,
            IconAndText,
            RadioAndText
        }

        /// <summary>
        /// Occurs when the selected item changes.
        /// </summary>
        public event EventHandler<DropdownItem> SelectedItemChanged;

        /// <summary>
        /// Occurs when the dropdown is opened.
        /// </summary>
        public event EventHandler DropdownOpened;

        /// <summary>
        /// Occurs when the dropdown is closed.
        /// </summary>
        public event EventHandler DropdownClosed;

        /// <summary>
        /// Gets or sets the list of items in the dropdown.
        /// </summary>
        public List<DropdownItem> Items
        {
            get => _items;
            set
            {
                _items = value ?? new List<DropdownItem>();
                // If current selection is no longer valid, clear it
                if (_selectedItem != null && !_items.Contains(_selectedItem))
                {
                    _selectedItem = null;
                }
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the currently selected item.
        /// </summary>
        public DropdownItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    SelectedItemChanged?.Invoke(this, _selectedItem);
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected value (convenience property).
        /// </summary>
        public string SelectedValue
        {
            get => _selectedItem?.Value;
            set
            {
                if (value != null)
                {
                    var item = _items.FirstOrDefault(i => i.Value == value);
                    if (item != null)
                    {
                        SelectedItem = item;
                    }
                }
                else
                {
                    SelectedItem = null;
                }
            }
        }

        /// <summary>
        /// Gets or sets the label text displayed above the dropdown.
        /// </summary>
        public string Label
        {
            get => _label;
            set
            {
                if (_label != value)
                {
                    _label = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the placeholder text when no item is selected.
        /// </summary>
        public string Placeholder
        {
            get => _placeholder;
            set
            {
                if (_placeholder != value)
                {
                    _placeholder = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the error message to display.
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the dropdown variant.
        /// </summary>
        public DropdownVariant Variant
        {
            get => _variant;
            set
            {
                if (_variant != value)
                {
                    _variant = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the item display style.
        /// </summary>
        public ItemDisplayStyle DisplayStyle
        {
            get => _itemDisplayStyle;
            set
            {
                if (_itemDisplayStyle != value)
                {
                    _itemDisplayStyle = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the dropdown is enabled.
        /// </summary>
        public new bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    State = value ? ControlState.Normal : ControlState.Disabled;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the corner radius.
        /// </summary>
        public float CornerRadius
        {
            get => _cornerRadius;
            set
            {
                if (_cornerRadius != value)
                {
                    _cornerRadius = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the text color.
        /// </summary>
        public SKColor TextColor
        {
            get => _textColor ?? MaterialColors.OnSurface;
            set
            {
                if (_textColor != value)
                {
                    _textColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the label color.
        /// </summary>
        public SKColor LabelColor
        {
            get => _labelColor ?? MaterialColors.OnSurfaceVariant;
            set
            {
                if (_labelColor != value)
                {
                    _labelColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the border color.
        /// </summary>
        public SKColor BorderColor
        {
            get => _borderColor ?? MaterialColors.Outline;
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
        /// Gets or sets the background color.
        /// </summary>
        public SKColor BackgroundColor
        {
            get => _backgroundColor ?? GetBackgroundColorForVariant();
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
        /// Gets or sets the font size.
        /// </summary>
        public float FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum height of the dropdown list.
        /// </summary>
        public float DropdownHeight
        {
            get => _dropdownHeight;
            set
            {
                if (_dropdownHeight != value)
                {
                    _dropdownHeight = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of visible items in the dropdown.
        /// </summary>
        public int MaxVisibleItems
        {
            get => _maxVisibleItems;
            set
            {
                if (_maxVisibleItems != value)
                {
                    _maxVisibleItems = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets whether the dropdown is currently expanded.
        /// </summary>
        public bool IsExpanded => _isExpanded;

        /// <summary>
        /// Initializes a new instance of the Dropdown class.
        /// </summary>
        public Dropdown()
        {
            // Set default size
            Width = 200;
            Height = 56;

            // Handle click events
            Clicked += OnDropdownClicked;
        }

        /// <summary>
        /// Adds an item to the dropdown.
        /// </summary>
        public void AddItem(string text, string value = null, object tag = null, string iconPath = null, SKColor? iconColor = null)
        {
            _items.Add(new DropdownItem(text, value, tag, iconPath, iconColor));
            InvalidateVisual();
        }

        /// <summary>
        /// Removes an item from the dropdown.
        /// </summary>
        public void RemoveItem(DropdownItem item)
        {
            if (_items.Remove(item))
            {
                if (_selectedItem == item)
                {
                    _selectedItem = null;
                }
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Clears all items from the dropdown.
        /// </summary>
        public void ClearItems()
        {
            _items.Clear();
            _selectedItem = null;
            InvalidateVisual();
        }

        /// <summary>
        /// Toggles the dropdown expansion state.
        /// </summary>
        public void ToggleDropdown()
        {
            if (_isEnabled)
            {
                _isExpanded = !_isExpanded;
                if (_isExpanded)
                {
                    DropdownOpened?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    DropdownClosed?.Invoke(this, EventArgs.Empty);
                }
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Opens the dropdown.
        /// </summary>
        public void OpenDropdown()
        {
            if (_isEnabled && !_isExpanded)
            {
                _isExpanded = true;
                DropdownOpened?.Invoke(this, EventArgs.Empty);
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Closes the dropdown.
        /// </summary>
        public void CloseDropdown()
        {
            if (_isExpanded)
            {
                _isExpanded = false;
                DropdownClosed?.Invoke(this, EventArgs.Empty);
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Handles dropdown click events.
        /// </summary>
        private void OnDropdownClicked(object sender, EventArgs e)
        {
            ToggleDropdown();
        }

        /// <summary>
        /// Gets the background color based on variant.
        /// </summary>
        private SKColor GetBackgroundColorForVariant()
        {
            if (_variant == DropdownVariant.Filled)
            {
                return MaterialColors.Surface;
            }
            return SKColors.Transparent;
        }

        /// <summary>
        /// Draws the dropdown component.
        /// </summary>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            var bounds = new SKRect(X, Y, X + Width, Y + Height);

            // Draw the main dropdown field
            DrawDropdownField(canvas, bounds);

            // Draw selected item content (icon/text)
            DrawSelectedItemContent(canvas, bounds);

            // Draw the dropdown arrow
            DrawDropdownArrow(canvas, bounds);

            // Draw label if present
            if (!string.IsNullOrEmpty(_label))
            {
                DrawLabel(canvas, bounds);
            }

            // Draw error message if present
            if (!string.IsNullOrEmpty(_errorMessage))
            {
                DrawErrorMessage(canvas, bounds);
            }

            // Draw dropdown list if expanded
            if (_isExpanded)
            {
                DrawDropdownList(canvas, bounds);
            }
        }

        /// <summary>
        /// Draws the main dropdown field.
        /// </summary>
        private void DrawDropdownField(SKCanvas canvas, SKRect bounds)
        {
            using (var paint = new SKPaint())
            {
                // Draw background
                paint.Color = BackgroundColor;
                paint.IsAntialias = true;

                if (_variant == DropdownVariant.Filled)
                {
                    canvas.DrawRoundRect(bounds, _cornerRadius, _cornerRadius, paint);
                }
                else // Outlined
                {
                    // Draw border
                    paint.Color = BorderColor;
                    paint.Style = SKPaintStyle.Stroke;
                    paint.StrokeWidth = 1;
                    canvas.DrawRoundRect(bounds, _cornerRadius, _cornerRadius, paint);

                    // Fill background
                    paint.Color = BackgroundColor;
                    paint.Style = SKPaintStyle.Fill;
                    canvas.DrawRoundRect(bounds, _cornerRadius, _cornerRadius, paint);
                }

                // Draw state layer
                DrawStateLayer(canvas, bounds, MaterialColors.OnSurface);
            }
        }

        /// <summary>
        /// Draws the dropdown arrow icon.
        /// </summary>
        private void DrawDropdownArrow(SKCanvas canvas, SKRect bounds)
        {
            float arrowSize = 16;
            float arrowX = bounds.Right - arrowSize - 12;
            float arrowY = bounds.Top + (bounds.Height - arrowSize) / 2;

            using (var paint = new SKPaint())
            {
                paint.Color = _isEnabled ? MaterialColors.OnSurfaceVariant : MaterialColors.OnSurface;
                paint.IsAntialias = true;
                paint.Style = SKPaintStyle.Fill;

                var path = new SKPath();
                if (_isExpanded)
                {
                    // Up arrow
                    path.MoveTo(arrowX, arrowY + arrowSize);
                    path.LineTo(arrowX + arrowSize / 2, arrowY);
                    path.LineTo(arrowX + arrowSize, arrowY + arrowSize);
                }
                else
                {
                    // Down arrow
                    path.MoveTo(arrowX, arrowY);
                    path.LineTo(arrowX + arrowSize / 2, arrowY + arrowSize);
                    path.LineTo(arrowX + arrowSize, arrowY);
                }
                path.Close();
                canvas.DrawPath(path, paint);
            }
        }

        /// <summary>
        /// Draws the label text.
        /// </summary>
        private void DrawLabel(SKCanvas canvas, SKRect bounds)
        {
            if (string.IsNullOrEmpty(_label))
                return;
            float labelX = bounds.Left;
            using var font = new SKFont(SKTypeface.Default, _fontSize * 0.75f);
            using var paint = new SKPaint { Color = LabelColor, IsAntialias = true };
            float baseline = bounds.Top - 8; // keep layout
            canvas.DrawText(_label, labelX, baseline, SKTextAlign.Left, font, paint);
        }

        /// <summary>
        /// Draws the selected item content in the main dropdown field.
        /// </summary>
        private void DrawSelectedItemContent(SKCanvas canvas, SKRect bounds)
        {
            string displayText = _selectedItem?.Text ?? _placeholder;
            SKColor textColor = _selectedItem != null ? TextColor : MaterialColors.OnSurfaceVariant;

            float contentX = bounds.Left + 16;
            float textY = bounds.Top + (bounds.Height + _fontSize) / 2;

            // Draw selected item icon if present and using IconAndText style
            if (_itemDisplayStyle == ItemDisplayStyle.IconAndText && _selectedItem != null && !string.IsNullOrEmpty(_selectedItem.IconPath))
            {
                float iconSize = 20;
                float iconY = bounds.Top + (bounds.Height - iconSize) / 2;

                DrawSvgIcon(canvas, _selectedItem.IconPath, contentX, iconY, iconSize, iconSize, _selectedItem.IconColor ?? MaterialColors.OnSurfaceVariant);
                contentX += iconSize + 8; // Space for icon and padding
            }

            // Draw text
            using var font = new SKFont(SKTypeface.Default, _fontSize);
            using var paint = new SKPaint { Color = textColor, IsAntialias = true };
            // Replace previous baseline formula with cap-height centering
            float baseline = bounds.Top + (bounds.Height + font.Metrics.CapHeight) / 2f;
            canvas.DrawText(displayText, contentX, baseline, SKTextAlign.Left, font, paint);
        }

        /// <summary>
        /// Draws the error message.
        /// </summary>
        private void DrawErrorMessage(SKCanvas canvas, SKRect bounds)
        {
            if (string.IsNullOrEmpty(_errorMessage))
                return;
            using var font = new SKFont(SKTypeface.Default, _fontSize * 0.75f);
            using var paint = new SKPaint { Color = MaterialColors.Error, IsAntialias = true };
            float baseline = bounds.Bottom + 20;
            canvas.DrawText(_errorMessage, bounds.Left, baseline, SKTextAlign.Left, font, paint);
        }

        /// <summary>
        /// Draws the dropdown list when expanded.
        /// </summary>
        private void DrawDropdownList(SKCanvas canvas, SKRect bounds)
        {
            if (_items.Count == 0)
                return;

            float itemHeight = 48;
            float listHeight = Math.Min(_dropdownHeight, _items.Count * itemHeight);
            var listBounds = new SKRect(bounds.Left, bounds.Bottom + 4, bounds.Right, bounds.Bottom + 4 + listHeight);

            using (var paint = new SKPaint())
            {
                // Draw dropdown background
                paint.Color = MaterialColors.Surface;
                paint.IsAntialias = true;
                paint.Style = SKPaintStyle.Fill;

                // Add shadow effect
                paint.ImageFilter = SKImageFilter.CreateDropShadow(0, 2, 4, 4, new SKColor(0, 0, 0, 64));

                canvas.DrawRoundRect(listBounds, _cornerRadius, _cornerRadius, paint);

                // Draw border
                paint.Color = MaterialColors.Outline;
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = 1;
                paint.ImageFilter = null;
                canvas.DrawRoundRect(listBounds, _cornerRadius, _cornerRadius, paint);

                // Draw items
                paint.Style = SKPaintStyle.Fill;
                float currentY = listBounds.Top;

                foreach (var item in _items)
                {
                    var itemBounds = new SKRect(listBounds.Left, currentY, listBounds.Right, currentY + itemHeight);

                    // Highlight selected item
                    if (item == _selectedItem)
                    {
                        paint.Color = MaterialColors.SecondaryContainer;
                        canvas.DrawRect(itemBounds, paint);
                    }

                    // Draw selection indicator based on display style
                    float contentX = itemBounds.Left + 16;

                    if (_itemDisplayStyle == ItemDisplayStyle.RadioAndText)
                    {
                        DrawRadioButton(canvas, itemBounds, item == _selectedItem);
                        contentX += 32; // Space for radio button
                    }
                    else if (_itemDisplayStyle == ItemDisplayStyle.IconAndText && !string.IsNullOrEmpty(item.IconPath))
                    {
                        DrawItemIcon(canvas, itemBounds, item);
                        contentX += 40; // Space for icon
                    }

                    // Draw item text
                    using var itemFont = new SKFont(SKTypeface.Default, _fontSize);
                    using var itemPaint = new SKPaint { Color = MaterialColors.OnSurface, IsAntialias = true };
                    float baseline = currentY + (itemHeight + itemFont.Metrics.CapHeight) / 2f;
                    canvas.DrawText(item.Text, contentX, baseline, SKTextAlign.Left, itemFont, itemPaint);

                    currentY += itemHeight;
                }
            }
        }

        /// <summary>
        /// Draws a radio button for the item.
        /// </summary>
        private void DrawRadioButton(SKCanvas canvas, SKRect itemBounds, bool isSelected)
        {
            float radioSize = 16;
            float radioX = itemBounds.Left + 16;
            float radioY = itemBounds.Top + (itemBounds.Height - radioSize) / 2;

            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;

                // Draw outer circle
                paint.Color = isSelected ? MaterialColors.Primary : MaterialColors.OnSurfaceVariant;
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = 2;
                canvas.DrawCircle(radioX + radioSize / 2, radioY + radioSize / 2, radioSize / 2, paint);

                // Draw inner circle if selected
                if (isSelected)
                {
                    paint.Color = MaterialColors.Primary;
                    paint.Style = SKPaintStyle.Fill;
                    canvas.DrawCircle(radioX + radioSize / 2, radioY + radioSize / 2, radioSize / 4, paint);
                }
            }
        }

        /// <summary>
        /// Draws an icon for the item.
        /// </summary>
        private void DrawItemIcon(SKCanvas canvas, SKRect itemBounds, DropdownItem item)
        {
            if (string.IsNullOrEmpty(item.IconPath))
                return;

            float iconSize = 20;
            float iconX = itemBounds.Left + 16;
            float iconY = itemBounds.Top + (itemBounds.Height - iconSize) / 2;

            // Use the DrawSvgIcon method from MaterialControl
            DrawSvgIcon(canvas, item.IconPath, iconX, iconY, iconSize, iconSize, item.IconColor ?? MaterialColors.OnSurfaceVariant);
        }
    }
}

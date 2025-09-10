using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 Split Button component that combines a primary action with a dropdown menu of additional options.
    /// </summary>
    public class SplitButton : MaterialControl
    {
        private string _text = "Split Button";
        private string _selectedText = "Split Button";
        private SKColor _backgroundColor = MaterialColors.Primary;
        private SKColor _textColor = MaterialColors.OnPrimary;
        private SKColor _dropdownButtonColor = MaterialColors.PrimaryContainer;
        private SKColor _dropdownTextColor = MaterialColors.OnPrimaryContainer;
        private float _cornerRadius = 20; // Material Design 3.0 button corner radius
        private ButtonVariant _variant = ButtonVariant.Filled;
        private string _leadingIcon;
        private string _dropdownIcon = "▼"; // Default dropdown arrow
        private bool _isDropdownOpen = false;
        private readonly List<SplitButtonItem> _menuItems = new List<SplitButtonItem>();
        private SplitButtonItem _selectedItem;
        private float _separatorWidth = 1;
        private SKColor _separatorColor = MaterialColors.OutlineVariant;

        /// <summary>
        /// Material Design 3.0 button variants.
        /// </summary>
        public enum ButtonVariant
        {
            Filled,
            Outlined,
            Text,
            Elevated,
            Tonal
        }

        /// <summary>
        /// Represents a menu item in the split button dropdown.
        /// </summary>
        public class SplitButtonItem
        {
            private string _text;
            private string _icon;
            private bool _isEnabled = true;
            private object _tag;

            /// <summary>
            /// Gets or sets the text displayed for the menu item.
            /// </summary>
            public string Text
            {
                get => _text;
                set => _text = value;
            }

            /// <summary>
            /// Gets or sets the icon (SVG path or resource name) for the menu item.
            /// </summary>
            public string Icon
            {
                get => _icon;
                set => _icon = value;
            }

            /// <summary>
            /// Gets or sets whether this menu item is enabled.
            /// </summary>
            public bool IsEnabled
            {
                get => _isEnabled;
                set => _isEnabled = value;
            }

            /// <summary>
            /// Gets or sets custom data associated with this menu item.
            /// </summary>
            public object Tag
            {
                get => _tag;
                set => _tag = value;
            }

            /// <summary>
            /// Initializes a new instance of the SplitButtonItem class.
            /// </summary>
            /// <param name="text">The text to display for the menu item.</param>
            /// <param name="icon">The icon for the menu item (optional).</param>
            public SplitButtonItem(string text, string icon = null)
            {
                _text = text;
                _icon = icon;
            }
        }

        /// <summary>
        /// Gets or sets the text displayed on the primary button.
        /// </summary>
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets the text of the currently selected menu item.
        /// </summary>
        public string SelectedText => _selectedItem?.Text ?? _text;

        /// <summary>
        /// Gets or sets the background color of the primary button.
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
        /// Gets or sets the text color of the primary button.
        /// </summary>
        public SKColor TextColor
        {
            get => _textColor;
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
        /// Gets or sets the background color of the dropdown button.
        /// </summary>
        public SKColor DropdownButtonColor
        {
            get => _dropdownButtonColor;
            set
            {
                if (_dropdownButtonColor != value)
                {
                    _dropdownButtonColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the text color of the dropdown button.
        /// </summary>
        public SKColor DropdownTextColor
        {
            get => _dropdownTextColor;
            set
            {
                if (_dropdownTextColor != value)
                {
                    _dropdownTextColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the corner radius of the split button.
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
        /// Gets or sets the button variant (Material Design 3.0).
        /// </summary>
        public ButtonVariant Variant
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
        /// Gets or sets the leading icon (SVG path or resource name).
        /// </summary>
        public string LeadingIcon
        {
            get => _leadingIcon;
            set
            {
                if (_leadingIcon != value)
                {
                    _leadingIcon = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the dropdown icon.
        /// </summary>
        public string DropdownIcon
        {
            get => _dropdownIcon;
            set
            {
                if (_dropdownIcon != value)
                {
                    _dropdownIcon = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the dropdown menu is open.
        /// </summary>
        public bool IsDropdownOpen
        {
            get => _isDropdownOpen;
            set
            {
                if (_isDropdownOpen != value)
                {
                    _isDropdownOpen = value;
                    OnDropdownStateChanged();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the width of the separator between the primary and dropdown buttons.
        /// </summary>
        public float SeparatorWidth
        {
            get => _separatorWidth;
            set
            {
                if (_separatorWidth != value)
                {
                    _separatorWidth = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the color of the separator between the primary and dropdown buttons.
        /// </summary>
        public SKColor SeparatorColor
        {
            get => _separatorColor;
            set
            {
                if (_separatorColor != value)
                {
                    _separatorColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets the currently selected menu item.
        /// </summary>
        public SplitButtonItem SelectedItem => _selectedItem;

        /// <summary>
        /// Gets the collection of menu items.
        /// </summary>
        public IReadOnlyList<SplitButtonItem> MenuItems => _menuItems.AsReadOnly();

        /// <summary>
        /// Occurs when the primary button is clicked.
        /// </summary>
        public event EventHandler<EventArgs> PrimaryButtonClicked;

        /// <summary>
        /// Occurs when the dropdown button is clicked.
        /// </summary>
        public event EventHandler<EventArgs> DropdownButtonClicked;

        /// <summary>
        /// Occurs when a menu item is selected.
        /// </summary>
        public event EventHandler<SplitButtonItemSelectedEventArgs> ItemSelected;

        /// <summary>
        /// Occurs when the dropdown state changes.
        /// </summary>
        public event EventHandler<EventArgs> DropdownStateChanged;

        /// <summary>
        /// Initializes a new instance of the SplitButton class.
        /// </summary>
        public SplitButton()
        {
            // Set default size
            Width = 200;
            Height = 40;
        }

        /// <summary>
        /// Adds a menu item to the dropdown.
        /// </summary>
        /// <param name="text">The text for the menu item.</param>
        /// <param name="icon">The icon for the menu item (optional).</param>
        /// <returns>The created SplitButtonItem.</returns>
        public SplitButtonItem AddMenuItem(string text, string icon = null)
        {
            var item = new SplitButtonItem(text, icon);
            _menuItems.Add(item);
            return item;
        }

        /// <summary>
        /// Removes a menu item from the dropdown.
        /// </summary>
        /// <param name="item">The menu item to remove.</param>
        /// <returns>True if the item was removed successfully.</returns>
        public bool RemoveMenuItem(SplitButtonItem item)
        {
            if (_menuItems.Remove(item))
            {
                if (_selectedItem == item)
                {
                    _selectedItem = null;
                }
                InvalidateVisual();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clears all menu items from the dropdown.
        /// </summary>
        public void ClearMenuItems()
        {
            _menuItems.Clear();
            _selectedItem = null;
            InvalidateVisual();
        }

        /// <summary>
        /// Selects a menu item.
        /// </summary>
        /// <param name="item">The menu item to select.</param>
        public void SelectItem(SplitButtonItem item)
        {
            if (item != null && _menuItems.Contains(item) && item.IsEnabled)
            {
                _selectedItem = item;
                _text = item.Text;
                OnItemSelected(new SplitButtonItemSelectedEventArgs(item));
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Selects a menu item by index.
        /// </summary>
        /// <param name="index">The index of the menu item to select.</param>
        public void SelectItem(int index)
        {
            if (index >= 0 && index < _menuItems.Count)
            {
                SelectItem(_menuItems[index]);
            }
        }

        /// <summary>
        /// Draws the split button control.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            var bounds = new SKRect(X, Y, X + Width, Y + Height);

            // Calculate button dimensions
            float dropdownButtonWidth = 48; // Fixed width for dropdown button
            float primaryButtonWidth = Width - dropdownButtonWidth - _separatorWidth;

            // Draw primary button
            var primaryBounds = new SKRect(X, Y, X + primaryButtonWidth, Y + Height);
            DrawPrimaryButton(canvas, primaryBounds);

            // Draw separator
            DrawSeparator(canvas, X + primaryButtonWidth);

            // Draw dropdown button
            var dropdownBounds = new SKRect(X + primaryButtonWidth + _separatorWidth, Y, X + Width, Y + Height);
            DrawDropdownButton(canvas, dropdownBounds);

            // Draw dropdown menu if open
            if (_isDropdownOpen)
            {
                DrawDropdownMenu(canvas, bounds);
            }
        }

        /// <summary>
        /// Draws the primary button part.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The bounds of the primary button.</param>
        private void DrawPrimaryButton(SKCanvas canvas, SKRect bounds)
        {
            // Draw background
            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;

                switch (_variant)
                {
                    case ButtonVariant.Filled:
                        paint.Color = _backgroundColor;
                        break;
                    case ButtonVariant.Outlined:
                        paint.Color = SKColors.Transparent;
                        break;
                    case ButtonVariant.Text:
                        paint.Color = SKColors.Transparent;
                        break;
                    case ButtonVariant.Elevated:
                        paint.Color = _backgroundColor;
                        break;
                    case ButtonVariant.Tonal:
                        paint.Color = MaterialColors.SecondaryContainer;
                        break;
                }

                // Create path with rounded corners on the left side
                var path = new SKPath();
                path.MoveTo(bounds.Left + _cornerRadius, bounds.Top);
                path.LineTo(bounds.Right, bounds.Top);
                path.LineTo(bounds.Right, bounds.Bottom);
                path.LineTo(bounds.Left + _cornerRadius, bounds.Bottom);
                path.ArcTo(_cornerRadius, _cornerRadius, 0, SKPathArcSize.Small, SKPathDirection.CounterClockwise, bounds.Left + _cornerRadius, bounds.Top);
                path.Close();

                canvas.DrawPath(path, paint);

                // Draw outline for outlined variant
                if (_variant == ButtonVariant.Outlined)
                {
                    paint.Color = MaterialColors.Outline;
                    paint.StrokeWidth = 1;
                    paint.Style = SKPaintStyle.Stroke;
                    canvas.DrawPath(path, paint);
                    paint.Style = SKPaintStyle.Fill;
                }
            }

            // Draw text
            DrawButtonText(canvas, bounds, _text, _textColor);
        }

        /// <summary>
        /// Draws the dropdown button part.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The bounds of the dropdown button.</param>
        private void DrawDropdownButton(SKCanvas canvas, SKRect bounds)
        {
            // Draw background
            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;
                paint.Color = _dropdownButtonColor;

                // Create path with rounded corners on the right side
                var path = new SKPath();
                path.MoveTo(bounds.Left, bounds.Top);
                path.LineTo(bounds.Right - _cornerRadius, bounds.Top);
                path.ArcTo(_cornerRadius, _cornerRadius, 0, SKPathArcSize.Small, SKPathDirection.Clockwise, bounds.Right, bounds.Top + _cornerRadius);
                path.LineTo(bounds.Right, bounds.Bottom);
                path.LineTo(bounds.Left, bounds.Bottom);
                path.Close();

                canvas.DrawPath(path, paint);
            }

            // Draw dropdown icon
            DrawButtonText(canvas, bounds, _dropdownIcon, _dropdownTextColor);
        }

        /// <summary>
        /// Draws the separator between primary and dropdown buttons.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="x">The x-coordinate of the separator.</param>
        private void DrawSeparator(SKCanvas canvas, float x)
        {
            using (var paint = new SKPaint())
            {
                paint.Color = _separatorColor;
                paint.StrokeWidth = _separatorWidth;
                paint.IsAntialias = true;

                canvas.DrawLine(x + _separatorWidth / 2, 8, x + _separatorWidth / 2, Height - 8, paint);
            }
        }

        /// <summary>
        /// Draws text on a button.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The bounds of the button.</param>
        /// <param name="text">The text to draw.</param>
        /// <param name="color">The color of the text.</param>
        private void DrawButtonText(SKCanvas canvas, SKRect bounds, string text, SKColor color)
        {
            if (string.IsNullOrEmpty(text))
                return;

            using var font = new SKFont(SKTypeface.Default, 14);
            using var paint = new SKPaint { Color = color, IsAntialias = true };
            var metrics = font.Metrics;
            float baseline = bounds.MidY + metrics.CapHeight / 2f;
            canvas.DrawText(text, bounds.MidX, baseline, SKTextAlign.Center, font, paint);
        }

        /// <summary>
        /// Draws the dropdown menu.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="buttonBounds">The bounds of the button.</param>
        private void DrawDropdownMenu(SKCanvas canvas, SKRect buttonBounds)
        {
            if (_menuItems.Count == 0)
                return;

            float menuItemHeight = 36;
            float menuWidth = Width;
            float menuHeight = _menuItems.Count * menuItemHeight;
            float menuY = buttonBounds.Bottom + 4;

            // Draw menu background
            var menuBounds = new SKRect(buttonBounds.Left, menuY, buttonBounds.Right, menuY + menuHeight);
            using (var paint = new SKPaint())
            {
                paint.Color = MaterialColors.Surface;
                paint.IsAntialias = true;

                var menuPath = new SKPath();
                menuPath.AddRoundRect(menuBounds, 8, 8);
                canvas.DrawPath(menuPath, paint);

                // Draw menu outline
                paint.Color = MaterialColors.OutlineVariant;
                paint.StrokeWidth = 1;
                paint.Style = SKPaintStyle.Stroke;
                canvas.DrawPath(menuPath, paint);
                paint.Style = SKPaintStyle.Fill;
            }

            // Draw menu items
            for (int i = 0; i < _menuItems.Count; i++)
            {
                var item = _menuItems[i];
                var itemBounds = new SKRect(
                    menuBounds.Left,
                    menuY + i * menuItemHeight,
                    menuBounds.Right,
                    menuY + (i + 1) * menuItemHeight
                );

                DrawMenuItem(canvas, item, itemBounds, i == _menuItems.Count - 1);
            }
        }

        /// <summary>
        /// Draws a menu item.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="item">The menu item to draw.</param>
        /// <param name="bounds">The bounds of the menu item.</param>
        /// <param name="isLast">Whether this is the last menu item.</param>
        private void DrawMenuItem(SKCanvas canvas, SplitButtonItem item, SKRect bounds, bool isLast)
        {
            // Draw background if selected
            if (_selectedItem == item)
            {
                using (var bgPaint = new SKPaint())
                {
                    bgPaint.Color = MaterialColors.SecondaryContainer;
                    bgPaint.IsAntialias = true;
                    canvas.DrawRect(bounds, bgPaint);
                }
            }

            // Draw text (SKFont)
            using var font = new SKFont(SKTypeface.Default, 14);
            using var paint = new SKPaint { Color = item.IsEnabled ? MaterialColors.OnSurface : MaterialColors.OnSurfaceVariant, IsAntialias = true };
            var metrics = font.Metrics;
            float baseline = bounds.MidY + metrics.CapHeight / 2f;
            float textX = bounds.Left + 16;
            canvas.DrawText(item.Text, textX, baseline, SKTextAlign.Left, font, paint);
        }

        /// <summary>
        /// Handles mouse down events.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>True if the event was handled.</returns>
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            var handled = base.OnMouseDown(point, context);

            if (_isDropdownOpen)
            {
                // Handle menu item clicks
                return HandleMenuClick(point) || handled;
            }
            else
            {
                // Determine which button was clicked
                float dropdownButtonWidth = 48;
                float primaryButtonWidth = Width - dropdownButtonWidth - _separatorWidth;

                if (point.X < primaryButtonWidth)
                {
                    // Primary button clicked
                    OnPrimaryButtonClicked(EventArgs.Empty);
                    return true;
                }
                else
                {
                    // Dropdown button clicked
                    IsDropdownOpen = !IsDropdownOpen;
                    OnDropdownButtonClicked(EventArgs.Empty);
                    return true;
                }
            }
        }

        /// <summary>
        /// Handles clicks on menu items.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <returns>True if a menu item was clicked.</returns>
        private bool HandleMenuClick(SKPoint point)
        {
            if (_menuItems.Count == 0)
                return false;

            float menuItemHeight = 36;
            float menuY = Height + 4;

            if (point.Y >= menuY && point.Y <= menuY + _menuItems.Count * menuItemHeight)
            {
                int clickedIndex = (int)((point.Y - menuY) / menuItemHeight);
                if (clickedIndex >= 0 && clickedIndex < _menuItems.Count)
                {
                    var item = _menuItems[clickedIndex];
                    if (item.IsEnabled)
                    {
                        SelectItem(item);
                        IsDropdownOpen = false;
                        return true;
                    }
                }
            }

            // Click outside menu closes it
            IsDropdownOpen = false;
            return true;
        }

        /// <summary>
        /// Raises the PrimaryButtonClicked event.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnPrimaryButtonClicked(EventArgs e)
        {
            PrimaryButtonClicked?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the DropdownButtonClicked event.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnDropdownButtonClicked(EventArgs e)
        {
            DropdownButtonClicked?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the ItemSelected event.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnItemSelected(SplitButtonItemSelectedEventArgs e)
        {
            ItemSelected?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the DropdownStateChanged event.
        /// </summary>
        protected virtual void OnDropdownStateChanged()
        {
            DropdownStateChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Event arguments for split button item selection.
        /// </summary>
        public class SplitButtonItemSelectedEventArgs : EventArgs
        {
            /// <summary>
            /// Gets the selected menu item.
            /// </summary>
            public SplitButtonItem SelectedItem { get; }

            /// <summary>
            /// Initializes a new instance of the SplitButtonItemSelectedEventArgs class.
            /// </summary>
            /// <param name="selectedItem">The selected menu item.</param>
            public SplitButtonItemSelectedEventArgs(SplitButtonItem selectedItem)
            {
                SelectedItem = selectedItem;
            }
        }

        /// <summary>
        /// Creates a new SplitButton instance with the specified menu items.
        /// </summary>
        /// <param name="primaryText">The text for the primary button.</param>
        /// <param name="menuItems">The menu items for the dropdown.</param>
        /// <returns>A new SplitButton instance.</returns>
        public static SplitButton Create(string primaryText, params string[] menuItems)
        {
            var splitButton = new SplitButton();
            splitButton.Text = primaryText;

            foreach (var item in menuItems)
            {
                splitButton.AddMenuItem(item);
            }

            return splitButton;
        }

        /// <summary>
        /// Creates a new SplitButton instance with filled variant.
        /// </summary>
        /// <param name="primaryText">The text for the primary button.</param>
        /// <param name="menuItems">The menu items for the dropdown.</param>
        /// <returns>A new SplitButton instance.</returns>
        public static SplitButton CreateFilled(string primaryText, params string[] menuItems)
        {
            var splitButton = Create(primaryText, menuItems);
            splitButton.Variant = ButtonVariant.Filled;
            return splitButton;
        }

        /// <summary>
        /// Creates a new SplitButton instance with outlined variant.
        /// </summary>
        /// <param name="primaryText">The text for the primary button.</param>
        /// <param name="menuItems">The menu items for the dropdown.</param>
        /// <returns>A new SplitButton instance.</returns>
        public static SplitButton CreateOutlined(string primaryText, params string[] menuItems)
        {
            var splitButton = Create(primaryText, menuItems);
            splitButton.Variant = ButtonVariant.Outlined;
            return splitButton;
        }
    }
}

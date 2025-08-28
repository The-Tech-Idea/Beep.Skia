using SkiaSharp;
using System;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 Menu Item component.
    /// </summary>
    public class MenuItem
    {
        private string _text = "";
        private string _icon = "";
        private string _shortcut = "";
        private bool _isEnabled = true;
        private bool _isSelected = false;
        private bool _isHovered = false;
        private bool _showSeparator = false;
        private MenuItemType _itemType = MenuItemType.Standard;
        private SKColor _textColor = MaterialDesignColors.OnSurface;
        private SKColor _iconColor = MaterialDesignColors.OnSurfaceVariant;
        private float _iconSize = 20;
        private object _tag;

        /// <summary>
        /// Material Design 3.0 menu item types.
        /// </summary>
        public enum MenuItemType
        {
            Standard,
            WithIcon,
            WithShortcut,
            WithIconAndShortcut,
            Separator
        }

        /// <summary>
        /// Gets or sets the parent menu.
        /// </summary>
        public Menu ParentMenu { get; set; }

        /// <summary>
        /// Gets or sets the text displayed in the menu item.
        /// </summary>
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value ?? "";
                    ParentMenu?.Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets or sets the icon (Unicode character or SVG path).
        /// </summary>
        public string Icon
        {
            get => _icon;
            set
            {
                if (_icon != value)
                {
                    _icon = value ?? "";
                    ParentMenu?.Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets or sets the keyboard shortcut text.
        /// </summary>
        public string Shortcut
        {
            get => _shortcut;
            set
            {
                if (_shortcut != value)
                {
                    _shortcut = value ?? "";
                    ParentMenu?.Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the menu item is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    ParentMenu?.Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the menu item is selected.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    ParentMenu?.Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the menu item is hovered.
        /// </summary>
        public bool IsHovered
        {
            get => _isHovered;
            set
            {
                if (_isHovered != value)
                {
                    _isHovered = value;
                    ParentMenu?.Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show a separator line after this item.
        /// </summary>
        public bool ShowSeparator
        {
            get => _showSeparator;
            set
            {
                if (_showSeparator != value)
                {
                    _showSeparator = value;
                    ParentMenu?.Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets or sets the menu item type.
        /// </summary>
        public MenuItemType ItemType
        {
            get => _itemType;
            set
            {
                if (_itemType != value)
                {
                    _itemType = value;
                    ParentMenu?.Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets or sets the text color.
        /// </summary>
        public SKColor TextColor
        {
            get => _textColor;
            set
            {
                if (_textColor != value)
                {
                    _textColor = value;
                    ParentMenu?.Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets or sets the icon color.
        /// </summary>
        public SKColor IconColor
        {
            get => _iconColor;
            set
            {
                if (_iconColor != value)
                {
                    _iconColor = value;
                    ParentMenu?.Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets or sets the icon size.
        /// </summary>
        public float IconSize
        {
            get => _iconSize;
            set
            {
                if (_iconSize != value)
                {
                    _iconSize = value;
                    ParentMenu?.Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets or sets additional data associated with the menu item.
        /// </summary>
        public object Tag
        {
            get => _tag;
            set => _tag = value;
        }

        /// <summary>
        /// Occurs when the menu item is clicked.
        /// </summary>
        public event EventHandler Clicked;

        /// <summary>
        /// Initializes a new instance of the MenuItem class.
        /// </summary>
        public MenuItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the MenuItem class with text.
        /// </summary>
        /// <param name="text">The text to display.</param>
        public MenuItem(string text)
        {
            _text = text ?? "";
        }

        /// <summary>
        /// Initializes a new instance of the MenuItem class with text and icon.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="icon">The icon to display.</param>
        public MenuItem(string text, string icon)
        {
            _text = text ?? "";
            _icon = icon ?? "";
            _itemType = MenuItemType.WithIcon;
        }

        /// <summary>
        /// Initializes a new instance of the MenuItem class with text and shortcut.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="shortcut">The keyboard shortcut.</param>
        public MenuItem(string text, string icon, string shortcut)
        {
            _text = text ?? "";
            _icon = icon ?? "";
            _shortcut = shortcut ?? "";
            _itemType = MenuItemType.WithIconAndShortcut;
        }

        /// <summary>
        /// Called when the menu item is clicked.
        /// </summary>
        public void OnClick()
        {
            if (IsEnabled)
            {
                Clicked?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Draws the menu item on the specified canvas.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The bounds of the menu item.</param>
        /// <param name="drawingContext">The drawing context.</param>
        public void Draw(SKCanvas canvas, SKRect bounds, DrawingContext drawingContext)
        {
            if (_itemType == MenuItemType.Separator)
            {
                // Draw separator line
                using (var separatorPaint = new SKPaint
                {
                    Color = MaterialDesignColors.OutlineVariant,
                    Style = SKPaintStyle.Fill
                })
                {
                    float separatorCenterY = bounds.MidY;
                    canvas.DrawLine(bounds.Left + 16, separatorCenterY, bounds.Right - 16, separatorCenterY, separatorPaint);
                }
                return;
            }

            // Draw hover/selection background
            if (IsHovered || IsSelected)
            {
                SKColor backgroundColor = IsSelected ?
                    MaterialDesignColors.SecondaryContainer :
                    MaterialDesignColors.OnSurface.WithAlpha(12);

                using (var backgroundPaint = new SKPaint
                {
                    Color = backgroundColor,
                    Style = SKPaintStyle.Fill
                })
                {
                    canvas.DrawRect(bounds, backgroundPaint);
                }
            }

            float currentX = bounds.Left + 16; // Left padding
            float centerY = bounds.MidY;

            // Draw icon if present
            if (!string.IsNullOrEmpty(_icon) &&
                (_itemType == MenuItemType.WithIcon || _itemType == MenuItemType.WithIconAndShortcut))
            {
                using (var iconPaint = new SKPaint
                {
                    Color = IsEnabled ? _iconColor : MaterialDesignColors.OnSurfaceVariant.WithAlpha(100),
                    TextSize = _iconSize,
                    TextAlign = SKTextAlign.Left,
                    Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal)
                })
                {
                    float iconY = centerY + _iconSize / 3;
                    canvas.DrawText(_icon, currentX, iconY, iconPaint);
                }
                currentX += _iconSize + 12; // Icon width + spacing
            }

            // Draw text
            if (!string.IsNullOrEmpty(_text))
            {
                using (var textPaint = new SKPaint
                {
                    Color = IsEnabled ? _textColor : MaterialDesignColors.OnSurfaceVariant.WithAlpha(100),
                    TextSize = 14,
                    TextAlign = SKTextAlign.Left,
                    Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal)
                })
                {
                    float textY = centerY + 5; // Approximate text baseline
                    canvas.DrawText(_text, currentX, textY, textPaint);
                }
            }

            // Draw shortcut if present
            if (!string.IsNullOrEmpty(_shortcut) &&
                (_itemType == MenuItemType.WithShortcut || _itemType == MenuItemType.WithIconAndShortcut))
            {
                using (var shortcutPaint = new SKPaint
                {
                    Color = IsEnabled ? MaterialDesignColors.OnSurfaceVariant : MaterialDesignColors.OnSurfaceVariant.WithAlpha(100),
                    TextSize = 12,
                    TextAlign = SKTextAlign.Right,
                    Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal)
                })
                {
                    float shortcutY = centerY + 4;
                    float shortcutX = bounds.Right - 16; // Right padding
                    canvas.DrawText(_shortcut, shortcutX, shortcutY, shortcutPaint);
                }
            }
        }

        /// <summary>
        /// Creates a separator menu item.
        /// </summary>
        /// <returns>A separator menu item.</returns>
        public static MenuItem Separator()
        {
            return new MenuItem
            {
                ItemType = MenuItemType.Separator,
                ShowSeparator = true
            };
        }
    }
}

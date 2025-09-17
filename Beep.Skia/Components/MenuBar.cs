using SkiaSharp;
using System;
using System.Collections.Generic;
using Beep.Skia.Model;
namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 Menu Bar component that displays horizontal menu items.
    /// </summary>
    public class MenuBar : MaterialControl
    {
        private readonly List<MenuBarItem> _items = new List<MenuBarItem>();
        private MenuBarItem _activeItem;
        private SKColor _backgroundColor;
        private SKColor _itemHoverColor;
        private SKColor _itemActiveColor;
        private float _itemHeight = 32f;
        private float _itemSpacing = 8f;

        /// <summary>
        /// Gets the list of menu bar items.
        /// </summary>
        public IReadOnlyList<MenuBarItem> Items => _items.AsReadOnly();

        /// <summary>
        /// Gets or sets the currently active menu bar item.
        /// </summary>
        public MenuBarItem ActiveItem
        {
            get => _activeItem;
            private set
            {
                if (_activeItem != value)
                {
                    if (_activeItem != null)
                    {
                        _activeItem.IsActive = false;
                    }

                    _activeItem = value;

                    if (_activeItem != null)
                    {
                        _activeItem.IsActive = true;
                    }

                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the background color of the menu bar.
        /// </summary>
        public SKColor BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the hover color for menu items.
        /// </summary>
        public SKColor ItemHoverColor
        {
            get => _itemHoverColor;
            set
            {
                _itemHoverColor = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the active color for menu items.
        /// </summary>
        public SKColor ItemActiveColor
        {
            get => _itemActiveColor;
            set
            {
                _itemActiveColor = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the height of menu items.
        /// </summary>
        public float ItemHeight
        {
            get => _itemHeight;
            set
            {
                _itemHeight = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the spacing between menu items.
        /// </summary>
        public float ItemSpacing
        {
            get => _itemSpacing;
            set
            {
                _itemSpacing = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Occurs when a menu item is clicked.
        /// </summary>
        public event EventHandler<MenuBarItemEventArgs> ItemClicked;

        /// <summary>
        /// Occurs when a menu item is activated (submenu shown).
        /// </summary>
        public event EventHandler<MenuBarItemEventArgs> ItemActivated;

        /// <summary>
        /// Occurs when a menu item is deactivated (submenu hidden).
        /// </summary>
        public event EventHandler<MenuBarItemEventArgs> ItemDeactivated;

        /// <summary>
        /// Initializes a new instance of the MenuBar class.
        /// </summary>
        public MenuBar()
        {
            // Set default Material Design 3.0 colors
            BackgroundColor = MaterialDesignColors.Surface;
            ItemHoverColor = MaterialDesignColors.OnSurface.WithAlpha(12);
            ItemActiveColor = MaterialDesignColors.OnSurface.WithAlpha(8);

            // Set default size
            Width = 400f;
            Height = ItemHeight;
        }

        /// <summary>
        /// Adds a menu bar item.
        /// </summary>
        /// <param name="item">The menu bar item to add.</param>
        public void AddItem(MenuBarItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            _items.Add(item);
            item.MenuBar = this;
            InvalidateVisual();
        }

        /// <summary>
        /// Adds a menu bar item with the specified text and submenu.
        /// </summary>
        /// <param name="text">The text for the menu item.</param>
        /// <param name="submenu">The submenu to display when activated.</param>
        public MenuBarItem AddItem(string text, Menu submenu = null)
        {
            var item = new MenuBarItem(text, submenu);
            AddItem(item);
            return item;
        }

        /// <summary>
        /// Removes a menu bar item.
        /// </summary>
        /// <param name="item">The menu bar item to remove.</param>
        public void RemoveItem(MenuBarItem item)
        {
            if (item == null) return;

            if (_activeItem == item)
            {
                ActiveItem = null;
            }

            _items.Remove(item);
            item.MenuBar = null;
            InvalidateVisual();
        }

        /// <summary>
        /// Clears all menu bar items.
        /// </summary>
        public void ClearItems()
        {
            foreach (var item in _items)
            {
                item.MenuBar = null;
            }

            _items.Clear();
            ActiveItem = null;
            InvalidateVisual();
        }

        /// <summary>
        /// Activates the specified menu item.
        /// </summary>
        /// <param name="item">The menu item to activate.</param>
        public void ActivateItem(MenuBarItem item)
        {
            if (item == null || !_items.Contains(item)) return;

            ActiveItem = item;
            ItemActivated?.Invoke(this, new MenuBarItemEventArgs(item));
        }

        /// <summary>
        /// Deactivates the currently active menu item.
        /// </summary>
        public void DeactivateActiveItem()
        {
            if (_activeItem != null)
            {
                var item = _activeItem;
                ActiveItem = null;
                ItemDeactivated?.Invoke(this, new MenuBarItemEventArgs(item));
            }
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Use absolute rect
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            using (var paint = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill })
            {
                canvas.DrawRect(rect, paint);
            }

            float currentX = rect.Left;
            foreach (var item in _items)
            {
                var itemBounds = new SKRect(currentX, rect.Top, currentX + item.Width, rect.Bottom);
                item.Draw(canvas, itemBounds);
                currentX += item.Width + ItemSpacing;
            }
        }

        protected override bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            base.OnMouseMove(point, context);

            // Handle hover states
            foreach (var item in _items)
            {
                var itemBounds = GetItemBounds(item);
                bool isHovered = itemBounds.Contains(point);

                if (item.IsHovered != isHovered)
                {
                    item.IsHovered = isHovered;
                    InvalidateVisual();
                }
            }

            return true;
        }

        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            base.OnMouseDown(point, context);

            if (context.MouseButton == 0) // Left button
            {
                foreach (var item in _items)
                {
                    var itemBounds = GetItemBounds(item);
                    if (itemBounds.Contains(point))
                    {
                        if (item == ActiveItem)
                        {
                            // Click on active item - deactivate
                            DeactivateActiveItem();
                        }
                        else
                        {
                            // Click on inactive item - activate
                            ActivateItem(item);
                        }

                        ItemClicked?.Invoke(this, new MenuBarItemEventArgs(item));
                        break;
                    }
                }
            }

            return true;
        }

        private SKRect GetItemBounds(MenuBarItem item)
        {
            float xOffset = X; // start at absolute X
            foreach (var currentItem in _items)
            {
                if (currentItem == item)
                {
                    return new SKRect(xOffset, Y, xOffset + item.Width, Y + Height);
                }
                xOffset += currentItem.Width + ItemSpacing;
            }
            return SKRect.Empty;
        }

        /// <summary>
        /// Public wrapper for mouse down events (used by demos).
        /// </summary>
        public new void HandleMouseDown(SKPoint point, InteractionContext context)
        {
            base.OnMouseDown(point, context);

            if (context.MouseButton == 0) // Left button
            {
                foreach (var item in _items)
                {
                    var itemBounds = GetItemBounds(item);
                    if (itemBounds.Contains(point))
                    {
                        if (item == ActiveItem)
                        {
                            // Click on active item - deactivate
                            DeactivateActiveItem();
                        }
                        else
                        {
                            // Click on inactive item - activate
                            ActivateItem(item);
                        }

                        ItemClicked?.Invoke(this, new MenuBarItemEventArgs(item));
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Public wrapper for mouse move events (used by demos).
        /// </summary>
        public new void HandleMouseMove(SKPoint point, InteractionContext context)
        {
            base.OnMouseMove(point, context);

            // Handle hover states
            foreach (var item in _items)
            {
                var itemBounds = GetItemBounds(item);
                bool isHovered = itemBounds.Contains(point);

                if (item.IsHovered != isHovered)
                {
                    item.IsHovered = isHovered;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Menu bar item event arguments.
        /// </summary>
        public class MenuBarItemEventArgs : EventArgs
        {
            /// <summary>
            /// Gets the menu bar item.
            /// </summary>
            public MenuBarItem Item { get; }

            /// <summary>
            /// Initializes a new instance of the MenuBarItemEventArgs class.
            /// </summary>
            /// <param name="item">The menu bar item.</param>
            public MenuBarItemEventArgs(MenuBarItem item)
            {
                Item = item;
            }
        }
    }

    /// <summary>
    /// Represents an item in a menu bar.
    /// </summary>
    public class MenuBarItem
    {
        private string _text;
        private Menu _submenu;
        private MenuBar _menuBar;
        private bool _isHovered;
        private bool _isActive;
        private float _width;

        /// <summary>
        /// Gets or sets the text of the menu item.
        /// </summary>
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                UpdateWidth();
            }
        }

        /// <summary>
        /// Gets or sets the submenu associated with this item.
        /// </summary>
        public Menu Submenu
        {
            get => _submenu;
            set => _submenu = value;
        }

        /// <summary>
        /// Gets the menu bar that contains this item.
        /// </summary>
        public MenuBar MenuBar
        {
            get => _menuBar;
            internal set => _menuBar = value;
        }

        /// <summary>
        /// Gets or sets whether the item is hovered.
        /// </summary>
        public bool IsHovered
        {
            get => _isHovered;
            set => _isHovered = value;
        }

        /// <summary>
        /// Gets or sets whether the item is active (submenu shown).
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        /// <summary>
        /// Gets the width of the menu item.
        /// </summary>
        public float Width => _width;

        /// <summary>
        /// Initializes a new instance of the MenuBarItem class.
        /// </summary>
        /// <param name="text">The text of the menu item.</param>
        /// <param name="submenu">The submenu to display when activated.</param>
        public MenuBarItem(string text, Menu submenu = null)
        {
            _text = text;
            _submenu = submenu;
            UpdateWidth();
        }

        private void UpdateWidth()
        {
            if (string.IsNullOrEmpty(_text))
            {
                _width = 50f; // Minimum width
            }
            else
            {
                // Estimate width based on text length (rough calculation)
                _width = _text.Length * 8f + 20f; // 8px per character + padding
            }
        }

        /// <summary>
        /// Draws the menu bar item.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The bounds to draw within.</param>
        public void Draw(SKCanvas canvas, SKRect bounds)
        {
            // Background
            using var bgPaint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };
            if (IsActive)
                bgPaint.Color = _menuBar?.ItemActiveColor ?? MaterialDesignColors.OnSurface.WithAlpha(8);
            else if (IsHovered)
                bgPaint.Color = _menuBar?.ItemHoverColor ?? MaterialDesignColors.OnSurface.WithAlpha(12);
            else
                bgPaint.Color = SKColors.Transparent;
            canvas.DrawRect(bounds, bgPaint);

            // Fonts
            using var textFont = new SKFont(SKTypeface.Default, 14f);
            using var arrowFont = new SKFont(SKTypeface.Default, 10f);
            var metrics = textFont.Metrics; // ascent negative, cap height positive
            float capHeight = metrics.CapHeight;
            float baseline = bounds.Top + (bounds.Height + capHeight) / 2f;

            // Text
            using var textPaint = new SKPaint { Color = MaterialDesignColors.OnSurface, IsAntialias = true };
            float textWidth = textFont.MeasureText(_text ?? "");
            float textX = bounds.Left + (bounds.Width - textWidth) / 2f;
            canvas.DrawText(_text, textX, baseline, SKTextAlign.Left, textFont, textPaint);

            // Arrow indicator
            if (_submenu != null)
            {
                using var arrowPaint = new SKPaint { Color = MaterialDesignColors.OnSurfaceVariant, IsAntialias = true };
                float arrowWidth = arrowFont.MeasureText("▼");
                float arrowX = bounds.Right - 15 - arrowWidth / 2f; // keep similar spacing
                var arrowMetrics = arrowFont.Metrics;
                float arrowBaseline = bounds.Top + (bounds.Height + arrowMetrics.CapHeight) / 2f;
                canvas.DrawText("▼", arrowX, arrowBaseline, SKTextAlign.Left, arrowFont, arrowPaint);
            }
        }
    }
}

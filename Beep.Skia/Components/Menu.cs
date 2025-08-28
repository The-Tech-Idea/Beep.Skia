using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 Menu component that displays a list of selectable items.
    /// </summary>
    public class Menu : MaterialControl
    {
        private List<MenuItem> _items = new List<MenuItem>();
        private MenuItem _selectedItem;
        private float _itemHeight = 48; // Material Design 3.0 standard menu item height
        private float _menuWidth = 200;
        private float _cornerRadius = 4;
        private SKColor _surfaceColor = MaterialColors.SurfaceContainerHigh;
        private SKColor _onSurfaceColor = MaterialColors.OnSurface;
        private float _elevation = 3;
        private bool _isVisible = false;
        private SKPoint _anchorPoint;
        private MenuPosition _position = MenuPosition.BottomLeft;

        /// <summary>
        /// Material Design 3.0 menu positioning options.
        /// </summary>
        public enum MenuPosition
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            Center
        }

        /// <summary>
        /// Gets or sets the menu items.
        /// </summary>
        public List<MenuItem> Items
        {
            get => _items;
            set
            {
                _items = value ?? new List<MenuItem>();
                UpdateMenuSize();
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the selected menu item.
        /// </summary>
        public MenuItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    if (_selectedItem != null)
                        _selectedItem.IsSelected = false;

                    _selectedItem = value;

                    if (_selectedItem != null)
                        _selectedItem.IsSelected = true;

                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the menu width.
        /// </summary>
        public float MenuWidth
        {
            get => _menuWidth;
            set
            {
                if (_menuWidth != value)
                {
                    _menuWidth = value;
                    UpdateMenuSize();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the menu position relative to anchor point.
        /// </summary>
        public MenuPosition Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    UpdatePosition();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the anchor point for menu positioning.
        /// </summary>
        public SKPoint AnchorPoint
        {
            get => _anchorPoint;
            set
            {
                _anchorPoint = value;
                UpdatePosition();
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets whether the menu is visible.
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    if (_isVisible)
                    {
                        UpdatePosition();
                        // BringToFront(); // TODO: Implement BringToFront method
                    }
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Occurs when a menu item is clicked.
        /// </summary>
        public event EventHandler<MenuItem> ItemClicked;

        /// <summary>
        /// Occurs when the menu is opened.
        /// </summary>
        public event EventHandler MenuOpened;

        /// <summary>
        /// Occurs when the menu is closed.
        /// </summary>
        public event EventHandler MenuClosed;

        /// <summary>
        /// Initializes a new instance of the Menu class.
        /// </summary>
        public Menu()
        {
            IsVisible = false;
            UpdateMenuSize();
        }

        /// <summary>
        /// Adds a menu item to the menu.
        /// </summary>
        /// <param name="item">The menu item to add.</param>
        public void AddItem(MenuItem item)
        {
            if (item != null && !_items.Contains(item))
            {
                _items.Add(item);
                item.ParentMenu = this;
                UpdateMenuSize();
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Removes a menu item from the menu.
        /// </summary>
        /// <param name="item">The menu item to remove.</param>
        public void RemoveItem(MenuItem item)
        {
            if (item != null && _items.Contains(item))
            {
                _items.Remove(item);
                item.ParentMenu = null;
                if (_selectedItem == item)
                    _selectedItem = null;
                UpdateMenuSize();
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Clears all menu items.
        /// </summary>
        public void ClearItems()
        {
            foreach (var item in _items)
            {
                item.ParentMenu = null;
            }
            _items.Clear();
            _selectedItem = null;
            UpdateMenuSize();
            InvalidateVisual();
        }

        /// <summary>
        /// Shows the menu at the specified position.
        /// </summary>
        /// <param name="anchorPoint">The anchor point for positioning.</param>
        public void Show(SKPoint anchorPoint)
        {
            AnchorPoint = anchorPoint;
            IsVisible = true;
            MenuOpened?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Hides the menu.
        /// </summary>
        public void Hide()
        {
            IsVisible = false;
            MenuClosed?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateMenuSize()
        {
            Height = _items.Count * _itemHeight;
            Width = _menuWidth;
        }

        private void UpdatePosition()
        {
            if (!_isVisible) return;

            float menuX = _anchorPoint.X;
            float menuY = _anchorPoint.Y;

            switch (_position)
            {
                case MenuPosition.TopLeft:
                    menuX = _anchorPoint.X;
                    menuY = _anchorPoint.Y - Height;
                    break;
                case MenuPosition.TopRight:
                    menuX = _anchorPoint.X - Width;
                    menuY = _anchorPoint.Y - Height;
                    break;
                case MenuPosition.BottomLeft:
                    menuX = _anchorPoint.X;
                    menuY = _anchorPoint.Y;
                    break;
                case MenuPosition.BottomRight:
                    menuX = _anchorPoint.X - Width;
                    menuY = _anchorPoint.Y;
                    break;
                case MenuPosition.Center:
                    menuX = _anchorPoint.X - Width / 2;
                    menuY = _anchorPoint.Y - Height / 2;
                    break;
            }

            // Ensure menu stays within bounds
            if (menuX < 0) menuX = 0;
            if (menuY < 0) menuY = 0;
            float parentWidth = Parent?.Width ?? 0;
            float parentHeight = Parent?.Height ?? 0;
            if (menuX + Width > parentWidth && parentWidth > 0) menuX = parentWidth - Width;
            if (menuY + Height > parentHeight && parentHeight > 0) menuY = parentHeight - Height;

            X = menuX;
            Y = menuY;
        }

        /// <summary>
        /// Draws the menu on the specified canvas.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="drawingContext">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext drawingContext)
        {
            if (!_isVisible) return;

            // Draw menu background with elevation
            using (var backgroundPaint = new SKPaint
            {
                Color = _surfaceColor,
                Style = SKPaintStyle.Fill
            })
            {
                var menuRect = new SKRect(0, 0, Width, Height);
                canvas.DrawRoundRect(menuRect, _cornerRadius, _cornerRadius, backgroundPaint);
            }

            // Draw elevation shadow
            using (var shadowPaint = new SKPaint
            {
                Color = new SKColor(0, 0, 0, 30),
                Style = SKPaintStyle.Fill
            })
            {
                var shadowRect = new SKRect(2, 2, Width + 2, Height + 2);
                canvas.DrawRoundRect(shadowRect, _cornerRadius, _cornerRadius, shadowPaint);
            }

            // Draw menu items
            float currentY = 0;
            foreach (var item in _items)
            {
                item.Draw(canvas, new SKRect(0, currentY, Width, currentY + _itemHeight), drawingContext);
                currentY += _itemHeight;

                // Draw separator line if needed
                if (item.ShowSeparator && currentY < Height)
                {
                    using (var separatorPaint = new SKPaint
                    {
                        Color = MaterialColors.OutlineVariant,
                        Style = SKPaintStyle.Fill
                    })
                    {
                        canvas.DrawLine(16, currentY - 0.5f, Width - 16, currentY - 0.5f, separatorPaint);
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the specified point is contained within the menu.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>True if the point is contained within the menu.</returns>
        public override bool ContainsPoint(SKPoint point)
        {
            if (!_isVisible) return false;
            return point.X >= X && point.X <= X + Width && point.Y >= Y && point.Y <= Y + Height;
        }

        /// <summary>
        /// Handles mouse down events.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>True if the event was handled.</returns>
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            if (!IsVisible || !ContainsPoint(point)) return false;

            // Convert to local coordinates
            SKPoint localPoint = new SKPoint(point.X - X, point.Y - Y);

            // Find which item was clicked
            int itemIndex = (int)(localPoint.Y / _itemHeight);
            if (itemIndex >= 0 && itemIndex < _items.Count)
            {
                var clickedItem = _items[itemIndex];
                if (clickedItem.IsEnabled)
                {
                    SelectedItem = clickedItem;
                    ItemClicked?.Invoke(this, clickedItem);
                    clickedItem.OnClick();
                    return true;
                }
            }

            return base.OnMouseDown(point, context);
        }

        /// <summary>
        /// Handles mouse move events for hover effects.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>True if the event was handled.</returns>
        protected override bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            if (!IsVisible || !ContainsPoint(point)) return false;

            // Convert to local coordinates
            SKPoint localPoint = new SKPoint(point.X - X, point.Y - Y);

            // Update hover state
            int itemIndex = (int)(localPoint.Y / _itemHeight);
            if (itemIndex >= 0 && itemIndex < _items.Count)
            {
                var hoveredItem = _items[itemIndex];
                if (hoveredItem.IsEnabled)
                {
                    // Clear previous hover states
                    foreach (var item in _items)
                    {
                        if (item != hoveredItem)
                            item.IsHovered = false;
                    }
                    hoveredItem.IsHovered = true;
                    InvalidateVisual();
                    return true;
                }
            }

            return base.OnMouseMove(point, context);
        }

        /// <summary>
        /// Invalidates the menu visual and triggers a redraw.
        /// </summary>
        public void Invalidate()
        {
            InvalidateVisual();
        }
    }
}

using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 Floating Action Button Menu component.
    /// Provides a main FAB that opens a menu with multiple action items.
    /// </summary>
    public class FabMenu : MaterialControl
    {
        private FloatingActionButton _mainFab;
        private List<FabMenuItem> _menuItems;
        private bool _isOpen;
        private FabMenuDirection _direction;
        private float _itemSpacing;
        private float _animationProgress;
        private bool _isAnimating;
        private SKColor _overlayColor;

        /// <summary>
        /// Represents a menu item in the FAB menu.
        /// </summary>
        public class FabMenuItem
        {
            /// <summary>
            /// Gets or sets the icon for the menu item.
            /// </summary>
            public string Icon { get; set; }

            /// <summary>
            /// Gets or sets the label text for the menu item.
            /// </summary>
            public string Label { get; set; }

            /// <summary>
            /// Gets or sets the tooltip text for the menu item.
            /// </summary>
            public string Tooltip { get; set; }

            /// <summary>
            /// Gets or sets custom data associated with the menu item.
            /// </summary>
            public object Tag { get; set; }

            /// <summary>
            /// Gets or sets whether the menu item is enabled.
            /// </summary>
            public bool IsEnabled { get; set; } = true;

            /// <summary>
            /// Initializes a new instance of the FabMenuItem class.
            /// </summary>
            /// <param name="icon">The icon for the menu item.</param>
            /// <param name="label">The label text for the menu item.</param>
            public FabMenuItem(string icon, string label)
            {
                Icon = icon;
                Label = label;
            }
        }

        /// <summary>
        /// Material Design 3.0 FAB menu directions.
        /// </summary>
        public enum FabMenuDirection
        {
            Up,
            Down,
            Left,
            Right
        }

        /// <summary>
        /// Occurs when a menu item is clicked.
        /// </summary>
        public event EventHandler<FabMenuItemEventArgs> MenuItemClicked;

        /// <summary>
        /// Occurs when the menu is opened.
        /// </summary>
        public event EventHandler MenuOpened;

        /// <summary>
        /// Occurs when the menu is closed.
        /// </summary>
        public event EventHandler MenuClosed;

        /// <summary>
        /// Initializes a new instance of the FabMenu class.
        /// </summary>
        public FabMenu()
        {
            _menuItems = new List<FabMenuItem>();
            _direction = FabMenuDirection.Up;
            _itemSpacing = 16;
            _animationProgress = 0;
            _isAnimating = false;
            _overlayColor = new SKColor(0, 0, 0, 128); // Semi-transparent black

            InitializeMainFab();
        }

        /// <summary>
        /// Gets or sets the main FAB icon.
        /// </summary>
        public string MainIcon
        {
            get => _mainFab?.Icon ?? "➕";
            set
            {
                if (_mainFab != null)
                {
                    _mainFab.Icon = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the main FAB type.
        /// </summary>
        public FloatingActionButton.FabType MainFabType
        {
            get => _mainFab?.Type ?? FloatingActionButton.FabType.Primary;
            set
            {
                if (_mainFab != null)
                {
                    _mainFab.Type = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the menu direction.
        /// </summary>
        public FabMenuDirection Direction
        {
            get => _direction;
            set
            {
                if (_direction != value)
                {
                    _direction = value;
                    InvalidateVisual();
                }
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
                if (_itemSpacing != value)
                {
                    _itemSpacing = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the overlay color when the menu is open.
        /// </summary>
        public SKColor OverlayColor
        {
            get => _overlayColor;
            set
            {
                if (_overlayColor != value)
                {
                    _overlayColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets whether the menu is currently open.
        /// </summary>
        public bool IsOpen => _isOpen;

        /// <summary>
        /// Gets whether the menu is currently animating.
        /// </summary>
        public bool IsAnimating => _isAnimating;

        /// <summary>
        /// Adds a menu item to the FAB menu.
        /// </summary>
        /// <param name="item">The menu item to add.</param>
        public void AddMenuItem(FabMenuItem item)
        {
            if (item != null)
            {
                _menuItems.Add(item);
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Removes a menu item from the FAB menu.
        /// </summary>
        /// <param name="item">The menu item to remove.</param>
        public void RemoveMenuItem(FabMenuItem item)
        {
            if (item != null && _menuItems.Remove(item))
            {
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Clears all menu items.
        /// </summary>
        public void ClearMenuItems()
        {
            _menuItems.Clear();
            InvalidateVisual();
        }

        /// <summary>
        /// Opens the FAB menu.
        /// </summary>
        public void OpenMenu()
        {
            if (!_isOpen && !_isAnimating)
            {
                _isOpen = true;
                _isAnimating = true;
                StartAnimation(true);
                OnMenuOpened();
            }
        }

        /// <summary>
        /// Closes the FAB menu.
        /// </summary>
        public void CloseMenu()
        {
            if (_isOpen && !_isAnimating)
            {
                _isOpen = false;
                _isAnimating = true;
                StartAnimation(false);
                OnMenuClosed();
            }
        }

        /// <summary>
        /// Toggles the FAB menu open/closed state.
        /// </summary>
        public void ToggleMenu()
        {
            if (_isOpen)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }

        private void InitializeMainFab()
        {
            _mainFab = new FloatingActionButton("➕")
            {
                Size = FloatingActionButton.FabSize.Medium,
                Type = FloatingActionButton.FabType.Primary
            };

            _mainFab.Clicked += (s, e) => ToggleMenu();
        }

        private void StartAnimation(bool opening)
        {
            // Simple animation - in a real implementation, you'd use a proper animation framework
            _animationProgress = opening ? 0 : 1;

            // For now, we'll just set the final state immediately
            // In a real implementation, you'd animate _animationProgress over time
            _animationProgress = opening ? 1 : 0;
            _isAnimating = false;
            InvalidateVisual();
        }

        /// <summary>
        /// Draws the FAB menu on the specified canvas.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="drawingContext">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext drawingContext)
        {
            // Draw overlay if menu is open
            if (_isOpen && _animationProgress > 0)
            {
                using (var overlayPaint = new SKPaint
                {
                    Color = _overlayColor.WithAlpha((byte)(_overlayColor.Alpha * _animationProgress)),
                    Style = SKPaintStyle.Fill
                })
                {
                    canvas.DrawRect(0, 0, Width, Height, overlayPaint);
                }
            }

            // Position and draw the main FAB
            float fabX = Width - _mainFab.Width - 16;
            float fabY = Height - _mainFab.Height - 16;

            _mainFab.X = fabX;
            _mainFab.Y = fabY;
            _mainFab.Width = _mainFab.Width; // Keep existing size
            _mainFab.Height = _mainFab.Height;

            // Draw main FAB
            _mainFab.Draw(canvas, drawingContext);

            // Draw menu items if open
            if (_isOpen || _isAnimating)
            {
                DrawMenuItems(canvas, drawingContext, fabX, fabY);
            }
        }

        private void DrawMenuItems(SKCanvas canvas, DrawingContext drawingContext, float fabX, float fabY)
        {
            float itemSize = 40; // Mini FAB size
            float spacing = _itemSpacing + itemSize;

            for (int i = 0; i < _menuItems.Count; i++)
            {
                var item = _menuItems[i];
                if (!item.IsEnabled) continue;

                float progress = _animationProgress;
                float offset = (i + 1) * spacing * progress;

                float itemX = fabX;
                float itemY = fabY;

                // Calculate position based on direction
                switch (_direction)
                {
                    case FabMenuDirection.Up:
                        itemY -= offset;
                        break;
                    case FabMenuDirection.Down:
                        itemY += offset;
                        break;
                    case FabMenuDirection.Left:
                        itemX -= offset;
                        break;
                    case FabMenuDirection.Right:
                        itemX += offset;
                        break;
                }

                // Draw mini FAB background
                using (var fabPaint = new SKPaint
                {
                    Color = MaterialColors.SecondaryContainer,
                    Style = SKPaintStyle.Fill
                })
                {
                    float radius = itemSize / 2;
                    canvas.DrawCircle(itemX + radius, itemY + radius, radius, fabPaint);
                }

                // Draw icon
                if (!string.IsNullOrEmpty(item.Icon))
                {
                    using (var textPaint = new SKPaint
                    {
                        Color = MaterialColors.OnSecondaryContainer,
                        TextSize = 20,
                        TextAlign = SKTextAlign.Center,
                        Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal)
                    })
                    {
                        float textX = itemX + itemSize / 2;
                        float textY = itemY + itemSize / 2 + textPaint.TextSize / 3;
                        canvas.DrawText(item.Icon, textX, textY, textPaint);
                    }
                }

                // Draw label if provided
                if (!string.IsNullOrEmpty(item.Label) && progress > 0.5f)
                {
                    using (var labelPaint = new SKPaint
                    {
                        Color = MaterialColors.OnSurface,
                        TextSize = 14,
                        Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal)
                    })
                    {
                        float labelX = itemX - 8; // Position label to the left
                        float labelY = itemY + itemSize / 2 + labelPaint.TextSize / 3;

                        // Draw label background
                        var labelBounds = new SKRect();
                        labelPaint.MeasureText(item.Label, ref labelBounds);

                        using (var bgPaint = new SKPaint
                        {
                            Color = MaterialColors.Surface,
                            Style = SKPaintStyle.Fill
                        })
                        {
                            var bgRect = new SKRect(
                                labelX - labelBounds.Width - 16,
                                labelY - labelBounds.Height,
                                labelX - 8,
                                labelY + 4
                            );
                            canvas.DrawRoundRect(bgRect, 4, 4, bgPaint);
                        }

                        // Draw label text
                        canvas.DrawText(item.Label, labelX - labelBounds.Width - 12, labelY, labelPaint);
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the specified point is contained within the FAB menu.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>True if the point is contained within the FAB menu.</returns>
        public override bool ContainsPoint(SKPoint point)
        {
            // Check main FAB
            if (_mainFab.ContainsPoint(new SKPoint(point.X - _mainFab.X, point.Y - _mainFab.Y)))
            {
                return true;
            }

            // Check menu items if open
            if (_isOpen)
            {
                float fabX = Width - _mainFab.Width - 16;
                float fabY = Height - _mainFab.Height - 16;
                float itemSize = 40;
                float spacing = _itemSpacing + itemSize;

                for (int i = 0; i < _menuItems.Count; i++)
                {
                    var item = _menuItems[i];
                    if (!item.IsEnabled) continue;

                    float offset = (i + 1) * spacing;
                    float itemX = fabX;
                    float itemY = fabY;

                    switch (_direction)
                    {
                        case FabMenuDirection.Up:
                            itemY -= offset;
                            break;
                        case FabMenuDirection.Down:
                            itemY += offset;
                            break;
                        case FabMenuDirection.Left:
                            itemX -= offset;
                            break;
                        case FabMenuDirection.Right:
                            itemX += offset;
                            break;
                    }

                    var itemRect = new SKRect(itemX, itemY, itemX + itemSize, itemY + itemSize);
                    if (itemRect.Contains(point.X, point.Y))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Handles mouse down events.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>True if the event was handled.</returns>
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            // Check menu items first
            if (_isOpen)
            {
                float fabX = Width - _mainFab.Width - 16;
                float fabY = Height - _mainFab.Height - 16;
                float itemSize = 40;
                float spacing = _itemSpacing + itemSize;

                for (int i = 0; i < _menuItems.Count; i++)
                {
                    var item = _menuItems[i];
                    if (!item.IsEnabled) continue;

                    float offset = (i + 1) * spacing;
                    float itemX = fabX;
                    float itemY = fabY;

                    switch (_direction)
                    {
                        case FabMenuDirection.Up:
                            itemY -= offset;
                            break;
                        case FabMenuDirection.Down:
                            itemY += offset;
                            break;
                        case FabMenuDirection.Left:
                            itemX -= offset;
                            break;
                        case FabMenuDirection.Right:
                            itemX += offset;
                            break;
                    }

                    var itemRect = new SKRect(itemX, itemY, itemX + itemSize, itemY + itemSize);
                    if (itemRect.Contains(point.X, point.Y))
                    {
                        OnMenuItemClicked(new FabMenuItemEventArgs(item));
                        CloseMenu();
                        return true;
                    }
                }
            }

            // Check main FAB
            var fabPoint = new SKPoint(point.X - (Width - _mainFab.Width - 16), point.Y - (Height - _mainFab.Height - 16));
            if (_mainFab.ContainsPoint(fabPoint))
            {
                // Handle FAB click by toggling menu
                ToggleMenu();
                return true;
            }

            // Close menu if clicking outside
            if (_isOpen)
            {
                CloseMenu();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Handles mouse move events.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>True if the event was handled.</returns>
        protected override bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            // Handle hover effects for menu items and main FAB
            return base.OnMouseMove(point, context);
        }

        /// <summary>
        /// Handles mouse up events.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>True if the event was handled.</returns>
        protected override bool OnMouseUp(SKPoint point, InteractionContext context)
        {
            // Mouse up is handled in OnMouseDown for this component
            return base.OnMouseUp(point, context);
        }

        /// <summary>
        /// Raises the MenuItemClicked event.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnMenuItemClicked(FabMenuItemEventArgs e)
        {
            MenuItemClicked?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the MenuOpened event.
        /// </summary>
        protected virtual void OnMenuOpened()
        {
            MenuOpened?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the MenuClosed event.
        /// </summary>
        protected virtual void OnMenuClosed()
        {
            MenuClosed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Event arguments for FAB menu item clicks.
        /// </summary>
        public class FabMenuItemEventArgs : EventArgs
        {
            /// <summary>
            /// Gets the menu item that was clicked.
            /// </summary>
            public FabMenuItem MenuItem { get; }

            /// <summary>
            /// Initializes a new instance of the FabMenuItemEventArgs class.
            /// </summary>
            /// <param name="menuItem">The menu item that was clicked.</param>
            public FabMenuItemEventArgs(FabMenuItem menuItem)
            {
                MenuItem = menuItem;
            }
        }
    }
}

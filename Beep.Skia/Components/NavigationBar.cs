using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 Navigation Bar component for bottom navigation.
    /// </summary>
    public class NavigationBar : MaterialControl
    {
        private readonly List<NavigationBarItem> _items = new List<NavigationBarItem>();
        private NavigationBarItem _selectedItem;
        private SKColor _backgroundColor;
        private SKColor _activeColor;
        private SKColor _inactiveColor;
        private float _itemHeight = 80f;
        private float _iconSize = 24f;
        private float _labelFontSize = 12f;
        private bool _showLabels = true;

        /// <summary>
        /// Gets the list of navigation bar items.
        /// </summary>
        public IReadOnlyList<NavigationBarItem> Items => _items.AsReadOnly();

        /// <summary>
        /// Gets or sets the currently selected navigation bar item.
        /// </summary>
        public NavigationBarItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    if (_selectedItem != null)
                    {
                        _selectedItem.IsSelected = false;
                    }

                    _selectedItem = value;

                    if (_selectedItem != null)
                    {
                        _selectedItem.IsSelected = true;
                    }

                    RefreshVisual();
                    ItemSelected?.Invoke(this, new NavigationBarItemEventArgs(_selectedItem));
                }
            }
        }

        /// <summary>
        /// Gets or sets the background color of the navigation bar.
        /// </summary>
        public SKColor BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the active/selected color for navigation items.
        /// </summary>
        public SKColor ActiveColor
        {
            get => _activeColor;
            set
            {
                _activeColor = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the inactive color for navigation items.
        /// </summary>
        public SKColor InactiveColor
        {
            get => _inactiveColor;
            set
            {
                _inactiveColor = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the height of navigation items.
        /// </summary>
        public float ItemHeight
        {
            get => _itemHeight;
            set
            {
                _itemHeight = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the size of icons in navigation items.
        /// </summary>
        public float IconSize
        {
            get => _iconSize;
            set
            {
                _iconSize = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the font size for item labels.
        /// </summary>
        public float LabelFontSize
        {
            get => _labelFontSize;
            set
            {
                _labelFontSize = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets whether to show labels for navigation items.
        /// </summary>
        public bool ShowLabels
        {
            get => _showLabels;
            set
            {
                _showLabels = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Occurs when a navigation item is selected.
        /// </summary>
        public event EventHandler<NavigationBarItemEventArgs> ItemSelected;

        /// <summary>
        /// Initializes a new instance of the NavigationBar class.
        /// </summary>
        public NavigationBar()
        {
            // Set default Material Design 3.0 colors
            BackgroundColor = MaterialDesignColors.Surface;
            ActiveColor = MaterialDesignColors.Primary;
            InactiveColor = MaterialDesignColors.OnSurfaceVariant;

            // Set default size for bottom navigation
            Height = ItemHeight;
            Width = 400f; // Will be adjusted based on parent
        }

        /// <summary>
        /// Adds a navigation bar item.
        /// </summary>
        /// <param name="item">The navigation bar item to add.</param>
        public void AddItem(NavigationBarItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            _items.Add(item);
            item.NavigationBar = this;
            RefreshVisual();
        }

        /// <summary>
        /// Adds a navigation bar item with the specified icon and label.
        /// </summary>
        /// <param name="icon">The icon for the navigation item.</param>
        /// <param name="label">The label text for the navigation item.</param>
        /// <param name="tag">Optional tag object for the item.</param>
        public NavigationBarItem AddItem(string icon, string label, object tag = null)
        {
            var item = new NavigationBarItem(icon, label, tag);
            AddItem(item);
            return item;
        }

        /// <summary>
        /// Removes a navigation bar item.
        /// </summary>
        /// <param name="item">The navigation bar item to remove.</param>
        public void RemoveItem(NavigationBarItem item)
        {
            if (item == null) return;

            if (_selectedItem == item)
            {
                SelectedItem = null;
            }

            _items.Remove(item);
            item.NavigationBar = null;
            RefreshVisual();
        }

        /// <summary>
        /// Clears all navigation bar items.
        /// </summary>
        public void ClearItems()
        {
            foreach (var item in _items)
            {
                item.NavigationBar = null;
            }

            _items.Clear();
            SelectedItem = null;
            RefreshVisual();
        }

        /// <summary>
        /// Selects the item at the specified index.
        /// </summary>
        /// <param name="index">The index of the item to select.</param>
        public void SelectItem(int index)
        {
            if (index >= 0 && index < _items.Count)
            {
                SelectedItem = _items[index];
            }
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Draw background
            using (var paint = new SKPaint())
            {
                paint.Color = BackgroundColor;
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawRect(Bounds, paint);
            }

            if (_items.Count == 0) return;

            // Calculate item width
            float itemWidth = Width / _items.Count;

            // Draw navigation items
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                float x = i * itemWidth;
                var itemBounds = new SKRect(x, Bounds.Top, x + itemWidth, Bounds.Bottom);
                item.Draw(canvas, itemBounds);
            }
        }

        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            base.OnMouseDown(point, context);

            if (context.MouseButton == 0 && _items.Count > 0) // Left button
            {
                float itemWidth = Width / _items.Count;

                for (int i = 0; i < _items.Count; i++)
                {
                    float x = i * itemWidth;
                    var itemBounds = new SKRect(x, Bounds.Top, x + itemWidth, Bounds.Bottom);

                    if (itemBounds.Contains(point))
                    {
                        SelectedItem = _items[i];
                        break;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Public wrapper for mouse down events (used by demos).
        /// </summary>
        public new void HandleMouseDown(SKPoint point, InteractionContext context)
        {
            OnMouseDown(point, context);
        }

        /// <summary>
        /// Navigation bar item event arguments.
        /// </summary>
        public class NavigationBarItemEventArgs : EventArgs
        {
            /// <summary>
            /// Gets the navigation bar item.
            /// </summary>
            public NavigationBarItem Item { get; }

            /// <summary>
            /// Initializes a new instance of the NavigationBarItemEventArgs class.
            /// </summary>
            /// <param name="item">The navigation bar item.</param>
            public NavigationBarItemEventArgs(NavigationBarItem item)
            {
                Item = item;
            }
        }
    }

    /// <summary>
    /// Represents an item in a navigation bar.
    /// </summary>
    public class NavigationBarItem
    {
        private string _icon;
        private string _label;
        private object _tag;
        private NavigationBar _navigationBar;
        private bool _isSelected;
        private bool _hasBadge;
        private string _badgeText;

        /// <summary>
        /// Gets or sets the icon for the navigation item.
        /// </summary>
        public string Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                _navigationBar?.RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the label text for the navigation item.
        /// </summary>
        public string Label
        {
            get => _label;
            set
            {
                _label = value;
                _navigationBar?.RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the tag object associated with this item.
        /// </summary>
        public object Tag
        {
            get => _tag;
            set => _tag = value;
        }

        /// <summary>
        /// Gets the navigation bar that contains this item.
        /// </summary>
        public NavigationBar NavigationBar
        {
            get => _navigationBar;
            internal set => _navigationBar = value;
        }

        /// <summary>
        /// Gets or sets whether the item is selected.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                _navigationBar?.RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets whether the item has a badge.
        /// </summary>
        public bool HasBadge
        {
            get => _hasBadge;
            set
            {
                _hasBadge = value;
                _navigationBar?.RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the badge text.
        /// </summary>
        public string BadgeText
        {
            get => _badgeText;
            set
            {
                _badgeText = value;
                _navigationBar?.RefreshVisual();
            }
        }

        /// <summary>
        /// Initializes a new instance of the NavigationBarItem class.
        /// </summary>
        /// <param name="icon">The icon for the navigation item.</param>
        /// <param name="label">The label text for the navigation item.</param>
        /// <param name="tag">Optional tag object for the item.</param>
        public NavigationBarItem(string icon, string label, object tag = null)
        {
            _icon = icon;
            _label = label;
            _tag = tag;
        }

        /// <summary>
        /// Draws the navigation bar item.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The bounds to draw within.</param>
        public void Draw(SKCanvas canvas, SKRect bounds)
        {
            if (_navigationBar == null) return;

            using (var paint = new SKPaint())
            {
                // Calculate positions
                float centerX = bounds.MidX;
                float centerY = bounds.MidY;
                float iconY = _navigationBar.ShowLabels ?
                    centerY - (_navigationBar.LabelFontSize / 2) - 4 :
                    centerY;
                float labelY = centerY + (_navigationBar.LabelFontSize / 2) + 4;

                // Draw icon
                paint.Color = IsSelected ? _navigationBar.ActiveColor : _navigationBar.InactiveColor;
                paint.TextSize = _navigationBar.IconSize;
                paint.IsAntialias = true;
                paint.TextAlign = SKTextAlign.Center;

                // For demo purposes, we'll draw a simple symbol if no actual icon is provided
                string displayIcon = string.IsNullOrEmpty(_icon) ? "●" : _icon;
                canvas.DrawText(displayIcon, centerX, iconY, paint);

                // Draw label if enabled
                if (_navigationBar.ShowLabels && !string.IsNullOrEmpty(_label))
                {
                    paint.Color = IsSelected ? _navigationBar.ActiveColor : _navigationBar.InactiveColor;
                    paint.TextSize = _navigationBar.LabelFontSize;
                    paint.TextAlign = SKTextAlign.Center;
                    paint.Typeface = SKTypeface.FromFamilyName(null, IsSelected ? SKFontStyle.Bold : SKFontStyle.Normal);

                    canvas.DrawText(_label, centerX, labelY, paint);
                }

                // Draw badge if present
                if (HasBadge)
                {
                    float badgeRadius = 8f;
                    float badgeX = bounds.Right - badgeRadius - 4;
                    float badgeY = bounds.Top + badgeRadius + 4;

                    // Draw badge background
                    paint.Color = MaterialDesignColors.Error;
                    paint.Style = SKPaintStyle.Fill;
                    canvas.DrawCircle(badgeX, badgeY, badgeRadius, paint);

                    // Draw badge text
                    if (!string.IsNullOrEmpty(BadgeText))
                    {
                        paint.Color = MaterialDesignColors.OnError;
                        paint.TextSize = 10f;
                        paint.TextAlign = SKTextAlign.Center;
                        canvas.DrawText(BadgeText, badgeX, badgeY + 3, paint);
                    }
                }

                // Draw active indicator line
                if (IsSelected)
                {
                    float lineHeight = 3f;
                    float lineY = bounds.Bottom - lineHeight;

                    paint.Color = _navigationBar.ActiveColor;
                    paint.Style = SKPaintStyle.Fill;

                    var lineRect = new SKRect(bounds.Left + 8, lineY, bounds.Right - 8, bounds.Bottom);
                    canvas.DrawRect(lineRect, paint);
                }
            }
        }
    }
}

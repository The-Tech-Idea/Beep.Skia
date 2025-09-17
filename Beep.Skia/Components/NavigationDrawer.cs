using SkiaSharp;
using System;
using System.Collections.Generic;
using Beep.Skia.Model;
namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 Navigation Drawer component that slides in from the edge of the screen.
    /// </summary>
    public class NavigationDrawer : MaterialControl
    {
        private readonly List<NavigationDrawerBaseItem> _items = new List<NavigationDrawerBaseItem>();
        private NavigationDrawerItem _selectedItem;
        private NavigationDrawerHeader _header;
        private SKColor _backgroundColor;
        private SKColor _surfaceTintColor;
        private SKColor _selectedItemColor;
        private SKColor _unselectedItemColor;
        private float _itemHeight = 56f;
        private float _dividerHeight = 1f;
        private float _headerHeight = 168f;
        private bool _isOpen;
        private float _drawerWidth = 280f;
        private DrawerPosition _position = DrawerPosition.Left;
        private DrawerType _drawerType = DrawerType.Standard;

        /// <summary>
        /// Gets the list of navigation drawer items.
        /// </summary>
        public IReadOnlyList<NavigationDrawerBaseItem> Items => _items.AsReadOnly();

        /// <summary>
        /// Gets or sets the currently selected navigation drawer item.
        /// </summary>
        public NavigationDrawerItem SelectedItem
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
                    ItemSelected?.Invoke(this, new NavigationDrawerItemEventArgs(_selectedItem));
                }
            }
        }

        /// <summary>
        /// Gets or sets the header of the navigation drawer.
        /// </summary>
        public NavigationDrawerHeader Header
        {
            get => _header;
            set
            {
                _header = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the background color of the navigation drawer.
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
        /// Gets or sets the surface tint color of the navigation drawer.
        /// </summary>
        public SKColor SurfaceTintColor
        {
            get => _surfaceTintColor;
            set
            {
                _surfaceTintColor = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the color for selected navigation items.
        /// </summary>
        public SKColor SelectedItemColor
        {
            get => _selectedItemColor;
            set
            {
                _selectedItemColor = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the color for unselected navigation items.
        /// </summary>
        public SKColor UnselectedItemColor
        {
            get => _unselectedItemColor;
            set
            {
                _unselectedItemColor = value;
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
        /// Gets or sets the height of dividers.
        /// </summary>
        public float DividerHeight
        {
            get => _dividerHeight;
            set
            {
                _dividerHeight = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the height of the header.
        /// </summary>
        public float HeaderHeight
        {
            get => _headerHeight;
            set
            {
                _headerHeight = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets whether the navigation drawer is open.
        /// </summary>
        public bool IsOpen
        {
            get => _isOpen;
            set
            {
                if (_isOpen != value)
                {
                    _isOpen = value;
                    RefreshVisual();
                    if (_isOpen)
                    {
                        DrawerOpened?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        DrawerClosed?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the width of the navigation drawer.
        /// </summary>
        public float DrawerWidth
        {
            get => _drawerWidth;
            set
            {
                _drawerWidth = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the position of the navigation drawer.
        /// </summary>
        public DrawerPosition Position
        {
            get => _position;
            set
            {
                _position = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the type of the navigation drawer.
        /// </summary>
        public DrawerType DrawerType
        {
            get => _drawerType;
            set
            {
                _drawerType = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Occurs when a navigation item is selected.
        /// </summary>
        public event EventHandler<NavigationDrawerItemEventArgs> ItemSelected;

        /// <summary>
        /// Occurs when the navigation drawer is opened.
        /// </summary>
        public event EventHandler DrawerOpened;

        /// <summary>
        /// Occurs when the navigation drawer is closed.
        /// </summary>
        public event EventHandler DrawerClosed;

        /// <summary>
        /// Initializes a new instance of the NavigationDrawer class.
        /// </summary>
        public NavigationDrawer()
        {
            // Set default Material Design 3.0 colors
            BackgroundColor = MaterialDesignColors.SurfaceContainerHigh;
            SurfaceTintColor = MaterialDesignColors.Primary;
            SelectedItemColor = MaterialDesignColors.SecondaryContainer;
            UnselectedItemColor = MaterialDesignColors.OnSurface;

            // Set default size
            Width = DrawerWidth;
            Height = 100f; // Will be set by parent container
        }

        /// <summary>
        /// Adds a navigation drawer item.
        /// </summary>
        /// <param name="item">The navigation drawer item to add.</param>
        public void AddItem(NavigationDrawerBaseItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            _items.Add(item);
            if (item is NavigationDrawerItem navItem)
            {
                navItem.NavigationDrawer = this;
            }
            RefreshVisual();
        }

        /// <summary>
        /// Adds a navigation drawer item with the specified icon, label, and badge.
        /// </summary>
        /// <param name="icon">The icon for the navigation item.</param>
        /// <param name="label">The label text for the navigation item.</param>
        /// <param name="badge">The badge text for the navigation item.</param>
        /// <param name="tag">Optional tag object for the item.</param>
        public NavigationDrawerItem AddItem(string icon, string label, string badge = null, object tag = null)
        {
            var item = new NavigationDrawerItem(icon, label, badge, tag);
            AddItem(item);
            return item;
        }

        /// <summary>
        /// Adds a divider to the navigation drawer.
        /// </summary>
        public void AddDivider()
        {
            _items.Add(new NavigationDrawerDivider());
            RefreshVisual();
        }

        /// <summary>
        /// Removes a navigation drawer item.
        /// </summary>
        /// <param name="item">The navigation drawer item to remove.</param>
        public void RemoveItem(NavigationDrawerBaseItem item)
        {
            if (item == null) return;

            if (item is NavigationDrawerItem navItem && _selectedItem == navItem)
            {
                SelectedItem = null;
            }

            _items.Remove(item);
            if (item is NavigationDrawerItem navItem2)
            {
                navItem2.NavigationDrawer = null;
            }
            RefreshVisual();
        }

        /// <summary>
        /// Clears all navigation drawer items.
        /// </summary>
        public void ClearItems()
        {
            foreach (var item in _items)
            {
                if (item is NavigationDrawerItem navItem)
                {
                    navItem.NavigationDrawer = null;
                }
            }

            _items.Clear();
            SelectedItem = null;
            RefreshVisual();
        }

        /// <summary>
        /// Opens the navigation drawer.
        /// </summary>
        public void Open()
        {
            IsOpen = true;
        }

        /// <summary>
        /// Closes the navigation drawer.
        /// </summary>
        public void Close()
        {
            IsOpen = false;
        }

        /// <summary>
        /// Toggles the navigation drawer open/closed state.
        /// </summary>
        public void Toggle()
        {
            IsOpen = !IsOpen;
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            if (!IsOpen) return;

            // Calculate drawer bounds based on position
            SKRect drawerBounds = CalculateDrawerBounds(new SKRect(X, Y, X + Width, Y + Height));

            // Draw scrim for modal drawer
            if (DrawerType == DrawerType.Modal)
            {
                using (var scrimPaint = new SKPaint())
                {
                    scrimPaint.Color = MaterialDesignColors.OnSurface.WithAlpha(32); // Semi-transparent scrim
                    scrimPaint.Style = SKPaintStyle.Fill;
                    canvas.DrawRect(Bounds, scrimPaint);
                }
            }

            // Draw drawer background
            using (var backgroundPaint = new SKPaint())
            {
                backgroundPaint.Color = BackgroundColor;
                backgroundPaint.Style = SKPaintStyle.Fill;
                canvas.DrawRect(drawerBounds, backgroundPaint);
            }

            // Draw header if present
            float currentY = drawerBounds.Top;
            if (Header != null)
            {
                var headerBounds = new SKRect(drawerBounds.Left, currentY, drawerBounds.Right, currentY + HeaderHeight);
                Header.Draw(canvas, headerBounds);
                currentY += HeaderHeight;
            }

            // Draw navigation items
            foreach (var item in _items)
            {
                if (item is NavigationDrawerItem navItem)
                {
                    var itemBounds = new SKRect(drawerBounds.Left, currentY, drawerBounds.Right, currentY + ItemHeight);
                    navItem.Draw(canvas, itemBounds);
                    currentY += ItemHeight;
                }
                else if (item is NavigationDrawerDivider divider)
                {
                    var dividerBounds = new SKRect(drawerBounds.Left + 16, currentY, drawerBounds.Right - 16, currentY + DividerHeight);
                    divider.Draw(canvas, dividerBounds);
                    currentY += DividerHeight + 8; // Add some spacing after divider
                }
            }
        }

        private SKRect CalculateDrawerBounds(SKRect parentBounds)
        {
            switch (Position)
            {
                case DrawerPosition.Left:
                    return new SKRect(parentBounds.Left, parentBounds.Top, parentBounds.Left + DrawerWidth, parentBounds.Bottom);
                case DrawerPosition.Right:
                    return new SKRect(parentBounds.Right - DrawerWidth, parentBounds.Top, parentBounds.Right, parentBounds.Bottom);
                default:
                    return new SKRect(parentBounds.Left, parentBounds.Top, parentBounds.Left + DrawerWidth, parentBounds.Bottom);
            }
        }

        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            base.OnMouseDown(point, context);

            if (!IsOpen) return false;

            SKRect drawerBounds = CalculateDrawerBounds(Bounds);

            // Check if click is outside drawer (for modal drawer)
            if (DrawerType == DrawerType.Modal && !drawerBounds.Contains(point))
            {
                Close();
                return true;
            }

            // Handle item clicks
            if (drawerBounds.Contains(point))
            {
                float currentY = drawerBounds.Top;
                if (Header != null)
                {
                    currentY += HeaderHeight;
                }

                foreach (var item in _items)
                {
                    if (item is NavigationDrawerItem navItem)
                    {
                        var itemBounds = new SKRect(drawerBounds.Left, currentY, drawerBounds.Right, currentY + ItemHeight);
                        if (itemBounds.Contains(point))
                        {
                            SelectedItem = navItem;
                            if (DrawerType == DrawerType.Modal)
                            {
                                Close();
                            }
                            break;
                        }
                        currentY += ItemHeight;
                    }
                    else if (item is NavigationDrawerDivider)
                    {
                        currentY += DividerHeight + 8;
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
        /// Navigation drawer item event arguments.
        /// </summary>
        public class NavigationDrawerItemEventArgs : EventArgs
        {
            /// <summary>
            /// Gets the navigation drawer item.
            /// </summary>
            public NavigationDrawerItem Item { get; }

            /// <summary>
            /// Initializes a new instance of the NavigationDrawerItemEventArgs class.
            /// </summary>
            /// <param name="item">The navigation drawer item.</param>
            public NavigationDrawerItemEventArgs(NavigationDrawerItem item)
            {
                Item = item;
            }
        }
    }

    /// <summary>
    /// Specifies the position of the navigation drawer.
    /// </summary>
    public enum DrawerPosition
    {
        /// <summary>
        /// The drawer slides in from the left edge.
        /// </summary>
        Left,

        /// <summary>
        /// The drawer slides in from the right edge.
        /// </summary>
        Right
    }

    /// <summary>
    /// Specifies the type of the navigation drawer.
    /// </summary>
    public enum DrawerType
    {
        /// <summary>
        /// Standard navigation drawer that pushes content.
        /// </summary>
        Standard,

        /// <summary>
        /// Modal navigation drawer that overlays content with a scrim.
        /// </summary>
        Modal
    }

    /// <summary>
    /// Base class for navigation drawer items.
    /// </summary>
    public abstract class NavigationDrawerBaseItem
    {
        /// <summary>
        /// Draws the navigation drawer item.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The bounds to draw within.</param>
        public abstract void Draw(SKCanvas canvas, SKRect bounds);
    }

    /// <summary>
    /// Represents a navigation item in a navigation drawer.
    /// </summary>
    public class NavigationDrawerItem : NavigationDrawerBaseItem
    {
        private string _icon;
        private string _label;
        private string _badge;
        private object _tag;
        private NavigationDrawer _navigationDrawer;
        private bool _isSelected;
        private bool _isHovered;

        /// <summary>
        /// Gets or sets the icon of the navigation item.
        /// </summary>
        public string Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                _navigationDrawer?.RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the label text of the navigation item.
        /// </summary>
        public string Label
        {
            get => _label;
            set
            {
                _label = value;
                _navigationDrawer?.RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the badge text of the navigation item.
        /// </summary>
        public string Badge
        {
            get => _badge;
            set
            {
                _badge = value;
                _navigationDrawer?.RefreshVisual();
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
        /// Gets the navigation drawer that contains this item.
        /// </summary>
        public NavigationDrawer NavigationDrawer
        {
            get => _navigationDrawer;
            internal set => _navigationDrawer = value;
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
                _navigationDrawer?.RefreshVisual();
            }
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
        /// Initializes a new instance of the NavigationDrawerItem class.
        /// </summary>
        /// <param name="icon">The icon for the navigation item.</param>
        /// <param name="label">The label text for the navigation item.</param>
        /// <param name="badge">The badge text for the navigation item.</param>
        /// <param name="tag">Optional tag object for the item.</param>
        public NavigationDrawerItem(string icon, string label, string badge = null, object tag = null)
        {
            _icon = icon;
            _label = label;
            _badge = badge;
            _tag = tag;
        }

        /// <summary>
        /// Draws the navigation drawer item.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The bounds to draw within.</param>
        public override void Draw(SKCanvas canvas, SKRect bounds)
        {
            using (var paint = new SKPaint())
            {
                // Background
                if (IsSelected)
                    paint.Color = _navigationDrawer?.SelectedItemColor ?? MaterialDesignColors.SecondaryContainer;
                else if (IsHovered)
                    paint.Color = MaterialDesignColors.OnSurface.WithAlpha(8);
                else
                    paint.Color = SKColors.Transparent;
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawRect(bounds, paint);

                // Icon
                if (!string.IsNullOrEmpty(Icon))
                {
                    paint.Color = IsSelected ?
                        MaterialDesignColors.OnSecondaryContainer :
                        _navigationDrawer?.UnselectedItemColor ?? MaterialDesignColors.OnSurfaceVariant;
                    paint.IsAntialias = true;
                    using (var font = new SKFont(SKTypeface.FromFamilyName(null, SKFontStyle.Normal), 24f))
                    {
                        float iconX = bounds.Left + 16;
                        float iconY = bounds.Top + (bounds.Height + 12) / 2;
                        canvas.DrawText(Icon, iconX, iconY, SKTextAlign.Left, font, paint);
                    }
                }

                // Label
                if (!string.IsNullOrEmpty(Label))
                {
                    paint.Color = IsSelected ?
                        MaterialDesignColors.OnSecondaryContainer :
                        _navigationDrawer?.UnselectedItemColor ?? MaterialDesignColors.OnSurface;
                    paint.IsAntialias = true;
                    using (var font = new SKFont(SKTypeface.FromFamilyName(null, IsSelected ? SKFontStyle.Bold : SKFontStyle.Normal), 16f))
                    {
                        float labelX = bounds.Left + 72;
                        float labelY = bounds.Top + (bounds.Height + 6) / 2;
                        canvas.DrawText(Label, labelX, labelY, SKTextAlign.Left, font, paint);
                    }
                }

                // Badge
                if (!string.IsNullOrEmpty(Badge))
                {
                    float badgeRadius = 8f;
                    float badgeX = bounds.Right - 24;
                    float badgeY = bounds.Top + 16;
                    paint.Color = MaterialDesignColors.Error;
                    paint.Style = SKPaintStyle.Fill;
                    canvas.DrawCircle(badgeX, badgeY, badgeRadius, paint);
                    paint.Color = MaterialDesignColors.OnError;
                    using (var font = new SKFont(SKTypeface.FromFamilyName(null, SKFontStyle.Bold), 12f))
                    {
                        canvas.DrawText(Badge, badgeX, badgeY + 4, SKTextAlign.Center, font, paint);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Represents a divider in a navigation drawer.
    /// </summary>
    public class NavigationDrawerDivider : NavigationDrawerBaseItem
    {
        public override void Draw(SKCanvas canvas, SKRect bounds)
        {
            using (var paint = new SKPaint())
            {
                paint.Color = MaterialDesignColors.OutlineVariant;
                paint.StrokeWidth = 1;
                paint.Style = SKPaintStyle.Stroke;
                float centerY = bounds.MidY;
                canvas.DrawLine(bounds.Left, centerY, bounds.Right, centerY, paint);
            }
        }
    }

    /// <summary>
    /// Represents the header of a navigation drawer.
    /// </summary>
    public class NavigationDrawerHeader
    {
        private string _title;
        private string _subtitle;
        private string _avatarIcon;
        private SKColor _backgroundColor;
        private SKColor _titleColor;
        private SKColor _subtitleColor;

        /// <summary>
        /// Gets or sets the title text of the header.
        /// </summary>
        public string Title
        {
            get => _title;
            set => _title = value;
        }

        /// <summary>
        /// Gets or sets the subtitle text of the header.
        /// </summary>
        public string Subtitle
        {
            get => _subtitle;
            set => _subtitle = value;
        }

        /// <summary>
        /// Gets or sets the avatar icon of the header.
        /// </summary>
        public string AvatarIcon
        {
            get => _avatarIcon;
            set => _avatarIcon = value;
        }

        /// <summary>
        /// Gets or sets the background color of the header.
        /// </summary>
        public SKColor BackgroundColor
        {
            get => _backgroundColor;
            set => _backgroundColor = value;
        }

        /// <summary>
        /// Gets or sets the title text color of the header.
        /// </summary>
        public SKColor TitleColor
        {
            get => _titleColor;
            set => _titleColor = value;
        }

        /// <summary>
        /// Gets or sets the subtitle text color of the header.
        /// </summary>
        public SKColor SubtitleColor
        {
            get => _subtitleColor;
            set => _subtitleColor = value;
        }

        /// <summary>
        /// Initializes a new instance of the NavigationDrawerHeader class.
        /// </summary>
        public NavigationDrawerHeader()
        {
            // Set default colors
            BackgroundColor = MaterialDesignColors.Primary;
            TitleColor = MaterialDesignColors.OnPrimary;
            SubtitleColor = MaterialDesignColors.OnPrimary.WithAlpha(179); // 70% opacity
        }

        /// <summary>
        /// Initializes a new instance of the NavigationDrawerHeader class.
        /// </summary>
        /// <param name="title">The title text.</param>
        /// <param name="subtitle">The subtitle text.</param>
        /// <param name="avatarIcon">The avatar icon.</param>
        public NavigationDrawerHeader(string title, string subtitle = null, string avatarIcon = null)
            : this()
        {
            _title = title;
            _subtitle = subtitle;
            _avatarIcon = avatarIcon;
        }

        /// <summary>
        /// Draws the navigation drawer header.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The bounds to draw within.</param>
        public void Draw(SKCanvas canvas, SKRect bounds)
        {
            using (var paint = new SKPaint())
            {
                // Draw background
                paint.Color = BackgroundColor;
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawRect(bounds, paint);

                // Draw avatar
                if (!string.IsNullOrEmpty(AvatarIcon))
                {
                    paint.Color = TitleColor;
                    paint.IsAntialias = true;
                    using (var font = new SKFont(SKTypeface.FromFamilyName(null, SKFontStyle.Normal), 40f))
                    {
                        float avatarX = bounds.Left + 16;
                        float avatarY = bounds.Top + 80;
                        canvas.DrawText(AvatarIcon, avatarX, avatarY, SKTextAlign.Left, font, paint);
                    }
                }

                // Draw title
                if (!string.IsNullOrEmpty(Title))
                {
                    paint.Color = TitleColor;
                    paint.IsAntialias = true;
                    using (var font = new SKFont(SKTypeface.FromFamilyName(null, SKFontStyle.Normal), 20f))
                    {
                        float titleX = bounds.Left + 16 + (string.IsNullOrEmpty(AvatarIcon) ? 0 : 56);
                        float titleY = bounds.Top + 60;
                        canvas.DrawText(Title, titleX, titleY, SKTextAlign.Left, font, paint);
                    }
                }

                // Draw subtitle
                if (!string.IsNullOrEmpty(Subtitle))
                {
                    paint.Color = SubtitleColor;
                    paint.IsAntialias = true;
                    using (var font = new SKFont(SKTypeface.FromFamilyName(null, SKFontStyle.Normal), 14f))
                    {
                        float subtitleX = bounds.Left + 16 + (string.IsNullOrEmpty(AvatarIcon) ? 0 : 56);
                        float subtitleY = bounds.Top + 90;
                        canvas.DrawText(Subtitle, subtitleX, subtitleY, SKTextAlign.Left, font, paint);
                    }
                }
            }
        }
    }
}

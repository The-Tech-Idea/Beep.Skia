using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Timers;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 Cascading Menu component that supports nested submenus.
    /// </summary>
    public class CascadingMenu : Menu
    {
        private readonly List<CascadingMenuItem> _items = new List<CascadingMenuItem>();
        private CascadingMenuItem _hoveredItem;
        private Timer _submenuTimer;
        private SKPoint _currentMousePosition;
        private const int SUBMENU_DELAY = 300; // milliseconds

        /// <summary>
        /// Gets the list of cascading menu items.
        /// </summary>
    public new IReadOnlyList<CascadingMenuItem> Items => _items.AsReadOnly();

        /// <summary>
        /// Gets or sets the delay before showing submenus (in milliseconds).
        /// </summary>
        public int SubmenuDelay { get; set; } = SUBMENU_DELAY;

        /// <summary>
        /// Occurs when a submenu is about to be shown.
        /// </summary>
        public event EventHandler<CascadingMenuEventArgs> SubmenuShowing;

        /// <summary>
        /// Occurs when a submenu is hidden.
        /// </summary>
        public event EventHandler<CascadingMenuEventArgs> SubmenuHidden;

        /// <summary>
        /// Initializes a new instance of the CascadingMenu class.
        /// </summary>
        public CascadingMenu()
        {
            _submenuTimer = new Timer();
            _submenuTimer.Elapsed += OnSubmenuTimerElapsed;
        }

        /// <summary>
        /// Adds a cascading menu item.
        /// </summary>
        /// <param name="item">The cascading menu item to add.</param>
        public void AddItem(CascadingMenuItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            _items.Add(item);
            item.ParentMenu = this;
            InvalidateVisual();
        }

        /// <summary>
        /// Adds a cascading menu item with the specified text and submenu.
        /// </summary>
        /// <param name="text">The text for the menu item.</param>
        /// <param name="submenu">The submenu to display when hovered.</param>
        /// <param name="icon">The icon for the menu item.</param>
        /// <param name="shortcut">The keyboard shortcut.</param>
        public CascadingMenuItem AddItem(string text, CascadingMenu submenu = null, string icon = "", string shortcut = "")
        {
            var item = new CascadingMenuItem(text, submenu, icon, shortcut);
            AddItem(item);
            return item;
        }

        /// <summary>
        /// Removes a cascading menu item.
        /// </summary>
        /// <param name="item">The cascading menu item to remove.</param>
        public void RemoveItem(CascadingMenuItem item)
        {
            if (item == null) return;

            if (_hoveredItem == item)
            {
                HideSubmenu();
            }

            _items.Remove(item);
            item.ParentMenu = null;
            InvalidateVisual();
        }

        /// <summary>
        /// Clears all cascading menu items.
        /// </summary>
    public new void ClearItems()
        {
            HideSubmenu();

            foreach (var item in _items)
            {
                item.ParentMenu = null;
            }

            _items.Clear();
            InvalidateVisual();
        }

        /// <summary>
        /// Shows the submenu for the specified item.
        /// </summary>
        /// <param name="item">The menu item whose submenu to show.</param>
        public void ShowSubmenu(CascadingMenuItem item)
        {
            if (item == null || item.Submenu == null) return;

            HideSubmenu();

            _hoveredItem = item;

            // Calculate submenu position
            var itemBounds = GetItemBounds(item);
            var submenuPosition = new SKPoint(itemBounds.Right, itemBounds.Top);

            // Raise event before showing
            var args = new CascadingMenuEventArgs(item, item.Submenu);
            SubmenuShowing?.Invoke(this, args);

            // Show submenu
            item.Submenu.Show(submenuPosition);
        }

        /// <summary>
        /// Hides the currently shown submenu.
        /// </summary>
        public void HideSubmenu()
        {
            if (_hoveredItem?.Submenu != null)
            {
                var args = new CascadingMenuEventArgs(_hoveredItem, _hoveredItem.Submenu);
                SubmenuHidden?.Invoke(this, args);

                _hoveredItem.Submenu.Hide();
            }

            _hoveredItem = null;
            _submenuTimer.Stop();
        }

        protected override bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            _currentMousePosition = point;
            base.OnMouseMove(point, context);

            // Find hovered item
            CascadingMenuItem hoveredItem = null;
            foreach (var item in _items)
            {
                var itemBounds = GetItemBounds(item);
                if (itemBounds.Contains(point))
                {
                    hoveredItem = item;
                    break;
                }
            }

            // Handle submenu showing/hiding
            if (hoveredItem != _hoveredItem)
            {
                if (hoveredItem?.Submenu != null)
                {
                    // Start timer to show submenu
                    _submenuTimer.Stop();
                    _submenuTimer.Interval = SubmenuDelay;
                    _submenuTimer.Start();
                }
                else
                {
                    // Hide current submenu immediately
                    HideSubmenu();
                }
            }

            return true;
        }

        protected override void OnMouseLeave()
        {
            base.OnMouseLeave();
            HideSubmenu();
        }

        private void OnSubmenuTimerElapsed(object sender, EventArgs e)
        {
            _submenuTimer.Stop();

            // Find the currently hovered item
            foreach (var item in _items)
            {
                var itemBounds = GetItemBounds(item);
                if (itemBounds.Contains(_currentMousePosition))
                {
                    ShowSubmenu(item);
                    break;
                }
            }
        }

        private SKRect GetItemBounds(CascadingMenuItem item)
        {
            float y = 0;
            foreach (var currentItem in _items)
            {
                if (currentItem == item)
                {
                    return new SKRect(0, y, Width, y + currentItem.Height);
                }
                y += currentItem.Height;
            }

            return SKRect.Empty;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _submenuTimer?.Dispose();
                _submenuTimer = null;

                foreach (var item in _items)
                {
                    item.ParentMenu = null;
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Cascading menu event arguments.
        /// </summary>
        public class CascadingMenuEventArgs : EventArgs
        {
            /// <summary>
            /// Gets the menu item.
            /// </summary>
            public CascadingMenuItem Item { get; }

            /// <summary>
            /// Gets the submenu.
            /// </summary>
            public CascadingMenu Submenu { get; }

            /// <summary>
            /// Initializes a new instance of the CascadingMenuEventArgs class.
            /// </summary>
            /// <param name="item">The menu item.</param>
            /// <param name="submenu">The submenu.</param>
            public CascadingMenuEventArgs(CascadingMenuItem item, CascadingMenu submenu)
            {
                Item = item;
                Submenu = submenu;
            }
        }
    }

    /// <summary>
    /// Represents an item in a cascading menu that can have a submenu.
    /// </summary>
    public class CascadingMenuItem : MaterialControl
    {
        private string _text;
        private CascadingMenu _submenu;
        private CascadingMenu _parentMenu;
        private string _icon = "";
        private string _shortcut = "";
        private bool _isHovered;
        private bool _isSelected;

        /// <summary>
        /// Gets or sets the text of the menu item.
        /// </summary>
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the submenu associated with this item.
        /// </summary>
        public CascadingMenu Submenu
        {
            get => _submenu;
            set
            {
                _submenu = value;
                if (_submenu != null)
                {
                    _submenu.Position = Menu.MenuPosition.BottomRight;
                }
            }
        }

        /// <summary>
        /// Gets or sets the parent menu that contains this item.
        /// </summary>
        public CascadingMenu ParentMenu
        {
            get => _parentMenu;
            set => _parentMenu = value;
        }

        /// <summary>
        /// Gets or sets the icon for the menu item.
        /// </summary>
        public string Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the keyboard shortcut.
        /// </summary>
        public string Shortcut
        {
            get => _shortcut;
            set
            {
                _shortcut = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets whether the item is hovered.
        /// </summary>
        public bool IsHovered
        {
            get => _isHovered;
            set
            {
                _isHovered = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets whether the item is selected.
        /// </summary>
    public new bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets whether this item has a submenu.
        /// </summary>
        public bool HasSubmenu => _submenu != null;

        /// <summary>
        /// Occurs when the menu item is clicked.
        /// </summary>
    public new event EventHandler Clicked;

        /// <summary>
        /// Initializes a new instance of the CascadingMenuItem class.
        /// </summary>
        /// <param name="text">The text of the menu item.</param>
        /// <param name="submenu">The submenu to display when hovered.</param>
        /// <param name="icon">The icon for the menu item.</param>
        /// <param name="shortcut">The keyboard shortcut.</param>
        public CascadingMenuItem(string text, CascadingMenu submenu = null, string icon = "", string shortcut = "")
        {
            _text = text;
            _submenu = submenu;
            _icon = icon;
            _shortcut = shortcut;

            Width = 200f;
            Height = 32f;
        }

        /// <summary>
        /// Draws the cascading menu item.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The bounds to draw within.</param>
        public void Draw(SKCanvas canvas, SKRect bounds)
        {
            // Set the bounds for drawing
            X = bounds.Left;
            Y = bounds.Top;
            Width = bounds.Width;
            Height = bounds.Height;

            Draw(canvas, bounds);
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            var bounds = Bounds;

            // Draw background based on state
            using (var bgPaint = new SKPaint())
            {
                bgPaint.IsAntialias = true;

                if (IsSelected || IsHovered)
                {
                    bgPaint.Color = IsSelected ? MaterialColors.OnSurface.WithAlpha(12) :
                                               MaterialColors.OnSurface.WithAlpha(8);
                    bgPaint.Style = SKPaintStyle.Fill;
                    canvas.DrawRect(bounds, bgPaint);
                }
            }

            // Draw content (modern text rendering)
            float leftPadding = 12f;
            float rightPadding = 32f;

            using var iconFont = new SKFont(SKTypeface.Default, 16f);
            using var textFont = new SKFont(SKTypeface.Default, 14f);
            using var shortcutFont = new SKFont(SKTypeface.Default, 12f);
            using var arrowFont = new SKFont(SKTypeface.Default, 12f);

            float baseline = bounds.Top + (bounds.Height + textFont.Metrics.CapHeight) / 2f;

            float cursorX = bounds.Left + leftPadding;

            using var paint = new SKPaint { IsAntialias = true };

            // Icon
            if (!string.IsNullOrEmpty(Icon))
            {
                paint.Color = MaterialColors.OnSurfaceVariant;
                float iconBaseline = bounds.Top + (bounds.Height + iconFont.Metrics.CapHeight) / 2f;
                canvas.DrawText(Icon, cursorX, iconBaseline, SKTextAlign.Left, iconFont, paint);
                cursorX += iconFont.MeasureText(Icon) + 8f;
            }

            // Text
            paint.Color = MaterialColors.OnSurface;
            canvas.DrawText(Text, cursorX, baseline, SKTextAlign.Left, textFont, paint);
            float textWidth = textFont.MeasureText(Text);

            // Shortcut
            float arrowWidth = HasSubmenu ? arrowFont.MeasureText("▶") + 8f : 0f;
            float shortcutWidth = 0f;
            if (!string.IsNullOrEmpty(Shortcut))
            {
                shortcutWidth = shortcutFont.MeasureText(Shortcut);
                paint.Color = MaterialColors.OnSurfaceVariant;
                float shortcutBaseline = bounds.Top + (bounds.Height + shortcutFont.Metrics.CapHeight) / 2f;
                float shortcutX = bounds.Right - rightPadding - arrowWidth - shortcutWidth;
                canvas.DrawText(Shortcut, shortcutX, shortcutBaseline, SKTextAlign.Left, shortcutFont, paint);
            }

            // Arrow
            if (HasSubmenu)
            {
                paint.Color = MaterialColors.OnSurfaceVariant;
                float arrowBaseline = bounds.Top + (bounds.Height + arrowFont.Metrics.CapHeight) / 2f;
                float arrowX = bounds.Right - 20 - arrowFont.MeasureText("▶") / 2f; // maintain original spacing feel
                canvas.DrawText("▶", arrowX, arrowBaseline, SKTextAlign.Left, arrowFont, paint);
            }
        }

        /// <summary>
        /// Creates a separator cascading menu item.
        /// </summary>
        public static CascadingMenuItem Separator()
        {
            return new CascadingMenuItem("", null, "", "");
        }
    }

    /// <summary>
    /// Simple timer class for submenu delays.
    /// </summary>
    internal class Timer : IDisposable
    {
        private System.Timers.Timer _timer;

        public double Interval
        {
            get => _timer?.Interval ?? 0;
            set
            {
                if (_timer != null)
                {
                    _timer.Interval = value;
                }
            }
        }

        public event EventHandler Elapsed;

        public Timer()
        {
            _timer = new System.Timers.Timer();
            _timer.Elapsed += (s, e) => Elapsed?.Invoke(this, EventArgs.Empty);
            _timer.AutoReset = false;
        }

        public void Start()
        {
            _timer?.Start();
        }

        public void Stop()
        {
            _timer?.Stop();
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _timer = null;
        }
    }
}

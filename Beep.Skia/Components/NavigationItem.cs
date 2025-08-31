using System;
using SkiaSharp;

namespace Beep.Skia.Components
{
    /// <summary>
    /// Represents an item in a navigation component
    /// </summary>
    public class NavigationItem
    {
        private string _text = "";
        private string _icon = "";
        private SKColor _textColor = MaterialDesignColors.OnSurface;
        private SKColor _iconColor = MaterialDesignColors.OnSurface;
        private SKColor _backgroundColor = SKColors.Transparent;
        private SKColor _selectedBackgroundColor = MaterialDesignColors.PrimaryContainer;
        private SKColor _hoverBackgroundColor = MaterialDesignColors.SurfaceVariant;
        private float _height = 48;
        private bool _isVisible = true;
        private bool _isEnabled = true;
        private bool _isSelected = false;
        private object _tag;
        private bool _isHovered = false;
        private bool _isPressed = false;

        /// <summary>
        /// Gets or sets the item text
        /// </summary>
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value ?? "";
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the icon name or path
        /// </summary>
        public string Icon
        {
            get => _icon;
            set
            {
                if (_icon != value)
                {
                    _icon = value ?? "";
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the text color
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
        /// Gets or sets the icon color
        /// </summary>
        public SKColor IconColor
        {
            get => _iconColor;
            set
            {
                if (_iconColor != value)
                {
                    _iconColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the background color
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
        /// Gets or sets the selected background color
        /// </summary>
        public SKColor SelectedBackgroundColor
        {
            get => _selectedBackgroundColor;
            set
            {
                if (_selectedBackgroundColor != value)
                {
                    _selectedBackgroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the hover background color
        /// </summary>
        public SKColor HoverBackgroundColor
        {
            get => _hoverBackgroundColor;
            set
            {
                if (_hoverBackgroundColor != value)
                {
                    _hoverBackgroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the item height
        /// </summary>
        public float Height
        {
            get => _height;
            set
            {
                if (_height != value)
                {
                    _height = Math.Max(16, value);
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the item is visible
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the item is enabled
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the item is selected
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the tag object
        /// </summary>
        public object Tag
        {
            get => _tag;
            set => _tag = value;
        }

        /// <summary>
        /// Gets whether the item is currently hovered
        /// </summary>
        public bool IsHovered
        {
            get => _isHovered;
            internal set
            {
                if (_isHovered != value)
                {
                    _isHovered = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets whether the item is currently pressed
        /// </summary>
        public bool IsPressed
        {
            get => _isPressed;
            internal set
            {
                if (_isPressed != value)
                {
                    _isPressed = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets the parent NavigationBar
        /// </summary>
        public NavigationBar ParentNavigationBar { get; internal set; }

        /// <summary>
        /// Occurs when the item is clicked
        /// </summary>
        public event EventHandler Click;

        /// <summary>
        /// Initializes a new instance of the NavigationItem class
        /// </summary>
        public NavigationItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the NavigationItem class with text
        /// </summary>
        public NavigationItem(string text)
        {
            _text = text ?? "";
        }

        /// <summary>
        /// Initializes a new instance of the NavigationItem class with text and icon
        /// </summary>
        public NavigationItem(string text, string icon)
        {
            _text = text ?? "";
            _icon = icon ?? "";
        }

        /// <summary>
        /// Invalidates the visual representation
        /// </summary>
        protected virtual void InvalidateVisual()
        {
            ParentNavigationBar?.InvalidateVisual();
        }

        /// <summary>
        /// Raises the Click event
        /// </summary>
        public virtual void PerformClick()
        {
            if (_isEnabled)
            {
                Click?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// A collection of NavigationItem objects
    /// </summary>
    public class NavigationItemCollection : System.Collections.ObjectModel.Collection<NavigationItem>
    {
        private NavigationBar _parentNavigationBar;

        /// <summary>
        /// Initializes a new instance of the NavigationItemCollection class
        /// </summary>
        public NavigationItemCollection(NavigationBar parentNavigationBar)
        {
            _parentNavigationBar = parentNavigationBar;
        }

        /// <summary>
        /// Adds a NavigationItem to the collection
        /// </summary>
        public void Add(string text)
        {
            Add(new NavigationItem(text));
        }

        /// <summary>
        /// Adds a NavigationItem to the collection with text and icon
        /// </summary>
        public void Add(string text, string icon)
        {
            Add(new NavigationItem(text, icon));
        }

        /// <summary>
        /// Inserts an item into the collection at the specified index
        /// </summary>
        protected override void InsertItem(int index, NavigationItem item)
        {
            if (item != null)
            {
                item.ParentNavigationBar = _parentNavigationBar;
            }
            base.InsertItem(index, item);
            _parentNavigationBar?.InvalidateVisual();
        }

        /// <summary>
        /// Removes an item from the collection at the specified index
        /// </summary>
        protected override void RemoveItem(int index)
        {
            var item = this[index];
            if (item != null)
            {
                item.ParentNavigationBar = null;
            }
            base.RemoveItem(index);
            _parentNavigationBar?.InvalidateVisual();
        }

        /// <summary>
        /// Replaces an item in the collection at the specified index
        /// </summary>
        protected override void SetItem(int index, NavigationItem item)
        {
            var oldItem = this[index];
            if (oldItem != null)
            {
                oldItem.ParentNavigationBar = null;
            }

            if (item != null)
            {
                item.ParentNavigationBar = _parentNavigationBar;
            }

            base.SetItem(index, item);
            _parentNavigationBar?.InvalidateVisual();
        }

        /// <summary>
        /// Clears all items from the collection
        /// </summary>
        protected override void ClearItems()
        {
            foreach (var item in this)
            {
                if (item != null)
                {
                    item.ParentNavigationBar = null;
                }
            }
            base.ClearItems();
            _parentNavigationBar?.InvalidateVisual();
        }
    }
}

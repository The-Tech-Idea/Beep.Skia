using System;
using SkiaSharp;

namespace Beep.Skia.Components
{
    /// <summary>
    /// Represents an item in a tool bar
    /// </summary>
    public class ToolBarItem
    {
        private string _text = "";
        private string _tooltip = "";
        private SKColor _textColor = MaterialDesignColors.OnSurface;
        private SKColor _backgroundColor = SKColors.Transparent;
        private SKColor _hoverBackgroundColor = MaterialDesignColors.SurfaceVariant;
        private float _width = 80;
        private bool _isVisible = true;
        private bool _isEnabled = true;
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
        /// Gets or sets the tooltip text
        /// </summary>
        public string Tooltip
        {
            get => _tooltip;
            set => _tooltip = value ?? "";
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
        /// Gets or sets the item width
        /// </summary>
        public float Width
        {
            get => _width;
            set
            {
                if (_width != value)
                {
                    _width = Math.Max(0, value);
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
        /// Gets the parent ToolBar
        /// </summary>
        public ToolBar ParentToolBar { get; internal set; }

        /// <summary>
        /// Occurs when the item is clicked
        /// </summary>
        public event EventHandler Click;

        /// <summary>
        /// Initializes a new instance of the ToolBarItem class
        /// </summary>
        public ToolBarItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the ToolBarItem class with text
        /// </summary>
        public ToolBarItem(string text)
        {
            _text = text ?? "";
        }

        /// <summary>
        /// Invalidates the visual representation
        /// </summary>
        protected virtual void InvalidateVisual()
        {
            ParentToolBar?.InvalidateVisual();
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
    /// A collection of ToolBarItem objects
    /// </summary>
    public class ToolBarItemCollection : System.Collections.ObjectModel.Collection<ToolBarItem>
    {
        private ToolBar _parentToolBar;

        /// <summary>
        /// Initializes a new instance of the ToolBarItemCollection class
        /// </summary>
        public ToolBarItemCollection(ToolBar parentToolBar)
        {
            _parentToolBar = parentToolBar;
        }

        /// <summary>
        /// Adds a ToolBarItem to the collection
        /// </summary>
        public void Add(string text)
        {
            Add(new ToolBarItem(text));
        }

        /// <summary>
        /// Adds a ToolBarItem to the collection with specified properties
        /// </summary>
        public void Add(string text, float width)
        {
            var item = new ToolBarItem(text) { Width = width };
            Add(item);
        }

        /// <summary>
        /// Inserts an item into the collection at the specified index
        /// </summary>
        protected override void InsertItem(int index, ToolBarItem item)
        {
            if (item != null)
            {
                item.ParentToolBar = _parentToolBar;
            }
            base.InsertItem(index, item);
            _parentToolBar?.InvalidateVisual();
        }

        /// <summary>
        /// Removes an item from the collection at the specified index
        /// </summary>
        protected override void RemoveItem(int index)
        {
            var item = this[index];
            if (item != null)
            {
                item.ParentToolBar = null;
            }
            base.RemoveItem(index);
            _parentToolBar?.InvalidateVisual();
        }

        /// <summary>
        /// Replaces an item in the collection at the specified index
        /// </summary>
        protected override void SetItem(int index, ToolBarItem item)
        {
            var oldItem = this[index];
            if (oldItem != null)
            {
                oldItem.ParentToolBar = null;
            }

            if (item != null)
            {
                item.ParentToolBar = _parentToolBar;
            }

            base.SetItem(index, item);
            _parentToolBar?.InvalidateVisual();
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
                    item.ParentToolBar = null;
                }
            }
            base.ClearItems();
            _parentToolBar?.InvalidateVisual();
        }
    }
}

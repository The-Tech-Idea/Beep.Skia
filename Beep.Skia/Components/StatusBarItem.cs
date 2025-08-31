using System;
using SkiaSharp;

namespace Beep.Skia.Components
{
    /// <summary>
    /// Represents an item in a status bar
    /// </summary>
    public class StatusBarItem
    {
        private string _text = "";
        private SKColor _textColor = MaterialDesignColors.OnSurface;
        private SKColor _backgroundColor = SKColors.Transparent;
        private float _width = 100;
        private TextAlignment _textAlignment = TextAlignment.Left;
        private bool _isVisible = true;
        private object _tag;

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
        /// Gets or sets the text alignment
        /// </summary>
        public TextAlignment TextAlignment
        {
            get => _textAlignment;
            set
            {
                if (_textAlignment != value)
                {
                    _textAlignment = value;
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
        /// Gets or sets the tag object
        /// </summary>
        public object Tag
        {
            get => _tag;
            set => _tag = value;
        }

        /// <summary>
        /// Gets the parent StatusBar
        /// </summary>
        public StatusBar ParentStatusBar { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the StatusBarItem class
        /// </summary>
        public StatusBarItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the StatusBarItem class with text
        /// </summary>
        public StatusBarItem(string text)
        {
            _text = text ?? "";
        }

        /// <summary>
        /// Invalidates the visual representation
        /// </summary>
        protected virtual void InvalidateVisual()
        {
            ParentStatusBar?.InvalidateVisual();
        }
    }

    /// <summary>
    /// A collection of StatusBarItem objects
    /// </summary>
    public class StatusBarItemCollection : System.Collections.ObjectModel.Collection<StatusBarItem>
    {
        private StatusBar _parentStatusBar;

        /// <summary>
        /// Initializes a new instance of the StatusBarItemCollection class
        /// </summary>
        public StatusBarItemCollection(StatusBar parentStatusBar)
        {
            _parentStatusBar = parentStatusBar;
        }

        /// <summary>
        /// Adds a StatusBarItem to the collection
        /// </summary>
        public void Add(string text)
        {
            Add(new StatusBarItem(text));
        }

        /// <summary>
        /// Adds a StatusBarItem to the collection with specified properties
        /// </summary>
        public void Add(string text, float width)
        {
            var item = new StatusBarItem(text) { Width = width };
            Add(item);
        }

        /// <summary>
        /// Inserts an item into the collection at the specified index
        /// </summary>
        protected override void InsertItem(int index, StatusBarItem item)
        {
            if (item != null)
            {
                item.ParentStatusBar = _parentStatusBar;
            }
            base.InsertItem(index, item);
            _parentStatusBar?.InvalidateVisual();
        }

        /// <summary>
        /// Removes an item from the collection at the specified index
        /// </summary>
        protected override void RemoveItem(int index)
        {
            var item = this[index];
            if (item != null)
            {
                item.ParentStatusBar = null;
            }
            base.RemoveItem(index);
            _parentStatusBar?.InvalidateVisual();
        }

        /// <summary>
        /// Replaces an item in the collection at the specified index
        /// </summary>
        protected override void SetItem(int index, StatusBarItem item)
        {
            var oldItem = this[index];
            if (oldItem != null)
            {
                oldItem.ParentStatusBar = null;
            }

            if (item != null)
            {
                item.ParentStatusBar = _parentStatusBar;
            }

            base.SetItem(index, item);
            _parentStatusBar?.InvalidateVisual();
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
                    item.ParentStatusBar = null;
                }
            }
            base.ClearItems();
            _parentStatusBar?.InvalidateVisual();
        }
    }
}

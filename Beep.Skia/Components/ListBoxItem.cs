using System;

namespace Beep.Skia.Components
{
    /// <summary>
    /// Represents an item in a list box.
    /// </summary>
    public class ListBoxItem
    {
        private string _text = "";
        private object _value;
        private bool _selected = false;

        /// <summary>
        /// Gets or sets the text of the item.
        /// </summary>
        public string Text
        {
            get => _text;
            set => _text = value ?? "";
        }

        /// <summary>
        /// Gets or sets the value associated with the item.
        /// </summary>
        public object Value
        {
            get => _value;
            set => _value = value;
        }

        /// <summary>
        /// Gets or sets whether the item is selected.
        /// </summary>
        public bool Selected
        {
            get => _selected;
            set => _selected = value;
        }

        /// <summary>
        /// Initializes a new instance of the ListBoxItem class.
        /// </summary>
        public ListBoxItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the ListBoxItem class with the specified text.
        /// </summary>
        public ListBoxItem(string text)
        {
            _text = text ?? "";
        }

        /// <summary>
        /// Initializes a new instance of the ListBoxItem class with the specified text and value.
        /// </summary>
        public ListBoxItem(string text, object value)
        {
            _text = text ?? "";
            _value = value;
        }

        /// <summary>
        /// Returns a string representation of the item.
        /// </summary>
        public override string ToString()
        {
            return _text;
        }
    }

    /// <summary>
    /// A collection of list box items.
    /// </summary>
    public class ListBoxItemCollection : System.Collections.ObjectModel.Collection<ListBoxItem>
    {
        /// <summary>
        /// Adds an item with the specified text to the collection.
        /// </summary>
        public void Add(string text)
        {
            Add(new ListBoxItem(text));
        }

        /// <summary>
        /// Adds an item with the specified text and value to the collection.
        /// </summary>
        public void Add(string text, object value)
        {
            Add(new ListBoxItem(text, value));
        }
    }
}

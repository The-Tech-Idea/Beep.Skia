using System;
using Beep.Skia.Model;
namespace Beep.Skia.Components
{
    /// <summary>
    /// Represents an item in a combo box.
    /// </summary>
    public class ComboBoxItem
    {
        private string _text = "";
        private object _value;
        private bool _enabled = true;

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
        /// Gets or sets whether the item is enabled.
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>
        /// Initializes a new instance of the ComboBoxItem class.
        /// </summary>
        public ComboBoxItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the ComboBoxItem class with the specified text.
        /// </summary>
        public ComboBoxItem(string text)
        {
            _text = text ?? "";
        }

        /// <summary>
        /// Initializes a new instance of the ComboBoxItem class with the specified text and value.
        /// </summary>
        public ComboBoxItem(string text, object value)
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
    /// A collection of combo box items.
    /// </summary>
    public class ComboBoxItemCollection : System.Collections.ObjectModel.Collection<ComboBoxItem>
    {
        /// <summary>
        /// Adds an item with the specified text to the collection.
        /// </summary>
        public void Add(string text)
        {
            Add(new ComboBoxItem(text));
        }

        /// <summary>
        /// Adds an item with the specified text and value to the collection.
        /// </summary>
        public void Add(string text, object value)
        {
            Add(new ComboBoxItem(text, value));
        }
    }
}

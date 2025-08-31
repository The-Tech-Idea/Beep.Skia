using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace Beep.Skia.Components
{
    /// <summary>
    /// Represents a column in a DataGrid
    /// </summary>
    public class DataGridColumn
    {
        private string _header = "";
        private string _propertyName = "";
        private float _width = 100;
        private bool _isVisible = true;
        private TextAlignment _textAlignment = TextAlignment.Left;

        /// <summary>
        /// Gets or sets the column header text
        /// </summary>
        public string Header
        {
            get => _header;
            set
            {
                if (_header != value)
                {
                    _header = value ?? "";
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the property name to bind to
        /// </summary>
        public string PropertyName
        {
            get => _propertyName;
            set
            {
                if (_propertyName != value)
                {
                    _propertyName = value ?? "";
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the column width
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
        /// Gets or sets whether the column is visible
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
        /// Gets or sets the text alignment for the column
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
        /// Gets the parent DataGrid
        /// </summary>
        public DataGrid ParentGrid { get; internal set; }

        /// <summary>
        /// Invalidates the visual representation
        /// </summary>
        protected virtual void InvalidateVisual()
        {
            ParentGrid?.InvalidateVisual();
        }
    }

    /// <summary>
    /// Represents a row in a DataGrid
    /// </summary>
    public class DataGridRow
    {
        private object _dataItem;
        private bool _isSelected = false;
        private SKColor _backgroundColor = SKColors.Transparent;

        /// <summary>
        /// Gets or sets the data item for this row
        /// </summary>
        public object DataItem
        {
            get => _dataItem;
            set
            {
                if (_dataItem != value)
                {
                    _dataItem = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the row is selected
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
        /// Gets the parent DataGrid
        /// </summary>
        public DataGrid ParentGrid { get; internal set; }

        /// <summary>
        /// Gets the cell value for a specific column
        /// </summary>
        public object GetCellValue(DataGridColumn column)
        {
            if (_dataItem == null || column == null || string.IsNullOrEmpty(column.PropertyName))
                return null;

            var property = _dataItem.GetType().GetProperty(column.PropertyName);
            return property?.GetValue(_dataItem);
        }

        /// <summary>
        /// Sets the cell value for a specific column
        /// </summary>
        public void SetCellValue(DataGridColumn column, object value)
        {
            if (_dataItem == null || column == null || string.IsNullOrEmpty(column.PropertyName))
                return;

            var property = _dataItem.GetType().GetProperty(column.PropertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(_dataItem, value);
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Invalidates the visual representation
        /// </summary>
        protected virtual void InvalidateVisual()
        {
            ParentGrid?.InvalidateVisual();
        }
    }
}

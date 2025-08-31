using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 data grid component
    /// </summary>
    public class DataGrid : MaterialControl
    {
        private List<DataGridColumn> _columns = new List<DataGridColumn>();
        private List<DataGridRow> _rows = new List<DataGridRow>();
        private int _selectedRowIndex = -1;
        private float _rowHeight = 32;
        private float _headerHeight = 40;
        private SKColor _headerBackgroundColor = MaterialDesignColors.SurfaceVariant;
        private SKColor _rowBackgroundColor = MaterialDesignColors.Surface;
        private SKColor _selectedRowBackgroundColor = MaterialDesignColors.PrimaryContainer;
        private SKColor _gridLineColor = MaterialDesignColors.OutlineVariant;
        private float _gridLineWidth = 1;
        private bool _showGridLines = true;
        private bool _showHeaders = true;

        /// <summary>
        /// Gets or sets the columns in the data grid
        /// </summary>
        public List<DataGridColumn> Columns
        {
            get => _columns;
            set
            {
                if (_columns != value)
                {
                    // Remove old parent references
                    foreach (var column in _columns)
                    {
                        column.ParentGrid = null;
                    }

                    _columns = value ?? new List<DataGridColumn>();

                    // Set new parent references
                    foreach (var column in _columns)
                    {
                        column.ParentGrid = this;
                    }

                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the rows in the data grid
        /// </summary>
        public List<DataGridRow> Rows
        {
            get => _rows;
            set
            {
                if (_rows != value)
                {
                    // Remove old parent references
                    foreach (var row in _rows)
                    {
                        row.ParentGrid = null;
                    }

                    _rows = value ?? new List<DataGridRow>();

                    // Set new parent references
                    foreach (var row in _rows)
                    {
                        row.ParentGrid = this;
                    }

                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the selected row
        /// </summary>
        public int SelectedRowIndex
        {
            get => _selectedRowIndex;
            set
            {
                if (_selectedRowIndex != value)
                {
                    // Clear previous selection
                    if (_selectedRowIndex >= 0 && _selectedRowIndex < _rows.Count)
                    {
                        _rows[_selectedRowIndex].IsSelected = false;
                    }

                    _selectedRowIndex = value;

                    // Set new selection
                    if (_selectedRowIndex >= 0 && _selectedRowIndex < _rows.Count)
                    {
                        _rows[_selectedRowIndex].IsSelected = true;
                    }

                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the height of each row
        /// </summary>
        public float RowHeight
        {
            get => _rowHeight;
            set
            {
                if (_rowHeight != value)
                {
                    _rowHeight = Math.Max(16, value);
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the height of the header
        /// </summary>
        public float HeaderHeight
        {
            get => _headerHeight;
            set
            {
                if (_headerHeight != value)
                {
                    _headerHeight = Math.Max(20, value);
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the header background color
        /// </summary>
        public SKColor HeaderBackgroundColor
        {
            get => _headerBackgroundColor;
            set
            {
                if (_headerBackgroundColor != value)
                {
                    _headerBackgroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the row background color
        /// </summary>
        public SKColor RowBackgroundColor
        {
            get => _rowBackgroundColor;
            set
            {
                if (_rowBackgroundColor != value)
                {
                    _rowBackgroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected row background color
        /// </summary>
        public SKColor SelectedRowBackgroundColor
        {
            get => _selectedRowBackgroundColor;
            set
            {
                if (_selectedRowBackgroundColor != value)
                {
                    _selectedRowBackgroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the grid line color
        /// </summary>
        public SKColor GridLineColor
        {
            get => _gridLineColor;
            set
            {
                if (_gridLineColor != value)
                {
                    _gridLineColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the grid line width
        /// </summary>
        public float GridLineWidth
        {
            get => _gridLineWidth;
            set
            {
                if (_gridLineWidth != value)
                {
                    _gridLineWidth = Math.Max(0, value);
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show grid lines
        /// </summary>
        public bool ShowGridLines
        {
            get => _showGridLines;
            set
            {
                if (_showGridLines != value)
                {
                    _showGridLines = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show headers
        /// </summary>
        public bool ShowHeaders
        {
            get => _showHeaders;
            set
            {
                if (_showHeaders != value)
                {
                    _showHeaders = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the DataGrid class
        /// </summary>
        public DataGrid()
        {
            Width = 400;
            Height = 300;
        }

        /// <summary>
        /// Adds a column to the data grid
        /// </summary>
        public void AddColumn(DataGridColumn column)
        {
            if (column != null)
            {
                column.ParentGrid = this;
                _columns.Add(column);
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Removes a column from the data grid
        /// </summary>
        public void RemoveColumn(DataGridColumn column)
        {
            if (column != null && _columns.Remove(column))
            {
                column.ParentGrid = null;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Adds a row to the data grid
        /// </summary>
        public void AddRow(DataGridRow row)
        {
            if (row != null)
            {
                row.ParentGrid = this;
                _rows.Add(row);
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Removes a row from the data grid
        /// </summary>
        public void RemoveRow(DataGridRow row)
        {
            if (row != null && _rows.Remove(row))
            {
                row.ParentGrid = null;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Clears all rows from the data grid
        /// </summary>
        public void ClearRows()
        {
            foreach (var row in _rows)
            {
                row.ParentGrid = null;
            }
            _rows.Clear();
            _selectedRowIndex = -1;
            InvalidateVisual();
        }

        /// <summary>
        /// Gets the row at the specified index
        /// </summary>
        public DataGridRow GetRowAt(int index)
        {
            return index >= 0 && index < _rows.Count ? _rows[index] : null;
        }

        /// <summary>
        /// Gets the column at the specified index
        /// </summary>
        public DataGridColumn GetColumnAt(int index)
        {
            return index >= 0 && index < _columns.Count ? _columns[index] : null;
        }

        /// <summary>
        /// Draws the data grid content
        /// </summary>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds))
                return;

            var paint = new SKPaint
            {
                IsAntialias = true,
                TextSize = 12,
                Typeface = SKTypeface.Default
            };

            float currentY = Y;

            // Draw header
            if (_showHeaders)
            {
                DrawHeader(canvas, context, paint, currentY);
                currentY += _headerHeight;
            }

            // Draw rows
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                DrawRow(canvas, context, paint, row, i, currentY);
                currentY += _rowHeight;

                // Stop if we're beyond the visible area
                if (currentY > context.Bounds.Bottom)
                    break;
            }

            paint.Dispose();
        }

        private void DrawHeader(SKCanvas canvas, DrawingContext context, SKPaint paint, float y)
        {
            float currentX = X;

            // Draw header background
            using (var backgroundPaint = new SKPaint { Color = _headerBackgroundColor })
            {
                canvas.DrawRect(X, y, Width, _headerHeight, backgroundPaint);
            }

            // Draw column headers
            foreach (var column in _columns.Where(c => c.IsVisible))
            {
                // Draw header text
                paint.Color = MaterialDesignColors.OnSurfaceVariant;
                var textRect = new SKRect(currentX, y, currentX + column.Width, y + _headerHeight);
                DrawTextAligned(canvas, column.Header, textRect, paint, column.TextAlignment);

                // Draw vertical grid line
                if (_showGridLines)
                {
                    using (var linePaint = new SKPaint
                    {
                        Color = _gridLineColor,
                        StrokeWidth = _gridLineWidth,
                        IsAntialias = true
                    })
                    {
                        canvas.DrawLine(currentX + column.Width, y, currentX + column.Width, y + _headerHeight, linePaint);
                    }
                }

                currentX += column.Width;
            }

            // Draw horizontal grid line
            if (_showGridLines)
            {
                using (var linePaint = new SKPaint
                {
                    Color = _gridLineColor,
                    StrokeWidth = _gridLineWidth,
                    IsAntialias = true
                })
                {
                    canvas.DrawLine(X, y + _headerHeight, X + Width, y + _headerHeight, linePaint);
                }
            }
        }

        private void DrawRow(SKCanvas canvas, DrawingContext context, SKPaint paint, DataGridRow row, int rowIndex, float y)
        {
            float currentX = X;

            // Draw row background
            SKColor backgroundColor = row.IsSelected ? _selectedRowBackgroundColor : _rowBackgroundColor;
            using (var backgroundPaint = new SKPaint { Color = backgroundColor })
            {
                canvas.DrawRect(X, y, Width, _rowHeight, backgroundPaint);
            }

            // Draw cells
            foreach (var column in _columns.Where(c => c.IsVisible))
            {
                var cellValue = row.GetCellValue(column);
                var cellText = cellValue?.ToString() ?? "";

                // Draw cell text
                paint.Color = MaterialDesignColors.OnSurface;
                var textRect = new SKRect(currentX, y, currentX + column.Width, y + _rowHeight);
                DrawTextAligned(canvas, cellText, textRect, paint, column.TextAlignment);

                // Draw vertical grid line
                if (_showGridLines)
                {
                    using (var linePaint = new SKPaint
                    {
                        Color = _gridLineColor,
                        StrokeWidth = _gridLineWidth,
                        IsAntialias = true
                    })
                    {
                        canvas.DrawLine(currentX + column.Width, y, currentX + column.Width, y + _rowHeight, linePaint);
                    }
                }

                currentX += column.Width;
            }

            // Draw horizontal grid line
            if (_showGridLines)
            {
                using (var linePaint = new SKPaint
                {
                    Color = _gridLineColor,
                    StrokeWidth = _gridLineWidth,
                    IsAntialias = true
                })
                {
                    canvas.DrawLine(X, y + _rowHeight, X + Width, y + _rowHeight, linePaint);
                }
            }
        }

        private void DrawTextAligned(SKCanvas canvas, string text, SKRect rect, SKPaint paint, TextAlignment alignment)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var textBounds = new SKRect();
            paint.MeasureText(text, ref textBounds);

            float x = rect.Left;
            float y = rect.MidY + textBounds.Height / 2;

            switch (alignment)
            {
                case TextAlignment.Center:
                    x = rect.MidX - textBounds.Width / 2;
                    break;
                case TextAlignment.Right:
                    x = rect.Right - textBounds.Width;
                    break;
                case TextAlignment.Left:
                default:
                    x = rect.Left;
                    break;
            }

            canvas.DrawText(text, x, y, paint);
        }

        /// <summary>
        /// Handles mouse down events
        /// </summary>
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            base.OnMouseDown(point, context);

            if (context.Bounds.Contains(point))
            {
                float currentY = Y + (_showHeaders ? _headerHeight : 0);

                for (int i = 0; i < _rows.Count; i++)
                {
                    if (point.Y >= currentY && point.Y < currentY + _rowHeight)
                    {
                        SelectedRowIndex = i;
                        break;
                    }
                    currentY += _rowHeight;
                }
            }
            return true;
        }
    }
}

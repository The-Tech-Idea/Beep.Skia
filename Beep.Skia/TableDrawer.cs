using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using Beep.Skia.Model;
namespace Beep.Skia
{
  

    /// <summary>
    /// Partial class for TableDrawer containing business logic implementation.
    /// Inherits from the data model in Beep.Skia.Model.
    /// </summary>
    public partial class TableDrawer 
    {
        /// <summary>
        /// Gets or sets the SkiaSharp canvas used for drawing operations.
        /// </summary>
        public SKCanvas Canvas { get; set; }

        /// <summary>
        /// Gets or sets the number of rows in the table.
        /// </summary>
        public int NumRows { get; set; }

        /// <summary>
        /// Gets or sets the number of columns in the table.
        /// </summary>
        public int NumColumns { get; set; }

        /// <summary>
        /// Gets or sets the width of each cell in pixels.
        /// </summary>
        public float CellWidth { get; set; }

        /// <summary>
        /// Gets or sets the height of each cell in pixels.
        /// </summary>
        public float CellHeight { get; set; }

        /// <summary>
        /// Gets or sets the height of the header rows and columns in pixels.
        /// </summary>
        public float HeaderHeight { get; set; }

        /// <summary>
        /// Gets or sets the rectangles representing table cells.
        /// </summary>
        public SKRect[,] CellRects { get; set; }

        /// <summary>
        /// Gets or sets the rectangles representing column headers.
        /// </summary>
        public SKRect[] ColumnHeaderRects { get; set; }

        /// <summary>
        /// Gets or sets the rectangles representing row headers.
        /// </summary>
        public SKRect[] RowHeaderRects { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a drag operation is in progress.
        /// </summary>
        public bool IsDragging { get; set; }

        /// <summary>
        /// Gets or sets the index of the row being dragged.
        /// </summary>
        public int DraggedRowIndex { get; set; }

        /// <summary>
        /// Gets or sets the index of the column being dragged.
        /// </summary>
        public int DraggedColumnIndex { get; set; }

        /// <summary>
        /// Gets or sets the X offset for drag operations.
        /// </summary>
        public float DragOffsetX { get; set; }

        /// <summary>
        /// Gets or sets the Y offset for drag operations.
        /// </summary>
        public float DragOffsetY { get; set; }

        /// <summary>
        /// Gets the font used for header text.
        /// </summary>
        public SKFont HeaderFont { get; private set; }

        /// <summary>
        /// Gets the font used for cell text.
        /// </summary>
        public SKFont CellFont { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableDrawer"/> class.
        /// </summary>
        public TableDrawer()
        {
            // Default constructor for data model
            InitializeFonts();
        }

        /// <summary>
        /// Initializes the fonts used for drawing text.
        /// </summary>
        private void InitializeFonts()
        {
            HeaderFont = new SKFont { Size = 12 };
            CellFont = new SKFont { Size = 10 };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableDrawer"/> class with specified dimensions and canvas.
        /// </summary>
        /// <param name="canvas">The SkiaSharp canvas to draw on.</param>
        /// <param name="numRows">The number of rows in the table.</param>
        /// <param name="numColumns">The number of columns in the table.</param>
        /// <param name="cellWidth">The width of each cell in pixels.</param>
        /// <param name="cellHeight">The height of each cell in pixels.</param>
        /// <param name="headerHeight">The height of the header rows and columns in pixels.</param>
        public TableDrawer(SKCanvas canvas, int numRows, int numColumns, float cellWidth, float cellHeight, float headerHeight)
        {
            Canvas = canvas;
            NumRows = numRows;
            NumColumns = numColumns;
            CellWidth = cellWidth;
            CellHeight = cellHeight;
            HeaderHeight = headerHeight;
            CellRects = new SKRect[numRows, numColumns];
            ColumnHeaderRects = new SKRect[numColumns];
            RowHeaderRects = new SKRect[numRows];
            IsDragging = false;
            DraggedRowIndex = -1;
            DraggedColumnIndex = -1;
            DragOffsetX = 0;
            DragOffsetY = 0;
            InitializeFonts();
        }
    }
}
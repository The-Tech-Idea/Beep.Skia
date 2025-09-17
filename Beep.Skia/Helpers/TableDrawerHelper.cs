using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Beep.Skia;
using Beep.Skia.Model;
namespace Beep.Skia.Helpers
{
    using SkiaSharp;

    /// <summary>
    /// Helper class for TableDrawer operations and calculations
    /// </summary>
    public static class TableDrawerHelper
    {
        /// <summary>
        /// Calculates the rectangle for a column header at the specified index
        /// </summary>
        /// <param name="columnIndex">The zero-based index of the column</param>
        /// <param name="cellWidth">The width of each cell</param>
        /// <param name="headerHeight">The height of the header</param>
        /// <returns>The SKRect representing the column header area</returns>
        public static SKRect CalculateColumnHeaderRect(int columnIndex, float cellWidth, float headerHeight)
        {
            float columnHeaderX = cellWidth * (columnIndex + 1);
            float columnHeaderY = 0;
            return new SKRect(columnHeaderX, columnHeaderY, columnHeaderX + cellWidth, columnHeaderY + headerHeight);
        }

        /// <summary>
        /// Calculates the rectangle for a row header at the specified index
        /// </summary>
        /// <param name="rowIndex">The zero-based index of the row</param>
        /// <param name="cellWidth">The width of each cell</param>
        /// <param name="cellHeight">The height of each cell</param>
        /// <param name="headerHeight">The height of the header</param>
        /// <returns>The SKRect representing the row header area</returns>
        public static SKRect CalculateRowHeaderRect(int rowIndex, float cellWidth, float cellHeight, float headerHeight)
        {
            float rowHeaderX = 0;
            float rowHeaderY = headerHeight + cellHeight * rowIndex;
            return new SKRect(rowHeaderX, rowHeaderY, rowHeaderX + cellWidth, rowHeaderY + cellHeight);
        }

        /// <summary>
        /// Calculates the rectangle for a table cell at the specified row and column
        /// </summary>
        /// <param name="rowIndex">The zero-based index of the row</param>
        /// <param name="columnIndex">The zero-based index of the column</param>
        /// <param name="cellWidth">The width of each cell</param>
        /// <param name="cellHeight">The height of each cell</param>
        /// <param name="headerHeight">The height of the header</param>
        /// <returns>The SKRect representing the cell area</returns>
        public static SKRect CalculateCellRect(int rowIndex, int columnIndex, float cellWidth, float cellHeight, float headerHeight)
        {
            float cellX = cellWidth * (columnIndex + 1);
            float cellY = headerHeight + cellHeight * (rowIndex + 1);
            return new SKRect(cellX, cellY, cellX + cellWidth, cellY + cellHeight);
        }

        /// <summary>
        /// Creates a paint object for drawing headers with default styling
        /// </summary>
        /// <returns>An SKPaint object configured for header drawing</returns>
        public static SKPaint CreateHeaderPaint()
        {
            return new SKPaint() { Color = SKColors.Gray };
        }

        /// <summary>
        /// Creates a paint object for drawing header text with default styling
        /// </summary>
        /// <returns>An SKPaint object configured for header text drawing</returns>
        public static SKPaint CreateHeaderTextPaint()
        {
            return new SKPaint() { Color = SKColors.White };
        }

        /// <summary>
        /// Creates a paint object for drawing cell borders with default styling
        /// </summary>
        /// <returns>An SKPaint object configured for cell border drawing</returns>
        public static SKPaint CreateCellBorderPaint()
        {
            return new SKPaint() { Color = SKColors.White, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        }

        /// <summary>
        /// Creates a paint object for drawing cell text with default styling
        /// </summary>
        /// <returns>An SKPaint object configured for cell text drawing</returns>
        public static SKPaint CreateCellTextPaint()
        {
            return new SKPaint() { Color = SKColors.Black };
        }

        /// <summary>
        /// Creates a paint object for drawing dragged cells with default styling
        /// </summary>
        /// <returns>An SKPaint object configured for dragged cell drawing</returns>
        public static SKPaint CreateDraggedCellPaint()
        {
            return new SKPaint() { Color = SKColors.LightBlue };
        }

        /// <summary>
        /// Generates a default column header text for the specified column index
        /// </summary>
        /// <param name="columnIndex">The zero-based index of the column</param>
        /// <returns>The header text string</returns>
        public static string GenerateColumnHeaderText(int columnIndex)
        {
            return $"Column {columnIndex + 1}";
        }

        /// <summary>
        /// Generates a default row header text for the specified row index
        /// </summary>
        /// <param name="rowIndex">The zero-based index of the row</param>
        /// <returns>The header text string</returns>
        public static string GenerateRowHeaderText(int rowIndex)
        {
            return $"Row {rowIndex + 1}";
        }

        /// <summary>
        /// Generates default cell content text for the specified row and column
        /// </summary>
        /// <param name="rowIndex">The zero-based index of the row</param>
        /// <param name="columnIndex">The zero-based index of the column</param>
        /// <returns>The cell content text string</returns>
        public static string GenerateCellText(int rowIndex, int columnIndex)
        {
            return $"Cell {rowIndex + 1},{columnIndex + 1}";
        }

        /// <summary>
        /// Checks if a point is within any of the provided rectangles
        /// </summary>
        /// <param name="point">The point to test</param>
        /// <param name="rectangles">The array of rectangles to check against</param>
        /// <returns>The index of the rectangle containing the point, or -1 if none contain it</returns>
        public static int GetRectangleIndexContainingPoint(SKPoint point, SKRect[] rectangles)
        {
            for (int i = 0; i < rectangles.Length; i++)
            {
                if (rectangles[i].Contains(point))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Checks if a point is within any cell of the provided 2D rectangle array
        /// </summary>
        /// <param name="point">The point to test</param>
        /// <param name="cellRects">The 2D array of cell rectangles</param>
        /// <param name="rowIndex">When this method returns, contains the row index of the cell containing the point</param>
        /// <param name="columnIndex">When this method returns, contains the column index of the cell containing the point</param>
        /// <returns>true if the point is within a cell; otherwise, false</returns>
        public static bool TryGetCellIndexContainingPoint(SKPoint point, SKRect[,] cellRects, out int rowIndex, out int columnIndex)
        {
            rowIndex = -1;
            columnIndex = -1;

            for (int i = 0; i < cellRects.GetLength(0); i++)
            {
                for (int j = 0; j < cellRects.GetLength(1); j++)
                {
                    if (cellRects[i, j].Contains(point))
                    {
                        rowIndex = i;
                        columnIndex = j;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Calculates the drag offset for a cell being dragged from a specific position
        /// </summary>
        /// <param name="cellRect">The rectangle of the cell being dragged</param>
        /// <param name="mouseLocation">The current mouse location</param>
        /// <returns>The drag offset as a tuple of (offsetX, offsetY)</returns>
        public static (float offsetX, float offsetY) CalculateDragOffset(SKRect cellRect, SKPoint mouseLocation)
        {
            return (cellRect.MidX - mouseLocation.X, cellRect.MidY - mouseLocation.Y);
        }

        /// <summary>
        /// Updates the position of a dragged rectangle based on mouse movement
        /// </summary>
        /// <param name="originalRect">The original rectangle before dragging</param>
        /// <param name="mouseLocation">The current mouse location</param>
        /// <param name="dragOffsetX">The X offset from the drag start</param>
        /// <param name="dragOffsetY">The Y offset from the drag start</param>
        /// <returns>The updated rectangle position</returns>
        public static SKRect UpdateDraggedRectPosition(SKRect originalRect, SKPoint mouseLocation, float dragOffsetX, float dragOffsetY)
        {
            float newDraggedX = mouseLocation.X + dragOffsetX;
            float newDraggedY = mouseLocation.Y + dragOffsetY;
            float halfWidth = originalRect.Width / 2;
            float halfHeight = originalRect.Height / 2;
            return new SKRect(newDraggedX - halfWidth, newDraggedY - halfHeight, newDraggedX + halfWidth, newDraggedY + halfHeight);
        }
    }
}

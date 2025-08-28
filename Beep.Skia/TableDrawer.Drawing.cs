using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Beep.Skia.Helpers;
using SkiaSharp;

namespace Beep.Skia
{
  

    /// <summary>
    /// Partial class for TableDrawer containing all drawing-related methods
    /// </summary>
    public partial class TableDrawer 
    {
        /// <summary>
        /// Draws the complete table including headers, cells, and any dragged elements.
        /// </summary>
        /// <param name="canvas">The SkiaSharp canvas to draw on.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="canvas"/> is null.</exception>
        public void Draw(SKCanvas canvas)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));

            DrawColumnHeaders(canvas);
            DrawRowHeadersAndCells(canvas);
            DrawDraggedCell(canvas);
        }

        /// <summary>
        /// Draws the column headers for the table.
        /// </summary>
        /// <param name="canvas">The SkiaSharp canvas to draw on.</param>
        private void DrawColumnHeaders(SKCanvas canvas)
        {
            for (int i = 0; i < NumColumns; i++)
            {
                SKRect columnHeaderRect = TableDrawerHelper.CalculateColumnHeaderRect(i, CellWidth, HeaderHeight);
                canvas.DrawRect(columnHeaderRect, TableDrawerHelper.CreateHeaderPaint());
                canvas.DrawText(TableDrawerHelper.GenerateColumnHeaderText(i), columnHeaderRect.MidX, columnHeaderRect.MidY, SKTextAlign.Center, HeaderFont, TableDrawerHelper.CreateHeaderTextPaint());
                ColumnHeaderRects[i] = columnHeaderRect;
            }
        }

        /// <summary>
        /// Draws the row headers and all table cells with their content.
        /// </summary>
        /// <param name="canvas">The SkiaSharp canvas to draw on.</param>
        private void DrawRowHeadersAndCells(SKCanvas canvas)
        {
            for (int i = 0; i < NumRows; i++)
            {
                SKRect rowHeaderRect = TableDrawerHelper.CalculateRowHeaderRect(i, CellWidth, CellHeight, HeaderHeight);
                canvas.DrawRect(rowHeaderRect, TableDrawerHelper.CreateHeaderPaint());
                canvas.DrawText(TableDrawerHelper.GenerateRowHeaderText(i), rowHeaderRect.MidX, rowHeaderRect.MidY, SKTextAlign.Center, HeaderFont, TableDrawerHelper.CreateHeaderTextPaint());
                RowHeaderRects[i] = rowHeaderRect;

                for (int j = 0; j < NumColumns; j++)
                {
                    SKRect cellRect = TableDrawerHelper.CalculateCellRect(i, j, CellWidth, CellHeight, HeaderHeight);
                    canvas.DrawRect(cellRect, TableDrawerHelper.CreateCellBorderPaint());
                    CellRects[i, j] = cellRect;
                    canvas.DrawText(TableDrawerHelper.GenerateCellText(i, j), cellRect.MidX, cellRect.MidY, SKTextAlign.Center, CellFont, TableDrawerHelper.CreateCellTextPaint());
                }
            }
        }

        /// <summary>
        /// Draws the currently dragged cell if a drag operation is in progress.
        /// </summary>
        /// <param name="canvas">The SkiaSharp canvas to draw on.</param>
        private void DrawDraggedCell(SKCanvas canvas)
        {
            if (IsDragging && DraggedRowIndex != -1 && DraggedColumnIndex != -1)
            {
                SKRect draggedRect = CellRects[DraggedRowIndex, DraggedColumnIndex];
                canvas.DrawRect(draggedRect, TableDrawerHelper.CreateDraggedCellPaint());
                canvas.DrawText("Dragging", draggedRect.MidX + DragOffsetX, draggedRect.MidY + DragOffsetY, SKTextAlign.Center, CellFont, TableDrawerHelper.CreateCellTextPaint());
            }
        }
    }
}

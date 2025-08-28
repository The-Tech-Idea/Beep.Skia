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
    /// Partial class for TableDrawer containing all mouse interaction methods
    /// </summary>
    public partial class TableDrawer 
    {
        /// <summary>
        /// Handles mouse down events to initiate drag operations on table cells.
        /// </summary>
        /// <param name="mouseLocation">The location of the mouse pointer when the button was pressed.</param>
        public void HandleMouseDown(SKPoint mouseLocation)
        {
            if (TableDrawerHelper.TryGetCellIndexContainingPoint(mouseLocation, CellRects, out int rowIndex, out int columnIndex))
            {
                IsDragging = true;
                DraggedRowIndex = rowIndex;
                DraggedColumnIndex = columnIndex;
                (DragOffsetX, DragOffsetY) = TableDrawerHelper.CalculateDragOffset(CellRects[rowIndex, columnIndex], mouseLocation);
            }
        }

        /// <summary>
        /// Handles mouse move events to update the position of dragged cells.
        /// </summary>
        /// <param name="mouseLocation">The current location of the mouse pointer.</param>
        public void HandleMouseMove(SKPoint mouseLocation)
        {
            if (IsDragging && DraggedRowIndex != -1 && DraggedColumnIndex != -1)
            {
                SKRect originalRect = CellRects[DraggedRowIndex, DraggedColumnIndex];
                CellRects[DraggedRowIndex, DraggedColumnIndex] = TableDrawerHelper.UpdateDraggedRectPosition(originalRect, mouseLocation, DragOffsetX, DragOffsetY);
            }
        }

        /// <summary>
        /// Handles mouse up events to complete drag operations and potentially swap cell positions.
        /// </summary>
        /// <param name="mouseLocation">The location of the mouse pointer when the button was released.</param>
        public void HandleMouseUp(SKPoint mouseLocation)
        {
            if (IsDragging && DraggedRowIndex != -1 && DraggedColumnIndex != -1)
            {
                // Find the target cell
                if (TableDrawerHelper.TryGetCellIndexContainingPoint(mouseLocation, CellRects, out int targetRowIndex, out int targetColumnIndex))
                {
                    // Only swap if it's a different cell
                    if (targetRowIndex != DraggedRowIndex || targetColumnIndex != DraggedColumnIndex)
                    {
                        // Swap the cell contents (you would need to implement this based on your data model)
                        // For now, we'll just swap the rectangles
                        var tempRect = CellRects[DraggedRowIndex, DraggedColumnIndex];
                        CellRects[DraggedRowIndex, DraggedColumnIndex] = CellRects[targetRowIndex, targetColumnIndex];
                        CellRects[targetRowIndex, targetColumnIndex] = tempRect;
                    }
                }

                // Reset drag state
                IsDragging = false;
                DraggedRowIndex = -1;
                DraggedColumnIndex = -1;
                DragOffsetX = 0;
                DragOffsetY = 0;
            }
        }
    }
}

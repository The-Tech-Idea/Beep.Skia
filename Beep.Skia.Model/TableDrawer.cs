using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beep.Skia
{
    using SkiaSharp;

    public class TableDrawer
    {
        public SKCanvas Canvas { get; }

        private int numRows;
        private int numColumns;
        private float cellWidth;
        private float cellHeight;
        private float headerHeight;
        private SKRect[,] cellRects;
        private SKRect[] columnHeaderRects;
        private SKRect[] rowHeaderRects;
        private bool isDragging;
        private int draggedRowIndex;
        private int draggedColumnIndex;
        private float dragOffsetX;
        private float dragOffsetY;

        public TableDrawer(SKCanvas canvas, int numRows, int numColumns, float cellWidth, float cellHeight, float headerHeight)
        {
            Canvas = canvas;
            this.numRows = numRows;
            this.numColumns = numColumns;
            this.cellWidth = cellWidth;
            this.cellHeight = cellHeight;
            this.headerHeight = headerHeight;
            this.cellRects = new SKRect[numRows, numColumns];
            this.columnHeaderRects = new SKRect[numColumns];
            this.rowHeaderRects = new SKRect[numRows];
            this.isDragging = false;
            this.draggedRowIndex = -1;
            this.draggedColumnIndex = -1;
            this.dragOffsetX = 0;
            this.dragOffsetY = 0;
        }

        public void Draw(SKCanvas canvas)
        {
            DrawColumnHeaders(canvas);
            DrawRowHeadersAndCells(canvas);
            DrawDraggedCell(canvas);
        }

        private void DrawColumnHeaders(SKCanvas canvas)
        {
            float columnHeaderX = cellWidth;
            float columnHeaderY = 0;
            for (int i = 0; i < numColumns; i++)
            {
                SKRect columnHeaderRect = new SKRect(columnHeaderX, columnHeaderY, columnHeaderX + cellWidth, columnHeaderY + headerHeight);
                canvas.DrawRect(columnHeaderRect, new SKPaint() { Color = SKColors.Gray });
                canvas.DrawText($"Column {i + 1}", columnHeaderRect.MidX, columnHeaderRect.MidY, new SKPaint() { Color = SKColors.White, TextAlign = SKTextAlign.Center });
                columnHeaderRects[i] = columnHeaderRect;
                columnHeaderX += cellWidth;
            }
        }

        private void DrawRowHeadersAndCells(SKCanvas canvas)
        {
            float rowHeaderX = 0;
            float rowHeaderY = headerHeight;
            for (int i = 0; i < numRows; i++)
            {
                SKRect rowHeaderRect = new SKRect(rowHeaderX, rowHeaderY, rowHeaderX + cellWidth, rowHeaderY + cellHeight);
                canvas.DrawRect(rowHeaderRect, new SKPaint() { Color = SKColors.Gray });
                canvas.DrawText($"Row {i + 1}", rowHeaderRect.MidX, rowHeaderRect.MidY, new SKPaint() { Color = SKColors.White, TextAlign = SKTextAlign.Center });
                rowHeaderRects[i] = rowHeaderRect;

                float cellX = cellWidth;
                float cellY = headerHeight + cellHeight * (i + 1);
                for (int j = 0; j < numColumns; j++)
                {
                    SKRect cellRect = new SKRect(cellX, cellY, cellX + cellWidth, cellY + cellHeight);
                    canvas.DrawRect(cellRect, new SKPaint() { Color = SKColors.White, Style = SKPaintStyle.Stroke, StrokeWidth = 1 });
                    cellRects[i, j] = cellRect;
                    canvas.DrawText($"Cell {i + 1},{j + 1}", cellRect.MidX, cellRect.MidY, new SKPaint()
                    {
                        Color = SKColors.Black,
                        TextAlign = SKTextAlign.Center
                    });
                    cellX += cellWidth;
                }
                rowHeaderY += cellHeight;
            }
        }

        private void DrawDraggedCell(SKCanvas canvas)
        {
            if (isDragging && draggedRowIndex != -1 && draggedColumnIndex != -1)
            {
                SKRect draggedRect = cellRects[draggedRowIndex, draggedColumnIndex];
                canvas.DrawRect(draggedRect, new SKPaint() { Color = SKColors.LightBlue });
                canvas.DrawText("Dragging", draggedRect.MidX + dragOffsetX, draggedRect.MidY + dragOffsetY, new SKPaint() { Color = SKColors.Black, TextAlign = SKTextAlign.Center });
            }
        }

        public void HandleMouseDown(SKPoint mouseLocation)
        {
            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numColumns; j++)
                {
                    if (cellRects[i, j].Contains(mouseLocation))
                    {
                        isDragging = true;
                        draggedRowIndex = i;
                        draggedColumnIndex = j;
                        dragOffsetX = cellRects[i, j].MidX - mouseLocation.X;
                        dragOffsetY = cellRects[i, j].MidY - mouseLocation.Y;
                        break;
                    }
                }
                if (isDragging)
                {
                    break;
                }
            }
        }

        public void HandleMouseMove(SKPoint mouseLocation)
        {
            if (isDragging && draggedRowIndex != -1 && draggedColumnIndex != -1)
            {
                float newDraggedX = mouseLocation.X + dragOffsetX;
                float newDraggedY = mouseLocation.Y + dragOffsetY;
                cellRects[draggedRowIndex, draggedColumnIndex] = new SKRect(newDraggedX - cellWidth / 2, newDraggedY - cellHeight / 2, newDraggedX + cellWidth / 2, newDraggedY + cellHeight / 2);
            }
        }

        public void HandleMouseUp(SKPoint mouseLocation)
        {
            if (isDragging && draggedRowIndex != -1 && draggedColumnIndex != -1)
            {
                for (int i = 0; i < numRows; i++)
                {
                    for (int j = 0; j < numColumns; j++)
                    {
                        if (i != draggedRowIndex && j != draggedColumnIndex && cellRects[i, j].Contains(mouseLocation))
                        {
                            SKRect tempRect = cellRects[draggedRowIndex, draggedColumnIndex];
                            cellRects[draggedRowIndex, draggedColumnIndex] = cellRects[i, j];
                            cellRects[i, j] = tempRect;

                            string tempText = $"Cell {i + 1},{j + 1}";
                            Canvas.DrawText(tempText, cellRects[draggedRowIndex, draggedColumnIndex].MidX, cellRects[draggedRowIndex, draggedColumnIndex].MidY, new SKPaint() { Color = SKColors.Black, TextAlign = SKTextAlign.Center });
                            Canvas.DrawText($"Cell {draggedRowIndex + 1},{draggedColumnIndex + 1}", cellRects[i, j].MidX, cellRects[i, j].MidY, new SKPaint() { Color = SKColors.Black, TextAlign = SKTextAlign.Center });
                            break;
                        }
                    }
                    if (isDragging)
                    {
                        break;
                    }
                }

                isDragging = false;
                draggedRowIndex = -1;
                draggedColumnIndex = -1;
                dragOffsetX = 0;
                dragOffsetY = 0;
            }
        }

    }


}


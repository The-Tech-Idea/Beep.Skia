using Beep.Skia.Helpers;
using SkiaSharp;

namespace Beep.Skia
{
    public partial class TableDrawer
    {
        /// <summary>The cell under <paramref name="point"/> (relative to the table's top-left corner): the top-left cell of
        /// the merged cell it falls in, or the cell itself; null outside the table.</summary>
        public (int Row, int Column)? CellAt(SKPoint point)
        {
            var row = TableDrawerHelper.IndexAt(_tops, point.Y);
            var column = TableDrawerHelper.IndexAt(_lefts, Content.RightToLeft ? Size.Width - point.X : point.X);
            if (row is null || column is null) return null;
            return Content.Anchor(row.Value, column.Value);
        }

        /// <summary>The column whose trailing edge (its right edge; its left edge in a right-to-left table) is within
        /// <paramref name="tolerance"/> of <paramref name="point"/>, along the table's height; null when none is. Dragging
        /// that edge sets the column's width.</summary>
        public int? ColumnEdgeAt(SKPoint point, float tolerance)
        {
            if (point.Y < 0 || point.Y > Size.Height) return null;
            return TableDrawerHelper.EdgeAt(DrawnEdges(), point.X, tolerance);
        }

        /// <summary>The row whose bottom edge is within <paramref name="tolerance"/> of <paramref name="point"/>, across the
        /// table's width; null when none is. Dragging that edge sets the row's minimum height.</summary>
        public int? RowEdgeAt(SKPoint point, float tolerance)
        {
            if (point.X < 0 || point.X > Size.Width) return null;
            return TableDrawerHelper.EdgeAt(_tops, point.Y, tolerance);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia.Components;
using Beep.Skia.Helpers;
using Beep.Skia.Text;
using SkiaSharp;

namespace Beep.Skia
{
    /// <summary>
    /// Lays out a table's content at a text size, then draws it (<c>TableDrawer.Drawing.cs</c>) and finds cells and edges
    /// under a point (<c>TableDrawer.Interaction.cs</c>). Columns are as wide as the content says; each row is as tall as its
    /// minimum or its tallest wrapped text, whichever is more, and a merged cell taller than its rows makes its last row
    /// taller. A right-to-left table places its first column on the right. Positions are relative to the table's top-left corner. <see cref="TableShape"/> uses it, and so can any host
    /// that draws a table. The bundled font families the cells name must be registered (<see cref="TypefaceCache.Register"/>).
    /// </summary>
    public partial class TableDrawer
    {
        /// <summary>Space between a cell's border and its text, in drawing units.</summary>
        public const float PaddingX = 8, PaddingY = 6;
        private readonly float[] _lefts, _tops, _heights;
        private readonly Dictionary<(int Row, int Column), List<GlyphLine>> _lines = new();

        public TableDrawer(TableContent content, float fontSize)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            if (!float.IsFinite(fontSize) || fontSize <= 0) throw new ArgumentOutOfRangeException(nameof(fontSize));
            FontSize = fontSize;
            _lefts = TableDrawerHelper.Offsets(content.Columns);
            _heights = content.Rows.ToArray();
            var fonts = new Dictionary<string, SKFont>(StringComparer.Ordinal);
            try
            {
                var spanning = new List<(TableMerge Area, float Needed)>();
                foreach (var (row, column) in content.Anchors())
                {
                    var cell = content[row, column]; var area = content.AreaOf(row, column);
                    if (!fonts.TryGetValue(cell.FontFamily, out var font)) fonts[cell.FontFamily] = font = TextBlock.Font(cell.FontFamily, fontSize);
                    var width = TableDrawerHelper.Span(content.Columns, area.Column, area.ColumnSpan) - 2 * PaddingX;
                    var lines = cell.Text.Length == 0 ? new List<GlyphLine>() : TextBlock.Lines(cell.FontFamily, font, cell.Text, Math.Max(1, width));
                    _lines[(row, column)] = lines;
                    var needed = TextHeight(lines.Count) + 2 * PaddingY;
                    if (area.RowSpan == 1) _heights[row] = Math.Max(_heights[row], needed);
                    else spanning.Add((area, needed));
                }
                // Shorter spans first, so a longer one sees the rows the shorter ones already made taller.
                foreach (var (area, needed) in spanning.OrderBy(s => s.Area.RowSpan))
                {
                    var have = TableDrawerHelper.Span(_heights, area.Row, area.RowSpan);
                    if (needed > have) _heights[area.LastRow] += needed - have;
                }
            }
            finally
            {
                foreach (var font in fonts.Values) font.Dispose();
            }
            _tops = TableDrawerHelper.Offsets(_heights);
            Size = new SKSize(_lefts[_lefts.Length - 1], _tops[_tops.Length - 1]);
        }

        public TableContent Content { get; }
        public float FontSize { get; }
        /// <summary>The laid-out size: the sum of the columns by the sum of the laid-out rows.</summary>
        public SKSize Size { get; }
        /// <summary>Each row's laid-out height (its minimum, or taller for its text).</summary>
        public IReadOnlyList<float> RowHeights => _heights;
        /// <summary>The distance between baselines in a cell.</summary>
        public float LineHeight => FontSize * TextBlock.LineSpacing;

        /// <summary>The height <paramref name="lines"/> lines of text take; an empty cell keeps the height of one line.</summary>
        public float TextHeight(int lines) => Math.Max(1, lines) * LineHeight;
        /// <summary>The left edge of a column as drawn (a right-to-left table counts columns from its right edge).</summary>
        public float ColumnLeft(int column) => Horizontal(CheckColumn(column), column).Left;
        /// <summary>Where the edge before column <paramref name="boundary"/> (0 to the column count) is drawn.</summary>
        private float X(int boundary) => Content.RightToLeft ? Size.Width - _lefts[boundary] : _lefts[boundary];
        /// <summary>The drawn left and right of the columns from <paramref name="first"/> to <paramref name="last"/>.</summary>
        private (float Left, float Right) Horizontal(int first, int last) =>
            Content.RightToLeft ? (X(last + 1), X(first)) : (X(first), X(last + 1));
        /// <summary>Every column edge as drawn, from the first column's leading edge to the last column's trailing edge.</summary>
        private float[] DrawnEdges()
        {
            var edges = new float[_lefts.Length];
            for (int i = 0; i < edges.Length; i++) edges[i] = X(i);
            return edges;
        }
        public float RowTop(int row) => _tops[CheckRow(row)];
        /// <summary>The rectangle a cell is drawn in: its merged area's, or its own.</summary>
        public SKRect CellBounds(int row, int column)
        {
            var area = Content.AreaOf(CheckRow(row), CheckColumn(column));
            var (left, right) = Horizontal(area.Column, area.LastColumn);
            return new SKRect(left, _tops[area.Row], right, _tops[area.LastRow + 1]);
        }
        /// <summary>The rectangle a cell's text wraps in: its bounds less the padding.</summary>
        public SKRect TextBounds(int row, int column)
        {
            var bounds = CellBounds(row, column);
            return new SKRect(bounds.Left + PaddingX, bounds.Top + PaddingY, Math.Max(bounds.Left + PaddingX, bounds.Right - PaddingX), Math.Max(bounds.Top + PaddingY, bounds.Bottom - PaddingY));
        }

        private int CheckRow(int row) => row >= 0 && row < Content.RowCount ? row : throw new ArgumentOutOfRangeException(nameof(row));
        private int CheckColumn(int column) => column >= 0 && column < Content.ColumnCount ? column : throw new ArgumentOutOfRangeException(nameof(column));
    }
}

using System;
using System.Collections.Generic;
using Beep.Skia.Components;
using Beep.Skia.Text;
using SkiaSharp;

namespace Beep.Skia
{
    /// <summary>How a table is painted: the colour of its lines and text, its background (transparent for none), the
    /// header row's background and the width of its lines.</summary>
    public readonly record struct TableLook(SKColor Stroke, SKColor Fill, SKColor HeaderFill, float StrokeWidth);

    public partial class TableDrawer
    {
        /// <summary>
        /// Paints the table with its top-left corner at <paramref name="origin"/>: the background, the header row's
        /// background under header cells without their own fill, each cell's own fill, the lines between cells (none inside
        /// a merged cell) and the border, then each cell's text clipped to its cell.
        /// </summary>
        public void Draw(SKCanvas canvas, SKPoint origin, TableLook look)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            if (!float.IsFinite(look.StrokeWidth) || look.StrokeWidth < 0) throw new ArgumentOutOfRangeException(nameof(look));
            var save = canvas.Save();
            try
            {
                canvas.Translate(origin.X, origin.Y);
                using var fill = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
                var table = new SKRect(0, 0, Size.Width, Size.Height);
                if (look.Fill.Alpha != 0) { fill.Color = look.Fill; canvas.DrawRect(table, fill); }
                foreach (var (row, column) in Content.Anchors())
                {
                    var cell = Content[row, column];
                    var color = cell.Fill.Length != 0 ? SKColor.Parse(cell.Fill) : Content.HeaderRow && row == 0 ? look.HeaderFill : SKColors.Transparent;
                    if (color.Alpha == 0) continue;
                    fill.Color = color; canvas.DrawRect(CellBounds(row, column), fill);
                }
                if (look.StrokeWidth > 0)
                {
                    using var lines = Lines();
                    using var stroke = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = look.StrokeWidth, Color = look.Stroke, StrokeJoin = SKStrokeJoin.Miter };
                    canvas.DrawPath(lines, stroke);
                }
                DrawTexts(canvas, look.Stroke);
            }
            finally { canvas.RestoreToCount(save); }
        }

        /// <summary>The border and every line between cells that are not part of one merged cell.</summary>
        private SKPath Lines()
        {
            using var path = new SKPathBuilder();
            path.AddRect(new SKRect(0, 0, Size.Width, Size.Height));
            // A line between two columns runs down each row where the two cells are not one merged cell; runs that touch join.
            for (int column = 1; column < Content.ColumnCount; column++)
            {
                float? start = null;
                for (int row = 0; row <= Content.RowCount; row++)
                {
                    var open = row < Content.RowCount && !Joined(row, column - 1, row, column);
                    if (open && start is null) start = _tops[row];
                    if (!open && start is { } from) { path.MoveTo(X(column), from); path.LineTo(X(column), _tops[row]); start = null; }
                }
            }
            for (int row = 1; row < Content.RowCount; row++)
            {
                float? start = null;
                for (int column = 0; column <= Content.ColumnCount; column++)
                {
                    var open = column < Content.ColumnCount && !Joined(row - 1, column, row, column);
                    if (open && start is null) start = X(column);
                    if (!open && start is { } from) { path.MoveTo(from, _tops[row]); path.LineTo(X(column), _tops[row]); start = null; }
                }
            }
            return path.Detach();
        }
        private bool Joined(int row, int column, int otherRow, int otherColumn) =>
            Content.MergeAt(row, column) is { } merge && merge.Contains(otherRow, otherColumn);

        private void DrawTexts(SKCanvas canvas, SKColor color)
        {
            var fonts = new Dictionary<string, SKFont>(StringComparer.Ordinal);
            using var paint = new SKPaint { IsAntialias = true, Color = color };
            try
            {
                foreach (var pair in _lines)
                {
                    if (pair.Value.Count == 0) continue;
                    var (row, column) = pair.Key; var cell = Content[row, column];
                    if (!fonts.TryGetValue(cell.FontFamily, out var font)) fonts[cell.FontFamily] = font = TextBlock.Font(cell.FontFamily, FontSize);
                    var box = TextBounds(row, column); var height = pair.Value.Count * LineHeight;
                    var top = cell.VerticalAlign switch
                    {
                        TableVerticalAlign.Top => box.Top,
                        TableVerticalAlign.Middle => box.MidY - height / 2,
                        TableVerticalAlign.Bottom => box.Bottom - height,
                    };
                    var save = canvas.Save();
                    canvas.ClipRect(CellBounds(row, column));
                    TextBlock.Draw(canvas, font, pair.Value, box, cell.Align, top - font.Metrics.Ascent, LineHeight, paint, false);
                    canvas.RestoreToCount(save);
                }
            }
            finally
            {
                foreach (var font in fonts.Values) font.Dispose();
            }
        }
    }
}

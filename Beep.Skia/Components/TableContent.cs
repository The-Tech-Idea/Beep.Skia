using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using SkiaSharp;

namespace Beep.Skia.Components;

/// <summary>Where a cell's text sits between the cell's top and bottom.</summary>
public enum TableVerticalAlign { Top, Middle, Bottom }

/// <summary>
/// One table cell: its text (lines separated by '\n'), its own fill ("" for none; otherwise #RRGGBB or #AARRGGBB), where the
/// text sits, and the bundled font family it is drawn in. The host chooses the family so the text can be drawn.
/// </summary>
public sealed record TableCell(string Text, string Fill, SketchTextAlign Align, TableVerticalAlign VerticalAlign, string FontFamily)
{
    /// <summary>An empty cell drawn in <paramref name="family"/>, text at the top left.</summary>
    public static TableCell Empty(string family) => new("", "", SketchTextAlign.Left, TableVerticalAlign.Top, family);
}

/// <summary>A rectangle of cells drawn as one: its top-left cell holds the text, the others are covered and empty.</summary>
public readonly record struct TableMerge(int Row, int Column, int RowSpan, int ColumnSpan)
{
    public int LastRow => Row + RowSpan - 1;
    public int LastColumn => Column + ColumnSpan - 1;
    public bool Contains(int row, int column) => row >= Row && row <= LastRow && column >= Column && column <= LastColumn;
    public bool Overlaps(TableMerge other) => Row <= other.LastRow && other.Row <= LastRow && Column <= other.LastColumn && other.Column <= LastColumn;
    public bool Inside(TableMerge other) => Row >= other.Row && LastRow <= other.LastRow && Column >= other.Column && LastColumn <= other.LastColumn;
}

/// <summary>
/// What a table holds, independent of where it is drawn: column widths, each row's minimum height, the cells in reading
/// order (row by row), merged areas, whether the first row is a header, and whether the columns run right to left (the
/// first column on the right, as Google Docs and Apple Pages offer for right-to-left text). Instances never change; every edit returns new
/// content, so an undo step can keep the content from before. Rows grow past their minimum to fit their text
/// (<see cref="TableDrawer"/> lays them out).
/// </summary>
public sealed class TableContent : IEquatable<TableContent>
{
    /// <summary>The narrowest column and the lowest row minimum, in drawing units.</summary>
    public const float MinColumnWidth = 24, MinRowHeight = 24;
    private readonly float[] _columns, _rows;
    private readonly TableCell[] _cells;
    private readonly TableMerge[] _merges;

    public TableContent(IEnumerable<float> columns, IEnumerable<float> rows, IEnumerable<TableCell> cells, IEnumerable<TableMerge> merges, bool headerRow,
        bool rightToLeft = false)
    {
        _columns = (columns ?? throw new ArgumentNullException(nameof(columns))).ToArray();
        _rows = (rows ?? throw new ArgumentNullException(nameof(rows))).ToArray();
        _cells = (cells ?? throw new ArgumentNullException(nameof(cells))).ToArray();
        _merges = (merges ?? throw new ArgumentNullException(nameof(merges))).OrderBy(m => m.Row).ThenBy(m => m.Column).ToArray();
        HeaderRow = headerRow; RightToLeft = rightToLeft;
        if (_columns.Length == 0 || _rows.Length == 0) throw new ArgumentException("A table has at least one row and one column.");
        if (_columns.Any(w => !float.IsFinite(w) || w < MinColumnWidth)) throw new ArgumentException($"Every column is at least {MinColumnWidth} units wide.", nameof(columns));
        if (_rows.Any(h => !float.IsFinite(h) || h < MinRowHeight)) throw new ArgumentException($"Every row is at least {MinRowHeight} units high.", nameof(rows));
        if (_cells.Length != _columns.Length * _rows.Length) throw new ArgumentException("A table has one cell for every row and column.", nameof(cells));
        foreach (var cell in _cells)
        {
            if (cell is null || cell.Text is null || cell.Text.IndexOf('\r') >= 0) throw new ArgumentException("Cell text uses '\\n' between lines.", nameof(cells));
            if (cell.Fill is null || cell.Fill.Length != 0 && !IsColor(cell.Fill)) throw new ArgumentException("A cell fill is empty or a #RRGGBB or #AARRGGBB color.", nameof(cells));
            if (!Enum.IsDefined(typeof(SketchTextAlign), cell.Align) || !Enum.IsDefined(typeof(TableVerticalAlign), cell.VerticalAlign))
                throw new ArgumentException("A cell has an unknown text alignment.", nameof(cells));
            if (string.IsNullOrWhiteSpace(cell.FontFamily)) throw new ArgumentException("Every cell names its font family.", nameof(cells));
        }
        for (int i = 0; i < _merges.Length; i++)
        {
            var m = _merges[i];
            if (m.Row < 0 || m.Column < 0 || m.RowSpan < 1 || m.ColumnSpan < 1 || m.RowSpan * m.ColumnSpan < 2
                || m.LastRow >= _rows.Length || m.LastColumn >= _columns.Length)
                throw new ArgumentException("A merged area covers at least two cells inside the table.", nameof(merges));
            for (int j = 0; j < i; j++) if (m.Overlaps(_merges[j])) throw new ArgumentException("Merged areas do not overlap.", nameof(merges));
            for (int r = m.Row; r <= m.LastRow; r++)
                for (int c = m.Column; c <= m.LastColumn; c++)
                    if ((r != m.Row || c != m.Column) && this[r, c].Text.Length != 0)
                        throw new ArgumentException("Only the top-left cell of a merged area holds text.", nameof(merges));
        }
    }

    /// <summary>An empty table of <paramref name="rows"/> by <paramref name="columns"/> cells of the given size; a
    /// right-to-left table's cells start right-aligned.</summary>
    public static TableContent Blank(int rows, int columns, float columnWidth, float rowHeight, string family, bool headerRow, bool rightToLeft = false)
    {
        if (rows < 1 || columns < 1) throw new ArgumentOutOfRangeException(rows < 1 ? nameof(rows) : nameof(columns));
        var cell = TableCell.Empty(family) with { Align = rightToLeft ? SketchTextAlign.Right : SketchTextAlign.Left };
        return new(Enumerable.Repeat(columnWidth, columns), Enumerable.Repeat(rowHeight, rows),
            Enumerable.Repeat(cell, rows * columns), Array.Empty<TableMerge>(), headerRow, rightToLeft);
    }

    public IReadOnlyList<float> Columns => _columns;
    /// <summary>Each row's minimum height; a row grows to fit its text.</summary>
    public IReadOnlyList<float> Rows => _rows;
    /// <summary>The cells row by row.</summary>
    public IReadOnlyList<TableCell> Cells => _cells;
    public IReadOnlyList<TableMerge> Merges => _merges;
    public bool HeaderRow { get; }
    /// <summary>True when the first column is on the right and the columns run leftwards.</summary>
    public bool RightToLeft { get; }
    public int RowCount => _rows.Length;
    public int ColumnCount => _columns.Length;
    public TableCell this[int row, int column] => _cells[Index(row, column)];

    private int Index(int row, int column)
    {
        if (row < 0 || row >= _rows.Length) throw new ArgumentOutOfRangeException(nameof(row));
        if (column < 0 || column >= _columns.Length) throw new ArgumentOutOfRangeException(nameof(column));
        return row * _columns.Length + column;
    }

    /// <summary>The merged area that covers a cell, if any.</summary>
    public TableMerge? MergeAt(int row, int column)
    {
        Index(row, column);
        foreach (var m in _merges) if (m.Contains(row, column)) return m;
        return null;
    }
    /// <summary>The cell that holds the text drawn at a cell: the top-left cell of its merged area, or itself.</summary>
    public (int Row, int Column) Anchor(int row, int column) => MergeAt(row, column) is { } m ? (m.Row, m.Column) : (row, column);
    /// <summary>The area a cell is drawn as: its merged area, or the cell alone.</summary>
    public TableMerge AreaOf(int row, int column) => MergeAt(row, column) ?? new TableMerge(row, column, 1, 1);
    /// <summary>The cells that hold text (every cell not covered by a merged area), row by row.</summary>
    public IEnumerable<(int Row, int Column)> Anchors()
    {
        for (int r = 0; r < _rows.Length; r++)
            for (int c = 0; c < _columns.Length; c++)
                if (MergeAt(r, c) is not { } m || m.Row == r && m.Column == c) yield return (r, c);
    }

    /// <summary>The same table with one cell replaced; a covered cell cannot hold text.</summary>
    public TableContent With(int row, int column, TableCell cell)
    {
        var cells = (TableCell[])_cells.Clone(); cells[Index(row, column)] = cell ?? throw new ArgumentNullException(nameof(cell));
        return new(_columns, _rows, cells, _merges, HeaderRow, RightToLeft);
    }
    /// <summary>The same table with every cell in <paramref name="area"/> changed by <paramref name="change"/>.</summary>
    public TableContent WithCells(TableMerge area, Func<TableCell, TableCell> change)
    {
        if (change is null) throw new ArgumentNullException(nameof(change));
        Index(area.Row, area.Column); Index(area.LastRow, area.LastColumn);
        var cells = (TableCell[])_cells.Clone();
        for (int r = area.Row; r <= area.LastRow; r++)
            for (int c = area.Column; c <= area.LastColumn; c++) cells[Index(r, c)] = change(cells[Index(r, c)]);
        return new(_columns, _rows, cells, _merges, HeaderRow, RightToLeft);
    }
    public TableContent WithHeaderRow(bool headerRow) => new(_columns, _rows, _cells, _merges, headerRow, RightToLeft);
    /// <summary>The same table with its columns running right to left (or left to right). When the direction changes, text
    /// aligned to one side moves to the other, as Apple Pages does when it reverses a table; centred text stays.</summary>
    public TableContent WithRightToLeft(bool rightToLeft)
    {
        if (rightToLeft == RightToLeft) return this;
        var cells = _cells.Select(c => c.Align switch
        {
            SketchTextAlign.Left => c with { Align = SketchTextAlign.Right },
            SketchTextAlign.Right => c with { Align = SketchTextAlign.Left },
            SketchTextAlign.Center => c,
        });
        return new(_columns, _rows, cells, _merges, HeaderRow, rightToLeft);
    }
    public TableContent WithColumnWidth(int column, float width)
    {
        Index(0, column); var columns = (float[])_columns.Clone(); columns[column] = width;
        return new(columns, _rows, _cells, _merges, HeaderRow, RightToLeft);
    }
    public TableContent WithRowHeight(int row, float height)
    {
        Index(row, 0); var rows = (float[])_rows.Clone(); rows[row] = height;
        return new(_columns, rows, _cells, _merges, HeaderRow, RightToLeft);
    }
    /// <summary>Column widths and row minimums multiplied by the factors, never below the minimum sizes.</summary>
    public TableContent Scaled(float x, float y)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y) || x <= 0 || y <= 0) throw new ArgumentOutOfRangeException(x <= 0 || !float.IsFinite(x) ? nameof(x) : nameof(y));
        return new(_columns.Select(w => Math.Max(MinColumnWidth, w * x)), _rows.Select(h => Math.Max(MinRowHeight, h * y)), _cells, _merges, HeaderRow, RightToLeft);
    }

    /// <summary>
    /// Inserts <paramref name="count"/> rows before row <paramref name="at"/> (or after the last when it equals the row
    /// count). New cells copy the style of the row they are inserted next to, with no text; a merged area the new rows fall
    /// inside grows over them.
    /// </summary>
    public TableContent InsertRows(int at, int count)
    {
        if (at < 0 || at > _rows.Length) throw new ArgumentOutOfRangeException(nameof(at));
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        var model = at < _rows.Length ? at : at - 1;
        var rows = _rows.Take(at).Concat(Enumerable.Repeat(_rows[model], count)).Concat(_rows.Skip(at));
        var cells = new List<TableCell>();
        for (int r = 0; r < _rows.Length + count; r++)
            for (int c = 0; c < _columns.Length; c++)
                cells.Add(r < at ? this[r, c] : r < at + count ? this[model, c] with { Text = "" } : this[r - count, c]);
        var merges = _merges.Select(m => m.Row >= at ? m with { Row = m.Row + count } : m.LastRow >= at ? m with { RowSpan = m.RowSpan + count } : m);
        return new(_columns, rows, cells, merges, HeaderRow, RightToLeft);
    }
    /// <summary>Inserts <paramref name="count"/> columns before column <paramref name="at"/>, as <see cref="InsertRows"/> does rows.</summary>
    public TableContent InsertColumns(int at, int count)
    {
        if (at < 0 || at > _columns.Length) throw new ArgumentOutOfRangeException(nameof(at));
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        var model = at < _columns.Length ? at : at - 1;
        var columns = _columns.Take(at).Concat(Enumerable.Repeat(_columns[model], count)).Concat(_columns.Skip(at));
        var cells = new List<TableCell>();
        for (int r = 0; r < _rows.Length; r++)
            for (int c = 0; c < _columns.Length + count; c++)
                cells.Add(c < at ? this[r, c] : c < at + count ? this[r, model] with { Text = "" } : this[r, c - count]);
        var merges = _merges.Select(m => m.Column >= at ? m with { Column = m.Column + count } : m.LastColumn >= at ? m with { ColumnSpan = m.ColumnSpan + count } : m);
        return new(columns, _rows, cells, merges, HeaderRow, RightToLeft);
    }
    /// <summary>
    /// Removes <paramref name="count"/> rows from row <paramref name="at"/>; at least one row stays. A merged area that
    /// loses rows shrinks, keeping its text when its top row goes; one left a single cell is no longer merged.
    /// </summary>
    public TableContent RemoveRows(int at, int count)
    {
        if (at < 0 || count < 1 || at + count > _rows.Length) throw new ArgumentOutOfRangeException(nameof(count));
        if (count == _rows.Length) throw new ArgumentException("A table keeps at least one row.");
        var cells = (TableCell[])_cells.Clone();
        var merges = new List<TableMerge>();
        foreach (var m in _merges)
        {
            var removed = Math.Max(0, Math.Min(m.LastRow, at + count - 1) - Math.Max(m.Row, at) + 1);
            if (removed == 0) { merges.Add(m.Row > at ? m with { Row = m.Row - count } : m); continue; }
            if (removed == m.RowSpan) continue;
            var top = m.Row >= at ? at + count : m.Row; // The merged area's first remaining row.
            if (top != m.Row) { cells[Index(top, m.Column)] = cells[Index(m.Row, m.Column)]; cells[Index(m.Row, m.Column)] = cells[Index(m.Row, m.Column)] with { Text = "" }; }
            var kept = m with { Row = top > at ? top - count : top, RowSpan = m.RowSpan - removed };
            if (kept.RowSpan * kept.ColumnSpan >= 2) merges.Add(kept);
        }
        var rows = _rows.Take(at).Concat(_rows.Skip(at + count));
        var remaining = new List<TableCell>();
        for (int r = 0; r < _rows.Length; r++)
            if (r < at || r >= at + count) for (int c = 0; c < _columns.Length; c++) remaining.Add(cells[Index(r, c)]);
        return new(_columns, rows, remaining, merges, HeaderRow, RightToLeft);
    }
    /// <summary>Removes <paramref name="count"/> columns from column <paramref name="at"/>, as <see cref="RemoveRows"/> does rows.</summary>
    public TableContent RemoveColumns(int at, int count)
    {
        if (at < 0 || count < 1 || at + count > _columns.Length) throw new ArgumentOutOfRangeException(nameof(count));
        if (count == _columns.Length) throw new ArgumentException("A table keeps at least one column.");
        var cells = (TableCell[])_cells.Clone();
        var merges = new List<TableMerge>();
        foreach (var m in _merges)
        {
            var removed = Math.Max(0, Math.Min(m.LastColumn, at + count - 1) - Math.Max(m.Column, at) + 1);
            if (removed == 0) { merges.Add(m.Column > at ? m with { Column = m.Column - count } : m); continue; }
            if (removed == m.ColumnSpan) continue;
            var left = m.Column >= at ? at + count : m.Column;
            if (left != m.Column) { cells[Index(m.Row, left)] = cells[Index(m.Row, m.Column)]; cells[Index(m.Row, m.Column)] = cells[Index(m.Row, m.Column)] with { Text = "" }; }
            var kept = m with { Column = left > at ? left - count : left, ColumnSpan = m.ColumnSpan - removed };
            if (kept.RowSpan * kept.ColumnSpan >= 2) merges.Add(kept);
        }
        var columns = _columns.Take(at).Concat(_columns.Skip(at + count));
        var remaining = new List<TableCell>();
        for (int r = 0; r < _rows.Length; r++)
            for (int c = 0; c < _columns.Length; c++)
                if (c < at || c >= at + count) remaining.Add(cells[Index(r, c)]);
        return new(columns, _rows, remaining, merges, HeaderRow, RightToLeft);
    }

    /// <summary>True when the <paramref name="count"/> rows from <paramref name="first"/> can move one place up
    /// (<paramref name="by"/> -1) or down (1): there is a row to swap with, and no merged cell would be split.</summary>
    public bool CanMoveRows(int first, int count, int by) => RowOrder(first, count, by) is { } order && MergesKeep(order, true);
    /// <summary>True when the columns can move one place towards the first column (-1) or the last (1).</summary>
    public bool CanMoveColumns(int first, int count, int by) => ColumnOrder(first, count, by) is { } order && MergesKeep(order, false);
    /// <summary>Moves the <paramref name="count"/> rows from <paramref name="first"/> one place up (-1) or down (1), with
    /// their heights and merged cells; refused when <see cref="CanMoveRows"/> is false.</summary>
    public TableContent MoveRows(int first, int count, int by)
    {
        if (RowOrder(first, count, by) is not { } order || !MergesKeep(order, true)) throw new ArgumentException("These rows cannot move that way.");
        var position = Positions(order);
        var cells = new List<TableCell>();
        foreach (var old in order) for (int c = 0; c < _columns.Length; c++) cells.Add(this[old, c]);
        return new(_columns, order.Select(r => _rows[r]), cells, _merges.Select(m => m with { Row = position[m.Row] }), HeaderRow, RightToLeft);
    }
    /// <summary>Moves the columns one place towards the first column (-1) or the last (1), as <see cref="MoveRows"/> does rows.</summary>
    public TableContent MoveColumns(int first, int count, int by)
    {
        if (ColumnOrder(first, count, by) is not { } order || !MergesKeep(order, false)) throw new ArgumentException("These columns cannot move that way.");
        var position = Positions(order);
        var cells = new List<TableCell>();
        for (int r = 0; r < _rows.Length; r++) foreach (var old in order) cells.Add(this[r, old]);
        return new(order.Select(c => _columns[c]), _rows, cells, _merges.Select(m => m with { Column = position[m.Column] }), HeaderRow, RightToLeft);
    }
    // The new order (new index → old index) when a block swaps places with its neighbour; null at the table's edge.
    private int[] RowOrder(int first, int count, int by) => Order(_rows.Length, first, count, by);
    private int[] ColumnOrder(int first, int count, int by) => Order(_columns.Length, first, count, by);
    private static int[] Order(int length, int first, int count, int by)
    {
        if (by is not (1 or -1) || first < 0 || count < 1 || first + count > length) return null;
        var neighbour = by < 0 ? first - 1 : first + count;
        if (neighbour < 0 || neighbour >= length) return null;
        var order = Enumerable.Range(0, length).ToArray();
        var start = Math.Min(first, neighbour);
        var block = Enumerable.Range(first, count).ToArray();
        var moved = by < 0 ? block.Append(neighbour) : block.Prepend(neighbour);
        foreach (var old in moved) order[start++] = old;
        return order;
    }
    private static int[] Positions(int[] order)
    {
        var position = new int[order.Length];
        for (int n = 0; n < order.Length; n++) position[order[n]] = n;
        return position;
    }
    // Every merged cell's rows (or columns) stay together and in order after the move.
    private bool MergesKeep(int[] order, bool rows)
    {
        var position = Positions(order);
        foreach (var m in _merges)
        {
            var (from, span) = rows ? (m.Row, m.RowSpan) : (m.Column, m.ColumnSpan);
            for (int i = 1; i < span; i++) if (position[from + i] != position[from] + i) return false;
        }
        return true;
    }

    /// <summary>
    /// Merges <paramref name="area"/> into one cell. Merged areas inside it join it; one that crosses its edge is refused.
    /// No text is lost: the texts of the cells, in reading order, become the merged cell's lines.
    /// </summary>
    public TableContent Merge(TableMerge area)
    {
        if (area.RowSpan < 1 || area.ColumnSpan < 1 || area.RowSpan * area.ColumnSpan < 2) throw new ArgumentException("Choose at least two cells to merge.");
        Index(area.Row, area.Column); Index(area.LastRow, area.LastColumn);
        if (_merges.Any(m => m.Overlaps(area) && !m.Inside(area))) throw new ArgumentException("The cells to merge cross a merged cell; include all of it or none.");
        var cells = (TableCell[])_cells.Clone();
        var texts = new List<string>();
        for (int r = area.Row; r <= area.LastRow; r++)
            for (int c = area.Column; c <= area.LastColumn; c++)
            {
                if (cells[Index(r, c)].Text.Length != 0) texts.Add(cells[Index(r, c)].Text);
                if (r != area.Row || c != area.Column) cells[Index(r, c)] = cells[Index(r, c)] with { Text = "" };
            }
        cells[Index(area.Row, area.Column)] = cells[Index(area.Row, area.Column)] with { Text = string.Join("\n", texts) };
        return new(_columns, _rows, cells, _merges.Where(m => !m.Inside(area)).Append(area), HeaderRow, RightToLeft);
    }
    /// <summary>Splits the merged area that covers a cell back into its cells; the text stays in the top-left one.</summary>
    public TableContent Unmerge(int row, int column)
    {
        var merge = MergeAt(row, column) ?? throw new ArgumentException("This cell is not merged.");
        return new(_columns, _rows, _cells, _merges.Where(m => m != merge), HeaderRow, RightToLeft);
    }

    public bool Equals(TableContent other) => other is not null && HeaderRow == other.HeaderRow && RightToLeft == other.RightToLeft
        && _columns.SequenceEqual(other._columns) && _rows.SequenceEqual(other._rows)
        && _cells.SequenceEqual(other._cells) && _merges.SequenceEqual(other._merges);
    public override bool Equals(object obj) => Equals(obj as TableContent);
    public override int GetHashCode()
    {
        var hash = new HashCode(); hash.Add(HeaderRow); hash.Add(RightToLeft); hash.Add(_columns.Length); hash.Add(_rows.Length);
        foreach (var cell in _cells) hash.Add(cell);
        return hash.ToHashCode();
    }

    private static bool IsColor(string value) => (value.Length == 7 || value.Length == 9) && value[0] == '#' && SKColor.TryParse(value, out _);

    // The persisted form: one JSON object with exactly these members, nothing else accepted.
    private sealed class Json
    {
        [JsonRequired, JsonPropertyName("columns")] public float[] Columns { get; set; }
        [JsonRequired, JsonPropertyName("rows")] public float[] Rows { get; set; }
        [JsonRequired, JsonPropertyName("header")] public bool Header { get; set; }
        [JsonRequired, JsonPropertyName("rtl")] public bool RightToLeft { get; set; }
        [JsonRequired, JsonPropertyName("cells")] public CellJson[] Cells { get; set; }
        [JsonRequired, JsonPropertyName("merges")] public MergeJson[] Merges { get; set; }
    }
    private sealed class CellJson
    {
        [JsonRequired, JsonPropertyName("text")] public string Text { get; set; }
        [JsonRequired, JsonPropertyName("fill")] public string Fill { get; set; }
        [JsonRequired, JsonPropertyName("align")] public string Align { get; set; }
        [JsonRequired, JsonPropertyName("valign")] public string VerticalAlign { get; set; }
        [JsonRequired, JsonPropertyName("font")] public string Font { get; set; }
    }
    private sealed class MergeJson
    {
        [JsonRequired, JsonPropertyName("row")] public int Row { get; set; }
        [JsonRequired, JsonPropertyName("column")] public int Column { get; set; }
        [JsonRequired, JsonPropertyName("rows")] public int Rows { get; set; }
        [JsonRequired, JsonPropertyName("columns")] public int Columns { get; set; }
    }
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        NumberHandling = JsonNumberHandling.Strict,
    };

    /// <summary>The persisted form: a JSON object of columns, rows, header, cells (row by row) and merges.</summary>
    public string ToJson() => JsonSerializer.Serialize(new Json
    {
        Columns = _columns, Rows = _rows, Header = HeaderRow, RightToLeft = RightToLeft,
        Cells = _cells.Select(c => new CellJson { Text = c.Text, Fill = c.Fill, Align = c.Align.ToString(), VerticalAlign = c.VerticalAlign.ToString(), Font = c.FontFamily }).ToArray(),
        Merges = _merges.Select(m => new MergeJson { Row = m.Row, Column = m.Column, Rows = m.RowSpan, Columns = m.ColumnSpan }).ToArray(),
    }, JsonOptions);

    /// <summary>Reads the persisted form strictly: every member present, none unknown, enums by name.</summary>
    public static TableContent FromJson(string json)
    {
        if (json is null) throw new ArgumentNullException(nameof(json));
        Json value;
        try { value = JsonSerializer.Deserialize<Json>(json, JsonOptions); }
        catch (JsonException ex) { throw new ArgumentException("The table is not in its JSON form.", nameof(json), ex); }
        if (value?.Columns is null || value.Rows is null || value.Cells is null || value.Merges is null
            || value.Cells.Any(c => c is null || c.Text is null || c.Fill is null || c.Align is null || c.VerticalAlign is null || c.Font is null)
            || value.Merges.Any(m => m is null))
            throw new ArgumentException("The table is missing a member.", nameof(json));
        return new(value.Columns, value.Rows,
            value.Cells.Select(c => new TableCell(c.Text, c.Fill, ParseEnum<SketchTextAlign>(c.Align), ParseEnum<TableVerticalAlign>(c.VerticalAlign), c.Font)),
            value.Merges.Select(m => new TableMerge(m.Row, m.Column, m.Rows, m.Columns)), value.Header, value.RightToLeft);
    }
    private static T ParseEnum<T>(string text) where T : struct, Enum =>
        Enum.TryParse<T>(text, false, out var value) && Enum.IsDefined(typeof(T), value) && value.ToString() == text
            ? value : throw new ArgumentException("The table has an unknown alignment name.");

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{RowCount}×{ColumnCount} table");
}

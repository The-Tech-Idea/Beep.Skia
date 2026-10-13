using System;
using System.Globalization;
using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Components;

/// <summary>
/// A table on the canvas: rows and columns of cells whose text wraps and makes its row taller, merged cells, an optional
/// header row and one look for its lines and text. What it holds is <see cref="Content"/> (immutable; every edit sets new
/// content); <see cref="TableDrawer"/> lays it out, draws it and finds cells. Its size always follows its content: setting
/// <see cref="SkiaComponent.Width"/> or <see cref="SkiaComponent.Height"/> scales the columns and the rows' minimum heights.
/// Everything it draws is persisted through its node properties, so any Beep.Skia host can save and load it; the font
/// families its cells name must be registered first.
/// </summary>
public sealed class TableShape : SkiaComponent, IRetainedVisual
{
    /// <summary>Line and text colour of a new table.</summary>
    public const string DefaultStroke = "#1e1e1e";
    /// <summary>A table without a background.</summary>
    public const string NoFill = "#00000000";
    public const float DefaultFontSize = 16, DefaultColumnWidth = 120, DefaultRowHeight = 40;
    private TableContent _content;
    private TableDrawer _drawer;
    private string _stroke = DefaultStroke, _fill = NoFill, _fontFamily = SketchShape.TextFontFamily;
    private float _strokeWidth = 1, _fontSize = DefaultFontSize;
    private long _version;
    private bool _sizing;
    private SKPicture _picture;
    private long _pictureVersion = -1;

    /// <summary>A table without content yet, for loaders that set <see cref="Table"/> afterwards.</summary>
    public TableShape()
    {
        ShowDisplayText = false; DisplayText = ""; Name = "Table";
        Persist();
    }
    public TableShape(TableContent content) : this() => Content = content;
    /// <summary>A table loaded with its persisted identity, before content is set.</summary>
    public TableShape(Guid id) : this() => RestoreIdentity(id);

    /// <summary>The table's rows, columns and cells. Setting it lays the table out and sizes the component to it.</summary>
    public TableContent Content
    {
        get => _content;
        set
        {
            var drawer = new TableDrawer(value ?? throw new ArgumentNullException(nameof(value)), _fontSize);
            _content = value; Relayout(drawer);
        }
    }
    /// <summary>The layout of the current content; null before content is set.</summary>
    public TableDrawer Layout => _drawer;

    /// <summary>The content in its persisted JSON form (<see cref="TableContent.ToJson"/>).</summary>
    public string Table
    {
        get => _content?.ToJson() ?? "";
        set => Content = TableContent.FromJson(value);
    }
    /// <summary>The colour of the lines and the text, #RRGGBB or #AARRGGBB.</summary>
    public string Stroke
    {
        get => _stroke;
        set { ValidateColor(value); _stroke = value; Changed(); }
    }
    /// <summary>The table's background, #RRGGBB or #AARRGGBB (<see cref="NoFill"/> for none); a cell's own fill covers it.</summary>
    public string Fill
    {
        get => _fill;
        set { ValidateColor(value); _fill = value; Changed(); }
    }
    /// <summary>The bundled family the table's text is meant to be drawn in. Each cell names the family it is actually drawn
    /// in (<see cref="TableCell.FontFamily"/>): the host picks this one when it has every character of the cell's text.</summary>
    public string FontFamily
    {
        get => _fontFamily;
        set
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Name the table's font family.", nameof(value));
            _fontFamily = value; Changed();
        }
    }
    /// <summary>The width of the lines; 0 draws none.</summary>
    public float StrokeWidth
    {
        get => _strokeWidth;
        set
        {
            if (!float.IsFinite(value) || value < 0 || value > 20) throw new ArgumentOutOfRangeException(nameof(value), "A table's lines are 0 to 20 units wide.");
            _strokeWidth = value; Changed();
        }
    }
    /// <summary>The text size of every cell. Changing it lays the table out again.</summary>
    public new float TextFontSize
    {
        get => _fontSize;
        set
        {
            if (!float.IsFinite(value) || value < 4 || value > 400) throw new ArgumentOutOfRangeException(nameof(value), "Table text is 4 to 400 units tall.");
            var drawer = _content is null ? null : new TableDrawer(_content, value);
            _fontSize = value;
            if (drawer is null) Changed(); else Relayout(drawer);
        }
    }
    /// <summary>Opacity as a whole percent, 0 to 100.</summary>
    public int OpacityPercent
    {
        get => (int)MathF.Round(base.Opacity * 100);
        set
        {
            if (value < 0 || value > 100) throw new ArgumentOutOfRangeException(nameof(value));
            base.Opacity = value / 100f;
        }
    }

    /// <summary>The background a header row shows under cells without their own fill: the line colour, lightly.</summary>
    public SKColor HeaderFill => SKColor.Parse(_stroke).WithAlpha((byte)Math.Round(SKColor.Parse(_stroke).Alpha * 0.12));

    /// <summary>The cell at a world point (the top-left cell of a merged cell); null outside the table or before content.</summary>
    public (int Row, int Column)? CellAt(SKPoint world) => _drawer?.CellAt(new SKPoint(world.X - X, world.Y - Y));
    /// <summary>The world rectangle a cell is drawn in (its merged cell's, when merged).</summary>
    public SKRect CellBounds(int row, int column)
    {
        var bounds = (_drawer ?? throw new InvalidOperationException("The table has no content.")).CellBounds(row, column);
        bounds.Offset(X, Y); return bounds;
    }

    public RetainedVisualKey VisualKey => RetainedVisualKey.Of(this, _version);
    public bool TryGetInkBounds(out SKRect bounds)
    {
        bounds = SKRect.Empty;
        if (_drawer is null || !IsVisible || State == ComponentState.Disposing || State == ComponentState.Inactive) return true;
        bounds = new SKRect(X, Y, X + Width, Y + Height);
        bounds.Inflate(_strokeWidth / 2, _strokeWidth / 2);
        if (ClipBounds is { } clip) bounds = bounds.IntersectsWith(clip) ? SKRect.Intersect(bounds, clip) : SKRect.Empty;
        return true;
    }

    public override bool ContainsPoint(SKPoint point) => _drawer is not null && new SKRect(X, Y, X + Width, Y + Height).Contains(point);

    protected override void DrawContent(SKCanvas canvas, DrawingContext context)
    {
        if (_drawer is null) return;
        if (ClipBounds is { } clip) canvas.ClipRect(clip);
        if (_picture is null || _pictureVersion != _version)
        {
            // The content is recorded once per change; frames draw the recording at the table's place.
            using var recorder = new SKPictureRecorder();
            var cull = new SKRect(0, 0, Width, Height); cull.Inflate(_strokeWidth + 2, _strokeWidth + 2);
            var recording = recorder.BeginRecording(cull);
            _drawer.Draw(recording, SKPoint.Empty, new TableLook(SKColor.Parse(_stroke), SKColor.Parse(_fill), HeaderFill, _strokeWidth));
            _picture?.Dispose(); _picture = recorder.EndRecording(); _pictureVersion = _version;
        }
        var save = canvas.Save();
        canvas.Translate(X, Y);
        canvas.DrawPicture(_picture);
        canvas.RestoreToCount(save);
    }

    protected override void UpdateBounds()
    {
        base.UpdateBounds();
        // A host that resizes the table scales its columns and rows; the size then follows the scaled content.
        if (_sizing || _drawer is null || Width <= 0 || Height <= 0) return;
        var size = _drawer.Size;
        if (Width == size.Width && Height == size.Height) return;
        Content = _content.Scaled(Width / size.Width, Height / size.Height);
    }

    protected override void OnOpacityChanged()
    {
        UpsertNodeProperty("Opacity", typeof(string), currentValue: OpacityPercent.ToString(CultureInfo.InvariantCulture));
        _version++;
    }

    private void Relayout(TableDrawer drawer)
    {
        _drawer = drawer;
        _sizing = true;
        try { Width = drawer.Size.Width; Height = drawer.Size.Height; }
        finally { _sizing = false; }
        Changed();
    }
    private void Changed() { _version++; Persist(); }
    private void Persist()
    {
        UpsertNodeProperty(nameof(Table), typeof(string), currentValue: Table);
        UpsertNodeProperty(nameof(Stroke), typeof(string), currentValue: _stroke);
        UpsertNodeProperty(nameof(Fill), typeof(string), currentValue: _fill);
        UpsertNodeProperty(nameof(StrokeWidth), typeof(string), currentValue: _strokeWidth.ToString("R", CultureInfo.InvariantCulture));
        UpsertNodeProperty(nameof(TextFontSize), typeof(string), currentValue: _fontSize.ToString("R", CultureInfo.InvariantCulture));
        UpsertNodeProperty(nameof(FontFamily), typeof(string), currentValue: _fontFamily);
    }
    private static void ValidateColor(string value)
    {
        if (value == null || (value.Length != 7 && value.Length != 9) || value[0] != '#' || !SKColor.TryParse(value, out _))
            throw new ArgumentException("Use a #RRGGBB or #AARRGGBB color.", nameof(value));
    }

    protected override void DisposeUnmanagedResources()
    {
        _picture?.Dispose(); _picture = null;
        base.DisposeUnmanagedResources();
    }
}

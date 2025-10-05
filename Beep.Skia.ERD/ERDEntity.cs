using SkiaSharp;
using Beep.Skia.Model;
using Beep.Skia.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Beep.Skia.ERD
{
    /// <summary>
    /// ERD Entity: rectangle with title band and attribute list area.
    /// </summary>
    public class ERDEntity : ERDControl
    {
        private const float TitleHeight = 24f;
        private const float RowHeight = 18f;
        private const float TopPadding = 18f;   // space between title band and first row baseline
        private const float BottomPadding = 8f; // space under the last row

        private class RowEntry
        {
            public Guid Id { get; set; }
            public string Text { get; set; } = string.Empty;
            public string DataType { get; set; } = "string";
            public bool IsPrimaryKey { get; set; }
            public bool IsForeignKey { get; set; }
            public bool IsNullable { get; set; } = true;
        }

        private string _entityName = "Entity";
        public string EntityName
        {
            get => _entityName;
            set
            {
                if (_entityName == value) return;
                _entityName = value ?? string.Empty;
                // Sync NodeProperties for editor/serialization
                if (NodeProperties != null)
                {
                    if (!NodeProperties.TryGetValue("EntityName", out var p) || p == null)
                    {
                        NodeProperties["EntityName"] = new Beep.Skia.Model.ParameterInfo
                        {
                            ParameterName = "EntityName",
                            ParameterType = typeof(string),
                            DefaultParameterValue = _entityName,
                            ParameterCurrentValue = _entityName,
                            Description = "Entity name/title"
                        };
                    }
                    else
                    {
                        p.ParameterType = typeof(string);
                        p.ParameterCurrentValue = _entityName;
                        if (p.DefaultParameterValue == null) p.DefaultParameterValue = _entityName;
                    }
                }
                InvalidateVisual();
            }
        }

        // Multiline or comma-separated rows; parsed into _rows for rendering/persistence
    private string _rowsText = "Id\nName";
        public string RowsText
        {
            get => _rowsText;
            set
            {
                if (_rowsText == value) return;
                _rowsText = value ?? string.Empty;
                if (NodeProperties != null)
                {
                    if (!NodeProperties.TryGetValue("RowsText", out var p) || p == null)
                    {
                        NodeProperties["RowsText"] = new Beep.Skia.Model.ParameterInfo
                        {
                            ParameterName = "RowsText",
                            ParameterType = typeof(string),
                            DefaultParameterValue = _rowsText,
                            ParameterCurrentValue = _rowsText,
                            Description = "One row name per line or comma-separated"
                        };
                    }
                    else
                    {
                        p.ParameterType = typeof(string);
                        p.ParameterCurrentValue = _rowsText;
                        if (p.DefaultParameterValue == null) p.DefaultParameterValue = _rowsText;
                    }
                }
                ParseRows();
                InvalidateVisual();
            }
        }

        // Stable rows with IDs to keep CPs attached across reorders
        private readonly List<RowEntry> _rowEntries = new List<RowEntry>
        {
            new RowEntry { Id = Guid.NewGuid(), Text = "Id", DataType = "int", IsPrimaryKey = true, IsNullable = false },
            new RowEntry { Id = Guid.NewGuid(), Text = "Name", DataType = "string", IsNullable = false }
        };

        public IReadOnlyList<string> Rows => _rowEntries.Select(r => r.Text).ToList();

        // Map row ID -> its left/right CPs
        private readonly Dictionary<Guid, ConnectionPoint> _rowToInCp = new();
        private readonly Dictionary<Guid, ConnectionPoint> _rowToOutCp = new();

        // Persistable representation of row IDs alongside RowsText
        [Browsable(false)]
        public string RowIdsCsv
        {
            get => string.Join(",", _rowEntries.Select(e => e.Id));
            set
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                var parts = value.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < _rowEntries.Count && i < parts.Length; i++)
                {
                    if (Guid.TryParse(parts[i].Trim(), out var gid))
                    {
                        // If ID changed, update mapping while keeping CPs if present
                        var oldId = _rowEntries[i].Id;
                        if (oldId != gid)
                        {
                            ConnectionPoint? inOld = _rowToInCp.ContainsKey(oldId) ? _rowToInCp[oldId] : null;
                            ConnectionPoint? outOld = _rowToOutCp.ContainsKey(oldId) ? _rowToOutCp[oldId] : null;
                            _rowEntries[i].Id = gid;
                            if (inOld != null)
                            {
                                _rowToInCp.Remove(oldId);
                                _rowToInCp[gid] = inOld;
                            }
                            if (outOld != null)
                            {
                                _rowToOutCp.Remove(oldId);
                                _rowToOutCp[gid] = outOld;
                            }
                        }
                    }
                }
                SyncConnectionPointsWithRows();
                InvalidateVisual();
            }
        }

        public ERDEntity()
        {
            Name = "ERD Entity";
            DisplayText = string.Empty;
            // Seed NodeProperties for SetProperties/GetProperties flows
            NodeProperties["EntityName"] = new Beep.Skia.Model.ParameterInfo
            {
                ParameterName = "EntityName",
                ParameterType = typeof(string),
                DefaultParameterValue = _entityName,
                ParameterCurrentValue = _entityName,
                Description = "Entity name/title"
            };
            // Enterprise: optional database schema and comment/description
            NodeProperties["Schema"] = new Beep.Skia.Model.ParameterInfo
            {
                ParameterName = "Schema",
                ParameterType = typeof(string),
                DefaultParameterValue = string.Empty,
                ParameterCurrentValue = string.Empty,
                Description = "Database schema name (e.g., dbo)"
            };
            NodeProperties["Comment"] = new Beep.Skia.Model.ParameterInfo
            {
                ParameterName = "Comment",
                ParameterType = typeof(string),
                DefaultParameterValue = string.Empty,
                ParameterCurrentValue = string.Empty,
                Description = "Entity/table comment"
            };
            NodeProperties["RowsText"] = new Beep.Skia.Model.ParameterInfo
            {
                ParameterName = "RowsText",
                ParameterType = typeof(string),
                DefaultParameterValue = _rowsText,
                ParameterCurrentValue = _rowsText,
                Description = "One row name per line or comma-separated"
            };
            // initialize CPs for default rows
            SyncConnectionPointsWithRows();
            AdjustHeightToRows();

            // Seed a structured Columns NodeProperty for advanced editors (JSON serialized)
            try
            {
                var columnsJson = System.Text.Json.JsonSerializer.Serialize(ToColumns());
                NodeProperties["Columns"] = new ParameterInfo
                {
                    ParameterName = "Columns",
                    ParameterType = typeof(string),
                    DefaultParameterValue = columnsJson,
                    ParameterCurrentValue = columnsJson,
                    Description = "Structured column list (JSON array of ColumnDefinition)"
                };
                // Enterprise: seed Indexes and ForeignKeys NodeProperties as JSON arrays
                var emptyIndexes = System.Text.Json.JsonSerializer.Serialize(new System.Collections.Generic.List<Beep.Skia.Model.IndexDefinition>());
                NodeProperties["Indexes"] = new ParameterInfo
                {
                    ParameterName = "Indexes",
                    ParameterType = typeof(string),
                    DefaultParameterValue = emptyIndexes,
                    ParameterCurrentValue = emptyIndexes,
                    Description = "Indexes (JSON array of IndexDefinition)"
                };
                var emptyFks = System.Text.Json.JsonSerializer.Serialize(new System.Collections.Generic.List<Beep.Skia.Model.ForeignKeyDefinition>());
                NodeProperties["ForeignKeys"] = new ParameterInfo
                {
                    ParameterName = "ForeignKeys",
                    ParameterType = typeof(string),
                    DefaultParameterValue = emptyFks,
                    ParameterCurrentValue = emptyFks,
                    Description = "Foreign keys (JSON array of ForeignKeyDefinition)"
                };
            }
            catch { }
        }

        private void ParseRows()
        {
            var texts = new List<string>();
            if (!string.IsNullOrWhiteSpace(_rowsText))
            {
                // Support both newline and comma separation; trim and drop empties
                var parts = _rowsText
                    .Replace("\r", string.Empty)
                    .Split(new[] { '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    var t = p.Trim();
                    if (t.Length > 0) texts.Add(t);
                }
            }
            ReconcileRows(texts);
            AdjustHeightToRows();
            SyncConnectionPointsWithRows();
            // keep Columns JSON in sync with simple rows
            TrySyncColumnsFromRows();
        }

        private void ReconcileRows(List<string> newTexts)
        {
            // Build multimap of existing entries by text to reuse IDs across reorders/duplicates
            var byText = _rowEntries
                .GroupBy(e => e.Text)
                .ToDictionary(g => g.Key, g => new Queue<RowEntry>(g));

            var nextEntries = new List<RowEntry>(newTexts.Count);
            foreach (var text in newTexts)
            {
                if (byText.TryGetValue(text, out var q) && q.Count > 0)
                {
                    // reuse an existing entry (preserves ID)
                    var e = q.Dequeue();
                    e.Text = text; // update text in case of trimming changes
                    nextEntries.Add(e);
                }
                else
                {
                    // new row
                    nextEntries.Add(new RowEntry { Id = Guid.NewGuid(), Text = text });
                }
            }

            // Remove CPs for rows that no longer exist
            var removedIds = _rowEntries.Select(e => e.Id).Except(nextEntries.Select(e => e.Id)).ToList();
            foreach (var rid in removedIds)
            {
                if (_rowToInCp.TryGetValue(rid, out var inCp)) InConnectionPoints.Remove(inCp);
                if (_rowToOutCp.TryGetValue(rid, out var outCp)) OutConnectionPoints.Remove(outCp);
                _rowToInCp.Remove(rid);
                _rowToOutCp.Remove(rid);
            }

            _rowEntries.Clear();
            _rowEntries.AddRange(nextEntries);
            // sync Columns JSON when structure changes
            TrySyncColumnsFromRows();
        }

        private void SyncConnectionPointsWithRows()
        {
            // Ensure a CP pair exists for each row ID and reorder lists to match row order
            foreach (var entry in _rowEntries)
            {
                if (!_rowToInCp.ContainsKey(entry.Id))
                {
                    var cp = new ConnectionPoint { Type = ConnectionPointType.In, Shape = ComponentShape.Circle, DataType = "link", IsAvailable = true, Component = this, Radius = (int)PortRadius };
                    cp.RowId = entry.Id;
                    _rowToInCp[entry.Id] = cp;
                    InConnectionPoints.Add(cp);
                }
                if (!_rowToOutCp.ContainsKey(entry.Id))
                {
                    var cp = new ConnectionPoint { Type = ConnectionPointType.Out, Shape = ComponentShape.Circle, DataType = "link", IsAvailable = true, Component = this, Radius = (int)PortRadius };
                    cp.RowId = entry.Id;
                    _rowToOutCp[entry.Id] = cp;
                    OutConnectionPoints.Add(cp);
                }
            }

            // Reorder lists to match row order
            var newIn = new List<ConnectionPoint>(_rowEntries.Count);
            var newOut = new List<ConnectionPoint>(_rowEntries.Count);
            for (int i = 0; i < _rowEntries.Count; i++)
            {
                var id = _rowEntries[i].Id;
                var inCp = _rowToInCp[id];
                var outCp = _rowToOutCp[id];
                inCp.Index = i; inCp.Component = this; inCp.IsAvailable = true;
                outCp.Index = i; outCp.Component = this; outCp.IsAvailable = true;
                newIn.Add(inCp);
                newOut.Add(outCp);
            }
            InConnectionPoints.Clear();
            InConnectionPoints.AddRange(newIn);
            OutConnectionPoints.Clear();
            OutConnectionPoints.AddRange(newOut);
        }

        private void AdjustHeightToRows()
        {
            float desired = TitleHeight + TopPadding + (_rowEntries.Count * RowHeight) + BottomPadding;
            if (desired < 80f) desired = 80f; // minimum height
            if (Math.Abs(Height - desired) > 0.1f)
            {
                Height = desired;
            }
        }

        protected override void LayoutPorts()
        {
            // One left (In) and one right (Out) port per row; align to row centers
            var b = Bounds;
            // Safety: keep counts in sync if called outside ParseRows
            if (InConnectionPoints.Count != _rowEntries.Count || OutConnectionPoints.Count != _rowEntries.Count)
            {
                SyncConnectionPointsWithRows();
            }

            float leftX = b.Left - 2f;
            float rightX = b.Right + 2f;

            for (int i = 0; i < _rowEntries.Count && i < InConnectionPoints.Count; i++)
            {
                float cy = b.Top + TitleHeight + (i * RowHeight) + (RowHeight / 2f);
                var cp = InConnectionPoints[i];
                cp.Center = new SKPoint(leftX, cy);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(leftX - PortRadius, cy - PortRadius, leftX + PortRadius, cy + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = i;
                cp.Component = this;
                cp.IsAvailable = true;
            }

            for (int i = 0; i < _rowEntries.Count && i < OutConnectionPoints.Count; i++)
            {
                float cy = b.Top + TitleHeight + (i * RowHeight) + (RowHeight / 2f);
                var cp = OutConnectionPoints[i];
                cp.Center = new SKPoint(rightX, cy);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(rightX - PortRadius, cy - PortRadius, rightX + PortRadius, cy + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = i;
                cp.Component = this;
                cp.IsAvailable = true;
            }
        }

        protected override void DrawERDContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var rect = Bounds;
            float titleHeight = TitleHeight;

            using var bodyFill = new SKPaint { Color = MaterialControl.MaterialColors.Surface, IsAntialias = true };
            using var border = new SKPaint { Color = MaterialControl.MaterialColors.Outline, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };
            using var titleFill = new SKPaint { Color = MaterialControl.MaterialColors.PrimaryContainer, IsAntialias = true };
            using var titleTextPaint = new SKPaint { Color = MaterialControl.MaterialColors.OnPrimaryContainer, IsAntialias = true };
            using var textPaint = new SKPaint { Color = MaterialControl.MaterialColors.OnSurface, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);

            // Body and border
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, bodyFill);
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, border);

            // Title band
            var titleRect = new SKRect(rect.Left, rect.Top, rect.Right, rect.Top + titleHeight);
            canvas.DrawRoundRect(titleRect, CornerRadius, CornerRadius, titleFill);
            // Cover the bottom corners of title band to keep only top corners rounded
            using (var eraser = new SKPaint { BlendMode = SKBlendMode.Src })
            {
                var flat = new SKRect(rect.Left, rect.Top + titleHeight - 1, rect.Right, rect.Top + titleHeight);
                canvas.DrawRect(flat, titleFill);
            }

            // Title text
            var titleX = rect.MidX - font.MeasureText(EntityName, titleTextPaint) / 2;
            var titleY = rect.Top + titleHeight - 6;
            canvas.DrawText(EntityName, titleX, titleY, SKTextAlign.Left, font, titleTextPaint);

            // Attribute list
            float y = rect.Top + titleHeight + TopPadding;
            foreach (var e in _rowEntries)
            {
                var a = e.Text;
                if (string.IsNullOrEmpty(a)) continue;
                var tx = rect.Left + 10;
                // prefix PK/FK markers
                string prefix = e.IsPrimaryKey ? "🔑 " : (e.IsForeignKey ? "🔗 " : string.Empty);
                string rowText = string.IsNullOrEmpty(e.DataType) ? a : $"{a}: {e.DataType}";
                canvas.DrawText(prefix + rowText, tx, y, SKTextAlign.Left, font, textPaint);
                y += RowHeight;
                if (y > rect.Bottom - 8) break;
            }

            DrawPorts(canvas);
        }

        // Hide port counts in property editor for ERDEntity; rows drive counts
        [Browsable(false)]
        public new int InPortCount { get => base.InPortCount; set => base.InPortCount = value; }
        [Browsable(false)]
        public new int OutPortCount { get => base.OutPortCount; set => base.OutPortCount = value; }

        // --- Columns <-> Rows synchronization helpers ---
        private System.Collections.Generic.List<Beep.Skia.Model.ColumnDefinition> ToColumns()
        {
            var list = new System.Collections.Generic.List<Beep.Skia.Model.ColumnDefinition>(_rowEntries.Count);
            foreach (var r in _rowEntries)
            {
                list.Add(new Beep.Skia.Model.ColumnDefinition
                {
                    Id = r.Id,
                    Name = r.Text ?? string.Empty,
                    DataType = r.DataType ?? "string",
                    IsPrimaryKey = r.IsPrimaryKey,
                    IsForeignKey = r.IsForeignKey,
                    IsNullable = r.IsNullable,
                    Description = string.Empty
                });
            }
            return list;
        }

        private void TrySyncColumnsFromRows()
        {
            try
            {
                if (NodeProperties != null && NodeProperties.TryGetValue("Columns", out var p) && p != null)
                {
                    p.ParameterCurrentValue = System.Text.Json.JsonSerializer.Serialize(ToColumns());
                }
            }
            catch { }
        }

        /// <summary>
        /// Public JSON property exposing columns in a structured form for enterprise editors.
        /// On set, updates the internal rows and connection points.
        /// </summary>
        [Browsable(true)]
        public string Columns
        {
            get
            {
                try { return System.Text.Json.JsonSerializer.Serialize(ToColumns()); } catch { return "[]"; }
            }
            set
            {
                try
                {
                    var newCols = System.Text.Json.JsonSerializer.Deserialize<List<Beep.Skia.Model.ColumnDefinition>>(value ?? "[]") ?? new();
                    // Build lookup by Id when present, else by Name
                    var byId = new Dictionary<Guid, Beep.Skia.Model.ColumnDefinition>();
                    foreach (var c in newCols)
                    {
                        if (c.Id != Guid.Empty && !byId.ContainsKey(c.Id)) byId[c.Id] = c;
                    }
                    var next = new List<RowEntry>(newCols.Count);
                    foreach (var c in newCols)
                    {
                        RowEntry? e = null;
                        if (c.Id != Guid.Empty)
                        {
                            var existing = _rowEntries.FirstOrDefault(r => r.Id == c.Id);
                            if (existing != null) e = existing;
                        }
                        if (e == null && !string.IsNullOrWhiteSpace(c.Name))
                        {
                            e = _rowEntries.FirstOrDefault(r => string.Equals(r.Text, c.Name, StringComparison.OrdinalIgnoreCase));
                        }
                        if (e == null)
                        {
                            e = new RowEntry { Id = c.Id != Guid.Empty ? c.Id : Guid.NewGuid() };
                        }
                        e.Text = c.Name ?? string.Empty;
                        e.DataType = c.DataType ?? (e.DataType ?? "string");
                        e.IsPrimaryKey = c.IsPrimaryKey;
                        e.IsForeignKey = c.IsForeignKey;
                        e.IsNullable = c.IsNullable;
                        next.Add(e);
                    }

                    // Remove CPs for rows that no longer exist
                    var removedIds = _rowEntries.Select(r => r.Id).Except(next.Select(r => r.Id)).ToList();
                    foreach (var rid in removedIds)
                    {
                        if (_rowToInCp.TryGetValue(rid, out var inCp)) InConnectionPoints.Remove(inCp);
                        if (_rowToOutCp.TryGetValue(rid, out var outCp)) OutConnectionPoints.Remove(outCp);
                        _rowToInCp.Remove(rid);
                        _rowToOutCp.Remove(rid);
                    }

                    _rowEntries.Clear();
                    _rowEntries.AddRange(next);

                    // Keep NodeProperties[Columns] in sync
                    try
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(ToColumns());
                        if (NodeProperties.ContainsKey("Columns"))
                        {
                            NodeProperties["Columns"].ParameterCurrentValue = json;
                        }
                        else
                        {
                            NodeProperties["Columns"] = new ParameterInfo { ParameterName = "Columns", ParameterType = typeof(string), DefaultParameterValue = json, ParameterCurrentValue = json };
                        }
                    }
                    catch { }

                    SyncConnectionPointsWithRows();
                    AdjustHeightToRows();
                    InvalidateVisual();
                }
                catch { }
            }
        }

        /// <summary>
        /// Public JSON property exposing table indexes. Mirrors NodeProperties["Indexes"].
        /// </summary>
        [Browsable(true)]
        public string Indexes
        {
            get
            {
                try
                {
                    if (NodeProperties != null && NodeProperties.TryGetValue("Indexes", out var p) && p != null)
                    {
                        return p.ParameterCurrentValue as string ?? p.DefaultParameterValue as string ?? "[]";
                    }
                }
                catch { }
                return "[]";
            }
            set
            {
                try
                {
                    if (NodeProperties != null)
                    {
                        if (!NodeProperties.ContainsKey("Indexes"))
                        {
                            NodeProperties["Indexes"] = new ParameterInfo { ParameterName = "Indexes", ParameterType = typeof(string) };
                        }
                        NodeProperties["Indexes"].ParameterCurrentValue = value ?? "[]";
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Public JSON property exposing foreign keys. Mirrors NodeProperties["ForeignKeys"].
        /// </summary>
        [Browsable(true)]
        public string ForeignKeys
        {
            get
            {
                try
                {
                    if (NodeProperties != null && NodeProperties.TryGetValue("ForeignKeys", out var p) && p != null)
                    {
                        return p.ParameterCurrentValue as string ?? p.DefaultParameterValue as string ?? "[]";
                    }
                }
                catch { }
                return "[]";
            }
            set
            {
                try
                {
                    if (NodeProperties != null)
                    {
                        if (!NodeProperties.ContainsKey("ForeignKeys"))
                        {
                            NodeProperties["ForeignKeys"] = new ParameterInfo { ParameterName = "ForeignKeys", ParameterType = typeof(string) };
                        }
                        NodeProperties["ForeignKeys"].ParameterCurrentValue = value ?? "[]";
                    }
                }
                catch { }
            }
        }
    }
}

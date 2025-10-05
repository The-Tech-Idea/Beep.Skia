using SkiaSharp;
using Beep.Skia.Components;
using Beep.Skia.Model;
using System;
using System.Collections.Generic;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// ETL Target (Sink) node representing a destination like a table, file, or topic.
    /// Accepts one input, validates incoming schema vs ExpectedSchema, and shows warnings on mismatch.
    /// </summary>
    public class ETLTarget : ETLControl
    {
        public enum DestinationKind { Table, File, Topic, Stream, Api }
        public enum WriteMode { Append, Overwrite, Upsert }

        private string _connectionString = string.Empty;
        public string ConnectionString
        {
            get => _connectionString;
            set
            {
                var v = value ?? string.Empty;
                if (_connectionString == v) return;
                _connectionString = v;
                if (NodeProperties.TryGetValue("ConnectionString", out var p)) p.ParameterCurrentValue = _connectionString; else NodeProperties["ConnectionString"] = new ParameterInfo { ParameterName = "ConnectionString", ParameterType = typeof(string), DefaultParameterValue = _connectionString, ParameterCurrentValue = _connectionString, Description = "Destination connection string (masked in UI)" };
                InvalidateVisual();
            }
        }

        private DestinationKind _destination = DestinationKind.Table;
        public DestinationKind Destination
        {
            get => _destination;
            set
            {
                if (_destination == value) return;
                _destination = value;
                if (NodeProperties.TryGetValue("Destination", out var p)) p.ParameterCurrentValue = _destination; else NodeProperties["Destination"] = new ParameterInfo { ParameterName = "Destination", ParameterType = typeof(DestinationKind), DefaultParameterValue = _destination, ParameterCurrentValue = _destination, Description = "Destination kind", Choices = Enum.GetNames(typeof(DestinationKind)) };
                InvalidateVisual();
            }
        }

        private string _tableName = string.Empty;
        public string TableName
        {
            get => _tableName;
            set
            {
                var v = value ?? string.Empty;
                if (_tableName == v) return;
                _tableName = v;
                if (NodeProperties.TryGetValue("TableName", out var p)) p.ParameterCurrentValue = _tableName; else NodeProperties["TableName"] = new ParameterInfo { ParameterName = "TableName", ParameterType = typeof(string), DefaultParameterValue = _tableName, ParameterCurrentValue = _tableName, Description = "Target table or object name" };
                InvalidateVisual();
            }
        }

        private WriteMode _writeMode = WriteMode.Append;
        public WriteMode Mode
        {
            get => _writeMode;
            set
            {
                if (_writeMode == value) return;
                _writeMode = value;
                if (NodeProperties.TryGetValue("WriteMode", out var p)) p.ParameterCurrentValue = _writeMode; else NodeProperties["WriteMode"] = new ParameterInfo { ParameterName = "WriteMode", ParameterType = typeof(WriteMode), DefaultParameterValue = _writeMode, ParameterCurrentValue = _writeMode, Description = "Append/Overwrite/Upsert", Choices = Enum.GetNames(typeof(WriteMode)) };
                InvalidateVisual();
            }
        }

        private bool _preCreateTable = true;
        public bool PreCreateTable
        {
            get => _preCreateTable;
            set
            {
                if (_preCreateTable == value) return;
                _preCreateTable = value;
                if (NodeProperties.TryGetValue("PreCreateTable", out var p)) p.ParameterCurrentValue = _preCreateTable; else NodeProperties["PreCreateTable"] = new ParameterInfo { ParameterName = "PreCreateTable", ParameterType = typeof(bool), DefaultParameterValue = _preCreateTable, ParameterCurrentValue = _preCreateTable, Description = "Auto-create table from expected schema" };
                InvalidateVisual();
            }
        }

        // JSON array of ColumnDefinition for the target schema we expect
        private string _expectedSchemaJson = "[]";
        public string ExpectedSchema
        {
            get => _expectedSchemaJson;
            set
            {
                var v = value ?? "[]";
                if (_expectedSchemaJson == v) return;
                _expectedSchemaJson = v;
                if (NodeProperties.TryGetValue("ExpectedSchema", out var p)) p.ParameterCurrentValue = _expectedSchemaJson; else NodeProperties["ExpectedSchema"] = new ParameterInfo { ParameterName = "ExpectedSchema", ParameterType = typeof(string), DefaultParameterValue = _expectedSchemaJson, ParameterCurrentValue = _expectedSchemaJson, Description = "Expected schema (JSON array of ColumnDefinition)" };
                InvalidateVisual();
            }
        }

        private List<ColumnDefinition> _expected = new();

        private string _dataQualityRulesJson = "[]";
        public string DataQualityRules
        {
            get => _dataQualityRulesJson;
            set
            {
                var v = value ?? "[]";
                if (_dataQualityRulesJson == v) return;
                _dataQualityRulesJson = v;
                if (NodeProperties.TryGetValue("DataQualityRules", out var p))
                    p.ParameterCurrentValue = _dataQualityRulesJson;
                else
                    NodeProperties["DataQualityRules"] = new ParameterInfo
                    {
                        ParameterName = "DataQualityRules",
                        ParameterType = typeof(string),
                        DefaultParameterValue = _dataQualityRulesJson,
                        ParameterCurrentValue = _dataQualityRulesJson,
                        Description = "Data quality validation rules (JSON array of DataQualityRule)"
                    };
                InvalidateVisual();
            }
        }

        public ETLTarget()
        {
            Title = "Target";
            Subtitle = "Sink";
            Width = Math.Max(140, Width);
            Height = Math.Max(72, Height);
            EnsurePortCounts(1, 0);

            // Seed NodeProperties
            NodeProperties["ConnectionString"] = new ParameterInfo { ParameterName = "ConnectionString", ParameterType = typeof(string), DefaultParameterValue = _connectionString, ParameterCurrentValue = _connectionString, Description = "Destination connection (masked)" };
            NodeProperties["Destination"] = new ParameterInfo { ParameterName = "Destination", ParameterType = typeof(DestinationKind), DefaultParameterValue = _destination, ParameterCurrentValue = _destination, Choices = Enum.GetNames(typeof(DestinationKind)), Description = "Destination kind" };
            NodeProperties["TableName"] = new ParameterInfo { ParameterName = "TableName", ParameterType = typeof(string), DefaultParameterValue = _tableName, ParameterCurrentValue = _tableName, Description = "Target table/object name" };
            NodeProperties["WriteMode"] = new ParameterInfo { ParameterName = "WriteMode", ParameterType = typeof(WriteMode), DefaultParameterValue = _writeMode, ParameterCurrentValue = _writeMode, Choices = Enum.GetNames(typeof(WriteMode)), Description = "Append/Overwrite/Upsert" };
            NodeProperties["PreCreateTable"] = new ParameterInfo { ParameterName = "PreCreateTable", ParameterType = typeof(bool), DefaultParameterValue = _preCreateTable, ParameterCurrentValue = _preCreateTable, Description = "Auto-create table from expected schema" };
            NodeProperties["ExpectedSchema"] = new ParameterInfo { ParameterName = "ExpectedSchema", ParameterType = typeof(string), DefaultParameterValue = _expectedSchemaJson, ParameterCurrentValue = _expectedSchemaJson, Description = "Expected schema (JSON)" };
        }

        protected override void DrawETLContent(SKCanvas canvas, DrawingContext context)
        {
            base.DrawETLContent(canvas, context);
            // Show the first few expected columns
            try { _expected = System.Text.Json.JsonSerializer.Deserialize<List<ColumnDefinition>>(ExpectedSchema) ?? new(); } catch { _expected = new(); }
            if (_expected.Count > 0)
            {
                using var font = new SKFont { Size = 11 };
                using var paint = new SKPaint { Color = new SKColor(70, 70, 70), IsAntialias = true };
                float top = Y + HeaderHeight + 22f;
                int max = Math.Min(4, _expected.Count);
                for (int i = 0; i < max; i++)
                {
                    var c = _expected[i];
                    var line = string.IsNullOrEmpty(c.DataType) ? c.Name : $"{c.Name}: {c.DataType}";
                    canvas.DrawText(line, X + 8, top + i * 14, SKTextAlign.Left, font, paint);
                }
            }
        }
    }
}

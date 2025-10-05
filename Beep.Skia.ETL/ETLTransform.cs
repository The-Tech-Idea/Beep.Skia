using SkiaSharp;
using Beep.Skia.Components;
using Beep.Skia.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// ETL Transform node with basic operations: Select/Project, Filter, Map/Compute.
    /// Exposes OutputSchema (JSON of ColumnDefinition) for downstream nodes and lines.
    /// </summary>
    public class ETLTransform : ETLControl
    {
        public enum TransformKind { Select, Filter, Map, Aggregate, Join }

        private TransformKind _kind = TransformKind.Select;
        public TransformKind Kind
        {
            get => _kind;
            set
            {
                if (_kind == value) return;
                _kind = value;
                if (NodeProperties.TryGetValue("Kind", out var p)) p.ParameterCurrentValue = _kind; else NodeProperties["Kind"] = new ParameterInfo { ParameterName = "Kind", ParameterType = typeof(TransformKind), DefaultParameterValue = _kind, ParameterCurrentValue = _kind, Description = "Transform kind", Choices = System.Enum.GetNames(typeof(TransformKind)) };
                InvalidateVisual();
            }
        }

        private string _expression = string.Empty;
        public string Expression
        {
            get => _expression;
            set
            {
                var v = value ?? string.Empty;
                if (_expression == v) return;
                _expression = v;
                if (NodeProperties.TryGetValue("Expression", out var p)) p.ParameterCurrentValue = _expression; else NodeProperties["Expression"] = new ParameterInfo { ParameterName = "Expression", ParameterType = typeof(string), DefaultParameterValue = _expression, ParameterCurrentValue = _expression, Description = "Filter/Map expression (simple syntax)" };
                InvalidateVisual();
            }
        }

        // The resulting schema after transformation
        private string _outputSchemaJson = "[]";
        public string OutputSchema
        {
            get => _outputSchemaJson;
            set
            {
                var v = value ?? "[]";
                if (_outputSchemaJson == v) return;
                _outputSchemaJson = v;
                if (NodeProperties.TryGetValue("OutputSchema", out var p)) p.ParameterCurrentValue = _outputSchemaJson; else NodeProperties["OutputSchema"] = new ParameterInfo { ParameterName = "OutputSchema", ParameterType = typeof(string), DefaultParameterValue = _outputSchemaJson, ParameterCurrentValue = _outputSchemaJson, Description = "Output schema (JSON array of ColumnDefinition)" };
                InvalidateVisual();
            }
        }

        private List<ColumnDefinition> _schema = new();

        // Simple configurable hints for inference
        private string _groupByCsv = string.Empty; // comma-separated group-by column names
        public string GroupBy
        {
            get => _groupByCsv;
            set
            {
                var v = value ?? string.Empty;
                if (_groupByCsv == v) return;
                _groupByCsv = v;
                if (NodeProperties.TryGetValue("GroupBy", out var p)) p.ParameterCurrentValue = _groupByCsv; else NodeProperties["GroupBy"] = new ParameterInfo { ParameterName = "GroupBy", ParameterType = typeof(string), DefaultParameterValue = _groupByCsv, ParameterCurrentValue = _groupByCsv, Description = "Aggregate group-by columns (CSV)" };
                InvalidateVisual();
            }
        }

        private string _joinKeyLeft = string.Empty;
        public string JoinKeyLeft
        {
            get => _joinKeyLeft;
            set
            {
                var v = value ?? string.Empty;
                if (_joinKeyLeft == v) return; _joinKeyLeft = v;
                if (NodeProperties.TryGetValue("JoinKeyLeft", out var p)) p.ParameterCurrentValue = _joinKeyLeft; else NodeProperties["JoinKeyLeft"] = new ParameterInfo { ParameterName = "JoinKeyLeft", ParameterType = typeof(string), DefaultParameterValue = _joinKeyLeft, ParameterCurrentValue = _joinKeyLeft, Description = "Join key in left input" };
                InvalidateVisual();
            }
        }

        private string _joinKeyRight = string.Empty;
        public string JoinKeyRight
        {
            get => _joinKeyRight;
            set
            {
                var v = value ?? string.Empty;
                if (_joinKeyRight == v) return; _joinKeyRight = v;
                if (NodeProperties.TryGetValue("JoinKeyRight", out var p)) p.ParameterCurrentValue = _joinKeyRight; else NodeProperties["JoinKeyRight"] = new ParameterInfo { ParameterName = "JoinKeyRight", ParameterType = typeof(string), DefaultParameterValue = _joinKeyRight, ParameterCurrentValue = _joinKeyRight, Description = "Join key in right input" };
                InvalidateVisual();
            }
        }

        public ETLTransform()
        {
            Title = "Transform";
            Subtitle = "Select/Filter/Map";
            EnsurePortCounts(1, 1);
            HeaderColor = MaterialColors.SecondaryContainer;

            // Seed NodeProperties
            NodeProperties["Kind"] = new ParameterInfo { ParameterName = "Kind", ParameterType = typeof(TransformKind), DefaultParameterValue = _kind, ParameterCurrentValue = _kind, Choices = System.Enum.GetNames(typeof(TransformKind)), Description = "Transform kind" };
            NodeProperties["Expression"] = new ParameterInfo { ParameterName = "Expression", ParameterType = typeof(string), DefaultParameterValue = _expression, ParameterCurrentValue = _expression, Description = "Filter/Map expression" };
            NodeProperties["OutputSchema"] = new ParameterInfo { ParameterName = "OutputSchema", ParameterType = typeof(string), DefaultParameterValue = _outputSchemaJson, ParameterCurrentValue = _outputSchemaJson, Description = "Output schema (JSON)" };
            NodeProperties["GroupBy"] = new ParameterInfo { ParameterName = "GroupBy", ParameterType = typeof(string), DefaultParameterValue = _groupByCsv, ParameterCurrentValue = _groupByCsv, Description = "Aggregate group-by columns (CSV)" };
            NodeProperties["JoinKeyLeft"] = new ParameterInfo { ParameterName = "JoinKeyLeft", ParameterType = typeof(string), DefaultParameterValue = _joinKeyLeft, ParameterCurrentValue = _joinKeyLeft, Description = "Join key in left input" };
            NodeProperties["JoinKeyRight"] = new ParameterInfo { ParameterName = "JoinKeyRight", ParameterType = typeof(string), DefaultParameterValue = _joinKeyRight, ParameterCurrentValue = _joinKeyRight, Description = "Join key in right input" };
        }

        protected override void DrawETLContent(SKCanvas canvas, DrawingContext context)
        {
            base.DrawETLContent(canvas, context);
            // simple icon-like glyph for transform kind
            using var icon = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true, Style = SKPaintStyle.Fill };
            var cx = X + Width - 18; var cy = Y + HeaderHeight / 2f + 2;
            canvas.DrawCircle(cx, cy, 5, icon);
            canvas.DrawCircle(cx - 10, cy, 5, icon);
            canvas.DrawRect(cx - 16, cy - 2, 12, 4, icon);

            // show first few output columns
            try { _schema = System.Text.Json.JsonSerializer.Deserialize<List<ColumnDefinition>>(OutputSchema) ?? new(); } catch { _schema = new(); }
            if (_schema.Count > 0)
            {
                using var font = new SKFont { Size = 11 };
                using var paint = new SKPaint { Color = new SKColor(70, 70, 70), IsAntialias = true };
                float top = Y + HeaderHeight + 22f;
                int max = System.Math.Min(4, _schema.Count);
                for (int i = 0; i < max; i++)
                {
                    var c = _schema[i];
                    var line = string.IsNullOrEmpty(c.DataType) ? c.Name : $"{c.Name}: {c.DataType}";
                    canvas.DrawText(line, X + 8, top + i * 14, SKTextAlign.Left, font, paint);
                }
            }
        }

        /// <summary>
        /// Draws a hexagon-like transform shape behind the header/body using the base shape pipeline.
        /// </summary>
        protected override void DrawShape(SKCanvas canvas)
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);

            using var path = new SKPath();
            float indent = 20f;
            // Create hexagon
            path.MoveTo(rect.Left + indent, rect.Top);
            path.LineTo(rect.Right - indent, rect.Top);
            path.LineTo(rect.Right, rect.MidY);
            path.LineTo(rect.Right - indent, rect.Bottom);
            path.LineTo(rect.Left + indent, rect.Bottom);
            path.LineTo(rect.Left, rect.MidY);
            path.Close();

            using var fill = new SKPaint { Color = Background, Style = SKPaintStyle.Fill, IsAntialias = true };
            canvas.DrawPath(path, fill);

            using var border = new SKPaint { Color = Stroke, Style = SKPaintStyle.Stroke, StrokeWidth = 1.25f, IsAntialias = true };
            canvas.DrawPath(path, border);
        }

        /// <summary>
        /// Positions ports at left/right midpoints of the hexagon.
        /// </summary>
        protected override void LayoutPorts()
        {
            // If Join, use two inputs; for others one input is enough
            int desiredIn = Kind == TransformKind.Join ? 2 : 1;
            if (InPortCount != desiredIn) { EnsurePortCounts(desiredIn, 1); MarkPortsDirty(); }

            var rect = new SKRect(X, Y, X + Width, Y + Height);

            if (InConnectionPoints.Count > 0)
            {
                var inputPoint = InConnectionPoints[0];
                inputPoint.Center = new SKPoint(rect.Left - PortRadius - 2, rect.MidY - (InConnectionPoints.Count == 2 ? 12 : 0));
                inputPoint.Position = inputPoint.Center;
                float r = PortRadius;
                inputPoint.Bounds = new SKRect(inputPoint.Center.X - r, inputPoint.Center.Y - r, inputPoint.Center.X + r, inputPoint.Center.Y + r);
                inputPoint.Rect = inputPoint.Bounds;
                inputPoint.Index = 0;
                inputPoint.Component = this;
                inputPoint.IsAvailable = true;
            }

            if (InConnectionPoints.Count > 1)
            {
                var inputPoint2 = InConnectionPoints[1];
                inputPoint2.Center = new SKPoint(rect.Left - PortRadius - 2, rect.MidY + 12);
                inputPoint2.Position = inputPoint2.Center;
                float r2 = PortRadius;
                inputPoint2.Bounds = new SKRect(inputPoint2.Center.X - r2, inputPoint2.Center.Y - r2, inputPoint2.Center.X + r2, inputPoint2.Center.Y + r2);
                inputPoint2.Rect = inputPoint2.Bounds;
                inputPoint2.Index = 1;
                inputPoint2.Component = this;
                inputPoint2.IsAvailable = true;
            }

            if (OutConnectionPoints.Count > 0)
            {
                var outputPoint = OutConnectionPoints[0];
                outputPoint.Center = new SKPoint(rect.Right + PortRadius + 2, rect.MidY);
                outputPoint.Position = outputPoint.Center;
                float r = PortRadius;
                outputPoint.Bounds = new SKRect(outputPoint.Center.X - r, outputPoint.Center.Y - r, outputPoint.Center.X + r, outputPoint.Center.Y + r);
                outputPoint.Rect = outputPoint.Bounds;
                outputPoint.Index = 0;
                outputPoint.Component = this;
                outputPoint.IsAvailable = true;
            }
        }

        /// <summary>
        /// Helper used by external orchestrators or editor buttons to infer OutputSchema
        /// from upstream schemas for Aggregate and Join kinds. This is a lightweight heuristic.
        /// </summary>
        public void InferOutputSchemaFromUpstreams(Func<int, string> getInputSchemaJson)
        {
            try
            {
                if (Kind == TransformKind.Aggregate)
                {
                    var inputJson = getInputSchemaJson?.Invoke(0);
                    var input = System.Text.Json.JsonSerializer.Deserialize<List<ColumnDefinition>>(inputJson ?? "[]") ?? new();
                    var groupSet = new HashSet<string>((GroupBy ?? string.Empty).Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()), StringComparer.OrdinalIgnoreCase);
                    var output = new List<ColumnDefinition>();
                    // Keep group-by columns as-is
                    foreach (var c in input)
                    {
                        if (groupSet.Contains(c.Name)) output.Add(new ColumnDefinition { Name = c.Name, DataType = c.DataType, IsNullable = false });
                    }
                    // Add example aggregations for non-group columns
                    foreach (var c in input)
                    {
                        if (groupSet.Contains(c.Name)) continue;
                        output.Add(new ColumnDefinition { Name = $"SUM_{c.Name}", DataType = c.DataType, IsNullable = true });
                        output.Add(new ColumnDefinition { Name = $"COUNT_{c.Name}", DataType = "int", IsNullable = false });
                    }
                    OutputSchema = System.Text.Json.JsonSerializer.Serialize(output);
                    MarkPortsDirty();
                }
                else if (Kind == TransformKind.Join)
                {
                    var leftJson = getInputSchemaJson?.Invoke(0);
                    var rightJson = getInputSchemaJson?.Invoke(1);
                    var left = System.Text.Json.JsonSerializer.Deserialize<List<ColumnDefinition>>(leftJson ?? "[]") ?? new();
                    var right = System.Text.Json.JsonSerializer.Deserialize<List<ColumnDefinition>>(rightJson ?? "[]") ?? new();
                    var output = new List<ColumnDefinition>();
                    // Carry join keys once, then prefix duplicates
                    string lk = JoinKeyLeft ?? string.Empty;
                    string rk = JoinKeyRight ?? string.Empty;
                    var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var c in left)
                    {
                        var name = c.Name;
                        if (!used.Add(name)) name = $"L_{name}";
                        output.Add(new ColumnDefinition { Name = name, DataType = c.DataType, IsNullable = c.IsNullable });
                    }
                    foreach (var c in right)
                    {
                        var name = c.Name;
                        // If name already exists (incl. the join key), prefix with R_
                        if (!used.Add(name)) name = $"R_{name}";
                        output.Add(new ColumnDefinition { Name = name, DataType = c.DataType, IsNullable = c.IsNullable });
                    }
                    OutputSchema = System.Text.Json.JsonSerializer.Serialize(output);
                    MarkPortsDirty();
                }
            }
            catch { }
        }
    }
}

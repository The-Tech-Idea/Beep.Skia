using SkiaSharp;
using Beep.Skia.ETL;
using Beep.Skia.Components;
using Beep.Skia.Model; // DrawingContext lives in Beep.Skia.Model

namespace Beep.Skia.ETL
{
    /// <summary>
    /// ETL Source component that represents data sources in ETL workflows.
    /// Displays as a cylinder shape indicating data storage/retrieval.
    /// </summary>
    public class ETLSource : ETLControl
    {
        // Optional schema for outputs
        private System.Collections.Generic.List<Beep.Skia.Model.ColumnDefinition> _outputColumns = new();

        public enum SourceKind { Database, File, Api, Queue, Stream }

        private SourceKind _kind = SourceKind.Database;
        public SourceKind Kind
        {
            get => _kind;
            set
            {
                if (_kind == value) return; _kind = value;
                if (NodeProperties.TryGetValue("Kind", out var p)) p.ParameterCurrentValue = _kind; else NodeProperties["Kind"] = new Beep.Skia.Model.ParameterInfo { ParameterName = "Kind", ParameterType = typeof(SourceKind), DefaultParameterValue = _kind, ParameterCurrentValue = _kind, Description = "Source kind", Choices = System.Enum.GetNames(typeof(SourceKind)) };
                InvalidateVisual();
            }
        }

        private string _connectionString = string.Empty;
        public string ConnectionString
        {
            get => _connectionString;
            set { var v = value ?? string.Empty; if (_connectionString == v) return; _connectionString = v; if (NodeProperties.TryGetValue("ConnectionString", out var p)) p.ParameterCurrentValue = _connectionString; else NodeProperties["ConnectionString"] = new Beep.Skia.Model.ParameterInfo { ParameterName = "ConnectionString", ParameterType = typeof(string), DefaultParameterValue = _connectionString, ParameterCurrentValue = _connectionString, Description = "Source connection (masked)" }; InvalidateVisual(); }
        }

        private string _path = string.Empty;
        public string Path
        {
            get => _path;
            set { var v = value ?? string.Empty; if (_path == v) return; _path = v; if (NodeProperties.TryGetValue("Path", out var p)) p.ParameterCurrentValue = _path; else NodeProperties["Path"] = new Beep.Skia.Model.ParameterInfo { ParameterName = "Path", ParameterType = typeof(string), DefaultParameterValue = _path, ParameterCurrentValue = _path, Description = "Table/File/Endpoint path" }; InvalidateVisual(); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ETLSource"/> class.
        /// </summary>
        public ETLSource()
        {
            Title = "Source";
            HeaderColor = MaterialControl.MaterialColors.PrimaryContainer; // Material token
            Width = 140;
            Height = 72;
            DisplayText = "Source";
            TextPosition = TextPosition.Below;
            ShowDisplayText = true;
            EnsurePortCounts(inCount: 0, outCount: 1);

            // Seed NodeProperty for OutputSchema (JSON) to interop with ERD entities/lines
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_outputColumns);
                NodeProperties["OutputSchema"] = new Beep.Skia.Model.ParameterInfo
                {
                    ParameterName = "OutputSchema",
                    ParameterType = typeof(string),
                    DefaultParameterValue = json,
                    ParameterCurrentValue = json,
                    Description = "Output schema (JSON array of ColumnDefinition)"
                };
                NodeProperties["Kind"] = new Beep.Skia.Model.ParameterInfo { ParameterName = "Kind", ParameterType = typeof(SourceKind), DefaultParameterValue = _kind, ParameterCurrentValue = _kind, Description = "Source kind", Choices = System.Enum.GetNames(typeof(SourceKind)) };
                NodeProperties["ConnectionString"] = new Beep.Skia.Model.ParameterInfo { ParameterName = "ConnectionString", ParameterType = typeof(string), DefaultParameterValue = _connectionString, ParameterCurrentValue = _connectionString, Description = "Source connection (masked)" };
                NodeProperties["Path"] = new Beep.Skia.Model.ParameterInfo { ParameterName = "Path", ParameterType = typeof(string), DefaultParameterValue = _path, ParameterCurrentValue = _path, Description = "Table/File/Endpoint path" };
            }
            catch { }
        }

        /// <summary>
        /// Draws the cylinder shape representing a data source.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        protected override void DrawShape(SKCanvas canvas)
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);

            // Draw cylinder body
            using var fill = new SKPaint
            {
                Color = Background,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawRect(new SKRect(rect.Left, rect.Top + 8, rect.Right, rect.Bottom - 8), fill);

            // Draw top ellipse
            canvas.DrawOval(new SKRect(rect.Left, rect.Top, rect.Right, rect.Top + 16), fill);

            // Draw bottom ellipse
            canvas.DrawOval(new SKRect(rect.Left, rect.Bottom - 16, rect.Right, rect.Bottom), fill);

            // Draw cylinder outline
            using var border = new SKPaint
            {
                Color = Stroke,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.25f,
                IsAntialias = true
            };
            canvas.DrawLine(rect.Left, rect.Top + 8, rect.Left, rect.Bottom - 8, border);
            canvas.DrawLine(rect.Right, rect.Top + 8, rect.Right, rect.Bottom - 8, border);
            canvas.DrawOval(new SKRect(rect.Left, rect.Top, rect.Right, rect.Top + 16), border);
            canvas.DrawOval(new SKRect(rect.Left, rect.Bottom - 16, rect.Right, rect.Bottom), border);
        }

        protected override void DrawETLContent(SKCanvas canvas, DrawingContext context)
        {
            base.DrawETLContent(canvas, context);

            // Optional: render first few output columns inside the body as a hint
            try
            {
                if (NodeProperties != null && NodeProperties.TryGetValue("OutputSchema", out var p) && p?.ParameterCurrentValue is string js && !string.IsNullOrWhiteSpace(js))
                {
                    _outputColumns = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<Beep.Skia.Model.ColumnDefinition>>(js) ?? new();
                }
            }
            catch { }

            if (_outputColumns != null && _outputColumns.Count > 0)
            {
                using var font = new SKFont { Size = 11 };
                using var paint = new SKPaint { Color = new SKColor(70, 70, 70), IsAntialias = true };
                float top = Y + HeaderHeight + 22f;
                int max = System.Math.Min(4, _outputColumns.Count);
                for (int i = 0; i < max; i++)
                {
                    var c = _outputColumns[i];
                    var line = string.IsNullOrEmpty(c.DataType) ? c.Name : $"{c.Name}: {c.DataType}";
                    canvas.DrawText(line, X + 8, top + i * 14, SKTextAlign.Left, font, paint);
                }
            }
        }

        /// <summary>
        /// Gets the effective text positioning bounds for the cylinder shape.
        /// For cylinder shapes, we adjust positioning to account for the curved top and bottom.
        /// </summary>
        /// <returns>A rectangle defining the effective bounds for text positioning.</returns>
        protected override SKRect GetTextPositioningBounds()
        {
            // For cylinder, use bounds that account for the elliptical caps
            // The effective positioning area is slightly inset from the full bounds
            return new SKRect(X + 2, Y + 8, X + Width - 2, Y + Height - 8);
        }

        /// <summary>
        /// Positions connection points for the cylinder shape.
        /// </summary>
        protected override void LayoutPorts()
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);

            // Position output at the right side of the cylinder body
            if (OutConnectionPoints.Count > 0)
            {
                var outputPoint = OutConnectionPoints[0];
                float bodyCenterY = rect.Top + 8 + (rect.Bottom - 8 - (rect.Top + 8)) / 2;
                outputPoint.Center = new SKPoint(rect.Right + PortRadius + 2, bodyCenterY);
                outputPoint.Position = outputPoint.Center;
                float r = PortRadius;
                outputPoint.Bounds = new SKRect(outputPoint.Center.X - r, outputPoint.Center.Y - r,
                                              outputPoint.Center.X + r, outputPoint.Center.Y + r);
                outputPoint.Rect = outputPoint.Bounds;
                outputPoint.Index = 0;
                outputPoint.Component = this;
                outputPoint.IsAvailable = true;
            }
        }
    }
}
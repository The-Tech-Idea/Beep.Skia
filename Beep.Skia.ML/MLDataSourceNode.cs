using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ML
{
    public enum DataConnector { File, Database, API, Stream }

    public class MLDataSourceNode : MLControl
    {
        private string _name = "Data Source";
        private DataConnector _connector = DataConnector.File;
        private string _format = "CSV";

        public string SourceName { get => _name; set { var v = value ?? string.Empty; if (_name != v) { _name = v; if (NodeProperties.TryGetValue("SourceName", out var p)) p.ParameterCurrentValue = _name; else NodeProperties["SourceName"] = new ParameterInfo { ParameterName = "SourceName", ParameterType = typeof(string), DefaultParameterValue = _name, ParameterCurrentValue = _name, Description = "Source name" }; Name = _name; InvalidateVisual(); } } }
        public DataConnector Connector { get => _connector; set { if (_connector != value) { _connector = value; if (NodeProperties.TryGetValue("Connector", out var p)) p.ParameterCurrentValue = _connector; else NodeProperties["Connector"] = new ParameterInfo { ParameterName = "Connector", ParameterType = typeof(DataConnector), DefaultParameterValue = _connector, ParameterCurrentValue = _connector, Description = "Data connector", Choices = Enum.GetNames(typeof(DataConnector)) }; InvalidateVisual(); } } }
        public string Format { get => _format; set { var v = value ?? string.Empty; if (_format != v) { _format = v; if (NodeProperties.TryGetValue("Format", out var p)) p.ParameterCurrentValue = _format; else NodeProperties["Format"] = new ParameterInfo { ParameterName = "Format", ParameterType = typeof(string), DefaultParameterValue = _format, ParameterCurrentValue = _format, Description = "Data format" }; InvalidateVisual(); } } }

        public MLDataSourceNode()
        {
            Width = 120; Height = 80;
            NodeProperties["SourceName"] = new ParameterInfo { ParameterName = "SourceName", ParameterType = typeof(string), DefaultParameterValue = _name, ParameterCurrentValue = _name, Description = "Source name" };
            NodeProperties["Connector"] = new ParameterInfo { ParameterName = "Connector", ParameterType = typeof(DataConnector), DefaultParameterValue = _connector, ParameterCurrentValue = _connector, Description = "Data connector", Choices = Enum.GetNames(typeof(DataConnector)) };
            NodeProperties["Format"] = new ParameterInfo { ParameterName = "Format", ParameterType = typeof(string), DefaultParameterValue = _format, ParameterCurrentValue = _format, Description = "Data format" };
            EnsurePortCounts(0, 1);
        }

        protected override void DrawMLContent(SKCanvas canvas, DrawingContext context)
        {
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var r = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(r, 6, 6, fill);
            canvas.DrawRoundRect(r, 6, 6, border);

            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            using var nameFont = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            using var meta = new SKFont(SKTypeface.Default, 8);
            canvas.DrawText(SourceName, r.MidX, r.MidY, SKTextAlign.Center, nameFont, text);
            canvas.DrawText($"{Connector} · {Format}", r.MidX, r.Bottom - 6, SKTextAlign.Center, meta, text);

            // Ports
            using var outPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
            foreach (var p in OutConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, outPaint);
        }
    }
}

using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Cloud
{
    public enum DbEngine { SqlServer, MySql, PostgreSql, CosmosDb, DynamoDb, MongoDb }

    public class CloudDatabaseNode : CloudControl
    {
        private string _name = "Database";
        private DbEngine _engine = DbEngine.PostgreSql;
        private string _version = "latest";
        private string _region = "us-east";

        public string DatabaseName { get => _name; set { var v = value ?? string.Empty; if (_name != v) { _name = v; if (NodeProperties.TryGetValue("DatabaseName", out var p)) p.ParameterCurrentValue = _name; else NodeProperties["DatabaseName"] = new ParameterInfo { ParameterName = "DatabaseName", ParameterType = typeof(string), DefaultParameterValue = _name, ParameterCurrentValue = _name, Description = "Database name" }; Name = _name; InvalidateVisual(); } } }
        public DbEngine Engine { get => _engine; set { if (_engine != value) { _engine = value; if (NodeProperties.TryGetValue("Engine", out var p)) p.ParameterCurrentValue = _engine; else NodeProperties["Engine"] = new ParameterInfo { ParameterName = "Engine", ParameterType = typeof(DbEngine), DefaultParameterValue = _engine, ParameterCurrentValue = _engine, Description = "Engine", Choices = Enum.GetNames(typeof(DbEngine)) }; InvalidateVisual(); } } }
        public string Version { get => _version; set { var v = value ?? string.Empty; if (_version != v) { _version = v; if (NodeProperties.TryGetValue("Version", out var p)) p.ParameterCurrentValue = _version; else NodeProperties["Version"] = new ParameterInfo { ParameterName = "Version", ParameterType = typeof(string), DefaultParameterValue = _version, ParameterCurrentValue = _version, Description = "Version" }; InvalidateVisual(); } } }
        public string Region { get => _region; set { var v = value ?? string.Empty; if (_region != v) { _region = v; if (NodeProperties.TryGetValue("Region", out var p)) p.ParameterCurrentValue = _region; else NodeProperties["Region"] = new ParameterInfo { ParameterName = "Region", ParameterType = typeof(string), DefaultParameterValue = _region, ParameterCurrentValue = _region, Description = "Region" }; InvalidateVisual(); } } }

        public CloudDatabaseNode()
        {
            Width = 170; Height = 90;
            NodeProperties["DatabaseName"] = new ParameterInfo { ParameterName = "DatabaseName", ParameterType = typeof(string), DefaultParameterValue = _name, ParameterCurrentValue = _name, Description = "Database name" };
            NodeProperties["Engine"] = new ParameterInfo { ParameterName = "Engine", ParameterType = typeof(DbEngine), DefaultParameterValue = _engine, ParameterCurrentValue = _engine, Description = "Engine", Choices = Enum.GetNames(typeof(DbEngine)) };
            NodeProperties["Version"] = new ParameterInfo { ParameterName = "Version", ParameterType = typeof(string), DefaultParameterValue = _version, ParameterCurrentValue = _version, Description = "Version" };
            NodeProperties["Region"] = new ParameterInfo { ParameterName = "Region", ParameterType = typeof(string), DefaultParameterValue = _region, ParameterCurrentValue = _region, Description = "Region" };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawCloudContent(SKCanvas canvas, DrawingContext context)
        {
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var fill = new SKPaint { Color = BackgroundColor, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, IsAntialias = true, Style = SKPaintStyle.Stroke };
            canvas.DrawRoundRect(r, 8, 8, fill);
            canvas.DrawRoundRect(r, 8, 8, border);

            using var namePaint = new SKPaint { Color = TextColor, IsAntialias = true };
            using var nameFont = new SKFont(SKTypeface.Default, 12) { Embolden = true };
            using var metaPaint = new SKPaint { Color = TextColor, IsAntialias = true };
            using var metaFont = new SKFont(SKTypeface.Default, 8);
            canvas.DrawText(DatabaseName, r.MidX, r.MidY - 8, SKTextAlign.Center, nameFont, namePaint);
            canvas.DrawText($"{Engine} {Version}", r.MidX, r.Bottom - 18, SKTextAlign.Center, metaFont, metaPaint);
            canvas.DrawText(Region, r.MidX, r.Bottom - 6, SKTextAlign.Center, metaFont, metaPaint);

            using var inPaint = new SKPaint { Color = MaterialColors.SecondaryContainer, IsAntialias = true };
            using var outPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
            foreach (var p in InConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, inPaint);
            foreach (var p in OutConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, outPaint);
        }
    }
}

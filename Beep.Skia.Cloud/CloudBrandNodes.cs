using SkiaSharp;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.Cloud
{
    /// <summary>
    /// Base class for provider-branded cloud service nodes (AWS / Azure / GCP).
    /// Renders a brand-colored accent bar, a glyph badge, the service name, and the provider.
    /// </summary>
    public abstract class CloudBrandNode : CloudControl
    {
        /// <summary>Cloud provider name (AWS, Azure, GCP).</summary>
        public abstract string Provider { get; }

        /// <summary>Service name (EC2, Blob Storage, ...).</summary>
        public abstract string ServiceName { get; }

        /// <summary>Provider brand color used for the accent bar and badge.</summary>
        public abstract SKColor BrandColor { get; }

        /// <summary>Short glyph shown inside the badge (defaults to the service name).</summary>
        public virtual string IconGlyph
            => ServiceName.Length <= 4 ? ServiceName : ServiceName.Substring(0, 4);

        protected CloudBrandNode()
        {
            Width = 170;
            Height = 86;
            BackgroundColor = MaterialColors.Surface;
            BorderColor = MaterialColors.Outline;
            TextColor = MaterialColors.OnSurface;
            EnsurePortCounts(1, 1);
        }

        /// <summary>Upserts a node property value for editor/persistence metadata.</summary>
        protected void SetProp(string name, object value, string? description = null)
        {
            if (NodeProperties.TryGetValue(name, out var p) && p != null)
                p.ParameterCurrentValue = value;
            else
                NodeProperties[name] = new ParameterInfo
                {
                    ParameterName = name,
                    ParameterType = value.GetType(),
                    DefaultParameterValue = value,
                    ParameterCurrentValue = value,
                    Description = description ?? name
                };
        }

        protected override void DrawCloudContent(SKCanvas canvas, DrawingContext context)
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = BorderThickness, IsAntialias = true };
            canvas.DrawRoundRect(rect, 8, 8, fill);
            canvas.DrawRoundRect(rect, 8, 8, stroke);

            // Brand accent bar across the top.
            const float barHeight = 7f;
            using var bar = new SKPaint { Color = BrandColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            canvas.DrawRoundRect(new SKRect(rect.Left, rect.Top, rect.Right, rect.Top + barHeight), 4, 4, bar);
            canvas.DrawRect(new SKRect(rect.Left, rect.Top + barHeight - 4f, rect.Right, rect.Top + barHeight), bar);

            // Glyph badge.
            const float badgeSize = 28f;
            var badge = new SKRect(rect.Left + 10f, rect.Top + 16f, rect.Left + 10f + badgeSize, rect.Top + 16f + badgeSize);
            using var badgeFill = new SKPaint { Color = BrandColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            canvas.DrawRoundRect(badge, 7, 7, badgeFill);

            using var glyphFont = new SKFont(SKTypeface.Default, 9) { Embolden = true };
            using var white = new SKPaint { Color = SKColors.White, IsAntialias = true };
            canvas.DrawText(IconGlyph, badge.MidX, badge.MidY + 3f, SKTextAlign.Center, glyphFont, white);

            // Service + provider labels.
            using var nameFont = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            using var providerFont = new SKFont(SKTypeface.Default, 8);
            using var textPaint = new SKPaint { Color = TextColor, IsAntialias = true };

            canvas.DrawText(ServiceName, badge.Right + 8f, rect.Top + 30f, SKTextAlign.Left, nameFont, textPaint);
            canvas.DrawText(Provider, badge.Right + 8f, rect.Top + 43f, SKTextAlign.Left, providerFont, textPaint);

            if (!string.IsNullOrWhiteSpace(Name))
                canvas.DrawText(Name, rect.MidX, rect.Bottom - 9f, SKTextAlign.Center, providerFont, textPaint);
        }
    }

    // ── AWS ──────────────────────────────────────────────────────────────────

    public class AwsEc2Node : CloudBrandNode
    {
        public override string Provider => "AWS";
        public override string ServiceName => "EC2";
        public override SKColor BrandColor => new SKColor(0xFF, 0x99, 0x00);
        public AwsEc2Node() { Name = "AWS EC2"; SetProp("Provider", "AWS"); SetProp("ServiceName", "EC2"); }
    }

    public class AwsS3Node : CloudBrandNode
    {
        public override string Provider => "AWS";
        public override string ServiceName => "S3";
        public override SKColor BrandColor => new SKColor(0x56, 0x9A, 0x31);
        public AwsS3Node() { Name = "AWS S3"; SetProp("Provider", "AWS"); SetProp("ServiceName", "S3"); }
    }

    public class AwsLambdaNode : CloudBrandNode
    {
        public override string Provider => "AWS";
        public override string ServiceName => "Lambda";
        public override SKColor BrandColor => new SKColor(0xFF, 0x99, 0x00);
        public override string IconGlyph => "λ";
        public AwsLambdaNode() { Name = "AWS Lambda"; SetProp("Provider", "AWS"); SetProp("ServiceName", "Lambda"); }
    }

    public class AwsRdsNode : CloudBrandNode
    {
        public override string Provider => "AWS";
        public override string ServiceName => "RDS";
        public override SKColor BrandColor => new SKColor(0x52, 0x7F, 0xFF);
        public AwsRdsNode() { Name = "AWS RDS"; SetProp("Provider", "AWS"); SetProp("ServiceName", "RDS"); }
    }

    // ── Azure ────────────────────────────────────────────────────────────────

    public class AzureVmNode : CloudBrandNode
    {
        public override string Provider => "Azure";
        public override string ServiceName => "Virtual Machines";
        public override SKColor BrandColor => new SKColor(0x00, 0x78, 0xD4);
        public override string IconGlyph => "VM";
        public AzureVmNode() { Name = "Azure VM"; SetProp("Provider", "Azure"); SetProp("ServiceName", "Virtual Machines"); }
    }

    public class AzureBlobStorageNode : CloudBrandNode
    {
        public override string Provider => "Azure";
        public override string ServiceName => "Blob Storage";
        public override SKColor BrandColor => new SKColor(0x00, 0x78, 0xD4);
        public override string IconGlyph => "Blob";
        public AzureBlobStorageNode() { Name = "Azure Blob"; SetProp("Provider", "Azure"); SetProp("ServiceName", "Blob Storage"); }
    }

    public class AzureFunctionsNode : CloudBrandNode
    {
        public override string Provider => "Azure";
        public override string ServiceName => "Functions";
        public override SKColor BrandColor => new SKColor(0x00, 0x62, 0xAD);
        public override string IconGlyph => "ƒ";
        public AzureFunctionsNode() { Name = "Azure Functions"; SetProp("Provider", "Azure"); SetProp("ServiceName", "Functions"); }
    }

    public class AzureSqlNode : CloudBrandNode
    {
        public override string Provider => "Azure";
        public override string ServiceName => "SQL Database";
        public override SKColor BrandColor => new SKColor(0x00, 0x78, 0xD4);
        public override string IconGlyph => "SQL";
        public AzureSqlNode() { Name = "Azure SQL"; SetProp("Provider", "Azure"); SetProp("ServiceName", "SQL Database"); }
    }

    // ── GCP ──────────────────────────────────────────────────────────────────

    public class GcpComputeNode : CloudBrandNode
    {
        public override string Provider => "GCP";
        public override string ServiceName => "Compute Engine";
        public override SKColor BrandColor => new SKColor(0x42, 0x85, 0xF4);
        public override string IconGlyph => "GCE";
        public GcpComputeNode() { Name = "GCP Compute"; SetProp("Provider", "GCP"); SetProp("ServiceName", "Compute Engine"); }
    }

    public class GcpStorageNode : CloudBrandNode
    {
        public override string Provider => "GCP";
        public override string ServiceName => "Cloud Storage";
        public override SKColor BrandColor => new SKColor(0x34, 0xA8, 0x53);
        public override string IconGlyph => "GCS";
        public GcpStorageNode() { Name = "GCP Storage"; SetProp("Provider", "GCP"); SetProp("ServiceName", "Cloud Storage"); }
    }

    public class GcpFunctionsNode : CloudBrandNode
    {
        public override string Provider => "GCP";
        public override string ServiceName => "Cloud Functions";
        public override SKColor BrandColor => new SKColor(0xFB, 0xBC, 0x05);
        public override string IconGlyph => "ƒ";
        public GcpFunctionsNode() { Name = "GCP Functions"; SetProp("Provider", "GCP"); SetProp("ServiceName", "Cloud Functions"); }
    }

    public class GcpSqlNode : CloudBrandNode
    {
        public override string Provider => "GCP";
        public override string ServiceName => "Cloud SQL";
        public override SKColor BrandColor => new SKColor(0xEA, 0x43, 0x35);
        public override string IconGlyph => "SQL";
        public GcpSqlNode() { Name = "GCP SQL"; SetProp("Provider", "GCP"); SetProp("ServiceName", "Cloud SQL"); }
    }
}

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
        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        public override string Provider => "AWS";
        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public override string ServiceName => "EC2";
        /// <summary>
        /// Gets or sets the brand color.
        /// </summary>
        public override SKColor BrandColor => new SKColor(0xFF, 0x99, 0x00);
        /// <summary>
        /// Initializes a new instance of the AwsEc2Node class.
        /// </summary>
        public AwsEc2Node() { Name = "AWS EC2"; SetProp("Provider", "AWS"); SetProp("ServiceName", "EC2"); }
    }

    public class AwsS3Node : CloudBrandNode
    {
        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        public override string Provider => "AWS";
        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public override string ServiceName => "S3";
        /// <summary>
        /// Gets or sets the brand color.
        /// </summary>
        public override SKColor BrandColor => new SKColor(0x56, 0x9A, 0x31);
        /// <summary>
        /// Initializes a new instance of the AwsS3Node class.
        /// </summary>
        public AwsS3Node() { Name = "AWS S3"; SetProp("Provider", "AWS"); SetProp("ServiceName", "S3"); }
    }

    public class AwsLambdaNode : CloudBrandNode
    {
        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        public override string Provider => "AWS";
        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public override string ServiceName => "Lambda";
        /// <summary>
        /// Gets or sets the brand color.
        /// </summary>
        public override SKColor BrandColor => new SKColor(0xFF, 0x99, 0x00);
        /// <summary>
        /// Gets or sets the icon glyph.
        /// </summary>
        public override string IconGlyph => "λ";
        /// <summary>
        /// Initializes a new instance of the AwsLambdaNode class.
        /// </summary>
        public AwsLambdaNode() { Name = "AWS Lambda"; SetProp("Provider", "AWS"); SetProp("ServiceName", "Lambda"); }
    }

    public class AwsRdsNode : CloudBrandNode
    {
        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        public override string Provider => "AWS";
        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public override string ServiceName => "RDS";
        /// <summary>
        /// Gets or sets the brand color.
        /// </summary>
        public override SKColor BrandColor => new SKColor(0x52, 0x7F, 0xFF);
        /// <summary>
        /// Initializes a new instance of the AwsRdsNode class.
        /// </summary>
        public AwsRdsNode() { Name = "AWS RDS"; SetProp("Provider", "AWS"); SetProp("ServiceName", "RDS"); }
    }

    // ── Azure ────────────────────────────────────────────────────────────────

    public class AzureVmNode : CloudBrandNode
    {
        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        public override string Provider => "Azure";
        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public override string ServiceName => "Virtual Machines";
        /// <summary>
        /// Gets or sets the brand color.
        /// </summary>
        public override SKColor BrandColor => new SKColor(0x00, 0x78, 0xD4);
        /// <summary>
        /// Gets or sets the icon glyph.
        /// </summary>
        public override string IconGlyph => "VM";
        /// <summary>
        /// Initializes a new instance of the AzureVmNode class.
        /// </summary>
        public AzureVmNode() { Name = "Azure VM"; SetProp("Provider", "Azure"); SetProp("ServiceName", "Virtual Machines"); }
    }

    public class AzureBlobStorageNode : CloudBrandNode
    {
        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        public override string Provider => "Azure";
        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public override string ServiceName => "Blob Storage";
        /// <summary>
        /// Gets or sets the brand color.
        /// </summary>
        public override SKColor BrandColor => new SKColor(0x00, 0x78, 0xD4);
        /// <summary>
        /// Gets or sets the icon glyph.
        /// </summary>
        public override string IconGlyph => "Blob";
        /// <summary>
        /// Initializes a new instance of the AzureBlobStorageNode class.
        /// </summary>
        public AzureBlobStorageNode() { Name = "Azure Blob"; SetProp("Provider", "Azure"); SetProp("ServiceName", "Blob Storage"); }
    }

    public class AzureFunctionsNode : CloudBrandNode
    {
        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        public override string Provider => "Azure";
        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public override string ServiceName => "Functions";
        /// <summary>
        /// Gets or sets the brand color.
        /// </summary>
        public override SKColor BrandColor => new SKColor(0x00, 0x62, 0xAD);
        /// <summary>
        /// Gets or sets the icon glyph.
        /// </summary>
        public override string IconGlyph => "ƒ";
        /// <summary>
        /// Initializes a new instance of the AzureFunctionsNode class.
        /// </summary>
        public AzureFunctionsNode() { Name = "Azure Functions"; SetProp("Provider", "Azure"); SetProp("ServiceName", "Functions"); }
    }

    public class AzureSqlNode : CloudBrandNode
    {
        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        public override string Provider => "Azure";
        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public override string ServiceName => "SQL Database";
        /// <summary>
        /// Gets or sets the brand color.
        /// </summary>
        public override SKColor BrandColor => new SKColor(0x00, 0x78, 0xD4);
        /// <summary>
        /// Gets or sets the icon glyph.
        /// </summary>
        public override string IconGlyph => "SQL";
        /// <summary>
        /// Initializes a new instance of the AzureSqlNode class.
        /// </summary>
        public AzureSqlNode() { Name = "Azure SQL"; SetProp("Provider", "Azure"); SetProp("ServiceName", "SQL Database"); }
    }

    // ── GCP ──────────────────────────────────────────────────────────────────

    public class GcpComputeNode : CloudBrandNode
    {
        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        public override string Provider => "GCP";
        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public override string ServiceName => "Compute Engine";
        /// <summary>
        /// Gets or sets the brand color.
        /// </summary>
        public override SKColor BrandColor => new SKColor(0x42, 0x85, 0xF4);
        /// <summary>
        /// Gets or sets the icon glyph.
        /// </summary>
        public override string IconGlyph => "GCE";
        /// <summary>
        /// Initializes a new instance of the GcpComputeNode class.
        /// </summary>
        public GcpComputeNode() { Name = "GCP Compute"; SetProp("Provider", "GCP"); SetProp("ServiceName", "Compute Engine"); }
    }

    public class GcpStorageNode : CloudBrandNode
    {
        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        public override string Provider => "GCP";
        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public override string ServiceName => "Cloud Storage";
        /// <summary>
        /// Gets or sets the brand color.
        /// </summary>
        public override SKColor BrandColor => new SKColor(0x34, 0xA8, 0x53);
        /// <summary>
        /// Gets or sets the icon glyph.
        /// </summary>
        public override string IconGlyph => "GCS";
        /// <summary>
        /// Initializes a new instance of the GcpStorageNode class.
        /// </summary>
        public GcpStorageNode() { Name = "GCP Storage"; SetProp("Provider", "GCP"); SetProp("ServiceName", "Cloud Storage"); }
    }

    public class GcpFunctionsNode : CloudBrandNode
    {
        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        public override string Provider => "GCP";
        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public override string ServiceName => "Cloud Functions";
        /// <summary>
        /// Gets or sets the brand color.
        /// </summary>
        public override SKColor BrandColor => new SKColor(0xFB, 0xBC, 0x05);
        /// <summary>
        /// Gets or sets the icon glyph.
        /// </summary>
        public override string IconGlyph => "ƒ";
        /// <summary>
        /// Initializes a new instance of the GcpFunctionsNode class.
        /// </summary>
        public GcpFunctionsNode() { Name = "GCP Functions"; SetProp("Provider", "GCP"); SetProp("ServiceName", "Cloud Functions"); }
    }

    public class GcpSqlNode : CloudBrandNode
    {
        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        public override string Provider => "GCP";
        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public override string ServiceName => "Cloud SQL";
        /// <summary>
        /// Gets or sets the brand color.
        /// </summary>
        public override SKColor BrandColor => new SKColor(0xEA, 0x43, 0x35);
        /// <summary>
        /// Gets or sets the icon glyph.
        /// </summary>
        public override string IconGlyph => "SQL";
        /// <summary>
        /// Initializes a new instance of the GcpSqlNode class.
        /// </summary>
        public GcpSqlNode() { Name = "GCP SQL"; SetProp("Provider", "GCP"); SetProp("ServiceName", "Cloud SQL"); }
    }
}

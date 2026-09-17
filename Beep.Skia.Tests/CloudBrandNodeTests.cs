using System.Linq;
using Beep.Skia;
using Beep.Skia.Cloud;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for provider-branded cloud nodes: identity, persistence, and rendering.
    /// </summary>
    public class CloudBrandNodeTests
    {
        [Theory]
        [InlineData(typeof(AwsEc2Node), "AWS", "EC2")]
        [InlineData(typeof(AwsS3Node), "AWS", "S3")]
        [InlineData(typeof(AwsLambdaNode), "AWS", "Lambda")]
        [InlineData(typeof(AwsRdsNode), "AWS", "RDS")]
        [InlineData(typeof(AzureVmNode), "Azure", "Virtual Machines")]
        [InlineData(typeof(AzureBlobStorageNode), "Azure", "Blob Storage")]
        [InlineData(typeof(AzureFunctionsNode), "Azure", "Functions")]
        [InlineData(typeof(AzureSqlNode), "Azure", "SQL Database")]
        [InlineData(typeof(GcpComputeNode), "GCP", "Compute Engine")]
        [InlineData(typeof(GcpStorageNode), "GCP", "Cloud Storage")]
        [InlineData(typeof(GcpFunctionsNode), "GCP", "Cloud Functions")]
        [InlineData(typeof(GcpSqlNode), "GCP", "Cloud SQL")]
        public void BrandNodes_ExposeProviderAndService(System.Type nodeType, string provider, string service)
        {
            var node = (CloudBrandNode)System.Activator.CreateInstance(nodeType)!;

            Assert.Equal(provider, node.Provider);
            Assert.Equal(service, node.ServiceName);
            Assert.NotEqual(default, node.BrandColor);
            Assert.False(string.IsNullOrWhiteSpace(node.IconGlyph));
        }

        [Fact]
        public void BrandNode_PersistsThroughSerialization()
        {
            var manager = new DrawingManager();
            manager.AddComponent(new AwsS3Node { X = 30, Y = 40, Name = "Bucket" });

            var manager2 = new DrawingManager();
            manager2.LoadFromDto(manager.ToDto());

            var loaded = manager2.GetComponents().OfType<AwsS3Node>().Single();
            Assert.Equal("AWS", loaded.Provider);
            Assert.Equal("S3", loaded.ServiceName);
            Assert.Equal("Bucket", loaded.Name);
        }

        [Fact]
        public void BrandNode_RendersBrandedContent()
        {
            var manager = new DrawingManager();
            manager.AddComponent(new AwsLambdaNode { X = 20, Y = 20, Width = 170, Height = 86, Name = "ResizeFn" });

            using var bitmap = manager.RenderToBitmap(240, 150);

            bool hasNonWhitePixel = false;
            for (int y = 0; y < bitmap.Height && !hasNonWhitePixel; y += 2)
            {
                for (int x = 0; x < bitmap.Width; x += 2)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    if (pixel != SKColors.White && pixel.Alpha > 0)
                    {
                        hasNonWhitePixel = true;
                        break;
                    }
                }
            }
            Assert.True(hasNonWhitePixel, "Branded cloud node rendered nothing.");
        }

        [Fact]
        public void BrandNode_UsesDistinctBrandColorsPerProvider()
        {
            var aws = new AwsEc2Node();
            var azure = new AzureVmNode();
            var gcp = new GcpComputeNode();

            Assert.NotEqual(aws.BrandColor, azure.BrandColor);
            Assert.NotEqual(azure.BrandColor, gcp.BrandColor);
            Assert.NotEqual(aws.BrandColor, gcp.BrandColor);
        }
    }
}

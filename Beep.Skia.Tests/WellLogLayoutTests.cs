using Beep.Skia.WellLogs;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    public class WellLogLayoutTests
    {
        [Fact]
        public void BuildTrackLayouts_DistributesWidthByRatio()
        {
            var document = new WellLogDocument();
            document.Tracks.Add(new WellLogTrack { Name = "Depth", WidthRatio = 1f });
            document.Tracks.Add(new WellLogTrack { Name = "Gamma", WidthRatio = 2f });

            var layouts = WellLogLayoutEngine.BuildTrackLayouts(new SKRect(0f, 0f, 300f, 600f), document, 0f, 28f);

            Assert.Equal(2, layouts.Count);
            var firstWidth = layouts[0].TrackBounds.Width;
            var secondWidth = layouts[1].TrackBounds.Width;
            Assert.InRange(secondWidth / firstWidth, 1.95f, 2.05f);
        }

        [Fact]
        public void MapDepthToY_MapsTopAndBottomDepths()
        {
            var axis = new WellLogDepthAxis
            {
                MinimumDepth = 1000f,
                MaximumDepth = 1100f
            };
            var plotBounds = new SKRect(10f, 20f, 110f, 220f);

            Assert.Equal(20f, WellLogLayoutEngine.MapDepthToY(axis, 1000f, plotBounds), 3);
            Assert.Equal(220f, WellLogLayoutEngine.MapDepthToY(axis, 1100f, plotBounds), 3);
        }

        [Fact]
        public void MapValueToX_UsesLogScale()
        {
            var curve = new WellLogCurve
            {
                MinimumValue = 0.2f,
                MaximumValue = 2000f,
                ScaleType = WellLogScaleType.Log10
            };
            var plotBounds = new SKRect(0f, 0f, 400f, 200f);

            var minX = WellLogLayoutEngine.MapValueToX(curve, 0.2f, plotBounds);
            var midX = WellLogLayoutEngine.MapValueToX(curve, 20f, plotBounds);
            var maxX = WellLogLayoutEngine.MapValueToX(curve, 2000f, plotBounds);

            Assert.True(minX < midX && midX < maxX);
            Assert.InRange(midX, 198f, 202f);
        }
    }
}
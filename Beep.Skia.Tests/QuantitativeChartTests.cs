using System.Linq;
using Beep.Skia;
using Beep.Ski.Quantitative;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for quantitative chart rendering: data model, chart types, persistence.
    /// </summary>
    public class QuantitativeChartTests
    {
        [Fact]
        public void AddSeries_StoresDataAndAssignsDistinctColors()
        {
            var chart = new ChartNode();
            chart.AddSeries("close", 1, 2, 3);
            chart.AddSeries("volume", 10, 20, 30);

            Assert.Equal(2, chart.Series.Count);
            Assert.Equal(3, chart.Series[0].Values.Count);
            Assert.NotEqual(chart.Series[0].ColorArgb, chart.Series[1].ColorArgb);
        }

        [Fact]
        public void SetData_ReplacesSeries()
        {
            var chart = new ChartNode();
            chart.AddSeries("a", 1, 2);
            chart.SetData(new ChartSeries { Name = "b", Values = { 5, 6 } });

            Assert.Single(chart.Series);
            Assert.Equal("b", chart.Series[0].Name);
        }

        [Fact]
        public void Series_PersistThroughSerialization()
        {
            var manager = new DrawingManager();
            var chart = new ChartNode { X = 20, Y = 20, Width = 260, Height = 170, ChartType = "Bar", Title = "Prices" };
            chart.AddSeries("close", 1.5, 2.5, 3.5);
            manager.AddComponent(chart);

            var manager2 = new DrawingManager();
            manager2.LoadFromDto(manager.ToDto());

            var loaded = manager2.GetComponents().OfType<ChartNode>().Single();
            Assert.Equal("Bar", loaded.ChartType);
            Assert.Equal("Prices", loaded.Title);
            Assert.Single(loaded.Series);
            Assert.Equal(new[] { 1.5, 2.5, 3.5 }, loaded.Series[0].Values);
        }

        [Theory]
        [InlineData("Line")]
        [InlineData("Area")]
        [InlineData("Bar")]
        [InlineData("Scatter")]
        [InlineData("Histogram")]
        [InlineData("Candlestick")]
        public void Chart_RendersDataForEveryType(string chartType)
        {
            var manager = new DrawingManager();
            var chart = new ChartNode
            {
                X = 10,
                Y = 10,
                Width = 280,
                Height = 180,
                ChartType = chartType,
                Title = chartType
            };
            chart.AddSeries("s1", 1, 4, 2, 6, 3);
            chart.AddSeries("s2", 3, 1, 5, 2, 4);
            manager.AddComponent(chart);

            using var bitmap = manager.RenderToBitmap(320, 220);

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
            Assert.True(hasNonWhitePixel, $"Chart type '{chartType}' rendered nothing.");
        }

        [Fact]
        public void EmptyChart_RendersPlaceholderWithoutThrowing()
        {
            var manager = new DrawingManager();
            manager.AddComponent(new ChartNode { X = 10, Y = 10, Width = 240, Height = 160 });

            using var bitmap = manager.RenderToBitmap(300, 200);
            Assert.Equal(300, bitmap.Width);
        }

        [Fact]
        public void MaxPoints_ClampsToAtLeastOne()
        {
            var chart = new ChartNode();
            chart.MaxPoints = 0;
            Assert.Equal(1, chart.MaxPoints);
        }
    }
}

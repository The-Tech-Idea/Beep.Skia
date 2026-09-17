using Beep.Skia;
using Beep.Skia.Model;
using SkiaSharp;
using Xunit;
using Xunit.Abstractions;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Verifies that connection lines are actually rendered (the render performance numbers are
    /// only meaningful if they do) and reports the ink the line itself contributes.
    /// </summary>
    public class ConnectionLineRenderingTests
    {
        private readonly ITestOutputHelper _output;

        public ConnectionLineRenderingTests(ITestOutputHelper output) => _output = output;

        private sealed class PlainNode : SkiaComponent
        {
            public PlainNode()
            {
                InConnectionPoints.Add(new ConnectionPoint { Component = this, Index = 0, Position = new SKPoint(0.5f, 0f) });
                OutConnectionPoints.Add(new ConnectionPoint { Component = this, Index = 0, Position = new SKPoint(0.5f, 1f) });
            }

            protected override void DrawContent(SKCanvas canvas, DrawingContext context)
            {
                using var paint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Fill };
                canvas.DrawRect(new SKRect(X, Y, X + Width, Y + Height), paint);
            }
        }

        [Fact]
        public void ConnectedComponents_DrawTheLineBetweenThem()
        {
            var manager = new DrawingManager();
            var top = new PlainNode { X = 100, Y = 20, Width = 40, Height = 20, Name = "top" };
            var bottom = new PlainNode { X = 100, Y = 220, Width = 40, Height = 20, Name = "bottom" };
            manager.AddComponent(top);
            manager.AddComponent(bottom);
            manager.ConnectComponents(top, bottom);

            var lines = manager.GetLines();
            Assert.Single(lines);

            var line = lines[0];
            _output.WriteLine($"line type: {line.GetType().Name}");
            if (line is ConnectionLine concrete)
                _output.WriteLine($"IsVisible: {concrete.IsVisible}");

            // Render with and without the line to isolate its contribution.
            using var withLine = Render(manager);

            manager.ClearComponents();
            using var withoutLine = Render(manager);

            var inkWith = CountInk(withLine);
            var inkWithout = CountInk(withoutLine);
            _output.WriteLine($"ink with components+line: {inkWith}, components only: {inkWithout}");

            // Sample the corridor between the two nodes: only the line can put ink there.
            var corridorInk = 0;
            for (var y = 60; y < 200; y++)
            {
                for (var x = 80; x < 160; x++)
                {
                    var pixel = withLine.GetPixel(x, y);
                    if (pixel != SKColors.White && pixel.Alpha > 0) corridorInk++;
                }
            }
            _output.WriteLine($"ink in the corridor between the nodes: {corridorInk}");

            Assert.True(corridorInk > 0, "the connection line must be rendered between the components");
        }

        private static SKBitmap Render(DrawingManager manager)
        {
            using var surface = SKSurface.Create(new SKImageInfo(300, 300));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            manager.Draw(canvas);
            canvas.Flush();
            using var image = surface.Snapshot();
            return SKBitmap.FromImage(image);
        }

        private static int CountInk(SKBitmap bitmap)
        {
            var ink = 0;
            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    if (pixel != SKColors.White && pixel.Alpha > 0) ink++;
                }
            }
            return ink;
        }
    }
}

using System.Linq;
using SkiaSharp;
using Xunit;
using Beep.Skia.Model;

namespace Beep.Skia.Tests
{
    public class RenderingSmokeTests
    {
        [Fact]
        public void DrawingManager_Draws_Component_To_Bitmap()
        {
            // Arrange
            var dm = new DrawingManager();

            // create a simple test component: a lightweight SkiaComponent that draws a filled rect
            var rect = new TestRectComponent
            {
                X = 10,
                Y = 10,
                Width = 100,
                Height = 30,
                Name = "testRect"
            };

            dm.AddComponent(rect);

            // Create bitmap and canvas
            int w = 200, h = 100;
            using var bmp = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            using var canvas = new SKCanvas(bmp);

            // Clear to transparent
            canvas.Clear(SKColors.Transparent);

            // Act
            dm.Draw(canvas);

            // Read pixels and assert that some are not transparent
            var span = bmp.Pixels;
            bool anyOpaque = span.Any(p => p != SKColors.Transparent);

            Assert.True(anyOpaque, "Expected some pixels to be drawn by DrawingManager.Draw");
        }

        private class TestRectComponent : SkiaComponent
        {
            protected override void DrawContent(SKCanvas canvas, DrawingContext context)
            {
                using var paint = new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Fill };
                canvas.DrawRect(Bounds, paint);
            }
        }
    }
}

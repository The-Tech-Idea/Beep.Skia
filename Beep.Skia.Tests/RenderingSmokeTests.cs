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

        [Fact]
        public void DrawingManager_InvokesWorldOverlay_WithPanZoomTransform()
        {
            var dm = new DrawingManager();
            dm.Zoom = 2f;
            dm.PanOffset = new SKPoint(5f, 7f);

            SKPoint observed = default;
            bool invoked = false;
            dm.WorldOverlay = canvas =>
            {
                invoked = true;
                var matrix = canvas.TotalMatrix;
                observed = new SKPoint(matrix.TransX, matrix.TransY);
            };

            using var bmp = new SKBitmap(200, 100, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            using var canvas = new SKCanvas(bmp);
            dm.Draw(canvas);

            Assert.True(invoked, "Expected the world overlay to be invoked on interactive draws");
            Assert.Equal(5f, observed.X, 2);
            Assert.Equal(7f, observed.Y, 2);
        }

        [Fact]
        public void DrawingManager_WorldOverlay_NotInvokedForExport()
        {
            var dm = new DrawingManager();
            bool invoked = false;
            dm.WorldOverlay = canvas => invoked = true;

            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beep_overlay_" + System.Guid.NewGuid().ToString("N") + ".png");
            try
            {
                dm.ExportToPng(path);
                Assert.False(invoked, "Overlays are interactive-only and must not appear in exports");
            }
            finally
            {
                try { System.IO.File.Delete(path); } catch { }
            }
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

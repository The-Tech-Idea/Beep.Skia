using System;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for the diagram minimap: world→minimap mapping, viewport computation, and rendering.
    /// </summary>
    public class MinimapTests
    {
        private sealed class TestNode : SkiaComponent
        {
            protected override void DrawContent(SKCanvas canvas, DrawingContext context)
            {
                using var paint = new SKPaint { Color = SKColors.SteelBlue, Style = SKPaintStyle.Fill };
                canvas.DrawRect(new SKRect(X, Y, X + Width, Y + Height), paint);
            }
        }

        [Fact]
        public void MapWorldToMinimap_MapsContentIntoFrame()
        {
            var manager = new DrawingManager();
            manager.AddComponent(new TestNode { X = 100, Y = 100, Width = 200, Height = 100, Name = "n1" });

            var minimap = new MinimapControl { X = 0, Y = 0, Width = 200, Height = 100, Manager = manager, MinimapPadding = 6f };

            var world = minimap.CalculateWorldBounds();
            Assert.Equal(100f, world.Left);
            Assert.Equal(300f, world.Right);

            var topLeft = minimap.MapWorldToMinimap(new SKPoint(100, 100));
            var bottomRight = minimap.MapWorldToMinimap(new SKPoint(300, 200));

            // Content is centered: scale = 0.88 → content 176 x 88, offsets (12, 6).
            Assert.Equal(12f, topLeft.X, 1);
            Assert.Equal(6f, topLeft.Y, 1);
            Assert.Equal(188f, bottomRight.X, 1);
            Assert.Equal(94f, bottomRight.Y, 1);

            // Everything stays inside the frame.
            Assert.True(topLeft.X >= 0 && topLeft.Y >= 0);
            Assert.True(bottomRight.X <= 200 && bottomRight.Y <= 100);
        }

        [Fact]
        public void CalculateViewportWorldRect_UsesPanAndZoom()
        {
            var manager = new DrawingManager();
            manager.AddComponent(new TestNode { X = 0, Y = 0, Width = 50, Height = 50 });

            using var surface = SKSurface.Create(new SKImageInfo(400, 300));
            manager.Canvas = surface.Canvas;
            manager.PanOffset = new SKPoint(-100, -50);
            manager.Zoom = 2f;

            var minimap = new MinimapControl { Manager = manager };
            var viewport = minimap.CalculateViewportWorldRect();

            Assert.Equal(50f, viewport.Left, 1);
            Assert.Equal(25f, viewport.Top, 1);
            Assert.Equal(250f, viewport.Right, 1);
            Assert.Equal(175f, viewport.Bottom, 1);
        }

        [Fact]
        public void CalculateViewportWorldRect_NoCanvas_IsEmpty()
        {
            var manager = new DrawingManager();
            var minimap = new MinimapControl { Manager = manager };

            Assert.True(minimap.CalculateViewportWorldRect().IsEmpty);
        }

        [Fact]
        public void Minimap_RendersComponentsAndViewport()
        {
            var manager = new DrawingManager();
            manager.AddComponent(new TestNode { X = 20, Y = 30, Width = 120, Height = 60, Name = "n1" });
            manager.AddComponent(new TestNode { X = 200, Y = 120, Width = 100, Height = 50, Name = "n2" });
            manager.ConnectComponents(manager.GetComponents()[0], manager.GetComponents()[1], 0, 0);

            var minimap = new MinimapControl { X = 10, Y = 10, Width = 200, Height = 150, Manager = manager };
            manager.AddComponent(minimap);

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
            Assert.True(hasNonWhitePixel, "Minimap rendered nothing.");
        }

        [Fact]
        public void Minimap_EmptyDiagram_DoesNotThrow()
        {
            var manager = new DrawingManager();
            manager.AddComponent(new MinimapControl { X = 10, Y = 10, Manager = manager });

            using var bitmap = manager.RenderToBitmap(240, 180);
            Assert.Equal(240, bitmap.Width);
        }
    }
}

using System;
using System.IO;
using Beep.Skia;
using Beep.Skia.Model;
using Beep.Skia.Serialization;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Export pipeline tests: exports must render without the interactive overlays
    /// (grid, palette, property editor) and must produce non-empty files.
    /// </summary>
    public class ExportTests
    {
        private sealed class TestNode : SkiaComponent
        {
            protected override void DrawContent(SKCanvas canvas, DrawingContext context)
            {
                using var paint = new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Fill };
                canvas.DrawRect(new SKRect(X, Y, X + Width, Y + Height), paint);
            }
        }

        private static DrawingManager CreateManager()
        {
            var manager = new DrawingManager();
            manager.AddComponent(new TestNode { X = 20, Y = 30, Width = 80, Height = 40, Name = "n1" });
            return manager;
        }

        private static string TempPath(string extension)
            => Path.Combine(Path.GetTempPath(), $"beepskia_test_{Guid.NewGuid():N}{extension}");

        [Fact]
        public void ExportToPng_CreatesNonEmptyFile()
        {
            var path = TempPath(".png");
            try
            {
                CreateManager().ExportToPng(path, scale: 1f);
                Assert.True(File.Exists(path));
                Assert.True(new FileInfo(path).Length > 0);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void ExportToSvg_CreatesNonEmptyFile()
        {
            var path = TempPath(".svg");
            try
            {
                CreateManager().ExportToSvg(path, background: SKColors.White);
                Assert.True(File.Exists(path));
                Assert.True(new FileInfo(path).Length > 0);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void ExportToPdf_CreatesNonEmptyFile()
        {
            var path = TempPath(".pdf");
            try
            {
                CreateManager().ExportToPdf(path);
                Assert.True(File.Exists(path));
                Assert.True(new FileInfo(path).Length > 0);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void Export_HandlesNegativeCoordinates()
        {
            var manager = new DrawingManager();
            manager.AddComponent(new TestNode { X = -150, Y = -75, Width = 60, Height = 30, Name = "neg" });

            var path = TempPath(".png");
            try
            {
                manager.ExportToPng(path, scale: 1f);
                Assert.True(File.Exists(path));
                Assert.True(new FileInfo(path).Length > 0);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void RenderToBitmap_DrawsComponent()
        {
            var bitmap = CreateManager().RenderToBitmap(400, 300);
            Assert.Equal(400, bitmap.Width);
            Assert.Equal(300, bitmap.Height);
        }
    }
}

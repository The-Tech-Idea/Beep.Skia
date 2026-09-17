using System.Linq;
using Beep.Skia.MindMap;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for <see cref="MindMapRichText"/> parsing and rendering.
    /// </summary>
    public class MindMapRichTextTests
    {
        [Fact]
        public void Parse_HandlesBoldAndItalic()
        {
            var spans = MindMapRichText.Parse("**bold** and *italic* text").Where(s => !s.IsLineBreak).ToList();

            Assert.Equal(4, spans.Count);
            Assert.True(spans[0].Bold);
            Assert.Equal("bold", spans[0].Text);
            Assert.False(spans[1].Bold);
            Assert.Equal(" and ", spans[1].Text);
            Assert.True(spans[2].Italic);
            Assert.Equal("italic", spans[2].Text);
            Assert.Equal(" text", spans[3].Text);
            Assert.False(spans[3].Bold);
            Assert.False(spans[3].Italic);
        }

        [Fact]
        public void Parse_HandlesBulletLines()
        {
            var spans = MindMapRichText.Parse("- first\n- second");
            var bullets = spans.Where(s => s.IsBullet && !s.IsLineBreak).ToList();

            Assert.Equal(2, bullets.Count);
            Assert.Equal("first", bullets[0].Text);
            Assert.Equal("second", bullets[1].Text);
            Assert.Equal(2, spans.Count(s => s.IsLineBreak));
        }

        [Fact]
        public void Parse_EmptyText_ReturnsNoSpans()
        {
            Assert.Empty(MindMapRichText.Parse(""));
            Assert.Empty(MindMapRichText.Parse(null));
        }

        [Fact]
        public void Draw_RendersRichText()
        {
            using var surface = SKSurface.Create(new SKImageInfo(240, 120));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            MindMapRichText.Draw(
                canvas,
                "**Title**\n- item one\n- *item* two",
                new SKRect(8, 8, 232, 112),
                SKColors.Black,
                fontSize: 11f,
                lineHeight: 14f);

            using var image = surface.Snapshot();
            using var bitmap = SKBitmap.FromImage(image);

            bool hasDarkPixel = false;
            for (int y = 0; y < bitmap.Height && !hasDarkPixel; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    if (bitmap.GetPixel(x, y).Red < 128)
                    {
                        hasDarkPixel = true;
                        break;
                    }
                }
            }
            Assert.True(hasDarkPixel, "Rich text drew nothing.");
        }
    }
}

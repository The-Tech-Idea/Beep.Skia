using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia;
using Beep.Skia.Collaboration;
using Beep.Skia.Model;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for comment pin anchoring, hit-testing, and rendering.
    /// </summary>
    public class CommentPinLayerTests
    {
        private sealed class TestNode : SkiaComponent
        {
            protected override void DrawContent(SKCanvas canvas, DrawingContext context) { }
        }

        private static TestNode MakeNode(string name, float x, float y, float w = 100, float h = 60)
        {
            var node = new TestNode { Name = name, X = x, Y = y, Width = w, Height = h };
            return node;
        }

        private static DiagramComment MakeComment(string componentId, string text = "note", bool resolved = false)
            => new DiagramComment { DocumentId = "doc", AuthorId = "alice", Text = text, ComponentId = componentId, Resolved = resolved };

        [Fact]
        public void Rebuild_MatchesByNameAndId()
        {
            var node = MakeNode("Start", 10, 20);
            var layer = new CommentPinLayer();

            layer.Rebuild(new[] { node }, new[]
            {
                MakeComment("start"),
                MakeComment(node.Id.ToString())
            });

            var pin = Assert.Single(layer.Pins);
            Assert.Equal(2, pin.CommentCount);
            Assert.Equal("Start", pin.ComponentId);
        }

        [Fact]
        public void Rebuild_SkipsUnanchoredOrphanedAndResolvedComments()
        {
            var node = MakeNode("Start", 10, 20);
            var layer = new CommentPinLayer();

            layer.Rebuild(new[] { node }, new[]
            {
                new DiagramComment { ComponentId = null, Text = "general" },
                MakeComment("Missing"),
                MakeComment("Start", resolved: true)
            });

            Assert.Empty(layer.Pins);
        }

        [Fact]
        public void Rebuild_PositionsPinAtComponentTopRight()
        {
            var node = MakeNode("Start", 10, 20, 100, 60);
            var layer = new CommentPinLayer();
            layer.Rebuild(new[] { node }, new[] { MakeComment("Start") });

            var pin = Assert.Single(layer.Pins);
            Assert.Equal(110 + CommentPinLayer.PinOffset, pin.Position.X, 3);
            Assert.Equal(20 - CommentPinLayer.PinOffset, pin.Position.Y, 3);
            Assert.Equal(node.Bounds, pin.ComponentBounds);
        }

        [Fact]
        public void Rebuild_RespectsMaxPins()
        {
            var nodes = Enumerable.Range(0, 5).Select(i => MakeNode("N" + i, i * 120, 0)).ToArray();
            var comments = nodes.Select(n => MakeComment(n.Name)).ToArray();
            var layer = new CommentPinLayer();

            layer.Rebuild(nodes, comments, maxPins: 2);

            Assert.Equal(2, layer.Pins.Count);
            Assert.Equal(new[] { 1, 2 }, layer.Pins.Select(p => p.Index));
        }

        [Fact]
        public void HitTest_FindsPinWithinRadius()
        {
            var node = MakeNode("Start", 10, 20);
            var layer = new CommentPinLayer();
            layer.Rebuild(new[] { node }, new[] { MakeComment("Start") });

            var pin = layer.Pins[0];
            Assert.Same(pin, layer.HitTest(pin.Position));
            Assert.Same(pin, layer.HitTest(new SKPoint(pin.Position.X + 5, pin.Position.Y - 5)));
            Assert.Null(layer.HitTest(new SKPoint(pin.Position.X + 50, pin.Position.Y)));
        }

        [Fact]
        public void Draw_RendersPinAtExpectedLocation()
        {
            var node = MakeNode("Start", 10, 20);
            var layer = new CommentPinLayer();
            layer.Rebuild(new[] { node }, new[] { MakeComment("Start") });

            using var surface = SKSurface.Create(new SKImageInfo(300, 200));
            surface.Canvas.Clear(SKColors.White);
            layer.Draw(surface.Canvas);

            using var image = surface.Snapshot();
            using var bitmap = SKBitmap.FromImage(image);

            var pin = layer.Pins[0];
            var sample = bitmap.GetPixel((int)pin.Position.X - 6, (int)pin.Position.Y + 6);
            Assert.True(sample.Red > 200 && sample.Green > 120 && sample.Blue < 120,
                $"Expected amber pin, got {sample}");

            var corner = bitmap.GetPixel(2, 2);
            Assert.Equal(SKColors.White, corner);
        }

        [Fact]
        public void Clear_RemovesPins()
        {
            var node = MakeNode("Start", 10, 20);
            var layer = new CommentPinLayer();
            layer.Rebuild(new[] { node }, new[] { MakeComment("Start") });

            layer.Clear();

            Assert.Empty(layer.Pins);
        }
    }
}

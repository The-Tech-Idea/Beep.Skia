using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Render smoke tests for the components whose path/text drawing was migrated to
    /// SKPathBuilder / the SKTextAlign DrawText overload. Each test renders into a bitmap
    /// and asserts that pixels were actually produced.
    /// </summary>
    public class ComponentRenderSmokeTests
    {
        private const int Width = 320;
        private const int Height = 240;

        private static int DrawAndCountInk(SkiaComponent component, SKColor? background = null)
        {
            using var surface = SKSurface.Create(new SKImageInfo(Width, Height));
            var canvas = surface.Canvas;
            canvas.Clear(background ?? SKColors.White);
            component.Draw(canvas);
            canvas.Flush();

            using var image = surface.Snapshot();
            using var bitmap = SKBitmap.FromImage(image);

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

        [Fact]
        public void Checkbox_CheckmarkRenders()
        {
            var checkbox = new Checkbox { X = 20, Y = 20, Width = 120, Height = 40, IsChecked = true };
            Assert.True(DrawAndCountInk(checkbox) > 20, "checked Checkbox should draw a checkmark");
        }

        [Fact]
        public void ProgressBar_RendersDeterminateAndIndeterminate()
        {
            var determinate = new ProgressBar { X = 20, Y = 20, Width = 200, Height = 20, Progress = 0.5f };
            Assert.True(DrawAndCountInk(determinate) > 50, "determinate progress bar should draw a track and fill");

            var indeterminate = new ProgressBar { X = 20, Y = 60, Width = 200, Height = 20, IsIndeterminate = true };
            Assert.True(DrawAndCountInk(indeterminate) > 20, "indeterminate progress bar should draw");
        }

        [Fact]
        public void Search_RendersIconAndText()
        {
            var search = new Search { X = 20, Y = 20, Width = 200, Height = 40, PlaceholderText = "Search" };
            Assert.True(DrawAndCountInk(search) > 20, "search box should draw its icon/underline");
        }

        [Fact]
        public void Dropdown_RendersArrow()
        {
            var dropdown = new Dropdown { X = 20, Y = 20, Width = 180, Height = 40, Placeholder = "Choose" };
            dropdown.AddItem("First");
            Assert.True(DrawAndCountInk(dropdown) > 20, "dropdown should draw its arrow and text");
        }

        [Fact]
        public void SplitButton_RendersBothSegments()
        {
            var split = new SplitButton { X = 20, Y = 20, Width = 180, Height = 40 };
            Assert.True(DrawAndCountInk(split) > 50, "split button should draw both segments");
        }

        [Fact]
        public void SegmentedButtons_RenderAllSegmentShapes()
        {
            var segmented = new SegmentedButtons { X = 20, Y = 20, Width = 240, Height = 40 };
            segmented.AddSegment("One");
            segmented.AddSegment("Two");
            segmented.AddSegment("Three");
            Assert.True(DrawAndCountInk(segmented) > 50, "segmented buttons should draw their shapes");
        }

        [Fact]
        public void ManualTriggerNode_RendersIcon()
        {
            var node = new ManualTriggerNode { X = 20, Y = 20, Width = 200, Height = 90, TriggerText = "Run" };
            Assert.True(DrawAndCountInk(node) > 50, "manual trigger node should draw its body and play icon");
        }

        [Fact]
        public void ConditionalNode_RendersDiamond()
        {
            var node = new ConditionalNode { X = 20, Y = 20, Width = 200, Height = 120 };
            Assert.True(DrawAndCountInk(node) > 50, "conditional node should draw its diamond outline");
        }

        [Fact]
        public void ConnectionLine_RendersBezierAndArrow()
        {
            var manager = new DrawingManager();
            var from = new ManualTriggerNode { X = 20, Y = 20, Width = 140, Height = 60, Name = "from" };
            var to = new ConditionalNode { X = 20, Y = 160, Width = 140, Height = 80, Name = "to" };
            manager.AddComponent(from);
            manager.AddComponent(to);

            manager.ConnectComponents(from, to);

            using var surface = SKSurface.Create(new SKImageInfo(Width, Height));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            manager.Draw(canvas);
            canvas.Flush();

            using var image = surface.Snapshot();
            using var bitmap = SKBitmap.FromImage(image);

            var ink = 0;
            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    if (pixel != SKColors.White && pixel.Alpha > 0) ink++;
                }
            }

            Assert.True(ink > 100, "the diagram with a connection line should render");
        }

        private static SKBitmap RenderToBitmap(SkiaComponent component)
        {
            using var surface = SKSurface.Create(new SKImageInfo(Width, Height));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            component.Draw(canvas);
            canvas.Flush();
            using var image = surface.Snapshot();
            return SKBitmap.FromImage(image);
        }

        private static int CountDifferences(SKBitmap left, SKBitmap right)
        {
            var differences = 0;
            for (var y = 0; y < left.Height; y++)
            {
                for (var x = 0; x < left.Width; x++)
                {
                    if (left.GetPixel(x, y) != right.GetPixel(x, y)) differences++;
                }
            }
            return differences;
        }

        [Fact]
        public void TextBox_SelectionHighlightRenders()
        {
            var textBox = new TextBox { X = 20, Y = 20, Width = 220, Height = 44, Text = "select me" };
            using var plain = RenderToBitmap(textBox);

            textBox.SelectAll();
            Assert.True(textBox.HasSelection);
            Assert.Equal("select me", textBox.SelectedText);

            using var selected = RenderToBitmap(textBox);
            Assert.True(CountDifferences(plain, selected) > 0,
                "the selection highlight must change the rendering");
        }

        [Fact]
        public void TextBox_ClearSelection_RemovesHighlight()
        {
            var textBox = new TextBox { X = 20, Y = 20, Width = 220, Height = 44, Text = "select me" };
            using var plain = RenderToBitmap(textBox);

            textBox.SelectAll();
            using var selected = RenderToBitmap(textBox);
            Assert.True(CountDifferences(plain, selected) > 0);

            textBox.ClearSelection();
            Assert.False(textBox.HasSelection);
            Assert.Equal(string.Empty, textBox.SelectedText);

            using var cleared = RenderToBitmap(textBox);
            Assert.Equal(0, CountDifferences(plain, cleared));
        }
    }
}

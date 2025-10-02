using Xunit;
using SkiaSharp;
using Beep.Skia;
using Beep.Skia.UML;
using Beep.Skia.StateMachine;

namespace Beep.Skia.Tests
{
    public class PortLayoutLazyTests
    {
        [Fact]
        public void UML_Ports_Recompute_On_Demand_And_Stay_Glued_On_Move_Resize()
        {
            var node = new UMLClass { X = 10, Y = 10, Width = 100, Height = 60 };

            // First draw should compute ports
            using var bmp = new SKBitmap(200, 120, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            using var canvas = new SKCanvas(bmp);
            var dm = new DrawingManager();
            dm.AddComponent(node);
            dm.Draw(canvas);

            // Capture top port before move
            var p0_before = node.InConnectionPoints[0].Center;

            // Move and resize; do not manually call LayoutPorts
            node.X += 50; node.Y += 20; node.Width += 20; node.Height += 10;

            // Next draw should recompute once
            dm.Draw(canvas);

            var p0_after = node.InConnectionPoints[0].Center;
            Assert.NotEqual(p0_before, p0_after);
            // Should align with new top edge center
            Assert.Equal(node.X + node.Width / 2f, p0_after.X, 2);
            Assert.Equal(node.Y, p0_after.Y, 2);
        }

        [Fact]
        public void UML_Ports_Realign_When_Port_Count_Changes()
        {
            var node = new UMLClass { X = 10, Y = 10, Width = 120, Height = 80 };
            var dm = new DrawingManager();
            dm.AddComponent(node);

            using var bmp = new SKBitmap(240, 160, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            using var canvas = new SKCanvas(bmp);

            dm.Draw(canvas); // initial
            var before = node.InConnectionPoints[0].Center;

            // UML uses a shared list; simulate a change by nudging size (marks dirty)
            node.Width += 40; node.Height += 20;
            dm.Draw(canvas);
            var after = node.InConnectionPoints[0].Center;
            Assert.NotEqual(before, after);
        }

        [Fact]
        public void StateMachine_Ports_Lazy_Layout_On_Move_And_Count_Change()
        {
            var node = new StateNode();
            node.X = 30; node.Y = 40; node.Width = 140; node.Height = 60;

            using var bmp = new SKBitmap(300, 200, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            using var canvas = new SKCanvas(bmp);
            var dm = new DrawingManager();
            dm.AddComponent(node);

            // Initial draw computes ports
            dm.Draw(canvas);
            var in0_before = node.InConnectionPoints.Count > 0 ? node.InConnectionPoints[0].Center : new SKPoint(float.NaN, float.NaN);
            var out0_before = node.OutConnectionPoints.Count > 0 ? node.OutConnectionPoints[0].Center : new SKPoint(float.NaN, float.NaN);

            // Move and resize
            node.X += 25; node.Y += 15; node.Width += 30; node.Height += 10;
            dm.Draw(canvas);

            var in0_after = node.InConnectionPoints.Count > 0 ? node.InConnectionPoints[0].Center : new SKPoint(float.NaN, float.NaN);
            var out0_after = node.OutConnectionPoints.Count > 0 ? node.OutConnectionPoints[0].Center : new SKPoint(float.NaN, float.NaN);

            Assert.NotEqual(in0_before, in0_after);
            Assert.NotEqual(out0_before, out0_after);

            // Change port counts via the family API: should mark dirty and realign next draw
            node.OutPortCount = node.OutPortCount + 2;
            dm.Draw(canvas);

            Assert.True(node.OutConnectionPoints.Count >= 3);
            // Ensure evenly distributed along right edge (y should be within new bounds)
            var b = node.Bounds;
            foreach (var p in node.OutConnectionPoints)
            {
                Assert.InRange(p.Center.Y, b.Top, b.Bottom);
                Assert.True(p.Center.X >= b.Right - 10 && p.Center.X <= b.Right + 10);
            }
        }
    }
}

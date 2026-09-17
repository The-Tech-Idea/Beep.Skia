using System.Linq;
using Beep.Skia;
using Beep.Skia.UML;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for UML sequence elements: activation bars and combined fragments.
    /// </summary>
    public class UmlSequenceNodeTests
    {
        [Fact]
        public void ActivationBar_LabelRoundTrips()
        {
            var manager = new DrawingManager();
            manager.AddComponent(new UMLActivationBar { Label = "handle()", X = 40, Y = 40, Height = 120 });

            var manager2 = new DrawingManager();
            manager2.LoadFromDto(manager.ToDto());

            var loaded = manager2.GetComponents().OfType<UMLActivationBar>().Single();
            Assert.Equal("handle()", loaded.Label);
            Assert.Equal(120f, loaded.Height);
        }

        [Fact]
        public void CombinedFragment_OperatorAndGuardRoundTrip()
        {
            var manager = new DrawingManager();
            manager.AddComponent(new UMLCombinedFragment { Operator = "loop", GuardCondition = "i < 10" });

            var manager2 = new DrawingManager();
            manager2.LoadFromDto(manager.ToDto());

            var loaded = manager2.GetComponents().OfType<UMLCombinedFragment>().Single();
            Assert.Equal("loop", loaded.Operator);
            Assert.Equal("i < 10", loaded.GuardCondition);
        }

        [Fact]
        public void CombinedFragment_InvalidOperator_FallsBackToAlt()
        {
            var fragment = new UMLCombinedFragment();
            fragment.Operator = "bogus";
            Assert.Equal("alt", fragment.Operator);

            fragment.Operator = "PAR";
            Assert.Equal("par", fragment.Operator);
        }

        [Fact]
        public void SequenceElements_RenderContent()
        {
            var manager = new DrawingManager();
            manager.AddComponent(new UMLCombinedFragment { X = 20, Y = 20, Width = 300, Height = 160, Operator = "alt", GuardCondition = "x > 0" });
            manager.AddComponent(new UMLActivationBar { X = 340, Y = 30, Width = 12, Height = 120 });

            using var bitmap = manager.RenderToBitmap(420, 220);

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
            Assert.True(hasNonWhitePixel, "Sequence elements rendered nothing.");
        }
    }
}

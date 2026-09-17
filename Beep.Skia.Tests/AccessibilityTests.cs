using System.Linq;
using System.Windows.Forms;
using Beep.Skia;
using Beep.Skia.Model;
using Beep.Skia.Winform.Controls;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for accessibility basics: keyboard selection cycling, host accessible metadata,
    /// and high-contrast theme availability.
    /// </summary>
    public class AccessibilityTests
    {
        private sealed class TestNode : SkiaComponent
        {
            protected override void DrawContent(SKCanvas canvas, DrawingContext context) { }
        }

        [Fact]
        public void SelectNext_CyclesInReadingOrder()
        {
            var manager = new DrawingManager();
            var a = new TestNode { X = 0, Y = 0, Width = 50, Height = 50, Name = "A" };
            var b = new TestNode { X = 0, Y = 100, Width = 50, Height = 50, Name = "B" };
            var c = new TestNode { X = 0, Y = 200, Width = 50, Height = 50, Name = "C" };
            manager.AddComponent(a);
            manager.AddComponent(b);
            manager.AddComponent(c);

            Assert.Equal(a, manager.SelectNextComponent());
            Assert.Equal(b, manager.SelectNextComponent());
            Assert.Equal(c, manager.SelectNextComponent());
            Assert.Equal(a, manager.SelectNextComponent()); // wraps
        }

        [Fact]
        public void SelectNext_Backward_WrapsToLast()
        {
            var manager = new DrawingManager();
            var a = new TestNode { X = 0, Y = 0, Name = "A" };
            var b = new TestNode { X = 0, Y = 100, Name = "B" };
            manager.AddComponent(a);
            manager.AddComponent(b);

            Assert.Equal(b, manager.SelectNextComponent(forward: false));
            Assert.Equal(a, manager.SelectNextComponent(forward: false));
        }

        [Fact]
        public void SelectNext_SkipsStaticComponents()
        {
            var manager = new DrawingManager();
            var normal = new TestNode { X = 0, Y = 0, Name = "Normal" };
            var overlay = new TestNode { X = 0, Y = 50, Name = "Overlay", IsStatic = true };
            manager.AddComponent(normal);
            manager.AddComponent(overlay);

            Assert.Equal(normal, manager.SelectNextComponent());
            Assert.Equal(normal, manager.SelectNextComponent()); // static overlay never selected
        }

        [Fact]
        public void SelectNext_EmptyDiagram_ReturnsNull()
        {
            var manager = new DrawingManager();
            Assert.Null(manager.SelectNextComponent());
        }

        [Fact]
        public void SelectNext_AdvancesFromCurrentSelection()
        {
            var manager = new DrawingManager();
            var a = new TestNode { X = 0, Y = 0, Name = "A" };
            var b = new TestNode { X = 100, Y = 0, Name = "B" };
            manager.AddComponent(a);
            manager.AddComponent(b);

            manager.SelectionManager.AddToSelection(a);
            Assert.Equal(b, manager.SelectNextComponent());
            Assert.True(b.IsSelected);
            Assert.False(a.IsSelected);
        }

        [Fact]
        public void Host_ExposesAccessibleMetadata()
        {
            using var host = new SkiaHostControl();

            Assert.Equal("Skia diagram canvas", host.AccessibleName);
            Assert.Equal(AccessibleRole.Diagram, host.AccessibleRole);
            Assert.False(string.IsNullOrWhiteSpace(host.AccessibleDescription));
        }

        [Fact]
        public void HighContrastTheme_IsAvailableAndApplies()
        {
            var original = ThemeManager.Current.Name;
            try
            {
                Assert.Contains("HighContrast", ThemeManager.BuiltInThemes);

                ThemeManager.ApplyTheme("HighContrast");
                Assert.Equal("HighContrast", ThemeManager.Current.Name);
            }
            finally
            {
                ThemeManager.ApplyTheme(original);
            }
        }
    }
}

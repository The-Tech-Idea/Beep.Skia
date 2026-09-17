using Beep.Skia;
using Beep.Skia.DFD;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for <see cref="DFDLevelNavigator"/> drill-down navigation.
    /// </summary>
    public class DFDLevelNavigatorTests
    {
        [Fact]
        public void Reset_SetsTopLevel()
        {
            var nav = new DFDLevelNavigator();
            nav.Reset(new Beep.Skia.Serialization.DiagramDto());

            Assert.Equal(0, nav.Depth);
            Assert.False(nav.CanGoUp);
            Assert.Equal("Level 0", nav.Breadcrumb);
            Assert.NotNull(nav.Current);
        }

        [Fact]
        public void DrillDown_And_GoUp_RestoreFrames()
        {
            var nav = new DFDLevelNavigator();
            var root = new Beep.Skia.Serialization.DiagramDto();
            var child = new Beep.Skia.Serialization.DiagramDto();
            nav.Reset(root);

            var drilled = nav.DrillDown("Process 1", root, child);
            Assert.True(drilled);
            Assert.Equal(1, nav.Depth);
            Assert.True(nav.CanGoUp);
            Assert.Same(child, nav.Current.Diagram);
            Assert.Equal("Level 0 > Process 1", nav.Breadcrumb);

            var restored = nav.GoUp();
            Assert.NotNull(restored);
            Assert.Same(root, restored.Diagram);
            Assert.Equal(0, nav.Depth);
            Assert.False(nav.CanGoUp);
            Assert.Equal("Level 0", nav.Breadcrumb);
        }

        [Fact]
        public void NestedDrillDown_BuildsBreadcrumb()
        {
            var nav = new DFDLevelNavigator();
            nav.Reset(new Beep.Skia.Serialization.DiagramDto());
            nav.DrillDown("P1", new Beep.Skia.Serialization.DiagramDto(), new Beep.Skia.Serialization.DiagramDto());
            nav.DrillDown("P1.1", new Beep.Skia.Serialization.DiagramDto(), new Beep.Skia.Serialization.DiagramDto());

            Assert.Equal(2, nav.Depth);
            Assert.Equal("Level 0 > P1 > P1.1", nav.Breadcrumb);
        }

        [Fact]
        public void DrillDown_WithNullDiagram_DoesNothing()
        {
            var nav = new DFDLevelNavigator();
            nav.Reset(new Beep.Skia.Serialization.DiagramDto());

            Assert.False(nav.DrillDown("P1", new Beep.Skia.Serialization.DiagramDto(), null));
            Assert.Equal(0, nav.Depth);
        }
    }
}

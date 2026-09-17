using System.ComponentModel;
using System.Linq;
using Beep.Skia.StateMachine;
using Beep.Skia.Winform.Controls;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for the WinForms PropertyGrid wrappers: write-back, color editing, multi-selection.
    /// </summary>
    public class SkiaPropertyWrapperTests
    {
        [Fact]
        public void Wrapper_NodePropertyEdit_AppliesToComponent()
        {
            var node = new StateNode { Title = "Original" };
            var wrapper = new SkiaComponentPropertyWrapper(node);

            var descriptor = ((ICustomTypeDescriptor)wrapper).GetProperties().Find("Title", false);
            Assert.NotNull(descriptor);

            descriptor.SetValue(wrapper, "Renamed");

            Assert.Equal("Renamed", node.Title);
            Assert.Equal("Renamed", node.NodeProperties["Title"].ParameterCurrentValue);
        }

        [Fact]
        public void Wrapper_GeometryEdit_AppliesAndNotifies()
        {
            var node = new StateNode();
            bool notified = false;
            var wrapper = new SkiaComponentPropertyWrapper(node, _ => notified = true);

            var xDescriptor = ((ICustomTypeDescriptor)wrapper).GetProperties().Find("X", false);
            xDescriptor.SetValue(wrapper, 42f);

            Assert.Equal(42f, node.X);
            Assert.True(notified);
        }

        [Fact]
        public void ColorConverter_RoundTripsHex()
        {
            var color = SkiaColorConverter.Parse("#FF1E88E5");
            Assert.Equal(255, color.Alpha);
            Assert.Equal(0x1E, color.Red);
            Assert.Equal(0x88, color.Green);
            Assert.Equal(0xE5, color.Blue);

            Assert.Equal("#FF1E88E5", SkiaColorConverter.Format(color));

            var rgb = SkiaColorConverter.Parse("#123456");
            Assert.Equal(255, rgb.Alpha);
            Assert.Equal(0x12, rgb.Red);

            Assert.Equal(SKColors.Transparent, SkiaColorConverter.Parse("not-a-color"));
        }

        [Fact]
        public void Wrapper_ColorProperty_UsesColorConverter()
        {
            var node = new StateNode();
            node.NodeProperties["BackgroundColor"].ParameterType = typeof(SKColor);
            var wrapper = new SkiaComponentPropertyWrapper(node);

            var descriptor = ((ICustomTypeDescriptor)wrapper).GetProperties().Find("BackgroundColor", false);
            Assert.NotNull(descriptor);
            Assert.IsType<SkiaColorConverter>(descriptor.Converter);

            descriptor.SetValue(wrapper, "#FF123456");
            Assert.Equal(new SKColor(0x12, 0x34, 0x56), node.BackgroundColor);
        }

        [Fact]
        public void MultiWrapper_AppliesGeometryToAllSelected()
        {
            var a = new StateNode();
            var b = new StateNode();
            var wrapper = new SkiaMultiComponentWrapper(new[] { a, b });

            var xDescriptor = ((ICustomTypeDescriptor)wrapper).GetProperties().Find("X", false);
            xDescriptor.SetValue(wrapper, 25f);

            Assert.Equal(25f, a.X);
            Assert.Equal(25f, b.X);
        }

        [Fact]
        public void MultiWrapper_ExposesOnlyCommonProperties()
        {
            var state = new StateNode();
            var final = new FinalStateNode();
            var wrapper = new SkiaMultiComponentWrapper(new[] { (Beep.Skia.SkiaComponent)state, final });

            var properties = ((ICustomTypeDescriptor)wrapper).GetProperties();
            Assert.NotNull(properties.Find("Title", false));
            Assert.Null(properties.Find("EntryAction", false));

            properties.Find("Title", false).SetValue(wrapper, "Shared");
            Assert.Equal("Shared", state.Title);
            Assert.Equal("Shared", final.Title);
        }

        [Fact]
        public void MultiWrapper_ReportsSelectionCount()
        {
            var wrapper = new SkiaMultiComponentWrapper(new[] { new StateNode(), new StateNode(), new StateNode() });

            Assert.Equal(3, wrapper.Count);
            Assert.Contains("3", ((ICustomTypeDescriptor)wrapper).GetComponentName());
        }
    }
}

using System;
using Xunit;
using Beep.Skia.Winform.Controls;
using Beep.Skia;
using System.Linq;

namespace Beep.Skia.Tests
{
    public class SkiaHostDescriptorTests
    {
    [Fact]
        public void EndInit_InstantiatesDescriptors_AssignsNames()
        {
            var host = new SkiaHostControl();

            // Create a descriptor without a name
            var desc = new SkiaComponentDescriptor
            {
                ComponentType = typeof(Beep.Skia.Components.Button).AssemblyQualifiedName,
                X = 10,
                Y = 20,
                Width = 100,
                Height = 30,
                Name = null
            };
            host.DesignTimeComponents.Add(desc);

            // Simulate end init (runtime path)
            host.EndInit();

            // Verify drawing manager has a component
            var dm = host.DrawingManager;
            var comps = dm.GetType().GetProperty("Components", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(dm) as System.Collections.IEnumerable;
            Assert.NotNull(comps);
            int count = 0;
            string assignedName = null;
            foreach (var c in comps)
            {
                count++;
                var nprop = c.GetType().GetProperty("Name");
                if (nprop != null) assignedName = nprop.GetValue(c) as string;
            }

            Assert.True(count >= 1, "Expected at least one instantiated component");
            Assert.False(string.IsNullOrEmpty(assignedName));
        }

    [Fact]
        public void EndInit_PreservesDescriptorName_WhenProvided()
        {
            var host = new SkiaHostControl();

            var desc = new SkiaComponentDescriptor
            {
                ComponentType = typeof(Beep.Skia.Components.Button).AssemblyQualifiedName,
                X = 5,
                Y = 6,
                Width = 50,
                Height = 20,
                Name = "mySkiaButton"
            };
            host.DesignTimeComponents.Add(desc);
            host.EndInit();

            var dm = host.DrawingManager;
            var comps = dm.GetType().GetProperty("Components", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(dm) as System.Collections.IEnumerable;
            Assert.NotNull(comps);
            bool found = false;
            foreach (var c in comps)
            {
                var nprop = c.GetType().GetProperty("Name");
                if (nprop != null && (nprop.GetValue(c) as string) == "mySkiaButton") found = true;
            }

            Assert.True(found, "Component with descriptor name should be present");
        }
    }
}

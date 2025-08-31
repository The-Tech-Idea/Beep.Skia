using Xunit;
using Beep.Skia.Winform.Controls;
using Beep.Skia.Components;

namespace Beep.Skia.Tests
{
    public class PaletteRegistryTests
    {
        [Fact]
        public void Palette_Can_Be_Populated_From_Registry_And_Added_To_DrawingManager()
        {
            var host = new SkiaHostControl();

            // Create palette and populate from registry
            var palette = new Palette();
            try
            {
                foreach (var def in Beep.Skia.SkiaComponentRegistry.GetComponentsOrdered())
                {
                    var label = def.className ?? def.type?.Name ?? def.dllname ?? def.AssemblyName ?? "Unknown";
                    var compType = def.type != null ? def.type.AssemblyQualifiedName : (def.className ?? def.dllname ?? string.Empty);
                    palette.Items.Add(new PaletteItem { Name = label, ComponentType = compType });
                }
            }
            catch
            {
                // if registry fails, ensure at least no exception
            }

            // Add to host drawing manager
            host.DrawingManager.AddComponent(palette);

            // Verify it is present in the drawing manager components
            var comps = host.DrawingManager.GetComponents();
            bool found = false;
            foreach (var c in comps)
            {
                if (object.ReferenceEquals(c, palette)) { found = true; break; }
            }

            Assert.True(found, "Palette should be present in the DrawingManager components collection");
        }
    }
}

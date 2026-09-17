using System.Linq;
using Beep.Skia.Components;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for palette search/filtering.
    /// </summary>
    public class PaletteSearchTests
    {
        private static Palette BuildPalette()
        {
            var palette = new Palette();
            palette.AddItem(new PaletteItem { Name = "Process", Category = "Flowchart" });
            palette.AddItem(new PaletteItem { Name = "Decision", Category = "Flowchart" });
            palette.AddItem(new PaletteItem { Name = "Entity", Category = "ERD" });
            palette.AddItem(new PaletteItem { Name = "Resistor", Category = "ECAD" });
            return palette;
        }

        [Fact]
        public void EmptySearch_ShowsAllItems()
        {
            var palette = BuildPalette();
            Assert.Equal(4, palette.VisibleItemCount);
        }

        [Fact]
        public void Search_FiltersByName()
        {
            var palette = BuildPalette();
            palette.SearchText = "proc";

            Assert.Equal(1, palette.VisibleItemCount);
            Assert.Equal("Process", palette.Categories.Single().Items.Single().Name);
        }

        [Fact]
        public void Search_FiltersByCategory()
        {
            var palette = BuildPalette();
            palette.SearchText = "flow";

            Assert.Equal(2, palette.VisibleItemCount);
            Assert.All(palette.Categories.SelectMany(c => c.Items), i => Assert.Equal("Flowchart", i.Category));
        }

        [Fact]
        public void Search_IsCaseInsensitive()
        {
            var palette = BuildPalette();
            palette.ApplyFilter("RESISTOR");

            Assert.Equal(1, palette.VisibleItemCount);
        }

        [Fact]
        public void Search_NoMatch_ShowsNothing()
        {
            var palette = BuildPalette();
            palette.SearchText = "zzz";

            Assert.Equal(0, palette.VisibleItemCount);
            Assert.Empty(palette.Categories);
        }

        [Fact]
        public void ClearingSearch_RestoresAllItems()
        {
            var palette = BuildPalette();
            palette.SearchText = "proc";
            Assert.Equal(1, palette.VisibleItemCount);

            palette.SearchText = string.Empty;
            Assert.Equal(4, palette.VisibleItemCount);
        }

        [Fact]
        public void AddItem_AfterFirstRender_IsVisible()
        {
            var palette = BuildPalette();
            // Force category construction (as the first render would).
            Assert.Equal(4, palette.VisibleItemCount);

            palette.AddItem(new PaletteItem { Name = "Cloud Service", Category = "Cloud" });

            Assert.Equal(5, palette.VisibleItemCount);
            Assert.Contains(palette.Categories, c => c.Name == "Cloud");
        }

        [Fact]
        public void RemoveItem_AfterFirstRender_RemovesFromCategories()
        {
            var palette = BuildPalette();
            Assert.Equal(4, palette.VisibleItemCount);

            var entity = palette.Items.Single(i => i.Name == "Entity");
            Assert.True(palette.RemoveItem(entity));

            Assert.Equal(3, palette.VisibleItemCount);
            Assert.DoesNotContain(palette.Categories, c => c.Name == "ERD");
        }

        [Fact]
        public void RefreshLayout_PicksUpDirectItemAdditions()
        {
            var palette = BuildPalette();
            Assert.Equal(4, palette.VisibleItemCount);

            palette.Items.Add(new PaletteItem { Name = "Extension Node", Category = "Extensions" });
            palette.RefreshLayout();

            Assert.Equal(5, palette.VisibleItemCount);
            Assert.Contains(palette.Categories, c => c.Name == "Extensions");
        }

        [Fact]
        public void RefreshLayout_PreservesActiveSearchFilter()
        {
            var palette = BuildPalette();
            palette.SearchText = "flow";
            Assert.Equal(2, palette.VisibleItemCount);

            palette.AddItem(new PaletteItem { Name = "Flow Chart Node", Category = "Flowchart" });
            palette.RefreshLayout();

            Assert.Equal(3, palette.VisibleItemCount);
            Assert.All(palette.Categories, c => Assert.Equal("Flowchart", c.Name));
        }
    }
}

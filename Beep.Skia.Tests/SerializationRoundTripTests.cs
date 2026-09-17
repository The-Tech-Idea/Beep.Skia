using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Beep.Skia;
using Beep.Skia.Model;
using Beep.Skia.Serialization;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Serialization round-trip tests: schema version, theme, simple values, and
    /// complex (list) values must survive ToDto -> JSON -> LoadFromDto.
    /// </summary>
    public class SerializationRoundTripTests
    {
        private sealed class ComplexPropsNode : SkiaComponent
        {
            public ComplexPropsNode()
            {
                NodeProperties["Title"] = new ParameterInfo
                {
                    ParameterName = "Title",
                    ParameterType = typeof(string),
                    DefaultParameterValue = "node",
                    ParameterCurrentValue = "node"
                };
                NodeProperties["Tags"] = new ParameterInfo
                {
                    ParameterName = "Tags",
                    ParameterType = typeof(List<string>),
                    DefaultParameterValue = new List<string>(),
                    ParameterCurrentValue = new List<string> { "alpha", "beta" }
                };
            }

            protected override void DrawContent(SKCanvas canvas, DrawingContext context) { }
        }

        [Fact]
        public void ToDto_StampsSchemaVersionAndTheme()
        {
            var manager = new DrawingManager();
            var dto = manager.ToDto();

            Assert.Equal(DiagramDto.CurrentSchemaVersion, dto.SchemaVersion);
            Assert.False(string.IsNullOrWhiteSpace(dto.ThemeName));
        }

        [Fact]
        public void RoundTrip_PreservesSimpleAndComplexProperties()
        {
            var manager = new DrawingManager();
            var node = new ComplexPropsNode { X = 10, Y = 20, Width = 100, Height = 50, Name = "complex" };
            manager.AddComponent(node);

            var dto = manager.ToDto();
            var json = JsonSerializer.Serialize(dto);
            var restored = JsonSerializer.Deserialize<DiagramDto>(json);
            Assert.NotNull(restored);

            var manager2 = new DrawingManager();
            manager2.LoadFromDto(restored);

            var loaded = manager2.GetComponents().OfType<ComplexPropsNode>().SingleOrDefault();
            Assert.NotNull(loaded);
            Assert.Equal("complex", loaded.Name);

            // Simple value survived the string bag.
            Assert.Equal("node", loaded.NodeProperties["Title"].ParameterCurrentValue);

            // Complex value survived the typed bag.
            var tags = loaded.NodeProperties["Tags"].ParameterCurrentValue as IEnumerable<object>;
            Assert.NotNull(tags);
            Assert.Equal(2, tags.Count());
        }

        [Fact]
        public void RoundTrip_PreservesThemeName()
        {
            var manager = new DrawingManager();
            var dto = manager.ToDto();
            dto.ThemeName = "Dark";

            var json = JsonSerializer.Serialize(dto);
            var restored = JsonSerializer.Deserialize<DiagramDto>(json);

            Assert.Equal("Dark", restored.ThemeName);
        }
    }
}

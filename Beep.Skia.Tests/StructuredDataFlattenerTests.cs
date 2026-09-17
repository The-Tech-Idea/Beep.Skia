using System.Linq;
using Beep.Skia.ETL;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for JSON/XML flattening and schema inference.
    /// </summary>
    public class StructuredDataFlattenerTests
    {
        private const string OrdersJson = """
        {
          "data": {
            "items": [
              { "id": 1, "customer": { "name": "Ada", "address": { "city": "London" } }, "total": 19.95, "tags": ["a", "b"], "notes": null },
              { "id": 2, "customer": { "name": "Grace", "address": { "city": "New York" } }, "total": 42, "tags": [], "notes": "priority" }
            ]
          }
        }
        """;

        [Fact]
        public void FlattenJson_NestedObjectsBecomeDotColumns()
        {
            var rows = new StructuredDataFlattener().FlattenJson(OrdersJson, "data.items");

            Assert.Equal(2, rows.Count);
            Assert.Equal(1L, rows[0]["id"]);
            Assert.Equal("Ada", rows[0]["customer.name"]);
            Assert.Equal("London", rows[0]["customer.address.city"]);
            Assert.Equal(19.95, rows[0]["total"]);
            Assert.Null(rows[0]["notes"]);
            Assert.Equal("priority", rows[1]["notes"]);
        }

        [Fact]
        public void FlattenJson_PreservesArraysAsJsonText()
        {
            var rows = new StructuredDataFlattener().FlattenJson(OrdersJson, "data.items");

            Assert.Equal("[\"a\",\"b\"]", rows[0]["tags"]);
            Assert.Equal("[]", rows[1]["tags"]);
        }

        [Fact]
        public void FlattenJson_RootObjectBecomesSingleRow()
        {
            var rows = new StructuredDataFlattener().FlattenJson("{\"name\":\"Ada\",\"age\":36}");

            Assert.Single(rows);
            Assert.Equal("Ada", rows[0]["name"]);
            Assert.Equal(36L, rows[0]["age"]);
        }

        [Fact]
        public void FlattenJson_UnknownRootPathReturnsEmpty()
        {
            var rows = new StructuredDataFlattener().FlattenJson(OrdersJson, "data.missing");
            Assert.Empty(rows);
        }

        [Fact]
        public void FlattenXml_RowsWithAttributesAndNestedElements()
        {
            const string xml = """
            <orders>
              <order id="1">
                <customer><name>Ada</name><city>London</city></customer>
                <total>19.95</total>
                <line>widget</line>
                <line>gadget</line>
              </order>
              <order id="2">
                <customer><name>Grace</name><city>New York</city></customer>
                <total>42</total>
              </order>
            </orders>
            """;

            var rows = new StructuredDataFlattener().FlattenXml(xml, "order");

            Assert.Equal(2, rows.Count);
            Assert.Equal("1", rows[0]["@id"]);
            Assert.Equal("Ada", rows[0]["customer.name"]);
            Assert.Equal("London", rows[0]["customer.city"]);
            Assert.Contains("widget", (string)rows[0]["line"]);
            Assert.Contains("gadget", (string)rows[0]["line"]);
        }

        [Fact]
        public void FlattenXml_InfersRowElementWhenNotSpecified()
        {
            const string xml = "<root><item><name>a</name></item><item><name>b</name></item><other>x</other></root>";
            var rows = new StructuredDataFlattener().FlattenXml(xml);

            Assert.Equal(2, rows.Count);
            Assert.Equal("a", rows[0]["name"]);
            Assert.Equal("b", rows[1]["name"]);
        }

        [Fact]
        public void InferSchema_ClassifiesTypesAndNullability()
        {
            var rows = new StructuredDataFlattener().FlattenJson(OrdersJson, "data.items");
            var schema = new StructuredDataFlattener().InferSchema(rows);

            var id = schema.Single(c => c.Name == "id");
            Assert.Equal("BIGINT", id.DataType);
            Assert.False(id.IsNullable);

            var total = schema.Single(c => c.Name == "total");
            Assert.Equal("FLOAT", total.DataType);

            var city = schema.Single(c => c.Name == "customer.address.city");
            Assert.Equal("VARCHAR", city.DataType);

            var notes = schema.Single(c => c.Name == "notes");
            Assert.True(notes.IsNullable);
        }

        [Fact]
        public void InferSchema_SparseColumnsAreNullable()
        {
            var rows = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>
            {
                new() { ["a"] = 1, ["b"] = "x" },
                new() { ["a"] = 2 }
            };

            var schema = new StructuredDataFlattener().InferSchema(rows);
            Assert.True(schema.Single(c => c.Name == "b").IsNullable);
            Assert.False(schema.Single(c => c.Name == "a").IsNullable);
        }

        [Fact]
        public void FlattenedJson_IntegratesWithProfilerAndPreview()
        {
            var rows = new StructuredDataFlattener().FlattenJson(OrdersJson, "data.items");
            var profile = new DataProfiler().Profile(rows);

            Assert.Equal(2, profile.RowCount);
            Assert.Contains(profile.Columns, c => c.Name == "customer.address.city");

            var preview = DataPreviewFormatter.ToTable(rows);
            Assert.Contains("Ada", preview);
        }
    }
}

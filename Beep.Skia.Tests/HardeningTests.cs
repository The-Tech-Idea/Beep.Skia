using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Beep.Skia;
using Beep.Skia.Assist;
using Beep.Skia.Collaboration;
using Beep.Skia.ERD;
using Beep.Skia.ETL;
using Beep.Skia.Extensions.Marketplace;
using Beep.Skia.Serialization;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Adversarial-input hardening: malformed, hostile, or extreme data must be rejected or
    /// handled gracefully (no crashes, no invalid state) at every external entry point.
    /// </summary>
    public class HardeningTests
    {
        // ── Diagram serialization ────────────────────────────────────────────

        [Fact]
        public void LoadFromDto_NullLists_DoesNotThrow()
        {
            var manager = new DrawingManager();
            manager.LoadFromDto(new DiagramDto { Components = null, Lines = null });
            Assert.Empty(manager.GetComponents());
        }

        [Fact]
        public void LoadFromDto_UnknownTypesAndBadGeometry_AreTolerated()
        {
            var dto = new DiagramDto();
            dto.Components.Add(new ComponentDto { Type = "No.Such.Type, Nope", X = 0, Y = 0, Width = 10, Height = 10 });
            dto.Components.Add(new ComponentDto { Type = null, X = 0, Y = 0, Width = 10, Height = 10 });
            dto.Components.Add(new ComponentDto
            {
                Type = "Beep.Skia.Components.ManualTriggerNode, Beep.Skia",
                X = float.NaN,
                Y = float.PositiveInfinity,
                Width = -5,
                Height = 0,
                Name = "extreme"
            });
            dto.Lines.Add(new LineDto { StartComponentIndex = 99, EndComponentIndex = -3 });
            dto.Lines.Add(new LineDto { StartComponentIndex = 0, EndComponentIndex = 0 });

            var manager = new DrawingManager();
            manager.LoadFromDto(dto);

            Assert.Single(manager.GetComponents());
            Assert.Equal(-5, manager.GetComponents()[0].Width);
        }

        [Fact]
        public void ToDto_WithExtremeGeometry_ProducesDeserializableJson()
        {
            var manager = new DrawingManager();
            manager.AddComponent(new Beep.Skia.Components.ManualTriggerNode
            {
                X = float.NaN,
                Y = float.MaxValue,
                Width = float.MinValue,
                Height = 0,
                Name = "extreme"
            });

            var json = JsonSerializer.Serialize(manager.ToDto());
            var roundTripped = JsonSerializer.Deserialize<DiagramDto>(json);

            Assert.NotNull(roundTripped);
        }

        // ── DDL importer ─────────────────────────────────────────────────────

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   \r\n\t  ")]
        [InlineData("not sql at all")]
        [InlineData("CREATE TABLE")]                          // truncated
        [InlineData("CREATE TABLE x (")]                      // unterminated
        [InlineData("CREATE TABLE x (a INT")]                 // unterminated, no paren close
        [InlineData("CREATE TABLE (a INT);")]                 // missing name
        [InlineData("CREATE TABLE x ();")]                    // no columns
        [InlineData("CREATE TABLE x (a INT); DROP TABLE x; -- trailing")]
        [InlineData("/* unterminated comment CREATE TABLE x (a INT);")]
        [InlineData("CREATE TABLE \"x\" (\"a\" INT, PRIMARY KEY (\"a\"));")]
        [InlineData("CREATE TABLE `weird``name` (a INT);")]
        [InlineData("CREATE TABLE x (a VARCHAR(999999999999999999999));")]
        public void DDLImporter_NeverThrowsOnMalformedInput(string ddl)
        {
            var importer = new DDLImporter();
            var tables = importer.Parse(ddl);
            Assert.NotNull(tables);
        }

        [Fact]
        public void DDLImporter_QuotedIdentifiersAndCommentsAreHandled()
        {
            var ddl = @"
                -- a comment
                /* block comment */
                CREATE TABLE ""Order Items"" (
                    ""id"" INT PRIMARY KEY,
                    ""qty"" INT NOT NULL DEFAULT 1,
                    CONSTRAINT ""fk_order"" FOREIGN KEY (""id"") REFERENCES ""Orders""(""id"") ON DELETE CASCADE
                );";

            var tables = new DDLImporter().Parse(ddl);

            Assert.Single(tables);
            Assert.Equal("Order Items", tables[0].TableName);
            Assert.True(tables[0].Columns.Count >= 2);
        }

        // ── Expression engine ────────────────────────────────────────────────

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("(")]
        [InlineData("1 + ")]
        [InlineData("unknown_func(1)")]
        [InlineData("1 / 0")]
        [InlineData("(((1))")]
        [InlineData("'unterminated string")]
        [InlineData("a.b.c.d.e")]
        public void ExpressionEngine_NeverThrowsOnMalformedExpressions(string expression)
        {
            var engine = new ExpressionEngine();
            var row = new Dictionary<string, object> { ["a"] = 1, ["b"] = 2 };

            try
            {
                var result = engine.Evaluate(expression, row);
                _ = result?.ToString();
            }
            catch (Beep.Skia.ETL.ExpressionException ex)
            {
                // ExpressionException is the documented domain error type for bad input.
                Assert.False(string.IsNullOrWhiteSpace(ex.Message));
            }
        }

        [Fact]
        public void ExpressionEngine_DeeplyNestedExpression_IsRejectedInsteadOfOverflowing()
        {
            var engine = new ExpressionEngine();
            var expression = new string('(', 5000) + "1" + new string(')', 5000);

            var ex = Assert.Throws<Beep.Skia.ETL.ExpressionException>(
                () => engine.Evaluate(expression, new Dictionary<string, object>()));
            Assert.Contains("nested too deeply", ex.Message);
        }

        [Fact]
        public void ExpressionEngine_NestingWithinLimit_StillEvaluates()
        {
            var engine = new ExpressionEngine();
            var expression = new string('(', 50) + "1 + 1" + new string(')', 50);

            Assert.Equal(2d, Convert.ToDouble(engine.Evaluate(expression, new Dictionary<string, object>())));
        }

        // ── DSL parsers ──────────────────────────────────────────────────────

        [Fact]
        public void MindMapDsl_DeeplyNestedOutline_IsBoundedByNodeLimit()
        {
            var builder = new System.Text.StringBuilder();
            for (var i = 0; i < 500; i++) builder.Append(new string(' ', i)).Append("level").Append(i).Append('\n');

            var root = MindMapDslParser.Parse(builder.ToString(), out var warnings, maxNodes: 50);

            Assert.NotNull(root);
            Assert.Contains(warnings, w => w.Contains("Node limit"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\n\n\n")]
        [InlineData("\t\t\t\t")]
        [InlineData("->")]
        [InlineData("A -> ")]
        [InlineData("-> -> ->")]
        [InlineData("A()")]
        [InlineData("(\"title\"")]
        [InlineData("A \"unterminated")]
        public void FlowchartDsl_NeverThrowsOnMalformedInput(string text)
        {
            var graph = FlowchartDslParser.Parse(text);
            Assert.NotNull(graph);
        }

        [Fact]
        public void FlowchartDsl_CyclicAndSelfReferencingGraph_IsHandled()
        {
            var graph = FlowchartDslParser.Parse("A -> A\nA -> B\nB -> A");

            Assert.Equal(2, graph.Nodes.Count);
            Assert.True(graph.Warnings.Count > 0);
        }

        // ── Collaboration + marketplace persistence ──────────────────────────

        [Theory]
        [InlineData("")]
        [InlineData("null")]
        [InlineData("{ not json }")]
        [InlineData("[1,2,3]")]
        [InlineData("{\"Users\":null,\"Shares\":null,\"Comments\":null}")]
        public void CollaborationSerializer_MalformedJson_DoesNotCorruptState(string json)
        {
            try
            {
                var service = CollaborationSerializer.FromJson(json);
                Assert.NotNull(service);
                Assert.Empty(service.GetComments("doc"));
            }
            catch (JsonException)
            {
                // Malformed JSON may surface as JsonException; that is acceptable and documented.
            }
        }

        [Fact]
        public void Marketplace_ReadManifest_OnCorruptArchive_ReturnsNull()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beep_corrupt_" + Guid.NewGuid().ToString("N") + ".beepkg");
            try
            {
                System.IO.File.WriteAllText(path, "this is not a zip archive");

                Assert.Null(ExtensionPackageBuilder.ReadManifest(path));

                var manager = new ExtensionPackageManager(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beep_corrupt_install_" + Guid.NewGuid().ToString("N")));
                var result = manager.Install(path);
                Assert.False(result.Success);
            }
            finally
            {
                try { System.IO.File.Delete(path); } catch { }
            }
        }

        // ── Schema comparison ────────────────────────────────────────────────

        [Fact]
        public void SchemaComparer_HandlesEmptyAndDuplicateTables()
        {
            var comparer = new SchemaComparer();
            var empty = new List<DDLImporter.TableInfo>();

            var result = comparer.Compare(empty, empty);
            Assert.NotNull(result);
            Assert.False(result.HasChanges);

            var duplicate = new List<DDLImporter.TableInfo>
            {
                new DDLImporter.TableInfo { TableName = "T" },
                new DDLImporter.TableInfo { TableName = "T" }
            };

            var duplicateResult = comparer.Compare(duplicate, empty);
            Assert.NotNull(duplicateResult);
        }
    }
}

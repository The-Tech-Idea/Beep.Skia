using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beep.Skia.Assist;
using Beep.Skia.Business;
using Beep.Skia.ETL;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Adversarial-input hardening for the recursive parsers: hostile nesting must be rejected
    /// or bounded, never crash the process with a stack overflow.
    /// </summary>
    public class RecursiveParserHardeningTests
    {
        // ── Expression engine: recursion beyond parentheses ──────────────────

        [Fact]
        public void ExpressionEngine_DeepUnaryChain_IsRejectedInsteadOfOverflowing()
        {
            var engine = new ExpressionEngine();
            var expression = new string('-', 20000) + "1";

            var ex = Assert.Throws<ExpressionException>(() => engine.Evaluate(expression, new Dictionary<string, object>()));
            Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        }

        [Fact]
        public void ExpressionEngine_DeepFunctionNesting_IsRejectedInsteadOfOverflowing()
        {
            var engine = new ExpressionEngine();
            var expression = string.Concat(Enumerable.Repeat("ABS(", 20000)) + "1" + new string(')', 20000);

            var ex = Assert.Throws<ExpressionException>(() => engine.Evaluate(expression, new Dictionary<string, object>()));
            Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        }

        [Fact]
        public void ExpressionEngine_LongFlatExpression_StillEvaluates()
        {
            var engine = new ExpressionEngine();
            var expression = string.Join("+", Enumerable.Repeat("1", 500));

            Assert.Equal(500d, Convert.ToDouble(engine.Evaluate(expression, new Dictionary<string, object>())));
        }

        [Fact]
        public void ExpressionEngine_HugeFlatExpression_IsRejectedInsteadOfOverflowing()
        {
            var engine = new ExpressionEngine();
            var expression = string.Join("+", Enumerable.Repeat("1", 20000));

            var ex = Assert.Throws<ExpressionException>(() => engine.Evaluate(expression, new Dictionary<string, object>()));
            Assert.Contains("too large", ex.Message);
        }

        // ── Structured data flattener ────────────────────────────────────────

        [Fact]
        public void FlattenJson_DeeplyNestedObject_IsBoundedByMaxDepth()
        {
            var depth = 5000;
            var json = new StringBuilder();
            for (var i = 0; i < depth; i++) json.Append("{\"a\":");
            json.Append("1");
            for (var i = 0; i < depth; i++) json.Append('}');

            var rows = new StructuredDataFlattener().FlattenJson(json.ToString(), options: new FlattenOptions { MaxDepth = 16 });

            Assert.NotNull(rows);
        }

        [Fact]
        public void FlattenJson_DeeplyNestedArray_IsBounded()
        {
            var depth = 3000;
            var json = new StringBuilder();
            for (var i = 0; i < depth; i++) json.Append("[");
            json.Append("1");
            for (var i = 0; i < depth; i++) json.Append("]");

            var rows = new StructuredDataFlattener().FlattenJson(json.ToString(), options: new FlattenOptions { MaxDepth = 8 });

            Assert.NotNull(rows);
        }

        [Fact]
        public void FlattenJson_HostileJson_ReturnsEmptyInsteadOfThrowing()
        {
            var flattener = new StructuredDataFlattener();

            foreach (var json in new[] { null, "", "   ", "{ not json }", "[1,2,3", "\"just a string\"", "{}" })
            {
                var rows = flattener.FlattenJson(json);
                Assert.NotNull(rows);
            }
        }

        [Fact]
        public void FlattenXml_DeeplyNestedElements_IsBounded()
        {
            var depth = 4000;
            var xml = new StringBuilder();
            for (var i = 0; i < depth; i++) xml.Append("<n>");
            xml.Append("1");
            for (var i = 0; i < depth; i++) xml.Append("</n>");

            var rows = new StructuredDataFlattener().FlattenXml(xml.ToString(), options: new FlattenOptions { MaxDepth = 8 });

            Assert.NotNull(rows);
        }

        [Fact]
        public void FlattenXml_HostileXml_ReturnsEmptyInsteadOfThrowing()
        {
            var flattener = new StructuredDataFlattener();

            foreach (var xml in new[] { null, "", "<", "<a>", "<a></b>", "<!DOCTYPE x [<!ENTITY e SYSTEM \"file:///etc/passwd\">]><x>&e;</x>", new string('<', 5000) })
            {
                var rows = flattener.FlattenXml(xml);
                Assert.NotNull(rows);
            }
        }

        // ── Mind-map layout recursion ────────────────────────────────────────

        [Fact]
        public void MindMapLayout_DeepOutline_DoesNotOverflowStack()
        {
            var depth = 2000;
            var builder = new StringBuilder();
            for (var i = 0; i < depth; i++) builder.Append(new string(' ', i * 2)).Append("n").Append(i).Append('\n');

            var root = MindMapDslParser.Parse(builder.ToString(), out var warnings, maxNodes: depth + 10);

            Assert.NotNull(root);
            Assert.NotNull(warnings);
        }

        // ── BPMN importer ────────────────────────────────────────────────────

        [Fact]
        public void BpmnImporter_HostileXml_DoesNotThrow()
        {
            var importer = new BpmnImporter();

            foreach (var xml in new[]
            {
                null,
                "",
                "<",
                "not xml",
                "<definitions>",
                "<definitions><process><task id=\"a\" name=\"A\"/></process>",
                "<!DOCTYPE d [<!ENTITY x \"y\">]><definitions>&x;</definitions>",
                "<definitions><process id=\"p\"><task id=\"a\"/><task id=\"a\"/></process></definitions>",
                "<definitions><process id=\"p\"><sequenceFlow id=\"f\" sourceRef=\"missing\" targetRef=\"alsoMissing\"/></process></definitions>"
            })
            {
                try
                {
                    var result = importer.Parse(xml);
                    Assert.NotNull(result);
                }
                catch (Exception ex) when (ex is System.Xml.XmlException or ArgumentException or InvalidOperationException)
                {
                    // XML domain errors are acceptable; process crashes are not.
                }
            }
        }

        [Fact]
        public void BpmnImporter_DeeplyNestedXml_IsRejectedInsteadOfOverflowing()
        {
            var depth = 20000;
            var xml = new StringBuilder("<definitions>");
            for (var i = 0; i < depth; i++) xml.Append("<n>");
            for (var i = 0; i < depth; i++) xml.Append("</n>");
            xml.Append("</definitions>");

            try
            {
                var result = new BpmnImporter().Parse(xml.ToString());
                Assert.NotNull(result);
            }
            catch (Exception ex) when (ex is System.Xml.XmlException or ArgumentException or InvalidOperationException)
            {
                // Rejection is fine.
            }
        }

        // ── LAS / well logs ──────────────────────────────────────────────────

        [Fact]
        public void LasFileParser_HostileInput_DoesNotThrow()
        {
            var parser = new Beep.Skia.WellLogs.LasFileParser();

            foreach (var text in new[]
            {
                null,
                "",
                "~V",
                "~VERSION INFORMATION\nVERS. 2.0\n~CURVE\nDEPT.M\n~A\n1 2 3\n",
                "~A\n" + string.Join("\n", Enumerable.Repeat("1 2 3 4 5", 5000)),
                "~A\n" + new string('x', 100000)
            })
            {
                try
                {
                    var result = parser.Parse(text);
                    Assert.NotNull(result);
                }
                catch (Exception ex) when (ex is FormatException or ArgumentException or InvalidOperationException)
                {
                    // Domain errors are acceptable.
                }
            }
        }
    }
}

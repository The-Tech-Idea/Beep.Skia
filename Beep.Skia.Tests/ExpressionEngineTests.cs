using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia.ETL;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for the ETL <see cref="ExpressionEngine"/>: parsing, precedence, functions,
    /// null handling, LIKE, and column resolution.
    /// </summary>
    public class ExpressionEngineTests
    {
        private static Dictionary<string, object> Row(params (string Key, object Value)[] values)
            => values.ToDictionary(v => v.Key, v => v.Value);

        [Fact]
        public void Arithmetic_RespectsPrecedenceAndParentheses()
        {
            var engine = new ExpressionEngine();
            Assert.Equal(7L, engine.Evaluate("1 + 2 * 3", null));
            Assert.Equal(9L, engine.Evaluate("(1 + 2) * 3", null));
            Assert.Equal(2L, engine.Evaluate("7 % 5", null));
        }

        [Fact]
        public void ColumnReferences_AreResolvedCaseInsensitively()
        {
            var engine = new ExpressionEngine();
            var row = Row(("amount", 21));

            Assert.Equal(42L, engine.Evaluate("[amount] * 2", row));
            Assert.Equal(42L, engine.Evaluate("AMOUNT * 2", row));
        }

        [Fact]
        public void StringConcatenation_WithPlus()
        {
            var engine = new ExpressionEngine();
            var row = Row(("first", "Ada"), ("last", "Lovelace"));

            Assert.Equal("Ada Lovelace", engine.Evaluate("first + ' ' + last", row));
        }

        [Fact]
        public void BooleanLogic_WithComparisons()
        {
            var engine = new ExpressionEngine();
            var row = Row(("amount", 150), ("status", "active"));

            Assert.True(engine.EvaluateBoolean("amount > 100 AND status = 'active'", row));
            Assert.False(engine.EvaluateBoolean("amount > 1000 OR status = 'closed'", row));
            Assert.True(engine.EvaluateBoolean("NOT (amount < 100)", row));
        }

        [Fact]
        public void Like_MatchesSqlWildcards()
        {
            var engine = new ExpressionEngine();
            var row = Row(("name", "Alice"));

            Assert.True(engine.EvaluateBoolean("name LIKE 'A%'", row));
            Assert.True(engine.EvaluateBoolean("name LIKE '_lice'", row));
            Assert.False(engine.EvaluateBoolean("name LIKE 'B%'", row));
            Assert.True(engine.EvaluateBoolean("name NOT LIKE 'B%'", row));
        }

        [Fact]
        public void IsNull_And_IsNotNull()
        {
            var engine = new ExpressionEngine();
            var row = Row(("email", null), ("name", "Ada"));

            Assert.True(engine.EvaluateBoolean("email IS NULL", row));
            Assert.False(engine.EvaluateBoolean("email IS NOT NULL", row));
            Assert.True(engine.EvaluateBoolean("name IS NOT NULL", row));
        }

        [Fact]
        public void Functions_WorkAsExpected()
        {
            var engine = new ExpressionEngine();

            Assert.Equal("ADA", engine.Evaluate("UPPER('ada')", null));
            Assert.Equal("ada", engine.Evaluate("LOWER('ADA')", null));
            Assert.Equal("Ada Lovelace", engine.Evaluate("CONCAT('Ada', ' ', 'Lovelace')", null));
            Assert.Equal("ell", engine.Evaluate("SUBSTRING('hello', 2, 3)", null));
            Assert.Equal(3, engine.Evaluate("LEN('abc')", null));
            Assert.Equal(3.14, Convert.ToDouble(engine.Evaluate("ROUND(3.14159, 2)", null)), 2);
            Assert.Equal("fallback", engine.Evaluate("COALESCE(null, 'fallback')", null));
            Assert.Equal("a", engine.Evaluate("IIF(1 = 1, 'a', 'b')", null));
            Assert.Equal("b", engine.Evaluate("IIF(1 = 2, 'a', 'b')", null));
            Assert.Null(engine.Evaluate("NULLIF(5, 5)", null));
            Assert.Equal(5L, engine.Evaluate("NULLIF(5, 6)", null));
        }

        [Fact]
        public void NullPropagation_AndBooleanCoercion()
        {
            var engine = new ExpressionEngine();

            Assert.Null(engine.Evaluate("null + 1", null));
            Assert.Null(engine.Evaluate("missing + 1", null));
            Assert.False(engine.EvaluateBoolean("null", null));
            Assert.False(engine.EvaluateBoolean("0", null));
            Assert.True(engine.EvaluateBoolean("1", null));
        }

        [Fact]
        public void GetReferencedColumns_ExcludesFunctionsAndKeywords()
        {
            var engine = new ExpressionEngine();
            var columns = engine.GetReferencedColumns("amount > 100 AND UPPER(status) = 'ACTIVE'");

            Assert.Contains("amount", columns);
            Assert.Contains("status", columns);
            Assert.DoesNotContain("UPPER", columns);
            Assert.Equal(2, columns.Count);
        }

        [Fact]
        public void DerivedColumn_AndSplitCondition_ExpressionsEvaluate()
        {
            var engine = new ExpressionEngine();
            var row = Row(("price", 100.0), ("quantity", 3));

            var derived = new Beep.Skia.Model.DerivedColumnDefinition
            {
                Name = "total",
                Expression = "price * quantity"
            };
            Assert.Equal(300.0, Convert.ToDouble(engine.Evaluate(derived.Expression, row)), 2);

            var split = new Beep.Skia.Model.SplitCondition
            {
                Name = "large",
                Expression = "price * quantity >= 250"
            };
            Assert.True(engine.EvaluateBoolean(split.Expression, row));
        }

        [Fact]
        public void Errors_ThrowExpressionException()
        {
            var engine = new ExpressionEngine();

            Assert.Throws<ExpressionException>(() => engine.Evaluate("(1 + 2", null));
            Assert.Throws<ExpressionException>(() => engine.Evaluate("1 / 0", null));
            Assert.Throws<ExpressionException>(() => engine.Evaluate("UNKNOWN_FN(1)", null));
            Assert.Throws<ExpressionException>(() => engine.Evaluate("", null));
        }
    }
}

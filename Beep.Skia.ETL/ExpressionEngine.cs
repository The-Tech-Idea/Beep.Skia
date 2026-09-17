using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// Raised when an expression cannot be parsed or evaluated.
    /// </summary>
    public class ExpressionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the ExpressionException class.
        /// </summary>
        public ExpressionException(string message) : base(message) { }
    }

    /// <summary>
    /// Small SQL-like expression engine for ETL derived columns, conditional splits,
    /// filters, and custom data-quality rules.
    ///
    /// Supports: literals (number/string/TRUE/FALSE/NULL), column references
    /// ([Column], "Column", `Column`, or bare identifiers), arithmetic (+ - * / %),
    /// comparisons (= == != &lt;&gt; &lt; &lt;= &gt; &gt;=), logical operators (AND OR NOT),
    /// IS NULL / IS NOT NULL, LIKE, and a built-in function library.
    /// Unknown columns evaluate to null.
    /// </summary>
    public class ExpressionEngine
    {
        private enum TokenType { Number, String, Identifier, Operator, LParen, RParen, Comma, End }

        private sealed class Token
        {
            public TokenType Type;
            public string Text;
            public int Position;
            /// <summary>
            /// Gets or sets the to string.
            /// </summary>
            public override string ToString() => $"{Type}:{Text}";
        }

        private abstract class Node { }
        private sealed class LiteralNode : Node { public object Value; }
        private sealed class IdentifierNode : Node { public string Name; }
        private sealed class UnaryNode : Node { public string Operator; public Node Operand; }
        private sealed class BinaryNode : Node { public string Operator; public Node Left; public Node Right; }
        private sealed class FunctionNode : Node { public string Name; public List<Node> Arguments = new List<Node>(); }
        private sealed class IsNullNode : Node { public Node Operand; public bool Negated; }
        private sealed class LikeNode : Node { public Node Operand; public Node Pattern; public bool Negated; }

        private static readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AND", "OR", "NOT", "TRUE", "FALSE", "NULL", "IS", "LIKE"
        };

        private static readonly Dictionary<string, Func<object[], object>> FunctionLibrary =
            new Dictionary<string, Func<object[], object>>(StringComparer.OrdinalIgnoreCase)
            {
                ["UPPER"] = args => ToStr(args.ElementAtOrDefault(0))?.ToUpperInvariant(),
                ["LOWER"] = args => ToStr(args.ElementAtOrDefault(0))?.ToLowerInvariant(),
                ["TRIM"] = args => ToStr(args.ElementAtOrDefault(0))?.Trim(),
                ["LEN"] = args => ToStr(args.ElementAtOrDefault(0))?.Length ?? 0,
                ["LENGTH"] = args => ToStr(args.ElementAtOrDefault(0))?.Length ?? 0,
                ["CONCAT"] = args => string.Concat(args.Select(ToStr)),
                ["SUBSTRING"] = args =>
                {
                    var text = ToStr(args.ElementAtOrDefault(0));
                    if (text == null) return null;
                    int start = (int)ToDouble(args.ElementAtOrDefault(1));
                    int index = Math.Max(0, start - 1); // SQL-style 1-based index
                    if (index >= text.Length) return string.Empty;
                    if (args.Length >= 3)
                    {
                        int length = (int)ToDouble(args[2]);
                        if (length <= 0) return string.Empty;
                        return text.Substring(index, Math.Min(length, text.Length - index));
                    }
                    return text.Substring(index);
                },
                ["REPLACE"] = args => ToStr(args.ElementAtOrDefault(0))?.Replace(
                    ToStr(args.ElementAtOrDefault(1)) ?? string.Empty,
                    ToStr(args.ElementAtOrDefault(2)) ?? string.Empty),
                ["LEFT"] = args =>
                {
                    var text = ToStr(args.ElementAtOrDefault(0));
                    if (text == null) return null;
                    int n = (int)ToDouble(args.ElementAtOrDefault(1));
                    return n <= 0 ? string.Empty : text.Substring(0, Math.Min(n, text.Length));
                },
                ["RIGHT"] = args =>
                {
                    var text = ToStr(args.ElementAtOrDefault(0));
                    if (text == null) return null;
                    int n = (int)ToDouble(args.ElementAtOrDefault(1));
                    return n <= 0 ? string.Empty : text.Substring(Math.Max(0, text.Length - n));
                },
                ["ROUND"] = args =>
                {
                    if (IsNull(args.ElementAtOrDefault(0))) return null;
                    int digits = args.Length >= 2 ? (int)ToDouble(args[1]) : 0;
                    return Math.Round(ToDouble(args[0]), digits, MidpointRounding.AwayFromZero);
                },
                ["ABS"] = args => IsNull(args.ElementAtOrDefault(0)) ? (object)null : Math.Abs(ToDouble(args[0])),
                ["FLOOR"] = args => IsNull(args.ElementAtOrDefault(0)) ? (object)null : Math.Floor(ToDouble(args[0])),
                ["CEILING"] = args => IsNull(args.ElementAtOrDefault(0)) ? (object)null : Math.Ceiling(ToDouble(args[0])),
                ["COALESCE"] = args => args.FirstOrDefault(a => !IsNull(a)),
                ["ISNULL"] = args => IsNull(args.ElementAtOrDefault(0)) ? args.ElementAtOrDefault(1) : args.ElementAtOrDefault(0),
                ["NULLIF"] = args => AreEqual(args.ElementAtOrDefault(0), args.ElementAtOrDefault(1)) ? null : args.ElementAtOrDefault(0),
                ["IIF"] = args => ToBool(args.ElementAtOrDefault(0)) ? args.ElementAtOrDefault(1) : args.ElementAtOrDefault(2),
                ["YEAR"] = args => ToDate(args.ElementAtOrDefault(0))?.Year,
                ["MONTH"] = args => ToDate(args.ElementAtOrDefault(0))?.Month,
                ["DAY"] = args => ToDate(args.ElementAtOrDefault(0))?.Day,
                ["NOW"] = _ => DateTime.UtcNow
            };

        /// <summary>
        /// Gets the names of all built-in functions.
        /// </summary>
        public static IReadOnlyCollection<string> Functions => FunctionLibrary.Keys.ToList().AsReadOnly();

        /// <summary>
        /// Evaluates an expression against a row. Returns null when the result is NULL.
        /// </summary>
        public object Evaluate(string expression, IDictionary<string, object> row)
        {
            var ast = Parse(expression);
            return EvaluateNode(ast, row ?? new Dictionary<string, object>());
        }

        /// <summary>
        /// Evaluates an expression as a boolean (null/0/empty/"false" are false).
        /// </summary>
        public bool EvaluateBoolean(string expression, IDictionary<string, object> row)
            => ToBool(Evaluate(expression, row));

        /// <summary>
        /// Returns the column names referenced by the expression (excluding function names).
        /// </summary>
        public IReadOnlyCollection<string> GetReferencedColumns(string expression)
        {
            var ast = Parse(expression);
            var columns = new List<string>();
            CollectColumns(ast, columns);
            return columns.Distinct(StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        // ── Parsing ──────────────────────────────────────────────────────────

        private static Node Parse(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new ExpressionException("Expression is empty.");

            var tokens = Tokenize(expression);
            var parser = new Parser(tokens);
            var node = parser.ParseExpression();
            parser.ExpectEnd();
            return node;
        }

        private static List<Token> Tokenize(string input)
        {
            var tokens = new List<Token>();
            int i = 0;
            while (i < input.Length)
            {
                char c = input[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }

                int start = i;

                if (c == '(') { tokens.Add(new Token { Type = TokenType.LParen, Text = "(", Position = i }); i++; continue; }
                if (c == ')') { tokens.Add(new Token { Type = TokenType.RParen, Text = ")", Position = i }); i++; continue; }
                if (c == ',') { tokens.Add(new Token { Type = TokenType.Comma, Text = ",", Position = i }); i++; continue; }

                // String literal
                if (c == '\'')
                {
                    i++;
                    var sb = new StringBuilder();
                    while (i < input.Length)
                    {
                        if (input[i] == '\'')
                        {
                            if (i + 1 < input.Length && input[i + 1] == '\'') { sb.Append('\''); i += 2; continue; }
                            break;
                        }
                        sb.Append(input[i++]);
                    }
                    if (i >= input.Length) throw new ExpressionException($"Unterminated string literal at position {start}.");
                    i++; // closing quote
                    tokens.Add(new Token { Type = TokenType.String, Text = sb.ToString(), Position = start });
                    continue;
                }

                // Quoted identifiers
                if (c == '[' || c == '"' || c == '`')
                {
                    char close = c == '[' ? ']' : c;
                    i++;
                    var sb = new StringBuilder();
                    while (i < input.Length && input[i] != close) sb.Append(input[i++]);
                    if (i >= input.Length) throw new ExpressionException($"Unterminated identifier at position {start}.");
                    i++;
                    tokens.Add(new Token { Type = TokenType.Identifier, Text = sb.ToString(), Position = start });
                    continue;
                }

                // Numbers
                if (char.IsDigit(c) || (c == '.' && i + 1 < input.Length && char.IsDigit(input[i + 1])))
                {
                    while (i < input.Length && (char.IsDigit(input[i]) || input[i] == '.')) i++;
                    tokens.Add(new Token { Type = TokenType.Number, Text = input.Substring(start, i - start), Position = start });
                    continue;
                }

                // Identifiers / keywords
                if (char.IsLetter(c) || c == '_')
                {
                    while (i < input.Length && (char.IsLetterOrDigit(input[i]) || input[i] == '_' || input[i] == '.')) i++;
                    tokens.Add(new Token { Type = TokenType.Identifier, Text = input.Substring(start, i - start), Position = start });
                    continue;
                }

                // Operators
                string two = i + 1 < input.Length ? input.Substring(i, 2) : string.Empty;
                if (two == "!=" || two == "<>" || two == "<=" || two == ">=" || two == "==")
                {
                    tokens.Add(new Token { Type = TokenType.Operator, Text = two, Position = i });
                    i += 2;
                    continue;
                }

                if ("+-*/%=<>".IndexOf(c) >= 0)
                {
                    tokens.Add(new Token { Type = TokenType.Operator, Text = c.ToString(), Position = i });
                    i++;
                    continue;
                }

                throw new ExpressionException($"Unexpected character '{c}' at position {i}.");
            }

            tokens.Add(new Token { Type = TokenType.End, Text = string.Empty, Position = input.Length });
            return tokens;
        }

        private sealed class Parser
        {
            /// <summary>Maximum expression nesting depth; deeper input is rejected instead of overflowing the stack.</summary>
            private const int MaxDepth = 100;

            /// <summary>Maximum number of operator nodes; bounds recursive evaluation depth.</summary>
            private const int MaxNodes = 1000;

            private readonly List<Token> _tokens;
            private int _index;
            private int _depth;
            private int _nodeCount;
            /// <summary>
            /// Initializes a new instance of the Parser class.
            /// </summary>
            public Parser(List<Token> tokens) { _tokens = tokens; }

            private Token Current => _tokens[_index];
            private Token Next() => _tokens[_index++];

            /// <summary>
            /// Gets or sets the expect end.
            /// </summary>
            public void ExpectEnd()
            {
                if (Current.Type != TokenType.End)
                    throw new ExpressionException($"Unexpected token '{Current.Text}' at position {Current.Position}.");
            }

            /// <summary>
            /// Gets or sets the parse expression.
            /// </summary>
            public Node ParseExpression() => ParseOr();

            private Node ParseOr()
            {
                var left = ParseAnd();
                while (Current.Type == TokenType.Identifier && IsKeyword(Current.Text, "OR"))
                {
                    Next();
                    var right = ParseAnd();
                    CountNode();
                    left = new BinaryNode { Operator = "OR", Left = left, Right = right };
                }
                return left;
            }

            private Node ParseAnd()
            {
                var left = ParseNot();
                while (Current.Type == TokenType.Identifier && IsKeyword(Current.Text, "AND"))
                {
                    Next();
                    var right = ParseNot();
                    CountNode();
                    left = new BinaryNode { Operator = "AND", Left = left, Right = right };
                }
                return left;
            }

            private Node ParseNot()
            {
                if (Current.Type == TokenType.Identifier && IsKeyword(Current.Text, "NOT"))
                {
                    Next();
                    return new UnaryNode { Operator = "NOT", Operand = ParseNot() };
                }
                return ParseComparison();
            }

            private Node ParseComparison()
            {
                var left = ParseAdditive();

                if (Current.Type == TokenType.Identifier && IsKeyword(Current.Text, "IS"))
                {
                    Next();
                    bool negated = false;
                    if (Current.Type == TokenType.Identifier && IsKeyword(Current.Text, "NOT")) { negated = true; Next(); }
                    if (Current.Type != TokenType.Identifier || !IsKeyword(Current.Text, "NULL"))
                        throw new ExpressionException($"Expected NULL after IS at position {Current.Position}.");
                    Next();
                    return new IsNullNode { Operand = left, Negated = negated };
                }

                if (Current.Type == TokenType.Identifier && IsKeyword(Current.Text, "LIKE"))
                {
                    Next();
                    var pattern = ParseAdditive();
                    bool negated = false;
                    if (Current.Type == TokenType.Identifier && IsKeyword(Current.Text, "NOT")) { negated = true; Next(); }
                    return new LikeNode { Operand = left, Pattern = pattern, Negated = negated };
                }

                if (Current.Type == TokenType.Identifier && IsKeyword(Current.Text, "NOT")
                    && _index + 1 < _tokens.Count && _tokens[_index + 1].Type == TokenType.Identifier && IsKeyword(_tokens[_index + 1].Text, "LIKE"))
                {
                    Next(); Next();
                    var pattern = ParseAdditive();
                    return new LikeNode { Operand = left, Pattern = pattern, Negated = true };
                }

                if (Current.Type == TokenType.Operator && IsComparisonOperator(Current.Text))
                {
                    var op = Next().Text;
                    var right = ParseAdditive();
                    CountNode();
                    return new BinaryNode { Operator = op, Left = left, Right = right };
                }

                return left;
            }

            private Node ParseAdditive()
            {
                var left = ParseMultiplicative();
                while (Current.Type == TokenType.Operator && (Current.Text == "+" || Current.Text == "-"))
                {
                    var op = Next().Text;
                    var right = ParseMultiplicative();
                    CountNode();
                    left = new BinaryNode { Operator = op, Left = left, Right = right };
                }
                return left;
            }

            private Node ParseMultiplicative()
            {
                var left = ParseUnary();
                while (Current.Type == TokenType.Operator && (Current.Text == "*" || Current.Text == "/" || Current.Text == "%"))
                {
                    var op = Next().Text;
                    var right = ParseUnary();
                    CountNode();
                    left = new BinaryNode { Operator = op, Left = left, Right = right };
                }
                return left;
            }

            private Node ParseUnary()
            {
                if (Current.Type == TokenType.Operator && (Current.Text == "-" || Current.Text == "+"))
                {
                    EnterNesting();
                    try
                    {
                        var op = Next().Text;
                        return new UnaryNode { Operator = op, Operand = ParseUnary() };
                    }
                    finally
                    {
                        _depth--;
                    }
                }
                return ParsePrimary();
            }

            /// <summary>Increments the nesting depth, rejecting input that would overflow the stack.</summary>
            private void EnterNesting()
            {
                if (++_depth > MaxDepth)
                {
                    _depth--;
                    throw new ExpressionException($"Expression is nested too deeply (limit {MaxDepth}).");
                }
            }

            /// <summary>
            /// Counts operator nodes. Evaluation walks the tree recursively, so a very long flat
            /// chain (e.g. thousands of "+" terms) must be rejected rather than overflowing the stack.
            /// </summary>
            private void CountNode()
            {
                if (++_nodeCount > MaxNodes)
                    throw new ExpressionException($"Expression is too large (limit {MaxNodes} operations).");
            }

            private Node ParsePrimary()
            {
                var token = Current;

                if (token.Type == TokenType.Number)
                {
                    Next();
                    return new LiteralNode { Value = ParseNumber(token.Text) };
                }

                if (token.Type == TokenType.String)
                {
                    Next();
                    return new LiteralNode { Value = token.Text };
                }

                if (token.Type == TokenType.LParen)
                {
                    EnterNesting();
                    try
                    {
                        Next();
                        var inner = ParseExpression();
                        if (Current.Type != TokenType.RParen)
                            throw new ExpressionException($"Expected ')' at position {Current.Position}.");
                        Next();
                        return inner;
                    }
                    finally
                    {
                        _depth--;
                    }
                }

                if (token.Type == TokenType.Identifier)
                {
                    Next();

                    if (IsKeyword(token.Text, "TRUE")) return new LiteralNode { Value = true };
                    if (IsKeyword(token.Text, "FALSE")) return new LiteralNode { Value = false };
                    if (IsKeyword(token.Text, "NULL")) return new LiteralNode { Value = null };

                    if (Current.Type == TokenType.LParen)
                    {
                        Next();
                        var function = new FunctionNode { Name = token.Text };
                        EnterNesting();
                        try
                        {
                            if (Current.Type != TokenType.RParen)
                            {
                                function.Arguments.Add(ParseExpression());
                                while (Current.Type == TokenType.Comma)
                                {
                                    Next();
                                    function.Arguments.Add(ParseExpression());
                                }
                            }
                        }
                        finally
                        {
                            _depth--;
                        }
                        if (Current.Type != TokenType.RParen)
                            throw new ExpressionException($"Expected ')' after function arguments at position {Current.Position}.");
                        Next();
                        return function;
                    }

                    return new IdentifierNode { Name = token.Text };
                }

                throw new ExpressionException($"Unexpected token '{token.Text}' at position {token.Position}.");
            }

            private static bool IsKeyword(string text, string keyword)
                => string.Equals(text, keyword, StringComparison.OrdinalIgnoreCase);

            private static bool IsComparisonOperator(string op)
                => op == "=" || op == "==" || op == "!=" || op == "<>" || op == "<" || op == "<=" || op == ">" || op == ">=";
        }

        private static object ParseNumber(string text)
        {
            if (text.IndexOf('.') >= 0)
            {
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return d;
            }
            else if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
            {
                return l;
            }
            throw new ExpressionException($"Invalid number '{text}'.");
        }

        // ── Evaluation ───────────────────────────────────────────────────────

        private static object EvaluateNode(Node node, IDictionary<string, object> row)
        {
            switch (node)
            {
                case LiteralNode literal:
                    return literal.Value;

                case IdentifierNode identifier:
                    return ResolveColumn(identifier.Name, row);

                case UnaryNode unary:
                    return EvaluateUnary(unary, row);

                case BinaryNode binary:
                    return EvaluateBinary(binary, row);

                case IsNullNode isNull:
                    return isNull.Negated ? !IsNull(EvaluateNode(isNull.Operand, row)) : IsNull(EvaluateNode(isNull.Operand, row));

                case LikeNode like:
                    return EvaluateLike(like, row);

                case FunctionNode function:
                    return EvaluateFunction(function, row);

                default:
                    throw new ExpressionException("Unsupported expression node.");
            }
        }

        private static object EvaluateUnary(UnaryNode unary, IDictionary<string, object> row)
        {
            var value = EvaluateNode(unary.Operand, row);
            switch (unary.Operator)
            {
                case "NOT": return !ToBool(value);
                case "-": return IsNull(value) ? (object)null : -ToDouble(value);
                case "+": return value;
                default: throw new ExpressionException($"Unsupported unary operator '{unary.Operator}'.");
            }
        }

        private static object EvaluateBinary(BinaryNode binary, IDictionary<string, object> row)
        {
            switch (binary.Operator)
            {
                case "AND":
                    return ToBool(EvaluateNode(binary.Left, row)) && ToBool(EvaluateNode(binary.Right, row));
                case "OR":
                    return ToBool(EvaluateNode(binary.Left, row)) || ToBool(EvaluateNode(binary.Right, row));
            }

            var left = EvaluateNode(binary.Left, row);
            var right = EvaluateNode(binary.Right, row);

            switch (binary.Operator)
            {
                case "+": return Add(left, right);
                case "-": return Numeric(left, right, (a, b) => a - b);
                case "*": return Numeric(left, right, (a, b) => a * b);
                case "/":
                    if (IsNull(left) || IsNull(right)) return null;
                    var divisor = ToDouble(right);
                    if (Math.Abs(divisor) < double.Epsilon) throw new ExpressionException("Division by zero.");
                    return ToDouble(left) / divisor;
                case "%":
                    if (IsNull(left) || IsNull(right)) return null;
                    var modulo = ToDouble(right);
                    if (Math.Abs(modulo) < double.Epsilon) throw new ExpressionException("Division by zero.");
                    return Numeric(left, right, (a, b) => a % b);
                case "=":
                case "==": return AreEqual(left, right);
                case "!=":
                case "<>": return !AreEqual(left, right);
                case "<": return Compare(left, right) < 0;
                case "<=": return Compare(left, right) <= 0;
                case ">": return Compare(left, right) > 0;
                case ">=": return Compare(left, right) >= 0;
                default: throw new ExpressionException($"Unsupported operator '{binary.Operator}'.");
            }
        }

        private static object EvaluateFunction(FunctionNode function, IDictionary<string, object> row)
        {
            if (!FunctionLibrary.TryGetValue(function.Name, out var handler))
                throw new ExpressionException($"Unknown function '{function.Name}'.");

            var args = function.Arguments.Select(a => EvaluateNode(a, row)).ToArray();
            return handler(args);
        }

        private static object EvaluateLike(LikeNode like, IDictionary<string, object> row)
        {
            var value = ToStr(EvaluateNode(like.Operand, row));
            var pattern = ToStr(EvaluateNode(like.Pattern, row));
            if (value == null || pattern == null) return false;

            bool match = LikeMatch(value, pattern);
            return like.Negated ? !match : match;
        }

        private static bool LikeMatch(string value, string pattern)
        {
            // Translate SQL wildcards to a regex.
            var sb = new StringBuilder("^");
            foreach (var c in pattern)
            {
                switch (c)
                {
                    case '%': sb.Append(".*"); break;
                    case '_': sb.Append('.'); break;
                    default: sb.Append(System.Text.RegularExpressions.Regex.Escape(c.ToString())); break;
                }
            }
            sb.Append('$');
            return System.Text.RegularExpressions.Regex.IsMatch(value, sb.ToString(), System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        private static object ResolveColumn(string name, IDictionary<string, object> row)
        {
            if (row == null) return null;
            if (row.TryGetValue(name, out var value)) return value;

            foreach (var kvp in row)
            {
                if (string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase)) return kvp.Value;
            }
            return null;
        }

        // ── Value coercion helpers ───────────────────────────────────────────

        private static object Add(object left, object right)
        {
            if (left is string || right is string)
                return (ToStr(left) ?? string.Empty) + (ToStr(right) ?? string.Empty);
            return Numeric(left, right, (a, b) => a + b);
        }

        private static object Numeric(object left, object right, Func<double, double, double> op)
        {
            if (IsNull(left) || IsNull(right)) return null;
            var a = ToDouble(left);
            var b = ToDouble(right);
            var result = op(a, b);

            // Preserve integers when both inputs and the result are integral.
            if (IsIntegral(left) && IsIntegral(right) && Math.Abs(result % 1) < double.Epsilon)
                return (long)result;
            return result;
        }

        private static bool AreEqual(object left, object right)
        {
            if (IsNull(left) && IsNull(right)) return true;
            if (IsNull(left) || IsNull(right)) return false;

            if (IsNumeric(left) && IsNumeric(right))
                return Math.Abs(ToDouble(left) - ToDouble(right)) < 1e-9;

            if (left is bool || right is bool)
                return ToBool(left) == ToBool(right);

            return string.Equals(ToStr(left), ToStr(right), StringComparison.Ordinal);
        }

        private static int Compare(object left, object right)
        {
            if (IsNull(left) && IsNull(right)) return 0;
            if (IsNull(left)) return -1;
            if (IsNull(right)) return 1;

            if (IsNumeric(left) && IsNumeric(right))
                return ToDouble(left).CompareTo(ToDouble(right));

            if (left is DateTime || right is DateTime)
            {
                var ld = ToDate(left) ?? DateTime.MinValue;
                var rd = ToDate(right) ?? DateTime.MinValue;
                return ld.CompareTo(rd);
            }

            return string.Compare(ToStr(left), ToStr(right), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNull(object value) => value == null || value is DBNull;

        private static bool IsNumeric(object value)
            => value is byte || value is sbyte || value is short || value is ushort
               || value is int || value is uint || value is long || value is ulong
               || value is float || value is double || value is decimal;

        private static bool IsIntegral(object value)
            => value is byte || value is sbyte || value is short || value is ushort
               || value is int || value is uint || value is long || value is ulong;

        private static double ToDouble(object value)
        {
            if (IsNull(value)) return 0d;
            if (value is double d) return d;
            if (value is float f) return f;
            if (value is decimal m) return (double)m;
            if (value is bool b) return b ? 1d : 0d;
            if (value is string s)
            {
                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)) return parsed;
                return 0d;
            }
            try { return Convert.ToDouble(value, CultureInfo.InvariantCulture); }
            catch { return 0d; }
        }

        private static string ToStr(object value)
        {
            if (IsNull(value)) return null;
            if (value is DateTime date) return date.ToString("O", CultureInfo.InvariantCulture);
            if (value is bool flag) return flag ? "true" : "false";
            if (value is double d) return d.ToString(CultureInfo.InvariantCulture);
            if (value is float f) return f.ToString(CultureInfo.InvariantCulture);
            if (value is decimal m) return m.ToString(CultureInfo.InvariantCulture);
            return value.ToString();
        }

        private static bool ToBool(object value)
        {
            if (IsNull(value)) return false;
            if (value is bool b) return b;
            if (IsNumeric(value)) return Math.Abs(ToDouble(value)) > double.Epsilon;
            if (value is string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return false;
                if (bool.TryParse(s, out var parsed)) return parsed;
                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)) return Math.Abs(number) > double.Epsilon;
                return true;
            }
            return true;
        }

        private static DateTime? ToDate(object value)
        {
            if (IsNull(value)) return null;
            if (value is DateTime date) return date;
            if (value is DateTimeOffset offset) return offset.DateTime;
            if (value is string s && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return parsed;
            return null;
        }

        private static void CollectColumns(Node node, List<string> columns)
        {
            switch (node)
            {
                case IdentifierNode identifier:
                    if (!Keywords.Contains(identifier.Name)) columns.Add(identifier.Name);
                    break;
                case UnaryNode unary:
                    CollectColumns(unary.Operand, columns);
                    break;
                case BinaryNode binary:
                    CollectColumns(binary.Left, columns);
                    CollectColumns(binary.Right, columns);
                    break;
                case IsNullNode isNull:
                    CollectColumns(isNull.Operand, columns);
                    break;
                case LikeNode like:
                    CollectColumns(like.Operand, columns);
                    CollectColumns(like.Pattern, columns);
                    break;
                case FunctionNode function:
                    foreach (var argument in function.Arguments) CollectColumns(argument, columns);
                    break;
            }
        }
    }
}

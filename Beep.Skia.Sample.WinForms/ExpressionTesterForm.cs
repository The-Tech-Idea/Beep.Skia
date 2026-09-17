using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Beep.Skia.ETL;

namespace Beep.Skia.Sample.WinForms
{
    /// <summary>
    /// Expression builder/tester for the ETL expression engine: compose expressions from
    /// columns, operators, and function snippets, then evaluate against sample row values.
    /// </summary>
    public class ExpressionTesterForm : Form
    {
        private TextBox _expression = null!;
        private TextBox _variables = null!;
        private Label _result = null!;
        private ListBox _columns = null!;
        private ListBox _functions = null!;

        private static readonly (string Label, string Snippet)[] FunctionSnippets =
        {
            ("UPPER()", "UPPER()"),
            ("LOWER()", "LOWER()"),
            ("TRIM()", "TRIM()"),
            ("LEN()", "LEN()"),
            ("CONCAT()", "CONCAT()"),
            ("SUBSTRING(,,)", "SUBSTRING(, 1, 3)"),
            ("REPLACE(,,)", "REPLACE(, '', '')"),
            ("LEFT(,)", "LEFT(, 3)"),
            ("RIGHT(,)", "RIGHT(, 3)"),
            ("ROUND(,)", "ROUND(, 2)"),
            ("ABS()", "ABS()"),
            ("COALESCE(,)", "COALESCE(, '')"),
            ("ISNULL(,)", "ISNULL(, '')"),
            ("NULLIF(,)", "NULLIF(, '')"),
            ("IIF(,,)", "IIF(, 'yes', 'no')"),
            ("YEAR()", "YEAR()"),
            ("MONTH()", "MONTH()"),
            ("DAY()", "DAY()")
        };

        public ExpressionTesterForm()
        {
            BuildUi();
            RefreshColumns();
            Evaluate();
        }

        private void BuildUi()
        {
            Text = "Expression Builder";
            Width = 940;
            Height = 640;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;

            var expressionLabel = new Label { Text = "Expression:", Left = 10, Top = 12, Width = 80 };
            _expression = new TextBox
            {
                Left = 95,
                Top = 9,
                Width = 815,
                Font = new System.Drawing.Font("Consolas", 10f),
                Text = "UPPER([name]) + ' (' + [qty] + ')'"
            };

            var operators = new FlowLayoutPanel
            {
                Left = 95,
                Top = 38,
                Width = 815,
                Height = 32,
                WrapContents = false,
                AutoScroll = true
            };
            foreach (var op in new[] { "+", "-", "*", "/", "%", "=", "<>", "<", "<=", ">", ">=", "AND", "OR", "NOT", "LIKE '%'" })
            {
                var button = new Button { Text = op, Width = op.Length > 2 ? 64 : 34, Height = 26, Margin = new Padding(1) };
                button.Click += (s, e) => InsertAtCaret(_expression, " " + op + " ");
                operators.Controls.Add(button);
            }

            var variablesLabel = new Label { Text = "Row values (key=value per line):", Left = 10, Top = 76, Width = 300 };
            _variables = new TextBox
            {
                Left = 10,
                Top = 98,
                Width = 460,
                Height = 300,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new System.Drawing.Font("Consolas", 10f),
                Text = "name=widget\r\nqty=3\r\nprice=19.95\r\nstatus=active\r\nemail="
            };
            _variables.TextChanged += (s, e) => RefreshColumns();

            var columnsGroup = new GroupBox { Text = "Columns (double-click to insert)", Left = 480, Top = 76, Width = 200, Height = 160 };
            _columns = new ListBox { Dock = DockStyle.Fill, Font = new System.Drawing.Font("Consolas", 10f) };
            _columns.DoubleClick += (s, e) =>
            {
                if (_columns.SelectedItem is string column)
                    InsertAtCaret(_expression, "[" + column + "]");
            };
            columnsGroup.Controls.Add(_columns);

            var functionsGroup = new GroupBox { Text = "Functions (double-click to insert)", Left = 690, Top = 76, Width = 220, Height = 160 };
            _functions = new ListBox { Dock = DockStyle.Fill, Font = new System.Drawing.Font("Consolas", 10f) };
            foreach (var snippet in FunctionSnippets) _functions.Items.Add(snippet.Label);
            _functions.DoubleClick += (s, e) =>
            {
                if (_functions.SelectedIndex >= 0)
                    InsertFunction(FunctionSnippets[_functions.SelectedIndex].Snippet);
            };
            functionsGroup.Controls.Add(_functions);

            var evaluate = new Button { Text = "Evaluate", Left = 10, Top = 410, Width = 100 };
            evaluate.Click += (s, e) => Evaluate();

            _result = new Label
            {
                Left = 120,
                Top = 415,
                Width = 790,
                Height = 40,
                Font = new System.Drawing.Font("Consolas", 11f, System.Drawing.FontStyle.Bold)
            };

            var hint = new Label
            {
                Left = 10,
                Top = 458,
                Width = 900,
                Height = 120,
                ForeColor = System.Drawing.Color.DimGray,
                Text = "Supported: + - * / %, comparisons, AND/OR/NOT, IS NULL, LIKE, and functions like " +
                       "UPPER, LOWER, TRIM, LEN, CONCAT, SUBSTRING, REPLACE, LEFT, RIGHT, ROUND, ABS, COALESCE, ISNULL, NULLIF, IIF, YEAR, MONTH, DAY, NOW.\r\n" +
                       "Columns are referenced as [name] (double-click a column to insert). Functions insert with placeholder arguments; " +
                       "move the caret between the parentheses and type values."
            };

            Controls.Add(expressionLabel);
            Controls.Add(_expression);
            Controls.Add(operators);
            Controls.Add(variablesLabel);
            Controls.Add(_variables);
            Controls.Add(columnsGroup);
            Controls.Add(functionsGroup);
            Controls.Add(evaluate);
            Controls.Add(_result);
            Controls.Add(hint);
        }

        private void RefreshColumns()
        {
            var selected = _columns.SelectedItem as string;
            _columns.Items.Clear();

            foreach (var line in (_variables.Text ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int separator = line.IndexOf('=');
                if (separator <= 0) continue;
                var name = line.Substring(0, separator).Trim();
                if (name.Length > 0 && !_columns.Items.Contains(name))
                    _columns.Items.Add(name);
            }

            if (selected != null && _columns.Items.Contains(selected))
                _columns.SelectedItem = selected;
        }

        private void InsertAtCaret(TextBox box, string text)
        {
            int position = box.SelectionStart;
            box.Text = box.Text.Insert(position, text);
            box.SelectionStart = position + text.Length;
            box.Focus();
        }

        private void InsertFunction(string snippet)
        {
            int open = snippet.IndexOf('(');
            InsertAtCaret(_expression, snippet);

            if (open >= 0)
            {
                // Place the caret inside the parentheses.
                int trailing = snippet.Length - open - 1;
                _expression.SelectionStart = Math.Max(0, _expression.SelectionStart - trailing);
            }
        }

        private void Evaluate()
        {
            try
            {
                var row = ParseVariables(_variables.Text);
                var engine = new ExpressionEngine();
                var result = engine.Evaluate(_expression.Text, row);
                _result.ForeColor = System.Drawing.Color.FromArgb(0x2E, 0x7D, 0x32);
                _result.Text = result == null ? "= NULL" : $"= {result} ({result.GetType().Name})";
            }
            catch (Exception ex)
            {
                _result.ForeColor = System.Drawing.Color.FromArgb(0xC6, 0x28, 0x28);
                _result.Text = "Error: " + ex.Message;
            }
        }

        private static Dictionary<string, object> ParseVariables(string text)
        {
            var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(text)) return row;

            foreach (var line in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int separator = line.IndexOf('=');
                if (separator <= 0) continue;

                var key = line.Substring(0, separator).Trim();
                var raw = line.Substring(separator + 1).Trim();
                if (key.Length == 0) continue;

                var parsed = ParseValue(raw);
                if (parsed != null) row[key] = parsed;
            }
            return row;
        }

        private static object? ParseValue(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            if (bool.TryParse(raw, out var b)) return b;
            if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)) return l;
            if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
            return raw;
        }
    }
}
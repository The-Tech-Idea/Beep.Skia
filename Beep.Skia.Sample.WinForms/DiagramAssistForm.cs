using System;
using System.Windows.Forms;
using Beep.Skia;
using Beep.Skia.Assist;

namespace Beep.Skia.Sample.WinForms
{
    /// <summary>
    /// Generates diagrams from a text description (flowchart DSL or indented mind-map outline)
    /// and inserts the result into the canvas.
    /// </summary>
    public class DiagramAssistForm : Form
    {
        private readonly DrawingManager _manager;
        private ComboBox _kind = null!;
        private TextBox _prompt = null!;
        private Label _status = null!;
        private Button _insert = null!;
        private DiagramSuggestion _suggestion = null!;

        private const string SampleFlowchart =
            "Start(start) \"Start\" -> Receive \"Receive order\" -> Valid(decision) \"Valid?\"\n" +
            "Valid -> Pay \"Process payment\" : yes\n" +
            "Valid -> Notify \"Notify customer\" : no\n" +
            "Pay -> Ship \"Ship order\" -> End(end) \"End\"\n" +
            "Notify -> End";

        private const string SampleMindMap =
            "Product strategy\n" +
            "  Customers\n" +
            "    Personas\n" +
            "    Feedback\n" +
            "  Pricing\n" +
            "  Roadmap";

        public DiagramAssistForm(DrawingManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "Generate Diagram from Description";
            Width = 760;
            Height = 560;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;

            var top = new Panel { Dock = DockStyle.Top, Height = 40 };
            var kindLabel = new Label { Text = "Kind:", Left = 10, Top = 12, Width = 40 };
            _kind = new ComboBox
            {
                Left = 52,
                Top = 8,
                Width = 130,
                DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            };
            _kind.Items.AddRange(new object[] { "Auto", "Flowchart", "Mindmap" });
            _kind.SelectedIndex = 0;

            var sample = new Button { Text = "Load Sample", Left = 194, Top = 8, Width = 100 };
            sample.Click += (s, e) =>
            {
                _prompt.Text = _kind.SelectedIndex == 2 ? SampleMindMap : SampleFlowchart;
            };

            top.Controls.Add(kindLabel);
            top.Controls.Add(_kind);
            top.Controls.Add(sample);

            _prompt = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Consolas", 10f),
                Text = SampleFlowchart
            };

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 110 };
            _status = new Label { Left = 10, Top = 8, Width = 720, Height = 40 };

            var generate = new Button { Text = "Generate", Left = 10, Top = 52, Width = 100 };
            generate.Click += (s, e) => Generate();

            _insert = new Button { Text = "Insert into Canvas", Left = 118, Top = 52, Width = 150, Enabled = false };
            _insert.Click += (s, e) => Insert();

            var close = new Button { Text = "Close", Left = 276, Top = 52, Width = 80 };
            close.Click += (s, e) => Close();

            bottom.Controls.Add(_status);
            bottom.Controls.Add(generate);
            bottom.Controls.Add(_insert);
            bottom.Controls.Add(close);

            Controls.Add(_prompt);
            Controls.Add(top);
            Controls.Add(bottom);
        }

        private void Generate()
        {
            var request = new DiagramRequest
            {
                Prompt = _prompt.Text,
                DiagramKind = _kind.SelectedIndex switch
                {
                    1 => "flowchart",
                    2 => "mindmap",
                    _ => null
                }
            };

            _suggestion = new DiagramAssistantRegistry().Generate(request);
            _insert.Enabled = _suggestion.Success;

            if (_suggestion.Success)
            {
                var warnings = _suggestion.Warnings.Count > 0
                    ? "  Warnings: " + string.Join(" | ", _suggestion.Warnings)
                    : string.Empty;
                _status.Text = $"{_suggestion.Explanation}  (provider: {_suggestion.Provider}){warnings}";
            }
            else
            {
                _status.Text = "Generation failed: " + string.Join(" | ", _suggestion.Warnings);
            }
        }

        private void Insert()
        {
            if (_suggestion?.Success != true || _suggestion.Diagram == null) return;
            try
            {
                _manager.LoadFromDto(_suggestion.Diagram);
                _status.Text = _suggestion.Explanation + "  Inserted into canvas.";
            }
            catch (Exception ex)
            {
                _status.Text = "Insert failed: " + ex.Message;
            }
        }
    }
}
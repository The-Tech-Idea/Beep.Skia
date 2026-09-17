using System;
using System.IO;
using System.Windows.Forms;
using Beep.Skia.Flowchart;

namespace Beep.Skia.Sample.WinForms
{
    /// <summary>
    /// Modal preview for generated flowchart code with language switching, copy, and save.
    /// </summary>
    public class CodePreviewForm : Form
    {
        private readonly Func<CodeLanguage, string> _generate;
        private ComboBox _language = null!;
        private TextBox _code = null!;

        public CodePreviewForm(Func<CodeLanguage, string> generate)
        {
            _generate = generate;
            BuildUi();
            Regenerate();
        }

        private void BuildUi()
        {
            Text = "Generated Code";
            Width = 760;
            Height = 560;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;

            var top = new Panel { Dock = DockStyle.Top, Height = 40 };
            _language = new ComboBox
            {
                DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList,
                Width = 160,
                Left = 8,
                Top = 8
            };
            _language.Items.AddRange(new object[] { CodeLanguage.Pseudocode, CodeLanguage.Python, CodeLanguage.CSharp });
            _language.SelectedIndex = 0;
            _language.SelectedIndexChanged += (s, e) => Regenerate();

            var copy = new Button { Text = "Copy", Left = 180, Top = 7, Width = 80 };
            copy.Click += (s, e) =>
            {
                try { Clipboard.SetText(_code.Text); } catch { }
            };

            var save = new Button { Text = "Save…", Left = 268, Top = 7, Width = 80 };
            save.Click += (s, e) => SaveCode();

            top.Controls.Add(_language);
            top.Controls.Add(copy);
            top.Controls.Add(save);

            _code = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Consolas", 10f)
            };

            Controls.Add(_code);
            Controls.Add(top);
        }

        private CodeLanguage SelectedLanguage =>
            _language?.SelectedItem is CodeLanguage lang ? lang : CodeLanguage.Pseudocode;

        private void Regenerate()
        {
            try
            {
                _code.Text = _generate?.Invoke(SelectedLanguage) ?? string.Empty;
            }
            catch (Exception ex)
            {
                _code.Text = "// Generation failed: " + ex.Message;
            }
        }

        private void SaveCode()
        {
            var extension = SelectedLanguage switch
            {
                CodeLanguage.Python => "py",
                CodeLanguage.CSharp => "cs",
                _ => "txt"
            };
            using var dlg = new SaveFileDialog
            {
                Title = "Save Generated Code",
                Filter = $"Code (*.{extension})|*.{extension}|All files (*.*)|*.*",
                FileName = $"flowchart.{extension}"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try { File.WriteAllText(dlg.FileName, _code.Text); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Save Code"); }
        }
    }
}
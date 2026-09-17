using System;
using System.IO;
using System.Windows.Forms;

namespace Beep.Skia.Sample.WinForms
{
    /// <summary>
    /// Simple read-only text preview with copy and save, used for generated scripts and reports.
    /// </summary>
    public class TextPreviewForm : Form
    {
        private readonly string _text;
        private TextBox _body = null!;

        public TextPreviewForm(string title, string text)
        {
            _text = text ?? string.Empty;
            BuildUi(title);
        }

        private void BuildUi(string title)
        {
            Text = string.IsNullOrWhiteSpace(title) ? "Preview" : title;
            Width = 820;
            Height = 620;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;

            var top = new Panel { Dock = DockStyle.Top, Height = 40 };

            var copy = new Button { Text = "Copy", Left = 8, Top = 7, Width = 80 };
            copy.Click += (s, e) => { try { Clipboard.SetText(_text); } catch { } };

            var save = new Button { Text = "Save…", Left = 96, Top = 7, Width = 80 };
            save.Click += (s, e) => Save();

            top.Controls.Add(copy);
            top.Controls.Add(save);

            _body = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Consolas", 10f),
                Text = _text
            };

            Controls.Add(_body);
            Controls.Add(top);
        }

        private void Save()
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Save",
                Filter = "SQL script (*.sql)|*.sql|Text file (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = "migration.sql"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try { File.WriteAllText(dlg.FileName, _text); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Save"); }
        }
    }
}
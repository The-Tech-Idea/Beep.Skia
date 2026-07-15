using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Layout;
using Beep.Skia.Model;
using Beep.Skia.Serialization;

namespace Beep.Skia.Sample.WinForms
{
    public partial class MainForm : Form
    {
        private StatusStrip _statusBar;
        private ToolStripStatusLabel _statusLabel;
        private ToolStripDropDownButton _ddTemplates;
        private ToolStripButton _tbTheme, _tbExportPng, _tbExportSvg, _tbValidate, _tbArrange, _tbClear;
        private ToolStripButton _tbUndo, _tbRedo;
        private ToolStripButton _tbCopy, _tbPaste, _tbDelete;

        public MainForm()
        {
            InitializeComponent();
            BuildToolbar();
            BuildStatusBar();
            WireKeyboard();
            WireTheme();

            _statusLabel.Text = "Ready — Select a template from Templates menu or use the in-canvas palette";
        }

        private void BuildToolbar()
        {
            toolStrip1.Items.Clear();

            // Templates dropdown
            _ddTemplates = new ToolStripDropDownButton("Templates");
            foreach (var kvp in DiagramTemplates.All)
            {
                var name = kvp.Key;
                _ddTemplates.DropDownItems.Add(name, null, (s, e) => LoadTemplate(name));
            }
            _ddTemplates.DropDownItems.Add(new ToolStripSeparator());
            _ddTemplates.DropDownItems.Add("Clear All", null, (s, e) => ClearAll());
            toolStrip1.Items.Add(_ddTemplates);

            toolStrip1.Items.Add(new ToolStripSeparator());

            // Edit operations
            _tbUndo = AddToolBtn("Undo", (s, e) => DoUndo());
            _tbRedo = AddToolBtn("Redo", (s, e) => DoRedo());
            _tbCopy = AddToolBtn("Copy", (s, e) => DoCopy());
            _tbPaste = AddToolBtn("Paste", (s, e) => DoPaste());
            _tbDelete = AddToolBtn("Delete", (s, e) => DoDelete());

            toolStrip1.Items.Add(new ToolStripSeparator());

            // Diagram operations
            _tbArrange = AddToolBtn("Arrange", (s, e) => DoArrange());
            _tbValidate = AddToolBtn("Validate", (s, e) => DoValidate());

            toolStrip1.Items.Add(new ToolStripSeparator());

            // Theme
            _tbTheme = AddToolBtn("Dark Mode", (s, e) => ToggleTheme());

            // Export
            _tbExportPng = AddToolBtn("PNG", (s, e) => ExportPng());
            _tbExportSvg = AddToolBtn("SVG", (s, e) => ExportSvg());

            toolStrip1.Items.Add(new ToolStripSeparator());

            // Save/Load
            var tbSave = AddToolBtn("Save", (s, e) => SaveLayout());
            var tbLoad = AddToolBtn("Load", (s, e) => LoadLayout());

            // Clear
            _tbClear = AddToolBtn("Clear", (s, e) => ClearAll());
        }

        private ToolStripButton AddToolBtn(string text, EventHandler handler)
        {
            var btn = new ToolStripButton(text);
            btn.Click += handler;
            toolStrip1.Items.Add(btn);
            return btn;
        }

        private void BuildStatusBar()
        {
            _statusBar = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel("Ready");
            _statusBar.Items.Add(_statusLabel);
            this.Controls.Add(_statusBar);
        }

        private void WireKeyboard()
        {
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                int mods = 0;
                if (e.Control) mods |= 1;
                if (e.Shift) mods |= 2;
                if (e.Alt) mods |= 4;

                var mgr = skiaHostControl1?.DrawingManager;
                if (mgr == null) return;

                if (mgr.HandleKeyDown((int)e.KeyCode, mods))
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
        }

        private void WireTheme()
        {
            ThemeManager.ThemeChanged += (s, e) =>
            {
                UpdateThemeButton();
            };
        }

        private void UpdateThemeButton()
        {
            _tbTheme.Text = ThemeManager.Current.Name == "Dark" ? "Light Mode" : "Dark Mode";
            _tbTheme.Invalidate();
        }

        // ── Actions ───────────────────────────────────────────────────────────

        private void LoadTemplate(string name)
        {
            if (!DiagramTemplates.All.TryGetValue(name, out var factory)) return;
            var dto = factory();
            skiaHostControl1.DrawingManager.LoadTemplate(dto);
            _statusLabel.Text = $"Loaded: {name}";
        }

        private void DoUndo()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr?.CanUndo == true) { mgr.Undo(); _statusLabel.Text = "Undo"; }
        }

        private void DoRedo()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr?.CanRedo == true) { mgr.Redo(); _statusLabel.Text = "Redo"; }
        }

        private void DoCopy()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            mgr?.CopySelectedComponents();
            _statusLabel.Text = "Copied to clipboard";
        }

        private void DoPaste()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            mgr?.PasteComponents(new SkiaSharp.SKPoint(100, 100));
            _statusLabel.Text = "Pasted";
        }

        private void DoDelete()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr != null && mgr.SelectionManager.SelectionCount > 0)
            {
                mgr.DeleteSelectedComponents();
                _statusLabel.Text = "Deleted selected";
            }
        }

        private void DoArrange()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr != null)
            {
                mgr.ArrangeDiagram(new GridAutoLayout { Columns = 3, StartX = 50, StartY = 50 });
                _statusLabel.Text = "Arranged (grid)";
            }
        }

        private void DoValidate()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;
            var issues = mgr.ValidateDiagram();
            if (issues.Count == 0)
            {
                _statusLabel.Text = "Validation passed — no issues";
            }
            else
            {
                var errors = issues.Count(i => i.Severity == DiagramIssueSeverity.Error);
                var warnings = issues.Count(i => i.Severity == DiagramIssueSeverity.Warning);
                _statusLabel.Text = $"Validation: {errors} errors, {warnings} warnings";
                var msg = string.Join("\n", issues.Take(10).Select(i => i.ToString()));
                if (issues.Count > 10) msg += $"\n... and {issues.Count - 10} more";
                MessageBox.Show(msg, "Diagram Validation", MessageBoxButtons.OK,
                    errors > 0 ? MessageBoxIcon.Error : MessageBoxIcon.Warning);
            }
        }

        private void ToggleTheme()
        {
            ThemeManager.ApplyTheme(ThemeManager.Current.Name == "Dark" ? "Light" : "Dark");
            _statusLabel.Text = $"Theme: {ThemeManager.Current.Name}";
        }

        private void ExportPng()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;
            try
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"skia_diagram_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                mgr.ExportToPng(path, scale: 2f);
                _statusLabel.Text = $"Exported: {path}";
                MessageBox.Show($"Exported to:\n{path}", "Export PNG", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Export failed: " + ex.Message;
            }
        }

        private void ExportSvg()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;
            try
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"skia_diagram_{DateTime.Now:yyyyMMdd_HHmmss}.svg");
                mgr.ExportToSvg(path);
                _statusLabel.Text = $"Exported: {path}";
                MessageBox.Show($"Exported to:\n{path}", "Export SVG", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Export failed: " + ex.Message;
            }
        }

        private void SaveLayout()
        {
            try
            {
                var mgr = skiaHostControl1?.DrawingManager;
                if (mgr == null) return;
                var dto = mgr.ToDto();
                var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText("skia_layout.json", json);
                _statusLabel.Text = "Saved layout to skia_layout.json";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Save failed: " + ex.Message;
            }
        }

        private void LoadLayout()
        {
            try
            {
                if (!File.Exists("skia_layout.json")) { _statusLabel.Text = "No layout file found"; return; }
                var json = File.ReadAllText("skia_layout.json");
                var dto = JsonSerializer.Deserialize<DiagramDto>(json);
                if (dto != null)
                {
                    skiaHostControl1?.DrawingManager?.LoadFromDto(dto);
                    _statusLabel.Text = "Loaded layout from skia_layout.json";
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Load failed: " + ex.Message;
            }
        }

        private void ClearAll()
        {
            skiaHostControl1?.DrawingManager?.ClearComponents();
            _statusLabel.Text = "Cleared all components";
        }
    }
}

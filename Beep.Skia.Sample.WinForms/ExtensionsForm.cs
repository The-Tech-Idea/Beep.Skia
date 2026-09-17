using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Beep.Skia.Extensions.Marketplace;
using Beep.Skia.Winform.Controls;

namespace Beep.Skia.Sample.WinForms
{
    /// <summary>
    /// Extension manager: shows installed packages, installs from a .beepkg file or a local
    /// registry folder, uninstalls, updates, and displays the operation log.
    /// </summary>
    public class ExtensionsForm : Form
    {
        private readonly SkiaHostControl _host;
        private ListView _installed = null!;
        private ListView _available = null!;
        private TextBox _log = null!;
        private Label _registryLabel = null!;
        private LocalExtensionRegistry _registry = null!;

        public ExtensionsForm(SkiaHostControl host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            BuildUi();
            RefreshInstalled();
            UpdateRegistryLabel();
        }

        private void BuildUi()
        {
            Text = "Extensions";
            Width = 900;
            Height = 560;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;

            var top = new Panel { Dock = DockStyle.Top, Height = 44 };
            var installFile = new Button { Text = "Install from File…", Left = 10, Top = 8, Width = 130 };
            installFile.Click += (s, e) => InstallFromFile();

            var chooseRegistry = new Button { Text = "Choose Registry…", Left = 148, Top = 8, Width = 120 };
            chooseRegistry.Click += (s, e) => ChooseRegistry();

            var installSelected = new Button { Text = "Install Selected", Left = 276, Top = 8, Width = 110 };
            installSelected.Click += (s, e) => InstallSelected();

            var updateAll = new Button { Text = "Update All", Left = 394, Top = 8, Width = 90 };
            updateAll.Click += (s, e) => UpdateAll();

            var uninstall = new Button { Text = "Uninstall", Left = 492, Top = 8, Width = 90 };
            uninstall.Click += (s, e) => UninstallSelected();

            var reload = new Button { Text = "Reload", Left = 590, Top = 8, Width = 70 };
            reload.Click += (s, e) => Reload();

            _registryLabel = new Label { Left = 670, Top = 13, Width = 210, Text = "Registry: (none)" };

            top.Controls.Add(installFile);
            top.Controls.Add(chooseRegistry);
            top.Controls.Add(installSelected);
            top.Controls.Add(updateAll);
            top.Controls.Add(uninstall);
            top.Controls.Add(reload);
            top.Controls.Add(_registryLabel);

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = System.Windows.Forms.Orientation.Horizontal,
                SplitterDistance = 210
            };

            _installed = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true };
            _installed.Columns.Add("Installed", 200);
            _installed.Columns.Add("Version", 80);
            _installed.Columns.Add("Installed At", 130);
            _installed.Columns.Add("Path", 420);

            _available = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true };
            _available.Columns.Add("Available", 200);
            _available.Columns.Add("Version", 80);
            _available.Columns.Add("Tags", 140);
            _available.Columns.Add("Description", 420);

            split.Panel1.Controls.Add(_installed);
            split.Panel2.Controls.Add(_available);

            _log = new TextBox
            {
                Dock = DockStyle.Bottom,
                Height = 110,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new System.Drawing.Font("Consolas", 8.5f)
            };

            Controls.Add(split);
            Controls.Add(_log);
            Controls.Add(top);
        }

        private void ChooseRegistry()
        {
            using var dialog = new FolderBrowserDialog { Description = "Select a folder containing .beepkg extension packages" };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            _registry = new LocalExtensionRegistry(dialog.SelectedPath);
            _host.Extensions.Registry = _registry;
            UpdateRegistryLabel();
            RefreshAvailable();
            AppendLog($"Registry set to {dialog.SelectedPath} ({_registry.Packages.Count} package(s))");
        }

        private void UpdateRegistryLabel()
        {
            _registryLabel.Text = _registry == null ? "Registry: (none)" : $"Registry: {Path.GetFileName(_registry.Root.TrimEnd(Path.DirectorySeparatorChar))}";
        }

        private void InstallFromFile()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Select an extension package",
                Filter = "Beep extension packages (*.beepkg)|*.beepkg|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            var result = _host.Extensions.Install(dialog.FileName);
            AppendLog(result.Success
                ? $"Installed {result.Extension.Id} {result.Extension.Version}"
                : "Install failed: " + result.Error);
            Reload();
        }

        private void InstallSelected()
        {
            if (_available.SelectedItems.Count == 0) return;
            var package = _available.SelectedItems[0].Tag as ExtensionPackage;
            if (package == null) return;

            var result = _host.Extensions.InstallFromRegistry(package.Id, package.Version);
            AppendLog(result.Success
                ? $"Installed {result.Extension.Id} {result.Extension.Version}"
                : "Install failed: " + result.Error);
            Reload();
        }

        private void UpdateAll()
        {
            if (_registry == null)
            {
                AppendLog("Choose a registry folder first.");
                return;
            }

            foreach (var installed in _host.Extensions.Installed.ToList())
            {
                var result = _host.Extensions.Update(installed.Id);
                AppendLog(result.Success
                    ? $"Updated {installed.Id} → {result.Extension.Version}"
                    : $"Update skipped for {installed.Id}: {result.Error}");
            }
            Reload();
        }

        private void UninstallSelected()
        {
            if (_installed.SelectedItems.Count == 0) return;
            var id = _installed.SelectedItems[0].Tag as string;
            if (string.IsNullOrEmpty(id)) return;

            if (MessageBox.Show(this, $"Uninstall '{id}'?", "Extensions", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            var result = _host.Extensions.Uninstall(id);
            AppendLog(result.Success ? $"Uninstalled {id}" : "Uninstall failed: " + result.Error);
            Reload();
        }

        private void Reload()
        {
            var count = _host.ReloadExtensions();
            AppendLog($"Reloaded extensions: {count} component(s) in the palette.");
            RefreshInstalled();
            RefreshAvailable();
        }

        private void RefreshInstalled()
        {
            _installed.BeginUpdate();
            try
            {
                _installed.Items.Clear();
                foreach (var extension in _host.Extensions.Installed.OrderBy(e => e.Id))
                {
                    var item = new ListViewItem(extension.Id);
                    item.SubItems.Add(extension.Version);
                    item.SubItems.Add(extension.InstalledAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
                    item.SubItems.Add(extension.InstallPath);
                    item.Tag = extension.Id;
                    _installed.Items.Add(item);
                }
            }
            finally
            {
                _installed.EndUpdate();
            }
        }

        private void RefreshAvailable()
        {
            _available.BeginUpdate();
            try
            {
                _available.Items.Clear();
                if (_registry == null) return;
                foreach (var package in _registry.Search())
                {
                    var item = new ListViewItem(package.Id);
                    item.SubItems.Add(package.Version);
                    item.SubItems.Add(string.Join(", ", package.Manifest?.Tags ?? new System.Collections.Generic.List<string>()));
                    item.SubItems.Add(package.Manifest?.Description ?? string.Empty);
                    item.Tag = package;
                    _available.Items.Add(item);
                }
            }
            finally
            {
                _available.EndUpdate();
            }
        }

        private void AppendLog(string message)
        {
            _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }
    }
}
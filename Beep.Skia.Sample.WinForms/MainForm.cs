using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.DFD;
using Beep.Skia.Flowchart;
using Beep.Skia.Layout;
using Beep.Skia.Model;
using Beep.Skia.Serialization;
using ExecutionContext = Beep.Skia.Model.ExecutionContext;

namespace Beep.Skia.Sample.WinForms
{
    public partial class MainForm : Form
    {
        private StatusStrip _statusBar = null!;
        private ToolStripStatusLabel _statusLabel = null!;
        private ToolStripDropDownButton _ddTemplates = null!;
        private ToolStripButton _tbTheme = null!, _tbExportPng = null!, _tbExportSvg = null!, _tbExportPdf = null!, _tbPrint = null!, _tbValidate = null!, _tbClear = null!;
        private ToolStripButton _tbUndo = null!, _tbRedo = null!;
        private ToolStripButton _tbCopy = null!, _tbPaste = null!, _tbDelete = null!;
        private FlowchartSimulator _simulator = null!;
        private SkiaComponent _simHighlighted = null!;
        private SkiaSharp.SKColor? _simOriginalStroke;
        private readonly DFDLevelNavigator _levelNavigator = new DFDLevelNavigator();
        private WorkflowEngine _workflowEngine = null!;
        private string _activeExecutionId = null!;
        private readonly System.Collections.Generic.List<string> _runHistory = new System.Collections.Generic.List<string>();

        public MainForm()
        {
            InitializeComponent();
            BuildToolbar();
            BuildStatusBar();
            WireKeyboard();
            WireTheme();
            ApplyHelpHints();

            _statusLabel.Text = "Ready — Select a template from Templates menu or use the in-canvas palette (F1 for documentation)";
        }

        private static readonly Dictionary<string, string> HelpToolTips = new()
        {
            ["Undo"] = "Undo the last action (Ctrl+Z)",
            ["Redo"] = "Redo the last undone action (Ctrl+Y)",
            ["Copy"] = "Copy the selection (Ctrl+C)",
            ["Paste"] = "Paste at the pointer position (Ctrl+V)",
            ["Delete"] = "Delete the selection and its attached lines (Del)",
            ["Validate"] = "Run the diagram validator and report issues in the status bar",
            ["Dark Mode"] = "Switch between the light and dark theme (Ctrl+T)",
            ["PNG"] = "Export the diagram to a PNG file",
            ["SVG"] = "Export the diagram to an SVG file",
            ["PDF"] = "Export the diagram to a PDF file",
            ["Print"] = "Show the print preview",
            ["Save"] = "Save the diagram as JSON",
            ["Load"] = "Load a diagram from JSON",
            ["Clear"] = "Remove every component from the canvas"
        };

        private void ApplyHelpHints()
        {
            foreach (ToolStripItem item in toolStrip1.Items)
            {
                if (!string.IsNullOrEmpty(item.Text) && HelpToolTips.TryGetValue(item.Text, out var tip))
                    item.ToolTipText = tip;
            }

            var hint = new ToolTip { AutoPopDelay = 8000, InitialDelay = 400 };
            hint.SetToolTip(skiaHostControl1,
                "Drag components from the in-canvas palette, then connect them by dragging from a port.\n" +
                "Wheel zooms, middle-button drags pan, F1 opens the documentation.");
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

            // Auto-layout dropdown
            var ddLayout = new ToolStripDropDownButton("Layout");
            ddLayout.DropDownItems.Add("Grid", null, (s, e) => DoLayout("grid"));
            ddLayout.DropDownItems.Add("Hierarchical", null, (s, e) => DoLayout("hierarchical"));
            ddLayout.DropDownItems.Add("Radial", null, (s, e) => DoLayout("radial"));
            ddLayout.DropDownItems.Add("MindMap (radial)", null, (s, e) => DoLayout("mindmap"));
            ddLayout.DropDownItems.Add("Force-directed", null, (s, e) => DoLayout("force"));
            toolStrip1.Items.Add(ddLayout);

            // Alignment dropdown
            var ddAlign = new ToolStripDropDownButton("Align");
            ddAlign.DropDownItems.Add("Left", null, (s, e) => DoAlign("left"));
            ddAlign.DropDownItems.Add("Right", null, (s, e) => DoAlign("right"));
            ddAlign.DropDownItems.Add("Top", null, (s, e) => DoAlign("top"));
            ddAlign.DropDownItems.Add("Bottom", null, (s, e) => DoAlign("bottom"));
            ddAlign.DropDownItems.Add("Center (H)", null, (s, e) => DoAlign("centerH"));
            ddAlign.DropDownItems.Add("Center (V)", null, (s, e) => DoAlign("centerV"));
            ddAlign.DropDownItems.Add(new ToolStripSeparator());
            ddAlign.DropDownItems.Add("Distribute (H)", null, (s, e) => DoAlign("distH"));
            ddAlign.DropDownItems.Add("Distribute (V)", null, (s, e) => DoAlign("distV"));
            toolStrip1.Items.Add(ddAlign);

            _tbValidate = AddToolBtn("Validate", (s, e) => DoValidate());

            // Analyzer tools
            var ddTools = new ToolStripDropDownButton("Tools");
            ddTools.DropDownItems.Add("Run ERC (ECAD)", null, (s, e) => DoElectricalRulesCheck());
            ddTools.DropDownItems.Add("Generate STRIDE Threats", null, (s, e) => DoStrideAnalysis());
            ddTools.DropDownItems.Add("Compute Schedule (PM)", null, (s, e) => DoComputeSchedule());
            ddTools.DropDownItems.Add("Add Gantt Timeline (PM)", null, (s, e) => DoAddGantt());
            ddTools.DropDownItems.Add("Drill Into Process (DFD)", null, (s, e) => DoDrillDown());
            ddTools.DropDownItems.Add("Go Up Level (DFD)", null, (s, e) => DoGoUpLevel());
            ddTools.DropDownItems.Add("Import DDL… (ERD)", null, (s, e) => DoImportDdl());
            ddTools.DropDownItems.Add("Compare Schemas… (ERD)", null, (s, e) => DoCompareSchemas());
            ddTools.DropDownItems.Add("Expression Tester… (ETL)", null, (s, e) => DoExpressionTester());
            ddTools.DropDownItems.Add("Data Profiler… (ETL)", null, (s, e) => DoDataProfiler());
            ddTools.DropDownItems.Add("Flatten JSON/XML… (ETL)", null, (s, e) => DoFlattenStructured());
            ddTools.DropDownItems.Add("Export BPMN… (Business)", null, (s, e) => DoExportBpmn());
            ddTools.DropDownItems.Add("Export XMI… (UML)", null, (s, e) => DoExportXmi());
            ddTools.DropDownItems.Add("Toggle Collapse (MindMap)", null, (s, e) => DoToggleCollapse());
            ddTools.DropDownItems.Add("Add Sample Chart (Quantitative)", null, (s, e) => DoAddSampleChart());
            ddTools.DropDownItems.Add("Export ML Pipeline… (ML)", null, (s, e) => DoExportMlPipeline());
            ddTools.DropDownItems.Add(new ToolStripSeparator());
            ddTools.DropDownItems.Add("Run Workflow (Automation)", null, (s, e) => DoRunWorkflow());
            ddTools.DropDownItems.Add("Execution Service…", null, (s, e) => DoShowExecutionService());
            ddTools.DropDownItems.Add("Cancel Workflow", null, (s, e) => DoCancelWorkflow());
            ddTools.DropDownItems.Add("Run History…", null, (s, e) => DoShowRunHistory());
            ddTools.DropDownItems.Add("Analyze Network", null, (s, e) => DoNetworkAnalysis());
            ddTools.DropDownItems.Add("Find Shortest Path (Network)", null, (s, e) => DoFindShortestPath());
            ddTools.DropDownItems.Add("Generate Code… (Flowchart)", null, (s, e) => DoGenerateCode());
            toolStrip1.Items.Add(ddTools);

            // Flowchart simulation
            var ddSimulate = new ToolStripDropDownButton("Simulate");
            ddSimulate.DropDownItems.Add("Step", null, (s, e) => DoSimStep());
            ddSimulate.DropDownItems.Add("Run", null, (s, e) => DoSimRun());
            ddSimulate.DropDownItems.Add("Reset", null, (s, e) => DoSimReset());
            toolStrip1.Items.Add(ddSimulate);

            // Collaboration
            var ddCollab = new ToolStripDropDownButton("Collaborate");
            ddCollab.DropDownItems.Add("Comments…", null, (s, e) => DoShowComments());
            ddCollab.DropDownItems.Add("Toggle Comment Pins", null, (s, e) => DoToggleCommentPins());
            ddCollab.DropDownItems.Add("Presence Snapshot", null, (s, e) => DoPresenceSnapshot());
            toolStrip1.Items.Add(ddCollab);

            // Assisted generation
            var ddAssist = new ToolStripDropDownButton("Generate");
            ddAssist.DropDownItems.Add("From Description…", null, (s, e) => DoGenerateFromDescription());
            toolStrip1.Items.Add(ddAssist);

            // Extensions
            var ddExt = new ToolStripDropDownButton("Extensions");
            ddExt.DropDownItems.Add("Manage Extensions…", null, (s, e) => DoManageExtensions());
            toolStrip1.Items.Add(ddExt);

            // Help
            var ddHelp = new ToolStripDropDownButton("Help");
            ddHelp.DropDownItems.Add("Documentation (F1)", null, (s, e) => DoOpenHelp("index.html"));
            ddHelp.DropDownItems.Add("Keyboard Shortcuts…", null, (s, e) => DoShowShortcuts());
            ddHelp.DropDownItems.Add("Quick Start", null, (s, e) => DoOpenHelp("getting-started/quick-start.html"));
            ddHelp.DropDownItems.Add("Sample Applications", null, (s, e) => DoOpenHelp("getting-started/samples.html"));
            ddHelp.DropDownItems.Add("Troubleshooting", null, (s, e) => DoOpenHelp("guides/troubleshooting.html"));
            ddHelp.DropDownItems.Add("API Reference", null, (s, e) => DoOpenHelp("reference/api-index.html"));
            ddHelp.DropDownItems.Add(new ToolStripSeparator());
            ddHelp.DropDownItems.Add("About Beep.Skia", null, (s, e) => DoShowAbout());
            toolStrip1.Items.Add(ddHelp);

            toolStrip1.Items.Add(new ToolStripSeparator());

            // Theme
            _tbTheme = AddToolBtn("Dark Mode", (s, e) => ToggleTheme());

            // Export
            _tbExportPng = AddToolBtn("PNG", (s, e) => ExportPng());
            _tbExportSvg = AddToolBtn("SVG", (s, e) => ExportSvg());
            _tbExportPdf = AddToolBtn("PDF", (s, e) => ExportPdf());
            _tbPrint = AddToolBtn("Print", (s, e) => PrintDiagram());

            toolStrip1.Items.Add(new ToolStripSeparator());

            // Save/Load
            var tbSave = AddToolBtn("Save", (s, e) => SaveLayout());
            var tbLoad = AddToolBtn("Load", (s, e) => LoadLayout());

            // Clear
            _tbClear = AddToolBtn("Clear", (s, e) => ClearAll());

            // First-run hint (dismissible)
            toolStrip1.Items.Add(new ToolStripSeparator());
            var hintLabel = new ToolStripLabel(
                "New here?  Press F1 for the documentation  ·  drag components from the in-canvas palette  ·  Ctrl+T toggles the theme")
            {
                ForeColor = SystemColors.GrayText
            };
            var dismissHint = new ToolStripButton("×")
            {
                ToolTipText = "Hide this hint",
                Alignment = ToolStripItemAlignment.Right
            };
            dismissHint.Click += (s, e) =>
            {
                toolStrip1.Items.Remove(hintLabel);
                toolStrip1.Items.Remove(dismissHint);
            };
            toolStrip1.Items.Add(hintLabel);
            toolStrip1.Items.Add(dismissHint);
        }

        private ToolStripButton AddToolBtn(string text, EventHandler handler)
        {
            var btn = new ToolStripButton(text);
            btn.Click += handler;
            toolStrip1.Items.Add(btn);
            return btn;
        }

        private static string? ResolveHelpRoot()
        {
            var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
            {
                var candidate = System.IO.Path.Combine(dir.FullName, "Help");
                if (System.IO.File.Exists(System.IO.Path.Combine(candidate, "index.html")))
                    return candidate;
            }
            return null;
        }

        private void DoOpenHelp(string relativePath)
        {
            var root = ResolveHelpRoot();
            if (root != null)
            {
                var full = System.IO.Path.Combine(root, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(full))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(full) { UseShellExecute = true });
                    _statusLabel.Text = $"Opened documentation: {relativePath}";
                    return;
                }
            }

            var url = "https://github.com/The-Tech-Idea/Beep.Skia/blob/master/README.md";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            _statusLabel.Text = "Help folder not found next to the app; opened the repository README";
        }

        private void DoShowShortcuts()
        {
            var lines = new[]
            {
                "Editing",
                "  Ctrl+Z / Ctrl+Y          Undo / redo",
                "  Ctrl+C / Ctrl+X / Ctrl+V Copy / cut / paste",
                "  Delete or Backspace      Delete the selection and its lines",
                "  Ctrl+S                   Save (raises SaveRequested)",
                "",
                "Selection and navigation",
                "  Ctrl+A                   Select all",
                "  Tab / Shift+Tab          Cycle the selection",
                "  Escape                   Clear the selection",
                "  Arrow keys               Move the selection by 1 unit",
                "  Shift+arrows             Move the selection by 10 units",
                "",
                "View",
                "  + / -                    Zoom in / out (0.1x to 5x)",
                "  Mouse wheel              Zoom around the pointer",
                "  Middle-button drag       Pan the viewport",
                "  Ctrl+G                   Toggle the grid",
                "  Ctrl+T                   Toggle the light / dark theme",
                "",
                "Mouse",
                "  Drag from a port         Draw a connection",
                "  Drag on empty canvas     Marquee selection",
                "  Right-click              Context menu",
                "  Double-click a component Drill-down hook",
                "",
                "Help",
                "  F1                       Open the documentation"
            };

            MessageBox.Show(string.Join(Environment.NewLine, lines),
                "Keyboard and Mouse Shortcuts",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DoShowAbout()
        {
            var version = typeof(MainForm).Assembly.GetName().Version?.ToString() ?? "1.0.0";
            var helpRoot = ResolveHelpRoot();
            MessageBox.Show(
                $"Beep.Skia diagram editor sample\n\n" +
                $"Assembly version: {version}\n" +
                $"Help folder: {(helpRoot ?? "not found (run from the repository to browse offline docs)")}\n\n" +
                $"Documentation: Help/index.html\n" +
                $"Repository: https://github.com/The-Tech-Idea/Beep.Skia",
                "About Beep.Skia",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                if (e.KeyCode == Keys.F1)
                {
                    DoOpenHelp("index.html");
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }

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

        private void DoLayout(string kind)
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;

            IAutoLayout layout = kind switch
            {
                "hierarchical" => new HierarchicalLayout(),
                "radial" => new RadialLayout(),
                "mindmap" => new Beep.Skia.MindMap.MindMapLayout(),
                "force" => new ForceDirectedLayout(),
                _ => new GridAutoLayout { Columns = 3, StartX = 50, StartY = 50 }
            };

            mgr.ArrangeDiagram(layout);
            _statusLabel.Text = $"Arranged ({kind})";
        }

        private void DoAlign(string kind)
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;

            switch (kind)
            {
                case "left": mgr.AlignLeft(); break;
                case "right": mgr.AlignRight(); break;
                case "top": mgr.AlignTop(); break;
                case "bottom": mgr.AlignBottom(); break;
                case "centerH": mgr.AlignCenterHorizontal(); break;
                case "centerV": mgr.AlignCenterVertical(); break;
                case "distH": mgr.DistributeHorizontal(); break;
                case "distV": mgr.DistributeVertical(); break;
            }
            _statusLabel.Text = $"Aligned ({kind})";
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

        private void DoElectricalRulesCheck()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;

            var components = mgr.GetComponents().Where(c => c is Beep.Skia.ECAD.ECADControl).ToList();
            if (components.Count == 0)
            {
                _statusLabel.Text = "ERC: no ECAD components in the diagram";
                return;
            }

            var checker = new Beep.Skia.ECAD.ElectricalRulesChecker();
            checker.RunChecks(mgr.GetComponents(), mgr.GetLines());

            if (checker.Violations.Count == 0)
            {
                _statusLabel.Text = "ERC passed — no electrical rule violations";
                MessageBox.Show("ERC passed — no violations.", "Electrical Rules Check",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var errors = checker.Violations.Count(v => v.Severity == Beep.Skia.ECAD.ElectricalViolationSeverity.Error);
            _statusLabel.Text = $"ERC: {checker.Violations.Count} violations ({errors} errors)";
            var msg = string.Join("\n", checker.Violations.Take(12).Select(v => $"[{v.Severity}] {v.Message}"));
            if (checker.Violations.Count > 12) msg += $"\n... and {checker.Violations.Count - 12} more";
            MessageBox.Show(msg, "Electrical Rules Check", MessageBoxButtons.OK,
                errors > 0 ? MessageBoxIcon.Error : MessageBoxIcon.Warning);
        }

        private void DoStrideAnalysis()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;

            var assets = mgr.GetComponents().Where(c => c is Beep.Skia.Security.AssetNode).ToList();
            if (assets.Count == 0)
            {
                _statusLabel.Text = "STRIDE: no asset nodes in the diagram";
                return;
            }

            var analyzer = new Beep.Skia.Security.StrideAnalyzer();
            analyzer.Analyze(mgr.GetComponents());

            // Materialize threats on the canvas so they can be inspected and connected.
            float x = 60, y = 60;
            foreach (var threat in analyzer.Threats)
            {
                var node = new Beep.Skia.Security.ThreatNode
                {
                    X = x,
                    Y = y,
                    Width = 180,
                    Height = 72
                };
                node.ThreatName = threat.Threat;
                node.Severity = Enum.TryParse<Beep.Skia.Security.Severity>(threat.Severity, true, out var sev)
                    ? sev
                    : Beep.Skia.Security.Severity.Medium;
                mgr.AddComponent(node);

                x += 200;
                if (x > 900) { x = 60; y += 92; }
            }

            _statusLabel.Text = $"STRIDE: generated {analyzer.Threats.Count} threats";
            var msg = string.Join("\n", analyzer.Threats.Take(10).Select(t =>
                $"[{t.Category}] {t.Threat} — DREAD {t.Dread.Average:0.0} ({t.Dread.RiskLevel})" +
                (t.Techniques.Count > 0 ? $" [{t.Techniques[0].Id}]" : "")));
            if (analyzer.Threats.Count > 10) msg += $"\n... and {analyzer.Threats.Count - 10} more";
            MessageBox.Show(msg, "STRIDE Threat Analysis", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DoComputeSchedule()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;

            var tasks = mgr.GetComponents().Where(c => c is Beep.Skia.PM.TaskNode).ToList();
            if (tasks.Count == 0)
            {
                _statusLabel.Text = "Schedule: no task nodes in the diagram";
                return;
            }

            var calculator = new Beep.Skia.PM.CriticalPathCalculator();
            calculator.Calculate(mgr.GetComponents(), mgr.GetLines());

            var criticalNames = calculator.CriticalPath.Select(t => t.Name ?? t.GetType().Name).ToList();
            _statusLabel.Text = $"Schedule: {calculator.ProjectDuration} days, {criticalNames.Count} critical tasks";

            var msg = $"Project duration: {calculator.ProjectDuration} days\n" +
                      $"Critical path: {(criticalNames.Count > 0 ? string.Join(" → ", criticalNames) : "(none)")}";
            MessageBox.Show(msg, "Critical Path", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DoAddGantt()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;

            var tasks = mgr.GetComponents().OfType<Beep.Skia.PM.TaskNode>().ToList();
            if (tasks.Count == 0)
            {
                _statusLabel.Text = "Gantt: no task nodes in the diagram";
                return;
            }

            var calculator = new Beep.Skia.PM.CriticalPathCalculator();
            calculator.Calculate(mgr.GetComponents(), mgr.GetLines());

            var contentBounds = mgr.GetContentBounds();
            var gantt = new Beep.Skia.PM.GanttTimelineNode
            {
                X = contentBounds.Left,
                Y = contentBounds.Bottom + 20
            };
            gantt.SetSchedule(tasks, calculator);
            mgr.AddComponent(gantt);

            _statusLabel.Text = $"Gantt timeline added ({gantt.Rows.Count} rows)";
        }

        private async void DoRunWorkflow()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;

            var automation = mgr.GetComponents().OfType<Beep.Skia.Components.AutomationNode>().ToList();
            if (automation.Count == 0)
            {
                _statusLabel.Text = "Workflow: add automation nodes (DataInput, DataTransform, Conditional, …) first";
                return;
            }

            var engine = new WorkflowEngine();
            var workflow = mgr.ToWorkflowDefinition("Canvas Workflow");
            if (workflow.Nodes.Count == 0)
            {
                _statusLabel.Text = "Workflow: no executable automation nodes found";
                return;
            }

            foreach (var node in automation)
            {
                node.SetExecutionStatus(NodeStatus.Idle);
            }

            var context = new ExecutionContext(workflow.Id, Guid.NewGuid().ToString("N")[..12]);
            var startedAt = DateTime.UtcNow;
            _workflowEngine = engine;
            _activeExecutionId = context.ExecutionId;

            var timer = new System.Windows.Forms.Timer { Interval = 120 };
            timer.Tick += (s, e) => SyncWorkflowStatus(engine, mgr, workflow, context.ExecutionId);
            timer.Start();

            _statusLabel.Text = $"Workflow running ({workflow.Nodes.Count} nodes)…";
            try
            {
                await engine.LoadWorkflowAsync(workflow);
                var result = await engine.ExecuteWorkflowAsync(workflow.Id, context);
                var elapsed = DateTime.UtcNow - startedAt;

                SyncWorkflowStatus(engine, mgr, workflow, context.ExecutionId);
                _runHistory.Add($"{DateTime.Now:HH:mm:ss} {result.Status} in {elapsed.TotalMilliseconds:0} ms — {result.NodeResults.Count} node(s) ok, {result.Errors.Count} error(s)");
                _statusLabel.Text = $"Workflow {result.Status} in {elapsed.TotalMilliseconds:0} ms";
            }
            catch (Exception ex)
            {
                _runHistory.Add($"{DateTime.Now:HH:mm:ss} failed: {ex.Message}");
                _statusLabel.Text = "Workflow failed: " + ex.Message;
            }
            finally
            {
                timer.Stop();
                timer.Dispose();
                _activeExecutionId = string.Empty;
            }
        }

        private void SyncWorkflowStatus(WorkflowEngine engine, DrawingManager mgr, WorkflowDefinition workflow, string executionId)
        {
            var execution = engine.GetExecutionStatus(executionId);
            if (execution == null) return;

            var components = mgr.GetComponents().OfType<Beep.Skia.Components.AutomationNode>().ToList();
            foreach (var nodeExec in execution.NodeExecutions)
            {
                var definition = workflow.Nodes.FirstOrDefault(n => n.Id == nodeExec.NodeId);
                if (definition == null) continue;

                var component = components.FirstOrDefault(c => c.Name == definition.Name);
                if (component == null || component.Status == nodeExec.Status) continue;

                component.SetExecutionStatus(nodeExec.Status);
            }
            skiaHostControl1?.Invalidate();
        }

        private void DoCancelWorkflow()
        {
            if (_workflowEngine == null || string.IsNullOrEmpty(_activeExecutionId))
            {
                _statusLabel.Text = "Workflow: no run in progress";
                return;
            }

            _ = _workflowEngine.CancelExecutionAsync(_activeExecutionId);
            _statusLabel.Text = "Workflow cancellation requested";
        }

        private void DoShowRunHistory()
        {
            var text = _runHistory.Count == 0
                ? "(no workflow runs yet)"
                : string.Join(Environment.NewLine, _runHistory);
            using var preview = new TextPreviewForm("Workflow Run History", text);
            preview.ShowDialog(this);
        }

        private void DoExportMlPipeline()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;
            if (!mgr.GetComponents().Any(c => c is Beep.Skia.ML.MLControl))
            {
                _statusLabel.Text = "ML: no ML pipeline nodes in the diagram";
                return;
            }

            using var dlg = new SaveFileDialog
            {
                Title = "Export ML Pipeline",
                Filter = "JSON (*.json)|*.json|All files (*.*)|*.*",
                FileName = "pipeline.json"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var json = new Beep.Skia.ML.MLPipelineExporter().ExportJson(mgr, "Pipeline");
                File.WriteAllText(dlg.FileName, json);
                _statusLabel.Text = $"Exported ML pipeline: {dlg.FileName}";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "ML export failed: " + ex.Message;
            }
        }

        private void DoAddSampleChart()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;

            var bounds = mgr.GetContentBounds();
            var chart = new Beep.Ski.Quantitative.ChartNode
            {
                X = bounds.Left,
                Y = bounds.Bottom + 20,
                Width = 320,
                Height = 200,
                ChartType = "Line",
                Title = "Demo Prices"
            };

            var random = new Random(7);
            double price = 100;
            var close = new Beep.Ski.Quantitative.ChartSeries { Name = "close" };
            var volume = new Beep.Ski.Quantitative.ChartSeries { Name = "volume" };
            for (int i = 0; i < 60; i++)
            {
                price += (random.NextDouble() - 0.5) * 4;
                close.Values.Add(Math.Round(price, 2));
                volume.Values.Add(random.Next(50, 200));
            }
            chart.SetData(close, volume);

            mgr.AddComponent(chart);
            _statusLabel.Text = "Sample chart added (Quantitative)";
        }

        private void DoToggleCollapse()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;

            var selected = mgr.SelectionManager.SelectedComponents
                .OfType<Beep.Skia.MindMap.MindMapControl>()
                .ToList();
            if (selected.Count == 0)
            {
                _statusLabel.Text = "MindMap: select one or more mind-map nodes to collapse/expand";
                return;
            }

            foreach (var node in selected)
                node.IsCollapsed = !node.IsCollapsed;

            var changes = Beep.Skia.MindMap.MindMapVisibility.Apply(mgr.GetComponents(), mgr.GetLines());
            skiaHostControl1?.Invalidate();
            _statusLabel.Text = $"MindMap: toggled {selected.Count} node(s), {changes} visibility change(s)";
        }

        private void DoExportBpmn()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;
            if (!mgr.GetComponents().Any(c => c is Beep.Skia.Business.BusinessControl))
            {
                _statusLabel.Text = "BPMN: no business process nodes in the diagram";
                return;
            }

            using var dlg = new SaveFileDialog
            {
                Title = "Export BPMN 2.0",
                Filter = "BPMN XML (*.bpmn;*.xml)|*.bpmn;*.xml|All files (*.*)|*.*",
                FileName = "process.bpmn"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var xml = new Beep.Skia.Business.BpmnExporter().Export(mgr.GetComponents(), mgr.GetLines(), "Process");
                File.WriteAllText(dlg.FileName, xml);
                _statusLabel.Text = $"Exported BPMN: {dlg.FileName}";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "BPMN export failed: " + ex.Message;
            }
        }

        private void DoExportXmi()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;
            if (!mgr.GetComponents().Any(c => c is Beep.Skia.UML.UMLControl))
            {
                _statusLabel.Text = "XMI: no UML elements in the diagram";
                return;
            }

            using var dlg = new SaveFileDialog
            {
                Title = "Export XMI",
                Filter = "XMI (*.xmi;*.xml)|*.xmi;*.xml|All files (*.*)|*.*",
                FileName = "model.xmi"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var xmi = new Beep.Skia.UML.XmiExporter().Export(mgr.GetComponents(), mgr.GetLines(), "Model");
                File.WriteAllText(dlg.FileName, xmi);
                _statusLabel.Text = $"Exported XMI: {dlg.FileName}";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "XMI export failed: " + ex.Message;
            }
        }

        private void DoFlattenStructured()
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Open JSON or XML",
                Filter = "Structured data (*.json;*.xml)|*.json;*.xml|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var text = File.ReadAllText(dlg.FileName);
                var flattener = new Beep.Skia.ETL.StructuredDataFlattener();
                var isXml = dlg.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);
                var rows = isXml ? flattener.FlattenXml(text) : flattener.FlattenJson(text);

                if (rows.Count == 0)
                {
                    _statusLabel.Text = "Flatten: no rows found";
                    return;
                }

                var schema = flattener.InferSchema(rows);
                var profile = new Beep.Skia.ETL.DataProfiler().Profile(rows);
                var schemaText = string.Join("\n", schema.Select(c =>
                    $"  {c.Name,-32} {c.DataType,-10} {(c.IsNullable ? "NULL" : "NOT NULL")}"));

                var report = $"Inferred schema ({schema.Count} columns):\n{schemaText}\n\n" +
                             profile.Summary() + "\n\n--- Preview (first 20 rows) ---\n" +
                             Beep.Skia.ETL.DataPreviewFormatter.ToTable(rows, 20);

                using var preview = new TextPreviewForm("Structured Data", report);
                preview.ShowDialog(this);

                _statusLabel.Text = $"Flattened {rows.Count} rows / {schema.Count} columns";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Flatten failed: " + ex.Message;
            }
        }

        private void DoDataProfiler()
        {
            try
            {
                var rows = BuildDemoRows(120);
                var profile = new Beep.Skia.ETL.DataProfiler().Profile(rows);

                var metrics = new Beep.Skia.ETL.PipelineMetrics();
                metrics.Start();
                metrics.Measure("Extract", 0, () => rows.Count);
                metrics.Measure("Profile", rows.Count, () => profile.Columns.Count);
                metrics.Complete();

                var text = profile.Summary() + "\n\n--- Preview (first 15 rows) ---\n" +
                           Beep.Skia.ETL.DataPreviewFormatter.ToTable(rows, 15) + "\n\n" +
                           metrics.Summary();

                using var preview = new TextPreviewForm("Data Profile", text);
                preview.ShowDialog(this);

                _statusLabel.Text = $"Profiled {profile.RowCount} rows / {profile.Columns.Count} columns";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Profile failed: " + ex.Message;
            }
        }

        private static System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>> BuildDemoRows(int count)
        {
            var rows = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>();
            var random = new Random(42);
            var regions = new[] { "North", "South", "East", "West" };
            for (int i = 0; i < count; i++)
            {
                rows.Add(new System.Collections.Generic.Dictionary<string, object>
                {
                    ["order_id"] = 1000 + i,
                    ["customer"] = $"Customer {random.Next(1, 25)}",
                    ["region"] = regions[random.Next(regions.Length)],
                    ["amount"] = Math.Round(random.NextDouble() * 500, 2),
                    ["quantity"] = random.Next(1, 10),
                    ["ordered_at"] = DateTime.UtcNow.AddDays(-random.Next(0, 90)),
                    ["notes"] = i % 7 == 0 ? null! : "priority"
                });
            }
            return rows;
        }

        private void DoExpressionTester()
        {
            using var tester = new ExpressionTesterForm();
            tester.ShowDialog(this);
            _statusLabel.Text = "Expression tester closed";
        }

        private void DoShowComments()
        {
            using var comments = new CommentsForm(skiaHostControl1);
            comments.ShowDialog(this);
            _statusLabel.Text = "Comments closed";
        }

        private void DoToggleCommentPins()
        {
            skiaHostControl1.ShowCommentPins = !skiaHostControl1.ShowCommentPins;
            skiaHostControl1.RefreshCommentPins();
            _statusLabel.Text = skiaHostControl1.ShowCommentPins ? "Comment pins shown" : "Comment pins hidden";
        }

        private void DoPresenceSnapshot()
        {
            try
            {
                var service = skiaHostControl1.Collaboration;
                service.SetPresence(skiaHostControl1.DocumentId, "local", "editing");
                var active = service.GetPresence(skiaHostControl1.DocumentId);
                var lines = active.Select(p => $"{p.UserId} — {p.Activity} (last seen {p.LastSeen:HH:mm:ss})");
                _statusLabel.Text = $"{active.Count} active collaborator(s)";
                MessageBox.Show(this,
                    active.Count == 0 ? "No active collaborators." : string.Join(Environment.NewLine, lines),
                    "Presence",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Presence failed: " + ex.Message;
            }
        }

        private void DoGenerateFromDescription()
        {
            try
            {
                using var form = new DiagramAssistForm(skiaHostControl1.DrawingManager);
                form.ShowDialog(this);
                _statusLabel.Text = "Diagram generation closed";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Generation failed: " + ex.Message;
            }
        }

        private void DoShowExecutionService()
        {
            try
            {
                using var form = new ExecutionServiceForm(skiaHostControl1.DrawingManager);
                form.ShowDialog(this);
                _statusLabel.Text = "Execution service closed";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Execution service failed: " + ex.Message;
            }
        }

        private void DoManageExtensions()
        {
            try
            {
                using var form = new ExtensionsForm(skiaHostControl1);
                form.ShowDialog(this);
                _statusLabel.Text = "Extension manager closed";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Extension manager failed: " + ex.Message;
            }
        }

        private void DoCompareSchemas()
        {
            using var sourceDlg = new OpenFileDialog
            {
                Title = "Select SOURCE schema (current)",
                Filter = "SQL files (*.sql)|*.sql|All files (*.*)|*.*"
            };
            if (sourceDlg.ShowDialog(this) != DialogResult.OK) return;

            using var targetDlg = new OpenFileDialog
            {
                Title = "Select TARGET schema (desired)",
                Filter = "SQL files (*.sql)|*.sql|All files (*.*)|*.*"
            };
            if (targetDlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var importer = new Beep.Skia.ERD.DDLImporter();
                var source = importer.Parse(File.ReadAllText(sourceDlg.FileName));
                var target = importer.Parse(File.ReadAllText(targetDlg.FileName));

                var diff = new Beep.Skia.ERD.SchemaComparer().Compare(source, target);
                var generator = new Beep.Skia.ERD.MigrationScriptGenerator();
                var text = diff.Summary() + "\n\n" +
                           generator.GenerateForward(diff) + "\n" +
                           generator.GenerateRollback(diff);

                using var preview = new TextPreviewForm("Schema Migration", text);
                preview.ShowDialog(this);

                _statusLabel.Text = diff.HasChanges
                    ? $"Schema compare: {diff.Changes.Count} change(s)"
                    : "Schema compare: schemas are identical";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Schema compare failed: " + ex.Message;
            }
        }

        private void DoDrillDown()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;

            var process = mgr.SelectionManager.SelectedComponents.OfType<DFDProcess>().FirstOrDefault();
            if (process == null)
            {
                _statusLabel.Text = "DFD: select a process node with a child diagram";
                return;
            }
            if (process.ChildDiagramData == null)
            {
                _statusLabel.Text = $"DFD: '{process.Label}' has no child diagram";
                return;
            }

            if (_levelNavigator.Current == null) _levelNavigator.Reset();
            _levelNavigator.DrillDown(process.Label ?? process.Name ?? "Process", mgr.ToDto(), process.ChildDiagramData);
            mgr.LoadFromDto(process.ChildDiagramData);
            _statusLabel.Text = $"DFD level: {_levelNavigator.Breadcrumb}";
        }

        private void DoGoUpLevel()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;

            if (!_levelNavigator.CanGoUp)
            {
                _statusLabel.Text = "DFD: already at the top level";
                return;
            }

            var frame = _levelNavigator.GoUp();
            if (frame?.Diagram != null) mgr.LoadFromDto(frame.Diagram);
            _statusLabel.Text = $"DFD level: {_levelNavigator.Breadcrumb}";
        }

        private void DoImportDdl()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;

            using var dlg = new OpenFileDialog
            {
                Title = "Import DDL",
                Filter = "SQL files (*.sql)|*.sql|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var ddl = File.ReadAllText(dlg.FileName);
                var importer = new Beep.Skia.ERD.DDLImporter(Beep.Skia.ERD.DDLImporter.SQLDialect.ANSI);
                var entities = importer.ImportToEntities(ddl, Beep.Skia.ERD.DDLImporter.SQLDialect.ANSI);

                float x = 60, y = 60;
                foreach (var entity in entities)
                {
                    entity.X = x;
                    entity.Y = y;
                    entity.Width = 220;
                    entity.Height = 140;
                    mgr.AddComponent(entity);

                    x += 260;
                    if (x > 1000) { x = 60; y += 180; }
                }

                _statusLabel.Text = $"Imported {entities.Count} tables from DDL";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "DDL import failed: " + ex.Message;
            }
        }

        private void DoSimReset()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;

            ClearSimHighlight();
            _simulator = new FlowchartSimulator();
            _simulator.Reset(mgr.GetComponents(), mgr.GetLines());
            HighlightSimCurrent();

            _statusLabel.Text = _simulator.IsFinished
                ? "Simulation: no entry node found"
                : $"Simulation reset at '{SimNodeName(_simulator.CurrentNode!)}'";
        }

        private void DoSimStep()
        {
            if (_simulator == null) { DoSimReset(); if (_simulator == null) return; }
            if (_simulator.IsFinished) { _statusLabel.Text = "Simulation already finished — press Reset"; return; }

            ClearSimHighlight();
            _simulator.Step();
            HighlightSimCurrent();

            _statusLabel.Text = _simulator.IsFinished
                ? $"Simulation finished after {_simulator.StepCount} steps"
                : $"Step {_simulator.StepCount}: '{SimNodeName(_simulator.CurrentNode!)}'";
        }

        private void DoSimRun()
        {
            if (_simulator == null) { DoSimReset(); if (_simulator == null) return; }

            ClearSimHighlight();
            var finished = _simulator.Run(500);
            HighlightSimCurrent();

            _statusLabel.Text = finished
                ? $"Simulation finished after {_simulator.StepCount} steps"
                : $"Simulation capped at {_simulator.StepCount} steps (possible cycle)";
        }

        private void HighlightSimCurrent()
        {
            if (_simulator?.CurrentNode is FlowchartControl node)
            {
                _simHighlighted = node;
                _simOriginalStroke = node.CustomStrokeColor;
                node.CustomStrokeColor = new SkiaSharp.SKColor(255, 152, 0); // amber
                node.InvalidateVisual();
                skiaHostControl1?.Invalidate();
            }
        }

        private void ClearSimHighlight()
        {
            if (_simHighlighted is FlowchartControl node)
            {
                node.CustomStrokeColor = _simOriginalStroke;
                node.InvalidateVisual();
            }
            _simHighlighted = null!;
            _simOriginalStroke = null;
            skiaHostControl1?.Invalidate();
        }

        private static string SimNodeName(SkiaComponent node)
        {
            if (node == null) return "(none)";
            return string.IsNullOrWhiteSpace(node.Name) ? node.GetType().Name : node.Name;
        }

        private void DoGenerateCode()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;

            var hasFlowchart = mgr.GetComponents().Any(c => c is Beep.Skia.Flowchart.FlowchartControl);
            if (!hasFlowchart)
            {
                _statusLabel.Text = "Codegen: no flowchart nodes in the diagram";
                return;
            }

            using var preview = new CodePreviewForm(lang =>
                new Beep.Skia.Flowchart.FlowchartCodeGenerator().Generate(mgr, lang));
            preview.ShowDialog(this);
            _statusLabel.Text = "Generated flowchart code";
        }

        private void DoNetworkAnalysis()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;

            var nodes = mgr.GetComponents().OfType<Beep.Skia.Network.NetworkNode>().ToList();
            var links = mgr.GetComponents().OfType<Beep.Skia.Network.NetworkLink>().ToList();
            if (nodes.Count == 0)
            {
                _statusLabel.Text = "Network: no network nodes in the diagram";
                return;
            }

            new Beep.Skia.Network.CentralityMeasure().CalculateCentrality(nodes, links);
            new Beep.Skia.Network.CommunityDetector().DetectCommunities(nodes, links);

            var pageRank = Beep.Skia.Network.NetworkAlgorithms.PageRank(nodes, links);
            Beep.Skia.Network.NetworkAlgorithms.ApplyPageRank(pageRank);

            int communities = nodes.Select(n => n.CommunityId).Distinct().Count();
            var top = string.Join(", ", pageRank.Top(3).Select(kvp => $"{kvp.Key.Name} ({kvp.Value:0.###})"));
            _statusLabel.Text = $"Network: {nodes.Count} nodes, {links.Count} links, {communities} communities | top: {top}";
        }

        private void DoFindShortestPath()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;

            var selected = mgr.SelectionManager.SelectedComponents
                .OfType<Beep.Skia.Network.NetworkNode>()
                .ToList();
            if (selected.Count != 2)
            {
                _statusLabel.Text = "Path: select exactly two network nodes";
                return;
            }

            var links = mgr.GetComponents().OfType<Beep.Skia.Network.NetworkLink>().ToList();
            foreach (var node in mgr.GetComponents().OfType<Beep.Skia.Network.NetworkNode>())
            {
                if (node.IsHighlighted) { node.IsHighlighted = false; node.InvalidateVisual(); }
            }

            var path = Beep.Skia.Network.NetworkAlgorithms.ShortestPath(selected[0], selected[1], links);
            if (path.Count == 0)
            {
                _statusLabel.Text = $"Path: no route from '{selected[0].Name}' to '{selected[1].Name}'";
                return;
            }

            foreach (var node in path) { node.IsHighlighted = true; node.InvalidateVisual(); }
            var cost = Beep.Skia.Network.NetworkAlgorithms.PathCost(path, links);
            _statusLabel.Text = $"Path ({cost:0.##}): {string.Join(" -> ", path.Select(n => n.Name))}";
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
                mgr.ExportToSvg(path, background: SkiaSharp.SKColors.White);
                _statusLabel.Text = $"Exported: {path}";
                MessageBox.Show($"Exported to:\n{path}", "Export SVG", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Export failed: " + ex.Message;
            }
        }

        private void ExportPdf()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;
            try
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"skia_diagram_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                mgr.ExportToPdf(path);
                _statusLabel.Text = $"Exported: {path}";
                MessageBox.Show($"Exported to:\n{path}", "Export PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Export failed: " + ex.Message;
            }
        }

        private void PrintDiagram()
        {
            var mgr = skiaHostControl1?.DrawingManager;
            if (mgr == null) return;
            try
            {
                var doc = mgr.CreatePrintDocument("Beep.Skia Diagram");
                using var preview = new PrintPreviewDialog
                {
                    Document = doc,
                    Width = 1000,
                    Height = 700,
                    Text = "Print Preview"
                };
                preview.ShowDialog(this);
                _statusLabel.Text = "Print preview closed";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Print failed: " + ex.Message;
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

                Beep.Skia.Collaboration.CollaborationSerializer.Save(
                    skiaHostControl1!.Collaboration, "skia_collaboration.json");

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

                    if (File.Exists("skia_collaboration.json"))
                    {
                        var restored = Beep.Skia.Collaboration.CollaborationSerializer.Load("skia_collaboration.json");
                        skiaHostControl1!.Collaboration.Restore(restored.CreateSnapshot());
                        skiaHostControl1.RefreshCommentPins();
                    }

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
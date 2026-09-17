using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows.Forms;
using SkiaSharp.Views.Desktop;
using SkiaSharp;
using Beep.Skia.Components;
using System.Linq;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Host control that provides a Skia drawing surface and a DrawingManager to host Skia components.")]
    [DisplayName("Skia Host")]
    [Designer(typeof(SkiaHostControlDesigner))]
    public class SkiaHostControl : UserControl, ISupportInitialize
    {
        private SKControl _skControl;
        private Beep.Skia.DrawingManager _drawingManager;
    // private Beep.Skia.ComponentManager _componentManager;
    private SkiaComponentDescriptorCollection _designTimeComponents = new SkiaComponentDescriptorCollection();
    private Palette _palette;
    private Beep.Skia.Components.ContextMenu _contextMenu;
    private System.Windows.Forms.TextBox _paletteSearch;
    private MinimapControl _minimap;
    private Beep.Skia.Extensions.SkiaExtensionHost _extensionHost;
    private Beep.Skia.Extensions.Marketplace.ExtensionPackageManager _extensionPackages;
    private readonly List<PaletteItem> _extensionPaletteItems = new List<PaletteItem>();
    private Beep.Skia.Collaboration.CollaborationService _collaboration;
    private readonly Beep.Skia.Collaboration.CommentPinLayer _commentPins = new Beep.Skia.Collaboration.CommentPinLayer();

    /// <summary>Folder that holds installed extensions (packages plus loose assemblies).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ExtensionsRoot { get; set; } =
        System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Extensions");

    /// <summary>Package manager for installing, updating, and removing extensions.</summary>
    public Beep.Skia.Extensions.Marketplace.ExtensionPackageManager Extensions
        => _extensionPackages ?? (_extensionPackages = new Beep.Skia.Extensions.Marketplace.ExtensionPackageManager(ExtensionsRoot));

    /// <summary>Extension host with the currently loaded extensions.</summary>
    public Beep.Skia.Extensions.SkiaExtensionHost ExtensionHost
        => _extensionHost ?? (_extensionHost = new Beep.Skia.Extensions.SkiaExtensionHost());

    /// <summary>
    /// Reloads extensions from the install root and refreshes their palette entries.
    /// Returns the number of extension components added.
    /// </summary>
        public int ReloadExtensions()
        {
            try
            {
                foreach (var item in _extensionPaletteItems) _palette?.RemoveItem(item);
                _extensionPaletteItems.Clear();

                ExtensionHost.Clear();
                ExtensionHost.LoadFromDirectory(ExtensionsRoot);
                foreach (var directory in Extensions.GetLoadDirectories())
                    ExtensionHost.LoadFromDirectory(directory);

                foreach (var descriptor in ExtensionHost.Components)
                {
                    var item = new PaletteItem
                    {
                        Name = descriptor.DisplayName ?? descriptor.ComponentType?.Name ?? "Extension",
                        ComponentType = descriptor.AssemblyQualifiedName,
                        Category = descriptor.Category ?? "Extensions"
                    };
                    _palette?.AddItem(item);
                    _extensionPaletteItems.Add(item);
                }

                _palette?.RefreshLayout();
                _skControl?.Invalidate();
            }
            catch { }

            return _extensionPaletteItems.Count;
        }

    /// <summary>Collaboration service backing comments, sharing, presence, and audit.</summary>
    public Beep.Skia.Collaboration.CollaborationService Collaboration
        => _collaboration ?? (_collaboration = new Beep.Skia.Collaboration.CollaborationService());

    /// <summary>Document id used for collaboration state.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string DocumentId { get; set; } = "diagram";

    /// <summary>Shows or hides comment pins in the canvas.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowCommentPins { get; set; } = true;

    /// <summary>Comment pin layer currently rendered over the diagram.</summary>
    public Beep.Skia.Collaboration.CommentPinLayer CommentPins => _commentPins;
    private ComponentPropertyEditor _propertyEditor;
    private SkiaComponentPropertyWrapper _selectedComponentWrapper;
    private SkiaMultiComponentWrapper _selectedMultiWrapper;
    // Runtime registry of created components keyed by Guid Id for quick lookup
    private readonly Dictionary<Guid, Beep.Skia.SkiaComponent> _componentRegistry = new Dictionary<Guid, Beep.Skia.SkiaComponent>();
    [Browsable(false)]
    public IReadOnlyDictionary<Guid, Beep.Skia.SkiaComponent> ComponentRegistry => _componentRegistry;
    /// <summary>
    /// If true, new components dropped (via wrapper drag/drop) will be centered under the cursor.
    /// If false (default), the cursor position becomes the component's top-left corner.
    /// </summary>
    [DefaultValue(false)]
    [Category("Behavior")]
    [Description("Center the component on the drop point instead of using it as the top-left.")]
    public bool CenterOnDrop { get; set; } = false;

    // Enable/disable runtime dragging of existing Skia components
    /// <summary>
    /// Gets or sets the allow component dragging.
    /// </summary>
    [Category("Behavior"), DefaultValue(true)]
    [Description("Allow picking up and moving existing components with the mouse.")]
    public bool AllowComponentDragging { get; set; } = true;

    // Drag state
    private Beep.Skia.SkiaComponent _dragComponent = null; // component currently being dragged
    private bool _isDraggingComponent = false;
    private SKPoint _dragStartCanvas; // mouse position (canvas coords) at drag start
    private float _dragComponentStartX;
    private float _dragComponentStartY;

        /// <summary>
        /// Gets or sets the drawing manager.
        /// </summary>
        public Beep.Skia.DrawingManager DrawingManager => _drawingManager;
        // Runtime manager for components (preferred for rendering and input)
        [Browsable(false)]
    // public Beep.Skia.ComponentManager ComponentManager => _componentManager;

    private bool _designDescriptorsInstantiated = false;

        /// <summary>
        /// Diagram editor canvas. Tab cycles through components; arrow keys move the selection; Delete removes it.
        /// </summary>
        public SkiaHostControl()
        {
            InitializeSkiaSurface();
            AllowDrop = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            // Accessibility: expose the canvas to assistive technology and honor OS high contrast.
            try
            {
                this.AccessibleName = "Skia diagram canvas";
                this.AccessibleRole = AccessibleRole.Diagram;
                this.AccessibleDescription = "Diagram editor canvas. Tab cycles through components; arrow keys move the selection; Delete removes it.";

                if (SystemInformation.HighContrast)
                    Beep.Skia.ThemeManager.ApplyTheme("HighContrast");

                Microsoft.Win32.SystemEvents.UserPreferenceChanged += (s, e) =>
                {
                    try
                    {
                        if (SystemInformation.HighContrast)
                            Beep.Skia.ThemeManager.ApplyTheme("HighContrast");
                        _skControl?.Invalidate();
                    }
                    catch { }
                };
            }
            catch { }

            _drawingManager = new Beep.Skia.DrawingManager();
            _drawingManager.DrawSurface += (s, e) =>
            {
                // Comment pins follow component geometry, so rebuild them when the diagram changes
                // (dragging fires DrawSurface) rather than on every paint.
                RebuildCommentPins();
                _skControl?.Invalidate();
            };
            _drawingManager.WorldOverlay = canvas =>
            {
                if (!ShowCommentPins) return;
                _commentPins.Draw(canvas);
            };
            // Keep the property editor synchronized with current selection
            _drawingManager.SelectionChanged += (s, e) =>
            {
                try
                {
                    if (_propertyEditor == null) return;
                    var sm = _drawingManager.SelectionManager;
                    // Prefer single line selection; otherwise single component; otherwise clear
                    if (sm.SelectedLines != null && sm.SelectedLines.Count == 1)
                    {
                        _propertyEditor.SelectedLine = sm.SelectedLines[0];
                        ClearSelectionWrapper();
                    }
                    else if (sm.SelectedComponents != null && sm.SelectedComponents.Count == 1)
                    {
                        _propertyEditor.SelectedComponent = sm.SelectedComponents[0];
                        SyncPropertyGridToSelection(sm.SelectedComponents[0]);
                        // Check if DDL was generated (ERD entity export)
                        CheckForDDLExport(sm.SelectedComponents[0]);
                    }
                    else if (sm.SelectedComponents != null && sm.SelectedComponents.Count > 1)
                    {
                        _propertyEditor.SelectedComponent = null;
                        SyncPropertyGridToMultiSelection(sm.SelectedComponents.ToList());
                    }
                    else
                    {
                        _propertyEditor.SelectedLine = null;
                        _propertyEditor.SelectedComponent = null;
                        ClearSelectionWrapper();
                    }
                    try
                    {
                        this.AccessibleDescription = sm.SelectionCount switch
                        {
                            0 => "Diagram editor canvas. Tab cycles through components.",
                            1 => $"Selected component: {sm.SelectedComponents[0].Name}.",
                            _ => $"{sm.SelectionCount} components selected."
                        };
                    }
                    catch { }

                    _skControl?.Invalidate();
                }
                catch { }
            };
            // ComponentManager removed: runtime now uses DrawingManager exclusively.
            // Create the in-Skia palette and add it to the drawing manager so it's present by default.
            try
            {
                _palette = new Palette
                {
                    X = 8,
                    Y = 40
                };
                // Constrain palette height to the host so it scrolls instead of growing endlessly
                try { _palette.MaxHeight = Math.Max(120, this.Height - 80); } catch { }
                try
                {
                    this.SizeChanged += (s2, e2) =>
                    {
                        try
                        {
                            if (_palette != null)
                            {
                                _palette.MaxHeight = Math.Max(120, this.Height - 80);
                                _palette.RefreshLayout();
                                _skControl?.Invalidate();
                            }
                        }
                        catch { }
                    };
                }
                catch { }

                // Populate palette using registry discovery; categorize UML vs Components
                try
                {
                    // Discover components across loaded and base-directory assemblies
                    var discovered = Beep.Skia.SkiaComponentRegistry.DiscoverAndRegisterDomainComponents();
                    foreach (var def in Beep.Skia.SkiaComponentRegistry.GetComponentsOrdered())
                    {
                        try
                        {
                            var display = def.className ?? def.type?.Name ?? def.dllname ?? def.AssemblyName ?? "Unknown";
                            var compType = def.type != null ? def.type.AssemblyQualifiedName : (def.className ?? def.dllname ?? string.Empty);
                            string category = "Components";
                            var ns = def.type?.Namespace ?? string.Empty;
                            if (!string.IsNullOrEmpty(ns))
                            {
                                if (ns.Contains(".UML", StringComparison.OrdinalIgnoreCase))
                                    category = "UML";
                                else if (ns.Contains(".Network", StringComparison.OrdinalIgnoreCase))
                                    category = "Network";
                                else if (ns.Contains(".ETL", StringComparison.OrdinalIgnoreCase))
                                    category = "ETL";
                                else if (ns.Contains(".Business", StringComparison.OrdinalIgnoreCase))
                                    category = "Business";
                                else if (ns.Contains(".DFD", StringComparison.OrdinalIgnoreCase))
                                    category = "DFD";
                                else if (ns.Contains(".ERD", StringComparison.OrdinalIgnoreCase))
                                    category = "ERD";
                                else if (ns.Contains(".Flowchart", StringComparison.OrdinalIgnoreCase))
                                    category = "Flowchart";
                                else if (ns.Contains(".PM", StringComparison.OrdinalIgnoreCase))
                                    category = "PM";
                                else if (ns.Contains(".StateMachine", StringComparison.OrdinalIgnoreCase))
                                    category = "StateMachine";
                                else if (ns.Contains(".Cloud", StringComparison.OrdinalIgnoreCase))
                                    category = "Cloud";
                                else if (ns.Contains(".ECAD", StringComparison.OrdinalIgnoreCase))
                                    category = "ECAD";
                                else if (ns.Contains(".ML", StringComparison.OrdinalIgnoreCase))
                                    category = "ML";
                                else if (ns.Contains(".Security", StringComparison.OrdinalIgnoreCase))
                                    category = "Security";
                                else if (ns.Contains(".Quantitative", StringComparison.OrdinalIgnoreCase))
                                    category = "Quantitative";
                            }
                            if (!string.IsNullOrWhiteSpace(compType))
                            {
                                _palette.AddItem(new PaletteItem { Name = display, ComponentType = compType, Category = category });
                            }
                        }
                        catch { }
                    }

                    // Add ERD multiplicity presets (tool items with no component type)
                    _palette.AddItem(new PaletteItem { Name = "ERD preset: One (|)", Category = "ERD", StartMultiplicity = Beep.Skia.Model.ERDMultiplicity.One, EndMultiplicity = null });
                    _palette.AddItem(new PaletteItem { Name = "ERD preset: Many (crow's foot)", Category = "ERD", StartMultiplicity = Beep.Skia.Model.ERDMultiplicity.Many, EndMultiplicity = null });
                    _palette.AddItem(new PaletteItem { Name = "ERD preset: One and only one (||)", Category = "ERD", StartMultiplicity = Beep.Skia.Model.ERDMultiplicity.OneOnly, EndMultiplicity = null });
                    _palette.AddItem(new PaletteItem { Name = "ERD preset: Zero or one (o|)", Category = "ERD", StartMultiplicity = Beep.Skia.Model.ERDMultiplicity.ZeroOrOne, EndMultiplicity = null });
                    _palette.AddItem(new PaletteItem { Name = "ERD preset: One or many (|<)", Category = "ERD", StartMultiplicity = Beep.Skia.Model.ERDMultiplicity.OneOrMany, EndMultiplicity = null });
                    _palette.AddItem(new PaletteItem { Name = "ERD preset: Zero or many (o<)", Category = "ERD", StartMultiplicity = Beep.Skia.Model.ERDMultiplicity.ZeroOrMany, EndMultiplicity = null });
                    _palette.AddItem(new PaletteItem { Name = "ERD preset: Clear (none)", Category = "ERD", StartMultiplicity = Beep.Skia.Model.ERDMultiplicity.Unspecified, EndMultiplicity = null });
                }
                catch { }

                // Wire palette events so drops and clicks actually instantiate components
                _palette.ItemDropped += (s, tup) =>
                {
                    try
                    {
                        var item = tup.Item;
                        // Convert palette-provided screen-space point to canvas coordinates
                        var ptScreen = tup.DropPoint;
                        var dm = _drawingManager;
                        var pt = ptScreen;
                        try
                        {
                            if (dm != null)
                            {
                                var offset = new SKPoint(ptScreen.X - dm.PanOffset.X, ptScreen.Y - dm.PanOffset.Y);
                                pt = new SKPoint(dm.Zoom != 0f ? offset.X / dm.Zoom : offset.X,
                                                 dm.Zoom != 0f ? offset.Y / dm.Zoom : offset.Y);
                            }
                        }
                        catch { }
                        if (string.IsNullOrWhiteSpace(item.ComponentType) && (item.StartMultiplicity.HasValue || item.EndMultiplicity.HasValue))
                        {
                            // Treat as ERD preset tool: set pending multiplicities for next line
                            _drawingManager.SetNextLineMultiplicityPreset(item.StartMultiplicity, item.EndMultiplicity);
                        }
                        else
                        {
                            var desc = new SkiaComponentDescriptor
                            {
                                ComponentType = item.ComponentType,
                                X = pt.X,
                                Y = pt.Y,
                                Width = 120,
                                Height = 36,
                                Name = "skia" + DateTime.UtcNow.Ticks.ToString("x")
                            };
                            CreateAndAddComponentFromDescriptor(desc);
                        }
                    }
                    catch { }
                };
                _palette.ItemActivated += (s, item) =>
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(item.ComponentType) && (item.StartMultiplicity.HasValue || item.EndMultiplicity.HasValue))
                        {
                            _drawingManager.SetNextLineMultiplicityPreset(item.StartMultiplicity, item.EndMultiplicity);
                        }
                        else
                        {
                            var desc = new SkiaComponentDescriptor
                            {
                                ComponentType = item.ComponentType,
                                // Place near the palette in canvas space regardless of pan/zoom
                                X = (_palette.X + _palette.Width + 20 - _drawingManager.PanOffset.X) / (_drawingManager.Zoom == 0 ? 1f : _drawingManager.Zoom),
                                Y = (_palette.Y + 20 - _drawingManager.PanOffset.Y) / (_drawingManager.Zoom == 0 ? 1f : _drawingManager.Zoom),
                                Width = 120,
                                Height = 36,
                                Name = "skia" + DateTime.UtcNow.Ticks.ToString("x")
                            };
                            CreateAndAddComponentFromDescriptor(desc);
                        }
                    }
                    catch { }
                };
                // Load installed extension packages into the palette.
                ReloadExtensions();

                _drawingManager?.AddComponent(_palette);

                // Palette search box (sits above the in-canvas palette).
                try
                {
                    _paletteSearch = new System.Windows.Forms.TextBox
                    {
                        Left = 8,
                        Top = 12,
                        Width = Math.Max(140, (int)_palette.Width),
                        Height = 22,
                        PlaceholderText = "Search palette…",
                        Font = new Font("Segoe UI", 8f)
                    };
                    _paletteSearch.TextChanged += (s2, e2) =>
                    {
                        try
                        {
                            _palette.SearchText = _paletteSearch.Text;
                            _skControl?.Invalidate();
                        }
                        catch { }
                    };
                    this.Controls.Add(_paletteSearch);
                    _paletteSearch.BringToFront();
                }
                catch { }

                // Minimap overlay (bottom-left).
                try
                {
                    _minimap = new MinimapControl
                    {
                        Manager = _drawingManager,
                        X = 8,
                        Y = Math.Max(220, this.Height - 170),
                        Width = 200,
                        Height = 150
                    };
                    this.SizeChanged += (s2, e2) =>
                    {
                        try
                        {
                            if (_minimap != null)
                            {
                                _minimap.Y = Math.Max(220, this.Height - 170);
                                _skControl?.Invalidate();
                            }
                        }
                        catch { }
                    };
                    _drawingManager?.AddComponent(_minimap);
                }
                catch { }

                // Create and add the property editor panel inside the Skia surface
                try
                {
                    _propertyEditor = new ComponentPropertyEditor
                    {
                        X = Math.Max(8, this.Width - 340),
                        Y = 40,
                        Width = 330,
                        Height = Math.Max(200, this.Height - 80),
                        Manager = _drawingManager
                    };
                    // Keep it aligned to the right on resize
                    this.SizeChanged += (s2, e2) =>
                    {
                        try
                        {
                            if (_propertyEditor != null)
                            {
                                _propertyEditor.X = Math.Max(8, this.Width - _propertyEditor.Width - 10);
                                _propertyEditor.Height = Math.Max(200, this.Height - 80);
                                _skControl?.Invalidate();
                            }
                        }
                        catch { }
                    };

                    // On Save: apply NodeProperties back to nodes then invalidate for visual feedback
                    _propertyEditor.PropertiesSaved += (s3, e3) =>
                    {
                        try
                        {
                            if (e3?.Component is Beep.Skia.Components.AutomationNode an)
                            {
                                an.ApplyNodeProperties();
                            }
                        }
                        catch { }
                        try { _skControl?.Invalidate(); } catch { }
                    };
                    // Redraw live when editor values change
                    _propertyEditor.PropertyValueChanged += (s3, e3) => { try { _skControl?.Invalidate(); } catch { } };
                    _propertyEditor.PropertiesCancelled += (s3, e3) => { try { _skControl?.Invalidate(); } catch { } };

                    _drawingManager?.AddComponent(_propertyEditor);
                }
                catch { }
            }
            catch { }
            SetupContextMenu();
            _skControl.PaintSurface += SkControl_PaintSurface;
            this.DragEnter += SkiaHostControl_DragEnter;
            this.DragDrop += SkiaHostControl_DragDrop;
        }

        /// <summary>
        /// Creates the in-canvas context menu and wires it to right-click events.
        /// </summary>
        private void SetupContextMenu()
        {
            try
            {
                _contextMenu = new Beep.Skia.Components.ContextMenu
                {
                    IsStatic = true,
                    Visible = false
                };
                _contextMenu.AddStandardItems(_drawingManager);
                _contextMenu.AddContextItem("Export PNG…", (s, e) => { _contextMenu.Hide(); PromptExportPng(); });
                _contextMenu.AddContextItem("Export SVG…", (s, e) => { _contextMenu.Hide(); PromptExportSvg(); });
                _contextMenu.AddContextItem("Export PDF…", (s, e) => { _contextMenu.Hide(); PromptExportPdf(); });
                _contextMenu.AddContextItem("Print…", (s, e) => { _contextMenu.Hide(); PrintWithPreview(); });
                _drawingManager.AddComponent(_contextMenu);
                // The menu is chrome, not user content; keep it out of undo history.
                _drawingManager.HistoryManager.Clear();

                _drawingManager.ComponentRightClicked += (s, e) => ShowContextMenuAt(e.CanvasPosition);
                _drawingManager.LineRightClicked += (s, e) => ShowContextMenuAt(e.CanvasPosition);
                _drawingManager.DiagramRightClicked += (s, e) => ShowContextMenuAt(e.CanvasPosition);
            }
            catch { }
        }

        private void ShowContextMenuAt(SKPoint canvasPoint)
        {
            try
            {
                if (_contextMenu == null) return;
                _contextMenu.Show(canvasPoint);
                _skControl?.Invalidate();
            }
            catch { }
        }

        // ── Collaboration ─────────────────────────────────────────────────────

        /// <summary>Rebuilds comment pins from the current components and comments.</summary>
        public void RebuildCommentPins()
        {
            try
            {
                _commentPins.Rebuild(
                    _drawingManager?.GetComponents() ?? (IReadOnlyList<Beep.Skia.SkiaComponent>)Array.Empty<Beep.Skia.SkiaComponent>(),
                    _collaboration?.AllComments ?? (IReadOnlyList<Beep.Skia.Collaboration.DiagramComment>)Array.Empty<Beep.Skia.Collaboration.DiagramComment>());
            }
            catch { }
        }

        /// <summary>Rebuilds comment pins and repaints the canvas.</summary>
        public void RefreshCommentPins()
        {
            RebuildCommentPins();
            _skControl?.Invalidate();
        }

        /// <summary>
        /// Adds a comment anchored to the first selected component.
        /// Returns null when nothing is selected or the user cannot comment.
        /// </summary>
        public Beep.Skia.Collaboration.DiagramComment AddCommentToSelection(string text, string authorId = "local")
        {
            try
            {
                var selected = _drawingManager?.SelectionManager?.SelectedComponents;
                var component = selected?.FirstOrDefault();
                if (component == null) return null;

                EnsureLocalUser(authorId);
                var comment = Collaboration.AddComment(DocumentId, authorId, text, component.Name ?? component.Id.ToString());
                RefreshCommentPins();
                return comment;
            }
            catch { return null; }
        }

        /// <summary>Resolves a comment by id and repaints.</summary>
        public bool ResolveComment(string commentId, string userId = "local")
        {
            var resolved = Collaboration.ResolveComment(DocumentId, commentId, userId);
            if (resolved) RefreshCommentPins();
            return resolved;
        }

        private void EnsureLocalUser(string userId)
        {
            var service = Collaboration;
            if (service.GetUser(userId) == null)
            {
                service.RegisterUser(new Beep.Skia.Collaboration.CollaborationUser
                {
                    Id = userId,
                    DisplayName = userId
                });
            }
            if (service.GetShare(DocumentId) == null)
                service.Share(DocumentId, userId);
        }

        // ── Export / Print ────────────────────────────────────────────────────

        /// <summary>
        /// Exports the current diagram to a PNG file.
        /// </summary>
        public void ExportToPng(string filePath, float scale = 2f)
        {
            _drawingManager?.ExportToPng(filePath, scale);
        }

        /// <summary>
        /// Exports the current diagram to an SVG file with a white background.
        /// </summary>
        public void ExportToSvg(string filePath)
        {
            _drawingManager?.ExportToSvg(filePath, background: SKColors.White);
        }

        /// <summary>
        /// Exports the current diagram to a PDF file.
        /// </summary>
        public void ExportToPdf(string filePath)
        {
            _drawingManager?.ExportToPdf(filePath);
        }

        /// <summary>
        /// Shows a print preview for the current diagram.
        /// </summary>
        public void PrintWithPreview()
        {
            try
            {
                if (_drawingManager == null) return;
                var doc = _drawingManager.CreatePrintDocument("Beep.Skia Diagram");
                using var preview = new PrintPreviewDialog
                {
                    Document = doc,
                    Width = 1000,
                    Height = 700,
                    Text = "Print Preview"
                };
                preview.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Print failed: {ex.Message}", "Print", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void PromptExportPng()
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Export PNG",
                Filter = "PNG image (*.png)|*.png",
                FileName = $"skia_diagram_{DateTime.Now:yyyyMMdd_HHmmss}.png"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try { ExportToPng(dlg.FileName); } catch (Exception ex) { MessageBox.Show(ex.Message, "Export PNG"); }
        }

        private void PromptExportSvg()
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Export SVG",
                Filter = "SVG image (*.svg)|*.svg",
                FileName = $"skia_diagram_{DateTime.Now:yyyyMMdd_HHmmss}.svg"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try { ExportToSvg(dlg.FileName); } catch (Exception ex) { MessageBox.Show(ex.Message, "Export SVG"); }
        }

        private void PromptExportPdf()
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Export PDF",
                Filter = "PDF document (*.pdf)|*.pdf",
                FileName = $"skia_diagram_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try { ExportToPdf(dlg.FileName); } catch (Exception ex) { MessageBox.Show(ex.Message, "Export PDF"); }
        }

        /// <summary>
        /// Design-time canvas preview — renders a placeholder with component count and grid.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!this.DesignMode) return;

            var g = e.Graphics;
            var rect = this.ClientRectangle;

            // Background
            using var bgBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(248, 249, 252));
            g.FillRectangle(bgBrush, rect);

            // Grid
            using var gridPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(220, 224, 230), 0.5f);
            for (float x = 0; x < rect.Width; x += 20)
                g.DrawLine(gridPen, x, 0, x, rect.Height);
            for (float y = 0; y < rect.Height; y += 20)
                g.DrawLine(gridPen, 0, y, rect.Width, y);

            // Title
            using var titleFont = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold);
            using var titleBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(100, 100, 120));
            g.DrawString("Beep.Skia Host", titleFont, titleBrush, 12, 12);

            // Component count
            if (_designTimeComponents != null && _designTimeComponents.Count > 0)
            {
                using var countFont = new System.Drawing.Font("Segoe UI", 9);
                using var countBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(80, 80, 100));
                g.DrawString($"{_designTimeComponents.Count} design-time component(s)", countFont, countBrush, 12, 32);

                // Draw component placeholders as labeled rectangles
                using var compPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(100, 120, 180), 1f);
                using var compFill = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(220, 230, 248));
                using var compFont = new System.Drawing.Font("Segoe UI", 7);

                foreach (var desc in _designTimeComponents)
                {
                    var cr = new System.Drawing.Rectangle(
                        (int)desc.X, (int)desc.Y,
                        Math.Max(40, (int)desc.Width), Math.Max(20, (int)desc.Height));
                    if (cr.Right > rect.Width || cr.Bottom > rect.Height) continue;
                    if (cr.X < 0 || cr.Y < 0) continue;

                    g.FillRectangle(compFill, cr);
                    g.DrawRectangle(compPen, cr);
                    var typeName = desc.ComponentType?.Split(',').FirstOrDefault()?.Split('.').LastOrDefault() ?? "Comp";
                    g.DrawString(typeName, compFont, System.Drawing.Brushes.DarkSlateGray, cr.X + 2, cr.Y + 2);
                }
            }
            else
            {
                using var hintFont = new System.Drawing.Font("Segoe UI", 9);
                using var hintBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(160, 160, 180));
                g.DrawString("Drop Skia components from the toolbox, or use Templates in smart-tag", hintFont, hintBrush, 12, 32);
            }

            // Border
            using var borderPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(180, 185, 195), 1f);
            g.DrawRectangle(borderPen, 0, 0, rect.Width - 1, rect.Height - 1);
        }

        /// <summary>
        /// Gets or sets the begin init.
        /// </summary>
        public void BeginInit()
        {
            // no-op
        }

    /// <summary>
    /// Gets or sets the end init.
    /// </summary>
    public void EndInit()
        {
            // At runtime, after designer serialization, instantiate any descriptors into real components
            if (_designDescriptorsInstantiated) return;
            try
            {
                if (this.Site == null || this.Site.DesignMode == false)
                {
                    foreach (var desc in DesignTimeComponents)
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(desc.ComponentType)) continue;
                            var t = Type.GetType(desc.ComponentType);
                            if (t == null) continue;
                            if (!typeof(Beep.Skia.SkiaComponent).IsAssignableFrom(t)) continue;

                            var obj = Activator.CreateInstance(t) as Beep.Skia.SkiaComponent;
                            if (obj == null) continue;

                            // Set basic layout properties if available
                            try { obj.X = desc.X; } catch { }
                            try { obj.Y = desc.Y; } catch { }
                            try { obj.Width = desc.Width; } catch { }
                            try { obj.Height = desc.Height; } catch { }
                            // Apply any property bag values
                            try { ApplyPropertyBagToObject(obj, desc.PropertyBag); } catch { }
                            // Ensure component has a unique name when descriptor didn't include one
                            var nameToUse = desc.Name;
                            if (string.IsNullOrEmpty(nameToUse))
                            {
                                try
                                {
                                    nameToUse = GenerateUniqueNameFor(t.Name);
                                    desc.Name = nameToUse; // update descriptor so subsequent operations see the name
                                }
                                catch { nameToUse = string.Empty; }
                            }
                            try { obj.Name = nameToUse ?? string.Empty; } catch { }
                            try
                            {
                                var msg = $"Instantiating descriptor: Type={t.FullName} Name={nameToUse} RequestedX={desc.X},RequestedY={desc.Y},W={desc.Width},H={desc.Height}";
                                Console.WriteLine(msg);
                                try { System.Diagnostics.Debug.WriteLine(msg); } catch { }
                            }
                            catch { }

                                _drawingManager?.AddComponent(obj);
                            try
                            {
                                var msg = $"Descriptor instantiated: Type={t.FullName} Name={obj.Name} FinalX={obj.X},FinalY={obj.Y},W={obj.Width},H={obj.Height}";
                                Console.WriteLine(msg);
                                try { System.Diagnostics.Debug.WriteLine(msg); } catch { }
                            }
                            catch { }
                        }
                        catch
                        {
                            // ignore failures for individual descriptors
                        }
                    }

                    _skControl?.Invalidate();
                }
            }
            catch
            {
                // swallow overall errors during initialization
            }
            finally
            {
                _designDescriptorsInstantiated = true;
            }
        }

        /// <summary>
        /// Gets or sets the design time components.
        /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Description("Components hosted by this Skia host at design time.")]
        public SkiaComponentDescriptorCollection DesignTimeComponents
        {
            get => _designTimeComponents;
            set => _designTimeComponents = value ?? new SkiaComponentDescriptorCollection();
        }

    /// <summary>
    /// The in-host Palette component (if created).
    /// </summary>
    [Browsable(false)]
    public Palette Palette => _palette;

        /// <summary>
        /// If true, component creation will be blocked when the new component's bounds overlap an existing component.
        /// Default false: overlaps allowed (prevents silent disappearance when dropping in same area).
        /// </summary>
        [Category("Behavior"), DefaultValue(false)]
        public bool BlockOverlappingDrops { get; set; } = false;
    /// <summary>
    /// If true (default), each drop operation will force generation of a unique component Name even if a name was supplied.
    /// Prevents confusion where repeated drops with the same descriptor name appear to replace earlier components.
    /// </summary>
    [Category("Behavior"), DefaultValue(true)]
    public bool AlwaysUniqueNamesOnDrop { get; set; } = true;
    /// <summary>
    /// If true (default), when a new component is created and overlaps an existing one (and overlaps are allowed),
    /// the new component will be auto-offset (diagonally) until it no longer overlaps, up to a max number of attempts.
    /// This prevents the visual illusion that the previous component "disappeared" when it was only covered.
    /// </summary>
    [Category("Behavior"), DefaultValue(true)]
    public bool AutoOffsetOnOverlap { get; set; } = true;

        // Helper used by the generated InitializeComponent code to construct and add
        // a Skia component directly (this is what the custom serializer emits).
        /// <summary>
        /// Gets or sets the create and add component.
        /// </summary>
        [Browsable(false)]
        public Beep.Skia.SkiaComponent CreateAndAddComponent(Type componentType, float x, float y, float width, float height, string name)
        {
            if (componentType == null) return null;
            try
            {
                if (!typeof(Beep.Skia.SkiaComponent).IsAssignableFrom(componentType)) return null;
                var obj = Activator.CreateInstance(componentType) as Beep.Skia.SkiaComponent;
                if (obj == null) return null;

                // Reuse detection: if an existing component already has this Id (should not happen unless factory returns singleton)
                try
                {
                    var mgrForReuse = this.DrawingManager;
                    if (mgrForReuse != null && mgrForReuse.GetComponents().Any(c => c.Id == obj.Id))
                    {
                        var msgReuse = $"[CreateAndAddComponent] Detected reused instance for type {componentType.FullName}. Cloning new instance.";
                        Console.WriteLine(msgReuse);
                        try { System.Diagnostics.Debug.WriteLine(msgReuse); } catch { }
                        obj = Activator.CreateInstance(componentType) as Beep.Skia.SkiaComponent; // try again fresh
                    }
                }
                catch { }

                try { System.Diagnostics.Debug.WriteLine($"CreateAndAddComponent requested: Type={componentType.FullName} ReqX={x},ReqY={y},W={width},H={height},Name={name}"); } catch { }

                // Log initial coordinates right after creation
                try { System.Diagnostics.Debug.WriteLine($"CreateAndAddComponent AfterCreation: Type={componentType.FullName} X={obj.X},Y={obj.Y},W={obj.Width},H={obj.Height}"); } catch { }

                // Assign properties defensively
                try { obj.X = x; } catch { }
                try { obj.Y = y; } catch { }
                try { obj.Width = width; } catch { }
                try { obj.Height = height; } catch { }

                // Log coordinates after assignment
                try { System.Diagnostics.Debug.WriteLine($"CreateAndAddComponent AfterAssignment: Type={componentType.FullName} X={obj.X},Y={obj.Y},W={obj.Width},H={obj.Height}"); } catch { }
                // If the caller passed a descriptor name that may encode additional properties, nothing else here.

                // Ensure unique name (optional force) to avoid same-identity replacement illusions
                if (AlwaysUniqueNamesOnDrop || string.IsNullOrEmpty(name) || NameExists(name))
                {
                    name = GenerateUniqueNameFor(descriptorBaseName: componentType.Name);
                }
                try { obj.Name = name ?? string.Empty; } catch { }

                // No descriptor property bag passed here; caller can retrieve the returned object and set properties.

                // Optional overlap blocking or auto-offset
                try
                {
                    var mgr = this.DrawingManager;
                    if (mgr != null)
                    {
                        int attempts = 0;
                        const int maxAttempts = 25;
                        bool hasOverlap;
                        do
                        {
                            hasOverlap = false;
                            foreach (var ecomp in mgr.GetComponents())
                            {
                                try
                                {
                                    if (ReferenceEquals(ecomp, obj)) continue;
                                    var er = new SKRect(ecomp.X, ecomp.Y, ecomp.X + ecomp.Width, ecomp.Y + ecomp.Height);
                                    var desiredRect = new SKRect(obj.X, obj.Y, obj.X + obj.Width, obj.Y + obj.Height);
                                    if (er.IntersectsWith(desiredRect))
                                    {
                                        if (BlockOverlappingDrops)
                                        {
                                            try { Console.WriteLine($"[CreateAndAddComponent] BLOCKED overlap at X={obj.X},Y={obj.Y},W={obj.Width},H={obj.Height}"); } catch { }
                                            return null;
                                        }
                                        if (AutoOffsetOnOverlap)
                                        {
                                            obj.X += 20f;
                                            obj.Y += 20f;
                                            hasOverlap = true;
                                            attempts++;
                                            break;
                                        }
                                    }
                                }
                                catch { }
                            }
                        } while (hasOverlap && AutoOffsetOnOverlap && attempts < maxAttempts);
                    }
                }
                catch { }

                    _drawingManager?.AddComponent(obj);
                    try { _componentRegistry[obj.Id] = obj; } catch { }
                try { System.Diagnostics.Debug.WriteLine($"CreateAndAddComponent created: Type={componentType.FullName} Name={obj.Name} FinalX={obj.X},FinalY={obj.Y},W={obj.Width},H={obj.Height}"); } catch { }
                _skControl?.Invalidate();
                return obj;
            }
            catch
            {
                return null;
            }
        }

        // Public helper to create and add a component from a descriptor, applying its PropertyBag.
        /// <summary>
        /// Gets or sets the create and add component from descriptor.
        /// </summary>
        public Beep.Skia.SkiaComponent CreateAndAddComponentFromDescriptor(SkiaComponentDescriptor desc)
        {
            if (desc == null) return null;
            try
            {
                try { System.Diagnostics.Debug.WriteLine($"CreateAndAddComponentFromDescriptor requested: Type={desc.ComponentType} ReqX={desc.X},ReqY={desc.Y},W={desc.Width},H={desc.Height},Name={desc.Name}"); } catch { }
                if (string.IsNullOrEmpty(desc.ComponentType)) return null;
                var t = Type.GetType(desc.ComponentType);
                if (t == null) return null;
                if (!typeof(Beep.Skia.SkiaComponent).IsAssignableFrom(t)) return null;

                var obj = Activator.CreateInstance(t) as Beep.Skia.SkiaComponent;
                if (obj == null) return null;

                // Reuse detection: ensure a brand-new instance (guard against singleton-style implementations)
                try
                {
                    var mgrForReuse = this.DrawingManager;
                    if (mgrForReuse != null && mgrForReuse.GetComponents().Any(c => c.Id == obj.Id))
                    {
                        var msgReuse = $"[CreateAndAddComponentFromDescriptor] Detected reused instance for type {t.FullName}. Cloning new instance.";
                        Console.WriteLine(msgReuse);
                        try { System.Diagnostics.Debug.WriteLine(msgReuse); } catch { }
                        obj = Activator.CreateInstance(t) as Beep.Skia.SkiaComponent; // attempt second instantiation
                    }
                }
                catch { }

                try { Console.WriteLine($"Descriptor requested X={desc.X}, Y={desc.Y}, W={desc.Width}, H={desc.Height}"); } catch { }
                try { obj.X = desc.X; } catch { }
                try { obj.Y = desc.Y; } catch { }
                try { obj.Width = desc.Width; } catch { }
                try { obj.Height = desc.Height; } catch { }
                try { System.Diagnostics.Debug.WriteLine($"After apply: component X={obj.X}, Y={obj.Y}, W={obj.Width}, H={obj.Height}"); } catch { }

                var nameToUse = desc.Name;
                if (AlwaysUniqueNamesOnDrop || string.IsNullOrEmpty(nameToUse) || NameExists(nameToUse))
                {
                    nameToUse = GenerateUniqueNameFor(t.Name);
                    desc.Name = nameToUse;
                }
                try { obj.Name = nameToUse ?? string.Empty; } catch { }

                try { ApplyPropertyBagToObject(obj, desc.PropertyBag); } catch { }

                try
                {
                    var mgr = this.DrawingManager;
                    if (mgr != null)
                    {
                        int attempts = 0;
                        const int maxAttempts = 25;
                        bool hasOverlap;
                        do
                        {
                            hasOverlap = false;
                            foreach (var ecomp in mgr.GetComponents())
                            {
                                try
                                {
                                    if (ReferenceEquals(ecomp, obj)) continue;
                                    var er = new SKRect(ecomp.X, ecomp.Y, ecomp.X + ecomp.Width, ecomp.Y + ecomp.Height);
                                    var desiredRect = new SKRect(obj.X, obj.Y, obj.X + obj.Width, obj.Y + obj.Height);
                                    if (er.IntersectsWith(desiredRect))
                                    {
                                        if (BlockOverlappingDrops)
                                        {
                                            try { Console.WriteLine($"[CreateAndAddComponentFromDescriptor] BLOCKED overlap at X={obj.X},Y={obj.Y}"); } catch { }
                                            return null;
                                        }
                                        if (AutoOffsetOnOverlap)
                                        {
                                            obj.X += 20f;
                                            obj.Y += 20f;
                                            hasOverlap = true;
                                            attempts++;
                                            break;
                                        }
                                    }
                                }
                                catch { }
                            }
                        } while (hasOverlap && AutoOffsetOnOverlap && attempts < maxAttempts);
                    }
                }
                catch { }
                _drawingManager?.AddComponent(obj);
                try { _componentRegistry[obj.Id] = obj; } catch { }
                try { System.Diagnostics.Debug.WriteLine($"CreateAndAddComponentFromDescriptor created: Type={t.FullName} Name={obj.Name} FinalX={obj.X},FinalY={obj.Y},W={obj.Width},H={obj.Height}"); } catch { }
                _skControl?.Invalidate();
                return obj;
            }
            catch { return null; }
        }

        // Generate a simple unique name using existing design-time component names and the host's collection
        private string GenerateUniqueNameFor(string descriptorBaseName)
        {
            if (string.IsNullOrEmpty(descriptorBaseName)) descriptorBaseName = "SkiaComponent";
            var baseName = "skia" + descriptorBaseName;
            int i = 1;

            bool Exists(string n)
            {
                foreach (var d in DesignTimeComponents)
                {
                    if (string.Equals(d.Name, n, StringComparison.OrdinalIgnoreCase)) return true;
                }
                // also check container components if available (design time)
                try
                {
                    var host = this.Site?.GetService(typeof(System.ComponentModel.Design.IDesignerHost)) as System.ComponentModel.Design.IDesignerHost;
                    if (host != null)
                    {
                        foreach (System.ComponentModel.IComponent c in host.Container.Components)
                        {
                            var nprop = c.Site?.Name;
                            if (!string.IsNullOrEmpty(nprop) && string.Equals(nprop, n, StringComparison.OrdinalIgnoreCase)) return true;
                        }
                    }
                }
                catch { }

                return false;
            }

            var name = baseName + i.ToString();
            while (Exists(name))
            {
                i++;
                name = baseName + i.ToString();
            }
            return name;
        }

        private bool NameExists(string proposed)
        {
            if (string.IsNullOrEmpty(proposed)) return false;
            try
            {
                foreach (var d in DesignTimeComponents)
                {
                    if (string.Equals(d.Name, proposed, StringComparison.OrdinalIgnoreCase)) return true;
                }
                var mgr = this.DrawingManager;
                if (mgr != null)
                {
                    foreach (var c in mgr.GetComponents())
                    {
                        try
                        {
                            if (string.Equals(c.Name, proposed, StringComparison.OrdinalIgnoreCase)) return true;
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return false;
        }

        // Apply a string-based property bag to an object's public writable properties.
        private void ApplyPropertyBagToObject(object obj, System.Collections.Generic.Dictionary<string, string> bag)
        {
            if (obj == null || bag == null || bag.Count == 0) return;

            var type = obj.GetType();
            foreach (var kv in bag)
            {
                try
                {
                    var prop = type.GetProperty(kv.Key);
                    if (prop == null || !prop.CanWrite) continue;
                    var targetType = prop.PropertyType;
                    if (TryConvert(kv.Value, targetType, out var converted))
                    {
                        prop.SetValue(obj, converted);
                    }
                }
                catch { }
            }
        }

        // Limited conversion from string to common types (string, float, int, bool, SKColor)
    private bool TryConvert(string s, Type targetType, out object result)
        {
            result = null;
            if (targetType == typeof(string)) { result = s; return true; }
            if (targetType == typeof(float) || targetType == typeof(float?))
            {
                if (float.TryParse(s, out var f)) { result = f; return true; }
                return false;
            }
            if (targetType == typeof(int) || targetType == typeof(int?))
            {
                if (int.TryParse(s, out var i)) { result = i; return true; }
                return false;
            }
            if (targetType == typeof(bool) || targetType == typeof(bool?))
            {
                if (bool.TryParse(s, out var b)) { result = b; return true; }
                return false;
            }
            // SKColor from hex strings like #RRGGBB or #AARRGGBB
            if (targetType == typeof(SKColor) || targetType == typeof(SKColor?))
            {
                try
                {
                    var str = s?.Trim();
                    if (string.IsNullOrEmpty(str)) return false;
                    if (str.StartsWith("#")) str = str.Substring(1);
                    uint v = Convert.ToUInt32(str, 16);
                    if (str.Length == 6)
                    {
                        // assume RRGGBB
                        v |= 0xFF000000u;
                    }
                    result = new SKColor((byte)((v >> 16) & 0xFF), (byte)((v >> 8) & 0xFF), (byte)(v & 0xFF), (byte)((v >> 24) & 0xFF));
                    return true;
                }
                catch { return false; }
            }

            return false;
        }

        /// <summary>
        /// Checks if DDL export was triggered for an ERD entity and shows a dialog with the result.
        /// </summary>
        private void CheckForDDLExport(Beep.Skia.SkiaComponent component)
        {
            try
            {
                if (component?.Tag is { } tag)
                {
                    var tagType = tag.GetType();
                    var ddlProp = tagType.GetProperty("DDL");
                    var errorProp = tagType.GetProperty("Error");
                    
                    if (ddlProp != null)
                    {
                        var ddl = ddlProp.GetValue(tag) as string;
                        if (!string.IsNullOrWhiteSpace(ddl))
                        {
                            // Show dialog with DDL and copy to clipboard
                            Clipboard.SetText(ddl);
                            MessageBox.Show("DDL copied to clipboard!", "Export DDL", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            // Clear Tag after showing
                            component.Tag = null;
                        }
                    }
                    else if (errorProp != null)
                    {
                        var error = errorProp.GetValue(tag) as string;
                        if (!string.IsNullOrWhiteSpace(error))
                        {
                            MessageBox.Show($"Export failed: {error}", "Export DDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            // Clear Tag after showing
                            component.Tag = null;
                        }
                    }
                }
            }
            catch { }
        }

        private void InitializeSkiaSurface()
        {
            _skControl = new SKControl { Dock = DockStyle.Fill };
            _skControl.MouseDown += SkControl_MouseDown;
            _skControl.MouseMove += SkControl_MouseMove;
            _skControl.MouseUp += SkControl_MouseUp;
            _skControl.MouseWheel += SkControl_MouseWheel;
            // Keyboard routing to property editor
            _skControl.KeyDown += SkControl_KeyDown;
            _skControl.KeyPress += SkControl_KeyPress;
            _skControl.TabStop = true;
            _skControl.Focus();
            this.Controls.Add(_skControl);
        }

        private static SKKeyModifiers GetModifiers()
        {
            SKKeyModifiers m = SKKeyModifiers.None;
            if (Control.ModifierKeys.HasFlag(Keys.Control)) m |= SKKeyModifiers.Control;
            if (Control.ModifierKeys.HasFlag(Keys.Shift)) m |= SKKeyModifiers.Shift;
            if (Control.ModifierKeys.HasFlag(Keys.Alt)) m |= SKKeyModifiers.Alt;
            return m;
        }

        private void SkControl_MouseDown(object sender, MouseEventArgs e)
        {
            var pt = new SKPoint(e.X, e.Y);
            try { _skControl.Focus(); } catch { }
            // Convert WinForms button to int (0=left, 1=right, 2=middle)
            int mouseButton = e.Button switch
            {
                MouseButtons.Left => 0,
                MouseButtons.Right => 1,
                MouseButtons.Middle => 2,
                _ => 0
            };
            // Route all input to the DrawingManager
            _drawingManager.HandleMouseDown(pt, GetModifiers(), mouseButton);

            if (!AllowComponentDragging || e.Button != MouseButtons.Left)
                return;

            try
            {
                var canvasPoint = ClientToCanvas(pt);
                // Find topmost component under cursor (reverse drawing order)
                var comps = _drawingManager?.GetComponents()?.ToList();
                if (comps != null)
                {
                    for (int i = comps.Count - 1; i >= 0; i--)
                    {
                        var c = comps[i];
                        if (c == null) continue;
                        var rect = new SKRect(c.X, c.Y, c.X + c.Width, c.Y + c.Height);
                        if (rect.Contains(canvasPoint))
                        {
                            _dragComponent = c;
                            // Respect IsStatic components: do not start drag
                            try { if (_dragComponent.IsStatic) { _dragComponent = null; break; } } catch { }
                            _isDraggingComponent = true;
                            _dragStartCanvas = canvasPoint;
                            _dragComponentStartX = c.X;
                            _dragComponentStartY = c.Y;
                            // Bring to front without removing lines
                            try { _drawingManager.BringToFront(c); } catch { }
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        private void SkControl_MouseMove(object sender, MouseEventArgs e)
        {
            var pt = new SKPoint(e.X, e.Y);
            _drawingManager.HandleMouseMove(pt, GetModifiers());

            if (!_isDraggingComponent || _dragComponent == null || !AllowComponentDragging)
                return;
            try
            {
                var canvasPoint = ClientToCanvas(pt);
                float dx = canvasPoint.X - _dragStartCanvas.X;
                float dy = canvasPoint.Y - _dragStartCanvas.Y;
                _dragComponent.X = _dragComponentStartX + dx;
                _dragComponent.Y = _dragComponentStartY + dy;
                // Keep within non-negative canvas for basic safety
                if (_dragComponent.X < 0) _dragComponent.X = 0;
                if (_dragComponent.Y < 0) _dragComponent.Y = 0;
                _skControl.Invalidate();
            }
            catch { }
        }

        private void SkControl_MouseUp(object sender, MouseEventArgs e)
        {
            var pt = new SKPoint(e.X, e.Y);
            // Convert WinForms button to int (0=left, 1=right, 2=middle)
            int mouseButton = e.Button switch
            {
                MouseButtons.Left => 0,
                MouseButtons.Right => 1,
                MouseButtons.Middle => 2,
                _ => 0
            };
            _drawingManager.HandleMouseUp(pt, GetModifiers(), mouseButton);

            if (e.Button == MouseButtons.Left && _isDraggingComponent)
            {
                _isDraggingComponent = false;
                _dragComponent = null;
            }
        }

        private void SkControl_MouseWheel(object sender, MouseEventArgs e)
        {
            // Windows Forms mouse wheel Delta is in e.Delta
            // ComponentManager currently doesn't expose a wheel handler; fall back to drawing manager
            _drawingManager.HandleMouseWheel(new SKPoint(e.X, e.Y), e.Delta);
        }

        private void SkControl_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                // When the in-canvas property editor has a target, give it navigation/editing keys first.
                bool editorActive = _propertyEditor != null &&
                                    (_propertyEditor.SelectedComponent != null || _propertyEditor.SelectedLine != null);

                if (editorActive)
                {
                    Beep.Skia.Components.PropertyEditorKey? key = e.KeyCode switch
                    {
                        Keys.Left => Beep.Skia.Components.PropertyEditorKey.Left,
                        Keys.Right => Beep.Skia.Components.PropertyEditorKey.Right,
                        Keys.Back => Beep.Skia.Components.PropertyEditorKey.Back,
                        Keys.Delete => Beep.Skia.Components.PropertyEditorKey.Delete,
                        Keys.Home => Beep.Skia.Components.PropertyEditorKey.Home,
                        Keys.End => Beep.Skia.Components.PropertyEditorKey.End,
                        _ => null
                    };
                    if (key.HasValue)
                    {
                        _propertyEditor.HandleKeyDown(key.Value);
                        _skControl.Invalidate();
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        return;
                    }
                }

                // Diagram-wide shortcuts (undo/redo, copy/paste, delete, arrows, zoom, grid, theme).
                int mods = 0;
                if (e.Control) mods |= 1;
                if (e.Shift) mods |= 2;
                if (e.Alt) mods |= 4;

                if (_drawingManager.HandleKeyDown((int)e.KeyCode, mods))
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    _skControl?.Invalidate();
                }
            }
            catch { }
        }

        private void SkControl_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (_propertyEditor == null) return;
                _propertyEditor.HandleKeyChar(e.KeyChar);
                _skControl.Invalidate();
            }
            catch { }
        }

        /// <summary>
        /// Intercepts Tab / Shift+Tab for keyboard selection cycling before WinForms dialog navigation.
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            try
            {
                if ((keyData & Keys.KeyCode) == Keys.Tab && _drawingManager != null)
                {
                    _drawingManager.SelectNextComponent((keyData & Keys.Shift) == 0);
                    _skControl?.Invalidate();
                    return true;
                }
            }
            catch { }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void SkControl_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            // Clear the canvas and let the DrawingManager render
            canvas.Clear(SKColors.White);
            _drawingManager.Canvas = canvas;
            try
            {
                _drawingManager.Draw(canvas);
            }
            catch { }
        }

        // Convert mouse point in SKControl client coords to canvas coords accounting for pan & zoom
        private SKPoint ClientToCanvas(SKPoint clientPoint)
        {
            var mgr = _drawingManager;
            if (mgr == null) return clientPoint;
            try
            {
                var offset = new SKPoint(clientPoint.X - mgr.PanOffset.X, clientPoint.Y - mgr.PanOffset.Y);
                if (mgr.Zoom != 0f) return new SKPoint(offset.X / mgr.Zoom, offset.Y / mgr.Zoom);
                return offset;
            }
            catch { return clientPoint; }
        }

        private void SkiaHostControl_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Control)))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void SkiaHostControl_DragDrop(object sender, DragEventArgs e)
        {
            // Convert screen coordinates to the inner SKControl's client coordinates
            var clientPoint = (_skControl != null)
                ? _skControl.PointToClient(new System.Drawing.Point(e.X, e.Y))
                : this.PointToClient(new System.Drawing.Point(e.X, e.Y));
            var skPoint = new SKPoint(clientPoint.X, clientPoint.Y);
            try
            {
                var msg0 = $"[DragDrop] RAW screen=({e.X},{e.Y}) client=({clientPoint.X},{clientPoint.Y})";
                System.Console.WriteLine(msg0);
                try { System.Diagnostics.Debug.WriteLine(msg0); } catch { }
            }
            catch { }

            if (e.Data.GetDataPresent(typeof(Control)))
            {
                var c = e.Data.GetData(typeof(Control)) as Control;
                // If it's a Skia wrapper, create the underlying Skia component and add it to the manager
                if (c is SkiaControl sc && sc.SkiaComponent != null)
                {
                    // Build a descriptor and delegate creation to the host's descriptor-based factory
                    // Convert the drop point (client pixels) into canvas coordinates considering pan/zoom
                    var canvasPoint = new SKPoint(skPoint.X, skPoint.Y);
                    try
                    {
                        var mgr = this.DrawingManager;
                        if (mgr != null)
                        {
                            try
                            {
                                var preMsg = $"[DragDrop] PreTransform canvasPoint=({canvasPoint.X},{canvasPoint.Y}) PanOffset={mgr.PanOffset} Zoom={mgr.Zoom}";
                                System.Console.WriteLine(preMsg);
                                try { System.Diagnostics.Debug.WriteLine(preMsg); } catch { }
                            }
                            catch { }
                            var offset = new SKPoint(canvasPoint.X - mgr.PanOffset.X, canvasPoint.Y - mgr.PanOffset.Y);
                            canvasPoint = new SKPoint(offset.X / mgr.Zoom, offset.Y / mgr.Zoom);
                            try
                            {
                                var postMsg = $"[DragDrop] PostTransform canvasPoint=({canvasPoint.X},{canvasPoint.Y})";
                                System.Console.WriteLine(postMsg);
                                try { System.Diagnostics.Debug.WriteLine(postMsg); } catch { }
                            }
                            catch { }
                        }
                    }
                    catch { }

                    // Use the prototype to read default size and name, but create via descriptor to ensure a fresh instance
                    var proto = sc.SkiaComponent;
                    float w = proto?.Width ?? 120f;
                    float h = proto?.Height ?? 36f;
                    string typeName = proto?.GetType().AssemblyQualifiedName;
                    string name = proto?.Name ?? string.Empty;

                    // Placement: either center on cursor or use cursor as top-left
                    float newX = CenterOnDrop ? canvasPoint.X - (w / 2f) : canvasPoint.X;
                    float newY = CenterOnDrop ? canvasPoint.Y - (h / 2f) : canvasPoint.Y;
                    newX = Math.Max(0f, newX);
                    newY = Math.Max(0f, newY);

                    try
                    {
                        var posMsg = $"[DragDrop] DescriptorPlacement Type={typeName} W={w} H={h} newX={newX} newY={newY} CenterOnDrop={CenterOnDrop}";
                        System.Console.WriteLine(posMsg);
                        try { System.Diagnostics.Debug.WriteLine(posMsg); } catch { }
                    }
                    catch { }

                    var desc = new SkiaComponentDescriptor
                    {
                        ComponentType = typeName,
                        X = newX,
                        Y = newY,
                        Width = w,
                        Height = h,
                        Name = name
                    };

                    // Use the unified creation path which applies property bags and logging and performs overlap checks
                    var created = CreateAndAddComponentFromDescriptor(desc);
                    if (created == null)
                    {
                        try { Console.WriteLine($"DragDrop: creation blocked or failed for type {typeName} at X={newX},Y={newY}"); } catch { }
                    }
                    else
                    {
                        // Enforce final position in case any constructor/update logic reset it.
                        try
                        {
                            var before = $"[DragDrop] PostCreate BEFORE adjust: Type={created.GetType().Name} X={created.X} Y={created.Y}";
                            Console.WriteLine(before);
                            try { System.Diagnostics.Debug.WriteLine(before); } catch { }

                            created.X = newX;
                            created.Y = newY;

                            var after = $"[DragDrop] PostCreate AFTER adjust: Type={created.GetType().Name} ForcedX={created.X} ForcedY={created.Y}";
                            Console.WriteLine(after);
                            try { System.Diagnostics.Debug.WriteLine(after); } catch { }
                        }
                        catch { }
                    }
                    _skControl.Invalidate();
                }
            }
        }

        /// <summary>
        /// Synchronizes the VS PropertyGrid with the selected Skia component at design time.
        /// Creates a property wrapper and sets it as the primary selection.
        /// </summary>
        private void SyncPropertyGridToSelection(SkiaComponent component)
        {
            if (!DesignMode || component == null) return;
            try
            {
                _selectedComponentWrapper = new SkiaComponentPropertyWrapper(component, _ => _skControl?.Invalidate());
                var selService = this.Site?.GetService(typeof(System.ComponentModel.Design.ISelectionService))
                    as System.ComponentModel.Design.ISelectionService;
                if (selService != null)
                {
                    selService.SetSelectedComponents(new object[] { _selectedComponentWrapper });
                }
            }
            catch { }
        }

        /// <summary>
        /// Shows common editable properties for a multi-component selection in the PropertyGrid.
        /// </summary>
        private void SyncPropertyGridToMultiSelection(System.Collections.Generic.List<SkiaComponent> components)
        {
            if (!DesignMode || components == null || components.Count == 0) return;
            try
            {
                _selectedMultiWrapper?.Dispose();
                _selectedMultiWrapper = new SkiaMultiComponentWrapper(components, _ => _skControl?.Invalidate());
                var selService = this.Site?.GetService(typeof(System.ComponentModel.Design.ISelectionService))
                    as System.ComponentModel.Design.ISelectionService;
                selService?.SetSelectedComponents(new object[] { _selectedMultiWrapper });
            }
            catch { }
        }

        /// <summary>
        /// Clears the PropertyGrid selection wrapper and returns selection to the host control.
        /// </summary>
        private void ClearSelectionWrapper()
        {
            if (!DesignMode) return;
            try
            {
                _selectedComponentWrapper?.Dispose();
                _selectedComponentWrapper = null;
                _selectedMultiWrapper?.Dispose();
                _selectedMultiWrapper = null;
                var selService = this.Site?.GetService(typeof(System.ComponentModel.Design.ISelectionService))
                    as System.ComponentModel.Design.ISelectionService;
                if (selService != null)
                {
                    selService.SetSelectedComponents(new object[] { this });
                }
            }
            catch { }
        }
    }
}

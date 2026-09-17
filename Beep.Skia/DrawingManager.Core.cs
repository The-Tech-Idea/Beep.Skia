using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Versioning;
using Beep.Skia.Model;
namespace Beep.Skia
{
    /// <summary>
    /// Manages the drawing and interaction of workflow components and connections on a Skia canvas.
    /// This class handles component management, connection creation, mouse interactions, and rendering.
    /// Enhanced with drag-and-drop, line manipulation, selection system, and other modern features.
    /// </summary>
    public partial class DrawingManager
    {
        private readonly List<SkiaComponent> _components;
        private readonly List<IConnectionLine> _lines;
        // Centralized connection point registry
        private readonly Dictionary<Guid, IConnectionPoint> _connectionPointsById = new Dictionary<Guid, IConnectionPoint>();
        private readonly Dictionary<IConnectionPoint, SkiaComponent> _ownerByConnectionPoint = new Dictionary<IConnectionPoint, SkiaComponent>();
        private SKPoint _panOffset = SKPoint.Empty;
        private float _zoom = 1.0f;
        private string _clipboardData;

        // Helper classes
        private SelectionManager _selectionManager;
        private InteractionHelper _interactionHelper;
        private RenderingHelper _renderingHelper;
        private HistoryManager _historyManager;
        private DiagramValidator _diagramValidator;

        /// <summary>
        /// Gets the diagram validator. Call AddRule() to customize, then ValidateDiagram() to run.
        /// </summary>
        public DiagramValidator DiagramValidator
        {
            get
            {
                if (_diagramValidator == null)
                {
                    _diagramValidator = new DiagramValidator();
                    _diagramValidator.AddDefaultRules();
                }
                return _diagramValidator;
            }
        }

        /// <summary>
        /// Gets or sets the Skia canvas used for drawing.
        /// </summary>
        public SKCanvas Canvas { get; set; }

        /// <summary>
        /// Gets the list of components (for internal use by helpers).
        /// </summary>
        internal IReadOnlyList<SkiaComponent> Components => _components;

        /// <summary>
        /// Gets the list of connection lines (for internal use by helpers).
        /// </summary>
        internal IReadOnlyList<IConnectionLine> Lines => _lines;

        /// <summary>
        /// Gets or sets the current zoom level.
        /// </summary>
        public float Zoom
        {
            get => _zoom;
            set
            {
                _zoom = Math.Max(0.1f, Math.Min(5.0f, value));
                DrawSurface?.Invoke(this, null);
            }
        }

        /// <summary>
        /// Gets or sets the pan offset for canvas translation.
        /// </summary>
        public SKPoint PanOffset
        {
            get => _panOffset;
            set
            {
                _panOffset = value;
                DrawSurface?.Invoke(this, null);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the grid is visible.
        /// </summary>
        public bool ShowGrid { get; set; } = true;

        /// <summary>
        /// Gets or sets the grid spacing.
        /// </summary>
        public float GridSpacing { get; set; } = 20.0f;

        /// <summary>
        /// Gets or sets a value indicating whether snapping to grid is enabled.
        /// </summary>
        public bool SnapToGrid { get; set; } = true;

        /// <summary>
        /// Gets the selection manager.
        /// </summary>
        public SelectionManager SelectionManager => _selectionManager;

        /// <summary>
        /// Gets the interaction helper.
        /// </summary>
        public InteractionHelper InteractionHelper => _interactionHelper;

        /// <summary>
        /// Gets the rendering helper.
        /// </summary>
        public RenderingHelper RenderingHelper => _renderingHelper;

        /// <summary>
        /// Gets the history manager.
        /// </summary>
        public HistoryManager HistoryManager => _historyManager;

        /// <summary>
        /// Gets a registered connection point by its identifier.
        /// </summary>
        public IConnectionPoint GetConnectionPoint(Guid id)
        {
            return _connectionPointsById.TryGetValue(id, out var cp) ? cp : null;
        }

        private static IConnectionPoint FirstOutPoint(SkiaComponent component)
            => component?.OutConnectionPoints?.FirstOrDefault();

        private static IConnectionPoint FirstInPoint(SkiaComponent component)
            => component?.InConnectionPoints?.FirstOrDefault();

        /// <summary>
        /// Gets the owning component for a connection point.
        /// </summary>
        public SkiaComponent GetOwnerForConnectionPoint(IConnectionPoint point)
        {
            if (point == null) return null;
            return _ownerByConnectionPoint.TryGetValue(point, out var owner) ? owner : point.Component as SkiaComponent;
        }

        /// <summary>
        /// Occurs when the drawing surface needs to be updated.
        /// </summary>
        public event EventHandler<ConnectionEventArgs> DrawSurface;

        /// <summary>
        /// Optional world-space overlay drawn on interactive renders after components, lines,
        /// and selection adorners (pan/zoom transform applied). Use for annotations such as
        /// comment pins. Not included in exports.
        /// </summary>
        public Action<SKCanvas> WorldOverlay { get; set; }

        /// <summary>
        /// Occurs when a component is dropped.
        /// </summary>
        public event EventHandler<ComponentDropEventArgs> ComponentDropped;

        internal void RaiseComponentDropped(ComponentDropEventArgs args)
        {
            ComponentDropped?.Invoke(this, args);
        }

        /// <summary>
        /// Requests a redraw of the drawing surface. This is the safe, public way for
        /// helpers (interaction/rendering/etc.) to trigger a repaint without invoking
        /// the DrawSurface event from outside this type.
        /// </summary>
        public void RequestRedraw()
        {
            try { DrawSurface?.Invoke(this, null); } catch { }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingManager"/> class.
        /// </summary>
        public DrawingManager()
        {
            _components = new List<SkiaComponent>();
            _lines = new List<IConnectionLine>();

            // Initialize helper classes
            _selectionManager = new SelectionManager(this);
            _interactionHelper = new InteractionHelper(this);
            _renderingHelper = new RenderingHelper(this);
            _historyManager = new HistoryManager(this);

            // Wire up events
            _selectionManager.SelectionChanged += (s, e) => SelectionChanged?.Invoke(this, e);
            _historyManager.HistoryChanged += (s, e) => HistoryChanged?.Invoke(this, e);

            // Subscribe to theme changes for redraw
            ThemeManager.ThemeChanged += (s, e) => DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Occurs when the selection changes.
        /// </summary>
        public event EventHandler SelectionChanged;

        /// <summary>
        /// Occurs when the undo/redo history changes.
        /// </summary>
        public event EventHandler HistoryChanged;

        // ========== Interaction Events (LinkedIn-style infographic support) ==========

        /// <summary>
        /// Occurs when a component (node) is clicked.
        /// Use this for external handling like property panels, navigation, etc.
        /// </summary>
        public event EventHandler<ComponentInteractionEventArgs> ComponentClicked;

        /// <summary>
        /// Occurs when a component is double-clicked.
        /// Use this for opening property dialogs, editors, or detailed views.
        /// </summary>
        public event EventHandler<ComponentInteractionEventArgs> ComponentDoubleClicked;

        /// <summary>
        /// Occurs when a component is right-clicked.
        /// Use this for showing context menus with component-specific actions.
        /// </summary>
        public event EventHandler<ComponentInteractionEventArgs> ComponentRightClicked;

        /// <summary>
        /// Occurs when a component's hover state changes.
        /// Use this for showing tooltips, previews, or highlighting related components.
        /// </summary>
        public event EventHandler<ComponentInteractionEventArgs> ComponentHoverChanged;

        /// <summary>
        /// Occurs when a connection line is clicked.
        /// Use this for selecting lines, showing line properties, or editing connections.
        /// </summary>
        public event EventHandler<LineInteractionEventArgs> LineClicked;

        /// <summary>
        /// Occurs when a connection line is double-clicked.
        /// Use this for editing line properties, labels, or routing.
        /// </summary>
        public event EventHandler<LineInteractionEventArgs> LineDoubleClicked;

        /// <summary>
        /// Occurs when a connection line is right-clicked.
        /// Use this for showing context menus with line-specific actions (delete, reroute, etc.).
        /// </summary>
        public event EventHandler<LineInteractionEventArgs> LineRightClicked;

        /// <summary>
        /// Occurs when a connection line's hover state changes.
        /// Use this for showing schema tooltips, data flow info, or highlighting.
        /// </summary>
        public event EventHandler<LineInteractionEventArgs> LineHoverChanged;

        /// <summary>
        /// Occurs when the empty canvas/diagram area is clicked.
        /// Use this for deselecting, showing diagram-level context menus, or canvas actions.
        /// </summary>
        public event EventHandler<DiagramInteractionEventArgs> DiagramClicked;

        /// <summary>
        /// Occurs when the empty canvas/diagram area is double-clicked.
        /// Use this for adding new nodes at the clicked position or canvas-level actions.
        /// </summary>
        public event EventHandler<DiagramInteractionEventArgs> DiagramDoubleClicked;

        /// <summary>
        /// Occurs when the empty canvas/diagram area is right-clicked.
        /// Use this for showing diagram-level context menus (add node, paste, etc.).
        /// </summary>
        public event EventHandler<DiagramInteractionEventArgs> DiagramRightClicked;

        // ========== Event Raisers ==========

        /// <summary>
        /// Raises the ComponentClicked event.
        /// </summary>
        internal void RaiseComponentClicked(ComponentInteractionEventArgs args)
        {
            ComponentClicked?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the ComponentDoubleClicked event.
        /// </summary>
        internal void RaiseComponentDoubleClicked(ComponentInteractionEventArgs args)
        {
            ComponentDoubleClicked?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the ComponentRightClicked event.
        /// </summary>
        internal void RaiseComponentRightClicked(ComponentInteractionEventArgs args)
        {
            ComponentRightClicked?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the ComponentHoverChanged event.
        /// </summary>
        internal void RaiseComponentHoverChanged(ComponentInteractionEventArgs args)
        {
            ComponentHoverChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the LineClicked event.
        /// </summary>
        internal void RaiseLineClicked(LineInteractionEventArgs args)
        {
            LineClicked?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the LineDoubleClicked event.
        /// </summary>
        internal void RaiseLineDoubleClicked(LineInteractionEventArgs args)
        {
            LineDoubleClicked?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the LineRightClicked event.
        /// </summary>
        internal void RaiseLineRightClicked(LineInteractionEventArgs args)
        {
            LineRightClicked?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the LineHoverChanged event.
        /// </summary>
        internal void RaiseLineHoverChanged(LineInteractionEventArgs args)
        {
            LineHoverChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the DiagramClicked event.
        /// </summary>
        internal void RaiseDiagramClicked(DiagramInteractionEventArgs args)
        {
            DiagramClicked?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the DiagramDoubleClicked event.
        /// </summary>
        internal void RaiseDiagramDoubleClicked(DiagramInteractionEventArgs args)
        {
            DiagramDoubleClicked?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the DiagramRightClicked event.
        /// </summary>
        internal void RaiseDiagramRightClicked(DiagramInteractionEventArgs args)
        {
            DiagramRightClicked?.Invoke(this, args);
        }

        /// <summary>
        /// Builds a serializable DTO representing the current diagram state.
        /// Components are captured minimally (type/geometry/name) and lines capture endpoint connection point IDs.
        /// </summary>
        /// <summary>
        /// Replaces non-finite geometry with 0 so a diagram containing NaN/Infinity can still be
        /// serialized to JSON (which has no representation for those values).
        /// </summary>
        private static float SanitizeCoordinate(float value)
            => float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;

        public Beep.Skia.Serialization.DiagramDto ToDto()
        {
            var dto = new Beep.Skia.Serialization.DiagramDto
            {
                ThemeName = ThemeManager.Current?.Name
            };
            foreach (var c in _components)
            {
                var comp = new Beep.Skia.Serialization.ComponentDto
                {
                    Type = c.GetType().AssemblyQualifiedName,
                    X = SanitizeCoordinate(c.X),
                    Y = SanitizeCoordinate(c.Y),
                    Width = SanitizeCoordinate(c.Width),
                    Height = SanitizeCoordinate(c.Height),
                    Name = c.Name
                };
                // Persist all NodeProperties generically — covers ALL diagram families
                try
                {
                    var props = c.GetProperties(includeCommon: false, includeNodeProperties: true);
                    foreach (var kvp in props)
                    {
                        try
                        {
                            var value = kvp.Value;
                            if (value == null) continue;

                            // Simple values round-trip through strings (existing behavior).
                            if (value is string || value is bool || value is Enum || value is SKColor || value is IConvertible)
                            {
                                comp.PropertyBag[kvp.Key] = Convert.ToString(value);
                            }
                            else
                            {
                                // Complex values (lists, dictionaries, nested objects) are stored typed.
                                comp.TypedPropertyBag[kvp.Key] = System.Text.Json.JsonSerializer.SerializeToElement(value, value.GetType());
                            }
                        }
                        catch { }
                    }
                }
                catch { }
                // Persist connection point IDs if available
                foreach (var p in c.InConnectionPoints)
                {
                    if (p != null) comp.InPointIds.Add(p.Id);
                }
                foreach (var p in c.OutConnectionPoints)
                {
                    if (p != null) comp.OutPointIds.Add(p.Id);
                }

                // Automation nodes carry their runtime configuration (transform mappings, filters,
                // credentials references, ...) which is not part of the NodeProperties bag.
                if (c is Model.IAutomationNode automation && automation.Configuration != null && automation.Configuration.Count > 0)
                {
                    try { comp.TypedPropertyBag["Configuration"] = System.Text.Json.JsonSerializer.SerializeToElement(automation.Configuration); }
                    catch { }
                }

                dto.Components.Add(comp);
            }
            foreach (var l in _lines)
            {
                if (l?.Start == null || l.End == null) continue;
                var line = new Beep.Skia.Serialization.LineDto
                {
                    StartPointId = l.Start.Id,
                    EndPointId = l.End.Id,
                    ShowStartArrow = l.ShowStartArrow,
                    ShowEndArrow = l.ShowEndArrow,
                    Label1 = l.Label1,
                    Label2 = l.Label2,
                    Label3 = l.Label3,
                    DataTypeLabel = (l as ConnectionLine)?.DataTypeLabel,
                    GuardCondition = (l as ConnectionLine)?.GuardCondition,
                    TransitionAction = (l as ConnectionLine)?.TransitionAction,
                    TriggerEvent = (l as ConnectionLine)?.TriggerEvent,
                    LineColor = (uint)l.LineColor,

                    // Extended
                    RoutingMode = (int)l.RoutingMode,
                    FlowDirection = (int)l.FlowDirection,
                    Label1Placement = (int)l.Label1Placement,
                    Label2Placement = (int)l.Label2Placement,
                    Label3Placement = (int)l.Label3Placement,
                    DataLabelPlacement = (int)l.DataLabelPlacement,
                    ArrowSize = l.ArrowSize,
                    DashPattern = l.DashPattern,
                    ShowStatusIndicator = l.ShowStatusIndicator,
                    Status = (int)l.Status,
                    StatusColor = (uint)l.StatusColor,
                    IsAnimated = l.IsAnimated,
                    IsDataFlowAnimated = (l as ConnectionLine)?.IsDataFlowAnimated ?? false,
                    DataFlowSpeed = (l as ConnectionLine)?.DataFlowSpeed ?? 0f,
                    DataFlowParticleSize = (l as ConnectionLine)?.DataFlowParticleSize ?? 0f,
                    DataFlowColor = (uint)((l as ConnectionLine)?.DataFlowColor ?? default),

                    // ERD multiplicity
                    StartMultiplicity = (int)l.StartMultiplicity,
                    EndMultiplicity = (int)l.EndMultiplicity,

                    // Schema
                    SchemaJson = (l as ConnectionLine)?.SchemaJson,
                    ExpectedSchemaJson = (l as ConnectionLine)?.ExpectedSchemaJson
                };
                dto.Lines.Add(line);
            }
            return dto;
        }

        /// <summary>
        /// Restores a diagram from a DTO. Components are created via reflection using their type names.
        /// Existing diagram is cleared prior to load.
        /// </summary>
        public void LoadFromDto(Beep.Skia.Serialization.DiagramDto dto)
        {
            if (dto == null) return;
            ClearComponents();

            // Apply the persisted theme before rendering the restored diagram.
            if (!string.IsNullOrWhiteSpace(dto.ThemeName))
            {
                try { ThemeManager.ApplyTheme(dto.ThemeName); } catch { }
            }

            // Create components first
            var componentDtos = dto.Components ?? new List<Beep.Skia.Serialization.ComponentDto>();
            var loadedComponents = new List<SkiaComponent>(componentDtos.Count);
            foreach (var comp in componentDtos)
            {
                try
                {
                    var type = Type.GetType(comp.Type, throwOnError: false);
                    if (type == null)
                    {
                        loadedComponents.Add(null);
                        continue;
                    }
                    if (Activator.CreateInstance(type) is SkiaComponent instance)
                    {
                        instance.X = comp.X;
                        instance.Y = comp.Y;
                        instance.Width = comp.Width;
                        instance.Height = comp.Height;
                        instance.Name = comp.Name;
                        // Apply all persisted NodeProperties BEFORE assigning connection point IDs
                        // (InPortCount/OutPortCount must be set first so CP arrays match)
                        try
                        {
                            var propValues = new Dictionary<string, object>();
                            if (comp.PropertyBag != null)
                            {
                                foreach (var kvp in comp.PropertyBag)
                                    propValues[kvp.Key] = kvp.Value;
                            }
                            if (comp.TypedPropertyBag != null)
                            {
                                foreach (var kvp in comp.TypedPropertyBag)
                                    propValues[kvp.Key] = ConvertJsonElement(kvp.Value);
                            }
                            if (propValues.Count > 0)
                            {
                                instance.SetPropperties(propValues, updateNodeProperties: true, applyToPublicSetters: true);
                            }
                        }
                        catch { }
                        // Attempt to apply persisted connection point IDs if counts match
                        var inPoints = instance.InConnectionPoints.ToList();
                        for (int i = 0; i < Math.Min(inPoints.Count, comp.InPointIds.Count); i++)
                        {
                            if (inPoints[i] is ConnectionPoint cp)
                            {
                                cp.SetId(comp.InPointIds[i]);
                            }
                        }
                        var outPoints = instance.OutConnectionPoints.ToList();
                        for (int i = 0; i < Math.Min(outPoints.Count, comp.OutPointIds.Count); i++)
                        {
                            if (outPoints[i] is ConnectionPoint cp)
                            {
                                cp.SetId(comp.OutPointIds[i]);
                            }
                        }
                        AddComponent(instance);
                        loadedComponents.Add(instance);
                    }
                    else
                    {
                        loadedComponents.Add(null);
                    }
                }
                catch { loadedComponents.Add(null); }
            }

            // Then connect lines using registry (by connection point id, or by component index)
            foreach (var line in dto.Lines ?? new List<Beep.Skia.Serialization.LineDto>())
            {
                IConnectionPoint start = line.StartPointId != Guid.Empty ? GetConnectionPoint(line.StartPointId) : null;
                IConnectionPoint end = line.EndPointId != Guid.Empty ? GetConnectionPoint(line.EndPointId) : null;

                if ((start == null || end == null) &&
                    line.StartComponentIndex >= 0 && line.StartComponentIndex < loadedComponents.Count &&
                    line.EndComponentIndex >= 0 && line.EndComponentIndex < loadedComponents.Count)
                {
                    start ??= FirstOutPoint(loadedComponents[line.StartComponentIndex]);
                    end ??= FirstInPoint(loadedComponents[line.EndComponentIndex]);
                }

                if (start == null || end == null) continue;
                var l = new ConnectionLine(start, end, () => RequestRedraw())
                {
                    ShowStartArrow = line.ShowStartArrow,
                    ShowEndArrow = line.ShowEndArrow,
                    LineColor = new SKColor(line.LineColor)
                };
                // Extended properties
                l.RoutingMode = (LineRoutingMode)line.RoutingMode;
                l.FlowDirection = (DataFlowDirection)line.FlowDirection;
                l.Label1 = line.Label1;
                l.Label2 = line.Label2;
                l.Label3 = line.Label3;
                l.DataTypeLabel = line.DataTypeLabel;
                l.GuardCondition = line.GuardCondition;
                l.TransitionAction = line.TransitionAction;
                l.TriggerEvent = line.TriggerEvent;
                l.Label1Placement = (LabelPlacement)line.Label1Placement;
                l.Label2Placement = (LabelPlacement)line.Label2Placement;
                l.Label3Placement = (LabelPlacement)line.Label3Placement;
                l.DataLabelPlacement = (LabelPlacement)line.DataLabelPlacement;
                l.ArrowSize = line.ArrowSize > 0 ? line.ArrowSize : l.ArrowSize;
                l.DashPattern = line.DashPattern;
                l.ShowStatusIndicator = line.ShowStatusIndicator;
                l.Status = (LineStatus)line.Status;
                l.StatusColor = new SKColor(line.StatusColor);
                l.IsAnimated = line.IsAnimated;
                l.IsDataFlowAnimated = line.IsDataFlowAnimated;
                if (line.DataFlowSpeed > 0) l.DataFlowSpeed = line.DataFlowSpeed;
                if (line.DataFlowParticleSize > 0) l.DataFlowParticleSize = line.DataFlowParticleSize;
                if (line.DataFlowColor != 0) l.DataFlowColor = new SKColor(line.DataFlowColor);
                // ERD multiplicity
                l.StartMultiplicity = (ERDMultiplicity)line.StartMultiplicity;
                l.EndMultiplicity = (ERDMultiplicity)line.EndMultiplicity;
                // Schema
                if (!string.IsNullOrWhiteSpace(line.SchemaJson)) l.SchemaJson = line.SchemaJson;
                if (!string.IsNullOrWhiteSpace(line.ExpectedSchemaJson)) l.ExpectedSchemaJson = line.ExpectedSchemaJson;
                AddLine(l);
            }

            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Converts a persisted JSON value back to CLR primitives, lists, and dictionaries
        /// so complex node properties survive a save/load round-trip.
        /// </summary>
        private static object ConvertJsonElement(System.Text.Json.JsonElement element)
        {
            switch (element.ValueKind)
            {
                case System.Text.Json.JsonValueKind.String:
                    return element.GetString();
                case System.Text.Json.JsonValueKind.Number:
                    if (element.TryGetInt64(out var l)) return l;
                    return element.GetDouble();
                case System.Text.Json.JsonValueKind.True:
                    return true;
                case System.Text.Json.JsonValueKind.False:
                    return false;
                case System.Text.Json.JsonValueKind.Array:
                    var list = new List<object>();
                    foreach (var item in element.EnumerateArray())
                        list.Add(ConvertJsonElement(item));
                    return list;
                case System.Text.Json.JsonValueKind.Object:
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in element.EnumerateObject())
                        dict[prop.Name] = ConvertJsonElement(prop.Value);
                    return dict;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the bounding rectangle that contains all components.
        /// </summary>
        /// <param name="padding">Padding to add around the content.</param>
        /// <returns>The content bounding box.</returns>
        public SKRect GetContentBounds(float padding = 20f)
        {
            if (_components.Count == 0) return new SKRect(0, 0, 800, 600);
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            foreach (var c in _components)
            {
                if (c.IsStatic) continue;
                var right = c.X + c.Width;
                var bottom = c.Y + c.Height;
                if (c.X < minX) minX = c.X;
                if (c.Y < minY) minY = c.Y;
                if (right > maxX) maxX = right;
                if (bottom > maxY) maxY = bottom;
            }
            if (minX == float.MaxValue) return new SKRect(0, 0, 800, 600);
            return new SKRect(minX - padding, minY - padding, maxX + padding, maxY + padding);
        }

        /// <summary>
        /// Arranges the diagram using the specified auto-layout algorithm.
        /// </summary>
        /// <param name="layout">The layout algorithm to apply.</param>
        public void ArrangeDiagram(Beep.Skia.Layout.IAutoLayout layout)
        {
            if (layout == null) return;
            layout.Arrange(_components, _lines);
            foreach (var c in _components)
                RefreshConnectionPoints(c);
            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Validates the current diagram using all registered rules and returns issues.
        /// </summary>
        /// <returns>List of diagram issues found.</returns>
        public List<DiagramIssue> ValidateDiagram()
        {
            return DiagramValidator.Validate(_components, _lines);
        }

        /// <summary>
        /// Converts the current diagram to a WorkflowDefinition suitable for execution by IWorkflowEngine.
        /// Only IAutomationNode components are included.
        /// </summary>
        public WorkflowDefinition ToWorkflowDefinition(string workflowName = "Workflow")
        {
            var wf = new WorkflowDefinition(Guid.NewGuid().ToString("N")[..8], workflowName);
            var idMap = new Dictionary<SkiaComponent, string>();

            foreach (var c in _components)
            {
                if (c is IAutomationNode auto)
                {
                    var nodeId = Guid.NewGuid().ToString("N")[..8];
                    idMap[c] = nodeId;

                    var nodeDef = new NodeDefinition(
                        nodeId,
                        c.Name ?? auto.GetType().Name,
                        auto.NodeType,
                        auto.GetType().FullName ?? auto.GetType().Name);

                    if (auto.Configuration != null)
                        nodeDef.Configuration = new Dictionary<string, object>(auto.Configuration);

                    wf.AddNode(nodeDef);
                }
            }

            foreach (var line in _lines)
            {
                if (line?.Start?.Component is SkiaComponent src && idMap.TryGetValue(src, out var srcId)
                    && line?.End?.Component is SkiaComponent dst && idMap.TryGetValue(dst, out var dstId))
                {
                    wf.Connections.Add(new ConnectionDefinition(
                        Guid.NewGuid().ToString("N")[..8], srcId, dstId));
                }
            }

            return wf;
        }

        /// <summary>
        /// Exports the current diagram to a PNG image file.
        /// </summary>
        /// <param name="filePath">Output file path.</param>
        /// <param name="scale">Scale factor for export resolution (default 2x for retina).</param>
        /// <param name="background">Background color (default white).</param>
        /// <param name="cropToContent">When true, crops to content bounds; otherwise uses full canvas.</param>
        public void ExportToPng(string filePath, float scale = 2f, SKColor? background = null, bool cropToContent = true)
        {
            var bgColor = background ?? SKColors.White;
            SKRect bounds;
            if (cropToContent)
            {
                bounds = GetContentBounds();
            }
            else
            {
                bounds = new SKRect(0, 0, 1920, 1080);
            }

            int width = (int)Math.Ceiling(bounds.Width * scale);
            int height = (int)Math.Ceiling(bounds.Height * scale);

            using var bitmap = new SKBitmap(width, height);
            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            canvas.Clear(bgColor);
            canvas.Scale(scale);
            canvas.Translate(-bounds.Left, -bounds.Top);

            DrawForExport(canvas);
            canvas.Flush();

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = System.IO.File.OpenWrite(filePath);
            data.SaveTo(stream);
        }

        /// <summary>
        /// Exports the current diagram to an SVG file.
        /// </summary>
        /// <param name="filePath">Output file path.</param>
        /// <param name="cropToContent">When true, crops to content bounds.</param>
        /// <param name="background">Optional background fill color; null keeps the SVG transparent.</param>
        public void ExportToSvg(string filePath, bool cropToContent = true, SKColor? background = null)
        {
            SKRect bounds;
            if (cropToContent)
            {
                bounds = GetContentBounds();
            }
            else
            {
                bounds = new SKRect(0, 0, 1920, 1080);
            }

            var size = new SKSize(bounds.Width, bounds.Height);
            using var stream = System.IO.File.Create(filePath);
            using var svgCanvas = SKSvgCanvas.Create(new SKRect(0, 0, size.Width, size.Height), stream);

            svgCanvas.Save();
            if (background.HasValue)
            {
                using var bgPaint = new SKPaint { Color = background.Value, Style = SKPaintStyle.Fill };
                svgCanvas.DrawRect(new SKRect(0, 0, size.Width, size.Height), bgPaint);
            }
            svgCanvas.Translate(-bounds.Left, -bounds.Top);
            DrawForExport(svgCanvas);
            svgCanvas.Restore();
            svgCanvas.Flush();
        }

        /// <summary>
        /// Exports the current diagram to a PDF file. Content is scaled to fit a single page when
        /// it fits; larger diagrams are tiled at natural scale across multiple pages.
        /// </summary>
        /// <param name="filePath">Output file path.</param>
        /// <param name="pageWidth">Page width in points (default A4 landscape).</param>
        /// <param name="pageHeight">Page height in points (default A4 landscape).</param>
        /// <param name="margin">Page margin in points.</param>
        public void ExportToPdf(string filePath, float pageWidth = 842f, float pageHeight = 595f, float margin = 24f)
        {
            var bounds = GetContentBounds();
            float contentWidth = Math.Max(bounds.Width, 1f);
            float contentHeight = Math.Max(bounds.Height, 1f);
            float usableW = Math.Max(1f, pageWidth - margin * 2f);
            float usableH = Math.Max(1f, pageHeight - margin * 2f);

            float scaleToFit = Math.Min(Math.Min(usableW / contentWidth, usableH / contentHeight), 1f);

            using var stream = System.IO.File.Create(filePath);
            using var document = SKDocument.CreatePdf(stream);

            bool fitsAtNaturalSize = scaleToFit >= 0.999f;
            bool scaleDownIsLegible = contentWidth <= usableW * 2f && contentHeight <= usableH * 2f;

            if (fitsAtNaturalSize || scaleDownIsLegible)
            {
                RenderPdfPage(document, pageWidth, pageHeight, margin, scaleToFit, bounds.Left, bounds.Top);
            }
            else
            {
                // Tile at natural scale so large diagrams stay legible.
                int cols = (int)Math.Ceiling(contentWidth / usableW);
                int rows = (int)Math.Ceiling(contentHeight / usableH);
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        float srcX = bounds.Left + col * usableW;
                        float srcY = bounds.Top + row * usableH;
                        RenderPdfPage(document, pageWidth, pageHeight, margin, 1f, srcX, srcY);
                    }
                }
            }

            document.Close();
        }

        private void RenderPdfPage(SKDocument document, float pageWidth, float pageHeight, float margin, float scale, float originX, float originY)
        {
            using var pageCanvas = document.BeginPage(pageWidth, pageHeight);
            pageCanvas.Save();
            pageCanvas.Translate(margin, margin);
            pageCanvas.Scale(scale);
            pageCanvas.Translate(-originX, -originY);
            DrawForExport(pageCanvas);
            pageCanvas.Restore();
            pageCanvas.Flush();
            document.EndPage();
        }

        /// <summary>
        /// Renders the diagram to an offscreen bitmap for testing or headless scenarios.
        /// </summary>
        /// <param name="width">Output width in pixels.</param>
        /// <param name="height">Output height in pixels.</param>
        /// <param name="background">Background color.</param>
        /// <returns>The rendered bitmap.</returns>
        public SKBitmap RenderToBitmap(int width = 1920, int height = 1080, SKColor? background = null)
        {
            var bgColor = background ?? SKColors.White;
            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            canvas.Clear(bgColor);
            DrawForExport(canvas);
            canvas.Flush();

            var bitmap = new SKBitmap(width, height);
            using var image = surface.Snapshot();
            image.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0);
            return bitmap;
        }

        /// <summary>
        /// Creates a System.Drawing.Printing.PrintDocument for printing the diagram.
        /// The caller should call doc.Print() or show a PrintPreviewDialog with this document.
        /// </summary>
        /// <param name="documentName">Name shown in the print queue.</param>
        /// <returns>A configured PrintDocument ready for printing or preview.</returns>
        [SupportedOSPlatform("windows")]
        public System.Drawing.Printing.PrintDocument CreatePrintDocument(string documentName = "Beep.Skia Diagram")
        {
            var doc = new System.Drawing.Printing.PrintDocument();
            doc.DocumentName = documentName;
            var bounds = GetContentBounds();
            int currentPage = 0;
            int totalPages = 0;

            doc.BeginPrint += (s, e) =>
            {
                currentPage = 0;
                // Calculate page dimensions in world-space
                float pageW = (doc.DefaultPageSettings.PaperSize.Width
                    - doc.DefaultPageSettings.Margins.Left
                    - doc.DefaultPageSettings.Margins.Right) / 100f * 96f;
                float pageH = (doc.DefaultPageSettings.PaperSize.Height
                    - doc.DefaultPageSettings.Margins.Top
                    - doc.DefaultPageSettings.Margins.Bottom) / 100f * 96f;
                int cols = (int)Math.Ceiling(bounds.Width / pageW);
                int rows = (int)Math.Ceiling(bounds.Height / pageH);
                totalPages = cols * rows;
            };

            doc.PrintPage += (s, e) =>
            {
                float marginX = doc.DefaultPageSettings.Margins.Left / 100f * 96f;
                float marginY = doc.DefaultPageSettings.Margins.Top / 100f * 96f;
                float pageW = (doc.DefaultPageSettings.PaperSize.Width
                    - doc.DefaultPageSettings.Margins.Left
                    - doc.DefaultPageSettings.Margins.Right) / 100f * 96f;
                float pageH = (doc.DefaultPageSettings.PaperSize.Height
                    - doc.DefaultPageSettings.Margins.Top
                    - doc.DefaultPageSettings.Margins.Bottom) / 100f * 96f;
                int cols = (int)Math.Ceiling(bounds.Width / pageW);
                int rows = (int)Math.Ceiling(bounds.Height / pageH);
                int col = currentPage % cols;
                int row = currentPage / cols;

                float srcX = bounds.Left + col * pageW;
                float srcY = bounds.Top + row * pageH;
                float srcW = Math.Min(pageW, bounds.Right - srcX);
                float srcH = Math.Min(pageH, bounds.Bottom - srcY);

                if (srcW > 0 && srcH > 0)
                {
                    using var surface = SKSurface.Create(new SKImageInfo((int)Math.Ceiling(srcW), (int)Math.Ceiling(srcH)));
                    var canvas = surface.Canvas;
                    canvas.Clear(SKColors.White);
                    canvas.Translate(-srcX, -srcY);
                    DrawForExport(canvas);
                    canvas.Flush();

                    using var image = surface.Snapshot();
                    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                    using var ms = new System.IO.MemoryStream();
                    data.SaveTo(ms);
                    ms.Seek(0, System.IO.SeekOrigin.Begin);

                    using var gdiBitmap = new System.Drawing.Bitmap(ms);
                    // Draw the tile at natural size (1:1 with world pixels), not stretched to the page.
                    e.Graphics.DrawImage(gdiBitmap, marginX, marginY, srcW, srcH);
                }

                e.Graphics.DrawString(
                    $"Page {currentPage + 1} of {totalPages}",
                    new System.Drawing.Font("Segoe UI", 8),
                    System.Drawing.Brushes.Gray,
                    marginX + pageW - 100, marginY + pageH + 5);

                currentPage++;
                e.HasMorePages = currentPage < totalPages;
            };

            return doc;
        }
    }
}

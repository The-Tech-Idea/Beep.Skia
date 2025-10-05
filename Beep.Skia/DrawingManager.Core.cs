using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        // Helper classes
        private SelectionManager _selectionManager;
        private InteractionHelper _interactionHelper;
        private RenderingHelper _renderingHelper;
        private HistoryManager _historyManager;

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
        public Beep.Skia.Serialization.DiagramDto ToDto()
        {
            var dto = new Beep.Skia.Serialization.DiagramDto();
            foreach (var c in _components)
            {
                var comp = new Beep.Skia.Serialization.ComponentDto
                {
                    Type = c.GetType().AssemblyQualifiedName,
                    X = c.X,
                    Y = c.Y,
                    Width = c.Width,
                    Height = c.Height,
                    Name = c.Name
                };
                // Persist MindMap-specific options via PropertyBag
                try
                {
                    if (c.GetType().Namespace == "Beep.Skia.MindMap")
                    {
                        var t = c.GetType();
                        var title = t.GetProperty("Title");
                        var notes = t.GetProperty("Notes");
                        var inCount = t.GetProperty("InPortCount");
                        var outCount = t.GetProperty("OutPortCount");
                        if (title != null)
                        {
                            var val = title.GetValue(c) as string;
                            if (!string.IsNullOrEmpty(val)) comp.PropertyBag["Title"] = val;
                        }
                        if (notes != null)
                        {
                            var val = notes.GetValue(c) as string;
                            if (!string.IsNullOrEmpty(val)) comp.PropertyBag["Notes"] = val;
                        }
                        if (inCount != null)
                        {
                            var val = inCount.GetValue(c);
                            if (val != null) comp.PropertyBag["InPortCount"] = Convert.ToString(val);
                        }
                        if (outCount != null)
                        {
                            var val = outCount.GetValue(c);
                            if (val != null) comp.PropertyBag["OutPortCount"] = Convert.ToString(val);
                        }
                    }
                }
                catch { }
                // Persist StateMachine-specific options via PropertyBag
                try
                {
                    if (c.GetType().Namespace == "Beep.Skia.StateMachine")
                    {
                        var t = c.GetType();
                        var title = t.GetProperty("Title");
                        var inCount = t.GetProperty("InPortCount");
                        var outCount = t.GetProperty("OutPortCount");
                        if (title != null)
                        {
                            var val = title.GetValue(c) as string;
                            if (!string.IsNullOrEmpty(val)) comp.PropertyBag["Title"] = val;
                        }
                        if (inCount != null)
                        {
                            var val = inCount.GetValue(c);
                            if (val != null) comp.PropertyBag["InPortCount"] = Convert.ToString(val);
                        }
                        if (outCount != null)
                        {
                            var val = outCount.GetValue(c);
                            if (val != null) comp.PropertyBag["OutPortCount"] = Convert.ToString(val);
                        }
                    }
                }
                catch { }
                // Persist Flowchart-specific options via PropertyBag (counts/flags)
                try
                {
                    if (c.GetType().Namespace == "Beep.Skia.Flowchart")
                    {
                        var t = c.GetType();
                        var inCount = t.GetProperty("InPortCount");
                        var outCount = t.GetProperty("OutPortCount");
                        var showTB = t.GetProperty("ShowTopBottomPorts");
                        var outOnTop = t.GetProperty("OutPortsOnTop");
                        if (inCount != null)
                        {
                            var val = inCount.GetValue(c);
                            if (val != null) comp.PropertyBag["InPortCount"] = Convert.ToString(val);
                        }
                        if (outCount != null)
                        {
                            var val = outCount.GetValue(c);
                            if (val != null) comp.PropertyBag["OutPortCount"] = Convert.ToString(val);
                        }
                        if (showTB != null)
                        {
                            var val = showTB.GetValue(c);
                            if (val != null) comp.PropertyBag["ShowTopBottomPorts"] = Convert.ToString(val).ToLowerInvariant();
                        }
                        if (outOnTop != null)
                        {
                            var val = outOnTop.GetValue(c);
                            if (val != null) comp.PropertyBag["OutPortsOnTop"] = Convert.ToString(val).ToLowerInvariant();
                        }
                    }
                }
                catch { }
                // Persist PM-specific options via PropertyBag (counts and common fields)
                try
                {
                    if (c.GetType().Namespace == "Beep.Skia.PM")
                    {
                        var t = c.GetType();
                        var inCount = t.GetProperty("InPortCount");
                        var outCount = t.GetProperty("OutPortCount");
                        var title = t.GetProperty("Title");
                        var label = t.GetProperty("Label");
                        var percent = t.GetProperty("PercentComplete");
                        if (inCount != null)
                        {
                            var val = inCount.GetValue(c);
                            if (val != null) comp.PropertyBag["InPortCount"] = Convert.ToString(val);
                        }
                        if (outCount != null)
                        {
                            var val = outCount.GetValue(c);
                            if (val != null) comp.PropertyBag["OutPortCount"] = Convert.ToString(val);
                        }
                        if (title != null)
                        {
                            var val = title.GetValue(c) as string;
                            if (!string.IsNullOrEmpty(val)) comp.PropertyBag["Title"] = val;
                        }
                        if (label != null)
                        {
                            var val = label.GetValue(c) as string;
                            if (!string.IsNullOrEmpty(val)) comp.PropertyBag["Label"] = val;
                        }
                        if (percent != null)
                        {
                            var val = percent.GetValue(c);
                            if (val != null) comp.PropertyBag["PercentComplete"] = Convert.ToString(val);
                        }
                    }
                }
                catch { }
                // Persist ERD-specific options via PropertyBag (entity rows and name)
                try
                {
                    if (c.GetType().Namespace == "Beep.Skia.ERD")
                    {
                        var t = c.GetType();
                        var entityName = t.GetProperty("EntityName");
                        var rowsText = t.GetProperty("RowsText");
                        var rowIdsCsv = t.GetProperty("RowIdsCsv");
                        var inCount = t.GetProperty("InPortCount");
                        var outCount = t.GetProperty("OutPortCount");
                        if (entityName != null)
                        {
                            var val = entityName.GetValue(c) as string;
                            if (!string.IsNullOrEmpty(val)) comp.PropertyBag["EntityName"] = val;
                        }
                        if (rowsText != null)
                        {
                            var val = rowsText.GetValue(c) as string;
                            if (!string.IsNullOrEmpty(val)) comp.PropertyBag["RowsText"] = val;
                        }
                        if (rowIdsCsv != null)
                        {
                            var val = rowIdsCsv.GetValue(c) as string;
                            if (!string.IsNullOrEmpty(val)) comp.PropertyBag["RowIdsCsv"] = val;
                        }
                        if (inCount != null)
                        {
                            var val = inCount.GetValue(c);
                            if (val != null) comp.PropertyBag["InPortCount"] = Convert.ToString(val);
                        }
                        if (outCount != null)
                        {
                            var val = outCount.GetValue(c);
                            if (val != null) comp.PropertyBag["OutPortCount"] = Convert.ToString(val);
                        }
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

            // Create components first
            foreach (var comp in dto.Components)
            {
                try
                {
                    var type = Type.GetType(comp.Type, throwOnError: false);
                    if (type == null) continue;
                    if (Activator.CreateInstance(type) is SkiaComponent instance)
                    {
                        instance.X = comp.X;
                        instance.Y = comp.Y;
                        instance.Width = comp.Width;
                        instance.Height = comp.Height;
                        instance.Name = comp.Name;
                        // Apply MindMap-specific options BEFORE assigning connection point IDs
                        try
                        {
                            if (type.Namespace == "Beep.Skia.MindMap" && comp.PropertyBag != null)
                            {
                                var t = type;
                                if (comp.PropertyBag.TryGetValue("Title", out var titleStr))
                                {
                                    var prop = t.GetProperty("Title");
                                    prop?.SetValue(instance, titleStr);
                                }
                                if (comp.PropertyBag.TryGetValue("Notes", out var notesStr))
                                {
                                    var prop = t.GetProperty("Notes");
                                    prop?.SetValue(instance, notesStr);
                                }
                                if (comp.PropertyBag.TryGetValue("InPortCount", out var inCountStr) && int.TryParse(inCountStr, out var inCount))
                                {
                                    var prop = t.GetProperty("InPortCount");
                                    prop?.SetValue(instance, inCount);
                                }
                                if (comp.PropertyBag.TryGetValue("OutPortCount", out var outCountStr) && int.TryParse(outCountStr, out var outCount))
                                {
                                    var prop = t.GetProperty("OutPortCount");
                                    prop?.SetValue(instance, outCount);
                                }
                            }
                        }
                        catch { }
                        // Apply StateMachine-specific options BEFORE assigning connection point IDs
                        try
                        {
                            if (type.Namespace == "Beep.Skia.StateMachine" && comp.PropertyBag != null)
                            {
                                var t = type;
                                if (comp.PropertyBag.TryGetValue("InPortCount", out var inCountStr) && int.TryParse(inCountStr, out var inCount))
                                {
                                    var prop = t.GetProperty("InPortCount");
                                    prop?.SetValue(instance, inCount);
                                }
                                if (comp.PropertyBag.TryGetValue("OutPortCount", out var outCountStr) && int.TryParse(outCountStr, out var outCount))
                                {
                                    var prop = t.GetProperty("OutPortCount");
                                    prop?.SetValue(instance, outCount);
                                }
                                if (comp.PropertyBag.TryGetValue("Title", out var titleStr))
                                {
                                    var prop = t.GetProperty("Title");
                                    prop?.SetValue(instance, titleStr);
                                }
                            }
                        }
                        catch { }
                        // Apply Flowchart-specific persisted options BEFORE assigning connection point IDs
                        try
                        {
                            if (type.Namespace == "Beep.Skia.Flowchart" && comp.PropertyBag != null)
                            {
                                var t = type;
                                // Adjust counts first to ensure CP arrays match
                                if (comp.PropertyBag.TryGetValue("InPortCount", out var inCountStr) && int.TryParse(inCountStr, out var inCount))
                                {
                                    var prop = t.GetProperty("InPortCount");
                                    prop?.SetValue(instance, inCount);
                                }
                                if (comp.PropertyBag.TryGetValue("OutPortCount", out var outCountStr) && int.TryParse(outCountStr, out var outCount))
                                {
                                    var prop = t.GetProperty("OutPortCount");
                                    prop?.SetValue(instance, outCount);
                                }
                                // Placement flags
                                if (comp.PropertyBag.TryGetValue("ShowTopBottomPorts", out var showTBStr) && bool.TryParse(showTBStr, out var showTB))
                                {
                                    var prop = t.GetProperty("ShowTopBottomPorts");
                                    prop?.SetValue(instance, showTB);
                                }
                                if (comp.PropertyBag.TryGetValue("OutPortsOnTop", out var outOnTopStr) && bool.TryParse(outOnTopStr, out var outOnTop))
                                {
                                    var prop = t.GetProperty("OutPortsOnTop");
                                    prop?.SetValue(instance, outOnTop);
                                }
                            }
                        }
                        catch { }
                        // Apply PM-specific persisted options BEFORE assigning connection point IDs
                        try
                        {
                            if (type.Namespace == "Beep.Skia.PM" && comp.PropertyBag != null)
                            {
                                var t = type;
                                if (comp.PropertyBag.TryGetValue("InPortCount", out var inCountStr) && int.TryParse(inCountStr, out var inCount))
                                {
                                    var prop = t.GetProperty("InPortCount");
                                    prop?.SetValue(instance, inCount);
                                }
                                if (comp.PropertyBag.TryGetValue("OutPortCount", out var outCountStr) && int.TryParse(outCountStr, out var outCount))
                                {
                                    var prop = t.GetProperty("OutPortCount");
                                    prop?.SetValue(instance, outCount);
                                }
                                if (comp.PropertyBag.TryGetValue("Title", out var titleStr))
                                {
                                    var prop = t.GetProperty("Title");
                                    prop?.SetValue(instance, titleStr);
                                }
                                if (comp.PropertyBag.TryGetValue("Label", out var labelStr))
                                {
                                    var prop = t.GetProperty("Label");
                                    prop?.SetValue(instance, labelStr);
                                }
                                if (comp.PropertyBag.TryGetValue("PercentComplete", out var pctStr) && int.TryParse(pctStr, out var pct))
                                {
                                    var prop = t.GetProperty("PercentComplete");
                                    prop?.SetValue(instance, pct);
                                }
                            }
                        }
                        catch { }
                        // Apply ERD-specific persisted options BEFORE assigning connection point IDs
                        try
                        {
                            if (type.Namespace == "Beep.Skia.ERD" && comp.PropertyBag != null)
                            {
                                var t = type;
                                if (comp.PropertyBag.TryGetValue("EntityName", out var nameStr))
                                {
                                    var prop = t.GetProperty("EntityName");
                                    prop?.SetValue(instance, nameStr);
                                }
                                if (comp.PropertyBag.TryGetValue("RowsText", out var rowsStr))
                                {
                                    var prop = t.GetProperty("RowsText");
                                    prop?.SetValue(instance, rowsStr);
                                }
                                if (comp.PropertyBag.TryGetValue("RowIdsCsv", out var idsStr))
                                {
                                    var prop = t.GetProperty("RowIdsCsv");
                                    prop?.SetValue(instance, idsStr);
                                }
                                // If counts were persisted, set them after rows so row-based layout can sync
                                if (comp.PropertyBag.TryGetValue("InPortCount", out var inCountStr2) && int.TryParse(inCountStr2, out var inCount2))
                                {
                                    var prop = t.GetProperty("InPortCount");
                                    prop?.SetValue(instance, inCount2);
                                }
                                if (comp.PropertyBag.TryGetValue("OutPortCount", out var outCountStr2) && int.TryParse(outCountStr2, out var outCount2))
                                {
                                    var prop = t.GetProperty("OutPortCount");
                                    prop?.SetValue(instance, outCount2);
                                }
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
                    }
                }
                catch { }
            }

            // Then connect lines using registry
            foreach (var line in dto.Lines)
            {
                var start = GetConnectionPoint(line.StartPointId);
                var end = GetConnectionPoint(line.EndPointId);
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
    }
}

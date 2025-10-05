# Comprehensive Event System for External Handling

The DrawingManager provides a **complete event system** for handling all user interactions with nodes, lines, and the diagram canvas. This enables you to build rich external UIs, property panels, context menus, and custom behaviors.

## 📋 Table of Contents

1. [Component (Node) Events](#component-node-events)
2. [Connection Line Events](#connection-line-events)
3. [Diagram Canvas Events](#diagram-canvas-events)
4. [Event Arguments Reference](#event-arguments-reference)
5. [Usage Examples](#usage-examples)
6. [Best Practices](#best-practices)

---

## Component (Node) Events

These events fire when users interact with any node in the diagram (TaskNode, ETLSource, ProcessNode, etc.).

### `ComponentClicked`
Fired when a component is single-clicked with left mouse button.

**Use Cases:**
- Select the component
- Show properties in a side panel
- Update UI to reflect current selection
- Navigate to related data

### `ComponentDoubleClicked`
Fired when a component is double-clicked.

**Use Cases:**
- Open property editor dialog
- Open node-specific configuration window
- Toggle expand/collapse for container nodes
- Drill down to details

### `ComponentRightClicked`
Fired when a component is right-clicked.

**Use Cases:**
- Show context menu with node-specific actions
- Delete, duplicate, copy/paste options
- Quick property access
- Connection management

### `ComponentHoverChanged`
Fired when mouse enters or leaves a component.

**Use Cases:**
- Show/hide tooltips with component information
- Highlight connected nodes and lines
- Preview component details
- Show mini property card

---

## Connection Line Events

These events fire when users interact with connection lines between nodes.

### `LineClicked`
Fired when a line is single-clicked with left mouse button.

**Use Cases:**
- Select the line
- Show line properties (labels, routing, style)
- Highlight connected nodes
- Display data flow information

### `LineDoubleClicked`
Fired when a line is double-clicked.

**Use Cases:**
- Edit line labels inline
- Open line property editor
- Change routing mode
- Configure animation settings

### `LineRightClicked`
Fired when a line is right-clicked.

**Use Cases:**
- Show context menu (delete, reroute, properties)
- Quick access to line styling
- Toggle animation on/off
- Add intermediate waypoints

### `LineHoverChanged`
Fired when mouse enters or leaves a connection line.

**Use Cases:**
- Show schema tooltip (for ETL lines)
- Display data flow direction/status
- Highlight connected nodes
- Show line metadata

---

## Diagram Canvas Events

These events fire when users interact with empty space on the canvas.

### `DiagramClicked`
Fired when empty canvas is single-clicked.

**Use Cases:**
- Deselect all components and lines
- Clear property panels
- Place new node at clicked position
- Cancel ongoing operations

### `DiagramDoubleClicked`
Fired when empty canvas is double-clicked.

**Use Cases:**
- Quick add node at clicked position
- Create group/container at location
- Open canvas properties
- Zoom to fit all nodes

### `DiagramRightClicked`
Fired when empty canvas is right-clicked.

**Use Cases:**
- Show diagram-level context menu
- Add node menu (categorized)
- Paste from clipboard
- Canvas settings (grid, snap, zoom)

---

## Event Arguments Reference

### `ComponentInteractionEventArgs`

```csharp
public class ComponentInteractionEventArgs : EventArgs
{
    SkiaComponent Component         // The component that was interacted with
    SKPoint CanvasPosition          // Mouse position in canvas coordinates
    SKPoint ScreenPosition          // Mouse position in screen coordinates
    int MouseButton                 // 0=Left, 1=Right, 2=Middle
    SKKeyModifiers Modifiers        // Ctrl, Shift, Alt keys pressed
    InteractionType InteractionType // Click, DoubleClick, RightClick, etc.
    bool Handled                    // Set to true to prevent default behavior
    object Tag                      // Custom data you can attach
}
```

### `LineInteractionEventArgs`

```csharp
public class LineInteractionEventArgs : EventArgs
{
    IConnectionLine Line            // The line that was interacted with
    SKPoint CanvasPosition          // Mouse position in canvas coordinates
    SKPoint ScreenPosition          // Mouse position in screen coordinates
    int MouseButton                 // 0=Left, 1=Right, 2=Middle
    SKKeyModifiers Modifiers        // Ctrl, Shift, Alt keys pressed
    InteractionType InteractionType // Click, DoubleClick, RightClick, etc.
    bool IsArrowClick               // True if an arrow head was clicked
    bool Handled                    // Set to true to prevent default behavior
    object Tag                      // Custom data you can attach
}
```

### `DiagramInteractionEventArgs`

```csharp
public class DiagramInteractionEventArgs : EventArgs
{
    SKPoint CanvasPosition          // Mouse position in canvas coordinates
    SKPoint ScreenPosition          // Mouse position in screen coordinates
    int MouseButton                 // 0=Left, 1=Right, 2=Middle
    SKKeyModifiers Modifiers        // Ctrl, Shift, Alt keys pressed
    InteractionType InteractionType // Click, DoubleClick, RightClick, etc.
    bool Handled                    // Set to true to prevent default behavior
    object Tag                      // Custom data you can attach
}
```

### `InteractionType` Enum

```csharp
public enum InteractionType
{
    Click,          // Single click
    DoubleClick,    // Double click
    RightClick,     // Right click (context menu)
    MiddleClick,    // Middle button click
    HoverEnter,     // Mouse entered bounds
    HoverLeave,     // Mouse left bounds
    Hover,          // Mouse is hovering (continuous)
    DragStart,      // Drag operation started
    Dragging,       // Drag in progress
    DragEnd         // Drag ended
}
```

---

## Usage Examples

### Example 1: Property Panel Integration

```csharp
using Beep.Skia;
using System.Windows.Forms; // or your UI framework

public class MyDiagramForm : Form
{
    private DrawingManager drawingManager;
    private Panel propertyPanel;
    
    public MyDiagramForm()
    {
        InitializeComponent();
        
        drawingManager = new DrawingManager();
        
        // Show properties when any node is clicked
        drawingManager.ComponentClicked += OnComponentClicked;
        
        // Clear properties when canvas is clicked
        drawingManager.DiagramClicked += OnDiagramClicked;
    }
    
    private void OnComponentClicked(object sender, ComponentInteractionEventArgs e)
    {
        // Update property panel with component details
        propertyPanel.Controls.Clear();
        
        var component = e.Component;
        
        // Add property controls
        AddPropertyLabel("Name", component.Name);
        AddPropertyLabel("Type", component.GetType().Name);
        AddPropertyLabel("Position", $"({component.X:F0}, {component.Y:F0})");
        AddPropertyLabel("Size", $"{component.Width:F0} × {component.Height:F0}");
        
        // For PM TaskNode
        if (component is Beep.Skia.PM.TaskNode taskNode)
        {
            AddPropertyLabel("Title", taskNode.Title);
            AddPropertySlider("Progress", taskNode.PercentComplete, 0, 100);
        }
        
        // For ETL nodes
        if (component is Beep.Skia.ETL.ETLTransform transform)
        {
            AddPropertyLabel("Transform Type", transform.Kind.ToString());
            AddPropertyTextBox("Expression", transform.Expression);
        }
    }
    
    private void OnDiagramClicked(object sender, DiagramInteractionEventArgs e)
    {
        // Clear property panel when clicking empty space
        propertyPanel.Controls.Clear();
        drawingManager.SelectionManager.ClearSelection();
    }
}
```

### Example 2: Context Menus

```csharp
public class MyDiagramControl : UserControl
{
    private DrawingManager drawingManager;
    private ContextMenuStrip nodeContextMenu;
    private ContextMenuStrip lineContextMenu;
    private ContextMenuStrip diagramContextMenu;
    
    public MyDiagramControl()
    {
        InitializeComponent();
        
        drawingManager = new DrawingManager();
        CreateContextMenus();
        
        // Wire up right-click events
        drawingManager.ComponentRightClicked += OnComponentRightClicked;
        drawingManager.LineRightClicked += OnLineRightClicked;
        drawingManager.DiagramRightClicked += OnDiagramRightClicked;
    }
    
    private void CreateContextMenus()
    {
        // Node context menu
        nodeContextMenu = new ContextMenuStrip();
        nodeContextMenu.Items.Add("Properties...", null, OnNodeProperties);
        nodeContextMenu.Items.Add("Duplicate", null, OnNodeDuplicate);
        nodeContextMenu.Items.Add("-");
        nodeContextMenu.Items.Add("Delete", null, OnNodeDelete);
        
        // Line context menu
        lineContextMenu = new ContextMenuStrip();
        lineContextMenu.Items.Add("Edit Labels...", null, OnLineEditLabels);
        lineContextMenu.Items.Add("Change Routing", null, OnLineChangeRouting);
        lineContextMenu.Items.Add("Toggle Animation", null, OnLineToggleAnimation);
        lineContextMenu.Items.Add("-");
        lineContextMenu.Items.Add("Delete", null, OnLineDelete);
        
        // Diagram context menu
        diagramContextMenu = new ContextMenuStrip();
        var addMenu = new ToolStripMenuItem("Add Node");
        addMenu.DropDownItems.Add("PM Task", null, OnAddTaskNode);
        addMenu.DropDownItems.Add("ETL Transform", null, OnAddETLTransform);
        addMenu.DropDownItems.Add("Flowchart Process", null, OnAddProcess);
        diagramContextMenu.Items.Add(addMenu);
        diagramContextMenu.Items.Add("-");
        diagramContextMenu.Items.Add("Paste", null, OnPaste);
        diagramContextMenu.Items.Add("Select All", null, OnSelectAll);
    }
    
    private void OnComponentRightClicked(object sender, ComponentInteractionEventArgs e)
    {
        // Store component for menu actions
        nodeContextMenu.Tag = e.Component;
        
        // Show context menu at screen position
        nodeContextMenu.Show(this, e.ScreenPosition.ToPoint());
        
        // Mark as handled to prevent default behavior
        e.Handled = true;
    }
    
    private void OnLineRightClicked(object sender, LineInteractionEventArgs e)
    {
        lineContextMenu.Tag = e.Line;
        lineContextMenu.Show(this, e.ScreenPosition.ToPoint());
        e.Handled = true;
    }
    
    private void OnDiagramRightClicked(object sender, DiagramInteractionEventArgs e)
    {
        diagramContextMenu.Tag = e.CanvasPosition;
        diagramContextMenu.Show(this, e.ScreenPosition.ToPoint());
        e.Handled = true;
    }
    
    private void OnNodeDelete(object sender, EventArgs e)
    {
        var component = nodeContextMenu.Tag as SkiaComponent;
        if (component != null)
        {
            drawingManager.RemoveComponent(component);
        }
    }
}
```

### Example 3: Tooltips and Hover Effects

```csharp
public class MyDiagramViewer : Control
{
    private DrawingManager drawingManager;
    private ToolTip tooltip;
    private SkiaComponent currentHoveredComponent;
    private IConnectionLine currentHoveredLine;
    
    public MyDiagramViewer()
    {
        drawingManager = new DrawingManager();
        tooltip = new ToolTip { AutoPopDelay = 5000, InitialDelay = 500 };
        
        // Handle hover changes
        drawingManager.ComponentHoverChanged += OnComponentHoverChanged;
        drawingManager.LineHoverChanged += OnLineHoverChanged;
    }
    
    private void OnComponentHoverChanged(object sender, ComponentInteractionEventArgs e)
    {
        if (e.InteractionType == InteractionType.HoverEnter)
        {
            currentHoveredComponent = e.Component;
            
            // Build tooltip text
            var tooltipText = $"{e.Component.Name}\n";
            tooltipText += $"Type: {e.Component.GetType().Name}\n";
            
            // Add component-specific info
            if (e.Component is Beep.Skia.PM.TaskNode taskNode)
            {
                tooltipText += $"Title: {taskNode.Title}\n";
                tooltipText += $"Progress: {taskNode.PercentComplete}%";
            }
            else if (e.Component is Beep.Skia.ETL.ETLSource source)
            {
                tooltipText += $"Connection: {source.ConnectionString}";
            }
            
            // Show tooltip
            tooltip.Show(tooltipText, this, e.ScreenPosition.ToPoint(), 3000);
            
            // Highlight connected lines
            HighlightConnections(e.Component);
        }
        else if (e.InteractionType == InteractionType.HoverLeave)
        {
            currentHoveredComponent = null;
            tooltip.Hide(this);
            ClearHighlights();
        }
    }
    
    private void OnLineHoverChanged(object sender, LineInteractionEventArgs e)
    {
        if (e.InteractionType == InteractionType.HoverEnter)
        {
            currentHoveredLine = e.Line;
            
            // Show line schema tooltip (for ETL lines)
            if (!string.IsNullOrEmpty(e.Line.SchemaJson))
            {
                var schemaInfo = ParseSchema(e.Line.SchemaJson);
                tooltip.Show(schemaInfo, this, e.ScreenPosition.ToPoint(), 3000);
            }
            
            // Highlight connected nodes
            if (e.Line.Start?.Component != null)
                e.Line.Start.Component.IsHighlighted = true;
            if (e.Line.End?.Component != null)
                e.Line.End.Component.IsHighlighted = true;
                
            drawingManager.RequestRedraw();
        }
        else if (e.InteractionType == InteractionType.HoverLeave)
        {
            currentHoveredLine = null;
            tooltip.Hide(this);
            ClearHighlights();
        }
    }
    
    private void HighlightConnections(SkiaComponent component)
    {
        // Find all lines connected to this component
        foreach (var line in drawingManager.Lines)
        {
            if (line.Start?.Component == component || line.End?.Component == component)
            {
                // Temporarily change line color or width to highlight
                line.Paint.StrokeWidth = 4f;
                line.Paint.Color = SKColors.Orange;
            }
        }
        drawingManager.RequestRedraw();
    }
}
```

### Example 4: Double-Click to Edit

```csharp
public class DiagramEditor : Control
{
    private DrawingManager drawingManager;
    
    public DiagramEditor()
    {
        drawingManager = new DrawingManager();
        
        // Open editor on double-click
        drawingManager.ComponentDoubleClicked += OnComponentDoubleClicked;
        drawingManager.LineDoubleClicked += OnLineDoubleClicked;
        drawingManager.DiagramDoubleClicked += OnDiagramDoubleClicked;
    }
    
    private void OnComponentDoubleClicked(object sender, ComponentInteractionEventArgs e)
    {
        // Open property editor dialog
        using (var editor = new ComponentPropertyDialog(e.Component))
        {
            if (editor.ShowDialog() == DialogResult.OK)
            {
                // Apply changes
                editor.ApplyChanges(e.Component);
                drawingManager.RequestRedraw();
            }
        }
        
        e.Handled = true;
    }
    
    private void OnLineDoubleClicked(object sender, LineInteractionEventArgs e)
    {
        // Inline edit line labels
        using (var labelEditor = new LineLabelsDialog(e.Line))
        {
            if (labelEditor.ShowDialog() == DialogResult.OK)
            {
                e.Line.Label1 = labelEditor.Label1;
                e.Line.Label2 = labelEditor.Label2;
                e.Line.Label3 = labelEditor.Label3;
                drawingManager.RequestRedraw();
            }
        }
        
        e.Handled = true;
    }
    
    private void OnDiagramDoubleClicked(object sender, DiagramInteractionEventArgs e)
    {
        // Quick add node at clicked position
        var menu = new ContextMenuStrip();
        menu.Items.Add("Add Task", null, (s, args) => AddNodeAt<Beep.Skia.PM.TaskNode>(e.CanvasPosition));
        menu.Items.Add("Add Process", null, (s, args) => AddNodeAt<Beep.Skia.Flowchart.ProcessNode>(e.CanvasPosition));
        menu.Items.Add("Add Transform", null, (s, args) => AddNodeAt<Beep.Skia.ETL.ETLTransform>(e.CanvasPosition));
        menu.Show(this, e.ScreenPosition.ToPoint());
        
        e.Handled = true;
    }
    
    private void AddNodeAt<T>(SKPoint position) where T : SkiaComponent, new()
    {
        var node = new T { X = position.X, Y = position.Y };
        drawingManager.AddComponent(node);
    }
}
```

### Example 5: Keyboard Modifiers

```csharp
private void OnComponentClicked(object sender, ComponentInteractionEventArgs e)
{
    // Ctrl+Click = Add to selection
    if (e.Modifiers.HasFlag(SKKeyModifiers.Control))
    {
        drawingManager.SelectionManager.ToggleSelection(e.Component);
        e.Handled = true;
        return;
    }
    
    // Shift+Click = Range selection
    if (e.Modifiers.HasFlag(SKKeyModifiers.Shift))
    {
        var lastSelected = drawingManager.SelectionManager.GetLastSelected();
        if (lastSelected != null)
        {
            SelectRange(lastSelected, e.Component);
            e.Handled = true;
            return;
        }
    }
    
    // Alt+Click = Quick duplicate
    if (e.Modifiers.HasFlag(SKKeyModifiers.Alt))
    {
        var duplicate = e.Component.Clone();
        duplicate.X += 20;
        duplicate.Y += 20;
        drawingManager.AddComponent(duplicate as SkiaComponent);
        e.Handled = true;
        return;
    }
    
    // Normal click = Select only this
    drawingManager.SelectionManager.SelectSingle(e.Component);
}
```

---

## Best Practices

### 1. **Always Check Event Args**
```csharp
private void OnComponentClicked(object sender, ComponentInteractionEventArgs e)
{
    if (e.Component == null) return;
    if (e.Handled) return; // Another handler already processed this
    
    // Your logic here
    
    e.Handled = true; // Mark as handled if you processed it
}
```

### 2. **Use Type Checking for Node-Specific Logic**
```csharp
private void OnComponentClicked(object sender, ComponentInteractionEventArgs e)
{
    switch (e.Component)
    {
        case Beep.Skia.PM.TaskNode taskNode:
            ShowTaskProperties(taskNode);
            break;
        case Beep.Skia.ETL.ETLSource source:
            ShowETLSourceConfig(source);
            break;
        case Beep.Skia.Flowchart.ProcessNode process:
            ShowProcessEditor(process);
            break;
        default:
            ShowGenericProperties(e.Component);
            break;
    }
}
```

### 3. **Unsubscribe When Done**
```csharp
public class MyControl : Control
{
    private DrawingManager drawingManager;
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Unsubscribe from events
            drawingManager.ComponentClicked -= OnComponentClicked;
            drawingManager.LineClicked -= OnLineClicked;
            // ... other events
        }
        base.Dispose(disposing);
    }
}
```

### 4. **Coordinate Conversion**
```csharp
// Event args provide both canvas and screen coordinates
private void OnComponentClicked(object sender, ComponentInteractionEventArgs e)
{
    // Use CanvasPosition for diagram logic
    var canvasPos = e.CanvasPosition; // Already accounts for zoom/pan
    
    // Use ScreenPosition for showing UI (menus, tooltips)
    var screenPos = e.ScreenPosition; // Window/control coordinates
    contextMenu.Show(this, screenPos.ToPoint());
}
```

### 5. **Performance: Avoid Heavy Operations in Hover Events**
```csharp
// BAD: Don't do expensive operations on every hover
private void OnComponentHoverChanged(object sender, ComponentInteractionEventArgs e)
{
    if (e.InteractionType == InteractionType.Hover)
    {
        // This fires continuously while hovering!
        LoadAllRelatedData(e.Component); // ❌ Too expensive
    }
}

// GOOD: Use HoverEnter for initialization
private void OnComponentHoverChanged(object sender, ComponentInteractionEventArgs e)
{
    if (e.InteractionType == InteractionType.HoverEnter)
    {
        // This fires once when hover starts
        LoadTooltipData(e.Component); // ✅ One-time operation
    }
}
```

### 6. **Combine with Existing Features**
```csharp
// Combine events with selection, history, and other features
private void OnComponentDoubleClicked(object sender, ComponentInteractionEventArgs e)
{
    // Create undo point before editing
    drawingManager.HistoryManager.RecordState("Edit Component");
    
    // Open editor
    using (var editor = new PropertyDialog(e.Component))
    {
        if (editor.ShowDialog() == DialogResult.OK)
        {
            // Changes applied
            drawingManager.RequestRedraw();
        }
        else
        {
            // User cancelled, undo changes
            drawingManager.HistoryManager.Undo();
        }
    }
}
```

---

## Summary

The event system provides **11 powerful events** for external handling:

### Component Events (4)
- ✅ `ComponentClicked`
- ✅ `ComponentDoubleClicked`
- ✅ `ComponentRightClicked`
- ✅ `ComponentHoverChanged`

### Line Events (4)
- ✅ `LineClicked`
- ✅ `LineDoubleClicked`
- ✅ `LineRightClicked`
- ✅ `LineHoverChanged`

### Diagram Events (3)
- ✅ `DiagramClicked`
- ✅ `DiagramDoubleClicked`
- ✅ `DiagramRightClicked`

All events provide:
- ✅ Canvas and screen coordinates
- ✅ Mouse button information
- ✅ Keyboard modifiers (Ctrl, Shift, Alt)
- ✅ Interaction type (Click, DoubleClick, Hover, etc.)
- ✅ Handled flag for event bubbling control
- ✅ Custom Tag property for your data

Use these events to build **property panels, context menus, tooltips, dialogs, navigation, and any custom UI/UX** you need!

---

**Created**: 2025-10-05  
**Version**: 1.0  
**Framework**: Beep.Skia

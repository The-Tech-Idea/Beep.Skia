# Comprehensive Event System for External Handling

The `DrawingManager` now exposes **11 powerful events** for external handling of user interactions with nodes, lines, and the diagram canvas. Perfect for building property panels, context menus, navigation, tooltips, and custom behaviors!

## 🎯 Available Events

### Component (Node) Events

#### 1. **ComponentClicked**
Fires when any node/component is single-clicked.
```csharp
public event EventHandler<ComponentInteractionEventArgs> ComponentClicked;
```
**Use Cases:**
- Select node in property panel
- Show node details in side panel
- Navigate to node details
- Update status bar

#### 2. **ComponentDoubleClicked**
Fires when any node is double-clicked (within 500ms, 5px tolerance).
```csharp
public event EventHandler<ComponentInteractionEventArgs> ComponentDoubleClicked;
```
**Use Cases:**
- Open property editor dialog
- Open node configuration form
- Enter edit mode
- Expand/collapse groups

#### 3. **ComponentRightClicked**
Fires when any node is right-clicked.
```csharp
public event EventHandler<ComponentInteractionEventArgs> ComponentRightClicked;
```
**Use Cases:**
- Show context menu
- Node-specific actions (delete, duplicate, copy, paste)
- Quick actions menu

#### 4. **ComponentHoverChanged**
Fires when hover state changes (enter/leave).
```csharp
public event EventHandler<ComponentInteractionEventArgs> ComponentHoverChanged;
```
**Use Cases:**
- Show tooltips
- Highlight related nodes
- Preview node data
- Show mini info popup

### Connection Line Events

#### 5. **LineClicked**
Fires when any connection line is single-clicked.
```csharp
public event EventHandler<LineInteractionEventArgs> LineClicked;
```
**Use Cases:**
- Select line in property panel
- Show line properties
- Highlight data flow path
- Update status bar

#### 6. **LineDoubleClicked**
Fires when a line is double-clicked.
```csharp
public event EventHandler<LineInteractionEventArgs> LineDoubleClicked;
```
**Use Cases:**
- Edit line labels
- Configure routing mode
- Edit relationship properties
- Change line style

#### 7. **LineRightClicked**
Fires when a line is right-clicked.
```csharp
public event EventHandler<LineInteractionEventArgs> LineRightClicked;
```
**Use Cases:**
- Show line context menu
- Delete connection
- Reverse direction
- Add breakpoints/waypoints

#### 8. **LineHoverChanged**
Fires when line hover state changes.
```csharp
public event EventHandler<LineInteractionEventArgs> LineHoverChanged;
```
**Use Cases:**
- Show schema tooltip (ETL)
- Display data flow info
- Show relationship details (ERD)
- Preview connection data

### Canvas/Diagram Events

#### 9. **DiagramClicked**
Fires when empty canvas area is single-clicked.
```csharp
public event EventHandler<DiagramInteractionEventArgs> DiagramClicked;
```
**Use Cases:**
- Deselect all
- Close property panels
- Clear selection
- Canvas-level actions

#### 10. **DiagramDoubleClicked**
Fires when empty canvas is double-clicked.
```csharp
public event EventHandler<DiagramInteractionEventArgs> DiagramDoubleClicked;
```
**Use Cases:**
- Add new node at position
- Create annotation
- Quick add menu
- Canvas editing

#### 11. **DiagramRightClicked**
Fires when empty canvas is right-clicked.
```csharp
public event EventHandler<DiagramInteractionEventArgs> DiagramRightClicked;
```
**Use Cases:**
- Show diagram-level context menu
- Add node submenu
- Paste operations
- Diagram settings

## 📦 Event Args Details

### ComponentInteractionEventArgs
```csharp
public class ComponentInteractionEventArgs
{
    public SkiaComponent Component { get; }          // The clicked/hovered component
    public SKPoint CanvasPosition { get; }           // Canvas coordinates
    public SKPoint ScreenPosition { get; }           // Screen coordinates
    public int MouseButton { get; }                  // 0=left, 1=right, 2=middle
    public SKKeyModifiers Modifiers { get; }         // Ctrl, Shift, Alt
    public InteractionType InteractionType { get; }  // Click, DoubleClick, HoverEnter, etc.
    public bool Handled { get; set; }                // Set true to stop propagation
    public object Tag { get; set; }                  // Custom data
}
```

### LineInteractionEventArgs
```csharp
public class LineInteractionEventArgs
{
    public IConnectionLine Line { get; }             // The clicked/hovered line
    public SKPoint CanvasPosition { get; }           // Canvas coordinates
    public SKPoint ScreenPosition { get; }           // Screen coordinates
    public int MouseButton { get; }                  // 0=left, 1=right, 2=middle
    public SKKeyModifiers Modifiers { get; }         // Ctrl, Shift, Alt
    public InteractionType InteractionType { get; }  // Click, DoubleClick, HoverEnter, etc.
    public bool IsArrowClick { get; }                // True if arrow head was clicked
    public bool Handled { get; set; }                // Set true to stop propagation
    public object Tag { get; set; }                  // Custom data
}
```

### DiagramInteractionEventArgs
```csharp
public class DiagramInteractionEventArgs
{
    public SKPoint CanvasPosition { get; }           // Canvas coordinates
    public SKPoint ScreenPosition { get; }           // Screen coordinates
    public int MouseButton { get; }                  // 0=left, 1=right, 2=middle
    public SKKeyModifiers Modifiers { get; }         // Ctrl, Shift, Alt
    public InteractionType InteractionType { get; }  // Click, DoubleClick
    public bool Handled { get; set; }                // Set true to stop propagation
    public object Tag { get; set; }                  // Custom data
}
```

### InteractionType Enum
```csharp
public enum InteractionType
{
    Click,           // Single click
    DoubleClick,     // Double click
    RightClick,      // Right click
    MiddleClick,     // Middle click
    HoverEnter,      // Mouse entered bounds
    HoverLeave,      // Mouse left bounds
    Hover,           // Hovering (continuous)
    DragStart,       // Drag started
    Dragging,        // Drag in progress
    DragEnd          // Drag ended
}
```

## 💡 Usage Examples

### Example 1: Property Panel Integration
```csharp
using Beep.Skia;

public class MyDiagramEditor
{
    private DrawingManager drawingManager;
    private PropertyPanel propertyPanel;

    public void InitializeEvents()
    {
        // Show properties when node is clicked
        drawingManager.ComponentClicked += (s, e) =>
        {
            propertyPanel.ShowProperties(e.Component);
            e.Handled = true;
        };

        // Clear properties when canvas is clicked
        drawingManager.DiagramClicked += (s, e) =>
        {
            propertyPanel.Clear();
        };

        // Edit node on double-click
        drawingManager.ComponentDoubleClicked += (s, e) =>
        {
            OpenNodeEditor(e.Component);
            e.Handled = true;
        };
    }
}
```

### Example 2: Context Menus
```csharp
// Component context menu
drawingManager.ComponentRightClicked += (s, e) =>
{
    var menu = new ContextMenu();
    menu.Items.Add(new MenuItem("Edit Properties", (s2, e2) => EditNode(e.Component)));
    menu.Items.Add(new MenuItem("Delete", (s2, e2) => DeleteNode(e.Component)));
    menu.Items.Add(new MenuItem("Duplicate", (s2, e2) => DuplicateNode(e.Component)));
    menu.Show(e.ScreenPosition);
    e.Handled = true;
};

// Line context menu
drawingManager.LineRightClicked += (s, e) =>
{
    var menu = new ContextMenu();
    menu.Items.Add(new MenuItem("Delete Connection", (s2, e2) => DeleteLine(e.Line)));
    menu.Items.Add(new MenuItem("Reverse Direction", (s2, e2) => ReverseLine(e.Line)));
    menu.Items.Add(new MenuItem("Edit Labels", (s2, e2) => EditLineLabels(e.Line)));
    menu.Show(e.ScreenPosition);
    e.Handled = true;
};

// Diagram context menu
drawingManager.DiagramRightClicked += (s, e) =>
{
    var menu = new ContextMenu();
    menu.Items.Add(new MenuItem("Add Node", CreateAddNodeSubmenu(e.CanvasPosition)));
    menu.Items.Add(new MenuItem("Paste", (s2, e2) => PasteAtPosition(e.CanvasPosition)));
    menu.Items.Add(new MenuItem("Select All", (s2, e2) => SelectAll()));
    menu.Show(e.ScreenPosition);
    e.Handled = true;
};
```

### Example 3: Tooltips & Hover Effects
```csharp
private ToolTip tooltip = new ToolTip();

// Show tooltip on component hover
drawingManager.ComponentHoverChanged += (s, e) =>
{
    if (e.InteractionType == InteractionType.HoverEnter)
    {
        var text = $"{e.Component.Name}\nType: {e.Component.GetType().Name}";
        tooltip.Show(text, e.ScreenPosition);
    }
    else if (e.InteractionType == InteractionType.HoverLeave)
    {
        tooltip.Hide();
    }
};

// Show schema info on line hover (ETL)
drawingManager.LineHoverChanged += (s, e) =>
{
    if (e.InteractionType == InteractionType.HoverEnter)
    {
        var line = e.Line;
        if (!string.IsNullOrEmpty(line.SchemaJson))
        {
            var schemaInfo = ParseSchema(line.SchemaJson);
            tooltip.Show(schemaInfo, e.ScreenPosition);
        }
    }
    else if (e.InteractionType == InteractionType.HoverLeave)
    {
        tooltip.Hide();
    }
};
```

### Example 4: Highlighting Related Components
```csharp
// Highlight connected components on hover
drawingManager.ComponentHoverChanged += (s, e) =>
{
    if (e.InteractionType == InteractionType.HoverEnter)
    {
        // Highlight component
        e.Component.IsHighlighted = true;
        
        // Highlight connected components
        foreach (var conn in e.Component.ConnectedComponents)
        {
            conn.IsHighlighted = true;
        }
        
        // Highlight connecting lines
        foreach (var line in drawingManager.Lines)
        {
            if (line.Start?.Component == e.Component || line.End?.Component == e.Component)
            {
                line.LineColor = SKColors.Orange;
            }
        }
        
        drawingManager.RequestRedraw();
    }
    else if (e.InteractionType == InteractionType.HoverLeave)
    {
        // Remove all highlights
        ClearAllHighlights();
        drawingManager.RequestRedraw();
    }
};
```

### Example 5: Keyboard Modifiers
```csharp
// Different actions based on modifiers
drawingManager.ComponentClicked += (s, e) =>
{
    if (e.Modifiers.HasFlag(SKKeyModifiers.Control))
    {
        // Ctrl+Click: Add to selection
        selectionManager.ToggleSelection(e.Component);
    }
    else if (e.Modifiers.HasFlag(SKKeyModifiers.Shift))
    {
        // Shift+Click: Extend selection
        selectionManager.ExtendSelection(e.Component);
    }
    else
    {
        // Normal click: Single selection
        selectionManager.SelectSingle(e.Component);
    }
};
```

### Example 6: Navigation & Breadcrumbs
```csharp
// Navigate to component details
drawingManager.ComponentClicked += (s, e) =>
{
    NavigationService.NavigateTo(e.Component.Id);
    BreadcrumbTrail.Add(e.Component.Name);
};

// Drill down on double-click
drawingManager.ComponentDoubleClicked += (s, e) =>
{
    if (e.Component is GroupNode group)
    {
        NavigationService.DrillDown(group);
    }
};
```

### Example 7: Quick Add Nodes
```csharp
// Double-click empty canvas to add node
drawingManager.DiagramDoubleClicked += (s, e) =>
{
    var quickAddMenu = new QuickAddMenu();
    quickAddMenu.NodeSelected += (s2, nodeType) =>
    {
        var newNode = CreateNode(nodeType);
        newNode.X = e.CanvasPosition.X;
        newNode.Y = e.CanvasPosition.Y;
        drawingManager.AddComponent(newNode);
    };
    quickAddMenu.Show(e.ScreenPosition);
};
```

## 🎨 Integration with PM/ETL/Flowchart

All node families automatically support these events:

```csharp
// PM nodes
var taskNode = new TaskNode();
drawingManager.ComponentClicked += (s, e) =>
{
    if (e.Component is TaskNode task)
    {
        ShowTaskDetails(task.Title, task.PercentComplete);
    }
};

// ETL nodes
var etlTransform = new ETLTransform();
drawingManager.ComponentDoubleClicked += (s, e) =>
{
    if (e.Component is ETLControl etl)
    {
        OpenETLEditor(etl);
    }
};

// Flowchart nodes
var processNode = new ProcessNode();
drawingManager.ComponentRightClicked += (s, e) =>
{
    if (e.Component is FlowchartControl flowchart)
    {
        ShowFlowchartContextMenu(flowchart);
    }
};
```

## 🔧 Advanced Features

### Event Propagation
Set `Handled = true` to stop event propagation:
```csharp
drawingManager.ComponentClicked += (s, e) =>
{
    if (HandleMyCustomLogic(e.Component))
    {
        e.Handled = true; // Don't fire other handlers
    }
};
```

### Custom Data with Tag
Pass custom data through events:
```csharp
drawingManager.ComponentClicked += (s, e) =>
{
    e.Tag = new { Timestamp = DateTime.Now, UserId = currentUser.Id };
};
```

### Double-Click Tuning
- **Threshold**: 500ms between clicks
- **Distance**: 5 pixels tolerance
- Modify constants in `InteractionHelper.cs` if needed

## 📊 Performance Considerations

- Events fire only for actual user interactions (not programmatic)
- Hover events throttled to drawing updates
- Double-click detection is lightweight (simple timer + distance check)
- No event queue overhead - direct invocation

## ✅ Best Practices

1. **Always check event args**: Verify component type before casting
2. **Set Handled=true**: Prevent unwanted behaviors
3. **Use weak events**: For long-lived subscriptions, consider weak event patterns
4. **Dispose properly**: Unsubscribe in Dispose() methods
5. **Async handlers**: Keep event handlers fast; use Task.Run for heavy operations
6. **Error handling**: Wrap handler logic in try-catch

## 🎯 Summary

With these 11 events, you can build:
- ✅ Property panels
- ✅ Context menus
- ✅ Tooltips
- ✅ Navigation systems
- ✅ Custom interactions
- ✅ Analytics tracking
- ✅ Collaborative features
- ✅ Undo/redo systems
- ✅ Copy/paste with preview
- ✅ LinkedIn-style infographic interactions

---

**Created**: 2025-10-05  
**Version**: 1.0  
**Compatibility**: Beep.Skia Framework with DrawingManager

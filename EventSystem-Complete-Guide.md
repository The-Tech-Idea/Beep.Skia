# Complete Event System Guide

## Overview
The Beep.Skia framework provides a comprehensive event system for handling all user interactions with diagram components, connection lines, and the canvas itself. All events now properly handle **left-click**, **right-click**, **middle-click**, **double-click**, and **hover** interactions.

## ✅ What's Now Working

### Mouse Button Detection
All mouse buttons are now properly detected and passed through the event system:
- **Left-click** (button = 0)
- **Right-click** (button = 1) ✨ **NOW WORKING!**
- **Middle-click** (button = 2)

### Event Flow
```
WinForms MouseEventArgs → SkiaHostControl → DrawingManager → InteractionHelper → Events Raised
```

The complete chain:
1. **SkiaHostControl.cs**: Converts `MouseEventArgs.Button` to int (0/1/2)
2. **DrawingManager.Interaction.cs**: Passes button to InteractionHelper
3. **InteractionHelper.cs**: Detects click type and raises appropriate event
4. **DrawingManager.Core.cs**: Event handlers receive `ComponentInteractionEventArgs`

## 11 Available Events

### Component Events
```csharp
// Single left-click
public event EventHandler<ComponentInteractionEventArgs> ComponentClicked;

// Double left-click (500ms threshold, 5px distance tolerance)
public event EventHandler<ComponentInteractionEventArgs> ComponentDoubleClicked;

// Right-click (newly working!)
public event EventHandler<ComponentInteractionEventArgs> ComponentRightClicked;

// Hover enter/leave
public event EventHandler<ComponentInteractionEventArgs> ComponentHoverChanged;
```

### Connection Line Events
```csharp
// Single left-click on line
public event EventHandler<LineInteractionEventArgs> LineClicked;

// Double left-click on line
public event EventHandler<LineInteractionEventArgs> LineDoubleClicked;

// Right-click on line (newly working!)
public event EventHandler<LineInteractionEventArgs> LineRightClicked;

// Hover enter/leave on line
public event EventHandler<LineInteractionEventArgs> LineHoverChanged;
```

### Diagram Canvas Events
```csharp
// Click on empty canvas
public event EventHandler<DiagramInteractionEventArgs> DiagramClicked;

// Double-click on empty canvas
public event EventHandler<DiagramInteractionEventArgs> DiagramDoubleClicked;

// Right-click on empty canvas (newly working!)
public event EventHandler<DiagramInteractionEventArgs> DiagramRightClicked;
```

## Event Arguments

### ComponentInteractionEventArgs
```csharp
public class ComponentInteractionEventArgs : EventArgs
{
    public IDrawableComponent Component { get; set; }  // The clicked component
    public SKPoint CanvasPosition { get; set; }        // Position in canvas coordinates
    public SKPoint ScreenPosition { get; set; }        // Position in screen coordinates
    public int MouseButton { get; set; }               // 0=Left, 1=Right, 2=Middle
    public SKKeyModifiers Modifiers { get; set; }      // Ctrl, Shift, Alt
    public InteractionType InteractionType { get; set; } // Click, DoubleClick, RightClick, etc.
    public bool Handled { get; set; }                  // Set to true to prevent further processing
    public object Tag { get; set; }                    // Optional custom data
}
```

### LineInteractionEventArgs
All properties from `ComponentInteractionEventArgs` plus:
```csharp
public IConnectionLine Line { get; set; }  // The clicked line
public bool IsArrowClick { get; set; }     // True if arrow head was clicked
```

### DiagramInteractionEventArgs
```csharp
public class DiagramInteractionEventArgs : EventArgs
{
    public SKPoint CanvasPosition { get; set; }
    public SKPoint ScreenPosition { get; set; }
    public int MouseButton { get; set; }
    public SKKeyModifiers Modifiers { get; set; }
    public InteractionType InteractionType { get; set; }
    public bool Handled { get; set; }
    public object Tag { get; set; }
}
```

### InteractionType Enum
```csharp
public enum InteractionType
{
    Click,         // Single left-click
    DoubleClick,   // Double left-click (< 500ms, < 5px apart)
    RightClick,    // Right-click (context menu trigger)
    MiddleClick,   // Middle mouse button click
    HoverEnter,    // Mouse enters component/line
    HoverLeave,    // Mouse leaves component/line
    Hover,         // Mouse hovering over component/line
    DragStart,     // Drag operation started
    Dragging,      // Drag in progress
    DragEnd        // Drag completed
}
```

## Complete Usage Examples

### Example 1: Context Menu on Right-Click ✨ NEW!
```csharp
private void SetupEvents()
{
    // Component right-click
    drawingManager.ComponentRightClicked += OnComponentRightClicked;
    
    // Line right-click
    drawingManager.LineRightClicked += OnLineRightClicked;
    
    // Diagram right-click
    drawingManager.DiagramRightClicked += OnDiagramRightClicked;
}

private void OnComponentRightClicked(object sender, ComponentInteractionEventArgs e)
{
    var contextMenu = new ContextMenuStrip();
    
    contextMenu.Items.Add("Edit Properties", null, (s, args) => 
    {
        OpenPropertyEditor(e.Component);
    });
    
    contextMenu.Items.Add("Delete", null, (s, args) => 
    {
        drawingManager.RemoveComponent(e.Component);
    });
    
    contextMenu.Items.Add("Bring to Front", null, (s, args) => 
    {
        drawingManager.BringToFront(e.Component);
    });
    
    contextMenu.Items.Add("Send to Back", null, (s, args) => 
    {
        drawingManager.SendToBack(e.Component);
    });
    
    // Show context menu at screen position
    contextMenu.Show(skiaControl, new Point((int)e.ScreenPosition.X, (int)e.ScreenPosition.Y));
}

private void OnLineRightClicked(object sender, LineInteractionEventArgs e)
{
    var contextMenu = new ContextMenuStrip();
    
    contextMenu.Items.Add("Edit Connection", null, (s, args) => 
    {
        // Open line property editor
        var editor = new ConnectionLineEditor(e.Line);
        editor.ShowDialog();
    });
    
    contextMenu.Items.Add("Animation Style", null, (s, args) => 
    {
        // Show animation style submenu
        ShowAnimationStyleMenu(e.Line, e.ScreenPosition);
    });
    
    contextMenu.Items.Add("Delete Connection", null, (s, args) => 
    {
        drawingManager.RemoveLine(e.Line);
    });
    
    contextMenu.Show(skiaControl, new Point((int)e.ScreenPosition.X, (int)e.ScreenPosition.Y));
}

private void OnDiagramRightClicked(object sender, DiagramInteractionEventArgs e)
{
    var contextMenu = new ContextMenuStrip();
    
    contextMenu.Items.Add("Add Component", null, (s, args) => 
    {
        // Show component palette
        ShowComponentPalette(e.CanvasPosition);
    });
    
    contextMenu.Items.Add("Paste", null, (s, args) => 
    {
        // Paste from clipboard at canvas position
        PasteAtPosition(e.CanvasPosition);
    });
    
    contextMenu.Items.Add("Select All", null, (s, args) => 
    {
        SelectAllComponents();
    });
    
    contextMenu.Show(skiaControl, new Point((int)e.ScreenPosition.X, (int)e.ScreenPosition.Y));
}
```

### Example 2: Property Panel on Left-Click
```csharp
private void SetupComponentSelection()
{
    drawingManager.ComponentClicked += (sender, e) =>
    {
        if (e.MouseButton == 0) // Left-click only
        {
            ShowPropertyPanel(e.Component);
            HighlightComponent(e.Component);
        }
    };
}

private void ShowPropertyPanel(IDrawableComponent component)
{
    propertyGrid.SelectedObject = component;
    propertyPanel.Visible = true;
}

private void HighlightComponent(IDrawableComponent component)
{
    // Clear previous selection
    foreach (var c in drawingManager.GetComponents())
    {
        c.IsSelected = false;
    }
    
    // Select current
    component.IsSelected = true;
    drawingManager.Invalidate();
}
```

### Example 3: Tooltips on Hover
```csharp
private ToolTip tooltip = new ToolTip();

private void SetupHoverTooltips()
{
    drawingManager.ComponentHoverChanged += OnComponentHoverChanged;
    drawingManager.LineHoverChanged += OnLineHoverChanged;
}

private void OnComponentHoverChanged(object sender, ComponentInteractionEventArgs e)
{
    if (e.InteractionType == InteractionType.HoverEnter)
    {
        string tooltipText = $"{e.Component.Name}\n" +
                           $"Type: {e.Component.GetType().Name}\n" +
                           $"Position: ({e.Component.X:F0}, {e.Component.Y:F0})";
        
        tooltip.Show(tooltipText, skiaControl, 
                    new Point((int)e.ScreenPosition.X, (int)e.ScreenPosition.Y + 20),
                    3000);
    }
    else if (e.InteractionType == InteractionType.HoverLeave)
    {
        tooltip.Hide(skiaControl);
    }
}

private void OnLineHoverChanged(object sender, LineInteractionEventArgs e)
{
    if (e.InteractionType == InteractionType.HoverEnter)
    {
        var line = e.Line;
        string tooltipText = $"Connection: {line.Source?.Name} → {line.Target?.Name}\n" +
                           $"Animation: {line.AnimationStyle}";
        
        tooltip.Show(tooltipText, skiaControl,
                    new Point((int)e.ScreenPosition.X, (int)e.ScreenPosition.Y + 20),
                    2000);
    }
    else if (e.InteractionType == InteractionType.HoverLeave)
    {
        tooltip.Hide(skiaControl);
    }
}
```

### Example 4: Double-Click to Edit
```csharp
private void SetupDoubleClickEdit()
{
    drawingManager.ComponentDoubleClicked += (sender, e) =>
    {
        OpenComponentEditor(e.Component);
    };
    
    drawingManager.LineDoubleClicked += (sender, e) =>
    {
        OpenLineEditor(e.Line);
    };
    
    drawingManager.DiagramDoubleClicked += (sender, e) =>
    {
        // Double-click on empty canvas - create new component
        CreateComponentAtPosition(e.CanvasPosition);
    };
}

private void OpenComponentEditor(IDrawableComponent component)
{
    var dialog = new ComponentEditorDialog(component);
    if (dialog.ShowDialog() == DialogResult.OK)
    {
        drawingManager.Invalidate();
    }
}

private void OpenLineEditor(IConnectionLine line)
{
    var dialog = new ConnectionLineEditorDialog(line);
    if (dialog.ShowDialog() == DialogResult.OK)
    {
        drawingManager.Invalidate();
    }
}

private void CreateComponentAtPosition(SKPoint position)
{
    var newComponent = new FlowchartNode
    {
        X = position.X - 50,
        Y = position.Y - 25,
        Width = 100,
        Height = 50,
        Name = $"Node {drawingManager.GetComponents().Count() + 1}"
    };
    drawingManager.AddComponent(newComponent);
}
```

### Example 5: Keyboard Modifiers
```csharp
private void SetupModifierHandling()
{
    drawingManager.ComponentClicked += (sender, e) =>
    {
        // Ctrl+Click: Add to selection
        if ((e.Modifiers & SKKeyModifiers.Control) != 0)
        {
            e.Component.IsSelected = !e.Component.IsSelected;
            e.Handled = true;
        }
        
        // Shift+Click: Select range
        else if ((e.Modifiers & SKKeyModifiers.Shift) != 0)
        {
            SelectRange(lastSelectedComponent, e.Component);
            e.Handled = true;
        }
        
        // Alt+Click: Toggle visibility
        else if ((e.Modifiers & SKKeyModifiers.Alt) != 0)
        {
            e.Component.IsVisible = !e.Component.IsVisible;
            drawingManager.Invalidate();
            e.Handled = true;
        }
        
        // Plain click: Single selection
        else
        {
            ClearSelection();
            e.Component.IsSelected = true;
        }
        
        lastSelectedComponent = e.Component;
    };
}

private IDrawableComponent lastSelectedComponent;

private void SelectRange(IDrawableComponent start, IDrawableComponent end)
{
    var components = drawingManager.GetComponents().ToList();
    int startIndex = components.IndexOf(start);
    int endIndex = components.IndexOf(end);
    
    if (startIndex < 0 || endIndex < 0) return;
    
    int min = Math.Min(startIndex, endIndex);
    int max = Math.Max(startIndex, endIndex);
    
    for (int i = min; i <= max; i++)
    {
        components[i].IsSelected = true;
    }
    
    drawingManager.Invalidate();
}

private void ClearSelection()
{
    foreach (var c in drawingManager.GetComponents())
    {
        c.IsSelected = false;
    }
}
```

### Example 6: Conditional Event Handling
```csharp
private void SetupConditionalHandling()
{
    drawingManager.ComponentClicked += (sender, e) =>
    {
        // Only handle specific component types
        if (e.Component is FlowchartNode flowNode)
        {
            HandleFlowchartNodeClick(flowNode, e);
        }
        else if (e.Component is ERDEntity erdEntity)
        {
            HandleERDEntityClick(erdEntity, e);
        }
        
        // Check if component has specific properties
        if (e.Component.Tag is ProcessData processData)
        {
            ShowProcessDetails(processData);
        }
    };
    
    drawingManager.LineClicked += (sender, e) =>
    {
        // Only handle animated lines
        if (e.Line.ShowDataFlow && e.Line.AnimationStyle != FlowAnimationStyle.Dots)
        {
            ShowAnimationControls(e.Line);
        }
        
        // Handle arrow clicks differently
        if (e.IsArrowClick)
        {
            ShowArrowProperties(e.Line);
        }
    };
}

private void HandleFlowchartNodeClick(FlowchartNode node, ComponentInteractionEventArgs e)
{
    Console.WriteLine($"Flowchart node clicked: {node.Name}");
    Console.WriteLine($"Shape: {node.Shape}, Size: {node.Width}x{node.Height}");
}

private void HandleERDEntityClick(ERDEntity entity, ComponentInteractionEventArgs e)
{
    Console.WriteLine($"ERD entity clicked: {entity.Name}");
    Console.WriteLine($"Columns: {entity.Children.Count}");
}

private void ShowProcessDetails(ProcessData data)
{
    var detailsForm = new ProcessDetailsForm(data);
    detailsForm.Show();
}

private void ShowAnimationControls(IConnectionLine line)
{
    var panel = new AnimationControlPanel(line);
    panel.Show();
}

private void ShowArrowProperties(IConnectionLine line)
{
    var dialog = new ArrowPropertiesDialog(line);
    dialog.ShowDialog();
}
```

## Testing Your Event Implementation

### Simple Console Test
```csharp
private void TestAllEvents()
{
    // Component events
    drawingManager.ComponentClicked += (s, e) => 
        Console.WriteLine($"Component clicked: {e.Component.Name} (Button: {e.MouseButton})");
    
    drawingManager.ComponentDoubleClicked += (s, e) => 
        Console.WriteLine($"Component double-clicked: {e.Component.Name}");
    
    drawingManager.ComponentRightClicked += (s, e) => 
        Console.WriteLine($"Component right-clicked: {e.Component.Name} ✨");
    
    drawingManager.ComponentHoverChanged += (s, e) => 
        Console.WriteLine($"Component hover: {e.Component.Name} - {e.InteractionType}");
    
    // Line events
    drawingManager.LineClicked += (s, e) => 
        Console.WriteLine($"Line clicked: {e.Line.Source?.Name} → {e.Line.Target?.Name} (Button: {e.MouseButton})");
    
    drawingManager.LineDoubleClicked += (s, e) => 
        Console.WriteLine($"Line double-clicked: {e.Line.Source?.Name} → {e.Line.Target?.Name}");
    
    drawingManager.LineRightClicked += (s, e) => 
        Console.WriteLine($"Line right-clicked: {e.Line.Source?.Name} → {e.Line.Target?.Name} ✨");
    
    drawingManager.LineHoverChanged += (s, e) => 
        Console.WriteLine($"Line hover: {e.InteractionType}");
    
    // Diagram events
    drawingManager.DiagramClicked += (s, e) => 
        Console.WriteLine($"Diagram clicked at ({e.CanvasPosition.X:F0}, {e.CanvasPosition.Y:F0}) (Button: {e.MouseButton})");
    
    drawingManager.DiagramDoubleClicked += (s, e) => 
        Console.WriteLine($"Diagram double-clicked at ({e.CanvasPosition.X:F0}, {e.CanvasPosition.Y:F0})");
    
    drawingManager.DiagramRightClicked += (s, e) => 
        Console.WriteLine($"Diagram right-clicked at ({e.CanvasPosition.X:F0}, {e.CanvasPosition.Y:F0}) ✨");
}
```

### WinForms Test Application
```csharp
public class EventTestForm : Form
{
    private Beep_Skia_Control skiaControl;
    private DrawingManager drawingManager;
    private ListBox eventLog;
    
    public EventTestForm()
    {
        InitializeComponents();
        SetupEvents();
        CreateTestDiagram();
    }
    
    private void InitializeComponents()
    {
        this.Text = "Event System Test";
        this.Size = new Size(1200, 800);
        
        // Skia control
        skiaControl = new Beep_Skia_Control
        {
            Dock = DockStyle.Fill
        };
        
        // Event log
        eventLog = new ListBox
        {
            Dock = DockStyle.Right,
            Width = 400,
            Font = new Font("Consolas", 9)
        };
        
        var splitter = new Splitter
        {
            Dock = DockStyle.Right,
            Width = 3
        };
        
        this.Controls.Add(skiaControl);
        this.Controls.Add(splitter);
        this.Controls.Add(eventLog);
        
        drawingManager = new DrawingManager();
        skiaControl.DrawingManager = drawingManager;
    }
    
    private void SetupEvents()
    {
        // Log all events
        drawingManager.ComponentClicked += (s, e) => 
            LogEvent($"ComponentClicked: {e.Component.Name} (Btn:{e.MouseButton})");
        
        drawingManager.ComponentDoubleClicked += (s, e) => 
            LogEvent($"ComponentDoubleClicked: {e.Component.Name}");
        
        drawingManager.ComponentRightClicked += (s, e) => 
            LogEvent($"ComponentRightClicked: {e.Component.Name} ✨");
        
        drawingManager.ComponentHoverChanged += (s, e) => 
            LogEvent($"ComponentHover: {e.Component.Name} - {e.InteractionType}");
        
        drawingManager.LineClicked += (s, e) => 
            LogEvent($"LineClicked: {e.Line.Source?.Name}→{e.Line.Target?.Name} (Btn:{e.MouseButton})");
        
        drawingManager.LineDoubleClicked += (s, e) => 
            LogEvent($"LineDoubleClicked: {e.Line.Source?.Name}→{e.Line.Target?.Name}");
        
        drawingManager.LineRightClicked += (s, e) => 
            LogEvent($"LineRightClicked: {e.Line.Source?.Name}→{e.Line.Target?.Name} ✨");
        
        drawingManager.LineHoverChanged += (s, e) => 
            LogEvent($"LineHover: {e.InteractionType}");
        
        drawingManager.DiagramClicked += (s, e) => 
            LogEvent($"DiagramClicked: ({e.CanvasPosition.X:F0},{e.CanvasPosition.Y:F0}) (Btn:{e.MouseButton})");
        
        drawingManager.DiagramDoubleClicked += (s, e) => 
            LogEvent($"DiagramDoubleClicked: ({e.CanvasPosition.X:F0},{e.CanvasPosition.Y:F0})");
        
        drawingManager.DiagramRightClicked += (s, e) => 
            LogEvent($"DiagramRightClicked: ({e.CanvasPosition.X:F0},{e.CanvasPosition.Y:F0}) ✨");
    }
    
    private void LogEvent(string message)
    {
        eventLog.Items.Insert(0, $"{DateTime.Now:HH:mm:ss.fff} - {message}");
        if (eventLog.Items.Count > 100)
        {
            eventLog.Items.RemoveAt(eventLog.Items.Count - 1);
        }
    }
    
    private void CreateTestDiagram()
    {
        var node1 = new FlowchartNode
        {
            X = 100, Y = 100,
            Width = 120, Height = 60,
            Name = "Start Node",
            Shape = ComponentShape.RoundedRectangle
        };
        
        var node2 = new FlowchartNode
        {
            X = 300, Y = 100,
            Width = 120, Height = 60,
            Name = "Process Node",
            Shape = ComponentShape.Rectangle
        };
        
        drawingManager.AddComponent(node1);
        drawingManager.AddComponent(node2);
        drawingManager.ConnectComponents(node1, node2);
    }
}
```

## Best Practices

### 1. Always Check MouseButton
```csharp
drawingManager.ComponentClicked += (s, e) =>
{
    if (e.MouseButton == 0) // Left-click
    {
        // Selection logic
    }
    else if (e.MouseButton == 1) // Right-click
    {
        // Context menu (though ComponentRightClicked is better)
    }
};
```

### 2. Use Specific Events When Possible
```csharp
// ✅ GOOD: Use the specific event
drawingManager.ComponentRightClicked += ShowContextMenu;

// ⚠️ OK: But less clear
drawingManager.ComponentClicked += (s, e) =>
{
    if (e.MouseButton == 1)
        ShowContextMenu(s, e);
};
```

### 3. Set Handled Flag to Prevent Propagation
```csharp
drawingManager.ComponentClicked += (s, e) =>
{
    if (ShouldPreventDefaultBehavior(e.Component))
    {
        e.Handled = true;
        // Custom handling here
    }
};
```

### 4. Unsubscribe When Done
```csharp
public class DiagramEditor : IDisposable
{
    private DrawingManager _drawingManager;
    
    public DiagramEditor(DrawingManager manager)
    {
        _drawingManager = manager;
        _drawingManager.ComponentClicked += OnComponentClicked;
        _drawingManager.ComponentRightClicked += OnComponentRightClicked;
    }
    
    public void Dispose()
    {
        _drawingManager.ComponentClicked -= OnComponentClicked;
        _drawingManager.ComponentRightClicked -= OnComponentRightClicked;
    }
}
```

### 5. Use InteractionType for Precise Control
```csharp
drawingManager.ComponentHoverChanged += (s, e) =>
{
    switch (e.InteractionType)
    {
        case InteractionType.HoverEnter:
            StartHoverAnimation(e.Component);
            break;
        case InteractionType.HoverLeave:
            StopHoverAnimation(e.Component);
            break;
    }
};
```

### 6. Combine with Modifiers for Power Features
```csharp
drawingManager.ComponentClicked += (s, e) =>
{
    bool ctrlPressed = (e.Modifiers & SKKeyModifiers.Control) != 0;
    bool shiftPressed = (e.Modifiers & SKKeyModifiers.Shift) != 0;
    bool altPressed = (e.Modifiers & SKKeyModifiers.Alt) != 0;
    
    if (ctrlPressed && shiftPressed)
    {
        // Ctrl+Shift+Click: Clone component
        CloneComponent(e.Component);
    }
    else if (ctrlPressed)
    {
        // Ctrl+Click: Multi-select
        ToggleSelection(e.Component);
    }
    else if (shiftPressed)
    {
        // Shift+Click: Range select
        SelectRange(e.Component);
    }
};
```

## Troubleshooting

### Q: Right-click events not firing?
**A:** Make sure you're using the latest build. The right-click support was just added!

### Q: Getting events twice?
**A:** Check if you subscribed to the event multiple times. Unsubscribe before resubscribing.

### Q: Hover events firing too frequently?
**A:** Use `InteractionType.HoverEnter` and `HoverLeave` instead of checking every hover event.

### Q: Double-click not detected?
**A:** Double-click threshold is 500ms with 5px distance tolerance. Make sure clicks are close together in time and space.

### Q: Events not firing for child components?
**A:** Child components may consume the event. Check the `Handled` flag in parent handlers.

## Summary

The Beep.Skia event system now provides complete support for:
- ✅ **Left-click** (single and double)
- ✅ **Right-click** (context menus) ✨ NEW!
- ✅ **Middle-click**
- ✅ **Hover** (enter/leave)
- ✅ **Keyboard modifiers** (Ctrl, Shift, Alt)
- ✅ **Canvas coordinates** and **screen coordinates**
- ✅ **Handled flag** for event propagation control

All 11 events are fully functional and production-ready! 🎉

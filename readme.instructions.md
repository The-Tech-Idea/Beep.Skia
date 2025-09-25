# Beep.Skia – Usage and Extension Guide

This guide walks you through using the Beep.Skia UI/diagramming framework, wiring it into an app (WinForms host), and extending it with your own component families.

## What is Beep.Skia?
Beep.Skia is a SkiaSharp-based, cross-platform 2D UI framework focused on diagramming/workflow, with Material Design 3.0 controls and multiple domain “families” (e.g., Flowchart, DFD, ERD, PM, Network, ETL). It centralizes selection, rendering, interactions, lines, and persistence.

Key projects:
- `Beep.Skia` – Core framework: base `SkiaComponent`, `DrawingManager`, `ConnectionLine`, selection, hit-testing, rendering helpers.
- `Beep.Skia.Model` – Interfaces/contracts (`IConnectionPoint`, `IConnectionLine`, enums, event args).
- Family libraries (optional):
  - `Beep.Skia.FlowChart`, `Beep.Skia.DFD`, `Beep.Skia.ERD`, `Beep.Skia.PM`, `Beep.Skia.Network`, `Beep.Skia.ETL`, etc.
    - `Beep.Skia.MindMap` – Material-themed mind map nodes.
- `Beep.Skia.Winform.Controls` – Host control/palette integration for WinForms samples.
- `Beep.Skia.Sample.WinForms` – Example host application.

Core concepts:
- Components inherit `SkiaComponent` (or a family base like `FlowchartControl`, `DFDControl`, `PMControl`).
- Components expose input/output `IConnectionPoint` collections for wiring.
- `DrawingManager` coordinates components, lines, selection, zoom/pan, persistence.
- One `ConnectionLine` class draws all line types; ERD multiplicities and data-flow animation are supported via properties.

## Build and Run
Requirements: .NET 8+ SDK; Windows or cross-platform .NET with SkiaSharp.

- Build the full solution:
  ```powershell
  dotnet build "Beep.Skia.Solution.sln"
  ```
- Build just the core library:
  ```powershell
  dotnet build "Beep.Skia/Beep.Skia.csproj" -f net8.0
  ```
- Run the sample WinForms app:
  ```powershell
  dotnet build "Beep.Skia.Sample.WinForms/Beep.Skia.Sample.WinForms.csproj" -f net8.0
  dotnet run --project "Beep.Skia.Sample.WinForms/Beep.Skia.Sample.WinForms.csproj" -f net8.0
  ```

Notes:
- Projects multi-target `net8.0;net9.0` where applicable.
- Custom post-build targets may copy artifacts to `outputDLL` and/or local NuGet drop if configured.

## Using Beep.Skia in Your App (WinForms)
1) Add references to the core and any desired families (e.g., FlowChart, DFD, ERD, PM) in your WinForms project.

2) Place the Skia host control on your form and wire its `DrawingManager`:
```csharp
using Beep.Skia;
using Beep.Skia.Winform.Controls;

public partial class MainForm : Form
{
    private readonly DrawingManager _drawingManager = new();

    public MainForm()
    {
        InitializeComponent();
        var skiaHost = new SkiaHostControl
        {
            Dock = DockStyle.Fill
        };
        Controls.Add(skiaHost);

        skiaHost.DrawingManager = _drawingManager;
        // Optionally: set initial zoom/pan
        _drawingManager.Zoom = 1.0f;
        _drawingManager.PanOffset = new SKPoint(0, 0);

        // Add components and lines here or via a palette UI
        LoadDemoDiagram();
    }

    private void LoadDemoDiagram()
    {
        // Example: create two components and connect them
        var a = new Beep.Skia.Flowchart.ProcessNode { X = 100, Y = 100, Width = 120, Height = 60, Name = "A" };
        var b = new Beep.Skia.Flowchart.DecisionNode { X = 300, Y = 100, Width = 120, Height = 80, Name = "B" };

        _drawingManager.AddComponent(a);
        _drawingManager.AddComponent(b);

        // Use first OUT of A to first IN of B
        var start = a.OutConnectionPoints.First();
        var end = b.InConnectionPoints.First();
        var line = new ConnectionLine(start, end, () => skiaHost.Invalidate())
        {
            FlowDirection = DataFlowDirection.Forward,
            ShowEndArrow = true
        };
        _drawingManager.AddLine(line);

        // Render
        skiaHost.Invalidate();
    }
}
```

3) Interactions provided out-of-the-box:
- Select/move/resize components (subject to each component’s handles/logic).
- Draw lines between compatible ports.
- Zoom/pan via `DrawingManager` and host control helpers.

## Persistence (Save/Load)
Use `DrawingManager.ToDto()` and `LoadFromDto()` to capture/restore diagrams.
- Components are persisted with type, geometry, name.
- Connection points carry GUIDs; lines reference endpoints by GUID.
- Family-specific options are persisted in a `PropertyBag` (e.g., Flowchart port counts/placement flags, PM Title/Label/Percent). These are applied before restoring connection point IDs to preserve stable reconnect behavior.

Example:
```csharp
var dto = _drawingManager.ToDto();
var json = System.Text.Json.JsonSerializer.Serialize(dto);
// ... persist json

// later
var loaded = System.Text.Json.JsonSerializer.Deserialize<DiagramDto>(json);
_drawingManager.LoadFromDto(loaded);
```

## Lines and ERD Multiplicity Presets
- `ConnectionLine` supports: 
  - `RoutingMode`, `FlowDirection`, arrows, labels (positions), dash patterns.
  - Data flow animation (`IsDataFlowAnimated`, `DataFlowSpeed`, `DataFlowParticleSize`, `DataFlowColor`).
  - ERD multiplicity on start/end (`StartMultiplicity`, `EndMultiplicity`).
- For ERD quick-connect, you can set a one-shot multiplicity preset:
```csharp
_drawingManager.SetNextLineMultiplicityPreset(ERDMultiplicity.One, ERDMultiplicity.Many);
// Next created line will use the preset, then preset clears
```

## Centralized Connection Points
- All `IConnectionPoint`s are registered centrally when a component is added.
- The framework updates the registry on component moves/resizes and after load, ensuring the line endpoints remain correct.
- Components are responsible for computing the exact port geometry in their `LayoutPorts` (or equivalent), while `DrawingManager` listens to `BoundsChanged` and refreshes the registry in one place.

## Extending the Framework
You can extend Beep.Skia by creating new components or entire families.

### 1) Create a Component
- Inherit from `SkiaComponent` or a family base (e.g., `FlowchartControl`, `DFDControl`, `PMControl`).
- Implement `DrawContent(SKCanvas, DrawingContext)`.
- Manage in/out ports and place them in `LayoutPorts()` (or `UpdateConnectionPointPositions()` for legacy patterns).
- Call `InvalidateVisual()` (if available) or trigger a redraw when properties change.

Example skeleton:
```csharp
public class MyNode : Beep.Skia.Flowchart.FlowchartControl
{
    public MyNode()
    {
        Width = 120; Height = 60; Name = "MyNode";
        InPortCount = 1; OutPortCount = 1; // family base will ensure ports and call LayoutPorts
    }

    protected override void DrawContent(SKCanvas canvas, DrawingContext context)
    {
        // Draw shape
        using var fill = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill, IsAntialias = true };
        using var border = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 1.25f, IsAntialias = true };
        var rect = new SKRoundRect(new SKRect(X, Y, X + Width, Y + Height), 8, 8);
        canvas.DrawRoundRect(rect, fill);
        canvas.DrawRoundRect(rect, border);

        // Optionally draw port visuals if your family base doesn’t
    }

    protected override void LayoutPorts()
    {
        // Place InConnectionPoints and OutConnectionPoints; set Center/Position/Bounds
        // Example: distribute vertically along left/right edges
        float top = Y + 6f; float bottom = Y + Height - 6f; float left = X; float right = X + Width;
        PositionAlongEdge(InConnectionPoints, new SKPoint(left, 0), top, bottom, -1);
        PositionAlongEdge(OutConnectionPoints, new SKPoint(right, 0), top, bottom, +1);
    }

    private static void PositionAlongEdge(IList<IConnectionPoint> ports, SKPoint edge, float top, float bottom, int dir)
    {
        int n = Math.Max(ports.Count, 1);
        float span = Math.Max(bottom - top, 1f);
        for (int i = 0; i < ports.Count; i++)
        {
            var p = ports[i];
            float t = (i + 1) / (float)(n + 1);
            float cy = top + t * span;
            float cx = edge.X + dir * 8f; // port offset
            p.Center = new SKPoint(cx, cy);
            p.Position = p.Center;
            p.Bounds = new SKRect(cx - 4, cy - 4, cx + 4, cy + 4);
            p.Rect = p.Bounds;
            p.Index = i;
            p.Component = this;
            p.IsAvailable = true;
        }
    }
}
```

### 2) Register and Use the Component
- Simply add an instance with `_drawingManager.AddComponent(new MyNode { ... });`.
- Connection points are automatically registered and discoverable by lines.

### 3) Build a New Family Library (Optional)
- Create a new class library (e.g., `Beep.Skia.MyFamily`). Target `net8.0;net9.0` to match the framework.
- Add a base like `MyFamilyControl : SkiaComponent` to share common visuals and `LayoutPorts` helpers for the family.
- Provide several concrete nodes (e.g., `MyFamily.Start`, `MyFamily.Process`, etc.).
- If you use the WinForms sample/palette discovery, naming your namespace with `.MyFamily` makes grouping easier in the palette.

### 4) Persistence of Custom Properties
- Custom properties can be persisted using `ComponentDto.PropertyBag` without breaking versioning.
- Extend `DrawingManager.ToDto()` and `.LoadFromDto()` to save/load your family’s options before assigning port IDs.

Example (pattern used by Flowchart and PM):
```csharp
// ToDto: set comp.PropertyBag["YourKey"] = value
// LoadFromDto: if (comp.PropertyBag.TryGetValue("YourKey", out var s)) { /* parse & assign */ }
```

### 5) Clean Resource Management
- Dispose SkiaSharp resources (`SKPaint`, `SKPath`, etc.) if you cache them on the component.
- Follow the standard disposable pattern.

## Interaction & Rendering Helpers
- `RenderingHelper` handles pan/zoom once globally; components draw at absolute `X, Y`.
- `SelectionManager` provides rubber-band/multi-select and hit-testing.
- `InteractionHelper` routes mouse events for selection, dragging, and line drawing.

## Tips & Conventions
- Keep `LayoutPorts()` fast; avoid per-frame allocations.
- Set `IConnectionPoint.Center`, `Position`, and `Bounds` consistently; lines consume `Position`.
- Use `MaterialDesignColors` (where applicable) for consistent theming.
- For property-driven visuals, trigger a redraw when values change.
- Use `Id` (GUID) on connection points and components for stable persistence.

## Troubleshooting
- Ports not lining up with shapes? Ensure `LayoutPorts()` uses the actual shape bounds (rounded corners, ellipses, headers) and sets both `Center` and `Position`.
- Lines not reconnecting after load? Verify you apply family options from `PropertyBag` before restoring connection point IDs, and that counts match.
- Nothing draws? Ensure host control is attached to a `DrawingManager` and you call `Invalidate()` after changes or use the built-in `DrawSurface` event.

---
For deeper architectural notes and patterns, also see `.github/copilot-instructions.md` and the existing family base classes (Flowchart/DFD/ERD/PM) inside the repo.

## Mind Map Family
Namespace: `Beep.Skia.MindMap`

Mind Map provides lightweight nodes for brainstorming diagrams with Material theming and standardized ports.

Nodes:
- `CentralNode`: Elliptical/capsule center with multiple outward outputs, no inputs.
- `TopicNode`: Rounded rectangle with 1 input (left) and multiple outputs (right).
- `SubTopicNode`: Smaller variant themed with `SurfaceVariant`.
- `NoteNode`: Dog-ear note shape; 1 input, 0 outputs; supports `Notes` body text.

Common base: `MindMapControl : MaterialControl`
- Provides `EnsurePortCounts(in, out)`, default left-in/right-out layout, and `LayoutOutputsOnEllipse()` for central nodes.
- Port tokens: inputs use `SecondaryContainer`, outputs use `Primary`.
- Exposes `InPortCount`/`OutPortCount` for editors and persistence.

Properties persisted (via `DrawingManager.ToDto()`):
- `Title`, `Notes` on each node type.
- For port counts: `InPortCount`, `OutPortCount`.

Usage example:
```csharp
using Beep.Skia.MindMap;

var center = new CentralNode { X = 200, Y = 120, Title = "Project" };
var topic = new TopicNode { X = 460, Y = 140, Title = "Requirements" };
_drawingManager.AddComponent(center);
_drawingManager.AddComponent(topic);
var line = new ConnectionLine(center.OutConnectionPoints[0], topic.InConnectionPoints[0], () => host.Invalidate()) { ShowEndArrow = true };
_drawingManager.AddLine(line);
```

Persistence notes:
- Mind Map options are applied before restoring connection point IDs to preserve stable reconnects.
- Keys used: `Title`, `Notes`, `InPortCount`, `OutPortCount`.

# Beep.Skia Framework - AI Coding Agent Instructions

## Project Overview
This is a cross-platform SkiaSharp-based UI framework for creating workflow diagrams, automation components, and Material Design 3.0 controls. The framework consists of multiple projects working together to provide a comprehensive 2D graphics and component system.

## Architecture & Key Components

### Core Framework Structure
```
Beep.Skia/                    # Main framework implementation
├── SkiaComponent.cs         # Abstract base class for all drawable components
├── MaterialControl.cs       # Material Design 3.0 base class with color tokens
├── DrawingManager.cs        # Central coordinator (split into partial classes)
├── Components/              # Material Design 3.0 UI components
└── Extensions/             # Framework extensions and utilities

Beep.Skia.Model/            # Interfaces and data contracts
├── IConnectionLine.cs      # Connection line interface
├── IConnectionPoint.cs     # Connection point interface
└── Enums.cs               # Component shapes, types, and states

Beep.Automation/            # Workflow automation system
├── Models/                 # Data models for scenarios, modules, variables
└── Implementation/        # Business logic implementations
```

### Essential Patterns

#### 1. Component Inheritance Hierarchy
```csharp
// All visual components inherit from SkiaComponent
public abstract class SkiaComponent : IDrawableComponent, IDisposable
{
    // Core properties: X, Y, Width, Height, Name, Tag
    // Event handling: Clicked, Mouse events
    // State management: IsVisible, IsEnabled, Opacity
}

// Material Design components inherit from MaterialControl
public abstract class MaterialControl : SkiaComponent
{
    // Material Design 3.0 color tokens (protected static MaterialColors)
    // State layer opacities for hover/press/focus
    // SVG support for icons
}
```

#### 2. Drawing Pattern
```csharp
// All components implement DrawContent method
public abstract void DrawContent(SKCanvas canvas, DrawingContext context);

// DrawingContext provides:
// - Canvas bounds and transformation
// - Current mouse position and interaction state
// - Parent component reference
```

#### 3. Event Handling
```csharp
// Mouse events with SkiaSharp coordinates
public virtual void OnMouseDown(SKPoint point, InteractionContext context)
public virtual void OnMouseMove(SKPoint point, InteractionContext context)
public virtual void OnMouseUp(SKPoint point, InteractionContext context)

// InteractionContext contains:
// - Mouse button state (context.MouseButton)
// - Keyboard modifiers
// - Interaction type (Click, Drag, Hover)
```

#### 4. Material Design 3.0 Integration
```csharp
// Use MaterialDesignColors for consistent theming
SKColor backgroundColor = MaterialDesignColors.Surface;
SKColor textColor = MaterialDesignColors.OnSurface;

// State layers for interactive elements
float hoverOpacity = StateLayerOpacity.Hover; // 0.08f
```

#### 5. Generic property access and containers (2025-09)
```csharp
// Base: SkiaComponent
// Exposes metadata-driven NodeProperties for editor-driven settings.
public Dictionary<string, ParameterInfo> NodeProperties { get; }

// New: retrieve/apply properties in a generic way
public virtual Dictionary<string, object> GetProperties(
    bool includeCommon = true,
    bool includeNodeProperties = true);

// Note spelling + alias: SetPropperties + SetProperties
public virtual void SetPropperties(IDictionary<string, object> props,
    bool updateNodeProperties = true,
    bool applyToPublicSetters = true);
public void SetProperties(IDictionary<string, object> props,
    bool updateNodeProperties = true,
    bool applyToPublicSetters = true);

// Type conversion supported: string/bool/int/float/double/enum/TimeSpan/SKColor.
// Example
component.SetProperties(new Dictionary<string, object>{
  ["X"] = 100,
  ["Y"] = 50,
  ["Width"] = 200,
  ["PrimaryColor"] = "#FF2196F3",   // SKColor parse supported
  ["Interval"] = "00:05:00"         // TimeSpan parse supported
});

// Container helpers: alongside ordered Children, use ChildNodes for named lookup
public List<SkiaComponent> Children { get; }
public Dictionary<string, SkiaComponent> ChildNodes { get; }
// AddChild/RemoveChild keep both collections in sync; ChildNodes key usually child.Name.
```

## Development Workflow

### Building the Project
```bash
# Build all projects
dotnet build Beep.Skia.Solution.sln

# Build specific project
dotnet build Beep.Skia/Beep.Skia.csproj

# Build with specific target framework
dotnet build Beep.Skia/Beep.Skia.csproj -f net8.0
```

### Key Build Targets
- **CopyPackage**: Copies NuGet packages to `..\..\..\LocalNugetFiles`
- **PostBuild**: Copies DLLs to `..\..\outputDLL\$(PackageId)\$(TargetFramework)`

### Multi-Framework Support
- **Target Frameworks**: `net8.0; net9.0`
- **Implicit Usings**: Enabled
- **LangVersion**: Latest

## Component Development Guidelines

### 1. Creating New Components
```csharp
public class MyComponent : MaterialControl
{
    public MyComponent()
    {
        // Initialize component properties
        Width = 100;
        Height = 50;
    }

    public override void DrawContent(SKCanvas canvas, DrawingContext context)
    {
        // Implement drawing logic using SKCanvas methods
        // Use MaterialDesignColors for theming
        // Handle bounds checking with context.Bounds
    }

    public override void OnMouseDown(SKPoint point, InteractionContext context)
    {
        // Handle mouse interactions
        // Call base.OnMouseDown for default behavior
        base.OnMouseDown(point, context);
    }
}
```

### 2. Component Registration
```csharp
// Add to DrawingManager
drawingManager.AddComponent(myComponent);

// Connect components (if applicable)
drawingManager.ConnectComponents(component1, component2);
```

### 3. Memory Management
```csharp
// Always implement IDisposable for components with resources
public class MyComponent : MaterialControl, IDisposable
{
    private SKPaint _paint;
    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _paint?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
```

## Common Patterns & Conventions

### 1. Property Change Notifications
```csharp
private string _text = "";
public string Text
{
    get => _text;
    set
    {
        if (_text != value)
        {
            _text = value ?? "";
            InvalidateVisual(); // Trigger redraw
        }
    }
}
```

### 2. Bounds Checking
```csharp
public override void DrawContent(SKCanvas canvas, DrawingContext context)
{
    if (!context.Bounds.IntersectsWith(Bounds))
        return; // Skip drawing if outside visible area
}
```

### 3. Color Usage
```csharp
// Use Material Design colors consistently
SKColor backgroundColor = IsHovered ?
    MaterialDesignColors.PrimaryContainer :
    MaterialDesignColors.Surface;

// Apply state layers for interactions
SKColor hoverColor = MaterialDesignColors.Primary
    .WithAlpha((byte)(StateLayerOpacity.Hover * 255));
```

### 4. Event Propagation
```csharp
public override void OnMouseDown(SKPoint point, InteractionContext context)
{
    // Handle component-specific logic first
    if (IsPointInComponent(point))
    {
        // Component was clicked
        OnClicked(EventArgs.Empty);
    }

    // Always call base for framework behavior
    base.OnMouseDown(point, context);
}
```

## Integration Points

### 1. Windows Forms Integration
```csharp
// Use Beep_Skia_Control in WinForms applications
var skiaControl = new Beep_Skia_Control();
skiaControl.DrawingManager = myDrawingManager;
```

### 2. External Dependencies
- **SkiaSharp**: Core 2D graphics rendering
- **SkiaSharp.Extended**: Extended functionality (SVG support)
- **SkiaSharp.Svg**: SVG rendering capabilities
- **TheTechIdea.Beep.DataManagementModels**: Data management framework
- **TheTechIdea.Beep.Vis.Modules**: Visualization modules

### 3. NuGet Package Configuration
```xml
<!-- Automatic package generation -->
<GeneratePackageOnBuild>True</GeneratePackageOnBuild>

<!-- Custom package copy targets -->
<Target Name="CopyPackage" AfterTargets="Pack">
  <Copy SourceFiles="$(OutputPath)$(PackageId).$(PackageVersion).nupkg"
        DestinationFolder="..\..\..\LocalNugetFiles" />
</Target>
```

## Debugging & Troubleshooting

### 1. Common Issues
- **Missing InvalidateVisual()**: Components won't redraw after property changes
- **Incorrect Bounds**: Use `context.Bounds` instead of `Bounds` for clipping
- **Memory Leaks**: Always dispose SKPaint, SKPath, and other SK objects
- **Event Not Firing**: Check if component is properly added to DrawingManager

### 2. Performance Considerations
- Cache SKPaint objects in component properties
- Use `context.Bounds.IntersectsWith()` for early exit
- Minimize object creation in DrawContent methods
- Implement proper disposal patterns

### 3. Build Issues
```bash
# Clean and rebuild
dotnet clean
dotnet build --no-incremental

# Check for specific errors
dotnet build /verbosity:detailed
```

## File Organization Standards

### 1. Component Structure
```
Components/
├── Base classes (MaterialControl.cs, SkiaComponent.cs)
├── UI Components (Button.cs, Label.cs, etc.)
├── Material Design (Menu.cs, ContextMenu.cs, etc.)
└── Utilities (MaterialDesignColors.cs)
```

### 2. Model Organization
```
Model/
├── Interfaces (I*.cs)
├── Enums and constants
├── Event argument classes
└── Data transfer objects
```

### 3. Implementation Patterns
```
Implementation/
├── Base classes with common functionality
├── Concrete implementations
├── Helper classes
└── Extension methods
Never create summary or documentation comments for these files until i tell you.
```

## Quality Assurance

### 1. Code Review Checklist
- [ ] Inherits from correct base class (SkiaComponent/MaterialControl)
- [ ] Implements required abstract methods (DrawContent)
- [ ] Handles mouse events appropriately
- [ ] Uses MaterialDesignColors for theming
- [ ] Implements IDisposable if using unmanaged resources
- [ ] Includes XML documentation comments
- [ ] Follows PascalCase naming conventions

### 2. Testing Considerations
- Test component rendering with different states
- Verify mouse interaction boundaries
- Check memory disposal in long-running applications
- Validate Material Design color usage

## Key Files for Understanding
- `SkiaComponent.cs`: Core component architecture
- `MaterialControl.cs`: Material Design 3.0 implementation
- `DrawingManager.cs`: Central coordination system
- `Enums.cs`: Component types and shapes
- `MaterialDesignColors.cs`: Color token definitions

Tip: When building editors or serializers, prefer using `GetProperties()` and `SetProperties()` for generic flows. For fine-grained control in nodes, synchronize your public setters with `NodeProperties` using the provided Upsert helper in AutomationNode.

This framework provides a solid foundation for building cross-platform workflow and automation applications with modern Material Design aesthetics.

Adoption requirement (2025-10): All nodes/components should expose their configuration via SkiaComponent.GetProperties and apply changes via SetProperties (or SetPropperties alias). Family bases and concrete nodes should seed NodeProperties for node-specific fields so the editor can render them (checkbox/dropdown/text) and persist changes. Container controls should manage child visuals via Children (ordered) and ChildNodes (named) to support drawing and refresh of nested items (e.g., ERDEntity with ERD rows).

## Port Layout Design Recommendations (2025-09 update)

To standardize port layout across all families (Flowchart, ETL, PM, ERD, DFD, Business, Network, MindMap, StateMachine, Quantitative), follow this pattern:

- Implement a protected virtual method in each family base: `LayoutPorts()` that computes port geometry for the current bounds. Each concrete node overrides it as needed (e.g., vertical segments, ellipse perimeter, diamonds).
- Use lazy port layout. The framework now exposes two helpers in the base classes:
    - In `SkiaComponent`: `MarkPortsDirty()` and `ArePortsDirty` are available; bounds changes automatically mark ports dirty in `OnBoundsChanged`. A `ClearPortsDirty()` helper is also available to safely clear the flag when not using `MaterialControl`.
    - In `MaterialControl`: call `EnsurePortLayout(() => LayoutPorts())` early in your `DrawContent` to re-layout ports only when dirty. Family base classes have been updated to call this and delegate to a template method (`DrawXxxContent`) for actual visuals. `EnsurePortLayout` now uses `ClearPortsDirty()` internally.

Migration rules when adding new nodes or refactoring existing ones:
- Replace any `UpdateConnectionPointPositions` overrides with `LayoutPorts()`; there should be no remaining usages of the legacy method name.
- Ensure family base overrides `OnBoundsChanged` and calls `MarkPortsDirty()` (do not eagerly call `LayoutPorts()` here).
- Ensure family `DrawContent` calls `base.EnsurePortLayout()` early by using the base wrapper and overriding the family’s template method (e.g., `DrawFlowchartContent`, `DrawDFDContent`, `DrawERDContent`, `DrawPMContent`, `DrawMindMapContent`, `DrawStateMachineContent`, `DrawBusinessContent`, `DrawETLContent`) instead of `DrawContent`.
- When port counts change (e.g., via `EnsurePortCounts` or editor properties), call `MarkPortsDirty()` and optionally `OnBoundsChanged(Bounds)` to notify listeners. Avoid directly calling `LayoutPorts()` outside initialization.

Edge cases and tips:
- Keep `LayoutPorts()` fast and allocation-free; it will be called on-demand during rendering.
- For non-rectangular shapes (ellipse, cylinder, document fold), compute exact edge intersections for port anchors and set both `Center` and `Position` plus `Bounds`/`Rect`.
- When text or headers affect usable edge segments, pass top/bottom insets to helpers to avoid overlapping rounded corners and headers.

Testing checklist per node:
- Resize/move: ports remain glued correctly; no redundant layout work between frames.
- Dynamic port counts: adjusting counts marks dirty and ports realign on next draw.
- No lingering `UpdateConnectionPointPositions` methods.

## Status and migration log (2025-09)

What’s done now (verified by builds on Sep 30, 2025):

- Core infrastructure
    - SkiaComponent exposes MarkPortsDirty() and ArePortsDirty; OnBoundsChanged marks ports dirty.
    - MaterialControl provides EnsurePortLayout(Action layoutPorts) to re-layout ports on demand.
    - SkiaComponent now exposes GetProperties()/SetPropperties(SetProperties alias) for generic property I/O, with safe type conversion (enum/TimeSpan/SKColor) and NodeProperties flattening.
    - SkiaComponent now maintains ChildNodes: Dictionary<string, SkiaComponent> alongside Children for container-style named access.
- Family bases call EnsurePortLayout early and delegate visuals to templates:
    - Flowchart: DrawFlowchartContent
    - ERD: DrawERDContent
    - DFD: DrawDFDContent
    - PM: DrawPMContent
    - MindMap: DrawMindMapContent
    - StateMachine: DrawStateMachineContent
    - Business: DrawBusinessContent
    - ETL: DrawETLContent
    - UML: Base DrawContent performs lazy ensure; derived nodes implement DrawUMLContent
    - Network: Now inherits from MaterialControl; DrawContent calls EnsurePortLayout and delegates to DrawNetworkContent
    - Quantitative: Now inherits from MaterialControl; DrawContent calls EnsurePortLayout and delegates to DrawQuantContent
    - Business: Base wraps DrawBusinessContent
    - Cloud: Base wraps DrawCloudContent
    - ECAD: Base wraps DrawECADContent
    - ML: Base wraps DrawMLContent
    - Security: Base wraps DrawSecurityContent
- DFD family sweep completed: all nodes now override DrawDFDContent and no longer call LayoutPorts() directly in draw.
- Flowchart and ERD nodes previously refactored to template methods; direct LayoutPorts() calls in draw removed.
- Business and ETL bases already conform and wrap drawing with EnsurePortLayout.
- Network family previously used an internal lazy ensure variant; it now conforms to MaterialControl + EnsurePortLayout.

Palette and discovery

- The WinForms host uses SkiaComponentRegistry to discover components and categorize them into the in-canvas Palette.
- Categories now include UML, Network, ETL, Business, DFD, ERD, Flowchart, PM, StateMachine, Cloud, ECAD, ML, Security, and Quantitative based on namespace.

- Quantitative family migrated to lazy port layout:
    - QuantControl stops calling LayoutPorts eagerly; EnsurePortCounts marks ports dirty instead of laying out.
    - DrawContent in QuantControl performs an internal lazy ensure (ArePortsDirty check, then LayoutPorts) mirroring the Network pattern.

Remaining opportunities (nice-to-have):

- Minor compile warnings (unused fields/events) can be tidied in a follow-up grooming pass.

Property editor UX and data model

- ParameterInfo supports Choices for constrained strings and enums; ComponentPropertyEditor renders checkboxes (bool) and dropdowns (enums or strings with choices).
- AutomationNode.ApplyNodeProperties maps NodeProperties values back to public properties and Configuration with type conversion (including SKColor and TimeSpan).

## Event model updates: ComponentDropped

Drag-end now publishes a unified drop event with canvas and screen coordinates.

- DTO: ComponentDropEventArgs { Component, CanvasPosition, ScreenPosition, Bounds }.
- Emission: Interaction end-of-drag constructs the args and calls DrawingManager.RaiseComponentDropped(args).
- Subscribe:

```csharp
// Somewhere after you create/own the DrawingManager instance
drawingManager.ComponentDropped += (s, e) =>
{
        var comp = e.Component;          // the dropped component
        var canvasPt = e.CanvasPosition; // SKPoint in canvas coordinates
        var screenPt = e.ScreenPosition; // SKPoint in screen coordinates
        var rect = e.Bounds;             // SKRect of the component after drop
        // handle drop (snap-to-grid, auto-connect, etc.)
};
```

Notes:

- A legacy event args type with a similar name was renamed (internal, Obsolete) to avoid collisions.
- Use the new DTO and event; avoid invoking events from helpers directly—always go through DrawingManager’s raiser.
- For custom family bases that don’t inherit from `MaterialControl`, prefer calling `MarkPortsDirty()` on changes and `ClearPortsDirty()` after a successful lazy `LayoutPorts()` in `DrawContent`.

## Build warning hygiene

- Windows Forms projects use older-targeted packages (e.g., OpenTK, SkiaSharp.Views.WindowsForms) that trigger NU1701 warnings. These are suppressed to keep logs clean:
    - Beep.Skia.Winform.Controls.csproj: added `<NoWarn>$(NoWarn);NU1701</NoWarn>`
    - Beep.Skia.Sample.WinForms.csproj: added the same.
- If you upgrade or replace those packages later, you can remove the suppression.

## Adding a new node (mini checklist)

Follow this quick checklist to keep ports efficient and consistent:

1) In the family base, ensure DrawContent calls EnsurePortLayout(() => LayoutPorts()); then delegate to DrawXxxContent.
2) In the node, override LayoutPorts() and DrawXxxContent(canvas, context). Do not call LayoutPorts() inside DrawXxxContent.
3) For properties that change geometry (port counts, shape, header sizes), set values, call MarkPortsDirty(), and optionally OnBoundsChanged(Bounds) to notify listeners. Do not eagerly layout here.
4) Keep LayoutPorts() allocation-free and fast. For curved shapes (ellipse/cylinder), compute exact edge intersections and set Center/Position and Bounds/Rect on each port.
5) Validate with the testing checklist above (resize/move, dynamic port counts).

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

This framework provides a solid foundation for building cross-platform workflow and automation applications with modern Material Design aesthetics.

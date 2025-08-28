# Beep.Skia

This is my Skia Framework for creating graphs, charts, and automation workflows. It uses SkiaSharp and is cross-platform, designed to be expandable by other projects to add components similar to Make.com and DevExpress charts.

## Features

- **Cross-platform**: Works on Windows, macOS, Linux, and mobile platforms
- **SkiaSharp-based**: High-performance 2D graphics rendering
- **Workflow Components**: Create interactive workflow diagrams with connectable components
- **Connection Lines**: Draw connections between components with customizable arrows and labels
- **Animation Support**: Built-in animation capabilities for dynamic visualizations
- **Extensible Architecture**: Easy to add new component types and shapes

## Recent Improvements

### ✅ Phase 1: Critical Fixes
- Fixed project configuration issues (removed duplicate entries, updated target frameworks)
- Corrected typos and naming conventions
- Fixed bugs in core classes
- Updated package dependencies

### ✅ Phase 2: Architecture Improvements
- Added comprehensive error handling and validation
- Implemented proper null checks
- Improved event handling consistency
- Enhanced input parameter validation

### ✅ Phase 3: Performance Optimizations
- Optimized drawing operations with cached SKPaint objects
- Implemented proper memory management with IDisposable
- Reduced object creation in drawing loops
- Fixed potential memory leaks

## Project Structure

```
Beep.Skia/
├── Beep.Skia.Model/          # Core interfaces and models
│   ├── IConnectionLine.cs     # Connection line interface
│   ├── IConnectionPoint.cs   # Connection point interface
│   ├── ISkiaWorkFlowComponents.cs # Workflow component interface
│   ├── Enums.cs              # Component shapes and types
│   ├── Args.cs               # Event argument classes
│   └── TableDrawer.cs        # Table drawing utility
├── Beep.Skia/                # Main implementation
│   ├── SkiaWorkFlowComponents.cs # Workflow component implementation
│   ├── ConnectionLine.cs     # Connection line implementation
│   ├── ConnectionPoint.cs   # Connection point implementation
│   ├── DrawingManager.cs    # Main drawing coordinator
│   ├── SkiaUtil.cs          # Utility functions
│   └── Extensions/           # Framework integration (currently disabled)
└── Beep.Skia.Winform/        # Windows Forms control
    └── Beep_Skia_Control.cs  # WinForms integration
```

## Building the Project

The project targets .NET 6.0, 7.0, and 8.0. To build:

```bash
cd Beep.Skia/Beep.Skia
dotnet build
```

## Usage Example

```csharp
// Create a drawing manager
var drawingManager = new DrawingManager();

// Create workflow components
var component1 = new SkiaWorkFlowComponents(100, 100, "Start", ComponentShape.Circle);
var component2 = new SkiaWorkFlowComponents(300, 100, "Process", ComponentShape.Square);

// Add components to the manager
drawingManager.AddComponent(component1);
drawingManager.AddComponent(component2);

// Connect the components
drawingManager.ConnectComponents(component1, component2);

// Draw on a canvas
drawingManager.Draw(skCanvas);
```

## Dependencies

- SkiaSharp 2.88.9
- SkiaSharp.Extended 2.0.0
- SkiaSharp.Svg 1.60.0
- TheTechIdea.Beep.DataManagementModels 2.0.34
- TheTechIdea.Beep.Vis.Modules 1.0.128

## Status

✅ **Core functionality optimized and working**
✅ **Builds successfully for all target frameworks**
✅ **Performance optimizations implemented**
✅ **Memory management improved**

## Next Steps

- Add comprehensive XML documentation
- Implement unit tests
- Create usage examples and tutorials
- Add more component shapes and customization options

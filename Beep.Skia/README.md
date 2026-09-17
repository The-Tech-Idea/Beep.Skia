# Beep.Skia

SkiaSharp diagramming and automation framework: component canvas, 16 diagram families (Flowchart, ERD, UML, DFD, ETL, BPMN, PM, State Machine, Mind Map, Network, charts, security, ML, ECAD, well logs, cloud), an executable workflow runtime, collaboration, and an extension SDK.

| | |
|---|---|
| Target frameworks | `net9.0;net8.0;net10.0` |
| NuGet package | `Beep.Skia` |
| Documentation | [skia-component.html](../Help/core-concepts/skia-component.html) |
| Start with | DrawingManager, SkiaComponent, ConnectionLine, HistoryManager, ThemeManager, SkiaComponentRegistry |

Also contains the automation runtime, credential vault, collaboration, extension SDK and serialization.

```bash
dotnet add package Beep.Skia
```

## Building

```bash
dotnet build Beep.Skia/Beep.Skia.csproj
```

The solution requires the private `TheTechIdea.Beep.*` NuGet feed; see [CONTRIBUTING.md](../CONTRIBUTING.md).

## Documentation

- [Documentation site](../Help/index.html) (open `Help/index.html`)
- [Guide for this module](../Help/core-concepts/skia-component.html)
- [API reference](../Help/reference/beep-skia.html)
- [Feature roadmap](../FEATURE_ROADMAP.md)


# Beep.Skia.FlowChart

Flowchart diagram components with structured code generation and step-through simulation

| | |
|---|---|
| Target frameworks | `net8.0;net9.0;net10.0` |
| NuGet package | `Beep.Skia.FlowChart` |
| Documentation | [flowchart.html](../Help/diagram-families/flowchart.html) |
| Start with | ProcessNode, DecisionNode, StartEndNode, FlowchartControl |

Structured code generation and a flowchart simulator.

## Portable editor integration

ProcessNode, DecisionNode and StartEndNode provide constructors taking a component ID and ordered input/output port IDs. They validate non-empty, distinct identities before restoring a detached definition. Restore before scene registration or connections. Default constructors keep generated identities.

The three nodes honor CustomFillColor, CustomStrokeColor and CustomTextColor. NodeStrokeWidth and LabelFontSize are bounded properties stored in NodeProperties. Setting RequiredLabelFontFamily selects an explicitly registered bundled font through TypefaceCache.GetRequired; labels draw glyph outlines on raster and SVG canvases. Register those bytes before rendering. A missing required font or unsupported glyph raises an error. Leaving that host configuration unset retains system-font label rendering. Long labels clip to node bounds. This does not promise portable rendering for every other flowchart node.

Editor GroupId now belongs to SkiaComponent, so different reviewed families can share membership. Port relayout preserves connected-port availability. DrawingManager's renderer passes world-space clip bounds through pan/zoom and export transforms.

```bash
dotnet add package Beep.Skia.FlowChart
```

## Building

```bash
dotnet build Beep.Skia.FlowChart/Beep.Skia.FlowChart.csproj
```

The solution requires the private `TheTechIdea.Beep.*` NuGet feed; see [CONTRIBUTING.md](../CONTRIBUTING.md).

## Documentation

- [Documentation site](../Help/index.html) (open `Help/index.html`)
- [Guide for this module](../Help/diagram-families/flowchart.html)
- [API reference](../Help/reference/beep-skia-flowchart.html)
- [Feature roadmap](../FEATURE_ROADMAP.md)


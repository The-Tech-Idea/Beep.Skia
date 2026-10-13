# Beep.Skia.Blazor.Controls

Blazor WebAssembly host controls for Beep.Skia components

| | |
|---|---|
| Target frameworks | `net8.0`, `net10.0` |
| NuGet package | `Beep.Skia.Blazor.Controls` |
| Documentation | [platforms.html](../Help/guides/platforms.html) |
| Start with | SkiaHostComponent |

Targets net8.0 and net10.0. The host renders SKCanvasView and uses CSS-pixel pointer capture for browser input.

```razor
@using Beep.Skia.Blazor.Controls
<SkiaHostComponent Manager="manager" Height="100%" />
@code {
    private Beep.Skia.DrawingManager manager = new();
}
```

For editor tools, set `HandleDefaultInteraction="false"` and handle `OnPointer` (`CanvasPointerEvent`: down, move, up, cancel, wheel, doubleclick). Coalesced samples retain their order in bounded batches. `PaintOverlay` renders previews after the scene. `IgnorePixelScaling=true` keeps input and rendering in CSS coordinates across DPR changes.
`CanvasLabel` and `CanvasDescription` provide a region name and the ID of caller-owned keyboard instructions. `CanvasPointerEvent` also reports pointer ID/type and bounded pressure; consumers choose how pressure affects their tools.

With custom interaction, opt into `EnableViewportGestures="true"` to receive `resize` (CSS Width/Height), `pinchstart`, `pinchmove` (centroid Position and Scale relative to the initial two-touch distance), and `pinchend`. A second touch delivers `cancel` before promotion; callers must roll back the preview before starting the viewport gesture. Lift both fingers before drawing again. Active pen gestures ignore incidental touch. Blur, capture loss and size changes cancel input. The default remains disabled; default manager interaction ignores the additional viewport messages. SimpleDraw's focused tests verify the event policy and transforms; actual device/engine review is tracked separately.
The supplied manager remains owned by the caller. The host releases subscriptions, captured input listeners and JS references on asynchronous disposal. `SketchShape` supplies rectangle/ellipse/diamond/line/arrow/freehand/text on the existing component/property/history contracts, with group membership and deterministic seeded outlines. Text requires registered Kalam bytes through the existing TypefaceCache; missing fonts/glyphs fail explicitly. SimpleDraw embeds and registers the licensed font. Shared text draws glyph paths so SVG needs no external font.
Build WebAssembly consumers with the SDK's `wasm-tools` workload and matching SkiaSharp native assets. Browser smoke and release/offline checks are separate from this library's build.

```bash
dotnet add package Beep.Skia.Blazor.Controls
```

## Building

```bash
dotnet build Beep.Skia.Blazor.Controls/Beep.Skia.Blazor.Controls.csproj
```

The solution requires the private `TheTechIdea.Beep.*` NuGet feed; see [CONTRIBUTING.md](../CONTRIBUTING.md).

## Documentation

- [Documentation site](../Help/index.html) (open `Help/index.html`)
- [Guide for this module](../Help/guides/platforms.html)
- [API reference](../Help/reference/beep-skia-blazor-controls.html)
- [Feature roadmap](../FEATURE_ROADMAP.md)


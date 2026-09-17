# Beep.Skia

A SkiaSharp diagramming and automation framework for .NET. It provides a component canvas, 16
diagram families, an executable workflow runtime, a collaboration model, an extension SDK with a
package marketplace, and cross-platform UI hosts (WinForms, WPF, Blazor, MAUI, Avalonia).

## Highlights

- **Diagram families (16)** - Flowchart, ERD, UML (class/component/sequence), DFD, ETL, BPMN,
  Project Management (CPM/Gantt), State Machine, Mind Map, Network, Quantitative charts, Security
  (STRIDE/DREAD/MITRE), Machine Learning pipelines, ECAD, Well Logs, Cloud architecture.
- **Automation runtime** - `WorkflowEngine` (retry, variables, pause/resume/cancel), triggers
  (manual/schedule/event/file-watch), a credential vault, and a diagram-to-workflow bridge.
- **Server execution SKU** - `WorkflowExecutionService` (bounded-concurrency job queue),
  `WorkflowExecutionApi` (JSON facade) and `WorkflowRequestRouter` (HTTP routes); a ready-to-run
  ASP.NET host lives in `Beep.Skia.Sample.Server`.
- **Extension SDK + marketplace** - `ISkiaExtension` plugins loaded from an `Extensions` folder,
  plus `.beepkg` packaging with install/update/uninstall, dependency and host-version gates, and
  zip-slip / decompression-bomb protection.
- **Collaboration** - sharing with Viewer/Commenter/Editor/Admin roles, component-anchored
  comments rendered as review pins, presence, and an audit trail (thread-safe, snapshot reads).
- **Assisted generation** - a provider-agnostic `IDiagramAssistant` with an offline rule-based
  baseline (flowchart DSL and indented mind-map outlines); cloud/LLM providers plug in via the
  registry.
- **Editor UX** - palette search, minimap, property grid with write-back, 12 templates,
  keyboard selection cycling and high-contrast support.

## Quick start

```bash
# Build and test the whole solution
dotnet build Beep.Skia.Solution.sln
dotnet test Beep.Skia.Tests/Beep.Skia.Tests.csproj

# Run the flagship WinForms sample
dotnet run --project Beep.Skia.Sample.WinForms

# Run the workflow server (HTTP execution SKU)
dotnet run --project Beep.Skia.Sample.Server -- --demo --url http://localhost:5199
```

```csharp
// Draw a diagram programmatically
var manager = new DrawingManager();
var start = new Beep.Skia.Components.ManualTriggerNode { X = 40, Y = 40, Name = "Start" };
var step  = new Beep.Skia.Components.DataTransformNode  { X = 40, Y = 160, Name = "Transform" };
manager.AddComponent(start);
manager.AddComponent(step);
manager.ConnectComponents(start, step);

using var surface = SKSurface.Create(new SKImageInfo(800, 600));
manager.Draw(surface.Canvas);
```

```csharp
// Execute the same diagram as a workflow
using var service = new WorkflowExecutionService(maxConcurrency: 2);
service.Publish(manager.ToWorkflowDefinition("Nightly Sync"));
var job = service.Submit(service.Workflows[0].Id, submittedBy: "me");
service.WaitForCompletion(job.Id, TimeSpan.FromSeconds(30));
Console.WriteLine(job.State); // Completed
```

## Repository layout

```
Beep.Skia/                    Core canvas, components, serialization, automation, extensions
Beep.Skia.Model/              Shared interfaces, enums, and data models
Beep.Skia.Loader/             BeepDM plugin-loader integration
Beep.Skia.<Family>/           16 diagram families (Flowchart, ERD, UML, DFD, ETL, BPMN, PM, ...)
Beep.Skia.Winform.Controls/   WinForms host control (flagship v1.0 host)
Beep.Skia.{Wpf,Blazor,Maui,Avalonia}.Controls/   Additional platform hosts
Beep.Skia.Sample.WinForms/    Flagship sample application
Beep.Skia.Sample.Server/      ASP.NET workflow-execution host
Beep.Skia.Tests/              501 tests covering every library
docs/                         Feature notes, gap analyses and guides (docs/archive for history)
FEATURE_ROADMAP.md            Live status of the feature programme
```

## Building and contributing

- **Target frameworks:** the core, model and family libraries multi-target `net8.0` / `net9.0` /
  `net10.0`. The **Windows hosts** (WinForms, WPF) and the sample app target
  `net10.0-windows10.0.19041.0` — SkiaSharp's 4.x view packages ship no `net8.0` assets, so
  anything that hosts a canvas requires .NET 10 on Windows 10 2004 or later. The Blazor and
  Avalonia hosts target `net8.0`; MAUI targets `net10.0-*`.
- Each shipped target is **runtime-verified** by a standalone consumer (restore, render, save/load
  round-trip, workflow execution) on `net8.0`, `net9.0` and `net10.0`.
- `Directory.Build.props` derives package versions from git tags via **MinVer**
  (`v1.2.3` -> `1.2.3`; untagged builds produce `1.0.0-alpha.0.<commit height>`).
- CI (`.github/workflows/ci.yml`) builds `Beep.Skia.CI.slnf`, runs the full test suite and packs
  NuGet packages on tags. It treats **warnings as errors** - the solution is warning-free with no
  warning suppressions.
- The MAUI host is excluded from CI because GitHub-hosted Windows runners cannot build the
  `net10.0-ios`/`maccatalyst` targets; build it locally with `dotnet build Beep.Skia.Maui.Controls`.
- **Private dependency note:** the core depends on `TheTechIdea.Beep.*` packages that are not
  published to nuget.org at the required versions. A feed providing them must be configured
  (locally via a `LocalNugetFiles` folder source; in CI via a feed secret) before restore succeeds.

## Dependencies

SkiaSharp 4.150.1, Avalonia 11.3.5, Microsoft.Maui.Controls 10.0.20, MinVer 7, and the
`TheTechIdea.Beep.*` ecosystem packages (see the note above).

## License

MIT - see [LICENSE.txt](LICENSE.txt).

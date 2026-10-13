# Changelog

All notable changes to Beep.Skia are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html). Versions are derived
from git tags with MinVer (`v` prefix); untagged builds produce `1.0.0-alpha.0.<commit height>`.

## [Unreleased]

### Added

- **Diagramming core** — `DrawingManager` with pan/zoom, grid and snapping, selection, drag and
  resize, connection routing (`Straight`, `Orthogonal`, `Curved`), undo/redo through
  `HistoryManager` and `DrawingAction`s, copy/paste, arrange and align commands.
- **16 diagram families** — Flowchart, Business Process (BPMN), Project Management, Mind Map,
  State Machine, ERD, DFD, ETL, UML, Network, ECAD, Cloud, Security, Machine Learning,
  Quantitative and Well Logs: 293 component types in total.
- **Automation runtime** — execute diagrams as workflows with topological ordering, variables,
  retries and timeouts, pause/resume/cancel, and per-node results.
- **Triggers** — manual, scheduled, event and file-watch triggers with lifecycle, validation,
  activation telemetry and error events.
- **Credential vault and connection manager** — passphrase-encrypted export (AES + PBKDF2-SHA256),
  masked listing, and named connections that reference credentials instead of secrets.
- **Server execution SKU** — `WorkflowExecutionService` job queue (queued/running/paused/completed/
  failed/cancelled), `WorkflowExecutionApi`, `WorkflowRequestRouter` and the ASP.NET sample host.
- **Extension SDK** — `ISkiaExtension`, `SkiaExtensionHost` discovery from assemblies or a folder,
  component and command registration, and `.beepkg` packages through `ExtensionPackageManager`
  with install/update/uninstall, dependency and archive safety gates.
- **Collaboration** — roles and sharing, component-anchored review comments with canvas pins,
  presence, and an audit trail, all persisted and thread-safe.
- **Assisted generation** — `IDiagramAssistant` with an offline rule-based implementation, a
  flowchart DSL parser and mind-map outline parsing.
- **Editor UX** — palette search, minimap, property-grid write-back, twelve diagram templates and
  accessibility improvements.
- **Family analysis** — code generation and simulation (Flowchart), schema diff and migration
  scripts (ERD), DDL import/export, CPM and Gantt (PM), XMI and BPMN export, DREAD and MITRE
  (Security), graph algorithms (Network), expression engine, profiling and metrics (ETL).
- **Platform hosts** — WinForms (with a wrapper control for every Material component), WPF,
  Blazor, MAUI and Avalonia.
- **Samples** — a full WinForms editor and an ASP.NET workflow server.
- **Packaging** — 24 NuGet packages with metadata, XML documentation for IntelliSense and a
  package README; MinVer versioning and a CI pipeline that packs on tags.

### Security

- Hardening pass across the framework: 6 crash paths (including two native Skia paths),
  3 silent save/load data losses, 3 data races, 2 resource leaks, a decompression bomb in package
  import, an authorization bypass in collaboration, and error-type leaks were fixed.
- Recursive parsers (mind-map, JSON/XML flattening, expressions, BPMN/XMI) are depth-limited and
  covered by dedicated hardening tests.

### Documentation

- `Help/` documentation site with 105 pages: getting started, core concepts, all 16 families,
  UI components, automation, ecosystem, editor, architecture internals, guides, a per-project API
  reference for all ~890 public types, a troubleshooting guide, and site-wide search.
- Repository docs: README, this changelog, contributing guide, security policy and the
  `FEATURE_ROADMAP.md` tracker.
- XML documentation is generated for every library and shipped in the packages.

### Changed

- `HierarchicalLayout` is a full layered layout: cycles are broken, nodes take longest-path layers, long edges pass
  through placeholders so barycentre sweeps can choose the layer orders with the fewest crossings, positions along a
  layer are fitted to the neighbours without overlapping (sizes respected), and separate graphs sit side by side. It
  previously looped forever on a cycle, laid `TopToBottom` out left to right and did not reduce crossings. The
  `TopToBottom` flag is replaced by `Direction` (`LayoutDirection`: top-to-bottom, bottom-to-top, left-to-right,
  right-to-left); `LayerSpacing` and `NodeSpacing` are now gaps between boxes; `Layout(sizes, edges)` returns
  positions without needing components.
- `ConnectionRoute` owns a connector's path for drawing, arrowheads, hit testing, the middle label and
  `GetPathAndLabelBounds`. Orthogonal connectors leave and enter each port away from its component (the side the port
  faces); a link to a node behind its source goes round through the gap between them instead of across both. Forward
  links keep the three-segment step. Clicking an orthogonal or curved connector now hits its drawn path rather than
  the straight line between its ends.
- `DrawingContext.InvertedDisplay`, `RenderingHelper.InvertedDisplay` and the Blazor host's `InvertedDisplay`
  parameter: a host that shows the canvas with inverted lightness (a dark theme) has pictures drawn pre-compensated
  (`DisplayFilters`) so they keep their colours; exports never use it, and the frame cache repaints when it changes.
- `HistoryManager.ActionRecorded`: raised when a new entry is recorded (not on undo, redo or clear), so a host
  with several managers (one per page) can order their histories into one.
- `SketchShapeKind.Frame`: a named rectangular region drawn as a crisp, unfilled border with its title above it;
  picked by its border or title, never by its inside.
- `SkiaComponent.ClipBounds`: a runtime, unserialized world-space clip a host sets on a component (for example a
  frame's members). The scene renderer clips the component to it, sketch ink bounds stop at it, and the frame cache
  repaints when it changes.
- `IConnectionPoint.IsAvailable` is read-only and derived from `Connection`: a port is available exactly when it has
  no connection. Port layouts no longer reset it, so redrawing ERD, network, mind map, state machine and other family
  components keeps their connected ports connected; `ConnectionPoint.Disconnect` frees both ends.
- Windows hosts target `net10.0-windows10.0.19041.0` because SkiaSharp 4.x view packages ship no
  `net8.0` assets; the previous `NU1701` suppressions were removed.
- The core library multi-targets `net8.0;net9.0;net10.0`.
- Package metadata corrected: descriptions, repository URLs, copyright and the dependency surface.

[Unreleased]: https://github.com/The-Tech-Idea/Beep.Skia/commits/master

## 2026-10-05 sketch rendering resource reuse
- SimpleDraw measured 266.1 ms p95 editing on a 1,000-object rough-rectangle board before optimization. SketchShape now retains its native geometry/paints and recorded glyph-outline drawing between frames, invalidating the same renderer's cache on all pixel-affecting keys. Native resources follow the existing component disposal path.
- Current focused suite: 173 C# tests, including seven fresh-scene pixel comparisons across move/style/size/path/kind/text changes, exact original glyph-command pixels at integer/fractional positions, undo/redo and native 2,000/2,001 boundary preservation. Empty child lists avoid a sorted iterator allocation while nonempty priority order stays intact.
- Actual SimpleDraw desktop rectangle p95 improved to 39.4/35.7 ms after the native cache plus removal of quadratic HTML list work. Mixed-tool p95 remains 50.4 ms against a 50 ms target. Native boundary and list/file/PNG/SVG desktop checks passed. A local-coordinate glyph experiment regressed latency and was reverted; current recordings preserve the original absolute commands. Broad sibling/device acceptance remains open.

## 2026-10-05 portable build configuration
- Four selected projects accept opt-in `BeepSkiaPortableTargetFramework=net10.0`; ordinary target lists remain their defaults. SimpleDraw maps those same projects to Release and uses one existing project-reference path.
- Core/model local copy targets honor `BeepSkiaLocalBuildCopies=false` for a portable build; default consumers retain their behavior. Blazor excludes the net10 NuGet lock from published content.
- Fresh pinned SimpleDraw Release gate passed 162 C# and 47 JavaScript tests, actual native-Skia WASM link, zero build/publish warnings/errors and static artifact integrity. Root/subpath/CSP/offline/update desktop evidence is in the sibling app's P6 verification. No new retained-net8 or full desktop/family regression is claimed for this configuration increment.

## 2026-10-05 connected diagram consistency
- Exact configured connection creation/removal uses the existing manager and history. Connect/disconnect redo restores the same line/ports/styles/paint index; delete/clear/rewire settle endpoint availability and component relationships, preserving other edges between a component pair.
- Flowchart RefreshPortLayout supplies current geometry before first paint. ConnectionLine optionally requires bundled glyph label outlines, centers labels once, exposes ordinary path/arrow/label bounds and orients arrows along routed endpoint segments.
- Focused sibling SimpleDraw suite: 162 cases including exact branch/history/deletion/rewire/batch, strict profiles, connected native/library/duplicate/export and actual browser fixtures; retained net8 FlowChart build passes. Broader desktop/family regression remains open.

# Changelog

## SimpleDraw editing integration — 2026-10-05
- Added net10 to the existing FlowChart targets. Process/Decision/StartEnd have constructor-based validated component/port identity restoration, honored custom colors, bounded border/label sizes and an optional required bundled label font rendered as glyph outlines.
- GroupId is shared on SkiaComponent. Sketch and portable flowchart labels reuse TypefaceCache.DrawGlyphRun. World-space clip bounds now respect viewport/export transforms; connected ports stay unavailable during relayout.
- The sibling application provides 116 focused C# tests, including eight existing flowchart simulator/code-generator regressions and strict reviewed-factory tests. Original net8 FlowChart builds cleanly. Full family/desktop regression and browser family-palette integration remain open.
- Extended the shared SketchShape with ordered local line/arrow/freehand geometry, arrow tangent rendering, distance hit testing and eraser sweep intersection.
- Added SketchEditAction for undoable multi-shape styles, geometry, text and membership; delete undo preserves original paint order and deduplicates shared deleted lines.
- Extended SketchShape with bounded multiline text using explicitly registered bundled font metrics and glyph-path rendering, persistent groups, roughness and deterministic seeds. Diagram schema 3 persists component IDs. History batches and component order actions support atomic duplication and paint-order undo without replacing components.
- Blazor pointer host delivers ordered coalesced samples in bounded batches. Original net8 host target remains build verified.
- Opt-in `EnableViewportGestures` adds ordered two-touch cancel/pinch messages, pointer ID/type/pressure metadata and observed CSS viewport sizing. Active pen input rejects incidental palm touch; remaining-finger movement cannot start a stroke after pinch. `CanvasLabel`/`CanvasDescription` let the caller describe keyboard interaction. Default consumers retain the original secondary-pointer policy.
- Focused SimpleDraw regression evidence is maintained in the sibling application's docs/VERIFICATION.md; broader family/desktop regression is still open.

All notable changes to Beep.Skia are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html). Versions are derived
from git tags with MinVer (`v` prefix); untagged builds produce `1.0.0-alpha.0.<commit height>`.

## [Unreleased]

### Added

- **Simple sketch shapes** — `SketchShape` uses the existing component/NodeProperties contracts for rectangle, ellipse and diamond geometry, style persistence and geometry-aware hit tests.
- **Functional Blazor canvas host** — `SkiaHostComponent` now renders `SKCanvasView`, accepts a caller-owned manager, captures ordered CSS-pixel pointer events, exposes editor input/preview callbacks and cleans up browser subscriptions. Blazor controls and the model also support .NET 10.

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

- Windows hosts target `net10.0-windows10.0.19041.0` because SkiaSharp 4.x view packages ship no
  `net8.0` assets; the previous `NU1701` suppressions were removed.
- The core library multi-targets `net8.0;net9.0;net10.0`.
- Package metadata corrected: descriptions, repository URLs, copyright and the dependency surface.

[Unreleased]: https://github.com/The-Tech-Idea/Beep.Skia/commits/master

# Beep.Skia Feature Roadmap

**Created:** 2026-09-13 | **Direction:** diagrams first, automation second | **v1.0 platform:** WinForms

This is the active feature roadmap. It supersedes the status sections of `docs/archive/plan_enhancements.md` (which is stale — see the re-baseline note at the top of that file). Deployment/release engineering is intentionally out of scope; the focus is features and enhancements.

---

## Baseline (verified 2026-09-13)

| Check | Result |
|---|---|
| `dotnet build Beep.Skia.Solution.sln` | 0 errors |
| `dotnet test Beep.Skia.Tests` | 501/501 passing on net10.0-windows10.0.19041.0 (execution serialized; see notes) |
| Warnings | 0 - CI builds with warnings-as-errors (`CI=true` sets `TreatWarningsAsErrors`) |
| Render cost (1600×1000, median of 5) | 200 nodes: ~3.1 ms/frame, ~40.7 KB/frame; + 199 lines: ~4.5 ms/frame, ~40.8 KB/frame |
| Projects in solution | 27 (core + Model + Loader + 15 families + Quantitative + 5 hosts + 2 samples + tests) |
| Warning suppressions | none in Windows projects (NU1701 removed; dependency mismatch fixed by retargeting) |
| Out of solution | none — all 27 projects build |
| CI (`Beep.Skia.CI.slnf` + GitHub Actions) | validated locally: restore → build → 501 tests → 23 packages → consumed by a standalone project |
| Package version | MinVer: untagged → `1.0.0-alpha.0.<commit height>`; `v1.2.3` tag → `1.2.3` |
| Package metadata | 23/23 packages audited clean (descriptions, project/repository URLs, copyright) |
| Consumer matrix | net8.0 / net9.0 / net10.0 consumers all run the packaged library end-to-end (render, round-trip, workflow) |

**Phase A1 stabilization fixes applied:**

- `WorkflowEngine`: rewritten against the real `IWorkflowEngine`/`WorkflowDefinition` contracts; resolves nodes by component type; registers defaults in the constructor; initializes nodes with `NodeDefinition.Configuration`.
- `DrawingManager.ToWorkflowDefinition`: uses the real `AutomationNode.NodeType` and copies node configuration.
- `DrawingManager.Components` clipboard: fixed `kvp.Value?.Value` compile error; paste now records its created lines for redo.
- `Beep.Skia.Business/WorkflowEnums.cs`: restored the orphaned enum members as `ConnectionType`.
- SkiaSharp 4.x text migration completed in ETL, ECAD (13 files), UML, WellLogs, SwimlaneContainer (removed `SKPaint.TextSize/Typeface/TextAlign/MeasureText` and legacy `DrawText` overloads).
- Added missing family helpers (`ETLControl.LayoutPortsVerticalSegments`); aligned new UML nodes with UML's shared cardinal-port model.
- Fixed `AssetNode.AssetType` usage in `StrideAnalyzer` (uses `Category`), read-only `WellLogTrack.Curves` in `LasFileParser`, `IConnectionLine.DataTypeLabel` in DFD, `BusinessTask.TaskName` in `BpmnExporter`, `ConnectionLine.Tag` in PM, `BehaviorService` using in the WinForms designer.
- Solution integrity: added PM, DFD, Cloud, ECAD, ML, Security, Tests; removed the four broken experimental hosts.
- **Undo/redo repaired for all actions**: actions now implement `Execute()` (redo); `HistoryManager` suppresses nested history recording while replaying and caps depth (default 100).
- **Context menu activated**: `ContextMenu.AddStandardItems(DrawingManager, …)` wires Cut/Copy/Paste/Delete/Select All; the WinForms host shows it on right-click.
- **WinForms host keyboard routing**: keys reach `DrawingManager.HandleKeyDown` when the property editor has no active target.
- **Drag bug fixed**: `MoveSelectedComponents` and `MoveComponentsAction` used `Move` (absolute) with a delta, teleporting components; now use `MoveBy`.
- Regression tests added: `UndoRedoTests.cs` (add/delete/move undo-redo, history integrity, depth cap), `ExportTests.cs` (PNG/SVG/PDF files, negative coordinates, offscreen render), `SerializationRoundTripTests.cs` (schema version, theme, simple + complex property round-trip), `FlowchartCodeGeneratorTests.cs`, `FlowchartSimulatorTests.cs`, `CriticalPathCalculatorTests.cs` (lag from DependencyNode and line labels), `GanttTimelineTests.cs`, `DFDLevelNavigatorTests.cs`, `SchemaComparisonTests.cs` (diff + forward/rollback scripts + dialects + UNIQUE/CHECK constraints + end-to-end DDL parse), `ExpressionEngineTests.cs` (precedence, functions, LIKE, nulls, errors), `EtlProfilingTests.cs` (profiler stats, preview table, pipeline metrics), `StructuredDataFlattenerTests.cs` (JSON/XML flattening + schema inference), `NetworkAlgorithmsTests.cs` (PageRank, shortest/simple paths, components), `MindMapLayoutTests.cs` (radial layout), `MindMapVisibilityTests.cs` (collapse/expand, icons, serialization), `MindMapRichTextTests.cs` (bold/italic/bullets/rendering), `BpmnExporterTests.cs` (semantic elements, task types, DI, pools/lanes/message flows), `BpmnImporterTests.cs` (round-trip, mappings, warnings), `XmiExporterTests.cs` (classes, members, associations, generalizations), `UmlNodeTests.cs` (component/deployment/artifact, rendering), `UmlSequenceNodeTests.cs` (activation bars, combined fragments), `QuantitativeChartTests.cs` (series model, chart rendering per type, persistence), `SecurityAnalysisTests.cs` (DREAD scoring, MITRE library, STRIDE enrichment), `MLPipelineExporterTests.cs` (topology, JSON, cycles), `ElectricalRulesCheckerTests.cs` (pin model, short circuits), `WellLogTrackGroupingTests.cs` (LAS multi-track grouping), `CloudBrandNodeTests.cs` (provider identity, persistence, rendering), `WorkflowEngineTests.cs` (retry, variables, pause/resume, cancel), `TriggerTests.cs` (manual, schedule, event, file watch), `CredentialVaultTests.cs` (vault storage, encryption, connection resolution), `DataSourceProviderTests.cs` (provider bridge, fallback), `PaletteSearchTests.cs` (palette filtering, post-render add/remove, category rebuild), `MinimapTests.cs` (world mapping, viewport, rendering), `SkiaPropertyWrapperTests.cs` (write-back, color converter, multi-select), `DiagramTemplateTests.cs` (template loading, branch wiring, registry), `AccessibilityTests.cs` (selection cycling, host metadata, high contrast), `ExtensionSdkTests.cs` (discovery, registration, validation, commands), `CollaborationTests.cs` (sharing, roles, comments, presence, audit, JSON snapshot round-trip), `CommentPinLayerTests.cs` (anchoring, grouping, hit-testing, rendering), `DiagramAssistantTests.cs` (DSL parsing, layout, cycle/limit warnings, DTO generation, mind-map radial layout, registry fallback), `WorkflowExecutionServiceTests.cs` (publish/submit, queueing, inputs, pause/resume/cancel, listing, events), `WorkflowExecutionApiTests.cs` (diagram publishing, job lifecycle, inputs parsing, filtering, JSON), `WorkflowHttpRouterTests.cs` (route mapping, status codes, query parsing, publish→submit→poll lifecycle), `PanInteractionTests.cs` (middle-button panning, zoom-to-cursor), `ComponentRenderSmokeTests.cs` (render smoke for migrated path/text components, TextBox selection image-diff), `ExtensionMarketplaceTests.cs` (semantic versions, package build/registry, install/update/uninstall, host-version and dependency gates, zip-slip rejection, catalog persistence, package→host load), `StateMachineValidatorTests.cs` (guards, activities, regions, validation rules).

---

## Phase A — Stabilize & Activate (current)

### A1. Build + core interaction — DONE
Compile fixes, solution integrity, clipboard redo, context menu, host keyboard, undo/redo correctness.

### A2. Activate existing engines in the host — DONE

| Feature | Action | Status |
|---|---|---|
| Export correctness | Offscreen render with identity transform, exclude palette/property editor overlays, negative-bounds handling, PDF multi-page tiling, host Export PNG/SVG/PDF menu + public API | Done |
| Printing | `CreatePrintDocument` wired to `PrintPreviewDialog` (host context menu + sample); zero-size tile guard; tiles drawn at natural size instead of stretched | Done |
| ECAD ERC | "Run ERC" sample command with violation report | Done |
| Security STRIDE | "Generate Threats" materializes `ThreatNode`s on the canvas | Done |
| PM scheduling | "Compute Schedule" runs `CriticalPathCalculator`, reports duration + critical path | Done |
| DFD leveling | Requires drill-down navigation; deferred to Wave 1 | Pending |
| ERD DDL import | "Import DDL…" dialog; imports entities in a grid layout | Done |
| Network analysis | "Analyze Network" runs centrality + community detection and applies node colors | Done |
| Alignment UI | Sample "Align" dropdown for all 8 align/distribute methods | Done |
| Validation panel | Sample still uses a dialog; docked panel deferred | Pending |
| Auto-layout UI | Sample "Layout" dropdown: Grid / Hierarchical / Radial / Force-directed | Done |
| Serialization | DTO schema version, typed property bag for complex values, theme persistence, MindMap reference in sample | Done |
| Perf debt | Removed `%TEMP%\beepskia_render.log` writes from `X`/`Y` setters, `Update`, `UpdateBounds`, render loop, and 17 host sites | Done |

**Exit criteria:** clean build; every engine runnable from the WinForms UI; clean export/print; undo/redo survives drags/resizes/edits; save/load round-trips all families.

---

## Stage 2 — Family depth (all families in scope)

- **Wave 1 — Core diagramming (in progress):**
  - Flowchart code generation: **done** — `FlowchartCodeGenerator` emits Pseudocode/Python/C# for linear flows, decisions, loops, forks, sub-processes, connectors; sample "Tools → Generate Code…" preview with language switch, copy, save; 4 tests.
  - Flowchart step-through simulation: **done** — `FlowchartSimulator` with Step/Run/Reset, branch selector, trace, and step events; sample "Simulate" menu highlights the current node in amber; 4 tests.
  - PM scheduling: **done** — `CriticalPathCalculator` now consumes `DependencyNode.LagDays` (with line-label fallback) including negative lag; 4 tests.
  - StateMachine guards: **done** — `ConnectionLine` gains `GuardCondition`/`TransitionAction`/`TriggerEvent` with serialization; `StateMachineValidator` checks initial/final states, reachability, unguarded choice transitions, and unlabeled transitions; 5 tests.
  - StateMachine entry/exit/do activities: **done** — `StateNode.EntryAction/DoActivity/ExitAction` with rendering, NodeProperties, and serialization round-trip; 1 test.
  - StateMachine orthogonal regions: **done** — `CompositeStateNode.RegionCount` (1–4) with dashed dividers and round-trip; 1 test.
  - PM Gantt timeline view: **done** — `GanttTimelineNode` renders scheduled bars over a day grid with progress and critical-path coloring, persists rows via `RowsJson`; sample "Tools → Add Gantt Timeline"; 3 tests.
  - DFD drill-down/breadcrumb: **done** — `DFDLevelNavigator` manages decomposition levels with push/pop and breadcrumbs; sample "Tools → Drill Into Process / Go Up Level"; 4 tests.
  - PM resource leveling/working calendar: **pending**
  - DFD true parent/child balancing: **pending**
- **Wave 2 — Data professionals (in progress):**
  - ERD schema compare + migration/rollback scripts: **done** — `SchemaComparer` reports added/removed tables, added/removed/altered columns, and FK changes; `MigrationScriptGenerator` emits forward + rollback scripts for ANSI/SQL Server/PostgreSQL/MySQL/Oracle/SQLite; sample "Tools → Compare Schemas…" previews the report and scripts; 9 tests. Also fixed a DDL importer bug that truncated multi-character types (e.g., `VARCHAR` parsed as `VA`).
  - ETL expression engine + function library: **done** — `ExpressionEngine` (SQL-like parser/evaluator) with arithmetic, comparisons, AND/OR/NOT, IS NULL, LIKE, null propagation, case-insensitive column resolution, and functions (UPPER, LOWER, TRIM, LEN, CONCAT, SUBSTRING, REPLACE, LEFT, RIGHT, ROUND, ABS, FLOOR, CEILING, COALESCE, ISNULL, NULLIF, IIF, YEAR, MONTH, DAY, NOW); plugs into `DerivedColumnDefinition`, `SplitCondition`, and `DataQualityRule`; sample "Tools → Expression Tester…"; 11 tests.
  - ETL data preview/profiling + runtime metrics: **done** — `DataProfiler` (per-column count/nulls/distinct/min/max/average/sum/lengths/samples + report), `DataPreviewFormatter` (aligned text table), and `PipelineMetrics` (per-node rows in/out, elapsed, rows/sec, failure capture); sample "Tools → Data Profiler…" with demo data; 7 tests.
  - ETL structured XML/JSON nodes: **done** — `StructuredDataFlattener` flattens JSON/XML into rows (nested objects → dot columns, arrays preserved as compact JSON, XML attributes → `@name`, row-element inference) and infers a schema (BIGINT/FLOAT/BOOLEAN/TIMESTAMP/VARCHAR with nullability); sample "Tools → Flatten JSON/XML…"; 9 tests.
  - ERD constraints completeness: **done** — UNIQUE and CHECK constraints are detected by the comparer, included in `CREATE TABLE` generation, and emitted as `ADD CONSTRAINT`/`DROP CONSTRAINT` (MySQL UNIQUE uses `DROP INDEX`, SQLite emits rebuild guidance); 4 tests. Visual FK grid editor remains deferred (UI work).
- **Wave 3 — Modeling (in progress):**
  - Network PageRank + pathfinding: **done** — `NetworkAlgorithms` with PageRank (damping/iterations/convergence, dangling redistribution, bidirectional support), visual rank application (scale + color), Dijkstra/BFS shortest path, all simple paths, connectivity, connected components, and path cost; sample "Analyze Network" now reports top PageRank nodes and "Find Shortest Path" highlights the route between two selected nodes; 10 tests.
  - MindMap radial layout: **done** — `MindMapLayout` (IAutoLayout) places the central node, distributes level-1 topics around the circle, fans deeper levels within parent sectors, and parks unconnected nodes; sample "Layout → MindMap (radial)"; 4 tests.
  - BPMN task types + diagram interchange: **done** — `BusinessTask.TaskType` (Task/Service/User/Script/Manual/BusinessRule/Send/Receive) maps to the correct `bpmn:*Task` element; `BpmnExporter` now emits BPMN DI (`BPMNDiagram`/`BPMNPlane`/`BPMNShape` with `dc:Bounds`, `BPMNEdge` with waypoints) alongside sequence flows; sample "Tools → Export BPMN…"; 5 tests.
  - UML XMI export: **done** — `XmiExporter` emits XMI 2.5 for classes (abstract + attributes/operations with visibility/types), interfaces, actors, use cases, packages, associations (roles), and generalizations; sample "Tools → Export XMI…"; 5 tests.
  - BPMN import: **done** — `BpmnImporter` parses BPMN XML (namespace-agnostic) into StartEvent/EndEvent/tasks/gateways/intermediate events/sub-processes, applies BPMN DI bounds, resolves sequence flows (with warnings for dangling refs), and can load directly into a `DrawingManager`; round-trip tests against the exporter. Also fixed `IntermediateEventNode` declaring 0 inputs, which made intermediate events impossible to connect. 6 tests.
  - MindMap collapse/expand + icons: **done** — `MindMapControl.Icon` (glyph drawn top-left) and `IsCollapsed` with collapse badges; `MindMapVisibility.Apply` hides descendants and their lines (new `IConnectionLine.IsVisible` honored by the renderer); `MindMapLayout` skips hidden nodes; sample "Tools → Toggle Collapse"; 5 tests.
  - MindMap rich text and explicit relationship links: **pending** (cross-branch links already work as connection lines)
  - UML component/deployment/artifact nodes: **done** — `UMLComponentNode` (component icon, provided/required interfaces), `UMLDeploymentNode` (3D node box, Device/ExecutionEnvironment), `UMLArtifactNode` (folded-corner artifact); all round-trip through serialization and export as `uml:Component`/`uml:Node`/`uml:Artifact` in XMI. Also fixed a latent rendering bug where the base `DrawUMLContent` never invoked `DrawShape`, so UseCase/SystemBoundary/Package/Activity nodes rendered nothing. 5 tests.
  - BPMN message flows and pools/lanes: **done** — `BpmnPoolNode` (participant container with title bar), `BpmnLaneNode` (lane with header), and `BpmnMessageFlow` (dashed line); the exporter emits `bpmn:collaboration` with one participant + process per pool (nodes assigned by geometric containment), `bpmn:laneSet`/`bpmn:lane` with `flowNodeRef`s, and `bpmn:messageFlow` for cross-pool or explicit message lines; single-process output is unchanged when no pools exist; 4 tests.
  - UML sequence activation bars + combined fragments: **done** — `UMLActivationBar` (vertical bar with optional rotated label) and `UMLCombinedFragment` (operator tab with alt/opt/loop/par/break/critical/neg/assert/ignore/consider, guard text, dashed operand divider for alt/par); both round-trip through serialization; 4 tests.
  - MindMap rich text: **done** — `MindMapRichText` parses `**bold**`, `*italic*`, and `- ` / `* ` bullets with word wrapping; `NoteNode` renders notes with it; 4 tests.

Wave 3 is complete. Next: Wave 4 — Specialized families.
- **Wave 4 — Specialized (complete):**
  - Quantitative charts: **done** — `ChartSeries` data model; `ChartNode` now actually renders Line/Area/Bar/Scatter/Histogram with auto-scaled axes, grid, zero line, legend, max-point capping, and JSON persistence (Candlestick/Heatmap fall back to Line until OHLC/grid data models land); sample "Tools → Add Sample Chart"; 11 tests.
  - Security DREAD + MITRE ATT&CK: **done** — `DreadCalculator` (severity/likelihood → D/R/E/A/D ratings, average, risk level) and `MitreAttackLibrary` (33 techniques across 11 tactics with STRIDE mapping); `StrideAnalyzer` now enriches every threat with DREAD scores and techniques; sample STRIDE report shows both; 6 tests.
  - ML pipeline export hooks: **done** — `MLPipelineExporter` emits a portable JSON definition (topologically ordered nodes with hyperparameters + labeled edges) consumable by external frameworks; handles cycles; sample "Tools → Export ML Pipeline…"; 4 tests.
  - ECAD pin model + short-circuit checks: **done** — `ElectricalPinType`/`ElectricalPinModel` infer pin roles (Power/Ground/Passive/Input/Output) per component and port direction; `ElectricalRulesChecker` now flags direct power-to-ground connections as Error `ShortCircuit` violations; sample ERC reports them; 5 tests.
  - WellLogs multi-track: **done** — the LAS parser now groups curves into a Depth track plus family tracks (Gamma Ray, Resistivity, Porosity, Sonic, Caliper, Other) with stable ordering, while the data section still maps columns by original file order; 4 tests. (DLIS/RP66 binary parsing remains out of scope.)
  - Cloud provider-specific icons: **done** — `CloudBrandNode` base renders a brand accent bar, glyph badge, service, and provider; 12 branded nodes for AWS (EC2/S3/Lambda/RDS), Azure (VM/Blob/Functions/SQL), and GCP (Compute/Storage/Functions/SQL) with distinct brand colors; 15 tests.

Wave 4 is complete. Next: Stage 3 — Automation runtime (Beep.Make vision).

## Stage 3 — Automation runtime (Beep.Make vision) — COMPLETE

- **Engine completion: done** — retry policies (Fixed/Linear/Exponential with caps, node re-initialization between attempts), workflow variables seeded into the execution context, real pause/resume (semaphore gate blocks the loop), and real cancel (per-execution `CancellationTokenSource` stops in-flight nodes); `StopAsync(graceful:false)` cancels all controls. 6 tests.
- **Trigger pack: done** — `TriggerBase` plus `ManualTrigger`, `ScheduleTrigger` (interval-based with validation), `EventTrigger` (webhook/API/data-change via `Notify`), and `FileWatchTrigger` (FileSystemWatcher with create/change/delete/rename events); 8 tests.
- **Credential vault + connection manager: done** — `CredentialVault` (named credentials with types, masking in listings, AES + PBKDF2 encrypted export/import, wrong-passphrase detection) and `ConnectionManager` (named connections, credential resolution into flat settings, config-level `TestConnection`); 11 tests.
- **Execution UX: done** — `AutomationNode.SetExecutionStatus` host hook; sample "Tools → Run Workflow" executes the canvas through the engine with live per-node status coloring (Executing/Completed/Failed), cancel support, and a run-history viewer; sample "Cancel Workflow" and "Run History…".
- **Expression builder UI: done** — the sample expression tester is now a builder: column list (double-click inserts `[name]`), operator buttons, function palette with caret placement inside parentheses, live typed results.
- **Real BeepDataSources connector wiring: done** — `IAutomationDataSourceProvider` + `AutomationDataSourceRegistry` ambient bridge; `DataSourceAutomationNode` prefers a host-registered provider (BeepDataSources/BeepDM) and falls back to the DME editor or simulated data; 5 tests.

Stage 3 is complete. Next: Stage 4 — Canvas/editor UX & ecosystem.

## Stage 4 — Canvas/editor UX & ecosystem — COMPLETE

- **Palette search: done** — `Palette.SearchText`/`ApplyFilter` filter items by name or category (case-insensitive), `VisibleItemCount` for hosts/tests; the WinForms host now shows a "Search palette…" box above the in-canvas palette. 6 tests.
- **Minimap: done** — `MinimapControl` renders a scaled overview of components and lines with a viewport rectangle derived from pan/zoom, plus `CalculateWorldBounds`/`CalculateScale`/`MapWorldToMinimap`/`CalculateViewportWorldRect` helpers; the WinForms host places it bottom-left. 5 tests.
- **PropertyGrid write-back + color editor + multi-select: done** — wrapper edits now apply through public setters (with a change callback for canvas invalidation), `SkiaColorConverter` edits SKColor properties as `#AARRGGBB` hex, and `SkiaMultiComponentWrapper` exposes geometry + common NodeProperties for multi-selection with apply-to-all semantics; host syncs the wrapper on single and multi selection. 7 tests.
- **Template coverage + wiring fix: done** — added FlowChart Purchase Approval and StateMachine Order Lifecycle templates (12 total); fixed two real defects: `LoadTemplate` ignored template endpoints (auto-connected sequentially, mis-wiring every branch) and `AQN` used the wrong FlowChart namespace (`Beep.Skia.FlowChart` vs `Beep.Skia.Flowchart`), so flowchart templates loaded **zero** components. Templates now carry explicit endpoint indices. 6 tests.
- **Accessibility basics: done** — `SelectionManager.SelectNext` / `DrawingManager.SelectNextComponent` cycle selection in reading order (Tab / Shift+Tab, skipping static overlays, wrapping); `HandleKeyDown` handles Tab; the WinForms host intercepts Tab via `ProcessCmdKey`, exposes `AccessibleName`/`AccessibleRole.Diagram`/`AccessibleDescription` (updated with selection info), and auto-applies the HighContrast theme from `SystemInformation.HighContrast` (watching `SystemEvents.UserPreferenceChanged`). 7 tests.
- **Extension SDK: done** — `ISkiaExtension` + `ISkiaExtensionContext` contracts and `SkiaExtensionHost` (assembly/directory discovery, duplicate-id protection, component-type validation, custom categories/display names, named commands, diagnostic log, per-extension error capture); the WinForms host loads packages from an `Extensions` folder next to the app and adds their components to the palette. 8 tests.
- **Collaboration: done** — `CollaborationService` (users, document sharing with Viewer/Commenter/Editor/Admin roles, public view links, comments anchored to components, presence with activity + expiry window, audit trail), `CommentPinLayer` (review badges anchored to component corners, hit-testing, rendering), `DrawingManager.WorldOverlay` hook for interactive world-space annotations (excluded from exports), host integration (comment pins over the canvas, `AddCommentToSelection`/`ResolveComment`/`RefreshCommentPins`), `CollaborationSerializer` (JSON snapshot save/load of users/shares/comments/audit; presence excluded as ephemeral), sample Comments dialog + presence snapshot, and layout save/load now persists collaboration state alongside the diagram. 24 tests.
- **Fixed along the way:** `SkiaComponent.Width`/`Height` setters now update `Bounds` (previously stale when size was assigned after position).
- **Assisted generation: done (offline baseline)** — `IDiagramAssistant` provider contract, `DiagramAssistantRegistry` (priority order + fallback, failed-attempt warnings carried), `RuleBasedDiagramAssistant` (no external services), flowchart DSL parser (`Start(start) "Start" -> Valid(decision) "Valid?" : yes`, node types, edge labels, comments, start/end inference, cycle + node-limit warnings, top-down ranked layout), indentation mind-map outline parser with radial layout, DTO output loadable via `LoadFromDto`, and a sample Generate dialog (kind selector, sample prompts, warnings, insert into canvas). Cloud/LLM assistants plug in via the registry. 18 tests.
- **Fixed along the way:** `LoadFromDto` now honors `StartComponentIndex`/`EndComponentIndex` line connections (previously GUID-only, so index-based DTOs silently lost all lines).
- **Server execution SKU: done (service + API facade)** — `WorkflowExecutionService` (publish workflows, submit jobs with inputs, bounded-concurrency queue, job states Queued/Running/Paused/Completed/Failed/Cancelled, pause/resume/cancel by execution id, job listing/filtering, state-change events, wait-for-completion) and `WorkflowExecutionApi` (JSON envelope `ApiResponse`, publish a diagram DTO as an executable workflow via `ToWorkflowDefinition`, submit with JSON inputs, job/workflow queries, cancel/pause/resume, `ToJson` for any transport). Sample "Execution Service…" dialog publishes the current diagram, submits jobs, and shows live state with pause/resume/cancel. An HTTP host (ASP.NET, HttpListener, Functions) is a thin adapter over the facade. 19 tests.
- **Marketplace: done (offline-first)** — `SemanticVersion` (prerelease-aware compare), `ExtensionManifest` + `.beepkg` builder/reader (zip with manifest.json), `IExtensionRegistry` + `LocalExtensionRegistry` (folder-backed search/get/download, pluggable for a hosted registry), `ExtensionPackageManager` (install/update/uninstall with min-host-version and dependency checks, duplicate protection, zip-slip-safe extraction, persistent `installed.json` catalog, operation log), host integration (`ExtensionsRoot`, package manager, `ReloadExtensions()` that refreshes palette entries), and a sample Extensions manager dialog (install from file/registry, update all, uninstall, reload, log). 16 tests including an end-to-end package → install → host-load test.
- **Cross-platform hosts: WPF, Blazor, and MAUI are back in the solution** — `SkiaComponentDescriptor`/`SkiaComponentDescriptorCollection` moved from the WinForms project into core `Beep.Skia` (all hosts referenced them but could not see them); WPF fixed (`SkiaSharp.Views.Desktop` using for `SKPaintSurfaceEventArgs`, `PaletteItem`-based `AddItem`, `System.Windows.Input.MouseButton` qualification); MAUI retargeted to `net10.0-android/ios/maccatalyst/windows` with `SkiaSharp.Views.Maui.Controls` 4.150.1 (was net8.0 EOL workloads + a SkiaSharp 3.x pin). Blazor needed no changes.
- **Fixed along the way (palette):** `Palette.RefreshLayout()` now rebuilds categories instead of only doing so when they are empty — items added after the first render (extensions, marketplace installs, registry components) were previously invisible because the draw path iterates `Categories`, not `Items`; host call sites now use `AddItem`/`RemoveItem`. 4 regression tests.
- **Avalonia host: fixed without the missing package** — `SkiaSharp.Views.Avalonia` does not exist on NuGet and `Avalonia.Skia` pins SkiaSharp 3.119.4 against the core's 4.150.1, so the host was rewritten around a new `SkiaSurface : Control` that renders into an offscreen `SKSurface` backed by an Avalonia `WriteableBitmap` (BGRA8888 premultiplied, DPI-aware) and blits it — dependencies are now just Avalonia 11.3.5 + SkiaSharp 4.150.1, matching the rest of the solution.
- **All five hosts are in the solution** (WinForms, WPF, Blazor, MAUI, Avalonia) plus the HTTP server sample: 27/27 projects build, 0 errors.
- **HTTP transport host: done** — `WorkflowRequestRouter` in core maps HTTP method/path/query/body onto `WorkflowExecutionApi` (health, workflows publish/list, jobs list/submit/detail/cancel/pause/resume) with proper 200/400/404 codes, tolerant query parsing (URL-encoded, repeated, inline `?` in path) and a testable request/response pair; `Beep.Skia.Sample.Server` hosts it over ASP.NET minimal API (single catch-all endpoint, JSON from the same `ToJson` envelope, `--url/--concurrency/--demo/--publish/--help` options) and was smoke-tested end-to-end (publish → submit with inputs → poll → 404/400 paths). 13 tests.
- **Fixed along the way:** the sample server was first written on `HttpListener`, which rejects bodyless `POST` with HTTP 411 before the app sees the request — moved to ASP.NET minimal API. Also de-flaked `MaxConcurrency_SerializesJobs`: `SemaphoreSlim` is not FIFO, so the test now asserts non-overlapping execution windows instead of submission-order timestamps.
- **Roadmap complete.** Remaining work is release/product rather than feature code: code signing and hosting a public registry service.

---

## Release infrastructure — DONE

- **Versioning: MinVer 7** — `Directory.Build.props` applies `MinVer` (tag prefix `v`, minimum `1.0`, default pre-release `alpha.0`) plus shared package metadata (authors, company, license, project/repo URLs) and `Deterministic`/`ContinuousIntegrationBuild`. Untagged builds produce `1.0.0-alpha.0.<commit height>` (verified: `Beep.Skia.1.0.0-alpha.0.107.nupkg`); a `vX.Y.Z` tag produces that release version.
- **CI solution filter** — `Beep.Skia.CI.slnf` contains all 26 buildable projects; the MAUI host is excluded because GitHub-hosted Windows runners cannot build the `net10.0-ios`/`maccatalyst` targets (workloads plus a Mac). Build MAUI locally with `dotnet build Beep.Skia.Maui.Controls`.
- **GitHub Actions workflow** — `.github/workflows/ci.yml` runs on pushes/PRs to `master`/`main`/`develop`, `v*` tags, and manual dispatch: `build-test` job (checkout with `fetch-depth: 0` for MinVer, .NET 8/9/10 SDKs, restore, Release build with `GeneratePackageOnBuild=false`, tests with a TRX artifact) and a `pack` job that produces 23 NuGet packages on tags/manual runs and uploads them.
- **Packaging hygiene** — the WinForms sample is now `IsPackable=false` (it was producing a package); `.gitignore` covers `artifacts/`, `LocalNugetFiles/`, `outputDLL/`, `TestResults/`, and the sample's runtime `skia_layout.json`/`skia_collaboration.json`.
- **Remaining (product ops, not code):** code signing, a hosted public registry service, and publishing the packages.

---

## Quality pass — core warning-free

- **Middle-button panning implemented** — `_isPanning` was a vestigial field (`IsPanning` always false) and there was no pan gesture at all; middle-button drag now pans the view (start offset + delta), never touching selection, components, or line drawing. 5 tests (`PanInteractionTests.cs`).
- **`CascadingMenuItem.Clicked` was a dead shadow** — it re-declared (with `new`) the base `MaterialControl.Clicked` event, which *is* raised, so subscribers to a menu item's `Clicked` were never notified. The shadow is removed.
- **`TextBox` selection implemented** — `SelectAll()`/`ClearSelection()` only mutated fields that nothing read (no visible effect). Added `SelectionStart`/`SelectionEnd`/`HasSelection`/`SelectedText`/`SelectionColor` and selection highlighting behind the text; verified by image-diff rendering tests.
- **SkiaSharp 4 deprecations migrated in the core** — 28 `SKCanvas.DrawText(text, x, y, font, paint)` calls now use the `SKTextAlign` overload; 19 `new SKPath()` sites migrated to `SKPathBuilder` + `Detach()` (which also fixes 7 undisposed `SKPath` leaks in Dropdown/Search/SegmentedButtons/SplitButton).
- **Portability + crypto** — `DrawingManager.CreatePrintDocument` is now `[SupportedOSPlatform("windows")]` (54 CA1416 warnings; the API throws off-Windows), and `CredentialVault` derives keys with `Rfc2898DeriveBytes.Pbkdf2` instead of the obsolete constructor (SYSLIB0060).
- **Dead members removed** (`SplitButton._selectedText`, `WorkflowCanvas._draggedConnection`, unused `ex`/`ex2`/`ex3` in the Loader).
- **`Beep.Skia` core and `Beep.Skia.Loader` now build with 0 warnings**; 11 new render smoke tests cover the migrated components (`ComponentRenderSmokeTests.cs`).
- **Follow-up (family projects) — DONE:** all 658 remaining warnings cleared.
  - **484 CS0618 SkiaSharp 4 deprecations** → 29 `DrawText` calls migrated to the `SKTextAlign` overload and 99 `new SKPath()` sites across 82 files migrated to `SKPathBuilder` + `Detach()` (also fixing undisposed `SKPath` leaks). Applied with a reviewed, backup-first script; four sites needed hand fixes (detach landing inside an `if`/switch scope, `using var` in a switch section).
  - **~154 nullable warnings** → optional DTO members declared `string?`/nullable (`DDLImporter`, `SchemaChange`, `ElectricalRuleViolation`, `DFDBalanceIssue`, `StateMachineIssue`, `ThreatResult`, `SimulationEdge`, `MLPipelineEdge`, …), null-tolerant private signatures made explicit (`Quote`/`QuoteList`/`Append`, `CreateTable`/`AddColumn`/`AlterConstraint` family, `Describe`, `Normalize`, `GetLabel`, `FindEntry`, `Find`), and the WinForms sample's designer-initialized fields given `= null!` initializers.
  - **57 CA1416** → `DrawingManager.CreatePrintDocument` marked `[SupportedOSPlatform("windows")]`; the Blazor host marked `[SupportedOSPlatform("browser")]`.
  - **9 unused members** and the MAUI `MA002` implicit-package warning fixed (explicit `Microsoft.Maui.Controls` reference).
- **Result: the whole solution builds with 0 warnings and 0 errors** (`Beep.Skia.CI.slnf` and `Beep.Skia.Solution.sln`), 369/369 tests passing. `TreatWarningsAsErrors` is now enabled whenever `CI=true`, so the state is enforced on every pipeline run.

---

## Performance pass (render loop)

- **Hot-path allocations removed** — `RenderingHelper.DrawAll` allocated three lists plus LINQ enumerators and three `DrawingContext` objects every frame; it now partitions components in a single pass into reused buffers and reuses the contexts.
- **Comment-pin overlay no longer rebuilds every frame** — the host rebuilt the pin layer inside the paint callback (dictionary + LINQ per frame); it now rebuilds on `DrawSurface` (which fires on edits and drags) and only draws during paint.
- **Measured baseline** (`RenderPerformanceTests.cs`, 1600×1000, median of 5 runs): 200 nodes ≈ **4.1 ms/frame**; 200 nodes + 199 lines ≈ **3.9 ms/frame** at ~58 KB/frame allocated. The test fails if frame time exceeds 100 ms or allocations exceed 512 KB/frame.
- **Typeface caching** — `TypefaceCache` (core) caches `SKTypeface` per (family, weight, width, slant); all 58 per-draw `SKTypeface.FromFamilyName` calls across core/UML/ETL/MindMap now go through it. Micro-benchmark in the perf test: **2.21 µs → 0.17 µs per lookup (13× faster)**, i.e. per-frame font matching is gone for text-drawing components. The cache is process-lifetime and thread-safe (verified under parallel access); it deliberately exposes no `Clear()` — disposing shared typefaces while other threads render caused a race in early testing. 6 tests.
- **Known next step (documented, not done):** the remaining per-frame allocations come from per-draw `SKPaint`/`SKFont` creation inside component and line `Draw` implementations. Caching paints per component (invalidated on style change) is the highest-value follow-up if profiling shows GC pressure at larger diagram sizes.

---

## Hardening pass (adversarial inputs)

New `HardeningTests.cs` (49 tests) feeds malformed, hostile, and extreme data to every external entry point — diagram DTO load/save, DDL import, expression engine, both DSL parsers, collaboration JSON, marketplace archives, and schema comparison. It found and fixed **six real defects**:

1. **Stack overflow / process crash** — the `ExpressionEngine` recursive-descent parser had no depth limit, so a hostile expression (thousands of nested parentheses, e.g. from a data file) crashed the whole process. Now capped at 100 levels with a clear `ExpressionException("Expression is nested too deeply (limit 100)")`.
2. **`ExtensionPackageBuilder.ReadManifest` threw `InvalidDataException`** on a corrupt archive instead of returning null (the registry already guarded this, the public API did not).
3. **`DrawingManager.LoadFromDto` threw `NullReferenceException`** when `Components`/`Lines` were null — now tolerated.
4. **`SchemaComparer.Compare` threw `ArgumentException`** on duplicate table names (common in messy dumps) — now last-definition-wins.
5. **`ToDto()` could produce un-serializable JSON** when geometry contained `NaN`/`Infinity` (saving such a diagram threw) — non-finite coordinates are now sanitized to 0.
6. **`DDLImporter` could not parse quoted identifiers containing spaces** (`CREATE TABLE "Order Items" (...)`) — the table-name regex now accepts `"..."`, `[...]`, `` `...` `` and plain identifiers.

The remaining hardening tests pin the tolerant behaviour: truncated SQL, unterminated comments/strings, empty DSL input, self-referencing/cyclic graphs, malformed collaboration JSON, corrupt packages, and empty/duplicate schemas all produce domain errors or safe defaults — never crashes.

### Recursive parser hardening (`RecursiveParserHardeningTests.cs`, 13 tests)

A second pass targeted every remaining recursive parser and found **three more crash paths plus two robustness gaps**:

1. **`ExpressionEngine` unary chains** (`---------1`) overflowed the stack — `ParseUnary` recursion was unbounded. Guarded.
2. **`ExpressionEngine` function nesting** (`ABS(ABS(ABS(...)))`) — the function-argument path recursed outside the parenthesis guard. Guarded.
3. **`ExpressionEngine` evaluation of long flat chains** (`1+1+1+…` ×2000) — parsing is iterative, but the **evaluator** walks the resulting left-deep tree recursively and overflowed. Operator-node count is now capped at 1000 (`ExpressionException("Expression is too large…")`), which bounds evaluation depth; 500-term expressions still evaluate normally.
4. **`StructuredDataFlattener` had no error handling at all** — malformed JSON/XML threw instead of returning no rows. Both `FlattenJson` and `FlattenXml` now return an empty result for unparseable input (matching their handling of empty input).
5. Verified bounded behaviour for: deeply nested JSON objects/arrays (bounded by `MaxDepth`), deeply nested XML, hostile BPMN XML (DTD/entity, unclosed tags, dangling references, 20k-deep nesting), and hostile LAS files.

### Mind-map depth hardening (`MindMapDepthHardeningTests.cs`, 2 tests)

6. **Mind-map outline parser overflowed on a 20k-deep outline** — the radial layout walks the tree recursively (`Size`/`Place`). A first attempt that capped only the `Depth` *value* still crashed, because the nodes remained a 20k-long **child chain**; the fix flattens by re-attaching deep items to the deepest allowed *ancestor* (`MaxOutlineDepth = 100`), keeping the tree itself shallow, with a warning. Also skipped the quadratic collision scan above 2000 nodes so layout time stays bounded. Outlines ≤30 levels are unaffected (verified).

### Security hardening (`SecurityHardeningTests.cs`, 10 tests)

7. **Decompression-bomb protection for the marketplace** — package extraction had no size limits, so a tiny `.beepkg` could expand to gigabytes (a classic zip bomb) or exhaust inodes with a huge entry count. `ExtensionPackageManager` now enforces per-entry size (256 MB), total uncompressed size (512 MB) and entry-count (10,000) limits, all configurable, and a rejected install leaves **no partial state** (the version *and* the now-empty extension folder are removed).
8. **Credential-vault hostile documents** — malformed JSON and a non-base64 salt surfaced as `JsonException`/`FormatException` from a "load vault" call; both now raise `InvalidDataException` with a clear message. Verified that an encrypted export refuses to import without a passphrase, rejects a wrong passphrase, rejects **tampered ciphertext**, never contains the plaintext secret, and still round-trips correctly.

### Concurrency hardening (`CollaborationConcurrencyTests.cs`, 6 tests)

9. **`CollaborationService` was not thread-safe** — plain `Dictionary`/`List` state shared by UI and server callers, so concurrent comments/grants/presence updates could corrupt state or throw. All state is now guarded by a single lock (reentrant, so nested permission checks are fine) and the read APIs return **snapshots**: `GetShare`/`Share` previously handed out the *live* internal `DocumentShare`, letting any caller add itself as Admin or flip `IsPublic`, bypassing every role check. `CredentialVault` (shared by concurrent workflows) received the same lock treatment. Verified with 8-thread stress tests (400 concurrent comments with zero loss, presence collapsing to one entry per user, interleaved grant/revoke keeping the owner Admin, concurrent snapshot+mutate with no exceptions).
10. **`ExtensionPackageManager` catalog/log had the same race** — parallel installs could corrupt the catalog dictionary, the operation log, or the `installed.json` write. Guarded, and verified with parallel installs of 8 packages (all succeed, catalog complete and readable by a fresh manager) plus mixed read/install traffic.
11. **Verified the automation runtime is already concurrency-safe** — the server SKU runs several jobs through one `WorkflowEngine` and one `WorkflowExecutionService`: 8 workflows executed in parallel while 4 threads continuously read execution history, plus 20 queued jobs through the service and 100 mixed submit/query calls — no exceptions, no lost history.

### Resource-lifecycle audit (`TriggerLifecycleTests.cs`, 5 tests)

12. **Triggers were not `IDisposable`** — a host that abandoned a running trigger leaked its `FileSystemWatcher` (file-watch) or its background loop + `CancellationTokenSource` (schedule). `TriggerBase` now implements `IDisposable` (stops the trigger, idempotent, safe on stopped triggers), verified by tests that dispose a *running* trigger and assert it stops firing/watching and can be restarted afterwards. The audit also confirmed the engine disposes each execution's semaphore + CTS in its `finally` block, and `WorkflowExecutionService.Dispose` cancels outstanding jobs — no per-job leaks in the server path.
13. **The warnings-as-errors gate immediately proved its worth**: it failed the CI build on `xUnit1031` (blocking task operations) in the new tests I had just written, forcing them to be properly `async`/`await`.

### Loader-extension hardening (`LoaderExtensionTests.cs`, 6 tests — the last untested library)

14. **`BeepSkiaLoaderExtensions` leaked a process-wide subscription** — it hooked `AppDomain.AssemblyResolve` in its constructor and never unhooked, so a discarded instance stayed alive and ran its resolution logic for every unresolved assembly in the process. It now implements `IDisposable` (idempotent unsubscribe).
15. **Its resolve handler could throw into the runtime** — `Loader.Assemblies`/`Loader.ConfigEditor.Config.Folders` were dereferenced unguarded, and exceptions from an `AssemblyResolve` handler surface as confusing load failures elsewhere. The handler is now fully guarded (null loader/config, null `DllLib`) and wraps everything in a catch-all that returns null.
16. **`Scan()` reported success regardless of outcome** — it called `LoadAllAssembly()` and unconditionally set `Errors.Ok`, hiding a null/misconfigured loader. It now propagates the result. `LoadAllAssembly` also returns a descriptive failure instead of throwing `NullReferenceException` when no loader was supplied.

### Documentation + repo hygiene

- **README rewritten** — the previous README still described "Phase 1-3" fixes, the .NET 6/7/8 targets and SkiaSharp 2.88, listed files that no longer exist, and contained mojibake (broken box-drawing/emoji from an earlier encoding mixup). The new README covers the 16 diagram families, automation runtime, server SKU, extension SDK/marketplace, collaboration, assisted generation and editor UX, with verified quick-start snippets, an accurate repository layout, build/CI/versioning notes (including the private `TheTechIdea.Beep.*` feed requirement) and an ASCII-only body so it renders identically everywhere.
- **Repository layout cleaned up** — the root now holds only what belongs there (`README.md`, `FEATURE_ROADMAP.md`, `LICENSE.txt`, the solution/filter, `Directory.Build.props`, `.gitignore`/`.gitattributes`, the workspace file). Project documentation moved to `docs/` (gap analyses, feature notes, guides, design plans); historical plans and development scratch moved to `docs/archive/` (`plan.md`, `plan_enhancements.md`, `progress.md`, `CoordinateTest.cs`, `test_coordinates.ps1`, and an unrelated third-party Tizen `workload-install.ps1` that had been committed by mistake). Nothing was deleted — git history and the files themselves are intact — and the build/test suite is unaffected. `skia_layout.json` (the sample's saved diagram) was left at the root as an example artifact.
- **Help site expanded and corrected** — the `Help/` documentation site grew from 52 to **73 pages**: new Automation (5), Ecosystem (4) and Editor (1) sections plus `guides/platforms.html`, `guides/testing-and-quality.html` and `guides/release-notes.html`, all generated with scripts (`_gen_automation_docs.ps1`, `_gen_ecosystem_docs.ps1`, `_gen_editor_guides_docs.ps1`, `_gen_release_notes.ps1`). The sidebar (`index.html`), landing page stats and `DOCUMENTATION_STATUS.md` were updated to match. A second round added the missing component coverage: `ui-components/inputs.html`, `buttons.html`, `navigation.html`, `lists.html`, `display.html` and `editors.html` (generated by `_gen_component_reference.ps1` from source metadata), `hosts/winforms.html` for the 30+ WinForms wrapper controls, `architecture/events.html`, `automation/nodes.html` and `reference/api-index.html` (every public type by project). A link checker (`_check_links.ps1`) verifies all 324 local references.
- **API accuracy pass over every Help page** — a systematic audit replaced API usage that did not match the framework: `DrawingManager` (`AddComponent`/`GetComponents`/`GetLines`/`ConnectComponents` void, `GridSpacing`), `ConnectionLine` (`LineColor`, `Paint.StrokeWidth`, `ShowEndArrow`, `Label1`, `RoutingMode`), component geometry (`X`/`Y`/`Width`/`Height`, `InConnectionPoints`/`OutConnectionPoints`), `ParameterInfo` (`ParameterName`/`ParameterCurrentValue`/`ParameterType`/`Choices`), `SkiaTheme`+`ThemeManager`, `SelectionManager`/clipboard, `HistoryManager`, the extension SDK (`ISkiaExtension`/`SkiaExtensionHost`/`ExtensionPackageManager`), `DiagramDto`/`ComponentDto`/`LineDto` serialization, and the lazy port layout (`MarkPortsDirty()` + `LayoutPorts()`). Removed invented APIs such as `SkiaCanvas`, `FlowChartShape`, `CommandHistory`, `DiagramSerializer`, `[AddinAttribute]`, `IPaletteProvider` and `MaterialDesignColors`.
- **Sixth documentation round (repo + in-app)** — the repository now carries the standard documentation set: `CONTRIBUTING.md` (private-feed prerequisite, build/test commands, conventions, how to add a family, how to regenerate the docs), `CHANGELOG.md` (Keep a Changelog format with the feature and hardening record), `SECURITY.md` (private reporting, the security-relevant surfaces and user guidance), `CODE_OF_CONDUCT.md` (Contributor Covenant 2.1), a `docs/README.md` index, and a generated `README.md` in **all 24 library projects** (purpose, TFMs, package id, starting types, links to the guide and API reference). The sample app gained **in-app help**: a Help menu (documentation, quick start, samples, troubleshooting, API reference, About) and **F1**, resolving the local `Help/` folder by walking up from the executable and falling back to the repository README. Enabling XML documentation earlier also surfaced **10 malformed doc comments in the source** (CS1570/CS1572/CS1573: bare `&` in "Gane & Sarson", `<<interface>>` stereotypes, a duplicated `<summary>`, a stray `</summary>`, missing `param` tags and a wrong parameter name); all were corrected and the build is warning-free again.
- **Fifth documentation round (depth pass)** — the audit turned from page count to content quality and found the architecture section was stubs (98-118 words each). The four core pages were rewritten with the real internals: `architecture/component-registry.html` (registry model, duplicate rules, registration paths, query surface, instantiation, consumers), `architecture/rendering-pipeline.html` (the seven frame passes, world vs. screen split, culling via `DrawingContext.Bounds`, export configuration, measured frame costs and the paint/typeface caches), `architecture/serialization-system.html` (DTO fields, property-bag routing, port-identity persistence, the seven-step load algorithm, invariants) and `architecture/interaction-system.html` (input flow with world-space conversion, drag/selection state, keyboard and clipboard API, undo integration). `getting-started/installation.html` was rewritten from scratch because it documented four packages that do not exist (`Beep.Skia.Canvas`, `.Nodes`, `.Connections`, `.Serialization`), SkiaSharp 2.88 dependencies and a missing private-feed prerequisite; it now lists the real 24 packages, the actual TFMs (including why the Windows hosts need `net10.0-windows10.0.19041.0`), the real dependency versions and a working `nuget.config`. Also expanded: the trigger contract (`TriggerBase` members, `TriggerType` values, corrected `ManualTrigger`/`TriggerEventArgs` usage), the credential vault and connection manager APIs, the automation data-source provider seam (corrected from the earlier multi-provider sketch to the real single `AutomationDataSourceRegistry.Provider`), the Material Design surface (`MaterialControl` members, `MaterialDesignColors`) and the layout-engine contracts. `Beep.Skia.Sample.Server` is now `IsPackable=false` so the sample is not shipped as a package. Build remains warning-free.
- **Fourth documentation round (105 pages)** — folded the feature documents that lived only under `docs/` into the Help site and closed the type-coverage audit: new pages `core-concepts/connection-animation.html` (the 7 `FlowAnimationStyle` effects, data-flow properties, `LineStatus` indicators, `ERDMultiplicity` markers), `core-concepts/validation.html` (`DiagramValidator`, the six built-in rules, custom `IDiagramRule`s), `architecture/event-catalog.html` (all 16 `DrawingManager` events plus `ComponentInteractionEventArgs`/`LineInteractionEventArgs`/`DiagramInteractionEventArgs` and `InteractionType`), `automation/jobs.html` (`WorkflowExecutionService`, `ExecutionJob`/`JobState`, `WorkflowResult`/`NodeResult`, engine `WorkflowExecution`/`NodeExecution`), `diagram-families/erd-advanced.html` (DDL export/import dialects, `SchemaComparer`, `MigrationScriptGenerator`) and `diagram-families/etl-advanced.html` (lookups and fuzzy matching, SCD/CDC, reshape nodes, `ExpressionEngine`, `DataProfiler`, `PipelineMetrics`). Generated type-reference sections were appended to the layout, history, assisted-generation, marketplace, collaboration and performance pages, and the audit of undocumented core/model types dropped from 98 to 51 (the remainder are model records covered by the API reference). Two more fiction fixes: the layout page claimed every engine implements `ILayoutManager` (the graph engines implement `IAutoLayout.Arrange`) and the history page listed a non-existent `PropertyChangeAction`.
- **Third documentation round** — the site reached **99 pages**: 24 per-project API reference pages covering all ~890 public types with member tables (generated by `_gen_api_reference.ps1`), a `getting-started/samples.html` walkthrough of the WinForms editor and workflow server, a `guides/troubleshooting.html` FAQ (restore/feed, NU1701, ports, drop offset, theme repaint, serialization type resolution, workflow and extension failures, test parallelization), and a site-wide full-text search (`search-index.js`, 98 pages, scoring by title/heading/body) wired into the sidebar box. `_check_links.ps1` now verifies 437 local references with zero broken.
- **XML documentation and package READMEs** — `Directory.Build.props` now sets `GenerateDocumentationFile` for every library, so the produced packages carry `lib/<tfm>/<Assembly>.xml` for IntelliSense, and the repository README is packed as the package readme. CS1591 is suppressed so the warning-free build invariant holds. Verified in the packed `Beep.Skia` nupkg (XML for net8.0/net9.0/net10.0 plus `README.md`).
- **Encoding fixes** — the last U+FFFD replacement characters (7 in `Beep.Skia/ConnectionLine.cs`, 1 in `Help/sphinx-style.css`) were repaired with their intended characters, and the Help generators now write UTF-8 without BOM.
- **Corrections to the accuracy pass** — three real APIs were initially removed by the audit and have been restored with their correct documentation: `MaterialDesignColors` (static MD3 token class, 23 tokens), the lazy port gate `EnsurePortLayout(() => LayoutPorts())` with `ArePortsDirty`/`ClearPortsDirty`, and `DrawingManager.ComponentDropped` with `ComponentDropEventArgs`. The palette/registry reference was also corrected to the real `AssemblyClassDefinition` members (`className`, `dllname`, `GuidID`, `type`, `Methods`).

### Package consumption validated (release chain closed)

The one link never tested was **consuming** the produced packages. A standalone project was created *outside* the repository, pointed at a folder feed containing the packed `Beep.Skia`/`Beep.Skia.Model` `.nupkg` files (plus the private `TheTechIdea.Beep.*` feed), and it restored, compiled and ran a program that builds a diagram, renders and exports a PNG, round-trips it through serialization **with its connection intact**, and executes it as a workflow to completion. The full chain — source -> build -> test -> pack -> consume — is now verified.

Auditing the produced `.nuspec` files while doing so exposed packaging defects that would have shipped:

1. **18 packages carried the `Package Description` placeholder** — every family project and `Beep.Skia.Model` had no `<Description>`, so the published metadata was meaningless. Each now has a specific one-line description.
2. **13 packages advertised the wrong project/repository URL** — `https://github.com/The-Tech-Idea/` (the organisation root) instead of the repository, in both the project files and the shared `Directory.Build.props` default. All corrected.
3. **Two unused dependencies were being shipped to consumers** — `System.Text.Encoding.CodePages` (pinned at a **.NET 10 package version** for the net8.0/net9.0 targets, and never referenced in code) and `SkiaSharp.Views.Desktop.Common` (also unreferenced; the desktop view types are only used by the host packages). Both removed; the dependency surface of the core package is now just SkiaSharp, its Extended/Svg companions, and the Beep model packages.
4. Stale `2022` copyright replaced with `2025`, and the placeholder descriptions replaced (see 1).

A metadata audit over all 23 packages now reports **0 issues** (no placeholder descriptions, no organisation-only URLs), and the consumer still runs clean against the repacked output.

### Platform-target and dependency audit (found by the consumer test)

Consuming the **WinForms host package** exposed a deeper problem: the project emitted `NU1701` — *"package restored using .NET Framework assets instead of the project target framework"* — and the projects **suppressed that warning**. Root cause: **SkiaSharp's 4.x view packages ship no `net8.0` assets** (`SkiaSharp.Views.WindowsForms`/`WPF` 4.150.1 offer only `net9`/`net10-windows10.0.19041` plus `net462`/`net48`), so every Windows project silently fell back to **.NET Framework assemblies** while the suppression hid it from CI.

Fixes applied:

1. **Retargeted the four Windows projects** (`Beep.Skia.Winform.Controls`, `Beep.Skia.Wpf.Controls`, `Beep.Skia.Sample.WinForms`, `Beep.Skia.Tests`) from `net8.0-windows`/`net9.0-windows` to `net10.0-windows10.0.19041.0`, matching the packages' native assets and the installed SDK/runtime, and **removed the `NU1701` suppressions** — the build is now honest instead of hiding a mismatch. (A `net9.0-windows10.0.19041.0` attempt hit a WinForms runtime roll-forward mismatch, `System.Private.Windows.Core 10.0.0.0`, so the SDK's own TFM is the consistent choice.)
2. **WinForms designer analyzers (`WFO1000`) activated** by the retarget flagged four control properties with no designer-serialization metadata — `SkiaComponent`, plus `ExtensionsRoot`, `DocumentId` and `ShowCommentPins` (all added this session). They are runtime settings, not designer state, so each is now `[DesignerSerializationVisibility(Hidden)]`.
3. **`System.Windows.Forms.ContextMenu` ambiguity** surfaced under net10 (the framework type now collides with the in-canvas `Beep.Skia.Components.ContextMenu`); the host's field and construction are fully qualified.
4. **`NU1901`** — the assembly-loader dependency brings `NuGet.Packaging`/`NuGet.Protocol` 7.3.0, which carry a known low-severity advisory (GHSA-g4vj-cjjj-v7hg). Both are pinned to the patched **7.3.1** in `Beep.Skia.Loader`.

Result: the solution builds with **0 warnings and 0 errors** with no warning suppressions left for the Windows projects, tests run on **net10.0** (501/501), and both consumers — the plain library consumer and the WinForms host consumer — now build and run with **no warnings at all**.

### Consumer matrix per shipped TFM

Retargeting the test project to `net10.0-windows10.0.19041.0` (forced by the WinForms host's package assets) means the suite no longer *executes* the core's `net8.0`/`net9.0` builds — they were only compile-verified. The standalone consumer was therefore run against each shipped target framework:

| Consumer TFM | Result |
|---|---|
| `net8.0` | restore, diagram build, render + PNG export, serialization round-trip (connection preserved), workflow execution -> **Completed** |
| `net9.0` | same -> **Completed** |
| `net10.0` | same -> **Completed** |

All three packages-as-shipped targets are now **runtime-verified**, not just compiled, and each consumer runs with zero warnings.

### Benchmark and test-reliability fixes

- **Corrected the render benchmark** — the "nodes only" configuration was silently building a diagram *with* its 199 connection lines (a parameter was ignored), so both reported figures measured the same workload. With the bug fixed the real numbers are **200 nodes: ~3.6 ms/frame / 40.7 KB; plus 199 lines: ~5.3 ms/frame / 57.9 KB** (≈86 bytes and ~9 µs per line per frame). The test now asserts the two configurations differ (and that only one has lines) so this cannot recur, and the allocation half of the assertion is deterministic.
- **Validated the measurement technique** — `AllocationMeasurementSanityTests` proves `GC.GetAllocatedBytesForCurrentThread` detects a known 1 MB allocation and per-iteration allocations, so the reported figures can be trusted.
- **Eliminated a cross-class test race** — `LoadFromDto` applies the persisted theme to the process-wide `ThemeManager` while `AccessibilityTests` asserts on that same global state; parallel class execution made it flaky (observed intermittently). Test execution is now serialized assembly-wide (`DisableTestParallelization`), costing a few seconds of wall clock (8-13 s → 11-15 s) and removing the whole class of shared-state flakes. Four consecutive full runs are green.
- **Confirmed connection lines render** — `ConnectionLineRenderingTests` renders two connected nodes and asserts ink in the corridor between them (1,092 pixels only the line can produce), which also validates that the line measurements above are real work.

### Paint-reuse pass (hot path)

- **`ConnectionLine` no longer allocates per frame** — every line created a fresh `SKPaint` (managed wrapper + native Skia object) plus a dash `SKPathEffect` on each draw. Both are now cached on the instance and refreshed in place, with the dash effect rebuilt only when the pattern actually changes (zoom-scaled and preview patterns vary per frame). Result: **199 lines went from +17 KB/frame to +0.04 KB/frame** and frame time from ~5.3 ms to ~4.5 ms, with **pixel-identical output** (corridor ink unchanged at 1,092; all render smoke tests pass).
- **Remaining (documented, not done):** component draws still allocate ~210 bytes each per frame (~40.7 KB for 200 nodes) — each `DrawContent` creates its own paints/fonts. Caching those per component is a broad change across ~30 classes and should be driven by profiling at target diagram sizes.

### Automation save/load defects (found by the end-to-end scenario test)

The new `EndToEndScenarioTests` (author -> save -> reload -> execute -> export -> collaborate -> extend -> generate) immediately found **two severe defects in the flagship automation flow**:

1. **Every connection was silently dropped on save/load.** `AutomationNode` kept its ports in private `_inputConnections`/`_outputConnections` lists while the drawing manager and `ToDto`/`LoadFromDto` use `SkiaComponent.InConnectionPoints`/`OutConnectionPoints`. Serialized diagrams therefore carried **no port ids** (`inIds=0 outIds=0`) and reloading produced a diagram with zero lines — and a workflow with zero connections. Fixed by unifying the storage: the automation `InputConnections`/`OutputConnections` now delegate to the canvas collections, so the runtime, the renderer, the drawing manager and serialization all see the same ports.
2. **Node configuration was never persisted.** `DataTransformNode.FieldMappings` (and `Filters`, and any other runtime configuration) live in the node's `Configuration` dictionary, which `ToDto` did not write — so a saved transform node lost its mappings and **failed validation on execution** ("Field mappings are required for Map transform type"). `ToDto` now persists `IAutomationNode.Configuration` in the typed property bag (restored by `LoadFromDto`), and `DataTransformNode.InitializeAsync` tolerates the dictionary form produced by JSON so mappings/filters hydrate correctly on load.

Both are pinned by `AutomationSerializationTests` (ports exposed on the canvas collections, connections survive the round-trip, configuration survives, and the reloaded diagram converts to a workflow *with* its connection), plus the end-to-end scenario which now executes the reloaded diagram successfully.

### Family-wide save/load sweep (`FamilyRoundTripSweepTests`, 14 tests)

Since the automation defect was a *class* of bug (state stored where serialization cannot see it), a parameterised sweep now builds a connected pair of components for every diagram family and asserts the connection survives a save/load round-trip. It immediately found a third instance:

3. **ML family lost every connection on save/load.** `MLControl`'s base constructor registers `NodeProperties["InPortCount"]/["OutPortCount"]` while the port lists are still **empty** (derived constructors add the ports afterwards), so the stale `0` was persisted and **re-applied on load** — and the port-count setter *removes* ports to match, deleting every port and with them every connection. Fixed by keeping those NodeProperties in sync inside `EnsurePortCounts`, so the persisted value is always the real count.

The sweep now covers Flowchart, ERD, UML, DFD, StateMachine, MindMap, PM, Cloud, ML and Automation (trigger -> transform) — all round-trip their connections. Families whose identical components expose no compatible port pair (ETL destination, ECAD ground, chart, trigger-to-trigger) are skipped by design.

### Whole-surface sweep (`ComponentSurfaceSweepTests`, 2 tests)

The same "generalise the check" approach applied to the entire component surface: every public component type in every family is instantiated, rendered, and round-tripped through serialization — **293 types** — asserting two universal invariants: rendering must not throw, and a save/load round-trip must preserve the type. It found a crash:

4. **`UMLTransformNode` returned a disposed path** — `CreateHexagonPath` did `using var path = builder.Detach(); return path;`, so the caller drew a path whose native memory had already been freed. This was a latent defect introduced by the mechanical `SKPath` -> `SKPathBuilder` migration and only shows up as a **native crash inside `SkCanvas.DrawPath`** (not a managed exception), which is why per-type smoke tests had missed it. Fixed by returning `builder.Detach()` directly; an audit of all ~40 `using var x = builder.Detach()` sites confirmed this was the only one that escaped its scope.

The render half of the sweep also revealed that its own first version was weak — components cull against the context bounds and the parameterless `Draw(canvas)` supplies an empty rect, so 69 types "drew nothing". Passing a real viewport reduced that to **8**, all legitimately content-less (menus, tabs, drawer, list, SVG holder, network link). The test now asserts that at least 200 of the 293 types produce visible ink.

### Behaviour sweep (`ComponentBehaviourSweepTests`, 3 tests)

The remaining per-frame and per-edit operations were swept across all **293 component types** with no failures found — the value here is locked-in coverage rather than new defects:

- **`Update()`** (called once per frame by the render loop, *not* exercised by the render sweep) at zoom 0.25x / 1x / 4x — 0 threw.
- **Property write-back** — every type reads all of its properties and writes them back through `SetPropperties`, exactly what `LoadFromDto` and the property grid do — 0 threw.
- **Rendering at extreme zoom** 0.1x / 0.25x / 1x / 4x / 8x — 0 threw.

Together with the render/serialization sweep, the whole component surface (293 types) is now verified against five universal invariants: renders, updates, survives serialization, accepts its own property values, and holds up at extreme zoom.

### Interaction sweep (`ComponentInteractionSweepTests`, 2 tests)

The last untested axis — input — was swept across all **293 component types** with no defects found:

- **Full input pipeline**: hover, click-select, drag, middle-button pan, wheel zoom, selection-box drag, and Tab / Delete / Ctrl+A key handling. **0 threw**.
- **Click-selectability**: **269 of 293** types select cleanly (the remainder are static overlays — palette, property editor, minimap, context menu — which are not selectable by design). A component that cannot be selected cannot be edited, so the test asserts a healthy majority rather than mere absence of exceptions.
- **Clipboard + undo/redo**: select-all, copy, paste, delete, three undos, three redos and clear for every type — **0 threw**.

The surface is now covered on every axis the framework exercises per component: render, update, serialize, property write-back, extreme zoom, and input.

---

## Notes

- All five hosts (WinForms, WPF, Blazor, MAUI, Avalonia) are in the solution. The Avalonia surface renders offscreen into an Avalonia bitmap rather than through `Avalonia.Skia`, which keeps SkiaSharp at 4.150.1 across the solution; if Avalonia adopts SkiaSharp 4.x later, the surface can be swapped for a leased `ISkiaSharpApiLeaseFeature` canvas.
- Tests run on `net10.0-windows10.0.19041.0` (retargeted with the Windows hosts when SkiaSharp 4.x view assets required it); a cross-platform core test project is desirable but not part of the feature-first plan.
- `docs/archive/plan_enhancements.md` remains the catalog of original ideas; use this file for live status. Project documentation lives under `docs/`.

# Beep.Skia Enhancement Plan

**Created:** 2026-07-15 | **Status:** In Progress | **Total ideas:** 42 | **Implemented:** 6

---

## Implementation Log

| Date | Item | Status |
|------|------|--------|
| 2026-07-15 | P1-1: Clipboard Copy/Paste | Done — `DrawingManager.Components.cs:CopySelectedComponents()` + `PasteComponents()` |
| 2026-07-15 | P1-3: Image/Vector/PDF Export | Done — `ExportToPng()`, `ExportToSvg()`, `ExportToPdf()`, `RenderToBitmap()`, `GetContentBounds()` |
| 2026-07-15 | P1-6: Serialization Coverage | Partial — `PersistPropertyBag()` uses GetProperties() for all families starting with paste |
| 2026-07-15 | P2-16: Keyboard Shortcut System | Done — `HandleKeyDown()` with Ctrl+C/V/X/Z/Y/A/S/G, arrows, Delete, Escape, zoom |
| 2026-07-15 | P2-21: Undo/Redo at Runtime | Done — `PasteComponentsAction`, `AlignComponentsAction` added; all new ops support undo |
| 2026-07-15 | P3-32: Alignment & Distribution | Done — `AlignLeft/Right/Top/Bottom/CenterHorizontal/Vertical`, `DistributeHorizontal/Vertical` |
| 2026-07-15 | P3-29: Diagram Validation Framework | Done — `DiagramValidator` with 6 built-in rules + `DrawingManager.ValidateDiagram()` |
| 2026-07-15 | P2-11: StateMachine Completion | Done — `CompositeStateNode`, `ForkNode`, `JoinNode`, `HistoryNode`, `ChoiceNode` (5 nodes) |
| 2026-07-15 | P2-12: BPMN 2.0 Compliance | Partial — `IntermediateEventNode` (12 types), `GatewayType` enum (5 types), `EventPosition` enum |
| 2026-07-15 | P2-13: UML Diagram Types | Partial — `UMLUseCaseNode`, `UMLSystemBoundary`, `UMLActivityNode` (8 types), `UMLPackageNode` |
| 2026-07-15 | P3-22: ECAD Electrical Rules | Done — `ElectricalRulesChecker` with floating nets, output conflicts, missing power/ground, unconnected ports |
| 2026-07-15 | P3-27: DFD Leveling | Done — `DFDBalancingValidator` + `DFDProcess.ChildDiagramData` drill-down support |

---

## Executive Summary

Beep.Skia is a robust cross-platform SkiaSharp-based 2D diagramming framework. Through comprehensive auditing, **42 enhancement opportunities** were identified across 6 categories. The highest-impact gaps are: clipboard copy/paste (stubbed), workflow engine execution (zero implementation), image/vector export (only Network has stubs), swimlane/pool containers (affects 3 families), and auto-layout algorithms (missing for all families except Network). **Estimated effort to address all Priority 1 items: 8-12 weeks.**

---

## Audit Methodology

Three deep-dive audits were performed:
1. **Core framework** — TODO/FIXME markers, stub methods, missing implementations, architecture gaps
2. **Diagram families** — Per-family feature coverage vs professional tools, gap analysis docs, plan.md alignment
3. **Host integration** — WinForms designer experience, toolbox, PropertyGrid, printing, export, cross-platform readiness

---

## Enhancement Opportunities by Priority

### PRIORITY 1 — Critical (blocks production use, 9 items)

#### 1. Clipboard Copy/Paste — P1
- **File:** `DrawingManager.Components.cs:128-142`
- **Status:** Empty method bodies; context menu "Copy"/"Paste" items exist but are NOT wired
- **Approach:** Serialize selected components to `ClipboardDto` (JSON), paste with offset position. Wire context menu items.
- **Effort:** 2 days

#### 2. IWorkflowEngine Implementation — P1
- **File:** `Beep.Skia.Model/IWorkflowEngine.cs` (510 lines, fully defined interface)
- **Status:** Zero implementation classes exist. `ITrigger` also unimplemented. Automation nodes exist individually but no engine orchestrates multi-node workflows.
- **Approach:** Create `WorkflowEngine` class implementing `IWorkflowEngine`. Implement `ITrigger` base types. Add `WorkflowCanvas.ExecuteAsync()` integration.
- **Effort:** 3-4 weeks

#### 3. General-Purpose Image/Vector Export — P1
- **Status:** Only `Network/ExportPanel.cs` has PNG/SVG export (and it draws hardcoded shapes, bypassing the rendering pipeline)
- **Approach:**
  - `DrawingManager.ExportToPng(path, scale, background)` — render to offscreen `SKSurface`
  - `DrawingManager.ExportToSvg(path)` — use `SKSvgCanvas` to record draw commands
  - `DrawingManager.ExportToPdf(path)` — use `SKDocument.CreatePdf`
  - Auto-crop to content bounds
  - Add context menu items: "Export as PNG...", "Export as SVG..."
- **Effort:** 1 week

#### 4. Swimlane/Pool/Lane Container — P1
- **Status:** Requested in `FLOWCHART_GAP_ANALYSIS.md` but never built. Affects Flowchart, Business/BPMN, UML Activity.
- **Approach:** Create `SwimlaneContainer : MaterialControl`. Horizontal or vertical lanes. Components auto-parent to lanes. Lane headers with titles.
- **Effort:** 2 weeks

#### 5. Auto-Layout Algorithms for All Families — P1
- **Status:** Only `Network/LayoutSelector.cs` has layout algorithms. MindMap (core feature), FlowChart, DFD, ETL, StateMachine, UML have zero auto-layout.
- **Approach:**
  - `AutoLayoutEngine` base class with `Layout(List<SkiaComponent>, List<ConnectionLine>)`
  - Implementations: `HierarchicalLayout` (flowchart/DFD/ETL), `RadialLayout` (mindmap), `ForceDirectedLayout` (network), `GridLayout` (UML class), `TreeLayout` (org chart)
  - `DrawingManager.ArrangeDiagram(layoutType)`
- **Effort:** 3-4 weeks

#### 6. Serialization Coverage for Newer Families — P1
- **File:** `DrawingManager.Core.cs` — `ToDto()` / `LoadFromDto()`
- **Status:** Hardcoded per-family property extraction for MindMap, StateMachine, Flowchart, PM, ERD only. Cloud, ECAD, ML, Security, Quantitative, WellLogs are NOT covered — node-specific properties lost on save/load.
- **Approach:** Replace hardcoded per-family extraction with generic `NodeProperties` / `GetProperties()` iteration. Add `ComponentDto.PropertyBag` versioning. Validate deserialized types exist.
- **Effort:** 1 week

#### 7. WinForms PropertyGrid Integration for NodeProperties — P1
- **Files:** `SkiaHostControl.cs`, `SkiaControlDesigner.cs`
- **Status:** When a Skia component is selected at design time, the VS PropertyGrid shows `SkiaHostControl` properties, NOT the selected component's `NodeProperties`.
- **Approach:**
  - `SkiaHostControl` implements `ICustomTypeDescriptor`
  - When a Skia component is selected, expose its `NodeProperties` as `PropertyDescriptor` instances
  - Implement `SkiaComponentConverter : TypeConverter` for string round-trip
  - Sync `DrawingManager.SelectionChanged` → `ISelectionService.SetSelectedComponents()`
- **Effort:** 2 weeks

#### 8. Design-Time Canvas Preview — P1
- **File:** `SkiaHostControl.cs`, `SkiaControlDesigner.cs`
- **Status:** `SkiaHostControl` appears as a blank white rectangle in the VS designer. No design-time rendering.
- **Approach:** Override `OnPaint` or enable `SKControl` rendering at design time. Render `DesignTimeComponents` as labeled rectangles with connector previews using GDI+ fallback.
- **Effort:** 1 week

#### 9. Printing Support — P1
- **Status:** Zero printing code. Beep.Winform has `BeepPrintPreviewForm` and `PrintDocument` patterns.
- **Approach:** Create `SkiaPrintDocument : PrintDocument`. Render canvas to printer via `SKSurface.Create` from `e.Graphics`. Tile large diagrams across pages.
- **Effort:** 1 week

---

### PRIORITY 2 — High Value (major feature additions, 12 items)

#### 10. Network Analysis Algorithms — Real Implementation — P2
- **Files:** `CentralityMeasure.cs`, `CommunityDetector.cs`
- **Status:** Both contain placeholder comments: "This is a placeholder for more sophisticated..."
- **Approach:** Implement Brandes algorithm (betweenness), Louvain method (community detection), eigenvector centrality, PageRank. Replace all placeholder logic.
- **Effort:** 2 weeks

#### 11. StateMachine Completion (Composite States, Fork/Join, History) — P2
- **Status:** 15% feature complete — only InitialState, State, FinalState. Missing compound/composite states, fork/join pseudostates, choice junction, history pseudostates, entry/exit/do activities.
- **Approach:** Add `CompositeStateNode`, `ForkNode`, `JoinNode`, `ChoiceNode`, `HistoryNode` (deep/shallow). Guard condition support on transitions. Import `UML.StateMachine.StateNode` for integration.
- **Effort:** 2-3 weeks

#### 12. BPMN 2.0 Compliance (Business Family) — P2
- **Status:** ~30% BPMN compliant. Missing: Pools/Lanes, Intermediate Events (8 types), Gateway type differentiation (XOR/AND/OR/Event-Based), Message Flow, Service/User/Script tasks, Compensation.
- **Approach:**
  - Add `IntermediateEventNode` with subtypes (Timer, Message, Signal, Error, Escalation, Compensation, Conditional, Link, Terminate)
  - Add `EventBasedGatewayNode`, `ParallelGatewayNode`, `InclusiveGatewayNode`
  - Add `ServiceTaskNode`, `UserTaskNode`, `ScriptTaskNode`
  - Add `MessageFlowLine` (dashed arrow) separate from Sequence Flow
  - BPMN 2.0 XML export
- **Effort:** 3-4 weeks

#### 13. UML Diagram Types Beyond Class — P2
- **Status:** 35% feature complete. Missing: Use Case, Activity, Component, Deployment diagrams entirely. Sequence diagrams incomplete (no activation bars, combined fragments).
- **Approach:**
  - Add `UMLUseCaseNode`, `UMLSystemBoundary`, `UMLIncludeRelationship`, `UMLExtendRelationship`
  - Add `UMLActivityNode`, `UMLControlFlow`, `UMLObjectFlow`, `UMLForkJoinNode`
  - Add `UMLComponentNode`, `UMLPortNode`, `UMLDependencyArrow`
  - Sequence: Add `UMLActivationBar`, `UMLCombinedFragment` (alt, opt, loop, par)
- **Effort:** 4-6 weeks

#### 14. MindMap Auto-Layout & Features — P2
- **Status:** 25% complete. Missing auto-layout (the defining feature), relationship lines, node icons, collapse/expand, rich text.
- **Approach:**
  - `RadialMindMapLayout` — radial arrangement from CentralNode outward
  - `TreeMindMapLayout` — horizontal/vertical tree branching
  - Add `RelationshipNode` for cross-branch connections
  - Add collapse/expand icon on TopicNode for subtree folding
  - Add `Icon` property support on all mind map nodes (emoji/image)
- **Effort:** 2 weeks

#### 15. Rich Sample Application — P2
- **File:** `Beep.Skia.Sample.WinForms/`
- **Status:** Only demonstrates 3 controls (Button, Label, Menu). References 10 diagram families in .csproj but shows NONE.
- **Approach:**
  - Add tabbed demo launcher with sample diagrams for every family
  - Demonstrate: connections, labels, multiplicities, zoom/pan, themes, undo/redo
  - Add "Quick Demo" menu for each family
  - Show palette drag-drop, property editing, export, printing
- **Effort:** 2 weeks

#### 16. Keyboard Shortcut System — P2
- **Files:** `SkiaHostControl.cs`
- **Status:** Only arrow keys for property editor. No standard diagram shortcuts.
- **Approach:** `ShortcutManager` class with configurable bindings: Delete (remove selected), Ctrl+C/V (copy/paste), Ctrl+Z/Y (undo/redo), Ctrl+A (select all), Ctrl+S (save), +/- (zoom), arrow keys (nudge 1px, Shift+arrow = 10px), Ctrl+G (toggle grid)
- **Effort:** 3 days

#### 17. WinForms Toolbox Drop Position Mapping — P2
- **Files:** `SkiaControlDesigner.cs`
- **Status:** Components dropped from toolbox land at (0,0) or wrapper defaults, ignoring mouse position.
- **Approach:** Detect drop position on host control in `Initialize()`, translate to canvas coordinates accounting for current pan/zoom.
- **Effort:** 2 days

#### 18. ERD DDL Reverse Engineering — P2
- **Status:** DDL Exporter (forward) exists but no import. Listed as "Future Enhancement" in `ERD_ENTERPRISE_FEATURES.md`.
- **Approach:** SQL parser that extracts CREATE TABLE statements → `ERDEntity` with columns, PKs, FKs, indexes. Support SQL Server, PostgreSQL, MySQL dialects.
- **Effort:** 2 weeks

#### 19. ETL CDC / Fuzzy Matching — P2
- **Status:** Documented in `ETL_PROFESSIONAL_FEATURES.md` as high-priority missing features.
- **Approach:**
  - `ETLCdcNode` — Change Data Capture config (timestamp/version column, delta detection)
  - `ETLFuzzyLookupNode` — Levenshtein distance, Soundex, Metaphone, configurable threshold
  - `ETLSurrogateKeyNode` — Auto-increment key generation
- **Effort:** 2-3 weeks

#### 20. Design-Time Smart-Tag Verbs for SkiaHostControl — P2
- **Files:** New `SkiaHostControlDesigner.cs`, `SkiaHostControlActionList.cs`
- **Status:** No designer action list exists for the host control.
- **Approach:** Follow Beep.Winform `AGENTS.md` pattern: `ComponentDesigner` → `DesignerActionList` → verbs: "Import Layout from JSON...", "Clear All Components", "Reset Zoom/Pan", "Toggle Grid", "Export as PNG..."
- **Effort:** 3 days

#### 21. Undo/Redo at Runtime (via Clipboard Integration) — P2
- **Status:** `HistoryManager` and `DrawingActions` exist and work, but Copy/Paste is stubbed. Undo/Redo count is unlimited.
- **Approach:** Add undo stack depth limit (configurable, default 100). Integrate copy/paste actions into undo stack. Add Undo/Redo toolbar buttons and keyboard shortcuts.
- **Effort:** 3 days

---

### PRIORITY 3 — Desirable (completeness and polish, 12 items)

#### 22. ECAD Electrical Rules Checking — P3
- **Status:** 20% feature complete. 18 component types but no pin compatibility, floating net detection, or basic ERC.
- **Approach:** `ElectricalRulesChecker` that validates: pin type compatibility, floating nets, short circuits, power/ground connectivity, unconnected mandatory pins.
- **Effort:** 2 weeks

#### 23. Cloud Provider-Specific Icon Library — P3
- **Status:** 10% feature complete — 4 generic node types. No AWS/Azure/GCP branded shapes.
- **Approach:**
  - Add ~30 provider-specific node classes with branded SVG icons
  - AWS: EC2, S3, Lambda, RDS, DynamoDB, VPC, API Gateway, CloudFront, IAM, etc.
  - Azure: VM, Blob Storage, Functions, SQL Database, Cosmos DB, VNet, etc.
  - GCP: Compute Engine, Cloud Storage, Cloud Functions, Cloud SQL, etc.
  - Reuse `SvgImage` component for icon rendering
- **Effort:** 3-4 weeks

#### 24. WellLogs LAS/DLIS File Parsing — P3
- **Status:** 15% feature complete. Rendering architecture exists but only demo data.
- **Approach:** Implement LAS 2.0 and LAS 3.0 parsers. Bootstrap DLIS/RP66 reader. Wire parsed data into `WellLogCanvas.SetData()`.
- **Effort:** 2 weeks

#### 25. ML Pipeline Diagram Execution Hooks — P3
- **Status:** 25% feature complete. 19 node types for visual ML pipeline design but no computation backend.
- **Approach:** Add `MLExecutionEngine` interface. Implement TF/PyTorch bridge via Python process or ONNX Runtime. At minimum: export pipeline to JSON that can be consumed by external ML frameworks.
- **Effort:** 4 weeks

#### 26. Security Threat Modeling Methodology (STRIDE) — P3
- **Status:** 20% feature complete. Nodes exist but no methodology integration.
- **Approach:** Add `STRIDEAnalyzer` that auto-generates threat nodes per asset. Add `DREADCalculator` for risk scores. MITRE ATT&CK tactic/technique library integration.
- **Effort:** 2 weeks

#### 27. DFD Hierarchical Decomposition (Leveling) — P3
- **Status:** 75% feature complete. Missing drill-down from Level 0 → Level 1+.
- **Approach:** `DFDProcess.DrillDown()` opens child diagram. `DFDBalancingValidator` checks input/output consistency across levels. Navigation breadcrumb between levels.
- **Effort:** 1 week

#### 28. PM Auto-Scheduling & Resource Leveling — P3
- **Status:** 55% feature complete. Missing critical path calculation, auto-dependency scheduling, resource leveling.
- **Approach:**
  - `CriticalPathCalculator` — forward/backward pass, float calculation
  - `ResourceLeveler` — detect over-allocations, suggest leveling
  - Add working calendar support (work days, holidays)
- **Effort:** 2 weeks

#### 29. Global Diagram Validation Framework — P3
- **Status:** Only ERD has FK validation; no family has structural validation.
- **Approach:** `DiagramValidator` class with pluggable rules: cycle detection, orphan node detection, path completeness, port count validation, type compatibility. Per-family rule sets.
- **Effort:** 1 week

#### 30. Diagram Templates / Starter Diagrams — P3
- **Status:** No pre-built templates for any family.
- **Approach:** Create 2-3 starter templates per family (stored as JSON DTOs). Add "New from Template" menu in sample app. `ComponentPalette.AddTemplateSection()`.
- **Effort:** 1 week

#### 31. Theme Engine for Diagram Families — P3
- **Status:** All families use hardcoded `MaterialColors.*` tokens. No dark/light mode toggle, no per-diagram theme presets.
- **Approach:** `DiagramTheme` class with color token overrides per family. `DrawingManager.SetTheme(theme)`. Dark mode toggle. Persist theme choice in serialization.
- **Effort:** 1 week

#### 32. Alignment & Distribution Tools — P3
- **Status:** No alignment or distribution commands.
- **Approach:** DrawingManager methods: `AlignLeft()`, `AlignRight()`, `AlignTop()`, `AlignBottom()`, `CenterHorizontal()`, `CenterVertical()`, `DistributeHorizontal()`, `DistributeVertical()`. Add toolbar buttons and context menu items.
- **Effort:** 3 days

#### 33. Standard Format Interchange — P3
- **Status:** No import/export for standard formats beyond JSON.
- **Approach:**
  - BPMN 2.0 XML export (Business family)
  - XMI export (UML family)
  - GraphML export (Network family)
  - MS Project XML export (PM family)
  - OPML export (MindMap family)
- **Effort:** 2-3 weeks

---

### PRIORITY 4 — Cross-Platform & Future (6 items)

#### 34. WPF Host Project — P4
- **Approach:** Create `Beep.Skia.Wpf.Controls` project. `SkiaHostElement : FrameworkElement` using `SKElement` from `SkiaSharp.Views.WPF`. Share `SkiaComponentDescriptorCollection` serialization. Same API surface as WinForms host.
- **Effort:** 3 weeks

#### 35. MAUI Host Project — P4
- **Approach:** Create `Beep.Skia.Maui.Controls` project. Use `SKCanvasView` from `SkiaSharp.Views.Maui.Controls`. Cross-platform (Windows, macOS, iOS, Android).
- **Effort:** 4 weeks

#### 36. Blazor WebAssembly Host Project — P4
- **Approach:** Create `Beep.Skia.Blazor.Controls` project. Use `SKCanvasView` from `SkiaSharp.Views.Blazor`. JS interop for mouse/touch/keyboard events.
- **Effort:** 4 weeks

#### 37. Avalonia Host Project — P4
- **Approach:** Create `Beep.Skia.Avalonia.Controls` project. Use `SKControl` from `SkiaSharp.Views.Avalonia`.
- **Effort:** 3 weeks

#### 38. Headless Rendering for CI/Testing — P4
- **Approach:** `DrawingManager.RenderToBitmap(width, height)` using offscreen `SKSurface`. Use in unit tests to verify rendering output. Snapshot comparison tests.
- **Effort:** 3 days

#### 39. SVG Icon Sharing from Beep.Winform — P4
- **Status:** Beep.Winform has extensive `IconsManagement/` and SVG resources. Skia's `SvgImage` could share the same set.
- **Approach:** Extract SVG icon resources to a shared package (`Beep.Shared.Icons`). Reference from both Beep.Winform and Beep.Skia.
- **Effort:** 1 week

---

### PRIORITY 5 — Technical Debt & Polish (3 items)

#### 40. Remove Console.WriteLine Debug Logging — P5
- **Files:** `SkiaHostControl.cs` (~30+ instances), `DrawingManager.*.cs`, component files
- **Approach:** Replace with `System.Diagnostics.Debug.WriteLine` gated by `#if DEBUG`, or use a proper `ILogger` interface.
- **Effort:** 1 day

#### 41. Reduce Empty Catch Blocks — P5
- **Files:** `SkiaHostControl.cs` (~40+ empty catch blocks), various component files
- **Approach:** Add at minimum `Debug.WriteLine(ex)` in each catch. Review each for potential recovery logic.
- **Effort:** 1 day

#### 42. Consolidate Component Discovery — P5
- **Files:** `SkiaHostControl.cs`, `SkiaComponentRegistry.cs`
- **Status:** Discovery logic is duplicated between `SkiaHostControl.ctor` and `SkiaComponentRegistry`. Both scan for types independently.
- **Approach:** Consolidate all discovery into `SkiaComponentRegistry`. Have host subscribe to registry events. Remove duplicate scanning code.
- **Effort:** 2 days

---

## Family Feature Coverage Matrix

| Family | Current % | Target % | Biggest Gap |
|--------|-----------|----------|-------------|
| FlowChart | 70% | 90% | Swimlanes (P1), auto-layout (P1) |
| DFD | 75% | 90% | Leveling/decomposition (P3) |
| ERD | 85% | 95% | DDL reverse engineering (P2) |
| ETL | 50% | 80% | CDC, fuzzy matching, surrogate keys (P2) |
| UML | 35% | 80% | UseCase/Activity/Component/Deployment (P2) |
| PM | 55% | 80% | Auto-scheduling, resource leveling (P3) |
| MindMap | 25% | 85% | Auto-layout (P1-P2) |
| StateMachine | 15% | 85% | Composite states, fork/join, history (P2) |
| Network | 45% | 85% | Real analysis algorithms (P2) |
| Business | 30% | 80% | BPMN 2.0 pools/lanes/events (P2) |
| Cloud | 10% | 75% | Provider-specific icons (P3) |
| ECAD | 20% | 60% | Electrical rules checking (P3) |
| ML | 25% | 60% | Execution hooks / framework bridge (P3) |
| Security | 20% | 65% | STRIDE methodology (P3) |
| Quantitative | 40% | 70% | Chart rendering, options pricing |
| WellLogs | 15% | 60% | LAS/DLIS file parsing (P3) |

---

## Effort Estimation Summary

| Priority | Items | Estimated Weeks |
|----------|-------|-----------------|
| P1 — Critical | 9 | 16-21 weeks |
| P2 — High Value | 12 | 21-29 weeks |
| P3 — Desirable | 12 | 23-29 weeks |
| P4 — Cross-Platform | 6 | 15-17 weeks |
| P5 — Tech Debt | 3 | 0.5 week |
| **Total** | **42** | **75-97 weeks** |

---

## Recommended Phase Plan

### Phase 1: Core Infrastructure (Weeks 1-4)
- P1-1: Clipboard Copy/Paste
- P1-3: Image/Vector Export
- P1-6: Serialization Coverage
- P5-40/41/42: Debug logging, catch blocks, component discovery

### Phase 2: Designer Experience (Weeks 5-8)
- P1-7: PropertyGrid Integration
- P1-8: Design-Time Preview
- P1-9: Printing Support
- P2-17: Toolbox Drop Position
- P2-20: Smart-Tag Verbs
- P2-21: Undo/Redo at Runtime

### Phase 3: Diagram Engine (Weeks 9-14)
- P1-2: IWorkflowEngine Implementation
- P1-4: Swimlane Container
- P1-5: Auto-Layout Algorithms
- P2-16: Keyboard Shortcuts

### Phase 4: Family Enhancements — Tier 1 (Weeks 15-22)
- P2-11: StateMachine Completion
- P2-12: BPMN 2.0 Compliance
- P2-14: MindMap Auto-Layout
- P2-15: Rich Sample Application

### Phase 5: Family Enhancements — Tier 2 (Weeks 23-30)
- P2-10: Network Analysis Algorithms
- P2-13: UML Diagram Types
- P2-18: ERD DDL Reverse Engineering
- P2-19: ETL CDC / Fuzzy Matching

### Phase 6: Family Enhancements — Tier 3 (Weeks 31-40)
- P3-22: ECAD Rules Checking
- P3-23: Cloud Provider Icons
- P3-24: WellLogs File Parsing
- P3-25: ML Pipeline Hooks
- P3-26: Security STRIDE
- P3-27 through P3-33: Remaining P3 items

### Phase 7: Cross-Platform & Polish (Weeks 41-50)
- P4-34 through P4-39: Platform host projects
- Remaining P3 items

---

## Quick Wins (Under 1 Week Each)

These can be executed any time for immediate value:

1. **Clipboard Copy/Paste** — 2 days, 2 stubbed methods to fill (P1-1)
2. **Keyboard Shortcut System** — 3 days, new class + wiring (P2-16)
3. **Alignment/Distribution Tools** — 3 days, 8 new methods (P3-32)
4. **Undo Stack Depth Limit** — 3 days, config parameter + clipping (P2-21)
5. **Smart-Tag Verbs** — 3 days, follow Beep.Winform pattern (P2-20)
6. **Diagram Validation Framework** — 1 week, pluggable rules (P3-29)
7. **Headless Rendering** — 3 days, offscreen SKSurface (P4-38)
8. **Tech Debt Cleanup** — 1 day logging + 1 day catch blocks (P5-40/41)

Cumulative quick wins: add significant production-readiness in 3-4 weeks.

---

## Appendix A: Known Defects Not in This Plan

- Duplicate `ConnectionEventArgs` in `Args.cs` and `ISkiaWorkFlowComponents.cs` (noted in `progress.md`)
- `ISkiaWorkFlowComponents` interface referenced but missing from Model
- `NavigationItem` absolute coordinate conversion still PENDING (`progress.md`)
- Unticked checklist items in root `plan.md` (~100+ per-node items)

## Appendix B: Existing Analysis Documents

These root-level documents should be reviewed when planning specific family work:

| Document | Lines | Content |
|----------|-------|---------|
| `ETL_GAP_ANALYSIS.md` | 377 | 25 missing professional ETL features |
| `FLOWCHART_GAP_ANALYSIS.md` | 529 | Flowchart gaps (partially outdated) |
| `ERD_ENTERPRISE_FEATURES.md` | 569 | ERD enterprise feature catalog |
| `ETL_PROFESSIONAL_FEATURES.md` | N/A | ETL professional feature catalog |
| `AnimatedFlowLines.md` | N/A | Animated flow lines reference |
| `plan.md` (root) | 790 | Master optimization plan with per-component checklists |
| `progress.md` | 175 | Phase 1-5 completion status |

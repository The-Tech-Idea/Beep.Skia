# Beep.Skia Documentation Status

**Last updated:** 2026-09-17 (round 9)
**Total pages:** 107 | **Sections:** 14

---

## Coverage by Section

| Section | Pages | Status |
|---------|-------|--------|
| Getting Started | 4 | Complete |
| Core Concepts | 8 | Complete |
| Diagram Families | 16 | Complete |
| UI Components | 17 | Complete |
| Guides | 11 | Complete |
| Architecture & Internals | 8 | Complete |
| Automation | 7 | Complete |
| Ecosystem | 4 | Complete |
| Editor | 1 | Complete |
| Windows Hosts | 1 | Complete |
| API Reference | 25 | Complete |
| Infrastructure | 2 | Complete |
| **Total** | **63** | |

---

## Page Inventory

### Getting Started (3)
- [x] `getting-started/installation.html` — private feed prerequisite, real package/TFM tables, project setup, verification
- [x] `getting-started/quick-start.html` — Complete tutorial with DrawingManager and SkiaHostControl
- [x] `getting-started/architecture-overview.html` — Solution structure, layers, patterns
- [x] `getting-started/samples.html` — WinForms editor sample and workflow server sample

### Core Concepts (6)
- [x] `core-concepts/skia-component.html` — SkiaComponent base class, NodeProperties, ports
- [x] `core-concepts/drawing-manager.html` — DrawingManager coordinator (5 partials)
- [x] `core-concepts/coordinate-system.html` — Absolute coords, pan/zoom, grid
- [x] `core-concepts/connection-system.html` — ConnectionLine, routing, animation
- [x] `core-concepts/serialization.html` — JSON persistence, DTOs, load/save
- [x] `core-concepts/interaction.html` — Selection, drag-drop, undo/redo
- [x] `core-concepts/connection-animation.html` — 7 flow animation styles, status indicators, multiplicity markers
- [x] `core-concepts/validation.html` — DiagramValidator, six built-in rules, custom rules

### Diagram Families (16)

**Business & Process**
- [x] `diagram-families/flowchart.html` — 36 node types, ISO 5807 shapes, code generation and simulation
- [x] `diagram-families/business-process.html` — 23 node types, BPMN import/export
- [x] `diagram-families/project-management.html` — 12 node types, Gantt/CriticalPath
- [x] `diagram-families/mindmap.html` — 4 node types, radial layout, collapse/expand
- [x] `diagram-families/state-machine.html` — 3 node types, transitions with triggers, guards and actions

**Data & Systems**
- [x] `diagram-families/erd.html` — 4 node types, DDL export
- [x] `diagram-families/erd-advanced.html` — DDL import/export, schema comparison, migration scripts
- [x] `diagram-families/dfd.html` — 18 node types, Gane-Sarson & Yourdon
- [x] `diagram-families/etl.html` — 18 node types, data pipeline
- [x] `diagram-families/etl-advanced.html` — Lookups, SCD/CDC, expression engine, profiling, metrics
- [x] `diagram-families/uml.html` — 13 node types, Class/Sequence/Activity
- [x] `diagram-families/network.html` — 12 components, graph algorithms

**Engineering**
- [x] `diagram-families/ecad.html` — 17 node types, IEEE/ANSI symbols, electrical rules check
- [x] `diagram-families/cloud.html` — 4 node types, AWS/Azure/GCP
- [x] `diagram-families/security.html` — 9 node types, STRIDE analysis, DREAD scoring, MITRE ATT&amp;CK mapping

**Analytics**
- [x] `diagram-families/ml.html` — 18 node types, ML pipeline
- [x] `diagram-families/quantitative.html` — 6 node types, trading/finance
- [x] `diagram-families/welllogs.html` — 6 components, LAS/DLIS

### UI Components (17)
- [x] `ui-components/button.html`
- [x] `ui-components/buttons.html` — ButtonGroup, FAB/FabMenu, SplitButton, MenuButton
- [x] `ui-components/card.html`
- [x] `ui-components/menu.html`
- [x] `ui-components/navigation.html` — MenuBar, CascadingMenu, ContextMenu, NavigationBar/Drawer/Item, StatusBar, Panel, Swimlane
- [x] `ui-components/tabs.html`
- [x] `ui-components/datagrid.html`
- [x] `ui-components/lists.html` — List, ListBoxItem, ListItem, ComboBoxItem
- [x] `ui-components/textbox.html`
- [x] `ui-components/inputs.html` — ColorPicker, DatePicker, TimePicker, Search, Switch, Spinner, ProgressBar, TextArea, CheckBoxGroup, RadioGroup, SegmentedButtons
- [x] `ui-components/dropdown.html`
- [x] `ui-components/checkbox.html`
- [x] `ui-components/slider.html`
- [x] `ui-components/display.html` — Label, SvgImage, MinimapControl
- [x] `ui-components/notifications.html`
- [x] `ui-components/editors.html` — editor usage, save/cancel events, SkiaComponentGrid
- [x] `ui-components/palette.html`

### Guides (8)
- [x] `guides/creating-custom-family.html`
- [x] `guides/best-practices.html`
- [x] `guides/performance.html`
- [x] `guides/theming.html`
- [x] `guides/extensibility.html`
- [x] `guides/platforms.html` — Supported TFMs, Windows hosts, sample, server host
- [x] `guides/testing-and-quality.html` — Test strategy, sweeps, hardening record
- [x] `guides/release-notes.html` — Versioning, packaging, release history
- [x] `guides/troubleshooting.html` — Restore/build, rendering, serialization, automation, extension and testing fixes
- [x] `guides/keyboard-shortcuts.html` — every keyboard and mouse shortcut with the API behind it
- [x] `guides/getting-help.html` — in-app help, documentation map, code-level diagnostics and reporting paths

### Architecture & Internals (8)
- [x] `architecture/component-registry.html` — registry model, registration paths, queries, instantiation, consumers
- [x] `architecture/rendering-pipeline.html` — frame passes, world/screen split, culling, export path, measured cost
- [x] `architecture/interaction-system.html` — input flow, drag states, selection model, keyboard/clipboard, undo integration
- [x] `architecture/serialization-system.html` — DTO fields, property routing, identity, load algorithm, invariants
- [x] `architecture/material-design-system.html` — tokens, ThemeManager, state layers, MaterialControl surface, MaterialDesignColors
- [x] `architecture/layout-engines.html`
- [x] `architecture/history-undo-system.html` (+ action catalog)
- [x] `architecture/event-catalog.html` — 16 DrawingManager events and interaction args
- [x] `architecture/events.html` — event raising semantics, argument selection guide, legacy event args

### Automation (6)
- [x] `automation/workflow-engine.html` — Execution engine, retries, variables, pause/resume/cancel
- [x] `automation/nodes.html` — AutomationNode base, trigger/data/logic node types
- [x] `automation/triggers.html` — TriggerBase contract, TriggerType values, lifecycle, activation telemetry
- [x] `automation/credentials.html` — vault API, CredentialType values, encrypted export/import, connection manager
- [x] `automation/server-sku.html` — WorkflowExecutionService, API, HTTP router, server sample
- [x] `automation/data-sources.html` — IAutomationDataSourceProvider contract, registry, node properties, BeepDM wiring
- [x] `automation/jobs.html` — ExecutionJob, JobState, results and engine execution records

### Ecosystem (4)
- [x] `ecosystem/extensions.html` — contract, lifecycle, packaging, checklist
- [x] `ecosystem/marketplace.html` — .beepkg packages, install/update/uninstall, safety gates
- [x] `ecosystem/collaboration.html` — Roles, comments and pins, presence, audit
- [x] `ecosystem/assisted-generation.html` — IDiagramAssistant, flowchart DSL, mind-map outlines

### Editor (1)
- [x] `editor/editor-ux.html` — Palette search, minimap, property grid write-back, templates, accessibility

### Windows Hosts (1)
- [x] `hosts/winforms.html` — SkiaHostControl, SkiaControl base, 30+ WinForms wrapper controls, designers

### API Reference (25)
- [x] `reference/api-index.html` — Index of all 890 public classes/interfaces/enums/structs grouped by project
- [x] `reference/beep-skia.html` — Core assembly (338 types, full member tables)
- [x] `reference/beep-skia-model.html` — Shared model types (67)
- [x] `reference/beep-skia-flowchart.html`, `beep-skia-business.html`, `beep-skia-erd.html`, `beep-skia-dfd.html`, `beep-skia-etl.html`, `beep-skia-uml.html`, `beep-skia-network.html`, `beep-skia-pm.html`, `beep-skia-mindmap.html`, `beep-skia-statemachine.html`, `beep-skia-ecad.html`, `beep-skia-cloud.html`, `beep-skia-security.html`, `beep-skia-ml.html`, `beep-ski-quantitative.html`, `beep-skia-welllogs.html` — Family assemblies
- [x] `reference/beep-skia-winform-controls.html`, `beep-skia-wpf-controls.html`, `beep-skia-blazor-controls.html`, `beep-skia-maui-controls.html`, `beep-skia-avalonia-controls.html` — Platform hosts
- [x] `reference/beep-skia-loader.html` — Loader extension

### Infrastructure (2)
- [x] `index.html` — Full sidebar navigation with iframe content
- [x] `home.html` — Landing page with stats, features, quick start

### Repository documentation
- [x] `README.md` — project overview, layout, build/CI notes and documentation index
- [x] `CONTRIBUTING.md` — prerequisites (private feed), build/test commands, conventions, adding a family, docs generators
- [x] `CHANGELOG.md` — Keep a Changelog format with the full feature and hardening record
- [x] `SECURITY.md` — supported versions, private reporting, security-relevant surfaces and user guidance
- [x] `CODE_OF_CONDUCT.md` — Contributor Covenant 2.1
- [x] `docs/README.md` — index of the working documents and tooling
- [x] Per-project `README.md` in all 24 library projects (purpose, TFMs, package id, start-with types, links)

### Tooling (not counted as pages)
- [x] `_gen_skia_docs.py` — Python generator for diagram family + architecture pages
- [x] `_gen_automation_docs.ps1`, `_gen_ecosystem_docs.ps1`, `_gen_editor_guides_docs.ps1`, `_gen_release_notes.ps1` — PowerShell generators for the new sections
- [x] `_gen_component_reference.ps1` — Extracts XML summaries and public members from source and generates the component, host, events, automation-node and API-index pages
- [x] `_check_links.ps1` — Verifies every local reference across the site (currently 437 references, 0 broken)
- [x] `_gen_api_reference.ps1` — Generates the 24 per-project API reference pages from source
- [x] `_gen_search_index.ps1` — Builds `search-index.js` (98 pages) for the site-wide search box
- [x] `_gen_lib.ps1` — Shared extraction helpers (XML summaries, `[Description]` fallback, public members)
- [x] `docs/_gen_project_readmes.ps1` — Regenerates the per-project READMEs from csproj metadata

### Images (rendered from the framework)
- [x] `assets/families/*.png` — one component grid per diagram family (16) plus `ui-controls.png`, generated by `Beep.Skia.Tests/DocumentationImageGenerator.cs` and embedded in the family pages and the landing page

### Assets
- [x] `sphinx-style.css` — Sphinx/Furo-inspired theme with dark mode
- [x] `assets/beep-logo.svg` — Beep logo for sidebar

---

## API Accuracy Pass (2026-09-17)

All code samples across the site were verified against the framework source and corrected where they
described APIs that do not exist. Highlights of the corrections:

- **DrawingManager** — `AddComponent`/`GetComponents`/`GetLines`/`ConnectComponents` (void) and
  `ToDto`/`LoadFromDto`; grid properties are `ShowGrid`, `GridSpacing`, `SnapToGrid`.
- **ConnectionLine** — `LineColor`, `Paint.StrokeWidth`, `ShowStartArrow`/`ShowEndArrow`,
  `Label1`–`Label3`, `RoutingMode` (`LineRoutingMode`), `ERDMultiplicity` markers.
- **Components** — `X`/`Y`/`Width`/`Height`, `Name`, `NodeProperties`, `InConnectionPoints`/
  `OutConnectionPoints`; flowchart nodes expose `Label`, `CustomFillColor`, `CustomStrokeColor`
  and override `DrawContent`/`DrawFlowchartContent`.
- **ParameterInfo** — `ParameterName`, `ParameterCurrentValue`, `DefaultParameterValue`,
  `ParameterType`, `Description`, `Choices`; `GetProperties()` returns plain values.
- **Theming** — `SkiaTheme` + `ThemeManager.Current`/`ApplyTheme`/`ThemeChanged`; built-in themes
  Light, Dark, HighContrast, Nord, Dracula; theme persisted as `DiagramDto.ThemeName`.
- **Selection/clipboard** — `SelectionManager` (`SelectComponent`, `AddToSelection`,
  `SelectedComponents`, `SelectComponentsInRect`), `CopySelectedComponents`, `PasteComponents`,
  `DeleteSelectedComponents`.
- **Extensions** — `ISkiaExtension`/`ISkiaExtensionContext`, `SkiaExtensionHost`,
  `ExtensionPackageManager`; the `[AddinAttribute]` story was removed.
- **History** — `HistoryManager` + `DrawingAction` (the `CommandHistory` name was removed).
- **Ports** — lazy layout via `MarkPortsDirty()` + `LayoutPorts()`.

---

## Remaining Opportunities


- [ ] Video tutorials / walkthroughs
- [ ] Interactive playground with live SkiaSharp canvas
- [ ] VS Code extension with IntelliSense for diagram families
- [ ] Search index ranking/snippets (a plain-text index with scoring now ships as `search-index.js`)
- [x] Screenshots for each diagram family (generated component grids)

- [ ] Migration guide from other diagramming libraries

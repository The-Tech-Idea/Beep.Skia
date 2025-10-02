# Beep.Skia Optimization Plan

## Overview
This document outlines the plan to fix and optimize the Beep.Skia and Beep.Skia.Model projects based on analysis of the codebase.

Family sweep progress (NodeProperties + Get/SetProperties + lazy ports) – rollup

Status legend: DONE = migrated and validated, MOSTLY = main nodes migrated, verify stragglers, NEXT = queued work

- Core base (SkiaComponent/MaterialControl): DONE – NodeProperties, Get/SetProperties, ChildNodes, MarkPortsDirty/EnsurePortLayout
- Flowchart: DONE – base + nodes migrated and synced setters
- DFD: DONE – base + nodes migrated; lazy port layout in family templates
- ETL: DONE – base + nodes migrated; lazy ports
- PM: DONE – base + nodes migrated; metadata synced
- MindMap: DONE – base + Central/Topic/SubTopic/Note nodes migrated
- StateMachine: DONE – base + Initial/State/Final migrated
- ERD: DONE – base + entity/rows adopted nodes and template draw used
- UML: MOSTLY – base and major nodes migrated; verify any remaining minor ones
- Network: DONE – NetworkNode, NetworkLink, NetworkGraph migrated (details below)
- Quantitative: DONE – QuantControl, IndicatorNode, TimeSeriesNode migrated (details below)

---

## Controls projects sweep matrix (project-level checklist)

Legend: DONE = migrated and validated, MOSTLY = main nodes migrated, verify stragglers, NEXT = queued work, N/A = not a controls project.

- Beep.Skia (core):
	- Contains: Core framework (SkiaComponent, MaterialControl, DrawingManager)
	- NodeProperties + Get/Set: DONE (base infrastructure complete)
	- Lazy port layout: DONE (MarkPortsDirty/EnsurePortLayout; family bases use it)
	- Absolute coordinates: PARTIAL – tracked per-control in "Location Normalization Plan" below
	- Next: Continue absolute normalization per component table

- Beep.Skia.FlowChart: DONE – family migrated (NodeProperties + lazy ports). Absolute coords: verify.
- Beep.Skia.DFD: DONE – family migrated. Absolute coords: verify.
- Beep.Skia.ETL: DONE – family migrated. Absolute coords: verify.
- Beep.Skia.PM: DONE – family migrated. Absolute coords: verify.
- Beep.Skia.MindMap: DONE – family migrated. Absolute coords: verify.
- Beep.Skia.StateMachine: DONE – family migrated. Absolute coords: verify.
- Beep.Skia.ERD: DONE – family migrated; template draw in use. Absolute coords: verify.
- Beep.Skia.UML: MOSTLY – base + major nodes migrated; sweep minor nodes. Absolute coords: verify.
- Beep.Skia.Network: DONE – Node, Link, Graph migrated; Link now MaterialControl. Absolute coords: verify.
- Beep.Ski.Quantitative: DONE – QuantControl + nodes migrated. Absolute coords: verify.

- Beep.Skia.Business: DONE – family migrated; NodeProperties + lazy ports across nodes (see inventory below).
- Beep.Skia.Cloud: NEXT – scaffold started (base + 2 sample nodes). Continue adding nodes per checklist below.
- Beep.Skia.ECAD: NEXT – scaffold started (base + 2 sample nodes). Continue adding nodes per checklist below.
- Beep.Skia.ML: NEXT – scaffold started (base + 2 sample nodes). Continue adding nodes per checklist below.
- Beep.Skia.Security: MOSTLY – base + key nodes scaffolded (Asset/Threat/Control/Policy/RiskAssessment/MitigationLink).

- Beep.Skia.Model: N/A – interfaces and contracts only (ensure ParameterInfo docs up to date).
- Beep.Skia.Loader: N/A – infrastructure; no drawable controls.
- Beep.Skia.Winform.Controls: N/A – host control; verify integration only.
- Beep.Skia.Winform.Controls: N/A – host control; palette categorization updated to include Cloud, ECAD, ML, Security, Quantitative.
- Beep.Skia.Sample.WinForms: N/A – sample app; no migration needed.
- Beep.Skia.Tests: N/A – tests updated earlier; no runtime components.

Notes:
- "Absolute coords: verify" indicates components likely already draw using X/Y, but should be quickly checked during QA against the normalization checklist.
- For projects marked NEXT, follow the adoption checklist: seed NodeProperties in constructors, sync in setters with InvalidateVisual and MarkPortsDirty for geometry, and ensure family DrawContent wraps EnsurePortLayout and delegates visuals to a template method.

Global sign-off checklist (applies to every family):
- [ ] NodeProperties seeded for all node-specific fields; GetProperties/SetProperties round-trip works
- [ ] Public setters sync NodeProperties and call InvalidateVisual; geometry-affecting setters call MarkPortsDirty
- [ ] Family DrawContent calls EnsurePortLayout(() => LayoutPorts()) and delegates visuals to DrawXxxContent
- [ ] No direct LayoutPorts() calls in DrawXxxContent; no legacy UpdateConnectionPointPositions remaining
- [ ] Absolute-coordinate drawing (no canvas.Translate in nodes); hit-tests use absolute rects or explicit local conversion
- [ ] Editor UX: enums/strings with constrained values expose ParameterInfo.Choices; bools show as checkboxes; SKColor and TimeSpan parse supported
- [ ] Containers manage Children order and ChildNodes named access consistently; AddChild/RemoveChild keep both in sync
- [ ] Ports stay glued on resize/move; dynamic port counts re-layout lazily; no redundant layout between frames

Targeted inventories to validate quickly (spot-check + tick):
- UML nodes: [ ] UMLClass [ ] UMLInterface [ ] UMLActor [ ] UMLLifeline [ ] UMLDataSourceNode [ ] UMLTransformNode [ ] UMLOutputNode
- Flowchart nodes: [ ] Process [ ] Decision [ ] Start/End [ ] Document [ ] InputOutput [ ] Data [ ] PredefinedProcess

Flowchart family adoption (NodeProperties + Get/SetProperties) – progress update:

- Base (FlowchartControl): added NodeProperties for InPortCount and OutPortCount; setters keep metadata in sync.
- ProcessNode: added NodeProperties Label, ShowTopBottomPorts; setters sync values.
- DecisionNode: added NodeProperties Label, ShowTopBottomPorts; setters sync.
- StartEndNode: added NodeProperties Label, ShowTopBottomPorts; setters sync.
- DocumentNode: added NodeProperties Label, OutPortsOnTop; setters sync.
- InputOutputNode: added NodeProperties Label; setter syncs.
- DataNode: added NodeProperties Label; setter syncs.

Next:
- Verify property editor shows new fields and that SetProperties applies counts/toggles correctly.
- Consider exposing common Material properties (e.g., PrimaryColor) via base GetProperties if not already included.

DFD family adoption (NodeProperties + Get/SetProperties) – progress update:

- Base (DFDControl): added NodeProperties for InPortCount/OutPortCount and common style tokens; setters sync and MarkPortsDirty for geometry.
- ProcessNode: NodeProperties Label, ShowAllSidesPorts; setters sync; DrawDFDContent template used; lazy port layout via EnsurePortLayout.
- DataStoreNode: NodeProperties Label; setters sync; ports aligned to store edges; lazy layout.
- ExternalEntityNode: NodeProperties Label; setters sync; lazy layout.

Next for DFD:
- Confirm editor round-trip for port counts and label; verify no lingering UpdateConnectionPointPositions usage.

Per-node checklist (DFD):
- [ ] Base (DFDControl)
	- [ ] Seed NodeProperties: InPortCount, OutPortCount, BackgroundColor, BorderColor, BorderThickness, TextColor
	- [ ] Setters sync NodeProperties and call InvalidateVisual()
	- [ ] Geometry setters call MarkPortsDirty(); DrawDFDContent wraps EnsurePortLayout(() => LayoutPorts())
- [ ] ProcessNode
	- [ ] NodeProperties: Label, ShowAllSidesPorts
	- [ ] Setters sync + InvalidateVisual(); ShowAllSidesPorts marks ports dirty
	- [ ] DrawDFDContent uses absolute coordinates; no canvas.Translate
- [ ] DataStoreNode
	- [ ] NodeProperties: Label
	- [ ] Setters sync + InvalidateVisual(); geometry props mark ports dirty
	- [ ] Ports align to store edges; DrawDFDContent uses EnsurePortLayout
- [ ] ExternalEntityNode
	- [ ] NodeProperties: Label
	- [ ] Setters sync + InvalidateVisual(); geometry props mark ports dirty
	- [ ] Absolute coordinate drawing; lazy port layout ensured

ETL family adoption (NodeProperties + Get/SetProperties) – progress update:

- Base (ETLControl): seeded NodeProperties for styles and In/Out port counts; EnsurePortCounts marks ports dirty (no eager layout).
- SourceNode: NodeProperties Label, Connection/Path fields (metadata only); setters sync and invalidate; lazy port layout.
- TransformNode: NodeProperties Label and transform-specific flags; setters sync; lazy layout.
- TargetNode: NodeProperties Label, Connection/Path; setters sync; lazy layout.

Next for ETL:
- Extend ParameterInfo.Choices for common transform types where applicable; verify geometry-affecting props mark ports dirty.

Per-node checklist (ETL):
- [ ] Base (ETLControl)
	- [ ] Seed NodeProperties: InPortCount, OutPortCount, Fill, Stroke, StrokeWidth, TextColor
	- [ ] Setters sync NodeProperties + InvalidateVisual(); EnsurePortCounts marks ports dirty
	- [ ] DrawETLContent wraps EnsurePortLayout(() => LayoutPorts())
- [ ] SourceNode
	- [ ] NodeProperties: Label, ConnectionString/Path
	- [ ] Setters sync + InvalidateVisual(); geometry props mark ports dirty
	- [ ] Absolute coordinate drawing; lazy port layout ensured
- [ ] TransformNode
	- [ ] NodeProperties: Label, TransformType (Choices), Parameters (metadata fields)
	- [ ] Setters sync + InvalidateVisual(); TransformType choices wired in ParameterInfo
	- [ ] Lazy port layout via EnsurePortLayout
- [ ] TargetNode
	- [ ] NodeProperties: Label, ConnectionString/Path
	- [ ] Setters sync + InvalidateVisual(); geometry props mark ports dirty
	- [ ] Absolute coordinate drawing; lazy port layout ensured

PM family adoption (NodeProperties + Get/SetProperties) – progress update:

- Base (PMControl): seeded NodeProperties for style tokens and In/Out port counts; setters sync + MarkPortsDirty for geometry changes.
- TaskNode: NodeProperties Title, Progress, Assignee (metadata); setters sync and invalidate; lazy port layout in DrawPMContent.
- MilestoneNode: NodeProperties Title/Date; setters sync; lazy layout.
- PhaseNode/SummaryNode: NodeProperties Title; setters sync; lazy layout.
- DependencyLink (if present): aligned with MaterialControl for invalidation; NodeProperties Color/Thickness/Arrow; setters sync and invalidate.

Next for PM:
- Validate editor shows Task/Milestone fields; ensure dependency link rendering respects NodeProperties.

Per-node checklist (PM):
- [ ] Base (PMControl)
	- [ ] Seed NodeProperties: InPortCount, OutPortCount, BackgroundColor, BorderColor, BorderThickness, TextColor
	- [ ] Setters sync + InvalidateVisual(); geometry setters MarkPortsDirty
	- [ ] DrawPMContent wraps EnsurePortLayout(() => LayoutPorts())
- [ ] TaskNode
	- [ ] NodeProperties: Title, Progress, Assignee
	- [ ] Setters sync + InvalidateVisual(); Progress clamps 0..100
	- [ ] Absolute coordinate drawing; lazy port layout ensured
- [ ] MilestoneNode
	- [ ] NodeProperties: Title, Date
	- [ ] Setters sync + InvalidateVisual(); geometry props MarkPortsDirty
	- [ ] Lazy port layout via EnsurePortLayout
- [ ] PhaseNode
	- [ ] NodeProperties: Title
	- [ ] Setters sync + InvalidateVisual(); geometry props MarkPortsDirty
	- [ ] Absolute coordinate drawing; lazy port layout ensured
- [ ] SummaryNode
	- [ ] NodeProperties: Title
	- [ ] Setters sync + InvalidateVisual(); geometry props MarkPortsDirty
	- [ ] Lazy port layout via EnsurePortLayout
- [ ] DependencyLink (if separate component)
	- [ ] Base = MaterialControl; NodeProperties: Color, Thickness, Arrow (bool)
	- [ ] Setters sync + InvalidateVisual(); Draw uses absolute coordinates and optional arrowhead

ERD family adoption (NodeProperties + Get/SetProperties) – progress update:

- Base (ERDControl): inherits MaterialControl; seeded NodeProperties for HeaderBackground, RowBackground, BorderColor/Thickness, TextColor; setters sync and invalidate.
- ERDEntity: NodeProperties Name, HeaderHeight, RowHeight; setters sync; DrawERDContent template used; ports laid out per row boundaries.
- ERDRow/Attribute rows: NodeProperties Name/Type/IsKey; setters sync; EnsurePortLayout keeps side anchors aligned to row rects.

Next for ERD:
- Verify row add/remove updates NodeProperties and marks ports dirty; ensure editor reflects row-level properties.

Per-node checklist (ERD):
- [ ] Base (ERDControl)
	- [ ] Seed NodeProperties: HeaderBackground, RowBackground, BorderColor, BorderThickness, TextColor
	- [ ] Setters sync + InvalidateVisual(); DrawERDContent wraps EnsurePortLayout
- [ ] ERDEntity
	- [ ] NodeProperties: Name, HeaderHeight, RowHeight
	- [ ] Setters sync + InvalidateVisual(); Header/Row size MarkPortsDirty
	- [ ] Ports per row boundaries; absolute coordinate drawing
- [ ] ERDRow/Attribute
	- [ ] NodeProperties: Name, Type, IsKey (bool; checkbox in editor)
	- [ ] Setters sync + InvalidateVisual(); choices for Type if constrained
	- [ ] EnsurePortLayout keeps side anchors aligned to the row rect
- [ ] RelationshipLink (if present)
	- [ ] Base = MaterialControl; NodeProperties: CardinalityLeft/Right (Choices), LineStyle (Choices)
	- [ ] Setters sync + InvalidateVisual(); Draw respects styles

StateMachine family adoption (NodeProperties + Get/SetProperties) – progress update:

- Base (StateMachineControl): seeded NodeProperties for styles and port counts; lazy port layout in DrawStateMachineContent.
- InitialNode: NodeProperties Label (optional); setters sync; lazy layout.
- StateNode: NodeProperties Name/IsComposite flags; setters sync and invalidate; lazy layout.
- FinalNode: NodeProperties Label (optional); setters sync; lazy layout.

Next for StateMachine:
- Confirm transitions use updated property pipeline if represented as components; verify editor displays state-specific fields.

Per-node checklist (StateMachine):
- [ ] Base (StateMachineControl)
	- [ ] Seed NodeProperties: InPortCount, OutPortCount, BackgroundColor, BorderColor, BorderThickness, TextColor
	- [ ] Setters sync + InvalidateVisual(); DrawStateMachineContent wraps EnsurePortLayout
- [ ] InitialNode
	- [ ] NodeProperties: Label (optional)
	- [ ] Setters sync + InvalidateVisual(); geometry props MarkPortsDirty
	- [ ] Absolute coordinate drawing; lazy port layout ensured
- [ ] StateNode
	- [ ] NodeProperties: Name, IsComposite (bool)
	- [ ] Setters sync + InvalidateVisual(); geometry-affecting setters MarkPortsDirty
	- [ ] Lazy port layout via EnsurePortLayout
- [ ] FinalNode
	- [ ] NodeProperties: Label (optional)
	- [ ] Setters sync + InvalidateVisual(); geometry props MarkPortsDirty
	- [ ] Absolute coordinate drawing; lazy port layout ensured
- [ ] TransitionLink (if separate component)
	- [ ] Base = MaterialControl; NodeProperties: Color, Thickness, HasArrow (bool), Label
	- [ ] Setters sync + InvalidateVisual(); Draw uses absolute coordinates with arrowhead

Business family adoption (NodeProperties + Get/SetProperties) – progress update:

- Base (BusinessControl): inherits MaterialControl; seeded NodeProperties for ComponentType (Choices), BusinessRole, BusinessPriority (Choices), Status (Choices), BackgroundColor, BorderColor, BorderThickness, TextColor, InPortCount, OutPortCount; setters sync and invalidate; EnsurePortLayout used for lazy ports; In/Out counts exposed as properties and kept in sync when EnsurePortCounts runs.
- TaskNode: NodeProperties TaskDescription, TaskStatus (Choices), AssignedTo, DueDate, Progress (0..100); setters sync and invalidate; absolute coordinate drawing; lazy ports ensured.
- StartEvent/EndEvent: Label property added and synced to Name; NodeProperties seeded; ports placed on ellipse perimeter; absolute drawing.
- Gateway/Decision: Label property added and synced to Name; NodeProperties seeded; diamond drawing uses absolute coordinates; lazy ports.

Next for Business:
- Sweep remaining nodes (DataStore, Database, Document, ExternalData, Person, Department, Role, System, Group, SubProcess, ActionNode, EventNode, Annotation, BusinessTask, RuleFlow, RuleEngine) to add appropriate NodeProperties (labels/titles/types/flags), sync setters, and ensure geometry-affecting properties MarkPortsDirty.

Per-node checklist (Business):
- [ ] Base (BusinessControl)
	- [ ] Seed NodeProperties: ComponentType (Choices), BusinessRole, BusinessPriority (Choices), Status (Choices), BackgroundColor, BorderColor, BorderThickness, TextColor, InPortCount, OutPortCount
	- [ ] Setters sync + InvalidateVisual(); geometry setters MarkPortsDirty
	- [ ] DrawBusinessContent wraps EnsurePortLayout(() => LayoutPorts())
- [ ] TaskNode
	- [ ] NodeProperties: TaskDescription, TaskStatus (Choices), AssignedTo, DueDate, Progress (0..100)
	- [ ] Setters sync + InvalidateVisual(); Progress clamps 0..100
	- [ ] Absolute coordinate drawing; lazy port layout ensured
- [ ] StartEvent
	- [ ] NodeProperties: Label (optional)
	- [ ] Setters sync + InvalidateVisual(); EnsurePortCounts(0,1); LayoutPortsOnEllipse; absolute drawing
- [ ] EndEvent
	- [ ] NodeProperties: Label (optional)
	- [ ] Setters sync + InvalidateVisual(); EnsurePortCounts(1,0); LayoutPortsOnEllipse; absolute drawing
- [ ] Gateway
	- [ ] NodeProperties: Label (optional)
	- [ ] Setters sync + InvalidateVisual(); EnsurePortCounts(1,2); LayoutPortsVerticalSegments; absolute drawing
- [ ] Decision
	- [ ] NodeProperties: Label (optional)
	- [ ] Setters sync + InvalidateVisual(); EnsurePortCounts(1,2); LayoutPortsVerticalSegments; absolute drawing
- [ ] Document
	- [ ] NodeProperties: Label
	- [ ] Setters sync + InvalidateVisual(); geometry props MarkPortsDirty
	- [ ] Absolute coordinate drawing; lazy port layout ensured
- [ ] Database/DataStore
	- [ ] NodeProperties: Label
	- [ ] Setters sync + InvalidateVisual(); geometry props MarkPortsDirty
	- [ ] Absolute coordinate drawing; lazy port layout ensured
- [ ] ExternalData
	- [ ] NodeProperties: Label
	- [ ] Setters sync + InvalidateVisual(); geometry props MarkPortsDirty
	- [ ] Absolute coordinate drawing; lazy port layout ensured
- [ ] Person/Department/Role/System/Group/SubProcess/ActionNode/EventNode/Annotation/BusinessTask/RuleFlow/RuleEngine
	- [ ] Seed relevant NodeProperties (titles, types, flags); sync setters; absolute drawing; lazy ports

Node inventory and status (Business):
- [x] TaskNode – NodeProperties seeded (TaskDescription, TaskStatus, AssignedTo, DueDate, Progress); lazy ports ensured
- [x] StartEvent – Label NodeProperty; ellipse ports (0 in, 1 out)
- [x] EndEvent – Label NodeProperty; ellipse ports (1 in, 0 out)
- [x] Gateway – Label NodeProperty; diamond ports (1 in, 2 out)
- [x] Decision – Label NodeProperty; diamond ports (1 in, 2 out)
- [x] Document – Label NodeProperty; folded-corner rect; ports 1/1
- [x] Database – Label NodeProperty; cylinder; ports 1/1
- [x] DataStore – Label NodeProperty; open box; ports 1/1
- [x] ExternalData – Label NodeProperty; parallelogram; ports 1/1
- [x] BusinessTask – Label NodeProperty + Name sync; ports 1/1
- [x] BusinessSystem – SystemName NodeProperty; ports 1/1
- [x] SubProcess – ProcessName, IsCollapsed NodeProperties; ChildNodes kept in sync; ports 1/1
- [x] ActionNode – ActionText, ActionType NodeProperties; ports 1/1
- [x] EventNode – EventName, EventType, TriggerCondition, IsTriggered, TriggerTime NodeProperties; ellipse ports 1/1
- [x] Annotation – AnnotationText, AnnotationType, ShowBorder, ShowBackground NodeProperties (visual helper); ports 1/1
- [x] Group – GroupName, GroupColor, GroupType NodeProperties; ChildNodes synced; container (0 ports)
- [x] DataObject – DataName, DataType, DataFormat, IsCollection NodeProperties; ports 1/1
- [x] Department – DepartmentName, EmployeeCount NodeProperties; ports 1/1
- [x] Person – PersonName, Title NodeProperties; ellipse ports 1/1
- [x] Role – RoleName NodeProperty; ports 1/1
- [x] RuleFlow – FlowLabel, Direction, IsActive NodeProperties; connector (no ports)
- [x] RuleEngine – RuleSetName, RuleCount, IsActive NodeProperties; ellipse ports 1/1
- [ ] ConditionBuilder – N/A (helper/logic, not a visual node)

Acceptance criteria (Business):
- All nodes expose their editable fields via NodeProperties and apply via SetProperties.
- Setters sync NodeProperties and call InvalidateVisual; geometry-affecting setters call MarkPortsDirty.
- DrawBusinessContent uses absolute coordinates; no canvas.Translate in nodes.
- Ports re-layout lazily via EnsurePortLayout(() => LayoutPorts()).
- Container-style nodes (e.g., Group) manage Children and ChildNodes consistently.

MindMap family adoption (NodeProperties + Get/SetProperties) – progress update:

- Base (MindMapControl): added NodeProperties for InPortCount, OutPortCount, BackgroundColor, BorderColor, BorderThickness, TextColor; setters keep metadata in sync. EnsurePortCounts updates metadata and marks ports dirty lazily.
- CentralNode: NodeProperties Title, Notes; setters sync; drawing refactored to DrawMindMapContent template with lazy port layout.
- TopicNode: NodeProperties Title, Notes; setters sync; uses DrawMindMapContent.
- SubTopicNode: NodeProperties Title, Notes; setters sync; uses DrawMindMapContent.
- NoteNode: NodeProperties Title, Notes; setters sync; uses DrawMindMapContent; port layout avoids note fold area.

Next for MindMap:
- Verify editor round-trips for color fields (SKColor parsing via SetProperties) and port counts.
- Add any missing style tokens if needed (e.g., per-node accent colors) as NodeProperties.

UML family adoption (NodeProperties + Get/SetProperties) – progress update:

- Base (UMLControl): now inherits from MaterialControl for consistency and InvalidateVisual; seeded NodeProperties for Stereotype, BackgroundColor, BorderColor, BorderThickness, and TextColor (metadata only); property setters sync NodeProperties and invalidate; lazy port layout preserved (LayoutPorts with ArePortsDirty + ClearPortsDirty in DrawContent).
- UMLClass: NodeProperties for ClassName, IsAbstract, AttributesText (one-per-line), OperationsText; setters sync metadata and call InvalidateVisual; drawing remains via DrawUMLContent.
- UMLInterface: NodeProperties for InterfaceName and OperationsText; setters sync and invalidate.

Next for UML:
- Sweep remaining UML nodes (Actor, Lifeline, DataSource/Transform/Output nodes) to seed their specific metadata (titles, types, flags) and ensure any style/geometry properties update NodeProperties and MarkPortsDirty as applicable.

Per-node checklist (UML):
- [ ] Base (UMLControl)
	- [ ] Seed NodeProperties: Stereotype, BackgroundColor, BorderColor, BorderThickness, TextColor
	- [ ] Setters sync + InvalidateVisual(); DrawUMLContent wraps EnsurePortLayout(() => LayoutPorts())
- [ ] UMLClass
	- [ ] NodeProperties: ClassName, IsAbstract (bool), AttributesText (multiline), OperationsText (multiline)
	- [ ] Setters sync + InvalidateVisual(); absolute coordinates; lazy ports ensured
- [ ] UMLInterface
	- [ ] NodeProperties: InterfaceName, OperationsText (multiline)
	- [ ] Setters sync + InvalidateVisual(); lazy ports ensured
- [ ] UMLActor (if present)
	- [ ] NodeProperties: Name
	- [ ] Setters sync + InvalidateVisual(); lazy ports ensured
- [ ] UMLLifeline (if present)
	- [ ] NodeProperties: Name, ActivationStyle (optional)
	- [ ] Setters sync + InvalidateVisual(); geometry props MarkPortsDirty

Node inventory and status (UML):
- [x] UMLControl (base) – NodeProperties seeded; lazy ports ensured
- [x] UMLClass – NodeProperties (ClassName, IsAbstract, AttributesText, OperationsText)
- [x] UMLInterface – NodeProperties (InterfaceName, OperationsText)
- [x] UMLActor – NodeProperties (ActorName)
- [x] UMLLifeline – NodeProperties (ObjectName, ClassName, LifelineLength)
- [x] UMLDataSourceNode – NodeProperties (DataSourceType, DataSourceName)
- [x] UMLTransformNode – NodeProperties (TransformType, TransformDescription)
- [x] UMLOutputNode – NodeProperties (OutputType, OutputDestination)

Network family adoption (NodeProperties + Get/SetProperties) – progress update:

- NetworkNode: already on MaterialControl; NodeProperties seeded for Fill/Stroke/StrokeWidth/CornerRadius, NodeType/IsHighlighted/Scale/CentralityColor/CommunityColor, and port counts; setters sync and invalidate; lazy port layout in DrawContent via EnsurePortLayout.
- NetworkLink: now inherits MaterialControl to support InvalidateVisual; NodeProperties added for Color, Thickness, Weight, LinkType, and IsHighlighted; setters sync metadata and invalidate; DrawContent computes cubic curve between nodes using absolute coordinates.
- NetworkGraph: NodeProperties added for Background, GridColor, and GridSpacing; constructor seeds metadata; DrawContent renders background/grid then draws Links and Nodes.

Next for Network:
- Consider exposing graph-level editor actions (Add/Remove Node/Link) via Commands; optional.
- Confirm palette/registry entries reflect updated types (Link now MaterialControl); update any designer metadata if applicable.

Per-node checklist (Network):
- [ ] NetworkNode
	- [ ] NodeProperties: Fill, Stroke, StrokeWidth, CornerRadius, NodeType (Choices), IsHighlighted (bool), Scale, CentralityColor, CommunityColor, InPortCount, OutPortCount
	- [ ] Setters sync + InvalidateVisual(); geometry-affecting setters MarkPortsDirty
	- [ ] DrawContent uses EnsurePortLayout(() => LayoutPorts()); absolute coordinates only
- [ ] NetworkLink
	- [ ] Base = MaterialControl; NodeProperties: Color, Thickness, Weight, LinkType (Choices: Default/Dashed/Directed/Weighted), ArrowSize, IsHighlighted (bool)
	- [ ] Setters sync + InvalidateVisual(); Draw supports dashed/arrow/weighted
	- [ ] Absolute coordinate cubic path; no canvas.Translate
- [ ] NetworkGraph
	- [ ] NodeProperties: Background, GridColor, GridSpacing
	- [ ] Setters sync + InvalidateVisual(); draws background, grid, children
	- [ ] ChildNodes populated for named lookup when applicable

Node inventory and status (Network):
- [x] NetworkNode – NodeProperties seeded; lazy ports ensured
- [x] NetworkLink – NodeProperties (Color, Thickness, Weight, LinkType, ArrowSize, IsHighlighted)
- [x] NetworkGraph – NodeProperties (Background, GridColor, GridSpacing)

Quantitative family adoption (NodeProperties + Get/SetProperties) – progress update:

- QuantControl (base): already inherits MaterialControl and seeds Fill/Stroke/StrokeWidth/TextColor; lazy ports with EnsurePortLayout.
- TimeSeriesNode: added NodeProperties for Length and Symbol; setters sync and invalidate; still produces one output series.
- IndicatorNode: added NodeProperties for Indicator and Period; setters sync and invalidate; defaults to SMA/20. Choices for Indicator can be extended (SMA/EMA/RSI/MACD) in editor.

Next for Quantitative:
- Optional follow-ups: extend Indicator Choices (e.g., SMA/EMA/RSI/MACD/MACDH), add additional nodes (Aggregator, Normalizer) with the same pattern.

Per-node checklist (Quantitative):
- [ ] Base (QuantControl)
	- [ ] Seed NodeProperties: Fill, Stroke, StrokeWidth, TextColor
	- [ ] DrawContent wraps EnsurePortLayout(() => LayoutPorts()); absolute coordinates
- [ ] TimeSeriesNode
	- [ ] NodeProperties: Length (int), Symbol (string)
	- [ ] Setters sync + InvalidateVisual(); geometry props MarkPortsDirty if they affect ports
- [ ] IndicatorNode
	- [ ] NodeProperties: Indicator (Choices: SMA/EMA/RSI/MACD), Period (int)
	- [ ] Setters sync + InvalidateVisual(); extend Choices as needed

Node inventory and status (Quantitative):
- [x] QuantControl (base) – NodeProperties seeded; lazy ports
- [x] TimeSeriesNode – NodeProperties (Length, Symbol)
- [x] IndicatorNode – NodeProperties (Indicator, Period)

---

Upcoming families (planning) – Business, Cloud, ECAD, ML, Security

For each family below, follow the standard adoption template: seed NodeProperties in constructors, sync in setters (InvalidateVisual + MarkPortsDirty for geometry), ensure family DrawContent wraps EnsurePortLayout and delegates visuals to DrawXxxContent, and verify absolute-coordinate drawing.

- Beep.Skia.Business
	- [ ] Project-level: Identify node list; add family base DrawBusinessContent wrapper if missing
	- [ ] Base: Seed style and port count NodeProperties; EnsurePortCounts marks ports dirty
	- [ ] Nodes: Add per-node NodeProperties (titles, flags); sync setters; lazy port layout

- Beep.Skia.Cloud
	- [ ] Project-level: Identify node list (providers/resources); add family base template wrapper
	- [ ] Base: Seed style + port counts; lazy ports
	- [ ] Nodes: Add NodeProperties and Choices where types are constrained

- Beep.Skia.ECAD
	- [ ] Project-level: Identify node list (components, nets); add base template wrapper
	- [ ] Base: Seed style + port counts; lazy ports; non-rect shapes compute precise anchors
	- [ ] Nodes: NodeProperties for footprint/pads; geometry setters MarkPortsDirty

- Beep.Skia.ML
	- [ ] Project-level: Identify node list (Model/Data/Train/Evaluate); add base template wrapper
	- [ ] Base: Seed style + port counts; lazy ports
	- [ ] Nodes: NodeProperties with Choices for model types; sync and invalidate

- Beep.Skia.Security
	- [ ] Project-level: Identify node list (Policy/Asset/Threat/Control); add base template wrapper
	- [ ] Base: Seed style + port counts; lazy ports
	- [ ] Nodes: NodeProperties for severity/classification; Choices for enums; sync setters

---

Cloud family adoption (planning detail)

- Base (CloudControl): inherits MaterialControl; seed NodeProperties: Provider (Choices: AWS/Azure/GCP/OnPrem), ResourceType (Choices), Background, BorderColor, BorderThickness, TextColor, InPortCount, OutPortCount; setters sync, geometry setters MarkPortsDirty; DrawCloudContent wraps EnsurePortLayout.
- Nodes (examples):
  - StorageBucket/BlobStorage: NodeProperties Name, StorageClass (Choices), Region; lazy ports.
  - VMInstance: NodeProperties Name, Size (Choices), OS (Choices), Region; lazy ports.
  - FunctionApp/Lambda: NodeProperties Name, Runtime (Choices), MemoryMB; lazy ports.
  - DatabaseService (SQL/NoSQL): NodeProperties Engine (Choices), Version, Region; lazy ports.
  - VNet/Subnet: NodeProperties CIDR, Region; geometry props MarkPortsDirty.
  - LoadBalancer/API Gateway: NodeProperties SKU (Choices), Protocol (Choices); arrow/flow oriented ports.

Per-node checklist (Cloud):
- [ ] Base (CloudControl)
	- [ ] Seed NodeProperties: Provider (Choices), ResourceType (Choices), Background, BorderColor, BorderThickness, TextColor, InPortCount, OutPortCount
	- [ ] Setters sync + InvalidateVisual(); geometry setters MarkPortsDirty
	- [ ] DrawCloudContent wraps EnsurePortLayout(() => LayoutPorts())
- [ ] StorageBucket/BlobStorage
	- [ ] NodeProperties: Name, StorageClass (Choices), Region
	- [ ] Setters sync + InvalidateVisual(); absolute drawing; lazy ports ensured
- [ ] VMInstance
	- [ ] NodeProperties: Name, Size (Choices), OS (Choices), Region
	- [ ] Setters sync + InvalidateVisual(); geometry props MarkPortsDirty if shape changes
- [ ] FunctionApp/Lambda
	- [ ] NodeProperties: Name, Runtime (Choices), MemoryMB
	- [ ] Setters sync + InvalidateVisual(); lazy ports
- [ ] DatabaseService (SQL/NoSQL)
	- [ ] NodeProperties: Engine (Choices), Version, Region
	- [ ] Setters sync + InvalidateVisual(); lazy ports
- [ ] VNet/Subnet
	- [ ] NodeProperties: CIDR, Region
	- [ ] Setters sync + InvalidateVisual(); ports along edges; lazy ports
- [ ] LoadBalancer/APIGateway
	- [ ] NodeProperties: SKU (Choices), Protocol (Choices)
	- [ ] Setters sync + InvalidateVisual(); arrow/flow cues; absolute drawing

ECAD family adoption (planning detail)

- Base (ECADControl): inherits MaterialControl; seed NodeProperties: Footprint, Package (Choices), Orientation (Choices: 0/90/180/270), PinCount, Background, BorderColor, BorderThickness, TextColor, In/Out ports as applicable; lazy ports; non-rect shapes compute exact pin anchors.
- Nodes (examples): Component, Resistor, Capacitor, IC, Connector, Net/Trace.

Per-node checklist (ECAD):
- [ ] Base (ECADControl)
	- [ ] Seed NodeProperties: Footprint, Package (Choices), Orientation (Choices), PinCount, Background, BorderColor, BorderThickness, TextColor, InPortCount, OutPortCount
	- [ ] Setters sync + InvalidateVisual(); geometry setters MarkPortsDirty
	- [ ] DrawECADContent wraps EnsurePortLayout(() => LayoutPorts())
- [ ] Component/IC
	- [ ] NodeProperties: Reference, Footprint, PinCount, PinNames (multiline)
	- [ ] Setters sync + InvalidateVisual(); compute pin anchors along perimeter; lazy ports
- [ ] Resistor/Capacitor/Inductor
	- [ ] NodeProperties: Value, Package (Choices)
	- [ ] Setters sync + InvalidateVisual(); absolute drawing; lazy ports
- [ ] Connector
	- [ ] NodeProperties: Positions/PinCount, Keying (optional)
	- [ ] Setters sync + InvalidateVisual(); exact pin locations; lazy ports
- [ ] Net/Trace (if represented)
	- [ ] Base = MaterialControl; NodeProperties: Width, Layer (Choices)
	- [ ] Setters sync + InvalidateVisual(); cubic/segment path absolute

ML family adoption (planning detail)

- Base (MLControl): inherits MaterialControl; seed NodeProperties: Background, BorderColor, BorderThickness, TextColor, In/Out ports; DrawMLContent wraps EnsurePortLayout.
- Nodes (examples): DataSource, Preprocess, Model, Train, Evaluate, Predict.

Per-node checklist (ML):
- [ ] Base (MLControl)
	- [ ] Seed NodeProperties: Background, BorderColor, BorderThickness, TextColor, InPortCount, OutPortCount
	- [ ] Setters sync + InvalidateVisual(); geometry setters MarkPortsDirty
	- [ ] DrawMLContent wraps EnsurePortLayout(() => LayoutPorts())
- [ ] DataSource
	- [ ] NodeProperties: SourceType (Choices), Path/Connection
	- [ ] Setters sync + InvalidateVisual(); absolute drawing; lazy ports
- [ ] Preprocess
	- [ ] NodeProperties: Transform (Choices: Normalize/OneHot/Impute/etc.), Parameters (metadata fields)
	- [ ] Setters sync + InvalidateVisual(); lazy ports
- [ ] Model
	- [ ] NodeProperties: ModelType (Choices: Linear/Tree/NN/etc.), Hyperparams (metadata)
	- [ ] Setters sync + InvalidateVisual(); geometry props MarkPortsDirty if shape varies
- [ ] Train/Evaluate/Predict
	- [ ] NodeProperties: Epochs/Batches/Metrics (as applicable)
	- [ ] Setters sync + InvalidateVisual(); lazy ports

Security family adoption (planning detail)

- Base (SecurityControl): inherits MaterialControl; seed NodeProperties: Background, BorderColor, BorderThickness, TextColor, SeverityScale (Choices), In/Out ports; DrawSecurityContent wraps EnsurePortLayout.
- Nodes (examples): Asset, Threat, Control, Policy, RiskAssessment, MitigationLink.

Per-node checklist (Security):
- [ ] Base (SecurityControl)
	- [ ] Seed NodeProperties: Background, BorderColor, BorderThickness, TextColor, SeverityScale (Choices), InPortCount, OutPortCount
	- [ ] Setters sync + InvalidateVisual(); geometry setters MarkPortsDirty
	- [ ] DrawSecurityContent wraps EnsurePortLayout(() => LayoutPorts())
- [ ] Asset
	- [ ] NodeProperties: Name, Category (Choices), Criticality (Choices)
	- [ ] Setters sync + InvalidateVisual(); absolute drawing; lazy ports
- [ ] Threat
	- [ ] NodeProperties: Name, Severity (Choices), Likelihood (Choices)
	- [ ] Setters sync + InvalidateVisual(); lazy ports
- [ ] Control
	- [ ] NodeProperties: Name, Type (Choices), Status (Choices)
	- [ ] Setters sync + InvalidateVisual(); lazy ports
- [ ] Policy
	- [ ] NodeProperties: Name, Scope, Status (Choices)
	- [ ] Setters sync + InvalidateVisual(); lazy ports
- [ ] RiskAssessment
	- [ ] NodeProperties: Method (Choices), RiskScore
	- [ ] Setters sync + InvalidateVisual(); lazy ports
- [ ] MitigationLink (if separate component)
	- [ ] Base = MaterialControl; NodeProperties: Color, Thickness, HasArrow (bool), Status (Choices)
	- [ ] Setters sync + InvalidateVisual(); absolute path drawing with arrow option

Scaffold tasks (Cloud/ECAD/ML/Security):
- [ ] Create family base class inheriting MaterialControl (CloudControl/ECADControl/MLControl/SecurityControl)
- [ ] Implement DrawXxxContent(canvas, context) and call EnsurePortLayout(() => LayoutPorts()) early
- [ ] Seed base NodeProperties (styles + InPortCount/OutPortCount; family-specific enums with Choices)
- [ ] Add 2–3 sample nodes per family and seed their NodeProperties; sync setters + InvalidateVisual; MarkPortsDirty for geometry
- [ ] Register sample nodes in palette/registry (if applicable) and add minimal usage docs

Scaffold progress (Cloud):
- [x] CloudControl (base) – NodeProperties seeded (Background/Border/Text, In/Out counts); lazy port layout; DrawContent wrapper
- [x] CloudStorageNode – NodeProperties (ResourceName, Provider, StorageType); ports 1/1
- [x] CloudComputeNode – NodeProperties (VmName, Provider, CpuCores, MemoryGB); ports 1/1
 - [x] CloudFunctionNode – NodeProperties (FunctionName, Runtime, MemoryMB); ports 1/1
 - [x] CloudDatabaseNode – NodeProperties (DatabaseName, Engine, Version, Region); ports 1/1

Scaffold progress (ECAD):
- [x] ECADControl (base) – NodeProperties seeded (Background/Border/Text, In/Out counts); lazy port layout; DrawContent wrapper
- [x] ECADResistorNode – NodeProperties (ComponentValue, Package, Orientation); ports 1/1
- [x] ECADCapacitorNode – NodeProperties (ComponentValue, Package, Orientation); ports 1/1
 - [x] ECADICNode – NodeProperties (Reference, Footprint, PinCount); ports 4/4
 - [x] ECADTraceNode – NodeProperties (WidthPx, Layer); connector-style segment (no ports)

Scaffold progress (ML):
- [x] MLControl (base) – NodeProperties seeded (Background/Border/Text, In/Out counts); lazy port layout; DrawContent wrapper
- [x] MLDataSourceNode – NodeProperties (SourceName, Connector, Format); ports 0/1
- [x] MLModelNode – NodeProperties (ModelName, Framework, ModelType, HyperParameters); ports 1/1
 - [x] MLPreprocessNode – NodeProperties (StepName, Transform, Parameters); ports 1/1
 - [x] MLEvaluateNode – NodeProperties (StepName, Metric, Threshold); ports 1/1

Scaffold progress (Security):
- [x] SecurityControl (base) – NodeProperties seeded (Background/Border/Text, In/Out counts); lazy port layout; DrawContent wrapper
- [x] AssetNode – NodeProperties (AssetName, Category, Criticality); ports 1/1
- [x] ThreatNode – NodeProperties (ThreatName, Severity, Likelihood); ports 1/1
- [x] ControlNode – NodeProperties (ControlName, ControlType, Status); ports 1/1
- [x] PolicyNode – NodeProperties (PolicyName, Version, Status); ports 1/1
- [x] RiskAssessmentNode – NodeProperties (AssessmentName, RiskScore, AssessedOn); ports 1/1
- [x] MitigationLink – NodeProperties (Color, Thickness, Style); connector (no ports)
 - [x] VulnerabilityNode – NodeProperties (VulnId, Severity, Cvss, AffectedAsset); ports 1/1
 - [x] IncidentNode – NodeProperties (IncidentId, Status, DetectedOn, Confidence); ports 1/1
 - [x] FindingNode – NodeProperties (Title, Type, Confidence); ports 1/1

Change log (2025-10-02)

- NetworkLink
	- Changed base to MaterialControl to unify invalidation.
	- Added NodeProperties: Color, Thickness, Weight, LinkType (Choices), IsHighlighted, ArrowSize.
	- Rendering: supports Dashed, Directed (arrowhead), and Weighted (stroke scaled by Weight).
- NetworkGraph
	- Changed base to MaterialControl; Background/GridColor/GridSpacing setters invalidate and sync NodeProperties.
- Quantitative
	- IndicatorNode: NodeProperties Indicator (Choices: SMA/EMA/RSI/MACD), Period; setters sync + invalidate.
	- TimeSeriesNode: NodeProperties Length/Symbol; setters sync + invalidate.
- Tests (internal hygiene only; no runtime behavior change)
	- Adjusted tests to current ValidationResult/NodeResult APIs and public ConnectComponents usage.

- Documentation
    - Added Controls projects sweep matrix with project-level statuses and next steps
    - Added per-node checklists for DFD, ETL, PM, ERD, StateMachine, UML, Network, and Quantitative
    - Added planning sections for Business, Cloud, ECAD, ML, and Security
	- Added detailed planning subsections for Cloud/ECAD/ML/Security with per-node checklists

- Business
	- Base (BusinessControl): seeded NodeProperties; exposed In/Out port counts; ensured lazy ports
	- Migrated nodes: TaskNode (TaskDescription/TaskStatus/AssignedTo/DueDate/Progress), StartEvent, EndEvent, Gateway, Decision, Document, Database, DataStore, ExternalData (Label synched + seeded)
	- New this pass: BusinessTask (Label), BusinessSystem (SystemName), SubProcess (ProcessName/IsCollapsed + ChildNodes sync), ActionNode (ActionText/ActionType), Annotation (AnnotationText/AnnotationType/ShowBorder/ShowBackground), EventNode (EventName/EventType/TriggerCondition/IsTriggered/TriggerTime), Department (DepartmentName/EmployeeCount), Person (PersonName/Title), Role (RoleName), DataObject (DataName/DataType/DataFormat/IsCollection), RuleFlow (FlowLabel/Direction/IsActive), RuleEngine (RuleSetName/RuleCount/IsActive)

- Security
	- Base (SecurityControl): created with MaterialControl base; seeded style + port count NodeProperties; lazy port layout ensured
	- Nodes added: AssetNode, ThreatNode, ControlNode, PolicyNode, RiskAssessmentNode, MitigationLink (connector)
	- All nodes expose NodeProperties with enum Choices and sync setters; absolute coordinate drawing

- ECAD
	- Base (ECADControl): created; seeded style + port count NodeProperties; lazy ports
	- Nodes added: ECADResistorNode, ECADCapacitorNode with Value/Package/Orientation

- ML
	- Base (MLControl): created; seeded style + port count NodeProperties; lazy ports
	- Nodes added: MLDataSourceNode (Connector/Format), MLModelNode (Framework/ModelType/HyperParameters)

- Host/Palette
	- SkiaHostControl: palette category mapping extended to recognize Cloud, ECAD, ML, Security, and Quantitative namespaces so new families appear under their own sections.

Next steps

1) UML sanity sweep: verify remaining UML nodes expose NodeProperties and respect lazy port layout (Actor, Lifeline, DataSource/Transform/Output already largely covered).
2) Editor UX polish: ensure property editor uses ParameterInfo.Choices for Indicator and LinkType; add any missing display descriptions.
3) Optional: add graph-level commands (add/remove node/link) to NetworkGraph if desired by the editor.
4) Minor grooming: address low-priority warnings in unrelated components in a later pass.
5) Register any remaining nodes in palette via registry metadata if they don’t show up; confirm ShowInPalette=true where appropriate.
## Issues Identified

### 1. Project Configuration Issues
- **Duplicate entries** in .csproj files (ImplicitUsings, LangVersion)
- **Target framework** includes net9.0 (not yet released)
- **Missing package icon** references
- **Outdated package versions** that need updating

### 2. Code Quality Issues
- **Typo**: "IsConnectd" should be "IsConnected" in interfaces and implementations
- **Naming conventions**: Properties in SkiaMenuItem should follow PascalCase
- **Namespace inconsistency**: SkiaMenuItem uses wrong namespace
- **Bug in ConnectionPoint.Disconnect()**: Calls method on potentially null object

### 3. Architecture Issues
- **Tight coupling** between components
- **Inconsistent error handling**
- **Missing null checks** in several places
- **Event handling inconsistencies**

### 4. Performance Issues
- **Inefficient drawing operations** in TableDrawer
- **Memory leaks** potential in animation timers
- **Redundant calculations** in intersection detection

## Optimization Plan

### Phase 1: Critical Fixes
1. Fix .csproj configuration issues
2. Correct typos and naming conventions
3. Fix bugs in ConnectionPoint and TableDrawer
4. Update package dependencies

### Phase 2: Architecture Improvements
1. Implement proper error handling
2. Add null checks and validation
3. Improve event handling consistency
4. Refactor tight coupling issues

### Phase 3: Performance Optimizations
1. Optimize drawing operations
2. Improve memory management
3. Cache frequently used calculations
4. Implement object pooling where appropriate

### Phase 4: Code Quality
1. Add comprehensive documentation
2. Implement unit tests
3. Add code analysis rules
4. Improve code organization

## Timeline
- Phase 1: 2-3 days
- Phase 2: 3-4 days
- Phase 3: 2-3 days
- Phase 4: 2-3 days

## Success Criteria
- All compilation errors resolved
- No runtime exceptions with proper input
- Improved performance benchmarks
- Clean code analysis results
- Comprehensive test coverage

---

## Location Normalization Plan (Absolute Coordinate Migration)

Goal: Ensure every visual component renders using its absolute X,Y (no implicit local origin (0,0) assumptions and no unintended parent canvas translation coupling) so drag/drop positioning is consistent.

### Strategy
1. Audit each component for patterns:
	- new SKRect(0, 0, Width, Height) or variants (0-based partial height)
	- canvas.DrawRect / DrawRoundRect / paths using raw 0 offsets
	- Hit tests using new SKRect(0,0,...)
	- Internal offset drawing (e.g., item loops) lacking X,Y prefix
	- Use of canvas.Translate in component DrawContent (should be removed)
2. Replace with absolute rectangles (X, Y, X+Width, Y+Height) and propagate X/Y into child element positioning.
3. Adjust hit-tests similarly (retain local math only when explicitly converting point to local).
4. Leave framework-level pan/zoom (RenderingHelper) intact; it applies globally once.
5. Avoid changing obsolete text API calls in this pass (defer to separate modernization task to limit regression risk).

### Component Status Table
Legend: DONE (converted), PENDING (needs conversion), N/A (already absolute / not visual), PARTIAL (in progress)

| Component | Status | Notes |
|-----------|--------|-------|
| Checkbox | DONE | Baseline reference pattern |
| Button | DONE | Earlier pass |
| ButtonGroup | DONE | Removed per-button translate |
| Card | DONE | Rect made absolute |
| SvgImage | DONE | Removed Translate / pan zoom path |
| Menu | DONE | Rewritten absolute + Invalidate shim |
| MenuItem | DONE | Uses ParentMenu.Invalidate() shim present |
| MenuButton | PENDING | Audit required |
| MenuBar | PENDING | Likely horizontal item rects at origin |
| CascadingMenu / ContextMenu | PENDING | Derive from Menu patterns |
| ComponentPalette / Palette | DONE | Already absolute (validated) |
| List | DONE | Item bounds absolute |
| Dropdown | DONE | Field + list absolute |
| SegmentedButtons | DONE | Segment rects absolute |
| SplitButton | DONE | Primary & dropdown rects absolute |
| Slider | DONE | Hover rect absolute; track still local (acceptable) |
| Switch | DONE | Hit tests absolute |
| TextBox | DONE | All helper drawings absolute |
| TextArea | DONE | Background rect absolute |
| Search | DONE | Bar & view absolute + hit tests |
| FabMenu | DONE | Overlay absolute; item loops local OK |
| Notification | DONE | Round rect absolute |
| RadioGroup | DONE | Background rect absolute |
| CheckBoxGroup | DONE | Background rect absolute |
| Panel | DONE | Already absolute |
| ProgressBar | PARTIAL | Linear trackRect 0-based; fix to absolute |
| Spinner | PENDING | Verify arcs use X,Y center |
| Tabs | PENDING | Tab strip likely 0-based |
| NavigationBar | PENDING | Verify item rect loops |
| NavigationDrawer | PENDING | Drawer surface + items |
| NavigationItem | PENDING | Sub-item offsets |
| StatusBar / StatusBarItem | PENDING | Check 0-origin draws |
| ToolBar / ToolBarItem | PENDING | Similar to StatusBar |
| ToggleButton | PENDING | Likely button-like; confirm |
| FloatingActionButton | PENDING | Circle path origin |
| ColorPicker | PENDING | Gradient/wheel origin |
| DatePicker / TimePicker | PENDING | Calendar/time grid origins |
| DataGrid / DataGridColumn | PENDING | Cell rect local origins |

### Execution Order
1. Simple primitives (ProgressBar final fix, ToggleButton, FloatingActionButton, Spinner).
2. Navigation set (Tabs, Navigation*, StatusBar, ToolBar).
3. Pickers (DatePicker, TimePicker, ColorPicker).
4. DataGrid family.

### Acceptance Criteria (per component)
- No unintended (0,0)-anchored draw primitives for component bounds.
- Hit tests reflect absolute coordinates or convert point to local intentionally.
- No introduction of new translation side-effects.
- Visual parity maintained aside from correct placement.

### Deferred Work
- Text API modernization (SKFont, new DrawText overloads).
- Color token consolidation.


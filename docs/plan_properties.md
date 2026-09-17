# Properties Adoption Plan (GetProperties/SetProperties)

Goal: Ensure every node/control across all projects exposes and applies data via the base APIs:

- SkiaComponent.GetProperties(includeCommon=true, includeNodeProperties=true)
- SkiaComponent.SetProperties(props, updateNodeProperties=true, applyToPublicSetters=true)

Principles
- Seed NodeProperties for node-specific fields so they appear in editors and serialization.
- Keep NodeProperties in sync inside public setters (update ParameterCurrentValue).
- Prefer strongly-typed public setters; SetProperties will reflect and convert types safely (enum/TimeSpan/SKColor).
- For containers, use Children (ordered) and ChildNodes (named) to manage sub-components.

Phased Sweep Checklist (updated Oct 2, 2025)

1) ERD (this commit)
- ERDEntity: Seed NodeProperties for EntityName, RowsText; keep setters synced. Use SetProperties/GetProperties in editor bindings.
- Status: DONE

2) FlowChart
- Nodes: Process, Decision, Terminator, etc. Seed their configurable fields (Title, ShapeStyle, Port counts). Sync setters.
- Status: DONE

3) DFD
- ExternalEntity, DataStore, Process. Seed names, store numbers, etc. Sync setters.
- Status: DONE

4) Business, ETL, PM, MindMap, StateMachine, UML, Network, Quantitative
 - For each family base, standardize property exposure and port layout hooks. Seed commonly edited fields.
 - Status per family:
	 - Business: DONE (all nodes expose NodeProperties; setters sync; lazy ports). 
	 - ETL: DONE (base + nodes; lazy ports).
	 - PM: DONE (base + nodes; lazy ports).
	 - MindMap: DONE (base + nodes; lazy ports).
	 - StateMachine: DONE (base + nodes; lazy ports).
	 - UML: MOSTLY (base + major nodes done; verify stragglers).
	 - Network: DONE (Node/Link/Graph; Link has Label/Curvature/Bidirectional).
	 - Quantitative: DONE (TimeSeries/Indicator; lazy ports).

5) Cloud, ECAD, ML, Security (new families)
 - Cloud: Scaffolded (Storage, Compute, Function, Database) with NodeProperties and lazy ports.
 - ECAD: Scaffolded (Resistor, Capacitor, IC, Trace) with NodeProperties and lazy ports.
 - ML: Scaffolded (DataSource, Model, Preprocess, Evaluate) with NodeProperties and lazy ports.
 - Security: MOSTLY (Asset, Threat, Control, Policy, RiskAssessment, MitigationLink, Vulnerability, Incident, Finding).
 - Status: IN PROGRESS

5) Automation nodes (Beep.Skia.Components.*)
- Many already use NodeProperties (e.g., DataInputNode, TimerTriggerNode). Verify Choices/enums and keep Configuration in sync where applicable.
- Status: PARTIAL

Notes
- For complex or nested objects, consider serializing to JSON in editors but prefer discrete properties for common fields.
- If a node maintains dynamic sub-items (e.g., ERD rows), expose top-level editors (RowsText) and keep internal state consistent.

Editor and base improvements (this pass)
- ComponentPropertyEditor: 
	- Auto-populates enum dropdown choices when ParameterType is an enum and Choices is empty.
	- Adds TimeSpan editing via textual parse (HH:MM:SS).
- SkiaComponent.SetPropperties: 
	- After applying matching public setters, auto-seeds NodeProperties entries for non-common properties when missing, enabling generic round-trip of newly introduced fields.

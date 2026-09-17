# Working documents

This folder holds working documents, analyses and internal instructions. **User-facing
documentation lives in [`Help/`](../Help/index.html)** — open `Help/index.html` for the full site
(105 pages: guides, all 16 diagram families, automation, ecosystem, architecture internals,
per-project API reference, troubleshooting and search).

| Document | Purpose |
|----------|---------|
| [`AnimatedFlowLines.md`](AnimatedFlowLines.md) | Design notes for the seven connection-line animation styles (now also in `Help/core-concepts/connection-animation.html`) |
| [`ComprehensiveEventSystem.md`](ComprehensiveEventSystem.md) | Event-system design notes (now also in `Help/architecture/event-catalog.html`) |
| [`EventSystem-Complete-Guide.md`](EventSystem-Complete-Guide.md), [`EventSystem-Documentation.md`](EventSystem-Documentation.md) | Earlier event-system drafts, kept for reference |
| [`ERD_ENTERPRISE_FEATURES.md`](ERD_ENTERPRISE_FEATURES.md) | ERD enterprise feature notes (now also in `Help/diagram-families/erd-advanced.html`) |
| [`ETL_PROFESSIONAL_FEATURES.md`](ETL_PROFESSIONAL_FEATURES.md) | ETL professional feature notes (now also in `Help/diagram-families/etl-advanced.html`) |
| [`ETL_GAP_ANALYSIS.md`](ETL_GAP_ANALYSIS.md), [`FLOWCHART_GAP_ANALYSIS.md`](FLOWCHART_GAP_ANALYSIS.md) | Gap analyses used to plan the family depth work |
| [`Beep.Make.instructions.md`](Beep.Make.instructions.md) | Internal instructions for the Beep.Make-style automation direction |
| [`readme.instructions.md`](readme.instructions.md) | Internal conventions used when writing README-level documentation |
| [`plan_fixLocationDragandDrop.md`](plan_fixLocationDragandDrop.md), [`plan_properties.md`](plan_properties.md) | Historical fix plans |
| [`archive/`](archive/) | Superseded plans and scratch files, kept for history |

## Tooling

| Script | Purpose |
|--------|---------|
| [`_gen_project_readmes.ps1`](_gen_project_readmes.ps1) | Regenerates `README.md` for every library project from csproj metadata |

Documentation-site generators live next to the site in [`Help/`](../Help/):
`_gen_component_reference.ps1`, `_gen_api_reference.ps1`, `_gen_type_sections.ps1`,
`_gen_search_index.ps1`, `_gen_skia_docs.py`, `_gen_automation_docs.ps1`,
`_gen_ecosystem_docs.ps1`, `_gen_editor_guides_docs.ps1`, `_gen_release_notes.ps1` and the
link checker `_check_links.ps1`.

## Status

Live feature status, hardening record and packaging notes are tracked in
[`FEATURE_ROADMAP.md`](../FEATURE_ROADMAP.md). The Help site's own inventory is
[`Help/DOCUMENTATION_STATUS.md`](../Help/DOCUMENTATION_STATUS.md).

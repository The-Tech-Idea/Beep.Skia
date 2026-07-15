# Beep.Skia.WellLogs

This project adds a dedicated well-log drawing surface on top of SkiaSharp and Beep.Skia.

What is in place now:

- A well-log document model with depth axis, tracks, curves, lithology intervals, linear and log scaling.
- A layered brush library for shale, sand, limestone, gamma-ray shading, resistivity shading, porosity shading, gas crossover, oil, and water.
- A layout engine that computes track rectangles from width ratios and maps depth and curve values into screen coordinates.
- A renderer that draws multi-track composites with headers, depth gridlines, lithology fills, curve overlays, and pattern shading.
- A `WellLogCanvas` component that can be discovered by Beep.Skia and seeded with a demo triple-combo style layout.

Standards the project is designed around:

- LAS 2.0 and LAS 3.0 as the main ASCII interchange targets.
- DLIS / RP66 V1 and V2 as the main binary interchange targets.
- LIS79 as a legacy ingest target.

External references reviewed while shaping this scaffold:

- USGS LAS format summary for LAS 3.0 scope and sections.
- Energistics RP66 V1 and V2 documentation tables of contents for DLIS structure.
- Petroware Log I/O and StarSeis Well Log Viewer pages to sanity-check common industry-supported formats and composite viewer expectations.

Rendering approach used here:

- Track-first composition: each log track gets its own bounds, header, plot area, grid, and scale behavior.
- Depth-major layout: all tracks share the same vertical depth transform so overlays stay aligned.
- Style-by-brush: fills and lithology bands come from named brush definitions instead of hardcoded paint blocks.
- Template-friendly design: the document model is versionable so future public display-spec variants can be represented as named templates without changing the renderer.

One note on the requested "standard logs 1.2 and 1.3":

- I did not find an authoritative public document matching that exact naming during the initial pass.
- The current design keeps display templates separate from file standards so those profile versions can be added cleanly once you confirm the exact spec source you want to follow.
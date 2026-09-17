# Contributing to Beep.Skia

Thanks for your interest in improving Beep.Skia. This guide covers how to build, test and submit changes.

## Prerequisites

- A .NET SDK that can build the target frameworks you touch. The repository builds with the **.NET 10 SDK**.
- Access to the **private `TheTechIdea.Beep.*` feed**. The core package depends on BeepDM model packages that are not on nuget.org at the required versions. Add the feed to your `nuget.config` before restoring:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="beep" value="https://nuget.your-company.com/v3/index.json" />
  </packageSources>
</configuration>
```

If restore fails, see the [Troubleshooting guide](Help/guides/troubleshooting.html).

## Build and test

```bash
# Full solution (includes the MAUI head only if its workload is installed)
dotnet build Beep.Skia.Solution.sln

# CI-equivalent build: excludes MAUI, warnings are errors
dotnet build Beep.Skia.CI.slnf

# Tests (501 tests, including whole-surface sweeps)
dotnet test Beep.Skia.Tests/Beep.Skia.Tests.csproj
```

The solution is **warning-free** and CI turns warnings into errors (`CI=true`). Keep it that way: a change that introduces a warning will fail the pipeline.

## Repository layout

| Path | Contents |
|------|----------|
| `Beep.Skia/` | Core library: components, canvas, connections, automation, collaboration, extensions |
| `Beep.Skia.Model/` | Shared models and enums |
| `Beep.Skia.<Family>/` | One project per diagram family (16) |
| `Beep.Skia.*.Controls/` | Platform hosts (WinForms, WPF, Blazor, MAUI, Avalonia) |
| `Beep.Skia.Tests/` | xUnit test suite |
| `Beep.Skia.Sample.WinForms/`, `Beep.Skia.Sample.Server/` | Runnable samples |
| `Help/` | The HTML documentation site (open `Help/index.html`) |
| `docs/` | Working documents, gap analyses and internal instructions |
| `FEATURE_ROADMAP.md` | Live status of features and hardening work |

## Coding conventions

- **Language level:** latest C#; the core multi-targets `net8.0;net9.0;net10.0`, so avoid APIs newer than .NET 8 unless they are conditioned.
- **Nullable:** enabled where the project enables it; do not introduce new nullability warnings.
- **Public API:** document new public members with `///` XML comments. The build ships XML documentation in the packages; CS1591 is suppressed so missing comments do not break the build, but documented members are what consumers see in IntelliSense.
- **Comments in code:** explain *why*, not *what*. The framework style is a short summary on the type and member plus comments only where behavior is non-obvious.
- **Disposal:** anything that owns an `SKPaint`, `SKPath`, `SKBitmap`, `FileSystemWatcher` or `Timer` implements `IDisposable` and is disposed deterministically.
- **Thread safety:** `DrawingManager` is not thread-safe; the automation service, credential vault and collaboration service are. Keep new shared state either single-threaded or explicitly synchronized.
- **No per-frame allocations** in draw paths: reuse paints, cache typefaces through `TypefaceCache`.

## Adding a diagram family

1. Create `Beep.Skia.YourFamily/` multi-targeting `net8.0;net9.0`, referencing `Beep.Skia`.
2. Derive a family base from `MaterialControl` and concrete nodes from it; override `DrawContent(SKCanvas, DrawingContext)` and `LayoutPorts()`.
3. Implement `ISkiaExtension` to register the component types with palette categories.
4. Add tests: rendering, serialization round-trip and port layout at minimum (see existing family tests).
5. Document it: add a `Help/diagram-families/yourfamily.html` page and link it from `Help/index.html`.

The [Creating Custom Families guide](Help/guides/creating-custom-family.html) walks through the same steps with code.

## Tests

- Tests live in `Beep.Skia.Tests/` and use xUnit.
- Parallel test execution is disabled (`TestAssemblyInfo.cs`) because Skia native state is process-wide. Keep it that way for any project that renders.
- Prefer behavioral assertions over snapshot images; the suite favors "does it render ink / survive a round-trip / accept its own properties" checks that run everywhere.
- New public component types are picked up automatically by the whole-surface sweeps (`ComponentSurfaceSweepTests`, `ComponentBehaviourSweepTests`, `ComponentInteractionSweepTests`, `FamilyRoundTripSweepTests`).

## Submitting changes

1. Branch from `master`.
2. Keep commits focused; the project uses conventional prefixes:
   `feat(scope):`, `fix(scope):`, `docs:`, `test:`, `build:`, `perf:`, `refactor:`.
3. Write a body that explains the problem and the fix, not just the file list.
4. Run the build and the full test suite before pushing.
5. If you change behavior, update `FEATURE_ROADMAP.md` and the relevant `Help/` pages in the same change.

## Documentation

The documentation site is plain HTML under `Help/`. Two things to know:

- Several sections are **generated** from source metadata. If you change component APIs, regenerate:
  `Help/_gen_component_reference.ps1`, `Help/_gen_api_reference.ps1`, `Help/_gen_type_sections.ps1`, `Help/_gen_search_index.ps1`.
- After editing pages, run `Help/_check_links.ps1` and fix any broken reference it reports.

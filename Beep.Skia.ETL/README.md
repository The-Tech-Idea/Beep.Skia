# Beep.Skia.ETL

ETL pipeline diagram components with an expression engine, data profiling and structured data flattening

| | |
|---|---|
| Target frameworks | `net8.0;net9.0` |
| NuGet package | `Beep.Skia.ETL` |
| Documentation | [etl-advanced.html](../Help/diagram-families/etl-advanced.html) |
| Start with | ETLSource, ETLTransform, ETLLookup, ETLScd, ETLCdcNode, ExpressionEngine, DataProfiler |

Fuzzy lookups, SCD/CDC, reshaping, profiling and pipeline metrics.

```bash
dotnet add package Beep.Skia.ETL
```

## Building

```bash
dotnet build Beep.Skia.ETL/Beep.Skia.ETL.csproj
```

The solution requires the private `TheTechIdea.Beep.*` NuGet feed; see [CONTRIBUTING.md](../CONTRIBUTING.md).

## Documentation

- [Documentation site](../Help/index.html) (open `Help/index.html`)
- [Guide for this module](../Help/diagram-families/etl-advanced.html)
- [API reference](../Help/reference/beep-skia-etl.html)
- [Feature roadmap](../FEATURE_ROADMAP.md)


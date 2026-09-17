# Beep.Skia.ECAD

Electrical CAD diagram components (passives, semiconductors, sources) with pin modelling and electrical rules checking

| | |
|---|---|
| Target frameworks | `net8.0;net9.0` |
| NuGet package | `Beep.Skia.ECAD` |
| Documentation | [ecad.html](../Help/diagram-families/ecad.html) |
| Start with | ECADResistorNode, ECADICNode, ECADLogicGateNode, ElectricalRulesChecker |

IEEE/ANSI symbols with an electrical rules check.

```bash
dotnet add package Beep.Skia.ECAD
```

## Building

```bash
dotnet build Beep.Skia.ECAD/Beep.Skia.ECAD.csproj
```

The solution requires the private `TheTechIdea.Beep.*` NuGet feed; see [CONTRIBUTING.md](../CONTRIBUTING.md).

## Documentation

- [Documentation site](../Help/index.html) (open `Help/index.html`)
- [Guide for this module](../Help/diagram-families/ecad.html)
- [API reference](../Help/reference/beep-skia-ecad.html)
- [Feature roadmap](../FEATURE_ROADMAP.md)


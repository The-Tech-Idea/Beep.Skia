# Beep.Skia.ML

Machine-learning pipeline diagram components with pipeline export

| | |
|---|---|
| Target frameworks | `net8.0;net9.0` |
| NuGet package | `Beep.Skia.ML` |
| Documentation | [ml.html](../Help/diagram-families/ml.html) |
| Start with | MLDataSourceNode, MLModelNode, MLTrainerNode, MLInferenceNode |

Pipeline export (ONNX/PMML style).

```bash
dotnet add package Beep.Skia.ML
```

## Building

```bash
dotnet build Beep.Skia.ML/Beep.Skia.ML.csproj
```

The solution requires the private `TheTechIdea.Beep.*` NuGet feed; see [CONTRIBUTING.md](../CONTRIBUTING.md).

## Documentation

- [Documentation site](../Help/index.html) (open `Help/index.html`)
- [Guide for this module](../Help/diagram-families/ml.html)
- [API reference](../Help/reference/beep-skia-ml.html)
- [Feature roadmap](../FEATURE_ROADMAP.md)


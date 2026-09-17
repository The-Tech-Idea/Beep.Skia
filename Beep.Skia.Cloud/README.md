# Beep.Skia.Cloud

Cloud architecture diagram components for AWS, Azure and GCP (compute, storage, network, branded nodes)

| | |
|---|---|
| Target frameworks | `net8.0;net9.0` |
| NuGet package | `Beep.Skia.Cloud` |
| Documentation | [cloud.html](../Help/diagram-families/cloud.html) |
| Start with | CloudComputeNode, CloudStorageNode, CloudFunctionNode, CloudDatabaseNode |

AWS, Azure and GCP service shapes.

```bash
dotnet add package Beep.Skia.Cloud
```

## Building

```bash
dotnet build Beep.Skia.Cloud/Beep.Skia.Cloud.csproj
```

The solution requires the private `TheTechIdea.Beep.*` NuGet feed; see [CONTRIBUTING.md](../CONTRIBUTING.md).

## Documentation

- [Documentation site](../Help/index.html) (open `Help/index.html`)
- [Guide for this module](../Help/diagram-families/cloud.html)
- [API reference](../Help/reference/beep-skia-cloud.html)
- [Feature roadmap](../FEATURE_ROADMAP.md)


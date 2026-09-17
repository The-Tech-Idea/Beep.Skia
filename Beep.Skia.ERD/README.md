# Beep.Skia.ERD

Entity-Relationship diagram components with DDL import/export, schema comparison and migration script generation

| | |
|---|---|
| Target frameworks | `net8.0;net9.0` |
| NuGet package | `Beep.Skia.ERD` |
| Documentation | [erd-advanced.html](../Help/diagram-families/erd-advanced.html) |
| Start with | ERDEntity, ERDRelationship, DDLExporter, DDLImporter, SchemaComparer, MigrationScriptGenerator |

DDL round-trips and migration scripts for six SQL dialects.

```bash
dotnet add package Beep.Skia.ERD
```

## Building

```bash
dotnet build Beep.Skia.ERD/Beep.Skia.ERD.csproj
```

The solution requires the private `TheTechIdea.Beep.*` NuGet feed; see [CONTRIBUTING.md](../CONTRIBUTING.md).

## Documentation

- [Documentation site](../Help/index.html) (open `Help/index.html`)
- [Guide for this module](../Help/diagram-families/erd-advanced.html)
- [API reference](../Help/reference/beep-skia-erd.html)
- [Feature roadmap](../FEATURE_ROADMAP.md)


# Generates a README.md for every library project from its csproj metadata plus a per-project
# purpose/key-types/help-page map. Run from anywhere:  powershell -File docs\_gen_project_readmes.ps1
$ErrorActionPreference = 'Stop'
$utf8 = [System.Text.UTF8Encoding]::new($false)
$root = Split-Path -Parent $PSScriptRoot

# Per-project extras: help page and the types a reader should start with.
$extras = @{
    'Beep.Skia'                     = @{ Help = 'Help/core-concepts/skia-component.html'; Types = 'DrawingManager, SkiaComponent, ConnectionLine, HistoryManager, ThemeManager, SkiaComponentRegistry'; Extra = 'Also contains the automation runtime, credential vault, collaboration, extension SDK and serialization.' }
    'Beep.Skia.Model'               = @{ Help = 'Help/architecture/serialization-system.html'; Types = 'DiagramDto, ComponentDto, LineDto, ParameterInfo, WorkflowDefinition, IConnectionPoint, IConnectionLine'; Extra = 'No rendering dependencies; safe to reference from tooling.' }
    'Beep.Skia.FlowChart'           = @{ Help = 'Help/diagram-families/flowchart.html'; Types = 'ProcessNode, DecisionNode, StartEndNode, FlowchartControl'; Extra = 'Structured code generation and a flowchart simulator.' }
    'Beep.Skia.Business'            = @{ Help = 'Help/diagram-families/business-process.html'; Types = 'BusinessTask, Gateway, StartEvent, EndEvent, BpmnPoolNode, BpmnLaneNode'; Extra = 'BPMN import and export.' }
    'Beep.Skia.ERD'                 = @{ Help = 'Help/diagram-families/erd-advanced.html'; Types = 'ERDEntity, ERDRelationship, DDLExporter, DDLImporter, SchemaComparer, MigrationScriptGenerator'; Extra = 'DDL round-trips and migration scripts for six SQL dialects.' }
    'Beep.Skia.DFD'                 = @{ Help = 'Help/diagram-families/dfd.html'; Types = 'ProcessNode, DataStoreNode, ExternalEntityNode, DFDLevelNavigator'; Extra = 'Gane-Sarson and Yourdon notation with level drill-down.' }
    'Beep.Skia.ETL'                 = @{ Help = 'Help/diagram-families/etl-advanced.html'; Types = 'ETLSource, ETLTransform, ETLLookup, ETLScd, ETLCdcNode, ExpressionEngine, DataProfiler'; Extra = 'Fuzzy lookups, SCD/CDC, reshaping, profiling and pipeline metrics.' }
    'Beep.Skia.UML'                 = @{ Help = 'Help/diagram-families/uml.html'; Types = 'UMLClass, UMLInterface, UMLInheritance, UMLMessage, XmiExporter'; Extra = 'Class, component, deployment, sequence and activity diagrams with XMI export.' }
    'Beep.Skia.Network'             = @{ Help = 'Help/diagram-families/network.html'; Types = 'NetworkNode, NetworkLink, NetworkGraph, PathFinder, CentralityMeasure'; Extra = 'Graph algorithms and community detection.' }
    'Beep.Skia.PM'                  = @{ Help = 'Help/diagram-families/project-management.html'; Types = 'TaskNode, MilestoneNode, GanttBarNode, GanttTimelineNode, CriticalPathNode'; Extra = 'Critical-path calculation and Gantt timelines.' }
    'Beep.Skia.MindMap'             = @{ Help = 'Help/diagram-families/mindmap.html'; Types = 'CentralNode, TopicNode, SubTopicNode, NoteNode'; Extra = 'Radial layout with collapse/expand.' }
    'Beep.Skia.StateMachine'        = @{ Help = 'Help/diagram-families/state-machine.html'; Types = 'StateNode, InitialStateNode, FinalStateNode'; Extra = 'Guards, entry/exit actions and transition semantics on the line.' }
    'Beep.Skia.ECAD'                = @{ Help = 'Help/diagram-families/ecad.html'; Types = 'ECADResistorNode, ECADICNode, ECADLogicGateNode, ElectricalRulesChecker'; Extra = 'IEEE/ANSI symbols with an electrical rules check.' }
    'Beep.Skia.Cloud'               = @{ Help = 'Help/diagram-families/cloud.html'; Types = 'CloudComputeNode, CloudStorageNode, CloudFunctionNode, CloudDatabaseNode'; Extra = 'AWS, Azure and GCP service shapes.' }
    'Beep.Skia.Security'            = @{ Help = 'Help/diagram-families/security.html'; Types = 'AssetNode, ThreatNode, ControlNode, StrideAnalyzer'; Extra = 'STRIDE analysis, DREAD scoring and MITRE technique mapping.' }
    'Beep.Skia.ML'                  = @{ Help = 'Help/diagram-families/ml.html'; Types = 'MLDataSourceNode, MLModelNode, MLTrainerNode, MLInferenceNode'; Extra = 'Pipeline export (ONNX/PMML style).' }
    'Beep.Ski.Quantitative'         = @{ Help = 'Help/diagram-families/quantitative.html'; Types = 'TimeSeriesNode, IndicatorNode, StrategyNodes'; Extra = 'Charts: bar, line, pie, scatter, area.' }
    'Beep.Skia.WellLogs'            = @{ Help = 'Help/diagram-families/welllogs.html'; Types = 'WellLogCanvas, WellLogRenderer, WellLogLayoutEngine, WellLogTemplates'; Extra = 'LAS 2.0/3.0 parsing with multi-track rendering.' }
    'Beep.Skia.Winform.Controls'    = @{ Help = 'Help/hosts/winforms.html'; Types = 'SkiaHostControl, SkiaControl, SkiaButton, SkiaDataGrid'; Extra = 'A wrapper control for every Material component plus designers.' }
    'Beep.Skia.Wpf.Controls'        = @{ Help = 'Help/guides/platforms.html'; Types = 'SkiaHost'; Extra = 'Targets net10.0-windows10.0.19041.0 because of SkiaSharp view assets.' }
    'Beep.Skia.Blazor.Controls'     = @{ Help = 'Help/guides/platforms.html'; Types = 'SkiaCanvas'; Extra = 'Targets net8.0.' }
    'Beep.Skia.Maui.Controls'       = @{ Help = 'Help/guides/platforms.html'; Types = 'SkiaCanvasView'; Extra = 'Requires the MAUI workload.' }
    'Beep.Skia.Avalonia.Controls'   = @{ Help = 'Help/guides/platforms.html'; Types = 'SkiaCanvasControl'; Extra = 'Targets net8.0.' }
    'Beep.Skia.Loader'              = @{ Help = 'Help/guides/extensibility.html'; Types = 'BeepSkiaLoaderExtensions'; Extra = 'BeepDM plugin-loader integration.' }
}

$projects = Get-ChildItem -Directory -Filter 'Beep.Ski*' | Where-Object { $_.Name -notmatch 'Tests|Sample' } | Sort-Object Name
$written = 0

foreach ($dir in $projects) {
    $csproj = Get-ChildItem $dir.FullName -Filter *.csproj | Select-Object -First 1
    if (-not $csproj) { continue }
    $text = [System.IO.File]::ReadAllText($csproj.FullName)

    $name = $dir.Name
    $desc = if ($text -match '<Description>([^<]+)</Description>') { $matches[1] } else { "Beep.Skia module: $name" }
    $tfm = if ($text -match '<TargetFrameworks?>([^<]+)<') { $matches[1] } else { 'net8.0' }
    $packable = ($text -notmatch 'IsPackable>false')
    $extra = $extras[$name]

    $packageCell = if ($packable) { '`' + $name + '`' } else { 'not packed' }
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine("# $name")
    [void]$sb.AppendLine()
    [void]$sb.AppendLine($desc)
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("| | |")
    [void]$sb.AppendLine("|---|---|")
    [void]$sb.AppendLine("| Target frameworks | ``$tfm`` |")
    [void]$sb.AppendLine("| NuGet package | $packageCell |")
    if ($extra) {
        [void]$sb.AppendLine("| Documentation | [$(Split-Path $extra.Help -Leaf)](../$($extra.Help)) |")
        [void]$sb.AppendLine("| Start with | $($extra.Types) |")
    }
    [void]$sb.AppendLine()
    if ($extra -and $extra.Extra) {
        [void]$sb.AppendLine($extra.Extra)
        [void]$sb.AppendLine()
    }
    if ($packable) {
        [void]$sb.AppendLine('```bash')
        [void]$sb.AppendLine("dotnet add package $name")
        [void]$sb.AppendLine('```')
        [void]$sb.AppendLine()
    }
    [void]$sb.AppendLine('## Building')
    [void]$sb.AppendLine()
    [void]$sb.AppendLine('```bash')
    [void]$sb.AppendLine("dotnet build $name/$name.csproj")
    [void]$sb.AppendLine('```')
    [void]$sb.AppendLine()
    [void]$sb.AppendLine('The solution requires the private `TheTechIdea.Beep.*` NuGet feed; see [CONTRIBUTING.md](../CONTRIBUTING.md).')
    [void]$sb.AppendLine()
    [void]$sb.AppendLine('## Documentation')
    [void]$sb.AppendLine()
    [void]$sb.AppendLine('- [Documentation site](../Help/index.html) (open `Help/index.html`)')
    if ($extra) { [void]$sb.AppendLine("- [Guide for this module](../$($extra.Help))") }
    [void]$sb.AppendLine("- [API reference](../Help/reference/$(($name -replace '[^A-Za-z0-9]', '-').ToLowerInvariant()).html)")
    [void]$sb.AppendLine('- [Feature roadmap](../FEATURE_ROADMAP.md)')
    [void]$sb.AppendLine()

    $out = Join-Path $dir.FullName 'README.md'
    [System.IO.File]::WriteAllText($out, $sb.ToString(), $utf8)
    $written++
}

Write-Output "wrote $written project READMEs"

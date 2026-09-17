# Appends generated "type reference" sections to existing guide pages.
$ErrorActionPreference = 'Stop'
$utf8 = [System.Text.UTF8Encoding]::new($false)
$root = Split-Path -Parent $PSScriptRoot
$help = $PSScriptRoot
. (Join-Path $help '_gen_lib.ps1')

function New-Section([string]$id, [string]$heading, [string]$intro, [array]$specs) {
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine("<section class=`"section`" id=`"$id`">")
    [void]$sb.AppendLine("  <h2>$heading</h2>")
    [void]$sb.AppendLine("  <p>$intro</p>")
    foreach ($spec in $specs) {
        $file = Join-Path $root $spec.File
        $info = Get-TypeInfo $file $spec.Name
        [void]$sb.AppendLine("  <h3>$($spec.Name)</h3>")
        if ($info.Summary) { [void]$sb.AppendLine("  <p>$($info.Summary)</p>") }
        if (@($info.Members).Count -gt 0) {
            [void]$sb.AppendLine('  <table class="property-table">')
            [void]$sb.AppendLine('    <thead><tr><th>Member</th><th>Kind</th><th>Description</th></tr></thead>')
            [void]$sb.AppendLine('    <tbody>')
            foreach ($m in $info.Members) {
                [void]$sb.AppendLine("      <tr><td><code>$($m.Signature)</code></td><td>$($m.Kind)</td><td>$($m.Summary)</td></tr>")
            }
            [void]$sb.AppendLine('    </tbody>')
            [void]$sb.AppendLine('  </table>')
        }
    }
    [void]$sb.AppendLine('</section>')
    return $sb.ToString()
}

$edits = @(
    @{
        Page = 'architecture\layout-engines.html'
        Section = (New-Section 'auto-layout-types' 'Automatic Layout Engines' 'The layout engines compute positions for every component from the graph structure. Each implements the auto-layout contract and can be run over the current selection or the whole diagram.' @(
            @{ Name = 'ForceDirectedLayout'; File = 'Beep.Skia\Layout\AutoLayoutEngines.cs' }
            @{ Name = 'HierarchicalLayout'; File = 'Beep.Skia\Layout\AutoLayoutEngines.cs' }
            @{ Name = 'RadialLayout'; File = 'Beep.Skia\Layout\AutoLayoutEngines.cs' }
            @{ Name = 'GridAutoLayout'; File = 'Beep.Skia\Layout\AutoLayoutEngines.cs' }
            @{ Name = 'FlowLayout'; File = 'Beep.Skia\Layout\FlowLayout.cs' }
            @{ Name = 'GridLayout'; File = 'Beep.Skia\Layout\GridLayout.cs' }
            @{ Name = 'StackLayout'; File = 'Beep.Skia\Layout\StackLayout.cs' }
        ))
    }
    @{
        Page = 'architecture\history-undo-system.html'
        Section = (New-Section 'action-catalog' 'Action Catalog' 'Every mutating operation is recorded as a DrawingAction subclass. The manager pushes these automatically; hosts only construct them when applying their own commands.' @(
            @{ Name = 'AddComponentAction'; File = 'Beep.Skia\DrawingActions.cs' }
            @{ Name = 'RemoveComponentAction'; File = 'Beep.Skia\DrawingActions.cs' }
            @{ Name = 'DeleteComponentsAction'; File = 'Beep.Skia\DrawingActions.cs' }
            @{ Name = 'MoveComponentsAction'; File = 'Beep.Skia\DrawingActions.cs' }
            @{ Name = 'ConnectComponentsAction'; File = 'Beep.Skia\DrawingActions.cs' }
            @{ Name = 'DisconnectComponentsAction'; File = 'Beep.Skia\DrawingActions.cs' }
            @{ Name = 'PasteComponentsAction'; File = 'Beep.Skia\DrawingActions.cs' }
            @{ Name = 'AlignComponentsAction'; File = 'Beep.Skia\DrawingActions.cs' }
            @{ Name = 'MoveLineAction'; File = 'Beep.Skia\DrawingActions.cs' }
            @{ Name = 'ConnectAutomationNodesAction'; File = 'Beep.Skia\DrawingActions.cs' }
        ))
    }
    @{
        Page = 'ecosystem\assisted-generation.html'
        Section = (New-Section 'dsl-types' 'DSL and Assistant Types' 'The offline assistant parses a small DSL into a graph and then materializes components. These types are the parser surface, useful for validation and tooling.' @(
            @{ Name = 'RuleBasedDiagramAssistant'; File = 'Beep.Skia\Assist\RuleBasedDiagramAssistant.cs' }
            @{ Name = 'FlowchartDslParser'; File = 'Beep.Skia\Assist\FlowchartDslParser.cs' }
            @{ Name = 'DslGraph'; File = 'Beep.Skia\Assist\FlowchartDslParser.cs' }
            @{ Name = 'DslNode'; File = 'Beep.Skia\Assist\FlowchartDslParser.cs' }
            @{ Name = 'DslEdge'; File = 'Beep.Skia\Assist\FlowchartDslParser.cs' }
            @{ Name = 'MindMapDslParser'; File = 'Beep.Skia\Assist\MindMapDslParser.cs' }
            @{ Name = 'MindMapDslNode'; File = 'Beep.Skia\Assist\MindMapDslParser.cs' }
        ))
    }
    @{
        Page = 'ecosystem\marketplace.html'
        Section = (New-Section 'package-types' 'Package and Registry Types' 'The marketplace API works with these records. They describe a package before install, an installed extension afterwards, and the log of install operations.' @(
            @{ Name = 'ExtensionPackage'; File = 'Beep.Skia\Extensions\Marketplace\ExtensionPackage.cs' }
            @{ Name = 'InstalledExtension'; File = 'Beep.Skia\Extensions\Marketplace\ExtensionPackageManager.cs' }
            @{ Name = 'InstallLogEntry'; File = 'Beep.Skia\Extensions\Marketplace\ExtensionPackageManager.cs' }
            @{ Name = 'SemanticVersion'; File = 'Beep.Skia\Extensions\Marketplace\SemanticVersion.cs' }
            @{ Name = 'LoadedExtension'; File = 'Beep.Skia\Extensions\ISkiaExtension.cs' }
            @{ Name = 'ExtensionLoadedEventArgs'; File = 'Beep.Skia\Extensions\ISkiaExtension.cs' }
        ))
    }
    @{
        Page = 'ecosystem\collaboration.html'
        Section = (New-Section 'collaboration-types' 'Collaboration Types' 'Review data is stored as plain records so it can be persisted and audited: users and shares, presence, comment pins and the audit trail.' @(
            @{ Name = 'UserRecord'; File = 'Beep.Skia\Collaboration\CollaborationSerializer.cs' }
            @{ Name = 'CollaborationSnapshot'; File = 'Beep.Skia\Collaboration\CollaborationSerializer.cs' }
            @{ Name = 'PresenceEntry'; File = 'Beep.Skia\Collaboration\CollaborationModels.cs' }
            @{ Name = 'DocumentShare'; File = 'Beep.Skia\Collaboration\CollaborationModels.cs' }
            @{ Name = 'AuditEntry'; File = 'Beep.Skia\Collaboration\CollaborationModels.cs' }
            @{ Name = 'CommentPin'; File = 'Beep.Skia\Collaboration\CommentPinLayer.cs' }
        ))
    }
    @{
        Page = 'guides\performance.html'
        Section = (New-Section 'utility-types' 'Performance Utilities' 'Two helpers keep the hot path fast: a typeface cache for text and a paint/text helper used across families.' @(
            @{ Name = 'TypefaceCache'; File = 'Beep.Skia\TypefaceCache.cs' }
            @{ Name = 'SkiaUtil'; File = 'Beep.Skia\SkiaUtil.cs' }
        ))
    }
)

foreach ($edit in $edits) {
    $path = Join-Path $help $edit.Page
    $text = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
    if ($text.Contains($edit.Section.Trim())) { Write-Output "skip (already present): $($edit.Page)"; continue }
    $marker = '</div></main></div>'
    if (-not $text.Contains($marker)) { $marker = '</div></main>' }
    $idx = $text.LastIndexOf($marker)
    if ($idx -lt 0) { Write-Output "FAILED (no marker): $($edit.Page)"; continue }
    $text = $text.Substring(0, $idx) + $edit.Section + $text.Substring($idx)
    [System.IO.File]::WriteAllText($path, $text, $utf8)
    Write-Output "updated $($edit.Page)"
}

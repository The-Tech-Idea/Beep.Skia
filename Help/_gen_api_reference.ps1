# Generates the per-project API reference pages from source metadata.
# Reuses the extraction helpers in _gen_lib.ps1.

$ErrorActionPreference = 'Stop'
$utf8 = [System.Text.UTF8Encoding]::new($false)
$root = Split-Path -Parent $PSScriptRoot
$help = $PSScriptRoot
. (Join-Path $help '_gen_lib.ps1')

$projects = @(
    @{ Name = 'Beep.Skia'; Dir = 'Beep.Skia'; Title = 'Beep.Skia (core)' }
    @{ Name = 'Beep.Skia.Model'; Dir = 'Beep.Skia.Model'; Title = 'Beep.Skia.Model' }
    @{ Name = 'Beep.Skia.FlowChart'; Dir = 'Beep.Skia.FlowChart'; Title = 'Flowchart family' }
    @{ Name = 'Beep.Skia.Business'; Dir = 'Beep.Skia.Business'; Title = 'Business process (BPMN) family' }
    @{ Name = 'Beep.Skia.ERD'; Dir = 'Beep.Skia.ERD'; Title = 'ERD family' }
    @{ Name = 'Beep.Skia.DFD'; Dir = 'Beep.Skia.DFD'; Title = 'DFD family' }
    @{ Name = 'Beep.Skia.ETL'; Dir = 'Beep.Skia.ETL'; Title = 'ETL family' }
    @{ Name = 'Beep.Skia.UML'; Dir = 'Beep.Skia.UML'; Title = 'UML family' }
    @{ Name = 'Beep.Skia.Network'; Dir = 'Beep.Skia.Network'; Title = 'Network family' }
    @{ Name = 'Beep.Skia.PM'; Dir = 'Beep.Skia.PM'; Title = 'Project management family' }
    @{ Name = 'Beep.Skia.MindMap'; Dir = 'Beep.Skia.MindMap'; Title = 'Mind map family' }
    @{ Name = 'Beep.Skia.StateMachine'; Dir = 'Beep.Skia.StateMachine'; Title = 'State machine family' }
    @{ Name = 'Beep.Skia.ECAD'; Dir = 'Beep.Skia.ECAD'; Title = 'ECAD family' }
    @{ Name = 'Beep.Skia.Cloud'; Dir = 'Beep.Skia.Cloud'; Title = 'Cloud architecture family' }
    @{ Name = 'Beep.Skia.Security'; Dir = 'Beep.Skia.Security'; Title = 'Security family' }
    @{ Name = 'Beep.Skia.ML'; Dir = 'Beep.Skia.ML'; Title = 'Machine learning family' }
    @{ Name = 'Beep.Ski.Quantitative'; Dir = 'Beep.Ski.Quantitative'; Title = 'Quantitative finance family' }
    @{ Name = 'Beep.Skia.WellLogs'; Dir = 'Beep.Skia.WellLogs'; Title = 'Well logs family' }
    @{ Name = 'Beep.Skia.Winform.Controls'; Dir = 'Beep.Skia.Winform.Controls'; Title = 'WinForms controls' }
    @{ Name = 'Beep.Skia.Wpf.Controls'; Dir = 'Beep.Skia.Wpf.Controls'; Title = 'WPF controls' }
    @{ Name = 'Beep.Skia.Blazor.Controls'; Dir = 'Beep.Skia.Blazor.Controls'; Title = 'Blazor controls' }
    @{ Name = 'Beep.Skia.Maui.Controls'; Dir = 'Beep.Skia.Maui.Controls'; Title = 'MAUI controls' }
    @{ Name = 'Beep.Skia.Avalonia.Controls'; Dir = 'Beep.Skia.Avalonia.Controls'; Title = 'Avalonia controls' }
    @{ Name = 'Beep.Skia.Loader'; Dir = 'Beep.Skia.Loader'; Title = 'Assembly loader extension' }
)

function Get-ProjectTypes([string]$dir) {
    $path = Join-Path $root $dir
    $results = New-Object System.Collections.Generic.List[object]
    $files = Get-ChildItem -Recurse $path -File -Filter *.cs | Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\' }
    foreach ($f in $files) {
        $lines = [System.IO.File]::ReadAllLines($f.FullName)
        for ($i = 0; $i -lt $lines.Length; $i++) {
            if ($lines[$i] -match '^\s*public\s+(sealed\s+|abstract\s+|partial\s+|static\s+)*(class|interface|enum|struct|record)\s+(\w+)') {
                $kind = $matches[2]
                $name = $matches[3]
                if ($kind -eq 'record' -and $lines[$i] -match 'record\s+(class|struct)\s+(\w+)') { $name = $matches[2] }
                $results.Add([pscustomobject]@{ Name = $name; Kind = $kind; File = $f.FullName; Rel = $f.FullName.Substring($path.Length + 1) })
            }
        }
    }
    return $results
}

function Get-TypeSummaryOnly([string]$file, [string]$typeName, [string]$kind) {
    $lines = [System.IO.File]::ReadAllLines($file)
    for ($i = 0; $i -lt $lines.Length; $i++) {
        if ($lines[$i] -match "(class|interface|enum|struct|record)\s+$([regex]::Escape($typeName))\b") {
            $summary = Get-SummaryBlock $lines $i
            if (-not $summary) {
                for ($k = $i - 1; $k -ge 0 -and $k -ge $i - 12; $k--) {
                    if ($lines[$k] -match '\[Description\("([^"]+)"\)\]') { $summary = $matches[1]; break }
                    if ($lines[$k].Trim() -ne '' -and -not $lines[$k].Trim().StartsWith('[')) { break }
                }
            }
            return $summary
        }
    }
    return ''
}

function New-TypePage([string]$dir, [string]$assemblyPage, [string]$assemblyTitle, [object]$type) {
    $anchor = 'type-' + $type.Name.ToLowerInvariant()
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('<!DOCTYPE html>')
    [void]$sb.AppendLine('<html lang="en">')
    [void]$sb.AppendLine('<head>')
    [void]$sb.AppendLine('<meta charset="UTF-8">')
    [void]$sb.AppendLine('<meta name="viewport" content="width=device-width, initial-scale=1.0">')
    [void]$sb.AppendLine("<title>$($type.Name) | Beep.Skia Documentation</title>")
    [void]$sb.AppendLine('<link rel="stylesheet" href="../../sphinx-style.css">')
    [void]$sb.AppendLine('<link rel="preconnect" href="https://fonts.googleapis.com">')
    [void]$sb.AppendLine('<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>')
    [void]$sb.AppendLine('<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">')
    [void]$sb.AppendLine('<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.5/font/bootstrap-icons.css">')
    [void]$sb.AppendLine('<style>.content{margin-left:0!important}</style>')
    [void]$sb.AppendLine('</head>')
    [void]$sb.AppendLine('<body>')
    [void]$sb.AppendLine('<div class="container">')
    [void]$sb.AppendLine('<main class="content"><div class="content-wrapper">')
    [void]$sb.AppendLine("<nav class=`"breadcrumb-nav`"><a href=`"../../index.html`">Home</a><span>&rsaquo;</span> <a href=`"../api-index.html`">API Reference</a><span>&rsaquo;</span> <a href=`"../$assemblyPage`">$assemblyTitle</a><span>&rsaquo;</span> <span>$($type.Name)</span></nav>")
    [void]$sb.AppendLine("<div class=`"page-header`"><h1>$($type.Name) <span style=`"font-size:0.5em;color:var(--color-foreground-muted)`">$($type.Kind)</span></h1>")
    [void]$sb.AppendLine("<p class=`"page-subtitle`">$assemblyTitle &mdash; $($type.Kind)</p></div>")
    if ($type.Summary) { [void]$sb.AppendLine("<p>$($type.Summary)</p>") }

    # Components (types exposing X/Y/Width/Height) get a quick usage snippet.
    $memberNames = @($type.Members | ForEach-Object { if ($_.Signature -match '([A-Za-z_]\w*)\s*$') { $matches[1] } })
    $isComponent = ($memberNames -contains 'X') -and ($memberNames -contains 'Y') -and ($memberNames -contains 'Width') -and ($memberNames -contains 'Height')
    if ($isComponent) {
        $labelProp = $null
        foreach ($candidate in @('Label', 'Title', 'Text', 'DisplayText', 'Name')) {
            if ($memberNames -contains $candidate) { $labelProp = $candidate; break }
        }
        [void]$sb.AppendLine('<h2>Quick usage</h2>')
        [void]$sb.AppendLine('<div class="code-example">')
        [void]$sb.AppendLine('<pre><code class="language-csharp">var component = new ' + $type.Name + ' { X = 100, Y = 100, Width = 160, Height = 60 };')
        if ($labelProp) { [void]$sb.AppendLine('component.' + $labelProp + ' = "Example";') }
        [void]$sb.AppendLine('drawingManager.AddComponent(component);')
        [void]$sb.AppendLine('drawingManager.RequestRedraw();</code></pre>')
        [void]$sb.AppendLine('</div>')
    }

    if ($type.Members -and @($type.Members).Count -gt 0) {
        [void]$sb.AppendLine("<h2>Members</h2>")
        [void]$sb.AppendLine('<table class="property-table">')
        [void]$sb.AppendLine('  <thead><tr><th>Member</th><th>Kind</th><th>Description</th></tr></thead>')
        [void]$sb.AppendLine('  <tbody>')
        foreach ($m in $type.Members) {
            [void]$sb.AppendLine("    <tr><td><code>$($m.Signature)</code></td><td>$($m.Kind)</td><td>$($m.Summary)</td></tr>")
        }
        [void]$sb.AppendLine('  </tbody>')
        [void]$sb.AppendLine('</table>')
    } else {
        [void]$sb.AppendLine('<p>This type exposes no public members beyond its declaration.</p>')
    }

    [void]$sb.AppendLine('<h2>Related</h2>')
    [void]$sb.AppendLine('<ul>')
    [void]$sb.AppendLine("  <li><a href=`"../$assemblyPage`">All types in $assemblyTitle</a></li>")
    [void]$sb.AppendLine("  <li><a href=`"../api-index.html`">API index</a></li>")
    [void]$sb.AppendLine('</ul>')
    [void]$sb.AppendLine('</div></main></div>')
    [void]$sb.AppendLine('</body></html>')

    $path = Join-Path (Join-Path $help "reference\$dir") "$($type.Name).html"
    [System.IO.File]::WriteAllText($path, $sb.ToString(), $utf8)
}

function New-RefPage([string]$fileName, [string]$title, [string]$subtitle, [string]$intro, [array]$types, [bool]$withMembers) {
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('<!DOCTYPE html>')
    [void]$sb.AppendLine('<html lang="en">')
    [void]$sb.AppendLine('<head>')
    [void]$sb.AppendLine('<meta charset="UTF-8">')
    [void]$sb.AppendLine('<meta name="viewport" content="width=device-width, initial-scale=1.0">')
    [void]$sb.AppendLine("<title>$title | Beep.Skia Documentation</title>")
    [void]$sb.AppendLine('<link rel="stylesheet" href="../sphinx-style.css">')
    [void]$sb.AppendLine('<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/themes/prism-tomorrow.min.css">')
    [void]$sb.AppendLine('<link rel="preconnect" href="https://fonts.googleapis.com">')
    [void]$sb.AppendLine('<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>')
    [void]$sb.AppendLine('<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">')
    [void]$sb.AppendLine('<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.5/font/bootstrap-icons.css">')
    [void]$sb.AppendLine('<style>.content{margin-left:0!important} .type-list{columns:3;column-gap:2rem;list-style:none;padding-left:0} .type-list li{margin:0.15rem 0} @media(max-width:900px){.type-list{columns:1}}</style>')
    [void]$sb.AppendLine('</head>')
    [void]$sb.AppendLine('<body>')
    [void]$sb.AppendLine('<div class="container">')
    [void]$sb.AppendLine('<main class="content"><div class="content-wrapper">')
    [void]$sb.AppendLine("<nav class=`"breadcrumb-nav`"><a href=`"../index.html`">Home</a><span>&rsaquo;</span> <a href=`"api-index.html`">API Reference</a><span>&rsaquo;</span> <span>$title</span></nav>")
    [void]$sb.AppendLine("<div class=`"page-header`"><h1>$title</h1><p class=`"page-subtitle`">$subtitle</p></div>")
    [void]$sb.AppendLine("<p>$intro</p>")

    if ($types.Count -gt 1) {
        [void]$sb.AppendLine('<h2>Types in this assembly</h2>')
        [void]$sb.AppendLine('<ul class="type-list">')
        foreach ($t in $types) {
            $anchor = 'type-' + $t.Name.ToLowerInvariant()
            [void]$sb.AppendLine("  <li><a href=`"#$anchor`"><code>$($t.Name)</code></a> <span style=`"color:var(--color-foreground-muted)`">$($t.Kind)</span></li>")
        }
        [void]$sb.AppendLine('</ul>')
    }

    foreach ($t in $types) {
        $anchor = 'type-' + $t.Name.ToLowerInvariant()
        $typePage = "$slug/$($t.Name).html"
        [void]$sb.AppendLine("<section class=`"section`" id=`"$anchor`">")
        [void]$sb.AppendLine("  <h2><a href=`"$typePage`">$($t.Name)</a> <span style=`"font-size:0.6em;color:var(--color-foreground-muted)`">($($t.Kind))</span></h2>")
        if ($t.Summary) { [void]$sb.AppendLine("  <p>$($t.Summary)</p>") }
        if ($t.Members -and @($t.Members).Count -gt 0) {
            [void]$sb.AppendLine('  <table class="property-table">')
            [void]$sb.AppendLine('    <thead><tr><th>Member</th><th>Kind</th><th>Description</th></tr></thead>')
            [void]$sb.AppendLine('    <tbody>')
            foreach ($m in $t.Members) {
                [void]$sb.AppendLine("      <tr><td><code>$($m.Signature)</code></td><td>$($m.Kind)</td><td>$($m.Summary)</td></tr>")
            }
            [void]$sb.AppendLine('    </tbody>')
            [void]$sb.AppendLine('  </table>')
        }
        [void]$sb.AppendLine('</section>')
    }

    [void]$sb.AppendLine('</div></main></div>')
    [void]$sb.AppendLine('</body></html>')
    [System.IO.File]::WriteAllText((Join-Path $help "reference\$fileName"), $sb.ToString(), $utf8)
    Write-Output "wrote reference/$fileName ($($types.Count) types)"
}

# Project index links are collected for the API index page rewrite.
$indexLinks = @{}

foreach ($p in $projects) {
    $dir = Join-Path $root $p.Dir
    if (-not (Test-Path $dir)) { continue }
    $types = Get-ProjectTypes $p.Dir | Sort-Object Name -Unique
    if ($types.Count -eq 0) { continue }

    $slug = ($p.Name -replace '[^A-Za-z0-9]', '-').ToLowerInvariant()

    # Attach summaries and members.
    $enriched = @()
    foreach ($t in $types) {
        $info = Get-TypeInfo $t.File $t.Name
        $summary = $info.Summary
        if (-not $summary) { $summary = Get-TypeSummaryOnly $t.File $t.Name $t.Kind }
        $members = @($info.Members)
        # Constructors register NodeProperties; never harvest those descriptions for the ctor itself.
        foreach ($m in $members) {
            if ($m.Signature -match "^$([regex]::Escape($t.Name))\s*\(" -and $m.Summary -notmatch '^Initializes a new instance') {
                $m.Summary = "Initializes a new instance of the $($t.Name) class."
            }
        }
        $enriched += [pscustomobject]@{ Name = $t.Name; Kind = $t.Kind; Summary = $summary; Members = $members }
    }

    $file = "$slug.html"
    $intro = "Complete API surface of the <code>$($p.Name)</code> assembly: $($enriched.Count) public types with their documented members. Follow the family guide for usage patterns."
    New-RefPage $file $p.Title "API reference for $($p.Name) ($($enriched.Count) public types)" $intro $enriched $true
    $indexLinks[$p.Name] = $file

    # One page per type
    $typeDir = Join-Path $help "reference\$slug"
    if (-not (Test-Path $typeDir)) { New-Item -ItemType Directory -Path $typeDir -Force | Out-Null }
    foreach ($t in $enriched) {
        New-TypePage $slug $file $p.Title $t
    }
    Write-Output "wrote reference/$slug/ ($($enriched.Count) type pages)"
}

# Persist the mapping for the API index rewrite.
$lines = foreach ($k in $indexLinks.Keys) { "$k=$($indexLinks[$k])" }
[System.IO.File]::WriteAllLines((Join-Path $help 'reference\_project-pages.txt'), $lines, $utf8)
Write-Output 'api reference done'

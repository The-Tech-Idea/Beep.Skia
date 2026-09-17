# Inserts the generated family images into the family pages and the landing page.
$ErrorActionPreference = 'Stop'
$utf8 = [System.Text.UTF8Encoding]::new($false)
$help = $PSScriptRoot

$families = @(
    @{ File = 'flowchart';          Page = 'diagram-families\flowchart.html';          Alt = 'Flowchart family: process, decision, loop, document and storage shapes' }
    @{ File = 'business-process';   Page = 'diagram-families\business-process.html';   Alt = 'Business process family: tasks, gateways, events, pools and lanes' }
    @{ File = 'erd';                Page = 'diagram-families\erd.html';                Alt = 'ERD family: entities, attributes and relationships' }
    @{ File = 'dfd';                Page = 'diagram-families\dfd.html';                Alt = 'DFD family: processes, data stores and external entities' }
    @{ File = 'etl';                Page = 'diagram-families\etl.html';                Alt = 'ETL family: sources, transforms, lookups and destinations' }
    @{ File = 'uml';                Page = 'diagram-families\uml.html';                Alt = 'UML family: classes, interfaces, actors, lifelines and relationships' }
    @{ File = 'network';            Page = 'diagram-families\network.html';            Alt = 'Network family: nodes, links and analysis panels' }
    @{ File = 'project-management'; Page = 'diagram-families\project-management.html'; Alt = 'Project management family: tasks, milestones, Gantt bars and risk shapes' }
    @{ File = 'mindmap';            Page = 'diagram-families\mindmap.html';            Alt = 'Mind map family: central topic, topics, sub-topics and notes' }
    @{ File = 'state-machine';      Page = 'diagram-families\state-machine.html';      Alt = 'State machine family: initial, state and final nodes' }
    @{ File = 'ecad';               Page = 'diagram-families\ecad.html';               Alt = 'ECAD family: passives, semiconductors, power and logic symbols' }
    @{ File = 'cloud';              Page = 'diagram-families\cloud.html';              Alt = 'Cloud family: compute, storage, functions and managed databases' }
    @{ File = 'security';           Page = 'diagram-families\security.html';           Alt = 'Security family: assets, threats, controls, vulnerabilities and findings' }
    @{ File = 'ml';                 Page = 'diagram-families\ml.html';                 Alt = 'Machine learning family: data, preprocessing, models, training and evaluation' }
    @{ File = 'quantitative';       Page = 'diagram-families\quantitative.html';       Alt = 'Quantitative family: time series, indicators and strategy nodes' }
    @{ File = 'welllogs';           Page = 'diagram-families\welllogs.html';           Alt = 'Well logs family: tracks, curves and log canvases' }
)

$inserted = 0
foreach ($family in $families) {
    $path = Join-Path $help $family.Page
    if (-not (Test-Path $path)) { Write-Output "skip missing: $($family.Page)"; continue }
    $text = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
    if ($text.Contains($family.File + '.png')) { Write-Output "skip (present): $($family.Page)"; continue }

    $headerIdx = $text.IndexOf('class="page-header"')
    if ($headerIdx -lt 0) { Write-Output "FAILED (no header): $($family.Page)"; continue }
    $divEnd = $text.IndexOf('</div>', $headerIdx)
    if ($divEnd -lt 0) { Write-Output "FAILED (no header close): $($family.Page)"; continue }
    $divEnd += '</div>'.Length

    $img = "`n<p><img src=`"../assets/families/$($family.File).png`" alt=`"$($family.Alt)`" loading=`"lazy`" style=`"max-width:100%;border:1px solid var(--color-background-border);border-radius:8px`"></p>"
    $text = $text.Substring(0, $divEnd) + $img + $text.Substring($divEnd)
    [System.IO.File]::WriteAllText($path, $text, $utf8)
    Write-Output "updated $($family.Page)"
    $inserted++
}

# Landing page hero
$homePath = Join-Path $help 'home.html'
$homeText = [System.IO.File]::ReadAllText($homePath, [System.Text.Encoding]::UTF8)
if (-not $homeText.Contains('families/flowchart.png')) {
    $idx = $homeText.IndexOf('</h1>')
    if ($idx -ge 0) {
        $img = "`n  <p><img src=`"assets/families/flowchart.png`" alt=`"Flowchart components rendered by Beep.Skia`" style=`"max-width:100%;border:1px solid var(--color-background-border);border-radius:8px`"></p>"
        $homeText = $homeText.Substring(0, $idx + 5) + $img + $homeText.Substring($idx + 5)
        [System.IO.File]::WriteAllText($homePath, $homeText, $utf8)
        Write-Output 'updated home.html (hero image)'
    }
}

Write-Output "family images embedded: $inserted"

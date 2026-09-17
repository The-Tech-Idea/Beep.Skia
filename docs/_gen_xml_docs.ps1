# Adds XML documentation comments to public members that lack them.
# Descriptions are harvested from the member's own ParameterInfo/Description text where present,
# otherwise derived from the member name. Only comment lines are inserted; code is untouched.
#
# Usage: powershell -File docs\_gen_xml_docs.ps1 -Projects Beep.Skia.ETL,Beep.Skia.ERD [-WhatIf]

param(
    [string[]]$Projects = @('Beep.Skia.FlowChart','Beep.Skia.ERD','Beep.Skia.ETL','Beep.Skia.Business','Beep.Skia.UML','Beep.Skia.Winform.Controls'),
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$utf8 = [System.Text.UTF8Encoding]::new($false)

function Get-HumanName([string]$name) {
    $spaced = [regex]::Replace($name, '(?<=[a-z0-9])(?=[A-Z])', ' ')
    $spaced = [regex]::Replace($spaced, '(?<=[A-Z])(?=[A-Z][a-z])', ' ')
    $words = $spaced -split ' '
    $out = @()
    foreach ($w in $words) {
        if ($w.Length -gt 1 -and $w -cmatch '^[A-Z]+$') { $out += $w } else { $out += $w.ToLowerInvariant() }
    }
    return ($out -join ' ')
}

function Escape-Xml([string]$s) {
    return ($s -replace '&', '&amp;' -replace '<', '&lt;' -replace '>', '&gt;')
}

$totalAdded = 0
$totalSkipped = 0

foreach ($project in $Projects) {
    $dir = Join-Path $root $project
    if (-not (Test-Path $dir)) { Write-Output "skip missing project: $project"; continue }

    foreach ($file in (Get-ChildItem $dir -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' })) {
        $lines = [System.IO.File]::ReadAllLines($file.FullName)
        $out = New-Object System.Collections.Generic.List[string]
        $added = 0
        $skipped = 0

        for ($i = 0; $i -lt $lines.Length; $i++) {
            $line = $lines[$i]
            if ($line -notmatch '^(\s*)public\s+(?!class\b|interface\b|enum\b|struct\b|record\b)(.+)$') {
                $out.Add($line)
                continue
            }
            $indent = $matches[1]
            $member = $matches[2].Trim()

            # Already documented? walk back over attributes and blank lines looking for ///
            $j = $i - 1
            $hasDoc = $false
            while ($j -ge 0) {
                $t = $lines[$j].Trim()
                if ($t.StartsWith('///')) { $hasDoc = $true; break }
                if ($t.StartsWith('[') -or $t -eq '') { $j--; continue }
                break
            }
            if ($hasDoc) { $out.Add($line); $skipped++; continue }

            # Harvest a description from the member body (ParameterInfo Description or SetProp)
            $description = ''
            $memberStart = '^' + [regex]::Escape($indent) + '(public|private|protected|internal)\s'
            $bodyEnd = [Math]::Min($i + 60, $lines.Length - 1)
            for ($k = $i + 1; $k -le $bodyEnd; $k++) {
                if ($lines[$k] -match $memberStart) { break }
                if ($lines[$k] -match 'Description\s*=\s*"([^"]+)"') { $description = $matches[1]; break }
                if ($lines[$k] -match 'SetProp\(\s*"[^"]+"\s*,\s*[^,]+,\s*"([^"]+)"') { $description = $matches[1]; break }
            }

            $cleaned = $member -replace '^(static\s+|readonly\s+|virtual\s+|override\s+|sealed\s+|new\s+|async\s+|unsafe\s+|const\s+|event\s+|delegate\s+|partial\s+|extern\s+)+', ''
            $name = ''
            if ($cleaned -match '^[\w<>?\[\],\.]+\s+(\w+)\s*(\{|\(|$|=>)') { $name = $matches[1] }
            elseif ($cleaned -match '^(\w+)\s*\(') { $name = $matches[1] }
            if (-not $name) { $out.Add($line); continue }

            if (-not $description) {
                if ($cleaned -match '^\w+\s*\(') {
                    $description = "Initializes a new instance of the $name class."
                } elseif ($cleaned -match '^event\b') {
                    $description = "Occurs when $((Get-HumanName $name)) changes."
                } else {
                    $description = "Gets or sets the $((Get-HumanName $name))."
                }
            }

            if (-not $WhatIf) {
                # XML doc comments must precede attributes: move any trailing attribute lines
                # that are already in the output after the new comment.
                $attrLines = New-Object System.Collections.Generic.List[string]
                while ($out.Count -gt 0 -and $out[$out.Count - 1].Trim().StartsWith('[')) {
                    $attrLines.Insert(0, $out[$out.Count - 1])
                    $out.RemoveAt($out.Count - 1)
                }
                $out.Add("$indent/// <summary>")
                $out.Add("$indent/// $(Escape-Xml $description)")
                $out.Add("$indent/// </summary>")
                foreach ($a in $attrLines) { $out.Add($a) }
            }
            $out.Add($line)
            $added++
        }

        if ($added -gt 0 -and -not $WhatIf) {
            [System.IO.File]::WriteAllLines($file.FullName, $out, $utf8)
        }
        if ($added -gt 0) { Write-Output ("{0,-52} +{1} docs ({2} already present)" -f $file.FullName.Substring($root.Length + 1), $added, $skipped) }
        $totalAdded += $added
        $totalSkipped += $skipped
    }
}

Write-Output "total: +$totalAdded comments added, $totalSkipped members already documented"

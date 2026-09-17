# Shared extraction helpers for the Help generators.
# Reads C# source and produces summaries + public member tables.

function Get-SummaryBlock([string[]]$lines, [int]$memberIdx) {
    $i = $memberIdx - 1
    $buf = New-Object System.Collections.Generic.List[string]
    while ($i -ge 0) {
        $t = $lines[$i].Trim()
        if ($t.StartsWith('///')) { $buf.Insert(0, $t.Substring(3).Trim()); $i--; continue }
        if ($t.StartsWith('[') -or $t -eq '') { $i--; continue }
        break
    }
    if ($buf.Count -eq 0) { return '' }
    $text = ($buf -join ' ')
    $text = $text -replace '<summary>', '' -replace '</summary>', ''
    $text = $text -replace '<param[^>]*>', '' -replace '</param>', ''
    $text = $text -replace '<returns>', '' -replace '</returns>', ''
    $text = $text -replace '<see cref="[^"]*?\.?(\w+)"\s*/>', '$1'
    $text = $text -replace '<see cref="[^"]*?\.?(\w+)">', '$1' -replace '</see>', ''
    $text = $text -replace '<see langword="([^"]*)"\s*/>', '$1'
    $text = $text -replace '<c>', '<code>' -replace '</c>', '</code>'
    $text = $text -replace '<[^>]+>', ''
    $text = $text -replace '\s+', ' '
    $text = $text.Replace('&', '&amp;').Replace('<', '&lt;').Replace('>', '&gt;')
    return $text.Trim()
}

function Get-HumanName([string]$name) {
    $spaced = [regex]::Replace($name, '(?<=[a-z0-9])(?=[A-Z])', ' ')
    $spaced = [regex]::Replace($spaced, '(?<=[A-Z])(?=[A-Z][a-z])', ' ')
    $words = $spaced -split ' '
    $out = @()
    foreach ($w in $words) {
        if ($w.Length -gt 1 -and $w -cmatch '^[A-Z]+$') { $out += $w }
        else { $out += $w.ToLowerInvariant() }
    }
    return ($out -join ' ')
}

function Get-TypeMembers([string]$file, [string]$typeName) {
    $lines = [System.IO.File]::ReadAllLines($file)
    $classIdx = -1
    for ($i = 0; $i -lt $lines.Length; $i++) {
        if ($lines[$i] -match "class\s+$([regex]::Escape($typeName))\b") { $classIdx = $i; break }
    }
    if ($classIdx -lt 0) { return @() }

    # Find the class body range by brace matching, starting at the declaration.
    $bodyStart = -1; $depth = 0; $bodyEnd = $lines.Length - 1
    for ($i = $classIdx; $i -lt $lines.Length; $i++) {
        foreach ($ch in $lines[$i].ToCharArray()) {
            if ($ch -eq '{') { if ($depth -eq 0 -and $bodyStart -lt 0) { $bodyStart = $i }; $depth++ }
            elseif ($ch -eq '}') { $depth--; if ($depth -eq 0 -and $bodyStart -ge 0) { $bodyEnd = $i; break } }
        }
        if ($bodyStart -ge 0 -and $depth -eq 0) { $bodyEnd = $i; break }
    }
    if ($bodyStart -lt 0) { return @() }

    $members = New-Object System.Collections.Generic.List[object]
    $skipTo = -1
    for ($i = $bodyStart + 1; $i -le $bodyEnd; $i++) {
        if ($i -le $skipTo) { continue }
        $line = $lines[$i]
        # Skip nested type declarations entirely.
        if ($line -match '^\s{8,}(public\s+|internal\s+|private\s+|protected\s+)?(sealed\s+|abstract\s+|static\s+|partial\s+)*(class|interface|enum|struct|record)\s+\w+') {
            $nd = 0; $started = $false
            for ($k = $i; $k -le $bodyEnd; $k++) {
                foreach ($ch in $lines[$k].ToCharArray()) {
                    if ($ch -eq '{') { $nd++; $started = $true }
                    elseif ($ch -eq '}') { $nd-- }
                }
                if ($started -and $nd -le 0) { $skipTo = $k; break }
            }
            continue
        }
        if ($line -match '^\s{8}public\s+(?!class\b|interface\b|enum\b|struct\b|record\b)(.+)$') {
            $sig = $matches[1].Trim()
            $depth = 0
            foreach ($ch in $sig.ToCharArray()) { if ($ch -eq '(') { $depth++ } elseif ($ch -eq ')') { $depth-- } }
            $j = $i
            while ($depth -gt 0 -and $j + 1 -le $bodyEnd) {
                $j++
                $sig += ' ' + $lines[$j].Trim()
                foreach ($ch in $lines[$j].ToCharArray()) { if ($ch -eq '(') { $depth++ } elseif ($ch -eq ')') { $depth-- } }
            }
            $sig = ($sig -split '\s*\{\s*get|\s*\{\s*set|\s*=>|\s*\{\s*$|\s*;\s*$')[0].Trim()
            $sig = $sig -replace '\s+', ' '
            $kind = 'Property'
            if ($sig -match '^(event|delegate)\b') { $kind = 'Event' }
            elseif ($sig -match '\(') { $kind = 'Method' }
            elseif ($sig -match '^const\b') { $kind = 'Constant' }
            elseif ($sig -match '^static readonly\b') { $kind = 'Field' }
            $sig = $sig -replace '^public\s+', ''
            $sig = $sig.Replace('&', '&amp;').Replace('<', '&lt;').Replace('>', '&gt;')
            $summary = Get-SummaryBlock $lines $i
            $name = ''
            if ($sig -match '([A-Za-z_]\w*)\s*(\(|\{|\s*$|\s*=)') { $name = $matches[1] }
            if (-not $summary -and $name -and $name -ne $typeName) {
                $human = Get-HumanName $name
                if ($kind -eq 'Property') { $summary = "Gets or sets the $human." }
                elseif ($kind -eq 'Event') { $summary = "Raised when $human changes." }
                elseif ($kind -eq 'Constant') { $summary = "Constant $human." }
            }
            $members.Add([pscustomobject]@{ Kind = $kind; Signature = $sig; Summary = $summary })
        }
    }
    return $members
}

function Get-TypeInfo([string]$file, [string]$typeName) {
    $lines = [System.IO.File]::ReadAllLines($file)
    for ($i = 0; $i -lt $lines.Length; $i++) {
        if ($lines[$i] -match "class\s+$([regex]::Escape($typeName))\b") {
            $summary = Get-SummaryBlock $lines $i
            if (-not $summary) {
                for ($k = $i - 1; $k -ge 0 -and $k -ge $i - 12; $k--) {
                    if ($lines[$k] -match '\[Description\("([^"]+)"\)\]') { $summary = $matches[1]; break }
                    if ($lines[$k].Trim() -ne '' -and -not $lines[$k].Trim().StartsWith('[')) { break }
                }
            }
            return [pscustomobject]@{ Summary = $summary; Members = @(Get-TypeMembers $file $typeName) }
        }
    }
    return [pscustomobject]@{ Summary = ''; Members = @() }
}


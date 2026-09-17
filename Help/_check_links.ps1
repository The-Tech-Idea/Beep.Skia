# Checks every local href/src across the Help site and reports broken targets.
$help = $PSScriptRoot
$files = Get-ChildItem -Recurse $help -File -Filter *.html
$broken = New-Object System.Collections.Generic.List[string]
$checked = 0

foreach ($f in $files) {
    $text = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    foreach ($m in [regex]::Matches($text, '(?:href|src)="([^"#:]+\.(?:html|css|svg|png|js))"')) {
        $target = $m.Groups[1].Value
        if ($target -match '^(https?:)?//') { continue }
        $resolved = [System.IO.Path]::GetFullPath((Join-Path $f.DirectoryName $target))
        $checked++
        if (-not (Test-Path -LiteralPath $resolved)) {
            $broken.Add("$($f.Name) -> $target")
        }
    }
}

Write-Output "checked: $checked references across $($files.Count) pages"
if ($broken.Count -eq 0) { Write-Output 'no broken local links' }
else { Write-Output "BROKEN ($($broken.Count)):"; $broken | Sort-Object -Unique | ForEach-Object { Write-Output "  $_" } }

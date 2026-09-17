# Generates component/host reference pages from source metadata (summaries + public members).
# ASCII-only script body: HTML entities are used for punctuation.

$ErrorActionPreference = 'Stop'
$utf8 = [System.Text.UTF8Encoding]::new($false)
$root = Split-Path -Parent $PSScriptRoot
$help = $PSScriptRoot

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

function New-Page([string]$path, [string]$title, [string]$breadcrumbLabel, [string]$subtitle, [string]$intro, [array]$sections) {
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
    [void]$sb.AppendLine('<style>.content{margin-left:0!important}</style>')
    [void]$sb.AppendLine('</head>')
    [void]$sb.AppendLine('<body>')
    [void]$sb.AppendLine('<div class="container">')
    [void]$sb.AppendLine('<main class="content"><div class="content-wrapper">')
    [void]$sb.AppendLine("<nav class=`"breadcrumb-nav`"><a href=`"../index.html`">Home</a><span>&rsaquo;</span> <span>$breadcrumbLabel</span><span>&rsaquo;</span> <span>$title</span></nav>")
    [void]$sb.AppendLine("<div class=`"page-header`"><h1>$title</h1><p class=`"page-subtitle`">$subtitle</p></div>")
    [void]$sb.AppendLine("<p>$intro</p>")

    if ($sections.Count -gt 1) {
        [void]$sb.AppendLine('<div class="toc">')
        [void]$sb.AppendLine('  <h3>On this page</h3>')
        [void]$sb.AppendLine('  <ul>')
        foreach ($s in $sections) { [void]$sb.AppendLine("    <li><a href=`"#$($s.Id)`">$($s.Heading)</a></li>") }
        [void]$sb.AppendLine('  </ul>')
        [void]$sb.AppendLine('</div>')
    }

    foreach ($s in $sections) {
        [void]$sb.AppendLine("<section class=`"section`" id=`"$($s.Id)`">")
        [void]$sb.AppendLine("  <h2>$($s.Heading)</h2>")
        if ($s.Description) { [void]$sb.AppendLine("  <p>$($s.Description)</p>") }
        if ($s.Types) {
            foreach ($t in $s.Types) {
                $info = $t.Info
                $anchor = "type-" + $t.Name.ToLowerInvariant()
                [void]$sb.AppendLine("  <h3 id=`"$anchor`">$($t.Name)</h3>")
                if ($info.Summary) { [void]$sb.AppendLine("  <p>$($info.Summary)</p>") }
                if (@($info.Members).Count -gt 0) {
                    [void]$sb.AppendLine('  <table class="property-table">')
                    [void]$sb.AppendLine('    <thead><tr><th>Member</th><th>Kind</th><th>Description</th></tr></thead>')
                    [void]$sb.AppendLine('    <tbody>')
                    foreach ($m in $info.Members) {
                        $desc = if ($m.Summary) { $m.Summary } else { '' }
                        [void]$sb.AppendLine("      <tr><td><code>$($m.Signature)</code></td><td>$($m.Kind)</td><td>$desc</td></tr>")
                    }
                    [void]$sb.AppendLine('    </tbody>')
                    [void]$sb.AppendLine('  </table>')
                }
            }
        }
        if ($s.Code) {
            [void]$sb.AppendLine('  <div class="code-example">')
            [void]$sb.AppendLine("    <h3>$($s.CodeTitle)</h3>")
            [void]$sb.AppendLine("    <pre><code class=`"language-csharp`">$($s.Code)</code></pre>")
            [void]$sb.AppendLine('  </div>')
        }
        [void]$sb.AppendLine('</section>')
    }

    [void]$sb.AppendLine('</div></main></div>')
    [void]$sb.AppendLine('<script src="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-core.min.js"></script>')
    [void]$sb.AppendLine('<script src="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/plugins/autoloader/prism-autoloader.min.js"></script>')
    [void]$sb.AppendLine('</body></html>')
    [System.IO.File]::WriteAllText($path, $sb.ToString(), $utf8)
    Write-Output "wrote $path"
}

function New-TypeSection([string]$id, [string]$heading, [string]$description, [array]$typeSpecs) {
    $types = @()
    foreach ($spec in $typeSpecs) {
        $file = Join-Path $root $spec.File
        $info = Get-TypeInfo $file $spec.Name
        $types += [pscustomobject]@{ Name = $spec.Name; Info = $info }
    }
    return [pscustomobject]@{ Id = $id; Heading = $heading; Description = $description; Types = $types }
}

$comp = 'Beep.Skia\Components'

# ------------------------------------------------------------------ inputs
New-Page (Join-Path $help 'ui-components\inputs.html') 'Input Components' 'UI Components' `
    'Color, date and time pickers, search, switch, spinner, text area and grouped choice controls' `
    'Beyond the basic <a href="textbox.html">TextBox</a>, <a href="checkbox.html">Checkbox</a> and <a href="dropdown.html">Dropdown</a> controls, Beep.Skia ships pickers, search, switches and grouped selection controls. All of them are <code>MaterialControl</code> components, so they participate in theming, hit testing and the property grid.' `
    @(
        (New-TypeSection 'pickers' 'Pickers' 'Color, date and time selection controls.' @(
            @{ Name = 'ColorPicker'; File = "$comp\ColorPicker.cs" }
            @{ Name = 'DatePicker'; File = "$comp\DatePicker.cs" }
            @{ Name = 'TimePicker'; File = "$comp\TimePicker.cs" }
        )),
        (New-TypeSection 'search' 'Search' 'Search box with suggestions.' @(
            @{ Name = 'Search'; File = "$comp\Search.cs" }
            @{ Name = 'SearchSuggestion'; File = "$comp\Search.cs" }
            @{ Name = 'SearchEventArgs'; File = "$comp\Search.cs" }
        )),
        (New-TypeSection 'toggles' 'Toggles and Progress' 'Switch, spinner and progress indicators.' @(
            @{ Name = 'Switch'; File = "$comp\Switch.cs" }
            @{ Name = 'Spinner'; File = "$comp\Spinner.cs" }
            @{ Name = 'ProgressBar'; File = "$comp\ProgressBar.cs" }
        )),
        (New-TypeSection 'text' 'Text Input' 'Multi-line text input.' @(
            @{ Name = 'TextArea'; File = "$comp\TextArea.cs" }
        )),
        (New-TypeSection 'groups' 'Grouped Choices' 'Checkbox, radio and segmented button groups.' @(
            @{ Name = 'CheckBoxGroup'; File = "$comp\CheckBoxGroup.cs" }
            @{ Name = 'RadioGroup'; File = "$comp\RadioGroup.cs" }
            @{ Name = 'SegmentedButtons'; File = "$comp\SegmentedButtons.cs" }
        ))
    )

# ------------------------------------------------------------------ buttons
New-Page (Join-Path $help 'ui-components\buttons.html') 'Button Family' 'UI Components' `
    'ButtonGroup, floating action button, FAB menu, split button and menu button' `
    'The <a href="button.html">Button</a> page covers the base control. This page documents the rest of the button family: grouped buttons, floating action buttons with expandable menus, split buttons and menu buttons.' `
    @(
        (New-TypeSection 'groups' 'Button Groups' 'Segmented rows of related actions.' @(
            @{ Name = 'ButtonGroup'; File = "$comp\ButtonGroup.cs" }
        )),
        (New-TypeSection 'fab' 'Floating Action Buttons' 'Primary action buttons with expandable menus.' @(
            @{ Name = 'FloatingActionButton'; File = "$comp\FloatingActionButton.cs" }
            @{ Name = 'FabMenu'; File = "$comp\FabMenu.cs" }
            @{ Name = 'FabMenuItem'; File = "$comp\FabMenu.cs" }
        )),
        (New-TypeSection 'split' 'Split and Menu Buttons' 'Buttons that combine a primary action with a menu.' @(
            @{ Name = 'SplitButton'; File = "$comp\SplitButton.cs" }
            @{ Name = 'MenuButton'; File = "$comp\MenuButton.cs" }
        ))
    )

# ------------------------------------------------------------------ navigation
New-Page (Join-Path $help 'ui-components\navigation.html') 'Navigation &amp; Menus' 'UI Components' `
    'Menu bars, cascading menus, context menus, navigation bars and drawers, status bars, panels and swimlanes' `
    'Navigation controls arrange the application shell and diagram structure. The <a href="menu.html">Menu</a> and <a href="tabs.html">Tabs</a> pages cover those controls; this page documents the surrounding family.' `
    @(
        (New-TypeSection 'menus' 'Menus' 'Menu bars, cascading menus and context menus.' @(
            @{ Name = 'MenuBar'; File = "$comp\MenuBar.cs" }
            @{ Name = 'CascadingMenu'; File = "$comp\CascadingMenu.cs" }
            @{ Name = 'ContextMenu'; File = "$comp\ContextMenu.cs" }
        )),
        (New-TypeSection 'drawer' 'Navigation Bars and Drawers' 'Primary navigation surfaces.' @(
            @{ Name = 'NavigationBar'; File = "$comp\NavigationBar.cs" }
            @{ Name = 'NavigationDrawer'; File = "$comp\NavigationDrawer.cs" }
            @{ Name = 'NavigationItem'; File = "$comp\NavigationItem.cs" }
        )),
        (New-TypeSection 'status' 'Status Bars' 'Status reporting along the bottom edge.' @(
            @{ Name = 'StatusBar'; File = "$comp\StatusBar.cs" }
            @{ Name = 'StatusBarItem'; File = "$comp\StatusBarItem.cs" }
        )),
        (New-TypeSection 'containers' 'Containers' 'Panels and swimlanes for grouping components.' @(
            @{ Name = 'Panel'; File = "$comp\Panel.cs" }
            @{ Name = 'SwimlaneContainer'; File = "$comp\SwimlaneContainer.cs" }
        ))
    )

# ------------------------------------------------------------------ display
New-Page (Join-Path $help 'ui-components\display.html') 'Display Components' 'UI Components' `
    'Labels, SVG images and the minimap overview control' `
    'Display components render static or derived content: text labels, vector artwork and the minimap overview used by large diagrams.' `
    @(
        (New-TypeSection 'label' 'Label' 'Simple text display.' @(
            @{ Name = 'Label'; File = "$comp\Label.cs" }
        )),
        (New-TypeSection 'svg' 'SVG Image' 'Vector artwork rendered from SVG markup.' @(
            @{ Name = 'SvgImage'; File = "$comp\SvgImage.cs" }
        )),
        (New-TypeSection 'minimap' 'Minimap' 'Scaled overview of the diagram with a viewport rectangle.' @(
            @{ Name = 'MinimapControl'; File = "$comp\MinimapControl.cs" }
        ))
    )

# ------------------------------------------------------------------ editors
New-Page (Join-Path $help 'ui-components\editors.html') 'Property Editors' 'UI Components' `
    'NodePropertyEditor, ComponentPropertyEditor and the grid that backs them' `
    'Property editors render the editable surface for components and connection lines. They read <code>NodeProperties</code> and write values back through <code>SetProperties</code>, which is the same path used by serialization.' `
    @(
        (New-TypeSection 'node' 'NodePropertyEditor' 'Editor for a single component.' @(
            @{ Name = 'NodePropertyEditor'; File = "$comp\NodePropertyEditor.cs" }
        )),
        (New-TypeSection 'component' 'ComponentPropertyEditor' 'Editor for components and connection lines, with type-specific editors.' @(
            @{ Name = 'ComponentPropertyEditor'; File = "$comp\ComponentPropertyEditor.cs" }
            @{ Name = 'SkiaComponentGrid'; File = "$comp\ComponentPropertyEditor.cs" }
        ))
    )

# ------------------------------------------------------------------ lists
New-Page (Join-Path $help 'ui-components\lists.html') 'List Controls' 'UI Components' `
    'List, ListBoxItem, ListItem and ComboBoxItem' `
    'List controls present selectable rows. The <a href="dropdown.html">Dropdown</a> and <a href="datagrid.html">DataGrid</a> pages cover those controls; this page documents the list primitives.' `
    @(
        (New-TypeSection 'list' 'List' 'Scrollable list of items.' @(
            @{ Name = 'List'; File = "$comp\List.cs" }
        )),
        (New-TypeSection 'items' 'List Items' 'Row primitives used by lists, dropdowns and combo boxes.' @(
            @{ Name = 'ListBoxItem'; File = "$comp\ListBoxItem.cs" }
            @{ Name = 'ListItem'; File = "$comp\ListItem.cs" }
            @{ Name = 'ComboBoxItem'; File = "$comp\ComboBoxItem.cs" }
        ))
    )

Write-Output 'component pages done'

# ------------------------------------------------------------------ automation nodes
New-Page (Join-Path $help 'automation\nodes.html') 'Automation Nodes' 'Automation' `
    'The executable node types the workflow engine runs, plus the trigger components' `
    'Workflow nodes derive from <code>AutomationNode</code>, which gives them typed input/output ports, execution state and a parameter set that the engine can drive. Drag them like any other component; the engine picks them up when the diagram is executed. See <a href="workflow-engine.html">Workflow Engine</a> for the execution model.' `
    @(
        (New-TypeSection 'base' 'Base Node' 'Common behavior for every executable node.' @(
            @{ Name = 'AutomationNode'; File = "$comp\AutomationNode.cs" }
        )),
        (New-TypeSection 'triggers' 'Trigger Nodes' 'Start executions from user actions or schedules.' @(
            @{ Name = 'ManualTriggerNode'; File = "$comp\ManualTriggerNode.cs" }
            @{ Name = 'TimerTriggerNode'; File = "$comp\TimerTriggerNode.cs" }
        )),
        (New-TypeSection 'data' 'Data Nodes' 'Read, transform and emit data.' @(
            @{ Name = 'DataInputNode'; File = "$comp\DataInputNode.cs" }
            @{ Name = 'DataTransformNode'; File = "$comp\DataTransformNode.cs" }
            @{ Name = 'DataSourceAutomationNode'; File = "$comp\DataSourceAutomationNode.cs" }
        )),
        (New-TypeSection 'logic' 'Logic Nodes' 'Branching and external calls.' @(
            @{ Name = 'ConditionalNode'; File = "$comp\ConditionalNode.cs" }
            @{ Name = 'HttpRequestNode'; File = "$comp\HttpRequestNode.cs" }
        ))
    )

# ------------------------------------------------------------------ winforms host
$wf = 'Beep.Skia.Winform.Controls'
New-Page (Join-Path $help 'hosts\winforms.html') 'WinForms Controls' 'Windows Hosts' `
    'The SkiaHostControl canvas host plus a WinForms wrapper for every Material control' `
    'The WinForms package ships two layers: <code>SkiaHostControl</code>, the full diagram editor surface (canvas, palette, property grid, export), and a wrapper control for every Material component so the same visuals can be used in ordinary WinForms layouts. All wrappers derive from <code>SkiaControl</code>.' `
    @(
        (New-TypeSection 'host' 'Canvas Host' 'The complete diagram editing surface.' @(
            @{ Name = 'SkiaHostControl'; File = "$wf\SkiaHostControl.cs" }
        )),
        (New-TypeSection 'base' 'Control Base' 'Common base for every wrapper control.' @(
            @{ Name = 'SkiaControl'; File = "$wf\SkiaControl.cs" }
        )),
        (New-TypeSection 'input' 'Input Wrappers' 'WinForms wrappers for the input components.' @(
            @{ Name = 'SkiaTextBox'; File = "$wf\SkiaTextBox.cs" }
            @{ Name = 'SkiaTextArea'; File = "$wf\SkiaTextArea.cs" }
            @{ Name = 'SkiaCheckBox'; File = "$wf\SkiaCheckbox.cs" }
            @{ Name = 'SkiaComboBox'; File = "$wf\SkiaComboBox.cs" }
            @{ Name = 'SkiaDatePicker'; File = "$wf\SkiaDatePicker.cs" }
            @{ Name = 'SkiaSearch'; File = "$wf\SkiaSearch.cs" }
            @{ Name = 'SkiaSlider'; File = "$wf\SkiaSlider.cs" }
            @{ Name = 'SkiaSwitch'; File = "$wf\SkiaSwitch.cs" }
            @{ Name = 'SkiaToggleButton'; File = "$wf\SkiaToggleButton.cs" }
        )),
        (New-TypeSection 'actions' 'Action Wrappers' 'Buttons and button groups.' @(
            @{ Name = 'SkiaButton'; File = "$wf\SkiaButton.cs" }
            @{ Name = 'SkiaSplitButton'; File = "$wf\SkiaSplitButton.cs" }
            @{ Name = 'SkiaFloatingActionButton'; File = "$wf\SkiaFloatingActionButton.cs" }
            @{ Name = 'SkiaFabMenu'; File = "$wf\SkiaFabMenu.cs" }
            @{ Name = 'SkiaSegmentedButtons'; File = "$wf\SkiaSegmentedButtons.cs" }
            @{ Name = 'SkiaMenuButton'; File = "$wf\SkiaMenuButton.cs" }
        )),
        (New-TypeSection 'navigation' 'Navigation Wrappers' 'Menus, drawers and status bars.' @(
            @{ Name = 'SkiaMenu'; File = "$wf\SkiaMenu.cs" }
            @{ Name = 'SkiaMenuBar'; File = "$wf\SkiaMenuBar.cs" }
            @{ Name = 'SkiaCascadingMenu'; File = "$wf\SkiaCascadingMenu.cs" }
            @{ Name = 'SkiaContextMenu'; File = "$wf\SkiaContextMenu.cs" }
            @{ Name = 'SkiaNavigationBar'; File = "$wf\SkiaNavigationBar.cs" }
            @{ Name = 'SkiaNavigationDrawer'; File = "$wf\SkiaNavigationDrawer.cs" }
            @{ Name = 'SkiaStatusBar'; File = "$wf\SkiaStatusBar.cs" }
            @{ Name = 'SkiaToolBar'; File = "$wf\SkiaToolBar.cs" }
        )),
        (New-TypeSection 'display' 'Display Wrappers' 'Cards, panels, labels and indicators.' @(
            @{ Name = 'SkiaCard'; File = "$wf\SkiaCard.cs" }
            @{ Name = 'SkiaPanel'; File = "$wf\SkiaPanel.cs" }
            @{ Name = 'SkiaLabel'; File = "$wf\SkiaLabel.cs" }
            @{ Name = 'SkiaProgressBar'; File = "$wf\SkiaProgressBar.cs" }
            @{ Name = 'SkiaSpinner'; File = "$wf\SkiaSpinner.cs" }
            @{ Name = 'SkiaSvgImage'; File = "$wf\SkiaSvgImage.cs" }
            @{ Name = 'SkiaList'; File = "$wf\SkiaList.cs" }
            @{ Name = 'SkiaDataGrid'; File = "$wf\SkiaDataGrid.cs" }
            @{ Name = 'SkiaTabs'; File = "$wf\SkiaTabs.cs" }
        )),
        (New-TypeSection 'designer' 'Designer Support' 'Design-time integration and property wrappers.' @(
            @{ Name = 'SkiaControlDesigner'; File = "$wf\SkiaControlDesigner.cs" }
            @{ Name = 'SkiaHostControlDesigner'; File = "$wf\SkiaHostControlDesigner.cs" }
            @{ Name = 'SkiaComponentPropertyWrapper'; File = "$wf\SkiaComponentPropertyWrapper.cs" }
            @{ Name = 'SkiaMultiComponentWrapper'; File = "$wf\SkiaComponentPropertyWrapper.cs" }
            @{ Name = 'SkiaNodePropertyDescriptor'; File = "$wf\SkiaComponentPropertyWrapper.cs" }
            @{ Name = 'SkiaColorConverter'; File = "$wf\SkiaComponentPropertyWrapper.cs" }
            @{ Name = 'SkiaEnumConverter'; File = "$wf\SkiaComponentPropertyWrapper.cs" }
        ))
    )

# ------------------------------------------------------------------ events
New-Page (Join-Path $help 'architecture\events.html') 'Event Model' 'Architecture &amp; Internals' `
    'The event argument types raised by the manager, the interaction helper and the host' `
    'Beep.Skia reports activity through plain .NET events. The manager raises diagram-level events (<code>ComponentDropped</code>, <code>DrawSurface</code>, <code>SelectionChanged</code> and theme changes), while the interaction helper raises pointer and hover events. The event argument types below carry the payloads.' `
    @(
        (New-TypeSection 'pointer' 'Pointer Events' 'Click and hover payloads.' @(
            @{ Name = 'ComponentClickEventArgs'; File = 'Beep.Skia\Events\DiagramEventArgs.cs' }
            @{ Name = 'DiagramClickEventArgs'; File = 'Beep.Skia\Events\DiagramEventArgs.cs' }
            @{ Name = 'LineClickEventArgs'; File = 'Beep.Skia\Events\DiagramEventArgs.cs' }
            @{ Name = 'HoverChangedEventArgs'; File = 'Beep.Skia\Events\DiagramEventArgs.cs' }
        )),
        (New-TypeSection 'drop' 'Drag and Drop Events' 'Payload raised when a component drag ends.' @(
            @{ Name = 'ComponentDropEventArgs'; File = 'Beep.Skia\Events\ComponentDropEventArgs.cs' }
        ))
    )

# ------------------------------------------------------------------ api index
$projects = @(
    @{ Name = 'Beep.Skia'; Dir = 'Beep.Skia'; Page = 'reference/beep-skia.html' }
    @{ Name = 'Beep.Skia.Model'; Dir = 'Beep.Skia.Model'; Page = 'reference/beep-skia-model.html' }
    @{ Name = 'Beep.Skia.FlowChart'; Dir = 'Beep.Skia.FlowChart'; Page = 'reference/beep-skia-flowchart.html' }
    @{ Name = 'Beep.Skia.Business'; Dir = 'Beep.Skia.Business'; Page = 'reference/beep-skia-business.html' }
    @{ Name = 'Beep.Skia.ERD'; Dir = 'Beep.Skia.ERD'; Page = 'reference/beep-skia-erd.html' }
    @{ Name = 'Beep.Skia.DFD'; Dir = 'Beep.Skia.DFD'; Page = 'reference/beep-skia-dfd.html' }
    @{ Name = 'Beep.Skia.ETL'; Dir = 'Beep.Skia.ETL'; Page = 'reference/beep-skia-etl.html' }
    @{ Name = 'Beep.Skia.UML'; Dir = 'Beep.Skia.UML'; Page = 'reference/beep-skia-uml.html' }
    @{ Name = 'Beep.Skia.Network'; Dir = 'Beep.Skia.Network'; Page = 'reference/beep-skia-network.html' }
    @{ Name = 'Beep.Skia.PM'; Dir = 'Beep.Skia.PM'; Page = 'reference/beep-skia-pm.html' }
    @{ Name = 'Beep.Skia.MindMap'; Dir = 'Beep.Skia.MindMap'; Page = 'reference/beep-skia-mindmap.html' }
    @{ Name = 'Beep.Skia.StateMachine'; Dir = 'Beep.Skia.StateMachine'; Page = 'reference/beep-skia-statemachine.html' }
    @{ Name = 'Beep.Skia.ECAD'; Dir = 'Beep.Skia.ECAD'; Page = 'reference/beep-skia-ecad.html' }
    @{ Name = 'Beep.Skia.Cloud'; Dir = 'Beep.Skia.Cloud'; Page = 'reference/beep-skia-cloud.html' }
    @{ Name = 'Beep.Skia.Security'; Dir = 'Beep.Skia.Security'; Page = 'reference/beep-skia-security.html' }
    @{ Name = 'Beep.Skia.ML'; Dir = 'Beep.Skia.ML'; Page = 'reference/beep-skia-ml.html' }
    @{ Name = 'Beep.Ski.Quantitative'; Dir = 'Beep.Ski.Quantitative'; Page = 'reference/beep-ski-quantitative.html' }
    @{ Name = 'Beep.Skia.WellLogs'; Dir = 'Beep.Skia.WellLogs'; Page = 'reference/beep-skia-welllogs.html' }
    @{ Name = 'Beep.Skia.Winform.Controls'; Dir = 'Beep.Skia.Winform.Controls'; Page = 'reference/beep-skia-winform-controls.html' }
    @{ Name = 'Beep.Skia.Wpf.Controls'; Dir = 'Beep.Skia.Wpf.Controls'; Page = 'reference/beep-skia-wpf-controls.html' }
    @{ Name = 'Beep.Skia.Blazor.Controls'; Dir = 'Beep.Skia.Blazor.Controls'; Page = 'reference/beep-skia-blazor-controls.html' }
    @{ Name = 'Beep.Skia.Maui.Controls'; Dir = 'Beep.Skia.Maui.Controls'; Page = 'reference/beep-skia-maui-controls.html' }
    @{ Name = 'Beep.Skia.Avalonia.Controls'; Dir = 'Beep.Skia.Avalonia.Controls'; Page = 'reference/beep-skia-avalonia-controls.html' }
    @{ Name = 'Beep.Skia.Loader'; Dir = 'Beep.Skia.Loader'; Page = 'reference/beep-skia-loader.html' }
)

$indexSections = @()
foreach ($p in $projects) {
    $dir = Join-Path $root $p.Dir
    if (-not (Test-Path $dir)) { continue }
    $types = rg -o "public (sealed |abstract |partial |static )*(class|interface|enum|struct) (\w+)" -r '$3' $dir --glob '*.cs' 2>$null | Sort-Object -Unique
    $rows = New-Object System.Collections.Generic.List[string]
    foreach ($t in $types) {
        $rows.Add("<li><code>$t</code></li>")
    }
    $indexSections += [pscustomobject]@{
        Id = ($p.Name -replace '[^A-Za-z0-9]', '-').ToLowerInvariant()
        Heading = $p.Name
        Description = "<a href=`"$(($p.Page -replace '^reference/',''))`">Full API reference for this assembly</a>"
        Types = $null
        Code = $null
        CodeTitle = $null
        Html = ($rows -join "`n")
    }
}

# The API index needs raw HTML lists, so render it directly.
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('<!DOCTYPE html>')
[void]$sb.AppendLine('<html lang="en">')
[void]$sb.AppendLine('<head>')
[void]$sb.AppendLine('<meta charset="UTF-8">')
[void]$sb.AppendLine('<meta name="viewport" content="width=device-width, initial-scale=1.0">')
[void]$sb.AppendLine('<title>API Index | Beep.Skia Documentation</title>')
[void]$sb.AppendLine('<link rel="stylesheet" href="../sphinx-style.css">')
[void]$sb.AppendLine('<link rel="preconnect" href="https://fonts.googleapis.com">')
[void]$sb.AppendLine('<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>')
[void]$sb.AppendLine('<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">')
[void]$sb.AppendLine('<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.5/font/bootstrap-icons.css">')
[void]$sb.AppendLine('<style>.content{margin-left:0!important} .api-list{columns:3;column-gap:2rem;list-style:none;padding-left:0} .api-list li{margin:0.15rem 0} @media(max-width:900px){.api-list{columns:1}}</style>')
[void]$sb.AppendLine('</head>')
[void]$sb.AppendLine('<body>')
[void]$sb.AppendLine('<div class="container">')
[void]$sb.AppendLine('<main class="content"><div class="content-wrapper">')
[void]$sb.AppendLine('<nav class="breadcrumb-nav"><a href="../index.html">Home</a><span>&rsaquo;</span> <span>API Index</span></nav>')
[void]$sb.AppendLine('<div class="page-header"><h1>API Index</h1><p class="page-subtitle">Public classes, interfaces, enums and structs by project</p></div>')
[void]$sb.AppendLine('<p>Every public type shipped in the solution, grouped by project. Follow the project link for the guide that documents it in depth; component families are documented on their family pages, and the core engine on the core concepts pages.</p>')
[void]$sb.AppendLine('<div class="toc"><h3>On this page</h3><ul>')
foreach ($s in $indexSections) { [void]$sb.AppendLine("  <li><a href=`"#$($s.Id)`">$($s.Heading)</a></li>") }
[void]$sb.AppendLine('</ul></div>')
foreach ($s in $indexSections) {
    [void]$sb.AppendLine("<section class=`"section`" id=`"$($s.Id)`">")
    [void]$sb.AppendLine("  <h2>$($s.Heading)</h2>")
    [void]$sb.AppendLine("  <p>$($s.Description)</p>")
    [void]$sb.AppendLine('  <ul class="api-list">')
    [void]$sb.AppendLine($s.Html)
    [void]$sb.AppendLine('  </ul>')
    [void]$sb.AppendLine('</section>')
}
[void]$sb.AppendLine('</div></main></div>')
[void]$sb.AppendLine('</body></html>')
[System.IO.File]::WriteAllText((Join-Path $help 'reference\api-index.html'), $sb.ToString(), $utf8)
Write-Output 'wrote API index'

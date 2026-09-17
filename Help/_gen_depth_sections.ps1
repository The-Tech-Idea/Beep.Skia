# Appends the missing depth sections to the remaining thin pages.
$ErrorActionPreference = 'Stop'
$utf8 = [System.Text.UTF8Encoding]::new($false)
$help = $PSScriptRoot

function Add-Section([string]$file, [string]$section) {
    $path = Join-Path $help $file
    $text = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
    $probe = $section.Trim().Substring(0, 60)
    if ($text.Contains($probe)) { Write-Output "skip (present): $file"; return }
    $marker = '</div></main></div>'
    $idx = $text.LastIndexOf($marker)
    if ($idx -lt 0) { $marker = '</div></main>'; $idx = $text.LastIndexOf($marker) }
    if ($idx -lt 0) { Write-Output "FAILED (no marker): $file"; return }
    [System.IO.File]::WriteAllText($path, $text.Substring(0, $idx) + $section + $text.Substring($idx), $utf8)
    Write-Output "updated $file"
}

$events = @'
<section class="section" id="raising">
  <h2>How Events Are Raised</h2>
  <ul>
    <li><strong>Thread:</strong> every manager event is raised on the thread that delivers input (the UI thread in desktop hosts), so handlers may touch UI state directly.</li>
    <li><strong>Order:</strong> for one gesture, the interaction event fires before the resulting lifecycle event (for example <code>ComponentDoubleClicked</code> before any <code>SelectionChanged</code> caused by the click).</li>
    <li><strong>Handled:</strong> setting <code>e.Handled = true</code> marks the interaction as consumed; the default behaviour (for example clearing the selection) is skipped.</li>
    <li><strong>Exceptions:</strong> an exception thrown by a handler is the host's responsibility; the manager does not catch them, so keep handlers short and defensive.</li>
  </ul>

  <h3>Choosing the Right Arguments</h3>
  <table>
    <thead><tr><th>You want</th><th>Use</th></tr></thead>
    <tbody>
      <tr><td>Pointer interaction with a component, line or empty canvas</td><td>The interaction arguments on this page (<code>ComponentInteractionEventArgs</code>, <code>LineInteractionEventArgs</code>, <code>DiagramInteractionEventArgs</code>)</td></tr>
      <tr><td>A component drag finishing</td><td><code>ComponentDropEventArgs</code> with canvas and screen positions plus final bounds</td></tr>
      <tr><td>Component state transitions (selected, disabled, disposing)</td><td><code>ComponentStateChangedEventArgs</code> from the component's <code>StateChanged</code> event</td></tr>
      <tr><td>Structural changes (nodes or lines added or removed)</td><td><code>NodeAddedEventArgs</code>, <code>NodeRemovedEventArgs</code>, <code>ConnectionCreatedEventArgs</code>, <code>ConnectionRemovedEventArgs</code></td></tr>
      <tr><td>Selection changes</td><td>The manager's <code>SelectionChanged</code> event, or <code>NodeSelectedEventArgs</code> for a single node</td></tr>
      <tr><td>A simpler, flat payload for host code</td><td>The legacy <code>Beep.Skia.Events</code> types (<code>ComponentClickEventArgs</code>, <code>LineClickEventArgs</code>, <code>DiagramClickEventArgs</code>, <code>HoverChangedEventArgs</code>)</td></tr>
    </tbody>
  </table>
  <p>The complete event catalog &mdash; all sixteen manager events, their arguments and handling examples &mdash; is on the <a href="event-catalog.html">Event Catalog</a> page.</p>
</section>
'@
Add-Section 'architecture\events.html' $events

$extensions = @'
<section class="section" id="lifecycle">
  <h2>Extension Lifecycle</h2>
  <p>The host drives every extension through the same sequence:</p>
  <ol>
    <li><strong>Discover</strong> &mdash; assemblies are scanned for types implementing <code>ISkiaExtension</code>.</li>
    <li><strong>Instantiate</strong> &mdash; each extension type is created with its parameterless constructor.</li>
    <li><strong>Initialize</strong> &mdash; <code>Initialize(context)</code> runs once; register components, commands and diagnostics here.</li>
    <li><strong>Register</strong> &mdash; <code>RegisterComponent</code> adds the type to the registry with an optional palette category and display name; <code>RegisterCommand</code> adds a named action.</li>
    <li><strong>Create</strong> &mdash; components are created on demand through the registry or <code>SkiaExtensionHost.CreateComponent</code>.</li>
    <li><strong>Clear</strong> &mdash; <code>Clear()</code> drops the loaded extensions and their registrations (host shutdown or tests).</li>
  </ol>

  <table class="property-table">
    <thead><tr><th>Host member</th><th>Description</th></tr></thead>
    <tbody>
      <tr><td><code>LoadFromAssemblies(assemblies)</code></td><td>Loads every extension found in the given assemblies; returns how many were loaded.</td></tr>
      <tr><td><code>LoadFromDirectory(directory, pattern)</code></td><td>Scans a folder for plugin assemblies (default <code>*.dll</code>).</td></tr>
      <tr><td><code>CreateComponent(assemblyQualifiedName)</code></td><td>Creates an instance of a contributed component type.</td></tr>
      <tr><td><code>InvokeCommand(name, target)</code></td><td>Runs a command registered by an extension against the current selection.</td></tr>
      <tr><td><code>Clear()</code></td><td>Unloads extensions and clears registrations.</td></tr>
    </tbody>
  </table>
</section>

<section class="section" id="packaging">
  <h2>Packaging and Distribution</h2>
  <p>Extensions ship as <code>.beepkg</code> packages and are installed through the marketplace, which tracks versions and provenance:</p>
  <div class="code-example">
    <pre><code class="language-csharp">var packages = new ExtensionPackageManager(extensionsRoot);

var result = packages.Install(@"C:\downloads\acme.pnid-1.0.0.beepkg");
if (!result.Success)
    Console.WriteLine(result.Error);

var host = new SkiaExtensionHost();
foreach (var dir in packages.GetLoadDirectories())
    host.LoadFromDirectory(dir);

// Later: upgrade or remove
packages.Update("com.acme.beep.pnid");
packages.Uninstall("com.acme.beep.pnid");</code></pre>
  </div>
  <p>Installation validates the archive (path traversal and decompression-bomb protection), package version compatibility and declared dependencies before anything is copied. See the <a href="marketplace.html">Extension Marketplace</a> page for the package format.</p>
</section>

<section class="section" id="checklist">
  <h2>Extension Checklist</h2>
  <ul>
    <li>Stable reverse-DNS <code>Id</code>, semantic <code>Version</code>, meaningful <code>Name</code> and <code>Description</code>.</li>
    <li>Every component type derives from <code>SkiaComponent</code> and has a public parameterless constructor.</li>
    <li><code>GetComponentTypes()</code> and <code>RegisterComponent</code> agree &mdash; the first is what hosts scan, the second is what the palette shows.</li>
    <li>Component class names never change after release; saved diagrams resolve types by name.</li>
    <li>Commands are idempotent and safe to call with an empty selection.</li>
    <li>Use <code>context.Log</code> for diagnostics instead of writing to the console.</li>
  </ul>
</section>
'@
Add-Section 'ecosystem\extensions.html' $extensions

$editors = @'
<section class="section" id="usage">
  <h2>Using the Editors</h2>
  <p>Both editors are ordinary components: place them on the canvas (or host them in a panel), point them at a selection, and react to their save/cancel events. Changes are applied through <code>SetPropperties</code>, the same path serialization uses, so values round-trip identically.</p>

  <h3>NodePropertyEditor</h3>
  <table class="property-table">
    <thead><tr><th>Member</th><th>Type</th><th>Description</th></tr></thead>
    <tbody>
      <tr><td><code>Node</code></td><td><code>AutomationNode</code></td><td>The node whose <code>NodeProperties</code> are edited.</td></tr>
      <tr><td><code>PropertiesSaved</code></td><td><code>event</code></td><td>Raised with the edited node after Apply; use it to push undo or persist.</td></tr>
      <tr><td><code>PropertiesCancelled</code></td><td><code>event</code></td><td>Raised when the edit is discarded.</td></tr>
      <tr><td><code>ShowInPalette</code></td><td><code>bool</code></td><td>False by default &mdash; the editor is a tool surface, not a palette item.</td></tr>
    </tbody>
  </table>

  <h3>ComponentPropertyEditor</h3>
  <table class="property-table">
    <thead><tr><th>Member</th><th>Type</th><th>Description</th></tr></thead>
    <tbody>
      <tr><td><code>SelectedComponent</code></td><td><code>SkiaComponent</code></td><td>Gets or sets the component under edit.</td></tr>
      <tr><td><code>PropertiesSaved</code> / <code>PropertiesCancelled</code></td><td><code>event</code></td><td>Apply and discard notifications, both carrying the component.</td></tr>
      <tr><td><code>PropertyValueChanged</code></td><td><code>event</code></td><td>Raised as individual values change, for live previews.</td></tr>
    </tbody>
  </table>

  <p>The editor also handles connection lines: when a line is selected it exposes line-specific editors (labels, routing mode, arrowheads, ERD multiplicity, data-flow animation), which is why the same control serves the whole editor shell.</p>

  <div class="code-example">
    <pre><code class="language-csharp">var editor = new ComponentPropertyEditor();
editor.PropertiesSaved += (s, e) =&gt;
{
    // The component already has the new values applied
    drawingManager.RequestRedraw();
};

editor.SelectedComponent = drawingManager.SelectionManager.SelectedComponents.FirstOrDefault();</code></pre>
  </div>
</section>

<section class="section" id="grid">
  <h2>SkiaComponentGrid</h2>
  <p><code>SkiaComponentGrid</code> is the editable table used by the ETL and ERD editors for list-shaped data (columns, mappings, rules). It is constructed with a column model and a change callback, so hosts can embed it without reimplementing table editing:</p>
  <div class="code-example">
    <pre><code class="language-csharp">var grid = new SkiaComponentGrid(
    columns: new List&lt;ColumnDefinition&gt; { new ColumnDefinition { Name = "Source" }, new ColumnDefinition { Name = "Target" } },
    onChanged: cols =&gt; ApplyMappings(cols),
    extraTypeChoices: new[] { "string", "number", "date" });</code></pre>
  </div>
</section>
'@
Add-Section 'ui-components\editors.html' $editors

Write-Output 'thin-page sections done'

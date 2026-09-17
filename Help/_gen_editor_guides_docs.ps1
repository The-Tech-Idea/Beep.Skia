# Generates the Editor UX page plus the Platforms and Testing/Quality guides.

$template = @'
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>{TITLE} | Beep.Skia Documentation</title>
<link rel="stylesheet" href="../sphinx-style.css">
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/themes/prism-tomorrow.min.css">
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.5/font/bootstrap-icons.css">
<style>.content{margin-left:0!important}</style>
</head>
<body>
<div class="container">
<main class="content"><div class="content-wrapper">
<nav class="breadcrumb-nav"><a href="../index.html">Home</a><span>&rsaquo;</span> <a href="{SECTION_LINK}">{SECTION}</a><span>&rsaquo;</span> <span>{TITLE}</span></nav>
<div class="page-header"><h1>{TITLE}</h1><p class="page-subtitle">{SUBTITLE}</p></div>
{CONTENT}
</div></main></div>
<script src="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-core.min.js"></script>
<script src="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/plugins/autoloader/prism-autoloader.min.js"></script>
</body></html>
'@

$pages = @()

$pages += @{ File = 'Help/editor/editor-ux.html'; Title = 'Editor Experience'; Subtitle = 'Palette search, minimap, property editing, templates and accessibility'; Section = 'Editor'; SectionLink = 'editor-ux.html'; Content = @'
<p>These features are what make the canvas usable on real projects: finding the component you want, keeping your bearings,
editing properties without code, starting from a template, and operating the editor from the keyboard.</p>

<div class="toc">
  <h3>Table of Contents</h3>
  <ul>
    <li><a href="#search">Palette Search</a></li>
    <li><a href="#minimap">Minimap</a></li>
    <li><a href="#property-grid">Property Editing</a></li>
    <li><a href="#templates">Templates</a></li>
    <li><a href="#accessibility">Accessibility</a></li>
  </ul>
</div>

<section class="section" id="search">
  <h2>Palette Search</h2>
  <p>The palette filters as you type, matching both component names and categories, and reports how many entries remain
  visible. Items added after the first render (for example by installing an extension) are picked up immediately.</p>
  <div class="code-example">
    <pre><code class="language-csharp">palette.SearchText = "decision";
Console.WriteLine($"{palette.VisibleItemCount} component(s) match");

palette.AddItem(new PaletteItem { Name = "Acme Gauge", Category = "Acme" });   // appears at once</code></pre>
  </div>
</section>

<section class="section" id="minimap">
  <h2>Minimap</h2>
  <p>The minimap draws a scaled overview of the diagram with a viewport rectangle derived from the current pan and zoom,
  which keeps orientation when working on diagrams larger than the screen. It exposes its maths for hosts and tests:
  <code>CalculateWorldBounds</code>, <code>CalculateScale</code>, <code>MapWorldToMinimap</code> and
  <code>CalculateViewportWorldRect</code>.</p>
</section>

<section class="section" id="property-grid">
  <h2>Property Editing</h2>
  <p>The in-canvas property editor writes changes straight back to the component through its public setters and repaints
  the canvas, so there is no separate "apply" step. It understands the property kinds the framework uses:</p>
  <ul>
    <li><strong>Colours</strong> are edited as <code>#AARRGGBB</code> hex through a dedicated converter.</li>
    <li><strong>Multi-selection</strong> exposes shared geometry and common node properties, with apply-to-all semantics.</li>
    <li><strong>Node properties</strong> declared by a component appear automatically, which is how families surface their
      own settings (transform mappings, chart series, LAS curves, and so on).</li>
  </ul>
</section>

<section class="section" id="templates">
  <h2>Templates</h2>
  <p>Twelve ready-made diagrams cover the main families &mdash; order validation and purchase approval flowcharts, an order
  lifecycle state machine, ERD, DFD, ETL, BPMN, mind map, network, UML, project plan and more. Each template is a
  <code>DiagramDto</code> with explicit wiring, so loading one produces a fully connected diagram you can edit and run:</p>
  <div class="code-example">
    <pre><code class="language-csharp">var template = DiagramTemplates.FlowChartOrderValidation();
manager.LoadTemplate(template);        // or manager.LoadFromDto(template)</code></pre>
  </div>
</section>

<section class="section" id="accessibility">
  <h2>Accessibility</h2>
  <ul>
    <li><strong>Keyboard navigation</strong> &mdash; <kbd>Tab</kbd> and <kbd>Shift</kbd>+<kbd>Tab</kbd> cycle the selection in
      reading order, skipping screen-space overlays such as the palette.</li>
    <li><strong>Screen readers</strong> &mdash; the host control exposes an accessible name, role and description that
      reflect the current selection.</li>
    <li><strong>High contrast</strong> &mdash; the editor detects the Windows high-contrast setting and applies the
      matching theme automatically, and follows changes at run time.</li>
  </ul>
  <div class="code-example">
    <pre><code class="language-csharp">manager.SelectNextComponent();          // Tab / Shift+Tab support in the engine itself
manager.SelectNextComponent(reverse: true);</code></pre>
  </div>
</section>
'@ }

$pages += @{ File = 'Help/guides/platforms.html'; Title = 'Platforms and Hosts'; Subtitle = 'Target frameworks, the five host controls, and packaging notes'; Section = 'Guides'; SectionLink = 'creating-custom-family.html'; Content = @'
<p>The framework core is platform-neutral; the hosts are thin adapters that give you a canvas control for your UI stack.
This page is the practical reference for what targets what, and what to expect when you consume the packages.</p>

<div class="toc">
  <h3>Table of Contents</h3>
  <ul>
    <li><a href="#frameworks">Target Frameworks</a></li>
    <li><a href="#hosts">Host Controls</a></li>
    <li><a href="#packages">Consuming the Packages</a></li>
    <li><a href="#feed">Private Dependency Note</a></li>
  </ul>
</div>

<section class="section" id="frameworks">
  <h2>Target Frameworks</h2>
  <table>
    <thead><tr><th>Component</th><th>Targets</th><th>Notes</th></tr></thead>
    <tbody>
      <tr><td>Core, model, family libraries</td><td><code>net8.0</code>, <code>net9.0</code>, <code>net10.0</code></td><td>Multi-targeted; every shipped target is runtime-verified by a consumer</td></tr>
      <tr><td>WinForms host</td><td><code>net10.0-windows10.0.19041.0</code></td><td>SkiaSharp's 4.x view packages ship no <code>net8.0</code> assets, so the Windows hosts require .NET 10 on Windows 10 2004 or later</td></tr>
      <tr><td>WPF host</td><td><code>net10.0-windows10.0.19041.0</code></td><td>Same constraint as WinForms</td></tr>
      <tr><td>Blazor host</td><td><code>net8.0</code></td><td>WebAssembly rendering through <code>SKCanvasView</code></td></tr>
      <tr><td>Avalonia host</td><td><code>net8.0</code></td><td>Renders into an Avalonia <code>WriteableBitmap</code>-backed surface, so it needs only Avalonia plus the same SkiaSharp version as the core</td></tr>
      <tr><td>MAUI host</td><td><code>net10.0-android/ios/maccatalyst/windows</code></td><td>Excluded from CI (Windows runners cannot build the iOS/MacCatalyst targets); build locally</td></tr>
    </tbody>
  </table>
</section>

<section class="section" id="hosts">
  <h2>Host Controls</h2>
  <p>Each host wraps the same <code>DrawingManager</code>, so diagram code is portable between them. Hosts add what is
  platform-specific: rendering surface, pointer and keyboard translation, and the in-canvas palette/editor where it makes
  sense.</p>
  <table>
    <thead><tr><th>Host</th><th>Control</th><th>Rendering surface</th></tr></thead>
    <tbody>
      <tr><td>WinForms</td><td><code>SkiaHostControl</code></td><td><code>SKControl</code> (flagship host: palette, property editor, minimap, dialogs)</td></tr>
      <tr><td>WPF</td><td><code>SkiaHostElement</code></td><td><code>SKElement</code></td></tr>
      <tr><td>Blazor</td><td><code>SkiaHostComponent</code></td><td><code>SKCanvasView</code> (WebGL/Canvas2D)</td></tr>
      <tr><td>Avalonia</td><td><code>SkiaHostControl</code></td><td><code>SkiaSurface</code> &mdash; offscreen <code>SKSurface</code> over a <code>WriteableBitmap</code></td></tr>
      <tr><td>MAUI</td><td><code>SkiaHostView</code></td><td><code>SKCanvasView</code></td></tr>
    </tbody>
  </table>
</section>

<section class="section" id="packages">
  <h2>Consuming the Packages</h2>
  <p>Packages are versioned from git tags with MinVer, and 23 of them are produced by a release build (core, model, the 16
  families, the hosts, and the loader). A consumer needs only what it uses:</p>
  <div class="code-example">
    <pre><code class="language-xml">&lt;ItemGroup&gt;
  &lt;!-- library only --&gt;
  &lt;PackageReference Include="Beep.Skia" Version="1.0.0" /&gt;
  &lt;PackageReference Include="Beep.Skia.ERD" Version="1.0.0" /&gt;

  &lt;!-- with the WinForms editor --&gt;
  &lt;PackageReference Include="Beep.Skia.Winform.Controls" Version="1.0.0" /&gt;
&lt;/ItemGroup&gt;</code></pre>
  </div>
  <p>Restore, compile, render, save/load and workflow execution from a packaged build are verified by a standalone
  consumer project on <code>net8.0</code>, <code>net9.0</code> and <code>net10.0</code>.</p>
</section>

<section class="section" id="feed">
  <h2>Private Dependency Note</h2>
  <div class="note">
    The core depends on <code>TheTechIdea.Beep.*</code> packages that are not published to nuget.org at the required
    versions. Configure a feed that provides them before restoring: locally a folder source (for example
    <code>LocalNugetFiles</code>), in CI a feed secret. Without it, restore fails for the projects that reference those
    packages.
  </div>
</section>
'@ }

$pages += @{ File = 'Help/guides/testing-and-quality.html'; Title = 'Testing and Quality'; Subtitle = 'The test suite, the sweeps, the hardening record and CI'; Section = 'Guides'; SectionLink = 'creating-custom-family.html'; Content = @'
<p>The framework is developed test-first in the literal sense: every subsystem has tests, and whole-surface sweeps check
universal invariants across every component type. This page explains what is covered, how to run it, and what the tests
have caught.</p>

<div class="toc">
  <h3>Table of Contents</h3>
  <ul>
    <li><a href="#running">Running the Tests</a></li>
    <li><a href="#coverage">What Is Covered</a></li>
    <li><a href="#sweeps">Whole-Surface Sweeps</a></li>
    <li><a href="#hardening">Hardening Record</a></li>
    <li><a href="#ci">Continuous Integration</a></li>
  </ul>
</div>

<section class="section" id="running">
  <h2>Running the Tests</h2>
  <div class="code-example">
    <pre><code class="language-bash">dotnet test Beep.Skia.Tests/Beep.Skia.Tests.csproj          # 501 tests
dotnet test --filter "FullyQualifiedName~HardeningTests"   # just the hardening suites</code></pre>
  </div>
  <p>Tests target <code>net10.0-windows10.0.19041.0</code> and run serialized: several suites exercise process-wide state
  (themes, static caches), so parallel class execution would make them flaky.</p>
</section>

<section class="section" id="coverage">
  <h2>What Is Covered</h2>
  <ul>
    <li><strong>Every library</strong> &mdash; all 26 projects, including the loader integration.</li>
    <li><strong>Feature coverage</strong> &mdash; each family's algorithms (codegen, schema diffing, CPM, XMI/BPMN export, PageRank, DREAD, ERC, LAS grouping) and each subsystem (automation, extensions, marketplace, collaboration, assistance).</li>
    <li><strong>Rendering</strong> &mdash; smoke tests that assert ink is produced, image-diff checks for edits, and pixel checks for migrated drawing code.</li>
    <li><strong>End-to-end</strong> &mdash; one scenario drives the whole story: author &rarr; save &rarr; reload &rarr; execute &rarr; export &rarr; collaborate &rarr; extend &rarr; generate.</li>
  </ul>
</section>

<section class="section" id="sweeps">
  <h2>Whole-Surface Sweeps</h2>
  <p>Rather than trusting per-type tests, sweeps instantiate <strong>every public component type</strong> (293 of them) and
  assert universal invariants:</p>
  <table>
    <thead><tr><th>Invariant</th><th>Why it matters</th></tr></thead>
    <tbody>
      <tr><td>Renders without throwing</td><td>A component that throws breaks the whole canvas</td></tr>
      <tr><td>Updates without throwing</td><td><code>Update</code> runs every frame, outside drawing</td></tr>
      <tr><td>Survives a save/load round-trip</td><td>Silent data loss is worse than a crash</td></tr>
      <tr><td>Accepts its own property values</td><td>This is exactly what loading a diagram does</td></tr>
      <tr><td>Renders at 0.1&times; to 8&times; zoom</td><td>Zoom-scaled drawing is easy to get wrong</td></tr>
      <tr><td>Survives the input pipeline</td><td>Click, drag, pan, wheel and keys run on every component</td></tr>
      <tr><td>Supports clipboard and undo/redo</td><td>Editing must work for every type</td></tr>
    </tbody>
  </table>
</section>

<section class="section" id="hardening">
  <h2>Hardening Record</h2>
  <p>Adversarial tests feed malformed, hostile and extreme input to every entry point: diagram DTOs, DDL, expressions,
  the flowchart and mind-map DSLs, JSON/XML flattening, BPMN XML, LAS files, packages and vaults, plus concurrency and
  resource-lifecycle checks. They found and fixed <strong>24 defects</strong>, including:</p>
  <ul>
    <li><strong>Six crash paths</strong> &mdash; unbounded recursion in the expression parser (nested parentheses, unary chains,
      nested calls), the expression <em>evaluator</em> (very long flat chains), the mind-map layout, and a component that
      drew a path it had already disposed.</li>
    <li><strong>Three silent save/load data losses</strong> &mdash; automation connections, automation configuration, and ML
      connections were dropped on reload.</li>
    <li><strong>Three data races</strong> &mdash; collaboration state, the credential vault, and the package catalog were not
      thread-safe.</li>
    <li><strong>A decompression bomb</strong> and an <strong>authorization bypass</strong> (role state handed out by
      reference) in the marketplace and collaboration services.</li>
  </ul>
</section>

<section class="section" id="ci">
  <h2>Continuous Integration</h2>
  <p><code>.github/workflows/ci.yml</code> builds <code>Beep.Skia.CI.slnf</code>, runs the suite, and packs packages on
  tags. It treats <strong>warnings as errors</strong> and the solution is warning-free with no suppressions, so the
  quality bar is enforced rather than aspirational. Versions come from git tags through MinVer.</p>
</section>
'@ }

foreach ($page in $pages) {
    $dir = Split-Path $page.File -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
    $html = $template.Replace('{TITLE}', $page.Title).Replace('{SUBTITLE}', $page.Subtitle).Replace('{SECTION}', $page.Section).Replace('{SECTION_LINK}', $page.SectionLink).Replace('{CONTENT}', $page.Content)
    [System.IO.File]::WriteAllText($page.File, $html, [System.Text.UTF8Encoding]::new($false))
    Write-Output "wrote $($page.File)"
}

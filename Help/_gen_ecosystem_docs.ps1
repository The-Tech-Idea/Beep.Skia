# Generates the Ecosystem section pages (extensions, marketplace, collaboration, assisted generation).

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
<nav class="breadcrumb-nav"><a href="../index.html">Home</a><span>&rsaquo;</span> <a href="extensions.html">Ecosystem</a><span>&rsaquo;</span> <span>{TITLE}</span></nav>
<div class="page-header"><h1>{TITLE}</h1><p class="page-subtitle">{SUBTITLE}</p></div>
{CONTENT}
</div></main></div>
<script src="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-core.min.js"></script>
<script src="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/plugins/autoloader/prism-autoloader.min.js"></script>
</body></html>
'@

$pages = @()

$pages += @{ File = 'Help/ecosystem/extensions.html'; Title = 'Extension SDK'; Subtitle = 'Shipping components and commands as loadable packages'; Content = @'
<p>Extensions let you add components to the editor without rebuilding it. An extension is a class library that implements
<code>ISkiaExtension</code>; the host discovers it, registers its components in the palette, and exposes its commands.</p>

<div class="toc">
  <h3>Table of Contents</h3>
  <ul>
    <li><a href="#contract">The Contract</a></li>
    <li><a href="#host">Loading Extensions</a></li>
    <li><a href="#palette">Palette Integration</a></li>
    <li><a href="#commands">Commands</a></li>
  </ul>
</div>

<section class="section" id="contract">
  <h2>The Contract</h2>
  <div class="code-example">
    <h3>A minimal extension</h3>
    <pre><code class="language-csharp">public sealed class GaugeExtension : ISkiaExtension
{
    public string Id =&gt; "acme.gauge";
    public string Name =&gt; "Acme Gauge";
    public string Version =&gt; "1.2.0";
    public string Description =&gt; "Gauge and dial components";

    public IEnumerable&lt;Type&gt; GetComponentTypes()
    {
        yield return typeof(GaugeNode);      // any concrete SkiaComponent
    }

    public void Initialize(ISkiaExtensionContext context)
    {
        context.RegisterComponent(typeof(StatusLampNode), category: "Acme", displayName: "Status Lamp");
        context.RegisterCommand("acme.reset", target =&gt; (target as GaugeNode)?.Reset());
        context.Log("Acme gauge ready");
    }
}</code></pre>
  </div>
  <p>The host validates what an extension registers: types must be concrete <code>SkiaComponent</code> subclasses, and
  anything invalid is skipped with a diagnostic instead of breaking loading.</p>
</section>

<section class="section" id="host">
  <h2>Loading Extensions</h2>
  <p><code>SkiaExtensionHost</code> loads extensions from assemblies or from a folder, and reports what it found:</p>
  <div class="code-example">
    <pre><code class="language-csharp">var host = new SkiaExtensionHost();
host.LoadFromDirectory(Path.Combine(AppContext.BaseDirectory, "Extensions"));

foreach (var extension in host.Extensions)
    Console.WriteLine($"{extension.Name} {extension.Version} ok={extension.Success}");

foreach (var component in host.Components)
    Console.WriteLine($"palette entry: {component.DisplayName} ({component.Category})");</code></pre>
  </div>
  <ul>
    <li>Extensions are keyed by <code>Id</code>; loading the same extension twice is ignored.</li>
    <li>An extension whose constructor or <code>Initialize</code> throws is recorded with its errors &mdash; other extensions still load.</li>
    <li>Every action is written to <code>host.Log</code> for diagnostics.</li>
  </ul>
</section>

<section class="section" id="palette">
  <h2>Palette Integration</h2>
  <p>The WinForms host scans the extension folder at startup and adds each component to the palette under its declared
  category, so extension components behave exactly like built-in ones: they can be dragged onto the canvas, selected,
  configured in the property grid, and saved with the diagram.</p>
</section>

<section class="section" id="commands">
  <h2>Commands</h2>
  <p>Extensions can also contribute commands. The host invokes them with the current selection, which makes it easy to add
  context-menu style behaviour (reset, refresh, import) that only applies to the extension's own components.</p>
  <div class="code-example">
    <pre><code class="language-csharp">host.InvokeCommand("acme.reset", selectedComponent);</code></pre>
  </div>
</section>

<section class="section">
  <h2>Related</h2>
  <ul>
    <li><a href="marketplace.html">Extension Marketplace</a> &mdash; package, install and update extensions</li>
    <li><a href="../guides/extensibility.html">Extensibility Guide</a> &mdash; other extension points</li>
  </ul>
</section>
'@ }

$pages += @{ File = 'Help/ecosystem/marketplace.html'; Title = 'Extension Marketplace'; Subtitle = 'Packaging, distributing, installing and updating extensions'; Content = @'
<p>Extensions are distributed as <code>.beepkg</code> packages &mdash; a zip archive with a <code>manifest.json</code> at
its root &mdash; and managed by <code>ExtensionPackageManager</code>, which installs, updates, and removes them.</p>

<div class="toc">
  <h3>Table of Contents</h3>
  <ul>
    <li><a href="#manifest">Package Manifest</a></li>
    <li><a href="#build">Building a Package</a></li>
    <li><a href="#registry">Registry</a></li>
    <li><a href="#install">Install, Update, Uninstall</a></li>
    <li><a href="#safety">Safety</a></li>
  </ul>
</div>

<section class="section" id="manifest">
  <h2>Package Manifest</h2>
  <table>
    <thead><tr><th>Field</th><th>Required</th><th>Notes</th></tr></thead>
    <tbody>
      <tr><td><code>id</code>, <code>name</code>, <code>version</code></td><td>yes</td><td>Version must be a semantic version</td></tr>
      <tr><td><code>author</code>, <code>description</code>, <code>tags</code></td><td>no</td><td>Used for search and display</td></tr>
      <tr><td><code>minHostVersion</code></td><td>no</td><td>Install refuses to run the extension on an older host</td></tr>
      <tr><td><code>dependencies</code></td><td>no</td><td>Other extension ids that must already be installed</td></tr>
    </tbody>
  </table>
</section>

<section class="section" id="build">
  <h2>Building a Package</h2>
  <div class="code-example">
    <pre><code class="language-csharp">var manifest = new ExtensionManifest
{
    Id = "acme.gauge",
    Name = "Acme Gauge",
    Version = "1.2.0",
    MinHostVersion = "1.0.0",
    Tags = { "gauge", "dashboard" }
};

var package = ExtensionPackageBuilder.Build("bin/Release/net10.0", manifest, "acme.gauge-1.2.0.beepkg");
Console.WriteLine($"{package.SizeBytes} bytes, sha256 {package.Sha256}");</code></pre>
  </div>
</section>

<section class="section" id="registry">
  <h2>Registry</h2>
  <p><code>LocalExtensionRegistry</code> indexes a folder of packages and supports text and tag search, latest-version
  lookup, and download. It is the offline default; a hosted registry implements the same <code>IExtensionRegistry</code>
  interface.</p>
  <div class="code-example">
    <pre><code class="language-csharp">var registry = new LocalExtensionRegistry(@"C:\packages");
foreach (var package in registry.Search("gauge"))
    Console.WriteLine($"{package.Id} {package.Version}");

var latest = registry.GetPackage("acme.gauge");   // newest version</code></pre>
  </div>
</section>

<section class="section" id="install">
  <h2>Install, Update, Uninstall</h2>
  <div class="code-example">
    <pre><code class="language-csharp">var manager = new ExtensionPackageManager(installRoot, registry);

var installed = manager.InstallFromRegistry("acme.gauge");          // newest version
var updated   = manager.Update("acme.gauge");                       // only when newer exists
manager.Uninstall("acme.gauge");

foreach (var directory in manager.GetLoadDirectories())             // feed these to SkiaExtensionHost
    Console.WriteLine(directory);</code></pre>
  </div>
  <p>Each extension is installed under <code>&lt;root&gt;/&lt;id&gt;/&lt;version&gt;</code> and the active version is
  tracked in <code>installed.json</code>, which survives restarts. Failed operations are reported as results with a
  message &mdash; and a rejected install leaves <strong>no partial state</strong> behind.</p>
</section>

<section class="section" id="safety">
  <h2>Safety</h2>
  <ul>
    <li><strong>Zip-slip protection</strong> &mdash; entries that would extract outside the install folder are rejected.</li>
    <li><strong>Decompression-bomb limits</strong> &mdash; per-entry size, total uncompressed size and entry count are capped (configurable).</li>
    <li><strong>Version gates</strong> &mdash; minimum host version and dependencies are checked before extracting anything.</li>
    <li><strong>Integrity</strong> &mdash; every package exposes its SHA-256 hash for verification.</li>
  </ul>
</section>
'@ }

$pages += @{ File = 'Help/ecosystem/collaboration.html'; Title = 'Collaboration'; Subtitle = 'Sharing, review comments, presence and audit for diagram documents'; Content = @'
<p>Collaboration adds the multi-user story to a diagram: who may do what, what reviewers said, and who is working on the
document right now.</p>

<div class="toc">
  <h3>Table of Contents</h3>
  <ul>
    <li><a href="#roles">Roles and Sharing</a></li>
    <li><a href="#comments">Comments and Pins</a></li>
    <li><a href="#presence">Presence</a></li>
    <li><a href="#audit">Audit Trail</a></li>
    <li><a href="#persistence">Persistence</a></li>
  </ul>
</div>

<section class="section" id="roles">
  <h2>Roles and Sharing</h2>
  <table>
    <thead><tr><th>Role</th><th>Can do</th></tr></thead>
    <tbody>
      <tr><td>Viewer</td><td>Open the document</td></tr>
      <tr><td>Commenter</td><td>View, add comments</td></tr>
      <tr><td>Editor</td><td>View, comment, edit</td></tr>
      <tr><td>Admin</td><td>Everything, including granting and revoking access</td></tr>
    </tbody>
  </table>
  <div class="code-example">
    <pre><code class="language-csharp">var collaboration = new CollaborationService();
collaboration.RegisterUser(new CollaborationUser { Id = "alice", DisplayName = "Alice" });
collaboration.RegisterUser(new CollaborationUser { Id = "bob", DisplayName = "Bob" });

collaboration.Share("diagram", ownerId: "alice", ("bob", CollaborationRole.Commenter));
collaboration.Grant("diagram", requesterId: "alice", userId: "bob", role: CollaborationRole.Editor);
collaboration.SetPublic("diagram", requesterId: "alice", isPublic: true);   // read-only link</code></pre>
  </div>
</section>

<section class="section" id="comments">
  <h2>Comments and Pins</h2>
  <p>Comments can be anchored to a component, which makes them review notes rather than chat. Anchored comments are drawn
  as numbered pins on the component's corner through <code>CommentPinLayer</code>, and resolved comments disappear from the
  canvas.</p>
  <div class="code-example">
    <pre><code class="language-csharp">var comment = collaboration.AddComment("diagram", "bob", "This step needs a guard", "DecisionNode");
collaboration.ResolveComment("diagram", comment.Id, "alice");

// pins follow component geometry automatically
pinLayer.Rebuild(manager.GetComponents(), collaboration.GetComments("diagram"));
pinLayer.Draw(canvas);</code></pre>
  </div>
</section>

<section class="section" id="presence">
  <h2>Presence</h2>
  <p>Presence records who is active on a document and what they are doing. Entries expire automatically, so a crashed
  client does not appear online forever:</p>
  <div class="code-example">
    <pre><code class="language-csharp">collaboration.SetPresence("diagram", "bob", "editing");
foreach (var entry in collaboration.GetPresence("diagram", TimeSpan.FromMinutes(2)))
    Console.WriteLine($"{entry.UserId} {entry.Activity} ({entry.LastSeen:HH:mm:ss})");</code></pre>
  </div>
</section>

<section class="section" id="audit">
  <h2>Audit Trail</h2>
  <p>Every share, grant, revoke, comment and resolve is recorded with the user and timestamp, which gives reviewers a
  history of the document's access and review decisions.</p>
</section>

<section class="section" id="persistence">
  <h2>Persistence</h2>
  <p>Collaboration state is saved next to the diagram with <code>CollaborationSerializer</code>. Presence is intentionally
  excluded (it is ephemeral); everything else &mdash; users, shares, comments, audit &mdash; round-trips.</p>
  <div class="code-example">
    <pre><code class="language-csharp">CollaborationSerializer.Save(collaboration, "diagram.collaboration.json");
var restored = CollaborationSerializer.Load("diagram.collaboration.json");</code></pre>
  </div>
  <div class="note">
    <strong>Safe under concurrent use.</strong> The service guards its state with a lock and hands out snapshots, so
    callers cannot mutate internal state (for example to grant themselves a role) and concurrent comments, presence
    updates and queries cannot corrupt it. This is covered by 8-thread stress tests.
  </div>
</section>
'@ }

$pages += @{ File = 'Help/ecosystem/assisted-generation.html'; Title = 'Assisted Generation'; Subtitle = 'Turning a description into a diagram, offline or through an LLM'; Content = @'
<p>Assisted generation turns text into a diagram. The framework ships a deterministic, offline assistant and a
provider-agnostic contract so a cloud or local model can be plugged in without changing the editor.</p>

<div class="toc">
  <h3>Table of Contents</h3>
  <ul>
    <li><a href="#contract">Provider Contract</a></li>
    <li><a href="#dsl">Flowchart DSL</a></li>
    <li><a href="#mindmap">Mind-Map Outlines</a></li>
    <li><a href="#registry">Provider Registry</a></li>
  </ul>
</div>

<section class="section" id="contract">
  <h2>Provider Contract</h2>
  <div class="code-example">
    <pre><code class="language-csharp">public interface IDiagramAssistant
{
    string Name { get; }
    DiagramSuggestion Generate(DiagramRequest request);
}</code></pre>
  </div>
  <p>A <code>DiagramSuggestion</code> carries a <code>DiagramDto</code> that loads straight into the canvas, plus an
  explanation and any warnings. Because it produces the same DTO the serializer uses, generated diagrams can be edited,
  saved and executed like any other.</p>
</section>

<section class="section" id="dsl">
  <h2>Flowchart DSL</h2>
  <p>The built-in assistant understands a compact description. Nodes are created on first mention, types are inferred or
  declared, and edges carry optional labels:</p>
  <div class="code-example">
    <pre><code class="language-text">Start(start) "Start" -> Receive "Receive order" -> Valid(decision) "Valid?"
Valid -> Pay "Process payment" : yes
Valid -> Notify "Notify customer" : no
Pay -> Ship "Ship order" -> End(end) "End"
Notify -> End</code></pre>
  </div>
  <ul>
    <li>Types: <code>start</code>, <code>end</code>, <code>process</code>, <code>decision</code>, <code>io</code>, <code>document</code>, <code>data</code>, <code>delay</code>, <code>manual</code>, <code>subprocess</code> and more.</li>
    <li><code>Start</code>/<code>End</code> are inferred from the node name when no type is given.</li>
    <li>Lines starting with <code>#</code> or <code>//</code> are comments.</li>
    <li>The result is laid out top-down by rank, with branches spread across rows.</li>
  </ul>
</section>

<section class="section" id="mindmap">
  <h2>Mind-Map Outlines</h2>
  <p>Indentation is all a mind map needs &mdash; the first line is the central topic and each level of indentation becomes
  a ring of the radial layout:</p>
  <div class="code-example">
    <pre><code class="language-text">Product strategy
  Customers
    Personas
    Feedback
  Pricing
  Roadmap</code></pre>
  </div>
</section>

<section class="section" id="registry">
  <h2>Provider Registry</h2>
  <p><code>DiagramAssistantRegistry</code> tries providers in order and falls back, so an LLM assistant can be tried first
  and the offline rule-based assistant guarantees a result:</p>
  <div class="code-example">
    <pre><code class="language-csharp">var registry = new DiagramAssistantRegistry();          // rule-based assistant by default
registry.Register(new MyLlmAssistant(), priority: 0);   // tried first

var suggestion = registry.Generate(new DiagramRequest
{
    Prompt = "Start -> Validate \"Check input\" -> End",
    MaxNodes = 40
});

manager.LoadFromDto(suggestion.Diagram);</code></pre>
  </div>
  <div class="note">
    <strong>Bounded input.</strong> The parsers cap nesting depth and node counts, and the expression engine rejects
    over-large expressions, so a hostile or accidentally huge description produces a clear error rather than exhausting
    the process.
  </div>
</section>
'@ }

foreach ($page in $pages) {
    $dir = Split-Path $page.File -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
    $html = $template.Replace('{TITLE}', $page.Title).Replace('{SUBTITLE}', $page.Subtitle).Replace('{CONTENT}', $page.Content)
    [System.IO.File]::WriteAllText($page.File, $html, [System.Text.UTF8Encoding]::new($false))
    Write-Output "wrote $($page.File)"
}

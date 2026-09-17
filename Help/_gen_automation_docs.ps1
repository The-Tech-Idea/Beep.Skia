# Generates the Automation section pages for the Beep.Skia Help site.
# Content is authored as single-quoted here-strings (no interpolation), wrapped in the
# shared page template that matches the existing Sphinx-style pages.

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

$pages += @{ File = 'Help/automation/workflow-engine.html'; Title = 'Workflow Engine'; Subtitle = 'Executing diagrams as workflows: node types, variables, retries, and execution control'; Content = @'
<p>Every diagram you draw with Beep.Skia can also be <strong>executed</strong>. The workflow engine walks the connected
components in dependency order and runs each one as an automation node, passing data along the connections. This is what
turns a diagram from documentation into a runnable process.</p>

<div class="toc">
  <h3>Table of Contents</h3>
  <ul>
    <li><a href="#model">Execution Model</a></li>
    <li><a href="#node-types">Built-in Node Types</a></li>
    <li><a href="#variables">Inputs and Variables</a></li>
    <li><a href="#reliability">Retries and Timeouts</a></li>
    <li><a href="#control">Pause, Resume, Cancel</a></li>
    <li><a href="#results">Results and Events</a></li>
    <li><a href="#diagram-bridge">Running a Diagram</a></li>
  </ul>
</div>

<section class="section" id="model">
  <h2>Execution Model</h2>
  <p>Components that implement <code>IAutomationNode</code> are the units of work. The engine sorts them topologically
  from the diagram's connections, so a node runs only after everything feeding into it has completed. Each execution gets
  its own <code>ExecutionContext</code> carrying input data, variables, and the execution id.</p>
  <table>
    <thead><tr><th>Concept</th><th>Type</th><th>Purpose</th></tr></thead>
    <tbody>
      <tr><td>Engine</td><td><code>WorkflowEngine</code> (implements <code>IWorkflowEngine</code>)</td><td>Loads workflow definitions and executes them</td></tr>
      <tr><td>Definition</td><td><code>WorkflowDefinition</code></td><td>Nodes, connections, variables, retry policies, priority</td></tr>
      <tr><td>Node</td><td><code>IAutomationNode</code></td><td>Initialize / validate / execute, with status and error events</td></tr>
      <tr><td>Context</td><td><code>ExecutionContext</code></td><td>Per-execution data, variables, and node results</td></tr>
      <tr><td>Result</td><td><code>WorkflowResult</code></td><td>Final status, per-node results, output data, errors</td></tr>
    </tbody>
  </table>
</section>

<section class="section" id="node-types">
  <h2>Built-in Node Types</h2>
  <p>The engine registers these by default, so a diagram built from them runs without any wiring:</p>
  <table>
    <thead><tr><th>Node</th><th>Role</th></tr></thead>
    <tbody>
      <tr><td><code>ManualTriggerNode</code></td><td>Starts a run on demand (button text, shortcut, cooldown, auto-reset)</td></tr>
      <tr><td><code>TimerTriggerNode</code></td><td>Starts a run on a schedule or interval</td></tr>
      <tr><td><code>DataInputNode</code></td><td>Feeds a dataset into the pipeline</td></tr>
      <tr><td><code>DataTransformNode</code></td><td>Map / filter / aggregate / custom transforms with field mappings</td></tr>
      <tr><td><code>ConditionalNode</code></td><td>Branches on an expression, with true/false outputs</td></tr>
      <tr><td><code>DataSourceAutomationNode</code></td><td>Reads or writes through a registered data source</td></tr>
      <tr><td><code>HttpRequestNode</code></td><td>Calls an HTTP endpoint (method, URL, headers, body)</td></tr>
    </tbody>
  </table>
  <p>Custom nodes are registered by type name, which is also how diagram-sourced workflows resolve their components:</p>
  <div class="code-example">
    <h3>Registering a custom node</h3>
    <pre><code class="language-csharp">var engine = new WorkflowEngine();
engine.RegisterNodeType("MyCustomNode", () => new MyCustomNode());
engine.RegisterNodeType(typeof(MyCustomNode).FullName, () => new MyCustomNode());</code></pre>
  </div>
</section>

<section class="section" id="variables">
  <h2>Inputs and Variables</h2>
  <p>Workflow variables are seeded into every node's context before execution, and input data supplied at run time is
  available to all nodes. This is how the same workflow behaves differently per invocation &mdash; an environment name,
  a batch size, a date range.</p>
  <div class="code-example">
    <h3>Variables and input data</h3>
    <pre><code class="language-csharp">var workflow = new WorkflowDefinition("nightly", "Nightly Sync");
workflow.Variables.Add(new WorkflowVariable("environment", DataType.String)
{
    DefaultValue = "production",
    IsRequired = true
});

await engine.LoadWorkflowAsync(workflow);
var result = await engine.ExecuteWorkflowAsync("nightly", new Dictionary&lt;string, object&gt;
{
    ["batchSize"] = 500
});</code></pre>
  </div>
</section>

<section class="section" id="reliability">
  <h2>Retries and Timeouts</h2>
  <p>Reliability is configured per node, not globally, so a flaky HTTP call can retry while a deterministic transform
  fails fast:</p>
  <div class="code-example">
    <h3>Retry policies</h3>
    <pre><code class="language-csharp">nodeDef.RetryPolicy = RetryPolicy.Simple(maxRetries: 3, delaySeconds: 2);
nodeDef.RetryPolicy = RetryPolicy.ExponentialBackoff(maxRetries: 5, initialDelaySeconds: 1, maxDelaySeconds: 30);
nodeDef.ExecutionTimeout = TimeSpan.FromSeconds(30);</code></pre>
  </div>
  <p>A node that keeps failing marks the execution <code>Failed</code> and the error is recorded against the node id, so
  you can see exactly where a run broke.</p>
</section>

<section class="section" id="control">
  <h2>Pause, Resume, Cancel</h2>
  <p>Long-running workflows can be controlled while they execute. Pausing takes effect at the next node boundary, and
  cancelling propagates a cancellation token into the running node:</p>
  <div class="code-example">
    <h3>Controlling a running execution</h3>
    <pre><code class="language-csharp">await engine.PauseExecutionAsync(executionId);   // holds before the next node
await engine.ResumeExecutionAsync(executionId);
await engine.CancelExecutionAsync(executionId);  // cooperative cancellation</code></pre>
  </div>
</section>

<section class="section" id="results">
  <h2>Results and Events</h2>
  <p>The engine raises events for the whole lifecycle &mdash; <code>WorkflowStarted</code>, <code>WorkflowCompleted</code>,
  <code>WorkflowFailed</code>, <code>WorkflowPaused</code>, <code>WorkflowResumed</code>, <code>WorkflowCancelled</code> &mdash;
  and returns a <code>WorkflowResult</code> with per-node results and the accumulated output data. Execution history is
  kept per workflow and is safe to query from other threads while runs are in flight.</p>
</section>

<section class="section" id="diagram-bridge">
  <h2>Running a Diagram</h2>
  <p><code>DrawingManager.ToWorkflowDefinition()</code> converts the current canvas into a workflow: automation
  components become nodes (with their configuration), and connection lines become the dependency graph.</p>
  <div class="code-example">
    <h3>Diagram to execution</h3>
    <pre><code class="language-csharp">var manager = new DrawingManager();
manager.AddComponent(new ManualTriggerNode { X = 40, Y = 40, Name = "Start" });
manager.AddComponent(new DataTransformNode { X = 40, Y = 180, Name = "Transform" });
manager.ConnectComponents(manager.GetComponents()[0], manager.GetComponents()[1]);

var workflow = manager.ToWorkflowDefinition("Saved Diagram");
await engine.LoadWorkflowAsync(workflow);
var result = await engine.ExecuteWorkflowAsync(workflow.Id);</code></pre>
  </div>
  <div class="note">
    <strong>Saved diagrams keep their wiring and configuration.</strong> Port identities and each automation node's
    runtime configuration (transform mappings, filters, and so on) survive a save/load round-trip, so a workflow that ran
    yesterday still runs after being reopened. This is covered by dedicated regression tests.
  </div>
</section>

<section class="section">
  <h2>Related</h2>
  <ul>
    <li><a href="triggers.html">Triggers</a> &mdash; start workflows on a schedule, an event, or a file change</li>
    <li><a href="credentials.html">Credential Vault &amp; Connections</a> &mdash; keep secrets out of diagrams</li>
    <li><a href="server-sku.html">Server Execution SKU</a> &mdash; run workflows as queued jobs over HTTP</li>
    <li><a href="data-sources.html">Automation Data Sources</a> &mdash; connect nodes to real data</li>
  </ul>
</section>
'@ }

$pages += @{ File = 'Help/automation/triggers.html'; Title = 'Triggers'; Subtitle = 'Starting workflows from schedules, events, and file changes'; Content = @'
<p>A trigger decides <em>when</em> a workflow runs. Every trigger implements <code>ITrigger</code> through the shared
<code>TriggerBase</code>, which provides identity, enabled/active state, activation counts, and error reporting.</p>

<div class="toc">
  <h3>Table of Contents</h3>
  <ul>
    <li><a href="#built-in">Built-in Triggers</a></li>
    <li><a href="#lifecycle">Lifecycle</a></li>
    <li><a href="#example">Example</a></li>
  </ul>
</div>

<section class="section" id="built-in">
  <h2>Built-in Triggers</h2>
  <table>
    <thead><tr><th>Trigger</th><th>Fires when</th><th>Key configuration</th></tr></thead>
    <tbody>
      <tr><td><code>ManualTrigger</code></td><td>You call <code>ActivateAsync()</code> (or the user clicks)</td><td>Optional payload passed to the workflow</td></tr>
      <tr><td><code>ScheduleTrigger</code></td><td>A fixed interval elapses</td><td><code>Interval</code> (must be greater than zero)</td></tr>
      <tr><td><code>EventTrigger</code></td><td>An application event is raised</td><td>Event name and optional filter</td></tr>
      <tr><td><code>FileWatchTrigger</code></td><td>A file matching a filter is created, changed, deleted, or renamed</td><td><code>Path</code>, <code>Filter</code>, <code>IncludeSubdirectories</code></td></tr>
    </tbody>
  </table>
</section>

<section class="section" id="lifecycle">
  <h2>Lifecycle</h2>
  <p>Triggers are validated, started, and stopped explicitly, and they report failures instead of throwing at you:</p>
  <div class="code-example">
    <h3>Validate, start, observe, dispose</h3>
    <pre><code class="language-csharp">var trigger = new FileWatchTrigger
{
    Path = @"C:\dropbox\incoming",
    Filter = "*.csv",
    IncludeSubdirectories = false,
    WorkflowId = "nightly"
};

var validation = await trigger.ValidateAsync();   // e.g. "Directory does not exist"
if (!validation.IsValid) { /* surface it */ }

trigger.Triggered += (s, e) =&gt; Console.WriteLine($"fired with {e.Data?.Count} field(s)");
await trigger.StartAsync();

// ... later
trigger.Dispose();   // stops the trigger and releases its watcher/timer</code></pre>
  </div>
  <div class="note">
    <strong>Triggers are <code>IDisposable</code>.</strong> Disposing a running trigger stops it and releases the
    underlying <code>FileSystemWatcher</code> or background loop, so hosts that discard a trigger cannot leak operating
    system handles. Disposal is idempotent and safe on an already-stopped trigger, and a stopped trigger can be restarted.
  </div>
  <p>Each trigger exposes <code>IsActive</code>, <code>IsEnabled</code>, <code>LastActivated</code> and
  <code>ActivationCount</code> for monitoring, plus <code>Triggered</code> and <code>ErrorOccurred</code> events.</p>
</section>

<section class="section" id="example">
  <h2>Example: a schedule that feeds a workflow</h2>
  <div class="code-example">
    <pre><code class="language-csharp">var engine = new WorkflowEngine();
await engine.LoadWorkflowAsync(savedWorkflow);

using var trigger = new ScheduleTrigger { Interval = TimeSpan.FromMinutes(15) };
trigger.Triggered += async (s, e) =&gt;
{
    var result = await engine.ExecuteWorkflowAsync(savedWorkflow.Id);
    Console.WriteLine($"scheduled run finished: {result.Status}");
};

await trigger.StartAsync();</code></pre>
  </div>
</section>

<section class="section">
  <h2>Related</h2>
  <ul>
    <li><a href="workflow-engine.html">Workflow Engine</a></li>
    <li><a href="server-sku.html">Server Execution SKU</a> &mdash; job submission with retries and status queries</li>
  </ul>
</section>
'@ }

$pages += @{ File = 'Help/automation/credentials.html'; Title = 'Credential Vault & Connections'; Subtitle = 'Keeping secrets out of diagrams and safely persisted at rest'; Content = @'
<p>Automation nodes need credentials &mdash; API keys, database passwords, tokens. Beep.Skia keeps them out of the
diagram itself in a <strong>credential vault</strong>, and resolves them by name at run time.</p>

<div class="toc">
  <h3>Table of Contents</h3>
  <ul>
    <li><a href="#vault">Credential Vault</a></li>
    <li><a href="#export">Export and Import</a></li>
    <li><a href="#connections">Connection Manager</a></li>
    <li><a href="#security">Security Properties</a></li>
  </ul>
</div>

<section class="section" id="vault">
  <h2>Credential Vault</h2>
  <p>The vault stores named entries of a type (API key, username/password, token, connection string, certificate) with
  optional metadata, and keeps the most recent update time per entry.</p>
  <div class="code-example">
    <h3>Storing and retrieving a credential</h3>
    <pre><code class="language-csharp">var vault = new CredentialVault("vault-passphrase");

vault.Set(new CredentialEntry
{
    Name = "crm-api",
    Type = CredentialType.ApiKey,
    Username = "integration",
    Secret = "sk-live-...",
    Metadata = { ["endpoint"] = "https://crm.example.com" }
});

var credential = vault.Get("crm-api");       // a copy, including the secret
foreach (var masked in vault.List())         // secrets masked for display
    Console.WriteLine($"{masked.Name}: {masked.Secret}");</code></pre>
  </div>
</section>

<section class="section" id="export">
  <h2>Export and Import</h2>
  <p>Vaults travel between machines as JSON. With a passphrase the secrets are encrypted (AES with a PBKDF2-SHA256
  derived key); without one they are written as-is for development only.</p>
  <div class="code-example">
    <pre><code class="language-csharp">var json = vault.Export();                  // encrypted when a passphrase was supplied
File.WriteAllText("vault.json", json);

var restored = new CredentialVault("vault-passphrase");
restored.Import(File.ReadAllText("vault.json"));</code></pre>
  </div>
</section>

<section class="section" id="connections">
  <h2>Connection Manager</h2>
  <p>The connection manager keeps named connections (host, port, database, credential reference) and resolves the
  credential when a node needs to talk to a system, so diagrams reference <code>crm-api</code> rather than a password.</p>
</section>

<section class="section" id="security">
  <h2>Security Properties</h2>
  <ul>
    <li><strong>No plaintext in exports</strong> &mdash; an encrypted export never contains the secret value.</li>
    <li><strong>Tamper detection</strong> &mdash; modified ciphertext is rejected on import rather than silently decrypting to garbage.</li>
    <li><strong>Wrong passphrase is an error</strong> &mdash; importing with the wrong passphrase fails loudly.</li>
    <li><strong>Masked by default</strong> &mdash; <code>List()</code> returns masked secrets for UI display; only <code>Get()</code> returns the real value, as a copy.</li>
    <li><strong>Thread-safe</strong> &mdash; concurrent workflows can read and write the vault safely.</li>
  </ul>
  <div class="note">
    Malformed vault documents (bad JSON, invalid base64 salt, missing passphrase for an encrypted export) are reported as
    clear errors instead of unexpected exceptions, and are covered by tests.
  </div>
</section>
'@ }

$pages += @{ File = 'Help/automation/server-sku.html'; Title = 'Server Execution SKU'; Subtitle = 'Running workflows as queued jobs, over HTTP or in-process'; Content = @'
<p>The server SKU turns the workflow engine into a service: publish workflows, submit jobs with inputs, watch them run,
and control them &mdash; from a desktop host, a test, or an HTTP client.</p>

<div class="toc">
  <h3>Table of Contents</h3>
  <ul>
    <li><a href="#service">Execution Service</a></li>
    <li><a href="#api">JSON API Facade</a></li>
    <li><a href="#router">HTTP Routes</a></li>
    <li><a href="#host">Sample HTTP Host</a></li>
    <li><a href="#notes">Deployment Notes</a></li>
  </ul>
</div>

<section class="section" id="service">
  <h2>Execution Service</h2>
  <p><code>WorkflowExecutionService</code> queues jobs with bounded concurrency and tracks each one through a small state
  machine: <code>Queued</code> &rarr; <code>Running</code> / <code>Paused</code> &rarr; <code>Completed</code> /
  <code>Failed</code> / <code>Cancelled</code>.</p>
  <div class="code-example">
    <h3>Publish, submit, wait</h3>
    <pre><code class="language-csharp">using var service = new WorkflowExecutionService(maxConcurrency: 2);

service.Publish(savedWorkflow);                       // publish once
var job = service.Submit(savedWorkflow.Id, submittedBy: "nightly",
    inputs: new Dictionary&lt;string, object&gt; { ["environment"] = "staging" });

service.WaitForCompletion(job.Id, TimeSpan.FromMinutes(5));
Console.WriteLine($"{job.State} in {job.Duration?.TotalMilliseconds:F0} ms");

service.Pause(job.Id);      // holds at the next node boundary
service.Resume(job.Id);
service.Cancel(job.Id);</code></pre>
  </div>
  <p>The service also exposes <code>GetJob</code>, <code>GetJobs(state, limit)</code>, a <code>JobStateChanged</code>
  event for progress UIs, and <code>Dispose</code> which cancels outstanding work.</p>
</section>

<section class="section" id="api">
  <h2>JSON API Facade</h2>
  <p><code>WorkflowExecutionApi</code> wraps the service in a JSON-friendly surface so any transport can expose it. It can
  publish a <em>diagram</em> directly, which means a saved layout becomes an executable workflow without writing code:</p>
  <div class="code-example">
    <pre><code class="language-csharp">var api = new WorkflowExecutionApi(service);

var published = api.PublishDiagram(File.ReadAllText("diagram.json"), "Nightly Sync");
var submitted = api.Submit(workflowId, "me", "{\"environment\":\"ci\"}");
var detail    = api.GetJob(jobId);
var json      = api.ToJson(detail);       // the same envelope every call returns</code></pre>
  </div>
  <p>Every method returns an <code>ApiResponse</code> with <code>Success</code>, <code>Error</code> and a JSON-serializable
  <code>Payload</code>, so errors are data rather than exceptions.</p>
</section>

<section class="section" id="router">
  <h2>HTTP Routes</h2>
  <p><code>WorkflowRequestRouter</code> maps HTTP requests onto the facade and owns the status codes, so the transport
  layer stays trivial and the routing logic stays testable.</p>
  <table>
    <thead><tr><th>Method</th><th>Route</th><th>Purpose</th></tr></thead>
    <tbody>
      <tr><td>GET</td><td><code>/health</code></td><td>Service status</td></tr>
      <tr><td>GET</td><td><code>/api/workflows</code></td><td>Published workflows</td></tr>
      <tr><td>POST</td><td><code>/api/workflows?name=</code></td><td>Publish a diagram DTO (body) as a workflow</td></tr>
      <tr><td>GET</td><td><code>/api/jobs?state=&amp;limit=</code></td><td>List jobs, optionally filtered</td></tr>
      <tr><td>POST</td><td><code>/api/jobs?workflowId=&amp;submittedBy=</code></td><td>Submit a job (body = inputs JSON)</td></tr>
      <tr><td>GET</td><td><code>/api/jobs/{id}</code></td><td>Job detail, including the result</td></tr>
      <tr><td>POST</td><td><code>/api/jobs/{id}/cancel</code> | <code>pause</code> | <code>resume</code></td><td>Control a running job</td></tr>
    </tbody>
  </table>
  <p>Successful calls return <code>200</code>, bad input returns <code>400</code>, and unknown routes or workflows return
  <code>404</code>.</p>
</section>

<section class="section" id="host">
  <h2>Sample HTTP Host</h2>
  <p><code>Beep.Skia.Sample.Server</code> is a runnable ASP.NET minimal-API host around the router:</p>
  <div class="code-example">
    <h3>Run the server</h3>
    <pre><code class="language-bash">dotnet run --project Beep.Skia.Sample.Server -- --demo --url http://localhost:5199

curl -X POST "http://localhost:5199/api/jobs?workflowId=demo" -d "{\"environment\":\"ci\"}"
curl "http://localhost:5199/api/jobs?state=completed"</code></pre>
  </div>
  <p>Options: <code>--url</code>, <code>--concurrency</code>, <code>--demo</code> (publish a built-in workflow),
  <code>--publish &lt;file&gt;</code> (publish a saved diagram) and <code>--help</code>.</p>
</section>

<section class="section" id="notes">
  <h2>Deployment Notes</h2>
  <ul>
    <li><strong>Transport-agnostic</strong> &mdash; the service and facade have no HTTP dependency; the ASP.NET host is one adapter among many.</li>
    <li><strong>Concurrency-safe</strong> &mdash; several jobs run through one engine, and history/job queries are safe while runs are in flight (covered by stress tests).</li>
    <li><strong>Bounded work</strong> &mdash; concurrency is capped and queued jobs wait rather than piling up.</li>
    <li><strong>Observable</strong> &mdash; job state changes are events, and every job records start/end times, duration, error text, and the engine result.</li>
  </ul>
</section>
'@ }

$pages += @{ File = 'Help/automation/data-sources.html'; Title = 'Automation Data Sources'; Subtitle = 'Bridging automation nodes to real data providers'; Content = @'
<p>Automation nodes that read or write data resolve their target through a provider registry, which keeps diagrams
independent of the concrete data access stack.</p>

<div class="toc">
  <h3>Table of Contents</h3>
  <ul>
    <li><a href="#provider">Provider Contract</a></li>
    <li><a href="#registry">Registry</a></li>
    <li><a href="#example">Example</a></li>
  </ul>
</div>

<section class="section" id="provider">
  <h2>Provider Contract</h2>
  <p><code>IAutomationDataSourceProvider</code> exposes the operations nodes need &mdash; list data sources, read rows,
  write rows, and test connectivity &mdash; without exposing a specific ORM, driver, or connection type.</p>
  <div class="code-example">
    <pre><code class="language-csharp">public interface IAutomationDataSourceProvider
{
    string Name { get; }
    IReadOnlyList&lt;string&gt; GetDataSourceNames();
    IReadOnlyList&lt;IDictionary&lt;string, object&gt;&gt; Read(string dataSource, string query);
    int Write(string dataSource, string target, IEnumerable&lt;IDictionary&lt;string, object&gt;&gt; rows);
    bool TestConnection(string dataSource, out string error);
}</code></pre>
  </div>
</section>

<section class="section" id="registry">
  <h2>Registry</h2>
  <p><code>AutomationDataSourceRegistry</code> holds providers by name and resolves the one a node asks for. When a node
  requests a provider that is not registered, the node reports a clear failure instead of falling over.</p>
</section>

<section class="section" id="example">
  <h2>Example</h2>
  <div class="code-example">
    <h3>Registering a provider</h3>
    <pre><code class="language-csharp">AutomationDataSourceRegistry.Register(new MyBeepDmProvider(editor));

var node = new DataSourceAutomationNode { Name = "Load Orders" };
node.Configuration["Provider"] = "BeepDM";
node.Configuration["DataSource"] = "Sales";
node.Configuration["Query"] = "SELECT * FROM Orders WHERE Status = 'Open'";</code></pre>
  </div>
  <p>Because the provider is resolved by name, the same diagram can run against a test database in staging and the
  production system in release by registering a different provider.</p>
</section>
'@ }

foreach ($page in $pages) {
    $dir = Split-Path $page.File -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
    $html = $template.Replace('{TITLE}', $page.Title).Replace('{SUBTITLE}', $page.Subtitle).Replace('{SECTION}', 'Automation').Replace('{SECTION_LINK}', 'workflow-engine.html').Replace('{CONTENT}', $page.Content)
    [System.IO.File]::WriteAllText($page.File, $html, [System.Text.UTF8Encoding]::new($false))
    Write-Output "wrote $($page.File)"
}

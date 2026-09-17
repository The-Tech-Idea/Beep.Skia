# Appends the missing analysis-feature sections to the family pages.
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

$flowchart = @'
<section class="section" id="code-generation">
  <h2>Code Generation</h2>
  <p><code>FlowchartCodeGenerator</code> turns a flowchart into structured code. It walks the graph from the start node and emits nested constructs for loops, decisions and sub-processes:</p>
  <table class="property-table">
    <thead><tr><th>Member</th><th>Type</th><th>Description</th></tr></thead>
    <tbody>
      <tr><td><code>Generate(manager, language)</code></td><td><code>string</code></td><td>Generates code for everything the manager holds.</td></tr>
      <tr><td><code>Generate(components, lines, language)</code></td><td><code>string</code></td><td>Generates code for an explicit graph.</td></tr>
      <tr><td><code>CodeLanguage</code></td><td><code>enum</code></td><td><code>Pseudocode</code> (default), <code>Python</code> or <code>CSharp</code>.</td></tr>
    </tbody>
  </table>
  <div class="code-example">
    <pre><code class="language-csharp">var generator = new FlowchartCodeGenerator();
string code = generator.Generate(drawingManager, CodeLanguage.CSharp);
File.WriteAllText("generated.cs", code);</code></pre>
  </div>
</section>

<section class="section" id="simulation">
  <h2>Simulation</h2>
  <p><code>FlowchartSimulator</code> executes the flowchart step by step without running the workflow engine. It tracks the current node, a step count and a readable trace, and lets the host choose branches at decision nodes:</p>
  <table class="property-table">
    <thead><tr><th>Member</th><th>Type</th><th>Description</th></tr></thead>
    <tbody>
      <tr><td><code>Reset(components, lines)</code></td><td><code>void</code></td><td>Prepares a run and locates the start node.</td></tr>
      <tr><td><code>Step()</code></td><td><code>bool</code></td><td>Advances one node; false when the flow has finished.</td></tr>
      <tr><td><code>Run(maxSteps)</code></td><td><code>bool</code></td><td>Runs to completion or until the step cap (default 1000).</td></tr>
      <tr><td><code>CurrentNode</code></td><td><code>SkiaComponent</code></td><td>The node reached by the last step.</td></tr>
      <tr><td><code>IsFinished</code> / <code>StepCount</code></td><td><code>bool</code> / <code>int</code></td><td>Run state.</td></tr>
      <tr><td><code>Trace</code></td><td><code>List&lt;string&gt;</code></td><td>Visited nodes, in order, for display or assertions.</td></tr>
      <tr><td><code>BranchSelector</code></td><td><code>Func&lt;SkiaComponent, IReadOnlyList&lt;SimulationEdge&gt;, SimulationEdge?&gt;</code></td><td>Called at decision nodes to pick the outgoing edge; return null to stop.</td></tr>
      <tr><td><code>StepExecuted</code></td><td>event</td><td>Raised per step with the node, outgoing edges and finished flag.</td></tr>
    </tbody>
  </table>
  <div class="code-example">
    <pre><code class="language-csharp">var simulator = new FlowchartSimulator();
simulator.Reset(drawingManager.GetComponents().ToList(), drawingManager.GetLines());

simulator.BranchSelector = (node, edges) =&gt; edges.FirstOrDefault();

simulator.Run(maxSteps: 500);
Console.WriteLine(string.Join(" -&gt; ", simulator.Trace));</code></pre>
  </div>
</section>
'@
Add-Section 'diagram-families\flowchart.html' $flowchart

$security = @'
<section class="section" id="stride">
  <h2>STRIDE Analysis</h2>
  <p><code>StrideAnalyzer.Analyze(components)</code> inspects the diagram's asset nodes and produces a threat list. Each threat records its STRIDE category, the asset it affects, a suggested mitigation, a severity, matching MITRE ATT&amp;CK techniques and a DREAD score:</p>
  <table class="property-table">
    <thead><tr><th>Member</th><th>Type</th><th>Description</th></tr></thead>
    <tbody>
      <tr><td><code>Analyze(components)</code></td><td><code>void</code></td><td>Runs the analysis and fills <code>Threats</code>.</td></tr>
      <tr><td><code>Threats</code></td><td><code>List&lt;ThreatResult&gt;</code></td><td>One entry per discovered threat.</td></tr>
      <tr><td><code>ThreatResult.Category</code></td><td><code>StrideCategory</code></td><td>Spoofing, Tampering, Repudiation, Information disclosure, Denial of service or Elevation of privilege.</td></tr>
      <tr><td><code>ThreatResult.AssetName</code> / <code>Threat</code> / <code>Mitigation</code></td><td><code>string</code></td><td>What is at risk, what the threat is and how to mitigate it.</td></tr>
      <tr><td><code>ThreatResult.Severity</code></td><td><code>string</code></td><td>Low, Medium or High.</td></tr>
      <tr><td><code>ThreatResult.Techniques</code></td><td><code>List&lt;MitreTechnique&gt;</code></td><td>Mapped ATT&amp;CK techniques (id, name, tactic).</td></tr>
      <tr><td><code>ThreatResult.Dread</code></td><td><code>DreadScore</code></td><td>Risk score for the threat.</td></tr>
    </tbody>
  </table>
  <div class="code-example">
    <pre><code class="language-csharp">var analyzer = new StrideAnalyzer();
analyzer.Analyze(drawingManager.GetComponents());

foreach (var threat in analyzer.Threats)
    Console.WriteLine($"[{threat.Category}] {threat.AssetName}: {threat.Threat} " +
                      $"(DREAD {threat.Dread.Average:0.0}, {threat.Dread.RiskLevel})");</code></pre>
  </div>
</section>

<section class="section" id="dread">
  <h2>DREAD Scoring</h2>
  <p><code>DreadScore</code> rates five dimensions and derives an average and a risk level:</p>
  <table class="property-table">
    <thead><tr><th>Member</th><th>Type</th><th>Description</th></tr></thead>
    <tbody>
      <tr><td><code>Damage</code>, <code>Reproducibility</code>, <code>Exploitability</code>, <code>AffectedUsers</code>, <code>Discoverability</code></td><td><code>DreadRating</code></td><td>Each rated Low, Medium or High (default Medium).</td></tr>
      <tr><td><code>Average</code></td><td><code>double</code></td><td>Mean of the five ratings.</td></tr>
      <tr><td><code>RiskLevel</code></td><td><code>string</code></td><td>Band derived from the average.</td></tr>
    </tbody>
  </table>
  <p><code>DreadCalculator</code> builds scores from a <code>ThreatNode</code>, from severity/likelihood pairs, or from their string names, so threat tables can be scored in bulk.</p>
</section>

<section class="section" id="mitre">
  <h2>MITRE ATT&amp;CK Mapping</h2>
  <p><code>MitreAttackLibrary</code> ships a technique catalog and maps STRIDE categories onto it:</p>
  <table class="property-table">
    <thead><tr><th>Member</th><th>Description</th></tr></thead>
    <tbody>
      <tr><td><code>Techniques</code></td><td>All catalogued techniques (<code>MitreTechnique</code>: <code>Id</code>, <code>Name</code>, <code>Tactic</code>).</td></tr>
      <tr><td><code>Tactics</code></td><td>Distinct tactic names, for grouping in reports.</td></tr>
      <tr><td><code>ForStrideCategory(category)</code></td><td>Techniques relevant to a STRIDE category.</td></tr>
      <tr><td><code>Find(id)</code></td><td>Lookup by technique id (for example <code>T1190</code>).</td></tr>
    </tbody>
  </table>
</section>
'@
Add-Section 'diagram-families\security.html' $security

$statemachine = @'
<section class="section" id="transitions">
  <h2>States and Transitions</h2>
  <p>A state machine is a <code>StateNode</code> graph where the transition semantics live on the connection lines. States carry their behaviour; lines carry the trigger, guard and action:</p>
  <table class="property-table">
    <thead><tr><th>Member</th><th>On</th><th>Description</th></tr></thead>
    <tbody>
      <tr><td><code>Title</code></td><td><code>StateNode</code></td><td>State name shown in the shape.</td></tr>
      <tr><td><code>EntryAction</code></td><td><code>StateNode</code></td><td>Behaviour executed when the state is entered.</td></tr>
      <tr><td><code>ExitAction</code></td><td><code>StateNode</code></td><td>Behaviour executed when the state is left.</td></tr>
      <tr><td><code>DoActivity</code></td><td><code>StateNode</code></td><td>Activity performed while in the state.</td></tr>
      <tr><td><code>TriggerEvent</code></td><td><code>ConnectionLine</code></td><td>Event that fires the transition.</td></tr>
      <tr><td><code>GuardCondition</code></td><td><code>ConnectionLine</code></td><td>Predicate that must hold (for example <code>[balance &gt;= amount]</code>).</td></tr>
      <tr><td><code>TransitionAction</code></td><td><code>ConnectionLine</code></td><td>Behaviour executed during the transition.</td></tr>
    </tbody>
  </table>
  <p>The three line properties also appear on the line's labels, so a transition can be read directly on the canvas: <code>trigger [guard] / action</code>. They are persisted with the diagram and validated by the diagram validator when transitions are unreachable.</p>
  <div class="code-example">
    <pre><code class="language-csharp">drawingManager.ConnectComponents(idle, processing);

var transition = drawingManager.GetLines().Last();
transition.TriggerEvent = "order.received";
transition.GuardCondition = "order.items > 0";
transition.TransitionAction = "reserveInventory()";
transition.Label1 = "order.received [items > 0] / reserveInventory()";</code></pre>
  </div>
</section>
'@
Add-Section 'diagram-families\state-machine.html' $statemachine

$ecad = @'
<section class="section" id="erc">
  <h2>Electrical Rules Check</h2>
  <p><code>ElectricalRulesChecker</code> validates the electrical consistency of the diagram: floating inputs, conflicting drivers, missing power or ground, and pin-type mismatches. Violations are data, not exceptions:</p>
  <table class="property-table">
    <thead><tr><th>Member</th><th>Type</th><th>Description</th></tr></thead>
    <tbody>
      <tr><td><code>RunChecks(components, lines)</code></td><td><code>void</code></td><td>Runs every rule and fills <code>Violations</code>.</td></tr>
      <tr><td><code>Violations</code></td><td><code>List&lt;ElectricalRuleViolation&gt;</code></td><td>One entry per problem found.</td></tr>
      <tr><td><code>ElectricalRuleViolation.Message</code></td><td><code>string</code></td><td>Human-readable description.</td></tr>
      <tr><td><code>ElectricalRuleViolation.Severity</code></td><td><code>ElectricalViolationSeverity</code></td><td>Warning or error.</td></tr>
      <tr><td><code>ElectricalRuleViolation.Category</code></td><td><code>string</code></td><td>Rule family that produced the violation.</td></tr>
      <tr><td><code>ElectricalRuleViolation.FixSuggestion</code></td><td><code>string</code></td><td>Suggested remedy, when known.</td></tr>
      <tr><td><code>ElectricalRuleViolation.Component</code> / <code>Port</code> / <code>Line</code></td><td>model refs</td><td>What the violation is attached to, for selection and highlighting.</td></tr>
    </tbody>
  </table>
  <div class="code-example">
    <pre><code class="language-csharp">var checker = new ElectricalRulesChecker();
checker.RunChecks(drawingManager.GetComponents().ToList(), drawingManager.GetLines());

int errors = checker.Violations.Count(v =&gt; v.Severity == ElectricalViolationSeverity.Error);
foreach (var violation in checker.Violations)
    Console.WriteLine($"[{violation.Severity}] {violation.Category}: {violation.Message}");</code></pre>
  </div>
</section>
'@
Add-Section 'diagram-families\ecad.html' $ecad

$bpmn = @'
<section class="section" id="bpmn">
  <h2>BPMN Import and Export</h2>
  <p>The family round-trips BPMN 2.0 XML so diagrams can be exchanged with process-modeling tools:</p>
  <table class="property-table">
    <thead><tr><th>Member</th><th>Type</th><th>Description</th></tr></thead>
    <tbody>
      <tr><td><code>BpmnExporter.Export(components, lines, processName)</code></td><td><code>string</code></td><td>Serializes the diagram to BPMN XML.</td></tr>
      <tr><td><code>BpmnImporter.Parse(xml)</code></td><td><code>BpmnImportResult</code></td><td>Parses XML without touching the canvas.</td></tr>
      <tr><td><code>BpmnImporter.LoadInto(manager, xml)</code></td><td><code>BpmnImportResult</code></td><td>Parses and materializes the process on a canvas.</td></tr>
      <tr><td><code>BpmnImportResult.ProcessName</code></td><td><code>string</code></td><td>Name of the imported process.</td></tr>
      <tr><td><code>BpmnImportResult.Components</code> / <code>Flows</code></td><td>lists</td><td>Created nodes and sequence flows.</td></tr>
      <tr><td><code>BpmnImportResult.Warnings</code></td><td><code>List&lt;string&gt;</code></td><td>Non-fatal issues encountered while parsing.</td></tr>
    </tbody>
  </table>
  <div class="code-example">
    <pre><code class="language-csharp">// Export the current diagram
var exporter = new BpmnExporter();
string xml = exporter.Export(drawingManager.GetComponents(), drawingManager.GetLines(), "Order Process");
File.WriteAllText("process.bpmn", xml);

// Import into a fresh canvas
var importer = new BpmnImporter();
var result = importer.LoadInto(drawingManager, File.ReadAllText("process.bpmn"));
foreach (var warning in result.Warnings)
    Console.WriteLine($"warning: {warning}");</code></pre>
  </div>
</section>
'@
Add-Section 'diagram-families\business-process.html' $bpmn

$mindmap = @'
<section class="section" id="collapse">
  <h2>Collapse and Expand</h2>
  <p><code>MindMapVisibility.Apply(components, lines)</code> recomputes visibility from each node's <code>IsCollapsed</code> flag: descendants of a collapsed node and their connecting lines are hidden, everything else is restored. It returns how many components and lines changed, which hosts use to request a redraw only when needed:</p>
  <div class="code-example">
    <pre><code class="language-csharp">var root = drawingManager.GetComponents().OfType&lt;CentralNode&gt;().First();
root.IsCollapsed = true;

int changed = MindMapVisibility.Apply(
    drawingManager.GetComponents().ToList(),
    drawingManager.GetLines());

if (changed &gt; 0)
    drawingManager.RequestRedraw();</code></pre>
  </div>
</section>
'@
Add-Section 'diagram-families\mindmap.html' $mindmap

Write-Output 'family sections done'

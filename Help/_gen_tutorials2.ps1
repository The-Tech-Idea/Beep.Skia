# Generates the second batch of tutorial pages (UML, PM, Security, State Machine).
$ErrorActionPreference = 'Stop'
$utf8 = [System.Text.UTF8Encoding]::new($false)
$help = $PSScriptRoot

function New-Tutorial([string]$file, [string]$title, [string]$subtitle, [string]$body) {
    $page = @"
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>$title | Beep.Skia Documentation</title>
<link rel="stylesheet" href="../sphinx-style.css">
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/themes/prism-tomorrow.min.css">
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.5/font/bootstrap-icons.css">
<style>.content{margin-left:0!important} .step{border-left:3px solid var(--color-brand-content);padding-left:1rem;margin:1.5rem 0} .step h3{margin-top:0}</style>
</head>
<body>
<div class="container">
<main class="content"><div class="content-wrapper">
<nav class="breadcrumb-nav"><a href="../index.html">Home</a><span>&rsaquo;</span> <span>Tutorials</span><span>&rsaquo;</span> <span>$title</span></nav>
<div class="page-header"><h1>$title</h1><p class="page-subtitle">$subtitle</p></div>
$body
</div></main></div>
<script src="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-core.min.js"></script>
<script src="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/plugins/autoloader/prism-autoloader.min.js"></script>
</body></html>
"@
    [System.IO.File]::WriteAllText((Join-Path $help "tutorials\$file"), $page, $utf8)
    Write-Output "wrote tutorials/$file"
}

$uml = @'
<p><strong>You will build:</strong> a class model with an inheritance relationship, a sequence sketch, and an XMI export for other tools. <strong>Time:</strong> about 20 minutes. <strong>Prerequisites:</strong> <code>Beep.Skia</code> and <code>Beep.Skia.UML</code>.</p>

<div class="toc"><h3>Steps</h3><ul>
<li><a href="#step-1">1. Classes and attributes</a></li>
<li><a href="#step-2">2. Interfaces and inheritance</a></li>
<li><a href="#step-3">3. Sequence sketch</a></li>
<li><a href="#step-4">4. Export XMI</a></li>
</ul></div>

<div class="step" id="step-1">
<h3>1. Classes and attributes</h3>
<p><code>UMLClass</code> keeps attributes and operations as lists, which the node renders in its three compartments.</p>
<pre><code class="language-csharp">using Beep.Skia;
using Beep.Skia.UML;

var manager = new DrawingManager();

var order = new UMLClass { ClassName = "Order", X = 60, Y = 80, Width = 220, Height = 140 };
order.Attributes.Add("- Id : int");
order.Attributes.Add("- PlacedAt : DateTime");
order.Attributes.Add("- Total : decimal");
order.Operations.Add("+ CalculateTotal() : decimal");
manager.AddComponent(order);</code></pre>
</div>

<div class="step" id="step-2">
<h3>2. Interfaces and inheritance</h3>
<p>Relationship shapes are created by connecting the components, exactly like any other line.</p>
<pre><code class="language-csharp">var auditable = new UMLInterface { InterfaceName = "IAuditable", X = 380, Y = 60, Width = 200, Height = 90 };
manager.AddComponent(auditable);

manager.ConnectComponents(order, auditable);
var inheritance = manager.GetLines().Last();
inheritance.RoutingMode = LineRoutingMode.Orthogonal;
inheritance.Label1 = "realizes";</code></pre>
</div>

<div class="step" id="step-3">
<h3>3. Sequence sketch</h3>
<p>For interaction diagrams, add lifelines and messages. Lifelines render as dashed vertical lines with activation bars.</p>
<pre><code class="language-csharp">var client = new UMLLifeline { ClassName = "Client", X = 120, Y = 260, Width = 120, Height = 240 };
var service = new UMLLifeline { ClassName = "OrderService", X = 340, Y = 260, Width = 140, Height = 240 };
manager.AddComponent(client);
manager.AddComponent(service);

manager.ConnectComponents(client, service);
manager.GetLines().Last().Label1 = "PlaceOrder()";</code></pre>
</div>

<div class="step" id="step-4">
<h3>4. Export XMI</h3>
<pre><code class="language-csharp">var exporter = new XmiExporter();
string xmi = exporter.Export(manager.GetComponents(), manager.GetLines());
File.WriteAllText("model.xmi", xmi);</code></pre>
<p>The XMI opens in Enterprise Architect, Visual Paradigm or Papyrus.</p>
</div>

<h2>Next steps</h2>
<ul>
<li><a href="../diagram-families/uml.html">UML family reference</a> &mdash; all 24 types including activity and deployment nodes</li>
<li><a href="bpmn.html">BPMN tutorial</a> &mdash; when the model is a process rather than a system</li>
<li><a href="../core-concepts/validation.html">Diagram validation</a> &mdash; catch orphan classes before exporting</li>
</ul>
'@
New-Tutorial 'uml.html' 'Tutorial: Model a UML Class Diagram' 'Classes, interfaces, lifelines and XMI export' $uml

$pm = @'
<p><strong>You will build:</strong> a small project plan, compute the critical path, and add a Gantt timeline. <strong>Time:</strong> about 15 minutes. <strong>Prerequisites:</strong> <code>Beep.Skia</code> and <code>Beep.Skia.PM</code>.</p>

<div class="toc"><h3>Steps</h3><ul>
<li><a href="#step-1">1. Tasks with durations</a></li>
<li><a href="#step-2">2. Dependencies</a></li>
<li><a href="#step-3">3. Critical path</a></li>
<li><a href="#step-4">4. Gantt timeline</a></li>
</ul></div>

<div class="step" id="step-1">
<h3>1. Tasks with durations</h3>
<p>Each <code>TaskNode</code> carries its duration and scheduling data; the calculators read them straight from the canvas.</p>
<pre><code class="language-csharp">using Beep.Skia;
using Beep.Skia.PM;

var manager = new DrawingManager();

var design = new TaskNode { X = 60, Y = 60, Width = 180, Height = 60, Label = "Design", Duration = 5 };
var build = new TaskNode { X = 300, Y = 60, Width = 180, Height = 60, Label = "Build", Duration = 10 };
var test = new TaskNode { X = 540, Y = 60, Width = 180, Height = 60, Label = "Test", Duration = 6 };
var launch = new MilestoneNode { X = 780, Y = 60, Width = 140, Height = 60, Label = "Launch" };

foreach (var node in new SkiaComponent[] { design, build, test, launch })
    manager.AddComponent(node);</code></pre>
</div>

<div class="step" id="step-2">
<h3>2. Dependencies</h3>
<pre><code class="language-csharp">manager.ConnectComponents(design, build);
manager.ConnectComponents(build, test);
manager.ConnectComponents(test, launch);

foreach (var line in manager.GetLines())
    line.RoutingMode = LineRoutingMode.Orthogonal;</code></pre>
</div>

<div class="step" id="step-3">
<h3>3. Critical path</h3>
<p>The calculator walks the dependency graph and reports the project duration plus the tasks with no slack.</p>
<pre><code class="language-csharp">var calculator = new CriticalPathCalculator();
var schedule = calculator.Compute(manager.GetComponents(), manager.GetLines());

Console.WriteLine($"Project duration: {schedule.ProjectDuration} days");
foreach (var task in schedule.CriticalTasks)
    Console.WriteLine($"critical: {task.Label}");</code></pre>
</div>

<div class="step" id="step-4">
<h3>4. Gantt timeline</h3>
<p>Drop a Gantt node on the canvas and it lays out one row per task with the critical path highlighted.</p>
<pre><code class="language-csharp">var gantt = new GanttTimelineNode { X = 60, Y = 220, Width = 860, Height = 260 };
manager.AddComponent(gantt);
gantt.Build(manager.GetComponents().OfType&lt;TaskNode&gt;());
manager.RequestRedraw();</code></pre>
</div>

<h2>Next steps</h2>
<ul>
<li><a href="../diagram-families/project-management.html">Project management family</a> &mdash; resources, budget, risk and issue nodes</li>
<li><a href="../guides/keyboard-shortcuts.html">Keyboard and mouse</a> &mdash; arrange and align the plan quickly</li>
<li><a href="../automation/workflow-engine.html">Workflow engine</a> &mdash; drive task states from automation</li>
</ul>
'@
New-Tutorial 'project-management.html' 'Tutorial: Plan a Project' 'Tasks, dependencies, critical path and Gantt' $pm

$security = @'
<p><strong>You will build:</strong> a threat model over a small system and turn it into a scored threat list with MITRE mappings. <strong>Time:</strong> about 20 minutes. <strong>Prerequisites:</strong> <code>Beep.Skia</code> and <code>Beep.Skia.Security</code>.</p>

<div class="toc"><h3>Steps</h3><ul>
<li><a href="#step-1">1. Assets</a></li>
<li><a href="#step-2">2. Controls and vulnerabilities</a></li>
<li><a href="#step-3">3. Run STRIDE</a></li>
<li><a href="#step-4">4. Score with DREAD</a></li>
<li><a href="#step-5">5. Map to MITRE ATT&amp;CK</a></li>
</ul></div>

<div class="step" id="step-1">
<h3>1. Assets</h3>
<p>STRIDE analysis inspects <code>AssetNode</code>s; describe what is worth protecting first.</p>
<pre><code class="language-csharp">using Beep.Skia;
using Beep.Skia.Security;

var manager = new DrawingManager();

var db = new AssetNode { X = 60, Y = 80, Width = 200, Height = 90, AssetName = "Customer database" };
var api = new AssetNode { X = 340, Y = 80, Width = 200, Height = 90, AssetName = "Public API" };
manager.AddComponent(db);
manager.AddComponent(api);</code></pre>
</div>

<div class="step" id="step-2">
<h3>2. Controls and vulnerabilities</h3>
<pre><code class="language-csharp">var waf = new ControlNode { X = 340, Y = 240, Width = 200, Height = 80, Label = "WAF" };
var vuln = new VulnerabilityNode { X = 60, Y = 240, Width = 200, Height = 80, Label = "Unpatched DB", Cve = "CVE-2024-0001" };
manager.AddComponent(waf);
manager.AddComponent(vuln);

manager.ConnectComponents(api, waf);
manager.ConnectComponents(db, vuln);</code></pre>
</div>

<div class="step" id="step-3">
<h3>3. Run STRIDE</h3>
<p>The analyzer produces a threat per asset with category, mitigation and severity.</p>
<pre><code class="language-csharp">var analyzer = new StrideAnalyzer();
analyzer.Analyze(manager.GetComponents());

foreach (var threat in analyzer.Threats)
    Console.WriteLine($"[{threat.Category}] {threat.AssetName}: {threat.Threat} ({threat.Severity})");</code></pre>
</div>

<div class="step" id="step-4">
<h3>4. Score with DREAD</h3>
<p>Each threat already carries a <code>DreadScore</code>; you can also score threat nodes directly or in bulk from severity/likelihood pairs.</p>
<pre><code class="language-csharp">foreach (var threat in analyzer.Threats)
    Console.WriteLine($"{threat.Threat}: {threat.Dread.Average:0.0} ({threat.Dread.RiskLevel})");</code></pre>
</div>

<div class="step" id="step-5">
<h3>5. Map to MITRE ATT&amp;CK</h3>
<pre><code class="language-csharp">foreach (var technique in MitreAttackLibrary.ForStrideCategory(StrideAnalyzer.StrideCategory.Tampering))
    Console.WriteLine($"{technique.Id} {technique.Name} ({technique.Tactic})");

var specific = MitreAttackLibrary.Find("T1190");
Console.WriteLine($"lookup: {specific}");</code></pre>
<p>Materialize the threats as nodes on the canvas so they can be reviewed and linked to controls (the sample does exactly that).</p>
</div>

<h2>Next steps</h2>
<ul>
<li><a href="../diagram-families/security.html">Security family</a> &mdash; every node type and the rendered grid</li>
<li><a href="../ecosystem/collaboration.html">Collaboration</a> &mdash; review findings with the team</li>
<li><a href="../guides/getting-help.html">Getting help</a> &mdash; reporting a security issue in the framework itself</li>
</ul>
'@
New-Tutorial 'security.html' 'Tutorial: Build a Threat Model' 'Assets, controls, STRIDE, DREAD and MITRE' $security

$sm = @'
<p><strong>You will build:</strong> a state machine with guarded transitions. <strong>Time:</strong> about 15 minutes. <strong>Prerequisites:</strong> <code>Beep.Skia</code> and <code>Beep.Skia.StateMachine</code>.</p>

<div class="toc"><h3>Steps</h3><ul>
<li><a href="#step-1">1. States</a></li>
<li><a href="#step-2">2. Transitions with triggers and guards</a></li>
<li><a href="#step-3">3. Behaviour on states</a></li>
<li><a href="#step-4">4. Validate</a></li>
</ul></div>

<div class="step" id="step-1">
<h3>1. States</h3>
<pre><code class="language-csharp">using Beep.Skia;
using Beep.Skia.StateMachine;

var manager = new DrawingManager();

var idle = new InitialStateNode { X = 60, Y = 120, Width = 50, Height = 50 };
var active = new StateNode { X = 200, Y = 100, Width = 180, Height = 80, Title = "Active" };
var done = new FinalStateNode { X = 460, Y = 120, Width = 50, Height = 50 };

foreach (var node in new SkiaComponent[] { idle, active, done })
    manager.AddComponent(node);</code></pre>
</div>

<div class="step" id="step-2">
<h3>2. Transitions with triggers and guards</h3>
<p>Transition semantics live on the line: a trigger event, an optional guard and an action. The three properties also drive the label, so the diagram reads <code>trigger [guard] / action</code>.</p>
<pre><code class="language-csharp">manager.ConnectComponents(idle, active);
var start = manager.GetLines().Last();
start.TriggerEvent = "power.on";
start.Label1 = "power.on";

manager.ConnectComponents(active, done);
var finish = manager.GetLines().Last();
finish.TriggerEvent = "work.completed";
finish.GuardCondition = "errors == 0";
finish.TransitionAction = "flushMetrics()";
finish.Label1 = "work.completed [errors == 0] / flushMetrics()";</code></pre>
</div>

<div class="step" id="step-3">
<h3>3. Behaviour on states</h3>
<p>States carry entry and exit behaviour plus an activity performed while active:</p>
<pre><code class="language-csharp">active.EntryAction = "startHeartbeat()";
active.DoActivity = "processQueue()";
active.ExitAction = "stopHeartbeat()";</code></pre>
</div>

<div class="step" id="step-4">
<h3>4. Validate</h3>
<p>Validation flags unreachable states and dead ends; the workflow engine then executes the graph with the guards evaluated at run time.</p>
<pre><code class="language-csharp">var validator = new DiagramValidator();
validator.AddDefaultRules();
var issues = validator.Validate(manager.GetComponents().ToList(), manager.GetLines());
Console.WriteLine($"{issues.Count} issue(s)");</code></pre>
<p>To execute it, publish the diagram as a workflow &mdash; see the <a href="../automation/workflow-engine.html">Workflow Engine</a>.</p>
</div>

<h2>Next steps</h2>
<ul>
<li><a href="../diagram-families/state-machine.html">State machine family</a> &mdash; the rendered grid and transition reference</li>
<li><a href="../automation/nodes.html">Automation nodes</a> &mdash; the executable node types</li>
<li><a href="flowchart.html">Flowchart tutorial</a> &mdash; for procedural flows without state</li>
</ul>
'@
New-Tutorial 'state-machine.html' 'Tutorial: Model a State Machine' 'States, guarded transitions and state behaviour' $sm

Write-Output 'tutorial batch 2 written'

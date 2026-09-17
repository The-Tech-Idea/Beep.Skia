# Generates the step-by-step tutorial pages under Help/tutorials/.
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

$flowchart = @'
<p><strong>You will build:</strong> an order-approval flowchart, generate C# from it, and step through it with the simulator. <strong>Time:</strong> about 20 minutes. <strong>Prerequisites:</strong> a project that references <code>Beep.Skia</code> and <code>Beep.Skia.FlowChart</code> (see <a href="../getting-started/installation.html">Installation</a>).</p>

<div class="toc"><h3>Steps</h3><ul>
<li><a href="#step-1">1. Create the canvas</a></li>
<li><a href="#step-2">2. Add the nodes</a></li>
<li><a href="#step-3">3. Connect and label the flow</a></li>
<li><a href="#step-4">4. Generate code</a></li>
<li><a href="#step-5">5. Simulate the flow</a></li>
<li><a href="#step-6">6. Save the diagram</a></li>
</ul></div>

<div class="step" id="step-1">
<h3>1. Create the canvas</h3>
<p>Everything hangs off a <code>DrawingManager</code>. When you are ready to show it, attach a host control (see the <a href="../hosts/winforms.html">WinForms controls</a> page); for now render to a surface.</p>
<pre><code class="language-csharp">using Beep.Skia;
using Beep.Skia.Flowchart;
using SkiaSharp;

var manager = new DrawingManager
{
    ShowGrid = true,
    GridSpacing = 20,
    SnapToGrid = true
};</code></pre>
</div>

<div class="step" id="step-2">
<h3>2. Add the nodes</h3>
<p>Flowchart nodes derive from <code>FlowchartControl</code>. Use <code>StartEndNode</code> for terminals, <code>ProcessNode</code> for steps and <code>DecisionNode</code> for branches; every component exposes <code>X</code>, <code>Y</code>, <code>Width</code> and <code>Height</code>.</p>
<pre><code class="language-csharp">var start = new StartEndNode { Label = "Order received", X = 340, Y = 40, Width = 160, Height = 48 };
var validate = new ProcessNode { Label = "Validate order", X = 340, Y = 140, Width = 160, Height = 60 };
var decision = new DecisionNode { Label = "Valid?", X = 350, Y = 250, Width = 140, Height = 80 };
var approve = new ProcessNode { Label = "Approve", X = 340, Y = 380, Width = 160, Height = 60 };
var reject = new ProcessNode
{
    Label = "Reject",
    X = 560, Y = 260, Width = 140, Height = 60,
    CustomFillColor = new SKColor(0xFF, 0xEB, 0xEE),
    CustomStrokeColor = new SKColor(0xC6, 0x28, 0x28)
};
var end = new StartEndNode { Label = "Done", X = 340, Y = 480, Width = 160, Height = 48 };

foreach (var node in new SkiaComponent[] { start, validate, decision, approve, reject, end })
    manager.AddComponent(node);</code></pre>
</div>

<div class="step" id="step-3">
<h3>3. Connect and label the flow</h3>
<p><code>ConnectComponents</code> creates the line with automatic port assignment; enumerate <code>GetLines()</code> to style it.</p>
<pre><code class="language-csharp">manager.ConnectComponents(start, validate);
manager.ConnectComponents(validate, decision);
manager.ConnectComponents(decision, approve);
manager.ConnectComponents(approve, end);
manager.ConnectComponents(decision, reject);

foreach (var line in manager.GetLines())
{
    line.RoutingMode = LineRoutingMode.Orthogonal;
    line.Paint.StrokeWidth = 2;
    line.ShowEndArrow = true;
}

var yes = manager.GetLines().First(l =&gt; l.Start.Component == decision &amp;&amp; l.End.Component == approve);
yes.Label1 = "Yes";
var no = manager.GetLines().First(l =&gt; l.Start.Component == decision &amp;&amp; l.End.Component == reject);
no.Label1 = "No";
no.LineColor = SKColors.Firebrick;</code></pre>
</div>

<div class="step" id="step-4">
<h3>4. Generate code</h3>
<p>The generator walks the graph from the start node and emits nested constructs for loops and decisions. Pseudocode is the default; C# and Python are also supported.</p>
<pre><code class="language-csharp">var generator = new FlowchartCodeGenerator();
string csharp = generator.Generate(manager, CodeLanguage.CSharp);
Console.WriteLine(csharp);</code></pre>
<p>Expect a structure like <code>ValidateOrder(); if (Valid) { Approve(); } else { Reject(); }</code> derived from the node labels and the graph shape.</p>
</div>

<div class="step" id="step-5">
<h3>5. Simulate the flow</h3>
<p>The simulator runs the same graph step by step. <code>BranchSelector</code> decides which outgoing edge to take at a decision node; return <code>null</code> to stop.</p>
<pre><code class="language-csharp">var simulator = new FlowchartSimulator();
simulator.Reset(manager.GetComponents().ToList(), manager.GetLines());

simulator.BranchSelector = (node, edges) =&gt;
    node is DecisionNode &amp;&amp; node.Label == "Valid?"
        ? edges.FirstOrDefault(e =&gt; e.Line?.Label1 == "Yes")
        : edges.FirstOrDefault();

simulator.Run(maxSteps: 200);

Console.WriteLine($"finished={simulator.IsFinished}, steps={simulator.StepCount}");
Console.WriteLine(string.Join(" -&gt; ", simulator.Trace));</code></pre>
<p>The trace shows the visited nodes in order, which is exactly what a test would assert on.</p>
</div>

<div class="step" id="step-6">
<h3>6. Save the diagram</h3>
<p>The same DTO round-trip used everywhere else:</p>
<pre><code class="language-csharp">var json = System.Text.Json.JsonSerializer.Serialize(manager.ToDto());
File.WriteAllText("approval-flow.json", json);</code></pre>
</div>

<h2>Next steps</h2>
<ul>
<li><a href="../diagram-families/flowchart.html">Flowchart family reference</a> &mdash; all 36 node types with the rendered grid</li>
<li><a href="../automation/workflow-engine.html">Run it as a workflow</a> &mdash; execute the diagram with real data</li>
<li><a href="etl.html">ETL tutorial</a> &mdash; when the flow is a data pipeline instead of logic</li>
</ul>
'@
New-Tutorial 'flowchart.html' 'Tutorial: Build a Flowchart' 'Draw an approval flow, generate code from it and simulate it' $flowchart

$erd = @'
<p><strong>You will build:</strong> a small sales schema, export it to PostgreSQL DDL, change it, and generate the migration. <strong>Time:</strong> about 20 minutes. <strong>Prerequisites:</strong> <code>Beep.Skia</code> and <code>Beep.Skia.ERD</code>.</p>

<div class="toc"><h3>Steps</h3><ul>
<li><a href="#step-1">1. Create entities</a></li>
<li><a href="#step-2">2. Describe columns, keys and indexes</a></li>
<li><a href="#step-3">3. Connect the relationship</a></li>
<li><a href="#step-4">4. Export DDL</a></li>
<li><a href="#step-5">5. Change the schema and diff it</a></li>
<li><a href="#step-6">6. Generate the migration</a></li>
</ul></div>

<div class="step" id="step-1">
<h3>1. Create entities</h3>
<p><code>ERDEntity</code> keeps the table definition as text, so the canvas and the schema tools share one source of truth.</p>
<pre><code class="language-csharp">using Beep.Skia;
using Beep.Skia.ERD;

var manager = new DrawingManager();

var customers = new ERDEntity
{
    EntityName = "Customers",
    X = 60, Y = 60, Width = 260, Height = 160
};
var orders = new ERDEntity
{
    EntityName = "Orders",
    X = 420, Y = 60, Width = 260, Height = 160
};

manager.AddComponent(customers);
manager.AddComponent(orders);</code></pre>
</div>

<div class="step" id="step-2">
<h3>2. Describe columns, keys and indexes</h3>
<p>Set <code>Columns</code>, <code>Indexes</code> and <code>ForeignKeys</code>. The exporter understands standard SQL fragments, so you can paste existing definitions.</p>
<pre><code class="language-csharp">customers.Columns = @"Id INT PRIMARY KEY
Name NVARCHAR(200) NOT NULL
Email NVARCHAR(320) NOT NULL
CreatedAt DATETIME NOT NULL";
customers.Indexes = "IX_Customers_Email UNIQUE (Email)";

orders.Columns = @"Id INT PRIMARY KEY
CustomerId INT NOT NULL
Total DECIMAL(18,2) NOT NULL
Status NVARCHAR(32) NOT NULL
PlacedAt DATETIME NOT NULL";
orders.Indexes = "IX_Orders_CustomerId (CustomerId)";
orders.ForeignKeys = "FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES Customers(Id)";</code></pre>
</div>

<div class="step" id="step-3">
<h3>3. Connect the relationship</h3>
<p>Draw the line for the diagram, then set the ERD cardinality markers on it.</p>
<pre><code class="language-csharp">manager.ConnectComponents(customers, orders);

var fk = manager.GetLines().Last();
fk.Label1 = "places";
fk.StartMultiplicity = ERDMultiplicity.OneOnly;        // one customer
fk.EndMultiplicity = ERDMultiplicity.ZeroOrMany;       // places zero or many orders
fk.RoutingMode = LineRoutingMode.Orthogonal;</code></pre>
</div>

<div class="step" id="step-4">
<h3>4. Export DDL</h3>
<p><code>DDLExporter</code> emits <code>CREATE TABLE</code> statements in six dialects, ordered so referenced tables come first.</p>
<pre><code class="language-csharp">var exporter = new DDLExporter(DDLImporter.SQLDialect.PostgreSQL);
var entities = manager.GetComponents().OfType&lt;ERDEntity&gt;();
string script = exporter.ExportEntities(entities);
File.WriteAllText("schema.sql", script);</code></pre>
</div>

<div class="step" id="step-5">
<h3>5. Change the schema and diff it</h3>
<p>Parse the exported DDL as the "source" schema, apply a change to the model (a new column), parse that as the "target", and compare. In practice the source usually comes from a live database dump.</p>
<pre><code class="language-csharp">var importer = new DDLImporter(DDLImporter.SQLDialect.PostgreSQL);
var source = importer.Parse(File.ReadAllText("schema.sql"));

orders.Columns += "\nShippedAt DATETIME NULL";
var target = importer.Parse(exporter.ExportEntities(entities));

var diff = new SchemaComparer().Compare(source, target);
Console.WriteLine(diff.Summary());
foreach (var change in diff.OfKind(SchemaChangeKind.AddedColumn))
    Console.WriteLine($"  + {change.TableName}.{change.Column?.Name}");</code></pre>
</div>

<div class="step" id="step-6">
<h3>6. Generate the migration</h3>
<p>The generator produces both directions; review them before running anything destructive.</p>
<pre><code class="language-csharp">var migrations = new MigrationScriptGenerator();
File.WriteAllText("migrate_up.sql", migrations.GenerateForward(diff, DDLImporter.SQLDialect.PostgreSQL));
File.WriteAllText("migrate_down.sql", migrations.GenerateRollback(diff, DDLImporter.SQLDialect.PostgreSQL));</code></pre>
</div>

<h2>Next steps</h2>
<ul>
<li><a href="../diagram-families/erd-advanced.html">ERD enterprise features</a> &mdash; dialects, constraints, the full comparison rules</li>
<li><a href="etl.html">ETL tutorial</a> &mdash; load the tables you just designed</li>
<li><a href="../guides/troubleshooting.html">Troubleshooting</a> &mdash; if DDL import does not parse a statement</li>
</ul>
'@
New-Tutorial 'erd.html' 'Tutorial: Model a Database Schema' 'Design entities, export DDL and generate a migration' $erd

$etl = @'
<p><strong>You will build:</strong> a customer-enrichment pipeline with an exact lookup, a slowly changing dimension and a data profile. <strong>Time:</strong> about 25 minutes. <strong>Prerequisites:</strong> <code>Beep.Skia</code> and <code>Beep.Skia.ETL</code>, plus a data source registered through the <a href="../automation/data-sources.html">provider seam</a> (or skip execution and just design the pipeline).</p>

<div class="toc"><h3>Steps</h3><ul>
<li><a href="#step-1">1. Source and destination</a></li>
<li><a href="#step-2">2. Add a lookup</a></li>
<li><a href="#step-3">3. Track changes with a dimension</a></li>
<li><a href="#step-4">4. Derived columns and filtering</a></li>
<li><a href="#step-5">5. Profile the data</a></li>
<li><a href="#step-6">6. Run and measure</a></li>
</ul></div>

<div class="step" id="step-1">
<h3>1. Source and destination</h3>
<pre><code class="language-csharp">using Beep.Skia;
using Beep.Skia.ETL;

var manager = new DrawingManager();

var source = new ETLSource { X = 40, Y = 60, Width = 160, Height = 70, DisplayText = "Orders" };
var target = new ETLTarget { X = 640, Y = 60, Width = 160, Height = 70, DisplayText = "DW.Orders" };
manager.AddComponent(source);
manager.AddComponent(target);</code></pre>
</div>

<div class="step" id="step-2">
<h3>2. Add a lookup</h3>
<p><code>ETLLookup</code> joins the stream against a reference set. Set the key and the returned columns; <code>FailOnNoMatch</code> decides whether an unmatched row fails the pipeline or passes through.</p>
<pre><code class="language-csharp">var lookup = new ETLLookup
{
    X = 260, Y = 60, Width = 180, Height = 80,
    MatchColumns = "CustomerId",
    ReturnColumns = "Name,Segment,Region",
    CacheMode = CacheMode.Full,
    FailOnNoMatch = false
};
manager.AddComponent(lookup);

manager.ConnectComponents(source, lookup);
manager.ConnectComponents(lookup, target);</code></pre>
</div>

<div class="step" id="step-3">
<h3>3. Track changes with a dimension</h3>
<p><code>ETLScd</code> implements the three classic dimension strategies. Type 2 keeps history with effective dates and a current flag.</p>
<pre><code class="language-csharp">var dimension = new ETLScd
{
    X = 260, Y = 220, Width = 180, Height = 90,
    Type = ScdType.Type2,
    BusinessKeys = "CustomerId",
    ChangeTrackingColumns = "Segment,Region",
    EffectiveFromColumn = "ValidFrom",
    EffectiveToColumn = "ValidTo",
    CurrentFlagColumn = "IsCurrent",
    TableName = "DimCustomer"
};
manager.AddComponent(dimension);
manager.ConnectComponents(lookup, dimension);</code></pre>
</div>

<div class="step" id="step-4">
<h3>4. Derived columns and filtering</h3>
<p>Derived columns and filters use the expression engine, the same syntax the expression tester dialog evaluates.</p>
<pre><code class="language-csharp">var derived = new ETLDerivedColumn
{
    X = 480, Y = 220, Width = 180, Height = 80
};
manager.AddComponent(derived);
manager.ConnectComponents(dimension, derived);

var engine = new ExpressionEngine();
var row = new Dictionary&lt;string, object&gt;
{
    ["Total"] = 250m,
    ["Discount"] = 0.1
};
Console.WriteLine(engine.Evaluate("Total * (1 - Discount)", row));   // 225
Console.WriteLine(engine.EvaluateBoolean("Total &gt; 100 AND Discount &gt; 0", row));</code></pre>
</div>

<div class="step" id="step-5">
<h3>5. Profile the data</h3>
<p>Before running a pipeline for real, profile the source to see nulls, distinct counts and ranges; the same statistics are useful in data-quality reports.</p>
<pre><code class="language-csharp">var profiler = new DataProfiler();
// Feed rows as they arrive; inspect the profile per column:
//   profile.Columns[i].Count / NullCount / DistinctCount / Min / Max / Average
//   profile.Columns[i].NullPercentage(rowCount)</code></pre>
</div>

<div class="step" id="step-6">
<h3>6. Run and measure</h3>
<p>Record per-node metrics while the pipeline runs, then report throughput:</p>
<pre><code class="language-csharp">var metrics = new PipelineMetrics();
metrics.Nodes.Add(new NodeRunMetrics
{
    NodeName = "LookupCustomers",
    RowsIn = 12000,
    RowsOut = 11840,
    Elapsed = TimeSpan.FromMilliseconds(184)
});

foreach (var node in metrics.Nodes)
    Console.WriteLine($"{node.NodeName}: {node.RowsIn} in / {node.RowsOut} out, {node.RowsPerSecond:0} rows/s");</code></pre>
<p>To execute the graph as a workflow instead of by hand, see the <a href="../automation/workflow-engine.html">Workflow Engine</a>.</p>
</div>

<h2>Next steps</h2>
<ul>
<li><a href="../diagram-families/etl-advanced.html">ETL professional features</a> &mdash; fuzzy lookups, CDC, pivot/unpivot, the expression engine reference</li>
<li><a href="../automation/data-sources.html">Automation data sources</a> &mdash; wire the pipeline to real data</li>
<li><a href="erd.html">ERD tutorial</a> &mdash; design the target schema first</li>
</ul>
'@
New-Tutorial 'etl.html' 'Tutorial: Build a Data Pipeline' 'Enrich, track changes and profile a data flow' $etl

$bpmn = @'
<p><strong>You will build:</strong> an order process with pools, lanes and a gateway, export it as BPMN 2.0, and import it back into a fresh canvas. <strong>Time:</strong> about 20 minutes. <strong>Prerequisites:</strong> <code>Beep.Skia</code> and <code>Beep.Skia.Business</code>.</p>

<div class="toc"><h3>Steps</h3><ul>
<li><a href="#step-1">1. Pool and lanes</a></li>
<li><a href="#step-2">2. Events and tasks</a></li>
<li><a href="#step-3">3. Gateway and flow</a></li>
<li><a href="#step-4">4. Validate the process</a></li>
<li><a href="#step-5">5. Export to BPMN</a></li>
<li><a href="#step-6">6. Import it back</a></li>
</ul></div>

<div class="step" id="step-1">
<h3>1. Pool and lanes</h3>
<p><code>BpmnPoolNode</code> is the process container; <code>BpmnLaneNode</code> splits it by role.</p>
<pre><code class="language-csharp">using Beep.Skia;
using Beep.Skia.Business;

var manager = new DrawingManager();

var pool = new BpmnPoolNode { X = 40, Y = 40, Width = 820, Height = 420, Label = "Order handling" };
manager.AddComponent(pool);

var sales = new BpmnLaneNode { X = 60, Y = 90, Width = 780, Height = 160, Label = "Sales" };
var warehouse = new BpmnLaneNode { X = 60, Y = 260, Width = 780, Height = 160, Label = "Warehouse" };
manager.AddComponent(sales);
manager.AddComponent(warehouse);</code></pre>
</div>

<div class="step" id="step-2">
<h3>2. Events and tasks</h3>
<pre><code class="language-csharp">var start = new StartEvent { X = 100, Y = 150, Width = 44, Height = 44, Label = "Order received" };
var review = new TaskNode { X = 220, Y = 140, Width = 150, Height = 70, Label = "Review order" };
var pick = new TaskNode { X = 220, Y = 300, Width = 150, Height = 70, Label = "Pick items" };
var end = new EndEvent { X = 700, Y = 310, Width = 44, Height = 44, Label = "Shipped" };

foreach (var node in new SkiaComponent[] { start, review, pick, end })
    manager.AddComponent(node);</code></pre>
</div>

<div class="step" id="step-3">
<h3>3. Gateway and flow</h3>
<pre><code class="language-csharp">var decision = new Gateway { X = 430, Y = 140, Width = 60, Height = 60, Label = "Approved?" };
manager.AddComponent(decision);

manager.ConnectComponents(start, review);
manager.ConnectComponents(review, decision);
manager.ConnectComponents(decision, pick);
manager.ConnectComponents(pick, end);

foreach (var line in manager.GetLines())
    line.RoutingMode = LineRoutingMode.Orthogonal;</code></pre>
</div>

<div class="step" id="step-4">
<h3>4. Validate the process</h3>
<p>The same validator used by every family catches orphans and dead ends before you export.</p>
<pre><code class="language-csharp">var validator = new DiagramValidator();
validator.AddDefaultRules();

var issues = validator.Validate(manager.GetComponents().ToList(), manager.GetLines());
foreach (var issue in issues)
    Console.WriteLine($"[{issue.Severity}] {issue.RuleName}: {issue.Message}");</code></pre>
</div>

<div class="step" id="step-5">
<h3>5. Export to BPMN</h3>
<pre><code class="language-csharp">var exporter = new BpmnExporter();
string xml = exporter.Export(manager.GetComponents(), manager.GetLines(), "Order Handling");
File.WriteAllText("order-process.bpmn", xml);</code></pre>
<p>The result opens in any BPMN 2.0 modeling tool (bpmn.io, Camunda Modeler, Signavio).</p>
</div>

<div class="step" id="step-6">
<h3>6. Import it back</h3>
<p>Import into a fresh manager and check the warnings: unsupported constructs are reported rather than dropped silently.</p>
<pre><code class="language-csharp">var importer = new BpmnImporter();
var result = importer.Parse(File.ReadAllText("order-process.bpmn"));

Console.WriteLine($"process={result.ProcessName}, nodes={result.Components.Count}, flows={result.Flows.Count}");
foreach (var warning in result.Warnings)
    Console.WriteLine($"warning: {warning}");

var roundTrip = new DrawingManager();
importer.LoadInto(roundTrip, File.ReadAllText("order-process.bpmn"));</code></pre>
</div>

<h2>Next steps</h2>
<ul>
<li><a href="../diagram-families/business-process.html">Business process family</a> &mdash; all node types and the rendered grid</li>
<li><a href="uml.html">UML tutorial</a> &mdash; when you model the system instead of the process</li>
<li><a href="../automation/workflow-engine.html">Workflow engine</a> &mdash; execute the process as automation</li>
</ul>
'@
New-Tutorial 'bpmn.html' 'Tutorial: Model a Business Process' 'Pools, lanes, gateways and a BPMN round-trip' $bpmn

Write-Output 'tutorials written'

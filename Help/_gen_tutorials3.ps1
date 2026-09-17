# Generates the third batch of tutorial pages (MindMap, DFD, Network, ECAD, Cloud, ML, Quantitative, WellLogs).
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

$mindmap = @'
<p><strong>You will build:</strong> a product mind map and collapse a branch. <strong>Time:</strong> about 10 minutes. <strong>Prerequisites:</strong> <code>Beep.Skia</code> and <code>Beep.Skia.MindMap</code>.</p>
<div class="step"><h3>1. Root and branches</h3>
<pre><code class="language-csharp">using Beep.Skia;
using Beep.Skia.MindMap;

var manager = new DrawingManager();

var root = new CentralNode { X = 420, Y = 260, Width = 200, Height = 90, Title = "Product Strategy" };
var marketing = new TopicNode { X = 180, Y = 140, Width = 180, Height = 70, Title = "Marketing" };
var product = new TopicNode { X = 660, Y = 140, Width = 180, Height = 70, Title = "Product" };
var social = new SubTopicNode { X = 40, Y = 60, Width = 160, Height = 60, Title = "Social media" };

foreach (var node in new SkiaComponent[] { root, marketing, product, social })
    manager.AddComponent(node);</code></pre>
</div>
<div class="step"><h3>2. Connect and lay out</h3>
<pre><code class="language-csharp">manager.ConnectComponents(root, marketing);
manager.ConnectComponents(root, product);
manager.ConnectComponents(marketing, social);

// Radial auto-layout repositions the graph around the root
var layout = new RadialLayout();
layout.Arrange(manager.GetComponents(), manager.GetLines());
manager.RequestRedraw();</code></pre>
</div>
<div class="step"><h3>3. Collapse a branch</h3>
<p><code>MindMapVisibility.Apply</code> hides the descendants of collapsed nodes and restores everything else; it returns how many items changed.</p>
<pre><code class="language-csharp">marketing.IsCollapsed = true;
int changed = MindMapVisibility.Apply(manager.GetComponents().ToList(), manager.GetLines());
Console.WriteLine($"{changed} node(s)/line(s) changed visibility");
manager.RequestRedraw();</code></pre>
</div>
<h2>Next steps</h2>
<ul>
<li><a href="../diagram-families/mindmap.html">Mind map family</a> &mdash; node types and the rendered grid</li>
<li><a href="../ecosystem/assisted-generation.html">Assisted generation</a> &mdash; build the map from an indented outline</li>
</ul>
'@
New-Tutorial 'mindmap.html' 'Tutorial: Build a Mind Map' 'Radial topics, auto-layout and collapse' $mindmap

$dfd = @'
<p><strong>You will build:</strong> a level-0 data flow diagram and drill into a process. <strong>Time:</strong> about 15 minutes. <strong>Prerequisites:</strong> <code>Beep.Skia</code> and <code>Beep.Skia.DFD</code>.</p>
<div class="step"><h3>1. Processes, stores and externals</h3>
<pre><code class="language-csharp">using Beep.Skia;
using Beep.Skia.DFD;

var manager = new DrawingManager();

var customer = new DFDExternalEntityGaneSarson { X = 40, Y = 140, Width = 160, Height = 80, Label = "Customer" };
var order = new DFDProcessGaneSarson { X = 300, Y = 140, Width = 180, Height = 90, Label = "1. Take order" };
var store = new DFDDataStore { X = 580, Y = 150, Width = 180, Height = 70, Label = "D1 Orders" };

foreach (var node in new SkiaComponent[] { customer, order, store })
    manager.AddComponent(node);</code></pre>
</div>
<div class="step"><h3>2. Flows</h3>
<pre><code class="language-csharp">manager.ConnectComponents(customer, order);
manager.GetLines().Last().Label1 = "order details";
manager.ConnectComponents(order, store);
manager.GetLines().Last().Label1 = "order record";

foreach (var line in manager.GetLines())
    line.RoutingMode = LineRoutingMode.Orthogonal;</code></pre>
</div>
<div class="step"><h3>3. Drill down a level</h3>
<p><code>DFDLevelNavigator</code> manages the level stack so the same canvas can show level 0 and the decomposition of a process.</p>
<pre><code class="language-csharp">var navigator = new DFDLevelNavigator();
navigator.Enter(manager, order);        // descend into "1. Take order"
// ... build the child level on the canvas ...
navigator.Up(manager);                  // return to the parent level</code></pre>
</div>
<h2>Next steps</h2>
<ul>
<li><a href="../diagram-families/dfd.html">DFD family</a> &mdash; Gane-Sarson and Yourdon shapes</li>
<li><a href="bpmn.html">BPMN tutorial</a> &mdash; when the model is executable process rather than data movement</li>
</ul>
'@
New-Tutorial 'dfd.html' 'Tutorial: Draw a Data Flow Diagram' 'Processes, stores, flows and level drill-down' $dfd

$network = @'
<p><strong>You will build:</strong> a small network graph and run graph analysis on it. <strong>Time:</strong> about 15 minutes. <strong>Prerequisites:</strong> <code>Beep.Skia</code> and <code>Beep.Skia.Network</code>.</p>
<div class="step"><h3>1. Nodes and links</h3>
<pre><code class="language-csharp">using Beep.Skia;
using Beep.Skia.Network;

var manager = new DrawingManager();

var a = new NetworkNode { X = 80, Y = 80, Width = 60, Height = 60, Label = "A" };
var b = new NetworkNode { X = 260, Y = 60, Width = 60, Height = 60, Label = "B" };
var c = new NetworkNode { X = 260, Y = 220, Width = 60, Height = 60, Label = "C" };
var d = new NetworkNode { X = 440, Y = 140, Width = 60, Height = 60, Label = "D" };

foreach (var node in new SkiaComponent[] { a, b, c, d })
    manager.AddComponent(node);

manager.ConnectComponents(a, b);
manager.ConnectComponents(a, c);
manager.ConnectComponents(b, d);
manager.ConnectComponents(c, d);</code></pre>
</div>
<div class="step"><h3>2. Auto-layout</h3>
<pre><code class="language-csharp">var layout = new ForceDirectedLayout();
layout.Arrange(manager.GetComponents(), manager.GetLines());
manager.RequestRedraw();</code></pre>
</div>
<div class="step"><h3>3. Analyse the graph</h3>
<pre><code class="language-csharp">var graph = new NetworkGraph();
// Add nodes/links to the graph (or import from the canvas), then:
//   new PathFinder().ShortestPath(graph, "A", "D")
//   new CentralityMeasure().Degree(graph) / Betweenness(graph) / PageRank(graph)
//   new CommunityDetector().Louvain(graph)
//   new NetworkAnalyzer().Analyze(graph)  -> density, diameter, clustering
var analysis = new NetworkAnalyzer().Analyze(graph);
Console.WriteLine(analysis);</code></pre>
</div>
<h2>Next steps</h2>
<ul>
<li><a href="../diagram-families/network.html">Network family</a> &mdash; layouts, filters and export panels</li>
<li><a href="../diagram-families/network.html#ports">Graph algorithms</a> &mdash; the full analysis surface</li>
</ul>
'@
New-Tutorial 'network.html' 'Tutorial: Analyse a Network Graph' 'Nodes, links, layouts and graph algorithms' $network

$ecad = @'
<p><strong>You will build:</strong> a small circuit and run the electrical rules check. <strong>Time:</strong> about 15 minutes. <strong>Prerequisites:</strong> <code>Beep.Skia</code> and <code>Beep.Skia.ECAD</code>.</p>
<div class="step"><h3>1. Components</h3>
<pre><code class="language-csharp">using Beep.Skia;
using Beep.Skia.ECAD;

var manager = new DrawingManager();

var supply = new ECADPowerSupplyNode { X = 60, Y = 140, Width = 120, Height = 80, Label = "5V" };
var resistor = new ECADResistorNode { X = 260, Y = 150, Width = 100, Height = 60, Label = "R1" };
var led = new ECADDiodeNode { X = 440, Y = 150, Width = 100, Height = 60, Label = "D1" };
var ground = new ECADGroundNode { X = 620, Y = 160, Width = 80, Height = 60, Label = "GND" };

foreach (var node in new SkiaComponent[] { supply, resistor, led, ground })
    manager.AddComponent(node);</code></pre>
</div>
<div class="step"><h3>2. Wire it</h3>
<pre><code class="language-csharp">manager.ConnectComponents(supply, resistor);
manager.ConnectComponents(resistor, led);
manager.ConnectComponents(led, ground);

foreach (var line in manager.GetLines())
    line.RoutingMode = LineRoutingMode.Orthogonal;</code></pre>
</div>
<div class="step"><h3>3. Electrical rules check</h3>
<pre><code class="language-csharp">var checker = new ElectricalRulesChecker();
checker.RunChecks(manager.GetComponents().ToList(), manager.GetLines());

foreach (var violation in checker.Violations)
    Console.WriteLine($"[{violation.Severity}] {violation.Category}: {violation.Message}");
// violations carry the component/port/line so a host can select and highlight them</code></pre>
</div>
<h2>Next steps</h2>
<ul>
<li><a href="../diagram-families/ecad.html">ECAD family</a> &mdash; all symbol types and the rendered grid</li>
<li><a href="../core-concepts/validation.html">Diagram validation</a> &mdash; structural rules beyond electrical ones</li>
</ul>
'@
New-Tutorial 'ecad.html' 'Tutorial: Draw a Circuit and Run ERC' 'ECAD symbols and the electrical rules check' $ecad

$cloud = @'
<p><strong>You will build:</strong> a three-tier cloud architecture sketch. <strong>Time:</strong> about 10 minutes. <strong>Prerequisites:</strong> <code>Beep.Skia</code> and <code>Beep.Skia.Cloud</code>.</p>
<div class="step"><h3>1. Services</h3>
<pre><code class="language-csharp">using Beep.Skia;
using Beep.Skia.Cloud;

var manager = new DrawingManager();

var cdn = new CloudComputeNode { X = 60, Y = 60, Width = 180, Height = 80, Label = "CDN" };
var api = new CloudFunctionNode { X = 320, Y = 60, Width = 180, Height = 80, Label = "API (functions)" };
var db = new CloudDatabaseNode { X = 580, Y = 60, Width = 180, Height = 80, Label = "Managed DB" };
var storage = new CloudStorageNode { X = 320, Y = 220, Width = 180, Height = 80, Label = "Object storage" };

foreach (var node in new SkiaComponent[] { cdn, api, db, storage })
    manager.AddComponent(node);</code></pre>
</div>
<div class="step"><h3>2. Data flow</h3>
<pre><code class="language-csharp">manager.ConnectComponents(cdn, api);
manager.ConnectComponents(api, db);
manager.ConnectComponents(api, storage);

foreach (var line in manager.GetLines())
{
    line.RoutingMode = LineRoutingMode.Orthogonal;
    line.IsDataFlowAnimated = true;
    line.AnimationStyle = FlowAnimationStyle.Dots;
}</code></pre>
</div>
<div class="step"><h3>3. Save the sketch</h3>
<pre><code class="language-csharp">File.WriteAllText("architecture.json",
    System.Text.Json.JsonSerializer.Serialize(manager.ToDto()));</code></pre>
</div>
<h2>Next steps</h2>
<ul>
<li><a href="../diagram-families/cloud.html">Cloud family</a> &mdash; the rendered grid and provider notes</li>
<li><a href="../core-concepts/connection-animation.html">Connection animation</a> &mdash; all seven flow styles</li>
</ul>
'@
New-Tutorial 'cloud.html' 'Tutorial: Sketch a Cloud Architecture' 'Compute, functions, storage and data flow' $cloud

$ml = @'
<p><strong>You will build:</strong> a training pipeline and export it. <strong>Time:</strong> about 15 minutes. <strong>Prerequisites:</strong> <code>Beep.Skia</code> and <code>Beep.Skia.ML</code>.</p>
<div class="step"><h3>1. Pipeline stages</h3>
<pre><code class="language-csharp">using Beep.Skia;
using Beep.Skia.ML;

var manager = new DrawingManager();

var data = new MLDataSourceNode { X = 60, Y = 120, Width = 180, Height = 80, Label = "Training data" };
var prep = new MLPreprocessNode { X = 300, Y = 120, Width = 180, Height = 80, Label = "Normalize" };
var model = new MLModelNode { X = 540, Y = 120, Width = 180, Height = 80, Label = "Model" };
var trainer = new MLTrainerNode { X = 300, Y = 260, Width = 180, Height = 80, Label = "Train" };
var evaluate = new MLEvaluateNode { X = 540, Y = 260, Width = 180, Height = 80, Label = "Evaluate" };

foreach (var node in new SkiaComponent[] { data, prep, model, trainer, evaluate })
    manager.AddComponent(node);</code></pre>
</div>
<div class="step"><h3>2. Connect the pipeline</h3>
<pre><code class="language-csharp">manager.ConnectComponents(data, prep);
manager.ConnectComponents(prep, model);
manager.ConnectComponents(model, trainer);
manager.ConnectComponents(prep, trainer);
manager.ConnectComponents(trainer, evaluate);

foreach (var line in manager.GetLines())
    line.RoutingMode = LineRoutingMode.Orthogonal;</code></pre>
</div>
<div class="step"><h3>3. Export the pipeline</h3>
<p>The ML family can export the graph as a pipeline description for external tooling; the exact format is configured on the export node.</p>
<pre><code class="language-csharp">var export = new MLModelExportNode { X = 780, Y = 260, Width = 180, Height = 80, Label = "Export" };
manager.AddComponent(export);
manager.ConnectComponents(evaluate, export);
manager.RequestRedraw();</code></pre>
</div>
<h2>Next steps</h2>
<ul>
<li><a href="../diagram-families/ml.html">ML family</a> &mdash; every stage type and the rendered grid</li>
<li><a href="../automation/workflow-engine.html">Workflow engine</a> &mdash; run the pipeline as a workflow</li>
</ul>
'@
New-Tutorial 'ml.html' 'Tutorial: Build an ML Pipeline' 'Data, preprocessing, training, evaluation and export' $ml

$quant = @'
<p><strong>You will build:</strong> a trading strategy sketch with indicators. <strong>Time:</strong> about 10 minutes. <strong>Prerequisites:</strong> <code>Beep.Skia</code> and <code>Beep.Ski.Quantitative</code>.</p>
<div class="step"><h3>1. Data and indicators</h3>
<pre><code class="language-csharp">using Beep.Skia;
using Beep.Skia.Quantitative;

var manager = new DrawingManager();

var prices = new TimeSeriesNode { X = 60, Y = 120, Width = 180, Height = 80, Label = "AAPL 1d" };
var rsi = new IndicatorNode { X = 300, Y = 60, Width = 180, Height = 80, Label = "RSI(14)" };
var sma = new IndicatorNode { X = 300, Y = 200, Width = 180, Height = 80, Label = "SMA(50)" };

foreach (var node in new SkiaComponent[] { prices, rsi, sma })
    manager.AddComponent(node);

manager.ConnectComponents(prices, rsi);
manager.ConnectComponents(prices, sma);</code></pre>
</div>
<div class="step"><h3>2. Strategy</h3>
<pre><code class="language-csharp">var strategy = new StrategyNodes { X = 560, Y = 120, Width = 200, Height = 100, Label = "Mean reversion" };
manager.AddComponent(strategy);
manager.ConnectComponents(rsi, strategy);
manager.ConnectComponents(sma, strategy);

foreach (var line in manager.GetLines())
    line.RoutingMode = LineRoutingMode.Orthogonal;</code></pre>
</div>
<div class="step"><h3>3. Charts</h3>
<p>The family also provides chart components (bar, line, pie, scatter, area) that read their series from the node properties.</p>
<pre><code class="language-csharp">manager.RequestRedraw();
// The sample adds a chart with Tools &gt; "Add Sample Chart (Quantitative)".</code></pre>
</div>
<h2>Next steps</h2>
<ul>
<li><a href="../diagram-families/quantitative.html">Quantitative family</a> &mdash; chart and indicator components</li>
<li><a href="../guides/theming.html">Theming</a> &mdash; restyle the charts with theme tokens</li>
</ul>
'@
New-Tutorial 'quantitative.html' 'Tutorial: Sketch a Trading Strategy' 'Time series, indicators and strategy nodes' $quant

$welllogs = @'
<p><strong>You will build:</strong> a multi-track well log canvas. <strong>Time:</strong> about 15 minutes. <strong>Prerequisites:</strong> <code>Beep.Skia</code> and <code>Beep.Skia.WellLogs</code>.</p>
<div class="step"><h3>1. Canvas and tracks</h3>
<p><code>WellLogCanvas</code> owns the depth range and the track list; <code>WellLogTemplates</code> provides ready-made track sets (gamma ray, resistivity, density, neutron, sonic).</p>
<pre><code class="language-csharp">using Beep.Skia;
using Beep.Skia.WellLogs;

var manager = new DrawingManager();

var canvas = new WellLogCanvas
{
    X = 40, Y = 40, Width = 900, Height = 520,
    DepthFrom = 1000, DepthTo = 1500
};
manager.AddComponent(canvas);</code></pre>
</div>
<div class="step"><h3>2. Data</h3>
<p>LAS 2.0/3.0 files parse into curve data plus the well header; the renderer draws curves, fills and lithology patterns.</p>
<pre><code class="language-csharp">// var log = LasParser.Parse(File.ReadAllText("well.las"));
// canvas.SetCurves(log.Curves);
// canvas.SetHeader(log.Header);
manager.RequestRedraw();</code></pre>
</div>
<div class="step"><h3>3. Layout and rendering</h3>
<p><code>WellLogLayoutEngine</code> computes track widths, headers and depth grids; <code>WellLogRenderer</code> draws them with the brush library.</p>
<pre><code class="language-csharp">// The canvas runs the layout engine internally on resize and depth changes.
// Brush patterns come from WellLogBrushLibrary (sandstone, shale, limestone, ...).</code></pre>
</div>
<h2>Next steps</h2>
<ul>
<li><a href="../diagram-families/welllogs.html">Well logs family</a> &mdash; tracks, curves and templates</li>
<li><a href="../guides/performance.html">Performance</a> &mdash; large logs render per-track with culling</li>
</ul>
'@
New-Tutorial 'welllogs.html' 'Tutorial: Render a Well Log' 'Tracks, curves, LAS parsing and layout' $welllogs

Write-Output 'tutorial batch 3 written'

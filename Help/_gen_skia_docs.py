# Beep.Skia Documentation Generator
# Run: python _gen_skia_docs.py

import os

BASE = r"C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.Skia\Help"
CSS_PATH = "../"

HEAD = r"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>{title}</title>
<link rel="stylesheet" href="{css_path}sphinx-style.css">
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/themes/prism-tomorrow.min.css">
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.5/font/bootstrap-icons.css">
<style>.content{{margin-left:0!important}}</style>
</head>
<body>
<div class="container">
<main class="content"><div class="content-wrapper">
"""

TAIL = r"""
</div></main>
</div>
<script src="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-core.min.js"></script>
<script src="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/plugins/autoloader/prism-autoloader.min.js"></script>
</body></html>"""

# ============================================================================
# HELPER FUNCTIONS
# ============================================================================

def toc(items):
    lis = "".join('<li><a href="#{}">{}</a></li>'.format(k, v) for k, v in items)
    return '<div class="toc"><h3>Table of Contents</h3><ul>{}</ul></div>'.format(lis)

def breadcrumb(parents, current):
    parts = '<a href="../index.html">Home</a>'
    for p in parents:
        parts += '<span>&rsaquo;</span> <a href="{}">{}</a>'.format(p[0], p[1])
    parts += "<span>&rsaquo;</span> <span>{}</span>".format(current)
    return '<nav class="breadcrumb-nav">{}</nav>'.format(parts)

def section(id, heading, body):
    return '<section class="section" id="{}"><h2>{}</h2>{}</section>'.format(id, heading, body)

def subsection(id, heading, body):
    return '<div id="{}"><h3>{}</h3>{}</div>'.format(id, heading, body)

def page(title, css_path, breadcrumb_html, body):
    h = HEAD.format(title=title, css_path=css_path)
    return h + breadcrumb_html + body + TAIL

def code(body, lang="csharp", title=None):
    title_html = '<h3>{}</h3>\n'.format(title) if title else ""
    return '<div class="code-example">{title}<pre><code class="language-{lang}">{body}</code></pre></div>'.format(
        title=title_html, lang=lang, body=body)

def node_table(nodes):
    rows = []
    for n in nodes:
        rows.append('<tr><td><code>{}</code></td><td>{}</td><td>{}</td></tr>'.format(
            n["class"], n["shape"], n["description"]))
    return '<table><thead><tr><th>Node Class</th><th>Shape</th><th>Description</th></tr></thead><tbody>{}</tbody></table>'.format(
        "\n".join(rows))

def ports_table(nodes):
    rows = []
    for n in nodes:
        rows.append('<tr><td><code>{}</code></td><td>{}</td><td>{}</td><td>{}</td></tr>'.format(
            n["class"], n.get("in_ports", "—"), n.get("out_ports", "—"), n.get("extra_ports", "—")))
    return '<table><thead><tr><th>Node Class</th><th>Input Ports</th><th>Output Ports</th><th>Extra</th></tr></thead><tbody>{}</tbody></table>'.format(
        "\n".join(rows))

def property_table(props):
    rows = []
    for p in props:
        rows.append('<tr><td><code>{}</code></td><td><code>{}</code></td><td>{}</td><td>{}</td></tr>'.format(
            p["name"], p["type"], p["default"], p["description"]))
    return '<table><thead><tr><th>Property</th><th>Type</th><th>Default</th><th>Description</th></tr></thead><tbody>{}</tbody></table>'.format(
        "\n".join(rows))

# ============================================================================
# FAMILY DATA
# ============================================================================

FAMILIES = [
    {
        "file": "flowchart.html",
        "title": "FlowChart Diagram Family",
        "description": "Classic flowchart diagrams with 32 node types covering process, decision, I/O, loops, and annotations. Based on ISO 5807 standard shapes.",
        "base_control": "FlowchartControl : MaterialControl",
        "namespace": "Beep.Skia.FlowChart",
        "example_code": '''// Create a FlowChart control
var fc = new FlowchartControl
{
    X = 100, Y = 50,
    Width = 600, Height = 400
};
drawingManager.AddComponent(fc);

// Add nodes
var start = new StartEndNode { X = 250, Y = 60, NodeType = NodeType.Start };
var process = new ProcessNode { X = 250, Y = 130, Text = "Process Data" };
var decision = new DecisionNode { X = 250, Y = 200, Text = "Valid?" };
var stop = new StartEndNode { X = 250, Y = 280, NodeType = NodeType.Stop };

drawingManager.AddComponent(start);
drawingManager.AddComponent(process);
drawingManager.AddComponent(decision);
drawingManager.AddComponent(stop);

// Connect
drawingManager.Connect(start.OutPoints[0], process.InPoints[0]);
drawingManager.Connect(process.OutPoints[0], decision.InPoints[0]);
drawingManager.Connect(decision.OutPoints[0], stop.InPoints[0]);''',
        "nodes": [
            {"class": "ProcessNode", "shape": "Rectangle", "description": "Standard process/action step. The most common flowchart node.", "in_ports": "1 (top-center)", "out_ports": "1 (bottom-center)"},
            {"class": "DecisionNode", "shape": "Diamond", "description": "Conditional branching. Yes/No or True/False paths.", "in_ports": "1 (top)", "out_ports": "2+ (left/right/bottom)"},
            {"class": "StartEndNode", "shape": "Rounded Rectangle", "description": "Start or End terminal. Use NodeType to specify.", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "DocumentNode", "shape": "Document (wavy bottom)", "description": "Represents a document or report output.", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "DataNode", "shape": "Parallelogram", "description": "Data input/output operation.", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "InputOutputNode", "shape": "Parallelogram", "description": "Input or output operation (alternative shape).", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "PredefinedProcessNode", "shape": "Rectangle with vertical bars", "description": "Subroutine or predefined process.", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "SubProcessNode", "shape": "Rectangle with vertical bars", "description": "Nested sub-process expandable node.", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "ForLoopNode", "shape": "Hexagon", "description": "For-loop construct with initialization, condition, increment.", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "WhileLoopNode", "shape": "Hexagon", "description": "While-loop construct with pre-condition.", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "DoWhileLoopNode", "shape": "Hexagon", "description": "Do-while loop construct with post-condition.", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "LoopLimitNode", "shape": "Hexagon", "description": "Loop with defined iteration limit.", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "DelayNode", "shape": "Rectangle with curved side", "description": "Delay or wait operation.", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "DisplayNode", "shape": "Screen/display shape", "description": "Display output on screen.", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "ManualInputNode", "shape": "Rectangle with angled top", "description": "Manual data entry step.", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "ManualOperationNode", "shape": "Trapezoid", "description": "Manual operation by user.", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "PreparationNode", "shape": "Hexagon", "description": "Initialization or preparation step.", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "MergeNode", "shape": "Inverted triangle", "description": "Merge multiple flows into one.", "in_ports": "2+ (top/sides)", "out_ports": "1 (bottom)"},
            {"class": "ForkNode", "shape": "Triangle", "description": "Split one flow into multiple parallel paths.", "in_ports": "1 (top)", "out_ports": "2+ (bottom/sides)"},
            {"class": "JoinNode", "shape": "Inverted triangle", "description": "Wait for all parallel flows to complete.", "in_ports": "2+ (top/sides)", "out_ports": "1 (bottom)"},
            {"class": "OrNode", "shape": "Circle", "description": "OR-logic junction for alternative paths.", "in_ports": "2+ (sides)", "out_ports": "1+ (sides)"},
            {"class": "CollateNode", "shape": "Hourglass", "description": "Organize/sequence parallel data.", "in_ports": "2+ (top)", "out_ports": "1 (bottom)"},
            {"class": "SortNode", "shape": "Diamond variant", "description": "Sort operation step.", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "ExtractNode", "shape": "Rectangle", "description": "Extract subset from data.", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "ConnectorNode", "shape": "Circle (small)", "description": "On-page connector reference.", "in_ports": "1 (any)", "out_ports": "1 (any)"},
            {"class": "OffPageReferenceNode", "shape": "Pentagon", "description": "Off-page connector reference.", "in_ports": "1 (any)", "out_ports": "1 (any)"},
            {"class": "SummingJunctionNode", "shape": "Circle with X", "description": "Summing junction for parallel flows.", "in_ports": "2+ (sides)", "out_ports": "1 (side)"},
            {"class": "InternalStorageNode", "shape": "Rectangle", "description": "Internal storage/persistence.", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "StoredDataNode", "shape": "Cylinder", "description": "Data stored in database/persistent storage.", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "SequentialAccessNode", "shape": "Rolled paper", "description": "Sequential access storage (tape).", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "DirectAccessNode", "shape": "Drum", "description": "Direct access storage (disk).", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "MultiDocumentNode", "shape": "Stacked documents", "description": "Multiple documents.", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "TapeNode", "shape": "Data block", "description": "Magnetic tape data representation.", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "CardNode", "shape": "Card/punched card", "description": "Punched card input.", "in_ports": "1 (top)", "out_ports": "1 (bottom)"},
            {"class": "AnnotationNode", "shape": "Open bracket", "description": "Annotation/comment connected to any node.", "in_ports": "0", "out_ports": "0"},
            {"class": "CommentNode", "shape": "Note/callout", "description": "Free-text comment box.", "in_ports": "0", "out_ports": "0"},
        ],
        "conventions": [
            "Top ports are inputs; bottom ports are outputs",
            "DecisionNode uses Yes (right) and No (bottom) ports",
            "StartEndNode uses NodeType enum (Start/Stop)",
            "Flow lines default to orthogonal routing with arrowheads",
        ],
    },
    {
        "file": "dfd.html",
        "title": "DFD (Data Flow Diagram) Family",
        "description": "Data Flow Diagrams with 19 node types including Gane-Sarson and Yourdon notation processes, data stores, external entities, and trust boundaries.",
        "base_control": "DFDControl : MaterialControl",
        "namespace": "Beep.Skia.DFD",
        "example_code": '''var dfd = new DFDControl
{
    X = 100, Y = 50,
    Width = 600, Height = 400
};
drawingManager.AddComponent(dfd);

var proc = new DFDProcess { X = 300, Y = 150, Text = "Process Order" };
var store = new DFDDataStore { X = 450, Y = 80, Text = "Orders DB" };
var ext = new DFDExternalEntity { X = 150, Y = 80, Text = "Customer" };

drawingManager.AddComponent(proc);
drawingManager.AddComponent(store);
drawingManager.AddComponent(ext);

drawingManager.Connect(ext.OutPoints[0], proc.InPoints[0]);
drawingManager.Connect(proc.OutPoints[0], store.InPoints[0]);''',
        "nodes": [
            {"class": "DFDProcess", "shape": "Circle", "description": "Standard process transforming data flows.", "in_ports": "2+ (any side)", "out_ports": "2+ (any side)"},
            {"class": "DFDProcessCircle", "shape": "Circle", "description": "Simple circle notation process.", "in_ports": "2+", "out_ports": "2+"},
            {"class": "DFDProcessGaneSarson", "shape": "Rounded rectangle", "description": "Gane-Sarson notation process with ID and location.", "in_ports": "2+", "out_ports": "2+"},
            {"class": "DFDProcessOval", "shape": "Oval", "description": "Oval-shaped process variant.", "in_ports": "2+", "out_ports": "2+"},
            {"class": "DFDProcessNumbered", "shape": "Rounded rectangle + number", "description": "Numbered process for decomposition.", "in_ports": "2+", "out_ports": "2+"},
            {"class": "DFDProcessMultiIO", "shape": "Rectangle with multiple ports", "description": "Process with multiple input/output ports.", "in_ports": "4+", "out_ports": "4+"},
            {"class": "DFDProcessGroupBoundary", "shape": "Dashed rectangle", "description": "Container grouping related processes.", "in_ports": "0", "out_ports": "0"},
            {"class": "DFDDataStore", "shape": "Open-ended rectangle", "description": "Standard data store notation.", "in_ports": "2 (left/right)", "out_ports": "2 (left/right)"},
            {"class": "DFDDataStoreCylinder", "shape": "Cylinder", "description": "Database-style data store.", "in_ports": "2", "out_ports": "2"},
            {"class": "DFDDataStoreOpenEnded", "shape": "Rectangle open right", "description": "Yourdon-style open-ended store.", "in_ports": "2", "out_ports": "2"},
            {"class": "DFDDataStoreParallel", "shape": "Double line rectangle", "description": "Parallel data store notation.", "in_ports": "2", "out_ports": "2"},
            {"class": "DFDDataStoreShared", "shape": "Rectangle with label", "description": "Shared data store across systems.", "in_ports": "2", "out_ports": "2"},
            {"class": "DFDExternalEntity", "shape": "Rectangle", "description": "External entity (source/sink of data).", "in_ports": "2", "out_ports": "2"},
            {"class": "DFDExternalEntityGaneSarson", "shape": "Rectangle with shadow", "description": "Gane-Sarson external entity notation.", "in_ports": "2", "out_ports": "2"},
            {"class": "DFDNote", "shape": "Rounded note", "description": "Annotation/comment node.", "in_ports": "0", "out_ports": "0"},
            {"class": "DFDTrustBoundary", "shape": "Dashed enclosure", "description": "Trust/security boundary marking.", "in_ports": "0", "out_ports": "0"},
            {"class": "DFDSubsystemContainer", "shape": "Rounded rectangle container", "description": "Container for decomposed subsystem.", "in_ports": "2+", "out_ports": "2+"},
            {"class": "DFDControl", "shape": "Database container", "description": "Control and coordination process.", "in_ports": "2+", "out_ports": "2+"},
        ],
        "conventions": [
            "Processes use consistent naming: 'Verb + Object'",
            "Data flows are labeled with the data being transferred",
            "External entities represent systems outside scope",
            "Data stores use DB cylinder or open-ended rectangle shapes",
        ],
    },
    {
        "file": "erd.html",
        "title": "ERD (Entity-Relationship Diagram) Family",
        "description": "Entity-Relationship Diagrams with entities, attributes, relationships, row-level connection points, multiplicity markers (1:1, 1:N, M:N), and DDL export.",
        "base_control": "ERDControl : MaterialControl",
        "namespace": "Beep.Skia.ERD",
        "example_code": '''var erd = new ERDControl
{
    X = 100, Y = 50,
    Width = 700, Height = 500
};
drawingManager.AddComponent(erd);

var entity = new ERDEntity
{
    X = 200, Y = 100,
    TableName = "Customers",
    Width = 200, Height = 150
};
entity.AddField("Id", "int", isPrimaryKey: true);
entity.AddField("Name", "varchar(100)");
entity.AddField("Email", "varchar(255)");
drawingManager.AddComponent(entity);

var attr = new ERDAttribute
{
    X = 450, Y = 100,
    AttributeName = "Email",
    DataType = "varchar(255)"
};
drawingManager.AddComponent(attr);

var rel = new ERDRelationship
{
    X = 200, Y = 300,
    RelationshipType = RelationshipType.OneToMany,
    Text = "places"
};
drawingManager.AddComponent(rel);

// Row-level connections with multiplicity
drawingManager.Connect(entity.GetFieldPoint("Id"), rel.InPoints[0]);
drawingManager.Connect(rel.OutPoints[0], attr.InPoints[0]);

// Export DDL
var exporter = new DDLExporter();
string sql = exporter.Export(entity);''',
        "nodes": [
            {"class": "ERDEntity", "shape": "Table rectangle", "description": "Database table with columns, primary keys, foreign keys, and row-level connection points for per-column relationships.", "in_ports": "2+ (left)", "out_ports": "2+ (right)", "extra_ports": "1 per field (left/right)"},
            {"class": "ERDAttribute", "shape": "Oval", "description": "Entity attribute with name, data type, and nullable flag.", "in_ports": "1", "out_ports": "1"},
            {"class": "ERDRelationship", "shape": "Diamond", "description": "Relationship between entities with cardinality (1:1, 1:N, M:N) and Crow's Foot notation.", "in_ports": "2+ (left/top)", "out_ports": "2+ (right/bottom)"},
            {"class": "DDLExporter", "shape": "N/A (utility)", "description": "Generates CREATE TABLE SQL from ERDEntity definitions including PKs, FKs, indexes, and constraints."},
        ],
        "conventions": [
            "ERDEntity uses row-level connection points for per-column relationships",
            "Multiplicity markers displayed on line endpoints (1, N, M)",
            "DDLExporter generates SQL from entity metadata",
            "ConnectionPoint.DataType and RowId enable schema validation",
        ],
    },
    {
        "file": "etl.html",
        "title": "ETL (Data Pipeline) Diagram Family",
        "description": "ETL/ELT data pipeline diagrams with 17 transformation nodes covering extract, transform, load, and data quality operations.",
        "base_control": "ETLControl : MaterialControl",
        "namespace": "Beep.Skia.ETL",
        "example_code": '''var etl = new ETLControl
{
    X = 50, Y = 50,
    Width = 800, Height = 500
};
drawingManager.AddComponent(etl);

var src = new ETLSource { X = 100, Y = 250, Text = "SQL Server" };
var filter = new ETLFilter { X = 280, Y = 250, Text = "Filter Active" };
var transform = new ETLTransform { X = 460, Y = 250, Text = "Normalize" };
var tgt = new ETLTarget { X = 640, Y = 250, Text = "Data Warehouse" };

drawingManager.AddComponent(src);
drawingManager.AddComponent(filter);
drawingManager.AddComponent(transform);
drawingManager.AddComponent(tgt);

drawingManager.Connect(src.OutPoints[0], filter.InPoints[0]);
drawingManager.Connect(filter.OutPoints[0], transform.InPoints[0]);
drawingManager.Connect(transform.OutPoints[0], tgt.InPoints[0]);''',
        "nodes": [
            {"class": "ETLSource", "shape": "Table/source icon", "description": "Data extraction source (database, file, API).", "in_ports": "0", "out_ports": "1+"},
            {"class": "ETLTransform", "shape": "Gear/rectangle", "description": "Generic data transformation step.", "in_ports": "1+", "out_ports": "1+"},
            {"class": "ETLTarget", "shape": "Database/target icon", "description": "Data loading destination.", "in_ports": "1+", "out_ports": "0-1"},
            {"class": "ETLDestination", "shape": "Target container", "description": "Alternative destination node.", "in_ports": "1+", "out_ports": "0"},
            {"class": "ETLFilter", "shape": "Funnel", "description": "Row-level filtering by condition.", "in_ports": "1", "out_ports": "1"},
            {"class": "ETLSort", "shape": "Arrow up/down", "description": "Sort operation on data stream.", "in_ports": "1", "out_ports": "1"},
            {"class": "ETLJoin", "shape": "Merge icon", "description": "Join two data streams (inner/left/right/full).", "in_ports": "2", "out_ports": "1"},
            {"class": "ETLMerge", "shape": "Merge (vertical)", "description": "Merge/union compatible data streams.", "in_ports": "2+", "out_ports": "1"},
            {"class": "ETLLookup", "shape": "Magnifying glass", "description": "Reference data lookup/join.", "in_ports": "2", "out_ports": "1"},
            {"class": "ETLAggregate", "shape": "Sigma symbol", "description": "Aggregation (SUM, COUNT, AVG, GROUP BY).", "in_ports": "1", "out_ports": "1"},
            {"class": "ETLPivot", "shape": "Arrow rotate", "description": "Pivot rows to columns.", "in_ports": "1", "out_ports": "1"},
            {"class": "ETLUnpivot", "shape": "Arrow counter-rotate", "description": "Unpivot columns to rows.", "in_ports": "1", "out_ports": "1"},
            {"class": "ETLConditionalSplit", "shape": "Diamond", "description": "Route rows to different outputs based on conditions.", "in_ports": "1", "out_ports": "2+"},
            {"class": "ETLDerivedColumn", "shape": "Column icon", "description": "Compute new column from expression.", "in_ports": "1", "out_ports": "1"},
            {"class": "ETLMulticast", "shape": "Triangle split", "description": "Duplicate stream to multiple outputs.", "in_ports": "1", "out_ports": "2+"},
            {"class": "ETLRowCount", "shape": "Counter", "description": "Count rows passing through.", "in_ports": "1", "out_ports": "1"},
            {"class": "ETLScd", "shape": "Clock icon", "description": "Slowly Changing Dimension (Type 1/2/3).", "in_ports": "1", "out_ports": "1"},
            {"class": "ETLScript", "shape": "Code block", "description": "Custom script/expression execution.", "in_ports": "1+", "out_ports": "1+"},
        ],
        "conventions": [
            "Flow direction: left to right or top to bottom",
            "Sources have no input ports, targets have no output ports",
            "Transforms may have multiple inputs/outputs",
            "Line colors indicate data flow, error flow, or logging flow",
        ],
    },
    {
        "file": "uml.html",
        "title": "UML Diagram Family",
        "description": "UML diagrams with 17 node types covering class, interface, use case, sequence, activity, and deployment views.",
        "base_control": "UMLControl : MaterialControl",
        "namespace": "Beep.Skia.UML",
        "example_code": '''var uml = new UMLControl
{
    X = 50, Y = 50,
    Width = 700, Height = 500
};
drawingManager.AddComponent(uml);

var cls = new UMLClass
{
    X = 100, Y = 80,
    ClassName = "Order",
    Width = 200
};
cls.AddProperty("- Id : int");
cls.AddProperty("- Date : DateTime");
cls.AddMethod("+ CalculateTotal() : decimal");
drawingManager.AddComponent(cls);

var iface = new UMLInterface
{
    X = 400, Y = 80,
    InterfaceName = "IOrderService",
    Width = 200
};
drawingManager.AddComponent(iface);

var inh = new UMLInheritance
{
    StartPoint = cls.OutPoints[0],
    EndPoint = iface.InPoints[0]
};
drawingManager.AddConnection(inh);''',
        "nodes": [
            {"class": "UMLClass", "shape": "3-section rectangle", "description": "Class with name, properties, and methods compartments.", "in_ports": "2+ (sides)", "out_ports": "2+ (sides)"},
            {"class": "UMLInterface", "shape": "Rectangle with <<interface>>", "description": "Interface with stereotype and method signatures.", "in_ports": "2+", "out_ports": "2+"},
            {"class": "UMLActor", "shape": "Stick figure", "description": "Use case actor (user/role/system).", "in_ports": "1", "out_ports": "1"},
            {"class": "UMLLifeline", "shape": "Dashed vertical line", "description": "Sequence diagram lifeline for object/actor.", "in_ports": "0", "out_ports": "1+"},
            {"class": "UMLAssociation", "shape": "Solid line", "description": "Association relationship between classes.", "in_ports": "0", "out_ports": "0"},
            {"class": "UMLInheritance", "shape": "Hollow triangle arrow", "description": "Inheritance/generalization relationship.", "in_ports": "0", "out_ports": "0"},
            {"class": "UMLMessage", "shape": "Arrow line", "description": "Sequence diagram message between lifelines.", "in_ports": "0", "out_ports": "0"},
            {"class": "UMLActionNode", "shape": "Rounded rectangle", "description": "Activity diagram action step.", "in_ports": "1", "out_ports": "1"},
            {"class": "UMLConditionNode", "shape": "Diamond", "description": "Activity diagram decision/branch.", "in_ports": "1", "out_ports": "2+"},
            {"class": "UMLDataSourceNode", "shape": "Database icon", "description": "Data source in deployment/component diagram.", "in_ports": "1", "out_ports": "1"},
            {"class": "UMLTransformNode", "shape": "Gear", "description": "Transform node in component diagram.", "in_ports": "1", "out_ports": "1"},
            {"class": "UMLOutputNode", "shape": "Output icon", "description": "Output/sink node.", "in_ports": "1", "out_ports": "0-1"},
            {"class": "UMLTriggerNode", "shape": "Event icon", "description": "Trigger/event source for activity.", "in_ports": "0", "out_ports": "1"},
        ],
        "conventions": [
            "Class diagram: inheritance uses hollow triangle, association uses open arrow",
            "Sequence diagram: lifelines with activation bars, messages as horizontal arrows",
            "Activity diagram: action nodes connected by control flow arrows",
        ],
    },
    {
        "file": "project-management.html",
        "title": "Project Management Diagram Family",
        "description": "Project management diagrams with 13 node types including tasks, milestones, Gantt bars, resources, risks, and critical path visualization.",
        "base_control": "PMControl : MaterialControl",
        "namespace": "Beep.Skia.PM",
        "example_code": '''var pm = new PMControl
{
    X = 50, Y = 50,
    Width = 700, Height = 400
};
drawingManager.AddComponent(pm);

var task = new TaskNode
{
    X = 150, Y = 100,
    Text = "Implement Login",
    Duration = TimeSpan.FromDays(5)
};
var milestone = new MilestoneNode
{
    X = 400, Y = 100,
    Text = "M1: Alpha Release",
    Date = DateTime.Now.AddDays(30)
};
var dep = new DependencyNode
{
    X = 300, Y = 100,
    DependencyType = "FS" // Finish-to-Start
};

drawingManager.AddComponent(task);
drawingManager.AddComponent(milestone);
drawingManager.AddComponent(dep);

drawingManager.Connect(task.OutPoints[0], dep.InPoints[0]);
drawingManager.Connect(dep.OutPoints[0], milestone.InPoints[0]);''',
        "nodes": [
            {"class": "TaskNode", "shape": "Rectangle with bars", "description": "Work task with duration, start/end dates, completion %.", "in_ports": "1 (left)", "out_ports": "1 (right)"},
            {"class": "MilestoneNode", "shape": "Diamond", "description": "Key milestone with target date.", "in_ports": "1+", "out_ports": "1+"},
            {"class": "PhaseNode", "shape": "Rounded rectangle", "description": "Project phase grouping tasks.", "in_ports": "1", "out_ports": "1"},
            {"class": "SummaryNode", "shape": "Black bar/rectangle", "description": "Summary task aggregating child tasks.", "in_ports": "1", "out_ports": "1"},
            {"class": "DependencyNode", "shape": "Arrow/diamond", "description": "Task dependency (FS, FF, SS, SF).", "in_ports": "1", "out_ports": "1"},
            {"class": "GanttBarNode", "shape": "Horizontal bar", "description": "Visual Gantt chart bar representing task duration.", "in_ports": "0", "out_ports": "0"},
            {"class": "ResourceNode", "shape": "Person/resource icon", "description": "Team member or resource with allocation %.", "in_ports": "1", "out_ports": "1"},
            {"class": "BudgetNode", "shape": "Dollar/metric rectangle", "description": "Budget/cost tracking with planned vs actual.", "in_ports": "1", "out_ports": "1"},
            {"class": "CriticalPathNode", "shape": "Red-highlighted path", "description": "Critical path indicator for longest dependency chain.", "in_ports": "1", "out_ports": "1"},
            {"class": "DeliverableNode", "shape": "Package/box icon", "description": "Project deliverable with acceptance criteria.", "in_ports": "1", "out_ports": "1"},
            {"class": "IssueNode", "shape": "Warning triangle", "description": "Issue/bug tracker with severity and status.", "in_ports": "1", "out_ports": "1"},
            {"class": "RiskNode", "shape": "Risk matrix rectangle", "description": "Risk with probability, impact, and mitigation plan.", "in_ports": "1", "out_ports": "1"},
        ],
        "conventions": [
            "Task dependencies follow standard FS/FF/SS/SF notation",
            "Gantt bars are time-scaled horizontally",
            "Critical path is highlighted in red/orange",
            "Milestones typically connect to preceding tasks",
        ],
    },
    {
        "file": "mindmap.html",
        "title": "MindMap Diagram Family",
        "description": "Mind map diagrams with 4 node types for radial idea visualization from a central topic outward.",
        "base_control": "MindMapControl : MaterialControl",
        "namespace": "Beep.Skia.MindMap",
        "example_code": '''var mm = new MindMapControl
{
    X = 50, Y = 50,
    Width = 600, Height = 400
};
drawingManager.AddComponent(mm);

var central = new CentralNode
{
    X = 300, Y = 200,
    Text = "Product Strategy"
};
var topic = new TopicNode
{
    X = 200, Y = 100,
    Text = "Marketing"
};
var subtopic = new SubTopicNode
{
    X = 100, Y = 50,
    Text = "Social Media"
};

drawingManager.AddComponent(central);
drawingManager.AddComponent(topic);
drawingManager.AddComponent(subtopic);

drawingManager.Connect(central.OutPoints[0], topic.InPoints[0]);
drawingManager.Connect(topic.OutPoints[0], subtopic.InPoints[0]);''',
        "nodes": [
            {"class": "CentralNode", "shape": "Circle/oval (large)", "description": "Central idea/node. Root of the mind map.", "in_ports": "0", "out_ports": "8 (radial)"},
            {"class": "TopicNode", "shape": "Rounded rectangle", "description": "First-level topic branching from central node.", "in_ports": "1 (center-facing)", "out_ports": "4 (radial)"},
            {"class": "SubTopicNode", "shape": "Rounded rectangle (smaller)", "description": "Second-level subtopic. Supports unlimited nesting.", "in_ports": "1", "out_ports": "4"},
            {"class": "NoteNode", "shape": "Callout/sticky note", "description": "Annotation or detail note attached to any node.", "in_ports": "1", "out_ports": "0"},
        ],
        "conventions": [
            "Central node always at origin (0,0 relative to control)",
            "Topics radiate outward from center",
            "Lines use curved routing for organic feel",
            "Colors typically change per depth level",
        ],
    },
    {
        "file": "state-machine.html",
        "title": "State Machine Diagram Family",
        "description": "State machine diagrams with initial, state, and final nodes for modeling system behavior and state transitions.",
        "base_control": "StateMachineControl : MaterialControl",
        "namespace": "Beep.Skia.StateMachine",
        "example_code": '''var sm = new StateMachineControl
{
    X = 50, Y = 50,
    Width = 600, Height = 400
};
drawingManager.AddComponent(sm);

var init = new InitialStateNode { X = 100, Y = 200 };
var idle = new StateNode { X = 250, Y = 200, StateName = "Idle" };
var active = new StateNode { X = 400, Y = 100, StateName = "Active" };
var final = new FinalStateNode { X = 400, Y = 300 };

drawingManager.AddComponent(init);
drawingManager.AddComponent(idle);
drawingManager.AddComponent(active);
drawingManager.AddComponent(final);

drawingManager.Connect(init.OutPoints[0], idle.InPoints[0]);
drawingManager.Connect(idle.OutPoints[0], active.InPoints[0]);
drawingManager.Connect(active.OutPoints[0], final.InPoints[0]);''',
        "nodes": [
            {"class": "InitialStateNode", "shape": "Filled circle (black)", "description": "Initial pseudo-state. Where the state machine begins.", "in_ports": "0", "out_ports": "1"},
            {"class": "StateNode", "shape": "Rounded rectangle", "description": "Named state with optional entry/exit/do actions and internal transitions.", "in_ports": "1+", "out_ports": "1+"},
            {"class": "FinalStateNode", "shape": "Circle with inner filled circle (bullseye)", "description": "Final termination state.", "in_ports": "1+", "out_ports": "0"},
        ],
        "conventions": [
            "Initial state has no inputs, one output transition",
            "State transitions are labeled with trigger[guard]/action",
            "Final state has inputs but no outputs",
            "States may contain do/entry/exit activity compartments",
        ],
    },
    {
        "file": "network.html",
        "title": "Network Graph Visualization Family",
        "description": "Network graph analysis and visualization with 12 components including node clustering, path finding, centrality measures, community detection, and graph navigation.",
        "base_control": "NetworkControl : MaterialControl",
        "namespace": "Beep.Skia.Network",
        "example_code": '''var net = new NetworkControl
{
    X = 50, Y = 50,
    Width = 700, Height = 500
};
drawingManager.AddComponent(net);

var graph = new NetworkGraph();
var n1 = graph.AddNode("A", x: 100, y: 100);
var n2 = graph.AddNode("B", x: 250, y: 80);
var n3 = graph.AddNode("C", x: 250, y: 180);
graph.AddLink(n1, n2, weight: 1.5);
graph.AddLink(n1, n3, weight: 2.0);
graph.AddLink(n2, n3, weight: 0.8);

var finder = new PathFinder(graph);
var path = finder.FindShortestPath(n1, n3);

var centrality = new CentralityMeasure(graph);
var scores = centrality.CalculateBetweenness();''',
        "nodes": [
            {"class": "NetworkNode", "shape": "Circle with label", "description": "Graph node/vertex with position, size, color, and label.", "in_ports": "Flexible", "out_ports": "Flexible"},
            {"class": "NetworkLink", "shape": "Line with weight", "description": "Weighted edge between two nodes. Supports directed/undirected.", "in_ports": "0", "out_ports": "0"},
            {"class": "NetworkGraph", "shape": "N/A (data structure)", "description": "Graph data structure: adjacency list, node/link management, serialization.", "in_ports": "0", "out_ports": "0"},
            {"class": "PathFinder", "shape": "N/A (algorithm)", "description": "Shortest path algorithms: Dijkstra, A*, Floyd-Warshall.", "in_ports": "0", "out_ports": "0"},
            {"class": "CentralityMeasure", "shape": "N/A (algorithm)", "description": "Node centrality: degree, betweenness, closeness, eigenvector, PageRank.", "in_ports": "0", "out_ports": "0"},
            {"class": "CommunityDetector", "shape": "N/A (algorithm)", "description": "Community detection: Louvain, label propagation, Girvan-Newman.", "in_ports": "0", "out_ports": "0"},
            {"class": "GraphNavigator", "shape": "N/A (interaction)", "description": "Graph exploration controls: zoom, pan, focus/unfocus, expand/collapse.", "in_ports": "0", "out_ports": "0"},
            {"class": "LayoutSelector", "shape": "N/A (UI)", "description": "Layout algorithm picker: force-directed, circular, hierarchical, grid, radial.", "in_ports": "0", "out_ports": "0"},
            {"class": "FilterPanel", "shape": "N/A (UI)", "description": "Node/link filtering by attributes, degree threshold, community membership.", "in_ports": "0", "out_ports": "0"},
            {"class": "ExportPanel", "shape": "N/A (UI)", "description": "Export graph as PNG, SVG, JSON, GEXF, GraphML.", "in_ports": "0", "out_ports": "0"},
            {"class": "NodeCluster", "shape": "Group/hull", "description": "Visual cluster grouping of related nodes with convex hull or bounding box.", "in_ports": "0", "out_ports": "0"},
            {"class": "NetworkAnalyzer", "shape": "N/A (algorithm)", "description": "Graph statistics: density, diameter, clustering coefficient, modularity.", "in_ports": "0", "out_ports": "0"},
        ],
        "conventions": [
            "NetworkGraph is the primary data model; NetworkNode/NetworkLink are visual",
            "Layout algorithms rearrange nodes to minimize edge crossings",
            "Analysis components operate on the graph structure, not the visual layer",
        ],
    },
    {
        "file": "business-process.html",
        "title": "Business Process Diagram Family",
        "description": "BPMN-inspired business process diagrams with 31 node types covering events, tasks, gateways, roles, data objects, and rule flows.",
        "base_control": "BusinessControl : MaterialControl",
        "namespace": "Beep.Skia.Business",
        "example_code": '''var biz = new BusinessControl
{
    X = 50, Y = 50,
    Width = 700, Height = 500
};
drawingManager.AddComponent(biz);

var start = new StartEvent { X = 80, Y = 200, EventName = "Order Received" };
var task = new BusinessTask { X = 220, Y = 200, TaskName = "Validate Order" };
var decision = new Decision { X = 380, Y = 200, DecisionName = "Valid?" };
var sys = new BusinessSystem { X = 200, Y = 350, SystemName = "ERP" };
var end = new EndEvent { X = 550, Y = 200, EventName = "Order Processed" };

drawingManager.AddComponent(start);
drawingManager.AddComponent(task);
drawingManager.AddComponent(decision);
drawingManager.AddComponent(sys);
drawingManager.AddComponent(end);

drawingManager.Connect(start.OutPoints[0], task.InPoints[0]);
drawingManager.Connect(task.OutPoints[0], decision.InPoints[0]);
drawingManager.Connect(decision.OutPoints[1], sys.InPoints[0]);''',
        "nodes": [
            {"class": "StartEvent", "shape": "Circle (thin)", "description": "Process start trigger.", "in_ports": "0", "out_ports": "1"},
            {"class": "EndEvent", "shape": "Circle (thick)", "description": "Process end/termination.", "in_ports": "1+", "out_ports": "0"},
            {"class": "TaskNode", "shape": "Rounded rectangle", "description": "Generic task/activity.", "in_ports": "1", "out_ports": "1"},
            {"class": "BusinessTask", "shape": "Rounded rectangle + icon", "description": "Business task with role assignment.", "in_ports": "1", "out_ports": "1"},
            {"class": "ActionNode", "shape": "Rounded rectangle", "description": "Manual/user action step.", "in_ports": "1", "out_ports": "1"},
            {"class": "Decision", "shape": "Diamond", "description": "Exclusive gateway (XOR) decision point.", "in_ports": "1", "out_ports": "2+"},
            {"class": "Gateway", "shape": "Diamond with icon", "description": "Generic gateway (AND/OR/XOR).", "in_ports": "1+", "out_ports": "1+"},
            {"class": "SubProcess", "shape": "Rounded rectangle + expand icon", "description": "Expandable sub-process with child diagram.", "in_ports": "1", "out_ports": "1"},
            {"class": "EventNode", "shape": "Circle (intermediate)", "description": "Intermediate event (timer, message, error, signal).", "in_ports": "1", "out_ports": "1"},
            {"class": "Annotation", "shape": "Open bracket with text", "description": "Text annotation for any element.", "in_ports": "0", "out_ports": "0"},
            {"class": "Database", "shape": "Cylinder", "description": "Database data store.", "in_ports": "1", "out_ports": "1"},
            {"class": "DataStore", "shape": "Open-ended rectangle", "description": "Data repository (XML, file, document store).", "in_ports": "1", "out_ports": "1"},
            {"class": "DataObject", "shape": "Document with folded corner", "description": "Data object passed between activities.", "in_ports": "1", "out_ports": "1"},
            {"class": "Document", "shape": "Document icon", "description": "Document/report artifact.", "in_ports": "1", "out_ports": "1"},
            {"class": "ExternalData", "shape": "Cloud/database", "description": "External data source connection.", "in_ports": "1", "out_ports": "1"},
            {"class": "Department", "shape": "Swimlane", "description": "Organizational department/unit swimlane.", "in_ports": "0", "out_ports": "0"},
            {"class": "Person", "shape": "Person icon", "description": "Individual actor/role.", "in_ports": "1", "out_ports": "1"},
            {"class": "Role", "shape": "Tag/label", "description": "Role definition for task assignment.", "in_ports": "1", "out_ports": "1"},
            {"class": "Group", "shape": "Border/container", "description": "Group of related activities.", "in_ports": "0", "out_ports": "0"},
            {"class": "BusinessSystem", "shape": "Server/system box", "description": "IT system/service performing automated tasks.", "in_ports": "1", "out_ports": "1"},
            {"class": "RuleEngine", "shape": "Hexagon/gear", "description": "Business rules engine node.", "in_ports": "1", "out_ports": "1"},
            {"class": "RuleFlow", "shape": "Arrow with condition", "description": "Conditional flow governed by business rules.", "in_ports": "0", "out_ports": "0"},
            {"class": "ConditionBuilder", "shape": "Expression builder", "description": "Visual condition/expression builder.", "in_ports": "1", "out_ports": "1"},
        ],
        "conventions": [
            "Start events have no inputs; end events have no outputs",
            "Gateways route flow based on conditions",
            "Swimlanes (Department) partition the diagram horizontally",
            "Data objects connect to activities via dashed association lines",
        ],
    },
    {
        "file": "cloud.html",
        "title": "Cloud Architecture Diagram Family",
        "description": "Cloud infrastructure diagrams with storage, compute, function, and database nodes for AWS, Azure, and GCP architecture visualization.",
        "base_control": "CloudControl : MaterialControl",
        "namespace": "Beep.Skia.Cloud",
        "example_code": '''var cloud = new CloudControl
{
    X = 50, Y = 50,
    Width = 700, Height = 500
};
drawingManager.AddComponent(cloud);

var storage = new CloudStorageNode
{
    X = 100, Y = 100,
    Text = "S3 Bucket",
    StorageType = "Object Storage"
};
var compute = new CloudComputeNode
{
    X = 300, Y = 100,
    Text = "EC2 Instance",
    InstanceType = "t3.medium"
};
var func = new CloudFunctionNode
{
    X = 300, Y = 250,
    Text = "Lambda: ProcessImage"
};
var db = new CloudDatabaseNode
{
    X = 500, Y = 100,
    Text = "RDS PostgreSQL",
    EngineType = "PostgreSQL"
};

drawingManager.AddComponent(storage);
drawingManager.AddComponent(compute);
drawingManager.AddComponent(func);
drawingManager.AddComponent(db);

drawingManager.Connect(storage.OutPoints[0], compute.InPoints[0]);
drawingManager.Connect(compute.OutPoints[0], db.InPoints[0]);
drawingManager.Connect(storage.OutPoints[1], func.InPoints[0]);''',
        "nodes": [
            {"class": "CloudStorageNode", "shape": "File/folder bucket icon", "description": "Cloud storage service (AWS S3, Azure Blob, GCP Cloud Storage).", "in_ports": "1+", "out_ports": "1+"},
            {"class": "CloudComputeNode", "shape": "Server/instance icon", "description": "Virtual machine/container compute (EC2, Azure VM, Compute Engine).", "in_ports": "1+", "out_ports": "1+"},
            {"class": "CloudFunctionNode", "shape": "Lambda/function icon", "description": "Serverless function (AWS Lambda, Azure Functions, Cloud Functions).", "in_ports": "1+", "out_ports": "1+"},
            {"class": "CloudDatabaseNode", "shape": "Database cylinder", "description": "Managed database service (RDS, Cosmos DB, Cloud SQL).", "in_ports": "1+", "out_ports": "1+"},
        ],
        "conventions": [
            "Nodes represent cloud services with provider-agnostic shapes",
            "Connection lines represent data flow, API calls, or network routes",
            "Properties allow specifying provider, region, SKU, and configuration",
        ],
    },
    {
        "file": "ecad.html",
        "title": "ECAD (Electronic CAD) Diagram Family",
        "description": "Electronic circuit diagrams with 18 component nodes covering passive components, semiconductors, ICs, power, and tracing.",
        "base_control": "ECADControl : MaterialControl",
        "namespace": "Beep.Skia.ECAD",
        "example_code": '''var ecad = new ECADControl
{
    X = 50, Y = 50,
    Width = 700, Height = 500
};
drawingManager.AddComponent(ecad);

var bat = new ECADBatteryNode { X = 100, Y = 200, Voltage = "9V" };
var res = new ECADResistorNode { X = 250, Y = 150, Resistance = "1k\u03A9" };
var led = new ECADDiodeNode { X = 400, Y = 150, ForwardVoltage = "2.1V" };
var gnd = new ECADGroundNode { X = 400, Y = 300 };

drawingManager.AddComponent(bat);
drawingManager.AddComponent(res);
drawingManager.AddComponent(led);
drawingManager.AddComponent(gnd);

drawingManager.Connect(bat.OutPoints[0], res.InPoints[0]);
drawingManager.Connect(res.OutPoints[0], led.InPoints[0]);
drawingManager.Connect(led.OutPoints[0], gnd.InPoints[0]);''',
        "nodes": [
            {"class": "ECADResistorNode", "shape": "Zigzag line", "description": "Resistor with value and tolerance.", "in_ports": "1", "out_ports": "1"},
            {"class": "ECADCapacitorNode", "shape": "Parallel plates", "description": "Capacitor (polarized or non-polarized).", "in_ports": "1", "out_ports": "1"},
            {"class": "ECADDiodeNode", "shape": "Triangle with bar", "description": "Diode including LED, Zener, Schottky variants.", "in_ports": "1", "out_ports": "1"},
            {"class": "ECADFuseNode", "shape": "Wire with break", "description": "Fuse/circuit breaker.", "in_ports": "1", "out_ports": "1"},
            {"class": "ECADICNode", "shape": "Rectangle with pin grid", "description": "Integrated circuit with configurable pin count.", "in_ports": "4+ (left)", "out_ports": "4+ (right)"},
            {"class": "ECADInductorNode", "shape": "Coil loops", "description": "Inductor/coil with inductance value.", "in_ports": "1", "out_ports": "1"},
            {"class": "ECADOpAmpNode", "shape": "Triangle with +/- inputs", "description": "Operational amplifier with inverting/non-inverting inputs.", "in_ports": "2", "out_ports": "1"},
            {"class": "ECADTransistorNode", "shape": "Circle with 3 legs", "description": "BJT (NPN/PNP) with collector, base, emitter.", "in_ports": "1", "out_ports": "1"},
            {"class": "ECADGroundNode", "shape": "Ground symbol", "description": "Ground/reference point.", "in_ports": "1", "out_ports": "0"},
            {"class": "ECADBatteryNode", "shape": "Long/short line pair", "description": "DC power source.", "in_ports": "0", "out_ports": "1"},
            {"class": "ECADPowerSupplyNode", "shape": "Circle with +/-", "description": "Power supply unit with voltage/current output.", "in_ports": "0", "out_ports": "1+"},
            {"class": "ECADVoltageRegulatorNode", "shape": "Rectangle with Vout", "description": "Linear/switching voltage regulator.", "in_ports": "1", "out_ports": "1"},
            {"class": "ECADTransformerNode", "shape": "Two coils", "description": "Transformer with primary/secondary windings.", "in_ports": "2", "out_ports": "2"},
            {"class": "ECADLogicGateNode", "shape": "AND/OR/NOT gate symbol", "description": "Logic gate (AND, OR, NOT, NAND, NOR, XOR).", "in_ports": "2+", "out_ports": "1"},
            {"class": "ECADMicrocontrollerNode", "shape": "Rectangle with pins", "description": "Microcontroller/MCU with GPIO, ADC, UART, I2C.", "in_ports": "8+", "out_ports": "8+"},
            {"class": "ECADMemoryNode", "shape": "Rectangle with address/data", "description": "Memory chip (RAM, ROM, EEPROM, Flash).", "in_ports": "4+", "out_ports": "4+"},
            {"class": "ECADTraceNode", "shape": "Line with width", "description": "PCB trace/routing line with configurable width.", "in_ports": "0", "out_ports": "0"},
        ],
        "conventions": [
            "Components follow IEEE/ANSI standard symbols",
            "IC pins are configurable via NodeProperties",
            "Ground and power are global reference nodes",
            "Traces connect component ports",
        ],
    },
    {
        "file": "ml.html",
        "title": "Machine Learning Diagram Family",
        "description": "Machine learning pipeline diagrams with 20 node types covering data preparation, model training, evaluation, inference, and neural network architectures.",
        "base_control": "MLControl : MaterialControl",
        "namespace": "Beep.Skia.ML",
        "example_code": '''var ml = new MLControl
{
    X = 50, Y = 50,
    Width = 800, Height = 500
};
drawingManager.AddComponent(ml);

var data = new MLDataSourceNode { X = 80, Y = 100, Text = "Training Data" };
var pre = new MLPreprocessNode { X = 220, Y = 100, Text = "Normalize" };
var split = new MLDataSplitterNode { X = 360, Y = 100, TrainRatio = 0.8 };
var model = new MLModelNode { X = 500, Y = 60, ModelType = "RandomForest" };
var eval = new MLEvaluateNode { X = 640, Y = 100 };

drawingManager.AddComponent(data);
drawingManager.AddComponent(pre);
drawingManager.AddComponent(split);
drawingManager.AddComponent(model);
drawingManager.AddComponent(eval);

drawingManager.Connect(data.OutPoints[0], pre.InPoints[0]);
drawingManager.Connect(pre.OutPoints[0], split.InPoints[0]);
drawingManager.Connect(split.OutPoints[0], model.InPoints[0]);
drawingManager.Connect(model.OutPoints[0], eval.InPoints[0]);''',
        "nodes": [
            {"class": "MLDataSourceNode", "shape": "Database/table", "description": "Training/test data source with format, size, and schema.", "in_ports": "0", "out_ports": "1+"},
            {"class": "MLPreprocessNode", "shape": "Gear/clean icon", "description": "Data preprocessing: normalization, encoding, imputation.", "in_ports": "1", "out_ports": "1"},
            {"class": "MLFeatureEngineeringNode", "shape": "Feature icon", "description": "Feature creation, selection, transformation.", "in_ports": "1", "out_ports": "1"},
            {"class": "MLDataAugmentationNode", "shape": "Multiply icon", "description": "Data augmentation (rotation, flip, noise, synthetic).", "in_ports": "1", "out_ports": "1"},
            {"class": "MLDataSplitterNode", "shape": "Split arrow", "description": "Train/validation/test split with configurable ratios.", "in_ports": "1", "out_ports": "2-3"},
            {"class": "MLModelNode", "shape": "Brain/model icon", "description": "ML model with hyperparameters: learning rate, layers, epochs.", "in_ports": "1+", "out_ports": "1+"},
            {"class": "MLTrainerNode", "shape": "Training icon", "description": "Model training node with optimizer, loss function, batch size.", "in_ports": "2 (data + model)", "out_ports": "1 (trained model)"},
            {"class": "MLEvaluateNode", "shape": "Checkmark/target", "description": "Model evaluation: accuracy, precision, recall, F1, ROC.", "in_ports": "1", "out_ports": "1"},
            {"class": "MLInferenceNode", "shape": "Forward arrow", "description": "Real-time inference/prediction node.", "in_ports": "2 (data + model)", "out_ports": "1 (predictions)"},
            {"class": "MLBatchPredictionNode", "shape": "Batch icon", "description": "Batch prediction on dataset.", "in_ports": "2", "out_ports": "1"},
            {"class": "MLCrossValidationNode", "shape": "K-fold icon", "description": "K-fold cross-validation with fold metrics.", "in_ports": "1", "out_ports": "1"},
            {"class": "MLHyperparameterTuningNode", "shape": "Tuning knob", "description": "Grid/random/Bayesian hyperparameter optimization.", "in_ports": "1", "out_ports": "1"},
            {"class": "MLModelExportNode", "shape": "Export/disk icon", "description": "Export model to ONNX, PMML, pickle, TensorFlow SavedModel.", "in_ports": "1", "out_ports": "0"},
            {"class": "MLModelLoadNode", "shape": "Import/disk icon", "description": "Load pre-trained model from file/registry.", "in_ports": "0", "out_ports": "1"},
            {"class": "MLCNNNode", "shape": "Convolution grid", "description": "Convolutional Neural Network: conv, pool, flatten, dense.", "in_ports": "1", "out_ports": "1"},
            {"class": "MLRNNNode", "shape": "Recurrent icon", "description": "Recurrent Neural Network: LSTM/GRU with sequence length.", "in_ports": "1", "out_ports": "1"},
            {"class": "MLNeuralNetworkNode", "shape": "Network layers", "description": "Dense/feedforward neural network with configurable layers.", "in_ports": "1", "out_ports": "1"},
            {"class": "MLTransformerNode", "shape": "Attention grid", "description": "Transformer architecture: attention, feedforward, layer norm.", "in_ports": "1", "out_ports": "1"},
        ],
        "conventions": [
            "Data flows left to right through the pipeline",
            "Data splitter produces train/val/test outputs",
            "Trainer takes separate data and model inputs",
            "Model export is a terminal node (no outputs)",
        ],
    },
    {
        "file": "security.html",
        "title": "Security Diagram Family",
        "description": "Security and threat modeling diagrams with 9 node types including assets, threats, controls, vulnerabilities, risk assessments, and mitigation links.",
        "base_control": "SecurityControl : MaterialControl",
        "namespace": "Beep.Skia.Security",
        "example_code": '''var sec = new SecurityControl
{
    X = 50, Y = 50,
    Width = 700, Height = 500
};
drawingManager.AddComponent(sec);

var asset = new AssetNode
{
    X = 100, Y = 150,
    Text = "Customer Database",
    AssetType = "Data"
};
var threat = new ThreatNode
{
    X = 350, Y = 80,
    ThreatType = "SQL Injection",
    Severity = "High"
};
var control = new ControlNode
{
    X = 350, Y = 220,
    ControlType = "Input Validation",
    ControlCategory = "Preventive"
};
var vuln = new VulnerabilityNode
{
    X = 600, Y = 150,
    CVE = "CVE-2023-XXXXX"
};

drawingManager.AddComponent(asset);
drawingManager.AddComponent(threat);
drawingManager.AddComponent(control);
drawingManager.AddComponent(vuln);

drawingManager.Connect(asset.OutPoints[0], threat.InPoints[0]);
drawingManager.Connect(threat.OutPoints[1], vuln.InPoints[0]);
drawingManager.Connect(control.OutPoints[0], threat.InPoints[1]);''',
        "nodes": [
            {"class": "AssetNode", "shape": "Diamond/server", "description": "Valuable asset (data, system, service, personnel).", "in_ports": "1+", "out_ports": "2+"},
            {"class": "ThreatNode", "shape": "Lightning bolt", "description": "Threat actor/event with type, likelihood, and impact.", "in_ports": "2+", "out_ports": "2+"},
            {"class": "ControlNode", "shape": "Shield", "description": "Security control/safeguard (preventive, detective, corrective).", "in_ports": "1+", "out_ports": "1+"},
            {"class": "VulnerabilityNode", "shape": "Crack/broken shield", "description": "Vulnerability with CVE, CVSS score, and remediation.", "in_ports": "1", "out_ports": "1"},
            {"class": "PolicyNode", "shape": "Document", "description": "Security policy/standard with compliance mapping.", "in_ports": "1", "out_ports": "1"},
            {"class": "RiskAssessmentNode", "shape": "Risk matrix", "description": "Risk assessment: inherent and residual risk levels.", "in_ports": "2+", "out_ports": "1+"},
            {"class": "IncidentNode", "shape": "Alert triangle", "description": "Security incident with timeline and response status.", "in_ports": "1", "out_ports": "1"},
            {"class": "FindingNode", "shape": "Checklist/document", "description": "Audit/assessment finding with recommendation.", "in_ports": "1", "out_ports": "1"},
            {"class": "MitigationLink", "shape": "Dashed line", "description": "Mitigation relationship: control mitigates threat/vulnerability.", "in_ports": "0", "out_ports": "0"},
        ],
        "conventions": [
            "Assets connect to threats they face",
            "Controls mitigate threats and reduce vulnerabilities",
            "Vulnerabilities link to assets they affect",
            "Mitigation links use dashed lines to distinguish from data flow",
        ],
    },
    {
        "file": "quantitative.html",
        "title": "Quantitative/Financial Diagram Family",
        "description": "Quantitative finance and trading diagrams with time series, technical indicators, statistical nodes, and strategy visualization.",
        "base_control": "QuantControl : MaterialControl",
        "namespace": "Beep.Ski.Quantitative",
        "example_code": '''var quant = new QuantControl
{
    X = 50, Y = 50,
    Width = 700, Height = 500
};
drawingManager.AddComponent(quant);

var ts = new TimeSeriesNode
{
    X = 100, Y = 150,
    Symbol = "AAPL",
    Interval = "1d"
};
var sma = new IndicatorNode
{
    X = 280, Y = 150,
    IndicatorType = "SMA",
    Period = 20
};
var strategy = new StrategyNode
{
    X = 460, Y = 150,
    StrategyType = "MovingAverageCrossover"
};

drawingManager.AddComponent(ts);
drawingManager.AddComponent(sma);
drawingManager.AddComponent(strategy);

drawingManager.Connect(ts.OutPoints[0], sma.InPoints[0]);
drawingManager.Connect(sma.OutPoints[0], strategy.InPoints[0]);''',
        "nodes": [
            {"class": "TimeSeriesNode", "shape": "Chart/candlestick", "description": "OHLCV time series with symbol, interval, and date range.", "in_ports": "0", "out_ports": "1+"},
            {"class": "IndicatorNode", "shape": "Indicator panel", "description": "Technical indicator: SMA, EMA, RSI, MACD, Bollinger, Stochastic.", "in_ports": "1", "out_ports": "1"},
            {"class": "DataNodes", "shape": "Data block", "description": "Input/transformed data including fundamentals and alternative data.", "in_ports": "1", "out_ports": "1"},
            {"class": "IndicatorNodes", "shape": "Multi-indicator", "description": "Composite indicator combining multiple signals.", "in_ports": "2+", "out_ports": "1"},
            {"class": "StatisticalNodes", "shape": "Statistics icon", "description": "Statistical analysis: correlation, regression, distribution tests.", "in_ports": "1+", "out_ports": "1"},
            {"class": "StrategyNodes", "shape": "Strategy cog/icon", "description": "Trading strategy with entry/exit rules and position sizing.", "in_ports": "2+", "out_ports": "1"},
        ],
        "conventions": [
            "TimeSeries is always the data source (no inputs)",
            "Indicators chain left-to-right from data to strategy",
            "Strategies may output to backtesting or execution nodes",
        ],
    },
    {
        "file": "welllogs.html",
        "title": "Well Logs Visualization Family",
        "description": "Oil & Gas well log visualization with layout engine, brush library, templates, and renderer supporting LAS 2.0/3.0 and DLIS/RP66 formats.",
        "base_control": "WellLogCanvas",
        "namespace": "Beep.Skia.WellLogs",
        "example_code": '''// Load LAS file
var lasData = WellLogModels.LoadFromLas("well.las");

// Create canvas
var canvas = new WellLogCanvas
{
    X = 50, Y = 50,
    Width = 800, Height = 600,
    DepthStart = 0,
    DepthEnd = 5000
};

// Configure tracks
var track1 = new WellLogTemplates.GammaRayTrack();
var track2 = new WellLogTemplates.ResistivityTrack();
canvas.AddTrack(track1);
canvas.AddTrack(track2);

// Set data and render
canvas.SetData(lasData);
var renderer = new WellLogRenderer(canvas);
renderer.Render(surface, canvas);''',
        "nodes": [
            {"class": "WellLogModels", "shape": "N/A (data layer)", "description": "LAS 2.0/3.0 and DLIS/RP66 file parsing. Curve data models, well header, and metadata.", "in_ports": "N/A", "out_ports": "N/A"},
            {"class": "WellLogCanvas", "shape": "Canvas container", "description": "Main canvas with depth range, track management, grid display, and zoom/scroll.", "in_ports": "N/A", "out_ports": "N/A"},
            {"class": "WellLogLayoutEngine", "shape": "N/A (layout)", "description": "Track layout calculation: widths, headers, depth grids, curve scaling.", "in_ports": "N/A", "out_ports": "N/A"},
            {"class": "WellLogRenderer", "shape": "N/A (rendering)", "description": "SkiaSharp-based renderer for curves, fills, lithology patterns, and annotations.", "in_ports": "N/A", "out_ports": "N/A"},
            {"class": "WellLogBrushLibrary", "shape": "N/A (styling)", "description": "Pre-built brush/fill patterns for lithology: sandstone, shale, limestone, etc.", "in_ports": "N/A", "out_ports": "N/A"},
            {"class": "WellLogTemplates", "shape": "N/A (templates)", "description": "Pre-configured track templates: Gamma Ray, Resistivity, Density, Neutron, Sonic.", "in_ports": "N/A", "out_ports": "N/A"},
        ],
        "conventions": [
            "Depth axis runs from top (shallow) to bottom (deep)",
            "Multiple tracks share the same depth column",
            "Curves are displayed per-track with independent scales",
        ],
    },
]

ARCH_PAGES = [
    {
        "file": "component-registry.html",
        "title": "SkiaComponentRegistry Architecture",
        "section_name": "Architecture & Internals",
        "description": "The static registry that discovers, loads, caches, and instantiates all SkiaComponent subclasses for dynamic plugin loading.",
        "content": '''<p><strong>Namespace:</strong> <code>Beep.Skia</code> &mdash; <strong>Lines:</strong> 661</p>
<p>The <code>SkiaComponentRegistry</code> is the central plugin discovery and instantiation system. It scans assemblies, resolves types, and provides factory methods for all <code>SkiaComponent</code> subclasses at runtime.</p>
<h3>Key Responsibilities</h3>
<ul>
<li>Assembly scanning for <code>[AddinAttribute]</code>-marked components</li>
<li>Type caching by display name and full type name</li>
<li>Instantiation with parameterless constructors</li>
<li>Category-based grouping for palette display</li>
<li>Integration with <code>AssemblyHandler</code> for NuGet-loaded plugins</li>
</ul>''',
    },
    {
        "file": "rendering-pipeline.html",
        "title": "Rendering Pipeline Architecture",
        "section_name": "Architecture & Internals",
        "description": "The complete rendering pipeline from DrawingManager through SkiaComponent to the SkiaSharp canvas.",
        "content": '''<p>The rendering pipeline transforms component state into pixels on screen through a multi-stage process managed by <code>RenderingHelper</code>.</p>
<h3>Pipeline Stages</h3>
<ol>
<li><strong>Canvas Preparation:</strong> Clear canvas, apply pan/zoom transform</li>
<li><strong>Grid Rendering:</strong> Draw background grid if enabled (minor + major lines)</li>
<li><strong>Connection Lines:</strong> Render all connection lines (behind components)</li>
<li><strong>Component Rendering:</strong> For each component: render background, text, icon, overlay</li>
<li><strong>Port Rendering:</strong> Draw connection points (visible when component selected)</li>
<li><strong>Selection Overlay:</strong> Marquee rectangle, resize handles, focus indicator</li>
</ol>''',
    },
    {
        "file": "interaction-system.html",
        "title": "Interaction System Architecture",
        "section_name": "Architecture & Internals",
        "description": "Mouse and keyboard interaction routing through InteractionHelper and SelectionManager.",
        "content": '''<p>The interaction system routes all user input events through <code>InteractionHelper</code> with a hit-test priority chain.</p>
<h3>Hit-Test Priority</h3>
<ol>
<li><strong>Connection Points:</strong> If cursor is over a port (small radius), port interaction mode</li>
<li><strong>Selection Handles:</strong> If cursor is over a resize/move handle, handle mode</li>
<li><strong>Component Body:</strong> Full component hit test via bounding rectangle</li>
<li><strong>Connection Lines:</strong> Line proximity test for click-to-select</li>
<li><strong>Canvas:</strong> Background click = marquee select or deselect all</li>
</ol>
<h3>Drag Modes</h3>
<ul>
<li><strong>None:</strong> No active drag</li>
<li><strong>ComponentMove:</strong> Dragging selected component(s)</li>
<li><strong>PortConnect:</strong> Drawing a new connection line from a port</li>
<li><strong>MarqueeSelect:</strong> Rubber-band selection rectangle</li>
<li><strong>CanvasPan:</strong> Middle-mouse or space+drag panning</li>
</ul>''',
    },
    {
        "file": "serialization-system.html",
        "title": "Serialization System Architecture",
        "section_name": "Architecture & Internals",
        "description": "JSON-based diagram serialization and deserialization with component registry integration.",
        "content": '''<p>The serialization system in <code>DiagramSerialization</code> converts the visual diagram state to/from JSON DTOs.</p>
<h3>Serialization Flow</h3>
<ol>
<li><code>DrawingManager.ToDto()</code> creates a <code>DiagramDto</code> with component and connection collections</li>
<li>Each component serializes its <code>X</code>, <code>Y</code>, <code>Width</code>, <code>Height</code>, <code>TypeName</code>, and <code>NodeProperties</code></li>
<li>Connections store start/end point GUIDs for stable reconnection</li>
<li>JSON output is compact for file storage</li>
</ol>
<h3>Deserialization Flow</h3>
<ol>
<li>Parse JSON into <code>DiagramDto</code></li>
<li>Look up component types via <code>SkiaComponentRegistry</code></li>
<li>Instantiate components, restore properties</li>
<li>Rebuild connections by GUID lookup in <code>DrawingManager.ConnectionPointIndex</code></li>
</ol>''',
    },
    {
        "file": "material-design-system.html",
        "title": "Material Design 3.0 Theming Architecture",
        "section_name": "Architecture & Internals",
        "description": "Material Design 3.0 color token system, border rendering, and text theming in MaterialControl.",
        "content": '''<p><code>MaterialControl</code> and <code>MaterialDesignColors</code> implement a complete Material Design 3.0 token system.</p>
<h3>Color Tokens</h3>
<ul>
<li><strong>Primary:</strong> Main brand color for buttons, active states, emphasis</li>
<li><strong>OnPrimary:</strong> Contrast color for content on primary surfaces</li>
<li><strong>Surface:</strong> Background color for cards, dialogs, sheets</li>
<li><strong>OnSurface:</strong> Text/icon color on surface backgrounds</li>
<li><strong>Outline:</strong> Border and separator colors</li>
<li><strong>Error:</strong> Error state color for validation</li>
</ul>
<h3>Border Rendering</h3>
<p>MaterialControl provides 14 border rendering styles: None, Solid, Dashed, Dotted, Double, Groove, Ridge, Inset, Outset, Rounded, Shadow, Underline, Glow, and Gradient. Each uses pre-computed <code>SKPaint</code> objects for performance.</p>''',
    },
    {
        "file": "layout-engines.html",
        "title": "Layout Engines Architecture",
        "section_name": "Architecture & Internals",
        "description": "FlowLayout, GridLayout, StackLayout, and ILayoutManager for automatic component positioning.",
        "content": '''<p>The layout subsystem provides automatic positioning of components within containers.</p>
<h3>Available Layouts</h3>
<ul>
<li><strong>FlowLayout:</strong> Left-to-right, top-to-bottom wrapping (like HTML inline flow)</li>
<li><strong>GridLayout:</strong> Fixed column count grid with configurable cell sizes</li>
<li><strong>StackLayout:</strong> Vertical or horizontal stacking with spacing and alignment</li>
</ul>
<p>All layout engines implement <code>ILayoutManager</code> which exposes <code>Layout(List<SkiaComponent>)</code>. Layouts recalculate positions on component add/remove/resize events.</p>''',
    },
    {
        "file": "history-undo-system.html",
        "title": "History / Undo-Redo Architecture",
        "section_name": "Architecture & Internals",
        "description": "Command-pattern undo/redo system with HistoryManager and DrawingAction subclasses.",
        "content": '''<p>The undo/redo system uses a command pattern with <code>HistoryManager</code> and concrete <code>DrawingAction</code> subclasses.</p>
<h3>Action Types</h3>
<ul>
<li><strong>AddComponentAction:</strong> Tracks component creation; undo removes, redo re-adds</li>
<li><strong>RemoveComponentAction:</strong> Tracks component deletion with full state snapshot</li>
<li><strong>MoveComponentAction:</strong> Stores old/new positions for a set of components</li>
<li><strong>ConnectAction:</strong> Tracks line creation between connection points</li>
<li><strong>DisconnectAction:</strong> Tracks line removal</li>
<li><strong>PropertyChangeAction:</strong> Stores old/new NodeProperties values</li>
</ul>
<p>Each action implements <code>Execute()</code> and <code>Undo()</code>. The <code>HistoryManager</code> maintains a bounded stack (configurable depth, default 100).</p>''',
    },
]

# ============================================================================
# FAMILY PAGE GENERATOR
# ============================================================================

def generate_family_page(family):
    b = '<div class="page-header"><h1>{}</h1><p class="page-subtitle">{}</p></div>\n'.format(
        family["title"], family["description"])
    
    toc_items = [
        ("overview", "Overview"),
        ("node-types", "Node Types"),
        ("ports", "Connection Points"),
        ("conventions", "Connection Conventions"),
        ("example", "Usage Example"),
    ]
    b += toc(toc_items)
    
    b += section("overview", "Overview",
        '<p><strong>Base Control:</strong> <code>{}</code><br>'
        '<strong>Namespace:</strong> <code>{}</code></p>'
        '<p>The <strong>{}</strong> is the root control for this diagram family. '
        'It inherits <code>MaterialControl</code> and provides shared port management, '
        'styling, and template methods for all nodes in the family.</p>'
        '<p>This family contains <strong>{} node types</strong>.</p>'.format(
            family["base_control"], family["namespace"], family["base_control"].split(" ")[0],
            len(family["nodes"])))
    
    b += section("node-types", "Node Types", node_table(family["nodes"]))
    
    # Per-node details
    for node in family["nodes"]:
        b += subsection(
            node["class"].lower(),
            node["class"],
            '<p>{}</p>'.format(node["description"]) +
            '<table><tr><th style="width:30%">Shape</th><td>{}</td></tr>'
            '<tr><th>Input Ports</th><td>{}</td></tr>'
            '<tr><th>Output Ports</th><td>{}</td></tr></table>'.format(
                node["shape"], node.get("in_ports", "—"), node.get("out_ports", "—"))
        )
    
    b += section("ports", "Connection Points",
        '<p>Every node in this family exposes connection points (ports) for linking to other nodes. '
        'Port positions are computed lazily by <code>LayoutPorts()</code>, which runs when <code>MarkPortsDirty()</code> flags the layout as stale '
        'when the node moves or resizes.</p>' +
        ports_table(family["nodes"]))
    
    conventions = "".join('<li>{}</li>'.format(c) for c in family.get("conventions", []))
    b += section("conventions", "Connection Conventions",
        '<p>When building diagrams with this family, follow these conventions:</p><ul>{}</ul>'.format(conventions))
    
    b += section("example", "Usage Example",
        '<p>Complete example showing creation, node placement, and connection:</p>' +
        code(family["example_code"], "csharp", "C# Example"))
    
    return page(
        family["title"] + " | Beep.Skia Documentation",
        "../",
        breadcrumb([("diagram-families/flowchart.html", "Diagram Families")], family["title"]),
        b)


def generate_arch_page(p):
    b = '<div class="page-header"><h1>{}</h1><p class="page-subtitle">{}</p></div>\n'.format(
        p["title"], p["description"])
    b += p["content"]
    return page(
        p["title"] + " | Beep.Skia Documentation",
        "../",
        breadcrumb([("../architecture/component-registry.html", "Architecture & Internals")], p["title"]),
        b)


# ============================================================================
# MAIN
# ============================================================================

def main():
    fam_dir = os.path.join(BASE, "diagram-families")
    arch_dir = os.path.join(BASE, "architecture")
    
    os.makedirs(fam_dir, exist_ok=True)
    os.makedirs(arch_dir, exist_ok=True)
    
    print("Generating diagram family pages...")
    for fam in FAMILIES:
        path = os.path.join(fam_dir, fam["file"])
        with open(path, "w", encoding="utf-8") as f:
            f.write(generate_family_page(fam))
        print(f"  {fam['file']}")
    
    print("Generating architecture pages...")
    for p in ARCH_PAGES:
        path = os.path.join(arch_dir, p["file"])
        with open(path, "w", encoding="utf-8") as f:
            f.write(generate_arch_page(p))
        print(f"  {p['file']}")
    
    print(f"\nDone! Generated {len(FAMILIES)} family pages + {len(ARCH_PAGES)} architecture pages.")


if __name__ == "__main__":
    main()

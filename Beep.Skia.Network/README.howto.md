# Beep.Skia.Network - Network Visualization Components

A comprehensive suite of network visualization and analysis components for the Beep.Skia framework, providing advanced graph visualization, analysis tools, and interactive controls.

## Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Core Components](#core-components)
- [Data Structures](#data-structures)
- [Component Usage](#component-usage)
- [Advanced Features](#advanced-features)
- [Integration Examples](#integration-examples)
- [API Reference](#api-reference)

## Overview

The Beep.Skia.Network library provides a complete toolkit for network visualization and analysis, including:

- **Network Visualization**: Interactive graph display with customizable layouts
- **Analysis Tools**: Centrality measures, community detection, path finding
- **Interactive Controls**: Navigation, filtering, and export capabilities
- **Multiple Layouts**: Force-directed, circular, hierarchical, and more
- **Export Options**: CSV, JSON, GraphML, PNG, SVG formats

## Quick Start

### 1. Basic Network Setup

```csharp
using Beep.Skia.Network;
using Beep.Skia;

// Create a drawing manager
var drawingManager = new DrawingManager();

// Create network nodes
var node1 = new NetworkNode { Name = "Node A", X = 100, Y = 100 };
var node2 = new NetworkNode { Name = "Node B", X = 200, Y = 150 };
var node3 = new NetworkNode { Name = "Node C", X = 150, Y = 200 };

var nodes = new List<NetworkNode> { node1, node2, node3 };

// Create network links
var link1 = new NetworkLink
{
    SourceNode = node1,
    TargetNode = node2,
    Weight = 1.0,
    LinkType = "Default"
};

var link2 = new NetworkLink
{
    SourceNode = node2,
    TargetNode = node3,
    Weight = 2.0,
    LinkType = "Strong"
};

var links = new List<NetworkLink> { link1, link2 };

// Add components to drawing manager
foreach (var node in nodes) drawingManager.AddComponent(node);
foreach (var link in links) drawingManager.AddComponent(link);
```

### 2. Add Analysis Components

```csharp
// Add centrality analysis
var centralityMeasure = new CentralityMeasure();
centralityMeasure.CalculateCentrality(nodes, links);
drawingManager.AddComponent(centralityMeasure);

// Add community detection
var communityDetector = new CommunityDetector();
communityDetector.DetectCommunities(nodes, links);
drawingManager.AddComponent(communityDetector);

// Add navigation controls
var navigator = new GraphNavigator();
drawingManager.AddComponent(navigator);

// Add filter panel
var filterPanel = new FilterPanel();
filterPanel.ApplyFilters(nodes, links);
drawingManager.AddComponent(filterPanel);
```

## Core Components

### NetworkControl (Base Class)

All network components inherit from `NetworkControl`, which provides:

- **Material Design 3.0** color schemes
- **Consistent styling** with borders, backgrounds, and text rendering
- **Drawing helpers** for common UI elements
- **Event handling** for mouse interactions

### NetworkNode

Represents a node in the network with the following properties:

```csharp
public class NetworkNode : SkiaComponent
{
    // Basic properties
    public SKColor FillColor { get; set; }
    public SKColor StrokeColor { get; set; }
    public float StrokeWidth { get; set; }
    public float CornerRadius { get; set; }

    // Network-specific properties
    public string NodeType { get; set; }        // "Default", "Hub", "Leaf", etc.
    // Note: Unique identifier is provided by base SkiaComponent.Id (Guid)
    public bool IsHighlighted { get; set; }     // Visual highlighting
    public float Scale { get; set; }            // Size scaling (1.0 = normal)
    public SKColor CentralityColor { get; set; } // Color based on centrality
    public SKColor CommunityColor { get; set; }  // Color based on community
    public int CommunityId { get; set; }        // Community membership
}
```

### NetworkLink

Represents a connection between nodes:

```csharp
public class NetworkLink : SkiaComponent
{
    // Basic properties
    public SKPoint Start { get; set; }
    public SKPoint End { get; set; }
    public SKColor Color { get; set; }
    public float Thickness { get; set; }

    // Network-specific properties
    public NetworkNode SourceNode { get; set; }  // Starting node
    public NetworkNode TargetNode { get; set; }  // Ending node
    public double Weight { get; set; }           // Connection strength
    public string LinkType { get; set; }         // "Default", "Strong", "Weak"
    public bool IsHighlighted { get; set; }      // Visual highlighting
}
```

## Data Structures

### Populating Network Data

#### From CSV Data

```csharp
public class NetworkDataLoader
{
    public static (List<NetworkNode>, List<NetworkLink>) LoadFromCSV(string nodesFile, string linksFile)
    {
        var nodes = new List<NetworkNode>();
        var links = new List<NetworkLink>();
        var nodeDict = new Dictionary<string, NetworkNode>(); // map CSV id -> node

        // Load nodes from CSV
        foreach (var line in File.ReadLines(nodesFile).Skip(1)) // Skip header
        {
            var parts = line.Split(',');
            var node = new NetworkNode
            {
                Name = parts[1],
                NodeType = parts[2],
                X = float.Parse(parts[3]),
                Y = float.Parse(parts[4])
            };
            nodes.Add(node);
            nodeDict[parts[0]] = node; // keep original ID mapping
        }

        // Load links from CSV
        foreach (var line in File.ReadLines(linksFile).Skip(1)) // Skip header
        {
            var parts = line.Split(',');
            var link = new NetworkLink
            {
                SourceNode = nodeDict[parts[0]],
                TargetNode = nodeDict[parts[1]],
                Weight = double.Parse(parts[2]),
                LinkType = parts[3]
            };
            links.Add(link);
        }

        return (nodes, links);
    }
}
```

#### From JSON Data

```csharp
public class JsonNetworkData
{
    public List<JsonNode> nodes { get; set; }
    public List<JsonLink> links { get; set; }
}

public class JsonNode
{
    public string id { get; set; } // use string/Guid ids
    public string name { get; set; }
    public string type { get; set; }
    public float x { get; set; }
    public float y { get; set; }
}

public class JsonLink
{
    public string source { get; set; }
    public string target { get; set; }
    public double weight { get; set; }
    public string type { get; set; }
}

public static (List<NetworkNode>, List<NetworkLink>) LoadFromJson(string jsonFile)
{
    var jsonData = JsonSerializer.Deserialize<JsonNetworkData>(File.ReadAllText(jsonFile));

    var nodes = jsonData.nodes.Select(n =>
    {
        var node = new NetworkNode
        {
            Name = n.name,
            NodeType = n.type,
            X = n.x,
            Y = n.y
        };
        return (n.id, node);
    }).ToList();

    // Build lookup by original ID
    var nodeDict = nodes.ToDictionary(t => t.id, t => t.node);
    var nodeList = nodes.Select(t => t.node).ToList();

    var links = jsonData.links.Select(l => new NetworkLink
    {
        SourceNode = nodeDict[l.source],
        TargetNode = nodeDict[l.target],
        Weight = l.weight,
        LinkType = l.type
    }).ToList();

    return (nodes, links);
}
```

#### Programmatic Network Generation

```csharp
public static (List<NetworkNode>, List<NetworkLink>) GenerateSampleNetwork()
{
    var nodes = new List<NetworkNode>();
    var links = new List<NetworkLink>();

    // Create a small world network
    for (int i = 0; i < 20; i++)
    {
        var node = new NetworkNode
        {
            Id = i,
            Name = $"Node {i}",
            NodeType = i < 5 ? "Hub" : "Regular",
            X = 100 + (i % 5) * 120,
            Y = 100 + (i / 5) * 100
        };
        nodes.Add(node);
    }

    // Create connections
    var random = new Random();
    for (int i = 0; i < nodes.Count; i++)
    {
        for (int j = i + 1; j < nodes.Count; j++)
        {
            if (random.NextDouble() < 0.3) // 30% connection probability
            {
                var link = new NetworkLink
                {
                    SourceNode = nodes[i],
                    TargetNode = nodes[j],
                    Weight = random.NextDouble() * 5 + 1,
                    LinkType = "Default"
                };
                links.Add(link);
            }
        }
    }

    return (nodes, links);
}
```

## Component Usage

### PathFinder - Shortest Path Visualization

```csharp
var pathFinder = new PathFinder();

// Set start and end nodes
pathFinder.StartNode = nodes[0];
pathFinder.EndNode = nodes[5];

// Calculate and display path (would integrate with pathfinding algorithm)
var pathNodes = new List<NetworkNode> { nodes[0], nodes[2], nodes[5] };
var pathLinks = new List<NetworkLink> { /* corresponding links */ };
pathFinder.SetPath(pathNodes, pathLinks, 3.5);

// Add to drawing manager
drawingManager.AddComponent(pathFinder);
```

### CentralityMeasure - Node Importance Analysis

```csharp
var centrality = new CentralityMeasure();

// Choose centrality algorithm
centrality.MeasureType = CentralityType.Degree; // or Betweenness, Closeness, Eigenvector

// Calculate centrality for all nodes
centrality.CalculateCentrality(nodes, links);

// Display top nodes
centrality.MaxDisplayCount = 10;
centrality.ShowValues = true;

// Add to drawing manager
drawingManager.AddComponent(centrality);
```

### CommunityDetector - Network Community Analysis

```csharp
var communityDetector = new CommunityDetector();

// Choose detection algorithm
communityDetector.Algorithm = CommunityAlgorithm.Louvain; // or GirvanNewman, LabelPropagation, ConnectedComponents

// Detect communities
communityDetector.DetectCommunities(nodes, links);

// Configure display
communityDetector.ShowLabels = true;
communityDetector.MinCommunitySize = 3;

// Add to drawing manager
drawingManager.AddComponent(communityDetector);
```

### GraphNavigator - Interactive Navigation

```csharp
var navigator = new GraphNavigator();

// Configure navigation
navigator.ZoomLevel = 1.0f;
navigator.ShowControls = true;

// Handle navigation events
navigator.NodeSelected += (sender, e) =>
{
    Console.WriteLine($"Selected node: {e.SelectedNode?.Name}");
};

navigator.ViewChanged += (sender, e) =>
{
    Console.WriteLine($"Zoom: {e.ZoomLevel:F2}, Pan: ({e.PanOffsetX:F0}, {e.PanOffsetY:F0})");
};

// Add to drawing manager
drawingManager.AddComponent(navigator);
```

### FilterPanel - Data Filtering and Search

```csharp
var filterPanel = new FilterPanel();

// Configure filters
filterPanel.SearchQuery = "Node";
filterPanel.MinDegree = 2;
filterPanel.MaxDegree = 10;
filterPanel.ShowConnectedOnly = true;
filterPanel.ShowIsolatedNodes = false;

// Apply filters
filterPanel.ApplyFilters(nodes, links);

// Handle filter changes
filterPanel.FilterChanged += (sender, e) =>
{
    Console.WriteLine($"Filtered to {e.FilteredNodes.Count} nodes and {e.FilteredLinks.Count} links");
};

// Add to drawing manager
drawingManager.AddComponent(filterPanel);
```

### LayoutSelector - Graph Layout Algorithms

```csharp
var layoutSelector = new LayoutSelector();

// Choose layout algorithm
layoutSelector.SelectedAlgorithm = LayoutAlgorithm.ForceDirected;

// Configure layout bounds
var bounds = new SKRect(50, 50, 750, 550);

// Apply layout to nodes
layoutSelector.ApplyLayout(nodes, links, bounds);

// Handle layout changes
layoutSelector.LayoutChanged += (sender, e) =>
{
    Console.WriteLine($"Layout changed to: {e.Algorithm}");
};

// Add to drawing manager
drawingManager.AddComponent(layoutSelector);
```

### ExportPanel - Data and Image Export

```csharp
var exportPanel = new ExportPanel();

// Choose export format
exportPanel.SelectedFormat = ExportFormat.JSON; // or CSV, GraphML, PNG, SVG

// Configure export options
exportPanel.Options.IncludePositions = true;
exportPanel.Options.IncludeAttributes = true;
exportPanel.Options.IncludeWeights = true;

// Export data
string exportPath = "network_export.json";
exportPanel.Export(nodes, links, exportPath);

// Handle export events
exportPanel.ExportCompleted += (sender, e) =>
{
    Console.WriteLine($"Exported {e.Format} to {e.FilePath}");
};

exportPanel.ExportFailed += (sender, e) =>
{
    Console.WriteLine($"Export failed: {e.ErrorMessage}");
};

// Add to drawing manager
drawingManager.AddComponent(exportPanel);
```

### NodeCluster - Node Grouping

```csharp
var cluster = new NodeCluster();

// Add nodes to cluster
cluster.AddNode(nodes[0]);
cluster.AddNode(nodes[1]);
cluster.AddNode(nodes[2]);

// Configure cluster appearance
cluster.Name = "Important Nodes";
cluster.PrimaryColor = new SKColor(0x4C, 0xAF, 0x50); // Green

// Update bounds automatically
cluster.UpdateBounds();

// Add to drawing manager
drawingManager.AddComponent(cluster);
```

### NetworkAnalyzer - Network Statistics

```csharp
var analyzer = new NetworkAnalyzer();

// Set network data
analyzer.Nodes = nodes;
analyzer.Links = links;

// Calculate metrics
analyzer.CalculateMetrics();

// Add to drawing manager
drawingManager.AddComponent(analyzer);
```

## Advanced Features

### Custom Layout Algorithms

```csharp
public class CustomLayout : LayoutSelector
{
    public override void ApplyLayout(List<NetworkNode> nodes, List<NetworkLink> links, SKRect bounds)
    {
        // Implement custom layout logic
        for (int i = 0; i < nodes.Count; i++)
        {
            // Custom positioning algorithm
            nodes[i].X = bounds.Left + (i * bounds.Width) / nodes.Count;
            nodes[i].Y = bounds.Top + bounds.Height / 2;
        }
    }
}
```

### Real-time Network Updates

```csharp
public class DynamicNetworkManager
{
    private List<NetworkNode> nodes;
    private List<NetworkLink> links;
    private DrawingManager drawingManager;

    public void AddNode(NetworkNode node)
    {
        nodes.Add(node);
        drawingManager.AddComponent(node);
        UpdateAnalysisComponents();
    }

    public void AddLink(NetworkLink link)
    {
        links.Add(link);
        drawingManager.AddComponent(link);
        UpdateAnalysisComponents();
    }

    private void UpdateAnalysisComponents()
    {
        // Update all analysis components with new data
        foreach (var component in drawingManager.GetComponents())
        {
            if (component is CentralityMeasure centrality)
            {
                centrality.CalculateCentrality(nodes, links);
            }
            else if (component is CommunityDetector community)
            {
                community.DetectCommunities(nodes, links);
            }
            // Update other analysis components...
        }
    }
}
```

### Custom Node and Link Styling

```csharp
public class StyledNetworkNode : NetworkNode
{
    public StyledNetworkNode()
    {
        // Custom styling based on node type
        switch (NodeType)
        {
            case "Hub":
                FillColor = new SKColor(0xFF, 0x98, 0x00); // Orange
                Scale = 1.5f;
                break;
            case "Leaf":
                FillColor = new SKColor(0x81, 0xC7, 0x84); // Green
                Scale = 0.8f;
                break;
            default:
                FillColor = new SKColor(0x42, 0xA5, 0xF5); // Blue
                break;
        }
    }
}
```

## Integration Examples

### WinForms Integration

```csharp
public class NetworkViewerForm : Form
{
    private Beep_Skia_Control skiaControl;
    private DrawingManager drawingManager;

    public NetworkViewerForm()
    {
        InitializeComponent();

        skiaControl = new Beep_Skia_Control();
        drawingManager = new DrawingManager();
        skiaControl.DrawingManager = drawingManager;

        // Load and display network
        var (nodes, links) = NetworkDataLoader.LoadFromCSV("nodes.csv", "links.csv");
        SetupNetworkComponents(nodes, links);

        Controls.Add(skiaControl);
    }

    private void SetupNetworkComponents(List<NetworkNode> nodes, List<NetworkLink> links)
    {
        // Add network elements
        foreach (var node in nodes) drawingManager.AddComponent(node);
        foreach (var link in links) drawingManager.AddComponent(link);

        // Add analysis tools
        var centrality = new CentralityMeasure();
        centrality.CalculateCentrality(nodes, links);
        drawingManager.AddComponent(centrality);

        var navigator = new GraphNavigator();
        drawingManager.AddComponent(navigator);

        // Position components
        centrality.X = 800;
        centrality.Y = 50;

        navigator.X = 800;
        navigator.Y = 300;
    }
}
```

### WPF Integration

```csharp
public class NetworkViewerControl : UserControl
{
    private DrawingManager drawingManager;
    private SKElement skElement;

    public NetworkViewerControl()
    {
        InitializeComponent();

        drawingManager = new DrawingManager();
        skElement = new SKElement();
        skElement.PaintSurface += OnPaintSurface;
        Content = skElement;
    }

    private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        drawingManager.Draw(e.Surface.Canvas, e.Info.Width, e.Info.Height);
    }

    public void LoadNetwork(List<NetworkNode> nodes, List<NetworkLink> links)
    {
        foreach (var node in nodes) drawingManager.AddComponent(node);
        foreach (var link in links) drawingManager.AddComponent(link);

        // Add analysis components...
        skElement.InvalidateVisual();
    }
}
```

## API Reference

### Key Classes

- **NetworkControl**: Base class for all network components
- **NetworkNode**: Represents a network node with visual and data properties
- **NetworkLink**: Represents a connection between nodes
- **PathFinder**: Shortest path visualization component
- **CentralityMeasure**: Node centrality analysis component
- **CommunityDetector**: Community detection component
- **GraphNavigator**: Interactive navigation component
- **FilterPanel**: Data filtering component
- **LayoutSelector**: Graph layout component
- **ExportPanel**: Data export component
- **NodeCluster**: Node grouping component
- **NetworkAnalyzer**: Network statistics component

### Key Enums

- **CentralityType**: Degree, Betweenness, Closeness, Eigenvector
- **CommunityAlgorithm**: Louvain, GirvanNewman, LabelPropagation, ConnectedComponents
- **LayoutAlgorithm**: ForceDirected, Circular, Hierarchical, Grid, Random, Radial
- **ExportFormat**: CSV, JSON, GraphML, PNG, SVG

### Key Events

- **FilterChanged**: Raised when filter criteria change
- **LayoutChanged**: Raised when layout algorithm changes
- **NodeSelected**: Raised when a node is selected
- **ViewChanged**: Raised when view (zoom/pan) changes
- **ExportCompleted**: Raised when export completes
- **ExportFailed**: Raised when export fails

## Best Practices

1. **Performance**: For large networks (>1000 nodes), consider using filtering and clustering
2. **Layout**: Choose appropriate layout algorithms based on network structure
3. **Styling**: Use consistent color schemes and meaningful visual encodings
4. **Interaction**: Provide navigation controls for large networks
5. **Analysis**: Combine multiple analysis components for comprehensive insights
6. **Export**: Use appropriate formats for different use cases (analysis vs. visualization)

## Troubleshooting

### Common Issues

1. **Components not visible**: Check component positions and bounds
2. **Layout not applying**: Ensure nodes and links are properly connected
3. **Analysis not updating**: Call analysis methods after data changes
4. **Export failing**: Check file paths and permissions
5. **Performance issues**: Use filtering for large datasets

### Debug Tips

```csharp
// Enable detailed logging
drawingManager.EnableDebugLogging = true;

// Check component bounds
foreach (var component in drawingManager.GetComponents())
{
    Console.WriteLine($"{component.Name}: Bounds={component.Bounds}");
}

// Validate network data
Console.WriteLine($"Nodes: {nodes.Count}, Links: {links.Count}");
Console.WriteLine($"Connected components: {CalculateConnectedComponents(nodes, links)}");
```

This comprehensive guide provides everything needed to effectively use the Beep.Skia.Network components for network visualization and analysis applications.</content>
<parameter name="filePath">c:\Users\f_ald\source\repos\The-Tech-Idea\Beep.Skia\Beep.Skia.Network\README.howto.md
# Animated Flow Lines - LinkedIn Infographic Style

The connection lines now support **7 professional animation styles** perfect for creating engaging data flow visualizations like those seen in LinkedIn infographics.

## Animation Styles

### 1. **Dots** (Default)
Classic animated dots with glow effect flowing along the line.
- Best for: General data flow, simple presentations
- Visual: Glowing circles moving along the path

### 2. **Dashes**
Animated dashes moving along the line like a marching ants effect.
- Best for: Active processes, ongoing transfers
- Visual: Short line segments flowing continuously

### 3. **Wave**
Pulsing wave effect that travels along the line.
- Best for: Signal transmission, network communication
- Visual: Sine wave pattern flowing along the path

### 4. **Gradient**
Smooth gradient that moves along the line with fade in/out effect.
- Best for: Premium/polished presentations, data streaming
- Visual: Color gradient sweep effect

### 5. **Particles**
Particles with trailing effect for dynamic motion.
- Best for: High-energy processes, real-time data
- Visual: Main particle with fading trail

### 6. **Pulse**
Glowing pulse that travels along the line with expanding rings.
- Best for: Heartbeat/monitoring systems, status checks
- Visual: Expanding concentric circles

### 7. **Arrows**
Arrow shapes moving along the line showing clear direction.
- Best for: Directional emphasis, workflow progression
- Visual: Small arrow glyphs flowing along path

## Usage Example

```csharp
using Beep.Skia;
using Beep.Skia.Model;

// Create a connection line between two nodes
var line = new ConnectionLine(startPort, endPort, InvalidateVisual);

// Enable animation
line.IsDataFlowAnimated = true;
line.FlowDirection = DataFlowDirection.Forward;

// Choose animation style
line.AnimationStyle = FlowAnimationStyle.Gradient; // or any other style

// Customize appearance
line.DataFlowSpeed = 100f;           // Speed in pixels per second
line.DataFlowParticleSize = 4f;      // Size of particles/dots
line.DataFlowColor = SKColors.Blue;  // Animation color

// Start the animation
line.StartAnimation();
```

## Direction Options

Set `FlowDirection` to control animation direction:

- **None**: No animation
- **Forward**: Start → End
- **Backward**: End → Start  
- **Bidirectional**: Both directions simultaneously

## Customization Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `IsDataFlowAnimated` | bool | false | Enable/disable animation |
| `AnimationStyle` | FlowAnimationStyle | Dots | Animation visual style |
| `FlowDirection` | DataFlowDirection | None | Direction of flow |
| `DataFlowSpeed` | float | 100f | Animation speed (px/sec) |
| `DataFlowParticleSize` | float | 4f | Size of animated elements |
| `DataFlowColor` | SKColor | Blue | Color of animation |

## Pro Tips

### For LinkedIn-Style Infographics:
```csharp
// Professional gradient flow
line.AnimationStyle = FlowAnimationStyle.Gradient;
line.DataFlowColor = new SKColor(0x00, 0x77, 0xB5); // LinkedIn blue
line.DataFlowSpeed = 80f; // Smooth, not too fast

// High-tech particle effect
line.AnimationStyle = FlowAnimationStyle.Particles;
line.DataFlowColor = new SKColor(0x00, 0xFF, 0x00); // Matrix green
line.DataFlowSpeed = 120f;

// Medical/heartbeat monitoring
line.AnimationStyle = FlowAnimationStyle.Pulse;
line.DataFlowColor = new SKColor(0xFF, 0x00, 0x00); // Red
line.DataFlowSpeed = 60f;
```

### Best Practices:
1. **Match the context**: Use Arrows for workflows, Gradient for data pipelines, Wave for signals
2. **Consistent colors**: Use your brand colors for the DataFlowColor
3. **Readable speed**: 60-120 px/sec works well for most presentations
4. **Direction clarity**: Always match FlowDirection to actual data flow logic
5. **Performance**: Limit to ~20-30 animated lines on screen simultaneously

## PM/ETL/Flowchart Integration

The animation works automatically with all node families:

```csharp
// ETL Pipeline with animated data flow
var etlSource = new ETLSource();
var etlTransform = new ETLTransform();
var connection = drawingManager.ConnectComponents(etlSource, etlTransform);
connection.IsDataFlowAnimated = true;
connection.AnimationStyle = FlowAnimationStyle.Gradient;
connection.FlowDirection = DataFlowDirection.Forward;

// PM Dependency with pulse animation
var task1 = new TaskNode();
var task2 = new TaskNode();
var dependency = drawingManager.ConnectComponents(task1, task2);
dependency.IsDataFlowAnimated = true;
dependency.AnimationStyle = FlowAnimationStyle.Pulse;
dependency.FlowDirection = DataFlowDirection.Forward;

// Flowchart with arrow animation
var process1 = new ProcessNode();
var process2 = new ProcessNode();
var flow = drawingManager.ConnectComponents(process1, process2);
flow.IsDataFlowAnimated = true;
flow.AnimationStyle = FlowAnimationStyle.Arrows;
flow.FlowDirection = DataFlowDirection.Forward;
```

## Performance Considerations

Animations use the existing timer-based system:
- Animations automatically pause when not visible
- Frame rate adapts to system performance
- Smooth 60 FPS on modern hardware
- Each style optimized for minimal CPU usage

## Framework Integration

The animation system integrates seamlessly with:
- ✅ All node families (Flowchart, ETL, PM, ERD, DFD, etc.)
- ✅ All line routing modes (Straight, Orthogonal, Curved)
- ✅ Zoom levels (animations scale appropriately)
- ✅ Selection and interaction (animations don't interfere)
- ✅ Export/serialization (animation state persists)

---

**Created**: 2025-10-05  
**Version**: 1.0  
**Compatibility**: Beep.Skia Framework (all versions with ConnectionLine support)

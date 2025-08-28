using SkiaSharp;
using System.Collections.Generic;
using System.ComponentModel;

namespace Beep.Skia
{
    using SkiaSharp;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Manages the drawing and interaction of workflow components and connections on a Skia canvas.
    /// This class handles component management, connection creation, mouse interactions, and rendering.
    /// Enhanced with drag-and-drop, line manipulation, selection system, and other modern features.
    /// </summary>
    public partial class DrawingManager
    {
        // This class is now split into multiple partial classes for better organization:
        // - DrawingManager.Core.cs: Main properties, events, and initialization
        // - DrawingManager.Components.cs: Component management methods
        // - DrawingManager.Connections.cs: Connection/line management methods
        // - DrawingManager.Interaction.cs: Mouse and keyboard interaction handling
        // - DrawingManager.Rendering.cs: Drawing and rendering methods
        //
        // Helper classes:
        // - SelectionManager.cs: Handles component selection
        // - InteractionHelper.cs: Handles mouse and keyboard interactions
        // - RenderingHelper.cs: Handles drawing operations
        // - HistoryManager.cs: Handles undo/redo operations
    }
}

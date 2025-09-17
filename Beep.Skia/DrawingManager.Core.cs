using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Beep.Skia.Model;
namespace Beep.Skia
{
    /// <summary>
    /// Manages the drawing and interaction of workflow components and connections on a Skia canvas.
    /// This class handles component management, connection creation, mouse interactions, and rendering.
    /// Enhanced with drag-and-drop, line manipulation, selection system, and other modern features.
    /// </summary>
    public partial class DrawingManager
    {
        private readonly List<SkiaComponent> _components;
        private readonly List<IConnectionLine> _lines;
        private SKPoint _panOffset = SKPoint.Empty;
        private float _zoom = 1.0f;

        // Helper classes
        private SelectionManager _selectionManager;
        private InteractionHelper _interactionHelper;
        private RenderingHelper _renderingHelper;
        private HistoryManager _historyManager;

        /// <summary>
        /// Gets or sets the Skia canvas used for drawing.
        /// </summary>
        public SKCanvas Canvas { get; set; }

        /// <summary>
        /// Gets the list of components (for internal use by helpers).
        /// </summary>
        internal IReadOnlyList<SkiaComponent> Components => _components;

        /// <summary>
        /// Gets the list of connection lines (for internal use by helpers).
        /// </summary>
        internal IReadOnlyList<IConnectionLine> Lines => _lines;

        /// <summary>
        /// Gets or sets the current zoom level.
        /// </summary>
        public float Zoom
        {
            get => _zoom;
            set
            {
                _zoom = Math.Max(0.1f, Math.Min(5.0f, value));
                DrawSurface?.Invoke(this, null);
            }
        }

        /// <summary>
        /// Gets or sets the pan offset for canvas translation.
        /// </summary>
        public SKPoint PanOffset
        {
            get => _panOffset;
            set
            {
                _panOffset = value;
                DrawSurface?.Invoke(this, null);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the grid is visible.
        /// </summary>
        public bool ShowGrid { get; set; } = true;

        /// <summary>
        /// Gets or sets the grid spacing.
        /// </summary>
        public float GridSpacing { get; set; } = 20.0f;

        /// <summary>
        /// Gets or sets a value indicating whether snapping to grid is enabled.
        /// </summary>
        public bool SnapToGrid { get; set; } = true;

        /// <summary>
        /// Gets the selection manager.
        /// </summary>
        public SelectionManager SelectionManager => _selectionManager;

        /// <summary>
        /// Gets the interaction helper.
        /// </summary>
        public InteractionHelper InteractionHelper => _interactionHelper;

        /// <summary>
        /// Gets the rendering helper.
        /// </summary>
        public RenderingHelper RenderingHelper => _renderingHelper;

        /// <summary>
        /// Gets the history manager.
        /// </summary>
        public HistoryManager HistoryManager => _historyManager;

        /// <summary>
        /// Occurs when the drawing surface needs to be updated.
        /// </summary>
        public event EventHandler<ConnectionEventArgs> DrawSurface;

        /// <summary>
        /// Occurs when a component is dropped.
        /// </summary>
        public event EventHandler<ComponentDropEventArgs> ComponentDropped;

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingManager"/> class.
        /// </summary>
        public DrawingManager()
        {
            _components = new List<SkiaComponent>();
            _lines = new List<IConnectionLine>();

            // Initialize helper classes
            _selectionManager = new SelectionManager(this);
            _interactionHelper = new InteractionHelper(this);
            _renderingHelper = new RenderingHelper(this);
            _historyManager = new HistoryManager(this);

            // Wire up events
            _selectionManager.SelectionChanged += (s, e) => SelectionChanged?.Invoke(this, e);
            _historyManager.HistoryChanged += (s, e) => HistoryChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Occurs when the selection changes.
        /// </summary>
        public event EventHandler SelectionChanged;

        /// <summary>
        /// Occurs when the undo/redo history changes.
        /// </summary>
        public event EventHandler HistoryChanged;
    }
}

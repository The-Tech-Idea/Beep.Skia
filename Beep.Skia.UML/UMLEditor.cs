using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.UML
{
    /// <summary>
    /// A comprehensive UML diagram editor with toolbar and canvas.
    /// Provides tools for creating, editing, and managing UML diagrams.
    /// </summary>
    public abstract class UMLEditor
    {
        private readonly List<UMLEditorTool> _tools;
        private UMLEditorTool _selectedTool;
        private SKRect _toolbarBounds;
        private SKRect _canvasBounds;
        private const float ToolbarHeight = 60f;
        private const float ToolButtonSize = 40f;
        private const float ToolButtonSpacing = 10f;
        
        // Mouse interaction state
        private SKPoint _mousePosition;
        private bool _isMouseOver;

        /// <summary>
        /// Gets the currently selected editing tool.
        /// </summary>
        public UMLEditorTool SelectedTool => _selectedTool;

        /// <summary>
        /// Event raised when the selected tool changes.
        /// </summary>
        public event EventHandler<UMLEditorTool> ToolChanged;

        /// <summary>
        /// Initializes a new instance of the UMLEditor class.
        /// </summary>
        public UMLEditor()
        {
            _tools = new List<UMLEditorTool>();

            // Initialize default tools
            InitializeTools();

            // Set default tool
            _selectedTool = _tools.FirstOrDefault(t => t.ToolType == UMLEditorToolType.Select);

            Width = 800;
            Height = 600;
        }

        /// <summary>
        /// Initializes the default editing tools.
        /// </summary>
        private void InitializeTools()
        {
            _tools.Add(new UMLEditorTool(UMLEditorToolType.Select, "Select", SKColors.Gray));
            _tools.Add(new UMLEditorTool(UMLEditorToolType.UMLClass, "Class", SKColors.Blue));
            _tools.Add(new UMLEditorTool(UMLEditorToolType.UMLInterface, "Interface", SKColors.Green));
            _tools.Add(new UMLEditorTool(UMLEditorToolType.UMLActor, "Actor", SKColors.Orange));
            _tools.Add(new UMLEditorTool(UMLEditorToolType.UMLLifeline, "Lifeline", SKColors.Purple));
            _tools.Add(new UMLEditorTool(UMLEditorToolType.Association, "Association", SKColors.Red));
            _tools.Add(new UMLEditorTool(UMLEditorToolType.Inheritance, "Inheritance", SKColors.Brown));
            _tools.Add(new UMLEditorTool(UMLEditorToolType.Message, "Message", SKColors.Magenta));
        }

        /// <summary>
        /// Sets the currently selected tool.
        /// </summary>
        /// <param name="tool">The tool to select.</param>
        public void SelectTool(UMLEditorTool tool)
        {
            if (tool != null && _tools.Contains(tool))
            {
                _selectedTool = tool;
                ToolChanged?.Invoke(this, tool);
            }
        }

        /// <summary>
        /// Sets the currently selected tool by type.
        /// </summary>
        /// <param name="toolType">The type of tool to select.</param>
        public void SelectTool(UMLEditorToolType toolType)
        {
            var tool = _tools.FirstOrDefault(t => t.ToolType == toolType);
            if (tool != null)
            {
                SelectTool(tool);
            }
        }

        /// <summary>
        /// Creates a new UML element at the specified position based on the selected tool.
        /// </summary>
        /// <param name="position">The position to create the element at.</param>
        /// <returns>The created element, or null if no element was created.</returns>
        public UMLControl CreateElementAt(SKPoint position)
        {
            UMLControl element = null;

            switch (_selectedTool?.ToolType)
            {
                case UMLEditorToolType.UMLClass:
                    element = new UMLClass
                    {
                        X = position.X,
                        Y = position.Y,
                        Name = "NewClass"
                    };
                    break;

                case UMLEditorToolType.UMLInterface:
                    element = new UMLInterface
                    {
                        X = position.X,
                        Y = position.Y,
                        Name = "NewInterface"
                    };
                    break;

                case UMLEditorToolType.UMLActor:
                    element = new UMLActor
                    {
                        X = position.X,
                        Y = position.Y,
                        Name = "NewActor"
                    };
                    break;

                case UMLEditorToolType.UMLLifeline:
                    element = new UMLLifeline
                    {
                        X = position.X,
                        Y = position.Y,
                        Name = "NewLifeline"
                    };
                    break;
            }

            if (element != null)
            {
                _diagram.AddElement(element);
            }

            return element;
        }

        /// <summary>
        /// Draws the editor content including toolbar and diagram canvas.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Calculate layout bounds
            _toolbarBounds = new SKRect(0, 0, Width, ToolbarHeight);
            _canvasBounds = new SKRect(0, ToolbarHeight, Width, Height);

            // Draw toolbar background
            using (var toolbarPaint = new SKPaint
            {
                Color = MaterialDesignColors.SurfaceContainerHigh,
                Style = SKPaintStyle.Fill
            })
            {
                canvas.DrawRect(_toolbarBounds, toolbarPaint);
            }

            // Draw toolbar border
            using (var borderPaint = new SKPaint
            {
                Color = MaterialDesignColors.Outline,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1
            })
            {
                canvas.DrawLine(0, ToolbarHeight, Width, ToolbarHeight, borderPaint);
            }

            // Draw tool buttons
            DrawToolbar(canvas);

            // Draw diagram canvas
            DrawCanvas(canvas, context);
        }

        /// <summary>
        /// Draws the toolbar with tool buttons.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        private void DrawToolbar(SKCanvas canvas)
        {
            float x = ToolButtonSpacing;
            float y = (ToolbarHeight - ToolButtonSize) / 2;

            foreach (var tool in _tools)
            {
                var buttonBounds = new SKRect(x, y, x + ToolButtonSize, y + ToolButtonSize);

                // Draw button background
                using (var buttonPaint = new SKPaint
                {
                    Color = tool == _selectedTool ?
                        MaterialDesignColors.PrimaryContainer :
                        MaterialDesignColors.SurfaceContainerHigh,
                    Style = SKPaintStyle.Fill
                })
                {
                    canvas.DrawRoundRect(buttonBounds, 4, 4, buttonPaint);
                }

                // Draw button border
                using (var borderPaint = new SKPaint
                {
                    Color = tool == _selectedTool ?
                        MaterialDesignColors.Primary :
                        MaterialDesignColors.Outline,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = tool == _selectedTool ? 2 : 1
                })
                {
                    canvas.DrawRoundRect(buttonBounds, 4, 4, borderPaint);
                }

                // Draw tool icon (simplified representation)
                DrawToolIcon(canvas, tool, buttonBounds);

                x += ToolButtonSize + ToolButtonSpacing;
            }
        }

        /// <summary>
        /// Draws a simplified icon for the tool.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="tool">The tool to draw an icon for.</param>
        /// <param name="bounds">The bounds of the button.</param>
        private void DrawToolIcon(SKCanvas canvas, UMLEditorTool tool, SKRect bounds)
        {
            using (var iconPaint = new SKPaint
            {
                Color = tool.Color,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            })
            {
                var centerX = bounds.MidX;
                var centerY = bounds.MidY;
                var size = bounds.Width * 0.6f;

                switch (tool.ToolType)
                {
                    case UMLEditorToolType.Select:
                        // Draw cursor icon
                        var cursorPath = new SKPath();
                        cursorPath.MoveTo(centerX - size * 0.3f, centerY - size * 0.4f);
                        cursorPath.LineTo(centerX - size * 0.3f, centerY + size * 0.4f);
                        cursorPath.LineTo(centerX + size * 0.1f, centerY + size * 0.3f);
                        cursorPath.LineTo(centerX + size * 0.2f, centerY + size * 0.4f);
                        cursorPath.LineTo(centerX + size * 0.2f, centerY + size * 0.2f);
                        cursorPath.LineTo(centerX - size * 0.1f, centerY + size * 0.2f);
                        cursorPath.Close();
                        canvas.DrawPath(cursorPath, iconPaint);
                        break;

                    case UMLEditorToolType.UMLClass:
                        // Draw rectangle for class
                        canvas.DrawRect(centerX - size * 0.3f, centerY - size * 0.3f,
                                      size * 0.6f, size * 0.6f, iconPaint);
                        break;

                    case UMLEditorToolType.UMLInterface:
                        // Draw circle for interface
                        canvas.DrawCircle(centerX, centerY, size * 0.3f, iconPaint);
                        break;

                    case UMLEditorToolType.UMLActor:
                        // Draw stick figure
                        canvas.DrawCircle(centerX, centerY - size * 0.2f, size * 0.1f, iconPaint);
                        canvas.DrawLine(centerX, centerY - size * 0.1f, centerX, centerY + size * 0.1f, iconPaint);
                        canvas.DrawLine(centerX - size * 0.15f, centerY, centerX + size * 0.15f, centerY, iconPaint);
                        canvas.DrawLine(centerX, centerY + size * 0.1f, centerX - size * 0.1f, centerY + size * 0.2f, iconPaint);
                        canvas.DrawLine(centerX, centerY + size * 0.1f, centerX + size * 0.1f, centerY + size * 0.2f, iconPaint);
                        break;

                    case UMLEditorToolType.Association:
                        // Draw line with arrow
                        canvas.DrawLine(centerX - size * 0.3f, centerY, centerX + size * 0.3f, centerY, iconPaint);
                        var arrowPath = new SKPath();
                        arrowPath.MoveTo(centerX + size * 0.3f, centerY);
                        arrowPath.LineTo(centerX + size * 0.2f, centerY - size * 0.1f);
                        arrowPath.LineTo(centerX + size * 0.2f, centerY + size * 0.1f);
                        arrowPath.Close();
                        canvas.DrawPath(arrowPath, iconPaint);
                        break;

                    default:
                        // Draw generic shape
                        canvas.DrawRoundRect(centerX - size * 0.25f, centerY - size * 0.25f,
                                           size * 0.5f, size * 0.5f, 4, 4, iconPaint);
                        break;
                }
            }
        }

        /// <summary>
        /// Draws the diagram canvas area.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        private void DrawCanvas(SKCanvas canvas, DrawingContext context)
        {
            // Clip to canvas bounds
            canvas.Save();
            canvas.ClipRect(_canvasBounds);

            // Draw canvas background
            using (var canvasPaint = new SKPaint
            {
                Color = MaterialDesignColors.Surface,
                Style = SKPaintStyle.Fill
            })
            {
                canvas.DrawRect(_canvasBounds, canvasPaint);
            }

            // Create a drawing context for the diagram
            var diagramContext = new DrawingContext
            {
                Bounds = _canvasBounds,
                Zoom = 1.0f,
                PanOffset = SKPoint.Empty,
                Data = new Dictionary<string, object>
                {
                    ["MousePosition"] = new SKPoint(_mousePosition.X, _mousePosition.Y - ToolbarHeight),
                    ["IsMouseOver"] = _isMouseOver
                }
            };

            // Draw the diagram
            _diagram.Draw(canvas, diagramContext);

            canvas.Restore();
        }

        /// <summary>
        /// Handles mouse down events for the editor.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>True if the event was handled, false otherwise.</returns>
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            // Check if click is in toolbar
            if (_toolbarBounds.Contains(point))
            {
                return HandleToolbarClick(point);
            }

            // Check if click is in canvas
            if (_canvasBounds.Contains(point))
            {
                return HandleCanvasClick(point, context);
            }

            return base.OnMouseDown(point, context);
        }

        /// <summary>
        /// Handles toolbar click events.
        /// </summary>
        /// <param name="point">The click position.</param>
        /// <returns>True if the event was handled, false otherwise.</returns>
        private bool HandleToolbarClick(SKPoint point)
        {
            float x = ToolButtonSpacing;
            float y = (ToolbarHeight - ToolButtonSize) / 2;

            for (int i = 0; i < _tools.Count; i++)
            {
                var buttonBounds = new SKRect(x, y, x + ToolButtonSize, y + ToolButtonSize);
                if (buttonBounds.Contains(point))
                {
                    SelectTool(_tools[i]);
                    return true;
                }
                x += ToolButtonSize + ToolButtonSpacing;
            }

            return false;
        }

        /// <summary>
        /// Handles canvas click events.
        /// </summary>
        /// <param name="point">The click position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>True if the event was handled, false otherwise.</returns>
        private bool HandleCanvasClick(SKPoint point, InteractionContext context)
        {
            // Adjust point for canvas offset
            var canvasPoint = new SKPoint(point.X, point.Y - ToolbarHeight);

            // If using a creation tool, create new element
            if (_selectedTool?.ToolType != UMLEditorToolType.Select)
            {
                CreateElementAt(canvasPoint);
                return true;
            }

            // Otherwise, let the diagram handle the interaction
            return _diagram.HandleMouseDown(canvasPoint, context);
        }

        /// <summary>
        /// Handles mouse move events for the editor.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>True if the event was handled, false otherwise.</returns>
        protected override bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            // Store mouse state for drawing
            _mousePosition = point;
            _isMouseOver = _canvasBounds.Contains(point);
            if (_canvasBounds.Contains(point))
            {
                var canvasPoint = new SKPoint(point.X, point.Y - ToolbarHeight);
                _diagram.ProcessMouseMove(canvasPoint, context);
            }

            return base.OnMouseMove(point, context);
        }

        /// <summary>
        /// Handles mouse up events for the editor.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>True if the event was handled, false otherwise.</returns>
        protected override bool OnMouseUp(SKPoint point, InteractionContext context)
        {
            // Adjust point for canvas offset if in canvas area
            if (_canvasBounds.Contains(point))
            {
                var canvasPoint = new SKPoint(point.X, point.Y - ToolbarHeight);
                _diagram.ProcessMouseUp(canvasPoint, context);
            }

            return base.OnMouseUp(point, context);
        }

        /// <summary>
        /// Gets all available tools.
        /// </summary>
        /// <returns>A read-only collection of tools.</returns>
        public IReadOnlyCollection<UMLEditorTool> GetTools()
        {
            return _tools.AsReadOnly();
        }

        /// <summary>
        /// Clears the diagram.
        /// </summary>
        public void ClearDiagram()
        {
            _diagram.Clear();
        }

        /// <summary>
        /// Fits the diagram to content.
        /// </summary>
        public void FitDiagramToContent()
        {
            _diagram.FitToContent();
        }
    }

    /// <summary>
    /// Represents a tool in the UML editor.
    /// </summary>
    public class UMLEditorTool
    {
        /// <summary>
        /// Gets the type of the tool.
        /// </summary>
        public UMLEditorToolType ToolType { get; }

        /// <summary>
        /// Gets the display name of the tool.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the color associated with the tool.
        /// </summary>
        public SKColor Color { get; }

        /// <summary>
        /// Initializes a new instance of the UMLEditorTool class.
        /// </summary>
        /// <param name="toolType">The type of the tool.</param>
        /// <param name="name">The display name of the tool.</param>
        /// <param name="color">The color associated with the tool.</param>
        public UMLEditorTool(UMLEditorToolType toolType, string name, SKColor color)
        {
            ToolType = toolType;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Color = color;
        }
    }

    /// <summary>
    /// Defines the types of tools available in the UML editor.
    /// </summary>
    public enum UMLEditorToolType
    {
        /// <summary>
        /// Selection tool for selecting and moving elements.
        /// </summary>
        Select,

        /// <summary>
        /// Tool for creating UML class elements.
        /// </summary>
        UMLClass,

        /// <summary>
        /// Tool for creating UML interface elements.
        /// </summary>
        UMLInterface,

        /// <summary>
        /// Tool for creating UML actor elements.
        /// </summary>
        UMLActor,

        /// <summary>
        /// Tool for creating UML lifeline elements.
        /// </summary>
        UMLLifeline,

        /// <summary>
        /// Tool for creating association connections.
        /// </summary>
        Association,

        /// <summary>
        /// Tool for creating inheritance connections.
        /// </summary>
        Inheritance,

        /// <summary>
        /// Tool for creating message connections.
        /// </summary>
        Message
    }
}
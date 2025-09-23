using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Base control for business graph components (flowcharts, organizational charts, business processes).
    /// Provides common functionality for business diagram elements with support for custom shapes.
    /// </summary>
    public abstract class BusinessControl : SkiaComponent
    {
        /// <summary>
        /// Gets or sets the business component type.
        /// </summary>
        public BusinessComponentType ComponentType 
        { 
            get => _componentType;
            set
            {
                if (_componentType != value)
                {
                    _componentType = value;
                    InitializeConnectionPoints();
                }
            }
        }
        private BusinessComponentType _componentType = BusinessComponentType.Task;

        /// <summary>
        /// Gets or sets the business role or department this component represents.
        /// </summary>
        public string BusinessRole { get; set; } = "";

        /// <summary>
        /// Gets or sets the business priority level of this business component.
        /// </summary>
        public BusinessPriority BusinessPriority { get; set; } = BusinessPriority.Normal;

        /// <summary>
        /// Gets or sets the status of this business component.
        /// </summary>
        public BusinessStatus Status { get; set; } = BusinessStatus.Active;

        /// <summary>
        /// Gets or sets the background color of the component.
        /// </summary>
        public SKColor BackgroundColor { get; set; } = SKColors.LightBlue;

        /// <summary>
        /// Gets or sets the border color of the component.
        /// </summary>
        public SKColor BorderColor { get; set; } = SKColors.DarkBlue;

    // Use TextColor from SkiaComponent

        /// <summary>
        /// Gets or sets the border thickness.
        /// </summary>
        public float BorderThickness { get; set; } = 2.0f;

        /// <summary>
        /// Initializes a new instance of the BusinessControl class.
        /// </summary>
        protected BusinessControl()
        {
            // Initialize default business styling
            BackgroundColor = GetDefaultBackgroundColor();
            BorderColor = GetDefaultBorderColor();
            TextColor = SKColors.Black;
            BorderThickness = 2.0f;
            
            // Initialize connection points based on component type
            InitializeConnectionPoints();
        }

        /// <summary>
        /// Initializes connection points based on the component type.
        /// </summary>
        protected virtual void InitializeConnectionPoints()
        {
            // Clear existing connection points
            InConnectionPoints.Clear();
            OutConnectionPoints.Clear();

            // Initialize connection points based on component type
            switch (ComponentType)
            {
                case BusinessComponentType.StartEvent:
                    // Start events have no inputs, 1 output
                    AddOutputConnectionPoint();
                    break;

                case BusinessComponentType.EndEvent:
                    // End events have 1 input, no outputs
                    AddInputConnectionPoint();
                    break;

                case BusinessComponentType.Task:
                    // Tasks have 1 input, 1 output
                    AddInputConnectionPoint();
                    AddOutputConnectionPoint();
                    break;

                case BusinessComponentType.Decision:
                    // Decisions have 1 input, 2 outputs (true/false)
                    AddInputConnectionPoint();
                    AddOutputConnectionPoint();
                    AddOutputConnectionPoint();
                    break;

                case BusinessComponentType.Gateway:
                    // Gateways have 1 input, 2 outputs (parallel processing)
                    AddInputConnectionPoint();
                    AddOutputConnectionPoint();
                    AddOutputConnectionPoint();
                    break;

                case BusinessComponentType.Document:
                case BusinessComponentType.Database:
                case BusinessComponentType.DataStore:
                case BusinessComponentType.ExternalData:
                    // Data components have 1 input/output for data flow
                    AddInputConnectionPoint();
                    AddOutputConnectionPoint();
                    break;

                case BusinessComponentType.Person:
                case BusinessComponentType.Department:
                case BusinessComponentType.Role:
                case BusinessComponentType.System:
                    // Organizational components have 1 input/output for relationships
                    AddInputConnectionPoint();
                    AddOutputConnectionPoint();
                    break;

                default:
                    // Default: 1 input, 1 output
                    AddInputConnectionPoint();
                    AddOutputConnectionPoint();
                    break;
            }

            // Position the connection points
            UpdateConnectionPointPositions();
            // Notify listeners (e.g., DrawingManager) that geometry-affecting port structure changed
            try { OnBoundsChanged(Bounds); } catch { }
        }

        /// <summary>
        /// Adds an input connection point.
        /// </summary>
        protected void AddInputConnectionPoint()
        {
            var point = new ConnectionPoint
            {
                Type = ConnectionPointType.In,
                Radius = 6,
                Component = this,
                Shape = ComponentShape.Circle,
                IsAvailable = true,
                DataType = "object"
            };
            InConnectionPoints.Add(point);
        }

        /// <summary>
        /// Adds an output connection point.
        /// </summary>
        protected void AddOutputConnectionPoint()
        {
            var point = new ConnectionPoint
            {
                Type = ConnectionPointType.Out,
                Radius = 6,
                Component = this,
                Shape = ComponentShape.Circle,
                IsAvailable = true,
                DataType = "object"
            };
            OutConnectionPoints.Add(point);
        }

        /// <summary>
        /// Updates the positions of all connection points.
        /// </summary>
        protected virtual void UpdateConnectionPointPositions()
        {
            // Position input points on the left side
            if (InConnectionPoints.Count > 0)
            {
                float spacing = Height / (InConnectionPoints.Count + 1);
                for (int i = 0; i < InConnectionPoints.Count; i++)
                {
                    var cp = InConnectionPoints[i] as ConnectionPoint;
                    if (cp != null)
                    {
                        float cy = Y + spacing * (i + 1);
                        float cx = X; // left edge
                        cp.Position = new SKPoint(cx, cy);
                        cp.Center = cp.Position;
                        cp.Offset = new SKPoint(0, spacing * (i + 1));
                        float r = Math.Max(1, cp.Radius);
                        cp.Bounds = new SKRect(cp.Center.X - r, cp.Center.Y - r, cp.Center.X + r, cp.Center.Y + r);
                        cp.Rect = cp.Bounds;
                        cp.Index = i;
                        cp.Component = this;
                        cp.IsAvailable = true;
                    }
                }
            }

            // Position output points on the right side
            if (OutConnectionPoints.Count > 0)
            {
                float spacing = Height / (OutConnectionPoints.Count + 1);
                for (int i = 0; i < OutConnectionPoints.Count; i++)
                {
                    var cp = OutConnectionPoints[i] as ConnectionPoint;
                    if (cp != null)
                    {
                        float cy = Y + spacing * (i + 1);
                        float cx = X + Width; // right edge
                        cp.Position = new SKPoint(cx, cy);
                        cp.Center = cp.Position;
                        cp.Offset = new SKPoint(Width, spacing * (i + 1));
                        float r = Math.Max(1, cp.Radius);
                        cp.Bounds = new SKRect(cp.Center.X - r, cp.Center.Y - r, cp.Center.X + r, cp.Center.Y + r);
                        cp.Rect = cp.Bounds;
                        cp.Index = i;
                        cp.Component = this;
                        cp.IsAvailable = true;
                    }
                }
            }
        }

        /// <summary>
        /// Draws the connection points.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected virtual void DrawConnectionPoints(SKCanvas canvas, DrawingContext context)
        {
            // Draw input connection points
            foreach (var point in InConnectionPoints)
            {
                DrawConnectionPoint(canvas, point, SKColors.Blue);
            }

            // Draw output connection points
            foreach (var point in OutConnectionPoints)
            {
                DrawConnectionPoint(canvas, point, SKColors.Green);
            }
        }

        /// <summary>
        /// Draws a single connection point.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="point">The connection point to draw.</param>
        /// <param name="color">The color to use for the point.</param>
        protected virtual void DrawConnectionPoint(SKCanvas canvas, IConnectionPoint point, SKColor color)
        {
            using var paint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var borderPaint = new SKPaint
            {
                Color = SKColors.White,
                StrokeWidth = 1,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            var center = point.Position;
            canvas.DrawCircle(center.X, center.Y, point.Radius, paint);
            canvas.DrawCircle(center.X, center.Y, point.Radius, borderPaint);
        }

        /// <summary>
        /// Updates the component bounds and refreshes connection point positions.
        /// </summary>
        protected override void UpdateBounds()
        {
            base.UpdateBounds();
            UpdateConnectionPointPositions();
        }


        /// <summary>
        /// Draws the content of the business component.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Draw the custom shape
            DrawShape(canvas, context);

            // Draw component text
            DrawComponentText(canvas);

            // Draw status indicators
            DrawStatusIndicators(canvas);

            // Draw connection points
            DrawConnectionPoints(canvas, context);

            // Draw selection indicator if selected
            if (IsSelected)
            {
                DrawSelectionBorder(canvas);
            }
        }

        /// <summary>
        /// Draws the shape for this business component. Override in derived classes for custom shapes.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected virtual void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            // Default rounded rectangle shape
            using var fillPaint = new SKPaint
            {
                Color = BackgroundColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var borderPaint = new SKPaint
            {
                Color = BorderColor,
                StrokeWidth = BorderThickness,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            var rect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(rect, 8, 8, fillPaint);
            canvas.DrawRoundRect(rect, 8, 8, borderPaint);
        }

        /// <summary>
        /// Draws the component text (name/title).
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        protected virtual void DrawComponentText(SKCanvas canvas)
        {
            if (string.IsNullOrEmpty(Name))
                return;

            using var font = new SKFont(SKTypeface.Default, 12) { Embolden = true };
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float centerY = Y + Height / 2;

            canvas.DrawText(Name, centerX, centerY + 4, SKTextAlign.Center, font, paint);
        }

        /// <summary>
        /// Draws status indicators based on priority and status.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        protected virtual void DrawStatusIndicators(SKCanvas canvas)
        {
            // Draw priority indicator in top-right corner
            if (BusinessPriority != BusinessPriority.Normal)
            {
                using var priorityPaint = new SKPaint
                {
                    Color = GetPriorityColor(),
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };

                var priorityRect = new SKRect(X + Width - 12, Y + 2, X + Width - 2, Y + 12);
                canvas.DrawOval(priorityRect, priorityPaint);
            }

            // Draw status indicator in bottom-left corner
            if (Status != BusinessStatus.Active)
            {
                using var statusPaint = new SKPaint
                {
                    Color = GetStatusColor(),
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };

                var statusRect = new SKRect(X + 2, Y + Height - 12, X + 12, Y + Height - 2);
                canvas.DrawRect(statusRect, statusPaint);
            }
        }

        /// <summary>
        /// Draws the selection border.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        protected virtual void DrawSelectionBorder(SKCanvas canvas)
        {
            using var selectionPaint = new SKPaint
            {
                Color = SKColors.Orange,
                StrokeWidth = 3,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0)
            };

            var selectionRect = new SKRect(X - 3, Y - 3, X + Width + 3, Y + Height + 3);
            canvas.DrawRect(selectionRect, selectionPaint);
        }

        /// <summary>
        /// Gets the default background color based on component type.
        /// </summary>
        /// <returns>The default background color.</returns>
        protected virtual SKColor GetDefaultBackgroundColor()
        {
            return ComponentType switch
            {
                BusinessComponentType.StartEvent => SKColors.LightGreen,
                BusinessComponentType.EndEvent => SKColors.LightCoral,
                BusinessComponentType.Task => SKColors.LightBlue,
                BusinessComponentType.Decision => SKColors.LightYellow,
                BusinessComponentType.Gateway => SKColors.LightGray,
                BusinessComponentType.Document => SKColors.White,
                BusinessComponentType.Database => SKColors.LightSteelBlue,
                BusinessComponentType.Person => SKColors.LightPink,
                BusinessComponentType.Department => SKColors.LightCyan,
                _ => SKColors.LightBlue
            };
        }

        /// <summary>
        /// Gets the default border color based on component type.
        /// </summary>
        /// <returns>The default border color.</returns>
        protected virtual SKColor GetDefaultBorderColor()
        {
            return ComponentType switch
            {
                BusinessComponentType.StartEvent => SKColors.DarkGreen,
                BusinessComponentType.EndEvent => SKColors.DarkRed,
                BusinessComponentType.Task => SKColors.DarkBlue,
                BusinessComponentType.Decision => SKColors.Orange,
                BusinessComponentType.Gateway => SKColors.Gray,
                BusinessComponentType.Document => SKColors.DarkGray,
                BusinessComponentType.Database => SKColors.SteelBlue,
                BusinessComponentType.Person => SKColors.DeepPink,
                BusinessComponentType.Department => SKColors.DarkCyan,
                _ => SKColors.DarkBlue
            };
        }

        /// <summary>
        /// Gets the color for the priority indicator.
        /// </summary>
        /// <returns>The priority color.</returns>
        protected SKColor GetPriorityColor()
        {
            return BusinessPriority switch
            {
                BusinessPriority.High => SKColors.Red,
                BusinessPriority.Medium => SKColors.Orange,
                BusinessPriority.Low => SKColors.Green,
                _ => SKColors.Gray
            };
        }

        /// <summary>
        /// Gets the color for the status indicator.
        /// </summary>
        /// <returns>The status color.</returns>
        protected SKColor GetStatusColor()
        {
            return Status switch
            {
                BusinessStatus.Active => SKColors.Green,
                BusinessStatus.Pending => SKColors.Yellow,
                BusinessStatus.Completed => SKColors.Blue,
                BusinessStatus.Cancelled => SKColors.Red,
                BusinessStatus.OnHold => SKColors.Gray,
                _ => SKColors.Gray
            };
        }
    }

    /// <summary>
    /// Specifies the type of business component.
    /// </summary>
    public enum BusinessComponentType
    {
        StartEvent,
        EndEvent,
        Task,
        Decision,
        Gateway,
        Document,
        Database,
        DataStore,
        ExternalData,
        Person,
        Department,
        Role,
        System
    }

    /// <summary>
    /// Specifies the priority level of a business component.
    /// </summary>
    public enum BusinessPriority
    {
        Normal,
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Specifies the status of a business component.
    /// </summary>
    public enum BusinessStatus
    {
        Active,
        Pending,
        Completed,
        Cancelled,
        OnHold
    }
}

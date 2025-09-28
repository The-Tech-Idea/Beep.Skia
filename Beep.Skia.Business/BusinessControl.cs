using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using Beep.Skia.Components;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Base control for business graph components (flowcharts, organizational charts, business processes).
    /// Provides common functionality for business diagram elements with support for custom shapes.
    /// </summary>
    public abstract class BusinessControl : MaterialControl
    {
        protected const float PortRadius = 4f;
        protected const float CornerRadius = 8f;

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
    public SKColor BackgroundColor { get; set; } = MaterialColors.Surface;

    /// <summary>
    /// Gets or sets the border color of the component.
    /// </summary>
    public SKColor BorderColor { get; set; } = MaterialColors.Outline;

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
            TextColor = MaterialColors.OnSurface;
            BorderThickness = 2.0f;
            
            // Initialize connection points based on component type
            InitializeConnectionPoints();
        }

        /// <summary>
        /// Initializes connection points based on the component type.
        /// </summary>
        protected virtual void InitializeConnectionPoints()
        {
            // Determine desired port counts based on component type
            int inCount, outCount;
            switch (ComponentType)
            {
                case BusinessComponentType.StartEvent:
                    inCount = 0; outCount = 1; break;
                case BusinessComponentType.EndEvent:
                    inCount = 1; outCount = 0; break;
                case BusinessComponentType.Decision:
                case BusinessComponentType.Gateway:
                    inCount = 1; outCount = 2; break;
                case BusinessComponentType.Document:
                case BusinessComponentType.Database:
                case BusinessComponentType.DataStore:
                case BusinessComponentType.ExternalData:
                case BusinessComponentType.Person:
                case BusinessComponentType.Department:
                case BusinessComponentType.Role:
                case BusinessComponentType.System:
                case BusinessComponentType.Task:
                default:
                    inCount = 1; outCount = 1; break;
            }

            EnsurePortCounts(inCount, outCount);
            // Notify listeners (e.g., DrawingManager) that geometry-affecting port structure changed
            try { OnBoundsChanged(Bounds); } catch { }
        }

        protected void EnsurePortCounts(int inputs, int outputs)
        {
            while (InConnectionPoints.Count < inputs)
                InConnectionPoints.Add(new ConnectionPoint { Type = ConnectionPointType.In, Shape = ComponentShape.Circle, DataType = "link", IsAvailable = true, Component = this, Radius = (int)PortRadius });
            while (InConnectionPoints.Count > inputs)
                InConnectionPoints.RemoveAt(InConnectionPoints.Count - 1);

            while (OutConnectionPoints.Count < outputs)
                OutConnectionPoints.Add(new ConnectionPoint { Type = ConnectionPointType.Out, Shape = ComponentShape.Circle, DataType = "link", IsAvailable = true, Component = this, Radius = (int)PortRadius });
            while (OutConnectionPoints.Count > outputs)
                OutConnectionPoints.RemoveAt(OutConnectionPoints.Count - 1);

            LayoutPorts();
        }

        /// <summary>
        /// Adds an input connection point.
        /// </summary>
        // Legacy helpers retained for compatibility, but EnsurePortCounts is preferred
        protected void AddInputConnectionPoint()
        {
            InConnectionPoints.Add(new ConnectionPoint { Type = ConnectionPointType.In, Radius = (int)PortRadius, Component = this, Shape = ComponentShape.Circle, IsAvailable = true, DataType = "link" });
        }

        /// <summary>
        /// Adds an output connection point.
        /// </summary>
        protected void AddOutputConnectionPoint()
        {
            OutConnectionPoints.Add(new ConnectionPoint { Type = ConnectionPointType.Out, Radius = (int)PortRadius, Component = this, Shape = ComponentShape.Circle, IsAvailable = true, DataType = "link" });
        }

        /// <summary>
        /// Updates the positions of all connection points.
        /// </summary>
        protected virtual void LayoutPorts()
        {
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 6f);
        }

        protected void LayoutPortsVerticalSegments(float topInset, float bottomInset, float leftOffset = -2f, float rightOffset = 2f)
        {
            var b = Bounds;
            float yTop = b.Top + Math.Max(0, topInset);
            float yBottom = b.Bottom - Math.Max(0, bottomInset);
            yBottom = Math.Max(yTop, yBottom);

            int nIn = Math.Max(InConnectionPoints.Count, 1);
            for (int i = 0; i < InConnectionPoints.Count; i++)
            {
                float t = (i + 1) / (float)(nIn + 1);
                float cy = yTop + t * (yBottom - yTop);
                float cx = b.Left + leftOffset;
                var cp = InConnectionPoints[i];
                cp.Center = new SKPoint(cx, cy);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(cx - PortRadius, cy - PortRadius, cx + PortRadius, cy + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = i;
                cp.Component = this;
                cp.IsAvailable = true;
            }

            int nOut = Math.Max(OutConnectionPoints.Count, 1);
            for (int i = 0; i < OutConnectionPoints.Count; i++)
            {
                float t = (i + 1) / (float)(nOut + 1);
                float cy = yTop + t * (yBottom - yTop);
                float cx = b.Right + rightOffset;
                var cp = OutConnectionPoints[i];
                cp.Center = new SKPoint(cx, cy);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(cx - PortRadius, cy - PortRadius, cx + PortRadius, cy + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = i;
                cp.Component = this;
                cp.IsAvailable = true;
            }
        }

        protected void LayoutPortsOnEllipse(float topInset, float bottomInset, float outwardOffset = 2f)
        {
            var b = Bounds;
            float cx = b.MidX;
            float cy = b.MidY;
            float rx = Math.Max(1f, b.Width / 2f);
            float ry = Math.Max(1f, b.Height / 2f);

            float yTop = b.Top + Math.Max(0, topInset);
            float yBottom = b.Bottom - Math.Max(0, bottomInset);
            yBottom = Math.Max(yTop, yBottom);

            int nIn = Math.Max(InConnectionPoints.Count, 1);
            for (int i = 0; i < InConnectionPoints.Count; i++)
            {
                float t = (i + 1) / (float)(nIn + 1);
                float y = yTop + t * (yBottom - yTop);
                y = Math.Clamp(y, cy - ry + 0.001f, cy + ry - 0.001f);
                float dy = y - cy;
                float term = 1f - (dy * dy) / (ry * ry);
                float dx = (float)(rx * Math.Sqrt(Math.Max(0, term)));
                float x = cx - dx; // left intersection

                float nx = (x - cx) / (rx * rx);
                float ny = (y - cy) / (ry * ry);
                float nlen = MathF.Sqrt(nx * nx + ny * ny);
                if (nlen > 1e-5f)
                {
                    nx /= nlen; ny /= nlen;
                    x += nx * outwardOffset; y += ny * outwardOffset;
                }

                var cp = InConnectionPoints[i];
                cp.Center = new SKPoint(x, y);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(x - PortRadius, y - PortRadius, x + PortRadius, y + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = i;
                cp.Component = this;
                cp.IsAvailable = true;
            }

            int nOut = Math.Max(OutConnectionPoints.Count, 1);
            for (int i = 0; i < OutConnectionPoints.Count; i++)
            {
                float t = (i + 1) / (float)(nOut + 1);
                float y = yTop + t * (yBottom - yTop);
                y = Math.Clamp(y, cy - ry + 0.001f, cy + ry - 0.001f);
                float dy = y - cy;
                float term = 1f - (dy * dy) / (ry * ry);
                float dx = (float)(rx * Math.Sqrt(Math.Max(0, term)));
                float x = cx + dx; // right intersection

                float nx = (x - cx) / (rx * rx);
                float ny = (y - cy) / (ry * ry);
                float nlen = MathF.Sqrt(nx * nx + ny * ny);
                if (nlen > 1e-5f)
                {
                    nx /= nlen; ny /= nlen;
                    x += nx * outwardOffset; y += ny * outwardOffset;
                }

                var cp = OutConnectionPoints[i];
                cp.Center = new SKPoint(x, y);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(x - PortRadius, y - PortRadius, x + PortRadius, y + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = i;
                cp.Component = this;
                cp.IsAvailable = true;
            }
        }

        /// <summary>
        /// Draws the connection points.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected virtual void DrawConnectionPoints(SKCanvas canvas, DrawingContext context)
        {
            using var inPaint = new SKPaint { Color = MaterialColors.SecondaryContainer, IsAntialias = true };
            using var outPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
            foreach (var point in InConnectionPoints)
            {
                var c = point.Position;
                canvas.DrawCircle(c.X, c.Y, PortRadius, inPaint);
            }
            foreach (var point in OutConnectionPoints)
            {
                var c = point.Position;
                canvas.DrawCircle(c.X, c.Y, PortRadius, outPaint);
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
            // Let base trigger OnBoundsChanged; LayoutPorts is called there to avoid double work
            base.UpdateBounds();
        }


        /// <summary>
        /// Draws the content of the business component.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            LayoutPorts();
            
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
                Color = MaterialColors.Tertiary,
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
                BusinessComponentType.StartEvent => MaterialColors.SecondaryContainer,
                BusinessComponentType.EndEvent => MaterialColors.ErrorContainer,
                BusinessComponentType.Task => MaterialColors.Surface,
                BusinessComponentType.Decision => MaterialColors.Surface,
                BusinessComponentType.Gateway => MaterialColors.SurfaceVariant,
                BusinessComponentType.Document => MaterialColors.Surface,
                BusinessComponentType.Database => MaterialColors.SurfaceVariant,
                BusinessComponentType.Person => MaterialColors.Surface,
                BusinessComponentType.Department => MaterialColors.Surface,
                _ => MaterialColors.Surface
            };
        }

        /// <summary>
        /// Gets the default border color based on component type.
        /// </summary>
        /// <returns>The default border color.</returns>
        protected virtual SKColor GetDefaultBorderColor()
        {
            return MaterialColors.Outline;
        }

        /// <summary>
        /// Gets the color for the priority indicator.
        /// </summary>
        /// <returns>The priority color.</returns>
        protected SKColor GetPriorityColor()
        {
            return BusinessPriority switch
            {
                BusinessPriority.High => MaterialColors.Error,
                BusinessPriority.Medium => MaterialColors.Tertiary,
                BusinessPriority.Low => MaterialColors.Primary,
                _ => MaterialColors.Outline
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
                BusinessStatus.Active => MaterialColors.Primary,
                BusinessStatus.Pending => MaterialColors.Secondary,
                BusinessStatus.Completed => MaterialColors.Tertiary,
                BusinessStatus.Cancelled => MaterialColors.Error,
                BusinessStatus.OnHold => MaterialColors.OutlineVariant,
                _ => MaterialColors.Outline
            };
        }

        protected override void OnBoundsChanged(SKRect bounds)
        {
            // Keep ports aligned when size or position changes
            LayoutPorts();
            base.OnBoundsChanged(bounds);
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

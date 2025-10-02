using SkiaSharp;
using Beep.Skia;
using System.Collections.Generic;
using Beep.Skia.Model;
using Beep.Skia.Components;
namespace Beep.Skia.UML
{
    /// <summary>
    /// Base class for all UML diagram elements in the Beep.Skia.UML framework.
    /// Provides common functionality for UML-specific controls.
    /// </summary>
    public abstract class UMLControl : MaterialControl
    {
        // Persisted connection points (shared for In/Out in UML)
        private readonly List<IConnectionPoint> _points = new List<IConnectionPoint>();
        private string _stereotype = string.Empty;
        private SKColor _backgroundColor = SKColors.White;
        private SKColor _borderColor = SKColors.Black;
        private float _borderThickness = 1.0f;
        private SKColor _selectionColor = SKColors.LightBlue;

        /// <summary>
        /// Gets or sets the stereotype of this UML element (e.g., "<<interface>>", "<<abstract>>").
        /// </summary>
        public string Stereotype
        {
            get => _stereotype;
            set
            {
                if (_stereotype == value) return;
                _stereotype = value ?? string.Empty;
                if (NodeProperties.TryGetValue("Stereotype", out var pi))
                    pi.ParameterCurrentValue = _stereotype;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the background color of this UML element.
        /// </summary>
        public SKColor BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor == value) return;
                _backgroundColor = value;
                if (NodeProperties.TryGetValue("BackgroundColor", out var pi))
                    pi.ParameterCurrentValue = _backgroundColor;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the border color of this UML element.
        /// </summary>
        public SKColor BorderColor
        {
            get => _borderColor;
            set
            {
                if (_borderColor == value) return;
                _borderColor = value;
                if (NodeProperties.TryGetValue("BorderColor", out var pi))
                    pi.ParameterCurrentValue = _borderColor;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the border thickness.
        /// </summary>
        public float BorderThickness
        {
            get => _borderThickness;
            set
            {
                if (System.Math.Abs(_borderThickness - value) < 0.0001f) return;
                _borderThickness = value;
                if (NodeProperties.TryGetValue("BorderThickness", out var pi))
                    pi.ParameterCurrentValue = _borderThickness;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the selection color.
        /// </summary>
        public SKColor SelectionColor
        {
            get => _selectionColor;
            set
            {
                if (_selectionColor == value) return;
                _selectionColor = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UMLControl"/> class.
        /// </summary>
        protected UMLControl()
        {
            // Set default size for UML elements
            Width = 120;
            Height = 80;

            // Enable palette visibility by default
            ShowInPalette = true;

            // Ensure base text color default and seed NodeProperties metadata for editor
            // Use base TextColor (from SkiaComponent) to avoid hiding
            TextColor = SKColors.Black;

            // Seed NodeProperties
            NodeProperties["Stereotype"] = new ParameterInfo
            {
                ParameterName = "Stereotype",
                ParameterType = typeof(string),
                DefaultParameterValue = _stereotype,
                ParameterCurrentValue = _stereotype,
                Description = "Optional UML stereotype, e.g., <<interface>>"
            };
            NodeProperties["BackgroundColor"] = new ParameterInfo
            {
                ParameterName = "BackgroundColor",
                ParameterType = typeof(SKColor),
                DefaultParameterValue = _backgroundColor,
                ParameterCurrentValue = _backgroundColor,
                Description = "Background color"
            };
            NodeProperties["BorderColor"] = new ParameterInfo
            {
                ParameterName = "BorderColor",
                ParameterType = typeof(SKColor),
                DefaultParameterValue = _borderColor,
                ParameterCurrentValue = _borderColor,
                Description = "Border color"
            };
            NodeProperties["BorderThickness"] = new ParameterInfo
            {
                ParameterName = "BorderThickness",
                ParameterType = typeof(float),
                DefaultParameterValue = _borderThickness,
                ParameterCurrentValue = _borderThickness,
                Description = "Border thickness"
            };
            NodeProperties["TextColor"] = new ParameterInfo
            {
                ParameterName = "TextColor",
                ParameterType = typeof(SKColor),
                DefaultParameterValue = this.TextColor,
                ParameterCurrentValue = this.TextColor,
                Description = "Text color"
            };

            // Initialize persisted connection points (shared In/Out positions)
            InitializeConnectionPoints();
            // Defer initial port layout; will be computed lazily on first draw
            MarkPortsDirty();
        }

        /// <summary>
        /// Draws the selection indicator if the element is selected.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected virtual void DrawSelection(SKCanvas canvas, DrawingContext context)
        {
            if (!IsSelected) return;

            using var paint = new SKPaint
            {
                Color = SelectionColor,
                StrokeWidth = 2.0f,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            var selectionRect = new SKRect(X - 3, Y - 3, X + Width + 3, Y + Height + 3);
            canvas.DrawRect(selectionRect, paint);
        }

        /// <summary>
        /// Draws the background of the UML element.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected virtual void DrawBackground(SKCanvas canvas, DrawingContext context)
        {
            // Use shape-based drawing instead of basic rectangle
            DrawShape(canvas, context);
        }

        /// <summary>
        /// Draws the shape for this UML element. Override in derived classes for custom shapes.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected virtual void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            // Default rectangle shape
            using var fillPaint = new SKPaint
            {
                Color = BackgroundColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            var backgroundRect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRect(backgroundRect, fillPaint);

            using var borderPaint = new SKPaint
            {
                Color = BorderColor,
                StrokeWidth = BorderThickness,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            canvas.DrawRect(backgroundRect, borderPaint);
        }

        /// <summary>
        /// Draws the border of the UML element.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected virtual void DrawBorder(SKCanvas canvas, DrawingContext context)
        {
            using var paint = new SKPaint
            {
                Color = BorderColor,
                StrokeWidth = BorderThickness,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            var borderRect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRect(borderRect, paint);
        }

        /// <summary>
        /// Draws the stereotype text if present.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        /// <param name="font">The font to use for text rendering.</param>
        protected virtual void DrawStereotype(SKCanvas canvas, DrawingContext context, SKFont font)
        {
            if (string.IsNullOrEmpty(Stereotype)) return;

            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            var textBounds = new SKRect();
            font.MeasureText(Stereotype, out textBounds);

            var textX = X + (Width - textBounds.Width) / 2;
            var textY = Y + textBounds.Height + 5; // Small margin from top

            canvas.DrawText(Stereotype, textX, textY, SKTextAlign.Left, font, paint);
        }

        /// <summary>
        /// UML input connection points (persisted).
        /// </summary>
        public override List<IConnectionPoint> InConnectionPoints => _points;

        /// <summary>
        /// UML output connection points (same instances, UML treats ports as bidirectional visually).
        /// </summary>
        public override List<IConnectionPoint> OutConnectionPoints => _points;

        /// <summary>
        /// Initialize the shared connection points at cardinal directions.
        /// </summary>
        protected virtual void InitializeConnectionPoints()
        {
            _points.Clear();
            // top, right, bottom, left
            _points.Add(new ConnectionPoint { Component = this, Type = ConnectionPointType.In, Radius = 6, Shape = ComponentShape.Circle, IsAvailable = true });
            _points.Add(new ConnectionPoint { Component = this, Type = ConnectionPointType.In, Radius = 6, Shape = ComponentShape.Circle, IsAvailable = true });
            _points.Add(new ConnectionPoint { Component = this, Type = ConnectionPointType.In, Radius = 6, Shape = ComponentShape.Circle, IsAvailable = true });
            _points.Add(new ConnectionPoint { Component = this, Type = ConnectionPointType.In, Radius = 6, Shape = ComponentShape.Circle, IsAvailable = true });
        }

        /// <summary>
        /// Update persisted connection point geometry to match current bounds.
        /// </summary>
        protected virtual void LayoutPorts()
        {
            if (_points.Count < 4) return;

            var top = _points[0];
            var right = _points[1];
            var bottom = _points[2];
            var left = _points[3];

            float cx = X + Width / 2f;
            float cy = Y + Height / 2f;
            float r = top is { Radius: > 0 } ? top.Radius : 6f;

            // Top
            top.Center = new SKPoint(cx, Y);
            top.Position = top.Center;
            top.Bounds = new SKRect(top.Center.X - r, top.Center.Y - r, top.Center.X + r, top.Center.Y + r);
            top.Rect = top.Bounds;
            top.Index = 0;
            top.Component = this;

            // Right
            right.Center = new SKPoint(X + Width, cy);
            right.Position = right.Center;
            right.Bounds = new SKRect(right.Center.X - r, right.Center.Y - r, right.Center.X + r, right.Center.Y + r);
            right.Rect = right.Bounds;
            right.Index = 1;
            right.Component = this;

            // Bottom
            bottom.Center = new SKPoint(cx, Y + Height);
            bottom.Position = bottom.Center;
            bottom.Bounds = new SKRect(bottom.Center.X - r, bottom.Center.Y - r, bottom.Center.X + r, bottom.Center.Y + r);
            bottom.Rect = bottom.Bounds;
            bottom.Index = 2;
            bottom.Component = this;

            // Left
            left.Center = new SKPoint(X, cy);
            left.Position = left.Center;
            left.Bounds = new SKRect(left.Center.X - r, left.Center.Y - r, left.Center.X + r, left.Center.Y + r);
            left.Rect = left.Bounds;
            left.Index = 3;
            left.Component = this;
        }

        protected override void UpdateBounds()
        {
            base.UpdateBounds();
            MarkPortsDirty();
        }

        protected override void OnBoundsChanged(SKRect bounds)
        {
            MarkPortsDirty();
            base.OnBoundsChanged(bounds);
        }

        /// <summary>
        /// Centralized draw wrapper to ensure ports are laid out lazily, then delegate to UML visuals.
        /// Derived UML nodes should override DrawUMLContent instead of DrawContent.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            if (ArePortsDirty)
            {
                try { LayoutPorts(); }
                finally { ClearPortsDirty(); }
            }
            DrawUMLContent(canvas, context);
        }

        /// <summary>
        /// Derived UML nodes implement their visuals here. Base DrawContent handles port layout.
        /// </summary>
        protected virtual void DrawUMLContent(SKCanvas canvas, DrawingContext context) { }

        /// <summary>
        /// Draws the connection points for this UML control.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected virtual void DrawConnectionPoints(SKCanvas canvas, DrawingContext context)
        {
            // If In/Out share the same list, draw once
            var points = InConnectionPoints;
            foreach (var point in points)
            {
                DrawConnectionPoint(canvas, point, SKColors.Blue);
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

            // Use Center to avoid drift if Offset is used elsewhere
            var center = point.Center == default ? point.Position : point.Center;
            canvas.DrawCircle(center.X, center.Y, point.Radius, paint);
            canvas.DrawCircle(center.X, center.Y, point.Radius, borderPaint);
        }
    }
}

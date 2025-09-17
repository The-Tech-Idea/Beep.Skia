using SkiaSharp;
using Beep.Skia;
using System.Collections.Generic;
using Beep.Skia.Model;
namespace Beep.Skia.UML
{
    /// <summary>
    /// Base class for all UML diagram elements in the Beep.Skia.UML framework.
    /// Provides common functionality for UML-specific controls.
    /// </summary>
    public abstract class UMLControl : SkiaComponent
    {
        /// <summary>
        /// Gets or sets the stereotype of this UML element (e.g., "<<interface>>", "<<abstract>>").
        /// </summary>
        public string Stereotype { get; set; }

        /// <summary>
        /// Gets or sets the background color of this UML element.
        /// </summary>
        public SKColor BackgroundColor { get; set; } = SKColors.White;

        /// <summary>
        /// Gets or sets the border color of this UML element.
        /// </summary>
        public SKColor BorderColor { get; set; } = SKColors.Black;

        /// <summary>
        /// Gets or sets the text color for this UML element.
        /// </summary>
        public SKColor TextColor { get; set; } = SKColors.Black;

        /// <summary>
        /// Gets or sets the border thickness.
        /// </summary>
        public float BorderThickness { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets whether this element is selected in the diagram.
        /// </summary>
        public new bool IsSelected { get; set; }

        /// <summary>
        /// Gets or sets the selection color.
        /// </summary>
        public SKColor SelectionColor { get; set; } = SKColors.LightBlue;

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
        /// Gets the connection points for this UML element.
        /// </summary>
        /// <returns>A list of connection points.</returns>
        public override List<IConnectionPoint> InConnectionPoints
        {
            get
            {
                var points = new List<IConnectionPoint>();

                // Add connection points at cardinal directions
                var topPoint = new ConnectionPoint
                {
                    Position = new SKPoint(X + Width / 2, Y),
                    Component = this,
                    Type = ConnectionPointType.In
                };
                points.Add(topPoint);

                var rightPoint = new ConnectionPoint
                {
                    Position = new SKPoint(X + Width, Y + Height / 2),
                    Component = this,
                    Type = ConnectionPointType.In
                };
                points.Add(rightPoint);

                var bottomPoint = new ConnectionPoint
                {
                    Position = new SKPoint(X + Width / 2, Y + Height),
                    Component = this,
                    Type = ConnectionPointType.In
                };
                points.Add(bottomPoint);

                var leftPoint = new ConnectionPoint
                {
                    Position = new SKPoint(X, Y + Height / 2),
                    Component = this,
                    Type = ConnectionPointType.In
                };
                points.Add(leftPoint);

                return points;
            }
        }

        /// <summary>
        /// Gets the output connection points for this UML element.
        /// </summary>
        /// <returns>A list of output connection points.</returns>
        public override List<IConnectionPoint> OutConnectionPoints
        {
            get
            {
                // For most UML elements, input and output points are the same
                return InConnectionPoints;
            }
        }
    }
}

using SkiaSharp;
using Beep.Skia;
using System.Collections.Generic;
using Beep.Skia.Model;
namespace Beep.Skia.UML
{
    /// <summary>
    /// Represents a UML class in a class diagram.
    /// Displays class name, attributes, and operations in separate compartments.
    /// </summary>
    public class UMLClass : UMLControl
    {
        /// <summary>
        /// Gets or sets the name of the class.
        /// </summary>
        public string ClassName { get; set; } = "ClassName";

        /// <summary>
        /// Gets or sets the list of attributes for this class.
        /// </summary>
        public List<string> Attributes { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of operations for this class.
        /// </summary>
        public List<string> Operations { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets whether this is an abstract class.
        /// </summary>
        public bool IsAbstract { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UMLClass"/> class.
        /// </summary>
        public UMLClass()
        {
            Width = 150;
            Height = 120;
            Name = "UMLClass";

            // Add some default attributes and operations for demonstration
            Attributes.Add("+attribute1: string");
            Attributes.Add("-attribute2: int");

            Operations.Add("+operation1(): void");
            Operations.Add("-operation2(param: string): bool");
        }

        /// <summary>
        /// Draws the shape for the UML class - a traditional compartmentalized rectangle.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            // Enhanced class box with rounded corners for modern look
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

            // Create rounded rectangle for class shape
            var classRect = new SKRect(X, Y, X + Width, Y + Height);
            var cornerRadius = 4.0f; // Slightly rounded corners for modern appearance
            
            canvas.DrawRoundRect(classRect, cornerRadius, cornerRadius, fillPaint);
            canvas.DrawRoundRect(classRect, cornerRadius, cornerRadius, borderPaint);

            // Add a subtle shadow effect for depth
            using var shadowPaint = new SKPaint
            {
                Color = SKColors.Black.WithAlpha(30),
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 2.0f)
            };

            var shadowRect = new SKRect(X + 2, Y + 2, X + Width + 2, Y + Height + 2);
            canvas.DrawRoundRect(shadowRect, cornerRadius, cornerRadius, shadowPaint);
            
            // Redraw the main shape on top of shadow
            canvas.DrawRoundRect(classRect, cornerRadius, cornerRadius, fillPaint);
            canvas.DrawRoundRect(classRect, cornerRadius, cornerRadius, borderPaint);
        }

        /// <summary>
        /// Draws the content of the UML class.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Draw background and border using shape method
            DrawBackground(canvas, context);

            // Create font for text rendering
            using var font = new SKFont(SKTypeface.Default, 12);
            using var boldFont = new SKFont(SKTypeface.Default, 12) { Embolden = true };

            float currentY = Y + 5;
            const float lineHeight = 18;
            const float compartmentMargin = 5;

            // Draw class name compartment
            var classNameFont = IsAbstract ? boldFont : font;
            var classNameText = IsAbstract ? $"<{ClassName}>" : ClassName;

            DrawTextCentered(canvas, classNameText, X + Width / 2, currentY + lineHeight, classNameFont, TextColor);
            currentY += lineHeight + compartmentMargin;

            // Draw separator line after class name
            DrawHorizontalLine(canvas, currentY, BorderColor, BorderThickness);
            currentY += compartmentMargin;

            // Draw attributes compartment
            foreach (var attribute in Attributes)
            {
                DrawTextLeftAligned(canvas, attribute, X + compartmentMargin, currentY + lineHeight, font, TextColor);
                currentY += lineHeight;
            }

            // Draw separator line after attributes
            if (Attributes.Count > 0)
            {
                currentY += compartmentMargin;
                DrawHorizontalLine(canvas, currentY, BorderColor, BorderThickness);
                currentY += compartmentMargin;
            }

            // Draw operations compartment
            foreach (var operation in Operations)
            {
                DrawTextLeftAligned(canvas, operation, X + compartmentMargin, currentY + lineHeight, font, TextColor);
                currentY += lineHeight;
            }

            // Draw stereotype if present
            DrawStereotype(canvas, context, font);

            // Draw selection indicator
            DrawSelection(canvas, context);
        }

        /// <summary>
        /// Draws text centered horizontally.
        /// </summary>
        private void DrawTextCentered(SKCanvas canvas, string text, float x, float y, SKFont font, SKColor color)
        {
            using var paint = new SKPaint { Color = color, IsAntialias = true };
            canvas.DrawText(text, x, y, SKTextAlign.Center, font, paint);
        }

        /// <summary>
        /// Draws text left-aligned.
        /// </summary>
        private void DrawTextLeftAligned(SKCanvas canvas, string text, float x, float y, SKFont font, SKColor color)
        {
            using var paint = new SKPaint { Color = color, IsAntialias = true };
            canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
        }

        /// <summary>
        /// Draws a horizontal line across the width of the class.
        /// </summary>
        private void DrawHorizontalLine(SKCanvas canvas, float y, SKColor color, float thickness)
        {
            using var paint = new SKPaint
            {
                Color = color,
                StrokeWidth = thickness,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            canvas.DrawLine(X, y, X + Width, y, paint);
        }

        /// <summary>
        /// Updates the bounds of the UML class based on content.
        /// </summary>
        protected override void UpdateBounds()
        {
            // Calculate required height based on content
            const float lineHeight = 18;
            const float compartmentMargin = 5;
            const float padding = 10;

            float requiredHeight = padding; // Top padding

            // Class name
            requiredHeight += lineHeight + compartmentMargin;

            // Attributes
            if (Attributes.Count > 0)
            {
                requiredHeight += Attributes.Count * lineHeight + compartmentMargin;
            }

            // Operations
            if (Operations.Count > 0)
            {
                requiredHeight += Operations.Count * lineHeight + compartmentMargin;
            }

            requiredHeight += padding; // Bottom padding

            // Ensure minimum height
            Height = Math.Max(requiredHeight, 80);

            // Update bounds
            Bounds = new SKRect(X, Y, X + Width, Y + Height);
        }
    }
}
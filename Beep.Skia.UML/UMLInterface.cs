using SkiaSharp;
using Beep.Skia;
using System.Collections.Generic;
using Beep.Skia.Model;

namespace Beep.Skia.UML
{
    /// <summary>
    /// Represents a UML interface in a class diagram.
    /// Displays interface name and operations with interface notation.
    /// </summary>
    public class UMLInterface : UMLControl
    {
        /// <summary>
        /// Gets or sets the name of the interface.
        /// </summary>
        public string InterfaceName
        {
            get => _interfaceName;
            set
            {
                if (_interfaceName != value)
                {
                    _interfaceName = value;
                    DisplayText = value;
                }
            }
        }
        private string _interfaceName = "InterfaceName";

        /// <summary>
        /// Gets or sets the list of operations for this interface.
        /// </summary>
        public List<string> Operations { get; set; } = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="UMLInterface"/> class.
        /// </summary>
        public UMLInterface()
        {
            Width = 150;
            Height = 100;
            Name = "UMLInterface";
            DisplayText = InterfaceName;
            TextPosition = TextPosition.Inside;
            ShowDisplayText = true;

            // Add some default operations for demonstration
            Operations.Add("+operation1(): void");
            Operations.Add("+operation2(param: string): bool");
        }

        /// <summary>
        /// Draws the shape for the UML interface - a circle with interface notation.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
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

            // Create a distinctive circular/oval shape for interface
            float centerX = X + Width / 2;
            float centerY = Y + Height / 2;
            float radiusX = Width / 2 - 5;
            float radiusY = Height / 2 - 5;

            // Create oval path for interface
            using var interfacePath = new SKPath();
            interfacePath.AddOval(new SKRect(centerX - radiusX, centerY - radiusY, 
                                           centerX + radiusX, centerY + radiusY));

            // Add subtle gradient effect
            var gradientColors = new SKColor[] { 
                BackgroundColor.WithAlpha(200), 
                BackgroundColor.WithAlpha(255) 
            };
            using var gradient = SKShader.CreateLinearGradient(
                new SKPoint(X, Y), 
                new SKPoint(X, Y + Height),
                gradientColors, 
                null, 
                SKShaderTileMode.Clamp);

            fillPaint.Shader = gradient;

            // Draw the interface shape
            canvas.DrawPath(interfacePath, fillPaint);
            canvas.DrawPath(interfacePath, borderPaint);

            // Add interface symbol (small 'I' at top)
            using var symbolPaint = new SKPaint
            {
                Color = BorderColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var symbolFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 16);

            canvas.DrawText("I", centerX - 4, Y + 20, SKTextAlign.Center, symbolFont, symbolPaint);
        }

        /// <summary>
        /// Draws the content of the UML interface.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            LayoutPorts();
            
            // Draw background using shape method
            DrawBackground(canvas, context);

            // Create font for text rendering
            using var font = new SKFont(SKTypeface.Default, 11);
            using var boldFont = new SKFont(SKTypeface.Default, 11) { Embolden = true };

            float currentY = Y + 25; // Start below the 'I' symbol
            const float lineHeight = 16;
            const float compartmentMargin = 4;

            // Draw interface name with <<interface>> stereotype
            var interfaceNameText = $"<<interface>>";
            DrawTextCentered(canvas, interfaceNameText, X + Width / 2, currentY + lineHeight, font, TextColor);
            currentY += lineHeight;
            
            DrawTextCentered(canvas, InterfaceName, X + Width / 2, currentY + lineHeight, boldFont, TextColor);
            currentY += lineHeight + compartmentMargin;

            // Draw separator line after interface name
            DrawHorizontalLine(canvas, currentY, BorderColor, BorderThickness);
            currentY += compartmentMargin;

            // Draw operations compartment
            foreach (var operation in Operations)
            {
                DrawTextLeftAligned(canvas, operation, X + compartmentMargin, currentY + lineHeight, font, TextColor);
                currentY += lineHeight;
            }

            // Draw stereotype if present (additional to interface stereotype)
            DrawStereotype(canvas, context, font);

            // Draw connection points
            DrawConnectionPoints(canvas, context);

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
        /// Draws a horizontal line across the width of the interface.
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
        /// Updates the bounds of the UML interface based on content.
        /// </summary>
        protected override void UpdateBounds()
        {
            // Calculate required height based on content
            const float lineHeight = 18;
            const float compartmentMargin = 5;
            const float padding = 10;

            float requiredHeight = padding; // Top padding

            // Interface name (with stereotype)
            requiredHeight += lineHeight * 2 + compartmentMargin;

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
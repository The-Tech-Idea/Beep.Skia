using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
namespace Beep.Skia.UML
{
    /// <summary>
    /// Represents a UML actor in use case diagrams.
    /// Displays a stick figure representation with an actor name below.
    /// </summary>
    public class UMLActor : UMLControl
    {
        /// <summary>
        /// Gets or sets the name of the actor.
        /// </summary>
        public string ActorName
        {
            get => _actorName;
            set
            {
                if (_actorName != value)
                {
                    _actorName = value;
                    DisplayText = value;
                }
            }
        }
        private string _actorName = "Actor";

        /// <summary>
        /// Initializes a new instance of the <see cref="UMLActor"/> class.
        /// </summary>
        public UMLActor()
        {
            Width = 80;
            Height = 120;
            Name = "UMLActor";
            DisplayText = ActorName;
            TextPosition = TextPosition.Below;
            ShowDisplayText = true;

            // Actors typically don't need connection points for use case diagrams
            // but we'll keep them for consistency with the framework
        }

        /// <summary>
        /// Draws the shape for the UML actor - a stick figure representation.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            // Transparent background for actor (no traditional shape background)
            // The stick figure itself is the shape
        }

        /// <summary>
        /// Draws the content of the UML actor.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            LayoutPorts();
            
            // Draw the enhanced stick figure
            DrawEnhancedStickFigure(canvas);

            // Draw the actor name below the figure
            DrawActorName(canvas);

            // Draw stereotype if present
            DrawStereotype(canvas, context, new SKFont(SKTypeface.Default, 10));

            // Draw connection points
            DrawConnectionPoints(canvas, context);

            // Draw selection indicator
            DrawSelection(canvas, context);
        }

        /// <summary>
        /// Draws connection points positioned around the stick figure.
        /// </summary>
        protected override void DrawConnectionPoints(SKCanvas canvas, DrawingContext context)
        {
            float centerX = Width / 2;
            float figureTop = 15;
            float headRadius = 12;
            float headCenterY = figureTop + headRadius;
            float bodyLength = 35;
            float bodyBottom = headCenterY + headRadius + bodyLength;
            float armY = headCenterY + headRadius + 12;
            float armLength = 25;

            // Position connection points around the stick figure
            var points = new List<(SKPoint position, SKColor color)>
            {
                // Above head (input)
                (new SKPoint(centerX, figureTop - 5), SKColors.Blue),
                // Left arm end (output)
                (new SKPoint(centerX - armLength, armY), SKColors.Green),
                // Right arm end (output)
                (new SKPoint(centerX + armLength, armY), SKColors.Green),
                // Below feet (input)
                (new SKPoint(centerX, bodyBottom + 10), SKColors.Blue)
            };

            foreach (var (position, color) in points)
            {
                DrawConnectionPoint(canvas, position, color);
            }
        }

        /// <summary>
        /// Draws a single connection point.
        /// </summary>
        private void DrawConnectionPoint(SKCanvas canvas, SKPoint position, SKColor color)
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

            canvas.DrawCircle(position.X, position.Y, 6, paint);
            canvas.DrawCircle(position.X, position.Y, 6, borderPaint);
        }

        /// <summary>
        /// Draws an enhanced stick figure representation of the actor.
        /// </summary>
        private void DrawEnhancedStickFigure(SKCanvas canvas)
        {
            using var strokePaint = new SKPaint
            {
                Color = TextColor,
                StrokeWidth = 2.5f,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round
            };

            using var fillPaint = new SKPaint
            {
                Color = BackgroundColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float figureTop = Y + 15;

            // Head (filled circle with outline)
            float headRadius = 12;
            float headCenterY = figureTop + headRadius;
            
            canvas.DrawCircle(centerX, headCenterY, headRadius, fillPaint);
            canvas.DrawCircle(centerX, headCenterY, headRadius, strokePaint);

            // Body (vertical line from head to hips)
            float bodyLength = 35;
            float bodyBottom = headCenterY + headRadius + bodyLength;
            canvas.DrawLine(centerX, headCenterY + headRadius, centerX, bodyBottom, strokePaint);

            // Arms (angled lines for more natural pose)
            float armY = headCenterY + headRadius + 12;
            float armLength = 25;
            float armAngle = 0.3f; // Slight downward angle
            
            // Left arm
            canvas.DrawLine(centerX, armY, 
                           centerX - armLength * (float)Math.Cos(armAngle), 
                           armY + armLength * (float)Math.Sin(armAngle), strokePaint);
            // Right arm
            canvas.DrawLine(centerX, armY, 
                           centerX + armLength * (float)Math.Cos(armAngle), 
                           armY + armLength * (float)Math.Sin(armAngle), strokePaint);

            // Legs (angled lines for stance)
            float legLength = 30;
            float legAngle = 0.4f; // Wider stance
            float legBottom = bodyBottom + legLength;

            // Left leg
            canvas.DrawLine(centerX, bodyBottom,
                           centerX - legLength * (float)Math.Sin(legAngle),
                           legBottom, strokePaint);
            // Right leg
            canvas.DrawLine(centerX, bodyBottom,
                           centerX + legLength * (float)Math.Sin(legAngle),
                           legBottom, strokePaint);

            // Add feet (small horizontal lines)
            float footLength = 8;
            canvas.DrawLine(centerX - legLength * (float)Math.Sin(legAngle) - footLength, legBottom,
                           centerX - legLength * (float)Math.Sin(legAngle), legBottom, strokePaint);
            canvas.DrawLine(centerX + legLength * (float)Math.Sin(legAngle), legBottom,
                           centerX + legLength * (float)Math.Sin(legAngle) + footLength, legBottom, strokePaint);
        }

        /// <summary>
        /// Draws the actor name below the stick figure.
        /// </summary>
        private void DrawActorName(SKCanvas canvas)
        {
            using var font = new SKFont(SKTypeface.Default, 12);
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            float textY = Y + Height - 5;
            canvas.DrawText(ActorName, X + Width / 2, textY, SKTextAlign.Center, font, paint);
        }

        /// <summary>
        /// Updates the bounds of the UML actor based on content.
        /// </summary>
        protected override void UpdateBounds()
        {
            // Calculate required height based on figure and text
            const float figureHeight = 90;
            const float textHeight = 20;
            const float padding = 10;

            float requiredHeight = figureHeight + textHeight + padding * 2;

            // Ensure minimum dimensions
            Height = System.Math.Max(requiredHeight, 100);
            Width = System.Math.Max(Width, 60);

            // Update bounds
            Bounds = new SKRect(X, Y, X + Width, Y + Height);
        }
    }
}
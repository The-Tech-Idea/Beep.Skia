using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a start event in a business process diagram.
    /// Displayed as a thin-bordered circle.
    /// </summary>
    public class StartEvent : BusinessControl
    {
        public StartEvent()
        {
            Width = 60;
            Height = 60;
            Name = "Start";
            ComponentType = BusinessComponentType.StartEvent;
        }

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
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float centerY = Y + Height / 2;
            float radius = Math.Min(Width, Height) / 2 - 2;

            canvas.DrawCircle(centerX, centerY, radius, fillPaint);
            canvas.DrawCircle(centerX, centerY, radius, borderPaint);
        }
    }

    /// <summary>
    /// Represents an end event in a business process diagram.
    /// Displayed as a thick-bordered circle.
    /// </summary>
    public class EndEvent : BusinessControl
    {
        public EndEvent()
        {
            Width = 60;
            Height = 60;
            Name = "End";
            ComponentType = BusinessComponentType.EndEvent;
        }

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
                StrokeWidth = 4, // Thick border for end event
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float centerY = Y + Height / 2;
            float radius = Math.Min(Width, Height) / 2 - 4;

            canvas.DrawCircle(centerX, centerY, radius, fillPaint);
            canvas.DrawCircle(centerX, centerY, radius, borderPaint);
        }
    }

    /// <summary>
    /// Represents a task in a business process diagram.
    /// Displayed as a rounded rectangle.
    /// </summary>
    public class BusinessTask : BusinessControl
    {
        public BusinessTask()
        {
            Width = 120;
            Height = 60;
            Name = "Task";
            ComponentType = BusinessComponentType.Task;
        }

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

            var rect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(rect, 12, 12, fillPaint);
            canvas.DrawRoundRect(rect, 12, 12, borderPaint);
        }
    }

    /// <summary>
    /// Represents a decision point in a business process diagram.
    /// Displayed as a diamond shape.
    /// </summary>
    public class Decision : BusinessControl
    {
        public Decision()
        {
            Width = 80;
            Height = 80;
            Name = "Decision";
            ComponentType = BusinessComponentType.Decision;
        }

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

            // Create diamond path
            using var path = new SKPath();
            float centerX = X + Width / 2;
            float centerY = Y + Height / 2;
            float halfWidth = Width / 2;
            float halfHeight = Height / 2;

            path.MoveTo(centerX, Y); // Top
            path.LineTo(X + Width, centerY); // Right
            path.LineTo(centerX, Y + Height); // Bottom
            path.LineTo(X, centerY); // Left
            path.Close();

            canvas.DrawPath(path, fillPaint);
            canvas.DrawPath(path, borderPaint);
        }
    }

    /// <summary>
    /// Represents a gateway in a business process diagram.
    /// Displayed as a diamond with a plus sign inside.
    /// </summary>
    public class Gateway : BusinessControl
    {
        public Gateway()
        {
            Width = 70;
            Height = 70;
            Name = "Gateway";
            ComponentType = BusinessComponentType.Gateway;
        }

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

            // Create diamond path
            using var path = new SKPath();
            float centerX = X + Width / 2;
            float centerY = Y + Height / 2;

            path.MoveTo(centerX, Y + 5); // Top
            path.LineTo(X + Width - 5, centerY); // Right
            path.LineTo(centerX, Y + Height - 5); // Bottom
            path.LineTo(X + 5, centerY); // Left
            path.Close();

            canvas.DrawPath(path, fillPaint);
            canvas.DrawPath(path, borderPaint);

            // Draw plus sign
            using var plusPaint = new SKPaint
            {
                Color = BorderColor,
                StrokeWidth = 3,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };

            float plusSize = 12;
            // Horizontal line
            canvas.DrawLine(centerX - plusSize, centerY, centerX + plusSize, centerY, plusPaint);
            // Vertical line
            canvas.DrawLine(centerX, centerY - plusSize, centerX, centerY + plusSize, plusPaint);
        }

        protected override void DrawComponentText(SKCanvas canvas)
        {
            // Override to not draw text in the center (due to plus sign)
            if (string.IsNullOrEmpty(Name))
                return;

            using var font = new SKFont(SKTypeface.Default, 10);
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float textY = Y + Height + 15; // Draw text below the shape

            canvas.DrawText(Name, centerX, textY, SKTextAlign.Center, font, paint);
        }
    }
}
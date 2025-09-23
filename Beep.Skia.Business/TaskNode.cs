using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a workflow task node with status indicators and progress tracking.
    /// Displayed as a rounded rectangle with task-specific icons and status.
    /// </summary>
    public class TaskNode : BusinessControl
    {
        public string TaskDescription { get; set; } = "Task Description";
        public TaskStatus TaskStatus { get; set; } = TaskStatus.NotStarted;
        public string AssignedTo { get; set; } = "";
        public DateTime? DueDate { get; set; }
        public int Progress { get; set; } = 0; // 0-100

        public TaskNode()
        {
            Width = 140;
            Height = 80;
            Name = "Task";
            ComponentType = BusinessComponentType.Task;
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            using var fillPaint = new SKPaint
            {
                Color = GetTaskStatusColor(),
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

            // Draw task icon
            DrawTaskIcon(canvas);

            // Draw progress bar if in progress
            if (TaskStatus == TaskStatus.InProgress && Progress > 0)
            {
                DrawProgressBar(canvas);
            }
        }

        private void DrawTaskIcon(SKCanvas canvas)
        {
            using var iconPaint = new SKPaint
            {
                Color = TextColor,
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            float iconX = X + 10;
            float iconY = Y + 10;
            float iconSize = 16;

            // Draw task checklist icon
            canvas.DrawRect(iconX, iconY, iconX + iconSize, iconY + iconSize, iconPaint);

            // Draw checkmark if completed
            if (TaskStatus == TaskStatus.Completed)
            {
                using var checkPaint = new SKPaint
                {
                    Color = SKColors.Green,
                    StrokeWidth = 2,
                    Style = SKPaintStyle.Stroke,
                    IsAntialias = true,
                    StrokeCap = SKStrokeCap.Round
                };

                using var checkPath = new SKPath();
                checkPath.MoveTo(iconX + 3, iconY + 8);
                checkPath.LineTo(iconX + 7, iconY + 12);
                checkPath.LineTo(iconX + 13, iconY + 6);

                canvas.DrawPath(checkPath, checkPaint);
            }
        }

        private void DrawProgressBar(SKCanvas canvas)
        {
            float barWidth = Width - 20;
            float barHeight = 6;
            float barX = X + 10;
            float barY = Y + Height - 15;

            // Background
            using var bgPaint = new SKPaint
            {
                Color = SKColors.LightGray,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            var bgRect = new SKRect(barX, barY, barX + barWidth, barY + barHeight);
            canvas.DrawRoundRect(bgRect, 3, 3, bgPaint);

            // Progress fill
            if (Progress > 0)
            {
                using var progressPaint = new SKPaint
                {
                    Color = SKColors.Green,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };

                float progressWidth = barWidth * Progress / 100f;
                var progressRect = new SKRect(barX, barY, barX + progressWidth, barY + barHeight);
                canvas.DrawRoundRect(progressRect, 3, 3, progressPaint);
            }
        }

        private SKColor GetTaskStatusColor()
        {
            return TaskStatus switch
            {
                TaskStatus.NotStarted => SKColors.LightBlue,
                TaskStatus.InProgress => SKColors.LightYellow,
                TaskStatus.Completed => SKColors.LightGreen,
                TaskStatus.Cancelled => SKColors.LightCoral,
                TaskStatus.OnHold => SKColors.LightGray,
                _ => SKColors.LightBlue
            };
        }

        protected override void DrawComponentText(SKCanvas canvas)
        {
            using var nameFont = new SKFont(SKTypeface.Default, 10) { Embolden = true };
            using var descFont = new SKFont(SKTypeface.Default, 8);
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float nameY = Y + Height + 15;
            float descY = nameY + 12;

            canvas.DrawText(Name, centerX, nameY, SKTextAlign.Center, nameFont, paint);

            if (!string.IsNullOrEmpty(TaskDescription))
            {
                canvas.DrawText(TaskDescription, centerX, descY, SKTextAlign.Center, descFont, paint);
            }
        }
    }
}
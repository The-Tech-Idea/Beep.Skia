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
        private string _taskDescription = "Task Description";
        public string TaskDescription
        {
            get => _taskDescription;
            set
            {
                if (_taskDescription != value)
                {
                    _taskDescription = value ?? string.Empty;
                    if (NodeProperties.TryGetValue("TaskDescription", out var p)) p.ParameterCurrentValue = _taskDescription; else NodeProperties["TaskDescription"] = new ParameterInfo { ParameterName = "TaskDescription", ParameterType = typeof(string), DefaultParameterValue = _taskDescription, ParameterCurrentValue = _taskDescription, Description = "Task description" };
                    InvalidateVisual();
                }
            }
        }

        private TaskStatus _taskStatus = TaskStatus.NotStarted;
        public TaskStatus TaskStatus
        {
            get => _taskStatus;
            set
            {
                if (_taskStatus != value)
                {
                    _taskStatus = value;
                    if (NodeProperties.TryGetValue("TaskStatus", out var p)) p.ParameterCurrentValue = value; else NodeProperties["TaskStatus"] = new ParameterInfo { ParameterName = "TaskStatus", ParameterType = typeof(TaskStatus), DefaultParameterValue = value, ParameterCurrentValue = value, Description = "Task status", Choices = Enum.GetNames(typeof(TaskStatus)) };
                    InvalidateVisual();
                }
            }
        }

        private string _assignedTo = string.Empty;
        public string AssignedTo
        {
            get => _assignedTo;
            set
            {
                if (_assignedTo != value)
                {
                    _assignedTo = value ?? string.Empty;
                    if (NodeProperties.TryGetValue("AssignedTo", out var p)) p.ParameterCurrentValue = _assignedTo; else NodeProperties["AssignedTo"] = new ParameterInfo { ParameterName = "AssignedTo", ParameterType = typeof(string), DefaultParameterValue = _assignedTo, ParameterCurrentValue = _assignedTo, Description = "Assigned owner" };
                    InvalidateVisual();
                }
            }
        }

        private DateTime? _dueDate;
        public DateTime? DueDate
        {
            get => _dueDate;
            set
            {
                if (_dueDate != value)
                {
                    _dueDate = value;
                    if (NodeProperties.TryGetValue("DueDate", out var p)) p.ParameterCurrentValue = _dueDate; else NodeProperties["DueDate"] = new ParameterInfo { ParameterName = "DueDate", ParameterType = typeof(DateTime?), DefaultParameterValue = _dueDate, ParameterCurrentValue = _dueDate, Description = "Due date" };
                    InvalidateVisual();
                }
            }
        }

        private int _progress = 0; // 0-100
        public int Progress
        {
            get => _progress;
            set
            {
                var v = Math.Clamp(value, 0, 100);
                if (_progress != v)
                {
                    _progress = v;
                    if (NodeProperties.TryGetValue("Progress", out var p)) p.ParameterCurrentValue = _progress; else NodeProperties["Progress"] = new ParameterInfo { ParameterName = "Progress", ParameterType = typeof(int), DefaultParameterValue = _progress, ParameterCurrentValue = _progress, Description = "Percent complete (0-100)" };
                    InvalidateVisual();
                }
            }
        }

        public TaskNode()
        {
            Width = 140;
            Height = 80;
            Name = "Task";
            ComponentType = BusinessComponentType.Task;
            // Seed TaskNode-specific NodeProperties
            NodeProperties["TaskDescription"] = new ParameterInfo { ParameterName = "TaskDescription", ParameterType = typeof(string), DefaultParameterValue = _taskDescription, ParameterCurrentValue = _taskDescription, Description = "Task description" };
            NodeProperties["TaskStatus"] = new ParameterInfo { ParameterName = "TaskStatus", ParameterType = typeof(TaskStatus), DefaultParameterValue = _taskStatus, ParameterCurrentValue = _taskStatus, Description = "Task status", Choices = Enum.GetNames(typeof(TaskStatus)) };
            NodeProperties["AssignedTo"] = new ParameterInfo { ParameterName = "AssignedTo", ParameterType = typeof(string), DefaultParameterValue = _assignedTo, ParameterCurrentValue = _assignedTo, Description = "Assigned owner" };
            NodeProperties["DueDate"] = new ParameterInfo { ParameterName = "DueDate", ParameterType = typeof(DateTime?), DefaultParameterValue = _dueDate, ParameterCurrentValue = _dueDate, Description = "Due date" };
            NodeProperties["Progress"] = new ParameterInfo { ParameterName = "Progress", ParameterType = typeof(int), DefaultParameterValue = _progress, ParameterCurrentValue = _progress, Description = "Percent complete (0-100)" };
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
                    Color = MaterialColors.Primary,
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
                Color = MaterialColors.SurfaceVariant,
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
                    Color = MaterialColors.Primary,
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
                TaskStatus.NotStarted => MaterialColors.Surface,
                TaskStatus.InProgress => MaterialColors.SecondaryContainer,
                TaskStatus.Completed => MaterialColors.TertiaryContainer,
                TaskStatus.Cancelled => MaterialColors.ErrorContainer,
                TaskStatus.OnHold => MaterialColors.SurfaceVariant,
                _ => MaterialColors.Surface
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
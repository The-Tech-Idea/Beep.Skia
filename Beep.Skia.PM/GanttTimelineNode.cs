using Beep.Skia.Components;
using Beep.Skia.Model;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Beep.Skia.PM
{
    /// <summary>
    /// A single row in the Gantt timeline (task bar).
    /// </summary>
    public class GanttRow
    {
        public string Name { get; set; } = string.Empty;
        public int StartDay { get; set; } = 1;
        public int FinishDay { get; set; } = 1;
        public float PercentComplete { get; set; }
        public bool IsCritical { get; set; }

        public int DurationDays => Math.Max(1, FinishDay - StartDay + 1);
    }

    /// <summary>
    /// Gantt timeline view: renders scheduled tasks as bars over a day grid,
    /// highlighting the critical path. Populate with <see cref="SetSchedule"/>.
    /// </summary>
    public class GanttTimelineNode : PMControl
    {
        private float _rowHeight = 26f;
        private float _headerHeight = 30f;
        private float _dayWidth = 22f;
        private float _labelWidth = 160f;

        public float RowHeight { get => _rowHeight; set { _rowHeight = Math.Max(16f, value); InvalidateVisual(); } }
        public float HeaderHeight { get => _headerHeight; set { _headerHeight = Math.Max(20f, value); InvalidateVisual(); } }
        public float DayWidth { get => _dayWidth; set { _dayWidth = Math.Max(6f, value); InvalidateVisual(); } }
        public float LabelWidth { get => _labelWidth; set { _labelWidth = Math.Max(60f, value); InvalidateVisual(); } }

        /// <summary>Highlight rows on the critical path in a distinct color.</summary>
        public bool ShowCriticalPath { get; set; } = true;

        public SKColor BarColor { get; set; } = new SKColor(0x42, 0xA5, 0xF5);
        public SKColor CriticalBarColor { get; set; } = new SKColor(0xE5, 0x39, 0x35);
        public SKColor ProgressColor { get; set; } = new SKColor(0x1E, 0x88, 0xE5);
        public SKColor GridColor { get; set; } = new SKColor(0xE0, 0xE0, 0xE0);

        public List<GanttRow> Rows { get; } = new List<GanttRow>();

        /// <summary>
        /// JSON projection of <see cref="Rows"/> used for persistence.
        /// </summary>
        public string RowsJson
        {
            get => JsonSerializer.Serialize(Rows);
            set
            {
                Rows.Clear();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    try
                    {
                        var rows = JsonSerializer.Deserialize<List<GanttRow>>(value);
                        if (rows != null) Rows.AddRange(rows);
                    }
                    catch { }
                }
                SyncRowMetadata();
                ResizeToContent();
                InvalidateVisual();
            }
        }

        public GanttTimelineNode()
        {
            Width = 720;
            Height = 260;
            Name = "Gantt Timeline";
            EnsurePortCounts(0, 0);

            NodeProperties["RowHeight"] = new ParameterInfo { ParameterName = "RowHeight", ParameterType = typeof(float), DefaultParameterValue = _rowHeight, ParameterCurrentValue = _rowHeight, Description = "Height of each task row" };
            NodeProperties["DayWidth"] = new ParameterInfo { ParameterName = "DayWidth", ParameterType = typeof(float), DefaultParameterValue = _dayWidth, ParameterCurrentValue = _dayWidth, Description = "Width of one day column" };
            NodeProperties["RowsJson"] = new ParameterInfo { ParameterName = "RowsJson", ParameterType = typeof(string), DefaultParameterValue = "[]", ParameterCurrentValue = "[]", Description = "Persisted schedule rows (JSON)" };
        }

        /// <summary>
        /// Populates the timeline from tasks, optionally using a computed schedule for dates/criticality.
        /// Without a schedule, tasks are laid out sequentially.
        /// </summary>
        public void SetSchedule(IEnumerable<TaskNode> tasks, CriticalPathCalculator? schedule = null)
        {
            Rows.Clear();
            if (tasks != null)
            {
                var taskList = tasks.Where(t => !t.IsStatic).ToList();

                if (schedule != null && schedule.Schedules.Count > 0)
                {
                    foreach (var s in schedule.Schedules
                                 .Where(s => s.Task != null)
                                 .OrderBy(s => s.EarlyStart)
                                 .ThenBy(s => s.Task!.Title))
                    {
                        Rows.Add(new GanttRow
                        {
                            Name = s.Task!.Title ?? s.Task.Name ?? "Task",
                            StartDay = Math.Max(1, s.EarlyStart),
                            FinishDay = Math.Max(1, s.EarlyFinish),
                            PercentComplete = s.Task.PercentComplete,
                            IsCritical = s.IsCritical
                        });
                    }
                }
                else
                {
                    int day = 1;
                    foreach (var task in taskList)
                    {
                        int duration = Math.Max(1, task.DurationDays);
                        Rows.Add(new GanttRow
                        {
                            Name = task.Title ?? task.Name ?? "Task",
                            StartDay = day,
                            FinishDay = day + duration - 1,
                            PercentComplete = task.PercentComplete,
                            IsCritical = false
                        });
                        day += duration;
                    }
                }
            }

            SyncRowMetadata();
            ResizeToContent();
            InvalidateVisual();
        }

        private void SyncRowMetadata()
        {
            if (NodeProperties.TryGetValue("RowsJson", out var pi))
            {
                pi.ParameterCurrentValue = RowsJson;
            }
        }

        private void ResizeToContent()
        {
            float height = HeaderHeight + Rows.Count * RowHeight + 10f;
            Height = Math.Max(120f, height);

            int totalDays = Math.Max(10, Rows.Count == 0 ? 10 : Rows.Max(r => r.FinishDay));
            float width = LabelWidth + totalDays * DayWidth + 24f;
            Width = Math.Max(320f, width);
        }

        protected override void LayoutPorts()
        {
            // Container view: no connection ports.
        }

        protected override void DrawPMContent(SKCanvas canvas, DrawingContext context)
        {
            var bounds = new SKRect(X, Y, X + Width, Y + Height);
            using var paint = new SKPaint { IsAntialias = true };
            using var textPaint = new SKPaint { Color = TextColor, IsAntialias = true };
            using var headerFont = new SKFont(SKTypeface.Default, 10) { Embolden = true };
            using var labelFont = new SKFont(SKTypeface.Default, 9);

            // Surface
            paint.Color = MaterialColors.Surface;
            paint.Style = SKPaintStyle.Fill;
            canvas.DrawRoundRect(bounds, 6, 6, paint);

            // Header band
            var headerRect = new SKRect(X, Y, X + Width, Y + HeaderHeight);
            paint.Color = MaterialColors.SurfaceContainerHigh;
            canvas.DrawRect(headerRect, paint);

            int totalDays = Math.Max(10, Rows.Count == 0 ? 10 : Rows.Max(r => r.FinishDay));
            float gridLeft = X + LabelWidth;

            // Day grid + header ticks every 5 days
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = 1f;
            paint.Color = GridColor;
            for (int d = 5; d <= totalDays; d += 5)
            {
                float gx = gridLeft + d * DayWidth;
                canvas.DrawLine(gx, Y + HeaderHeight, gx, Y + Height, paint);
                canvas.DrawText($"D{d}", gx + 2, Y + HeaderHeight - 10, SKTextAlign.Left, headerFont, textPaint);
            }

            // Task column header
            canvas.DrawText("Task", X + 6, Y + HeaderHeight - 10, SKTextAlign.Left, headerFont, textPaint);

            // Rows
            for (int i = 0; i < Rows.Count; i++)
            {
                var row = Rows[i];
                float y = Y + HeaderHeight + i * RowHeight;
                if (y + RowHeight > Y + Height) break;

                if (i % 2 == 1)
                {
                    paint.Style = SKPaintStyle.Fill;
                    paint.Color = MaterialColors.SurfaceVariant;
                    canvas.DrawRect(new SKRect(X, y, X + Width, y + RowHeight), paint);
                }

                // Label
                string name = Truncate(row.Name, labelFont, LabelWidth - 12f);
                canvas.DrawText(name, X + 6, y + RowHeight / 2f + 3f, SKTextAlign.Left, labelFont, textPaint);

                // Bar
                float barLeft = gridLeft + (Math.Max(1, row.StartDay) - 1) * DayWidth;
                float barWidth = Math.Max(DayWidth, row.DurationDays * DayWidth);
                var barRect = new SKRect(barLeft, y + 4f, barLeft + barWidth, y + RowHeight - 4f);

                paint.Style = SKPaintStyle.Fill;
                paint.Color = (ShowCriticalPath && row.IsCritical) ? CriticalBarColor : BarColor;
                canvas.DrawRoundRect(barRect, 3, 3, paint);

                // Progress
                float pct = Math.Max(0f, Math.Min(100f, row.PercentComplete));
                if (pct > 0)
                {
                    float progressWidth = barWidth * (pct / 100f);
                    var progressRect = new SKRect(barRect.Left, barRect.Top, barRect.Left + progressWidth, barRect.Bottom);
                    paint.Color = ProgressColor;
                    canvas.DrawRoundRect(progressRect, 3, 3, paint);
                }

                // Duration text inside the bar when it fits
                string durationText = $"{row.DurationDays}d";
                if (labelFont.MeasureText(durationText) + 8f < barWidth)
                {
                    using var barTextPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
                    canvas.DrawText(durationText, barRect.Left + 4f, barRect.MidY + 3f, SKTextAlign.Left, labelFont, barTextPaint);
                }
            }

            // Outer border
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = 1.2f;
            paint.Color = MaterialColors.Outline;
            canvas.DrawRoundRect(bounds, 6, 6, paint);
        }

        private static string Truncate(string text, SKFont font, float maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (font.MeasureText(text) <= maxWidth) return text;

            for (int length = text.Length - 1; length > 0; length--)
            {
                var candidate = text.Substring(0, length) + "…";
                if (font.MeasureText(candidate) <= maxWidth) return candidate;
            }
            return "…";
        }
    }
}
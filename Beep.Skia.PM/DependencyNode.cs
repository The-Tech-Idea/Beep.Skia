using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.PM
{
    /// <summary>
    /// PM Dependency node: represents task dependencies with relationship types (FS, SS, FF, SF).
    /// Arrow/link style with lag/lead time indication.
    /// </summary>
    public class DependencyNode : PMControl
    {
        private string _dependencyType = "FS";
        public string DependencyType
        {
            get => _dependencyType;
            set
            {
                var v = value ?? "FS";
                if (_dependencyType != v)
                {
                    _dependencyType = v;
                    if (NodeProperties.TryGetValue("DependencyType", out var p))
                        p.ParameterCurrentValue = _dependencyType;
                    InvalidateVisual();
                }
            }
        }

        private int _lagDays = 0;
        public int LagDays
        {
            get => _lagDays;
            set
            {
                if (_lagDays != value)
                {
                    _lagDays = value;
                    if (NodeProperties.TryGetValue("LagDays", out var p))
                        p.ParameterCurrentValue = _lagDays;
                    InvalidateVisual();
                }
            }
        }

        private string _fromTask = "";
        public string FromTask
        {
            get => _fromTask;
            set
            {
                var v = value ?? "";
                if (_fromTask != v)
                {
                    _fromTask = v;
                    if (NodeProperties.TryGetValue("FromTask", out var p))
                        p.ParameterCurrentValue = _fromTask;
                    InvalidateVisual();
                }
            }
        }

        private string _toTask = "";
        public string ToTask
        {
            get => _toTask;
            set
            {
                var v = value ?? "";
                if (_toTask != v)
                {
                    _toTask = v;
                    if (NodeProperties.TryGetValue("ToTask", out var p))
                        p.ParameterCurrentValue = _toTask;
                    InvalidateVisual();
                }
            }
        }

        public DependencyNode()
        {
            Name = "PM Dependency";
            Width = 120;
            Height = 60;
            EnsurePortCounts(1, 1);

            NodeProperties["DependencyType"] = new ParameterInfo
            {
                ParameterName = "DependencyType",
                ParameterType = typeof(string),
                DefaultParameterValue = _dependencyType,
                ParameterCurrentValue = _dependencyType,
                Description = "Dependency type (FS=Finish-to-Start, SS=Start-to-Start, FF=Finish-to-Finish, SF=Start-to-Finish)",
                Choices = new[] { "FS", "SS", "FF", "SF" }
            };
            NodeProperties["LagDays"] = new ParameterInfo
            {
                ParameterName = "LagDays",
                ParameterType = typeof(int),
                DefaultParameterValue = _lagDays,
                ParameterCurrentValue = _lagDays,
                Description = "Lag time in days (negative = lead time)"
            };
            NodeProperties["FromTask"] = new ParameterInfo
            {
                ParameterName = "FromTask",
                ParameterType = typeof(string),
                DefaultParameterValue = _fromTask,
                ParameterCurrentValue = _fromTask,
                Description = "Predecessor task"
            };
            NodeProperties["ToTask"] = new ParameterInfo
            {
                ParameterName = "ToTask",
                ParameterType = typeof(string),
                DefaultParameterValue = _toTask,
                ParameterCurrentValue = _toTask,
                Description = "Successor task"
            };
        }

        protected override void LayoutPorts()
        {
            LayoutPortsVerticalSegments(topInset: 8f, bottomInset: 8f);
        }

        protected override void DrawPMContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;

            using var fill = new SKPaint { Color = new SKColor(0xE3, 0xF2, 0xFD), IsAntialias = true };
            using var stroke = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var text = new SKPaint { Color = MaterialColors.OnSurface, IsAntialias = true };

            // Draw parallelogram (link shape)
            float skew = 10f;
            using var path = new SKPath();
            path.MoveTo(r.Left + skew, r.Top);
            path.LineTo(r.Right, r.Top);
            path.LineTo(r.Right - skew, r.Bottom);
            path.LineTo(r.Left, r.Bottom);
            path.Close();

            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            // Draw arrow icon
            float arrowX = r.Left + 15;
            float arrowY = r.MidY;
            using var arrowPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2f, StrokeCap = SKStrokeCap.Round };
            canvas.DrawLine(arrowX, arrowY, arrowX + 20, arrowY, arrowPaint);
            canvas.DrawLine(arrowX + 20, arrowY, arrowX + 15, arrowY - 4, arrowPaint);
            canvas.DrawLine(arrowX + 20, arrowY, arrowX + 15, arrowY + 4, arrowPaint);

            // Draw dependency type
            using var typeFont = new SKFont(SKTypeface.Default, 14) { Embolden = true };
            canvas.DrawText(DependencyType, arrowX + 28, r.Top + 20, SKTextAlign.Left, typeFont, text);

            // Draw lag/lead indicator
            if (LagDays != 0)
            {
                using var lagFont = new SKFont(SKTypeface.Default, 10);
                using var lagText = new SKPaint { Color = LagDays > 0 ? new SKColor(0xE5, 0x39, 0x35) : new SKColor(0x43, 0xA0, 0x47), IsAntialias = true };
                string lagStr = LagDays > 0 ? $"+{LagDays}d" : $"{LagDays}d";
                canvas.DrawText(lagStr, r.MidX, r.Bottom - 8, SKTextAlign.Center, lagFont, lagText);
            }

            DrawPorts(canvas);
        }
    }
}

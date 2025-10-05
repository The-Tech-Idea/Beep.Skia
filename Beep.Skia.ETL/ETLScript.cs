using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// ETL Script node: executes custom transformation script (SQL, Python, C#, etc).
    /// Allows arbitrary transformations beyond built-in operations.
    /// </summary>
    public class ETLScript : ETLControl
    {
        private string _scriptLanguage = "SQL";
        public string ScriptLanguage
        {
            get => _scriptLanguage;
            set
            {
                var v = value ?? "SQL";
                if (_scriptLanguage == v) return;
                _scriptLanguage = v;
                if (NodeProperties.TryGetValue("ScriptLanguage", out var p))
                    p.ParameterCurrentValue = _scriptLanguage;
                InvalidateVisual();
            }
        }

        private string _script = "";
        public string Script
        {
            get => _script;
            set
            {
                var v = value ?? "";
                if (_script == v) return;
                _script = v;
                if (NodeProperties.TryGetValue("Script", out var p))
                    p.ParameterCurrentValue = _script;
                InvalidateVisual();
            }
        }

        public ETLScript()
        {
            Title = "Script";
            Subtitle = "Custom Transform";
            EnsurePortCounts(1, 1);
            HeaderColor = MaterialColors.SecondaryContainer;

            NodeProperties["ScriptLanguage"] = new ParameterInfo
            {
                ParameterName = "ScriptLanguage",
                ParameterType = typeof(string),
                DefaultParameterValue = _scriptLanguage,
                ParameterCurrentValue = _scriptLanguage,
                Description = "Script language (SQL/Python/C#/JavaScript)",
                Choices = new[] { "SQL", "Python", "C#", "JavaScript", "PowerShell" }
            };
            NodeProperties["Script"] = new ParameterInfo
            {
                ParameterName = "Script",
                ParameterType = typeof(string),
                DefaultParameterValue = _script,
                ParameterCurrentValue = _script,
                Description = "Transformation script code"
            };
        }

        protected override void DrawETLContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;
            base.DrawETLContent(canvas, context);

            // Draw script icon (code brackets)
            var r = Bounds;
            float centerX = r.MidX;
            float centerY = r.Top + HeaderHeight + (r.Height - HeaderHeight) / 2;
            float size = 14f;

            using var iconPaint = new SKPaint
            {
                Color = MaterialColors.OnSurface.WithAlpha(128),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };

            // Left bracket {
            canvas.DrawLine(centerX - size / 2, centerY - size, centerX - size, centerY - size, iconPaint);
            canvas.DrawLine(centerX - size, centerY - size, centerX - size, centerY + size, iconPaint);
            canvas.DrawLine(centerX - size, centerY + size, centerX - size / 2, centerY + size, iconPaint);

            // Right bracket }
            canvas.DrawLine(centerX + size / 2, centerY - size, centerX + size, centerY - size, iconPaint);
            canvas.DrawLine(centerX + size, centerY - size, centerX + size, centerY + size, iconPaint);
            canvas.DrawLine(centerX + size, centerY + size, centerX + size / 2, centerY + size, iconPaint);
        }

        protected override void DrawShape(SKCanvas canvas)
        {
            var rect = new SKRoundRect(new SKRect(X, Y, X + Width, Y + Height), CornerRadius, CornerRadius);
            using (var fill = new SKPaint { Color = Background, Style = SKPaintStyle.Fill, IsAntialias = true })
                canvas.DrawRoundRect(rect, fill);
            using (var border = new SKPaint { Color = Stroke, Style = SKPaintStyle.Stroke, StrokeWidth = 1.25f, IsAntialias = true })
                canvas.DrawRoundRect(rect, border);
        }

        protected override void LayoutPorts()
        {
            float inAreaTop = Y + HeaderHeight + Padding;
            float inAreaBottom = Y + Height - Padding;
            PositionPortsAlongEdge(InConnectionPoints, new SKPoint(X, 0), inAreaTop, inAreaBottom, -1);
            PositionPortsAlongEdge(OutConnectionPoints, new SKPoint(X + Width, 0), inAreaTop, inAreaBottom, +1);
        }

        private void PositionPortsAlongEdge(System.Collections.Generic.List<IConnectionPoint> ports, SKPoint edge, float top, float bottom, int dir)
        {
            int n = Math.Max(ports.Count, 1);
            float span = Math.Max(bottom - top, 1f);
            for (int i = 0; i < ports.Count; i++)
            {
                var p = ports[i];
                float t = (i + 1) / (float)(n + 1);
                float cy = top + t * span;
                float cx = edge.X + dir * (PortRadius + 2);
                p.Center = new SKPoint(cx, cy);
                p.Position = p.Center;
                float r = PortRadius;
                p.Bounds = new SKRect(cx - r, cy - r, cx + r, cy + r);
                p.Rect = p.Bounds;
                p.Index = i;
                p.Component = this;
                p.IsAvailable = true;
            }
        }
    }
}

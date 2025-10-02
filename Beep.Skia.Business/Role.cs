using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a role in an organizational chart.
    /// Displayed as a badge shape.
    /// </summary>
    public class Role : BusinessControl
    {
        private string _roleName = "Role";
        public string RoleName
        {
            get => _roleName;
            set
            {
                var v = value ?? string.Empty;
                if (_roleName != v)
                {
                    _roleName = v;
                    if (NodeProperties.TryGetValue("RoleName", out var p)) p.ParameterCurrentValue = _roleName; else NodeProperties["RoleName"] = new ParameterInfo { ParameterName = "RoleName", ParameterType = typeof(string), DefaultParameterValue = _roleName, ParameterCurrentValue = _roleName, Description = "Role name" };
                    Name = _roleName;
                    InvalidateVisual();
                }
            }
        }

        public Role()
        {
            Width = 100;
            Height = 50;
            Name = "Role";
            ComponentType = BusinessComponentType.Role;
            NodeProperties["RoleName"] = new ParameterInfo { ParameterName = "RoleName", ParameterType = typeof(string), DefaultParameterValue = _roleName, ParameterCurrentValue = _roleName, Description = "Role name" };
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

            // Create badge path with notched corners
            using var path = new SKPath();
            float notchSize = 8;

            path.MoveTo(X + notchSize, Y);
            path.LineTo(X + Width - notchSize, Y);
            path.LineTo(X + Width, Y + notchSize);
            path.LineTo(X + Width, Y + Height - notchSize);
            path.LineTo(X + Width - notchSize, Y + Height);
            path.LineTo(X + notchSize, Y + Height);
            path.LineTo(X, Y + Height - notchSize);
            path.LineTo(X, Y + notchSize);
            path.Close();

            canvas.DrawPath(path, fillPaint);
            canvas.DrawPath(path, borderPaint);
        }

        protected override void DrawComponentText(SKCanvas canvas)
        {
            if (string.IsNullOrEmpty(RoleName))
                return;

            using var font = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float centerY = Y + Height / 2 + 4;

            canvas.DrawText(RoleName, centerX, centerY, SKTextAlign.Center, font, paint);
        }

        protected override void LayoutPorts()
        {
            EnsurePortCounts(1, 1);
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 6f);
        }
    }
}
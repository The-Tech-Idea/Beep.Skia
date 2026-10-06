using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;

namespace Beep.Skia.UML
{
    public class UMLLifeline : UMLControl
    {
        /// <summary>
        /// Gets or sets the object name.
        /// </summary>
        public string ObjectName
        {
            get => _objectName;
            set
            {
                if (_objectName != value)
                {
                    _objectName = value;
                    DisplayText = value;
                    if (NodeProperties.TryGetValue("ObjectName", out var pi))
                        pi.ParameterCurrentValue = _objectName ?? string.Empty;
                    InvalidateVisual();
                }
            }
        }
        private string _objectName = "Object";
        private string _className = "";
        /// <summary>
        /// Gets or sets the class name.
        /// </summary>
        public string ClassName
        {
            get => _className;
            set
            {
                if (_className == value) return;
                _className = value ?? string.Empty;
                if (NodeProperties.TryGetValue("ClassName", out var pi))
                    pi.ParameterCurrentValue = _className;
                InvalidateVisual();
            }
        }
        private float _lifelineLength = 200;
        /// <summary>
        /// Gets or sets the lifeline length.
        /// </summary>
        public float LifelineLength
        {
            get => _lifelineLength;
            set
            {
                if (System.Math.Abs(_lifelineLength - value) < 0.001f) return;
                _lifelineLength = value;
                if (NodeProperties.TryGetValue("LifelineLength", out var pi))
                    pi.ParameterCurrentValue = _lifelineLength;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Sequence object name
        /// </summary>
        public UMLLifeline()
        {
            Width = 120;
            Height = 250;
            Name = "UMLLifeline";
            DisplayText = ObjectName;
            TextPosition = TextPosition.Inside;
            ShowDisplayText = true;

            // Seed NodeProperties
            NodeProperties["ObjectName"] = new ParameterInfo
            {
                ParameterName = "ObjectName",
                ParameterType = typeof(string),
                DefaultParameterValue = _objectName,
                ParameterCurrentValue = _objectName,
                Description = "Sequence object name"
            };
            NodeProperties["ClassName"] = new ParameterInfo
            {
                ParameterName = "ClassName",
                ParameterType = typeof(string),
                DefaultParameterValue = _className,
                ParameterCurrentValue = _className,
                Description = "Optional class type for object"
            };
            NodeProperties["LifelineLength"] = new ParameterInfo
            {
                ParameterName = "LifelineLength",
                ParameterType = typeof(float),
                DefaultParameterValue = _lifelineLength,
                ParameterCurrentValue = _lifelineLength,
                Description = "Dashed lifeline length"
            };
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            const float headerHeight = 35;
            
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

            var headerRect = new SKRect(X, Y, X + Width, Y + headerHeight);
            canvas.DrawRoundRect(headerRect, 6, 6, fillPaint);
            canvas.DrawRoundRect(headerRect, 6, 6, borderPaint);

            using var lifelinePaint = new SKPaint
            {
                Color = BorderColor,
                StrokeWidth = 2.5f,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash(new float[] { 8, 4 }, 0)
            };

            float centerX = X + Width / 2;
            float lineStartY = Y + headerHeight;
            float lineEndY = Y + Height;

            canvas.DrawLine(centerX, lineStartY, centerX, lineEndY, lifelinePaint);
        }

        protected override void DrawUMLContent(SKCanvas canvas, DrawingContext context)
        {
            DrawBackground(canvas, context);
            DrawObjectName(canvas);
            DrawStereotype(canvas, context, new SKFont(Beep.Skia.TypefaceCache.Default, 10));
            DrawConnectionPoints(canvas, context);
            DrawSelection(canvas, context);
        }

        private void DrawObjectName(SKCanvas canvas)
        {
            using var font = new SKFont(Beep.Skia.TypefaceCache.Default, 12) { Embolden = true };
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float headerCenterY = Y + 22;

            string displayName = !string.IsNullOrEmpty(ClassName) 
                ? $"{ObjectName}:{ClassName}" 
                : ObjectName;

            canvas.DrawText(displayName, centerX, headerCenterY, SKTextAlign.Center, font, paint);
        }
    }
}

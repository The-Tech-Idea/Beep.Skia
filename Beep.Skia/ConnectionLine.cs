using SkiaSharp;
using System.Timers;

namespace Beep.Skia
{
    public class ConnectionLine : IConnectionLine
    {
        private readonly Action invalidateVisualAction;

        public ConnectionLine(Action invalidateVisualAction)
        {
            this.invalidateVisualAction = invalidateVisualAction;
            InitializePaint();
        }

        private void InitializePaint()
        {
            Paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };
        }
        public IConnectionPoint Start { get; set; }
        public IConnectionPoint End { get; set; }
        public SKPoint EndPoint { get; set; }
        public bool IsConnectd { get; set; }
        public bool IsSelected { get; set; }
        public bool IsAnimated { get; set; }
        public bool ShowStartArrow { get; set; } = true;
        public bool ShowEndArrow { get; set; } = true;
        public SKPaint Paint { get; set; }
        public string Label1 { get; set; }
        public string Label2 { get; set; }
        public string Label3 { get; set; }
        private const float ArrowSize = 10.0f;
        private const float LineClickThreshold = 5.0f;
        private System.Timers.Timer animationTimer;
        public event EventHandler<ConnectionArgs> StartArrowClicked;
        public event EventHandler<ConnectionArgs> EndArrowClicked;
        public event EventHandler<LineArgs> LineClicked;

        public SKColor LineColor { get; set; } = SKColors.Black;

        public ConnectionLine(IConnectionPoint start, IConnectionPoint end, Action invalidateVisualAction)
        {
            Start = start;
            End = end;
            this.invalidateVisualAction = invalidateVisualAction;
            InitializePaint();
        }
        public ConnectionLine(IConnectionPoint start, SKPoint end, Action invalidateVisualAction)
        {
            Start = start;
            EndPoint = end;
            this.invalidateVisualAction = invalidateVisualAction;
            InitializePaint();
        }
        public void Draw(SKCanvas canvas)
        {
            // Calculate line coordinates and draw the line
            var lineStart = Start.Position;
            var lineEnd = End.Position;
            // Calculate line coordinates and draw the line
            Paint.Color = LineColor;
            canvas.DrawLine(lineStart, lineEnd, Paint);

            // Draw optional arrows
            if (ShowStartArrow)
            {
                DrawArrow(canvas, Paint, lineStart, lineEnd);
            }

            if (ShowEndArrow)
            {
                DrawArrow(canvas, Paint, lineEnd, lineStart);
            }

            // Draw optional labels
            var textPaint = new SKPaint { IsAntialias = true, TextSize = 14 };
            if (!string.IsNullOrEmpty(Label1))
            {
                // Draw Label1 above the first arrow
                var labelPosition = lineStart + new SKPoint(-textPaint.MeasureText(Label1) / 2, -15);
                canvas.DrawText(Label1, labelPosition, textPaint);
            }

            if (!string.IsNullOrEmpty(Label2))
            {
                // Draw Label2 above the second arrow
                var labelPosition = lineEnd + new SKPoint(-textPaint.MeasureText(Label2) / 2, -15);
                canvas.DrawText(Label2, labelPosition, textPaint);
            }

            if (!string.IsNullOrEmpty(Label3))
            {
                // Draw Label3 in the middle of the line
                var midpoint = new SKPoint((lineStart.X + lineEnd.X) / 2, (lineStart.Y + lineEnd.Y) / 2);
                var labelPosition = midpoint + new SKPoint(-textPaint.MeasureText(Label3) / 2, -10);
                canvas.DrawText(Label3, labelPosition, textPaint);
            }

        }

        private void DrawArrow(SKCanvas canvas, SKPaint paint, SKPoint arrowPosition, SKPoint oppositePoint)
        {
            var angle = (float)Math.Atan2(arrowPosition.Y - oppositePoint.Y, arrowPosition.X - oppositePoint.X);
            var arrowEndPoint1 = new SKPoint(
                arrowPosition.X - ArrowSize * (float)Math.Cos(angle + Math.PI / 6),
                arrowPosition.Y - ArrowSize * (float)Math.Sin(angle + Math.PI / 6));
            var arrowEndPoint2 = new SKPoint(
                arrowPosition.X - ArrowSize * (float)Math.Cos(angle - Math.PI / 6),
                         arrowPosition.Y - ArrowSize * (float)Math.Sin(angle - Math.PI / 6));
            canvas.DrawLine(arrowPosition, arrowEndPoint1, paint);
            canvas.DrawLine(arrowPosition, arrowEndPoint2, paint);
        }

        public bool LineContainsPoint(SKPoint point)
        {
            // Check if the distance from the point to the line is within the click threshold
            var lineVector = End.Position - Start.Position;
            var pointVector = point - Start.Position;
            var lineLength = (float)Math.Sqrt(lineVector.X * lineVector.X + lineVector.Y * lineVector.Y);
            var dotProduct = lineVector.X * pointVector.X + lineVector.Y * pointVector.Y;
            var projectionLength = dotProduct / lineLength;
            if (projectionLength < 0 || projectionLength > lineLength)
            {
                return false;
            }
            var projectionPoint = Start.Position + new SKPoint(lineVector.X * projectionLength / lineLength, lineVector.Y * projectionLength / lineLength);
            return (float)Math.Sqrt(Math.Pow(point.X - projectionPoint.X, 2) + Math.Pow(point.Y - projectionPoint.Y, 2)) <= LineClickThreshold;
        }

        public bool ArrowContainsPoint(SKPoint point, bool startArrow)
        {
            // Check if the point is within the bounding box of the arrow
            if (startArrow && ShowStartArrow)
            {
                var arrowBoundingBox = CalculateArrowBoundingBox(Start.Position, End.Position);
                return arrowBoundingBox.Contains(point);
            }
            else if (!startArrow && ShowEndArrow)
            {
                var arrowBoundingBox = CalculateArrowBoundingBox(End.Position, Start.Position);
                return arrowBoundingBox.Contains(point);
            }
            return false;
        }
        private SKRect CalculateArrowBoundingBox(SKPoint arrowPosition, SKPoint oppositePoint)
        {
            var angle = (float)Math.Atan2(arrowPosition.Y - oppositePoint.Y, arrowPosition.X - oppositePoint.X);
            var arrowEndPoint1 = new SKPoint(
                arrowPosition.X - ArrowSize * (float)Math.Cos(angle + Math.PI / 6),
                arrowPosition.Y - ArrowSize * (float)Math.Sin(angle + Math.PI / 6));
            var arrowEndPoint2 = new SKPoint(
                arrowPosition.X - ArrowSize * (float)Math.Cos(angle - Math.PI / 6),
                arrowPosition.Y - ArrowSize * (float)Math.Sin(angle - Math.PI / 6));
            var left = Math.Min(arrowPosition.X, Math.Min(arrowEndPoint1.X, arrowEndPoint2.X));
            var top = Math.Min(arrowPosition.Y, Math.Min(arrowEndPoint1.Y, arrowEndPoint2.Y));
            var right = Math.Max(arrowPosition.X, Math.Max(arrowEndPoint1.X, arrowEndPoint2.X));
            var bottom = Math.Max(arrowPosition.Y, Math.Max(arrowEndPoint1.Y, arrowEndPoint2.Y));
            return new SKRect(left, top, right, bottom);
        }
        // Click detection method
        public void OnClick(SKPoint clickPoint)
        {
            if (ArrowContainsPoint(clickPoint, true))
            {
                StartArrowClicked?.Invoke(this, new ConnectionArgs(Start));
                // Handle click on start arrow
            }
            else if (ArrowContainsPoint(clickPoint, false))
            {
                EndArrowClicked?.Invoke(this, new ConnectionArgs(End));
                // Handle click on end arrow
            }
            else if (LineContainsPoint(clickPoint))
            {
                LineClicked?.Invoke(this, new LineArgs(Start, End, this));
                // Handle click on the line
            }
        }

        // Animation method
        public void StartAnimation()
        {
            if (IsAnimated)
            {
                animationTimer = new System.Timers.Timer(16); // Approximately 60 frames per second
                animationTimer.Elapsed += Animate;
                animationTimer.AutoReset = true;
                animationTimer.Start();
            }
        }
        public void StopAnimation()
        {
            animationTimer?.Stop();
        }
        private void Animate(object sender, ElapsedEventArgs e)
        {
            if (IsAnimated)
            {
                // Update the LineColor property based on the elapsed time
                var (hue, saturation, luminosity) = LineColor.ToHsl();
                hue = (hue + 1) % 360;
                LineColor = SkiaUtil.FromHsl(hue, saturation, luminosity);
                // Trigger a redraw of the scene
                invalidateVisualAction?.Invoke();
                // Trigger a redraw of the scene
                // This depends on your specific UI framework, e.g., InvalidateSurface() for SkiaSharp.Views.Forms.SKCanvasView
            }
        }

    }
}
using SkiaSharp;
using System.Timers;
using Beep.Skia.Model;
namespace Beep.Skia
{
    /// <summary>
    /// Represents a visual connection line between two connection points that can be drawn on a Skia canvas.
    /// This class implements the IConnectionLine interface and provides functionality for drawing lines,
    /// arrows, labels, animations, and user interaction handling.
    /// </summary>
    public class ConnectionLine : IConnectionLine, IDisposable
    {
        private readonly Action invalidateVisualAction;
        private SKPaint _textPaint;
        private SKFont _textFont;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionLine"/> class with an invalidate visual action.
        /// </summary>
        /// <param name="invalidateVisualAction">The action to call when the visual needs to be invalidated.</param>
        public ConnectionLine(Action invalidateVisualAction)
        {
            this.invalidateVisualAction = invalidateVisualAction;
            InitializePaint();
        }

        /// <summary>
        /// Initializes paint objects used for drawing the connection line and text.
        /// </summary>
        private void InitializePaint()
        {
            Paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };
            _textPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = SKColors.Black
            };
            _textFont = new SKFont
            {
                Size = 14
            };
        }

        /// <summary>
        /// Gets or sets the starting connection point of this line.
        /// </summary>
        public IConnectionPoint Start { get; set; }

        /// <summary>
        /// Gets or sets the ending connection point of this line.
        /// </summary>
        public IConnectionPoint End { get; set; }

        /// <summary>
        /// Gets or sets the end point coordinates when not connected to an end connection point.
        /// </summary>
        public SKPoint EndPoint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this connection line is connected.
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this connection line is selected.
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this connection line should be animated.
        /// </summary>
        public bool IsAnimated { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether data flow animation is enabled.
        /// </summary>
        public bool IsDataFlowAnimated { get; set; }

        /// <summary>
        /// Gets or sets the speed of the data flow animation (pixels per second).
        /// </summary>
        public float DataFlowSpeed { get; set; } = 100f;

        /// <summary>
        /// Gets or sets the size of the data flow particles.
        /// </summary>
        public float DataFlowParticleSize { get; set; } = 4f;

        /// <summary>
        /// Gets or sets the color of the data flow particles.
        /// </summary>
        public SKColor DataFlowColor { get; set; } = SKColors.Blue;

        /// <summary>
        /// Gets or sets a value indicating whether to show an arrow at the start of the line.
        /// </summary>
        public bool ShowStartArrow { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show an arrow at the end of the line.
        /// </summary>
        public bool ShowEndArrow { get; set; } = true;

        /// <summary>
        /// Gets or sets the paint object used for drawing the connection line.
        /// </summary>
        public SKPaint Paint { get; set; }

        /// <summary>
        /// Gets or sets the first label text to display on the connection line.
        /// </summary>
        public string Label1 { get; set; }

        /// <summary>
        /// Gets or sets the second label text to display on the connection line.
        /// </summary>
        public string Label2 { get; set; }

        /// <summary>
        /// Gets or sets the third label text to display on the connection line.
        /// </summary>
        public string Label3 { get; set; }

        /// <summary>
        /// Gets or sets the data type label to display on the connection line.
        /// If not set, will automatically use the data type from the start connection point.
        /// </summary>
        public string DataTypeLabel { get; set; }

        /// <summary>
        /// The size of the arrow in pixels.
        /// </summary>
        private const float ArrowSize = 10.0f;

        /// <summary>
        /// The threshold distance for detecting line clicks.
        /// </summary>
        private const float LineClickThreshold = 5.0f;

        private System.Timers.Timer animationTimer;

        /// <summary>
        /// Animation state for data flow particles.
        /// </summary>
        private float dataFlowOffset = 0f;
        private float dataFlowSpacing = 30f; // Distance between particles

        /// <summary>
        /// Occurs when the start arrow of the connection line is clicked.
        /// </summary>
        public event EventHandler<ConnectionArgs> StartArrowClicked;

        /// <summary>
        /// Occurs when the end arrow of the connection line is clicked.
        /// </summary>
        public event EventHandler<ConnectionArgs> EndArrowClicked;

        /// <summary>
        /// Occurs when the connection line itself is clicked.
        /// </summary>
        public event EventHandler<LineArgs> LineClicked;

        /// <summary>
        /// Gets or sets the color of the connection line.
        /// </summary>
        public SKColor LineColor { get; set; } = SKColors.Black;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionLine"/> class with start and end connection points.
        /// </summary>
        /// <param name="start">The starting connection point.</param>
        /// <param name="end">The ending connection point.</param>
        /// <param name="invalidateVisualAction">The action to call when the visual needs to be invalidated.</param>
        /// <exception cref="ArgumentNullException">Thrown when start, end, or invalidateVisualAction is null.</exception>
        public ConnectionLine(IConnectionPoint start, IConnectionPoint end, Action invalidateVisualAction)
        {
            if (start == null)
                throw new ArgumentNullException(nameof(start), "Start connection point cannot be null.");
            if (end == null)
                throw new ArgumentNullException(nameof(end), "End connection point cannot be null.");
            if (invalidateVisualAction == null)
                throw new ArgumentNullException(nameof(invalidateVisualAction), "Invalidate visual action cannot be null.");

            Start = start;
            End = end;
            this.invalidateVisualAction = invalidateVisualAction;
            InitializePaint();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionLine"/> class with a start connection point and end coordinates.
        /// </summary>
        /// <param name="start">The starting connection point.</param>
        /// <param name="end">The ending point coordinates.</param>
        /// <param name="invalidateVisualAction">The action to call when the visual needs to be invalidated.</param>
        /// <exception cref="ArgumentNullException">Thrown when start or invalidateVisualAction is null.</exception>
        public ConnectionLine(IConnectionPoint start, SKPoint end, Action invalidateVisualAction)
        {
            if (start == null)
                throw new ArgumentNullException(nameof(start), "Start connection point cannot be null.");
            if (invalidateVisualAction == null)
                throw new ArgumentNullException(nameof(invalidateVisualAction), "Invalidate visual action cannot be null.");

            Start = start;
            EndPoint = end;
            this.invalidateVisualAction = invalidateVisualAction;
            InitializePaint();
        }

        /// <summary>
        /// Draws the connection line on the specified Skia canvas.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
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
            if (!string.IsNullOrEmpty(Label1))
            {
                // Draw Label1 above the first arrow
                var labelPosition = lineStart + new SKPoint(-_textFont.MeasureText(Label1) / 2, -15);
                canvas.DrawText(Label1, labelPosition.X, labelPosition.Y, SKTextAlign.Center, _textFont, _textPaint);
            }

            if (!string.IsNullOrEmpty(Label2))
            {
                // Draw Label2 above the second arrow
                var labelPosition = lineEnd + new SKPoint(-_textFont.MeasureText(Label2) / 2, -15);
                canvas.DrawText(Label2, labelPosition.X, labelPosition.Y, SKTextAlign.Center, _textFont, _textPaint);
            }

            if (!string.IsNullOrEmpty(Label3))
            {
                // Draw Label3 in the middle of the line
                var midpoint = new SKPoint((lineStart.X + lineEnd.X) / 2, (lineStart.Y + lineEnd.Y) / 2);
                var labelPosition = midpoint + new SKPoint(-_textFont.MeasureText(Label3) / 2, -10);
                canvas.DrawText(Label3, labelPosition.X, labelPosition.Y, SKTextAlign.Center, _textFont, _textPaint);
            }

            // Draw data type label
            string dataTypeText = DataTypeLabel ?? Start?.DataType;
            if (!string.IsNullOrEmpty(dataTypeText))
            {
                // Draw data type label below the line in the middle
                var midpoint = new SKPoint((lineStart.X + lineEnd.X) / 2, (lineStart.Y + lineEnd.Y) / 2);
                var labelPosition = midpoint + new SKPoint(-_textFont.MeasureText(dataTypeText) / 2, 20);
                canvas.DrawText(dataTypeText, labelPosition.X, labelPosition.Y, SKTextAlign.Center, _textFont, _textPaint);
            }

            // Draw data flow animation particles
            if (IsDataFlowAnimated && Start != null && End != null)
            {
                DrawDataFlowParticles(canvas, lineStart, lineEnd);
            }

        }

        /// <summary>
        /// Draws an arrow on the canvas at the specified position.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="paint">The paint to use for drawing.</param>
        /// <param name="arrowPosition">The position of the arrow.</param>
        /// <param name="oppositePoint">The opposite point to calculate the arrow direction.</param>
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

        /// <summary>
        /// Draws animated data flow particles along the connection line.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="lineStart">The start point of the line.</param>
        /// <param name="lineEnd">The end point of the line.</param>
        private void DrawDataFlowParticles(SKCanvas canvas, SKPoint lineStart, SKPoint lineEnd)
        {
            // Calculate line direction and length
            var direction = lineEnd - lineStart;
            var length = (float)Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);

            if (length == 0) return;

            // Normalize direction
            direction = new SKPoint(direction.X / length, direction.Y / length);

            // Create paint for particles
            using var particlePaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = DataFlowColor,
                IsAntialias = true
            };

            // Draw multiple particles along the line
            float currentOffset = -dataFlowOffset;
            while (currentOffset < length)
            {
                if (currentOffset >= 0)
                {
                    // Calculate particle position
                    var particlePos = new SKPoint(
                        lineStart.X + direction.X * currentOffset,
                        lineStart.Y + direction.Y * currentOffset
                    );

                    // Draw particle as a circle
                    canvas.DrawCircle(particlePos, DataFlowParticleSize, particlePaint);

                    // Add a subtle glow effect
                    using var glowPaint = new SKPaint
                    {
                        Style = SKPaintStyle.Fill,
                        Color = DataFlowColor.WithAlpha(100),
                        IsAntialias = true
                    };
                    canvas.DrawCircle(particlePos, DataFlowParticleSize * 2, glowPaint);
                }

                currentOffset += dataFlowSpacing;
            }
        }

        /// <summary>
        /// Determines whether the specified point lies on the connection line within the click threshold.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>true if the point is on the line; otherwise, false.</returns>
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

        /// <summary>
        /// Determines whether the specified point lies within an arrow of the connection line.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <param name="startArrow">true to test the start arrow; false to test the end arrow.</param>
        /// <returns>true if the point is within the arrow; otherwise, false.</returns>
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

        /// <summary>
        /// Calculates the bounding box of an arrow for hit testing purposes.
        /// </summary>
        /// <param name="arrowPosition">The position of the arrow.</param>
        /// <param name="oppositePoint">The opposite point to calculate the arrow direction.</param>
        /// <returns>The bounding rectangle of the arrow.</returns>
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

        /// <summary>
        /// Handles click events on the connection line, determining whether an arrow or the line itself was clicked.
        /// </summary>
        /// <param name="clickPoint">The point where the click occurred.</param>
        public void OnClick(SKPoint clickPoint)
        {
            try
            {
                if (ArrowContainsPoint(clickPoint, true))
                {
                    StartArrowClicked?.Invoke(this, new ConnectionArgs(Start));
                }
                else if (ArrowContainsPoint(clickPoint, false))
                {
                    EndArrowClicked?.Invoke(this, new ConnectionArgs(End));
                }
                else if (LineContainsPoint(clickPoint))
                {
                    LineClicked?.Invoke(this, new LineArgs(Start, End, this));
                }
            }
            catch (Exception)
            {
                // Log error or handle appropriately
                // For now, we'll silently handle to prevent crashes
                invalidateVisualAction?.Invoke();
            }
        }

        /// <summary>
        /// Starts the animation of the connection line if it is set to be animated.
        /// </summary>
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

        /// <summary>
        /// Stops the animation of the connection line.
        /// </summary>
        public void StopAnimation()
        {
            animationTimer?.Stop();
        }

        /// <summary>
        /// Animates the connection line by changing its color over time.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void Animate(object sender, ElapsedEventArgs e)
        {
            bool needsRedraw = false;

            if (IsAnimated)
            {
                // Update the LineColor property based on the elapsed time
                var (hue, saturation, luminosity) = LineColor.ToHsl();
                hue = (hue + 1) % 360;
                LineColor = SkiaUtil.FromHsl(hue, saturation, luminosity);
                needsRedraw = true;
            }

            if (IsDataFlowAnimated)
            {
                // Update data flow animation
                dataFlowOffset += DataFlowSpeed * 0.016f; // 16ms per frame approximately

                // Reset offset when it exceeds the spacing
                if (dataFlowOffset > dataFlowSpacing)
                {
                    dataFlowOffset = 0f;
                }

                needsRedraw = true;
            }

            if (needsRedraw)
            {
                // Trigger a redraw of the scene
                invalidateVisualAction?.Invoke();
            }
        }

        /// <summary>
        /// Releases all resources used by the connection line.
        /// </summary>
        public void Dispose()
        {
            Paint?.Dispose();
            _textPaint?.Dispose();
            animationTimer?.Dispose();
        }
    }
}
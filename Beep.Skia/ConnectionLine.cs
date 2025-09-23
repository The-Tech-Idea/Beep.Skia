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
    /// Size of arrowheads in pixels.
    /// </summary>
    public float ArrowSize { get; set; } = 10.0f;

    /// <summary>
    /// Optional dash pattern (pairs of on/off lengths). Null/empty for solid.
    /// </summary>
    public float[] DashPattern { get; set; }

    /// <summary>
    /// Routing mode for how the line is drawn.
    /// </summary>
    public LineRoutingMode RoutingMode { get; set; } = LineRoutingMode.Straight;

    /// <summary>
    /// Data flow animation direction.
    /// </summary>
    public DataFlowDirection FlowDirection { get; set; } = DataFlowDirection.None;

    public LabelPlacement Label1Placement { get; set; } = LabelPlacement.Above;
    public LabelPlacement Label2Placement { get; set; } = LabelPlacement.Above;
    public LabelPlacement Label3Placement { get; set; } = LabelPlacement.Over;
    public LabelPlacement DataLabelPlacement { get; set; } = LabelPlacement.Below;

    public bool ShowStatusIndicator { get; set; } = false;
    public LineStatus Status { get; set; } = LineStatus.None;
    public SKColor StatusColor { get; set; } = SKColors.DimGray;

    // ERD multiplicity markers
    public ERDMultiplicity StartMultiplicity { get; set; } = ERDMultiplicity.Unspecified;
    public ERDMultiplicity EndMultiplicity { get; set; } = ERDMultiplicity.Unspecified;

    // Optional styling for ERD crow's foot markers
    // Spread angle in degrees between the center prong and each outer prong
    public float CrowFootSpreadDegrees { get; set; } = 18f; // ~PI/10
    // Length scale (multiplier on unit) for the crow's foot prongs
    public float CrowFootLength { get; set; } = 1.0f;

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
            Paint.Color = LineColor;

            // Configure dash pattern if provided
            using var stroke = new SKPaint
            {
                Color = Paint.Color,
                Style = Paint.Style,
                StrokeWidth = Paint.StrokeWidth,
                IsAntialias = Paint.IsAntialias,
                StrokeCap = Paint.StrokeCap,
                StrokeJoin = Paint.StrokeJoin
            };
            if (DashPattern != null && DashPattern.Length >= 2)
            {
                stroke.PathEffect = SKPathEffect.CreateDash(DashPattern, 0);
            }

            switch (RoutingMode)
            {
                case LineRoutingMode.Orthogonal:
                {
                    float midX = (lineStart.X + lineEnd.X) * 0.5f;
                    var p2 = new SKPoint(midX, lineStart.Y);
                    var p3 = new SKPoint(midX, lineEnd.Y);
                    canvas.DrawLine(lineStart, p2, stroke);
                    canvas.DrawLine(p2, p3, stroke);
                    canvas.DrawLine(p3, lineEnd, stroke);
                    break;
                }
                case LineRoutingMode.Curved:
                {
                    using var path = new SKPath();
                    path.MoveTo(lineStart);
                    var c1 = new SKPoint((lineStart.X * 2 + lineEnd.X) / 3f, lineStart.Y);
                    var c2 = new SKPoint((lineEnd.X * 2 + lineStart.X) / 3f, lineEnd.Y);
                    path.CubicTo(c1, c2, lineEnd);
                    canvas.DrawPath(path, stroke);
                    break;
                }
                default:
                    canvas.DrawLine(lineStart, lineEnd, stroke);
                    break;
            }

            // Draw ERD multiplicity markers or arrows
            if (StartMultiplicity != ERDMultiplicity.Unspecified)
                DrawErdMultiplicity(canvas, stroke, atPoint: lineStart, toward: lineEnd, StartMultiplicity);
            else if (ShowStartArrow)
                DrawArrow(canvas, Paint, lineStart, lineEnd);

            if (EndMultiplicity != ERDMultiplicity.Unspecified)
                DrawErdMultiplicity(canvas, stroke, atPoint: lineEnd, toward: lineStart, EndMultiplicity);
            else if (ShowEndArrow)
                DrawArrow(canvas, Paint, lineEnd, lineStart);

            // Draw optional labels
            if (!string.IsNullOrEmpty(Label1))
            {
                var labelPosition = GetLabelPosition(lineStart, lineEnd, Label1Placement, atStart: true, text: Label1);
                canvas.DrawText(Label1, labelPosition.X, labelPosition.Y, SKTextAlign.Center, _textFont, _textPaint);
            }

            if (!string.IsNullOrEmpty(Label2))
            {
                var labelPosition = GetLabelPosition(lineStart, lineEnd, Label2Placement, atStart: false, text: Label2);
                canvas.DrawText(Label2, labelPosition.X, labelPosition.Y, SKTextAlign.Center, _textFont, _textPaint);
            }

            if (!string.IsNullOrEmpty(Label3))
            {
                var labelPosition = GetMidLabelPosition(lineStart, lineEnd, Label3Placement, Label3);
                canvas.DrawText(Label3, labelPosition.X, labelPosition.Y, SKTextAlign.Center, _textFont, _textPaint);
            }

            // Draw data type label
            string dataTypeText = DataTypeLabel ?? Start?.DataType;
            if (!string.IsNullOrEmpty(dataTypeText))
            {
                var labelPosition = GetMidLabelPosition(lineStart, lineEnd, DataLabelPlacement, dataTypeText);
                canvas.DrawText(dataTypeText, labelPosition.X, labelPosition.Y, SKTextAlign.Center, _textFont, _textPaint);
            }

            // Draw data flow animation particles
            if (IsDataFlowAnimated && Start != null && End != null && FlowDirection != DataFlowDirection.None)
            {
                DrawDataFlowParticles(canvas, lineStart, lineEnd, FlowDirection);
            }

            // Status indicator
            if (ShowStatusIndicator && Status != LineStatus.None)
            {
                DrawStatusIndicator(canvas, lineStart, lineEnd);
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
        /// Draw ERD multiplicity marks at a line end: combinations of small circle, single/double bar, and crow's foot.
        /// Orientation is computed from the vector atPoint -> toward so symbols sit outside the endpoint.
        /// </summary>
        private void DrawErdMultiplicity(SKCanvas canvas, SKPaint paint, SKPoint atPoint, SKPoint toward, ERDMultiplicity mult)
        {
            // Geometry parameters (relative to ArrowSize/StrokeWidth)
            float unit = Math.Max(6f, ArrowSize * 0.75f);
            float barLen = unit * 1.2f;
            float gap = Math.Max(2f, paint.StrokeWidth);
            float circleR = unit * 0.45f;
            float footLen = unit * Math.Max(0.1f, CrowFootLength);
            float footSpread = (float)(Math.PI * CrowFootSpreadDegrees / 180.0); // angle offset for the outer prongs

            // Direction from end outward
            var vx = atPoint.X - toward.X;
            var vy = atPoint.Y - toward.Y;
            float len = (float)Math.Sqrt(vx * vx + vy * vy);
            if (len < 0.001f) len = 0.001f;
            vx /= len; vy /= len; // normalized outward

            // Build a local right vector for perpendiculars
            var rx = -vy; var ry = vx;

            // Helper to shift a point along v or r
            SKPoint Along(SKPoint p, float s) => new SKPoint(p.X + vx * s, p.Y + vy * s);
            SKPoint Right(SKPoint p, float s) => new SKPoint(p.X + rx * s, p.Y + ry * s);

            // Start a little off the endpoint so symbols don’t overlap the node outline
            var origin = Along(atPoint, gap + paint.StrokeWidth);

            // Rendering order: near endpoint to far (so circle is closest, then bars, then foot at farthest)
            bool needsCircle = mult == ERDMultiplicity.ZeroOrOne || mult == ERDMultiplicity.ZeroOrMany;
            bool needsBar = mult == ERDMultiplicity.ZeroOrOne || mult == ERDMultiplicity.OneOnly || mult == ERDMultiplicity.OneOrMany || mult == ERDMultiplicity.One;
            bool needsDoubleBar = mult == ERDMultiplicity.OneOnly; // exactly one often shown as double bar
            bool needsFoot = mult == ERDMultiplicity.OneOrMany || mult == ERDMultiplicity.ZeroOrMany || mult == ERDMultiplicity.Many;

            using var symbolPaint = new SKPaint
            {
                Color = paint.Color,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = paint.StrokeWidth,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Butt
            };

            float cursor = 0f;
            if (needsCircle)
            {
                var c = Along(origin, cursor + circleR);
                canvas.DrawCircle(c, circleR, symbolPaint);
                cursor += circleR * 2 + gap;
            }

            if (needsBar)
            {
                var p1 = Right(Along(origin, cursor), -barLen / 2);
                var p2 = Right(Along(origin, cursor), +barLen / 2);
                canvas.DrawLine(p1, p2, symbolPaint);
                cursor += gap * 1.2f;
            }

            if (needsDoubleBar)
            {
                var p1 = Right(Along(origin, cursor), -barLen / 2);
                var p2 = Right(Along(origin, cursor), +barLen / 2);
                canvas.DrawLine(p1, p2, symbolPaint);
                cursor += gap * 1.2f;
            }

            if (needsFoot)
            {
                // Crow’s foot with three prongs from a base point
                var baseP = Along(origin, cursor);
                // central prong straight out
                var mid = Along(baseP, footLen);
                canvas.DrawLine(baseP, mid, symbolPaint);

                // two outer prongs at small angles
                float ang = (float)Math.Atan2(vy, vx);
                var v1 = new SKPoint((float)Math.Cos(ang + footSpread), (float)Math.Sin(ang + footSpread));
                var v2 = new SKPoint((float)Math.Cos(ang - footSpread), (float)Math.Sin(ang - footSpread));
                var pOut1 = new SKPoint(baseP.X + v1.X * footLen, baseP.Y + v1.Y * footLen);
                var pOut2 = new SKPoint(baseP.X + v2.X * footLen, baseP.Y + v2.Y * footLen);
                canvas.DrawLine(baseP, pOut1, symbolPaint);
                canvas.DrawLine(baseP, pOut2, symbolPaint);
            }
        }

        /// <summary>
        /// Draws animated data flow particles along the connection line.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="lineStart">The start point of the line.</param>
        /// <param name="lineEnd">The end point of the line.</param>
        private void DrawDataFlowParticles(SKCanvas canvas, SKPoint lineStart, SKPoint lineEnd, DataFlowDirection directionMode)
        {
            // Calculate line direction and length
            var direction = lineEnd - lineStart;
            var length = (float)Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);

            if (length == 0) return;

            // Normalize direction
            direction = new SKPoint(direction.X / length, direction.Y / length);

            // Possibly reverse or draw both directions
            bool drawForward = directionMode == DataFlowDirection.Forward || directionMode == DataFlowDirection.Bidirectional;
            bool drawBackward = directionMode == DataFlowDirection.Backward || directionMode == DataFlowDirection.Bidirectional;

            // Create paint for particles
            using var particlePaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = DataFlowColor,
                IsAntialias = true
            };

            // Draw multiple particles along the line
            void drawAlong(SKPoint s, SKPoint e, bool reverse)
            {
                var dir = reverse ? new SKPoint(-direction.X, -direction.Y) : direction;
                float currentOffset = -dataFlowOffset;
                while (currentOffset < length)
                {
                    if (currentOffset >= 0)
                    {
                        var particlePos = new SKPoint(
                            s.X + dir.X * currentOffset,
                            s.Y + dir.Y * currentOffset
                        );
                        canvas.DrawCircle(particlePos, DataFlowParticleSize, particlePaint);
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

            if (drawForward) drawAlong(lineStart, lineEnd, reverse: false);
            if (drawBackward) drawAlong(lineEnd, lineStart, reverse: true);
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

            if (ShowStatusIndicator && Status is LineStatus.Working or LineStatus.Running)
            {
                // Rotate spinner implicitly by advancing a phase via color or offset
                // We can reuse dataFlowOffset to animate spinner rotation
                needsRedraw = true;
            }

            if (needsRedraw)
            {
                // Trigger a redraw of the scene
                invalidateVisualAction?.Invoke();
            }
        }

        private SKPoint GetLabelPosition(SKPoint start, SKPoint end, LabelPlacement placement, bool atStart, string text)
        {
            var pos = atStart ? start : end;
            float yOffset = placement switch
            {
                LabelPlacement.Above => -15,
                LabelPlacement.Below => 20,
                _ => -5,
            };
            return pos + new SKPoint(-_textFont.MeasureText(text) / 2, yOffset);
        }

        private SKPoint GetMidLabelPosition(SKPoint start, SKPoint end, LabelPlacement placement, string text)
        {
            var mid = new SKPoint((start.X + end.X) / 2, (start.Y + end.Y) / 2);
            float yOffset = placement switch
            {
                LabelPlacement.Above => -10,
                LabelPlacement.Below => 20,
                _ => 4,
            };
            return mid + new SKPoint(-_textFont.MeasureText(text) / 2, yOffset);
        }

        private void DrawStatusIndicator(SKCanvas canvas, SKPoint start, SKPoint end)
        {
            var mid = new SKPoint((start.X + end.X) / 2, (start.Y + end.Y) / 2);
            using var paint = new SKPaint { IsAntialias = true, Color = StatusColor, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            float r = 8f;

            if (Status is LineStatus.Working or LineStatus.Running)
            {
                // Simple spinner arc that appears to rotate as Animate updates
                float sweep = 270f;
                float angle = (dataFlowOffset * 360f / Math.Max(1f, dataFlowSpacing)) % 360f;
                using var path = new SKPath();
                var rect = new SKRect(mid.X - r, mid.Y - r, mid.X + r, mid.Y + r);
                path.AddArc(rect, angle, sweep);
                canvas.DrawPath(path, paint);
            }
            else if (Status == LineStatus.Warning)
            {
                canvas.DrawCircle(mid, r, paint);
                using var fill = new SKPaint { IsAntialias = true, Color = StatusColor.WithAlpha(60), Style = SKPaintStyle.Fill };
                canvas.DrawCircle(mid, r - 2, fill);
            }
            else if (Status == LineStatus.Error)
            {
                canvas.DrawCircle(mid, r, paint);
                using var cross = new SKPaint { IsAntialias = true, Color = StatusColor, StrokeWidth = 2, Style = SKPaintStyle.Stroke };
                canvas.DrawLine(mid.X - r / 2, mid.Y - r / 2, mid.X + r / 2, mid.Y + r / 2, cross);
                canvas.DrawLine(mid.X - r / 2, mid.Y + r / 2, mid.X + r / 2, mid.Y - r / 2, cross);
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
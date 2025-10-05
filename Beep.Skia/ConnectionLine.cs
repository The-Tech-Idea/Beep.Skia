using SkiaSharp;
using System.Timers;
using Beep.Skia.Model;
using System.Collections.Generic;
using System;
using System.Linq;
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
    /// Indicates the line is currently hovered by the mouse (set by interaction layer).
    /// </summary>
    public bool IsHovered { get; set; }

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

    // --- Schema/row-level semantics ---
    /// <summary>
    /// Optional source row/column identifier when connecting ERD rows or similar row-based ports.
    /// </summary>
    public System.Guid? SourceRowId { get; set; }

    /// <summary>
    /// Optional target row/column identifier when connecting ERD rows or similar row-based ports.
    /// </summary>
    public System.Guid? TargetRowId { get; set; }

    /// <summary>
    /// Optional JSON payload for schema flowing across this line (e.g., ETL OutputSchema).
    /// </summary>
    public string SchemaJson { get; set; }

    /// <summary>
    /// Optional JSON payload for an expected schema used to validate the actual SchemaJson (e.g., ETLTarget.ExpectedSchema).
    /// When provided, tooltip will show concrete differences on hover.
    /// </summary>
    public string ExpectedSchemaJson { get; set; }

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

    /// <summary>
    /// Animation style for data flow visualization (infographic style).
    /// </summary>
    public FlowAnimationStyle AnimationStyle { get; set; } = FlowAnimationStyle.Dots;

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
            if (Start == null && End == null)
            {
                // Nothing to draw
                return;
            }

            // Prefer actual connection points; fall back to EndPoint when End is not connected (preview/drag)
            var lineStart = Start != null ? Start.Position : EndPoint;
            var lineEnd = (End != null) ? End.Position : EndPoint;
            Paint.Color = LineColor;

            // Determine if this is an identifying relationship (check endpoints)
            bool isIdentifying = true; // default to solid (identifying)
            try
            {
                // Check if either endpoint component has IsIdentifying NodeProperty set to false
                var startComp = Start?.Component as SkiaComponent;
                var endComp = End?.Component as SkiaComponent;
                if (startComp?.NodeProperties != null && startComp.NodeProperties.TryGetValue("IsIdentifying", out var p1) && p1?.ParameterCurrentValue is bool b1)
                    isIdentifying = b1;
                else if (endComp?.NodeProperties != null && endComp.NodeProperties.TryGetValue("IsIdentifying", out var p2) && p2?.ParameterCurrentValue is bool b2)
                    isIdentifying = b2;
            }
            catch { }

            // Configure dash pattern if provided OR if non-identifying ERD relationship
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
            else if (!isIdentifying && (Start?.Component?.GetType()?.Namespace == "Beep.Skia.ERD" || End?.Component?.GetType()?.Namespace == "Beep.Skia.ERD"))
            {
                // Non-identifying ERD relationships: use dashed line
                stroke.PathEffect = SKPathEffect.CreateDash(new float[] { 8, 4 }, 0);
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
            if (Start != null && StartMultiplicity != ERDMultiplicity.Unspecified)
                DrawErdMultiplicity(canvas, stroke, atPoint: lineStart, toward: lineEnd, StartMultiplicity);
            else if (ShowStartArrow && Start != null)
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
            if (IsDataFlowAnimated && FlowDirection != DataFlowDirection.None)
            {
                DrawDataFlowParticles(canvas, lineStart, lineEnd, FlowDirection);
            }

            // Status indicator
            if (ShowStatusIndicator && Status != LineStatus.None)
            {
                DrawStatusIndicator(canvas, lineStart, lineEnd);
            }

            // Schema-derived label and tooltip
            TryDrawSchemaLabelAndTooltip(canvas, lineStart, lineEnd);
            // Hover badge (small "i") to hint details availability
            TryDrawInfoBadge(canvas, lineStart, lineEnd);

        }

        private void TryDrawSchemaLabelAndTooltip(SKCanvas canvas, SKPoint lineStart, SKPoint lineEnd)
        {
            if (string.IsNullOrWhiteSpace(SchemaJson)) return;
            List<ColumnDefinition> cols = null;
            try { cols = System.Text.Json.JsonSerializer.Deserialize<List<ColumnDefinition>>(SchemaJson); } catch { }
            if (cols == null) return;

            int n = cols.Count;
            if (n > 0)
            {
                string small = $"cols: {n}";
                var pos = GetMidLabelPosition(lineStart, lineEnd, LabelPlacement.Above, small);
                using var paint = new SKPaint { IsAntialias = true, Color = SKColors.DimGray, Style = SKPaintStyle.Fill };
                canvas.DrawText(small, pos.X, pos.Y, SKTextAlign.Center, _textFont, paint);
            }

            // Show tooltip when hovered
            if (IsHovered && n > 0)
            {
                var lines = new List<string>(16);

                // If an expected schema is available, compute concrete differences and prepend them to the tooltip
                try
                {
                    if (!string.IsNullOrWhiteSpace(ExpectedSchemaJson))
                    {
                        var expected = System.Text.Json.JsonSerializer.Deserialize<List<ColumnDefinition>>(ExpectedSchemaJson) ?? new();
                        var diff = Beep.Skia.Model.SchemaDiffUtil.Compute(expected, cols);
                        if (diff.HasDifferences())
                        {
                            lines.Add("Schema differences:");
                            int maxShow = 5;
                            foreach (var m in diff.MissingColumns.Take(maxShow)) lines.Add($"- Missing: {m}");
                            if (diff.MissingColumns.Count > maxShow) lines.Add($"  … +{diff.MissingColumns.Count - maxShow} more missing");
                            foreach (var td in diff.TypeDifferences.Take(maxShow)) lines.Add($"- Type: {td.Name} expected {td.ExpectedType}, actual {td.ActualType}");
                            if (diff.TypeDifferences.Count > maxShow) lines.Add($"  … +{diff.TypeDifferences.Count - maxShow} more type differences");
                            foreach (var nd in diff.NullabilityDifferences.Take(maxShow))
                                lines.Add($"- Nullability: {nd.Name} expected {(nd.ExpectedNullable ? "nullable" : "not nullable")}, actual {(nd.ActualNullable ? "nullable" : "not nullable")}");
                            if (diff.NullabilityDifferences.Count > maxShow) lines.Add($"  … +{diff.NullabilityDifferences.Count - maxShow} more nullability differences");
                            foreach (var dd in diff.DefaultDifferences.Take(maxShow))
                                lines.Add($"- Default: {dd.Name} expected '{dd.ExpectedDefault}', actual '{dd.ActualDefault}'");
                            if (diff.DefaultDifferences.Count > maxShow) lines.Add($"  … +{diff.DefaultDifferences.Count - maxShow} more default differences");
                            lines.Add(" "); // spacer before listing columns
                        }
                    }
                }
                catch { }

                // Append a compact columns preview: up to 8 columns (Name:Type), ellipsis if more
                int max = Math.Min(8, n);
                for (int i = 0; i < max; i++)
                {
                    var c = cols[i];
                    var name = c?.Name ?? "";
                    var type = c?.DataType ?? "";
                    var flags = (c?.IsPrimaryKey == true ? " [PK]" : "") + (c?.IsForeignKey == true ? " [FK]" : "");
                    lines.Add(string.IsNullOrWhiteSpace(type) ? name + flags : $"{name}: {type}{flags}");
                }
                if (n > max) lines.Add($"… +{n - max} more");

                // Measure tooltip box
                float padding = 6f;
                float lineH = _textFont.Size + 4;
                float width = 0f;
                using var tp = new SKPaint { IsAntialias = true, Color = SKColors.Black, Style = SKPaintStyle.Fill };
                foreach (var s in lines)
                {
                    width = Math.Max(width, _textFont.MeasureText(s));
                }
                float height = lineH * lines.Count + padding * 2;
                width += padding * 2;
                var mid = new SKPoint((lineStart.X + lineEnd.X) / 2, (lineStart.Y + lineEnd.Y) / 2);
                var box = new SKRect(mid.X - width / 2, mid.Y - height - 24, mid.X + width / 2, mid.Y - 24);

                // Draw background and border
                using var bg = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(230), Style = SKPaintStyle.Fill };
                using var border = new SKPaint { IsAntialias = true, Color = SKColors.Gray, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
                canvas.DrawRoundRect(box, 6, 6, bg);
                canvas.DrawRoundRect(box, 6, 6, border);

                // Draw text lines
                float y = box.Top + padding + _textFont.Size;
                foreach (var s in lines)
                {
                    canvas.DrawText(s, box.Left + padding, y, SKTextAlign.Left, _textFont, tp);
                    y += lineH;
                }
            }
        }

        private void TryDrawInfoBadge(SKCanvas canvas, SKPoint lineStart, SKPoint lineEnd)
        {
            // Draw a subtle info badge near midpoint if schema exists
            if (string.IsNullOrWhiteSpace(SchemaJson)) return;
            var mid = new SKPoint((lineStart.X + lineEnd.X) / 2, (lineStart.Y + lineEnd.Y) / 2);
            var center = new SKPoint(mid.X + 16, mid.Y - 16);
            float r = 7f;
            using var fill = new SKPaint { IsAntialias = true, Color = SKColors.SkyBlue.WithAlpha(200), Style = SKPaintStyle.Fill };
            using var stroke = new SKPaint { IsAntialias = true, Color = SKColors.DarkSlateBlue, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };
            using var txt = new SKPaint { IsAntialias = true, Color = SKColors.White, Style = SKPaintStyle.Fill };
            canvas.DrawCircle(center, r, fill);
            canvas.DrawCircle(center, r, stroke);
            // Draw a tiny 'i'
            var font = _textFont ?? new SKFont { Size = 12 };
            canvas.DrawText("i", center.X - font.MeasureText("i") / 2, center.Y + font.Size * 0.35f, SKTextAlign.Left, font, txt);
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

            // Draw based on selected animation style
            switch (AnimationStyle)
            {
                case FlowAnimationStyle.Dots:
                    DrawDotsAnimation(canvas, lineStart, lineEnd, direction, length, drawForward, drawBackward);
                    break;
                case FlowAnimationStyle.Dashes:
                    DrawDashesAnimation(canvas, lineStart, lineEnd, direction, length, drawForward, drawBackward);
                    break;
                case FlowAnimationStyle.Wave:
                    DrawWaveAnimation(canvas, lineStart, lineEnd, direction, length, drawForward, drawBackward);
                    break;
                case FlowAnimationStyle.Gradient:
                    DrawGradientAnimation(canvas, lineStart, lineEnd, direction, length, drawForward, drawBackward);
                    break;
                case FlowAnimationStyle.Particles:
                    DrawParticlesAnimation(canvas, lineStart, lineEnd, direction, length, drawForward, drawBackward);
                    break;
                case FlowAnimationStyle.Pulse:
                    DrawPulseAnimation(canvas, lineStart, lineEnd, direction, length, drawForward, drawBackward);
                    break;
                case FlowAnimationStyle.Arrows:
                    DrawArrowsAnimation(canvas, lineStart, lineEnd, direction, length, drawForward, drawBackward);
                    break;
                default:
                    DrawDotsAnimation(canvas, lineStart, lineEnd, direction, length, drawForward, drawBackward);
                    break;
            }
        }

        private void DrawDotsAnimation(SKCanvas canvas, SKPoint lineStart, SKPoint lineEnd, SKPoint direction, float length, bool drawForward, bool drawBackward)
        {
            using var particlePaint = new SKPaint { Style = SKPaintStyle.Fill, Color = DataFlowColor, IsAntialias = true };

            void drawAlong(SKPoint s, SKPoint e, bool reverse)
            {
                var dir = reverse ? new SKPoint(-direction.X, -direction.Y) : direction;
                float currentOffset = -dataFlowOffset;
                while (currentOffset < length)
                {
                    if (currentOffset >= 0)
                    {
                        var particlePos = new SKPoint(s.X + dir.X * currentOffset, s.Y + dir.Y * currentOffset);
                        canvas.DrawCircle(particlePos, DataFlowParticleSize, particlePaint);
                        using var glowPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = DataFlowColor.WithAlpha(100), IsAntialias = true };
                        canvas.DrawCircle(particlePos, DataFlowParticleSize * 2, glowPaint);
                    }
                    currentOffset += dataFlowSpacing;
                }
            }

            if (drawForward) drawAlong(lineStart, lineEnd, reverse: false);
            if (drawBackward) drawAlong(lineEnd, lineStart, reverse: true);
        }

        private void DrawDashesAnimation(SKCanvas canvas, SKPoint lineStart, SKPoint lineEnd, SKPoint direction, float length, bool drawForward, bool drawBackward)
        {
            using var dashPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = DataFlowColor, IsAntialias = true, StrokeWidth = 3, StrokeCap = SKStrokeCap.Round };

            void drawAlong(SKPoint s, SKPoint e, bool reverse)
            {
                var dir = reverse ? new SKPoint(-direction.X, -direction.Y) : direction;
                float currentOffset = -dataFlowOffset;
                float dashLength = 15f;
                while (currentOffset < length)
                {
                    if (currentOffset >= 0 && currentOffset + dashLength <= length)
                    {
                        var dashStart = new SKPoint(s.X + dir.X * currentOffset, s.Y + dir.Y * currentOffset);
                        var dashEnd = new SKPoint(s.X + dir.X * (currentOffset + dashLength), s.Y + dir.Y * (currentOffset + dashLength));
                        canvas.DrawLine(dashStart, dashEnd, dashPaint);
                    }
                    currentOffset += dataFlowSpacing;
                }
            }

            if (drawForward) drawAlong(lineStart, lineEnd, reverse: false);
            if (drawBackward) drawAlong(lineEnd, lineStart, reverse: true);
        }

        private void DrawWaveAnimation(SKCanvas canvas, SKPoint lineStart, SKPoint lineEnd, SKPoint direction, float length, bool drawForward, bool drawBackward)
        {
            using var wavePaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = DataFlowColor, IsAntialias = true, StrokeWidth = 2 };

            void drawAlong(SKPoint s, SKPoint e, bool reverse)
            {
                var dir = reverse ? new SKPoint(-direction.X, -direction.Y) : direction;
                var perpDir = new SKPoint(-dir.Y, dir.X); // Perpendicular for wave
                float currentOffset = -dataFlowOffset;
                float waveAmplitude = 6f;
                float waveFrequency = 20f;

                while (currentOffset < length)
                {
                    if (currentOffset >= 0 && currentOffset + waveFrequency <= length)
                    {
                        using var path = new SKPath();
                        bool first = true;
                        for (float t = 0; t <= waveFrequency; t += 2f)
                        {
                            float offset = currentOffset + t;
                            float wave = waveAmplitude * (float)Math.Sin((offset / waveFrequency) * Math.PI * 2);
                            var pos = new SKPoint(
                                s.X + dir.X * offset + perpDir.X * wave,
                                s.Y + dir.Y * offset + perpDir.Y * wave
                            );
                            if (first) { path.MoveTo(pos); first = false; } else { path.LineTo(pos); }
                        }
                        canvas.DrawPath(path, wavePaint);
                    }
                    currentOffset += dataFlowSpacing * 2;
                }
            }

            if (drawForward) drawAlong(lineStart, lineEnd, reverse: false);
            if (drawBackward) drawAlong(lineEnd, lineStart, reverse: true);
        }

        private void DrawGradientAnimation(SKCanvas canvas, SKPoint lineStart, SKPoint lineEnd, SKPoint direction, float length, bool drawForward, bool drawBackward)
        {
            void drawAlong(SKPoint s, SKPoint e, bool reverse)
            {
                var dir = reverse ? new SKPoint(-direction.X, -direction.Y) : direction;
                float gradientLength = 40f;
                float currentOffset = -dataFlowOffset;

                while (currentOffset < length)
                {
                    if (currentOffset >= 0 && currentOffset + gradientLength <= length)
                    {
                        var gradStart = new SKPoint(s.X + dir.X * currentOffset, s.Y + dir.Y * currentOffset);
                        var gradEnd = new SKPoint(s.X + dir.X * (currentOffset + gradientLength), s.Y + dir.Y * (currentOffset + gradientLength));
                        
                        using var shader = SKShader.CreateLinearGradient(
                            gradStart, gradEnd,
                            new[] { SKColors.Transparent, DataFlowColor, SKColors.Transparent },
                            new[] { 0f, 0.5f, 1f },
                            SKShaderTileMode.Clamp
                        );
                        using var gradPaint = new SKPaint { Style = SKPaintStyle.Stroke, Shader = shader, IsAntialias = true, StrokeWidth = 4, StrokeCap = SKStrokeCap.Round };
                        canvas.DrawLine(gradStart, gradEnd, gradPaint);
                    }
                    currentOffset += dataFlowSpacing * 2;
                }
            }

            if (drawForward) drawAlong(lineStart, lineEnd, reverse: false);
            if (drawBackward) drawAlong(lineEnd, lineStart, reverse: true);
        }

        private void DrawParticlesAnimation(SKCanvas canvas, SKPoint lineStart, SKPoint lineEnd, SKPoint direction, float length, bool drawForward, bool drawBackward)
        {
            using var particlePaint = new SKPaint { Style = SKPaintStyle.Fill, Color = DataFlowColor, IsAntialias = true };

            void drawAlong(SKPoint s, SKPoint e, bool reverse)
            {
                var dir = reverse ? new SKPoint(-direction.X, -direction.Y) : direction;
                float currentOffset = -dataFlowOffset;
                var random = new Random((int)dataFlowOffset);

                while (currentOffset < length)
                {
                    if (currentOffset >= 0)
                    {
                        // Main particle
                        var particlePos = new SKPoint(s.X + dir.X * currentOffset, s.Y + dir.Y * currentOffset);
                        canvas.DrawCircle(particlePos, DataFlowParticleSize, particlePaint);

                        // Trail particles
                        for (int i = 1; i <= 3; i++)
                        {
                            float trailOffset = currentOffset - (i * 5);
                            if (trailOffset >= 0)
                            {
                                var trailPos = new SKPoint(s.X + dir.X * trailOffset, s.Y + dir.Y * trailOffset);
                                byte alpha = (byte)(255 - (i * 70));
                                using var trailPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = DataFlowColor.WithAlpha(alpha), IsAntialias = true };
                                canvas.DrawCircle(trailPos, DataFlowParticleSize * (1 - i * 0.2f), trailPaint);
                            }
                        }
                    }
                    currentOffset += dataFlowSpacing;
                }
            }

            if (drawForward) drawAlong(lineStart, lineEnd, reverse: false);
            if (drawBackward) drawAlong(lineEnd, lineStart, reverse: true);
        }

        private void DrawPulseAnimation(SKCanvas canvas, SKPoint lineStart, SKPoint lineEnd, SKPoint direction, float length, bool drawForward, bool drawBackward)
        {
            void drawAlong(SKPoint s, SKPoint e, bool reverse)
            {
                var dir = reverse ? new SKPoint(-direction.X, -direction.Y) : direction;
                float currentOffset = -dataFlowOffset;

                while (currentOffset < length)
                {
                    if (currentOffset >= 0)
                    {
                        var pulseCenter = new SKPoint(s.X + dir.X * currentOffset, s.Y + dir.Y * currentOffset);
                        
                        // Draw expanding rings
                        for (int ring = 0; ring < 3; ring++)
                        {
                            float ringRadius = DataFlowParticleSize * (1 + ring * 1.5f);
                            byte alpha = (byte)(200 - ring * 60);
                            using var pulsePaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = DataFlowColor.WithAlpha(alpha), IsAntialias = true, StrokeWidth = 2 };
                            canvas.DrawCircle(pulseCenter, ringRadius, pulsePaint);
                        }
                    }
                    currentOffset += dataFlowSpacing * 2;
                }
            }

            if (drawForward) drawAlong(lineStart, lineEnd, reverse: false);
            if (drawBackward) drawAlong(lineEnd, lineStart, reverse: true);
        }

        private void DrawArrowsAnimation(SKCanvas canvas, SKPoint lineStart, SKPoint lineEnd, SKPoint direction, float length, bool drawForward, bool drawBackward)
        {
            using var arrowPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = DataFlowColor, IsAntialias = true };

            void drawAlong(SKPoint s, SKPoint e, bool reverse)
            {
                var dir = reverse ? new SKPoint(-direction.X, -direction.Y) : direction;
                float currentOffset = -dataFlowOffset;
                float arrowSize = DataFlowParticleSize * 2;

                while (currentOffset < length)
                {
                    if (currentOffset >= 0)
                    {
                        var arrowPos = new SKPoint(s.X + dir.X * currentOffset, s.Y + dir.Y * currentOffset);
                        
                        using var path = new SKPath();
                        // Arrow pointing in movement direction
                        path.MoveTo(arrowPos.X + dir.X * arrowSize, arrowPos.Y + dir.Y * arrowSize);
                        path.LineTo(arrowPos.X - dir.Y * arrowSize * 0.5f, arrowPos.Y + dir.X * arrowSize * 0.5f);
                        path.LineTo(arrowPos.X + dir.Y * arrowSize * 0.5f, arrowPos.Y - dir.X * arrowSize * 0.5f);
                        path.Close();
                        canvas.DrawPath(path, arrowPaint);
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
            // Guard against missing endpoints; use EndPoint when End is null (preview state)
            if (Start == null && End == null)
                return false;

            var a = Start != null ? Start.Position : EndPoint;
            var b = End != null ? End.Position : EndPoint;
            // Check if the distance from the point to the line is within the click threshold
            var lineVector = b - a;
            var pointVector = point - a;
            var lineLength = (float)Math.Sqrt(lineVector.X * lineVector.X + lineVector.Y * lineVector.Y);
            var dotProduct = lineVector.X * pointVector.X + lineVector.Y * pointVector.Y;
            var projectionLength = dotProduct / lineLength;
            if (projectionLength < 0 || projectionLength > lineLength)
            {
                return false;
            }
            var projectionPoint = a + new SKPoint(lineVector.X * projectionLength / lineLength, lineVector.Y * projectionLength / lineLength);
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
            if (startArrow && ShowStartArrow && Start != null)
            {
                var arrowBoundingBox = CalculateArrowBoundingBox(Start.Position, (End != null ? End.Position : EndPoint));
                return arrowBoundingBox.Contains(point);
            }
            else if (!startArrow && ShowEndArrow)
            {
                var tip = (End != null ? End.Position : EndPoint);
                var tail = (Start != null ? Start.Position : EndPoint);
                var arrowBoundingBox = CalculateArrowBoundingBox(tip, tail);
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
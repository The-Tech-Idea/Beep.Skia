using SkiaSharp;
using Beep.Skia;

namespace Beep.Skia.UML
{
    /// <summary>
    /// Represents a UML message in sequence diagrams.
    /// Displays arrows between lifelines with different styles for different message types.
    /// </summary>
    public class UMLMessage : ConnectionLine
    {
        /// <summary>
        /// Gets or sets the message text/label.
        /// </summary>
        public string MessageText { get; set; } = "message()";

        /// <summary>
        /// Gets or sets the type of message.
        /// </summary>
        public MessageType MessageType { get; set; } = MessageType.Synchronous;

        /// <summary>
        /// Gets or sets the sequence number (optional).
        /// </summary>
        public string SequenceNumber { get; set; } = "";

        /// <summary>
        /// Gets or sets whether to show animated data flow for this message.
        /// Useful for visualizing data flow in sequence diagrams.
        /// </summary>
        public bool ShowDataFlow { get; set; } = false;

        /// <summary>
        /// Gets or sets the return data type for the message (used for data flow visualization).
        /// </summary>
        public string ReturnDataType { get; set; } = "void";

        /// <summary>
        /// Initializes a new instance of the <see cref="UMLMessage"/> class.
        /// </summary>
        public UMLMessage() : base(() => { })
        {
            // Set default styling
            if (Paint != null)
            {
                Paint.Color = SKColors.Black;
                Paint.StrokeWidth = 1;
            }

            // Disable default arrows since we draw custom ones
            ShowStartArrow = false;
            ShowEndArrow = false;

            // Enable data flow animation by default for return messages
            IsDataFlowAnimated = false; // Let users opt-in
            DataFlowSpeed = 30.0f; // Slower for messages
            DataFlowParticleSize = 3.0f; // Smaller particles
        }

        /// <summary>
        /// Draws the message arrow with appropriate styling.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        public new void Draw(SKCanvas canvas)
        {
            // Enable/disable data flow animation based on ShowDataFlow setting
            IsDataFlowAnimated = ShowDataFlow;

            // Set data flow color based on return data type
            if (ShowDataFlow && ReturnDataType != "void")
            {
                DataFlowColor = GetDataFlowColorForType(ReturnDataType);
            }

            // Draw the basic line (includes data flow animation)
            base.Draw(canvas);

            // Draw message-specific arrow and label
            DrawMessageArrow(canvas);
            DrawMessageLabel(canvas);
        }

        /// <summary>
        /// Draws the arrow based on the message type.
        /// </summary>
        private void DrawMessageArrow(SKCanvas canvas)
        {
            if (Start == null || End == null)
                return;

            var startPoint = Start.Position;
            var endPoint = End.Position;

            using var paint = new SKPaint
            {
                Color = Paint?.Color ?? SKColors.Black,
                StrokeWidth = Paint?.StrokeWidth ?? 1,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            using var fillPaint = new SKPaint
            {
                Color = Paint?.Color ?? SKColors.Black,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            switch (MessageType)
            {
                case MessageType.Synchronous:
                    DrawFilledArrow(canvas, startPoint, endPoint, paint, fillPaint);
                    break;
                case MessageType.Asynchronous:
                    DrawOpenArrow(canvas, startPoint, endPoint, paint);
                    break;
                case MessageType.Return:
                    DrawDashedArrow(canvas, startPoint, endPoint, paint);
                    break;
                case MessageType.Create:
                    DrawDashedArrowWithCircle(canvas, startPoint, endPoint, paint);
                    break;
                case MessageType.Destroy:
                    DrawArrowWithX(canvas, startPoint, endPoint, paint);
                    break;
                case MessageType.Found:
                    DrawCircleAndArrow(canvas, startPoint, endPoint, paint, fillPaint);
                    break;
                case MessageType.Lost:
                    DrawArrowWithCircle(canvas, startPoint, endPoint, paint, fillPaint);
                    break;
                default:
                    DrawOpenArrow(canvas, startPoint, endPoint, paint);
                    break;
            }
        }

        /// <summary>
        /// Draws a filled arrow (synchronous message).
        /// </summary>
        private void DrawFilledArrow(SKCanvas canvas, SKPoint start, SKPoint end, SKPaint strokePaint, SKPaint fillPaint)
        {
            DrawArrow(canvas, start, end, strokePaint, fillPaint, true);
        }

        /// <summary>
        /// Draws an open arrow (asynchronous message).
        /// </summary>
        private void DrawOpenArrow(SKCanvas canvas, SKPoint start, SKPoint end, SKPaint paint)
        {
            DrawArrow(canvas, start, end, paint, null, false);
        }

        /// <summary>
        /// Draws a dashed arrow (return message).
        /// </summary>
        private void DrawDashedArrow(SKCanvas canvas, SKPoint start, SKPoint end, SKPaint paint)
        {
            paint.PathEffect = SKPathEffect.CreateDash(new float[] { 5, 3 }, 0);
            DrawArrow(canvas, start, end, paint, null, false);
        }

        /// <summary>
        /// Draws a dashed arrow with a circle (create message).
        /// </summary>
        private void DrawDashedArrowWithCircle(SKCanvas canvas, SKPoint start, SKPoint end, SKPaint paint)
        {
            // Draw circle at start
            canvas.DrawCircle(start.X, start.Y, 4, paint);

            // Draw dashed arrow
            paint.PathEffect = SKPathEffect.CreateDash(new float[] { 5, 3 }, 0);
            DrawArrow(canvas, start, end, paint, null, false);
        }

        /// <summary>
        /// Draws an arrow with an X at the end (destroy message).
        /// </summary>
        private void DrawArrowWithX(SKCanvas canvas, SKPoint start, SKPoint end, SKPaint paint)
        {
            DrawArrow(canvas, start, end, paint, null, false);

            // Draw X at the end
            const float xSize = 6;
            canvas.DrawLine(end.X - xSize, end.Y - xSize, end.X + xSize, end.Y + xSize, paint);
            canvas.DrawLine(end.X - xSize, end.Y + xSize, end.X + xSize, end.Y - xSize, paint);
        }

        /// <summary>
        /// Draws a circle and arrow (found message).
        /// </summary>
        private void DrawCircleAndArrow(SKCanvas canvas, SKPoint start, SKPoint end, SKPaint strokePaint, SKPaint fillPaint)
        {
            // Draw circle at start
            canvas.DrawCircle(start.X, start.Y, 4, strokePaint);

            // Draw arrow
            DrawArrow(canvas, start, end, strokePaint, fillPaint, true);
        }

        /// <summary>
        /// Draws an arrow with a circle at the end (lost message).
        /// </summary>
        private void DrawArrowWithCircle(SKCanvas canvas, SKPoint start, SKPoint end, SKPaint strokePaint, SKPaint fillPaint)
        {
            // Draw arrow
            DrawArrow(canvas, start, end, strokePaint, fillPaint, true);

            // Draw circle at end
            canvas.DrawCircle(end.X, end.Y, 4, strokePaint);
        }

        /// <summary>
        /// Draws a generic arrow with optional fill.
        /// </summary>
        private void DrawArrow(SKCanvas canvas, SKPoint start, SKPoint end, SKPaint strokePaint, SKPaint fillPaint, bool filled)
        {
            // Calculate arrow direction
            var direction = new SKPoint(end.X - start.X, end.Y - start.Y);
            var length = (float)System.Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);

            if (length == 0)
                return;

            direction = new SKPoint(direction.X / length, direction.Y / length);
            var perpendicular = new SKPoint(-direction.Y, direction.X);

            const float arrowSize = 8;

            // Arrow tip
            var tip = end;

            // Arrow wings
            var left = new SKPoint(
                end.X - direction.X * arrowSize + perpendicular.X * arrowSize / 2,
                end.Y - direction.Y * arrowSize + perpendicular.Y * arrowSize / 2
            );
            var right = new SKPoint(
                end.X - direction.X * arrowSize - perpendicular.X * arrowSize / 2,
                end.Y - direction.Y * arrowSize - perpendicular.Y * arrowSize / 2
            );

            // Draw arrow
            var path = new SKPath();
            path.MoveTo(tip);
            path.LineTo(left);
            path.LineTo(right);
            path.Close();

            if (filled && fillPaint != null)
            {
                canvas.DrawPath(path, fillPaint);
            }

            canvas.DrawPath(path, strokePaint);
        }

        /// <summary>
        /// Draws the message label above the arrow.
        /// </summary>
        private void DrawMessageLabel(SKCanvas canvas)
        {
            if (Start == null || End == null || string.IsNullOrEmpty(MessageText))
                return;

            using var font = new SKFont(SKTypeface.Default, 10);
            using var paint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true
            };

            var startPoint = Start.Position;
            var endPoint = End.Position;

            // Calculate midpoint
            var midPoint = new SKPoint(
                (startPoint.X + endPoint.X) / 2,
                (startPoint.Y + endPoint.Y) / 2
            );

            // Position label above the line
            float labelY = midPoint.Y - 5;

            // Add sequence number if present
            string labelText = string.IsNullOrEmpty(SequenceNumber) ?
                MessageText :
                $"{SequenceNumber}: {MessageText}";

            canvas.DrawText(labelText, midPoint.X, labelY, SKTextAlign.Center, font, paint);
        }

        /// <summary>
        /// Gets the appropriate data flow color for a given data type.
        /// </summary>
        /// <param name="dataType">The data type string.</param>
        /// <returns>The SKColor for data flow visualization.</returns>
        private SKColor GetDataFlowColorForType(string dataType)
        {
            if (string.IsNullOrEmpty(dataType))
                return SKColors.Gray;

            return dataType.ToLowerInvariant() switch
            {
                "string" => SKColors.Blue,
                "number" or "int" or "float" or "double" => SKColors.Green,
                "boolean" or "bool" => SKColors.Orange,
                "object" or "reference" => SKColors.Purple,
                "array" or "list" => SKColors.Red,
                "datetime" or "date" => SKColors.Cyan,
                "void" => SKColors.Gray,
                _ => SKColors.Gray
            };
        }
    }

    /// <summary>
    /// Defines the types of UML messages.
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// Synchronous message (filled arrow).
        /// </summary>
        Synchronous,

        /// <summary>
        /// Asynchronous message (open arrow).
        /// </summary>
        Asynchronous,

        /// <summary>
        /// Return message (dashed arrow).
        /// </summary>
        Return,

        /// <summary>
        /// Create message (dashed arrow with circle).
        /// </summary>
        Create,

        /// <summary>
        /// Destroy message (arrow with X).
        /// </summary>
        Destroy,

        /// <summary>
        /// Found message (circle and arrow).
        /// </summary>
        Found,

        /// <summary>
        /// Lost message (arrow with circle).
        /// </summary>
        Lost
    }
}
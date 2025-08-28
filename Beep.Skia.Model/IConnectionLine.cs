using SkiaSharp;

namespace Beep.Skia
{
    /// <summary>
    /// Represents a connection line between two workflow components in a Skia-based diagram.
    /// Connection lines can have arrows, labels, and support animation.
    /// </summary>
    public interface IConnectionLine
    {
        /// <summary>
        /// Gets or sets the starting connection point of the line.
        /// </summary>
        IConnectionPoint Start { get; set; }

        /// <summary>
        /// Gets or sets the ending connection point of the line.
        /// </summary>
        IConnectionPoint End { get; set; }

        /// <summary>
        /// Gets or sets the end point coordinates when not connected to a specific end point.
        /// </summary>
        SKPoint EndPoint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the connection line is currently connected.
        /// </summary>
        bool IsConnected { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the connection line is currently selected.
        /// </summary>
        bool IsSelected { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the connection line should be animated.
        /// </summary>
        bool IsAnimated { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show an arrow at the start of the line.
        /// </summary>
        bool ShowStartArrow { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show an arrow at the end of the line.
        /// </summary>
        bool ShowEndArrow { get; set; }

        /// <summary>
        /// Gets or sets the paint object used for drawing the connection line.
        /// </summary>
        SKPaint Paint { get; set; }

        /// <summary>
        /// Gets or sets the first label text to display along the connection line.
        /// </summary>
        string Label1 { get; set; }

        /// <summary>
        /// Gets or sets the second label text to display along the connection line.
        /// </summary>
        string Label2 { get; set; }

        /// <summary>
        /// Gets or sets the third label text to display along the connection line.
        /// </summary>
        string Label3 { get; set; }

        /// <summary>
        /// Gets or sets the color of the connection line.
        /// </summary>
        SKColor LineColor { get; set; }

        /// <summary>
        /// Event raised when the start arrow is clicked.
        /// </summary>
        event EventHandler<ConnectionArgs> StartArrowClicked;

        /// <summary>
        /// Event raised when the connection line itself is clicked.
        /// </summary>
        event EventHandler<LineArgs> LineClicked;

        /// <summary>
        /// Event raised when the end arrow is clicked.
        /// </summary>
        event EventHandler<ConnectionArgs> EndArrowClicked;

        /// <summary>
        /// Determines whether the specified point is within the bounds of an arrow.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <param name="startArrow">True to test the start arrow, false to test the end arrow.</param>
        /// <returns>True if the point is within the arrow bounds, otherwise false.</returns>
        bool ArrowContainsPoint(SKPoint point, bool startArrow);

        /// <summary>
        /// Draws the connection line on the specified canvas.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        void Draw(SKCanvas canvas);

        /// <summary>
        /// Determines whether the specified point is along the connection line.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>True if the point is along the line, otherwise false.</returns>
        bool LineContainsPoint(SKPoint point);

        /// <summary>
        /// Handles click events on the connection line or its arrows.
        /// </summary>
        /// <param name="clickPoint">The point where the click occurred.</param>
        void OnClick(SKPoint clickPoint);

        /// <summary>
        /// Starts the animation of the connection line.
        /// </summary>
        void StartAnimation();

        /// <summary>
        /// Stops the animation of the connection line.
        /// </summary>
        void StopAnimation();
    }
}
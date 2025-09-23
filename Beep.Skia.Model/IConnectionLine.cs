using SkiaSharp;
using System;

namespace Beep.Skia.Model
{
    /// <summary>
    /// ERD multiplicity markers to draw at each end of a connection.
    /// Unspecified means no ERD-specific markers are rendered at that end.
    /// </summary>
    public enum ERDMultiplicity
    {
        Unspecified = 0,
        ZeroOrOne = 1,   // circle + bar
        OneOnly = 2,     // double bar (exactly one)
        OneOrMany = 3,   // bar + crow's foot
        ZeroOrMany = 4,  // circle + crow's foot
        Many = 5,        // crow's foot only
        One = 6          // single bar only
    }
    /// <summary>
    /// Routing modes for connection lines.
    /// </summary>
    public enum LineRoutingMode
    {
        Straight,
        Orthogonal,
        Curved
    }

    /// <summary>
    /// Where to place a label relative to the line.
    /// </summary>
    public enum LabelPlacement
    {
        Above,
        Below,
        Over
    }

    /// <summary>
    /// Direction of animated data flow.
    /// </summary>
    public enum DataFlowDirection
    {
        None,
        Forward,
        Backward,
        Bidirectional
    }

    /// <summary>
    /// Status indicator shown on the line (e.g., spinner at midpoint).
    /// </summary>
    public enum LineStatus
    {
        None,
        Working,
        Running,
        Warning,
        Error
    }
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
    /// Size of arrowheads in pixels.
    /// </summary>
    float ArrowSize { get; set; }

    /// <summary>
    /// Optional dash pattern for the stroke (pairs of on/off lengths). Null or empty for solid.
    /// </summary>
    float[] DashPattern { get; set; }

    /// <summary>
    /// Routing mode for the line.
    /// </summary>
    LineRoutingMode RoutingMode { get; set; }

    /// <summary>
    /// Direction of animated data flow particles.
    /// </summary>
    DataFlowDirection FlowDirection { get; set; }

    /// <summary>
    /// Placement for labels relative to the line.
    /// </summary>
    LabelPlacement Label1Placement { get; set; }
    LabelPlacement Label2Placement { get; set; }
    LabelPlacement Label3Placement { get; set; }
    LabelPlacement DataLabelPlacement { get; set; }

    /// <summary>
    /// Shows a small status indicator at the line's midpoint.
    /// </summary>
    bool ShowStatusIndicator { get; set; }

    /// <summary>
    /// Type of the status to show (e.g., Working/Running spinner).
    /// </summary>
    LineStatus Status { get; set; }

    /// <summary>
    /// Color used for the status indicator.
    /// </summary>
    SKColor StatusColor { get; set; }

    /// <summary>
    /// Optional ERD multiplicity marker at the start end (at <see cref="Start"/>).
    /// If not Unspecified, ERD markers are rendered and the standard arrow is suppressed for this end.
    /// </summary>
    ERDMultiplicity StartMultiplicity { get; set; }

    /// <summary>
    /// Optional ERD multiplicity marker at the end end (at <see cref="End"/>).
    /// If not Unspecified, ERD markers are rendered and the standard arrow is suppressed for this end.
    /// </summary>
    ERDMultiplicity EndMultiplicity { get; set; }

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
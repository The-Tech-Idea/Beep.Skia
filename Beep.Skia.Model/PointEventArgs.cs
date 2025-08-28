
namespace Beep.Skia
{
    /// <summary>
    /// Provides event arguments for point-related events, such as mouse interactions.
    /// </summary>
    public class PointEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the X coordinate of the point.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Gets or sets the Y coordinate of the point.
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the right mouse button was used.
        /// </summary>
        public bool IsRightButton { get; set; }
    }
}

using SkiaSharp;
using Beep.Skia.Model;
namespace Beep.Skia
{
    /// <summary>
    /// Gets or sets the drawing manager.
    /// </summary>
    public partial class DrawingManager
    {
        /// <summary>
        /// Draws all components and connection lines on the specified canvas.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        public void Draw(SKCanvas canvas)
        {
            _renderingHelper.DrawAll(canvas);
        }

        /// <summary>
        /// Renders the diagram for export: no pan/zoom, no grid, no selection adorners,
        /// and no static screen-space overlays (palette, property editor).
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        internal void DrawForExport(SKCanvas canvas)
        {
            _renderingHelper.DrawForExport(canvas);
        }
    }
}

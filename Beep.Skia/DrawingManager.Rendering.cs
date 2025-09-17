using SkiaSharp;
using Beep.Skia.Model;
namespace Beep.Skia
{
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
    }
}

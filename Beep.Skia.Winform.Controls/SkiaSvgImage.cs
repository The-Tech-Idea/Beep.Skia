using System.ComponentModel;
using System.Windows.Forms;
using SkiaSvgComponent = Beep.Skia.Components.SvgImage;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Skia SvgImage wrapper (minimal)")]
    [DisplayName("Skia SvgImage")]
    public class SkiaSvgImage : SkiaControl
    {
        private SkiaSvgComponent _svg;

        public SkiaSvgImage()
        {
            _svg = CreateSkiaComponent<SkiaSvgComponent>();
            _svg.Width = 48;
            _svg.Height = 48;
            SkiaComponent = _svg;
        }
    }
}

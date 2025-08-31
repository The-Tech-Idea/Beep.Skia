using System.ComponentModel;
using System.Windows.Forms;
using SkiaProgressBarComponent = Beep.Skia.Components.ProgressBar;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Skia ProgressBar wrapper (minimal)")]
    [DisplayName("Skia ProgressBar")]
    public class SkiaProgressBar : SkiaControl
    {
        private SkiaProgressBarComponent _pb;

        public SkiaProgressBar()
        {
            _pb = CreateSkiaComponent<SkiaProgressBarComponent>();
            _pb.Width = 200;
            _pb.Height = 16;
            SkiaComponent = _pb;
        }
    }
}

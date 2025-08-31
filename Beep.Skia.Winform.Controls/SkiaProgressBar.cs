using System.ComponentModel;
using System.Windows.Forms;
using SkiaProgressBarComponent = Beep.Skia.Components.ProgressBar;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia ProgressBar component on the Skia host")]
    [DisplayName("Skia ProgressBar")]
    public class SkiaProgressBar : SkiaControl
    {
        private SkiaProgressBarComponent _pb;

        public SkiaProgressBar()
        {
            _pb = CreateSkiaComponent<SkiaProgressBarComponent>();
            _pb.Width = 120;
            _pb.Height = 8;
            SkiaComponent = _pb;
        }
    }
}

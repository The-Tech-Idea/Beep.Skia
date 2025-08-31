using System.ComponentModel;
using System.Windows.Forms;
using SkiaToolBarComponent = Beep.Skia.Components.ToolBar;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia ToolBar component on the Skia host")]
    [DisplayName("Skia ToolBar")]
    public class SkiaToolBar : SkiaControl
    {
        private SkiaToolBarComponent _tb;

        public SkiaToolBar()
        {
            _tb = CreateSkiaComponent<SkiaToolBarComponent>();
            _tb.Width = 320;
            _tb.Height = 40;
            SkiaComponent = _tb;
        }
    }
}

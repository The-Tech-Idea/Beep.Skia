using System.ComponentModel;
using System.Windows.Forms;
using SkiaStatusBarComponent = Beep.Skia.Components.StatusBar;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia StatusBar component on the Skia host")]
    [DisplayName("Skia StatusBar")]
    public class SkiaStatusBar : SkiaControl
    {
        private SkiaStatusBarComponent _sb;

        public SkiaStatusBar()
        {
            _sb = CreateSkiaComponent<SkiaStatusBarComponent>();
            _sb.Width = 320;
            _sb.Height = 24;
            SkiaComponent = _sb;
        }
    }
}

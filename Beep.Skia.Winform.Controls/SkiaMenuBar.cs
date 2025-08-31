using System.ComponentModel;
using System.Windows.Forms;
using SkiaMenuBarComponent = Beep.Skia.Components.MenuBar;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Skia MenuBar wrapper (minimal)")]
    [DisplayName("Skia MenuBar")]
    public class SkiaMenuBar : SkiaControl
    {
        private SkiaMenuBarComponent _mb;

        public SkiaMenuBar()
        {
            _mb = CreateSkiaComponent<SkiaMenuBarComponent>();
            _mb.Width = 400;
            _mb.Height = 28;
            SkiaComponent = _mb;
        }
    }
}

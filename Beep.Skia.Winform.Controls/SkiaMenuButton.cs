using System.ComponentModel;
using System.Windows.Forms;
using SkiaMenuButtonComponent = Beep.Skia.Components.MenuButton;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Skia MenuButton wrapper (minimal)")]
    [DisplayName("Skia MenuButton")]
    public class SkiaMenuButton : SkiaControl
    {
        private SkiaMenuButtonComponent _btn;

        public SkiaMenuButton()
        {
            _btn = CreateSkiaComponent<SkiaMenuButtonComponent>();
            _btn.Width = 120;
            _btn.Height = 28;
            SkiaComponent = _btn;
        }
    }
}

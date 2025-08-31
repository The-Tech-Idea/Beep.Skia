using System.ComponentModel;
using System.Windows.Forms;
using SkiaMenuComponent = Beep.Skia.Components.Menu;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia Menu component on the Skia host")]
    [DisplayName("Skia Menu")]
    public class SkiaMenu : SkiaControl
    {
        private SkiaMenuComponent _menu;

        public SkiaMenu()
        {
            _menu = CreateSkiaComponent<SkiaMenuComponent>();
            _menu.Width = 160;
            _menu.Height = 24;
            SkiaComponent = _menu;
        }
    }
}

using System.ComponentModel;
using System.Windows.Forms;
using SkiaFabMenuComponent = Beep.Skia.Components.FabMenu;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia FabMenu component on the Skia host")]
    [DisplayName("Skia FabMenu")]
    public class SkiaFabMenu : SkiaControl
    {
        private SkiaFabMenuComponent _fab;

        public SkiaFabMenu()
        {
            _fab = CreateSkiaComponent<SkiaFabMenuComponent>();
            _fab.Width = 56;
            _fab.Height = 56;
            SkiaComponent = _fab;
        }
    }
}

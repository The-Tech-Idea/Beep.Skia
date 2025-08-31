using System.ComponentModel;
using System.Windows.Forms;
using SkiaCascadingMenuComponent = Beep.Skia.Components.CascadingMenu;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Skia CascadingMenu wrapper (minimal)")]
    [DisplayName("Skia CascadingMenu")]
    public class SkiaCascadingMenu : SkiaControl
    {
        private SkiaCascadingMenuComponent _cm;

        public SkiaCascadingMenu()
        {
            _cm = CreateSkiaComponent<SkiaCascadingMenuComponent>();
            _cm.Width = 200;
            _cm.Height = 160;
            SkiaComponent = _cm;
        }
    }
}

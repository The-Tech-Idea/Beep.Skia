using System.ComponentModel;
using System.Windows.Forms;
using SkiaNavComponent = Beep.Skia.Components.NavigationDrawer;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Skia NavigationDrawer wrapper (minimal)")]
    [DisplayName("Skia NavigationDrawer")]
    public class SkiaNavigationDrawer : SkiaControl
    {
        private SkiaNavComponent _nav;

        public SkiaNavigationDrawer()
        {
            _nav = CreateSkiaComponent<SkiaNavComponent>();
            _nav.Width = 200;
            _nav.Height = 320;
            SkiaComponent = _nav;
        }
    }
}

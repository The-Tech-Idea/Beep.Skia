using System.ComponentModel;
using System.Windows.Forms;
using SkiaNavigationBarComponent = Beep.Skia.Components.NavigationBar;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia NavigationBar component on the Skia host")]
    [DisplayName("Skia NavigationBar")]
        public class SkiaNavigationBar : SkiaControl
    {
        private SkiaNavigationBarComponent _nav;

        public SkiaNavigationBar()
        {
            _nav = CreateSkiaComponent<SkiaNavigationBarComponent>();
            _nav.Width = 320;
            _nav.Height = 56;
            SkiaComponent = _nav;
        }
    }
}

using System.ComponentModel;
using System.Windows.Forms;
using SkiaTabsComponent = Beep.Skia.Components.Tabs;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia Tabs component on the Skia host")]
    [DisplayName("Skia Tabs")]
    public class SkiaTabs : SkiaControl
    {
        private SkiaTabsComponent _tabs;

        public SkiaTabs()
        {
            _tabs = CreateSkiaComponent<SkiaTabsComponent>();
            _tabs.Width = 320;
            _tabs.Height = 40;
            SkiaComponent = _tabs;
        }
    }
}

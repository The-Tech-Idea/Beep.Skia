using System.ComponentModel;
using System.Windows.Forms;
using SkiaPanelComponent = Beep.Skia.Components.Panel;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia Panel component on the Skia host")]
    [DisplayName("Skia Panel")]
    public class SkiaPanel : SkiaControl
    {
        private SkiaPanelComponent _panel;

        public SkiaPanel()
        {
            _panel = CreateSkiaComponent<SkiaPanelComponent>();
            _panel.Width = 200;
            _panel.Height = 100;
            SkiaComponent = _panel;
        }
    }
}

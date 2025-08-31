using System.ComponentModel;
using System.Windows.Forms;
using SkiaSwitchComponent = Beep.Skia.Components.Switch;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia Switch component on the Skia host")]
    [DisplayName("Skia Switch")]
    public class SkiaSwitch : SkiaControl
    {
        private SkiaSwitchComponent _sw;

        public SkiaSwitch()
        {
            _sw = CreateSkiaComponent<SkiaSwitchComponent>();
            _sw.Width = 48;
            _sw.Height = 24;
            SkiaComponent = _sw;
        }
    }
}

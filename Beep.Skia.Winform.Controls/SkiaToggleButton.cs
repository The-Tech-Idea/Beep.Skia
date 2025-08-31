using System.ComponentModel;
using System.Windows.Forms;
using SkiaToggleButtonComponent = Beep.Skia.Components.ToggleButton;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia ToggleButton component on the Skia host")]
    [DisplayName("Skia ToggleButton")]
    public class SkiaToggleButton : SkiaControl
    {
        private SkiaToggleButtonComponent _tb;

        public SkiaToggleButton()
        {
            _tb = CreateSkiaComponent<SkiaToggleButtonComponent>();
            _tb.Width = 48;
            _tb.Height = 24;
            SkiaComponent = _tb;
        }
    }
}

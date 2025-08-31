using System.ComponentModel;
using System.Windows.Forms;
using SkiaLabelComponent = Beep.Skia.Components.Label;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia Label component on the Skia host")]
    [DisplayName("Skia Label")]
    public class SkiaLabel : SkiaControl
    {
        private SkiaLabelComponent _label;

        public SkiaLabel()
        {
            _label = CreateSkiaComponent<SkiaLabelComponent>();
            _label.Width = 100;
            _label.Height = 24;
            SkiaComponent = _label;
        }
    }
}

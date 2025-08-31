using System.ComponentModel;
using System.Windows.Forms;
using SkiaSplitButtonComponent = Beep.Skia.Components.SplitButton;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Skia SplitButton wrapper (minimal)")]
    [DisplayName("Skia SplitButton")]
    public class SkiaSplitButton : SkiaControl
    {
        private SkiaSplitButtonComponent _sb;

        public SkiaSplitButton()
        {
            _sb = CreateSkiaComponent<SkiaSplitButtonComponent>();
            _sb.Width = 200;
            _sb.Height = 32;
            SkiaComponent = _sb;
        }
    }
}

using System.ComponentModel;
using System.Windows.Forms;
using SkiaSegmentedButtonsComponent = Beep.Skia.Components.SegmentedButtons;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Skia SegmentedButtons wrapper (minimal)")]
    [DisplayName("Skia SegmentedButtons")]
    public class SkiaSegmentedButtons : SkiaControl
    {
        private SkiaSegmentedButtonsComponent _sb;

        public SkiaSegmentedButtons()
        {
            _sb = CreateSkiaComponent<SkiaSegmentedButtonsComponent>();
            _sb.Width = 200;
            _sb.Height = 32;
            SkiaComponent = _sb;
        }
    }
}

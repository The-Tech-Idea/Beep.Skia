using System.ComponentModel;
using System.Windows.Forms;
using SkiaDatePickerComponent = Beep.Skia.Components.DatePicker;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia DatePicker component on the Skia host")]
    [DisplayName("Skia DatePicker")]
    public class SkiaDatePicker : SkiaControl
    {
        private SkiaDatePickerComponent _dp;

        public SkiaDatePicker()
        {
            _dp = CreateSkiaComponent<SkiaDatePickerComponent>();
            _dp.Width = 160;
            _dp.Height = 28;
            SkiaComponent = _dp;
        }
    }
}

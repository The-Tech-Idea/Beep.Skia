using System.ComponentModel;
using System.Windows.Forms;
using SkiaSliderComponent = Beep.Skia.Components.Slider;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia Slider component on the Skia host")]
    [DisplayName("Skia Slider")]
    public class SkiaSlider : SkiaControl
    {
        private SkiaSliderComponent _slider;

        public SkiaSlider()
        {
            _slider = CreateSkiaComponent<SkiaSliderComponent>();
            _slider.Width = 160;
            _slider.Height = 24;
            SkiaComponent = _slider;
        }
    }
}

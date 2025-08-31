using System.ComponentModel;
using System.Windows.Forms;
using SkiaSpinnerComponent = Beep.Skia.Components.Spinner;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia Spinner component on the Skia host")]
    [DisplayName("Skia Spinner")]
    public class SkiaSpinner : SkiaControl
    {
        private SkiaSpinnerComponent _spinner;

        public SkiaSpinner()
        {
            _spinner = CreateSkiaComponent<SkiaSpinnerComponent>();
            _spinner.Width = 24;
            _spinner.Height = 24;
            SkiaComponent = _spinner;
        }
    }
}

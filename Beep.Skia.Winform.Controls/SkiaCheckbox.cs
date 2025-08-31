using System.ComponentModel;
using System.Windows.Forms;
using SkiaCheckBoxComponent = Beep.Skia.Components.Checkbox;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia Checkbox component on the Skia host")]
    [DisplayName("Skia CheckBox")]
    public class SkiaCheckBox : SkiaControl
    {
        private SkiaCheckBoxComponent _chk;

        public SkiaCheckBox()
        {
            _chk = CreateSkiaComponent<SkiaCheckBoxComponent>();
            _chk.Width = 20;
            _chk.Height = 20;
            SkiaComponent = _chk;
        }
    }
}

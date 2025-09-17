using System.ComponentModel;
using System.Windows.Forms;
using SkiaButtonComponent = Beep.Skia.Components.Button;
using Beep.Skia.Model;
namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia Button component on the Skia host")]
    [DisplayName("Skia Button")]
    public class SkiaButton : SkiaControl
    {
        private SkiaButtonComponent _btn;

        public SkiaButton()
        {
            _btn = CreateSkiaComponent<SkiaButtonComponent>();
            _btn.Width = 100;
            _btn.Height = 36;
            SkiaComponent = _btn;
        }
    }
}

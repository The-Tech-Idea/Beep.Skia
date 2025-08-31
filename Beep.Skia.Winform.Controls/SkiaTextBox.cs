using System.ComponentModel;
using System.Windows.Forms;
using SkiaTextBoxComponent = Beep.Skia.Components.TextBox;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia TextBox component on the Skia host")]
    [DisplayName("Skia TextBox")]
    public class SkiaTextBox : SkiaControl
    {
        private SkiaTextBoxComponent _textBox;

        public SkiaTextBox()
        {
            _textBox = CreateSkiaComponent<SkiaTextBoxComponent>();
            _textBox.Width = 160;
            _textBox.Height = 28;
            SkiaComponent = _textBox;
        }
    }
}

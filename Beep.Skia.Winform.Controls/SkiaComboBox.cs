using System.ComponentModel;
using System.Windows.Forms;
using SkiaComboBoxComponent = Beep.Skia.Components.Dropdown;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia Dropdown component on the Skia host")]
    [DisplayName("Skia Combo Box")]
    public class SkiaComboBox : SkiaControl
    {
        // Minimal wrapper: only construct the Skia Dropdown component on the host.
        private SkiaComboBoxComponent _comboBox;

        public SkiaComboBox()
        {
            _comboBox = CreateSkiaComponent<SkiaComboBoxComponent>();
            _comboBox.Width = 150;
            _comboBox.Height = 32;
            SkiaComponent = _comboBox;
        }

        // No forwarded properties — designer-only factory.
    }
}

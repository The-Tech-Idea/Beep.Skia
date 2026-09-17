using System.ComponentModel;
using System.Windows.Forms;
using SkiaTextAreaComponent = Beep.Skia.Components.TextArea;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Skia TextArea wrapper (minimal)")]
    [DisplayName("Skia TextArea")]
    public class SkiaTextArea : SkiaControl
    {
        private SkiaTextAreaComponent _ta;

        /// <summary>
        /// Initializes a new instance of the SkiaTextArea class.
        /// </summary>
        public SkiaTextArea()
        {
            _ta = CreateSkiaComponent<SkiaTextAreaComponent>();
            _ta.Width = 320;
            _ta.Height = 120;
            SkiaComponent = _ta;
        }
    }
}

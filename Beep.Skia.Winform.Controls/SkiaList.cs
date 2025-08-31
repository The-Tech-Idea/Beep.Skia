using System.ComponentModel;
using System.Windows.Forms;
using SkiaListComponent = Beep.Skia.Components.List;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia List component on the Skia host")]
    [DisplayName("Skia List")]
    public class SkiaList : SkiaControl
    {
        private SkiaListComponent _list;

        public SkiaList()
        {
            _list = CreateSkiaComponent<SkiaListComponent>();
            _list.Width = 200;
            _list.Height = 120;
            SkiaComponent = _list;
        }
    }
}

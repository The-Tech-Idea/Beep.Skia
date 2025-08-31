using System.ComponentModel;
using System.Windows.Forms;
using SkiaContextMenuComponent = Beep.Skia.Components.ContextMenu;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia ContextMenu component on the Skia host")]
    [DisplayName("Skia ContextMenu")]
    public class SkiaContextMenu : SkiaControl
    {
        private SkiaContextMenuComponent _menu;

        public SkiaContextMenu()
        {
            _menu = CreateSkiaComponent<SkiaContextMenuComponent>();
            _menu.Width = 160;
            _menu.Height = 100;
            SkiaComponent = _menu;
        }
    }
}

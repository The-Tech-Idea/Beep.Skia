using System.ComponentModel;
using System.Windows.Forms;
using SkiaFabComponent = Beep.Skia.Components.FloatingActionButton;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia FloatingActionButton component on the Skia host")]
    [DisplayName("Skia FloatingActionButton")]
    public class SkiaFloatingActionButton : SkiaControl
    {
        private SkiaFabComponent _fab;

        public SkiaFloatingActionButton()
        {
            _fab = CreateSkiaComponent<SkiaFabComponent>();
            _fab.Width = 56;
            _fab.Height = 56;
            SkiaComponent = _fab;
        }
    }
}

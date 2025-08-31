using System.ComponentModel;
using System.Windows.Forms;
using SkiaDataGridComponent = Beep.Skia.Components.DataGrid;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia DataGrid component on the Skia host")]
    [DisplayName("Skia DataGrid")]
    public class SkiaDataGrid : SkiaControl
    {
        private SkiaDataGridComponent _grid;

        public SkiaDataGrid()
        {
            _grid = CreateSkiaComponent<SkiaDataGridComponent>();
            _grid.Width = 300;
            _grid.Height = 200;
            SkiaComponent = _grid;
        }
    }
}

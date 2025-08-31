using System.ComponentModel;
using System.Windows.Forms;
using SkiaSearchComponent = Beep.Skia.Components.Search;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Skia Search wrapper (minimal)")]
    [DisplayName("Skia Search")]
    public class SkiaSearch : SkiaControl
    {
        private SkiaSearchComponent _search;

        public SkiaSearch()
        {
            _search = CreateSkiaComponent<SkiaSearchComponent>();
            _search.Width = 240;
            _search.Height = 32;
            SkiaComponent = _search;
        }
    }
}

using System.ComponentModel;
using System.Windows.Forms;
using SkiaCardComponent = Beep.Skia.Components.Card;
using Beep.Skia.Model;
namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Design-time wrapper that creates a Skia Card component on the Skia host")]
    [DisplayName("Skia Card")]
    public class SkiaCard : SkiaControl
    {
        private SkiaCardComponent _card;

        /// <summary>
        /// Initializes a new instance of the SkiaCard class.
        /// </summary>
        public SkiaCard()
        {
            _card = CreateSkiaComponent<SkiaCardComponent>();
            _card.Width = 200;
            _card.Height = 120;
            SkiaComponent = _card;
        }
    }
}

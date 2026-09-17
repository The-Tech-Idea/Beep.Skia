using SkiaSharp;
using Beep.Skia.Model;

namespace Beep.Skia.Business
{
    /// <summary>
    /// BPMN message flow — a dashed connection between participants/pools
    /// (exported as bpmn:messageFlow inside a collaboration).
    /// </summary>
    public class BpmnMessageFlow : ConnectionLine
    {
        private string _messageName = string.Empty;

        /// <summary>
        /// Optional message name (exported as the message flow name).
        /// </summary>
        public string MessageName
        {
            get => _messageName;
            set => _messageName = value ?? string.Empty;
        }

        public BpmnMessageFlow() : base(() => { })
        {
            DashPattern = new float[] { 6f, 4f };
            ShowStartArrow = false;
            ShowEndArrow = true;
            LineColor = new SKColor(0x61, 0x61, 0x61);
            Label1Placement = LabelPlacement.Over;
        }
    }
}

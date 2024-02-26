using SkiaSharp;

namespace Beep.Skia
{
    public interface IConnectionPoint
    {
        SKRect Bounds { get; set; }
        SKPoint Center { get; set; }
        ISkiaWorkFlowComponent Component { get; set; }
        IConnectionPoint Connection { get; set; }
        int Index { get; set; }
        bool IsAvailable { get; set; }
        SKPoint Offset { get; set; }
        SKPoint Position { get; set; }
        int Radius { get; set; }
        SKRect Rect { get; set; }
        ComponentShape Shape { get; set; }
        ConnectionPointType Type { get; set; }

        event EventHandler<ConnectionEventArgs> ConnectionMade;

        void ConnectTo(IConnectionPoint target);
        void Disconnect();
    }
}
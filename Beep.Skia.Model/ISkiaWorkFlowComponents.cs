using SkiaSharp;

namespace Beep.Skia
{
    public interface ISkiaWorkFlowComponent
    {
        event EventHandler<ConnectionEventArgs> Connected;
        event EventHandler<ConnectionEventArgs> Disconnected;
        ISkiaWorkFlowComponent StartComponent { get; set; }
        ISkiaWorkFlowComponent EndComponent { get; set; }
        void ConnectTo(ISkiaWorkFlowComponent otherComponent);
        void DisconnectFrom(ISkiaWorkFlowComponent otherComponent);
        List<ISkiaWorkFlowComponent> ConnectedComponents { get; set; }
        IConnectionPoint[] InConnectionPoints { get; }
        bool IsRunning { get; set; }
        string Name { get; set; }
        SKRect Bounds { get; }
        IConnectionPoint[] OutConnectionPoints { get; }
        float Radius { get; set; }
        float X { get; set; }
        float Y { get; set; }
        bool Intersects(ISkiaWorkFlowComponent other);
        void Animate();
        bool Click(SKPoint point);
        void Draw(SKCanvas canvas);
        bool HitTest(SKPoint point);
        void Move(SKPoint offset);
        void MouseDown(SKPoint point);
         void MouseUp(SKPoint point);
        void MouseMove(SKPoint point);
        bool IsConnectedTo(ISkiaWorkFlowComponent otherComponent);
        // Event raised when the component's bounds change
        event EventHandler<SKRectEventArgs> BoundsChanged;
        event EventHandler<EventArgs> Clicked;

    }
    public class SKRectEventArgs : EventArgs
    {
        public SKRect Bounds { get; private set; }

        public SKRectEventArgs(SKRect bounds)
        {
            Bounds = bounds;
        }
    }
}
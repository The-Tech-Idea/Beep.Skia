using SkiaSharp;

namespace Beep.Skia
{
    public interface IConnectionLine
    {
        IConnectionPoint End { get; set; }
        SKPoint EndPoint { get; set; }
        bool IsAnimated { get; set; }
        bool IsConnectd { get; set; }
        bool IsSelected { get; set; }
        string Label1 { get; set; }
        string Label2 { get; set; }
        string Label3 { get; set; }
        SKColor LineColor { get; set; }
        SKPaint Paint { get; set; }
        bool ShowEndArrow { get; set; }
        bool ShowStartArrow { get; set; }
        IConnectionPoint Start { get; set; }

        event EventHandler<ConnectionArgs> EndArrowClicked;
        event EventHandler<LineArgs> LineClicked;
        event EventHandler<ConnectionArgs> StartArrowClicked;

        bool ArrowContainsPoint(SKPoint point, bool startArrow);
        void Draw(SKCanvas canvas);
        bool LineContainsPoint(SKPoint point);
        void OnClick(SKPoint clickPoint);
        void StartAnimation();
        void StopAnimation();
    }
}
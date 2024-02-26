

namespace Beep.Skia
{
    public class PointEventArgs : EventArgs
    {
        public float X { get; set; }
        public float Y { get; set; }
        public bool IsRightButton { get; set; }
    }

}

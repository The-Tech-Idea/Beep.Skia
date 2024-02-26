using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beep.Skia
{
    public class ConnectionPoint : IConnectionPoint
    {
        public ConnectionPoint()
        {

        }
        public bool IsAvailable { get; set; } = true;
        public SKPoint Position { get; set; }
        public SKPoint Offset { get; set; }
        public SKPoint Center { get; set; }
        public SKRect Rect { get; set; }
        public int Radius { get; set; }
        public int Index { get; set; }
        public SKRect Bounds { get; set; }
        public ComponentShape Shape { get; set; } = ComponentShape.Circle;
        public ISkiaWorkFlowComponent Component { get; set; }
        public IConnectionPoint Connection { get; set; }
        public ConnectionPointType Type { get; set; }
        public event EventHandler<ConnectionEventArgs> ConnectionMade;
        public void ConnectTo(IConnectionPoint target)
        {
            ConnectionMade?.Invoke(this, new ConnectionEventArgs(this, target));
            Connection = target;
            this.IsAvailable = false;
            target.IsAvailable = false;
        }

        public void Disconnect()
        {
            this.IsAvailable = true;
            Connection.Disconnect();
        }
    }

   

}

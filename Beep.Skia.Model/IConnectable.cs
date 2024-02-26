using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beep.Skia
{

    public class ConnectionEventArgs : EventArgs
    {
        public IConnectionPoint SourceIConnectionPoint { get; private set; }
        public IConnectionPoint TargetIConnectionPoint { get; private set; }
     

        public ConnectionEventArgs(IConnectionPoint sourceIConnectionPoint, IConnectionPoint targetIConnectionPoint)
        {
            SourceIConnectionPoint = sourceIConnectionPoint;
            TargetIConnectionPoint = targetIConnectionPoint;
           
        }
    }
    public class ConnectionArgs : EventArgs
    {
        public IConnectionPoint IconnectionPoint { get; private set; }
       


        public ConnectionArgs(IConnectionPoint iConnectionPoint)
        {
            IconnectionPoint = iConnectionPoint;
            

        }
    }
    public class LineArgs : EventArgs
    {
        public IConnectionPoint SourceIConnectionPoint { get; private set; }
        public IConnectionPoint TargetIConnectionPoint { get; private set; }
        public IConnectionLine ConnectionLine { get; private set; }

        public LineArgs(IConnectionPoint sourceIConnectionPoint, IConnectionPoint targetIConnectionPoint, IConnectionLine connectionLine)
        {
            SourceIConnectionPoint = sourceIConnectionPoint;
            TargetIConnectionPoint = targetIConnectionPoint;
            ConnectionLine = connectionLine;

        }
    }
}

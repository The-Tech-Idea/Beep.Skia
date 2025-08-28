using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beep.Skia
{
    /// <summary>
    /// Specifies the shape of a workflow component.
    /// </summary>
    public enum ComponentShape
    {
        /// <summary>
        /// A circular component shape.
        /// </summary>
        Circle,

        /// <summary>
        /// A square component shape.
        /// </summary>
        Square,

        /// <summary>
        /// A triangular component shape.
        /// </summary>
        Triangle,

        /// <summary>
        /// A table-shaped component.
        /// </summary>
        Table,

        /// <summary>
        /// A diamond-shaped component.
        /// </summary>
        Diamond,

        /// <summary>
        /// A line-shaped component.
        /// </summary>
        Line,
    }

    /// <summary>
    /// Specifies the type of a connection point (input or output).
    /// </summary>
    public enum ConnectionPointType
    {
        /// <summary>
        /// An input connection point that receives connections.
        /// </summary>
        In,

        /// <summary>
        /// An output connection point that initiates connections.
        /// </summary>
        Out
    }
}

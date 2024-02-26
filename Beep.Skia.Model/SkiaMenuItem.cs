using System;
using System.Collections.Generic;
using System.Text;

namespace Beep.Skia.Model
{
    public class SkiaMenuItem
    {
        public string menutext { get; set; }
        public string icontext { get; set; }
        public string forcolor { get; set; }
        public string backcolor { get; set; }
        public Action functiontoexecute { get; set; }
    }
}

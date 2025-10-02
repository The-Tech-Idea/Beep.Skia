using SkiaSharp;
using System;
using Beep.Skia.Model;
namespace Beep.Skia
{
    /// <summary>
    /// Legacy drop args, kept for source-compat only. Do not use.
    /// </summary>
    [Obsolete("Use Beep.Skia.Events.ComponentDropEventArgs (with Canvas/Screen positions) instead.", true)]
    internal class LegacyComponentDropEventArgs : EventArgs
    {
        public SkiaComponent Component { get; }
        public SKPoint Location { get; }

        public LegacyComponentDropEventArgs(SkiaComponent component, SKPoint location)
        {
            Component = component;
            Location = location;
        }
    }
}

using System;
using System.ComponentModel;
using System.Windows.Forms;
using Beep.Skia;

namespace Beep.Skia.Winform.Controls
{
    // Minimal WinForms wrapper base for Beep.Skia components.
    // Purpose: allow WinForms wrappers to create and hold a Skia component
    // without pulling in a large host implementation. Keep it small and safe.
    public class SkiaControl : UserControl
    {
        // Underlying Skia component instance (may be null in designer)
        [Browsable(false)]
        public SkiaComponent SkiaComponent { get; protected set; }

        public SkiaControl()
        {
            // Default control styles for a lightweight wrapper
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.AutoScaleMode = AutoScaleMode.None;
        }

        // Create a Skia component instance and assign it to the wrapper.
        // T must be a concrete Beep.Skia component type with a public parameterless ctor.
        protected T CreateSkiaComponent<T>() where T : SkiaComponent, new()
        {
            var comp = new T();
            SkiaComponent = comp;
            return comp;
        }

    // Request an update/redraw on the Skia component if available.
    // Uses the public InvalidateVisual() when present.
    protected virtual void UpdateComponent()
        {
            try
            {
                // Prefer calling the strongly-typed method if available
                var mc = SkiaComponent as dynamic;
                mc?.InvalidateVisual();
            }
            catch
            {
                // Swallow any runtime exceptions during design-time builds.
            }
        }
    }
}

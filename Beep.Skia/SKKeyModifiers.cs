using System;
using Beep.Skia.Model;
namespace Beep.Skia
{
    /// <summary>
    /// Specifies the state of modifier keys on the keyboard.
    /// </summary>
    [Flags]
    public enum SKKeyModifiers
    {
        /// <summary>
        /// No modifier keys are pressed.
        /// </summary>
        None = 0,

        /// <summary>
        /// The Control key is pressed.
        /// </summary>
        Control = 1 << 0,

        /// <summary>
        /// The Shift key is pressed.
        /// </summary>
        Shift = 1 << 1,

        /// <summary>
        /// The Alt key is pressed.
        /// </summary>
        Alt = 1 << 2,

        /// <summary>
        /// The Windows key (or Command key on Mac) is pressed.
        /// </summary>
        Meta = 1 << 3
    }
}

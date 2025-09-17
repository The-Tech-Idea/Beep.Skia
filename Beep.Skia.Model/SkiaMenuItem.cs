using System;
using System.Collections.Generic;
using System.Text;

namespace Beep.Skia.Model
{
    /// <summary>
    /// Represents a menu item for Skia-based user interfaces.
    /// </summary>
    public class SkiaMenuItem
    {
        /// <summary>
        /// Gets or sets the text displayed for this menu item.
        /// </summary>
        public string MenuText { get; set; }

        /// <summary>
        /// Gets or sets the icon text or symbol for this menu item.
        /// </summary>
        public string IconText { get; set; }

        /// <summary>
        /// Gets or sets the foreground color of this menu item.
        /// </summary>
        public string ForeColor { get; set; }

        /// <summary>
        /// Gets or sets the background color of this menu item.
        /// </summary>
        public string BackColor { get; set; }

        /// <summary>
        /// Gets or sets the action to execute when this menu item is selected.
        /// </summary>
        public Action FunctionToExecute { get; set; }
    }
}

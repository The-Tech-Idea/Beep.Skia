using System.Collections.Generic;

namespace Beep.Skia
{
    /// <summary>
    /// Built-in SVG icon library for Beep.Skia components.
    /// Each icon is stored as an SVG path string suitable for use with SvgImage or MaterialControl.
    /// These match the most common diagramming and UI icon needs.
    /// </summary>
    public static class BuiltinIcons
    {
        /// <summary>
        /// Gets a built-in SVG icon by name. Returns null if not found.
        /// </summary>
        public static string GetSvg(string name)
        {
            _all.TryGetValue(name?.ToLowerInvariant() ?? "", out var svg);
            return svg;
        }

        /// <summary>
        /// All available icon names.
        /// </summary>
        public static IEnumerable<string> Names => _all.Keys;

        private static readonly Dictionary<string, string> _all = new()
        {
            // ── Data ────────────────────────────────────────────────
            ["database"] = "<svg viewBox='0 0 24 24'><ellipse cx='12' cy='5' rx='8' ry='3' fill='none' stroke='currentColor' stroke-width='2'/><path d='M4 5v14c0 1.66 3.58 3 8 3s8-1.34 8-3V5' fill='none' stroke='currentColor' stroke-width='2'/><path d='M4 12c0 1.66 3.58 3 8 3s8-1.34 8-3' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
            ["table"] = "<svg viewBox='0 0 24 24'><rect x='3' y='3' width='18' height='18' rx='2' fill='none' stroke='currentColor' stroke-width='2'/><line x1='3' y1='9' x2='21' y2='9' stroke='currentColor' stroke-width='2'/><line x1='12' y1='9' x2='12' y2='21' stroke='currentColor' stroke-width='2'/></svg>",
            ["server"] = "<svg viewBox='0 0 24 24'><rect x='2' y='2' width='20' height='8' rx='2' fill='none' stroke='currentColor' stroke-width='2'/><rect x='2' y='14' width='20' height='8' rx='2' fill='none' stroke='currentColor' stroke-width='2'/><circle cx='6' cy='6' r='1' fill='currentColor'/><circle cx='6' cy='18' r='1' fill='currentColor'/></svg>",
            ["cloud"] = "<svg viewBox='0 0 24 24'><path d='M18 10h-1.26A8 8 0 1 0 9 20h9a5 5 0 0 0 0-10z' fill='none' stroke='currentColor' stroke-width='2'/></svg>",

            // ── People ──────────────────────────────────────────────
            ["user"] = "<svg viewBox='0 0 24 24'><circle cx='12' cy='8' r='4' fill='none' stroke='currentColor' stroke-width='2'/><path d='M4 20c0-4 4-7 8-7s8 3 8 7' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
            ["users"] = "<svg viewBox='0 0 24 24'><circle cx='9' cy='7' r='4' fill='none' stroke='currentColor' stroke-width='2'/><path d='M1 20c0-3 3-6 8-6s8 3 8 6' fill='none' stroke='currentColor' stroke-width='2'/><circle cx='17' cy='7' r='3' fill='none' stroke='currentColor' stroke-width='2'/><path d='M15 14c3 0 5 2 5 4' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
            ["person"] = "<svg viewBox='0 0 24 24'><circle cx='12' cy='8' r='4' fill='none' stroke='currentColor' stroke-width='2'/><path d='M4 20c0-4 4-7 8-7s8 3 8 7' fill='none' stroke='currentColor' stroke-width='2'/></svg>",

            // ── Navigation ──────────────────────────────────────────
            ["search"] = "<svg viewBox='0 0 24 24'><circle cx='11' cy='11' r='8' fill='none' stroke='currentColor' stroke-width='2'/><line x1='21' y1='21' x2='16.65' y2='16.65' stroke='currentColor' stroke-width='2'/></svg>",
            ["settings"] = "<svg viewBox='0 0 24 24'><circle cx='12' cy='12' r='3' fill='none' stroke='currentColor' stroke-width='2'/><path d='M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
            ["menu"] = "<svg viewBox='0 0 24 24'><line x1='3' y1='6' x2='21' y2='6' stroke='currentColor' stroke-width='2'/><line x1='3' y1='12' x2='21' y2='12' stroke='currentColor' stroke-width='2'/><line x1='3' y1='18' x2='21' y2='18' stroke='currentColor' stroke-width='2'/></svg>",
            ["home"] = "<svg viewBox='0 0 24 24'><path d='M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z' fill='none' stroke='currentColor' stroke-width='2'/><polyline points='9 22 9 12 15 12 15 22' fill='none' stroke='currentColor' stroke-width='2'/></svg>",

            // ── Actions ─────────────────────────────────────────────
            ["plus"] = "<svg viewBox='0 0 24 24'><line x1='12' y1='5' x2='12' y2='19' stroke='currentColor' stroke-width='2'/><line x1='5' y1='12' x2='19' y2='12' stroke='currentColor' stroke-width='2'/></svg>",
            ["minus"] = "<svg viewBox='0 0 24 24'><line x1='5' y1='12' x2='19' y2='12' stroke='currentColor' stroke-width='2'/></svg>",
            ["check"] = "<svg viewBox='0 0 24 24'><polyline points='20 6 9 17 4 12' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
            ["x"] = "<svg viewBox='0 0 24 24'><line x1='18' y1='6' x2='6' y2='18' stroke='currentColor' stroke-width='2'/><line x1='6' y1='6' x2='18' y2='18' stroke='currentColor' stroke-width='2'/></svg>",
            ["edit"] = "<svg viewBox='0 0 24 24'><path d='M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7' fill='none' stroke='currentColor' stroke-width='2'/><path d='M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
            ["trash"] = "<svg viewBox='0 0 24 24'><polyline points='3 6 5 6 21 6' fill='none' stroke='currentColor' stroke-width='2'/><path d='M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
            ["copy"] = "<svg viewBox='0 0 24 24'><rect x='9' y='9' width='13' height='13' rx='2' fill='none' stroke='currentColor' stroke-width='2'/><path d='M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
            ["save"] = "<svg viewBox='0 0 24 24'><path d='M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z' fill='none' stroke='currentColor' stroke-width='2'/><polyline points='17 21 17 13 7 13 7 21' fill='none' stroke='currentColor' stroke-width='2'/><polyline points='7 3 7 8 15 8' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
            ["download"] = "<svg viewBox='0 0 24 24'><path d='M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4' fill='none' stroke='currentColor' stroke-width='2'/><polyline points='7 10 12 15 17 10' fill='none' stroke='currentColor' stroke-width='2'/><line x1='12' y1='15' x2='12' y2='3' stroke='currentColor' stroke-width='2'/></svg>",
            ["print"] = "<svg viewBox='0 0 24 24'><polyline points='6 9 6 2 18 2 18 9' fill='none' stroke='currentColor' stroke-width='2'/><path d='M6 12H2a1 1 0 0 0-1 1v5a1 1 0 0 0 1 1h3' fill='none' stroke='currentColor' stroke-width='2'/><path d='M18 12h4a1 1 0 0 1 1 1v5a1 1 0 0 1-1 1h-3' fill='none' stroke='currentColor' stroke-width='2'/><rect x='6' y='14' width='12' height='8' fill='none' stroke='currentColor' stroke-width='2'/></svg>",

            // ── Diagram ─────────────────────────────────────────────
            ["diagram"] = "<svg viewBox='0 0 24 24'><rect x='3' y='3' width='7' height='7' rx='1' fill='none' stroke='currentColor' stroke-width='2'/><rect x='14' y='3' width='7' height='7' rx='1' fill='none' stroke='currentColor' stroke-width='2'/><rect x='3' y='14' width='7' height='7' rx='1' fill='none' stroke='currentColor' stroke-width='2'/><rect x='14' y='14' width='7' height='7' rx='1' fill='none' stroke='currentColor' stroke-width='2'/><line x1='10' y1='6.5' x2='14' y2='6.5' stroke='currentColor' stroke-width='2'/><line x1='10' y1='17.5' x2='14' y2='17.5' stroke='currentColor' stroke-width='2'/></svg>",
            ["connection"] = "<svg viewBox='0 0 24 24'><polyline points='22 12 18 12 15 15 9 9 6 12 2 12' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
            ["arrow-right"] = "<svg viewBox='0 0 24 24'><line x1='5' y1='12' x2='19' y2='12' stroke='currentColor' stroke-width='2'/><polyline points='12 5 19 12 12 19' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
            ["arrow-left"] = "<svg viewBox='0 0 24 24'><line x1='19' y1='12' x2='5' y2='12' stroke='currentColor' stroke-width='2'/><polyline points='12 19 5 12 12 5' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
            ["zoom-in"] = "<svg viewBox='0 0 24 24'><circle cx='11' cy='11' r='8' fill='none' stroke='currentColor' stroke-width='2'/><line x1='21' y1='21' x2='16.65' y2='16.65' stroke='currentColor' stroke-width='2'/><line x1='11' y1='8' x2='11' y2='14' stroke='currentColor' stroke-width='2'/><line x1='8' y1='11' x2='14' y2='11' stroke='currentColor' stroke-width='2'/></svg>",
            ["zoom-out"] = "<svg viewBox='0 0 24 24'><circle cx='11' cy='11' r='8' fill='none' stroke='currentColor' stroke-width='2'/><line x1='21' y1='21' x2='16.65' y2='16.65' stroke='currentColor' stroke-width='2'/><line x1='8' y1='11' x2='14' y2='11' stroke='currentColor' stroke-width='2'/></svg>",

            // ── Status ──────────────────────────────────────────────
            ["info"] = "<svg viewBox='0 0 24 24'><circle cx='12' cy='12' r='10' fill='none' stroke='currentColor' stroke-width='2'/><line x1='12' y1='16' x2='12' y2='12' stroke='currentColor' stroke-width='2'/><line x1='12' y1='8' x2='12.01' y2='8' stroke='currentColor' stroke-width='2'/></svg>",
            ["warning"] = "<svg viewBox='0 0 24 24'><path d='M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z' fill='none' stroke='currentColor' stroke-width='2'/><line x1='12' y1='9' x2='12' y2='13' stroke='currentColor' stroke-width='2'/><line x1='12' y1='17' x2='12.01' y2='17' stroke='currentColor' stroke-width='2'/></svg>",
            ["error"] = "<svg viewBox='0 0 24 24'><circle cx='12' cy='12' r='10' fill='none' stroke='currentColor' stroke-width='2'/><line x1='15' y1='9' x2='9' y2='15' stroke='currentColor' stroke-width='2'/><line x1='9' y1='9' x2='15' y2='15' stroke='currentColor' stroke-width='2'/></svg>",
            ["success"] = "<svg viewBox='0 0 24 24'><circle cx='12' cy='12' r='10' fill='none' stroke='currentColor' stroke-width='2'/><polyline points='16 10 11 15 8 12' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
            ["lock"] = "<svg viewBox='0 0 24 24'><rect x='3' y='11' width='18' height='11' rx='2' fill='none' stroke='currentColor' stroke-width='2'/><path d='M7 11V7a5 5 0 0 1 10 0v4' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
            ["unlock"] = "<svg viewBox='0 0 24 24'><rect x='3' y='11' width='18' height='11' rx='2' fill='none' stroke='currentColor' stroke-width='2'/><path d='M7 11V7a5 5 0 0 1 9.9-1' fill='none' stroke='currentColor' stroke-width='2'/></svg>",

            // ── Misc ────────────────────────────────────────────────
            ["clock"] = "<svg viewBox='0 0 24 24'><circle cx='12' cy='12' r='10' fill='none' stroke='currentColor' stroke-width='2'/><polyline points='12 6 12 12 16 14' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
            ["calendar"] = "<svg viewBox='0 0 24 24'><rect x='3' y='4' width='18' height='18' rx='2' fill='none' stroke='currentColor' stroke-width='2'/><line x1='16' y1='2' x2='16' y2='6' stroke='currentColor' stroke-width='2'/><line x1='8' y1='2' x2='8' y2='6' stroke='currentColor' stroke-width='2'/><line x1='3' y1='10' x2='21' y2='10' stroke='currentColor' stroke-width='2'/></svg>",
            ["mail"] = "<svg viewBox='0 0 24 24'><rect x='2' y='4' width='20' height='16' rx='2' fill='none' stroke='currentColor' stroke-width='2'/><polyline points='2 4 12 13 22 4' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
            ["phone"] = "<svg viewBox='0 0 24 24'><path d='M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72c.127.96.361 1.903.7 2.81a2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45c.907.339 1.85.573 2.81.7A2 2 0 0 1 22 16.92z' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
            ["file"] = "<svg viewBox='0 0 24 24'><path d='M13 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z' fill='none' stroke='currentColor' stroke-width='2'/><polyline points='13 2 13 9 20 9' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
            ["folder"] = "<svg viewBox='0 0 24 24'><path d='M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
            ["link"] = "<svg viewBox='0 0 24 24'><path d='M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71' fill='none' stroke='currentColor' stroke-width='2'/><path d='M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
            ["star"] = "<svg viewBox='0 0 24 24'><polygon points='12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
            ["heart"] = "<svg viewBox='0 0 24 24'><path d='M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z' fill='none' stroke='currentColor' stroke-width='2'/></svg>",
        };
    }
}

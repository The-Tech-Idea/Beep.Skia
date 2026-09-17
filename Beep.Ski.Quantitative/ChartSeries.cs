using System.Collections.Generic;
using System.Text.Json.Serialization;
using SkiaSharp;

namespace Beep.Ski.Quantitative
{
    /// <summary>
    /// A named data series for chart nodes.
    /// </summary>
    public class ChartSeries
    {
        /// <summary>Series display name (used by the legend).</summary>
        public string Name { get; set; } = "Series";

        /// <summary>Series color as an ARGB uint (JSON-friendly).</summary>
        public uint ColorArgb { get; set; } = 0xFF1E88E5;

        /// <summary>Series values in order.</summary>
        public List<double> Values { get; set; } = new List<double>();

        /// <summary>Gets the series color as an SKColor.</summary>
        [JsonIgnore]
        public SKColor Color => new SKColor(ColorArgb);

        /// <summary>Sets the series color from an SKColor.</summary>
        public ChartSeries WithColor(SKColor color)
        {
            ColorArgb = (uint)color;
            return this;
        }

        /// <summary>Appends a value.</summary>
        public ChartSeries Add(double value)
        {
            Values.Add(value);
            return this;
        }
    }
}

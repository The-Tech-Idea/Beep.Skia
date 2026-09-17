using SkiaSharp;
using System.Collections.Generic;

namespace Beep.Skia.WellLogs
{
    public enum WellLogScaleType
    {
        Linear,
        Log10
    }

    public enum WellLogTrackRole
    {
        Depth,
        Lithology,
        Curve,
        Overlay,
        Annotation
    }

    public enum WellLogFillMode
    {
        None,
        LeftToCurve,
        RightToCurve,
        BetweenCurves,
        CutoffShading,
        LithologyBands
    }

    public enum WellLogBrushPattern
    {
        Solid,
        Horizontal,
        DiagonalLeft,
        DiagonalRight,
        CrossHatch,
        Dots
    }

    public enum WellLogDataStandard
    {
        Las20,
        Las30,
        DlisRp66V1,
        DlisRp66V2,
        Lis79,
        Csv,
        Json
    }

    /// <summary>
    /// Gets or sets the well log depth axis.
    /// </summary>
    public sealed class WellLogDepthAxis
    {
        /// <summary>
        /// Gets or sets the minimum depth.
        /// </summary>
        public float MinimumDepth { get; set; } = 0f;
        /// <summary>
        /// Gets or sets the maximum depth.
        /// </summary>
        public float MaximumDepth { get; set; } = 3000f;
        /// <summary>
        /// Gets or sets the major step.
        /// </summary>
        public float MajorStep { get; set; } = 10f;
        /// <summary>
        /// Gets or sets the minor step.
        /// </summary>
        public float MinorStep { get; set; } = 2f;
        /// <summary>
        /// Gets or sets the is increasing down.
        /// </summary>
        public bool IsIncreasingDown { get; set; } = true;
        /// <summary>
        /// Gets or sets the depth span.
        /// </summary>
        public float DepthSpan => MaximumDepth - MinimumDepth;
    }

    /// <summary>
    /// Gets or sets the well log curve sample.
    /// </summary>
    public sealed class WellLogCurveSample
    {
        /// <summary>
        /// Gets or sets the depth.
        /// </summary>
        public float Depth { get; set; }
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public float Value { get; set; }
    }

    /// <summary>
    /// Gets or sets the well log curve style.
    /// </summary>
    public sealed class WellLogCurveStyle
    {
        /// <summary>
        /// Gets or sets the line color.
        /// </summary>
        public SKColor LineColor { get; set; } = SKColors.Black;
        /// <summary>
        /// Gets or sets the stroke width.
        /// </summary>
        public float StrokeWidth { get; set; } = 1.75f;
        /// <summary>
        /// Gets or sets the brush key.
        /// </summary>
        public string BrushKey { get; set; } = "default";
        /// <summary>
        /// Gets or sets the fill mode.
        /// </summary>
        public WellLogFillMode FillMode { get; set; } = WellLogFillMode.None;
        /// <summary>
        /// Gets or sets the companion curve mnemonic.
        /// </summary>
        public string CompanionCurveMnemonic { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the fill reference.
        /// </summary>
        public float? FillReference { get; set; }
    }

    /// <summary>
    /// Gets or sets the well log curve.
    /// </summary>
    public sealed class WellLogCurve
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = "Curve";
        /// <summary>
        /// Gets or sets the mnemonic.
        /// </summary>
        public string Mnemonic { get; set; } = "CURVE";
        /// <summary>
        /// Gets or sets the unit.
        /// </summary>
        public string Unit { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the minimum value.
        /// </summary>
        public float MinimumValue { get; set; } = 0f;
        /// <summary>
        /// Gets or sets the maximum value.
        /// </summary>
        public float MaximumValue { get; set; } = 100f;
        /// <summary>
        /// Gets or sets the is visible.
        /// </summary>
        public bool IsVisible { get; set; } = true;
        /// <summary>
        /// Gets or sets the scale type.
        /// </summary>
        public WellLogScaleType ScaleType { get; set; } = WellLogScaleType.Linear;
        /// <summary>
        /// Gets or sets the style.
        /// </summary>
        public WellLogCurveStyle Style { get; set; } = new WellLogCurveStyle();
        /// <summary>
        /// Gets or sets the samples.
        /// </summary>
        public List<WellLogCurveSample> Samples { get; } = new List<WellLogCurveSample>();
    }

    /// <summary>
    /// Gets or sets the well log lithology interval.
    /// </summary>
    public sealed class WellLogLithologyInterval
    {
        /// <summary>
        /// Gets or sets the top depth.
        /// </summary>
        public float TopDepth { get; set; }
        /// <summary>
        /// Gets or sets the bottom depth.
        /// </summary>
        public float BottomDepth { get; set; }
        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        public string Label { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the brush key.
        /// </summary>
        public string BrushKey { get; set; } = "sand";
    }

    /// <summary>
    /// Gets or sets the well log track.
    /// </summary>
    public sealed class WellLogTrack
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = "Track";
        /// <summary>
        /// Gets or sets the role.
        /// </summary>
        public WellLogTrackRole Role { get; set; } = WellLogTrackRole.Curve;
        /// <summary>
        /// Gets or sets the width ratio.
        /// </summary>
        public float WidthRatio { get; set; } = 1f;
        /// <summary>
        /// Gets or sets the show grid.
        /// </summary>
        public bool ShowGrid { get; set; } = true;
        /// <summary>
        /// Gets or sets the show header.
        /// </summary>
        public bool ShowHeader { get; set; } = true;
        /// <summary>
        /// Gets or sets the header height.
        /// </summary>
        public float HeaderHeight { get; set; } = 28f;
        /// <summary>
        /// Gets or sets the left margin.
        /// </summary>
        public float LeftMargin { get; set; } = 8f;
        /// <summary>
        /// Gets or sets the right margin.
        /// </summary>
        public float RightMargin { get; set; } = 8f;
        /// <summary>
        /// Gets or sets the background color.
        /// </summary>
        public SKColor BackgroundColor { get; set; } = new SKColor(248, 249, 252);
        /// <summary>
        /// Gets or sets the grid color.
        /// </summary>
        public SKColor GridColor { get; set; } = new SKColor(200, 206, 216);
        /// <summary>
        /// Gets or sets the curves.
        /// </summary>
        public List<WellLogCurve> Curves { get; } = new List<WellLogCurve>();
        /// <summary>
        /// Gets or sets the lithology intervals.
        /// </summary>
        public List<WellLogLithologyInterval> LithologyIntervals { get; } = new List<WellLogLithologyInterval>();
    }

    /// <summary>
    /// Gets or sets the well log document.
    /// </summary>
    public sealed class WellLogDocument
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = "Well Log";
        /// <summary>
        /// Gets or sets the source reference.
        /// </summary>
        public string SourceReference { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the primary standard.
        /// </summary>
        public WellLogDataStandard PrimaryStandard { get; set; } = WellLogDataStandard.Las30;
        /// <summary>
        /// Gets or sets the depth axis.
        /// </summary>
        public WellLogDepthAxis DepthAxis { get; set; } = new WellLogDepthAxis();
        /// <summary>
        /// Gets or sets the supported standards.
        /// </summary>
        public List<WellLogDataStandard> SupportedStandards { get; } = new List<WellLogDataStandard>();
        /// <summary>
        /// Gets or sets the tracks.
        /// </summary>
        public List<WellLogTrack> Tracks { get; } = new List<WellLogTrack>();
    }
}

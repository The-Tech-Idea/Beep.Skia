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

    public sealed class WellLogDepthAxis
    {
        public float MinimumDepth { get; set; } = 0f;
        public float MaximumDepth { get; set; } = 3000f;
        public float MajorStep { get; set; } = 10f;
        public float MinorStep { get; set; } = 2f;
        public bool IsIncreasingDown { get; set; } = true;
        public float DepthSpan => MaximumDepth - MinimumDepth;
    }

    public sealed class WellLogCurveSample
    {
        public float Depth { get; set; }
        public float Value { get; set; }
    }

    public sealed class WellLogCurveStyle
    {
        public SKColor LineColor { get; set; } = SKColors.Black;
        public float StrokeWidth { get; set; } = 1.75f;
        public string BrushKey { get; set; } = "default";
        public WellLogFillMode FillMode { get; set; } = WellLogFillMode.None;
        public string CompanionCurveMnemonic { get; set; } = string.Empty;
        public float? FillReference { get; set; }
    }

    public sealed class WellLogCurve
    {
        public string Name { get; set; } = "Curve";
        public string Mnemonic { get; set; } = "CURVE";
        public string Unit { get; set; } = string.Empty;
        public float MinimumValue { get; set; } = 0f;
        public float MaximumValue { get; set; } = 100f;
        public bool IsVisible { get; set; } = true;
        public WellLogScaleType ScaleType { get; set; } = WellLogScaleType.Linear;
        public WellLogCurveStyle Style { get; set; } = new WellLogCurveStyle();
        public List<WellLogCurveSample> Samples { get; } = new List<WellLogCurveSample>();
    }

    public sealed class WellLogLithologyInterval
    {
        public float TopDepth { get; set; }
        public float BottomDepth { get; set; }
        public string Label { get; set; } = string.Empty;
        public string BrushKey { get; set; } = "sand";
    }

    public sealed class WellLogTrack
    {
        public string Name { get; set; } = "Track";
        public WellLogTrackRole Role { get; set; } = WellLogTrackRole.Curve;
        public float WidthRatio { get; set; } = 1f;
        public bool ShowGrid { get; set; } = true;
        public bool ShowHeader { get; set; } = true;
        public float HeaderHeight { get; set; } = 28f;
        public float LeftMargin { get; set; } = 8f;
        public float RightMargin { get; set; } = 8f;
        public SKColor BackgroundColor { get; set; } = new SKColor(248, 249, 252);
        public SKColor GridColor { get; set; } = new SKColor(200, 206, 216);
        public List<WellLogCurve> Curves { get; } = new List<WellLogCurve>();
        public List<WellLogLithologyInterval> LithologyIntervals { get; } = new List<WellLogLithologyInterval>();
    }

    public sealed class WellLogDocument
    {
        public string Name { get; set; } = "Well Log";
        public string SourceReference { get; set; } = string.Empty;
        public WellLogDataStandard PrimaryStandard { get; set; } = WellLogDataStandard.Las30;
        public WellLogDepthAxis DepthAxis { get; set; } = new WellLogDepthAxis();
        public List<WellLogDataStandard> SupportedStandards { get; } = new List<WellLogDataStandard>();
        public List<WellLogTrack> Tracks { get; } = new List<WellLogTrack>();
    }
}
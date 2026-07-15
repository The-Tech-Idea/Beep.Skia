using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia.WellLogs
{
    public sealed class WellLogTrackLayout
    {
        public WellLogTrackLayout(WellLogTrack track, SKRect trackBounds, SKRect headerBounds, SKRect plotBounds)
        {
            Track = track;
            TrackBounds = trackBounds;
            HeaderBounds = headerBounds;
            PlotBounds = plotBounds;
        }

        public WellLogTrack Track { get; }
        public SKRect TrackBounds { get; }
        public SKRect HeaderBounds { get; }
        public SKRect PlotBounds { get; }
    }

    public static class WellLogLayoutEngine
    {
        public static IReadOnlyList<WellLogTrackLayout> BuildTrackLayouts(SKRect bounds, WellLogDocument document, float trackGap = 6f, float defaultHeaderHeight = 28f)
        {
            if (document == null || document.Tracks.Count == 0)
            {
                return Array.Empty<WellLogTrackLayout>();
            }

            var tracks = document.Tracks;
            var totalGap = Math.Max(0, tracks.Count - 1) * trackGap;
            var contentWidth = Math.Max(1f, bounds.Width - totalGap);
            var totalRatio = tracks.Sum(track => Math.Max(0.0001f, track.WidthRatio));
            var layouts = new List<WellLogTrackLayout>(tracks.Count);
            var currentLeft = bounds.Left;

            foreach (var track in tracks)
            {
                var width = contentWidth * (Math.Max(0.0001f, track.WidthRatio) / totalRatio);
                var trackBounds = new SKRect(currentLeft, bounds.Top, currentLeft + width, bounds.Bottom);
                var headerHeight = track.ShowHeader ? Math.Max(track.HeaderHeight, defaultHeaderHeight) : 0f;
                var headerBounds = new SKRect(trackBounds.Left, trackBounds.Top, trackBounds.Right, trackBounds.Top + headerHeight);
                var plotTop = track.ShowHeader ? headerBounds.Bottom + 4f : trackBounds.Top + 4f;
                var plotBounds = new SKRect(
                    trackBounds.Left + track.LeftMargin,
                    plotTop,
                    trackBounds.Right - track.RightMargin,
                    trackBounds.Bottom - 4f);

                if (plotBounds.Right < plotBounds.Left)
                {
                    plotBounds.Right = plotBounds.Left;
                }
                if (plotBounds.Bottom < plotBounds.Top)
                {
                    plotBounds.Bottom = plotBounds.Top;
                }

                layouts.Add(new WellLogTrackLayout(track, trackBounds, headerBounds, plotBounds));
                currentLeft = trackBounds.Right + trackGap;
            }

            return layouts;
        }

        public static float MapDepthToY(WellLogDepthAxis axis, float depth, SKRect plotBounds)
        {
            if (axis == null || axis.DepthSpan <= 0f)
            {
                return plotBounds.Top;
            }

            var normalized = (depth - axis.MinimumDepth) / axis.DepthSpan;
            normalized = Math.Clamp(normalized, 0f, 1f);

            return axis.IsIncreasingDown
                ? plotBounds.Top + normalized * plotBounds.Height
                : plotBounds.Bottom - normalized * plotBounds.Height;
        }

        public static float MapValueToX(WellLogCurve curve, float value, SKRect plotBounds)
        {
            if (curve == null)
            {
                return plotBounds.Left;
            }

            float normalized;

            if (curve.ScaleType == WellLogScaleType.Log10)
            {
                var min = Math.Max(0.0001f, curve.MinimumValue);
                var max = Math.Max(min + 0.0001f, curve.MaximumValue);
                var safe = Math.Clamp(value, min, max);
                normalized = (MathF.Log10(safe) - MathF.Log10(min)) / (MathF.Log10(max) - MathF.Log10(min));
            }
            else
            {
                var span = Math.Max(0.0001f, curve.MaximumValue - curve.MinimumValue);
                normalized = (value - curve.MinimumValue) / span;
            }

            normalized = Math.Clamp(normalized, 0f, 1f);
            return plotBounds.Left + normalized * plotBounds.Width;
        }
    }
}
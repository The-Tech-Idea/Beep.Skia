using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia.WellLogs
{
    public static class WellLogRenderer
    {
        public static void Draw(SKCanvas canvas, SKRect bounds, WellLogDocument document, float trackGap = 6f, float headerHeight = 28f)
        {
            if (canvas == null || document == null || document.Tracks.Count == 0)
            {
                return;
            }

            var layouts = WellLogLayoutEngine.BuildTrackLayouts(bounds, document, trackGap, headerHeight);

            using var outerPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = new SKColor(166, 174, 184),
                StrokeWidth = 1.25f
            };

            foreach (var layout in layouts)
            {
                DrawTrackBackground(canvas, layout);
                DrawTrackHeader(canvas, layout);
                DrawDepthGrid(canvas, layout, document.DepthAxis);

                switch (layout.Track.Role)
                {
                    case WellLogTrackRole.Depth:
                        DrawDepthTrack(canvas, layout, document.DepthAxis);
                        break;
                    case WellLogTrackRole.Lithology:
                        DrawLithologyTrack(canvas, layout, document.DepthAxis);
                        break;
                    default:
                        DrawCurveTrack(canvas, layout, document.DepthAxis);
                        break;
                }

                canvas.DrawRect(layout.TrackBounds, outerPaint);
            }
        }

        private static void DrawTrackBackground(SKCanvas canvas, WellLogTrackLayout layout)
        {
            using var fill = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = layout.Track.BackgroundColor
            };
            canvas.DrawRect(layout.TrackBounds, fill);

            if (layout.Track.Role == WellLogTrackRole.Depth)
            {
                WellLogBrushLibrary.DrawPattern(canvas, layout.PlotBounds, "depth", 28);
            }
        }

        private static void DrawTrackHeader(SKCanvas canvas, WellLogTrackLayout layout)
        {
            if (!layout.Track.ShowHeader || layout.HeaderBounds.Height <= 0f)
            {
                return;
            }

            using var headerFill = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = new SKColor(228, 233, 241)
            };
            using var headerBorder = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = new SKColor(170, 176, 186),
                StrokeWidth = 1f
            };
            using var textPaint = new SKPaint { IsAntialias = true, Color = new SKColor(38, 46, 58) };
            using var font = new SKFont { Size = 12f };
            using var subFont = new SKFont { Size = 10f };

            canvas.DrawRect(layout.HeaderBounds, headerFill);
            canvas.DrawRect(layout.HeaderBounds, headerBorder);

            var titleY = layout.HeaderBounds.Top + 13f;
            canvas.DrawText(layout.Track.Name, layout.HeaderBounds.MidX, titleY, SKTextAlign.Center, font, textPaint);

            if (layout.Track.Curves.Count == 1)
            {
                var curve = layout.Track.Curves[0];
                var subtitle = string.IsNullOrWhiteSpace(curve.Unit)
                    ? curve.Mnemonic
                    : $"{curve.Mnemonic} [{curve.Unit}]";
                canvas.DrawText(subtitle, layout.HeaderBounds.MidX, titleY + 12f, SKTextAlign.Center, subFont, textPaint);
            }
        }

        private static void DrawDepthGrid(SKCanvas canvas, WellLogTrackLayout layout, WellLogDepthAxis axis)
        {
            if (!layout.Track.ShowGrid || axis == null || axis.DepthSpan <= 0f)
            {
                return;
            }

            using var minorPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = layout.Track.GridColor.WithAlpha(80),
                StrokeWidth = 0.75f
            };
            using var majorPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = layout.Track.GridColor.WithAlpha(170),
                StrokeWidth = 1f
            };

            if (axis.MinorStep > 0f)
            {
                var firstMinor = MathF.Ceiling(axis.MinimumDepth / axis.MinorStep) * axis.MinorStep;
                for (float depth = firstMinor; depth <= axis.MaximumDepth + 0.001f; depth += axis.MinorStep)
                {
                    var y = WellLogLayoutEngine.MapDepthToY(axis, depth, layout.PlotBounds);
                    canvas.DrawLine(layout.PlotBounds.Left, y, layout.PlotBounds.Right, y, minorPaint);
                }
            }

            if (axis.MajorStep > 0f)
            {
                var firstMajor = MathF.Ceiling(axis.MinimumDepth / axis.MajorStep) * axis.MajorStep;
                for (float depth = firstMajor; depth <= axis.MaximumDepth + 0.001f; depth += axis.MajorStep)
                {
                    var y = WellLogLayoutEngine.MapDepthToY(axis, depth, layout.PlotBounds);
                    canvas.DrawLine(layout.PlotBounds.Left, y, layout.PlotBounds.Right, y, majorPaint);
                }
            }
        }

        private static void DrawDepthTrack(SKCanvas canvas, WellLogTrackLayout layout, WellLogDepthAxis axis)
        {
            if (axis == null || axis.MajorStep <= 0f)
            {
                return;
            }

            using var labelPaint = new SKPaint { IsAntialias = true, Color = new SKColor(44, 52, 62) };
            using var font = new SKFont { Size = 11f };

            var firstMajor = MathF.Ceiling(axis.MinimumDepth / axis.MajorStep) * axis.MajorStep;
            for (float depth = firstMajor; depth <= axis.MaximumDepth + 0.001f; depth += axis.MajorStep)
            {
                var y = WellLogLayoutEngine.MapDepthToY(axis, depth, layout.PlotBounds);
                canvas.DrawText(depth.ToString("0.0"), layout.PlotBounds.MidX, y - 2f, SKTextAlign.Center, font, labelPaint);
            }
        }

        private static void DrawLithologyTrack(SKCanvas canvas, WellLogTrackLayout layout, WellLogDepthAxis axis)
        {
            foreach (var interval in layout.Track.LithologyIntervals)
            {
                var top = WellLogLayoutEngine.MapDepthToY(axis, interval.TopDepth, layout.PlotBounds);
                var bottom = WellLogLayoutEngine.MapDepthToY(axis, interval.BottomDepth, layout.PlotBounds);
                var rect = new SKRect(layout.PlotBounds.Left, Math.Min(top, bottom), layout.PlotBounds.Right, Math.Max(top, bottom));
                WellLogBrushLibrary.DrawPattern(canvas, rect, interval.BrushKey, 110);

                if (!string.IsNullOrWhiteSpace(interval.Label) && rect.Height > 18f)
                {
                    using var textPaint = new SKPaint { IsAntialias = true, Color = new SKColor(38, 46, 58) };
                    using var font = new SKFont { Size = 10f };
                    canvas.DrawText(interval.Label, rect.MidX, rect.MidY + 4f, SKTextAlign.Center, font, textPaint);
                }
            }
        }

        private static void DrawCurveTrack(SKCanvas canvas, WellLogTrackLayout layout, WellLogDepthAxis axis)
        {
            foreach (var curve in layout.Track.Curves.Where(curve => curve.IsVisible))
            {
                switch (curve.Style.FillMode)
                {
                    case WellLogFillMode.LeftToCurve:
                    case WellLogFillMode.RightToCurve:
                    case WellLogFillMode.CutoffShading:
                        DrawSingleCurveFill(canvas, layout, axis, curve);
                        break;
                    case WellLogFillMode.BetweenCurves:
                        DrawCurveBandFill(canvas, layout, axis, curve);
                        break;
                }
            }

            foreach (var curve in layout.Track.Curves.Where(curve => curve.IsVisible))
            {
                DrawCurveLine(canvas, layout, axis, curve);
            }
        }

        private static void DrawSingleCurveFill(SKCanvas canvas, WellLogTrackLayout layout, WellLogDepthAxis axis, WellLogCurve curve)
        {
            var ordered = curve.Samples.OrderBy(sample => sample.Depth).ToList();
            if (ordered.Count < 2)
            {
                return;
            }

            var firstY = WellLogLayoutEngine.MapDepthToY(axis, ordered[0].Depth, layout.PlotBounds);
            var lastY = WellLogLayoutEngine.MapDepthToY(axis, ordered[ordered.Count - 1].Depth, layout.PlotBounds);
            var referenceValue = curve.Style.FillReference;
            var referenceX = curve.Style.FillMode == WellLogFillMode.RightToCurve
                ? layout.PlotBounds.Right
                : layout.PlotBounds.Left;

            if (referenceValue.HasValue)
            {
                referenceX = WellLogLayoutEngine.MapValueToX(curve, referenceValue.Value, layout.PlotBounds);
            }

            using var pathBuilder = new SKPathBuilder();
            pathBuilder.MoveTo(referenceX, firstY);

            foreach (var sample in ordered)
            {
                var x = WellLogLayoutEngine.MapValueToX(curve, sample.Value, layout.PlotBounds);
                var y = WellLogLayoutEngine.MapDepthToY(axis, sample.Depth, layout.PlotBounds);
                pathBuilder.LineTo(x, y);
            }

            pathBuilder.LineTo(referenceX, lastY);
            pathBuilder.Close();

            canvas.Save();
            using var path = pathBuilder.Detach();
            canvas.ClipPath(path, SKClipOperation.Intersect, true);
            WellLogBrushLibrary.DrawPattern(canvas, layout.PlotBounds, curve.Style.BrushKey, 90);
            canvas.Restore();
        }

        private static void DrawCurveBandFill(SKCanvas canvas, WellLogTrackLayout layout, WellLogDepthAxis axis, WellLogCurve curve)
        {
            if (string.IsNullOrWhiteSpace(curve.Style.CompanionCurveMnemonic))
            {
                return;
            }

            var companion = layout.Track.Curves.FirstOrDefault(candidate =>
                !ReferenceEquals(candidate, curve) &&
                string.Equals(candidate.Mnemonic, curve.Style.CompanionCurveMnemonic, StringComparison.OrdinalIgnoreCase));

            if (companion == null)
            {
                return;
            }

            var orderedA = curve.Samples.OrderBy(sample => sample.Depth).ToList();
            var orderedB = companion.Samples.OrderBy(sample => sample.Depth).ToList();
            var count = Math.Min(orderedA.Count, orderedB.Count);
            if (count < 2)
            {
                return;
            }

            using var pathBuilder = new SKPathBuilder();
            var firstA = orderedA[0];
            pathBuilder.MoveTo(
                WellLogLayoutEngine.MapValueToX(curve, firstA.Value, layout.PlotBounds),
                WellLogLayoutEngine.MapDepthToY(axis, firstA.Depth, layout.PlotBounds));

            for (int index = 0; index < count; index++)
            {
                var sample = orderedA[index];
                pathBuilder.LineTo(
                    WellLogLayoutEngine.MapValueToX(curve, sample.Value, layout.PlotBounds),
                    WellLogLayoutEngine.MapDepthToY(axis, sample.Depth, layout.PlotBounds));
            }

            for (int index = count - 1; index >= 0; index--)
            {
                var sample = orderedB[index];
                pathBuilder.LineTo(
                    WellLogLayoutEngine.MapValueToX(companion, sample.Value, layout.PlotBounds),
                    WellLogLayoutEngine.MapDepthToY(axis, sample.Depth, layout.PlotBounds));
            }

            pathBuilder.Close();
            canvas.Save();
            using var path = pathBuilder.Detach();
            canvas.ClipPath(path, SKClipOperation.Intersect, true);
            WellLogBrushLibrary.DrawPattern(canvas, layout.PlotBounds, curve.Style.BrushKey, 88);
            canvas.Restore();
        }

        private static void DrawCurveLine(SKCanvas canvas, WellLogTrackLayout layout, WellLogDepthAxis axis, WellLogCurve curve)
        {
            var ordered = curve.Samples.OrderBy(sample => sample.Depth).ToList();
            if (ordered.Count == 0)
            {
                return;
            }

            using var linePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = curve.Style.LineColor,
                StrokeWidth = curve.Style.StrokeWidth
            };
            using var pathBuilder = new SKPathBuilder();

            for (int index = 0; index < ordered.Count; index++)
            {
                var sample = ordered[index];
                var x = WellLogLayoutEngine.MapValueToX(curve, sample.Value, layout.PlotBounds);
                var y = WellLogLayoutEngine.MapDepthToY(axis, sample.Depth, layout.PlotBounds);

                if (index == 0)
                {
                    pathBuilder.MoveTo(x, y);
                }
                else
                {
                    pathBuilder.LineTo(x, y);
                }
            }

            canvas.Save();
            canvas.ClipRect(layout.PlotBounds);
            using var path = pathBuilder.Detach();
            canvas.DrawPath(path, linePaint);
            canvas.Restore();
        }
    }
}
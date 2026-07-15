using SkiaSharp;
using System;

namespace Beep.Skia.WellLogs
{
    public static class WellLogTemplates
    {
        public static WellLogDocument CreateTripleComboDemo()
        {
            var document = new WellLogDocument
            {
                Name = "Triple Combo Composite",
                SourceReference = "LAS 3.0 / DLIS-ready composite track layout"
            };

            document.SupportedStandards.Add(WellLogDataStandard.Las20);
            document.SupportedStandards.Add(WellLogDataStandard.Las30);
            document.SupportedStandards.Add(WellLogDataStandard.DlisRp66V1);
            document.SupportedStandards.Add(WellLogDataStandard.DlisRp66V2);

            document.DepthAxis.MinimumDepth = 8200f;
            document.DepthAxis.MaximumDepth = 8300f;
            document.DepthAxis.MajorStep = 10f;
            document.DepthAxis.MinorStep = 2f;

            var depthTrack = new WellLogTrack
            {
                Name = "Depth",
                Role = WellLogTrackRole.Depth,
                WidthRatio = 0.55f,
                LeftMargin = 6f,
                RightMargin = 6f
            };

            var lithologyTrack = new WellLogTrack
            {
                Name = "Lithology",
                Role = WellLogTrackRole.Lithology,
                WidthRatio = 0.65f,
                LeftMargin = 4f,
                RightMargin = 4f
            };
            lithologyTrack.LithologyIntervals.Add(new WellLogLithologyInterval { TopDepth = 8200f, BottomDepth = 8224f, BrushKey = "sand", Label = "Sand" });
            lithologyTrack.LithologyIntervals.Add(new WellLogLithologyInterval { TopDepth = 8224f, BottomDepth = 8247f, BrushKey = "shale", Label = "Shale" });
            lithologyTrack.LithologyIntervals.Add(new WellLogLithologyInterval { TopDepth = 8247f, BottomDepth = 8268f, BrushKey = "limestone", Label = "Lmst" });
            lithologyTrack.LithologyIntervals.Add(new WellLogLithologyInterval { TopDepth = 8268f, BottomDepth = 8300f, BrushKey = "sand", Label = "Sand" });

            var gammaTrack = new WellLogTrack
            {
                Name = "Gamma Ray",
                WidthRatio = 1.15f
            };
            var gammaRay = new WellLogCurve
            {
                Name = "Gamma Ray",
                Mnemonic = "GR",
                Unit = "API",
                MinimumValue = 0f,
                MaximumValue = 150f,
                Style = new WellLogCurveStyle
                {
                    LineColor = new SKColor(31, 94, 48),
                    StrokeWidth = 2f,
                    BrushKey = "gamma-ray",
                    FillMode = WellLogFillMode.LeftToCurve,
                    FillReference = 75f
                }
            };
            PopulateCurve(gammaRay, document.DepthAxis, depth =>
                Clamp(65f + 20f * MathF.Sin((depth - 8200f) * 0.22f) + 12f * MathF.Cos((depth - 8200f) * 0.47f), 12f, 138f));
            gammaTrack.Curves.Add(gammaRay);

            var resistivityTrack = new WellLogTrack
            {
                Name = "Resistivity",
                WidthRatio = 1.1f
            };
            var resistivity = new WellLogCurve
            {
                Name = "Deep Resistivity",
                Mnemonic = "RT",
                Unit = "ohm.m",
                MinimumValue = 0.2f,
                MaximumValue = 2000f,
                ScaleType = WellLogScaleType.Log10,
                Style = new WellLogCurveStyle
                {
                    LineColor = new SKColor(32, 82, 170),
                    StrokeWidth = 1.9f,
                    BrushKey = "resistivity"
                }
            };
            PopulateCurve(resistivity, document.DepthAxis, depth =>
            {
                var value = 20f + 18f * MathF.Sin((depth - 8200f) * 0.15f);
                value += 30f * MathF.Exp(-MathF.Abs(depth - 8262f) / 7f);
                return Clamp(value, 0.3f, 1600f);
            });
            resistivityTrack.Curves.Add(resistivity);

            var porosityTrack = new WellLogTrack
            {
                Name = "Porosity Overlay",
                WidthRatio = 1.3f
            };

            var neutron = new WellLogCurve
            {
                Name = "Neutron Porosity",
                Mnemonic = "NPHI",
                Unit = "v/v",
                MinimumValue = 0f,
                MaximumValue = 0.45f,
                Style = new WellLogCurveStyle
                {
                    LineColor = new SKColor(207, 82, 39),
                    StrokeWidth = 1.7f,
                    BrushKey = "gas",
                    FillMode = WellLogFillMode.BetweenCurves,
                    CompanionCurveMnemonic = "DPHI"
                }
            };

            var densityPorosity = new WellLogCurve
            {
                Name = "Density Porosity",
                Mnemonic = "DPHI",
                Unit = "v/v",
                MinimumValue = 0f,
                MaximumValue = 0.45f,
                Style = new WellLogCurveStyle
                {
                    LineColor = new SKColor(75, 116, 175),
                    StrokeWidth = 1.7f,
                    BrushKey = "porosity"
                }
            };

            PopulateCurve(neutron, document.DepthAxis, depth =>
                Clamp(0.22f + 0.05f * MathF.Sin((depth - 8200f) * 0.16f) + 0.03f * MathF.Cos((depth - 8200f) * 0.4f), 0.03f, 0.42f));
            PopulateCurve(densityPorosity, document.DepthAxis, depth =>
                Clamp(0.19f + 0.05f * MathF.Cos((depth - 8200f) * 0.12f) + 0.02f * MathF.Sin((depth - 8200f) * 0.34f), 0.02f, 0.4f));

            porosityTrack.Curves.Add(neutron);
            porosityTrack.Curves.Add(densityPorosity);

            document.Tracks.Add(depthTrack);
            document.Tracks.Add(lithologyTrack);
            document.Tracks.Add(gammaTrack);
            document.Tracks.Add(resistivityTrack);
            document.Tracks.Add(porosityTrack);

            return document;
        }

        private static void PopulateCurve(WellLogCurve curve, WellLogDepthAxis axis, Func<float, float> generator, float step = 0.5f)
        {
            curve.Samples.Clear();

            for (float depth = axis.MinimumDepth; depth <= axis.MaximumDepth + 0.001f; depth += step)
            {
                curve.Samples.Add(new WellLogCurveSample
                {
                    Depth = depth,
                    Value = generator(depth)
                });
            }
        }

        private static float Clamp(float value, float min, float max)
        {
            return Math.Clamp(value, min, max);
        }
    }
}
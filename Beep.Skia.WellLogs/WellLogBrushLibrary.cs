using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Beep.Skia.WellLogs
{
    /// <summary>
    /// Gets or sets the well log brush definition.
    /// </summary>
    public sealed class WellLogBrushDefinition
    {
        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        public string Key { get; set; } = "default";
        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string DisplayName { get; set; } = "Default";
        /// <summary>
        /// Gets or sets the primary color.
        /// </summary>
        public SKColor PrimaryColor { get; set; } = new SKColor(235, 238, 244);
        /// <summary>
        /// Gets or sets the secondary color.
        /// </summary>
        public SKColor SecondaryColor { get; set; } = new SKColor(150, 156, 166);
        /// <summary>
        /// Gets or sets the pattern.
        /// </summary>
        public WellLogBrushPattern Pattern { get; set; } = WellLogBrushPattern.Solid;
        /// <summary>
        /// Gets or sets the spacing.
        /// </summary>
        public float Spacing { get; set; } = 8f;
        /// <summary>
        /// Gets or sets the stroke width.
        /// </summary>
        public float StrokeWidth { get; set; } = 1f;
    }

    /// <summary>
    /// Gets or sets the well log brush library.
    /// </summary>
    public static class WellLogBrushLibrary
    {
        private static readonly Dictionary<string, WellLogBrushDefinition> _definitions =
            new Dictionary<string, WellLogBrushDefinition>(StringComparer.OrdinalIgnoreCase)
            {
                ["default"] = new WellLogBrushDefinition
                {
                    Key = "default",
                    DisplayName = "Default Layer",
                    PrimaryColor = new SKColor(239, 242, 247),
                    SecondaryColor = new SKColor(179, 185, 198),
                    Pattern = WellLogBrushPattern.Solid
                },
                ["depth"] = new WellLogBrushDefinition
                {
                    Key = "depth",
                    DisplayName = "Depth Track",
                    PrimaryColor = new SKColor(250, 251, 253),
                    SecondaryColor = new SKColor(210, 214, 220),
                    Pattern = WellLogBrushPattern.Horizontal,
                    Spacing = 10f
                },
                ["sand"] = new WellLogBrushDefinition
                {
                    Key = "sand",
                    DisplayName = "Sandstone",
                    PrimaryColor = new SKColor(246, 213, 92),
                    SecondaryColor = new SKColor(157, 116, 24),
                    Pattern = WellLogBrushPattern.DiagonalRight,
                    Spacing = 9f
                },
                ["shale"] = new WellLogBrushDefinition
                {
                    Key = "shale",
                    DisplayName = "Shale",
                    PrimaryColor = new SKColor(109, 133, 84),
                    SecondaryColor = new SKColor(58, 72, 44),
                    Pattern = WellLogBrushPattern.CrossHatch,
                    Spacing = 7f
                },
                ["limestone"] = new WellLogBrushDefinition
                {
                    Key = "limestone",
                    DisplayName = "Limestone",
                    PrimaryColor = new SKColor(191, 205, 188),
                    SecondaryColor = new SKColor(102, 122, 104),
                    Pattern = WellLogBrushPattern.Horizontal,
                    Spacing = 6f
                },
                ["gamma-ray"] = new WellLogBrushDefinition
                {
                    Key = "gamma-ray",
                    DisplayName = "Gamma Ray Shading",
                    PrimaryColor = new SKColor(215, 237, 190),
                    SecondaryColor = new SKColor(55, 120, 72),
                    Pattern = WellLogBrushPattern.Solid
                },
                ["resistivity"] = new WellLogBrushDefinition
                {
                    Key = "resistivity",
                    DisplayName = "Resistivity Layer",
                    PrimaryColor = new SKColor(218, 234, 252),
                    SecondaryColor = new SKColor(59, 113, 202),
                    Pattern = WellLogBrushPattern.DiagonalLeft,
                    Spacing = 8f
                },
                ["porosity"] = new WellLogBrushDefinition
                {
                    Key = "porosity",
                    DisplayName = "Porosity Layer",
                    PrimaryColor = new SKColor(247, 223, 205),
                    SecondaryColor = new SKColor(192, 111, 64),
                    Pattern = WellLogBrushPattern.Dots,
                    Spacing = 9f
                },
                ["gas"] = new WellLogBrushDefinition
                {
                    Key = "gas",
                    DisplayName = "Gas Crossover",
                    PrimaryColor = new SKColor(255, 196, 0),
                    SecondaryColor = new SKColor(184, 87, 0),
                    Pattern = WellLogBrushPattern.CrossHatch,
                    Spacing = 6f
                },
                ["oil"] = new WellLogBrushDefinition
                {
                    Key = "oil",
                    DisplayName = "Oil Layer",
                    PrimaryColor = new SKColor(112, 78, 56),
                    SecondaryColor = new SKColor(54, 35, 24),
                    Pattern = WellLogBrushPattern.DiagonalRight,
                    Spacing = 7f
                },
                ["water"] = new WellLogBrushDefinition
                {
                    Key = "water",
                    DisplayName = "Water Layer",
                    PrimaryColor = new SKColor(150, 208, 255),
                    SecondaryColor = new SKColor(49, 118, 185),
                    Pattern = WellLogBrushPattern.Horizontal,
                    Spacing = 8f
                }
            };

        public static IReadOnlyDictionary<string, WellLogBrushDefinition> Definitions => _definitions;

        /// <summary>
        /// Gets or sets the get.
        /// </summary>
        public static WellLogBrushDefinition Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return _definitions["default"];
            }

            return _definitions.TryGetValue(key, out var definition)
                ? definition
                : _definitions["default"];
        }

        /// <summary>
        /// Gets or sets the create fill paint.
        /// </summary>
        public static SKPaint CreateFillPaint(string key, SKRect bounds, byte alpha = 90)
        {
            var definition = Get(key);
            var primary = definition.PrimaryColor.WithAlpha(alpha);
            var secondary = definition.SecondaryColor.WithAlpha(alpha);

            var paint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = primary
            };

            if (definition.PrimaryColor != definition.SecondaryColor)
            {
                paint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(bounds.Left, bounds.Top),
                    new SKPoint(bounds.Right, bounds.Bottom),
                    new[] { primary, secondary },
                    null,
                    SKShaderTileMode.Clamp);
            }

            return paint;
        }

        /// <summary>
        /// Gets or sets the draw pattern.
        /// </summary>
        public static void DrawPattern(SKCanvas canvas, SKRect bounds, string key, byte alpha = 96)
        {
            var definition = Get(key);
            using var fillPaint = CreateFillPaint(key, bounds, alpha);
            canvas.DrawRect(bounds, fillPaint);

            if (definition.Pattern == WellLogBrushPattern.Solid)
            {
                return;
            }

            using var linePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = definition.StrokeWidth,
                Color = definition.SecondaryColor.WithAlpha((byte)Math.Min(255, alpha + 40))
            };

            var spacing = Math.Max(4f, definition.Spacing);

            switch (definition.Pattern)
            {
                case WellLogBrushPattern.Horizontal:
                    for (float y = bounds.Top; y <= bounds.Bottom; y += spacing)
                    {
                        canvas.DrawLine(bounds.Left, y, bounds.Right, y, linePaint);
                    }
                    break;

                case WellLogBrushPattern.DiagonalLeft:
                    for (float x = bounds.Left - bounds.Height; x <= bounds.Right; x += spacing)
                    {
                        canvas.DrawLine(x, bounds.Bottom, x + bounds.Height, bounds.Top, linePaint);
                    }
                    break;

                case WellLogBrushPattern.DiagonalRight:
                    for (float x = bounds.Left; x <= bounds.Right + bounds.Height; x += spacing)
                    {
                        canvas.DrawLine(x, bounds.Top, x - bounds.Height, bounds.Bottom, linePaint);
                    }
                    break;

                case WellLogBrushPattern.CrossHatch:
                    for (float x = bounds.Left - bounds.Height; x <= bounds.Right; x += spacing)
                    {
                        canvas.DrawLine(x, bounds.Bottom, x + bounds.Height, bounds.Top, linePaint);
                    }
                    for (float x = bounds.Left; x <= bounds.Right + bounds.Height; x += spacing)
                    {
                        canvas.DrawLine(x, bounds.Top, x - bounds.Height, bounds.Bottom, linePaint);
                    }
                    break;

                case WellLogBrushPattern.Dots:
                    using (var dotPaint = new SKPaint
                    {
                        IsAntialias = true,
                        Style = SKPaintStyle.Fill,
                        Color = definition.SecondaryColor.WithAlpha((byte)Math.Min(255, alpha + 30))
                    })
                    {
                        for (float y = bounds.Top + spacing * 0.5f; y <= bounds.Bottom; y += spacing)
                        {
                            for (float x = bounds.Left + spacing * 0.5f; x <= bounds.Right; x += spacing)
                            {
                                canvas.DrawCircle(x, y, Math.Max(1.5f, definition.StrokeWidth + 0.5f), dotPaint);
                            }
                        }
                    }
                    break;
            }
        }
    }
}

using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Beep.Skia.Model;
namespace Beep.Skia
{
    /// <summary>
    /// Provides utility methods for SkiaSharp operations including geometric transformations,
    /// color space conversions, and mathematical calculations.
    /// </summary>
    public static class SkiaUtil
    {
        /// <summary>
        /// Rotates a point around the origin by the specified number of degrees.
        /// </summary>
        /// <param name="point">The point to rotate.</param>
        /// <param name="degrees">The rotation angle in degrees.</param>
        /// <returns>The rotated point.</returns>
        public static SKPoint Rotate(SKPoint point, float degrees)
        {
            float radians = degrees * (float)Math.PI / 180;
            float cosRadians = (float)Math.Cos(radians);
            float sinRadians = (float)Math.Sin(radians);

            float x = point.X * cosRadians - point.Y * sinRadians;
            float y = point.X * sinRadians + point.Y * cosRadians;

            return new SKPoint(x, y);
        }

        /// <summary>
        /// Adds two points together component-wise.
        /// </summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point.</param>
        /// <returns>The sum of the two points.</returns>
        public static SKPoint Add(SKPoint point1, SKPoint point2)
        {
            return new SKPoint(point1.X + point2.X, point1.Y + point2.Y);
        }

        /// <summary>
        /// Subtracts the second point from the first point component-wise.
        /// </summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point to subtract.</param>
        /// <returns>The difference between the two points.</returns>
        public static SKPoint Subtract(SKPoint point1, SKPoint point2)
        {
            return new SKPoint(point1.X - point2.X, point1.Y - point2.Y);
        }

        /// <summary>
        /// Calculates the cross product (also known as the z-component of the 3D cross product) of two 2D vectors.
        /// </summary>
        /// <param name="v1">The first vector.</param>
        /// <param name="v2">The second vector.</param>
        /// <returns>The cross product value.</returns>
        public static float CrossProduct(SKPoint v1, SKPoint v2)
        {
            return (v1.X * v2.Y) - (v1.Y * v2.X);
        }

        /// <summary>
        /// Converts an SKColor to HSL (Hue, Saturation, Luminosity) color space.
        /// </summary>
        /// <param name="color">The SKColor to convert.</param>
        /// <returns>A tuple containing Hue (0-360), Saturation (0-1), and Luminosity (0-1).</returns>
        public static (float Hue, float Saturation, float Luminosity) ToHsl(this SKColor color)
        {
            float r = color.Red / 255f;
            float g = color.Green / 255f;
            float b = color.Blue / 255f;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float luminosity = (max + min) / 2;

            if (max == min)
            {
                return (0, 0, luminosity);
            }

            float delta = max - min;
            float saturation = luminosity > 0.5f ? delta / (2 - max - min) : delta / (max + min);

            float hue;
            if (max == r)
            {
                hue = (g - b) / delta + (g < b ? 6 : 0);
            }
            else if (max == g)
            {
                hue = (b - r) / delta + 2;
            }
            else
            {
                hue = (r - g) / delta + 4;
            }

            hue /= 6;

            return (hue * 360, saturation, luminosity);
        }

        /// <summary>
        /// Creates an SKColor from HSL (Hue, Saturation, Luminosity) values.
        /// </summary>
        /// <param name="hue">The hue component (0-360 degrees).</param>
        /// <param name="saturation">The saturation component (0-1).</param>
        /// <param name="luminosity">The luminosity component (0-1).</param>
        /// <returns>The corresponding SKColor.</returns>
        public static SKColor FromHsl(float hue, float saturation, float luminosity)
        {
            float r, g, b;

            if (saturation == 0)
            {
                r = g = b = luminosity;
            }
            else
            {
                float q = luminosity < 0.5f ? luminosity * (1 + saturation) : luminosity + saturation - luminosity * saturation;
                float p = 2 * luminosity - q;
                hue /= 360f;

                r = HueToRgb(p, q, hue + 1 / 3f);
                g = HueToRgb(p, q, hue);
                b = HueToRgb(p, q, hue - 1 / 3f);
            }

            return new SKColor((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        /// <summary>
        /// Converts a hue value to RGB component using the HSL to RGB conversion algorithm.
        /// </summary>
        /// <param name="p">The p component from the HSL conversion.</param>
        /// <param name="q">The q component from the HSL conversion.</param>
        /// <param name="t">The hue value to convert.</param>
        /// <returns>The RGB component value (0-1).</returns>
        private static float HueToRgb(float p, float q, float t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1 / 6f) return p + (q - p) * 6 * t;
            if (t < 1 / 2f) return q;
            if (t < 2 / 3f) return p + (q - p) * (2 / 3f - t) * 6;
            return p;
        }
    }
}

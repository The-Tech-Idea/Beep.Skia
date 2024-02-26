using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beep.Skia
{
    public static class SkiaUtil
    {
        public static SKPoint Rotate(SKPoint point, float degrees)
        {
            float radians = degrees * (float)Math.PI / 180;
            float cosRadians = (float)Math.Cos(radians);
            float sinRadians = (float)Math.Sin(radians);

            float x = point.X * cosRadians - point.Y * sinRadians;
            float y = point.X * sinRadians + point.Y * cosRadians;

            return new SKPoint(x, y);
        }

        public static SKPoint Add(SKPoint point1, SKPoint point2)
        {
            return new SKPoint(point1.X + point2.X, point1.Y + point2.Y);
        }

        public static SKPoint Subtract(SKPoint point1, SKPoint point2)
        {
            return new SKPoint(point1.X - point2.X, point1.Y - point2.Y);
        }
        public static float CrossProduct(SKPoint v1, SKPoint v2)
        {
            return (v1.X * v2.Y) - (v1.Y * v2.X);
        }
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

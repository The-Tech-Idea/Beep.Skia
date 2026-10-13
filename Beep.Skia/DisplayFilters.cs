using SkiaSharp;

namespace Beep.Skia;

/// <summary>
/// Display-only colour treatment for a host that shows the canvas with inverted lightness in a dark theme, the way
/// Excalidraw does: the host applies the CSS filter <c>invert(93%) hue-rotate(180deg)</c> to the canvas element, which
/// keeps hues and flips light and dark. Pictures should not be flipped, so they are drawn pre-compensated with the
/// inverse of that filter and appear in their true colours. Exports never use any of this.
/// </summary>
public static class DisplayFilters
{
    /// <summary>The invert() amount the host's display filter uses.</summary>
    public const float InvertAmount = .93f;

    // CSS hue-rotate(180deg) as the Filter Effects specification defines it (cos = -1, sin = 0). Rotating by 180 degrees
    // twice is the identity, so this matrix is its own inverse.
    private static readonly float[] HueRotate180 =
    [
        -.574f, 1.430f, .144f,
        .426f, .430f, .144f,
        .426f, 1.430f, -.856f,
    ];

    /// <summary>The 4×5 colour matrix (RGBA rows, translation in 0–1) that, followed by the host's invert and
    /// hue-rotate, gives back the original colour: first hue-rotate by 180 degrees, then undo invert(93%).</summary>
    public static float[] ImageCompensationMatrix { get; } = Compensation();
    /// <summary>A colour filter for <see cref="ImageCompensationMatrix"/>, shared by every image draw.</summary>
    public static SKColorFilter ImageCompensation { get; } = SKColorFilter.CreateColorMatrix(ImageCompensationMatrix);

    private static float[] Compensation()
    {
        // invert(p): y = p + (1 - 2p) x, so its inverse is x = (y - p) / (1 - 2p).
        var scale = 1 / (1 - 2 * InvertAmount); var offset = -InvertAmount * scale;
        var m = new float[20];
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++) m[row * 5 + column] = HueRotate180[row * 3 + column] * scale;
            m[row * 5 + 4] = offset;
        }
        m[18] = 1; // Alpha passes through.
        return m;
    }

    /// <summary>The host's display filter on one colour channel triple (0–1), for checking the compensation.</summary>
    public static (float R, float G, float B) HostFilter(float r, float g, float b)
    {
        static float Invert(float c) => InvertAmount + (1 - 2 * InvertAmount) * c;
        var (ir, ig, ib) = (Invert(r), Invert(g), Invert(b));
        return (Clamp(HueRotate180[0] * ir + HueRotate180[1] * ig + HueRotate180[2] * ib),
                Clamp(HueRotate180[3] * ir + HueRotate180[4] * ig + HueRotate180[5] * ib),
                Clamp(HueRotate180[6] * ir + HueRotate180[7] * ig + HueRotate180[8] * ib));
    }
    /// <summary><see cref="ImageCompensationMatrix"/> on one colour channel triple (0–1), clamped as a draw clamps it.</summary>
    public static (float R, float G, float B) Compensate(float r, float g, float b)
    {
        var m = ImageCompensationMatrix;
        return (Clamp(m[0] * r + m[1] * g + m[2] * b + m[4]), Clamp(m[5] * r + m[6] * g + m[7] * b + m[9]), Clamp(m[10] * r + m[11] * g + m[12] * b + m[14]));
    }
    private static float Clamp(float c) => Math.Clamp(c, 0, 1);
}

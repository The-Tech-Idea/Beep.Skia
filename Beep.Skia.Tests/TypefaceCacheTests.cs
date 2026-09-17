using Beep.Skia;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for the shared typeface cache: identity reuse, style separation, and fallbacks.
    /// </summary>
    public class TypefaceCacheTests
    {
        [Fact]
        public void Get_ReturnsSameInstanceForSameFamilyAndStyle()
        {
            var first = TypefaceCache.Get("Segoe UI", SKFontStyle.Normal);
            var second = TypefaceCache.Get("Segoe UI", SKFontStyle.Normal);

            Assert.NotNull(first);
            Assert.Same(first, second);
        }

        [Fact]
        public void Get_SeparatesStyles()
        {
            var normal = TypefaceCache.Get("Segoe UI", SKFontStyle.Normal);
            var bold = TypefaceCache.Get("Segoe UI", SKFontStyle.Bold);
            var italic = TypefaceCache.Get("Segoe UI", SKFontStyle.Italic);

            Assert.NotSame(normal, bold);
            Assert.NotSame(normal, italic);
            Assert.NotSame(bold, italic);
        }

        [Fact]
        public void Get_ExplicitStyleParts_MatchEquivalentStyle()
        {
            var viaStyle = TypefaceCache.Get("Segoe UI", SKFontStyle.Bold);
            var viaParts = TypefaceCache.Get("Segoe UI", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);

            Assert.Same(viaStyle, viaParts);
        }

        [Fact]
        public void Get_FallsBackToDefaultForUnknownFamily()
        {
            var unknown = TypefaceCache.Get("No Such Font Family 12345", SKFontStyle.Normal);

            Assert.NotNull(unknown);
            Assert.Same(unknown, TypefaceCache.Get("No Such Font Family 12345", SKFontStyle.Normal));
        }

        [Fact]
        public void Get_NullOrEmptyFamily_UsesDefaultFamily()
        {
            var fromNull = TypefaceCache.Get(null, SKFontStyle.Normal);
            var fromEmpty = TypefaceCache.Get("   ", SKFontStyle.Normal);

            Assert.Same(TypefaceCache.Default, fromNull);
            Assert.Same(fromNull, fromEmpty);
        }

        [Fact]
        public void Get_IsThreadSafe_AndReturnsStableInstances()
        {
            var results = new System.Collections.Concurrent.ConcurrentBag<SKTypeface>();
            System.Threading.Tasks.Parallel.For(0, 200, _ => results.Add(TypefaceCache.Get("Segoe UI", SKFontStyle.Bold)));

            var distinct = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Distinct(results));
            Assert.Single(distinct);
        }
    }
}

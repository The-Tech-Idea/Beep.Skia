using System;
using System.Collections.Generic;

namespace Beep.Skia.Helpers
{
    /// <summary>The arithmetic of table positions: sizes in a row or column turned into edges, and edges found at a point.</summary>
    public static class TableDrawerHelper
    {
        /// <summary>The edges of consecutive sizes from 0: one more than there are sizes, the last being their sum.</summary>
        public static float[] Offsets(IReadOnlyList<float> sizes)
        {
            if (sizes == null) throw new ArgumentNullException(nameof(sizes));
            var offsets = new float[sizes.Count + 1];
            for (int i = 0; i < sizes.Count; i++) offsets[i + 1] = offsets[i] + sizes[i];
            return offsets;
        }

        /// <summary>The total of <paramref name="count"/> sizes from <paramref name="start"/>.</summary>
        public static float Span(IReadOnlyList<float> sizes, int start, int count)
        {
            if (sizes == null) throw new ArgumentNullException(nameof(sizes));
            if (start < 0 || count < 0 || start + count > sizes.Count) throw new ArgumentOutOfRangeException(nameof(count));
            float total = 0;
            for (int i = start; i < start + count; i++) total += sizes[i];
            return total;
        }

        /// <summary>The index of the span of <paramref name="offsets"/> that holds <paramref name="value"/> (a value on an
        /// inner edge belongs to the span after it); null outside them all.</summary>
        public static int? IndexAt(IReadOnlyList<float> offsets, float value)
        {
            if (offsets == null) throw new ArgumentNullException(nameof(offsets));
            if (offsets.Count < 2 || !float.IsFinite(value) || value < offsets[0] || value > offsets[offsets.Count - 1]) return null;
            int low = 0, high = offsets.Count - 2;
            while (low < high)
            {
                var middle = (low + high + 1) / 2;
                if (offsets[middle] <= value) low = middle; else high = middle - 1;
            }
            return low;
        }

        /// <summary>The span whose far edge is nearest <paramref name="value"/> within <paramref name="tolerance"/>, or null.</summary>
        public static int? EdgeAt(IReadOnlyList<float> offsets, float value, float tolerance)
        {
            if (offsets == null) throw new ArgumentNullException(nameof(offsets));
            if (!float.IsFinite(value) || !float.IsFinite(tolerance) || tolerance < 0) return null;
            int? best = null; var distance = tolerance;
            for (int i = 1; i < offsets.Count; i++)
            {
                var d = Math.Abs(offsets[i] - value);
                if (d <= distance) { distance = d; best = i - 1; }
            }
            return best;
        }
    }
}

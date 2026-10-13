using System;
using System.Collections.Generic;

namespace Beep.Skia.Text
{
    /// <summary>Unicode Bidi_Class values, in the order of the generated data table.</summary>
    public enum BidiClass : byte { L, R, AL, EN, ES, ET, AN, CS, NSM, BN, B, S, WS, ON, LRE, LRO, RLE, RLO, PDF, LRI, RLI, FSI, PDI }

    /// <summary>
    /// The Unicode Bidirectional Algorithm (UAX #9) for one paragraph of plain text: rules P2–P3, X9–X10 with a single
    /// isolating run sequence, W1–W7, N0–N2, I1–I2 and L1–L2. Explicit embeddings, overrides and isolates, paragraph
    /// separators and segment separators are outside this scope: <see cref="Supports"/> reports them so callers can
    /// refuse such text, split paragraphs at line breaks and replace tabs before resolving.
    /// </summary>
    public static partial class UnicodeBidi
    {
        public static BidiClass GetClass(int codePoint)
        {
            if (codePoint < 0 || codePoint > 0x10FFFF) throw new ArgumentOutOfRangeException(nameof(codePoint));
            int index = Array.BinarySearch(RangeStarts, codePoint);
            return (BidiClass)RangeClasses[index >= 0 ? index : ~index - 1];
        }

        /// <summary>True for every class this paragraph-level implementation resolves.</summary>
        public static bool Supports(int codePoint) => GetClass(codePoint) switch
        {
            BidiClass.L or BidiClass.R or BidiClass.AL or BidiClass.EN or BidiClass.ES or BidiClass.ET or BidiClass.AN
                or BidiClass.CS or BidiClass.NSM or BidiClass.BN or BidiClass.WS or BidiClass.ON => true,
            BidiClass.B or BidiClass.S or BidiClass.LRE or BidiClass.LRO or BidiClass.RLE or BidiClass.RLO or BidiClass.PDF
                or BidiClass.LRI or BidiClass.RLI or BidiClass.FSI or BidiClass.PDI => false,
        };

        /// <summary>
        /// Resolves the embedding level of each code point of one paragraph (rules P2–I2; L1 is applied separately per
        /// line). <paramref name="paragraphLevel"/> is 0 or 1, or null to use the first strong character (P2–P3).
        /// Boundary neutrals (BN) receive the level of the preceding character, or the paragraph level at the start.
        /// </summary>
        public static byte[] ResolveLevels(int[] codePoints, int? paragraphLevel, out byte resolvedParagraphLevel)
        {
            if (codePoints == null) throw new ArgumentNullException(nameof(codePoints));
            if (paragraphLevel is not (null or 0 or 1)) throw new ArgumentOutOfRangeException(nameof(paragraphLevel));
            int n = codePoints.Length;
            var original = new BidiClass[n];
            for (int i = 0; i < n; i++)
            {
                original[i] = GetClass(codePoints[i]);
                if (!Supports(codePoints[i]))
                    throw new ArgumentException($"U+{codePoints[i]:X4} is an explicit directional formatting or separator character.", nameof(codePoints));
            }
            byte level = (byte)(paragraphLevel ?? FirstStrongLevel(original));
            resolvedParagraphLevel = level;
            var levels = new byte[n];
            // X9: boundary neutrals are removed from resolution; the sequence below holds the remaining positions.
            var sequence = new List<int>(n);
            for (int i = 0; i < n; i++) if (original[i] != BidiClass.BN) sequence.Add(i);
            int m = sequence.Count;
            var types = new BidiClass[m];
            for (int k = 0; k < m; k++) types[k] = original[sequence[k]];
            var embedding = level % 2 == 0 ? BidiClass.L : BidiClass.R; // sos and eos: one level run at the paragraph level.

            // W1: a non-spacing mark takes the type of the previous character, or sos.
            for (int k = 0; k < m; k++) if (types[k] == BidiClass.NSM) types[k] = k == 0 ? embedding : types[k - 1];
            // W2: a European number after Arabic letter context becomes an Arabic number.
            var lastStrong = embedding;
            for (int k = 0; k < m; k++)
            {
                if (types[k] is BidiClass.L or BidiClass.R or BidiClass.AL) lastStrong = types[k];
                else if (types[k] == BidiClass.EN && lastStrong == BidiClass.AL) types[k] = BidiClass.AN;
            }
            // W3: Arabic letters are right-to-left.
            for (int k = 0; k < m; k++) if (types[k] == BidiClass.AL) types[k] = BidiClass.R;
            // W4: a single separator between two numbers of the same kind joins them.
            for (int k = 1; k < m - 1; k++)
            {
                if (types[k] == BidiClass.ES && types[k - 1] == BidiClass.EN && types[k + 1] == BidiClass.EN) types[k] = BidiClass.EN;
                else if (types[k] == BidiClass.CS && types[k - 1] == types[k + 1] && types[k - 1] is BidiClass.EN or BidiClass.AN) types[k] = types[k - 1];
            }
            // W5: European terminators next to a European number join it.
            for (int k = 0; k < m; k++)
            {
                if (types[k] != BidiClass.ET) continue;
                int end = k; while (end < m && types[end] == BidiClass.ET) end++;
                if (k > 0 && types[k - 1] == BidiClass.EN || end < m && types[end] == BidiClass.EN)
                    for (int j = k; j < end; j++) types[j] = BidiClass.EN;
                k = end - 1;
            }
            // W6: remaining separators and terminators are neutral.
            for (int k = 0; k < m; k++) if (types[k] is BidiClass.ES or BidiClass.ET or BidiClass.CS) types[k] = BidiClass.ON;
            // W7: a European number in left-to-right context is left-to-right.
            lastStrong = embedding;
            for (int k = 0; k < m; k++)
            {
                if (types[k] is BidiClass.L or BidiClass.R) lastStrong = types[k];
                else if (types[k] == BidiClass.EN && lastStrong == BidiClass.L) types[k] = BidiClass.L;
            }
            ResolveBrackets(codePoints, original, sequence, types, embedding);
            // N1/N2: neutral sequences take the direction of matching surrounding strong text, else the embedding direction.
            for (int k = 0; k < m; k++)
            {
                if (!IsNeutral(types[k])) continue;
                int end = k; while (end < m && IsNeutral(types[end])) end++;
                var before = k == 0 ? embedding : StrongForNeutrals(types[k - 1]);
                var after = end == m ? embedding : StrongForNeutrals(types[end]);
                var resolved = before == after ? before : embedding;
                for (int j = k; j < end; j++) types[j] = resolved;
                k = end - 1;
            }
            // I1/I2: implicit levels.
            for (int k = 0; k < m; k++)
            {
                var t = types[k];
                levels[sequence[k]] = (byte)(level % 2 == 0
                    ? t == BidiClass.R ? level + 1 : t is BidiClass.AN or BidiClass.EN ? level + 2 : level
                    : t is BidiClass.L or BidiClass.EN or BidiClass.AN ? level + 1 : level);
            }
            for (int i = 0; i < n; i++) if (original[i] == BidiClass.BN) levels[i] = i == 0 ? level : levels[i - 1];
            return levels;
        }

        /// <summary>
        /// L1 for one line (a slice of a resolved paragraph): trailing white space and boundary neutrals return to the
        /// paragraph level. Returns the line's levels.
        /// </summary>
        public static byte[] LineLevels(int[] codePoints, byte[] paragraphLevels, int start, int length, byte paragraphLevel)
        {
            if (start < 0 || length < 0 || start + length > paragraphLevels.Length || paragraphLevels.Length != codePoints.Length)
                throw new ArgumentOutOfRangeException(nameof(length));
            var line = new byte[length];
            Array.Copy(paragraphLevels, start, line, 0, length);
            for (int i = length - 1; i >= 0 && GetClass(codePoints[start + i]) is BidiClass.WS or BidiClass.BN; i--) line[i] = paragraphLevel;
            return line;
        }

        /// <summary>L2: logical indexes in visual (left-to-right display) order for one line's levels.</summary>
        public static int[] VisualOrder(byte[] lineLevels)
        {
            var order = new int[lineLevels.Length];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            if (order.Length == 0) return order;
            // "Including intermediate levels not actually present": an even level contributes the odd level above it.
            int highest = 0, lowestOdd = int.MaxValue;
            foreach (var l in lineLevels) { highest = Math.Max(highest, l); lowestOdd = Math.Min(lowestOdd, l % 2 == 1 ? l : l + 1); }
            for (int target = highest; target >= lowestOdd; target--)
                for (int i = 0; i < order.Length; i++)
                {
                    if (lineLevels[order[i]] < target) continue;
                    int end = i; while (end < order.Length && lineLevels[order[end]] >= target) end++;
                    Array.Reverse(order, i, end - i);
                    i = end;
                }
            return order;
        }

        private static int FirstStrongLevel(BidiClass[] classes)
        {
            foreach (var c in classes)
            {
                if (c == BidiClass.L) return 0;
                if (c is BidiClass.R or BidiClass.AL) return 1;
            }
            return 0;
        }
        private static bool IsNeutral(BidiClass t) => t is BidiClass.WS or BidiClass.ON or BidiClass.B or BidiClass.S;
        // N1 treats European and Arabic numbers as right-to-left strong text.
        private static BidiClass StrongForNeutrals(BidiClass t) => t == BidiClass.L ? BidiClass.L : BidiClass.R;
        private static BidiClass? StrongForBrackets(BidiClass t) => t switch
        {
            BidiClass.L => BidiClass.L,
            BidiClass.R or BidiClass.AL or BidiClass.EN or BidiClass.AN => BidiClass.R,
            BidiClass.ES or BidiClass.ET or BidiClass.CS or BidiClass.NSM or BidiClass.BN or BidiClass.B or BidiClass.S
                or BidiClass.WS or BidiClass.ON or BidiClass.LRE or BidiClass.LRO or BidiClass.RLE or BidiClass.RLO
                or BidiClass.PDF or BidiClass.LRI or BidiClass.RLI or BidiClass.FSI or BidiClass.PDI => null,
        };

        // BD16 and N0: paired brackets take the direction of their content, or of the preceding context.
        private static void ResolveBrackets(int[] codePoints, BidiClass[] original, List<int> sequence, BidiClass[] types, BidiClass embedding)
        {
            var stack = new List<(int Closing, int Position)>();
            var pairs = new List<(int Open, int Close)>();
            for (int k = 0; k < types.Length; k++)
            {
                if (types[k] != BidiClass.ON || !TryBracket(codePoints[sequence[k]], out var pair, out var opening)) continue;
                if (opening)
                {
                    if (stack.Count == 63) break; // BD16: stop processing at stack overflow.
                    stack.Add((Canonical(pair), k));
                    continue;
                }
                var closing = Canonical(codePoints[sequence[k]]);
                for (int s = stack.Count - 1; s >= 0; s--)
                {
                    if (stack[s].Closing != closing) continue;
                    pairs.Add((stack[s].Position, k)); stack.RemoveRange(s, stack.Count - s);
                    break;
                }
            }
            pairs.Sort((a, b) => a.Open.CompareTo(b.Open));
            var opposite = embedding == BidiClass.L ? BidiClass.R : BidiClass.L;
            foreach (var (open, close) in pairs)
            {
                bool hasEmbedding = false, hasOpposite = false;
                for (int k = open + 1; k < close; k++)
                {
                    var strong = StrongForBrackets(types[k]);
                    if (strong == embedding) { hasEmbedding = true; break; }
                    if (strong == opposite) hasOpposite = true;
                }
                BidiClass? resolved = null;
                if (hasEmbedding) resolved = embedding;
                else if (hasOpposite)
                {
                    var context = embedding;
                    for (int k = open - 1; k >= 0; k--)
                        if (StrongForBrackets(types[k]) is { } strong) { context = strong; break; }
                    resolved = context == opposite ? opposite : embedding;
                }
                if (resolved is not { } value) continue;
                foreach (var bracket in new[] { open, close })
                {
                    types[bracket] = value;
                    // Marks that W1 attached to the bracket follow its new direction.
                    for (int k = bracket + 1; k < types.Length && original[sequence[k]] == BidiClass.NSM; k++) types[k] = value;
                }
            }
        }

        private static Dictionary<int, (int Pair, bool Opening)> _brackets;
        private static bool TryBracket(int codePoint, out int pair, out bool opening)
        {
            var map = _brackets;
            if (map == null)
            {
                map = new Dictionary<int, (int, bool)>(Brackets.Length);
                foreach (var b in Brackets) map[b.CodePoint] = (b.Pair, b.Opening);
                _brackets = map;
            }
            if (map.TryGetValue(codePoint, out var entry)) { pair = entry.Pair; opening = entry.Opening; return true; }
            pair = 0; opening = false; return false;
        }
        // BD16 compares brackets under canonical equivalence: U+2329/U+232A decompose to U+3008/U+3009.
        private static int Canonical(int codePoint) => codePoint switch { 0x2329 => 0x3008, 0x232A => 0x3009, _ => codePoint };
    }
}

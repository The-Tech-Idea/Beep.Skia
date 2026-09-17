using System;
using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia.Extensions.Marketplace
{
    /// <summary>
    /// Minimal semantic version (major.minor.patch with optional prerelease/build),
    /// used for extension and host version checks.
    /// </summary>
    public sealed class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
    {
        public SemanticVersion(int major, int minor = 0, int patch = 0, string prerelease = null)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Prerelease = string.IsNullOrWhiteSpace(prerelease) ? null : prerelease.Trim();
        }

        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        public string Prerelease { get; }

        public bool IsPrerelease => Prerelease != null;

        public static bool TryParse(string text, out SemanticVersion version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var body = text.Trim();
            var buildIndex = body.IndexOf('+');
            if (buildIndex >= 0) body = body.Substring(0, buildIndex);

            string prerelease = null;
            var preIndex = body.IndexOf('-');
            if (preIndex >= 0)
            {
                prerelease = body.Substring(preIndex + 1);
                body = body.Substring(0, preIndex);
            }

            var parts = body.Split('.');
            if (parts.Length == 0 || parts.Length > 3) return false;

            var numbers = new int[3];
            for (var i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out numbers[i]) || numbers[i] < 0) return false;
            }

            version = new SemanticVersion(numbers[0], numbers[1], numbers[2], prerelease);
            return true;
        }

        public static SemanticVersion Parse(string text)
            => TryParse(text, out var version) ? version : throw new FormatException($"'{text}' is not a valid semantic version.");

        public int CompareTo(SemanticVersion other)
        {
            if (other == null) return 1;

            var result = Major.CompareTo(other.Major);
            if (result != 0) return result;
            result = Minor.CompareTo(other.Minor);
            if (result != 0) return result;
            result = Patch.CompareTo(other.Patch);
            if (result != 0) return result;

            // A prerelease version is lower than the corresponding release.
            if (Prerelease == null && other.Prerelease == null) return 0;
            if (Prerelease == null) return 1;
            if (other.Prerelease == null) return -1;

            var left = Prerelease.Split('.');
            var right = other.Prerelease.Split('.');
            for (var i = 0; i < Math.Max(left.Length, right.Length); i++)
            {
                if (i >= left.Length) return -1;
                if (i >= right.Length) return 1;

                var leftNumeric = int.TryParse(left[i], out var leftNumber);
                var rightNumeric = int.TryParse(right[i], out var rightNumber);
                if (leftNumeric && rightNumeric)
                {
                    result = leftNumber.CompareTo(rightNumber);
                }
                else
                {
                    result = string.CompareOrdinal(left[i], right[i]);
                }
                if (result != 0) return result;
            }

            return 0;
        }

        public bool Equals(SemanticVersion other) => other != null && CompareTo(other) == 0;

        public override bool Equals(object obj) => Equals(obj as SemanticVersion);

        public override int GetHashCode()
            => HashCode.Combine(Major, Minor, Patch, Prerelease ?? string.Empty);

        public override string ToString()
            => Prerelease == null ? $"{Major}.{Minor}.{Patch}" : $"{Major}.{Minor}.{Patch}-{Prerelease}";

        public static bool operator >(SemanticVersion left, SemanticVersion right) => Compare(left, right) > 0;
        public static bool operator <(SemanticVersion left, SemanticVersion right) => Compare(left, right) < 0;
        public static bool operator >=(SemanticVersion left, SemanticVersion right) => Compare(left, right) >= 0;
        public static bool operator <=(SemanticVersion left, SemanticVersion right) => Compare(left, right) <= 0;

        private static int Compare(SemanticVersion left, SemanticVersion right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left is null) return -1;
            return left.CompareTo(right);
        }
    }
}

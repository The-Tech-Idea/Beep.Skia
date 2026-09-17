using System;

namespace Beep.Skia.Security
{
    /// <summary>
    /// DREAD rating level.
    /// </summary>
    public enum DreadRating
    {
        Low = 1,
        Medium = 2,
        High = 3
    }

    /// <summary>
    /// DREAD risk score (Damage, Reproducibility, Exploitability, Affected users, Discoverability).
    /// </summary>
    public class DreadScore
    {
        public DreadRating Damage { get; set; } = DreadRating.Medium;
        public DreadRating Reproducibility { get; set; } = DreadRating.Medium;
        public DreadRating Exploitability { get; set; } = DreadRating.Medium;
        public DreadRating AffectedUsers { get; set; } = DreadRating.Medium;
        public DreadRating Discoverability { get; set; } = DreadRating.Medium;

        /// <summary>Average rating (1.0 - 3.0).</summary>
        public double Average
            => ((int)Damage + (int)Reproducibility + (int)Exploitability + (int)AffectedUsers + (int)Discoverability) / 5.0;

        /// <summary>Risk level derived from the average (Low / Medium / High).</summary>
        public string RiskLevel => DreadCalculator.RiskLevel(Average);

        public override string ToString()
            => $"DREAD {Average:0.00} ({RiskLevel}) [D={(int)Damage} R={(int)Reproducibility} E={(int)Exploitability} A={(int)AffectedUsers} D={(int)Discoverability}]";
    }

    /// <summary>
    /// Computes DREAD scores from threat severity and likelihood.
    /// </summary>
    public static class DreadCalculator
    {
        /// <summary>
        /// Computes a DREAD score for a threat node.
        /// </summary>
        public static DreadScore Calculate(ThreatNode threat)
            => threat == null ? new DreadScore() : Calculate(threat.Severity, threat.Likelihood);

        /// <summary>
        /// Computes a DREAD score from severity and likelihood ratings.
        /// </summary>
        public static DreadScore Calculate(Severity severity, Likelihood likelihood)
        {
            return new DreadScore
            {
                Damage = MapSeverity(severity),
                Reproducibility = MapLikelihood(likelihood),
                Exploitability = severity >= Severity.High ? DreadRating.High : DreadRating.Medium,
                AffectedUsers = MapSeverity(severity),
                Discoverability = MapLikelihood(likelihood)
            };
        }

        /// <summary>
        /// Computes a DREAD score from text values (e.g., STRIDE analyzer output).
        /// Unknown values fall back to Medium/Possible.
        /// </summary>
        public static DreadScore Calculate(string severity, string likelihood)
        {
            var parsedSeverity = Enum.TryParse<Severity>(severity, true, out var s) ? s : Severity.Medium;
            var parsedLikelihood = Enum.TryParse<Likelihood>(likelihood, true, out var l) ? l : Likelihood.Possible;
            return Calculate(parsedSeverity, parsedLikelihood);
        }

        /// <summary>
        /// Maps an average DREAD rating to a risk level.
        /// </summary>
        public static string RiskLevel(double average)
            => average >= 2.5 ? "High" : average >= 1.75 ? "Medium" : "Low";

        private static DreadRating MapSeverity(Severity severity) => severity switch
        {
            Severity.Critical => DreadRating.High,
            Severity.High => DreadRating.High,
            Severity.Medium => DreadRating.Medium,
            _ => DreadRating.Low
        };

        private static DreadRating MapLikelihood(Likelihood likelihood) => likelihood switch
        {
            Likelihood.AlmostCertain => DreadRating.High,
            Likelihood.Likely => DreadRating.High,
            Likelihood.Possible => DreadRating.Medium,
            _ => DreadRating.Low
        };
    }
}

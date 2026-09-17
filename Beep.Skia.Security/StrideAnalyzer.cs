using System;
using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia.Security
{
    /// <summary>
    /// Analyzes a security diagram using the STRIDE threat modeling methodology.
    /// Auto-generates threat nodes per asset based on STRIDE categories.
    /// </summary>
    public class StrideAnalyzer
    {
        public enum StrideCategory
        {
            Spoofing,
            Tampering,
            Repudiation,
            InformationDisclosure,
            DenialOfService,
            ElevationOfPrivilege
        }

        public class ThreatResult
        {
            /// <summary>
            /// Gets or sets the category.
            /// </summary>
            public StrideCategory Category { get; set; }
            /// <summary>
            /// Gets or sets the asset name.
            /// </summary>
            public string AssetName { get; set; } = string.Empty;
            /// <summary>
            /// Gets or sets the threat.
            /// </summary>
            public string Threat { get; set; } = string.Empty;
            /// <summary>
            /// Gets or sets the mitigation.
            /// </summary>
            public string Mitigation { get; set; } = string.Empty;
            /// <summary>
            /// Gets or sets the severity.
            /// </summary>
            public string Severity { get; set; } = "Medium";

            /// <summary>MITRE ATT&amp;CK techniques mapped to this threat's STRIDE category.</summary>
            public List<MitreTechnique> Techniques { get; } = new List<MitreTechnique>();

            /// <summary>DREAD risk score derived from severity/likelihood.</summary>
            public DreadScore Dread { get; set; } = new DreadScore();

            /// <summary>
            /// Gets or sets the to string.
            /// </summary>
            public override string ToString() => $"[{Category}] {Threat} ({Severity}, {Dread.RiskLevel})";
        }

        /// <summary>
        /// Gets or sets the threats.
        /// </summary>
        public List<ThreatResult> Threats { get; } = new List<ThreatResult>();

        /// <summary>
        /// Runs STRIDE analysis on the given components.
        /// </summary>
        public void Analyze(IReadOnlyList<SkiaComponent> components)
        {
            Threats.Clear();
            var assets = components.Where(c => c is AssetNode).Cast<AssetNode>();

            foreach (var asset in assets)
            {
                GenerateThreats(asset.Name ?? "Unnamed", asset.Category.ToString());
            }

            // Enrich threats with MITRE ATT&CK techniques and DREAD risk scores.
            foreach (var threat in Threats)
            {
                threat.Techniques.Clear();
                threat.Techniques.AddRange(MitreAttackLibrary.ForStrideCategory(threat.Category));
                threat.Dread = DreadCalculator.Calculate(threat.Severity, "Possible");
            }
        }

        private void GenerateThreats(string assetName, string assetType)
        {
            var type = (assetType ?? "").ToUpperInvariant();
            bool isData = type.Contains("DATA") || type.Contains("DATABASE");
            bool isService = type.Contains("SERVICE") || type.Contains("API") || type.Contains("SYSTEM");
            bool isNetwork = type.Contains("NETWORK") || type.Contains("ENDPOINT");
            bool isUser = type.Contains("USER") || type.Contains("PERSONNEL");

            // Spoofing — identity forgery
            if (isUser || isService)
            {
                Threats.Add(new ThreatResult
                {
                    Category = StrideCategory.Spoofing,
                    AssetName = assetName,
                    Threat = $"Attacker impersonates a legitimate {assetName} identity",
                    Mitigation = "Implement multi-factor authentication, use strong credential storage, enforce session management",
                    Severity = isUser ? "High" : "Medium"
                });
            }

            // Tampering — data modification
            if (isData || isNetwork)
            {
                Threats.Add(new ThreatResult
                {
                    Category = StrideCategory.Tampering,
                    AssetName = assetName,
                    Threat = $"Unauthorized modification of {assetName} data in transit or at rest",
                    Mitigation = "Use TLS for data in transit, implement integrity checks (HMAC/digital signatures), enable audit logging",
                    Severity = isData ? "High" : "Medium"
                });
            }

            // Repudiation — denying actions
            if (isData || isService)
            {
                Threats.Add(new ThreatResult
                {
                    Category = StrideCategory.Repudiation,
                    AssetName = assetName,
                    Threat = $"User denies performing action affecting {assetName}",
                    Mitigation = "Implement comprehensive audit logging, use digital signatures for transactions, maintain access logs",
                    Severity = "Medium"
                });
            }

            // Information Disclosure — data leakage
            if (isData)
            {
                Threats.Add(new ThreatResult
                {
                    Category = StrideCategory.InformationDisclosure,
                    AssetName = assetName,
                    Threat = $"Sensitive data in {assetName} exposed to unauthorized parties",
                    Mitigation = "Encrypt data at rest (AES-256), use TLS 1.3 for data in transit, implement access control (RBAC), classify data sensitivity",
                    Severity = "Critical"
                });
            }

            // Denial of Service
            if (isService || isNetwork)
            {
                Threats.Add(new ThreatResult
                {
                    Category = StrideCategory.DenialOfService,
                    AssetName = assetName,
                    Threat = $"Service {assetName} overwhelmed by malicious traffic or resource exhaustion",
                    Mitigation = "Implement rate limiting, use CDN/DDoS protection, configure auto-scaling, add circuit breakers",
                    Severity = isService ? "High" : "Medium"
                });
            }

            // Elevation of Privilege
            if (isService || isUser)
            {
                Threats.Add(new ThreatResult
                {
                    Category = StrideCategory.ElevationOfPrivilege,
                    AssetName = assetName,
                    Threat = $"Attacker gains elevated permissions on {assetName}",
                    Mitigation = "Follow principle of least privilege, implement input validation, run services with minimal permissions, patch regularly",
                    Severity = "Critical"
                });
            }
        }

        /// <summary>
        /// Calculates a DREAD risk score (0-15) for a given threat.
        /// </summary>
        public static int CalculateDreadScore(int damage, int reproducibility, int exploitability, int affectedUsers, int discoverability)
        {
            return Math.Clamp(damage, 0, 3) + Math.Clamp(reproducibility, 0, 3)
                 + Math.Clamp(exploitability, 0, 3) + Math.Clamp(affectedUsers, 0, 3)
                 + Math.Clamp(discoverability, 0, 3);
        }
    }
}

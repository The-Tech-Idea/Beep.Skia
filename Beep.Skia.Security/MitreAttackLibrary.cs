using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia.Security
{
    /// <summary>
    /// A MITRE ATT&amp;CK technique entry.
    /// </summary>
    public class MitreTechnique
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the tactic.
        /// </summary>
        public string Tactic { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the to string.
        /// </summary>
        public override string ToString() => $"{Id} {Name} ({Tactic})";
    }

    /// <summary>
    /// A compact MITRE ATT&amp;CK technique library with STRIDE category mapping,
    /// used to enrich threat models with adversary techniques.
    /// </summary>
    public static class MitreAttackLibrary
    {
        /// <summary>All techniques in the library.</summary>
        public static IReadOnlyList<MitreTechnique> Techniques { get; } = new List<MitreTechnique>
        {
            new MitreTechnique { Id = "T1078", Name = "Valid Accounts", Tactic = "Initial Access" },
            new MitreTechnique { Id = "T1190", Name = "Exploit Public-Facing Application", Tactic = "Initial Access" },
            new MitreTechnique { Id = "T1566", Name = "Phishing", Tactic = "Initial Access" },
            new MitreTechnique { Id = "T1059", Name = "Command and Scripting Interpreter", Tactic = "Execution" },
            new MitreTechnique { Id = "T1204", Name = "User Execution", Tactic = "Execution" },
            new MitreTechnique { Id = "T1098", Name = "Account Manipulation", Tactic = "Persistence" },
            new MitreTechnique { Id = "T1136", Name = "Create Account", Tactic = "Persistence" },
            new MitreTechnique { Id = "T1543", Name = "Create or Modify System Process", Tactic = "Persistence" },
            new MitreTechnique { Id = "T1068", Name = "Exploitation for Privilege Escalation", Tactic = "Privilege Escalation" },
            new MitreTechnique { Id = "T1548", Name = "Abuse Elevation Control Mechanism", Tactic = "Privilege Escalation" },
            new MitreTechnique { Id = "T1070", Name = "Indicator Removal", Tactic = "Defense Evasion" },
            new MitreTechnique { Id = "T1562", Name = "Impair Defenses", Tactic = "Defense Evasion" },
            new MitreTechnique { Id = "T1027", Name = "Obfuscated Files or Information", Tactic = "Defense Evasion" },
            new MitreTechnique { Id = "T1110", Name = "Brute Force", Tactic = "Credential Access" },
            new MitreTechnique { Id = "T1003", Name = "OS Credential Dumping", Tactic = "Credential Access" },
            new MitreTechnique { Id = "T1555", Name = "Credentials from Password Stores", Tactic = "Credential Access" },
            new MitreTechnique { Id = "T1528", Name = "Steal Application Access Token", Tactic = "Credential Access" },
            new MitreTechnique { Id = "T1046", Name = "Network Service Discovery", Tactic = "Discovery" },
            new MitreTechnique { Id = "T1087", Name = "Account Discovery", Tactic = "Discovery" },
            new MitreTechnique { Id = "T1021", Name = "Remote Services", Tactic = "Lateral Movement" },
            new MitreTechnique { Id = "T1570", Name = "Lateral Tool Transfer", Tactic = "Lateral Movement" },
            new MitreTechnique { Id = "T1530", Name = "Data from Cloud Storage", Tactic = "Collection" },
            new MitreTechnique { Id = "T1005", Name = "Data from Local System", Tactic = "Collection" },
            new MitreTechnique { Id = "T1056", Name = "Input Capture", Tactic = "Collection" },
            new MitreTechnique { Id = "T1041", Name = "Exfiltration Over C2 Channel", Tactic = "Exfiltration" },
            new MitreTechnique { Id = "T1567", Name = "Exfiltration Over Web Service", Tactic = "Exfiltration" },
            new MitreTechnique { Id = "T1048", Name = "Exfiltration Over Alternative Protocol", Tactic = "Exfiltration" },
            new MitreTechnique { Id = "T1485", Name = "Data Destruction", Tactic = "Impact" },
            new MitreTechnique { Id = "T1565", Name = "Data Manipulation", Tactic = "Impact" },
            new MitreTechnique { Id = "T1486", Name = "Data Encrypted for Impact", Tactic = "Impact" },
            new MitreTechnique { Id = "T1490", Name = "Inhibit System Recovery", Tactic = "Impact" },
            new MitreTechnique { Id = "T1499", Name = "Endpoint Denial of Service", Tactic = "Impact" },
            new MitreTechnique { Id = "T1498", Name = "Network Denial of Service", Tactic = "Impact" }
        };

        /// <summary>Distinct tactic names present in the library.</summary>
        public static IEnumerable<string> Tactics
            => Techniques.Select(t => t.Tactic).Distinct();

        /// <summary>
        /// Gets techniques relevant to a STRIDE category.
        /// </summary>
        public static IReadOnlyList<MitreTechnique> ForStrideCategory(StrideAnalyzer.StrideCategory category)
        {
            IEnumerable<string> ids = category switch
            {
                StrideAnalyzer.StrideCategory.Spoofing => new[] { "T1078", "T1110", "T1528", "T1555" },
                StrideAnalyzer.StrideCategory.Tampering => new[] { "T1565", "T1485", "T1486", "T1027" },
                StrideAnalyzer.StrideCategory.Repudiation => new[] { "T1070", "T1562", "T1098" },
                StrideAnalyzer.StrideCategory.InformationDisclosure => new[] { "T1530", "T1005", "T1041", "T1567", "T1048", "T1056" },
                StrideAnalyzer.StrideCategory.DenialOfService => new[] { "T1499", "T1498", "T1490" },
                StrideAnalyzer.StrideCategory.ElevationOfPrivilege => new[] { "T1068", "T1548", "T1078", "T1543", "T1136" },
                _ => Enumerable.Empty<string>()
            };

            return ids
                .Select(Find)
                .Where(t => t != null)
                .Select(t => t!)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Finds a technique by id (case-insensitive), e.g., "T1078".
        /// </summary>
        public static MitreTechnique? Find(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return Techniques.FirstOrDefault(t => string.Equals(t.Id, id.Trim(), System.StringComparison.OrdinalIgnoreCase));
        }
    }
}

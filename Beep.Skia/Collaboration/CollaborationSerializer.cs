using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Beep.Skia.Collaboration
{
    /// <summary>
    /// Serializable collaboration state (users, shares, comments, audit).
    /// Presence is intentionally excluded — it is ephemeral.
    /// </summary>
    public class CollaborationSnapshot
    {
        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        public int Version { get; set; } = 1;
        /// <summary>
        /// Gets or sets the users.
        /// </summary>
        public List<UserRecord> Users { get; set; } = new List<UserRecord>();
        /// <summary>
        /// Gets or sets the shares.
        /// </summary>
        public List<DocumentShare> Shares { get; set; } = new List<DocumentShare>();
        /// <summary>
        /// Gets or sets the comments.
        /// </summary>
        public List<DiagramComment> Comments { get; set; } = new List<DiagramComment>();
        /// <summary>
        /// Gets or sets the audit.
        /// </summary>
        public List<AuditEntry> Audit { get; set; } = new List<AuditEntry>();
    }

    /// <summary>
    /// JSON-friendly user record (color stored as 0xAARRGGBB).
    /// </summary>
    public class UserRecord
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        public string Email { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        public uint Color { get; set; } = 0xFF1E88E5;
    }

    /// <summary>
    /// Persists collaboration state to and from JSON.
    /// </summary>
    public static class CollaborationSerializer
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        /// <summary>Serializes a service's state to JSON.</summary>
        public static string ToJson(CollaborationService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            return JsonSerializer.Serialize(service.CreateSnapshot(), Options);
        }

        /// <summary>Restores a new service from JSON.</summary>
        public static CollaborationService FromJson(string json)
        {
            var service = new CollaborationService();
            if (!string.IsNullOrWhiteSpace(json))
            {
                var snapshot = JsonSerializer.Deserialize<CollaborationSnapshot>(json, Options);
                service.Restore(snapshot);
            }
            return service;
        }

        /// <summary>Writes collaboration state to a file.</summary>
        public static void Save(CollaborationService service, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));
            File.WriteAllText(filePath, ToJson(service));
        }

        /// <summary>Reads collaboration state from a file.</summary>
        public static CollaborationService Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));
            return FromJson(File.ReadAllText(filePath));
        }
    }
}

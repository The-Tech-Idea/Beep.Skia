using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Beep.Skia.Collaboration
{
    /// <summary>
    /// Permission level a user has on a shared diagram document.
    /// </summary>
    public enum CollaborationRole
    {
        Viewer,
        Commenter,
        Editor,
        Admin
    }

    /// <summary>
    /// A user participating in collaboration.
    /// </summary>
    public class CollaborationUser
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

        /// <summary>Presence color used for cursors/avatars.</summary>
        public SKColor Color { get; set; } = new SKColor(0x1E, 0x88, 0xE5);
    }

    /// <summary>
    /// Sharing state of a diagram document.
    /// </summary>
    public class DocumentShare
    {
        /// <summary>
        /// Gets or sets the document id.
        /// </summary>
        public string DocumentId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the owner id.
        /// </summary>
        public string OwnerId { get; set; } = string.Empty;

        /// <summary>Member roles (userId → role). The owner is always Admin.</summary>
        public Dictionary<string, CollaborationRole> Members { get; set; } =
            new Dictionary<string, CollaborationRole>(StringComparer.OrdinalIgnoreCase);

        /// <summary>When true, any registered user can view the document.</summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// Gets or sets the to string.
        /// </summary>
        public override string ToString()
            => $"{DocumentId}: owner={OwnerId}, members={Members.Count}, public={IsPublic}";
    }

    /// <summary>
    /// A comment, optionally anchored to a component in the diagram.
    /// </summary>
    public class DiagramComment
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
        /// <summary>
        /// Gets or sets the document id.
        /// </summary>
        public string DocumentId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the author id.
        /// </summary>
        public string AuthorId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>Optional anchor: component name/id the comment refers to.</summary>
        public string ComponentId { get; set; }

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets the resolved.
        /// </summary>
        public bool Resolved { get; set; }
        /// <summary>
        /// Gets or sets the resolved by.
        /// </summary>
        public string ResolvedBy { get; set; }
        /// <summary>
        /// Gets or sets the resolved at.
        /// </summary>
        public DateTime? ResolvedAt { get; set; }
    }

    /// <summary>
    /// Live presence for a user on a document.
    /// </summary>
    public class PresenceEntry
    {
        /// <summary>
        /// Gets or sets the document id.
        /// </summary>
        public string DocumentId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the last seen.
        /// </summary>
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets the activity.
        /// </summary>
        public string Activity { get; set; } = "viewing";
    }

    /// <summary>
    /// An audit-trail entry for a collaboration action.
    /// </summary>
    public class AuditEntry
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
        /// <summary>
        /// Gets or sets the document id.
        /// </summary>
        public string DocumentId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        public string Action { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the details.
        /// </summary>
        public string Details { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the to string.
        /// </summary>
        public override string ToString() => $"{Timestamp:u} {UserId} {Action} {Details}";
    }
}

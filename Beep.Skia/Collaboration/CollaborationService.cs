using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace Beep.Skia.Collaboration
{
    /// <summary>
    /// In-memory collaboration service: users, document sharing with roles, comments,
    /// presence tracking, and an audit trail. Transport-agnostic so it can back a desktop
    /// host, a REST API, or a real-time server.
    ///
    /// All state is guarded by a single lock: the service is shared by UI and server callers,
    /// so concurrent comments/grants/presence updates must not corrupt it. Read APIs return
    /// snapshots so callers cannot mutate internal state (role changes must go through
    /// <see cref="Grant"/>/<see cref="Revoke"/>/<see cref="SetPublic"/>).
    /// </summary>
    public class CollaborationService
    {
        private readonly Dictionary<string, CollaborationUser> _users =
            new Dictionary<string, CollaborationUser>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DocumentShare> _shares =
            new Dictionary<string, DocumentShare>(StringComparer.OrdinalIgnoreCase);
        private readonly List<DiagramComment> _comments = new List<DiagramComment>();
        private readonly List<PresenceEntry> _presence = new List<PresenceEntry>();
        private readonly List<AuditEntry> _audit = new List<AuditEntry>();

        /// <summary>Guards all mutable state: the service is shared by UI and server callers.</summary>
        private readonly object _sync = new object();

        /// <summary>Registered users.</summary>
        public IReadOnlyList<CollaborationUser> Users
        {
            get { lock (_sync) { return _users.Values.ToList().AsReadOnly(); } }
        }

        /// <summary>All comments across documents.</summary>
        public IReadOnlyList<DiagramComment> AllComments
        {
            get { lock (_sync) { return _comments.ToList().AsReadOnly(); } }
        }

        /// <summary>Full audit trail.</summary>
        public IReadOnlyList<AuditEntry> AuditTrail
        {
            get { lock (_sync) { return _audit.ToList().AsReadOnly(); } }
        }

        // ── Users ────────────────────────────────────────────────────────────

        /// <summary>Registers or replaces a user.</summary>
        public void RegisterUser(CollaborationUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(user.Id))
                throw new ArgumentException("User id is required.", nameof(user));

            lock (_sync)
            {
                _users[user.Id] = user;
            }
        }

        /// <summary>Gets a user by id, or null.</summary>
        public CollaborationUser GetUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;
            lock (_sync)
            {
                return _users.TryGetValue(userId, out var user) ? user : null;
            }
        }

        // ── Sharing ──────────────────────────────────────────────────────────

        /// <summary>
        /// Shares a document with the given members. The owner is registered as Admin.
        /// Returns a snapshot of the resulting share.
        /// </summary>
        public DocumentShare Share(
            string documentId,
            string ownerId,
            params (string UserId, CollaborationRole Role)[] members)
        {
            if (string.IsNullOrWhiteSpace(documentId))
                throw new ArgumentException("Document id is required.", nameof(documentId));

            lock (_sync)
            {
                if (!_users.ContainsKey(ownerId ?? string.Empty))
                    throw new ArgumentException($"Owner '{ownerId}' is not a registered user.", nameof(ownerId));

                var share = new DocumentShare { DocumentId = documentId, OwnerId = ownerId };
                share.Members[ownerId] = CollaborationRole.Admin;

                if (members != null)
                {
                    foreach (var (userId, role) in members)
                    {
                        if (string.IsNullOrWhiteSpace(userId)) continue;
                        if (!_users.ContainsKey(userId)) continue;
                        share.Members[userId] = role;
                    }
                }

                _shares[documentId] = share;
                Audit(documentId, ownerId, "share", $"members={share.Members.Count}");
                return CopyOf(share);
            }
        }

        /// <summary>Gets a snapshot of the sharing state for a document, or null when not shared.</summary>
        public DocumentShare GetShare(string documentId)
        {
            if (string.IsNullOrWhiteSpace(documentId)) return null;
            lock (_sync)
            {
                return _shares.TryGetValue(documentId, out var share) ? CopyOf(share) : null;
            }
        }

        /// <summary>Gets a user's role on a document (public documents grant Viewer).</summary>
        public CollaborationRole? GetRole(string documentId, string userId)
        {
            lock (_sync)
            {
                if (!_shares.TryGetValue(documentId ?? string.Empty, out var share)) return null;
                if (share.Members.TryGetValue(userId ?? string.Empty, out var role)) return role;
                return share.IsPublic ? CollaborationRole.Viewer : (CollaborationRole?)null;
            }
        }

        public bool CanView(string documentId, string userId) => GetRole(documentId, userId) != null;

        public bool CanComment(string documentId, string userId)
            => GetRole(documentId, userId) >= CollaborationRole.Commenter;

        public bool CanEdit(string documentId, string userId)
            => GetRole(documentId, userId) >= CollaborationRole.Editor;

        public bool CanAdminister(string documentId, string userId)
            => GetRole(documentId, userId) == CollaborationRole.Admin;

        /// <summary>Grants or updates a member role (admin only).</summary>
        public bool Grant(string documentId, string requesterId, string userId, CollaborationRole role)
        {
            lock (_sync)
            {
                if (!_shares.TryGetValue(documentId ?? string.Empty, out var share)) return false;
                if (GetRole(documentId, requesterId) != CollaborationRole.Admin) return false;
                if (string.IsNullOrWhiteSpace(userId) || !_users.ContainsKey(userId)) return false;

                share.Members[userId] = role;
                Audit(documentId, requesterId, "grant", $"{userId}={role}");
                return true;
            }
        }

        /// <summary>Revokes a member (admin only; the owner cannot be revoked).</summary>
        public bool Revoke(string documentId, string requesterId, string userId)
        {
            lock (_sync)
            {
                if (!_shares.TryGetValue(documentId ?? string.Empty, out var share)) return false;
                if (GetRole(documentId, requesterId) != CollaborationRole.Admin) return false;
                if (string.Equals(share.OwnerId, userId, StringComparison.OrdinalIgnoreCase)) return false;

                if (share.Members.Remove(userId))
                {
                    Audit(documentId, requesterId, "revoke", userId);
                    return true;
                }
                return false;
            }
        }

        /// <summary>Toggles public (view-only) access (admin only).</summary>
        public bool SetPublic(string documentId, string requesterId, bool isPublic)
        {
            lock (_sync)
            {
                if (!_shares.TryGetValue(documentId ?? string.Empty, out var share)) return false;
                if (GetRole(documentId, requesterId) != CollaborationRole.Admin) return false;

                share.IsPublic = isPublic;
                Audit(documentId, requesterId, "set-public", isPublic.ToString());
                return true;
            }
        }

        // ── Comments ─────────────────────────────────────────────────────────

        /// <summary>Adds a comment (requires Commenter or higher). Returns null when not permitted.</summary>
        public DiagramComment AddComment(string documentId, string userId, string text, string componentId = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            lock (_sync)
            {
                if (!(GetRole(documentId, userId) >= CollaborationRole.Commenter)) return null;

                var comment = new DiagramComment
                {
                    DocumentId = documentId,
                    AuthorId = userId,
                    Text = text.Trim(),
                    ComponentId = string.IsNullOrWhiteSpace(componentId) ? null : componentId
                };
                _comments.Add(comment);
                Audit(documentId, userId, "comment", comment.Id + (comment.ComponentId != null ? $" on {comment.ComponentId}" : string.Empty));
                return comment;
            }
        }

        /// <summary>Resolves a comment (author or admin only).</summary>
        public bool ResolveComment(string documentId, string commentId, string userId)
        {
            lock (_sync)
            {
                var comment = _comments.FirstOrDefault(c =>
                    string.Equals(c.DocumentId, documentId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(c.Id, commentId, StringComparison.OrdinalIgnoreCase));
                if (comment == null || comment.Resolved) return false;

                bool isAuthor = string.Equals(comment.AuthorId, userId, StringComparison.OrdinalIgnoreCase);
                if (!isAuthor && GetRole(documentId, userId) != CollaborationRole.Admin) return false;

                comment.Resolved = true;
                comment.ResolvedBy = userId;
                comment.ResolvedAt = DateTime.UtcNow;
                Audit(documentId, userId, "resolve", comment.Id);
                return true;
            }
        }

        /// <summary>Gets comments for a document (optionally including resolved ones).</summary>
        public IReadOnlyList<DiagramComment> GetComments(string documentId, bool includeResolved = false)
        {
            lock (_sync)
            {
                return _comments
                    .Where(c => string.Equals(c.DocumentId, documentId, StringComparison.OrdinalIgnoreCase) &&
                                (includeResolved || !c.Resolved))
                    .ToList()
                    .AsReadOnly();
            }
        }

        // ── Presence ─────────────────────────────────────────────────────────

        /// <summary>Marks a user as active on a document (requires view permission).</summary>
        public void SetPresence(string documentId, string userId, string activity = null)
        {
            lock (_sync)
            {
                if (GetRole(documentId, userId) == null) return;

                var entry = _presence.FirstOrDefault(p =>
                    string.Equals(p.DocumentId, documentId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(p.UserId, userId, StringComparison.OrdinalIgnoreCase));

                if (entry == null)
                {
                    _presence.Add(new PresenceEntry
                    {
                        DocumentId = documentId,
                        UserId = userId,
                        Activity = string.IsNullOrWhiteSpace(activity) ? "viewing" : activity
                    });
                }
                else
                {
                    entry.LastSeen = DateTime.UtcNow;
                    if (!string.IsNullOrWhiteSpace(activity)) entry.Activity = activity;
                }
            }
        }

        /// <summary>Gets active presence entries within the given window (default 5 minutes).</summary>
        public IReadOnlyList<PresenceEntry> GetPresence(string documentId, TimeSpan? activeWindow = null)
        {
            var cutoff = DateTime.UtcNow - (activeWindow ?? TimeSpan.FromMinutes(5));
            lock (_sync)
            {
                return _presence
                    .Where(p => string.Equals(p.DocumentId, documentId, StringComparison.OrdinalIgnoreCase) &&
                                p.LastSeen >= cutoff)
                    .ToList()
                    .AsReadOnly();
            }
        }

        /// <summary>Removes a user's presence from a document.</summary>
        public bool RemovePresence(string documentId, string userId)
        {
            lock (_sync)
            {
                return _presence.RemoveAll(p =>
                    string.Equals(p.DocumentId, documentId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(p.UserId, userId, StringComparison.OrdinalIgnoreCase)) > 0;
            }
        }

        // ── Audit ────────────────────────────────────────────────────────────

        /// <summary>Gets the audit trail for a document (newest last).</summary>
        public IReadOnlyList<AuditEntry> GetAudit(string documentId)
        {
            lock (_sync)
            {
                return _audit
                    .Where(a => string.Equals(a.DocumentId, documentId, StringComparison.OrdinalIgnoreCase))
                    .ToList()
                    .AsReadOnly();
            }
        }

        // ── Persistence ──────────────────────────────────────────────────────

        /// <summary>Captures the persistable state (users, shares, comments, audit).</summary>
        public CollaborationSnapshot CreateSnapshot()
        {
            lock (_sync)
            {
                var snapshot = new CollaborationSnapshot
                {
                    Users = _users.Values.Select(u => new UserRecord
                    {
                        Id = u.Id,
                        DisplayName = u.DisplayName,
                        Email = u.Email,
                        Color = (uint)u.Color
                    }).ToList(),
                    Shares = _shares.Values.Select(s => new DocumentShare
                    {
                        DocumentId = s.DocumentId,
                        OwnerId = s.OwnerId,
                        IsPublic = s.IsPublic
                    }).ToList(),
                    Comments = _comments.ToList(),
                    Audit = _audit.ToList()
                };

                foreach (var share in snapshot.Shares)
                {
                    if (_shares.TryGetValue(share.DocumentId, out var original))
                    {
                        foreach (var pair in original.Members)
                            share.Members[pair.Key] = pair.Value;
                    }
                }

                return snapshot;
            }
        }

        /// <summary>Replaces all state from a snapshot (no-op when null).</summary>
        public void Restore(CollaborationSnapshot snapshot)
        {
            if (snapshot == null) return;

            lock (_sync)
            {
                _users.Clear();
                _shares.Clear();
                _comments.Clear();
                _presence.Clear();
                _audit.Clear();

                foreach (var user in snapshot.Users ?? new List<UserRecord>())
                {
                    if (string.IsNullOrWhiteSpace(user.Id)) continue;
                    _users[user.Id] = new CollaborationUser
                    {
                        Id = user.Id,
                        DisplayName = user.DisplayName,
                        Email = user.Email,
                        Color = new SKColor(
                            (byte)(user.Color >> 16),
                            (byte)(user.Color >> 8),
                            (byte)user.Color,
                            (byte)(user.Color >> 24))
                    };
                }

                foreach (var share in snapshot.Shares ?? new List<DocumentShare>())
                {
                    if (string.IsNullOrWhiteSpace(share.DocumentId)) continue;
                    if (share.Members == null)
                        share.Members = new Dictionary<string, CollaborationRole>(StringComparer.OrdinalIgnoreCase);
                    else if (!ReferenceEquals(share.Members.Comparer, StringComparer.OrdinalIgnoreCase))
                        share.Members = new Dictionary<string, CollaborationRole>(share.Members, StringComparer.OrdinalIgnoreCase);
                    _shares[share.DocumentId] = share;
                }

                if (snapshot.Comments != null) _comments.AddRange(snapshot.Comments);
                if (snapshot.Audit != null) _audit.AddRange(snapshot.Audit);
            }
        }

        /// <summary>Creates a detached copy of a share so callers cannot mutate internal state.</summary>
        private static DocumentShare CopyOf(DocumentShare share)
        {
            var copy = new DocumentShare
            {
                DocumentId = share.DocumentId,
                OwnerId = share.OwnerId,
                IsPublic = share.IsPublic
            };
            foreach (var pair in share.Members) copy.Members[pair.Key] = pair.Value;
            return copy;
        }

        /// <summary>Appends an audit entry. Callers must hold <see cref="_sync"/>.</summary>
        private void Audit(string documentId, string userId, string action, string details)
        {
            _audit.Add(new AuditEntry
            {
                DocumentId = documentId,
                UserId = userId,
                Action = action,
                Details = details ?? string.Empty
            });
        }
    }
}

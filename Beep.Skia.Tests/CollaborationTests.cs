using System;
using System.Linq;
using Beep.Skia.Collaboration;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for the collaboration service: sharing, roles, comments, presence, and audit.
    /// </summary>
    public class CollaborationTests
    {
        private const string Doc = "diagram-1";

        private static CollaborationService CreateService()
        {
            var service = new CollaborationService();
            service.RegisterUser(new CollaborationUser { Id = "alice", DisplayName = "Alice" });
            service.RegisterUser(new CollaborationUser { Id = "bob", DisplayName = "Bob" });
            service.RegisterUser(new CollaborationUser { Id = "carol", DisplayName = "Carol" });
            return service;
        }

        [Fact]
        public void Share_SetsOwnerAsAdminAndMembers()
        {
            var service = CreateService();
            var share = service.Share(Doc, "alice", ("bob", CollaborationRole.Editor));

            Assert.Equal("alice", share.OwnerId);
            Assert.Equal(CollaborationRole.Admin, service.GetRole(Doc, "alice"));
            Assert.Equal(CollaborationRole.Editor, service.GetRole(Doc, "bob"));
            Assert.Null(service.GetRole(Doc, "carol"));
        }

        [Fact]
        public void Share_UnregisteredOwner_Throws()
        {
            var service = CreateService();
            Assert.Throws<ArgumentException>(() => service.Share(Doc, "nobody"));
        }

        [Fact]
        public void PublicDocument_GrantsViewerToEveryone()
        {
            var service = CreateService();
            service.Share(Doc, "alice");
            Assert.False(service.SetPublic(Doc, "bob", true));

            Assert.True(service.SetPublic(Doc, "alice", true));
            Assert.Equal(CollaborationRole.Viewer, service.GetRole(Doc, "carol"));
            Assert.True(service.CanView(Doc, "carol"));
            Assert.False(service.CanComment(Doc, "carol"));
        }

        [Fact]
        public void RoleChecks_RespectHierarchy()
        {
            var service = CreateService();
            service.Share(Doc, "alice",
                ("bob", CollaborationRole.Editor),
                ("carol", CollaborationRole.Commenter));

            Assert.True(service.CanEdit(Doc, "alice"));
            Assert.True(service.CanEdit(Doc, "bob"));
            Assert.False(service.CanEdit(Doc, "carol"));

            Assert.True(service.CanComment(Doc, "carol"));
            Assert.False(service.CanAdminister(Doc, "bob"));
        }

        [Fact]
        public void Grant_RequiresAdminAndRegisteredUser()
        {
            var service = CreateService();
            service.Share(Doc, "alice", ("bob", CollaborationRole.Viewer));

            Assert.False(service.Grant(Doc, "bob", "carol", CollaborationRole.Editor));
            Assert.False(service.Grant(Doc, "alice", "nobody", CollaborationRole.Editor));

            Assert.True(service.Grant(Doc, "alice", "carol", CollaborationRole.Editor));
            Assert.Equal(CollaborationRole.Editor, service.GetRole(Doc, "carol"));
        }

        [Fact]
        public void Revoke_CannotRemoveOwner()
        {
            var service = CreateService();
            service.Share(Doc, "alice", ("bob", CollaborationRole.Editor));

            Assert.False(service.Revoke(Doc, "alice", "alice"));
            Assert.True(service.Revoke(Doc, "alice", "bob"));
            Assert.Null(service.GetRole(Doc, "bob"));
            Assert.False(service.Revoke(Doc, "alice", "bob"));
        }

        [Fact]
        public void AddComment_RequiresPermissionAndText()
        {
            var service = CreateService();
            service.Share(Doc, "alice", ("carol", CollaborationRole.Commenter));

            Assert.Null(service.AddComment(Doc, "bob", "not allowed"));
            Assert.Null(service.AddComment(Doc, "carol", "  "));

            var comment = service.AddComment(Doc, "carol", "Check this flow", "StartNode");
            Assert.NotNull(comment);
            Assert.Equal("carol", comment.AuthorId);
            Assert.Equal("StartNode", comment.ComponentId);
            Assert.Single(service.GetComments(Doc));
        }

        [Fact]
        public void ResolveComment_AuthorOrAdminOnly()
        {
            var service = CreateService();
            service.Share(Doc, "alice",
                ("bob", CollaborationRole.Editor),
                ("carol", CollaborationRole.Commenter));

            var comment = service.AddComment(Doc, "carol", "Needs a label");

            Assert.False(service.ResolveComment(Doc, comment.Id, "bob"));
            Assert.True(service.ResolveComment(Doc, comment.Id, "alice"));
            Assert.Empty(service.GetComments(Doc));
            Assert.Single(service.GetComments(Doc, includeResolved: true));
            Assert.Equal("alice", service.GetComments(Doc, includeResolved: true)[0].ResolvedBy);
        }

        [Fact]
        public void Presence_RequiresViewAccessAndRespectsWindow()
        {
            var service = CreateService();
            service.Share(Doc, "alice", ("bob", CollaborationRole.Viewer));

            service.SetPresence(Doc, "carol", "editing");
            Assert.Empty(service.GetPresence(Doc));

            service.SetPresence(Doc, "bob", "editing");
            var presence = service.GetPresence(Doc);
            Assert.Single(presence);
            Assert.Equal("editing", presence[0].Activity);

            Assert.Empty(service.GetPresence(Doc, TimeSpan.FromMilliseconds(-1)));
            Assert.True(service.RemovePresence(Doc, "bob"));
            Assert.Empty(service.GetPresence(Doc));
        }

        [Fact]
        public void AuditTrail_RecordsActions()
        {
            var service = CreateService();
            service.Share(Doc, "alice", ("bob", CollaborationRole.Editor));
            service.Grant(Doc, "alice", "carol", CollaborationRole.Commenter);
            var comment = service.AddComment(Doc, "bob", "Looks good");
            service.ResolveComment(Doc, comment.Id, "bob");
            service.Revoke(Doc, "alice", "carol");

            var audit = service.GetAudit(Doc);
            Assert.Equal(new[] { "share", "grant", "comment", "resolve", "revoke" }, audit.Select(a => a.Action));
            Assert.All(audit, a => Assert.Equal(Doc, a.DocumentId));
        }

        [Fact]
        public void Users_RegisterAndReplace()
        {
            var service = CreateService();
            Assert.Equal(3, service.Users.Count);

            service.RegisterUser(new CollaborationUser { Id = "alice", DisplayName = "Alice Updated" });
            Assert.Equal("Alice Updated", service.GetUser("alice").DisplayName);
            Assert.Null(service.GetUser("unknown"));

            Assert.Throws<ArgumentException>(() => service.RegisterUser(new CollaborationUser { Id = "  " }));
        }

        [Fact]
        public void Snapshot_RoundTripsThroughJson()
        {
            var service = CreateService();
            service.RegisterUser(new CollaborationUser { Id = "alice", DisplayName = "Alice", Email = "alice@x.io" });
            service.Share(Doc, "alice", ("bob", CollaborationRole.Editor));
            service.SetPublic(Doc, "alice", true);
            var comment = service.AddComment(Doc, "bob", "Check the guard", "DecisionNode");
            service.ResolveComment(Doc, comment.Id, "bob");

            var restored = CollaborationSerializer.FromJson(CollaborationSerializer.ToJson(service));

            Assert.Equal(3, restored.Users.Count);
            Assert.Equal(CollaborationRole.Admin, restored.GetRole(Doc, "alice"));
            Assert.Equal(CollaborationRole.Editor, restored.GetRole(Doc, "bob"));
            Assert.True(restored.GetShare(Doc).IsPublic);
            Assert.Equal("alice@x.io", restored.GetUser("alice").Email);

            var restoredComment = restored.GetComments(Doc, includeResolved: true).Single();
            Assert.Equal("DecisionNode", restoredComment.ComponentId);
            Assert.True(restoredComment.Resolved);
            Assert.Equal("bob", restoredComment.ResolvedBy);

            Assert.Equal(service.AuditTrail.Count, restored.AuditTrail.Count);
        }

        [Fact]
        public void Snapshot_PreservesUserColor()
        {
            var service = new CollaborationService();
            var color = new SkiaSharp.SKColor(0x12, 0x34, 0x56, 0x78);
            service.RegisterUser(new CollaborationUser { Id = "alice", Color = color });

            var restored = CollaborationSerializer.FromJson(CollaborationSerializer.ToJson(service));

            Assert.Equal(color, restored.GetUser("alice").Color);
        }

        [Fact]
        public void Snapshot_SaveAndLoadFromFile()
        {
            var service = CreateService();
            service.Share(Doc, "alice", ("carol", CollaborationRole.Commenter));
            service.AddComment(Doc, "carol", "Looks good");

            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beep_collab_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                CollaborationSerializer.Save(service, path);
                var loaded = CollaborationSerializer.Load(path);

                Assert.Equal(CollaborationRole.Commenter, loaded.GetRole(Doc, "carol"));
                Assert.Single(loaded.GetComments(Doc));
            }
            finally
            {
                try { System.IO.File.Delete(path); } catch { }
            }
        }

        [Fact]
        public void Restore_ReplacesExistingState()
        {
            var service = CreateService();
            service.Share(Doc, "alice");
            service.AddComment(Doc, "alice", "original");

            var other = new CollaborationService();
            other.RegisterUser(new CollaborationUser { Id = "dave" });
            other.Share("doc-2", "dave");

            service.Restore(other.CreateSnapshot());

            Assert.Null(service.GetShare(Doc));
            Assert.Empty(service.GetComments(Doc));
            Assert.Equal(CollaborationRole.Admin, service.GetRole("doc-2", "dave"));
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beep.Skia.Collaboration;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Concurrency hardening for the collaboration service: the service is shared by UI and
    /// server callers, so concurrent mutations must not corrupt state or throw.
    /// </summary>
    public class CollaborationConcurrencyTests
    {
        private const string Doc = "doc-concurrent";

        private static CollaborationService BuildService(int users)
        {
            var service = new CollaborationService();
            for (var i = 0; i < users; i++)
                service.RegisterUser(new CollaborationUser { Id = "user" + i, DisplayName = "User " + i });
            service.Share(Doc, "user0");
            for (var i = 1; i < users; i++)
                service.Grant(Doc, "user0", "user" + i, CollaborationRole.Editor);
            return service;
        }

        [Fact]
        public void ConcurrentComments_AllRecordedWithoutLoss()
        {
            const int threads = 8;
            const int perThread = 50;
            var service = BuildService(threads);

            Parallel.For(0, threads, t =>
            {
                for (var i = 0; i < perThread; i++)
                    service.AddComment(Doc, "user" + t, $"comment {t}-{i}", "node" + i);
            });

            Assert.Equal(threads * perThread, service.GetComments(Doc).Count);
            Assert.Equal(threads * perThread, service.AllComments.Count);
            Assert.Equal(threads * perThread, service.GetAudit(Doc).Count(a => a.Action == "comment"));
        }

        [Fact]
        public void ConcurrentPresence_LeavesOneEntryPerUser()
        {
            const int threads = 8;
            var service = BuildService(threads);

            Parallel.For(0, threads * 20, i =>
            {
                var user = "user" + (i % threads);
                service.SetPresence(Doc, user, "editing");
                service.GetPresence(Doc);
            });

            Assert.Equal(threads, service.GetPresence(Doc).Count);
        }

        [Fact]
        public void ConcurrentGrantRevoke_KeepsStateConsistent()
        {
            var service = BuildService(2);
            service.RegisterUser(new CollaborationUser { Id = "target" });

            Parallel.For(0, 500, i =>
            {
                if (i % 2 == 0) service.Grant(Doc, "user0", "target", CollaborationRole.Editor);
                else service.Revoke(Doc, "user0", "target");
                service.GetRole(Doc, "target");
            });

            // The owner must always remain admin regardless of the interleaving.
            Assert.Equal(CollaborationRole.Admin, service.GetRole(Doc, "user0"));
            Assert.NotNull(service.GetShare(Doc));
        }

        [Fact]
        public void ConcurrentSnapshotAndMutate_DoesNotThrow()
        {
            var service = BuildService(4);
            var errors = new ConcurrentBag<Exception>();

            Parallel.For(0, 200, i =>
            {
                try
                {
                    if (i % 3 == 0) service.CreateSnapshot();
                    else if (i % 3 == 1) service.AddComment(Doc, "user1", "note " + i);
                    else service.SetPresence(Doc, "user2", "viewing");
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            });

            Assert.Empty(errors);
        }

        [Fact]
        public void GetShare_ReturnsSnapshot_SoCallersCannotBypassRoleChecks()
        {
            var service = BuildService(2);

            var share = service.GetShare(Doc);
            share.Members["intruder"] = CollaborationRole.Admin;
            share.IsPublic = true;

            Assert.Null(service.GetRole(Doc, "intruder"));
            Assert.False(service.GetShare(Doc).IsPublic);
        }

        [Fact]
        public void Share_ReturnsSnapshot_SoCallersCannotMutateStoredShare()
        {
            var service = new CollaborationService();
            service.RegisterUser(new CollaborationUser { Id = "owner" });

            var share = service.Share(Doc, "owner");
            share.Members["intruder"] = CollaborationRole.Admin;

            Assert.Null(service.GetRole(Doc, "intruder"));
        }
    }
}

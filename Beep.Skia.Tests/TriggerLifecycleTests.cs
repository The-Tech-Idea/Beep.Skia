using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia.Triggers;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Resource-lifecycle hardening for triggers: disposing a running trigger must stop it and
    /// release its timer/watcher, and disposal must be idempotent.
    /// </summary>
    public class TriggerLifecycleTests
    {
        private static string NewTempDir()
        {
            var path = Path.Combine(Path.GetTempPath(), "beep_trigger_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        [Fact]
        public async Task DisposingRunningFileWatchTrigger_StopsWatching()
        {
            var dir = NewTempDir();
            try
            {
                var trigger = new FileWatchTrigger { Path = dir, Filter = "*.txt" };
                Assert.True(await trigger.StartAsync());
                Assert.True(trigger.IsActive);

                trigger.Dispose();

                Assert.False(trigger.IsActive);

                // No events may be raised after disposal.
                var fired = 0;
                trigger.Triggered += (s, e) => Interlocked.Increment(ref fired);
                File.WriteAllText(Path.Combine(dir, "after-dispose.txt"), "x");
                Thread.Sleep(150);

                Assert.Equal(0, fired);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        [Fact]
        public async Task DisposingRunningScheduleTrigger_StopsFiring()
        {
            var trigger = new ScheduleTrigger { Interval = TimeSpan.FromMilliseconds(20) };
            var fired = 0;
            trigger.Triggered += (s, e) => Interlocked.Increment(ref fired);

            Assert.True(await trigger.StartAsync());
            Thread.Sleep(120);
            Assert.True(fired > 0, "the trigger should have fired at least once");

            trigger.Dispose();
            var countAtDispose = fired;

            Thread.Sleep(150);
            Assert.Equal(countAtDispose, fired);
            Assert.False(trigger.IsActive);
        }

        [Fact]
        public async Task Dispose_IsIdempotent()
        {
            var trigger = new ScheduleTrigger { Interval = TimeSpan.FromMilliseconds(50) };
            await trigger.StartAsync();

            trigger.Dispose();
            trigger.Dispose();
            trigger.Dispose();

            Assert.False(trigger.IsActive);
        }

        [Fact]
        public void DisposingStoppedTrigger_IsSafe()
        {
            var trigger = new ScheduleTrigger { Interval = TimeSpan.FromMilliseconds(50) };

            trigger.Dispose();

            Assert.False(trigger.IsActive);
        }

        [Fact]
        public async Task TriggerStartedAndStopped_CanBeRestarted()
        {
            var trigger = new ScheduleTrigger { Interval = TimeSpan.FromMilliseconds(20) };
            var fired = 0;
            trigger.Triggered += (s, e) => Interlocked.Increment(ref fired);

            await trigger.StartAsync();
            await trigger.StopAsync();
            var afterFirst = fired;

            await trigger.StartAsync();
            await Task.Delay(120);
            await trigger.StopAsync();

            Assert.True(fired > afterFirst, "the trigger should fire again after a restart");
            trigger.Dispose();
        }
    }
}

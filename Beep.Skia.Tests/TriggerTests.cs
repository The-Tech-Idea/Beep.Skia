using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Beep.Skia.Model;
using Beep.Skia.Triggers;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for the workflow trigger pack: manual, schedule, event, and file-watch triggers.
    /// </summary>
    public class TriggerTests
    {
        [Fact]
        public async Task ManualTrigger_ActivatesAndCounts()
        {
            var trigger = new ManualTrigger();
            Dictionary<string, object> received = null;
            trigger.Triggered += (s, e) => received = e.TriggerData;

            Assert.True(await trigger.StartAsync());
            Assert.True(trigger.IsActive);

            await trigger.ActivateAsync(new Dictionary<string, object> { ["run"] = "now" });

            Assert.Equal(1, trigger.ActivationCount);
            Assert.NotNull(trigger.LastActivated);
            Assert.Equal("now", received["run"]);

            Assert.True(await trigger.StopAsync());
            Assert.False(trigger.IsActive);
        }

        [Fact]
        public async Task ManualTrigger_WhenDisabled_DoesNotFire()
        {
            var trigger = new ManualTrigger { IsEnabled = false };
            await trigger.StartAsync();
            await trigger.ActivateAsync();

            Assert.Equal(0, trigger.ActivationCount);
        }

        [Fact]
        public async Task ScheduleTrigger_FiresRepeatedlyUntilStopped()
        {
            var trigger = new ScheduleTrigger { Interval = TimeSpan.FromMilliseconds(50) };

            Assert.True(await trigger.StartAsync());
            await WaitUntilAsync(() => trigger.ActivationCount >= 2, 3000);

            await trigger.StopAsync();
            long snapshot = trigger.ActivationCount;

            await Task.Delay(200);
            Assert.Equal(snapshot, trigger.ActivationCount);
            Assert.False(trigger.IsActive);
        }

        [Fact]
        public async Task ScheduleTrigger_InvalidInterval_FailsValidationAndStart()
        {
            var trigger = new ScheduleTrigger { Interval = TimeSpan.Zero };

            var validation = await trigger.ValidateAsync();
            Assert.False(validation.IsValid);

            Assert.False(await trigger.StartAsync());
            Assert.False(trigger.IsActive);
        }

        [Fact]
        public async Task EventTrigger_NotifyRaisesPayload()
        {
            var trigger = new EventTrigger(TriggerType.WebHook);
            Dictionary<string, object> received = null;
            trigger.Triggered += (s, e) => received = e.TriggerData;

            await trigger.StartAsync();
            await trigger.Notify(new Dictionary<string, object> { ["body"] = "{\"id\":1}" });

            Assert.Equal(1, trigger.ActivationCount);
            Assert.Equal("{\"id\":1}", received["body"]);
            Assert.Equal(TriggerType.WebHook, trigger.TriggerType);
        }

        [Fact]
        public async Task EventTrigger_WhenNotActive_RaisesError()
        {
            var trigger = new EventTrigger();
            bool errored = false;
            trigger.ErrorOccurred += (s, e) => errored = true;

            await trigger.Notify(new Dictionary<string, object>());

            Assert.True(errored);
            Assert.Equal(0, trigger.ActivationCount);
        }

        [Fact]
        public async Task FileWatchTrigger_FiresOnFileChange()
        {
            var directory = Path.Combine(Path.GetTempPath(), "beep_trigger_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(directory);

            try
            {
                var trigger = new FileWatchTrigger { Path = directory, Filter = "*.txt" };
                var validation = await trigger.ValidateAsync();
                Assert.True(validation.IsValid);

                Assert.True(await trigger.StartAsync());
                File.WriteAllText(Path.Combine(directory, "event.txt"), "hello");

                await WaitUntilAsync(() => trigger.ActivationCount >= 1, 5000);
                Assert.True(trigger.ActivationCount >= 1);

                await trigger.StopAsync();
                Assert.False(trigger.IsActive);
            }
            finally
            {
                try { Directory.Delete(directory, recursive: true); } catch { }
            }
        }

        [Fact]
        public async Task FileWatchTrigger_MissingDirectory_IsInvalid()
        {
            var trigger = new FileWatchTrigger { Path = Path.Combine(Path.GetTempPath(), "does_not_exist_" + Guid.NewGuid().ToString("N")) };

            var validation = await trigger.ValidateAsync();
            Assert.False(validation.IsValid);
            Assert.False(await trigger.StartAsync());
        }

        private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (!condition())
            {
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException("Condition was not met in time.");
                await Task.Delay(25);
            }
        }
    }
}

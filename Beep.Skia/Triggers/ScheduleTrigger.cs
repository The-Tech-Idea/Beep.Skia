using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia.Model;

namespace Beep.Skia.Triggers
{
    /// <summary>
    /// Scheduled trigger: fires repeatedly on a fixed interval
    /// (Configuration["IntervalSeconds"] or the <see cref="Interval"/> property).
    /// </summary>
    public class ScheduleTrigger : TriggerBase
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public override string Name => "Schedule Trigger";
        /// <summary>
        /// Gets or sets the trigger type.
        /// </summary>
        public override TriggerType TriggerType => TriggerType.Scheduled;

        /// <summary>Interval between activations.</summary>
        public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);

        private CancellationTokenSource _cts;
        private Task _loop;

        /// <summary>
        /// Gets or sets the initialize async.
        /// </summary>
        public override Task<bool> InitializeAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken = default)
        {
            base.InitializeAsync(configuration, cancellationToken);
            var seconds = GetConfig("IntervalSeconds", 0.0);
            if (seconds > 0) Interval = TimeSpan.FromSeconds(seconds);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Gets or sets the validate async.
        /// </summary>
        public override Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Interval > TimeSpan.Zero
                ? ValidationResult.Success()
                : ValidationResult.Failure("Interval must be greater than zero."));

        /// <summary>
        /// Gets or sets the start async.
        /// </summary>
        public override Task<bool> StartAsync(CancellationToken cancellationToken = default)
        {
            if (IsActive) return Task.FromResult(true);
            if (Interval <= TimeSpan.Zero)
            {
                RaiseError(new InvalidOperationException("Interval must be greater than zero."));
                return Task.FromResult(false);
            }

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _loop = RunAsync(_cts.Token);
            IsActive = true;
            return Task.FromResult(true);
        }

        /// <summary>
        /// Gets or sets the stop async.
        /// </summary>
        public override async Task<bool> StopAsync(CancellationToken cancellationToken = default)
        {
            if (!IsActive) return true;

            try { _cts?.Cancel(); } catch { }
            if (_loop != null)
            {
                try { await _loop.ConfigureAwait(false); } catch { }
            }

            _cts?.Dispose();
            _cts = null;
            _loop = null;
            IsActive = false;
            return true;
        }

        private async Task RunAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(Interval, token).ConfigureAwait(false);
                    RaiseTriggered(new Dictionary<string, object>
                    {
                        ["firedAt"] = DateTime.UtcNow,
                        ["interval"] = Interval.TotalSeconds
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
            catch (Exception ex)
            {
                RaiseError(ex, "Schedule trigger loop failed.");
            }
        }

        public override Dictionary<string, object> GetConfigurationSchema()
            => new Dictionary<string, object> { ["IntervalSeconds"] = "Interval in seconds between activations" };
    }
}

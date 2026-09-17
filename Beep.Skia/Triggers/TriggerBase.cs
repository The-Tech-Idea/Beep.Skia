using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia.Model;

namespace Beep.Skia.Triggers
{
    /// <summary>
    /// Shared base for workflow triggers: identity, state, events, and activation bookkeeping.
    /// Disposing a trigger stops it, so hosts that abandon a running trigger do not leak
    /// timers, file watchers, or background loops.
    /// </summary>
    public abstract class TriggerBase : ITrigger, IDisposable
    {
        private bool _disposed;

        public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
        public abstract string Name { get; }
        public virtual string Description => Name;
        public abstract TriggerType TriggerType { get; }

        public bool IsActive { get; protected set; }
        public bool IsEnabled { get; set; } = true;
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        public string WorkflowId { get; set; }
        public DateTime? LastActivated { get; private set; }
        public long ActivationCount { get; private set; }

        public event EventHandler<TriggerEventArgs> Triggered;
        public event EventHandler<TriggerErrorEventArgs> ErrorOccurred;

        public virtual Task<bool> InitializeAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken = default)
        {
            if (configuration != null) Configuration = configuration;
            return Task.FromResult(true);
        }

        public abstract Task<bool> StartAsync(CancellationToken cancellationToken = default);

        public abstract Task<bool> StopAsync(CancellationToken cancellationToken = default);

        public virtual Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(ValidationResult.Success());

        public virtual Task<bool> TestAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        /// <summary>
        /// Stops the trigger and releases its resources. Safe to call multiple times.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try { StopAsync().GetAwaiter().GetResult(); } catch { }
            GC.SuppressFinalize(this);
        }

        public virtual Dictionary<string, object> GetConfigurationSchema() => new Dictionary<string, object>();

        public virtual Task ActivateAsync(Dictionary<string, object> testData = null, CancellationToken cancellationToken = default)
        {
            RaiseTriggered(testData);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raises the Triggered event (suppressed when the trigger is disabled).
        /// </summary>
        protected void RaiseTriggered(Dictionary<string, object> data)
        {
            if (!IsEnabled) return;

            LastActivated = DateTime.UtcNow;
            ActivationCount++;
            Triggered?.Invoke(this, new TriggerEventArgs(Id, TriggerType, data));
        }

        /// <summary>
        /// Raises the ErrorOccurred event.
        /// </summary>
        protected void RaiseError(Exception exception, string message = null)
        {
            ErrorOccurred?.Invoke(this, new TriggerErrorEventArgs(Id, exception, message));
        }

        /// <summary>
        /// Reads a configuration value with a default fallback.
        /// </summary>
        protected T GetConfig<T>(string key, T defaultValue = default)
        {
            if (Configuration != null && Configuration.TryGetValue(key, out var value) && value != null)
            {
                try
                {
                    if (value is T typed) return typed;
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch { }
            }
            return defaultValue;
        }
    }
}

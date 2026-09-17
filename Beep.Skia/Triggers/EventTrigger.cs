using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia.Model;

namespace Beep.Skia.Triggers
{
    /// <summary>
    /// Event-driven trigger fired by external code (webhook handlers, queue consumers,
    /// API endpoints, or data-change notifiers) calling <see cref="Notify"/>.
    /// </summary>
    public class EventTrigger : TriggerBase
    {
        private TriggerType _triggerType = TriggerType.Event;

        public override string Name => "Event Trigger";
        public override TriggerType TriggerType => _triggerType;

        /// <summary>Allows hosting a webhook/API/data-change trigger with the same implementation.</summary>
        public TriggerType EventKind
        {
            get => _triggerType;
            set => _triggerType = value;
        }

        public EventTrigger()
        {
        }

        public EventTrigger(TriggerType eventKind)
        {
            _triggerType = eventKind;
        }

        public override Task<bool> StartAsync(CancellationToken cancellationToken = default)
        {
            IsActive = true;
            return Task.FromResult(true);
        }

        public override Task<bool> StopAsync(CancellationToken cancellationToken = default)
        {
            IsActive = false;
            return Task.FromResult(true);
        }

        /// <summary>
        /// Fires the trigger with the given payload (webhook body, queue message, changed record, ...).
        /// </summary>
        public Task Notify(Dictionary<string, object> payload = null)
        {
            if (!IsActive)
            {
                RaiseError(new InvalidOperationException("Trigger is not active."));
                return Task.CompletedTask;
            }

            RaiseTriggered(payload ?? new Dictionary<string, object>());
            return Task.CompletedTask;
        }

        public override Dictionary<string, object> GetConfigurationSchema()
            => new Dictionary<string, object>
            {
                ["EventKind"] = "Manual/WebHook/DataChange/Event/API",
                ["Payload"] = "Event payload supplied by the caller"
            };
    }
}

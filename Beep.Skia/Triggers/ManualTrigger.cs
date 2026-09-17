using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia.Model;

namespace Beep.Skia.Triggers
{
    /// <summary>
    /// Manual trigger: fires only when <see cref="ITrigger.ActivateAsync"/> is called
    /// (useful for tests, run buttons, and API-triggered executions).
    /// </summary>
    public class ManualTrigger : TriggerBase
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public override string Name => "Manual Trigger";
        /// <summary>
        /// Gets or sets the trigger type.
        /// </summary>
        public override TriggerType TriggerType => TriggerType.Manual;

        /// <summary>
        /// Gets or sets the start async.
        /// </summary>
        public override Task<bool> StartAsync(CancellationToken cancellationToken = default)
        {
            IsActive = true;
            return Task.FromResult(true);
        }

        /// <summary>
        /// Gets or sets the stop async.
        /// </summary>
        public override Task<bool> StopAsync(CancellationToken cancellationToken = default)
        {
            IsActive = false;
            return Task.FromResult(true);
        }

        public override Dictionary<string, object> GetConfigurationSchema()
            => new Dictionary<string, object> { ["payload"] = "Optional activation payload (dictionary)" };
    }
}

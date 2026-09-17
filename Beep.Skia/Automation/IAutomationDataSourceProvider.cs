using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Beep.Skia.Automation
{
    /// <summary>
    /// Host-provided bridge to a real data-source stack (e.g., BeepDataSources / BeepDM).
    /// Register an implementation with <see cref="AutomationDataSourceRegistry"/> and
    /// <c>DataSourceAutomationNode</c> will use it instead of simulated data.
    /// </summary>
    public interface IAutomationDataSourceProvider
    {
        /// <summary>
        /// Executes a query and returns rows as dictionaries.
        /// </summary>
        /// <param name="dataSourceName">Registered data source name.</param>
        /// <param name="entityName">Entity/table name (optional when a raw query is supplied).</param>
        /// <param name="query">Raw query string (optional when an entity name is supplied).</param>
        /// <param name="maxRows">Maximum rows to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<IReadOnlyList<Dictionary<string, object>>> QueryAsync(
            string dataSourceName,
            string entityName,
            string query,
            int maxRows,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tests connectivity to a registered data source.
        /// </summary>
        Task<bool> TestConnectionAsync(string dataSourceName, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Ambient registry for the automation data-source provider.
    /// </summary>
    public static class AutomationDataSourceRegistry
    {
        private static IAutomationDataSourceProvider _provider;

        /// <summary>Gets or sets the active provider (null disables real data access).</summary>
        public static IAutomationDataSourceProvider Provider
        {
            get => _provider;
            set => _provider = value;
        }

        /// <summary>True when a provider is registered.</summary>
        public static bool HasProvider => _provider != null;

        /// <summary>Removes the registered provider.</summary>
        public static void Clear() => _provider = null;
    }
}

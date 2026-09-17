using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia.Automation;
using Beep.Skia.Components;
using Xunit;
using ExecutionContext = Beep.Skia.Model.ExecutionContext;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for the host data-source provider bridge used by DataSourceAutomationNode.
    /// </summary>
    public class DataSourceProviderTests
    {
        private sealed class FakeProvider : IAutomationDataSourceProvider
        {
            public string LastDataSource;
            public string LastEntity;
            public string LastQuery;
            public int LastMaxRows;
            public List<Dictionary<string, object>> Rows { get; } = new List<Dictionary<string, object>>();
            public bool ThrowOnQuery { get; set; }

            public Task<IReadOnlyList<Dictionary<string, object>>> QueryAsync(
                string dataSourceName, string entityName, string query, int maxRows, CancellationToken cancellationToken = default)
            {
                LastDataSource = dataSourceName;
                LastEntity = entityName;
                LastQuery = query;
                LastMaxRows = maxRows;

                if (ThrowOnQuery) throw new InvalidOperationException("provider down");

                return Task.FromResult<IReadOnlyList<Dictionary<string, object>>>(Rows);
            }

            public Task<bool> TestConnectionAsync(string dataSourceName, CancellationToken cancellationToken = default)
                => Task.FromResult(true);
        }

        [Fact]
        public async Task Node_UsesRegisteredProvider()
        {
            var provider = new FakeProvider();
            provider.Rows.Add(new Dictionary<string, object> { ["id"] = 1, ["name"] = "alpha" });
            provider.Rows.Add(new Dictionary<string, object> { ["id"] = 2, ["name"] = "beta" });
            AutomationDataSourceRegistry.Provider = provider;

            try
            {
                var node = new DataSourceAutomationNode
                {
                    DataSourceName = "Sales",
                    EntityName = "Orders",
                    MaxRows = 50,
                    OutputFormat = "json"
                };

                var result = await node.ExecuteAsync(new ExecutionContext("wf", "exec"));

                Assert.True(result.Success);
                Assert.Equal(2, Convert.ToInt32(result.OutputData["rowCount"]));
                Assert.Equal("Sales", provider.LastDataSource);
                Assert.Equal("Orders", provider.LastEntity);
                Assert.Equal(50, provider.LastMaxRows);
            }
            finally
            {
                AutomationDataSourceRegistry.Clear();
            }
        }

        [Fact]
        public async Task Node_ProviderFailure_ReturnsFailedResult()
        {
            AutomationDataSourceRegistry.Provider = new FakeProvider { ThrowOnQuery = true };

            try
            {
                var node = new DataSourceAutomationNode { DataSourceName = "Sales", EntityName = "Orders" };
                var result = await node.ExecuteAsync(new ExecutionContext("wf", "exec"));

                Assert.False(result.Success);
                Assert.Contains("provider down", result.ErrorMessage);
            }
            finally
            {
                AutomationDataSourceRegistry.Clear();
            }
        }

        [Fact]
        public async Task Node_WithoutProvider_FallsBackToSimulatedData()
        {
            AutomationDataSourceRegistry.Clear();

            var node = new DataSourceAutomationNode { DataSourceName = "Sales", EntityName = "Orders" };
            var result = await node.ExecuteAsync(new ExecutionContext("wf", "exec"));

            Assert.True(result.Success);
            Assert.True(Convert.ToInt32(result.OutputData["rowCount"]) > 0);
        }

        [Fact]
        public async Task Node_WithProviderButNoDataSourceName_Fails()
        {
            AutomationDataSourceRegistry.Provider = new FakeProvider();

            try
            {
                var node = new DataSourceAutomationNode { EntityName = "Orders" };
                var result = await node.ExecuteAsync(new ExecutionContext("wf", "exec"));

                Assert.False(result.Success);
                Assert.Contains("DataSourceName", result.ErrorMessage);
            }
            finally
            {
                AutomationDataSourceRegistry.Clear();
            }
        }

        [Fact]
        public void Registry_ReportsProviderPresence()
        {
            AutomationDataSourceRegistry.Clear();
            Assert.False(AutomationDataSourceRegistry.HasProvider);

            AutomationDataSourceRegistry.Provider = new FakeProvider();
            Assert.True(AutomationDataSourceRegistry.HasProvider);

            AutomationDataSourceRegistry.Clear();
            Assert.False(AutomationDataSourceRegistry.HasProvider);
        }
    }
}

using System.Linq;
using Beep.Skia;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for diagram templates: loading, explicit endpoint wiring, and registry coverage.
    /// </summary>
    public class DiagramTemplateTests
    {
        [Fact]
        public void AllTemplates_LoadAllComponents()
        {
            foreach (var template in DiagramTemplates.All)
            {
                var manager = new DrawingManager();
                manager.LoadTemplate(template.Value());

                Assert.Equal(template.Value().Components.Count, manager.GetComponents().Count);
            }
        }

        [Fact]
        public void Registry_ContainsNewTemplates()
        {
            Assert.Contains("FlowChart: Purchase Approval", DiagramTemplates.All.Keys);
            Assert.Contains("StateMachine: Order Lifecycle", DiagramTemplates.All.Keys);
        }

        [Fact]
        public void FlowChartOrderValidation_BranchesConnectIntendedComponents()
        {
            var manager = new DrawingManager();
            manager.LoadTemplate(DiagramTemplates.FlowChartOrderValidation());

            var decision = manager.GetComponents().First(c => c.Name == "Valid?");
            var notify = manager.GetComponents().First(c => c.Name == "Notify Customer");
            var processPayment = manager.GetComponents().First(c => c.Name == "Process Payment");

            // The "No" branch must go to Notify Customer (index 5), not to Process Payment (index 3).
            Assert.Contains(manager.GetLines(), l =>
                l.Start?.Component == decision && l.End?.Component == notify && l.Label1 == "No");
            Assert.DoesNotContain(manager.GetLines(), l =>
                l.Start?.Component == decision && l.End?.Component == processPayment && l.Label1 == "No");
        }

        [Fact]
        public void FlowChartApproval_LoadsExpectedGraph()
        {
            var dto = DiagramTemplates.FlowChartApproval();
            var manager = new DrawingManager();
            manager.LoadTemplate(dto);

            Assert.Equal(7, manager.GetComponents().Count);
            Assert.Equal(7, manager.GetLines().Count);

            var decision = manager.GetComponents().First(c => c.Name == "Approved?");
            var archive = manager.GetComponents().First(c => c.Name == "Archive Request");
            Assert.Contains(manager.GetLines(), l => l.Start?.Component == decision && l.End?.Component == archive);
        }

        [Fact]
        public void StateMachineOrder_LoadsExpectedGraph()
        {
            var dto = DiagramTemplates.StateMachineOrder();
            var manager = new DrawingManager();
            manager.LoadTemplate(dto);

            Assert.Equal(6, manager.GetComponents().Count);
            Assert.Equal(6, manager.GetLines().Count);

            var created = manager.GetComponents().First(c => c.Name == "Created");
            var cancelled = manager.GetComponents().First(c => c.Name == "Cancelled");
            Assert.Contains(manager.GetLines(), l =>
                l.Start?.Component == created && l.End?.Component == cancelled && l.Label1 == "cancel");
        }

        [Fact]
        public void ERDEcommerce_RelationshipsConnectIntendedEntities()
        {
            var dto = DiagramTemplates.ERDEcommerce();
            var manager = new DrawingManager();
            manager.LoadTemplate(dto);

            Assert.Equal(4, manager.GetComponents().Count);
            Assert.Equal(3, manager.GetLines().Count);

            var orders = manager.GetComponents().First(c => c.Name == "Orders");
            var orderItems = manager.GetComponents().First(c => c.Name == "OrderItems");
            Assert.Contains(manager.GetLines(), l => l.Start?.Component == orders && l.End?.Component == orderItems);
        }
    }
}

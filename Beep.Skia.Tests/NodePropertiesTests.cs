using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia.Components;
using Beep.Skia.Model;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    public class NodePropertiesTests
    {
        [Fact]
        public void DataInputNode_ApplyNodeProperties_Updates_Properties_And_Config()
        {
            var node = new DataInputNode();

            // Sanity: seeded NodeProperties should exist
            Assert.NotEmpty(node.NodeProperties);

            node.NodeProperties["InputMode"].ParameterCurrentValue = "Form";
            node.NodeProperties["JsonData"].ParameterCurrentValue = "{\"a\":1}";
            node.NodeProperties["AllowMultipleEntries"].ParameterCurrentValue = true;
            node.NodeProperties["ValidateInput"].ParameterCurrentValue = false;
            node.NodeProperties["DataSchema"].ParameterCurrentValue = "{type:'object'}";

            node.ApplyNodeProperties();

            Assert.Equal("Form", node.InputMode);
            Assert.Equal("{\"a\":1}", node.JsonData);
            Assert.True(node.AllowMultipleEntries);
            Assert.False(node.ValidateInput);
            Assert.Equal("{type:'object'}", node.DataSchema);

            Assert.Equal("Form", node.Configuration["InputMode"].ToString());
            Assert.Equal("{\"a\":1}", node.Configuration["JsonData"].ToString());
            Assert.Equal(true, node.Configuration["AllowMultipleEntries"]);
            Assert.Equal(false, node.Configuration["ValidateInput"]);
            Assert.Equal("{type:'object'}", node.Configuration["DataSchema"].ToString());
        }

        [Fact]
        public void ApplyNodeProperties_Performs_Type_Conversion()
        {
            var node = new ColorTestNode();

            // String to bool conversion
            node.NodeProperties["EnabledFlag"] = new ParameterInfo
            {
                ParameterName = "EnabledFlag",
                ParameterType = typeof(bool),
                DefaultParameterValue = false,
                ParameterCurrentValue = "true"
            };

            // String hex to SKColor conversion
            node.NodeProperties["AccentColor"] = new ParameterInfo
            {
                ParameterName = "AccentColor",
                ParameterType = typeof(SKColor),
                DefaultParameterValue = SKColors.Black,
                ParameterCurrentValue = "#FF00FF" // magenta
            };

            node.ApplyNodeProperties();

            Assert.True(node.EnabledFlag);
            Assert.Equal(SKColors.Magenta, node.AccentColor);
        }

        private class ColorTestNode : AutomationNode
        {
            public bool EnabledFlag { get; set; }
            public SKColor AccentColor { get; set; } = SKColors.Black;

            public override string Description => "Test node for color conversion";
            public override NodeType NodeType => NodeType.Action;

            public override Task<bool> InitializeAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken = default)
                => Task.FromResult(true);

            public override Task<ValidationResult> ValidateAsync(Beep.Skia.Model.ExecutionContext inputData, CancellationToken cancellationToken = default)
                => Task.FromResult(ValidationResult.Success());

            public override Task<NodeResult> ExecuteAsync(Beep.Skia.Model.ExecutionContext context, CancellationToken cancellationToken = default)
                => Task.FromResult(NodeResult.CreateSuccess());

            public override Dictionary<string, object> GetInputSchema() => new();
            public override Dictionary<string, object> GetOutputSchema() => new();
        }
    }
}

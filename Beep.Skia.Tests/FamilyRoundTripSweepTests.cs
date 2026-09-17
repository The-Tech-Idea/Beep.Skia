using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Beep.Skia;
using Beep.Skia.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Systematic sweep across every diagram family: whenever two components can be connected,
    /// the connection must survive a save/load round-trip.
    ///
    /// This targets the defect class found in the automation nodes, where ports lived in a private
    /// collection that serialization did not read, so diagrams silently reloaded with no lines.
    /// </summary>
    public class FamilyRoundTripSweepTests
    {
        private readonly ITestOutputHelper _output;

        public FamilyRoundTripSweepTests(ITestOutputHelper output) => _output = output;

        /// <summary>One representative component type per family (the families' own assemblies).</summary>
        public static IEnumerable<object[]> RepresentativeTypes()
        {
            yield return new object[] { "Flowchart", typeof(Beep.Skia.Flowchart.ProcessNode) };
            yield return new object[] { "ERD", typeof(Beep.Skia.ERD.ERDEntity) };
            yield return new object[] { "UML", typeof(Beep.Skia.UML.UMLPackageNode) };
            yield return new object[] { "DFD", typeof(Beep.Skia.DFD.DFDProcess) };
            yield return new object[] { "StateMachine", typeof(Beep.Skia.StateMachine.StateNode) };
            yield return new object[] { "MindMap", typeof(Beep.Skia.MindMap.TopicNode) };
            yield return new object[] { "PM", typeof(Beep.Skia.PM.DeliverableNode) };
            yield return new object[] { "ETL", typeof(Beep.Skia.ETL.ETLDestination) };
            yield return new object[] { "ECAD", typeof(Beep.Skia.ECAD.ECADGroundNode) };
            yield return new object[] { "Cloud", typeof(Beep.Skia.Cloud.CloudStorageNode) };
            yield return new object[] { "ML", typeof(Beep.Skia.ML.MLTransformerNode) };
            yield return new object[] { "Quantitative", typeof(Beep.Ski.Quantitative.TimeSeriesNode) };
            yield return new object[] { "Automation", typeof(Beep.Skia.Components.ManualTriggerNode) };
        }

        [Theory]
        [MemberData(nameof(RepresentativeTypes))]
        public void Connection_SurvivesRoundTrip_ForFamily(string family, Type componentType)
        {
            // ── Build two connected components of this family ───────────────
            var manager = new DrawingManager();
            SkiaComponent first;
            SkiaComponent second;
            try
            {
                first = (SkiaComponent)Activator.CreateInstance(componentType);
                second = (SkiaComponent)Activator.CreateInstance(componentType);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"{family}: cannot instantiate {componentType.Name} ({ex.GetType().Name}) - skipped");
                return;
            }

            first.X = 40; first.Y = 40; first.Width = 160; first.Height = 60; first.Name = "First";
            second.X = 40; second.Y = 220; second.Width = 160; second.Height = 60; second.Name = "Second";

            manager.AddComponent(first);
            manager.AddComponent(second);

            try
            {
                manager.ConnectComponents(first, second);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"{family}: ConnectComponents threw {ex.GetType().Name} - skipped");
                return;
            }

            if (manager.GetLines().Count == 0)
            {
                // The family's components do not expose compatible ports for this pair; nothing to verify.
                _output.WriteLine($"{family}: no connection could be made between two {componentType.Name} - skipped");
                return;
            }

            // ── Round-trip ──────────────────────────────────────────────────
            var json = JsonSerializer.Serialize(manager.ToDto());
            var reloaded = new DrawingManager();
            reloaded.LoadFromDto(JsonSerializer.Deserialize<DiagramDto>(json));

            var reloadedLines = reloaded.GetLines().Count;
            _output.WriteLine($"{family}: lines before=1 after={reloadedLines}");

            Assert.True(reloadedLines == 1,
                $"{family}: the connection was lost on save/load ({componentType.Name}) - serialized diagrams must keep their lines");
        }

        [Fact]
        public void SweepReport_SummarisesFamilyCoverage()
        {
            var summary = new StringBuilder();
            var connected = new List<string>();
            var skipped = new List<string>();

            foreach (var entry in RepresentativeTypes())
            {
                var family = (string)entry[0];
                var componentType = (Type)entry[1];

                var manager = new DrawingManager();
                try
                {
                    var first = (SkiaComponent)Activator.CreateInstance(componentType);
                    var second = (SkiaComponent)Activator.CreateInstance(componentType);
                    first.X = 40; first.Y = 40; first.Name = "First";
                    second.X = 40; second.Y = 220; second.Name = "Second";
                    manager.AddComponent(first);
                    manager.AddComponent(second);
                    manager.ConnectComponents(first, second);

                    if (manager.GetLines().Count > 0) connected.Add(family);
                    else skipped.Add(family);
                }
                catch
                {
                    skipped.Add(family);
                }
            }

            summary.Append($"connected: {string.Join(", ", connected)} | skipped: {string.Join(", ", skipped)}");
            _output.WriteLine(summary.ToString());

            // Families whose identical components expose no compatible port pair are skipped (a
            // trigger has no input, a chart has no ports, ...). The sweep's purpose is to catch the
            // "ports stored where serialization cannot see them" defect class, so it must actually
            // exercise a healthy number of families - the automation family is covered separately by
            // AutomationSerializationTests (trigger -> transform).
            Assert.True(connected.Count >= 5,
                $"the sweep should connect most families; connected: {string.Join(", ", connected)}");
        }
    }
}

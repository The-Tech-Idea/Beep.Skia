using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Beep.Skia;
using Beep.Skia.Automation;
using Beep.Skia.Collaboration;
using Beep.Skia.Extensions;
using Beep.Skia.Extensions.Marketplace;
using Beep.Skia.Serialization;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Golden-path acceptance test: drives the whole product story across subsystems in one flow.
    /// Each piece is unit-tested elsewhere; this verifies they compose.
    ///
    ///   diagram -> save/load -> execute as a workflow (API) -> export -> collaborate -> extend
    /// </summary>
    public class EndToEndScenarioTests
    {
        private static string NewTempDir(string prefix)
        {
            var path = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        [Fact]
        public void FullProductStory_DiagramToExecutionToExportToCollaborationToExtension()
        {
            var workDir = NewTempDir("beep_e2e_");
            try
            {
                // ── 1. Author a diagram with automation nodes ───────────────────
                var manager = new DrawingManager();
                var start = new Beep.Skia.Components.ManualTriggerNode
                {
                    X = 60,
                    Y = 40,
                    Width = 160,
                    Height = 60,
                    Name = "Start"
                };
                var transform = new Beep.Skia.Components.DataTransformNode
                {
                    X = 60,
                    Y = 180,
                    Width = 160,
                    Height = 60,
                    Name = "Transform"
                };
                // Configure through the property setter: that is the path the property grid uses and
                // it keeps the node's runtime configuration in sync for serialization.
                transform.FieldMappings = new List<Beep.Skia.Components.FieldMapping>
                {
                    new Beep.Skia.Components.FieldMapping { SourceField = "environment", TargetField = "environment" }
                };
                manager.AddComponent(start);
                manager.AddComponent(transform);
                manager.ConnectComponents(start, transform);
                Assert.Single(manager.GetLines());

                // ── 2. Save and reload the diagram ──────────────────────────────
                var layoutPath = Path.Combine(workDir, "diagram.json");
                File.WriteAllText(layoutPath, JsonSerializer.Serialize(manager.ToDto()));

                var reloaded = new DrawingManager();
                reloaded.LoadFromDto(JsonSerializer.Deserialize<DiagramDto>(File.ReadAllText(layoutPath)));

                Assert.Equal(2, reloaded.GetComponents().Count);
                Assert.Single(reloaded.GetLines());

                // ── 3. Execute the reloaded diagram as a workflow through the API ──
                using var service = new WorkflowExecutionService(maxConcurrency: 1);
                var api = new WorkflowExecutionApi(service);

                var publishResponse = api.PublishDiagram(
                    JsonSerializer.Serialize(reloaded.ToDto()),
                    "E2E Workflow");
                Assert.True(publishResponse.Success, publishResponse.Error);

                var workflowId = service.Workflows.Single().Id;
                var submitResponse = api.Submit(workflowId, "acceptance", "{\"environment\":\"e2e\"}");
                Assert.True(submitResponse.Success, submitResponse.Error);

                var job = service.GetJobs().Single();
                Assert.True(service.WaitForCompletion(job.Id, TimeSpan.FromSeconds(15)));
                Assert.Equal(JobState.Completed, job.State);
                Assert.Equal("e2e", job.Inputs["environment"]);

                // The input reached the workflow's execution context.
                var detail = api.GetJob(job.Id);
                Assert.True(detail.Success);
                Assert.Contains("Completed", api.ToJson(detail));

                // ── 4. Export the diagram ───────────────────────────────────────
                var pngPath = Path.Combine(workDir, "diagram.png");
                reloaded.ExportToPng(pngPath);
                Assert.True(File.Exists(pngPath));
                Assert.True(new FileInfo(pngPath).Length > 1000, "the exported PNG should not be empty");

                // ── 5. Collaborate on the document and persist the state ────────
                var collaboration = new CollaborationService();
                collaboration.RegisterUser(new CollaborationUser { Id = "alice", DisplayName = "Alice" });
                collaboration.RegisterUser(new CollaborationUser { Id = "bob", DisplayName = "Bob" });
                collaboration.Share("diagram", "alice", ("bob", CollaborationRole.Commenter));

                var comment = collaboration.AddComment("diagram", "bob", "Check this step", "Transform");
                Assert.NotNull(comment);

                var collaborationPath = Path.Combine(workDir, "collaboration.json");
                CollaborationSerializer.Save(collaboration, collaborationPath);

                var restored = CollaborationSerializer.Load(collaborationPath);
                Assert.Equal(CollaborationRole.Commenter, restored.GetRole("diagram", "bob"));
                Assert.Equal("Check this step", restored.GetComments("diagram").Single().Text);

                // Comments anchored to a component surface as pins on the canvas.
                var pins = new CommentPinLayer();
                pins.Rebuild(reloaded.GetComponents(), restored.GetComments("diagram"));
                Assert.Single(pins.Pins);
                Assert.Equal("Transform", pins.Pins[0].ComponentId);

                // ── 6. Package, install and load an extension ───────────────────
                var registryDir = Path.Combine(workDir, "registry");
                var installRoot = Path.Combine(workDir, "installed");
                Directory.CreateDirectory(registryDir);

                var staging = Path.Combine(workDir, "staging");
                Directory.CreateDirectory(staging);
                File.Copy(typeof(EndToEndScenarioTests).Assembly.Location,
                    Path.Combine(staging, Path.GetFileName(typeof(EndToEndScenarioTests).Assembly.Location)));

                var package = ExtensionPackageBuilder.Build(
                    staging,
                    new ExtensionManifest { Id = "e2e.ext", Name = "E2E Extension", Version = "1.0.0" },
                    Path.Combine(registryDir, "e2e.ext-1.0.0.beepkg"));

                var packageManager = new ExtensionPackageManager(installRoot);
                var install = packageManager.Install(package.PackagePath);
                Assert.True(install.Success, install.Error);

                var host = new SkiaExtensionHost();
                host.LoadFromDirectory(install.Extension.InstallPath);
                Assert.Contains(host.Extensions, e => e.Id == "test.extension" && e.Success);

                // ── 7. Assisted generation produces a loadable diagram ─────────
                var suggestion = new Beep.Skia.Assist.DiagramAssistantRegistry().Generate(
                    new Beep.Skia.Assist.DiagramRequest
                    {
                        Prompt = "Start(start) \"Start\" -> Work \"Do work\" -> End(end) \"End\""
                    });
                Assert.True(suggestion.Success, string.Join("; ", suggestion.Warnings));

                var generated = new DrawingManager();
                generated.LoadFromDto(suggestion.Diagram);
                Assert.Equal(3, generated.GetComponents().Count);
                Assert.Equal(2, generated.GetLines().Count);
            }
            finally
            {
                try { Directory.Delete(workDir, true); } catch { }
            }
        }
    }
}

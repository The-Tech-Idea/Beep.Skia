using System.Linq;
using Beep.Skia;
using Beep.Skia.UML;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for UML component/deployment/artifact nodes: properties, serialization,
    /// XMI mapping, and shape rendering (including the DrawShape pipeline fix).
    /// </summary>
    public class UmlNodeTests
    {
        [Fact]
        public void ComponentNode_PropertiesRoundTrip()
        {
            var manager = new DrawingManager();
            var node = new UMLComponentNode
            {
                ComponentName = "Payments",
                ProvidedInterfaces = "IPayment",
                RequiredInterfaces = "ILogger"
            };
            manager.AddComponent(node);

            var manager2 = new DrawingManager();
            manager2.LoadFromDto(manager.ToDto());

            var loaded = manager2.GetComponents().OfType<UMLComponentNode>().Single();
            Assert.Equal("Payments", loaded.ComponentName);
            Assert.Equal("IPayment", loaded.ProvidedInterfaces);
            Assert.Equal("ILogger", loaded.RequiredInterfaces);
        }

        [Fact]
        public void DeploymentNode_PropertiesRoundTrip()
        {
            var manager = new DrawingManager();
            manager.AddComponent(new UMLDeploymentNode { NodeName = "AppServer", NodeType = "ExecutionEnvironment" });

            var manager2 = new DrawingManager();
            manager2.LoadFromDto(manager.ToDto());

            var loaded = manager2.GetComponents().OfType<UMLDeploymentNode>().Single();
            Assert.Equal("AppServer", loaded.NodeName);
            Assert.Equal("ExecutionEnvironment", loaded.NodeType);
        }

        [Fact]
        public void ArtifactNode_PropertiesRoundTrip()
        {
            var manager = new DrawingManager();
            manager.AddComponent(new UMLArtifactNode { ArtifactName = "API", FileName = "api.dll" });

            var manager2 = new DrawingManager();
            manager2.LoadFromDto(manager.ToDto());

            var loaded = manager2.GetComponents().OfType<UMLArtifactNode>().Single();
            Assert.Equal("API", loaded.ArtifactName);
            Assert.Equal("api.dll", loaded.FileName);
        }

        [Fact]
        public void XmiExport_MapsComponentDeploymentAndArtifact()
        {
            var manager = new DrawingManager();
            manager.AddComponent(new UMLComponentNode { ComponentName = "Payments" });
            manager.AddComponent(new UMLDeploymentNode { NodeName = "AppServer" });
            manager.AddComponent(new UMLArtifactNode { ArtifactName = "API" });

            var xmi = new XmiExporter().Export(manager.GetComponents(), manager.GetLines());

            Assert.Contains("uml:Component", xmi);
            Assert.Contains("name=\"Payments\"", xmi);
            Assert.Contains("uml:Node", xmi);
            Assert.Contains("name=\"AppServer\"", xmi);
            Assert.Contains("uml:Artifact", xmi);
            Assert.Contains("name=\"API\"", xmi);
        }

        [Fact]
        public void ShapeOnlyNodes_RenderContent()
        {
            // UMLUseCaseNode overrides DrawShape; the base DrawUMLContent must invoke it.
            var manager = new DrawingManager();
            manager.AddComponent(new UMLUseCaseNode { X = 20, Y = 20, Width = 140, Height = 70, UseCaseName = "Checkout" });

            using var bitmap = manager.RenderToBitmap(240, 160);

            bool hasNonWhitePixel = false;
            for (int y = 0; y < bitmap.Height && !hasNonWhitePixel; y += 2)
            {
                for (int x = 0; x < bitmap.Width; x += 2)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    if (pixel != SKColors.White && pixel.Alpha > 0)
                    {
                        hasNonWhitePixel = true;
                        break;
                    }
                }
            }
            Assert.True(hasNonWhitePixel, "Use case node rendered nothing (DrawShape pipeline broken).");
        }
    }
}

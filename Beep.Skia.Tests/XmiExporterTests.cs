using System.Linq;
using Beep.Skia;
using Beep.Skia.UML;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for <see cref="XmiExporter"/>: classes, interfaces, actors, use cases,
    /// packages, associations, and generalizations.
    /// </summary>
    public class XmiExporterTests
    {
        [Fact]
        public void Export_ClassWithMembers()
        {
            var manager = new DrawingManager();
            var cls = new UMLClass { ClassName = "Order", IsAbstract = true };
            cls.AttributesText = "- id: INT\n+ total: DECIMAL";
            cls.OperationsText = "+ submit(): bool";
            manager.AddComponent(cls);

            var xmi = new XmiExporter().Export(manager.GetComponents(), manager.GetLines(), "Shop");

            Assert.Contains("uml:Class", xmi);
            Assert.Contains("name=\"Order\"", xmi);
            Assert.Contains("isAbstract=\"true\"", xmi);
            Assert.Contains("name=\"id\" visibility=\"private\" type=\"INT\"", xmi);
            Assert.Contains("name=\"total\" visibility=\"public\" type=\"DECIMAL\"", xmi);
            Assert.Contains("ownedOperation", xmi);
            Assert.Contains("name=\"submit\" visibility=\"public\" type=\"bool\"", xmi);
            Assert.Contains("name=\"Shop\"", xmi);
        }

        [Fact]
        public void Export_InterfaceActorUseCaseAndPackage()
        {
            var manager = new DrawingManager();
            manager.AddComponent(new UMLInterface { InterfaceName = "IPayment" });
            manager.AddComponent(new UMLActor { ActorName = "Customer" });
            manager.AddComponent(new UMLUseCaseNode { UseCaseName = "Checkout" });
            manager.AddComponent(new UMLPackageNode { PackageName = "Sales" });

            var xmi = new XmiExporter().Export(manager.GetComponents(), manager.GetLines());

            Assert.Contains("uml:Interface", xmi);
            Assert.Contains("name=\"IPayment\"", xmi);
            Assert.Contains("uml:Actor", xmi);
            Assert.Contains("name=\"Customer\"", xmi);
            Assert.Contains("uml:UseCase", xmi);
            Assert.Contains("name=\"Checkout\"", xmi);
            Assert.Contains("uml:Package", xmi);
            Assert.Contains("name=\"Sales\"", xmi);
        }

        [Fact]
        public void Export_AssociationBetweenClasses()
        {
            var manager = new DrawingManager();
            var order = new UMLClass { ClassName = "Order" };
            var customer = new UMLClass { ClassName = "Customer" };
            manager.AddComponent(order);
            manager.AddComponent(customer);
            manager.ConnectComponents(order, customer, 0, 0);
            manager.GetLines().Single().Label1 = "places";

            var xmi = new XmiExporter().Export(manager.GetComponents(), manager.GetLines());

            Assert.Contains("uml:Association", xmi);
            Assert.Contains("name=\"places\"", xmi);
            Assert.Contains("memberEnd", xmi);
            Assert.Contains("ownedEnd", xmi);
        }

        [Fact]
        public void Export_GeneralizationFromInheritanceLine()
        {
            var manager = new DrawingManager();
            var child = new UMLClass { ClassName = "PremiumOrder" };
            var parent = new UMLClass { ClassName = "Order" };
            manager.AddComponent(child);
            manager.AddComponent(parent);

            var inheritance = new UMLInheritance
            {
                Start = child.OutConnectionPoints[0],
                End = parent.InConnectionPoints[0]
            };
            child.OutConnectionPoints[0].Connection = parent.InConnectionPoints[0];
            parent.InConnectionPoints[0].Connection = child.OutConnectionPoints[0];

            var xmi = new XmiExporter().Export(
                manager.GetComponents(),
                new[] { (Beep.Skia.Model.IConnectionLine)inheritance });

            Assert.Contains("uml:Generalization", xmi);
            Assert.Contains("general=", xmi);
            Assert.Contains("specific=", xmi);
        }

        [Fact]
        public void Export_EscapesXmlCharacters()
        {
            var manager = new DrawingManager();
            manager.AddComponent(new UMLClass { ClassName = "A&B" });

            var xmi = new XmiExporter().Export(manager.GetComponents(), manager.GetLines());

            Assert.Contains("name=\"A&amp;B\"", xmi);
        }
    }
}

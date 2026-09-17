using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beep.Skia;
using Beep.Skia.Model;

namespace Beep.Skia.UML
{
    /// <summary>
    /// Exports a UML class-style diagram to XMI 2.5 (UML 2.5) for interchange with
    /// UML tools (Enterprise Architect, Papyrus, StarUML, Visual Paradigm, etc.).
    /// Maps classes, interfaces, actors, use cases, and packages, plus associations,
    /// inheritance (generalizations), and member text.
    /// </summary>
    public class XmiExporter
    {
        /// <summary>
        /// Exports components and lines to XMI.
        /// </summary>
        public string Export(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines, string modelName = "Model")
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<xmi:XMI xmlns:xmi=\"http://www.omg.org/spec/XMI/20131001\"");
            sb.AppendLine("         xmlns:uml=\"http://www.omg.org/spec/UML/20131001\">");
            sb.AppendLine("  <xmi:Documentation exporter=\"Beep.Skia\" exporterVersion=\"1.0\" />");
            sb.AppendLine($"  <uml:Model xmi:id=\"model_1\" name=\"{EscapeXml(modelName)}\">");

            var idMap = new Dictionary<SkiaComponent, string>();
            int counter = 0;
            string NextId(string prefix) => $"{prefix}_{++counter}";

            var classifiers = (components ?? Array.Empty<SkiaComponent>())
                .Where(c => c is UMLControl && !c.IsStatic)
                .ToList();

            foreach (var component in classifiers)
            {
                switch (component)
                {
                    case UMLClass umlClass:
                        {
                            string id = NextId("class");
                            idMap[component] = id;
                            sb.AppendLine($"    <packagedElement xmi:type=\"uml:Class\" xmi:id=\"{id}\" name=\"{EscapeXml(umlClass.ClassName)}\" isAbstract=\"{umlClass.IsAbstract.ToString().ToLowerInvariant()}\">");
                            AppendMembers(sb, umlClass.AttributesText, isOperation: false, NextId);
                            AppendMembers(sb, umlClass.OperationsText, isOperation: true, NextId);
                            sb.AppendLine("    </packagedElement>");
                            break;
                        }
                    case UMLInterface umlInterface:
                        {
                            string id = NextId("interface");
                            idMap[component] = id;
                            sb.AppendLine($"    <packagedElement xmi:type=\"uml:Interface\" xmi:id=\"{id}\" name=\"{EscapeXml(umlInterface.InterfaceName)}\">");
                            AppendMembers(sb, umlInterface.OperationsText, isOperation: true, NextId);
                            sb.AppendLine("    </packagedElement>");
                            break;
                        }
                    case UMLActor actor:
                        {
                            string id = NextId("actor");
                            idMap[component] = id;
                            sb.AppendLine($"    <packagedElement xmi:type=\"uml:Actor\" xmi:id=\"{id}\" name=\"{EscapeXml(actor.ActorName)}\" />");
                            break;
                        }
                    case UMLUseCaseNode useCase:
                        {
                            string id = NextId("usecase");
                            idMap[component] = id;
                            sb.AppendLine($"    <packagedElement xmi:type=\"uml:UseCase\" xmi:id=\"{id}\" name=\"{EscapeXml(useCase.UseCaseName)}\" />");
                            break;
                        }
                    case UMLPackageNode package:
                        {
                            string id = NextId("package");
                            idMap[component] = id;
                            sb.AppendLine($"    <packagedElement xmi:type=\"uml:Package\" xmi:id=\"{id}\" name=\"{EscapeXml(package.PackageName)}\" />");
                            break;
                        }
                    case UMLComponentNode componentNode:
                        {
                            string id = NextId("component");
                            idMap[component] = id;
                            sb.AppendLine($"    <packagedElement xmi:type=\"uml:Component\" xmi:id=\"{id}\" name=\"{EscapeXml(componentNode.ComponentName)}\" />");
                            break;
                        }
                    case UMLDeploymentNode deployment:
                        {
                            string id = NextId("node");
                            idMap[component] = id;
                            sb.AppendLine($"    <packagedElement xmi:type=\"uml:Node\" xmi:id=\"{id}\" name=\"{EscapeXml(deployment.NodeName)}\" />");
                            break;
                        }
                    case UMLArtifactNode artifact:
                        {
                            string id = NextId("artifact");
                            idMap[component] = id;
                            sb.AppendLine($"    <packagedElement xmi:type=\"uml:Artifact\" xmi:id=\"{id}\" name=\"{EscapeXml(artifact.ArtifactName)}\" />");
                            break;
                        }
                }
            }

            AppendRelationships(sb, lines, idMap, NextId);

            sb.AppendLine("  </uml:Model>");
            sb.AppendLine("</xmi:XMI>");
            return sb.ToString();
        }

        private static void AppendMembers(StringBuilder sb, string membersText, bool isOperation, Func<string, string> nextId)
        {
            if (string.IsNullOrWhiteSpace(membersText)) return;

            foreach (var rawLine in membersText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var (name, type, visibility) = ParseMember(rawLine);
                if (string.IsNullOrWhiteSpace(name)) continue;

                if (isOperation)
                    name = name.Replace("()", string.Empty).Trim();

                string id = nextId(isOperation ? "op" : "attr");
                var typeAttribute = string.IsNullOrWhiteSpace(type) ? string.Empty : $" type=\"{EscapeXml(type)}\"";
                sb.AppendLine($"      <{(isOperation ? "ownedOperation" : "ownedAttribute")} xmi:type=\"uml:{(isOperation ? "Operation" : "Property")}\" xmi:id=\"{id}\" name=\"{EscapeXml(name)}\" visibility=\"{visibility}\"{typeAttribute} />");
            }
        }

        private static void AppendRelationships(
            StringBuilder sb,
            IReadOnlyList<IConnectionLine> lines,
            Dictionary<SkiaComponent, string> idMap,
            Func<string, string> nextId)
        {
            if (lines == null) return;

            foreach (var line in lines)
            {
                if (!(line?.Start?.Component is SkiaComponent source)) continue;
                if (!(line?.End?.Component is SkiaComponent target)) continue;
                if (!idMap.TryGetValue(source, out var sourceId)) continue;
                if (!idMap.TryGetValue(target, out var targetId)) continue;

                if (line is UMLInheritance)
                {
                    // Convention: Start = child (specific), End = parent (general).
                    string generalId = nextId("generalization");
                    sb.AppendLine($"    <packagedElement xmi:type=\"uml:Generalization\" xmi:id=\"{generalId}\" general=\"{targetId}\" specific=\"{sourceId}\" />");
                    continue;
                }

                string associationId = nextId("association");
                string sourceEndId = nextId("end");
                string targetEndId = nextId("end");
                string name = !string.IsNullOrWhiteSpace(line.Label1) ? line.Label1 : string.Empty;

                var association = line as UMLAssociation;
                string sourceRole = association != null && !string.IsNullOrWhiteSpace(association.SourceRole)
                    ? $" name=\"{EscapeXml(association.SourceRole)}\"" : string.Empty;
                string targetRole = association != null && !string.IsNullOrWhiteSpace(association.TargetRole)
                    ? $" name=\"{EscapeXml(association.TargetRole)}\"" : string.Empty;

                sb.AppendLine($"    <packagedElement xmi:type=\"uml:Association\" xmi:id=\"{associationId}\"{(string.IsNullOrWhiteSpace(name) ? string.Empty : $" name=\"{EscapeXml(name)}\"")}>");
                sb.AppendLine($"      <memberEnd xmi:idref=\"{sourceEndId}\" />");
                sb.AppendLine($"      <memberEnd xmi:idref=\"{targetEndId}\" />");
                sb.AppendLine($"      <ownedEnd xmi:type=\"uml:Property\" xmi:id=\"{sourceEndId}\" type=\"{sourceId}\"{sourceRole} />");
                sb.AppendLine($"      <ownedEnd xmi:type=\"uml:Property\" xmi:id=\"{targetEndId}\" type=\"{targetId}\"{targetRole} />");
                sb.AppendLine("    </packagedElement>");
            }
        }

        private static (string Name, string Type, string Visibility) ParseMember(string rawLine)
        {
            var text = (rawLine ?? string.Empty).Trim();
            string visibility = "public";

            if (text.StartsWith("+")) { visibility = "public"; text = text.Substring(1).Trim(); }
            else if (text.StartsWith("-")) { visibility = "private"; text = text.Substring(1).Trim(); }
            else if (text.StartsWith("#")) { visibility = "protected"; text = text.Substring(1).Trim(); }
            else if (text.StartsWith("~")) { visibility = "package"; text = text.Substring(1).Trim(); }

            int colon = text.IndexOf(':');
            var name = colon >= 0 ? text.Substring(0, colon).Trim() : text;
            var type = colon >= 0 ? text.Substring(colon + 1).Trim() : string.Empty;
            return (name, type, visibility);
        }

        private static string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return System.Net.WebUtility.HtmlEncode(text);
        }
    }
}

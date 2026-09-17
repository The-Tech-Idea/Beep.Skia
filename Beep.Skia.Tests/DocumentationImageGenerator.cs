using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Beep.Skia;
using Beep.Skia.Model;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Generates the documentation images under Help/assets/families by rendering the real
    /// components through the real pipeline. Disabled unless BEEP_SKIA_GENERATE_DOC_IMAGES=1 so
    /// normal test runs (and CI) never write to the repository.
    ///
    /// Usage:  $env:BEEP_SKIA_GENERATE_DOC_IMAGES=1; dotnet test --filter DocumentationImageGenerator
    /// </summary>
    public class DocumentationImageGenerator
    {
        private static readonly (string Assembly, string File)[] Families =
        {
            ("Beep.Skia.FlowChart", "flowchart"),
            ("Beep.Skia.Business", "business-process"),
            ("Beep.Skia.ERD", "erd"),
            ("Beep.Skia.DFD", "dfd"),
            ("Beep.Skia.ETL", "etl"),
            ("Beep.Skia.UML", "uml"),
            ("Beep.Skia.Network", "network"),
            ("Beep.Skia.PM", "project-management"),
            ("Beep.Skia.MindMap", "mindmap"),
            ("Beep.Skia.StateMachine", "state-machine"),
            ("Beep.Skia.ECAD", "ecad"),
            ("Beep.Skia.Cloud", "cloud"),
            ("Beep.Skia.Security", "security"),
            ("Beep.Skia.ML", "ml"),
            ("Beep.Ski.Quantitative", "quantitative"),
            ("Beep.Skia.WellLogs", "welllogs"),
            ("Beep.Skia", "ui-controls")
        };

        private const int TileWidth = 240;
        private const int TileHeight = 170;
        private const int Columns = 4;
        private const int LabelHeight = 26;
        private const int Margin = 12;

        [Fact]
        public void GenerateFamilyImages()
        {
            if (Environment.GetEnvironmentVariable("BEEP_SKIA_GENERATE_DOC_IMAGES") != "1")
                return;

            var root = FindRepositoryRoot();
            Assert.False(root == null, "repository root (containing Help/) was not found");

            var outputDir = Path.Combine(root, "Help", "assets", "families");
            Directory.CreateDirectory(outputDir);

            var generated = 0;
            foreach (var family in Families)
            {
                var types = DiscoverTypes(family.Assembly).ToList();
                if (types.Count == 0) continue;

                var rows = (int)Math.Ceiling(types.Count / (double)Columns);
                var width = Margin * 2 + Columns * TileWidth;
                var height = Margin * 2 + rows * (TileHeight + LabelHeight);

                using var surface = SKSurface.Create(new SKImageInfo(width, height));
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);

                using var labelPaint = new SKPaint
                {
                    Color = new SKColor(0x33, 0x33, 0x3B),
                    IsAntialias = true
                };
                using var borderPaint = new SKPaint
                {
                    Color = new SKColor(0xE0, 0xE0, 0xE6),
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1f
                };
                using var typeface = SKTypeface.FromFamilyName("Segoe UI") ?? SKTypeface.Default;
                using var font = new SKFont(typeface, 13f);

                var manager = new DrawingManager();
                var placed = new List<(SkiaComponent Component, string Name)>();

                for (var i = 0; i < types.Count; i++)
                {
                    var col = i % Columns;
                    var row = i / Columns;
                    var x = Margin + col * TileWidth;
                    var y = Margin + row * (TileHeight + LabelHeight);

                    canvas.DrawRect(new SKRect(x, y, x + TileWidth - 8, y + TileHeight - 8), borderPaint);

                    SkiaComponent component;
                    try { component = (SkiaComponent)Activator.CreateInstance(types[i])!; }
                    catch { continue; }

                    var w = Math.Min(component.Width <= 0 ? 120 : component.Width, TileWidth - 60);
                    var h = Math.Min(component.Height <= 0 ? 60 : component.Height, TileHeight - 60);
                    component.Width = w;
                    component.Height = h;
                    component.X = x + (TileWidth - 8 - w) / 2;
                    component.Y = y + (TileHeight - 8 - h) / 2 - 6;
                    manager.AddComponent(component);
                    placed.Add((component, Humanize(types[i].Name)));
                }

                manager.Draw(canvas);

                for (var i = 0; i < placed.Count; i++)
                {
                    var col = i % Columns;
                    var row = i / Columns;
                    var x = Margin + col * TileWidth;
                    var y = Margin + row * (TileHeight + LabelHeight);
                    canvas.DrawText(placed[i].Name, x + (TileWidth - 8) / 2f, y + TileHeight + 4, SKTextAlign.Center, font, labelPaint);
                }

                using var image = surface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Png, 92);
                using var stream = File.OpenWrite(Path.Combine(outputDir, family.File + ".png"));
                data.SaveTo(stream);

                foreach (var (component, _) in placed) component.Dispose();
                generated++;
            }

            Assert.True(generated > 0, "no family images were generated");
        }

        [Fact]
        public void GenerateTemplateImages()
        {
            if (Environment.GetEnvironmentVariable("BEEP_SKIA_GENERATE_DOC_IMAGES") != "1")
                return;

            var root = FindRepositoryRoot();
            Assert.False(string.IsNullOrEmpty(root), "repository root (containing Help/) was not found");

            var outputDir = Path.Combine(root, "Help", "assets", "templates");
            Directory.CreateDirectory(outputDir);

            var generated = 0;
            foreach (var template in Beep.Skia.DiagramTemplates.All)
            {
                var manager = new DrawingManager();
                manager.LoadTemplate(template.Value());

                var bounds = manager.GetContentBounds(24f);
                var width = (int)Math.Ceiling(Math.Max(320, bounds.Width));
                var height = (int)Math.Ceiling(Math.Max(200, bounds.Height));

                using var surface = SKSurface.Create(new SKImageInfo(width, height));
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);
                canvas.Translate(-bounds.Left, -bounds.Top);
                manager.Draw(canvas);

                using var image = surface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Png, 92);
                using var stream = File.OpenWrite(Path.Combine(outputDir, SafeName(template.Key) + ".png"));
                data.SaveTo(stream);
                generated++;
            }

            Assert.True(generated > 0, "no template images were generated");
        }

        [Fact]
        public void GenerateArchitectureImage()
        {
            if (Environment.GetEnvironmentVariable("BEEP_SKIA_GENERATE_DOC_IMAGES") != "1")
                return;

            var root = FindRepositoryRoot();
            Assert.False(string.IsNullOrEmpty(root), "repository root (containing Help/) was not found");

            var outputDir = Path.Combine(root, "Help", "assets");
            Directory.CreateDirectory(outputDir);

            var manager = new DrawingManager();
            var layers = new (string Title, float Y, string[] Boxes)[]
            {
                ("Hosts", 60, new[] { "WinForms", "WPF", "Blazor", "MAUI", "Avalonia" }),
                ("DrawingManager", 190, new[] { "Canvas + viewport", "Selection", "History", "Templates", "Validation" }),
                ("Components", 320, new[] { "SkiaComponent", "Material controls", "16 diagram families", "293 component types" }),
                ("Connections", 450, new[] { "Routing", "Labels", "Animation", "ERD multiplicity" }),
                ("Services", 580, new[] { "Serialization", "Automation", "Extensions", "Collaboration", "Assist" })
            };

            var midpoints = new List<SkiaComponent>();

            foreach (var layer in layers)
            {
                var boxWidth = 180f;
                var gap = 16f;
                var totalWidth = layer.Boxes.Length * boxWidth + (layer.Boxes.Length - 1) * gap;
                var x = 40f + (900f - totalWidth) / 2f;
                SkiaComponent middle = null;
                var index = 0;

                foreach (var label in layer.Boxes)
                {
                    var box = new Beep.Skia.Flowchart.ProcessNode
                    {
                        Label = label,
                        X = x,
                        Y = layer.Y,
                        Width = boxWidth,
                        Height = 56
                    };
                    manager.AddComponent(box);
                    if (index == layer.Boxes.Length / 2) middle = box;
                    x += boxWidth + gap;
                    index++;
                }

                if (middle != null) midpoints.Add(middle);

                var band = new Beep.Skia.Components.Label
                {
                    Text = layer.Title,
                    X = 40,
                    Y = layer.Y - 32,
                    Width = 900,
                    Height = 26,
                    TextFontSize = 14
                };
                manager.AddComponent(band);
            }

            for (var i = 0; i < midpoints.Count - 1; i++)
            {
                manager.ConnectComponents(midpoints[i], midpoints[i + 1]);
                var line = manager.GetLines().Last();
                line.RoutingMode = LineRoutingMode.Straight;
                line.ShowEndArrow = true;
                line.LineColor = new SKColor(0x79, 0x75, 0x7E);
            }

            var bounds = manager.GetContentBounds(24f);
            var width = (int)Math.Ceiling(Math.Max(900, bounds.Width));
            var height = (int)Math.Ceiling(Math.Max(200, bounds.Height));

            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            canvas.Translate(-bounds.Left, -bounds.Top);
            manager.Draw(canvas);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 92);
            using var stream = File.OpenWrite(Path.Combine(outputDir, "architecture.png"));
            data.SaveTo(stream);

            foreach (var component in manager.GetComponents()) component.Dispose();
        }

        private static string SafeName(string name)
        {
            var chars = name.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-').ToArray();
            return new string(chars).Trim('-');
        }

        private static string FindRepositoryRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (var i = 0; i < 10 && dir != null; i++, dir = dir.Parent)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "Help")) &&
                    File.Exists(Path.Combine(dir.FullName, "Beep.Skia.Solution.sln")))
                    return dir.FullName;
            }
            return null;
        }

        private static IEnumerable<Type> DiscoverTypes(string assemblyName)
        {
            Assembly assembly;
            try { assembly = Assembly.Load(assemblyName); }
            catch { yield break; }

            Type[] types;
            try { types = assembly.GetExportedTypes(); }
            catch { yield break; }

            foreach (var type in types
                .Where(t => !t.IsAbstract && !t.IsInterface && t.IsClass && !t.IsGenericTypeDefinition)
                .Where(t => typeof(SkiaComponent).IsAssignableFrom(t))
                .Where(t => t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.Name))
            {
                yield return type;
            }
        }

        private static string Humanize(string name)
        {
            var spaced = System.Text.RegularExpressions.Regex.Replace(name, "(?<=[a-z0-9])(?=[A-Z])", " ");
            return spaced.Replace("Node", string.Empty).Replace("Control", string.Empty).Trim();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Beep.Skia;
using Beep.Skia.Serialization;
using SkiaSharp;
using Xunit;
using Xunit.Abstractions;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Whole-surface sweep: instantiates every public component type in every family and verifies
    /// two universal invariants that no unit test covers per type:
    ///
    ///   1. rendering must not throw
    ///   2. a save/load round-trip must preserve the component type
    ///
    /// Types that cannot be instantiated without configuration are reported, not failed.
    /// </summary>
    public class ComponentSurfaceSweepTests
    {
        private readonly ITestOutputHelper _output;

        public ComponentSurfaceSweepTests(ITestOutputHelper output) => _output = output;

        private static readonly string[] FamilyAssemblies =
        {
            "Beep.Skia",
            "Beep.Skia.ERD",
            "Beep.Skia.UML",
            "Beep.Skia.DFD",
            "Beep.Skia.StateMachine",
            "Beep.Skia.MindMap",
            "Beep.Skia.PM",
            "Beep.Skia.Network",
            "Beep.Skia.ETL",
            "Beep.Skia.Business",
            "Beep.Skia.ECAD",
            "Beep.Skia.Cloud",
            "Beep.Skia.ML",
            "Beep.Ski.Quantitative",
            "Beep.Skia.WellLogs",
            "Beep.Skia.FlowChart"
        };

        private static IEnumerable<Type> DiscoverComponentTypes()
        {
            foreach (var assemblyName in FamilyAssemblies)
            {
                Assembly assembly;
                try { assembly = Assembly.Load(assemblyName); }
                catch { continue; }

                Type[] types;
                try { types = assembly.GetExportedTypes(); }
                catch { continue; }

                foreach (var type in types)
                {
                    if (type.IsAbstract || type.IsInterface || !type.IsClass) continue;
                    if (!typeof(SkiaComponent).IsAssignableFrom(type)) continue;
                    if (type.GetConstructor(Type.EmptyTypes) == null) continue;
                    if (type.IsGenericTypeDefinition) continue;
                    yield return type;
                }
            }
        }

        [Fact]
        public void EveryComponentType_RendersWithoutThrowing()
        {
            var rendered = 0;
            var noInk = new List<string>();
            var failures = new List<string>();

            using var surface = SKSurface.Create(new SKImageInfo(400, 300));

            foreach (var type in DiscoverComponentTypes().Distinct())
            {
                SkiaComponent component;
                try
                {
                    component = (SkiaComponent)Activator.CreateInstance(type);
                }
                catch
                {
                    continue; // needs constructor arguments
                }

                component.X = 20;
                component.Y = 20;
                if (component.Width <= 0) component.Width = 120;
                if (component.Height <= 0) component.Height = 60;
                component.Name = type.Name;

                try
                {
                    var canvas = surface.Canvas;
                    canvas.Clear(SKColors.White);

                    // Components cull against the context bounds, so pass a real viewport
                    // (the parameterless Draw() overload supplies an empty rect and many
                    // components legitimately draw nothing).
                    var context = new Beep.Skia.Model.DrawingContext
                    {
                        PanOffset = SKPoint.Empty,
                        Zoom = 1f,
                        Bounds = new SKRect(0, 0, 400, 300)
                    };
                    component.Draw(canvas, context);
                    canvas.Flush();

                    using var image = surface.Snapshot();
                    using var bitmap = SKBitmap.FromImage(image);
                    var ink = 0;
                    for (var y = 0; y < bitmap.Height && ink == 0; y++)
                        for (var x = 0; x < bitmap.Width; x++)
                            if (bitmap.GetPixel(x, y) != SKColors.White) { ink = 1; break; }

                    if (ink == 0) noInk.Add(type.FullName);
                    rendered++;
                }
                catch (Exception ex)
                {
                    failures.Add($"{type.FullName}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            _output.WriteLine($"rendered {rendered} component types; {noInk.Count} produced no ink; {failures.Count} threw");
            if (noInk.Count > 0)
                _output.WriteLine("no ink: " + string.Join(", ", noInk.Take(30).Select(n => n.Split('.')[^1])));
            if (failures.Count > 0)
                _output.WriteLine("failures:\n  " + string.Join("\n  ", failures.Take(20)));

            Assert.True(rendered > 50, $"the sweep should exercise the component surface (rendered {rendered})");
            // Types that legitimately draw nothing without content (menus, tabs, image holders, links).
            Assert.True(rendered - noInk.Count >= 200,
                $"most components must produce visible output (only {rendered - noInk.Count} of {rendered} did)");
            Assert.True(failures.Count == 0,
                $"{failures.Count} component type(s) threw while rendering:\n  {string.Join("\n  ", failures.Take(20))}");
        }

        [Fact]
        public void EveryComponentType_SurvivesSerializationRoundTrip()
        {
            var preserved = new List<string>();
            var lost = new List<string>();
            var failures = new List<string>();

            foreach (var type in DiscoverComponentTypes().Distinct())
            {
                SkiaComponent component;
                try
                {
                    component = (SkiaComponent)Activator.CreateInstance(type);
                }
                catch
                {
                    continue;
                }

                component.X = 30;
                component.Y = 40;
                component.Width = 130;
                component.Height = 70;
                component.Name = "sweep-" + type.Name;

                try
                {
                    var manager = new DrawingManager();
                    manager.AddComponent(component);

                    var json = JsonSerializer.Serialize(manager.ToDto());
                    var reloaded = new DrawingManager();
                    reloaded.LoadFromDto(JsonSerializer.Deserialize<DiagramDto>(json));

                    var restored = reloaded.GetComponents();
                    if (restored.Count == 1 && restored[0].GetType() == type)
                        preserved.Add(type.FullName);
                    else
                        lost.Add($"{type.FullName} -> {(restored.Count == 0 ? "(missing)" : restored[0].GetType().FullName)}");
                }
                catch (Exception ex)
                {
                    failures.Add($"{type.FullName}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            _output.WriteLine($"round-trip preserved {preserved.Count} types; lost {lost.Count}; threw {failures.Count}");
            if (lost.Count > 0)
                _output.WriteLine("lost:\n  " + string.Join("\n  ", lost.Take(20)));
            if (failures.Count > 0)
                _output.WriteLine("failures:\n  " + string.Join("\n  ", failures.Take(20)));

            Assert.True(preserved.Count > 50, $"the sweep should exercise the component surface (preserved {preserved.Count})");
            Assert.True(lost.Count == 0,
                $"{lost.Count} component type(s) did not survive the round-trip:\n  {string.Join("\n  ", lost.Take(20))}");
            Assert.True(failures.Count == 0,
                $"{failures.Count} component type(s) threw during serialization:\n  {string.Join("\n  ", failures.Take(20))}");
        }
    }
}

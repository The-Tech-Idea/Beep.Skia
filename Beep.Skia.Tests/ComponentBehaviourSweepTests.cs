using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beep.Skia;
using Beep.Skia.Model;
using SkiaSharp;
using Xunit;
using Xunit.Abstractions;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Whole-surface sweep for the two operations the framework performs on every component outside
    /// of drawing: <c>Update</c> (called once per frame by the render loop) and property write-back
    /// (driven by the property grid and by <c>LoadFromDto</c>). Also renders at extreme zoom levels.
    ///
    /// Every invariant here is universal, so a failure is a real defect for that component type.
    /// </summary>
    public class ComponentBehaviourSweepTests
    {
        private readonly ITestOutputHelper _output;

        public ComponentBehaviourSweepTests(ITestOutputHelper output) => _output = output;

        private static readonly string[] FamilyAssemblies =
        {
            "Beep.Skia", "Beep.Skia.ERD", "Beep.Skia.UML", "Beep.Skia.DFD", "Beep.Skia.StateMachine",
            "Beep.Skia.MindMap", "Beep.Skia.PM", "Beep.Skia.Network", "Beep.Skia.ETL", "Beep.Skia.Business",
            "Beep.Skia.ECAD", "Beep.Skia.Cloud", "Beep.Skia.ML", "Beep.Ski.Quantitative",
            "Beep.Skia.WellLogs", "Beep.Skia.FlowChart"
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

        private static SkiaComponent Create(Type type)
        {
            var component = (SkiaComponent)Activator.CreateInstance(type);
            component.X = 25;
            component.Y = 35;
            if (component.Width <= 0) component.Width = 140;
            if (component.Height <= 0) component.Height = 70;
            component.Name = type.Name;
            return component;
        }

        private static DrawingContext Context(float zoom = 1f) => new DrawingContext
        {
            PanOffset = SKPoint.Empty,
            Zoom = zoom,
            Bounds = new SKRect(0, 0, 400, 300)
        };

        [Fact]
        public void EveryComponentType_UpdatesWithoutThrowing()
        {
            var updated = 0;
            var failures = new List<string>();

            foreach (var type in DiscoverComponentTypes().Distinct())
            {
                SkiaComponent component;
                try { component = Create(type); }
                catch { continue; }

                try
                {
                    // The render loop calls Update once per frame, then draws.
                    component.Update(Context());
                    component.Update(Context(0.25f));
                    component.Update(Context(4f));
                    updated++;
                }
                catch (Exception ex)
                {
                    failures.Add($"{type.FullName}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            _output.WriteLine($"updated {updated} component types; {failures.Count} threw");
            if (failures.Count > 0)
                _output.WriteLine("failures:\n  " + string.Join("\n  ", failures.Take(20)));

            Assert.True(updated > 200, $"the sweep should exercise the component surface (updated {updated})");
            Assert.True(failures.Count == 0,
                $"{failures.Count} component type(s) threw during Update:\n  {string.Join("\n  ", failures.Take(20))}");
        }

        [Fact]
        public void EveryComponentType_AcceptsItsOwnPropertyValues()
        {
            var applied = 0;
            var failures = new List<string>();

            foreach (var type in DiscoverComponentTypes().Distinct())
            {
                SkiaComponent component;
                try { component = Create(type); }
                catch { continue; }

                try
                {
                    // Mirror what serialization does: read every property, then write it back.
                    var properties = component.GetProperties(includeCommon: true, includeNodeProperties: true);
                    if (properties.Count == 0) continue;

                    component.SetPropperties(properties, updateNodeProperties: true, applyToPublicSetters: true);
                    applied++;
                }
                catch (Exception ex)
                {
                    failures.Add($"{type.FullName}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            _output.WriteLine($"wrote back properties for {applied} component types; {failures.Count} threw");
            if (failures.Count > 0)
                _output.WriteLine("failures:\n  " + string.Join("\n  ", failures.Take(20)));

            Assert.True(applied > 200, $"the sweep should exercise the component surface (applied {applied})");
            Assert.True(failures.Count == 0,
                $"{failures.Count} component type(s) threw while applying their own properties:\n  {string.Join("\n  ", failures.Take(20))}");
        }

        [Fact]
        public void EveryComponentType_RendersAtExtremeZoomLevels()
        {
            var rendered = 0;
            var failures = new List<string>();

            using var surface = SKSurface.Create(new SKImageInfo(400, 300));

            foreach (var type in DiscoverComponentTypes().Distinct())
            {
                SkiaComponent component;
                try { component = Create(type); }
                catch { continue; }

                try
                {
                    foreach (var zoom in new[] { 0.1f, 0.25f, 1f, 4f, 8f })
                    {
                        var canvas = surface.Canvas;
                        canvas.Clear(SKColors.White);
                        canvas.Save();
                        canvas.Scale(zoom);
                        component.Draw(canvas, Context(zoom));
                        canvas.Restore();
                        canvas.Flush();
                    }
                    rendered++;
                }
                catch (Exception ex)
                {
                    failures.Add($"{type.FullName}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            _output.WriteLine($"rendered {rendered} component types at 5 zoom levels; {failures.Count} threw");
            if (failures.Count > 0)
                _output.WriteLine("failures:\n  " + string.Join("\n  ", failures.Take(20)));

            Assert.True(rendered > 200, $"the sweep should exercise the component surface (rendered {rendered})");
            Assert.True(failures.Count == 0,
                $"{failures.Count} component type(s) threw at extreme zoom:\n  {string.Join("\n  ", failures.Take(20))}");
        }
    }
}

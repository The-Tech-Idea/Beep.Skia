using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beep.Skia;
using SkiaSharp;
using Xunit;
using Xunit.Abstractions;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Whole-surface interaction sweep: drives the editor's input pipeline (hit-testing, selection,
    /// drag, keyboard) against every public component type.
    ///
    /// Components override the mouse/keyboard hooks, and the editor calls them on every click, so a
    /// throw here breaks the editor for that component even though rendering looks fine.
    /// </summary>
    public class ComponentInteractionSweepTests
    {
        private readonly ITestOutputHelper _output;

        public ComponentInteractionSweepTests(ITestOutputHelper output) => _output = output;

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

        /// <summary>
        /// Drives a full click / drag / release / wheel / keyboard sequence over a component that is
        /// centred in the viewport, so the pointer lands inside it. Returns whether the initial click
        /// selected the component (checked before the destructive key presses run).
        /// </summary>
        private static bool ExerciseInput(DrawingManager manager, SkiaComponent component)
        {
            var centre = new SKPoint(component.X + component.Width / 2f, component.Y + component.Height / 2f);
            var outside = new SKPoint(component.X - 50, component.Y - 50);

            // Hover, press inside, release: this is what selects a component in the editor.
            manager.HandleMouseMove(centre);
            manager.HandleMouseDown(centre);
            manager.HandleMouseUp(centre);
            var selected = manager.SelectionManager.SelectedComponents.Contains(component);

            // Drag the selected component, then pan with the middle button.
            manager.HandleMouseDown(centre);
            manager.HandleMouseMove(new SKPoint(centre.X + 25, centre.Y + 15));
            manager.HandleMouseUp(new SKPoint(centre.X + 25, centre.Y + 15));

            manager.HandleMouseDown(centre, mouseButton: 2); // middle-button pan
            manager.HandleMouseMove(new SKPoint(centre.X + 10, centre.Y + 10));
            manager.HandleMouseUp(new SKPoint(centre.X + 10, centre.Y + 10), mouseButton: 2);

            manager.HandleMouseWheel(centre, 120);
            manager.HandleMouseWheel(centre, -120);

            // Click empty canvas and drag a selection box.
            manager.HandleMouseDown(outside);
            manager.HandleMouseMove(centre);
            manager.HandleMouseUp(centre);

            // Key codes follow the WinForms Keys convention used by the host (Tab=9, Delete=46, A=65);
            // modifier bit 1 = Ctrl.
            manager.HandleKeyDown(9);
            manager.HandleKeyDown(46);
            manager.HandleKeyDown(65, 1);

            return selected;
        }

        [Fact]
        public void EveryComponentType_SurvivesTheInteractionPipeline()
        {
            var exercised = 0;
            var selectable = 0;
            var failures = new List<string>();

            foreach (var type in DiscoverComponentTypes().Distinct())
            {
                SkiaComponent component;
                DrawingManager manager;
                try
                {
                    component = (SkiaComponent)Activator.CreateInstance(type);
                    component.X = 150;
                    component.Y = 120;
                    if (component.Width <= 0) component.Width = 120;
                    if (component.Height <= 0) component.Height = 60;
                    component.Name = type.Name;

                    manager = new DrawingManager();
                    manager.AddComponent(component);
                }
                catch
                {
                    continue;
                }

                try
                {
                    if (ExerciseInput(manager, component)) selectable++;
                    exercised++;
                }
                catch (Exception ex)
                {
                    failures.Add($"{type.FullName}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            _output.WriteLine($"exercised {exercised} component types through the input pipeline ({selectable} selectable); {failures.Count} threw");
            if (failures.Count > 0)
                _output.WriteLine("failures:\n  " + string.Join("\n  ", failures.Take(20)));

            Assert.True(exercised > 200, $"the sweep should exercise the component surface (exercised {exercised})");
            Assert.True(failures.Count == 0,
                $"{failures.Count} component type(s) threw during interaction:\n  {string.Join("\n  ", failures.Take(20))}");
            // A component that cannot be click-selected cannot be edited; static overlays are the
            // exception, so the bar is deliberately below the full count.
            Assert.True(selectable >= 150,
                $"most components must be click-selectable (only {selectable} of {exercised} were)");
        }

        [Fact]
        public void EveryComponentType_SupportsClipboardAndUndoRedo()
        {
            var exercised = 0;
            var failures = new List<string>();

            foreach (var type in DiscoverComponentTypes().Distinct())
            {
                SkiaComponent component;
                DrawingManager manager;
                try
                {
                    component = (SkiaComponent)Activator.CreateInstance(type);
                    component.X = 60;
                    component.Y = 60;
                    if (component.Width <= 0) component.Width = 120;
                    if (component.Height <= 0) component.Height = 60;
                    component.Name = type.Name;

                    manager = new DrawingManager();
                    manager.AddComponent(component);
                }
                catch
                {
                    continue;
                }

                try
                {
                    manager.SelectAllComponents();
                    manager.CopySelectedComponents();
                    manager.PasteComponents(new SKPoint(300, 300));
                    manager.DeleteSelectedComponents();

                    manager.Undo();
                    manager.Undo();
                    manager.Undo();
                    manager.Redo();
                    manager.Redo();
                    manager.Redo();

                    manager.ClearComponents();
                    exercised++;
                }
                catch (Exception ex)
                {
                    failures.Add($"{type.FullName}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            _output.WriteLine($"exercised clipboard/undo for {exercised} component types; {failures.Count} threw");
            if (failures.Count > 0)
                _output.WriteLine("failures:\n  " + string.Join("\n  ", failures.Take(20)));

            Assert.True(exercised > 200, $"the sweep should exercise the component surface (exercised {exercised})");
            Assert.True(failures.Count == 0,
                $"{failures.Count} component type(s) threw during clipboard/undo:\n  {string.Join("\n  ", failures.Take(20))}");
        }
    }
}

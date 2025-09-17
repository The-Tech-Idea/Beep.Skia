using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Beep.Skia.Components;
using Beep.Skia.Model;
namespace Beep.Skia.Sample.WinForms
{
    public partial class MainForm : Form
    {
        // Simple mapping from toolbox display names to Skia component CLR types
        private readonly System.Collections.Generic.Dictionary<string, string> _toolboxMap = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["Button"] = typeof(Beep.Skia.Components.Button).AssemblyQualifiedName,
            ["Label"] = typeof(Beep.Skia.Components.Label).AssemblyQualifiedName,
            ["Menu"] = typeof(Beep.Skia.Components.Menu).AssemblyQualifiedName,
            ["Card"] = typeof(Beep.Skia.Components.Card).AssemblyQualifiedName,
            ["Checkbox"] = typeof(Beep.Skia.Components.Checkbox).AssemblyQualifiedName,
            ["TextBox"] = typeof(Beep.Skia.Components.TextBox).AssemblyQualifiedName,
            ["Panel"] = typeof(Beep.Skia.Components.Panel).AssemblyQualifiedName,
            // add more as needed
        };

        public MainForm()
        {
            InitializeComponent();

            // Palette and discovery are handled by SkiaHostControl now to avoid duplication.

            toolAddButton.Click += (s, e) => AddSkiaButton();
            toolAddLabel.Click += (s, e) => AddSkiaLabel();
            toolAddMenu.Click += (s, e) => AddSkiaMenu();
            toolSave.Click += (s, e) => SaveLayout();
            toolLoad.Click += (s, e) => LoadLayout();

            // Palette is now owned and added by SkiaHostControl. You can access it via skiaHostControl1.Palette if needed.
        }

        private void AddSkiaButton()
        {
            var type = typeof(Beep.Skia.Components.Button);
            var name = "btn" + DateTime.Now.Ticks.ToString("x");
            Console.WriteLine($"[TEST] About to create Button at X=100, Y=150");
            var created = skiaHostControl1.CreateAndAddComponent(type, 100, 150, 120, 36, name);
            Console.WriteLine($"[TEST] Button created: X={created?.X}, Y={created?.Y}");
        }

        private void AddSkiaLabel()
        {
            var type = typeof(Beep.Skia.Components.Label);
            Console.WriteLine($"[TEST] About to create Label at X=200, Y=250");
            var comp = skiaHostControl1.CreateAndAddComponent(type, 200, 250, 140, 32, "lbl" + DateTime.Now.Ticks.ToString("x"));
            Console.WriteLine($"[TEST] Label created: X={comp?.X}, Y={comp?.Y}");
            // Apply a few properties if the component supports them
            try { if (comp is Beep.Skia.Components.Label l) l.Text = "Hello Skia"; } catch { }
        }

        private void AddSkiaMenu()
        {
            var type = typeof(Beep.Skia.Components.Menu);
            skiaHostControl1.CreateAndAddComponent(type, 10, 10, 300, 40, "menu" + DateTime.Now.Ticks.ToString("x"));
        }

    // ListBox-based toolbox removed; palette component now provides in-Skia palette.

        private void SaveLayout()
        {
            try
            {
                var comps = skiaHostControl1.DrawingManager.GetComponents();
                var list = new System.Collections.Generic.List<object>();
                foreach (var c in comps)
                {
                    // Build a small property bag for common properties
                    var bag = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    try { var p = c.GetType().GetProperty("Text"); if (p != null) { var v = p.GetValue(c) as string; if (!string.IsNullOrEmpty(v)) bag["Text"] = v; } } catch { }
                    try { var p = c.GetType().GetProperty("FillColor") ?? c.GetType().GetProperty("BackgroundColor") ?? c.GetType().GetProperty("Color"); if (p != null) { var v = p.GetValue(c); if (v != null) bag["Color"] = v.ToString(); } } catch { }

                    list.Add(new
                    {
                        Type = c.GetType().AssemblyQualifiedName,
                        X = c.X,
                        Y = c.Y,
                        Width = c.Width,
                        Height = c.Height,
                        Name = c.Name,
                        PropertyBag = bag
                    });
                }

                var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText("skia_layout.json", json);
                MessageBox.Show("Saved layout to skia_layout.json");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message);
            }
        }

        private void LoadLayout()
        {
            try
            {
                if (!File.Exists("skia_layout.json")) { MessageBox.Show("No layout file found"); return; }
                var json = File.ReadAllText("skia_layout.json");
                var arr = JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(json);
                if (arr == null) return;

                // Clear the component manager before loading
                // Clear existing components from the drawing manager
                foreach (var c in new System.Collections.Generic.List<Beep.Skia.SkiaComponent>(skiaHostControl1.DrawingManager.GetComponents()))
                {
                    try { skiaHostControl1.DrawingManager.RemoveComponent(c); } catch { }
                }

                foreach (var el in arr)
                {
                    try
                    {
                        var desc = new Beep.Skia.Winform.Controls.SkiaComponentDescriptor();
                        desc.ComponentType = el.GetProperty("Type").GetString() ?? string.Empty;
                        desc.X = (float)el.GetProperty("X").GetDouble();
                        desc.Y = (float)el.GetProperty("Y").GetDouble();
                        desc.Width = (float)el.GetProperty("Width").GetDouble();
                        desc.Height = (float)el.GetProperty("Height").GetDouble();
                        desc.Name = el.GetProperty("Name").GetString() ?? string.Empty;

                        // Load property bag
                        try
                        {
                            if (el.TryGetProperty("PropertyBag", out var pb))
                            {
                                foreach (var kvp in pb.EnumerateObject())
                                {
                                    try { var sval = kvp.Value.GetString(); if (sval != null) desc.PropertyBag[kvp.Name] = sval; } catch { }
                                }
                            }
                        }
                        catch { }

                        // Let the host create and apply property bag
                        skiaHostControl1.CreateAndAddComponentFromDescriptor(desc);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Load failed: " + ex.Message);
            }
        }
    }
}

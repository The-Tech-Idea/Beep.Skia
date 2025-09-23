using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Beep.Skia.Components;
using Beep.Skia.Model;
using Beep.Skia.Serialization;
namespace Beep.Skia.Sample.WinForms
{
    public partial class MainForm : Form
    {
        // Simple mapping from toolbox display names to Skia component CLR types
        private readonly System.Collections.Generic.Dictionary<string, string> _toolboxMap = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["Button"] = typeof(Beep.Skia.Components.Button).AssemblyQualifiedName!,
            ["Label"] = typeof(Beep.Skia.Components.Label).AssemblyQualifiedName!,
            ["Menu"] = typeof(Beep.Skia.Components.Menu).AssemblyQualifiedName!,
            ["Card"] = typeof(Beep.Skia.Components.Card).AssemblyQualifiedName!,
            ["Checkbox"] = typeof(Beep.Skia.Components.Checkbox).AssemblyQualifiedName!,
            ["TextBox"] = typeof(Beep.Skia.Components.TextBox).AssemblyQualifiedName!,
            ["Panel"] = typeof(Beep.Skia.Components.Panel).AssemblyQualifiedName!,
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
                // Use the built-in DTO exporter to capture components and connection lines
                DiagramDto dto = skiaHostControl1.DrawingManager.ToDto();

                // Optionally enrich property bag for common component properties
                // Not strictly required for restoring connections, but useful for demos
                foreach (var c in skiaHostControl1.DrawingManager.GetComponents())
                {
                    try
                    {
                        var match = dto.Components.Find(x => x.Name == c.Name && x.Type == c.GetType().AssemblyQualifiedName);
                        if (match == null) continue;
                        var bag = match.PropertyBag;
                        var pText = c.GetType().GetProperty("Text");
                        if (pText != null)
                        {
                            var v = pText.GetValue(c) as string;
                            if (!string.IsNullOrEmpty(v)) bag["Text"] = v;
                        }
                        var pColor = c.GetType().GetProperty("FillColor") ?? c.GetType().GetProperty("BackgroundColor") ?? c.GetType().GetProperty("Color");
                        if (pColor != null)
                        {
                            var v = pColor.GetValue(c);
                            if (v != null) bag["Color"] = v.ToString();
                        }
                    }
                    catch { }
                }

                var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
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
                var dto = JsonSerializer.Deserialize<DiagramDto>(json);
                if (dto == null) { MessageBox.Show("Invalid or empty layout file"); return; }

                // Use the built-in loader to clear and restore components and lines
                skiaHostControl1.DrawingManager.LoadFromDto(dto);

                // Optionally reapply saved property bags
                foreach (var compDto in dto.Components)
                {
                    try
                    {
                        var comp = skiaHostControl1.DrawingManager.GetComponents()
                            .FirstOrDefault(cc => cc.Name == compDto.Name && cc.GetType().AssemblyQualifiedName == compDto.Type);
                        if (comp == null) continue;
                        if (compDto.PropertyBag.TryGetValue("Text", out var text))
                        {
                            var pText = comp.GetType().GetProperty("Text");
                            if (pText != null) pText.SetValue(comp, text);
                        }
                        if (compDto.PropertyBag.TryGetValue("Color", out var colorStr))
                        {
                            var pColor = comp.GetType().GetProperty("FillColor") ?? comp.GetType().GetProperty("BackgroundColor") ?? comp.GetType().GetProperty("Color");
                            if (pColor != null)
                            {
                                // Best-effort parse of color string (e.g., SKColor text). If parsing fails, ignore.
                                try { pColor.SetValue(comp, colorStr); } catch { }
                            }
                        }
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

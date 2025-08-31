using System;
using System.ComponentModel;
using System.Windows.Forms;
using SkiaSharp.Views.Desktop;
using SkiaSharp;
using Beep.Skia.Components;

namespace Beep.Skia.Winform.Controls
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    [Description("Host control that provides a Skia drawing surface and a DrawingManager to host Skia components.")]
    [DisplayName("Skia Host")]
    public class SkiaHostControl : UserControl, ISupportInitialize
    {
        private SKControl _skControl;
        private Beep.Skia.DrawingManager _drawingManager;
    // private Beep.Skia.ComponentManager _componentManager;
    private SkiaComponentDescriptorCollection _designTimeComponents = new SkiaComponentDescriptorCollection();
    private Palette _palette;

        public Beep.Skia.DrawingManager DrawingManager => _drawingManager;
        // Runtime manager for components (preferred for rendering and input)
        [Browsable(false)]
    // public Beep.Skia.ComponentManager ComponentManager => _componentManager;

    private bool _designDescriptorsInstantiated = false;

    public SkiaHostControl()
        {
            InitializeSkiaSurface();
            AllowDrop = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            _drawingManager = new Beep.Skia.DrawingManager();
            _drawingManager.DrawSurface += (s, e) => _skControl?.Invalidate();
            // ComponentManager removed: runtime now uses DrawingManager exclusively.
            // Create the in-Skia palette and add it to the drawing manager so it's present by default.
            try
            {
                _palette = new Palette();
                // Palette constructor sets reasonable defaults (X,Y,Width,Height)
                // Ensure palette is placed near the top-left of the canvas
                _palette.X = 8;
                _palette.Y = 40;
                _drawingManager?.AddComponent(_palette);
            }
            catch { }
            _skControl.PaintSurface += SkControl_PaintSurface;
            this.DragEnter += SkiaHostControl_DragEnter;
            this.DragDrop += SkiaHostControl_DragDrop;
        }

        public void BeginInit()
        {
            // no-op
        }

    public void EndInit()
        {
            // At runtime, after designer serialization, instantiate any descriptors into real components
            if (_designDescriptorsInstantiated) return;
            try
            {
                if (this.Site == null || this.Site.DesignMode == false)
                {
                    foreach (var desc in DesignTimeComponents)
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(desc.ComponentType)) continue;
                            var t = Type.GetType(desc.ComponentType);
                            if (t == null) continue;
                            if (!typeof(Beep.Skia.SkiaComponent).IsAssignableFrom(t)) continue;

                            var obj = Activator.CreateInstance(t) as Beep.Skia.SkiaComponent;
                            if (obj == null) continue;

                            // Set basic layout properties if available
                            try { obj.X = desc.X; } catch { }
                            try { obj.Y = desc.Y; } catch { }
                            try { obj.Width = desc.Width; } catch { }
                            try { obj.Height = desc.Height; } catch { }
                            // Apply any property bag values
                            try { ApplyPropertyBagToObject(obj, desc.PropertyBag); } catch { }
                            // Ensure component has a unique name when descriptor didn't include one
                            var nameToUse = desc.Name;
                            if (string.IsNullOrEmpty(nameToUse))
                            {
                                try
                                {
                                    nameToUse = GenerateUniqueNameFor(t.Name);
                                    desc.Name = nameToUse; // update descriptor so subsequent operations see the name
                                }
                                catch { nameToUse = string.Empty; }
                            }
                            try { obj.Name = nameToUse ?? string.Empty; } catch { }
                            try
                            {
                                var msg = $"Instantiating descriptor: Type={t.FullName} Name={nameToUse} RequestedX={desc.X},RequestedY={desc.Y},W={desc.Width},H={desc.Height}";
                                Console.WriteLine(msg);
                                try { var lp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log"); System.IO.File.AppendAllText(lp, msg + Environment.NewLine); } catch { }
                            }
                            catch { }

                                _drawingManager?.AddComponent(obj);
                            try
                            {
                                var msg = $"Descriptor instantiated: Type={t.FullName} Name={obj.Name} FinalX={obj.X},FinalY={obj.Y},W={obj.Width},H={obj.Height}";
                                Console.WriteLine(msg);
                                try { var lp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log"); System.IO.File.AppendAllText(lp, msg + Environment.NewLine); } catch { }
                            }
                            catch { }
                        }
                        catch
                        {
                            // ignore failures for individual descriptors
                        }
                    }

                    _skControl?.Invalidate();
                }
            }
            catch
            {
                // swallow overall errors during initialization
            }
            finally
            {
                _designDescriptorsInstantiated = true;
            }
        }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Description("Components hosted by this Skia host at design time.")]
        public SkiaComponentDescriptorCollection DesignTimeComponents
        {
            get => _designTimeComponents;
            set => _designTimeComponents = value ?? new SkiaComponentDescriptorCollection();
        }

    /// <summary>
    /// The in-host Palette component (if created).
    /// </summary>
    [Browsable(false)]
    public Palette Palette => _palette;

        // Helper used by the generated InitializeComponent code to construct and add
        // a Skia component directly (this is what the custom serializer emits).
        [Browsable(false)]
        public Beep.Skia.SkiaComponent CreateAndAddComponent(Type componentType, float x, float y, float width, float height, string name)
        {
            if (componentType == null) return null;
            try
            {
                if (!typeof(Beep.Skia.SkiaComponent).IsAssignableFrom(componentType)) return null;
                var obj = Activator.CreateInstance(componentType) as Beep.Skia.SkiaComponent;
                if (obj == null) return null;

                try { var msg = $"CreateAndAddComponent requested: Type={componentType.FullName} ReqX={x},ReqY={y},W={width},H={height},Name={name}"; Console.WriteLine(msg); try { var lp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log"); System.IO.File.AppendAllText(lp, msg + Environment.NewLine); } catch { } } catch { }

                // Assign properties defensively
                try { obj.X = x; } catch { }
                try { obj.Y = y; } catch { }
                try { obj.Width = width; } catch { }
                try { obj.Height = height; } catch { }
                // If the caller passed a descriptor name that may encode additional properties, nothing else here.

                // Ensure unique name if none provided
                if (string.IsNullOrEmpty(name))
                {
                    name = GenerateUniqueNameFor(descriptorBaseName: componentType.Name);
                }
                try { obj.Name = name ?? string.Empty; } catch { }

                // No descriptor property bag passed here; caller can retrieve the returned object and set properties.

                // Check for overlap with existing components: don't add if any existing component intersects the desired bounds
                try
                {
                    var mgr = this.DrawingManager;
                    var desiredRect = new SKRect(obj.X, obj.Y, obj.X + obj.Width, obj.Y + obj.Height);
                    bool intersects = false;

                    // Check live components from the host's ComponentManager
                    if (mgr != null)
                    {
                        foreach (var ecomp in mgr.GetComponents())
                        {
                            try
                            {
                                var er = new SKRect(ecomp.X, ecomp.Y, ecomp.X + ecomp.Width, ecomp.Y + ecomp.Height);
                                if (er.IntersectsWith(desiredRect)) { intersects = true; break; }
                            }
                            catch { }
                        }
                    }

                    // Also consider design-time descriptors that may occupy the same area
                    try
                    {
                        foreach (var d in DesignTimeComponents)
                        {
                            try
                            {
                                var dr = new SKRect(d.X, d.Y, d.X + d.Width, d.Y + d.Height);
                                if (dr.IntersectsWith(desiredRect)) { intersects = true; break; }
                            }
                            catch { }
                        }
                    }
                    catch { }

                    if (intersects)
                    {
                        try { Console.WriteLine($"Create blocked: target area X={obj.X},Y={obj.Y},W={obj.Width},H={obj.Height} intersects existing component or design-time descriptor"); } catch { }
                        return null;
                    }
                }
                catch { }

                    _drawingManager?.AddComponent(obj);
                try { var msg = $"CreateAndAddComponent created: Type={componentType.FullName} Name={obj.Name} FinalX={obj.X},FinalY={obj.Y},W={obj.Width},H={obj.Height}"; Console.WriteLine(msg); try { var lp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log"); System.IO.File.AppendAllText(lp, msg + Environment.NewLine); } catch { } } catch { }
                _skControl?.Invalidate();
                return obj;
            }
            catch
            {
                return null;
            }
        }

        // Public helper to create and add a component from a descriptor, applying its PropertyBag.
        public Beep.Skia.SkiaComponent CreateAndAddComponentFromDescriptor(SkiaComponentDescriptor desc)
        {
            if (desc == null) return null;
            try
            {
                try { var msg = $"CreateAndAddComponentFromDescriptor requested: Type={desc.ComponentType} ReqX={desc.X},ReqY={desc.Y},W={desc.Width},H={desc.Height},Name={desc.Name}"; Console.WriteLine(msg); try { var lp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log"); System.IO.File.AppendAllText(lp, msg + Environment.NewLine); } catch { } } catch { }
                if (string.IsNullOrEmpty(desc.ComponentType)) return null;
                var t = Type.GetType(desc.ComponentType);
                if (t == null) return null;
                if (!typeof(Beep.Skia.SkiaComponent).IsAssignableFrom(t)) return null;

                var obj = Activator.CreateInstance(t) as Beep.Skia.SkiaComponent;
                if (obj == null) return null;

                try { Console.WriteLine($"Descriptor requested X={desc.X}, Y={desc.Y}, W={desc.Width}, H={desc.Height}"); } catch { }
                try { obj.X = desc.X; } catch { }
                try { obj.Y = desc.Y; } catch { }
                try { obj.Width = desc.Width; } catch { }
                try { obj.Height = desc.Height; } catch { }
                try { var msg = $"After apply: component X={obj.X}, Y={obj.Y}, W={obj.Width}, H={obj.Height}"; Console.WriteLine(msg); try { var lp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log"); System.IO.File.AppendAllText(lp, msg + Environment.NewLine); } catch { } } catch { }

                var nameToUse = desc.Name;
                if (string.IsNullOrEmpty(nameToUse))
                {
                    nameToUse = GenerateUniqueNameFor(t.Name);
                    desc.Name = nameToUse;
                }
                try { obj.Name = nameToUse ?? string.Empty; } catch { }

                try { ApplyPropertyBagToObject(obj, desc.PropertyBag); } catch { }

                _drawingManager?.AddComponent(obj);
                try { var msg = $"CreateAndAddComponentFromDescriptor created: Type={t.FullName} Name={obj.Name} FinalX={obj.X},FinalY={obj.Y},W={obj.Width},H={obj.Height}"; Console.WriteLine(msg); try { var lp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log"); System.IO.File.AppendAllText(lp, msg + Environment.NewLine); } catch { } } catch { }
                _skControl?.Invalidate();
                return obj;
            }
            catch { return null; }
        }

        // Generate a simple unique name using existing design-time component names and the host's collection
        private string GenerateUniqueNameFor(string descriptorBaseName)
        {
            if (string.IsNullOrEmpty(descriptorBaseName)) descriptorBaseName = "SkiaComponent";
            var baseName = "skia" + descriptorBaseName;
            int i = 1;

            bool Exists(string n)
            {
                foreach (var d in DesignTimeComponents)
                {
                    if (string.Equals(d.Name, n, StringComparison.OrdinalIgnoreCase)) return true;
                }
                // also check container components if available (design time)
                try
                {
                    var host = this.Site?.GetService(typeof(System.ComponentModel.Design.IDesignerHost)) as System.ComponentModel.Design.IDesignerHost;
                    if (host != null)
                    {
                        foreach (System.ComponentModel.IComponent c in host.Container.Components)
                        {
                            var nprop = c.Site?.Name;
                            if (!string.IsNullOrEmpty(nprop) && string.Equals(nprop, n, StringComparison.OrdinalIgnoreCase)) return true;
                        }
                    }
                }
                catch { }

                return false;
            }

            var name = baseName + i.ToString();
            while (Exists(name))
            {
                i++;
                name = baseName + i.ToString();
            }
            return name;
        }

        // Apply a string-based property bag to an object's public writable properties.
        private void ApplyPropertyBagToObject(object obj, System.Collections.Generic.Dictionary<string, string> bag)
        {
            if (obj == null || bag == null || bag.Count == 0) return;

            var type = obj.GetType();
            foreach (var kv in bag)
            {
                try
                {
                    var prop = type.GetProperty(kv.Key);
                    if (prop == null || !prop.CanWrite) continue;
                    var targetType = prop.PropertyType;
                    if (TryConvert(kv.Value, targetType, out var converted))
                    {
                        prop.SetValue(obj, converted);
                    }
                }
                catch { }
            }
        }

        // Limited conversion from string to common types (string, float, int, bool, SKColor)
    private bool TryConvert(string s, Type targetType, out object result)
        {
            result = null;
            if (targetType == typeof(string)) { result = s; return true; }
            if (targetType == typeof(float) || targetType == typeof(float?))
            {
                if (float.TryParse(s, out var f)) { result = f; return true; }
                return false;
            }
            if (targetType == typeof(int) || targetType == typeof(int?))
            {
                if (int.TryParse(s, out var i)) { result = i; return true; }
                return false;
            }
            if (targetType == typeof(bool) || targetType == typeof(bool?))
            {
                if (bool.TryParse(s, out var b)) { result = b; return true; }
                return false;
            }
            // SKColor from hex strings like #RRGGBB or #AARRGGBB
            if (targetType == typeof(SKColor) || targetType == typeof(SKColor?))
            {
                try
                {
                    var str = s?.Trim();
                    if (string.IsNullOrEmpty(str)) return false;
                    if (str.StartsWith("#")) str = str.Substring(1);
                    uint v = Convert.ToUInt32(str, 16);
                    if (str.Length == 6)
                    {
                        // assume RRGGBB
                        v |= 0xFF000000u;
                    }
                    result = new SKColor((byte)((v >> 16) & 0xFF), (byte)((v >> 8) & 0xFF), (byte)(v & 0xFF), (byte)((v >> 24) & 0xFF));
                    return true;
                }
                catch { return false; }
            }

            return false;
        }

        private void InitializeSkiaSurface()
        {
            _skControl = new SKControl { Dock = DockStyle.Fill };
            _skControl.MouseDown += SkControl_MouseDown;
            _skControl.MouseMove += SkControl_MouseMove;
            _skControl.MouseUp += SkControl_MouseUp;
            _skControl.MouseWheel += SkControl_MouseWheel;
            this.Controls.Add(_skControl);
        }

        private static SKKeyModifiers GetModifiers()
        {
            SKKeyModifiers m = SKKeyModifiers.None;
            if (Control.ModifierKeys.HasFlag(Keys.Control)) m |= SKKeyModifiers.Control;
            if (Control.ModifierKeys.HasFlag(Keys.Shift)) m |= SKKeyModifiers.Shift;
            if (Control.ModifierKeys.HasFlag(Keys.Alt)) m |= SKKeyModifiers.Alt;
            return m;
        }

        private void SkControl_MouseDown(object sender, MouseEventArgs e)
        {
            var pt = new SKPoint(e.X, e.Y);
            // Route all input to the DrawingManager
            _drawingManager.HandleMouseDown(pt, GetModifiers());
        }

        private void SkControl_MouseMove(object sender, MouseEventArgs e)
        {
            var pt = new SKPoint(e.X, e.Y);
            _drawingManager.HandleMouseMove(pt, GetModifiers());
        }

        private void SkControl_MouseUp(object sender, MouseEventArgs e)
        {
            var pt = new SKPoint(e.X, e.Y);
            _drawingManager.HandleMouseUp(pt, GetModifiers());
        }

        private void SkControl_MouseWheel(object sender, MouseEventArgs e)
        {
            // Windows Forms mouse wheel Delta is in e.Delta
            // ComponentManager currently doesn't expose a wheel handler; fall back to drawing manager
            _drawingManager.HandleMouseWheel(new SKPoint(e.X, e.Y), e.Delta);
        }

        private void SkControl_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            // Clear the canvas and let the DrawingManager render
            canvas.Clear(SKColors.White);
            _drawingManager.Canvas = canvas;
            try
            {
                _drawingManager.Draw(canvas);
            }
            catch { }
        }

        private void SkiaHostControl_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Control)))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void SkiaHostControl_DragDrop(object sender, DragEventArgs e)
        {
            // Convert screen coordinates to the inner SKControl's client coordinates
            var clientPoint = (_skControl != null)
                ? _skControl.PointToClient(new System.Drawing.Point(e.X, e.Y))
                : this.PointToClient(new System.Drawing.Point(e.X, e.Y));
            var skPoint = new SKPoint(clientPoint.X, clientPoint.Y);

            if (e.Data.GetDataPresent(typeof(Control)))
            {
                var c = e.Data.GetData(typeof(Control)) as Control;
                // If it's a Skia wrapper, create the underlying Skia component and add it to the manager
                if (c is SkiaControl sc && sc.SkiaComponent != null)
                {
                    // Build a descriptor and delegate creation to the host's descriptor-based factory
                    // Convert the drop point (client pixels) into canvas coordinates considering pan/zoom
                    var canvasPoint = new SKPoint(skPoint.X, skPoint.Y);
                    try
                    {
                        var mgr = this.DrawingManager;
                        if (mgr != null)
                        {
                            var offset = new SKPoint(canvasPoint.X - mgr.PanOffset.X, canvasPoint.Y - mgr.PanOffset.Y);
                            canvasPoint = new SKPoint(offset.X / mgr.Zoom, offset.Y / mgr.Zoom);
                        }
                    }
                    catch { }

                    // Use the prototype to read default size and name, but create via descriptor to ensure a fresh instance
                    var proto = sc.SkiaComponent;
                    float w = proto?.Width ?? 120f;
                    float h = proto?.Height ?? 36f;
                    string typeName = proto?.GetType().AssemblyQualifiedName;
                    string name = proto?.Name ?? string.Empty;

                    // Center the component under the cursor
                    var newX = canvasPoint.X - (w / 2f);
                    var newY = canvasPoint.Y - (h / 2f);
                    newX = Math.Max(0f, newX);
                    newY = Math.Max(0f, newY);

                    var desc = new SkiaComponentDescriptor
                    {
                        ComponentType = typeName,
                        X = newX,
                        Y = newY,
                        Width = w,
                        Height = h,
                        Name = name
                    };

                    // Use the unified creation path which applies property bags and logging and performs overlap checks
                    var created = CreateAndAddComponentFromDescriptor(desc);
                    if (created == null)
                    {
                        try { Console.WriteLine($"DragDrop: creation blocked or failed for type {typeName} at X={newX},Y={newY}"); } catch { }
                    }
                    _skControl.Invalidate();
                }
            }
        }
    }
}

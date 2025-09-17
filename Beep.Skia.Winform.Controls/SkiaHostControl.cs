using System;
using System.ComponentModel;
using System.Windows.Forms;
using SkiaSharp.Views.Desktop;
using SkiaSharp;
using Beep.Skia.Components;
using System.Linq;

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
    // Runtime registry of created components keyed by Guid Id for quick lookup
    private readonly Dictionary<Guid, Beep.Skia.SkiaComponent> _componentRegistry = new Dictionary<Guid, Beep.Skia.SkiaComponent>();
    [Browsable(false)]
    public IReadOnlyDictionary<Guid, Beep.Skia.SkiaComponent> ComponentRegistry => _componentRegistry;
    /// <summary>
    /// If true, new components dropped (via wrapper drag/drop) will be centered under the cursor.
    /// If false (default), the cursor position becomes the component's top-left corner.
    /// </summary>
    [DefaultValue(false)]
    [Category("Behavior")]
    [Description("Center the component on the drop point instead of using it as the top-left.")]
    public bool CenterOnDrop { get; set; } = false;

    // Enable/disable runtime dragging of existing Skia components
    [Category("Behavior"), DefaultValue(true)]
    [Description("Allow picking up and moving existing components with the mouse.")]
    public bool AllowComponentDragging { get; set; } = true;

    // Drag state
    private Beep.Skia.SkiaComponent _dragComponent = null; // component currently being dragged
    private bool _isDraggingComponent = false;
    private SKPoint _dragStartCanvas; // mouse position (canvas coords) at drag start
    private float _dragComponentStartX;
    private float _dragComponentStartY;

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
                _palette = new Palette
                {
                    X = 8,
                    Y = 40
                };
                // Constrain palette height to the host so it scrolls instead of growing endlessly
                try { _palette.MaxHeight = Math.Max(120, this.Height - 80); } catch { }
                try
                {
                    this.SizeChanged += (s2, e2) =>
                    {
                        try
                        {
                            if (_palette != null)
                            {
                                _palette.MaxHeight = Math.Max(120, this.Height - 80);
                                _palette.RefreshLayout();
                                _skControl?.Invalidate();
                            }
                        }
                        catch { }
                    };
                }
                catch { }

                // Populate palette using registry discovery; categorize UML vs Components
                try
                {
                    // Discover components across loaded and base-directory assemblies
                    var discovered = Beep.Skia.SkiaComponentRegistry.DiscoverAndRegisterDomainComponents();
                    foreach (var def in Beep.Skia.SkiaComponentRegistry.GetComponentsOrdered())
                    {
                        try
                        {
                            var display = def.className ?? def.type?.Name ?? def.dllname ?? def.AssemblyName ?? "Unknown";
                            var compType = def.type != null ? def.type.AssemblyQualifiedName : (def.className ?? def.dllname ?? string.Empty);
                            string category = "Components";
                            var ns = def.type?.Namespace ?? string.Empty;
                            if (!string.IsNullOrEmpty(ns))
                            {
                                if (ns.Contains(".UML", StringComparison.OrdinalIgnoreCase))
                                    category = "UML";
                                else if (ns.Contains(".Network", StringComparison.OrdinalIgnoreCase))
                                    category = "Network";
                                else if (ns.Contains(".ETL", StringComparison.OrdinalIgnoreCase))
                                    category = "ETL";
                                else if (ns.Contains(".Business", StringComparison.OrdinalIgnoreCase))
                                    category = "Business";
                            }
                            if (!string.IsNullOrWhiteSpace(compType))
                            {
                                _palette.Items.Add(new PaletteItem { Name = display, ComponentType = compType, Category = category });
                            }
                        }
                        catch { }
                    }
                }
                catch { }

                // Wire palette events so drops and clicks actually instantiate components
                _palette.ItemDropped += (s, tup) =>
                {
                    try
                    {
                        var item = tup.Item;
                        var pt = tup.DropPoint;
                        var desc = new SkiaComponentDescriptor
                        {
                            ComponentType = item.ComponentType,
                            X = pt.X,
                            Y = pt.Y,
                            Width = 120,
                            Height = 36,
                            Name = "skia" + DateTime.UtcNow.Ticks.ToString("x")
                        };
                        CreateAndAddComponentFromDescriptor(desc);
                    }
                    catch { }
                };
                _palette.ItemActivated += (s, item) =>
                {
                    try
                    {
                        var desc = new SkiaComponentDescriptor
                        {
                            ComponentType = item.ComponentType,
                            X = _palette.X + _palette.Width + 20,
                            Y = _palette.Y + 20,
                            Width = 120,
                            Height = 36,
                            Name = "skia" + DateTime.UtcNow.Ticks.ToString("x")
                        };
                        CreateAndAddComponentFromDescriptor(desc);
                    }
                    catch { }
                };
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

        /// <summary>
        /// If true, component creation will be blocked when the new component's bounds overlap an existing component.
        /// Default false: overlaps allowed (prevents silent disappearance when dropping in same area).
        /// </summary>
        [Category("Behavior"), DefaultValue(false)]
        public bool BlockOverlappingDrops { get; set; } = false;
    /// <summary>
    /// If true (default), each drop operation will force generation of a unique component Name even if a name was supplied.
    /// Prevents confusion where repeated drops with the same descriptor name appear to replace earlier components.
    /// </summary>
    [Category("Behavior"), DefaultValue(true)]
    public bool AlwaysUniqueNamesOnDrop { get; set; } = true;
    /// <summary>
    /// If true (default), when a new component is created and overlaps an existing one (and overlaps are allowed),
    /// the new component will be auto-offset (diagonally) until it no longer overlaps, up to a max number of attempts.
    /// This prevents the visual illusion that the previous component "disappeared" when it was only covered.
    /// </summary>
    [Category("Behavior"), DefaultValue(true)]
    public bool AutoOffsetOnOverlap { get; set; } = true;

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

                // Reuse detection: if an existing component already has this Id (should not happen unless factory returns singleton)
                try
                {
                    var mgrForReuse = this.DrawingManager;
                    if (mgrForReuse != null && mgrForReuse.GetComponents().Any(c => c.Id == obj.Id))
                    {
                        var msgReuse = $"[CreateAndAddComponent] Detected reused instance for type {componentType.FullName}. Cloning new instance.";
                        Console.WriteLine(msgReuse);
                        try { var lp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log"); System.IO.File.AppendAllText(lp, msgReuse + Environment.NewLine); } catch { }
                        obj = Activator.CreateInstance(componentType) as Beep.Skia.SkiaComponent; // try again fresh
                    }
                }
                catch { }

                try { var msg = $"CreateAndAddComponent requested: Type={componentType.FullName} ReqX={x},ReqY={y},W={width},H={height},Name={name}"; Console.WriteLine(msg); try { var lp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log"); System.IO.File.AppendAllText(lp, msg + Environment.NewLine); } catch { } } catch { }

                // Log initial coordinates right after creation
                try { var msg2 = $"CreateAndAddComponent AfterCreation: Type={componentType.FullName} X={obj.X},Y={obj.Y},W={obj.Width},H={obj.Height}"; Console.WriteLine(msg2); try { var lp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log"); System.IO.File.AppendAllText(lp, msg2 + Environment.NewLine); } catch { } } catch { }

                // Assign properties defensively
                try { obj.X = x; } catch { }
                try { obj.Y = y; } catch { }
                try { obj.Width = width; } catch { }
                try { obj.Height = height; } catch { }

                // Log coordinates after assignment
                try { var msg3 = $"CreateAndAddComponent AfterAssignment: Type={componentType.FullName} X={obj.X},Y={obj.Y},W={obj.Width},H={obj.Height}"; Console.WriteLine(msg3); try { var lp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log"); System.IO.File.AppendAllText(lp, msg3 + Environment.NewLine); } catch { } } catch { }
                // If the caller passed a descriptor name that may encode additional properties, nothing else here.

                // Ensure unique name (optional force) to avoid same-identity replacement illusions
                if (AlwaysUniqueNamesOnDrop || string.IsNullOrEmpty(name) || NameExists(name))
                {
                    name = GenerateUniqueNameFor(descriptorBaseName: componentType.Name);
                }
                try { obj.Name = name ?? string.Empty; } catch { }

                // No descriptor property bag passed here; caller can retrieve the returned object and set properties.

                // Optional overlap blocking or auto-offset
                try
                {
                    var mgr = this.DrawingManager;
                    if (mgr != null)
                    {
                        int attempts = 0;
                        const int maxAttempts = 25;
                        bool hasOverlap;
                        do
                        {
                            hasOverlap = false;
                            foreach (var ecomp in mgr.GetComponents())
                            {
                                try
                                {
                                    if (ReferenceEquals(ecomp, obj)) continue;
                                    var er = new SKRect(ecomp.X, ecomp.Y, ecomp.X + ecomp.Width, ecomp.Y + ecomp.Height);
                                    var desiredRect = new SKRect(obj.X, obj.Y, obj.X + obj.Width, obj.Y + obj.Height);
                                    if (er.IntersectsWith(desiredRect))
                                    {
                                        if (BlockOverlappingDrops)
                                        {
                                            try { Console.WriteLine($"[CreateAndAddComponent] BLOCKED overlap at X={obj.X},Y={obj.Y},W={obj.Width},H={obj.Height}"); } catch { }
                                            return null;
                                        }
                                        if (AutoOffsetOnOverlap)
                                        {
                                            obj.X += 20f;
                                            obj.Y += 20f;
                                            hasOverlap = true;
                                            attempts++;
                                            break;
                                        }
                                    }
                                }
                                catch { }
                            }
                        } while (hasOverlap && AutoOffsetOnOverlap && attempts < maxAttempts);
                    }
                }
                catch { }

                    _drawingManager?.AddComponent(obj);
                    try { _componentRegistry[obj.Id] = obj; } catch { }
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

                // Reuse detection: ensure a brand-new instance (guard against singleton-style implementations)
                try
                {
                    var mgrForReuse = this.DrawingManager;
                    if (mgrForReuse != null && mgrForReuse.GetComponents().Any(c => c.Id == obj.Id))
                    {
                        var msgReuse = $"[CreateAndAddComponentFromDescriptor] Detected reused instance for type {t.FullName}. Cloning new instance.";
                        Console.WriteLine(msgReuse);
                        try { var lp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log"); System.IO.File.AppendAllText(lp, msgReuse + Environment.NewLine); } catch { }
                        obj = Activator.CreateInstance(t) as Beep.Skia.SkiaComponent; // attempt second instantiation
                    }
                }
                catch { }

                try { Console.WriteLine($"Descriptor requested X={desc.X}, Y={desc.Y}, W={desc.Width}, H={desc.Height}"); } catch { }
                try { obj.X = desc.X; } catch { }
                try { obj.Y = desc.Y; } catch { }
                try { obj.Width = desc.Width; } catch { }
                try { obj.Height = desc.Height; } catch { }
                try { var msg = $"After apply: component X={obj.X}, Y={obj.Y}, W={obj.Width}, H={obj.Height}"; Console.WriteLine(msg); try { var lp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log"); System.IO.File.AppendAllText(lp, msg + Environment.NewLine); } catch { } } catch { }

                var nameToUse = desc.Name;
                if (AlwaysUniqueNamesOnDrop || string.IsNullOrEmpty(nameToUse) || NameExists(nameToUse))
                {
                    nameToUse = GenerateUniqueNameFor(t.Name);
                    desc.Name = nameToUse;
                }
                try { obj.Name = nameToUse ?? string.Empty; } catch { }

                try { ApplyPropertyBagToObject(obj, desc.PropertyBag); } catch { }

                try
                {
                    var mgr = this.DrawingManager;
                    if (mgr != null)
                    {
                        int attempts = 0;
                        const int maxAttempts = 25;
                        bool hasOverlap;
                        do
                        {
                            hasOverlap = false;
                            foreach (var ecomp in mgr.GetComponents())
                            {
                                try
                                {
                                    if (ReferenceEquals(ecomp, obj)) continue;
                                    var er = new SKRect(ecomp.X, ecomp.Y, ecomp.X + ecomp.Width, ecomp.Y + ecomp.Height);
                                    var desiredRect = new SKRect(obj.X, obj.Y, obj.X + obj.Width, obj.Y + obj.Height);
                                    if (er.IntersectsWith(desiredRect))
                                    {
                                        if (BlockOverlappingDrops)
                                        {
                                            try { Console.WriteLine($"[CreateAndAddComponentFromDescriptor] BLOCKED overlap at X={obj.X},Y={obj.Y}"); } catch { }
                                            return null;
                                        }
                                        if (AutoOffsetOnOverlap)
                                        {
                                            obj.X += 20f;
                                            obj.Y += 20f;
                                            hasOverlap = true;
                                            attempts++;
                                            break;
                                        }
                                    }
                                }
                                catch { }
                            }
                        } while (hasOverlap && AutoOffsetOnOverlap && attempts < maxAttempts);
                    }
                }
                catch { }
                _drawingManager?.AddComponent(obj);
                try { _componentRegistry[obj.Id] = obj; } catch { }
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

        private bool NameExists(string proposed)
        {
            if (string.IsNullOrEmpty(proposed)) return false;
            try
            {
                foreach (var d in DesignTimeComponents)
                {
                    if (string.Equals(d.Name, proposed, StringComparison.OrdinalIgnoreCase)) return true;
                }
                var mgr = this.DrawingManager;
                if (mgr != null)
                {
                    foreach (var c in mgr.GetComponents())
                    {
                        try
                        {
                            if (string.Equals(c.Name, proposed, StringComparison.OrdinalIgnoreCase)) return true;
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return false;
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

            if (!AllowComponentDragging || e.Button != MouseButtons.Left)
                return;

            try
            {
                var canvasPoint = ClientToCanvas(pt);
                // Find topmost component under cursor (reverse drawing order)
                var comps = _drawingManager?.GetComponents()?.ToList();
                if (comps != null)
                {
                    for (int i = comps.Count - 1; i >= 0; i--)
                    {
                        var c = comps[i];
                        if (c == null) continue;
                        var rect = new SKRect(c.X, c.Y, c.X + c.Width, c.Y + c.Height);
                        if (rect.Contains(canvasPoint))
                        {
                            _dragComponent = c;
                            // Respect IsStatic components: do not start drag
                            try { if (_dragComponent.IsStatic) { _dragComponent = null; break; } } catch { }
                            _isDraggingComponent = true;
                            _dragStartCanvas = canvasPoint;
                            _dragComponentStartX = c.X;
                            _dragComponentStartY = c.Y;
                            // Bring to front by re-adding at end (optional)
                            try
                            {
                                _drawingManager.RemoveComponent(c);
                                _drawingManager.AddComponent(c);
                            }
                            catch { }
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        private void SkControl_MouseMove(object sender, MouseEventArgs e)
        {
            var pt = new SKPoint(e.X, e.Y);
            _drawingManager.HandleMouseMove(pt, GetModifiers());

            if (!_isDraggingComponent || _dragComponent == null || !AllowComponentDragging)
                return;
            try
            {
                var canvasPoint = ClientToCanvas(pt);
                float dx = canvasPoint.X - _dragStartCanvas.X;
                float dy = canvasPoint.Y - _dragStartCanvas.Y;
                _dragComponent.X = _dragComponentStartX + dx;
                _dragComponent.Y = _dragComponentStartY + dy;
                // Keep within non-negative canvas for basic safety
                if (_dragComponent.X < 0) _dragComponent.X = 0;
                if (_dragComponent.Y < 0) _dragComponent.Y = 0;
                _skControl.Invalidate();
            }
            catch { }
        }

        private void SkControl_MouseUp(object sender, MouseEventArgs e)
        {
            var pt = new SKPoint(e.X, e.Y);
            _drawingManager.HandleMouseUp(pt, GetModifiers());

            if (e.Button == MouseButtons.Left && _isDraggingComponent)
            {
                _isDraggingComponent = false;
                _dragComponent = null;
            }
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

        // Convert mouse point in SKControl client coords to canvas coords accounting for pan & zoom
        private SKPoint ClientToCanvas(SKPoint clientPoint)
        {
            var mgr = _drawingManager;
            if (mgr == null) return clientPoint;
            try
            {
                var offset = new SKPoint(clientPoint.X - mgr.PanOffset.X, clientPoint.Y - mgr.PanOffset.Y);
                if (mgr.Zoom != 0f) return new SKPoint(offset.X / mgr.Zoom, offset.Y / mgr.Zoom);
                return offset;
            }
            catch { return clientPoint; }
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
            try
            {
                var msg0 = $"[DragDrop] RAW screen=({e.X},{e.Y}) client=({clientPoint.X},{clientPoint.Y})";
                System.Console.WriteLine(msg0);
                try { var lp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log"); System.IO.File.AppendAllText(lp, msg0 + System.Environment.NewLine); } catch { }
            }
            catch { }

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
                            try
                            {
                                var preMsg = $"[DragDrop] PreTransform canvasPoint=({canvasPoint.X},{canvasPoint.Y}) PanOffset={mgr.PanOffset} Zoom={mgr.Zoom}";
                                System.Console.WriteLine(preMsg);
                                try { var lp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log"); System.IO.File.AppendAllText(lp, preMsg + System.Environment.NewLine); } catch { }
                            }
                            catch { }
                            var offset = new SKPoint(canvasPoint.X - mgr.PanOffset.X, canvasPoint.Y - mgr.PanOffset.Y);
                            canvasPoint = new SKPoint(offset.X / mgr.Zoom, offset.Y / mgr.Zoom);
                            try
                            {
                                var postMsg = $"[DragDrop] PostTransform canvasPoint=({canvasPoint.X},{canvasPoint.Y})";
                                System.Console.WriteLine(postMsg);
                                try { var lp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log"); System.IO.File.AppendAllText(lp, postMsg + System.Environment.NewLine); } catch { }
                            }
                            catch { }
                        }
                    }
                    catch { }

                    // Use the prototype to read default size and name, but create via descriptor to ensure a fresh instance
                    var proto = sc.SkiaComponent;
                    float w = proto?.Width ?? 120f;
                    float h = proto?.Height ?? 36f;
                    string typeName = proto?.GetType().AssemblyQualifiedName;
                    string name = proto?.Name ?? string.Empty;

                    // Placement: either center on cursor or use cursor as top-left
                    float newX = CenterOnDrop ? canvasPoint.X - (w / 2f) : canvasPoint.X;
                    float newY = CenterOnDrop ? canvasPoint.Y - (h / 2f) : canvasPoint.Y;
                    newX = Math.Max(0f, newX);
                    newY = Math.Max(0f, newY);

                    try
                    {
                        var posMsg = $"[DragDrop] DescriptorPlacement Type={typeName} W={w} H={h} newX={newX} newY={newY} CenterOnDrop={CenterOnDrop}";
                        System.Console.WriteLine(posMsg);
                        try { var lp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log"); System.IO.File.AppendAllText(lp, posMsg + System.Environment.NewLine); } catch { }
                    }
                    catch { }

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
                    else
                    {
                        // Enforce final position in case any constructor/update logic reset it.
                        try
                        {
                            var before = $"[DragDrop] PostCreate BEFORE adjust: Type={created.GetType().Name} X={created.X} Y={created.Y}";
                            Console.WriteLine(before);
                            try { var lp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log"); System.IO.File.AppendAllText(lp, before + System.Environment.NewLine); } catch { }

                            created.X = newX;
                            created.Y = newY;

                            var after = $"[DragDrop] PostCreate AFTER adjust: Type={created.GetType().Name} ForcedX={created.X} ForcedY={created.Y}";
                            Console.WriteLine(after);
                            try { var lp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log"); System.IO.File.AppendAllText(lp, after + System.Environment.NewLine); } catch { }
                        }
                        catch { }
                    }
                    _skControl.Invalidate();
                }
            }
        }
    }
}

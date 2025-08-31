using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;

namespace Beep.Skia.Winform.Controls
{
    // Design-time behavior for SkiaControl wrappers.
    // When a SkiaControl is dropped while a SkiaHostControl is selected
    // in the designer, this designer will add the underlying SkiaComponent
    // to the host's DrawingManager and remove the WinForms wrapper from the
    // design surface to avoid leaving a WinForms placeholder.
    public class SkiaControlDesigner : ControlDesigner
    {
        public override void Initialize(IComponent component)
        {
            base.Initialize(component);

            try
            {
                var wrapper = component as SkiaControl;
                if (wrapper == null) return;

                var hostService = component.Site?.GetService(typeof(IDesignerHost)) as IDesignerHost;
                var selService = component.Site?.GetService(typeof(ISelectionService)) as ISelectionService;

                if (hostService == null || selService == null) return;

                var primary = selService.PrimarySelection as SkiaHostControl;
                if (primary == null) return;

                // If the wrapper has already created its SkiaComponent (most wrappers do in ctor), use it.
                var skComp = wrapper.SkiaComponent;
                if (skComp != null)
                {
                    // Add to the host drawing manager for immediate design-time preview.
                    primary.DrawingManager.AddComponent(skComp);

                    // Also add a descriptor to the host so it will be serialized into InitializeComponent
                    try
                    {
                        var props = TypeDescriptor.GetProperties(primary);
                        var p = props["DesignTimeComponents"];
                        if (p != null)
                        {
                            var collection = p.GetValue(primary) as SkiaComponentDescriptorCollection;
                            if (collection != null)
                            {
                                var desc = new SkiaComponentDescriptor
                                {
                                    ComponentType = skComp.GetType().AssemblyQualifiedName,
                                    X = skComp.X,
                                    Y = skComp.Y,
                                    Width = skComp.Width,
                                    Height = skComp.Height,
                                    Name = skComp.Name
                                };
                                collection.Add(desc);
                            }
                        }
                    }
                    catch
                    {
                        // ignore serialization-time errors
                    }
                }

                // Remove the WinForms wrapper control from the design surface so it doesn't become a child control.
                try
                {
                    hostService.DestroyComponent(component);
                }
                catch
                {
                    // ignore failures to destroy the component in some host scenarios
                }
            }
            catch
            {
                // swallow design-time exceptions
            }
        }
    }
}

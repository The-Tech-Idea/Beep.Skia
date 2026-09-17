using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Windows.Forms.Design.Behavior;

namespace Beep.Skia.Winform.Controls
{
    // Design-time behavior for SkiaControl wrappers.
    // When a SkiaControl is dropped while a SkiaHostControl is selected
    // in the designer, this designer will add the underlying SkiaComponent
    // to the host's DrawingManager and remove the WinForms wrapper from the
    // design surface to avoid leaving a WinForms placeholder.
    // The component is positioned at the current drop point (mouse position)
    // converted to canvas coordinates.
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
                    // Try to position at the current mouse cursor position (design-time drop point)
                    try
                    {
                        var behaviorService = component.Site?.GetService(typeof(BehaviorService)) as BehaviorService;
                        if (behaviorService != null)
                        {
                            // Get cursor position in screen coords, convert to host control coords
                            var screenPoint = Cursor.Position;
                            var clientPoint = primary.PointToClient(screenPoint);

                            // Convert to canvas coords (account for SKControl position within host)
                            if (primary.Controls.Count > 0 && primary.Controls[0] is Control skView)
                            {
                                int vx = clientPoint.X - skView.Left;
                                int vy = clientPoint.Y - skView.Top;
                                if (vx >= 0 && vy >= 0 && vx <= skView.Width && vy <= skView.Height)
                                {
                                    // Offset to center the component on the drop point
                                    skComp.X = Math.Max(0, vx - skComp.Width / 2);
                                    skComp.Y = Math.Max(0, vy - skComp.Height / 2);
                                }
                                else
                                {
                                    // Drop was outside the SKControl area — use default center
                                    skComp.X = Math.Max(20, (skView.Width - skComp.Width) / 2);
                                    skComp.Y = Math.Max(20, (skView.Height - skComp.Height) / 2);
                                }
                            }
                        }
                        else
                        {
                            // Fallback: position near center of host
                            skComp.X = Math.Max(20, (primary.Width - skComp.Width) / 2);
                            skComp.Y = Math.Max(20, (primary.Height - skComp.Height) / 2);
                        }
                    }
                    catch
                    {
                        // If positioning fails, leave at wrapper's default position
                    }

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

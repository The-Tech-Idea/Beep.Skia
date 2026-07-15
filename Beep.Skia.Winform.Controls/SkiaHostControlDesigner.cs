using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Beep.Skia.Winform.Controls
{
    /// <summary>
    /// Design-time designer for SkiaHostControl providing smart-tag verbs.
    /// </summary>
    public class SkiaHostControlDesigner : ControlDesigner
    {
        private DesignerActionListCollection _actionLists;

        public override DesignerActionListCollection ActionLists
        {
            get
            {
                if (_actionLists == null)
                {
                    _actionLists = new DesignerActionListCollection
                    {
                        new SkiaHostControlActionList(this)
                    };
                }
                return _actionLists;
            }
        }

        public SkiaHostControl HostControl => (SkiaHostControl)Component;
    }

    /// <summary>
    /// Smart-tag action list for SkiaHostControl providing design-time verbs.
    /// </summary>
    public class SkiaHostControlActionList : DesignerActionList
    {
        private readonly SkiaHostControlDesigner _designer;

        public SkiaHostControlActionList(SkiaHostControlDesigner designer)
            : base(designer.Component)
        {
            _designer = designer;
        }

        private SkiaHostControl Host => _designer.HostControl;

        public bool ShowGrid
        {
            get => Host.DrawingManager?.ShowGrid ?? true;
            set
            {
                if (Host.DrawingManager != null)
                    Host.DrawingManager.ShowGrid = value;
            }
        }

        public bool SnapToGrid
        {
            get => Host.DrawingManager?.SnapToGrid ?? true;
            set
            {
                if (Host.DrawingManager != null)
                    Host.DrawingManager.SnapToGrid = value;
            }
        }

        public void ClearAllComponents()
        {
            var host = Host;
            if (host?.DrawingManager == null) return;

            var result = System.Windows.Forms.MessageBox.Show(
                "Are you sure you want to remove all components from the diagram?",
                "Clear All Components",
                System.Windows.Forms.MessageBoxButtons.YesNo,
                System.Windows.Forms.MessageBoxIcon.Warning);

            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                host.DrawingManager.ClearComponents();
                host.DesignTimeComponents.Clear();

                // Notify designer of change
                var changeService = GetService(typeof(IComponentChangeService)) as IComponentChangeService;
                if (changeService != null)
                {
                    var prop = TypeDescriptor.GetProperties(Host)["DesignTimeComponents"];
                    changeService.OnComponentChanged(Host, prop, null, host.DesignTimeComponents);
                }
            }
        }

        public void ResetZoom()
        {
            var mgr = Host?.DrawingManager;
            if (mgr != null)
            {
                mgr.Zoom = 1.0f;
                mgr.PanOffset = new SkiaSharp.SKPoint(0, 0);
            }
        }

        public void ArrangeDiagram()
        {
            var mgr = Host?.DrawingManager;
            if (mgr != null)
            {
                mgr.ArrangeDiagram(new Beep.Skia.Layout.GridAutoLayout
                {
                    Columns = 3,
                    StartX = 50,
                    StartY = 50
                });
            }
        }

        public override DesignerActionItemCollection GetSortedActionItems()
        {
            var items = new DesignerActionItemCollection();

            items.Add(new DesignerActionHeaderItem("Diagram"));
            items.Add(new DesignerActionPropertyItem("ShowGrid", "Show Grid", "Diagram", "Toggle grid display"));
            items.Add(new DesignerActionPropertyItem("SnapToGrid", "Snap to Grid", "Diagram", "Snap components to grid"));

            items.Add(new DesignerActionHeaderItem("Actions"));
            items.Add(new DesignerActionMethodItem(this, nameof(ClearAllComponents), "Clear All Components", "Actions", "Remove all components from the diagram", true));
            items.Add(new DesignerActionMethodItem(this, nameof(ResetZoom), "Reset Zoom/Pan", "Actions", "Reset zoom to 100% and pan to origin", true));
            items.Add(new DesignerActionMethodItem(this, nameof(ArrangeDiagram), "Auto-Arrange (Grid)", "Actions", "Arrange components in a grid layout", true));

            return items;
        }
    }
}

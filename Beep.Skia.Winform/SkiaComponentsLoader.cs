using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Vis.Modules;

namespace Beep.Skia.Winform
{
    public class SkiaComponentsLoader
    {
        public IDMEEditor DMEEditor { get; set; }
        public List<AssemblyClassDefinition> Components { get; set; }=new List<AssemblyClassDefinition>();
        public SkiaComponentsLoader(IDMEEditor editor) 
        {
             DMEEditor = editor;
           
        }
        public void LoadComponents()
        {
            Components = new List<AssemblyClassDefinition>();
            foreach (AssemblyClassDefinition definition in DMEEditor.ConfigEditor.AppComponents.Where(p => p.componentType == "SkiaComponent" && p.classProperties.ObjectType != null && p.classProperties.Hidden == false).ToList()) //&& p.classProperties.ObjectType.Equals(libraryname, StringComparison.InvariantCultureIgnoreCase)
            {

                Components.Add(definition);
                //LibraryControl pc = new LibraryControl();
                ////  pc.AppField = new AppField();
                //pc.Name = definition.className;
                //pc.AppComponent = (IAppComponent)DMEEditor.assemblyHandler.CreateInstanceFromString(definition.dllname, definition.PackageName, null);
                //pc.AppComponent.ComponentName = definition.className;
                //pc.ClassDefinition = definition;
                //Button button = new Button();
                //button.Name = definition.classProperties.Caption;
                //button.Text = definition.classProperties.Caption;

                //button.Width = ControlsPanel.Width - 6;
                //button.Left = 3;
                //button.Height = 25;
                //button.Top = CurrentTopforPanel;
                //button.DragDrop += Control_DragDrop;
                //button.DragOver += Control_DragOver;
                //button.DragEnter += Control_DragEnter;
                //button.DragLeave += Control_DragLeave;
                //button.MouseDown += Control_MouseDown;
                //button.Tag = pc;
                //SetupIconForControl(button);
                //ControlsButtons.Add(button);
                //ControlsPanel.Controls.Add(button);
                //i++;
                //CurrentTopforPanel = 3 + (i * 25) + 2;

                //controlLibrary.Controls.Add(pc);
            }
        }  
        public SkiaComponent CreateAComponent(AssemblyClassDefinition definition)
        {
           
            return  (SkiaComponent)DMEEditor.assemblyHandler.CreateInstanceFromString(definition.dllname, definition.PackageName, null);
        }
}
}

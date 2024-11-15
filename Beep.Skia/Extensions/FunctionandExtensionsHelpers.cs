using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Vis.Modules;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Vis;



namespace Beep.Skia
{
    public class FunctionandExtensionsHelpers
    {
        public IDMEEditor DMEEditor { get; set; }
        public IPassedArgs Passedargs { get; set; }
        public IVisManager Vismanager { get; set; }
        public IControlManager Controlmanager { get; set; }
        public ITree TreeEditor { get; set; }

        CancellationTokenSource tokenSource;

        CancellationToken token;
        //public  IPythonRunTimeManager cpythonManager { get; set; }
        public IDataSource DataSource { get; set; }
        public IBranch pbr { get; set; }
        public IBranch RootBranch { get; set; }
        public IBranch ParentBranch { get; set; }
        public IBranch ViewRootBranch { get; set; }
        public FunctionandExtensionsHelpers(IDMEEditor pdMEEditor, IVisManager pvisManager, ITree ptreeControl)
        {
            DMEEditor = pdMEEditor;
            Vismanager = pvisManager;
            TreeEditor = ptreeControl;
            //cpythonManager = pythonManager;
            GetValues(DMEEditor.Passedarguments);
        }
        public void GetValues(IPassedArgs Passedarguments)
        {
            if (Passedarguments.Objects.Where(c => c.Name == "VISUTIL").Any())
            {
                Vismanager = (IVisManager)Passedarguments.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            }
            if (Passedarguments.Objects.Where(c => c.Name == "TreeControl").Any())
            {
                TreeEditor = (ITree)Passedarguments.Objects.Where(c => c.Name == "TreeControl").FirstOrDefault().obj;
            }
           
            if (Passedarguments.Objects.Where(c => c.Name == "ControlManager").Any())
            {
                Controlmanager = (IControlManager)Passedarguments.Objects.Where(c => c.Name == "ControlManager").FirstOrDefault().obj;
            }
          

            if (Passedarguments.Objects.Where(i => i.Name == "Branch").Any())
            {
                Passedarguments.Objects.Remove(Passedarguments.Objects.Where(c => c.Name == "Branch").FirstOrDefault());
            }
            if (Passedarguments.Id > 0)
            {
                pbr = TreeEditor.treeBranchHandler.GetBranch(Passedarguments.Id);
            }

            if (Passedarguments.Objects.Where(i => i.Name == "RootBranch").Any())
            {
                Passedarguments.Objects.Remove(Passedarguments.Objects.Where(c => c.Name == "RootBranch").FirstOrDefault());
            }

            if (Passedarguments.Objects.Where(i => i.Name == "ParentBranch").Any())
            {
                Passedarguments.Objects.Remove(Passedarguments.Objects.Where(c => c.Name == "ParentBranch").FirstOrDefault());
            }
            if (pbr != null)
            {
                Passedarguments.DatasourceName = pbr.DataSourceName;
                Passedarguments.CurrentEntity = pbr.BranchText;
                if (pbr.ParentBranchID > 0)
                {
                    ParentBranch = TreeEditor.treeBranchHandler.GetBranch(pbr.ParentBranchID);
                    Passedarguments.Objects.Add(new ObjectItem() { Name = "ParentBranch", obj = ParentBranch });
                }
                Passedarguments.Objects.Add(new ObjectItem() { Name = "Branch", obj = pbr });
                if(pbr.BranchType!= EnumPointType.Root)
                {
                    int idx = TreeEditor.Branches.FindIndex(x => x.BranchClass == pbr.BranchClass && x.BranchType == EnumPointType.Root);
                    if (idx > 0)
                    {
                        RootBranch = TreeEditor.Branches[idx];
                       
                    }
                  
                }
                else
                {
                    RootBranch = pbr;
                }
                
                Passedarguments.Objects.Add(new ObjectItem() { Name = "RootBranch", obj = RootBranch });
            }


         
            if (Passedarguments.DatasourceName != null)
            {
                DataSource = DMEEditor.GetDataSource(Passedarguments.DatasourceName);
                DMEEditor.OpenDataSource(Passedarguments.DatasourceName);
            }



            ViewRootBranch = TreeEditor.Branches[TreeEditor.Branches.FindIndex(x => x.BranchClass == "VIEW" && x.BranchType == EnumPointType.Root)];
        }
       
      
       
    }
}

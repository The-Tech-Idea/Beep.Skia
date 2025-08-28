

using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Vis.Modules;

namespace Beep.Skia
{
    [AddinAttribute(Caption = "Skia", Name = "SkiaFunctions", ObjectType = "Beep", menu = "Beep", misc = "IFunctionExtension", addinType = AddinType.Class,iconimage ="workflow.ico",order =1)]
    public class BeepSKiaExtensions : IFunctionExtension
    {
        public IDMEEditor DMEEditor { get ; set ; }
        public IPassedArgs Passedargs { get; set; }

        public BeepSKiaExtensions(IDMEEditor pdMEEditor, object pvisManager, object ptreeControl)
        {
            DMEEditor = pdMEEditor;

            // ExtensionsHelpers = new FunctionandExtensionsHelpers(DMEEditor, (IVisManager)pvisManager, (ITree)ptreeControl);
        }

        [CommandAttribute(Caption = "Skia View", Name = "skiaview", Click = true, iconimage = "scenario.ico", ObjectType = "Beep", PointType = EnumPointType.Global)]
        public IErrorsInfo dataconnection(IPassedArgs Passedarguments)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
               
                // ExtensionsHelpers.GetValues(Passedarguments);
                // ExtensionsHelpers.Vismanager.ShowPage("Beep_Skia_Control", (PassedArgs)DMEEditor.Passedarguments);
                // DMEEditor.AddLogMessage("Success", $"Open Data Connection", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Could not show data connection {ex.Message}", DateTime.Now, 0, Passedarguments.DatasourceName, Errors.Failed);
            }
            return DMEEditor.ErrorObject;

        }


    }
}

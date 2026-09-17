using Beep.Skia;
using System.Reflection;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Vis.Modules;
using Beep.Skia.Model;
namespace AppExtensionsLoader
{
    /// <summary>
    /// BeepDM loader extension that discovers Skia components in loaded assemblies.
    ///
    /// The extension subscribes to the process-wide <see cref="AppDomain.AssemblyResolve"/> event,
    /// so it must be disposed when the host is done with it; otherwise the instance stays alive
    /// and its resolution logic runs for every assembly load in the process.
    /// </summary>
    public class BeepSkiaLoaderExtensions : ILoaderExtention, IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Gets or sets the current domain.
        /// </summary>
        public AppDomain CurrentDomain { get; set; }

        /// <summary>
        /// Gets or sets the loader.
        /// </summary>
        public IAssemblyHandler Loader { get; set; }
        /// <summary>
        /// Initializes a new instance of the BeepSkiaLoaderExtensions class.
        /// </summary>
        public BeepSkiaLoaderExtensions(IAssemblyHandler ploader)
        {
            Loader = ploader;

            //  DMEEditor = 
            CurrentDomain = AppDomain.CurrentDomain;

            CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        /// <summary>Unsubscribes from the process-wide assembly-resolve event. Safe to call twice.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                if (CurrentDomain != null)
                    CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
            catch { }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets or sets the load all assembly.
        /// </summary>
        public IErrorsInfo LoadAllAssembly()
        {
            ErrorsInfo er = new ErrorsInfo();

            if (Loader == null)
            {
                er.Flag = Errors.Failed;
                er.Message = "No assembly loader was supplied; nothing to scan.";
                return er;
            }

            var assemblies = Loader.Assemblies;
            if (assemblies == null)
            {
                er.Flag = Errors.Failed;
                er.Message = "The assembly loader has no assembly list.";
                return er;
            }

            List<assemblies_rep> ls = assemblies.Where(p => p.FileTypes == FolderFileTypes.ProjectClass).ToList();
            foreach (var item in ls)
            {
                try
                {
                    if (item?.DllLib != null) ScanAssembly(item.DllLib);
                }
                catch (Exception)
                {


                }

            }



            return er;
        }
        #region "Class Extractors"
        //private Type[] GetInterfaces(Type[] t, string[] interfaces)
        //{
        //    t.Where(p=>p.GetInterfaces())
        //}
        private bool ScanAssembly(Assembly asm)
        {
            Type[] t;

            try
            {
                try
                {
                    t = asm.GetTypes();
                }
                catch (Exception)
                {
                    //DMEEditor.AddLogMessage("Failed", $"Could not get types for {asm.GetName().ToString()}", DateTime.Now, -1, asm.GetName().ToString(), Errors.Failed);
                    try
                    {
                        //DMEEditor.AddLogMessage("Try", $"Trying to get exported types for {asm.GetName().ToString()}", DateTime.Now, -1, asm.GetName().ToString(), Errors.Ok);
                        t = asm.GetExportedTypes();
                    }
                    catch (Exception)
                    {
                        t = null;
                        //DMEEditor.AddLogMessage("Failed", $"Could not get types for {asm.GetName().ToString()}", DateTime.Now, -1, asm.GetName().ToString(), Errors.Failed);
                    }

                }

                if (t != null)
                {
                    
                    foreach (var mytype in t) //asm.DefinedTypes
                    {
                       
                        TypeInfo type = mytype.GetTypeInfo();
                        //string[] p = asm.FullName.Split(new char[] { ',' });
                        //p[1] = p[1].Substring(p[1].IndexOf("=") + 1);
                        // Console.WriteLine(p[1]);
                        //-------------------------------------------------------


                        //-------------------------------------------------------

                        //-------------------------------------------------------
                        // Get IBranch Definitions
                        //-------------------------------------------------------
                       
                        if (type.ImplementedInterfaces.Contains(typeof(SkiaComponent)))
                        {
                            // Without a loader there is no configuration editor to register with.
                            var configEditor = Loader?.ConfigEditor;
                            if (configEditor?.AppComponents != null)
                                configEditor.AppComponents.Add(Loader.GetAssemblyClassDefinition(type, "SkiaComponent"));
                        }
                       

                        ////-------------------------------------------------------
                        //// Get Reports Implementations Definitions
                        //if (type.ImplementedInterfaces.Contains(typeof(IReportDMWriter)))
                        //{

                        //    Loader.ConfigEditor.ReportWritersClasses.Add(Loader.GetAssemblyClassDefinition(type, "IReportDMWriter"));
                        //}
                        ////-------------------------------------------------------


                    }
                }

            }
            catch (Exception)
            {
                //DMEEditor.AddLogMessage("Failed", $"Could not get Any types for {asm.GetName().ToString()}" , DateTime.Now, -1, asm.GetName().ToString(), Errors.Failed);
            };

            return true;


        }
        #endregion "Class Extractors"
        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // This handler runs for every unresolved assembly in the process, so it must never throw.
            try
            {
                return ResolveAssembly(args);
            }
            catch
            {
                return null;
            }
        }

        private Assembly ResolveAssembly(ResolveEventArgs args)
        {
            if (args == null || string.IsNullOrWhiteSpace(args.Name)) return null;

            // Ignore missing resources
            if (args.Name.Contains(".resources"))
                return null;

            // Without a loader (or its configuration) there is nothing to resolve from.
            if (Loader == null) return null;

            string filename = args.Name.Split(',')[0] + ".dll".ToLower();
            string filenamewo = args.Name.Split(',')[0];
            // check for assemblies already loaded
            //   var s = AppDomain.CurrentDomain.GetAssemblies();
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName.StartsWith(filenamewo));
            if (assembly == null)
            {
                assemblies_rep s = Loader.Assemblies?.FirstOrDefault(a => a.DllLib != null && a.DllLib.FullName.StartsWith(filenamewo));
                if (s != null)
                {
                    assembly = s.DllLib;
                }

            }
            if (assembly != null)
                return assembly;

            var folders = Loader.ConfigEditor?.Config?.Folders;
            if (folders == null) return null;

            foreach (var moduleDir in folders.Where(c => c.FolderFilesType == FolderFileTypes.OtherDLL))
            {
                var di = new DirectoryInfo(moduleDir.FolderPath);
                var module = di.GetFiles().FirstOrDefault(i => i.Name == filename);
                if (module != null)
                {
                    return Assembly.LoadFrom(module.FullName);
                }
            }
            if (assembly != null)
                return assembly;
            foreach (var moduleDir in Loader.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ConnectionDriver))
            {
                var di = new DirectoryInfo(moduleDir.FolderPath);
                var module = di.GetFiles().FirstOrDefault(i => i.Name == filename);
                if (module != null)
                {
                    return Assembly.LoadFrom(module.FullName);
                }
            }
            if (assembly != null)
                return assembly;
            foreach (var moduleDir in Loader.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass))
            {
                var di = new DirectoryInfo(moduleDir.FolderPath);
                var module = di.GetFiles().FirstOrDefault(i => i.Name == filename);
                if (module != null)
                {
                    return Assembly.LoadFrom(module.FullName);
                }
            }


            return null;

        }
        /// <summary>
        /// Gets or sets the scan.
        /// </summary>
        public IErrorsInfo Scan()
        {
            ErrorsInfo er = new ErrorsInfo();
            try
            {
                var result = LoadAllAssembly();
                er.Flag = result?.Flag ?? Errors.Failed;
                er.Message = result?.Message;
            }
            catch (Exception ex)
            {
                er.Ex = ex;
                er.Flag = Errors.Failed;
                er.Message = ex.Message;

            }
            return er;
        }

        /// <summary>
        /// Gets or sets the scan.
        /// </summary>
        public IErrorsInfo Scan(assemblies_rep assembly)
        {

            ErrorsInfo er = new ErrorsInfo();
            try
            {

                ScanAssembly(assembly.DllLib);
                er.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                er.Ex = ex;
                er.Flag = Errors.Failed;
                er.Message = ex.Message;

            }
            return er;
        }

        /// <summary>
        /// Gets or sets the scan.
        /// </summary>
        public IErrorsInfo Scan(Assembly assembly)
        {

            ErrorsInfo er = new ErrorsInfo();
            try
            {

                ScanAssembly(assembly);
                er.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                er.Ex = ex;
                er.Flag = Errors.Failed;
                er.Message = ex.Message;

            }
            return er;
        }
    }
}

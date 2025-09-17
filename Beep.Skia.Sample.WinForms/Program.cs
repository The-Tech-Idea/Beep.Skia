using System;
using System.IO;
using System.Windows.Forms;

namespace Beep.Skia.Sample.WinForms
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Global exception logging to help diagnose UI crashes
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                try
                {
                    var fp = Path.Combine(Path.GetTempPath(), "beepskia_render.log");
                    File.AppendAllText(fp, "[ThreadException] " + DateTime.UtcNow.ToString("o") + "\n" + e.Exception + "\n\n");
                }
                catch { }
                try { MessageBox.Show("Unhandled UI exception: " + e.Exception.Message, "Beep.Skia", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch { }
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    var fp = Path.Combine(Path.GetTempPath(), "beepskia_render.log");
                    File.AppendAllText(fp, "[UnhandledException] " + DateTime.UtcNow.ToString("o") + "\n" + e.ExceptionObject + "\n\n");
                }
                catch { }
            };
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}

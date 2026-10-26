using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace DshPet
{
    internal static class Program
    {
        static void Life(string msg)
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "life.log"),
                    DateTime.Now.ToString("HH:mm:ss.fff") + "  " + msg + "\r\n", new UTF8Encoding(false));
            }
            catch { }
        }

        [STAThread]
        static void Main(string[] args)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            Life("Main enter pid=" + System.Diagnostics.Process.GetCurrentProcess().Id);
            try { Native.SetProcessDPIAware(); }
            catch { }

            bool created;
            using (Mutex mutex = new Mutex(true, "DshPet_DeepSeek_Balance_Mutex", out created))
            {
                Life("mutex created=" + created);
                if (!created)
                {
                    Life("another instance running, exit");
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException += delegate(object s, ThreadExceptionEventArgs e)
                {
                    Life("ThreadException: " + (e.Exception == null ? "null" : e.Exception.ToString()));
                    WriteCrash(baseDir, e.Exception);
                };
                AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
                {
                    Life("UnhandledException: " + (e.ExceptionObject == null ? "null" : e.ExceptionObject.ToString()));
                    WriteCrash(baseDir, e.ExceptionObject as Exception);
                };

                Dbg.Start(baseDir);
                Config cfg = Config.Load(baseDir);
                cfg.Save();

                PetWindow w = new PetWindow(cfg);
                foreach (string a in args) { if (a == "--demo" || a == "-demo") w.DemoMode = true; }
                w.FormClosed += delegate { Life("FormClosed"); };
                Life("run window");
                Application.Run(w);
                Life("Application.Run returned");
            }
            Life("Main exit");
        }

        static void WriteCrash(string baseDir, Exception ex)
        {
            try
            {
                string dir = Path.Combine(baseDir, "logs");
                Directory.CreateDirectory(dir);
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("  ")
                  .Append(ex == null ? "(null)" : ex.ToString()).Append("\r\n\r\n");
                File.AppendAllText(Path.Combine(dir, "crash.log"), sb.ToString(), new UTF8Encoding(true));
            }
            catch { }
        }
    }
}
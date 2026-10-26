using System;
using System.IO;
using System.Text;

namespace DshPet
{
    /// <summary>轻量调试日志（logs\debug.log，单次运行覆盖）。</summary>
    internal static class Dbg
    {
        public static string LogPath;
        static readonly object Sync = new object();
        static bool _init;

        public static void Start(string baseDir)
        {
            try
            {
                string dir = Path.Combine(baseDir, "logs");
                Directory.CreateDirectory(dir);
                LogPath = Path.Combine(dir, "debug.log");
                if (!File.Exists(LogPath)) File.WriteAllText(LogPath, "", new UTF8Encoding(true));
                _init = true;
            }
            catch { }
        }

        public static void W(string msg)
        {
            if (!_init || LogPath == null) return;
            try
            {
                lock (Sync)
                {
                    File.AppendAllText(LogPath, DateTime.Now.ToString("HH:mm:ss.fff") + "  " + msg + "\r\n", new UTF8Encoding(false));
                }
            }
            catch { }
        }
    }
}

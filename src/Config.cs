using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace DshPet
{
    public class Config
    {
        public string ApiUrl = "";
        public string Token = "";
        public int PollSeconds = 30;
        public double Zoom = 1.2;
        public bool TopMost = true;
        public bool ShowHud = true;
        public bool Sound = true;
        public bool ClickBanter = true;
        public long LowBalanceWarn = 20000;
        public int ExpiryWarnDays = 7;
        public int X = int.MinValue;
        public int Y = int.MinValue;
        public int TimeoutSeconds = 10;
        public string Theme = "soft";

        public string BaseDir;
        public string FilePath;

        public static Config Load(string baseDir)
        {
            Config c = new Config();
            c.BaseDir = baseDir;
            c.FilePath = Path.Combine(baseDir, "config.json");
            try
            {
                if (File.Exists(c.FilePath))
                {
                    string txt = File.ReadAllText(c.FilePath, Encoding.UTF8);
                    Dictionary<string, object> d = Json.Obj(Json.Parse(txt));
                    if (d != null)
                    {
                        c.ApiUrl = Json.Str(d, "apiUrl", c.ApiUrl);
                        c.Token = Json.Str(d, "token", c.Token);
                        c.PollSeconds = (int)Json.Long(d, "pollSeconds", c.PollSeconds);
                        c.Zoom = Json.Dbl(d, "zoom", c.Zoom);
                        c.TopMost = Json.Bool(d, "topMost", c.TopMost);
                        c.ShowHud = Json.Bool(d, "showHud", c.ShowHud);
                        c.Sound = Json.Bool(d, "sound", c.Sound);
                        c.ClickBanter = Json.Bool(d, "clickBanter", c.ClickBanter);
                        c.LowBalanceWarn = Json.Long(d, "lowBalanceWarn", c.LowBalanceWarn);
                        c.ExpiryWarnDays = (int)Json.Long(d, "expiryWarnDays", c.ExpiryWarnDays);
                        c.X = (int)Json.Long(d, "x", c.X);
                        c.Y = (int)Json.Long(d, "y", c.Y);
                        c.TimeoutSeconds = (int)Json.Long(d, "timeoutSeconds", c.TimeoutSeconds);
                        c.Theme = Json.Str(d, "theme", c.Theme);
                    }
                }
            }
            catch
            {
                // 配置损坏时使用默认值，不阻塞启动
            }
            if (c.PollSeconds < 5) c.PollSeconds = 5;
            if (c.PollSeconds > 3600) c.PollSeconds = 3600;
            if (c.Zoom < 0.5) c.Zoom = 0.5;
            if (c.Zoom > 2.0) c.Zoom = 2.0;
            if (c.TimeoutSeconds < 3) c.TimeoutSeconds = 3;
            return c;
        }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("{\r\n");
                sb.Append("  \"apiUrl\": \"").Append(Json.Esc(ApiUrl)).Append("\",\r\n");
                sb.Append("  \"token\": \"").Append(Json.Esc(Token)).Append("\",\r\n");
                sb.Append("  \"pollSeconds\": ").Append(PollSeconds).Append(",\r\n");
                sb.Append("  \"zoom\": ").Append(Zoom.ToString("0.##", CultureInfo.InvariantCulture)).Append(",\r\n");
                sb.Append("  \"topMost\": ").Append(TopMost ? "true" : "false").Append(",\r\n");
                sb.Append("  \"showHud\": ").Append(ShowHud ? "true" : "false").Append(",\r\n");
                sb.Append("  \"sound\": ").Append(Sound ? "true" : "false").Append(",\r\n");
                sb.Append("  \"clickBanter\": ").Append(ClickBanter ? "true" : "false").Append(",\r\n");
                sb.Append("  \"lowBalanceWarn\": ").Append(LowBalanceWarn).Append(",\r\n");
                sb.Append("  \"expiryWarnDays\": ").Append(ExpiryWarnDays).Append(",\r\n");
                sb.Append("  \"x\": ").Append(X).Append(",\r\n");
                sb.Append("  \"y\": ").Append(Y).Append(",\r\n");
                sb.Append("  \"timeoutSeconds\": ").Append(TimeoutSeconds).Append(",\r\n");
                sb.Append("  \"theme\": \"").Append(Json.Esc(Theme)).Append("\",\r\n");
                sb.Append("}\r\n");
                File.WriteAllText(FilePath, sb.ToString(), new UTF8Encoding(false));
            }
            catch
            {
            }
        }
    }
}

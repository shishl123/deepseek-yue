using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace DshPet
{
    /// <summary>账号明细窗口。</summary>
    public class DetailsForm : Form
    {
        readonly Snapshot _snap;

        public DetailsForm(Snapshot snap, string fontName, Config cfg)
        {
            _snap = snap;
            Text = "DeepSeek 池 · 账号余额明细";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1180, 620);
            MinimumSize = new Size(720, 420);
            Font = new Font(fontName, 9f);
            ShowInTaskbar = true;
            try
            {
                string ico = System.IO.Path.Combine(cfg.BaseDir, "assets", "icon.ico");
                if (System.IO.File.Exists(ico)) Icon = new Icon(ico);
            }
            catch { }

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.RowCount = 2;
            root.ColumnCount = 1;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            Controls.Add(root);

            Label head = new Label();
            head.Dock = DockStyle.Fill;
            head.TextAlign = ContentAlignment.MiddleLeft;
            head.Padding = new Padding(10, 0, 0, 0);
            head.Font = new Font(fontName, 10f);
            head.Text = string.Format(
                "余额合计 {0}    累计发放 {1}    健康 {2}/{3}    冷却 {4}   禁用 {5}    {6} 天内到期 {7}    更新于 {8:HH:mm:ss}",
                N(snap.CreditsSum), N(snap.CreditsTotalSum), snap.Healthy, snap.Total, snap.Cooling, snap.Disabled,
                cfg.ExpiryWarnDays, N(snap.ExpiringRemain), snap.At);
            root.Controls.Add(head, 0, 0);

            DataGridView grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.BackgroundColor = Color.FromArgb(250, 250, 252);
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(238, 241, 248);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font(fontName, 9f, FontStyle.Bold);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 255);

            grid.Columns.Add(Col("nickname", "昵称", 90));
            grid.Columns.Add(Col("uid", "uid", 110));
            grid.Columns.Add(Col("credits", "当前积分", 90));
            grid.Columns.Add(Col("creditsTotal", "累计积分", 90));
            grid.Columns.Add(Col("realm", "区域", 55));
            grid.Columns.Add(Col("earliest", "最早到期", 140));
            grid.Columns.Add(Col("remain", "该批剩余", 80));
            grid.Columns.Add(Col("state", "状态", 70));
            grid.Columns.Add(Col("tokens", "累计 token", 70));
            grid.Columns.Add(Col("requests", "请求数", 55));
            grid.Columns.Add(Col("disabledAt", "禁用时间", 120));
            grid.Columns.Add(Col("reason", "禁用/异常原因", 380));

            grid.Rows.Clear();
            foreach (AccountInfo a in snap.Accounts)
            {
                string state = a.Disabled ? "禁用" : (a.Cooling ? "冷却" : "正常");
                string expiry = "";
                try
                {
                    DateTime dt;
                    if (DateTime.TryParse(a.EarliestExpiry, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt) && dt.Year > 2000)
                        expiry = dt.ToString("yyyy-MM-dd HH:mm");
                }
                catch { }
                string disabledAt = "";
                try
                {
                    DateTime dt2;
                    if (DateTime.TryParse(a.DisabledAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt2) && dt2.Year > 2000)
                        disabledAt = dt2.ToString("MM-dd HH:mm");
                }
                catch { }

                int idx = grid.Rows.Add(
                    a.Nickname, a.Uid, N(a.Credits), N(a.CreditsTotal), "cn", expiry, N(a.EarliestRemaining),
                    state, N(a.Tokens), N(a.Requests), disabledAt, ShortReason(a.DisabledReason));

                if (a.DisabledReason.Length > 0)
                {
                    grid.Rows[idx].Cells["reason"].ToolTipText = a.DisabledReason;
                    grid.Rows[idx].DefaultCellStyle.ForeColor = Color.FromArgb(150, 60, 60);
                }
                else if (a.Credits <= 0)
                    grid.Rows[idx].DefaultCellStyle.ForeColor = Color.FromArgb(190, 60, 60);
                else if (state != "正常")
                    grid.Rows[idx].DefaultCellStyle.ForeColor = Color.FromArgb(170, 130, 20);
            }
            root.Controls.Add(grid, 0, 1);
        }

        static string ShortReason(string r)
        {
            if (string.IsNullOrEmpty(r)) return "";
            string low = r.ToLowerInvariant();
            if (low.Contains("11140") && low.Contains("restricted"))
                return "上游限制(11140) 需申诉解封";
            if (low.Contains("restricted by upstream")) return "上游限制";
            if (low.Contains("unauthorized") || low.Contains("401")) return "鉴权失败(401)";
            if (low.Contains("rate limit") || low.Contains("429")) return "限流(429)";
            if (low.Contains("timeout") || low.Contains("timed out")) return "请求超时";
            if (low.Contains("session") && low.Contains("expire")) return "会话过期";
            return r.Length > 32 ? r.Substring(0, 32) + "…" : r;
        }
        static DataGridViewTextBoxColumn Col(string name, string header, int width)
        {
            DataGridViewTextBoxColumn c = new DataGridViewTextBoxColumn();
            c.Name = name;
            c.HeaderText = header;
            c.FillWeight = width;
            c.MinimumWidth = 50;
            c.SortMode = DataGridViewColumnSortMode.Automatic;
            return c;
        }

        static string N(long v)
        {
            return v.ToString("N0", CultureInfo.InvariantCulture);
        }
    }
}

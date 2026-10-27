using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DshPet
{
    public class PetWindow : Form
    {
        // ---------- 配色 ----------
        static readonly Color CBg = Color.FromArgb(244, 12, 16, 30);
        static readonly Color CEdge = Color.FromArgb(78, 120, 150, 255);
        static readonly Color CText = Color.FromArgb(242, 234, 241, 252);
        static readonly Color CDim = Color.FromArgb(170, 168, 184, 210);
        static readonly Color CRed = Color.FromArgb(255, 246, 118, 118);
        static readonly Color CGreen = Color.FromArgb(255, 96, 214, 168);
        static readonly Color CYellow = Color.FromArgb(255, 240, 202, 96);

        // ================= 外观皮肤 =================

        class Skin
        {
            public string Key;
            public string Name;
            public Color Bg;
            public Color Border;
            public Color Title;
            public Color Value;
            public Color Dim;
            public Color Accent;
            public Color Good;
            public Color Bad;
            public Color Warn;
            public Color OutlineColor;
            public bool Panel;
            public bool Lace;
            public bool Stars;
            public bool Outline;
        }

        static Skin MakeSoft()
        {
            Skin s = new Skin();
            s.Key = "soft";
            s.Name = "奶白手账";
            s.Bg = Color.FromArgb(250, 252, 251, 255);
            s.Border = Color.FromArgb(140, 142, 170, 236);
            s.Title = Color.FromArgb(255, 116, 130, 184);
            s.Value = Color.FromArgb(255, 38, 54, 138);
            s.Dim = Color.FromArgb(210, 136, 148, 186);
            s.Accent = Color.FromArgb(255, 104, 132, 232);
            s.Good = Color.FromArgb(255, 48, 184, 136);
            s.Bad = Color.FromArgb(255, 228, 98, 124);
            s.Warn = Color.FromArgb(255, 220, 152, 48);
            s.OutlineColor = Color.FromArgb(235, 255, 255, 255);
            s.Panel = true;
            s.Lace = true;
            s.Stars = false;
            s.Outline = false;
            return s;
        }

        static Skin MakeNight()
        {
            Skin s = new Skin();
            s.Key = "night";
            s.Name = "深蓝夜幕";
            s.Bg = Color.FromArgb(206, 22, 28, 58);
            s.Border = Color.FromArgb(130, 110, 136, 240);
            s.Title = Color.FromArgb(200, 152, 170, 224);
            s.Value = Color.FromArgb(255, 228, 236, 255);
            s.Dim = Color.FromArgb(172, 152, 168, 210);
            s.Accent = Color.FromArgb(255, 128, 156, 255);
            s.Good = Color.FromArgb(255, 96, 214, 168);
            s.Bad = Color.FromArgb(255, 246, 118, 130);
            s.Warn = Color.FromArgb(255, 240, 202, 96);
            s.OutlineColor = Color.FromArgb(210, 12, 16, 34);
            s.Panel = true;
            s.Lace = false;
            s.Stars = true;
            s.Outline = false;
            return s;
        }

        static Skin MakeMinimal()
        {
            Skin s = MakeSoft();
            s.Key = "minimal";
            s.Name = "极简无框";
            s.Bg = Color.FromArgb(0, 255, 255, 255);
            s.Border = Color.FromArgb(0, 255, 255, 255);
            s.OutlineColor = Color.FromArgb(238, 255, 255, 255);
            s.Title = Color.FromArgb(230, 88, 104, 160);
            s.Value = Color.FromArgb(255, 30, 44, 120);
            s.Dim = Color.FromArgb(230, 104, 118, 168);
            s.Panel = false;
            s.Lace = false;
            s.Stars = false;
            s.Outline = true;
            return s;
        }

        static Skin SkinByKey(string key)
        {
            if (key == "night") return MakeNight();
            if (key == "minimal") return MakeMinimal();
            return MakeSoft();
        }

        Skin _skin;
        double _shown = -1;
        double _target = -1;
        DateTime _flashAt = DateTime.MinValue;
        Bitmap _whaleIcon;
        Bitmap _hudCache;
        string _hudSig = "";

        static readonly FontStyle FS_R = FontStyle.Regular;
        static readonly FontStyle FS_B = FontStyle.Bold;

        // ---------- 依赖 ----------
        readonly Config _cfg;
        public bool DemoMode;
        bool _demoDone;
        readonly StatusClient _client;

        // ---------- 资源 ----------
        readonly Dictionary<string, Bitmap> _sprites = new Dictionary<string, Bitmap>(StringComparer.Ordinal);
        readonly Dictionary<string, Bitmap> _scaled = new Dictionary<string, Bitmap>(StringComparer.Ordinal);
        readonly List<Bitmap> _startRaw = new List<Bitmap>();
        readonly List<Bitmap> _startScaled = new List<Bitmap>();
        readonly Dictionary<string, SoundPlayer> _sfx = new Dictionary<string, SoundPlayer>(StringComparer.Ordinal);

        string _fontName = "Microsoft YaHei UI";
        Font _fLabel, _fValue, _fLine, _fSmall, _fFloat, _fBalloon, _fMenu;

        LayeredSurface _surface;
        System.Windows.Forms.Timer _anim;

        // ---------- 布局 ----------
        double _dpi = 1.0;
        int _baseW = 328, _baseH = 306;
        int _petW, _petH, _hudH, _w, _h;

        // ---------- 状态 ----------
        string _state = "start";
        DateTime _stateUntil = DateTime.MinValue;
        int _startIdx;
        DateTime _startFrameAt;
        int _frame;
        double _shakeTime = -99;
        readonly Stopwatch _watch = Stopwatch.StartNew();
        double _clock;
        double _dragVX;
        double _tilt, _tiltV;
        double _inertiaVX;
        bool _hoverOn;
        double _hoverScale = 1.0;
        DateTime _holdAt = DateTime.MinValue;
        bool _holdDone;
        int _clickCount;
        double _clickTime = -99;
        DateTime _headAt = DateTime.MinValue;
        bool _headDone;
        DateTime _nextChatAt = DateTime.MinValue;
        long _milestone;
        string _prevState = "";
        double _fadeStart = -99;
        DateTime _lastActAt = DateTime.Now;
        DateTime _nextPollDue = DateTime.MinValue;
        volatile bool _busy;
        bool _closing;

        // ---------- 数据 ----------
        Snapshot _snap;
        long _prevCredits = -1;
        long _runConsumed;
        long _lastDelta;
        DateTime _lastUpdate = DateTime.MinValue;
        readonly List<KeyValuePair<DateTime, long>> _history = new List<KeyValuePair<DateTime, long>>();
        DateTime _lowWarnAt = DateTime.MinValue;
        DateTime _expiryWarnAt = DateTime.MinValue;
        DateTime _poolWarnAt = DateTime.MinValue;

        // ---------- HUD 文本 ----------
        string _v1 = "--", _r1 = "连接中", _l2 = "等待首次同步…", _r2 = "", _l3 = "", _r3 = "";
        double _bar;
        bool _low;

        // ---------- 动画元素 ----------
        class Floater
        {
            public string Text;
            public Color Color;
            public DateTime Born;
            public float Y;
            public double Life = 1.9;
        }
        readonly List<Floater> _floaters = new List<Floater>();
        string _balloon = "";
        DateTime _balloonUntil = DateTime.MinValue;
        DateTime _balloonAt = DateTime.MinValue;
        Color _balloonColor = CText;

        // ---------- 交互 ----------
        bool _dragging, _dragMoved;
        bool[] _hit;
        int _hitW, _hitH;
        const int HitStep = 4;
        Point _dragStart, _dragOrigin;
        ContextMenuStrip _menu;
        NotifyIcon _tray;
        ToolStripMenuItem _miHud, _miTop, _miSound;

        // ---------- 台词 ----------
        static readonly string[] _consumeLines = new string[]
        {
            "哥哥，刚才烧掉了 {0} 点…我好心疼",
            "{0} 积分飞走了，你打算怎么补偿我？",
            "-{0}，我听着钱包在啜泣呢",
            "又花了 {0}，我可都记在小本本上了",
            "哥哥慢一点用，{0} 点够买我好多撒娇了",
            "叮，{0} 点没了。抱我一下就不计较了",
        };

        static readonly string[] _clickLines = new string[]
        {
            "别戳啦，我在看着你的余额呢~",
            "哥哥在忙什么？我一直盯着哦",
            "想我了？现在还剩 {0} 呢",
            "再戳我，我就把尾巴甩你脸上",
            "这点余额…你可得省着点花，我心疼",
            "乖乖，摸摸头就给你看实时余额",
        };

        public PetWindow(Config cfg)
        {
            _cfg = cfg;
            _client = new StatusClient(cfg.ApiUrl, cfg.Token, cfg.TimeoutSeconds);

            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = cfg.TopMost;
            BackColor = Color.Black;
            Text = "DeepSeek 桌宠 · 余额看板";
            DoubleBuffered = false;
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.UserPaint, true);

            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) _dpi = g.DpiX / 96.0;
            }
            catch { _dpi = 1.0; }
            if (_dpi < 1.0) _dpi = 1.0;

            _skin = SkinByKey(_cfg.Theme);
            PickFont();
            LoadSprites();
            LoadSfx();
            BuildFonts();
            BuildMenu();
            BuildTray();

            _anim = new System.Windows.Forms.Timer();
            _anim.Interval = 50;
            _anim.Tick += OnTick;

            ApplyLayout(false);
            PlaceWindow();
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Native.WS_EX_LAYERED | Native.WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e) { }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Dbg.W("OnHandleCreated handle=" + Handle.ToString());
            if (_surface == null) _surface = new LayeredSurface();
            ApplyLayout(false);
            Render();
            _anim.Start();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            StartPoll();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _closing = true;
            base.OnFormClosed(e);
        }

        // ================= 初始化 =================

        void PickFont()
        {
            try
            {
                bool yaheiUi = false, yahei = false;
                FontFamily[] fams = FontFamily.Families;
                foreach (FontFamily f in fams)
                {
                    if (f.Name == "Microsoft YaHei UI") yaheiUi = true;
                    else if (f.Name == "Microsoft YaHei") yahei = true;
                }
                if (yaheiUi) _fontName = "Microsoft YaHei UI";
                else if (yahei) _fontName = "Microsoft YaHei";
                else _fontName = "SimSun";
            }
            catch { _fontName = "SimSun"; }
        }

        static Bitmap LoadPng(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (Bitmap tmp = new Bitmap(fs))
            {
                return new Bitmap(tmp);
            }
        }

        void LoadSprites()
        {
            string dir = Path.Combine(_cfg.BaseDir, "assets", "dsh");
            string[] keys = new string[] { "idle", "working", "done", "error", "approval", "sing", "tool_edit", "tool_pwsh", "tool_read", "tool_web" };
            foreach (string k in keys)
            {
                string p = Path.Combine(dir, k + ".png");
                if (File.Exists(p))
                {
                    try { _sprites[k] = LoadPng(p); }
                    catch { }
                }
            }
            for (int i = 1; i <= 3; i++)
            {
                string p = Path.Combine(dir, "start_" + i.ToString() + ".png");
                if (File.Exists(p))
                {
                    try { _startRaw.Add(LoadPng(p)); }
                    catch { }
                }
            }
            if (!_sprites.ContainsKey("idle"))
            {
                Bitmap fallback = new Bitmap(_baseW, _baseH, PixelFormat.Format32bppPArgb);
                using (Graphics g = Graphics.FromImage(fallback))
                {
                    g.Clear(Color.Transparent);
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 120, 140, 230)))
                        g.FillEllipse(b, 40, 40, _baseW - 80, _baseH - 80);
                }
                _sprites["idle"] = fallback;
            }
            _baseW = _sprites["idle"].Width;
            _baseH = _sprites["idle"].Height;
            try
            {
                string icoPng = Path.Combine(_cfg.BaseDir, "assets", "icon.png");
                if (File.Exists(icoPng)) _whaleIcon = LoadPng(icoPng);
            }
            catch { }
        }

        void LoadSfx()
        {
            string dir = Path.Combine(_cfg.BaseDir, "assets", "sfx");
            TrySfx("consume", Path.Combine(dir, "ya2.wav"));
            TrySfx("click", Path.Combine(dir, "click.wav"));
            TrySfx("hello", Path.Combine(dir, "ya1.wav"));
        }

        void TrySfx(string key, string path)
        {
            try
            {
                if (File.Exists(path)) _sfx[key] = new SoundPlayer(path);
            }
            catch { }
        }

        void PlaySfx(string key)
        {
            if (!_cfg.Sound) return;
            try
            {
                SoundPlayer p;
                if (_sfx.TryGetValue(key, out p)) p.Play();
            }
            catch { }
        }

        void BuildFonts()
        {
            DisposeFonts();
            float d = (float)(_dpi * _cfg.Zoom);
            _fLabel = new Font(_fontName, 7.5f * d, FS_R, GraphicsUnit.Point);
            _fValue = new Font(_fontName, 13f * d, FS_B, GraphicsUnit.Point);
            _fLine = new Font(_fontName, 8f * d, FS_R, GraphicsUnit.Point);
            _fSmall = new Font(_fontName, 7f * d, FS_R, GraphicsUnit.Point);
            _fFloat = new Font(_fontName, 12f * d, FS_B, GraphicsUnit.Point);
            _fBalloon = new Font(_fontName, 8.5f * d, FS_R, GraphicsUnit.Point);
            _fMenu = new Font(_fontName, 9f, FS_R, GraphicsUnit.Point);
        }
        void DisposeFonts()
        {
            Font[] all = new Font[] { _fLabel, _fValue, _fLine, _fSmall, _fFloat, _fBalloon };
            foreach (Font f in all) { if (f != null) f.Dispose(); }
            if (_fMenu != null) { _fMenu.Dispose(); _fMenu = null; }
            _fLabel = _fValue = _fLine = _fSmall = _fFloat = _fBalloon = null;
        }

        void BuildMenu()
        {
            _menu = new ContextMenuStrip();
            _menu.Font = _fMenu;
            _menu.ShowImageMargin = true;

            ToolStripMenuItem miRefresh = new ToolStripMenuItem("立即刷新");
            miRefresh.Click += delegate { StartPoll(); };
            _menu.Items.Add(miRefresh);

            ToolStripMenuItem miDetail = new ToolStripMenuItem("查看账号明细…");
            miDetail.Click += delegate { ShowDetails(); };
            _menu.Items.Add(miDetail);

            _menu.Items.Add(new ToolStripSeparator());

            _miHud = new ToolStripMenuItem("显示余额面板");
            _miHud.Checked = _cfg.ShowHud;
            _miHud.CheckOnClick = true;
            _miHud.Click += delegate { _cfg.ShowHud = _miHud.Checked; _cfg.Save(); ApplyLayout(true); Render(); };
            _menu.Items.Add(_miHud);

            _miTop = new ToolStripMenuItem("始终置顶");
            _miTop.Checked = _cfg.TopMost;
            _miTop.CheckOnClick = true;
            _miTop.Click += delegate { _cfg.TopMost = _miTop.Checked; TopMost = _cfg.TopMost; _cfg.Save(); };
            _menu.Items.Add(_miTop);

            _miSound = new ToolStripMenuItem("提示音");
            _miSound.Checked = _cfg.Sound;
            _miSound.CheckOnClick = true;
            _miSound.Click += delegate { _cfg.Sound = _miSound.Checked; _cfg.Save(); };
            _menu.Items.Add(_miSound);

            ToolStripMenuItem miSize = new ToolStripMenuItem("大小");
            string[] names = new string[] { "小", "标准", "大", "特大" };
            double[] zooms = new double[] { 0.7, 1.0, 1.3, 1.6 };
            for (int i = 0; i < names.Length; i++)
            {
                double z = zooms[i];
                ToolStripMenuItem sub = new ToolStripMenuItem(names[i]);
                sub.Click += delegate { SetZoom(z); };
                miSize.DropDownItems.Add(sub);
            }
            _menu.Items.Add(miSize);

            ToolStripMenuItem miSkin = new ToolStripMenuItem("外观");
            string[] skeys = new string[] { "soft", "night", "minimal" };
            string[] snames = new string[] { "奶白手账", "深蓝夜幕", "极简无框" };
            for (int i = 0; i < skeys.Length; i++)
            {
                ToolStripMenuItem sub = new ToolStripMenuItem(snames[i]);
                sub.Tag = skeys[i];
                sub.Checked = (_cfg.Theme == skeys[i]);
                string key = skeys[i];
                sub.Click += delegate { ApplySkin(key); };
                miSkin.DropDownItems.Add(sub);
            }
            miSkin.DropDownOpening += delegate
            {
                foreach (ToolStripItem it in miSkin.DropDownItems)
                {
                    ToolStripMenuItem mi = it as ToolStripMenuItem;
                    if (mi != null) mi.Checked = ((string)mi.Tag) == _cfg.Theme;
                }
            };
            _menu.Items.Add(miSkin);

            _menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem miDir = new ToolStripMenuItem("打开程序目录");
            miDir.Click += delegate { try { Process.Start("explorer.exe", _cfg.BaseDir); } catch { } };
            _menu.Items.Add(miDir);

            ToolStripMenuItem miQuit = new ToolStripMenuItem("退出桌宠");
            miQuit.Click += delegate { Close(); Application.Exit(); };
            _menu.Items.Add(miQuit);
        }

        void BuildTray()
        {
            try
            {
                string ico = Path.Combine(_cfg.BaseDir, "assets", "icon.ico");
                _tray = new NotifyIcon();
                _tray.Text = "DeepSeek 桌宠 · 余额看板";
                if (File.Exists(ico)) _tray.Icon = new Icon(ico, 32, 32);
                else _tray.Icon = SystemIcons.Application;
                _tray.Visible = true;
                _tray.DoubleClick += delegate { ToggleVisible(); };
                _tray.ContextMenuStrip = _menu;
            }
            catch { }
        }

        void ToggleVisible()
        {
            if (Visible) { Hide(); }
            else { Show(); Render(); }
        }

        // ================= 布局 =================

        void PlaceWindow()
        {
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            int x = _cfg.X, y = _cfg.Y;
            if (x == int.MinValue) x = wa.Right - _w - 40;
            if (y == int.MinValue) y = wa.Bottom - _h - 40;
            if (x < wa.Left - 200 || x > wa.Right + 200) x = wa.Right - _w - 40;
            if (y < wa.Top - 200 || y > wa.Bottom + 200) y = wa.Bottom - _h - 40;
            Location = new Point(x, y);
            _cfg.X = Left;
            _cfg.Y = Top;
            _cfg.Save();
            Dbg.W(string.Format("PlaceWindow {0},{1} wa={2}", x, y, wa));
        }

        void ApplySkin(string key)
        {
            _cfg.Theme = key;
            _cfg.Save();
            _skin = SkinByKey(key);
            Render();
        }

        void StepNumbers()
        {
            if (_target < 0) return;
            if (_shown < 0) { _shown = _target - Math.Min(_target, 2600); return; }
            double diff = _target - _shown;
            if (Math.Abs(diff) < 0.5) _shown = _target;
            else _shown += diff * 0.22;
        }
        void SetZoom(double z)
        {
            if (z < 0.5) z = 0.5;
            if (z > 2.0) z = 2.0;
            _cfg.Zoom = z;
            _cfg.Save();
            BuildFonts();
            ApplyLayout(true);
            Render();
        }

        void ApplyLayout(bool keepAnchor)
        {
            Point anchor = new Point(Left + Width, Top + Height);

            foreach (Bitmap b in _scaled.Values) b.Dispose();
            _scaled.Clear();
            foreach (Bitmap b in _startScaled) b.Dispose();
            _startScaled.Clear();

            float u = (float)(_dpi * _cfg.Zoom);
            _petW = (int)Math.Round(_baseW * _cfg.Zoom);
            _petH = (int)Math.Round(_baseH * _cfg.Zoom);
            _hudH = _cfg.ShowHud ? (int)Math.Round(88 * u) : 0;
            _w = Math.Max(_petW + (int)Math.Round(6 * u), (int)Math.Round(286 * u));
            _h = _petH + _hudH;

            if (_surface == null) _surface = new LayeredSurface();
            _surface.EnsureSize(_w, _h);

            Size = new Size(_w, _h);
            if (IsHandleCreated) Native.SetWindowPos(Handle, IntPtr.Zero, Left, Top, _w, _h, Native.SWP_NOZORDER | Native.SWP_NOACTIVATE);
            Dbg.W(string.Format("ApplyLayout dpi={0} base={1}x{2} pet={3}x{4} hud={5} w={6} h={7} size={8} loc={9}", _dpi, _baseW, _baseH, _petW, _petH, _hudH, _w, _h, Size, Location));
            if (keepAnchor) Location = new Point(anchor.X - _w, anchor.Y - _h);
        }

        Bitmap GetScaled(string key)
        {
            Bitmap src;
            if (!_sprites.TryGetValue(key, out src)) src = _sprites["idle"];
            if (Math.Abs(_cfg.Zoom - 1.0) < 0.001) return src;
            Bitmap cached;
            if (_scaled.TryGetValue(key, out cached)) return cached;
            Bitmap b = new Bitmap(Math.Max(1, (int)Math.Round(src.Width * _cfg.Zoom)),
                                  Math.Max(1, (int)Math.Round(src.Height * _cfg.Zoom)),
                                  PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.InterpolationMode = InterpolationMode.Bilinear;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawImage(src, new Rectangle(0, 0, b.Width, b.Height));
            }
            _scaled[key] = b;
            return b;
        }

        List<Bitmap> StartFrames()
        {
            if (_startScaled.Count > 0) return _startScaled;
            foreach (Bitmap src in _startRaw)
            {
                if (Math.Abs(_cfg.Zoom - 1.0) < 0.001) { _startScaled.Add(src); continue; }
                Bitmap b = new Bitmap(Math.Max(1, (int)Math.Round(src.Width * _cfg.Zoom)),
                                      Math.Max(1, (int)Math.Round(src.Height * _cfg.Zoom)),
                                      PixelFormat.Format32bppPArgb);
                using (Graphics g = Graphics.FromImage(b))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(src, new Rectangle(0, 0, b.Width, b.Height));
                }
                _startScaled.Add(b);
            }
            return _startScaled;
        }

        // ================= 状态机 =================

        void SetState(string key, double seconds)
        {
            if (_state != key)
            {
                _prevState = _state;
                _fadeStart = Now;
            }
            _state = key;
            _stateUntil = seconds > 0 ? DateTime.Now.AddSeconds(seconds) : DateTime.MinValue;
        }

        double Now { get { return _watch.Elapsed.TotalSeconds; } }

        void Shake()
        {
            _shakeTime = Now;
            _lastActAt = DateTime.Now;
        }

        bool IsSolid(int x, int y)
        {
            if (_hit == null || _hitW <= 0) return false;
            int gx = x / HitStep;
            int gy = y / HitStep;
            if (gx < 0 || gy < 0 || gx >= _hitW || gy >= _hitH) return false;
            return _hit[gy * _hitW + gx];
        }

        void OnTick(object sender, EventArgs e)
        {
            if (_closing) return;
            double now = Now;
            double dt = now - _clock;
            if (dt <= 0) dt = 0.025;
            if (dt > 0.25) dt = 0.25;
            _clock = now;
            _frame++;

            HandleDrag(now);
            StepNumbers();
            UpdateTilt(dt);
            UpdateHover(dt);
            UpdateHeadPat(now);
            UpdateChat(now);

            if (_state == "start")
            {
                if (_startIdx < 3 && (DateTime.Now - _startFrameAt).TotalMilliseconds > 170)
                {
                    _startFrameAt = DateTime.Now;
                    _startIdx++;
                    if (_startIdx >= 3)
                    {
                        _startIdx = 2;
                        SetState(_snap == null ? "working" : (_snap.Ok ? "idle" : "error"), 0);
                    }
                }
            }

            if (_stateUntil != DateTime.MinValue && DateTime.Now >= _stateUntil)
            {
                _stateUntil = DateTime.MinValue;
                if (_snap == null) SetState("working", 0);
                else if (_snap.Ok) SetState("idle", 0);
                else SetState("error", 0);
            }

            if (DemoMode && !_demoDone && _snap != null && _snap.Ok && (DateTime.Now - _lastUpdate).TotalSeconds > 4)
            {
                _demoDone = true;
                RunDemo();
            }

            if (DateTime.Now >= _nextPollDue) StartPoll();

            if (_cfg.TopMost && (_frame % 45) == 0 && IsHandleCreated)
            {
                try { Native.SetWindowPos(Handle, new IntPtr(-1), 0, 0, 0, 0, Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE); }
                catch { }
            }

            Render();

            bool busy = _dragging || _floaters.Count > 0 || (DateTime.Now < _balloonUntil && _balloon.Length > 0)
                        || Math.Abs(_tilt) > 0.35 || Math.Abs(_inertiaVX) > 0.4
                        || _hoverOn || (now - _fadeStart) < 0.3;
            int interval = 25;
            if (!busy && (DateTime.Now - _lastActAt).TotalSeconds > 4) interval = 200;
            if (_anim.Interval != interval) _anim.Interval = interval;
        }

        void UpdateTilt(double dt)
        {
            double target = 0;
            if (_dragging) target = Math.Max(-10, Math.Min(10, _dragVX * 1.7));
            double acc = (target - _tilt) * 30 - _tiltV * 10;
            _tiltV += acc * dt;
            _tilt += _tiltV * dt;
            if (Math.Abs(_tilt) < 0.02 && Math.Abs(_tiltV) < 0.02) { _tilt = 0; _tiltV = 0; }
        }

        void UpdateHover(double dt)
        {
            Point mp = PointToClient(Cursor.Position);
            bool inside = mp.X >= 0 && mp.Y >= 0 && mp.X < _w && mp.Y < _petH;
            bool solid = inside && IsSolid(mp.X, mp.Y);
            if (solid != _hoverOn)
            {
                _hoverOn = solid;
                _lastActAt = DateTime.Now;
            }
            double target = _hoverOn ? 1.035 : 1.0;
            _hoverScale += (target - _hoverScale) * Math.Min(1.0, dt * 12);
        }

        void UpdateHeadPat(double now)
        {
            if (!_cfg.ClickBanter) return;
            Point mp = PointToClient(Cursor.Position);
            bool onHead = mp.X >= 0 && mp.Y >= 0 && mp.X < _w && mp.Y < _petH * 0.34 && IsSolid(mp.X, mp.Y);
            if (onHead && !_dragging)
            {
                if (_headAt == DateTime.MinValue) _headAt = DateTime.Now;
                else if (!_headDone && (DateTime.Now - _headAt).TotalSeconds > 0.7)
                {
                    _headDone = true;
                    Dbg.W("headpat fired");
                    SetState("approval", 1.9);
                    ShowBalloon("唔…摸头好舒服，再摸一下~", CGreen, 2.8);
                    PlaySfx("click");
                    _lastActAt = DateTime.Now;
                }
            }
            else
            {
                _headAt = DateTime.MinValue;
                _headDone = false;
            }
        }

        void UpdateChat(double now)
        {
            if (_nextChatAt == DateTime.MinValue) { _nextChatAt = DateTime.Now.AddMinutes(38 + _rnd.Next(0, 20)); return; }
            if (DateTime.Now < _nextChatAt) return;
            _nextChatAt = DateTime.Now.AddMinutes(38 + _rnd.Next(0, 30));
            if (_snap == null || !_snap.Ok) return;
            string[] lines = new string[]
            {
                "哥哥，现在还剩 " + Num(_snap.CreditsSum) + " 点，我盯着呢",
                "已经陪了你一会儿了，喝口水吧？",
                "别老盯着屏幕，偶尔看看我嘛",
                "余额我记着呢，你安心干活就好",
                "刚才又少了一点点，我不说你，我心疼"
            };
            ShowBalloon(lines[_rnd.Next(lines.Length)], CGreen, 6);
            SetState("sing", 1.8);
            _lastActAt = DateTime.Now;
        }

        void SnapToEdge()
        {
            Rectangle wa = Screen.FromPoint(Cursor.Position).WorkingArea;
            int sn = (int)Math.Round(16 * _dpi);
            if (Math.Abs(Left - wa.Left) < sn) Left = wa.Left;
            else if (Math.Abs(wa.Right - (Left + Width)) < sn) Left = wa.Right - Width;
            if (Math.Abs(Top - wa.Top) < sn) Top = wa.Top;
            else if (Math.Abs(wa.Bottom - (Top + Height)) < sn) Top = wa.Bottom - Height;
        }

        void HandleDrag(double now)
        {
            if (_dragging)
            {
                bool down = (Native.GetAsyncKeyState(Native.VK_LBUTTON) & 0x8000) != 0;
                if (down)
                {
                    Point p = Cursor.Position;
                    int nx = _dragOrigin.X + (p.X - _dragStart.X);
                    int ny = _dragOrigin.Y + (p.Y - _dragStart.Y);
                    int stepX = nx - Left;
                    _dragVX = _dragVX * 0.55 + stepX * 0.45;
                    if (Math.Abs(p.X - _dragStart.X) + Math.Abs(p.Y - _dragStart.Y) > 3) _dragMoved = true;
                    Location = new Point(nx, ny);
                    _lastActAt = DateTime.Now;

                    if (!_dragMoved && !_holdDone)
                    {
                        if (_holdAt == DateTime.MinValue) _holdAt = DateTime.Now;
                        else if ((DateTime.Now - _holdAt).TotalSeconds > 0.8)
                        {
                            _holdDone = true;
                            SetState("error", 1.8);
                            Shake();
                            ShowBalloon("唔…别捏我脸，会变形的啦", CYellow, 3.2);
                            PlaySfx("consume");
                        }
                    }
                }
                else
                {
                    _dragging = false;
                    _holdAt = DateTime.MinValue;
                    bool wasHold = _holdDone;
                    _holdDone = false;
                    if (_dragMoved)
                    {
                        SnapToEdge();
                        _inertiaVX = Math.Max(-26, Math.Min(26, _dragVX * 0.95));
                        _cfg.X = Left;
                        _cfg.Y = Top;
                        _cfg.Save();
                    }
                    else if (!wasHold)
                    {
                        OnPetClicked();
                    }
                }
            }
            else if (Math.Abs(_inertiaVX) > 0.4)
            {
                Rectangle wa = Screen.FromPoint(Cursor.Position).WorkingArea;
                int nx = Left + (int)Math.Round(_inertiaVX);
                if (nx < wa.Left) { nx = wa.Left; _inertiaVX = -_inertiaVX * 0.45; }
                if (nx + Width > wa.Right) { nx = wa.Right - Width; _inertiaVX = -_inertiaVX * 0.45; }
                Location = new Point(nx, Top);
                _inertiaVX *= 0.90;
                if (Math.Abs(_inertiaVX) <= 0.4)
                {
                    _inertiaVX = 0;
                    _cfg.X = Left;
                    _cfg.Y = Top;
                    _cfg.Save();
                }
            }
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_NCHITTEST && _hit != null && _hitW > 0)
            {
                int lp = m.LParam.ToInt32();
                int sx = (short)(lp & 0xFFFF);
                int sy = (short)((lp >> 16) & 0xFFFF);
                Point cp = PointToClient(new Point(sx, sy));
                int gx = cp.X / HitStep;
                int gy = cp.Y / HitStep;
                bool solid = false;
                if (gx >= 0 && gy >= 0 && gx < _hitW && gy < _hitH) solid = _hit[gy * _hitW + gx];
                if (!solid)
                {
                    m.Result = new IntPtr(Native.HTTRANSPARENT);
                    return;
                }
            }
            base.WndProc(ref m);
        }

        void RebuildHitMask()
        {
            if (_surface == null || _surface.Bitmap == null) return;
            Bitmap bmp = _surface.Bitmap;
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
            try
            {
                int w = bmp.Width, h = bmp.Height;
                byte[] buf = new byte[data.Stride * h];
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buf, 0, buf.Length);
                _hitW = (w + HitStep - 1) / HitStep;
                _hitH = (h + HitStep - 1) / HitStep;
                _hit = new bool[_hitW * _hitH];
                for (int y = 0; y < h; y += HitStep)
                {
                    int row = y * data.Stride;
                    int gy = y / HitStep;
                    for (int x = 0; x < w; x += HitStep)
                    {
                        int a = buf[row + x * 4 + 3];
                        _hit[gy * _hitW + (x / HitStep)] = a >= 40;
                    }
                }
            }
            finally { bmp.UnlockBits(data); }
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Right)
            {
                try { _menu.Show(Cursor.Position); } catch { }
                return;
            }
            if (e.Button == MouseButtons.Left)
            {
                if (e.Clicks >= 2)
                {
                    _dragging = false;
                    ShowDetails();
                    return;
                }
                _dragging = true;
                _dragMoved = false;
                _holdAt = DateTime.MinValue;
                _holdDone = false;
                _dragStart = Cursor.Position;
                _dragOrigin = Location;
            }
        }

        void OnPetClicked()
        {
            if (!_cfg.ClickBanter) return;
            _lastActAt = DateTime.Now;
            SetState(_rnd.Next(2) == 0 ? "approval" : "sing", 1.6);
            string line = _clickLines[_rnd.Next(_clickLines.Length)];
            if (line.IndexOf("{0}") >= 0) line = string.Format(line, _snap != null ? Num(_snap.CreditsSum) : "--");
            ShowBalloon(line, CText, 3.5);
            PlaySfx("click");
        }
        // ================= 网络 =================

        void StartPoll()
        {
            if (_busy || _closing) return;
            if (string.IsNullOrEmpty(_cfg.ApiUrl))
            {
                SetState("error", 0);
                ShowBalloon("还没配置接口：编辑 config.json 填 apiUrl 和 token", CYellow, 8);
                _l2 = "未配置接口"; _r1 = ""; _r3 = "离线";
                _nextPollDue = DateTime.Now.AddSeconds(30);
                Render();
                return;
            }
            _busy = true;
            _nextPollDue = DateTime.Now.AddSeconds(_cfg.PollSeconds);
            if (_snap == null) SetState("working", 0);

            Task.Factory.StartNew<Snapshot>(delegate { return _client.Fetch(_cfg.ExpiryWarnDays); })
                .ContinueWith(delegate(Task<Snapshot> t)
                {
                    Snapshot sn;
                    if (t.Status == TaskStatus.RanToCompletion) sn = t.Result;
                    else if (t.Exception != null) sn = Snapshot.Fail(t.Exception.GetBaseException().Message);
                    else sn = Snapshot.Fail("任务异常");
                    try
                    {
                        BeginInvoke(new Action<Snapshot>(OnSnapshot), sn);
                    }
                    catch
                    {
                        _busy = false;
                    }
                });
        }

        void OnSnapshot(Snapshot sn)
        {
            _busy = false;
            if (_closing || IsDisposed) return;
            DateTime now = DateTime.Now;

            if (!sn.Ok)
            {
                SetState("error", 0);
                ShowBalloon("连不上后台了… " + sn.Error, CRed, 6);
                _l2 = "同步失败，稍后重试"; _r1 = "离线"; _r3 = "离线";
                _r2 = now.ToString("HH:mm:ss");
                Log(now, sn, 0, "error");
                Render();
                return;
            }

            bool first = _prevCredits < 0;
            long prev = _prevCredits;
            _prevCredits = sn.CreditsSum;
            _snap = sn;
            _lastUpdate = now;
            _lastDelta = 0;

            if (first)
            {
                string cnt = sn.Accounts.Count.ToString();
                ShowBalloon("哥哥，我上岗啦，" + cnt + " 个号已盯住~", CGreen, 4);
                SetState("idle", 0);
                PlaySfx("hello");
                Log(now, sn, 0, "start");
            }
            else if (prev > sn.CreditsSum)
            {
                long delta = prev - sn.CreditsSum;
                _runConsumed += delta;
                RaiseConsume(delta, now);
                Log(now, sn, delta, "consume");
            }
            else if (prev < sn.CreditsSum)
            {
                long gain = sn.CreditsSum - prev;
                AddFloater("+" + Num(gain), CGreen);
                SetState("sing", 2.6);
                ShowBalloon("余额回来了 +" + Num(gain) + "，我又活过来了~", CGreen, 5);
                Log(now, sn, -gain, "recharge");
            }
            else
            {
                SetState("idle", 0);
                Log(now, sn, 0, "ok");
            }

            if (first && sn.Total > 0 && sn.Accounts.Count != sn.Total)
            {
                ShowBalloon("接口只返回了 " + sn.Accounts.Count + " 个号，但 total=" + sn.Total + "，余额可能少算", CYellow, 9);
            }

            _history.Add(new KeyValuePair<DateTime, long>(now, sn.CreditsSum));
            while (_history.Count > 0 && (now - _history[0].Key).TotalMinutes > 60) _history.RemoveAt(0);

            UpdateHudText();
            CheckWarnings(now);
            Render();
        }

        void CheckWarnings(DateTime now)
        {
            if (_snap == null) return;

            if (_snap.CreditsSum <= _cfg.LowBalanceWarn && (now - _lowWarnAt).TotalMinutes > 10)
            {
                _lowWarnAt = now;
                SetState("error", 4);
                ShowBalloon("余额只剩 " + Num(_snap.CreditsSum) + " 了，哥哥快续上！", CYellow, 7);
            }

            if (_snap.Cooling > 0 || _snap.Disabled > 0)
            {
                if ((now - _poolWarnAt).TotalMinutes > 10)
                {
                    _poolWarnAt = now;
                    ShowBalloon("池里有 " + _snap.Cooling.ToString() + " 个冷却、" + _snap.Disabled.ToString() + " 个禁用，注意下", CYellow, 6);
                }
            }

            if (_snap.ExpiringRemain > 0 && (now - _expiryWarnAt).TotalMinutes > 60)
            {
                _expiryWarnAt = now;
                ShowBalloon(_cfg.ExpiryWarnDays.ToString() + " 天内到期的额度 " + Num(_snap.ExpiringRemain) + "，别浪费呀", CYellow, 6);
            }
        }

        void RaiseConsume(long delta, DateTime now)
        {
            _lastDelta = delta;
            AddFloater("-" + Num(delta), CRed);
            Shake();
            SetState("done", 2.4);
            string line = _consumeLines[_rnd.Next(_consumeLines.Length)];
            ShowBalloon(string.Format(line, Num(delta)), CRed, 5);
            PlaySfx("consume");

            long m = _runConsumed / 1000;
            if (m > _milestone)
            {
                _milestone = m;
                ShowBalloon("已经烧掉 " + Num(_runConsumed) + " 点了…哥哥，我有点心疼", CYellow, 6);
            }
            UpdateHudText();
        }
        void RunDemo()
        {
            long delta = 1234;
            _runConsumed += delta;
            RaiseConsume(delta, DateTime.Now);
            Log(DateTime.Now, _snap, delta, "demo");
        }
        static string Num(long v)
        {
            return v.ToString("N0", CultureInfo.InvariantCulture);
        }

        void UpdateHudText()
        {
            if (_snap == null) return;
            _v1 = Num(_snap.CreditsSum);
            _target = _snap.CreditsSum;

            double rate = RatePerMinute();
            if (rate > 0.05) _r1 = "可撑 " + FmtDuration(_snap.CreditsSum / rate);
            else _r1 = "稳定";

            string delta = _lastDelta > 0 ? ("-" + Num(_lastDelta)) : "0";
            string total = _runConsumed > 0 ? ("-" + Num(_runConsumed)) : "0";
            _l2 = "本轮 " + delta + " · 累计 " + total;

            if (_snap.ExpiringRemain > 0)
                _l3 = _cfg.ExpiryWarnDays.ToString() + "天到期 " + Num(_snap.ExpiringRemain);
            else
                _l3 = _cfg.ExpiryWarnDays.ToString() + "天内无到期";

            _r3 = "健康 " + _snap.Healthy.ToString() + "/" + _snap.Total.ToString();
            _bar = _snap.Total > 0 ? (double)_snap.Healthy / _snap.Total : 0;
            _low = _snap.CreditsSum <= _cfg.LowBalanceWarn;
        }

        string ElapsedText()
        {
            if (_lastUpdate == DateTime.MinValue) return "";
            double s = (DateTime.Now - _lastUpdate).TotalSeconds;
            if (s < 3) return "刚刚";
            if (s < 90) return ((int)s).ToString() + "秒前";
            if (s < 5400) return ((int)(s / 60)).ToString() + "分钟前";
            return ((int)(s / 3600)).ToString() + "小时前";
        }
        double RatePerMinute()
        {
            if (_history.Count < 2) return 0;
            KeyValuePair<DateTime, long> a = _history[0];
            KeyValuePair<DateTime, long> b = _history[_history.Count - 1];
            double mins = (b.Key - a.Key).TotalMinutes;
            if (mins < 0.2) return 0;
            long used = a.Value - b.Value;
            if (used <= 0) return 0;
            return used / mins;
        }

        static string FmtDuration(double minutes)
        {
            if (minutes < 60) return Math.Round(minutes).ToString("0") + " 分钟";
            if (minutes < 60 * 24) return (minutes / 60).ToString("0.0") + " 小时";
            return (minutes / 1440).ToString("0.0") + " 天";
        }

        void AddFloater(string text, Color color)
        {
            Floater f = new Floater();
            f.Text = text;
            f.Color = color;
            f.Born = DateTime.Now;
            f.Y = _petH * 0.58f;
            _floaters.Add(f);
        }

        void ShowBalloon(string text, Color color, double seconds)
        {
            if (string.IsNullOrEmpty(text)) return;
            _balloon = text;
            _balloonColor = color;
            _balloonUntil = DateTime.Now.AddSeconds(seconds);
            _balloonAt = DateTime.Now;
        }

        void Log(DateTime now, Snapshot s, long delta, string note)
        {
            try
            {
                string dir = Path.Combine(_cfg.BaseDir, "logs");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, "balance-" + now.ToString("yyyyMMdd") + ".csv");
                bool create = !File.Exists(file);
                StringBuilder sb = new StringBuilder();
                if (create) sb.Append("time,credits_sum,credits_total,healthy,total,cooling,disabled,delta,expiring,note\r\n");
                sb.Append(now.ToString("yyyy-MM-dd HH:mm:ss")).Append(',')
                  .Append(s.CreditsSum).Append(',')
                  .Append(s.CreditsTotalSum).Append(',')
                  .Append(s.Healthy).Append(',')
                  .Append(s.Total).Append(',')
                  .Append(s.Cooling).Append(',')
                  .Append(s.Disabled).Append(',')
                  .Append(delta).Append(',')
                  .Append(s.ExpiringRemain).Append(',')
                  .Append(note).Append("\r\n");
                File.AppendAllText(file, sb.ToString(), create ? new UTF8Encoding(true) : new UTF8Encoding(false));
            }
            catch { }
        }

        // ================= 绘制 =================

        static readonly StringFormat SFLeft = CreateSf(StringAlignment.Near);
        static readonly StringFormat SFRight = CreateSf(StringAlignment.Far);

        static readonly SolidBrush BText = new SolidBrush(CText);
        static readonly SolidBrush BDim = new SolidBrush(CDim);
        static readonly SolidBrush BRed = new SolidBrush(CRed);
        static readonly SolidBrush BGreen = new SolidBrush(CGreen);
        static readonly SolidBrush BYellow = new SolidBrush(CYellow);

        static StringFormat CreateSf(StringAlignment a)
        {
            StringFormat f = new StringFormat(StringFormatFlags.NoWrap | StringFormatFlags.NoClip);
            f.Alignment = a;
            f.LineAlignment = StringAlignment.Near;
            return f;
        }

        static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            if (radius <= 0) { p.AddRectangle(r); return p; }
            int d = radius * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            if (d <= 1) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        readonly Random _rnd = new Random();

        void Render()
        {
            if (_surface == null || _surface.Bitmap == null) return;

            using (Graphics g = Graphics.FromImage(_surface.Bitmap))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                g.InterpolationMode = InterpolationMode.Bilinear;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                DrawPet(g);
                if (_cfg.ShowHud) DrawHudCached(g);
                DrawFloaters(g);
                DrawBalloon(g);
            }

            try
            {
                byte alpha = 245;
                if (_snap != null && !_snap.Ok) alpha = 210;
                _surface.Present(Handle, alpha);
                if (_frame < 4) Dbg.W(string.Format("Render frame={0} surf={1}x{2} win={3} ok={4} err={5} a_center={6} a_hud={7}", _frame, _surface.Width, _surface.Height, Size, _surface.LastOk, _surface.LastError, _surface.AlphaAt(_w / 2, _petH / 2), _surface.AlphaAt(_w / 2, _petH + 40)));
                if ((_frame % 24) == 0) RebuildHitMask();
            }
            catch { }
        }

        Bitmap SpriteFor(string key)
        {
            if (key == "start" && _startRaw.Count > 0)
            {
                List<Bitmap> frames = StartFrames();
                return frames[Math.Min(_startIdx, frames.Count - 1)];
            }
            return GetScaled(key);
        }

        void DrawSprite(Graphics g, Bitmap bmp, double alpha, float bob)
        {
            int x = (_w - bmp.Width) / 2;
            int y = (int)Math.Round(bob) + (_petH - bmp.Height);
            if (alpha >= 0.995) { g.DrawImage(bmp, x, y); return; }
            if (alpha <= 0.01) return;
            using (ImageAttributes ia = new ImageAttributes())
            {
                ColorMatrix cm = new ColorMatrix();
                cm.Matrix33 = (float)alpha;
                ia.SetColorMatrix(cm);
                g.DrawImage(bmp, new Rectangle(x, y, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, ia);
            }
        }

        void DrawPet(Graphics g)
        {
            double now = Now;
            Bitmap cur = SpriteFor(_state);
            if (cur == null) return;

            float breathe = (float)Math.Sin(now * 2.0);
            float bob = breathe * 2.2f * (float)_cfg.Zoom;

            float shake = 0f;
            double st = now - _shakeTime;
            if (st >= 0 && st < 0.55)
            {
                double k = 1.0 - st / 0.55;
                shake = (float)(Math.Sin(st * 42) * 5 * _cfg.Zoom * k);
            }

            float swayX = (float)(Math.Sin(now * 0.55) * 1.5 * (float)_cfg.Zoom);
            float tilt = (float)_tilt;
            float sc = (float)_hoverScale;
            float scY = sc * (1f + 0.010f * breathe);
            double fade = Math.Min(1.0, (now - _fadeStart) / 0.16);
            bool animated = Math.Abs(tilt) > 0.05 || Math.Abs(sc - 1f) > 0.002f || fade < 0.999;

            if (_cfg.Zoom >= 0.6)
            {
                int ew = (int)(cur.Width * 0.5);
                int eh = Math.Max(4, (int)(9 * _cfg.Zoom));
                Rectangle er = new Rectangle((_w - ew) / 2, _petH - eh, ew, eh);
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(34, 50, 66, 120)))
                    g.FillEllipse(sb, er);
            }

            if (animated)
            {
                GraphicsState gs = g.Save();
                g.TranslateTransform(_w / 2f + shake + swayX, _petH / 2f);
                g.RotateTransform(tilt);
                g.ScaleTransform(sc, scY);
                g.TranslateTransform(-_w / 2f, -_petH / 2f);

                if (fade < 1 && _prevState.Length > 0)
                {
                    Bitmap prev = SpriteFor(_prevState);
                    if (prev != null) DrawSprite(g, prev, 1 - fade, bob);
                }
                DrawSprite(g, cur, fade, bob);

                g.Restore(gs);
            }
            else
            {
                int xoff = (int)Math.Round(swayX + shake);
                int yoff = (int)Math.Round(bob);
                g.DrawImage(cur, (_w - cur.Width) / 2 + xoff, (_petH - cur.Height) + yoff);
            }
        }
        void DrawLabel(Graphics g, string text, Font f, SolidBrush b, float x, float y, Skin s)
        {
            DrawLabel(g, text, f, b, x, y, s, SFLeft);
        }

        void DrawLabel(Graphics g, string text, Font f, SolidBrush b, float x, float y, Skin s, StringFormat sf)
        {
            if (s.Outline)
            {
                using (SolidBrush o = new SolidBrush(s.OutlineColor))
                {
                    float ow = 1.7f;
                    g.DrawString(text, f, o, x - ow, y, sf);
                    g.DrawString(text, f, o, x + ow, y, sf);
                    g.DrawString(text, f, o, x, y - ow, sf);
                    g.DrawString(text, f, o, x, y + ow, sf);
                    g.DrawString(text, f, o, x - ow * 0.8f, y - ow * 0.8f, sf);
                    g.DrawString(text, f, o, x + ow * 0.8f, y + ow * 0.8f, sf);
                }
            }
            g.DrawString(text, f, b, x, y, sf);
        }

        void DrawSparkle(Graphics g, float cx, float cy, float r, Color c)
        {
            PointF[] pts = new PointF[]
            {
                new PointF(cx, cy - r),
                new PointF(cx + r * 0.3f, cy - r * 0.3f),
                new PointF(cx + r, cy),
                new PointF(cx + r * 0.3f, cy + r * 0.3f),
                new PointF(cx, cy + r),
                new PointF(cx - r * 0.3f, cy + r * 0.3f),
                new PointF(cx - r, cy),
                new PointF(cx - r * 0.3f, cy - r * 0.3f)
            };
            using (GraphicsPath p = new GraphicsPath())
            using (SolidBrush b = new SolidBrush(c))
            {
                p.AddPolygon(pts);
                g.FillPath(b, p);
            }
        }

        void DrawMiniWhale(Graphics g, Rectangle r, Color body, Color belly, Color eye)
        {
            float w = r.Width;
            float h = r.Height;
            if (w < 4 || h < 4) return;

            PointF[] tail = new PointF[]
            {
                new PointF(r.X + w * 0.62f, r.Y + h * 0.50f),
                new PointF(r.X + w * 0.99f, r.Y + h * 0.16f),
                new PointF(r.X + w * 0.99f, r.Y + h * 0.86f)
            };
            using (SolidBrush b = new SolidBrush(body)) g.FillPolygon(b, tail);

            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddEllipse(r.X, r.Y + h * 0.22f, w * 0.76f, h * 0.66f);
                using (SolidBrush b = new SolidBrush(body)) g.FillPath(b, p);
            }

            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddEllipse(r.X + w * 0.10f, r.Y + h * 0.52f, w * 0.48f, h * 0.32f);
                using (SolidBrush b = new SolidBrush(belly)) g.FillPath(b, p);
            }

            float ew = Math.Max(1.4f, w * 0.085f);
            float eh = Math.Max(1.4f, h * 0.10f);
            using (SolidBrush b = new SolidBrush(eye))
                g.FillEllipse(b, r.X + w * 0.28f, r.Y + h * 0.44f, ew, eh);

            using (Pen pen = new Pen(Color.FromArgb(170, body), Math.Max(1f, w * 0.055f)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawArc(pen, r.X + w * 0.12f, r.Y + h * 0.30f, w * 0.44f, h * 0.34f, 200f, 80f);
            }
        }
        Color SkinColor(Color c)
        {
            int a = c.ToArgb();
            if (a == CRed.ToArgb()) return _skin.Bad;
            if (a == CGreen.ToArgb()) return _skin.Good;
            if (a == CYellow.ToArgb()) return _skin.Warn;
            if (a == CText.ToArgb()) return _skin.Value;
            return c;
        }

        string BuildHudSig()
        {
            double fl = (DateTime.Now - _flashAt).TotalMilliseconds;
            bool flashing = fl >= 0 && fl < 760;
            return _cfg.Theme + "|" + _w + "x" + _h + "|" + _shown.ToString("0") + "|" + _v1
                 + "|" + _l2 + "|" + _l3 + "|" + _r1 + "|" + _r3 + "|" + _low.ToString()
                 + "|" + ElapsedText() + "|" + (flashing ? ((int)fl).ToString() : "-")
                 + "|" + (_snap == null ? "n" : (_snap.Ok ? "o" : "e"));
        }

        void DrawHudCached(Graphics g)
        {
            if (_hudCache == null || _hudCache.Width != _w || _hudCache.Height != _h)
            {
                if (_hudCache != null) _hudCache.Dispose();
                _hudCache = new Bitmap(_w, _h, PixelFormat.Format32bppPArgb);
                _hudSig = "";
            }
            string sig = BuildHudSig();
            if (sig != _hudSig)
            {
                _hudSig = sig;
                using (Graphics hg = Graphics.FromImage(_hudCache))
                {
                    hg.Clear(Color.Transparent);
                    hg.SmoothingMode = SmoothingMode.AntiAlias;
                    hg.TextRenderingHint = TextRenderingHint.AntiAlias;
                    hg.InterpolationMode = InterpolationMode.Bilinear;
                    hg.PixelOffsetMode = PixelOffsetMode.Half;
                    DrawHud(hg);
                }
            }
            g.DrawImage(_hudCache, 0, 0);
        }
        void DrawHud(Graphics g)
        {
            Skin s = _skin;
            float d = (float)(_dpi * _cfg.Zoom);
            int pad = (int)Math.Round(4 * d);
            Rectangle rc = new Rectangle(pad, _petH + (int)Math.Round(10 * d), _w - pad * 2, _hudH - (int)Math.Round(16 * d));
            if (rc.Height < 20) return;

            int radius = (int)Math.Round(15 * d);
            int x0 = rc.Left + (int)Math.Round(13 * d);
            int xr = rc.Right - (int)Math.Round(13 * d);

            if (s.Panel)
            {
                Rectangle sh = rc;
                sh.Offset(0, (int)Math.Round(3 * d));
                using (GraphicsPath p = RoundRect(sh, radius))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(44, 70, 90, 170)))
                    g.FillPath(b, p);

                using (GraphicsPath p = RoundRect(rc, radius))
                {
                    using (SolidBrush b = new SolidBrush(s.Bg)) g.FillPath(b, p);
                    using (Pen pen = new Pen(s.Border, Math.Max(1f, 1.15f * d))) g.DrawPath(pen, p);
                }

                double ft = (DateTime.Now - _flashAt).TotalMilliseconds;
                if (ft >= 0 && ft < 720)
                {
                    int a = (int)(100 * (1 - ft / 720));
                    using (GraphicsPath p = RoundRect(rc, radius))
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(a, 255, 148, 178)))
                        g.FillPath(b, p);
                }

                if (s.Lace)
                {
                    int dot = (int)Math.Round(7 * d);
                    int gap = (int)Math.Round(4 * d);
                    for (int x = rc.Left + (int)Math.Round(12 * d); x < rc.Right - dot - (int)Math.Round(12 * d); x += dot + gap)
                    {
                        using (SolidBrush b = new SolidBrush(Color.FromArgb(85, s.Accent)))
                            g.FillEllipse(b, x, rc.Top - dot / 2, dot, dot);
                    }
                }

                if (s.Stars)
                {
                    DrawSparkle(g, rc.Right - 18 * d, rc.Top + 16 * d, 4.5f * d, Color.FromArgb(145, s.Accent));
                    DrawSparkle(g, rc.Left + 11 * d, rc.Bottom - 13 * d, 3f * d, Color.FromArgb(105, s.Accent));
                }
            }

            using (SolidBrush bTitle = new SolidBrush(s.Title))
            using (SolidBrush bDim = new SolidBrush(s.Dim))
            using (SolidBrush bBad = new SolidBrush(s.Bad))
            using (SolidBrush bWarn = new SolidBrush(s.Warn))
            using (SolidBrush bValue = new SolidBrush(_low ? s.Warn : s.Value))
            using (SolidBrush bDot = new SolidBrush(_snap == null ? s.Dim : (_snap.Ok ? s.Good : s.Bad)))
            {
                float y1 = rc.Top + (int)Math.Round(3 * d);
                int iconH = (int)Math.Round(20 * d);
                int iconW = (int)Math.Round(15 * d);
                DrawMiniWhale(g, new Rectangle(x0, (int)(y1 + 4 * d), iconW, (int)(iconH * 0.8)), s.Accent, Color.FromArgb(238, 255, 255, 255), s.Value);

                float xl = x0 + iconW + (int)Math.Round(5 * d);
                DrawLabel(g, "余额", _fLabel, bTitle, xl, y1 + (int)Math.Round(8 * d), s);
                float wl = g.MeasureString("余额", _fLabel).Width;

                string numText = _shown >= 0 ? Num((long)Math.Round(_shown)) : _v1;
                DrawLabel(g, numText, _fValue, bValue, xl + wl + (int)Math.Round(4 * d), y1, s);

                float y2 = rc.Top + (int)Math.Round(34 * d);
                DrawLabel(g, _l2, _fLine, _lastDelta > 0 ? bBad : bDim, x0, y2, s);
                DrawLabel(g, ElapsedText(), _fSmall, bDim, xr, y2 + (int)Math.Round(1 * d), s, SFRight);

                float y3 = rc.Top + (int)Math.Round(52 * d);
                DrawLabel(g, _l3, _fLine, (_snap != null && _snap.ExpiringRemain > 0) ? bWarn : bDim, x0, y3, s);
                DrawLabel(g, _r3, _fSmall, bDot, xr, y3 + (int)Math.Round(1 * d), s, SFRight);

            }
        }

        void DrawFloaters(Graphics g)
        {
            DateTime now = DateTime.Now;
            float d = (float)(_dpi * _cfg.Zoom);
            for (int i = _floaters.Count - 1; i >= 0; i--)
            {
                Floater f = _floaters[i];
                double age = (now - f.Born).TotalSeconds;
                if (age > f.Life) { _floaters.RemoveAt(i); continue; }
                double k = age / f.Life;
                int alpha = (int)(255 * (1 - k));
                if (alpha < 8) alpha = 8;
                float y = f.Y - (float)(30 * d * k);
                SizeF sz = g.MeasureString(f.Text, _fFloat);
                float x = (_w - sz.Width) / 2;
                Color col = SkinColor(f.Color);

                using (SolidBrush o = new SolidBrush(Color.FromArgb(Math.Min(alpha, Math.Min(238, 238)), 255, 255, 255)))
                {
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            g.DrawString(f.Text, _fFloat, o, x + dx * 1.7f, y + dy * 1.7f);
                        }
                }
                using (SolidBrush b = new SolidBrush(Color.FromArgb(alpha, col)))
                    g.DrawString(f.Text, _fFloat, b, x, y);
            }
        }
        static readonly StringFormat SFWrap = CreateWrapSf();

        static StringFormat CreateWrapSf()
        {
            StringFormat f = new StringFormat(StringFormatFlags.NoClip);
            f.Alignment = StringAlignment.Near;
            f.LineAlignment = StringAlignment.Near;
            return f;
        }

        void DrawBalloon(Graphics g)
        {
            if (DateTime.Now > _balloonUntil || _balloon.Length == 0) return;
            float d = (float)(_dpi * _cfg.Zoom);

            string text = _balloon;
            if (text.Length > 80) text = text.Substring(0, 80) + "…";

            int padX = (int)Math.Round(11 * d);
            int maxW = _w - (int)Math.Round(12 * d);
            SizeF sz = g.MeasureString(text, _fBalloon, maxW - padX * 2, SFWrap);
            int bw = (int)Math.Ceiling(sz.Width) + padX * 2;
            int bh = (int)Math.Ceiling(sz.Height) + (int)Math.Round(12 * d);
            if (bw > maxW) bw = maxW;
            if (bw < 70) bw = 70;

            double age = (DateTime.Now - _balloonAt).TotalSeconds;
            double k = Math.Min(1.0, age / 0.16);
            int lift = (int)Math.Round((1 - k) * 9 * d);

            int bx = (_w - bw) / 2;
            int by = (int)Math.Round(8 * d) - lift;
            if (by + bh > _petH) by = Math.Max(0, _petH - bh - (int)Math.Round(6 * d));
            Rectangle rc = new Rectangle(bx, by, bw, bh);
            int radius = (int)Math.Round(12 * d);

            Color edge = SkinColor(_balloonColor);
            int fade = (int)(255 * k);
            Color fill = Color.FromArgb(fade, 253, 252, 255);
            Color edgeC = Color.FromArgb(fade, edge);
            Color txt = Color.FromArgb(fade, 38, 54, 140);

            using (GraphicsPath p = RoundRect(new Rectangle(rc.X, rc.Y + (int)(2 * d), rc.Width, rc.Height), radius))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(fade * 36 / 255, 60, 80, 160)))
                g.FillPath(b, p);

            using (GraphicsPath p = RoundRect(rc, radius))
            {
                using (SolidBrush b = new SolidBrush(fill)) g.FillPath(b, p);
                using (Pen pen = new Pen(edgeC, Math.Max(1f, 1.2f * d))) g.DrawPath(pen, p);
            }

            int cx = rc.Left + rc.Width / 2;
            int tw = (int)Math.Round(9 * d);
            int th = (int)Math.Round(8 * d);
            Point[] tri = new Point[] {
                new Point(cx - tw, rc.Bottom - 1),
                new Point(cx + tw, rc.Bottom - 1),
                new Point(cx, rc.Bottom + th)
            };
            using (GraphicsPath tp = new GraphicsPath())
            {
                tp.AddPolygon(tri);
                using (SolidBrush b = new SolidBrush(fill)) g.FillPath(b, tp);
                using (Pen pen = new Pen(edgeC, Math.Max(1f, 1.2f * d)))
                {
                    g.DrawLine(pen, tri[0], tri[2]);
                    g.DrawLine(pen, tri[1], tri[2]);
                }
            }

            RectangleF tr = new RectangleF(rc.X + padX, rc.Y + (int)Math.Round(5 * d), rc.Width - padX * 2, rc.Height);
            using (SolidBrush b = new SolidBrush(txt))
                g.DrawString(text, _fBalloon, b, tr, SFWrap);
        }
        // ================= 明细 =================

        void ShowDetails()
        {
            try
            {
                if (_snap == null)
                {
                    ShowBalloon("还没拿到数据，先刷新一下…", CYellow, 3);
                    StartPoll();
                    return;
                }
                DetailsForm f = new DetailsForm(_snap, _fontName, _cfg);
                f.Show();
                Native.SetForegroundWindow(f.Handle);
            }
            catch (Exception ex)
            {
                ShowBalloon("打不开明细：" + ex.Message, CRed, 5);
            }
        }

        // ================= 释放 =================

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { if (_anim != null) { _anim.Stop(); _anim.Dispose(); } } catch { }
                try { if (_tray != null) { _tray.Visible = false; _tray.Dispose(); } } catch { }
                try { if (_menu != null) _menu.Dispose(); } catch { }
                try { if (_surface != null) _surface.Dispose(); } catch { }
                try { if (_hudCache != null) _hudCache.Dispose(); } catch { }
                try { if (_client != null) _client.Dispose(); } catch { }
                DisposeFonts();
                foreach (Bitmap b in _sprites.Values) { try { b.Dispose(); } catch { } }
                foreach (Bitmap b in _scaled.Values) { try { b.Dispose(); } catch { } }
                foreach (Bitmap b in _startRaw) { try { b.Dispose(); } catch { } }
                foreach (Bitmap b in _startScaled) { try { b.Dispose(); } catch { } }
                foreach (SoundPlayer p in _sfx.Values) { try { p.Dispose(); } catch { } }
            }
            base.Dispose(disposing);
        }
    }
}

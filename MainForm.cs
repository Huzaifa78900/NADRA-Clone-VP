using System.ComponentModel;
using System.Data;
using System.Text;
using Microsoft.Data.Sqlite;
using OpenCvSharp.Extensions;
using Cv = OpenCvSharp;

namespace NadraApp;

public class MainForm : Form
{
    // ---------- theme ----------
    static readonly Color Green = Color.FromArgb(0, 104, 56);
    static readonly Color DarkGreen = Color.FromArgb(0, 70, 38);
    static readonly Color Soft = Color.FromArgb(240, 246, 242);
    static readonly Color Red = Color.FromArgb(190, 50, 50);
    static readonly Color Blue = Color.FromArgb(30, 100, 170);
    static readonly Color Orange = Color.FromArgb(200, 130, 20);

    static readonly Dictionary<string, string[]> Cities = new()
    {
        ["Punjab"] = new[] { "Lahore", "Rawalpindi", "Faisalabad", "Multan", "Gujranwala" },
        ["Sindh"] = new[] { "Karachi", "Hyderabad", "Sukkur", "Larkana" },
        ["Khyber Pakhtunkhwa"] = new[] { "Peshawar", "Mardan", "Abbottabad", "Swat" },
        ["Balochistan"] = new[] { "Quetta", "Gwadar", "Turbat" },
        ["Islamabad Capital Territory"] = new[] { "Islamabad" },
        ["Gilgit-Baltistan"] = new[] { "Gilgit", "Skardu" },
        ["Azad Jammu & Kashmir"] = new[] { "Muzaffarabad", "Mirpur" },
    };
    static readonly Dictionary<string, char> ProvCode = new()
    {
        ["Khyber Pakhtunkhwa"] = '1', ["Punjab"] = '3', ["Sindh"] = '4', ["Balochistan"] = '5',
        ["Islamabad Capital Territory"] = '6', ["Gilgit-Baltistan"] = '7', ["Azad Jammu & Kashmir"] = '8'
    };
    static readonly Dictionary<string, string> ShortProv = new()
    {
        ["Khyber Pakhtunkhwa"] = "KPK", ["Islamabad Capital Territory"] = "ICT", ["Gilgit-Baltistan"] = "GB", ["Azad Jammu & Kashmir"] = "AJK"
    };

    // ---------- shell ----------
    TabControl tabs = new();
    MenuStrip menu = new();
    ToolStrip toolbar = new();
    StatusStrip status = new();
    ToolStripStatusLabel lblStatus = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    ToolStripStatusLabel lblCount = new();
    ToolStripStatusLabel lblLive = new() { Text = "● LIVE", ForeColor = Color.SeaGreen };
    ToolStripProgressBar progress = new() { Visible = false, Width = 150 };
    ToolTip tip = new();
    ErrorProvider err = new();
    Toast toast = new();
    AnimatedBackdrop header = new();
    BackgroundWorker backupWorker = new() { WorkerReportsProgress = true };
    System.Windows.Forms.Timer liveTimer = new() { Interval = 600 };
    System.Windows.Forms.Timer fadeTimer = new() { Interval = 15 };
    System.Windows.Forms.Timer searchDebounce = new() { Interval = 300 };
    Random rnd = new();
    bool refreshing;

    // ---------- dashboard ----------
    StatCard cTotal = new(), cMale = new(), cFemale = new(), cOverseas = new(), cSpecial = new();
    ListBox lstRecent = new();
    MonthCalendar calendar = new();
    BarChart chart = new();
    AnimatedMeter meterCapacity = new();
    TickerBar ticker = new();
    PictureBox picRadar = new();
    Label lblNodes = new();
    System.Windows.Forms.Timer radarTimer = new() { Interval = 40 };
    double radarAngle;
    (double ang, double rad, int w)[] blips = Array.Empty<(double, double, int)>();
    Font recentBold = new("Segoe UI Semibold", 10.5f), recentSmall = new("Segoe UI", 9f);

    // ---------- registration ----------
    MaskedTextBox mskCnic = new(), mskPhone = new();
    TextBox txtName = new(), txtFather = new(), txtAddress = new();
    ComboBox cmbGender = new(), cmbProvince = new(), cmbCity = new();
    DateTimePicker dtpDob = new();
    Label lblAge = new();
    RadioButton rbSingle = new(), rbMarried = new(), rbWidowed = new();
    DomainUpDown dudBlood = new();
    NumericUpDown numFamily = new();
    CheckBox chkDisabled = new(), chkOverseas = new();
    CheckedListBox clbServices = new();
    PictureBox picPhoto = new();
    LinkLabel lnkOpenCv = new();
    OpenFileDialog ofd = new();
    AnimatedMeter meterForm = new();
    Label lblMode = new();
    string photoPath;
    int editingId;

    // ---------- records ----------
    FastGrid grid = new();
    BindingSource bs = new();
    TextBox txtSearch = new();
    ComboBox cmbFilter = new();
    Label lblResults = new(), lblEmpty = new();
    ContextMenuStrip gridMenu = new();

    // ---------- tree ----------
    TreeView tree = new();
    ListView lv = new();
    ImageList imgs = new();
    SplitContainer split = new();

    // ---------- OpenCV ----------
    PictureBox picOrig = new(), picFx = new();
    ComboBox cmbFx = new();
    TrackBar trkIntensity = new();
    CheckBox chkScan = new();
    Label lblIntensity = new();
    ModernButton btnCam;
    System.Windows.Forms.Timer scanTimer = new() { Interval = 40 };
    System.Windows.Forms.Timer camTimer = new() { Interval = 40 };
    Cv.VideoCapture cap;
    Cv.Mat cvSrc, cvFx;
    int scanY;

    public MainForm()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "NADRA - National Database & Registration Authority | Citizen Management System";
        ClientSize = new Size(1240, 800);
        MinimumSize = new Size(1150, 760);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10f);
        BackColor = Soft;
        KeyPreview = true;
        DoubleBuffered = true;
        Opacity = 0;

        tabs.Dock = DockStyle.Fill;
        tabs.Font = new Font("Segoe UI Semibold", 10.5f);
        tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabs.ItemSize = new Size(190, 36);
        tabs.SizeMode = TabSizeMode.Fixed;
        tabs.DrawItem += DrawTab;
        tabs.SelectedIndexChanged += (s, e) =>
        {
            if (tabs.SelectedIndex == 0) { chart.Replay(); foreach (var c in new[] { cTotal, cMale, cFemale, cOverseas, cSpecial }) c.Replay(); }
            if (tabs.SelectedIndex != 4 && cap != null) StopCamera();
        };

        AddTab("Dashboard", BuildDashboard());
        AddTab("Registration", BuildRegistration());
        AddTab("Citizen Records", BuildRecords());
        AddTab("Geographic Tree", BuildTree());
        AddTab("OpenCV Photo Lab", BuildOpenCv());
        AddTab("Requirements", BuildRequirements());

        header.Dock = DockStyle.Top; header.Height = 78; header.Title = "NADRA  |  Citizen Management System";
        header.Subtitle = "National Database & Registration Authority - Government of Pakistan"; header.ShowClock = true; header.Count = 30;

        Controls.Add(tabs);
        Controls.Add(header);
        BuildToolbar(); Controls.Add(toolbar);
        BuildMenu(); Controls.Add(menu);
        BuildStatus(); Controls.Add(status);
        MainMenuStrip = menu;
        toast.BottomMargin = status.Height + 14;
        Controls.Add(toast);

        // timers
        liveTimer.Tick += (s, e) => lblLive.ForeColor = lblLive.ForeColor == Color.SeaGreen ? Color.LightGray : Color.SeaGreen;
        liveTimer.Start();
        radarTimer.Tick += RadarTick; radarTimer.Start();
        scanTimer.Tick += (s, e) => { if (cap != null || cvFx == null || tabs.SelectedIndex != 4) return; scanY += 7; Present(); };
        camTimer.Tick += CamTick;
        searchDebounce.Tick += (s, e) => { searchDebounce.Stop(); Safe(LoadGrid, "Search"); };
        backupWorker.DoWork += BackupWork;
        backupWorker.ProgressChanged += (s, e) => progress.Value = e.ProgressPercentage;
        backupWorker.RunWorkerCompleted += (s, e) =>
        {
            progress.Visible = false;
            if (e.Error == null) toast.ShowMessage("Backup completed successfully.", Green);
            else { Program.Log(e.Error); toast.ShowMessage("Backup failed: " + e.Error.Message, Red); }
        };
        fadeTimer.Tick += (s, e) => { Opacity = Math.Min(1, Opacity + 0.07); if (Opacity >= 1) fadeTimer.Stop(); };

        Safe(RefreshAll, "Loading data");
        LoadSampleIntoLab();
    }

    protected override void OnShown(EventArgs e) { base.OnShown(e); fadeTimer.Start(); toast.ShowMessage("Welcome to NADRA Citizen Management System", Green); }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        StopCamera();
        foreach (var t in new[] { liveTimer, radarTimer, scanTimer, camTimer, searchDebounce, fadeTimer }) t.Stop();
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        cvSrc?.Dispose(); cvFx?.Dispose();
        base.OnFormClosed(e);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.N: NewRegistration(); return true;
            case Keys.Control | Keys.S: if (tabs.SelectedIndex == 1) SaveCitizen(); return true;
            case Keys.F5: Safe(RefreshAll, "Refresh"); toast.ShowMessage("Data refreshed", Green); return true;
            case Keys.Control | Keys.F: tabs.SelectedIndex = 2; txtSearch.Focus(); return true;
            case Keys.Control | Keys.D1: tabs.SelectedIndex = 0; return true;
            case Keys.Control | Keys.D2: tabs.SelectedIndex = 1; return true;
            case Keys.Control | Keys.D3: tabs.SelectedIndex = 2; return true;
            case Keys.Control | Keys.D4: tabs.SelectedIndex = 3; return true;
            case Keys.Control | Keys.D5: tabs.SelectedIndex = 4; return true;
            case Keys.Control | Keys.D6: tabs.SelectedIndex = 5; return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // =====================================================================
    //  helpers
    // =====================================================================
    void Safe(Action a, string what)
    {
        try { a(); }
        catch (Exception ex) { Program.Log(ex); toast.ShowMessage($"{what} failed: {ex.Message}", Red); }
    }

    void AddTab(string title, Control content)
    {
        var tp = new TabPage(title) { BackColor = Soft };
        content.Dock = DockStyle.Fill; tp.Controls.Add(content); tabs.TabPages.Add(tp);
    }

    void DrawTab(object s, DrawItemEventArgs e)
    {
        bool sel = e.Index == tabs.SelectedIndex;
        using var bg = new SolidBrush(sel ? Green : Color.FromArgb(225, 232, 227));
        e.Graphics.FillRectangle(bg, e.Bounds);
        if (sel) using (var br = new SolidBrush(Color.FromArgb(120, 255, 190))) e.Graphics.FillRectangle(br, e.Bounds.X, e.Bounds.Bottom - 3, e.Bounds.Width, 3);
        TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, tabs.Font, e.Bounds, sel ? Color.White : DarkGreen,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    ModernButton Btn(string text, Color c, int w = 130) => new() { Text = text, Accent = c, Width = w };

    T Place<T>(Control parent, string label, T c, int x, int y, int w) where T : Control
    {
        if (label != null) parent.Controls.Add(new Label { Text = label, Left = x, Top = y + 4, AutoSize = true });
        c.Left = label == null ? x : x + 130; c.Top = y; c.Width = label == null ? w : w - 130;
        parent.Controls.Add(c);
        return c;
    }

    Section Sec(string title, int x, int y, int w, int h, Color? accent = null)
        => new() { Title = title, Left = x, Top = y, Width = w, Height = h, Accent = accent ?? Green };

    // =====================================================================
    //  shell: menu / toolbar / status
    // =====================================================================
    void BuildMenu()
    {
        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add("Export records to CSV...", null, (s, e) => ExportCsv());
        file.DropDownItems.Add("Backup database...", null, (s, e) => StartBackup());
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add("E&xit", null, (s, e) => Close());

        var rec = new ToolStripMenuItem("&Records");
        rec.DropDownItems.Add("New registration\tCtrl+N", null, (s, e) => NewRegistration());
        rec.DropDownItems.Add("Find...\tCtrl+F", null, (s, e) => { tabs.SelectedIndex = 2; txtSearch.Focus(); });
        rec.DropDownItems.Add("Refresh\tF5", null, (s, e) => Safe(RefreshAll, "Refresh"));

        var view = new ToolStripMenuItem("&View");
        view.DropDownItems.Add("Replay dashboard animations", null, (s, e) => { tabs.SelectedIndex = 0; chart.Replay(); });
        view.DropDownItems.Add("Change background colour...", null, (s, e) =>
        {
            using var cd = new ColorDialog();
            if (cd.ShowDialog() == DialogResult.OK) BackColor = ControlPaint.Light(cd.Color, 0.9f);
        });

        var tools = new ToolStripMenuItem("&Tools");
        tools.DropDownItems.Add("OpenCV Photo Lab\tCtrl+5", null, (s, e) => tabs.SelectedIndex = 4);

        var help = new ToolStripMenuItem("&Help");
        help.DropDownItems.Add("Requirements (SRS)\tCtrl+6", null, (s, e) => tabs.SelectedIndex = 5);
        help.DropDownItems.Add("About", null, (s, e) => MessageBox.Show("NADRA Citizen Management System v2.0\nWinForms + SQLite + OpenCvSharp\nAcademic project.", "About", MessageBoxButtons.OK, MessageBoxIcon.Information));

        menu.Items.AddRange(new ToolStripItem[] { file, rec, view, tools, help });
        menu.Dock = DockStyle.Top;
    }

    void BuildToolbar()
    {
        toolbar.Dock = DockStyle.Top; toolbar.GripStyle = ToolStripGripStyle.Hidden; toolbar.BackColor = Color.White;
        toolbar.Items.Add(new ToolStripButton("＋ New", null, (s, e) => NewRegistration()) { ToolTipText = "New citizen registration (Ctrl+N)" });
        toolbar.Items.Add(new ToolStripButton("💾 Save", null, (s, e) => { tabs.SelectedIndex = 1; SaveCitizen(); }) { ToolTipText = "Save current form (Ctrl+S)" });
        toolbar.Items.Add(new ToolStripButton("⟳ Refresh", null, (s, e) => { Safe(RefreshAll, "Refresh"); toast.ShowMessage("Data refreshed", Green); }) { ToolTipText = "Reload data (F5)" });
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(new ToolStripButton("⇪ Export CSV", null, (s, e) => ExportCsv()));
        toolbar.Items.Add(new ToolStripButton("🛈 Backup", null, (s, e) => StartBackup()));
        toolbar.Items.Add(new ToolStripSeparator());
        var quick = new ToolStripTextBox { Width = 170, ToolTipText = "Quick search by CNIC / name, press Enter" };
        quick.KeyDown += (s, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            txtSearch.Text = quick.Text; tabs.SelectedIndex = 2; e.SuppressKeyPress = true;
        };
        toolbar.Items.Add(new ToolStripLabel("Quick search: "));
        toolbar.Items.Add(quick);
    }

    void BuildStatus()
    {
        status.Items.AddRange(new ToolStripItem[] { lblStatus, progress, lblCount, new ToolStripSeparator(), lblLive });
        lblStatus.Text = "Ready - tip: press Ctrl+N to register a new citizen.";
    }

    // =====================================================================
    //  Dashboard
    // =====================================================================
    Control BuildDashboard()
    {
        var root = new AnimatedBackdrop { Gradient = false, C1 = Soft, BubbleColor = Green, Count = 24, Padding = new Padding(12, 8, 12, 12) };

        ticker.Dock = DockStyle.Top;

        var cards = new BufferedTable { Dock = DockStyle.Top, Height = 108, ColumnCount = 5, RowCount = 1, BackColor = Color.Transparent, Padding = new Padding(0, 10, 0, 4) };
        for (int i = 0; i < 5; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        void Add(StatCard c, string title, string glyph, Color col, int i)
        {
            c.Title = title; c.Glyph = glyph; c.Accent = col; c.Dock = DockStyle.Fill; c.Margin = new Padding(0, 0, i == 4 ? 0 : 10, 0);
            cards.Controls.Add(c, i, 0);
        }
        Add(cTotal, "Total Citizens", "☻", Green, 0);
        Add(cMale, "Male", "♂", Blue, 1);
        Add(cFemale, "Female", "♀", Color.FromArgb(170, 60, 130), 2);
        Add(cOverseas, "Overseas (NICOP)", "✈", Orange, 3);
        Add(cSpecial, "Special Citizens", "♿", Red, 4);

        var table = new BufferedTable { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Color.Transparent, Padding = new Padding(0, 8, 0, 0) };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

        var s1 = Sec("Citizens by Province", 0, 0, 10, 10); s1.Dock = DockStyle.Fill; s1.Margin = new Padding(0, 0, 10, 0);
        chart.Dock = DockStyle.Fill; s1.Controls.Add(chart);

        var s2 = Sec("Recent Registrations", 0, 0, 10, 10, Blue); s2.Dock = DockStyle.Fill; s2.Margin = new Padding(0, 0, 10, 0);
        lstRecent.Dock = DockStyle.Fill; lstRecent.BorderStyle = BorderStyle.None; lstRecent.DrawMode = DrawMode.OwnerDrawFixed; lstRecent.ItemHeight = 44;
        lstRecent.DrawItem += DrawRecent;
        lstRecent.DoubleClick += (s, e) => { if (lstRecent.SelectedItem != null) { txtSearch.Text = lstRecent.SelectedItem.ToString().Split('|')[1]; tabs.SelectedIndex = 2; } };
        var capPanel = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = Color.White };
        meterCapacity.Caption = "Database capacity (demo)"; meterCapacity.Dock = DockStyle.Fill; meterCapacity.Height = 26;
        capPanel.Padding = new Padding(0, 8, 0, 0); capPanel.Controls.Add(meterCapacity);
        s2.Controls.Add(lstRecent); s2.Controls.Add(capPanel);
        tip.SetToolTip(lstRecent, "Double-click a citizen to open them in Records");

        var right = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var s3 = Sec("Live Network Monitor (OpenCV)", 0, 0, 10, 10, Color.SeaGreen); s3.Dock = DockStyle.Fill;
        picRadar.Dock = DockStyle.Fill; picRadar.SizeMode = PictureBoxSizeMode.Zoom; picRadar.BackColor = Color.FromArgb(22, 38, 20);
        lblNodes.Dock = DockStyle.Bottom; lblNodes.Height = 24; lblNodes.TextAlign = ContentAlignment.MiddleCenter; lblNodes.ForeColor = Green;
        s3.Controls.Add(picRadar); s3.Controls.Add(lblNodes);
        var s4 = Sec("Calendar", 0, 0, 10, 205, Orange); s4.Dock = DockStyle.Bottom; s4.Margin = new Padding(0);
        calendar.Left = 12; calendar.Top = 36; calendar.MaxSelectionCount = 1;
        s4.Controls.Add(calendar);
        var gap = new Panel { Dock = DockStyle.Bottom, Height = 10, BackColor = Color.Transparent };
        right.Controls.Add(s3); right.Controls.Add(gap); right.Controls.Add(s4);

        table.Controls.Add(s1, 0, 0); table.Controls.Add(s2, 1, 0); table.Controls.Add(right, 2, 0);
        root.Controls.Add(table); root.Controls.Add(cards); root.Controls.Add(ticker);
        return root;
    }

    void DrawRecent(object s, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        var g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var parts = lstRecent.Items[e.Index].ToString().Split('|');
        bool sel = (e.State & DrawItemState.Selected) != 0;
        using (var bg = new SolidBrush(sel ? Color.FromArgb(205, 232, 215) : e.Index % 2 == 0 ? Color.White : Color.FromArgb(246, 250, 247))) g.FillRectangle(bg, e.Bounds);
        var c = new Rectangle(e.Bounds.X + 8, e.Bounds.Y + 7, 30, 30);
        Color[] pal = { Green, Blue, Orange, Red, Color.FromArgb(120, 80, 160) };
        using (var br = new SolidBrush(pal[e.Index % pal.Length])) g.FillEllipse(br, c);
        TextRenderer.DrawText(g, parts[0].Substring(0, 1).ToUpper(), recentBold, c, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(g, parts[0], recentBold, new Point(e.Bounds.X + 48, e.Bounds.Y + 4), Color.FromArgb(30, 30, 30));
        TextRenderer.DrawText(g, parts[1] + "  •  " + parts[2], recentSmall, new Point(e.Bounds.X + 48, e.Bounds.Y + 24), Color.Gray);
    }

    void LoadDashboard()
    {
        cTotal.Value = Db.Scalar("SELECT COUNT(*) FROM Citizens");
        cMale.Value = Db.Scalar("SELECT COUNT(*) FROM Citizens WHERE Gender='Male'");
        cFemale.Value = Db.Scalar("SELECT COUNT(*) FROM Citizens WHERE Gender='Female'");
        cOverseas.Value = Db.Scalar("SELECT COUNT(*) FROM Citizens WHERE Overseas=1");
        cSpecial.Value = Db.Scalar("SELECT COUNT(*) FROM Citizens WHERE Disabled=1");

        var list = new List<(string, int)>();
        foreach (DataRow r in Db.Query("SELECT Province, COUNT(*) c FROM Citizens GROUP BY Province ORDER BY c DESC").Rows)
        {
            string p = r[0].ToString();
            list.Add((ShortProv.TryGetValue(p, out var sp) ? sp : p, Convert.ToInt32(r[1])));
        }
        chart.SetData(list);

        var bl = new List<(double, double, int)>();
        for (int i = 0; i < list.Count; i++) bl.Add((i * 51 + 20, 0.30 + 0.13 * ((i * 3) % 5), 3 + Math.Min(7, list[i].Item2)));
        blips = bl.ToArray();
        lblNodes.Text = $"● {list.Count} regional nodes online";

        lstRecent.Items.Clear();
        foreach (DataRow r in Db.Query("SELECT FullName, CNIC, City FROM Citizens ORDER BY Id DESC LIMIT 12").Rows)
            lstRecent.Items.Add($"{r[0]}|{r[1]}|{r[2]}");

        meterCapacity.Value = (int)Math.Min(100, cTotal.Value);
        ticker.Message = $"NADRA ADVISORY: Always verify your CNIC details before submission   ★   Registered citizens: {cTotal.Value}   ★   " +
                         "Tip: press Ctrl+N for a new registration and Ctrl+F to search   ★   Try the OpenCV Photo Lab for live camera effects   ★   Keep your CNIC safe and never share it";
    }

    void RadarTick(object s, EventArgs e)
    {
        if (tabs.SelectedIndex != 0 || !picRadar.Visible || picRadar.Width < 20) return;
        radarAngle = (radarAngle + 4) % 360;
        try
        {
            using var m = ImageFx.Radar(300, radarAngle, blips);
            var old = picRadar.Image;
            picRadar.Image = BitmapConverter.ToBitmap(m);
            old?.Dispose();
        }
        catch (Exception ex) { Program.Log(ex); radarTimer.Stop(); }
    }

    // =====================================================================
    //  Registration
    // =====================================================================
    Control BuildRegistration()
    {
        var root = new Panel { AutoScroll = true, BackColor = Soft };
        lblMode.Text = "NEW REGISTRATION"; lblMode.Font = new Font("Segoe UI Semibold", 12f); lblMode.ForeColor = Green; lblMode.Left = 14; lblMode.Top = 4; lblMode.AutoSize = true;
        root.Controls.Add(lblMode);

        // personal
        var gp = Sec("Personal Information", 10, 30, 545, 395);
        mskCnic.Mask = "00000-0000000-0"; mskCnic.PromptChar = '_';
        Place(gp, "CNIC No. *", mskCnic, 15, 40, 500);
        var btnGen = Btn("Auto-generate CNIC", Blue, 160); btnGen.Height = 28; btnGen.Left = 145; btnGen.Top = 72; btnGen.Click += (s, e) => GenerateCnic();
        gp.Controls.Add(btnGen);
        Place(gp, "Full Name *", txtName, 15, 110, 500);
        Place(gp, "Father's Name *", txtFather, 15, 148, 500);
        cmbGender.DropDownStyle = ComboBoxStyle.DropDownList; cmbGender.Items.AddRange(new object[] { "Male", "Female", "Transgender" }); cmbGender.SelectedIndex = 0;
        Place(gp, "Gender", cmbGender, 15, 186, 500);
        dtpDob.Format = DateTimePickerFormat.Custom; dtpDob.CustomFormat = "dd MMMM yyyy"; dtpDob.MaxDate = DateTime.Today; dtpDob.Value = new DateTime(1995, 1, 1);
        Place(gp, "Date of Birth", dtpDob, 15, 224, 345);
        lblAge.Left = 372; lblAge.Top = 228; lblAge.AutoSize = true; gp.Controls.Add(lblAge);
        dtpDob.ValueChanged += (s, e) => UpdateAge();

        gp.Controls.Add(new Label { Text = "Marital Status", Left = 15, Top = 268, AutoSize = true });
        var fl = new FlowLayoutPanel { Left = 145, Top = 262, Width = 380, Height = 32 };
        rbSingle.Text = "Single"; rbSingle.Checked = true; rbSingle.AutoSize = true;
        rbMarried.Text = "Married"; rbMarried.AutoSize = true; rbWidowed.Text = "Widowed"; rbWidowed.AutoSize = true;
        fl.Controls.AddRange(new Control[] { rbSingle, rbMarried, rbWidowed }); gp.Controls.Add(fl);

        dudBlood.Items.AddRange(new object[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" }); dudBlood.SelectedIndex = 0; dudBlood.ReadOnly = true;
        Place(gp, "Blood Group", dudBlood, 15, 304, 500);
        numFamily.Minimum = 1; numFamily.Maximum = 30; numFamily.Value = 4;
        Place(gp, "Family Members", numFamily, 15, 342, 500);
        root.Controls.Add(gp);

        // address
        var ga = Sec("Address & Contact", 565, 30, 420, 305, Blue);
        cmbProvince.DropDownStyle = ComboBoxStyle.DropDownList; cmbProvince.Items.AddRange(Cities.Keys.ToArray());
        cmbProvince.SelectedIndexChanged += (s, e) =>
        {
            cmbCity.Items.Clear();
            if (cmbProvince.SelectedItem != null) cmbCity.Items.AddRange(Cities[cmbProvince.SelectedItem.ToString()]);
            if (cmbCity.Items.Count > 0) cmbCity.SelectedIndex = 0;
        };
        Place(ga, "Province", cmbProvince, 15, 40, 390);
        cmbCity.DropDownStyle = ComboBoxStyle.DropDownList; Place(ga, "City", cmbCity, 15, 78, 390);
        txtAddress.Multiline = true; txtAddress.Height = 70; txtAddress.ScrollBars = ScrollBars.Vertical;
        Place(ga, "Permanent Address", txtAddress, 15, 116, 390);
        mskPhone.Mask = "0000-0000000"; mskPhone.PromptChar = '_';
        Place(ga, "Mobile No.", mskPhone, 15, 200, 390);
        chkDisabled.Text = "Special citizen (disability)"; chkDisabled.AutoSize = true; chkDisabled.Left = 145; chkDisabled.Top = 242;
        chkOverseas.Text = "Overseas Pakistani (NICOP)"; chkOverseas.AutoSize = true; chkOverseas.Left = 145; chkOverseas.Top = 270;
        ga.Controls.AddRange(new Control[] { chkDisabled, chkOverseas });
        root.Controls.Add(ga);
        cmbProvince.SelectedIndex = 1;

        // services
        var gs = Sec("Services Requested", 565, 345, 420, 80, Orange);
        clbServices.Items.AddRange(new object[] { "Smart Card", "Passport", "Driving Licence", "B-Form", "Family Tree" });
        clbServices.MultiColumn = true; clbServices.ColumnWidth = 130; clbServices.CheckOnClick = true; clbServices.BorderStyle = BorderStyle.None;
        clbServices.Left = 12; clbServices.Top = 36; clbServices.Width = 395; clbServices.Height = 38; clbServices.SetItemChecked(0, true);
        gs.Controls.Add(clbServices); root.Controls.Add(gs);

        // photo
        var gph = Sec("Photograph", 995, 30, 200, 395, Color.DarkCyan);
        picPhoto.Left = 20; picPhoto.Top = 42; picPhoto.Width = 160; picPhoto.Height = 190; picPhoto.SizeMode = PictureBoxSizeMode.Zoom; picPhoto.BorderStyle = BorderStyle.FixedSingle; picPhoto.BackColor = Color.Gainsboro;
        var btnPhoto = Btn("Browse...", Green, 160); btnPhoto.Left = 20; btnPhoto.Top = 244; btnPhoto.Click += (s, e) => BrowsePhoto();
        var btnClearPhoto = Btn("Remove", Color.Gray, 160); btnClearPhoto.Left = 20; btnClearPhoto.Top = 290; btnClearPhoto.Height = 30; btnClearPhoto.Click += (s, e) => SetPhoto(null);
        lnkOpenCv.Text = "Apply OpenCV effects ➜"; lnkOpenCv.Left = 22; lnkOpenCv.Top = 336; lnkOpenCv.AutoSize = true;
        lnkOpenCv.LinkClicked += (s, e) => SendPhotoToLab(photoPath);
        gph.Controls.AddRange(new Control[] { picPhoto, btnPhoto, btnClearPhoto, lnkOpenCv });
        root.Controls.Add(gph);
        ofd.Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp";

        // actions + live completeness meter
        var bSave = Btn("💾  Save Record", Green, 170); bSave.Left = 10; bSave.Top = 440; bSave.Click += (s, e) => SaveCitizen();
        var bClear = Btn("Clear Form", Color.Gray, 140); bClear.Left = 190; bClear.Top = 440; bClear.Click += (s, e) => NewRegistration();
        meterForm.Caption = "Form completion"; meterForm.Left = 350; meterForm.Top = 447; meterForm.Width = 400; meterForm.Height = 26;
        root.Controls.AddRange(new Control[] { bSave, bClear, meterForm });

        foreach (var c in new Control[] { mskCnic, mskPhone, txtName, txtFather, txtAddress }) c.TextChanged += (s, e) => UpdateCompleteness();
        cmbCity.SelectedIndexChanged += (s, e) => UpdateCompleteness();
        UpdateAge(); UpdateCompleteness();

        tip.SetToolTip(mskCnic, "13-digit CNIC: 5 digits (area) - 7 digits (family) - 1 digit (odd = male, even = female)");
        tip.SetToolTip(txtName, "Name exactly as on birth certificate");
        tip.SetToolTip(picPhoto, "Applicant photograph");
        tip.SetToolTip(bSave, "Insert (or update) the citizen record - Ctrl+S");
        err.BlinkStyle = ErrorBlinkStyle.NeverBlink;
        return root;
    }

    int CalcAge()
    {
        int age = DateTime.Today.Year - dtpDob.Value.Year;
        if (dtpDob.Value.Date > DateTime.Today.AddYears(-age)) age--;
        return age;
    }

    void UpdateAge()
    {
        int a = CalcAge();
        lblAge.Text = $"Age: {a} yrs" + (a < 18 ? " (minor)" : "");
        lblAge.ForeColor = a < 18 ? Red : Green;
    }

    void UpdateCompleteness()
    {
        int done = 0;
        if (mskCnic.MaskCompleted) done++;
        if (!string.IsNullOrWhiteSpace(txtName.Text)) done++;
        if (!string.IsNullOrWhiteSpace(txtFather.Text)) done++;
        if (cmbCity.SelectedItem != null) done++;
        if (mskPhone.MaskCompleted) done++;
        if (!string.IsNullOrWhiteSpace(txtAddress.Text)) done++;
        if (!string.IsNullOrEmpty(photoPath)) done++;
        meterForm.Value = done * 100 / 7;
    }

    void GenerateCnic()
    {
        char pc = ProvCode[cmbProvince.SelectedItem?.ToString() ?? "Punjab"];
        string area = pc + rnd.Next(0, 10000).ToString("D4");
        string fam = rnd.Next(0, 10000000).ToString("D7");
        int last = cmbGender.SelectedIndex == 0 ? new[] { 1, 3, 5, 7, 9 }[rnd.Next(5)] : new[] { 0, 2, 4, 6, 8 }[rnd.Next(5)];
        mskCnic.Text = area + fam + last;
        toast.ShowMessage("CNIC generated for " + cmbProvince.Text, Blue);
    }

    void BrowsePhoto() { if (ofd.ShowDialog() == DialogResult.OK) SetPhoto(ofd.FileName); }

    void SetPhoto(string path)
    {
        var old = picPhoto.Image; picPhoto.Image = null; old?.Dispose();
        photoPath = null;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var im = Image.FromStream(fs);
                picPhoto.Image = new Bitmap(im);
                photoPath = path;
            }
            catch (Exception ex) { Program.Log(ex); toast.ShowMessage("That file is not a valid image.", Red); }
        }
        UpdateCompleteness();
    }

    static string StorePhoto(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var dir = Path.Combine(AppContext.BaseDirectory, "Photos"); Directory.CreateDirectory(dir);
        if (Path.GetFullPath(path).StartsWith(dir, StringComparison.OrdinalIgnoreCase)) return path;
        var dest = Path.Combine(dir, Guid.NewGuid().ToString("N") + Path.GetExtension(path));
        File.Copy(path, dest); return dest;
    }

    bool ValidateForm()
    {
        err.Clear(); bool ok = true;
        if (!mskCnic.MaskCompleted) { err.SetError(mskCnic, "Enter all 13 digits of the CNIC"); ok = false; }
        if (string.IsNullOrWhiteSpace(txtName.Text)) { err.SetError(txtName, "Name is required"); ok = false; }
        if (string.IsNullOrWhiteSpace(txtFather.Text)) { err.SetError(txtFather, "Father's name is required"); ok = false; }
        if (CalcAge() < 18) { err.SetError(dtpDob, "Applicant must be 18+ for a CNIC (use B-Form for minors)"); ok = false; }
        if (cmbCity.SelectedItem == null) { err.SetError(cmbCity, "Select a city"); ok = false; }
        if (!mskPhone.MaskCompleted) { err.SetError(mskPhone, "Enter a valid mobile number"); ok = false; }
        return ok;
    }

    void SaveCitizen()
    {
        tabs.SelectedIndex = 1;
        if (!ValidateForm()) { lblStatus.Text = "Please correct the highlighted fields."; toast.ShowMessage("Please correct the highlighted fields", Red); return; }
        try
        {
            string marital = rbSingle.Checked ? "Single" : rbMarried.Checked ? "Married" : "Widowed";
            string services = string.Join(", ", clbServices.CheckedItems.Cast<object>());
            string stored = StorePhoto(photoPath);
            var p = new (string, object)[]
            {
                ("@c", mskCnic.Text), ("@n", txtName.Text.Trim()), ("@f", txtFather.Text.Trim()), ("@g", cmbGender.Text),
                ("@d", dtpDob.Value.ToString("yyyy-MM-dd")), ("@m", marital), ("@b", dudBlood.Text),
                ("@p", cmbProvince.Text), ("@ci", cmbCity.Text), ("@a", txtAddress.Text.Trim()), ("@ph", mskPhone.Text),
                ("@fm", (int)numFamily.Value), ("@dis", chkDisabled.Checked ? 1 : 0), ("@ov", chkOverseas.Checked ? 1 : 0),
                ("@s", services), ("@pic", stored), ("@cr", DateTime.Now.ToString("yyyy-MM-dd HH:mm")), ("@id", editingId)
            };
            bool isNew = editingId == 0;
            if (isNew)
                Db.Exec(@"INSERT INTO Citizens(CNIC,FullName,FatherName,Gender,DOB,Marital,BloodGroup,Province,City,Address,Phone,Family,Disabled,Overseas,Services,Photo,Created)
                          VALUES(@c,@n,@f,@g,@d,@m,@b,@p,@ci,@a,@ph,@fm,@dis,@ov,@s,@pic,@cr)", p.Take(17).ToArray());
            else
                Db.Exec(@"UPDATE Citizens SET CNIC=@c,FullName=@n,FatherName=@f,Gender=@g,DOB=@d,Marital=@m,BloodGroup=@b,Province=@p,City=@ci,
                          Address=@a,Phone=@ph,Family=@fm,Disabled=@dis,Overseas=@ov,Services=@s,Photo=@pic WHERE Id=@id",
                          p.Where(x => x.Item1 != "@cr").ToArray());
            string name = txtName.Text.Trim();
            NewRegistration(); Safe(RefreshAll, "Refresh");
            tabs.SelectedIndex = 0;
            toast.ShowMessage((isNew ? "Citizen registered: " : "Record updated: ") + name, Green);
            lblStatus.Text = "Saved: " + name;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            err.SetError(mskCnic, "This CNIC already exists in the database");
            toast.ShowMessage("A citizen with this CNIC is already registered.", Red);
        }
        catch (Exception ex) { Program.Log(ex); toast.ShowMessage("Could not save: " + ex.Message, Red); }
    }

    void NewRegistration()
    {
        editingId = 0; lblMode.Text = "NEW REGISTRATION"; err.Clear();
        mskCnic.Clear(); mskPhone.Clear(); txtName.Clear(); txtFather.Clear(); txtAddress.Clear();
        cmbGender.SelectedIndex = 0; dtpDob.Value = new DateTime(1995, 1, 1); rbSingle.Checked = true;
        dudBlood.SelectedIndex = 0; numFamily.Value = 4; chkDisabled.Checked = chkOverseas.Checked = false;
        for (int i = 0; i < clbServices.Items.Count; i++) clbServices.SetItemChecked(i, i == 0);
        cmbProvince.SelectedIndex = 1; SetPhoto(null);
        tabs.SelectedIndex = 1; txtName.Focus();
    }

    void EditSelected()
    {
        if (grid.CurrentRow?.DataBoundItem is not DataRowView v) { toast.ShowMessage("Select a record first", Orange); return; }
        var r = v.Row;
        editingId = Convert.ToInt32(r["Id"]); lblMode.Text = "EDITING RECORD #" + editingId;
        mskCnic.Text = r["CNIC"].ToString(); txtName.Text = r["FullName"].ToString(); txtFather.Text = r["FatherName"].ToString();
        cmbGender.Text = r["Gender"].ToString();
        dtpDob.Value = DateTime.TryParse(r["DOB"].ToString(), out var d) && d <= dtpDob.MaxDate && d >= dtpDob.MinDate ? d : DateTime.Today.AddYears(-20);
        string m = r["Marital"].ToString(); rbSingle.Checked = m == "Single"; rbMarried.Checked = m == "Married"; rbWidowed.Checked = m == "Widowed";
        dudBlood.Text = r["BloodGroup"].ToString(); cmbProvince.Text = r["Province"].ToString(); cmbCity.Text = r["City"].ToString();
        txtAddress.Text = r["Address"].ToString(); mskPhone.Text = r["Phone"].ToString();
        numFamily.Value = Math.Clamp(Convert.ToInt32(r["Family"]), 1, 30);
        chkDisabled.Checked = Convert.ToInt32(r["Disabled"]) == 1; chkOverseas.Checked = Convert.ToInt32(r["Overseas"]) == 1;
        var sv = r["Services"].ToString();
        for (int i = 0; i < clbServices.Items.Count; i++) clbServices.SetItemChecked(i, sv.Contains(clbServices.Items[i].ToString()));
        SetPhoto(r["Photo"] == DBNull.Value ? null : r["Photo"].ToString());
        tabs.SelectedIndex = 1;
    }

    // =====================================================================
    //  Records
    // =====================================================================
    Control BuildRecords()
    {
        var root = new Panel { Padding = new Padding(10), BackColor = Soft };
        var top = new Panel { Dock = DockStyle.Top, Height = 54 };
        top.Controls.Add(new Label { Text = "🔍", Left = 5, Top = 14, AutoSize = true });
        txtSearch.Left = 36; txtSearch.Top = 11; txtSearch.Width = 280; txtSearch.PlaceholderText = "Search by name or CNIC...";
        txtSearch.TextChanged += (s, e) => { searchDebounce.Stop(); searchDebounce.Start(); };
        var bClr = Btn("✕", Color.Gray, 34); bClr.Left = 320; bClr.Top = 8; bClr.Height = 32; bClr.Click += (s, e) => { txtSearch.Clear(); cmbFilter.SelectedIndex = 0; };
        cmbFilter.Left = 366; cmbFilter.Top = 11; cmbFilter.Width = 230; cmbFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbFilter.Items.Add("All provinces"); cmbFilter.Items.AddRange(Cities.Keys.ToArray()); cmbFilter.SelectedIndex = 0;
        cmbFilter.SelectedIndexChanged += (s, e) => Safe(LoadGrid, "Filter");
        var bEdit = Btn("Edit", Blue, 90); bEdit.Left = 612; bEdit.Top = 8; bEdit.Click += (s, e) => EditSelected();
        var bDel = Btn("Delete", Red, 90); bDel.Left = 710; bDel.Top = 8; bDel.Click += (s, e) => DeleteSelected();
        var bExp = Btn("Export CSV", Green, 120); bExp.Left = 808; bExp.Top = 8; bExp.Click += (s, e) => ExportCsv();
        lblResults.Left = 940; lblResults.Top = 16; lblResults.AutoSize = true; lblResults.ForeColor = DarkGreen;
        top.Controls.AddRange(new Control[] { txtSearch, bClr, cmbFilter, bEdit, bDel, bExp, lblResults });

        grid.Dock = DockStyle.Fill; grid.ReadOnly = true; grid.AllowUserToAddRows = false; grid.AllowUserToResizeRows = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.MultiSelect = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        grid.BackgroundColor = Color.White; grid.BorderStyle = BorderStyle.None; grid.RowHeadersVisible = false;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal; grid.GridColor = Color.FromArgb(230, 236, 232);
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Green; grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10f); grid.ColumnHeadersHeight = 38;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 248, 243);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(130, 200, 160); grid.DefaultCellStyle.SelectionForeColor = Color.Black;
        grid.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        grid.RowTemplate.Height = 30;
        grid.DataSource = bs;
        grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditSelected(); };
        grid.CellMouseDown += (s, e) => { if (e.Button == MouseButtons.Right && e.RowIndex >= 0) grid.CurrentCell = grid.Rows[e.RowIndex].Cells[Math.Max(0, e.ColumnIndex)]; };

        lblEmpty.Text = "No matching citizens found.\nTry a different search or press ✕ to clear."; lblEmpty.Dock = DockStyle.Fill;
        lblEmpty.TextAlign = ContentAlignment.MiddleCenter; lblEmpty.ForeColor = Color.Gray; lblEmpty.Font = new Font("Segoe UI", 12f); lblEmpty.BackColor = Color.White; lblEmpty.Visible = false;
        grid.Controls.Add(lblEmpty);

        gridMenu.Items.Add("Edit record", null, (s, e) => EditSelected());
        gridMenu.Items.Add("Delete record", null, (s, e) => DeleteSelected());
        gridMenu.Items.Add(new ToolStripSeparator());
        gridMenu.Items.Add("Send photo to OpenCV Lab", null, (s, e) =>
        {
            if (grid.CurrentRow?.DataBoundItem is DataRowView v) SendPhotoToLab(v.Row["Photo"]?.ToString());
        });
        grid.ContextMenuStrip = gridMenu;

        root.Controls.Add(grid); root.Controls.Add(top);
        return root;
    }

    void LoadGrid()
    {
        string q = "%" + txtSearch.Text.Trim() + "%";
        string prov = cmbFilter.SelectedIndex <= 0 ? "All" : cmbFilter.SelectedItem.ToString();
        bs.DataSource = Db.Query(@"SELECT Id,CNIC,FullName,FatherName,Gender,DOB,Marital,BloodGroup,Province,City,Phone,Family,Disabled,Overseas,Services,Address,Photo,Created
                                   FROM Citizens WHERE (FullName LIKE @q OR CNIC LIKE @q) AND (@p='All' OR Province=@p) ORDER BY Id DESC",
                                   ("@q", q), ("@p", prov));
        if (grid.Columns["Photo"] != null) grid.Columns["Photo"].Visible = false;
        lblEmpty.Visible = grid.Rows.Count == 0;
        lblResults.Text = $"{grid.Rows.Count} record(s)";
        lblStatus.Text = $"{grid.Rows.Count} record(s) shown";
    }

    void DeleteSelected()
    {
        if (grid.CurrentRow?.DataBoundItem is not DataRowView v) { toast.ShowMessage("Select a record first", Orange); return; }
        string name = v.Row["FullName"].ToString();
        if (MessageBox.Show($"Permanently delete the record of '{name}'?", "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        Safe(() => { Db.Exec("DELETE FROM Citizens WHERE Id=@i", ("@i", Convert.ToInt32(v.Row["Id"]))); RefreshAll(); toast.ShowMessage("Deleted: " + name, Red); }, "Delete");
    }

    void ExportCsv()
    {
        using var sfd = new SaveFileDialog { Filter = "CSV file|*.csv", FileName = "nadra_citizens.csv" };
        if (sfd.ShowDialog() != DialogResult.OK) return;
        Safe(() =>
        {
            var dt = Db.Query("SELECT * FROM Citizens");
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName)));
            foreach (DataRow r in dt.Rows)
                sb.AppendLine(string.Join(",", r.ItemArray.Select(o => "\"" + (o?.ToString() ?? "").Replace("\"", "\"\"") + "\"")));
            File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
            toast.ShowMessage($"Exported {dt.Rows.Count} records to CSV", Green);
        }, "Export");
    }

    string backupTarget;
    void StartBackup()
    {
        if (backupWorker.IsBusy) { toast.ShowMessage("A backup is already running", Orange); return; }
        using var sfd = new SaveFileDialog { Filter = "SQLite DB|*.db", FileName = $"nadra_backup_{DateTime.Now:yyyyMMdd_HHmm}.db" };
        if (sfd.ShowDialog() != DialogResult.OK) return;
        backupTarget = sfd.FileName; progress.Value = 0; progress.Visible = true; lblStatus.Text = "Backing up database...";
        backupWorker.RunWorkerAsync();
    }

    void BackupWork(object s, DoWorkEventArgs e)
    {
        for (int i = 0; i <= 100; i += 5) { Thread.Sleep(40); backupWorker.ReportProgress(i); }
        SqliteConnection.ClearAllPools();
        File.Copy(Db.FilePath, backupTarget, true);
    }

    // =====================================================================
    //  Tree + ListView
    // =====================================================================
    Control BuildTree()
    {
        imgs.ImageSize = new Size(20, 20); imgs.ColorDepth = ColorDepth.Depth32Bit;
        Bitmap Icon(Color c, int shape)
        {
            var b = new Bitmap(20, 20);
            using var g = Graphics.FromImage(b); g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var br = new SolidBrush(c);
            if (shape == 0) g.FillRectangle(br, 3, 3, 14, 14); else if (shape == 1) g.FillEllipse(br, 2, 2, 16, 16);
            else g.FillPolygon(br, new[] { new Point(10, 1), new Point(19, 10), new Point(10, 19), new Point(1, 10) });
            return b;
        }
        imgs.Images.Add(Icon(Green, 0)); imgs.Images.Add(Icon(Blue, 1)); imgs.Images.Add(Icon(Orange, 2));

        split.Dock = DockStyle.Fill; split.SplitterDistance = 330;
        tree.Dock = DockStyle.Fill; tree.Font = new Font("Segoe UI", 10.5f); tree.ImageList = imgs; tree.ItemHeight = 28; tree.BorderStyle = BorderStyle.None; tree.ShowLines = false; tree.FullRowSelect = true; tree.HideSelection = false;
        tree.AfterSelect += (s, e) => Safe(() => LoadListFromNode(e.Node), "Load list");
        split.Panel1.Controls.Add(tree);

        lv.Dock = DockStyle.Fill; lv.View = View.Details; lv.FullRowSelect = true; lv.GridLines = true; lv.SmallImageList = imgs; lv.BorderStyle = BorderStyle.None;
        lv.Columns.Add("CNIC", 150); lv.Columns.Add("Name", 190); lv.Columns.Add("City", 120); lv.Columns.Add("Gender", 90); lv.Columns.Add("Phone", 130);
        split.Panel2.Controls.Add(lv);
        return split;
    }

    void LoadTree()
    {
        tree.BeginUpdate(); tree.Nodes.Clear();
        var dt = Db.Query("SELECT Id,FullName,Province,City FROM Citizens ORDER BY Province,City,FullName");
        foreach (var pg in dt.AsEnumerable().GroupBy(r => r["Province"].ToString()))
        {
            var pn = tree.Nodes.Add($"{pg.Key} ({pg.Count()})"); pn.Tag = "P|" + pg.Key; pn.ImageIndex = pn.SelectedImageIndex = 0;
            foreach (var cg in pg.GroupBy(r => r["City"].ToString()))
            {
                var cn = pn.Nodes.Add($"{cg.Key} ({cg.Count()})"); cn.Tag = "C|" + cg.Key; cn.ImageIndex = cn.SelectedImageIndex = 1;
                foreach (var r in cg) { var n = cn.Nodes.Add(r["FullName"].ToString()); n.Tag = "I|" + r["Id"]; n.ImageIndex = n.SelectedImageIndex = 2; }
            }
        }
        tree.ExpandAll(); tree.EndUpdate();
    }

    void LoadListFromNode(TreeNode n)
    {
        lv.Items.Clear();
        if (n?.Tag is not string t) return;
        var parts = t.Split('|'); string col = parts[0] == "P" ? "Province" : parts[0] == "C" ? "City" : "Id";
        object val = parts[0] == "I" ? int.Parse(parts[1]) : parts[1];
        foreach (DataRow r in Db.Query($"SELECT CNIC,FullName,City,Gender,Phone FROM Citizens WHERE {col}=@v", ("@v", val)).Rows)
        {
            var it = new ListViewItem(r[0].ToString(), 2);
            for (int i = 1; i < 5; i++) it.SubItems.Add(r[i].ToString());
            lv.Items.Add(it);
        }
    }

    // =====================================================================
    //  OpenCV Photo Lab (+ live camera)
    // =====================================================================
    Control BuildOpenCv()
    {
        var root = new Panel { Padding = new Padding(10), BackColor = Soft };
        var bar = new Panel { Dock = DockStyle.Top, Height = 104, BackColor = Color.White };
        bar.Controls.Add(new Label { Text = "Effect:", Left = 12, Top = 16, AutoSize = true });
        cmbFx.Left = 70; cmbFx.Top = 12; cmbFx.Width = 180; cmbFx.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbFx.Items.AddRange(ImageFx.Effects); cmbFx.SelectedIndex = 5;
        cmbFx.SelectedIndexChanged += (s, e) => Safe(RenderFx, "Effect");
        bar.Controls.Add(cmbFx);
        bar.Controls.Add(new Label { Text = "Intensity:", Left = 270, Top = 16, AutoSize = true });
        trkIntensity.Left = 345; trkIntensity.Top = 8; trkIntensity.Width = 220; trkIntensity.Minimum = 5; trkIntensity.Maximum = 100; trkIntensity.Value = 40; trkIntensity.TickFrequency = 10;
        trkIntensity.Scroll += (s, e) => { lblIntensity.Text = trkIntensity.Value.ToString(); Safe(RenderFx, "Effect"); };
        lblIntensity.Left = 570; lblIntensity.Top = 16; lblIntensity.AutoSize = true; lblIntensity.Text = "40";
        bar.Controls.AddRange(new Control[] { trkIntensity, lblIntensity });
        chkScan.Text = "Animated ID-scan effect"; chkScan.Left = 620; chkScan.Top = 14; chkScan.AutoSize = true; chkScan.Checked = true;
        chkScan.CheckedChanged += (s, e) => { if (chkScan.Checked) scanTimer.Start(); else scanTimer.Stop(); Present(); };
        scanTimer.Start();
        bar.Controls.Add(chkScan);

        var bLoad = Btn("Load Image...", Green, 140); bLoad.Left = 12; bLoad.Top = 54;
        bLoad.Click += (s, e) => { if (ofd.ShowDialog() == DialogResult.OK) SendPhotoToLab(ofd.FileName); };
        var bSample = Btn("Sample ID Card", Blue, 140); bSample.Left = 162; bSample.Top = 54; bSample.Click += (s, e) => { StopCamera(); LoadSampleIntoLab(); };
        btnCam = Btn("● Live Camera", Red, 150); btnCam.Left = 312; btnCam.Top = 54; btnCam.Click += (s, e) => ToggleCamera();
        var bSave = Btn("Save Result...", Orange, 140); bSave.Left = 472; bSave.Top = 54;
        bSave.Click += (s, e) =>
        {
            if (cvFx == null) return;
            using var sfd = new SaveFileDialog { Filter = "PNG|*.png|JPEG|*.jpg", FileName = "nadra_effect.png" };
            if (sfd.ShowDialog() == DialogResult.OK) Safe(() => { cvFx.SaveImage(sfd.FileName); toast.ShowMessage("Image saved", Green); }, "Save image");
        };
        bar.Controls.AddRange(new Control[] { bLoad, bSample, btnCam, bSave });
        tip.SetToolTip(btnCam, "Apply the selected effect to your webcam in real time");

        var tl = new BufferedTable { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 10, 0, 0), BackColor = Color.Transparent };
        tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        var g1 = Sec("Original", 0, 0, 10, 10); g1.Dock = DockStyle.Fill; g1.Margin = new Padding(0, 0, 8, 0);
        var g2 = Sec("OpenCV Result", 0, 0, 10, 10, Orange); g2.Dock = DockStyle.Fill;
        foreach (var pb in new[] { picOrig, picFx }) { pb.Dock = DockStyle.Fill; pb.SizeMode = PictureBoxSizeMode.Zoom; pb.BackColor = Color.FromArgb(30, 30, 30); }
        g1.Controls.Add(picOrig); g2.Controls.Add(picFx);
        tl.Controls.Add(g1, 0, 0); tl.Controls.Add(g2, 1, 0);

        root.Controls.Add(tl); root.Controls.Add(bar);
        return root;
    }

    void LoadSampleIntoLab()
    {
        cvSrc?.Dispose(); cvSrc = ImageFx.Sample();
        SwapImage(picOrig, cvSrc);
        Safe(RenderFx, "Effect");
    }

    void SendPhotoToLab(string path)
    {
        tabs.SelectedIndex = 4; StopCamera();
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) { toast.ShowMessage("No photo available - sample card loaded", Orange); LoadSampleIntoLab(); return; }
        Safe(() =>
        {
            var m = ImageFx.Load(path);
            if (m == null) { toast.ShowMessage("Could not read this image", Red); return; }
            cvSrc?.Dispose(); cvSrc = m; SwapImage(picOrig, cvSrc); RenderFx();
        }, "Open image");
    }

    static void SwapImage(PictureBox pb, Cv.Mat m)
    {
        var old = pb.Image; pb.Image = BitmapConverter.ToBitmap(m); old?.Dispose();
    }

    void RenderFx()
    {
        if (cvSrc == null || cvSrc.Empty() || cmbFx.SelectedItem == null) return;
        cvFx?.Dispose();
        cvFx = ImageFx.Apply(cvSrc, cmbFx.SelectedItem.ToString(), trkIntensity.Value);
        Present();
    }

    void Present()
    {
        if (cvFx == null || cvFx.Empty()) return;
        try
        {
            if (chkScan.Checked) { using var o = ImageFx.ScanOverlay(cvFx, scanY % Math.Max(1, cvFx.Height)); SwapImage(picFx, o); }
            else SwapImage(picFx, cvFx);
        }
        catch (Exception ex) { Program.Log(ex); }
    }

    void ToggleCamera()
    {
        if (cap != null) { StopCamera(); return; }
        try
        {
            cap = new Cv.VideoCapture(0);
            if (!cap.IsOpened()) { StopCamera(); toast.ShowMessage("No camera found - use Load Image instead", Red); return; }
            camTimer.Start(); btnCam.Text = "■ Stop Camera"; btnCam.Accent = DarkGreen; btnCam.Invalidate();
            toast.ShowMessage("Camera started - effects are applied live", Blue);
        }
        catch (Exception ex) { Program.Log(ex); StopCamera(); toast.ShowMessage("Camera error: " + ex.Message, Red); }
    }

    void StopCamera()
    {
        camTimer.Stop();
        try { cap?.Release(); cap?.Dispose(); } catch { }
        cap = null;
        if (btnCam != null) { btnCam.Text = "● Live Camera"; btnCam.Accent = Red; btnCam.Invalidate(); }
    }

    void CamTick(object s, EventArgs e)
    {
        if (cap == null) return;
        try
        {
            using var f = new Cv.Mat();
            if (!cap.Read(f) || f.Empty()) return;
            var fr = ImageFx.FitWidth(f.Clone(), 480);
            cvSrc?.Dispose(); cvSrc = fr;
            SwapImage(picOrig, cvSrc);
            scanY += 7;
            RenderFx();
        }
        catch (Exception ex) { Program.Log(ex); StopCamera(); toast.ShowMessage("Camera stopped: " + ex.Message, Red); }
    }

    // =====================================================================
    //  Requirements
    // =====================================================================
    Control BuildRequirements()
    {
        var rtb = new RichTextBox { ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White, Font = new Font("Segoe UI", 11f) };
        void H(string t) { rtb.SelectionFont = new Font("Segoe UI Semibold", 14f); rtb.SelectionColor = Green; rtb.AppendText("\n" + t + "\n"); }
        void P(string t) { rtb.SelectionFont = new Font("Segoe UI", 11f); rtb.SelectionColor = Color.Black; rtb.AppendText(t + "\n"); }

        rtb.SelectionFont = new Font("Segoe UI Semibold", 20f); rtb.SelectionColor = DarkGreen;
        rtb.AppendText("Software Requirements Specification - NADRA Citizen Management System\n");
        H("1. Purpose");
        P("Provide a desktop application to register, search, update and report on citizen identity records held in a NADRA-style database, with an OpenCV image-processing and live-visual module.");
        H("2. Functional Requirements");
        P("FR-1  Register a new citizen: CNIC (13 digits, masked), name, father's name, gender, date of birth, marital status, blood group, province, city, address, mobile, family size, special-citizen and overseas flags, requested services and photograph.");
        P("FR-2  Validate input: mandatory fields, complete CNIC and phone mask, age >= 18 (live age display), unique CNIC (duplicates rejected).");
        P("FR-3  Auto-generate a CNIC whose province digit matches the province and whose last digit is odd for male / even for female.");
        P("FR-4  View all citizens in a sortable grid, search by name or CNIC (debounced live search) and filter by province.");
        P("FR-5  Edit and delete records (double-click, right-click menu or buttons) with confirmation before deletion.");
        P("FR-6  Dashboard: animated count-up statistics, animated province bar chart, recent registrations, capacity meter, calendar, scrolling live ticker.");
        P("FR-7  Browse the database hierarchically (Province > City > Citizen) in a tree and list view.");
        P("FR-8  Export all records to CSV and back up the database in the background with progress indication.");
        P("FR-9  OpenCV Photo Lab: Grayscale, Sepia, Canny Edges, Pencil Sketch, Cartoon, Blur, Emboss, Negative and Pixelate with adjustable intensity, animated ID-scan overlay, live webcam processing and image saving.");
        P("FR-10 Live visuals: animated header, splash screen, OpenCV-rendered radar 'network monitor', toast notifications and a live form-completion meter.");
        H("3. Non-Functional Requirements");
        P("NFR-1  Usability: consistent green NADRA theme, tooltips, error icons, keyboard shortcuts (Ctrl+N/S/F, F5, Ctrl+1..6), empty-state messages, toast feedback.");
        P("NFR-2  Reliability: parameterised SQL, global exception handler with error.log, try/catch around all I/O, camera and image operations.");
        P("NFR-3  Performance: double-buffered custom controls, animations pause when their tab is hidden, search debounce, images capped to 640 px (camera 480 px).");
        P("NFR-4  Portability: .NET 8, Windows 10/11, DPI-aware layout; single-file SQLite database (nadra.db).");
        P("NFR-5  Security (future scope): user login, role-based access, database encryption.");
        H("4. Data Dictionary (table Citizens)");
        P("Id (PK), CNIC (unique), FullName, FatherName, Gender, DOB, Marital, BloodGroup, Province, City, Address, Phone, Family, Disabled, Overseas, Services, Photo (stored copy in /Photos), Created.");
        H("5. Toolbox Controls Used");
        P("MenuStrip, ToolStrip (button, label, textbox), StatusStrip (labels, progress bar), TabControl, Panel, FlowLayoutPanel, TableLayoutPanel, SplitContainer, Label, LinkLabel, TextBox, MaskedTextBox, RichTextBox, ComboBox, DomainUpDown, NumericUpDown, DateTimePicker, MonthCalendar, RadioButton, CheckBox, CheckedListBox, ListBox, ListView, TreeView, DataGridView, BindingSource, PictureBox, ImageList, TrackBar, ProgressBar, Button, ToolTip, ErrorProvider, ContextMenuStrip, OpenFileDialog, SaveFileDialog, ColorDialog, Timer, BackgroundWorker, MessageBox, plus custom owner-drawn controls (animated backdrop, stat card, bar chart, meter, ticker, toast, modern button).");
        H("6. Technology");
        P("C# / .NET 8 Windows Forms, Microsoft.Data.Sqlite, OpenCvSharp4 (+ Extensions, runtime.win).");
        return rtb;
    }

    // =====================================================================
    void RefreshAll()
    {
        if (refreshing) return;
        refreshing = true;
        try { LoadDashboard(); LoadGrid(); LoadTree(); lblCount.Text = $"Citizens: {cTotal.Value}"; }
        finally { refreshing = false; }
    }
}

using System.Drawing.Drawing2D;

namespace NadraApp;

// =============================================================================
//  Graphics helpers
// =============================================================================
static class Gfx
{
    public static GraphicsPath Round(RectangleF r, float rad)
    {
        var p = new GraphicsPath();
        rad = Math.Min(rad, Math.Min(r.Width, r.Height) / 2f);
        if (rad < 1f) { p.AddRectangle(r); return p; }
        float d = rad * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    public static Color Blend(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
    }
}

// =============================================================================
//  Animated gradient / bubble backdrop (used for header and dashboard)
// =============================================================================
class AnimatedBackdrop : Panel
{
    class Bubble { public float X, Y, R, Vx, Vy; public int A; }
    readonly List<Bubble> bubbles = new();
    readonly System.Windows.Forms.Timer timer = new() { Interval = 33 };
    readonly Random rnd = new();
    float phase;

    public Color C1 { get; set; } = Color.FromArgb(0, 70, 38);
    public Color C2 { get; set; } = Color.FromArgb(40, 150, 90);
    public Color BubbleColor { get; set; } = Color.White;
    public bool Gradient { get; set; } = true;
    public int Count { get; set; } = 26;
    public string Title { get; set; }
    public string Subtitle { get; set; }
    public bool ShowClock { get; set; }

    public AnimatedBackdrop()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        timer.Tick += (s, e) => { if (!Visible || Width < 2) return; Step(); Invalidate(); };
    }

    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); if (!DesignMode) timer.Start(); }
    protected override void OnHandleDestroyed(EventArgs e) { timer.Stop(); base.OnHandleDestroyed(e); }
    protected override void Dispose(bool disposing) { if (disposing) timer.Dispose(); base.Dispose(disposing); }

    void Step()
    {
        phase += 0.03f;
        if (bubbles.Count == 0)
            for (int i = 0; i < Count; i++)
                bubbles.Add(new Bubble
                {
                    X = rnd.Next(Math.Max(1, Width)), Y = rnd.Next(Math.Max(1, Height)), R = 3 + (float)rnd.NextDouble() * 13,
                    Vx = ((float)rnd.NextDouble() - 0.5f) * 0.6f, Vy = -0.2f - (float)rnd.NextDouble() * 0.8f, A = 18 + rnd.Next(55)
                });
        foreach (var b in bubbles)
        {
            b.X += b.Vx; b.Y += b.Vy;
            if (b.Y + b.R < 0) { b.Y = Height + b.R; b.X = rnd.Next(Math.Max(1, Width)); }
            if (b.X < -b.R) b.X = Width + b.R; else if (b.X > Width + b.R) b.X = -b.R;
        }
    }

    void Wave(Graphics g, float level, float amp, float ph, Color c)
    {
        var pts = new List<PointF>();
        for (int x = 0; x <= Width + 12; x += 12) pts.Add(new PointF(x, Height * level + (float)Math.Sin(x / 90f + ph) * amp));
        pts.Add(new PointF(Width, Height)); pts.Add(new PointF(0, Height));
        using var br = new SolidBrush(c);
        g.FillPolygon(br, pts.ToArray());
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        var rc = ClientRectangle; if (rc.Width < 2 || rc.Height < 2) return;
        if (Gradient)
        {
            using var br = new LinearGradientBrush(rc, C1, C2, 0f);
            g.FillRectangle(br, rc);
            Wave(g, 0.62f, 12, phase, Color.FromArgb(28, 255, 255, 255));
            Wave(g, 0.76f, 9, phase * 1.4f + 2, Color.FromArgb(22, 255, 255, 255));
        }
        else g.Clear(C1);

        foreach (var b in bubbles)
        {
            using var br = new SolidBrush(Color.FromArgb(b.A, BubbleColor));
            g.FillEllipse(br, b.X - b.R, b.Y - b.R, b.R * 2, b.R * 2);
        }

        if (!string.IsNullOrEmpty(Title))
        {
            float pulse = (float)(Math.Sin(phase * 2.5) * 0.5 + 0.5);
            using (var pen = new Pen(Color.FromArgb(60 + (int)(120 * pulse), 255, 255, 255), 2f))
                g.DrawEllipse(pen, 16 - pulse * 3, 10 - pulse * 3, 60 + pulse * 6, 60 + pulse * 6);
            using (var f = new Font("Segoe UI Symbol", 30f))
                TextRenderer.DrawText(g, "★", f, new Rectangle(16, 10, 60, 60), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            using (var f = new Font("Segoe UI Semibold", 20f))
                TextRenderer.DrawText(g, Title, f, new Point(92, 8), Color.White);
            using (var f = new Font("Segoe UI", 10f))
                TextRenderer.DrawText(g, Subtitle ?? "", f, new Point(95, 48), Color.FromArgb(215, 240, 224));
        }
        if (ShowClock)
        {
            using var f = new Font("Segoe UI Semibold", 11f);
            var txt = DateTime.Now.ToString("dddd, dd MMM yyyy   HH:mm:ss");
            TextRenderer.DrawText(g, txt, f, new Rectangle(0, 14, Width - 20, 24), Color.White, TextFormatFlags.Right);
            float pulse = (float)(Math.Sin(phase * 3) * 0.5 + 0.5);
            using var br = new SolidBrush(Color.FromArgb(120 + (int)(135 * pulse), 120, 255, 140));
            g.FillEllipse(br, Width - 112, 44, 10, 10);
            using var f2 = new Font("Segoe UI", 9f);
            TextRenderer.DrawText(g, "SYSTEM ONLINE", f2, new Rectangle(0, 40, Width - 20, 20), Color.FromArgb(215, 240, 224), TextFormatFlags.Right);
        }
    }
}

// =============================================================================
//  Rounded white "card" section with a title
// =============================================================================
class Section : Panel
{
    public string Title { get; set; } = "";
    public Color Accent { get; set; } = Color.FromArgb(0, 104, 56);
    public Section()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true; BackColor = Color.White; Padding = new Padding(8, 38, 8, 8);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor == Color.Transparent || Parent == null ? Color.FromArgb(240, 246, 242) : Parent.BackColor);
        var rc = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
        using var path = Gfx.Round(rc, 10);
        using (var br = new SolidBrush(Color.White)) g.FillPath(br, path);
        using (var pen = new Pen(Color.FromArgb(215, 226, 219))) g.DrawPath(pen, path);
        using (var br = new SolidBrush(Accent)) g.FillRectangle(br, 14, 11, 4, 18);
        using var f = new Font("Segoe UI Semibold", 11f);
        TextRenderer.DrawText(g, Title, f, new Point(24, 8), Color.FromArgb(0, 70, 38));
    }
}

// =============================================================================
//  Modern animated button
// =============================================================================
class ModernButton : Button
{
    public Color Accent { get; set; } = Color.FromArgb(0, 104, 56);
    public int Radius { get; set; } = 8;
    float h; bool over, down;
    readonly System.Windows.Forms.Timer t = new() { Interval = 15 };

    public ModernButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; Cursor = Cursors.Hand;
        ForeColor = Color.White; Height = 38; Font = new Font("Segoe UI Semibold", 10f);
        t.Tick += (s, e) =>
        {
            h += over ? 0.2f : -0.2f; h = Math.Clamp(h, 0f, 1f); Invalidate();
            if ((over && h >= 1f) || (!over && h <= 0f)) t.Stop();
        };
    }
    protected override void OnMouseEnter(EventArgs e) { over = true; t.Start(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { over = false; down = false; t.Start(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
    protected override void Dispose(bool disposing) { if (disposing) t.Dispose(); base.Dispose(disposing); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent == null || Parent.BackColor == Color.Transparent ? SystemColors.Control : Parent.BackColor);
        var rc = new RectangleF(1, 1, Width - 3, Height - 3);
        var col = Enabled ? Gfx.Blend(Accent, Color.White, h * 0.18f) : Color.Silver;
        if (down) col = Gfx.Blend(col, Color.Black, 0.15f);
        using var path = Gfx.Round(rc, Radius);
        using (var br = new SolidBrush(col)) g.FillPath(br, path);
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        if (Focused && ShowFocusCues)
        {
            using var pen = new Pen(Color.White, 1f) { DashStyle = DashStyle.Dot };
            using var fp = Gfx.Round(new RectangleF(4, 4, Width - 9, Height - 9), Radius);
            g.DrawPath(pen, fp);
        }
    }
}

// =============================================================================
//  Dashboard stat card with count-up + shine animation
// =============================================================================
class StatCard : Control
{
    readonly System.Windows.Forms.Timer t = new() { Interval = 30 };
    double shown; long target; int tick; float shine = -1; bool hover;
    public string Title { get; set; } = "";
    public string Glyph { get; set; } = "●";
    public Color Accent { get; set; } = Color.FromArgb(0, 104, 56);
    public long Value { get => target; set { target = value; } }

    public StatCard()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true; Cursor = Cursors.Hand;
        t.Tick += (s, e) =>
        {
            bool inv = false;
            if (Math.Abs(shown - target) > 0.01) { shown += (target - shown) * 0.12; if (Math.Abs(shown - target) < 0.5) shown = target; inv = true; }
            tick++;
            if (tick % 180 == 0 && shine < 0) shine = 0;
            if (shine >= 0) { shine += 0.045f; if (shine > 1.3f) shine = -1; inv = true; }
            if (inv && Visible) Invalidate();
        };
    }
    public void Replay() { shown = 0; shine = 0; Invalidate(); }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); if (!DesignMode) t.Start(); }
    protected override void OnHandleDestroyed(EventArgs e) { t.Stop(); base.OnHandleDestroyed(e); }
    protected override void Dispose(bool d) { if (d) t.Dispose(); base.Dispose(d); }
    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(240, 246, 242));
        var rc = new RectangleF(1, 1, Width - 3, Height - 3);
        var c1 = hover ? Gfx.Blend(Accent, Color.White, 0.12f) : Accent;
        var c2 = Gfx.Blend(c1, Color.Black, 0.22f);
        using var path = Gfx.Round(rc, 12);
        using (var br = new LinearGradientBrush(rc, c1, c2, 45f)) g.FillPath(br, path);
        if (shine >= 0)
        {
            g.SetClip(path);
            float x = -Width * 0.3f + shine * Width * 1.6f;
            using var sb = new SolidBrush(Color.FromArgb(55, 255, 255, 255));
            g.FillPolygon(sb, new[] { new PointF(x, 0), new PointF(x + 44, 0), new PointF(x + 14, Height), new PointF(x - 30, Height) });
            g.ResetClip();
        }
        using (var f = new Font("Segoe UI Symbol", 34f))
            TextRenderer.DrawText(g, Glyph, f, new Rectangle(Width - 80, 6, 72, Height - 12), Color.FromArgb(70, 255, 255, 255), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        using (var f = new Font("Segoe UI Semibold", 26f))
            TextRenderer.DrawText(g, ((long)Math.Round(shown)).ToString("N0"), f, new Point(12, 4), Color.White);
        using (var f = new Font("Segoe UI", 10f))
            TextRenderer.DrawText(g, Title, f, new Point(14, Height - 30), Color.FromArgb(235, 248, 240));
    }
}

// =============================================================================
//  Animated bar chart (bars grow smoothly, hover highlight)
// =============================================================================
class BarChart : Control
{
    List<(string label, int value)> data = new();
    double[] cur = Array.Empty<double>();
    readonly System.Windows.Forms.Timer t = new() { Interval = 25 };
    int hoverIdx = -1; float slot = 1; int plotLeft = 36;
    public Color[] Palette { get; set; } =
    {
        Color.FromArgb(0, 104, 56), Color.FromArgb(30, 100, 170), Color.FromArgb(200, 130, 20), Color.FromArgb(190, 50, 50),
        Color.FromArgb(120, 80, 160), Color.FromArgb(0, 130, 140), Color.FromArgb(110, 140, 30)
    };

    public BarChart()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true; BackColor = Color.White;
        t.Tick += (s, e) =>
        {
            bool moving = false;
            for (int i = 0; i < cur.Length && i < data.Count; i++)
            {
                double tg = data[i].value;
                if (Math.Abs(cur[i] - tg) > 0.01) { cur[i] += (tg - cur[i]) * 0.13; if (Math.Abs(cur[i] - tg) < 0.02) cur[i] = tg; moving = true; }
            }
            if (moving && Visible) Invalidate();
        };
    }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); if (!DesignMode) t.Start(); }
    protected override void OnHandleDestroyed(EventArgs e) { t.Stop(); base.OnHandleDestroyed(e); }
    protected override void Dispose(bool d) { if (d) t.Dispose(); base.Dispose(d); }

    public void SetData(IEnumerable<(string, int)> d)
    {
        data = d.ToList();
        if (cur.Length != data.Count) cur = new double[data.Count];
        Invalidate();
    }
    public void Replay() { for (int i = 0; i < cur.Length; i++) cur[i] = 0; Invalidate(); }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        int idx = slot > 0 ? (int)((e.X - plotLeft) / slot) : -1;
        if (idx < 0 || idx >= data.Count) idx = -1;
        if (idx != hoverIdx) { hoverIdx = idx; Invalidate(); }
        base.OnMouseMove(e);
    }
    protected override void OnMouseLeave(EventArgs e) { hoverIdx = -1; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Color.White);
        if (data.Count == 0) { TextRenderer.DrawText(g, "No data yet", Font, ClientRectangle, Color.Gray, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); return; }
        int l = plotLeft, r = 12, tp = 26, b = 34;
        var plot = new Rectangle(l, tp, Width - l - r, Height - tp - b);
        if (plot.Width < 40 || plot.Height < 40) return;
        int max = Math.Max(1, data.Max(d => d.value));
        int nice = max <= 4 ? 4 : (int)Math.Ceiling(max / 4.0) * 4;
        using (var gp = new Pen(Color.FromArgb(228, 233, 229)))
            for (int i = 0; i <= 4; i++)
            {
                int y = plot.Bottom - plot.Height * i / 4;
                g.DrawLine(gp, plot.Left, y, plot.Right, y);
                TextRenderer.DrawText(g, (nice * i / 4).ToString(), Font, new Point(2, y - 9), Color.Gray);
            }
        int n = data.Count; slot = plot.Width / (float)n; float bw = Math.Min(64f, slot * 0.62f);
        for (int i = 0; i < n; i++)
        {
            float h = (float)(cur[i] / nice * plot.Height);
            float x = plot.Left + slot * i + (slot - bw) / 2;
            var col = Palette[i % Palette.Length]; if (i == hoverIdx) col = Gfx.Blend(col, Color.White, 0.22f);
            if (h > 3)
            {
                var rc = new RectangleF(x, plot.Bottom - h, bw, h);
                using var br = new LinearGradientBrush(rc, Gfx.Blend(col, Color.White, 0.3f), col, 90f);
                g.FillRectangle(br, rc);
            }
            TextRenderer.DrawText(g, ((int)Math.Round(cur[i])).ToString(), Font,
                new Rectangle((int)x - 12, (int)(plot.Bottom - h) - 22, (int)bw + 24, 20), Color.FromArgb(40, 40, 40), TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(g, data[i].label, Font, new Rectangle((int)(plot.Left + slot * i), plot.Bottom + 6, (int)slot, 22),
                i == hoverIdx ? Color.Black : Color.DimGray, TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}

// =============================================================================
//  Animated progress meter with shimmer
// =============================================================================
class AnimatedMeter : Control
{
    readonly System.Windows.Forms.Timer t = new() { Interval = 30 };
    double shown; float shimmer;
    public int Value { get; set; }
    public string Caption { get; set; } = "";

    public AnimatedMeter()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true; Height = 24;
        t.Tick += (s, e) =>
        {
            shown += (Value - shown) * 0.14; if (Math.Abs(shown - Value) < 0.2) shown = Value;
            shimmer += 0.03f; if (shimmer > 1.4f) shimmer = -0.3f;
            if (Visible) Invalidate();
        };
    }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); if (!DesignMode) t.Start(); }
    protected override void OnHandleDestroyed(EventArgs e) { t.Stop(); base.OnHandleDestroyed(e); }
    protected override void Dispose(bool d) { if (d) t.Dispose(); base.Dispose(d); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent == null || Parent.BackColor == Color.Transparent ? Color.White : Parent.BackColor);
        var track = new RectangleF(0, 1, Width - 1, Height - 3);
        using (var tp = Gfx.Round(track, Height / 2f - 1)) using (var br = new SolidBrush(Color.FromArgb(226, 232, 228))) g.FillPath(br, tp);
        float w = (float)(track.Width * Math.Clamp(shown, 0, 100) / 100.0);
        if (w > 4)
        {
            var fr = new RectangleF(0, 1, w, track.Height);
            var good = shown >= 100 ? Color.FromArgb(40, 160, 90) : Color.FromArgb(0, 104, 56);
            using var fp = Gfx.Round(fr, Height / 2f - 1);
            using (var br = new LinearGradientBrush(fr, Gfx.Blend(good, Color.White, 0.25f), good, 0f)) g.FillPath(br, fp);
            g.SetClip(fp);
            float x = shimmer * w;
            using (var sb = new SolidBrush(Color.FromArgb(60, 255, 255, 255)))
                g.FillPolygon(sb, new[] { new PointF(x, 0), new PointF(x + 30, 0), new PointF(x + 10, Height), new PointF(x - 20, Height) });
            g.ResetClip();
        }
        string txt = string.IsNullOrEmpty(Caption) ? $"{(int)Math.Round(shown)}%" : $"{Caption}  {(int)Math.Round(shown)}%";
        using var f = new Font("Segoe UI Semibold", 9f);
        TextRenderer.DrawText(g, txt, f, ClientRectangle, w > Width / 2 ? Color.White : Color.FromArgb(0, 70, 38),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

// =============================================================================
//  Scrolling news ticker with pulsing LIVE badge
// =============================================================================
class TickerBar : Control
{
    readonly System.Windows.Forms.Timer t = new() { Interval = 28 };
    float x = 400, pulse;
    public string Message { get; set; } = "";

    public TickerBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true; Height = 34;
        t.Tick += (s, e) =>
        {
            x -= 2f; pulse += 0.12f;
            int tw = TextRenderer.MeasureText(Message, Font).Width + 80;
            if (x < -tw) x += tw;
            if (Visible) Invalidate();
        };
    }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); if (!DesignMode) t.Start(); }
    protected override void OnHandleDestroyed(EventArgs e) { t.Stop(); base.OnHandleDestroyed(e); }
    protected override void Dispose(bool d) { if (d) t.Dispose(); base.Dispose(d); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(0, 70, 38));
        int tw = TextRenderer.MeasureText(Message, Font).Width + 80;
        for (float px = x; px < Width; px += tw)
            TextRenderer.DrawText(g, Message, Font, new Point((int)px + 110, 7), Color.White);
        using (var br = new SolidBrush(Color.FromArgb(0, 70, 38))) g.FillRectangle(br, 0, 0, 104, Height);
        var badge = new RectangleF(8, 6, 88, Height - 12);
        using (var p = Gfx.Round(badge, 6)) using (var br = new SolidBrush(Color.FromArgb(190, 50, 50))) g.FillPath(br, p);
        int a = 140 + (int)(115 * (Math.Sin(pulse) * 0.5 + 0.5));
        using (var br = new SolidBrush(Color.FromArgb(a, 255, 255, 255))) g.FillEllipse(br, 16, Height / 2 - 4, 8, 8);
        using var f = new Font("Segoe UI Semibold", 9.5f);
        TextRenderer.DrawText(g, "LIVE", f, new Rectangle(28, 6, 66, Height - 12), Color.White, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
    }
}

// =============================================================================
//  Toast notification that slides in from the right
// =============================================================================
class Toast : Control
{
    readonly System.Windows.Forms.Timer t = new() { Interval = 16 };
    int state, hold, targetLeft; string msg = ""; Color col = Color.Green;
    public int BottomMargin { get; set; } = 60;

    public Toast()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true; Visible = false; Height = 52;
        t.Tick += (s, e) =>
        {
            if (Parent == null) { t.Stop(); return; }
            if (state == 1)
            {
                int dx = targetLeft - Left;
                if (Math.Abs(dx) <= 2) { Left = targetLeft; state = 2; hold = 0; }
                else Left += (int)Math.Round(dx * 0.22);
            }
            else if (state == 2) { if (++hold > 190) state = 3; }
            else if (state == 3)
            {
                Left += 34;
                if (Left > Parent.ClientSize.Width) { Visible = false; state = 0; t.Stop(); }
            }
        };
    }
    protected override void Dispose(bool d) { if (d) t.Dispose(); base.Dispose(d); }

    public void ShowMessage(string message, Color color)
    {
        if (Parent == null) return;
        msg = message; col = color;
        Width = Math.Min(480, Math.Max(240, Parent.ClientSize.Width - 40));
        targetLeft = Parent.ClientSize.Width - Width - 20;
        Top = Parent.ClientSize.Height - BottomMargin - Height;
        Left = Parent.ClientSize.Width; state = 1; hold = 0;
        Visible = true; BringToFront(); t.Start(); Invalidate();
    }

    protected override void OnClick(EventArgs e) { state = 3; base.OnClick(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Color.White);
        var rc = new RectangleF(1, 1, Width - 3, Height - 3);
        using var p = Gfx.Round(rc, 10);
        using (var br = new SolidBrush(col)) g.FillPath(br, p);
        using (var br = new SolidBrush(Color.FromArgb(60, 255, 255, 255))) g.FillEllipse(br, 12, 11, 30, 30);
        string icon = col == Color.FromArgb(190, 50, 50) ? "✖" : "✔";
        using (var f = new Font("Segoe UI Symbol", 12f, FontStyle.Bold))
            TextRenderer.DrawText(g, icon, f, new Rectangle(12, 11, 30, 30), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        using (var f = new Font("Segoe UI Semibold", 10f))
            TextRenderer.DrawText(g, msg, f, new Rectangle(52, 4, Width - 62, Height - 8), Color.White, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

// =============================================================================
//  Flicker-free helpers
// =============================================================================
class FastGrid : DataGridView { public FastGrid() { DoubleBuffered = true; } }
class BufferedTable : TableLayoutPanel { public BufferedTable() { DoubleBuffered = true; } }

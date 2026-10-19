using System.Drawing.Drawing2D;

namespace NadraApp;

/// <summary>Animated start-up splash: fade-in, spinning ring, loading bar, fade-out.</summary>
class SplashForm : Form
{
    readonly System.Windows.Forms.Timer t = new() { Interval = 16 };
    float progress, spin; int tick; bool closing;
    static readonly string[] Steps = { "Connecting to citizen database...", "Loading registration modules...", "Starting OpenCV engine...", "Preparing dashboard..." };

    public SplashForm()
    {
        FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(560, 320); ShowInTaskbar = false; Opacity = 0; DoubleBuffered = true;
        t.Tick += (s, e) =>
        {
            tick++; spin += 6f;
            if (!closing)
            {
                if (Opacity < 1) Opacity += 0.06;
                progress = Math.Min(1f, progress + 0.008f + (1f - progress) * 0.004f);
                if (progress >= 1f && tick > 120) closing = true;
            }
            else { Opacity -= 0.09; if (Opacity <= 0.02) { t.Stop(); Close(); } }
            Invalidate();
        };
        Load += (s, e) => t.Start();
    }
    protected override void Dispose(bool d) { if (d) t.Dispose(); base.Dispose(d); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var br = new LinearGradientBrush(ClientRectangle, Color.FromArgb(0, 60, 32), Color.FromArgb(40, 150, 90), 60f)) g.FillRectangle(br, ClientRectangle);
        using (var pen = new Pen(Color.FromArgb(120, 255, 255, 255), 2)) g.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);

        var c = new PointF(Width / 2f, 100);
        using (var pen = new Pen(Color.FromArgb(200, 255, 255, 255), 4) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawArc(pen, c.X - 42, c.Y - 42, 84, 84, spin, 100);
        using (var pen = new Pen(Color.FromArgb(90, 255, 255, 255), 2))
            g.DrawArc(pen, c.X - 52, c.Y - 52, 104, 104, -spin * 0.7f, 160);
        using (var f = new Font("Segoe UI Symbol", 34f))
            TextRenderer.DrawText(g, "★", f, new Rectangle((int)c.X - 40, (int)c.Y - 40, 80, 80), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        using (var f = new Font("Segoe UI Semibold", 24f))
            TextRenderer.DrawText(g, "NADRA", f, new Rectangle(0, 165, Width, 40), Color.White, TextFormatFlags.HorizontalCenter);
        using (var f = new Font("Segoe UI", 10.5f))
            TextRenderer.DrawText(g, "Citizen Management System", f, new Rectangle(0, 205, Width, 24), Color.FromArgb(215, 240, 224), TextFormatFlags.HorizontalCenter);

        var bar = new RectangleF(80, 262, Width - 160, 8);
        using (var p = Gfx.Round(bar, 4)) using (var br = new SolidBrush(Color.FromArgb(70, 255, 255, 255))) g.FillPath(br, p);
        var fill = new RectangleF(bar.X, bar.Y, Math.Max(8, bar.Width * progress), bar.Height);
        using (var p = Gfx.Round(fill, 4)) using (var br = new SolidBrush(Color.White)) g.FillPath(br, p);
        using (var f = new Font("Segoe UI", 9f))
            TextRenderer.DrawText(g, Steps[Math.Min(Steps.Length - 1, (int)(progress * Steps.Length))], f, new Rectangle(0, 278, Width, 22), Color.FromArgb(215, 240, 224), TextFormatFlags.HorizontalCenter);
    }
}

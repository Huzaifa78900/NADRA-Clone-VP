using Cv = OpenCvSharp;

namespace NadraApp;

static partial class ImageFx
{
    public static Cv.Mat FitWidth(Cv.Mat m, int maxW) => Fit(m, maxW);

    /// <summary>Renders one frame of an animated radar (live network monitor) with OpenCV.</summary>
    public static Cv.Mat Radar(int size, double angleDeg, (double ang, double rad, int w)[] blips)
    {
        var m = new Cv.Mat(size, size, Cv.MatType.CV_8UC3);
        m.SetTo(new Cv.Scalar(22, 38, 20));
        var c = new Cv.Point(size / 2, size / 2);
        int R = size / 2 - 6;

        // fading sweep trail
        for (int i = 45; i >= 0; i--)
        {
            double a = (angleDeg - i) * Math.PI / 180.0;
            var p = new Cv.Point(c.X + (int)(R * Math.Cos(a)), c.Y + (int)(R * Math.Sin(a)));
            Cv.Cv2.Line(m, c, p, new Cv.Scalar(0, Math.Max(15, 190 - i * 4), 0), 2);
        }
        // rings + cross-hair
        for (int i = 1; i <= 4; i++) Cv.Cv2.Circle(m, c, R * i / 4, new Cv.Scalar(70, 130, 70), 1, Cv.LineTypes.AntiAlias);
        Cv.Cv2.Line(m, new Cv.Point(c.X - R, c.Y), new Cv.Point(c.X + R, c.Y), new Cv.Scalar(60, 110, 60), 1);
        Cv.Cv2.Line(m, new Cv.Point(c.X, c.Y - R), new Cv.Point(c.X, c.Y + R), new Cv.Scalar(60, 110, 60), 1);
        // leading edge
        double la = angleDeg * Math.PI / 180.0;
        Cv.Cv2.Line(m, c, new Cv.Point(c.X + (int)(R * Math.Cos(la)), c.Y + (int)(R * Math.Sin(la))), new Cv.Scalar(120, 255, 120), 2, Cv.LineTypes.AntiAlias);

        // blips light up when the sweep passes over them
        foreach (var b in blips)
        {
            double d = ((angleDeg - b.ang) % 360 + 360) % 360;
            double br = Math.Max(0, 1 - d / 150.0);
            double a = b.ang * Math.PI / 180.0;
            var p = new Cv.Point(c.X + (int)(R * b.rad * Math.Cos(a)), c.Y + (int)(R * b.rad * Math.Sin(a)));
            Cv.Cv2.Circle(m, p, b.w, new Cv.Scalar(40 + 60 * br, 90 + 165 * br, 40 + 60 * br), -1, Cv.LineTypes.AntiAlias);
            if (br > 0.7) Cv.Cv2.Circle(m, p, b.w + 5, new Cv.Scalar(120, 255, 160), 1, Cv.LineTypes.AntiAlias);
        }
        Cv.Cv2.Circle(m, c, 4, new Cv.Scalar(120, 255, 120), -1, Cv.LineTypes.AntiAlias);
        Cv.Cv2.PutText(m, "NADRA NET", new Cv.Point(8, size - 10), Cv.HersheyFonts.HersheySimplex, 0.4, new Cv.Scalar(120, 200, 120), 1);
        return m;
    }
}

using Cv = OpenCvSharp;

namespace NadraApp;

/// <summary>OpenCV visual-effects module.</summary>
static partial class ImageFx
{
    public static readonly string[] Effects =
    {
        "Original", "Grayscale", "Sepia", "Canny Edges", "Pencil Sketch",
        "Cartoon", "Gaussian Blur", "Emboss", "Negative", "Pixelate"
    };

    public static Cv.Mat Load(string path)
    {
        var m = Cv.Cv2.ImRead(path);
        if (m.Empty()) return null;
        return Fit(m, 640);
    }

    static Cv.Mat Fit(Cv.Mat m, int maxW)
    {
        if (m.Width <= maxW) return m;
        var r = new Cv.Mat();
        Cv.Cv2.Resize(m, r, new Cv.Size(maxW, m.Height * maxW / m.Width));
        m.Dispose();
        return r;
    }

    /// <summary>Generates a sample ID-card image so the module works without a photo.</summary>
    public static Cv.Mat Sample()
    {
        var m = new Cv.Mat(320, 520, Cv.MatType.CV_8UC3);
        m.SetTo(new Cv.Scalar(240, 248, 240));
        var green = new Cv.Scalar(56, 104, 0);
        Cv.Cv2.Rectangle(m, new Cv.Rect(0, 0, 520, 60), green, -1);
        Cv.Cv2.PutText(m, "NADRA - IDENTITY CARD", new Cv.Point(15, 40), Cv.HersheyFonts.HersheySimplex, 0.9, new Cv.Scalar(255, 255, 255), 2);
        Cv.Cv2.Rectangle(m, new Cv.Rect(25, 90, 140, 170), new Cv.Scalar(190, 190, 190), -1);
        Cv.Cv2.Circle(m, new Cv.Point(95, 150), 32, new Cv.Scalar(90, 90, 90), -1);
        Cv.Cv2.Ellipse(m, new Cv.Point(95, 255), new Cv.Size(60, 50), 0, 180, 360, new Cv.Scalar(90, 90, 90), -1);
        string[] lines = { "Name: Ali Raza", "Father: Muhammad Aslam", "Gender: M   DOB: 12.04.1990", "CNIC: 35202-1234567-1" };
        for (int i = 0; i < lines.Length; i++)
            Cv.Cv2.PutText(m, lines[i], new Cv.Point(190, 115 + i * 38), Cv.HersheyFonts.HersheyDuplex, 0.6, new Cv.Scalar(40, 40, 40), 1);
        Cv.Cv2.Rectangle(m, new Cv.Rect(5, 5, 510, 310), green, 3);
        Cv.Cv2.Circle(m, new Cv.Point(440, 230), 45, green, 4);
        Cv.Cv2.Line(m, new Cv.Point(400, 230), new Cv.Point(480, 230), green, 2);
        return m;
    }

    public static Cv.Mat Apply(Cv.Mat src, string fx, int k)
    {
        var dst = new Cv.Mat();
        switch (fx)
        {
            case "Grayscale":
                using (var g = new Cv.Mat())
                {
                    Cv.Cv2.CvtColor(src, g, Cv.ColorConversionCodes.BGR2GRAY);
                    Cv.Cv2.CvtColor(g, dst, Cv.ColorConversionCodes.GRAY2BGR);
                }
                break;
            case "Sepia":
                using (var kern = Cv.Mat.FromPixelData(3, 3, Cv.MatType.CV_32FC1, new float[]
                {
                    0.131f, 0.534f, 0.272f,
                    0.168f, 0.686f, 0.349f,
                    0.189f, 0.769f, 0.393f
                }))
                    Cv.Cv2.Transform(src, dst, kern);
                break;
            case "Canny Edges":
                using (var g = new Cv.Mat())
                using (var e = new Cv.Mat())
                {
                    Cv.Cv2.CvtColor(src, g, Cv.ColorConversionCodes.BGR2GRAY);
                    Cv.Cv2.GaussianBlur(g, g, new Cv.Size(5, 5), 1.2);
                    Cv.Cv2.Canny(g, e, Math.Max(10, k), Math.Max(10, k) * 2);
                    Cv.Cv2.CvtColor(e, dst, Cv.ColorConversionCodes.GRAY2BGR);
                }
                break;
            case "Pencil Sketch":
                using (var g = new Cv.Mat())
                using (var inv = new Cv.Mat())
                using (var blur = new Cv.Mat())
                using (var invBlur = new Cv.Mat())
                using (var sk = new Cv.Mat())
                {
                    int kk = Math.Max(3, (k / 2) | 1);
                    Cv.Cv2.CvtColor(src, g, Cv.ColorConversionCodes.BGR2GRAY);
                    Cv.Cv2.BitwiseNot(g, inv);
                    Cv.Cv2.GaussianBlur(inv, blur, new Cv.Size(kk, kk), 0);
                    Cv.Cv2.BitwiseNot(blur, invBlur);
                    Cv.Cv2.Divide(g, invBlur, sk, 256);
                    Cv.Cv2.CvtColor(sk, dst, Cv.ColorConversionCodes.GRAY2BGR);
                }
                break;
            case "Cartoon":
                using (var g = new Cv.Mat())
                using (var edges = new Cv.Mat())
                using (var edgesBgr = new Cv.Mat())
                using (var color = new Cv.Mat())
                {
                    Cv.Cv2.CvtColor(src, g, Cv.ColorConversionCodes.BGR2GRAY);
                    Cv.Cv2.MedianBlur(g, g, 7);
                    Cv.Cv2.AdaptiveThreshold(g, edges, 255, Cv.AdaptiveThresholdTypes.MeanC, Cv.ThresholdTypes.Binary, 9, 2);
                    Cv.Cv2.BilateralFilter(src, color, 9, 200, 200);
                    Cv.Cv2.CvtColor(edges, edgesBgr, Cv.ColorConversionCodes.GRAY2BGR);
                    Cv.Cv2.BitwiseAnd(color, edgesBgr, dst);
                }
                break;
            case "Gaussian Blur":
                int bk = Math.Max(3, k | 1);
                Cv.Cv2.GaussianBlur(src, dst, new Cv.Size(bk, bk), 0);
                break;
            case "Emboss":
                using (var kern = Cv.Mat.FromPixelData(3, 3, Cv.MatType.CV_32FC1, new float[]
                {
                    -2f, -1f, 0f,
                    -1f, 1f, 1f,
                    0f, 1f, 2f
                }))
                    Cv.Cv2.Filter2D(src, dst, -1, kern, delta: 128);
                break;
            case "Negative":
                Cv.Cv2.BitwiseNot(src, dst);
                break;
            case "Pixelate":
                using (var small = new Cv.Mat())
                {
                    int f = Math.Max(2, k / 5);
                    Cv.Cv2.Resize(src, small, new Cv.Size(Math.Max(1, src.Width / f), Math.Max(1, src.Height / f)));
                    Cv.Cv2.Resize(small, dst, src.Size(), 0, 0, Cv.InterpolationFlags.Nearest);
                }
                break;
            default:
                src.CopyTo(dst);
                break;
        }
        return dst;
    }

    /// <summary>Draws an animated green "scanner" band over the image.</summary>
    public static Cv.Mat ScanOverlay(Cv.Mat src, int y)
    {
        var m = src.Clone();
        using (var overlay = m.Clone())
        {
            Cv.Cv2.Rectangle(overlay, new Cv.Rect(0, y - 25, m.Width, 50), new Cv.Scalar(80, 255, 80), -1);
            Cv.Cv2.AddWeighted(overlay, 0.30, m, 0.70, 0, m);
        }
        Cv.Cv2.Line(m, new Cv.Point(0, y), new Cv.Point(m.Width, y), new Cv.Scalar(0, 255, 0), 2);
        Cv.Cv2.PutText(m, "SCANNING...", new Cv.Point(10, 25), Cv.HersheyFonts.HersheySimplex, 0.6, new Cv.Scalar(0, 255, 0), 2);
        return m;
    }
}

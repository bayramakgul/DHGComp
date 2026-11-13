using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DHGComp.Imaging;

// The four images shown in the main window and the binary image used for the calculation.
// All bitmaps are frozen, so this can be produced on a background thread.
public sealed record ProcessedImage(
    BitmapSource Original,
    BitmapSource Grayscale,
    BitmapSource ThresholdPreview,
    BitmapSource Binary,
    bool[,] Foreground,
    double Threshold)
{
    public int Width => Foreground.GetLength(0);
    public int Height => Foreground.GetLength(1);
}

// Black / white decision for every pixel ([x, y], true = black = part of the complex).
public sealed record Binarization(bool[,] Foreground, double Threshold);

// Replaces ImageClass and the pixel loop of FrmMain.StartProcessing (WinForms version).
public static class ImageProcessing
{
    // Reads the first frame of an image file. The file is read completely and closed, so it
    // is not kept locked. Embedded colour profiles are ignored, like Image.FromFile did.
    public static BitmapSource Load(string path)
    {
        using FileStream stream = File.OpenRead(path);
        BitmapDecoder decoder = BitmapDecoder.Create(stream,
            BitmapCreateOptions.IgnoreColorProfile | BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad);
        BitmapSource frame = decoder.Frames[0];
        frame.Freeze();
        return frame;
    }

    public static ProcessedImage Process(BitmapSource original)
    {
        int w = original.PixelWidth;
        int h = original.PixelHeight;
        byte[] bgra = ToBgra32(original);
        FlattenOnWhite(bgra);
        int stride = w * 4;

        // Grayscale preview: 0.3 R + 0.59 G + 0.11 B (as MakeGrayscale3).
        // Threshold preview: that gray value at a fixed threshold of 128 (as Threshold).
        byte[] gray = new byte[w * h];
        byte[] preview = new byte[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int p = y * stride + 4 * x;
                byte g = (byte)(0.3 * bgra[p + 2] + 0.59 * bgra[p + 1] + 0.11 * bgra[p] + 0.5);
                gray[y * w + x] = g;
                preview[y * w + x] = g >= 128 ? (byte)255 : (byte)0;
            }
        }

        Binarization bin = Binarize(bgra, w, h, stride);

        return new ProcessedImage(
            original,
            Create(w, h, PixelFormats.Gray8, gray, w),
            Create(w, h, PixelFormats.Gray8, preview, w),
            BinaryImage(bin.Foreground),
            bin.Foreground,
            bin.Threshold);
    }

    // Transparent pixels are composited onto white, i.e. the image as it looks on a white page.
    // Opaque pixels are unchanged. (The WinForms version turned fully transparent pixels black,
    // so a black shape on a transparent background came out as an all-black image.)
    public static void FlattenOnWhite(byte[] bgra)
    {
        for (int p = 0; p < bgra.Length; p += 4)
        {
            int a = bgra[p + 3];
            if (a == 255)
                continue;
            int white = 255 * (255 - a);
            bgra[p] = (byte)((bgra[p] * a + white + 127) / 255);
            bgra[p + 1] = (byte)((bgra[p + 1] * a + white + 127) / 255);
            bgra[p + 2] = (byte)((bgra[p + 2] * a + white + 127) / 255);
            bgra[p + 3] = 255;
        }
    }

    // The decision used for the calculation, unchanged from the WinForms version:
    // a pixel is black (foreground) when (R + G + B) / 3 is below the mean of that value over
    // the whole image; a uniform image is split at 128 instead. Expects opaque pixels (see
    // FlattenOnWhite).
    public static Binarization Binarize(byte[] bgra, int width, int height, int stride)
    {
        int[,] intensity = new int[width, height];
        long sum = 0;
        int min = 255, max = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int p = y * stride + 4 * x;
                int value = (bgra[p + 2] + bgra[p + 1] + bgra[p]) / 3;
                intensity[x, y] = value;
                sum += value;
                if (value < min) min = value;
                if (value > max) max = value;
            }
        }

        double threshold = (double)sum / ((long)width * height);
        if (min == max)
            threshold = 128;

        bool[,] foreground = new bool[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                foreground[x, y] = intensity[x, y] < threshold;

        return new Binarization(foreground, threshold);
    }

    // Black for foreground pixels, white for the rest.
    public static BitmapSource BinaryImage(bool[,] foreground)
    {
        int w = foreground.GetLength(0);
        int h = foreground.GetLength(1);
        byte[] pixels = new byte[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = foreground[x, y] ? (byte)0 : (byte)255;
        return Create(w, h, PixelFormats.Gray8, pixels, w);
    }

    // Non-premultiplied B, G, R, A bytes, rows of width * 4 bytes.
    public static byte[] ToBgra32(BitmapSource source)
    {
        BitmapSource bgra = source.Format == PixelFormats.Bgra32
            ? source
            : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        int stride = source.PixelWidth * 4;
        byte[] pixels = new byte[stride * source.PixelHeight];
        bgra.CopyPixels(pixels, stride, 0);
        return pixels;
    }

    private static BitmapSource Create(int w, int h, PixelFormat format, byte[] pixels, int stride)
    {
        BitmapSource bitmap = BitmapSource.Create(w, h, 96, 96, format, null, pixels, stride);
        bitmap.Freeze();
        return bitmap;
    }
}

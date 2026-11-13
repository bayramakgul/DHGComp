using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace DHGComp.Controls;

// The binary image as a grid of cells (replaces ImagePanel of the WinForms version).
// Black cells are the 0-simplices; each is labelled with its pixel coordinates "x,y" (white)
// and its vertex number (yellow), the numbering used in the report.
public sealed class PixelGridView : FrameworkElement
{
    // Larger images are not drawn as a grid; a message is shown instead.
    public const int MaxSize = 64;

    public static readonly DependencyProperty MaskProperty = DependencyProperty.Register(
        nameof(Mask), typeof(bool[,]), typeof(PixelGridView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    // mask[x, y] == true for black (foreground) pixels.
    public bool[,]? Mask
    {
        get => (bool[,]?)GetValue(MaskProperty);
        set => SetValue(MaskProperty, value);
    }

    private static readonly Pen GridPen = CreateGridPen();
    private static readonly Typeface LabelTypeface = new("Segoe UI");
    private static readonly Typeface MessageTypeface =
        new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

    public PixelGridView()
    {
        // Crisp cell edges and grid lines (text is not affected).
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
    }

    protected override void OnRender(DrawingContext dc)
    {
        bool[,]? mask = Mask;
        double width = ActualWidth, height = ActualHeight;
        if (mask == null || width <= 0 || height <= 0)
            return;

        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        int w = mask.GetLength(0), h = mask.GetLength(1);
        if (w > MaxSize || h > MaxSize)
        {
            DrawMessage(dc, $"Image size bigger than {MaxSize}x{MaxSize}", width, height, pixelsPerDip);
            return;
        }
        if (w == 0 || h == 0)
            return;

        // Square cells, as large as fit, centred.
        double cell = Math.Floor(Math.Min(width / w, height / h));
        if (cell < 2)
            return;
        double left = Math.Floor((width - cell * w) / 2);
        double top = Math.Floor((height - cell * h) / 2);

        dc.DrawRectangle(Brushes.White, null, new Rect(left, top, cell * w, cell * h));

        var black = new StreamGeometry();
        using (StreamGeometryContext g = black.Open())
        {
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                    if (mask[i, j])
                        AddRectangle(g, left + i * cell, top + j * cell, cell, cell);
        }
        black.Freeze();
        dc.DrawGeometry(Brushes.Black, null, black);

        var lines = new StreamGeometry();
        using (StreamGeometryContext g = lines.Open())
        {
            for (int i = 0; i <= w; i++)
            {
                g.BeginFigure(new Point(left + i * cell, top), false, false);
                g.LineTo(new Point(left + i * cell, top + h * cell), true, false);
            }
            for (int j = 0; j <= h; j++)
            {
                g.BeginFigure(new Point(left, top + j * cell), false, false);
                g.LineTo(new Point(left + w * cell, top + j * cell), true, false);
            }
        }
        lines.Freeze();
        dc.DrawGeometry(null, GridPen, lines);

        // Labels only when they fit in a cell ("63,63" over the vertex number).
        double fontSize = Math.Min(12, (cell - 3) / 2.8);
        if (fontSize < 7)
            return;

        int vertex = 0;
        for (int j = 0; j < h; j++)
        {
            for (int i = 0; i < w; i++)
            {
                if (!mask[i, j])
                    continue;

                double x = left + i * cell + 2, y = top + j * cell + 1;
                FormattedText coordinates = Text($"{i},{j}", Brushes.White, fontSize, pixelsPerDip);
                dc.DrawText(coordinates, new Point(x, y));
                dc.DrawText(Text(vertex.ToString(CultureInfo.InvariantCulture), Brushes.Yellow, fontSize, pixelsPerDip),
                    new Point(x, y + coordinates.Height));
                vertex++;
            }
        }
    }

    private static FormattedText Text(string text, Brush brush, double size, double pixelsPerDip) =>
        new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, LabelTypeface, size, brush, pixelsPerDip);

    private static void DrawMessage(DrawingContext dc, string message, double width, double height, double pixelsPerDip)
    {
        var text = new FormattedText(message, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            MessageTypeface, 28, Brushes.Red, pixelsPerDip)
        {
            MaxTextWidth = Math.Max(1, width - 20),
            TextAlignment = TextAlignment.Center,
        };
        dc.DrawText(text, new Point(10, Math.Max(0, (height - text.Height) / 2)));
    }

    private static void AddRectangle(StreamGeometryContext g, double x, double y, double w, double h)
    {
        g.BeginFigure(new Point(x, y), true, true);
        g.LineTo(new Point(x + w, y), false, false);
        g.LineTo(new Point(x + w, y + h), false, false);
        g.LineTo(new Point(x, y + h), false, false);
    }

    private static Pen CreateGridPen()
    {
        var pen = new Pen(Brushes.Red, 1);
        pen.Freeze();
        return pen;
    }
}

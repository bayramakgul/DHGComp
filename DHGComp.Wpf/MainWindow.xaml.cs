using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DHGComp.Core;
using DHGComp.Imaging;
using Microsoft.Win32;

namespace DHGComp;

public partial class MainWindow : Window
{
    // Homology Groups / Only Betti Numbers / Only Euler Characteristic (parameter: CalcType).
    public static readonly RoutedUICommand CalculateCommand =
        new("Calculate", nameof(CalculateCommand), typeof(MainWindow));

    private const string ImageFilter = "Resim Dosyaları(*.jpg;*.jpeg;*.bmp;*.png;*.gif)|*.jpg;*.jpeg;*.bmp;*.png;*.gif";

    private readonly DHGCalc _calc = new();
    private readonly string _baseTitle;
    private ProcessedImage? _image;
    private string? _imagePath;
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();
        _baseTitle = Title;
        UpdateResultLabels(null);
        Loaded += Window_Loaded;
    }

    // ----- Opening an image -----

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // An image given on the command line ("Open with", drag onto the exe) is opened at start.
        string[] args = Environment.GetCommandLineArgs();
        if (args.Length > 1 && File.Exists(args[1]))
            await OpenImageAsync(args[1]);
    }

    private void Open_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = !_busy;

    private async void Open_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = ImageFilter, Multiselect = false };
        if (dialog.ShowDialog(this) == true)
            await OpenImageAsync(dialog.FileName);
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = !_busy && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (!_busy && e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
            await OpenImageAsync(files[0]);
    }

    // Loads and binarizes the image on a background thread, then shows it.
    public async Task<bool> OpenImageAsync(string path)
    {
        SetBusy(true);
        try
        {
            ProcessedImage image = await Task.Run(() => ImageProcessing.Process(ImageProcessing.Load(path)));
            ShowImage(image, path);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "The image could not be opened:\r\n" + ex.Message, _baseTitle,
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ShowImage(ProcessedImage image, string path)
    {
        _image = image;
        _imagePath = path;

        _calc.SetImage(image.Foreground);
        _calc.out_filename = Path.Combine(GetOutputDirectory(), Path.GetFileNameWithoutExtension(path) + "_part.txt");

        ShowBitmap(imgOriginal, image.Original, image);
        ShowBitmap(imgGrayscale, image.Grayscale, image);
        ShowBitmap(imgThreshold, image.ThresholdPreview, image);
        ShowBitmap(imgBinary, image.Binary, image);
        pixelGrid.Mask = image.Foreground;

        txtImageInfo.Text = $"{image.Width} × {image.Height} px   |   threshold {image.Threshold:0.##}   |   {_calc.simp0_list.Count} black pixels";
        UpdateResultLabels(null);
    }

    private static void ShowBitmap(Image control, ImageSource source, ProcessedImage image)
    {
        control.Source = source;
        // Small images are enlarged with sharp pixel edges; large ones are scaled smoothly.
        RenderOptions.SetBitmapScalingMode(control,
            image.Width <= 256 && image.Height <= 256 ? BitmapScalingMode.NearestNeighbor : BitmapScalingMode.HighQuality);
    }

    // Reports are written next to the executable, as before. If that folder is not writable
    // (e.g. the program is installed under Program Files), Documents\DHGComp is used.
    private static string GetOutputDirectory()
    {
        string exeDir = AppContext.BaseDirectory;
        if (IsWritable(exeDir))
            return exeDir;

        string docs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DHGComp");
        Directory.CreateDirectory(docs);
        return docs;
    }

    private static bool IsWritable(string folder)
    {
        try
        {
            using (File.Create(Path.Combine(folder, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose)) { }
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }

    // ----- Calculation -----

    private void Calculate_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = _image != null && !_busy;

    private async void Calculate_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var calcType = (CalcType)e.Parameter;
        if (!await RunCalculationAsync(calcType))
            return;

        if (calcType == CalcType.Homology)
            MessageBox.Show(this, "Process completed!", "Process", MessageBoxButton.OK, MessageBoxImage.Information);

        OpenReport();
    }

    // Runs the calculation on a background thread (the window keeps responding) and shows
    // the results in the status bar. Returns false if it failed.
    public async Task<bool> RunCalculationAsync(CalcType calcType)
    {
        _calc.bPrintMatrix = chkPrintMatrices.IsChecked == true;
        SetBusy(true);
        try
        {
            await Task.Run(() => _calc.StartCalc(calcType));
            UpdateResultLabels(calcType);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "The calculation failed:\r\n" + ex.Message, _baseTitle,
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        finally
        {
            SetBusy(false);
        }
    }

    // Opens the report (<image name>_part.txt) in the program associated with .txt files.
    private void OpenReport()
    {
        try
        {
            Process.Start(new ProcessStartInfo(_calc.out_filename) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"The report was written to\r\n{_calc.out_filename}\r\nbut could not be opened:\r\n{ex.Message}",
                _baseTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // While loading or calculating, the toolbar commands are disabled, so the image and the
    // calculator cannot change while a background task uses them.
    private void SetBusy(bool busy)
    {
        _busy = busy;
        chkPrintMatrices.IsEnabled = !busy;
        progress.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        Cursor = busy ? Cursors.Wait : null;
        ForceCursor = busy;

        string title = _imagePath == null ? _baseTitle : $"{_baseTitle} [ {_imagePath} ]";
        Title = busy ? title + " - working..." : title;

        CommandManager.InvalidateRequerySuggested();
    }

    // Shows the values computed by the last calculation; values it did not compute stay empty.
    private void UpdateResultLabels(CalcType? calcType)
    {
        bool betti = calcType is CalcType.Betti or CalcType.Homology;
        bool full = calcType == CalcType.Homology;

        SetLabel(lblH0, "H0", _calc.H0, betti);
        SetLabel(lblH1, "H1", _calc.H1, betti);
        SetLabel(lblH2, "H2", _calc.H2, full);
        SetLabel(lblKerDelta0, "KerDelta0", _calc.KerDelta0, betti);
        SetLabel(lblKerDelta1, "KerDelta1", _calc.KerDelta1, betti);
        SetLabel(lblKerDelta2, "KerDelta2", _calc.KerDelta2, full);
        SetLabel(lblKerDelta3, "KerDelta3", _calc.KerDelta3, full);
        SetLabel(lblImDelta0, "ImDelta0", _calc.ImDelta0, betti);
        SetLabel(lblImDelta1, "ImDelta1", _calc.ImDelta1, betti);
        SetLabel(lblImDelta2, "ImDelta2", _calc.ImDelta2, full);
        SetLabel(lblImDelta3, "ImDelta3", _calc.ImDelta3, full);
    }

    private static void SetLabel(TextBlock label, string name, int value, bool known) =>
        label.Text = known ? $"{name}: {value}" : $"{name}:";

    private void About_Click(object sender, RoutedEventArgs e) =>
        new AboutWindow { Owner = this }.ShowDialog();
}

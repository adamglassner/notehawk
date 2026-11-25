using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NoteHawk.Models;

namespace NoteHawk.Services;

/// <summary>
/// <c>NoteHawk.exe --screenshots &lt;folder&gt;</c>: captures every page in light and dark mode as PNGs, then exits.
/// Used by CI to publish screenshots.
/// </summary>
public static class Screenshots
{
    public static string? OutputFolder { get; set; }
    public static bool IsRunning => OutputFolder != null;

    private const double Width = 1440, Height = 920, Scale = 1.25;
    private static int _failures;

    public static async Task<int> RunAsync(MainWindow window)
    {
        Directory.CreateDirectory(OutputFolder!);
        window.WindowState = WindowState.Normal;
        window.Left = 0; window.Top = 0;
        window.Width = Width; window.Height = Height;
        await SelfTest.Idle();

        int n = 1;
        foreach (var mode in new[] { AppTheme.Light, AppTheme.Dark })
        {
            App.ApplyTheme(mode);
            foreach (var module in ModuleCatalog.All)
            {
                window.GoTo(module.Key, force: true);
                await Capture(window, $"{n++:00}-{module.Key}-{mode.ToLowerInvariant()}");
            }
            var (b, s, note) = SampleData.AllNotes.First(x => x.Note.Title == "Thanksgiving prep");
            window.OpenNote(b, s, note);
            await Capture(window, $"{n++:00}-notes-personal-{mode.ToLowerInvariant()}");
        }
        return _failures;
    }

    private static async Task Capture(Window window, string name)
    {
        try
        {
            await SelfTest.Idle();
            await Task.Delay(150);
            await SelfTest.Idle();
            var root = (FrameworkElement)window.Content;
            var bmp = new RenderTargetBitmap((int)(root.ActualWidth * Scale), (int)(root.ActualHeight * Scale), 96 * Scale, 96 * Scale, PixelFormats.Pbgra32);
            bmp.Render(root);
            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(bmp));
            using var file = File.Create(Path.Combine(OutputFolder!, name + ".png"));
            png.Save(file);
        }
        catch (Exception ex)
        {
            _failures++;
            File.WriteAllText(Path.Combine(OutputFolder!, name + ".error.txt"), ex.ToString());
        }
    }
}

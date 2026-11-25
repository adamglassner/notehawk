using System.Reflection;
using System.Windows;
using Microsoft.Win32;
using NoteHawk.Services;

namespace NoteHawk;

public partial class App : Application
{
    public static AppSettings Settings { get; private set; } = new();

    private static Mutex? _single;
    private static EventWaitHandle? _showSignal;

    /// <summary>True when the dark palette is currently applied.</summary>
    public static bool IsDark { get; private set; }

    /// <summary>Raised after the light/dark palette has been swapped.</summary>
    public static event EventHandler? ThemeChanged;

    /// <summary>Optional first page from the command line: <c>NoteHawk.exe --page notes</c>.</summary>
    public static string? StartPage { get; private set; }

    /// <summary>"0.1.0", from the &lt;Version&gt; in the .csproj (set by build.ps1 / CI).</summary>
    public static string VersionText { get; } = GetVersion();

    private static string GetVersion()
    {
        var info = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrEmpty(info)) return Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
        int plus = info.IndexOf('+');
        return plus > 0 ? info[..plus] : info;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        for (int i = 0; i + 1 < e.Args.Length; i++)
        {
            if (e.Args[i] == "--self-test") SelfTest.OutputFile = e.Args[i + 1];
            if (e.Args[i] == "--screenshots") Screenshots.OutputFolder = e.Args[i + 1];
            if (e.Args[i] == "--page") StartPage = e.Args[i + 1].ToLowerInvariant();
        }

        // One NoteHawk at a time: a second launch brings the running one to the front.
        if (!SelfTest.IsRunning && !Screenshots.IsRunning)
        {
            _single = new Mutex(true, @"Local\NoteHawk.SingleInstance", out bool first);
            _showSignal = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\NoteHawk.Show");
            if (!first)
            {
                _showSignal.Set();
                Shutdown();
                return;
            }
            var listener = new Thread(() =>
            {
                while (_showSignal.WaitOne())
                    Current.Dispatcher.BeginInvoke(() => NoteHawk.MainWindow.Instance?.BringToFront());
            }) { IsBackground = true, Name = "NoteHawk show listener" };
            listener.Start();
        }

        Settings = AppSettings.Load();
        ApplyTheme(Settings.Theme);

        // Last-resort safety net: report unexpected errors instead of closing the app.
        DispatcherUnhandledException += (_, args) =>
        {
            args.Handled = true;
            if (SelfTest.IsRunning) { SelfTest.RecordUnhandled(args.Exception); return; }
            MessageBox.Show("Something went wrong.\n\n" + args.Exception.Message, "NoteHawk", MessageBoxButton.OK, MessageBoxImage.Warning);
        };

        // Follow Windows when the user picked "Match Windows".
        SystemEvents.UserPreferenceChanged += (_, args) =>
        {
            if (args.Category == UserPreferenceCategory.General && Settings.Theme == AppTheme.System)
                Current.Dispatcher.Invoke(() => ApplyTheme(AppTheme.System));
        };

        base.OnStartup(e);
        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { _single?.ReleaseMutex(); } catch (ApplicationException) { /* not owned */ }
        base.OnExit(e);
    }

    /// <summary>Apply "System", "Light" or "Dark".</summary>
    public static void ApplyTheme(string mode)
    {
        bool dark = mode switch
        {
            AppTheme.Dark => true,
            AppTheme.Light => false,
            _ => SystemTheme.WindowsAppsUseDarkMode(),
        };
        IsDark = dark;
        Current.Resources.MergedDictionaries[0] = new ResourceDictionary
        {
            Source = new Uri(dark ? "pack://application:,,,/Themes/Dark.xaml" : "pack://application:,,,/Themes/Light.xaml", UriKind.Absolute),
        };
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void SetThemePreference(string mode)
    {
        Settings.Theme = mode;
        Settings.Save();
        ApplyTheme(mode);
    }
}

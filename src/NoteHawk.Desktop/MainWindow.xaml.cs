using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using NoteHawk.Models;
using NoteHawk.Services;
using NoteHawk.Views;

namespace NoteHawk;

public partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }

    private readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromSeconds(2.6) };
    private Notebook? _pendingBook;
    private (Section Section, Note Note)? _pendingNote;

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;
        _toastTimer.Tick += (_, _) => { _toastTimer.Stop(); HideToast(); };

        NavList.ItemsSource = ModuleCatalog.Sidebar;
        BuildNotebookList();

        App.ThemeChanged += OnThemeChanged;
        SourceInitialized += (_, _) => SystemTheme.ApplyTitleBar(this, App.IsDark);
        UpdateThemeButton();
        RestorePlacement();

        Loaded += async (_, _) =>
        {
            if (SelfTest.IsRunning) { Application.Current.Shutdown(await SelfTest.RunAsync(this)); return; }
            if (Screenshots.IsRunning) { Application.Current.Shutdown(await Screenshots.RunAsync(this)); return; }
        };

        GoTo(App.StartPage ?? "home");
    }

    // ----- Navigation -----

    private string? _currentKey;
    private bool _selecting;

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (_selecting) return; // GoTo is choosing the page itself
        if (sender is FrameworkElement { DataContext: ModuleInfo module } && module.Key != _currentKey)
            Show(module);
    }

    /// <summary>Selects a sidebar section (or Settings) by key. <paramref name="force"/> rebuilds the page even if it's showing.</summary>
    public void GoTo(string key, bool force = false)
    {
        var module = ModuleCatalog.Find(key) ?? ModuleCatalog.Home;
        // Selecting the sidebar item raises Nav_Checked; don't let that build the page a second time
        // (it would use up a pending notebook/note and then show the default one).
        _selecting = true;
        try
        {
            if (module == ModuleCatalog.Settings)
                foreach (var m in ModuleCatalog.Sidebar) m.IsSelected = false;
            else
                module.IsSelected = true;
        }
        finally { _selecting = false; }
        if (force || _currentKey != module.Key) Show(module);
    }

    private void Show(ModuleInfo module)
    {
        _currentKey = module.Key;
        FrameworkElement page = module.Key switch
        {
            "home" => new HomeView(),
            "notes" => NotesPage(),
            _ => new ComingSoonView(module),
        };
        SetPage(page);
    }

    private NotesView NotesPage()
    {
        var view = new NotesView(_pendingBook);
        if (_pendingNote is { } pending)
        {
            view.ShowSection(pending.Section);
            view.ShowNote(pending.Note);
        }
        _pendingBook = null;
        _pendingNote = null;
        return view;
    }

    /// <summary>Opens the Notes screen on a notebook (sidebar notebook list, Home).</summary>
    public void OpenNotebook(Notebook book)
    {
        if (PageHost.Content is NotesView view) { view.ShowBook(book); return; }
        _pendingBook = book;
        GoTo("notes", force: true);
    }

    /// <summary>Opens the Notes screen on one note (Home tiles and pinned notes).</summary>
    public void OpenNote(Notebook book, Section section, Note note)
    {
        _pendingBook = book;
        _pendingNote = (section, note);
        GoTo("notes", force: true);
    }

    public object? CurrentPage => PageHost.Content is ScrollViewer { Content: ContentControl { Content: var inner } } ? inner : PageHost.Content;

    private void SetPage(FrameworkElement page)
    {
        // The notes screen fills the window; every other page scrolls with the usual Hawkline margins.
        FrameworkElement host = page is NotesView
            ? page
            : new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(40, 32, 40, 36),
                Focusable = false,
                Content = new ContentControl { MaxWidth = 1240, Content = page, Focusable = false },
            };
        PageHost.Content = host;
        if (SelfTest.IsRunning || Screenshots.IsRunning) return;

        // A short fade-and-rise so page changes feel smooth.
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        host.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)) { EasingFunction = ease });
        PageShift.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty,
            new DoubleAnimation(10, 0, TimeSpan.FromMilliseconds(220)) { EasingFunction = ease });
    }

    private void BuildNotebookList()
    {
        NotebookList.Children.Clear();
        foreach (var book in SampleData.Notebooks)
        {
            var row = new DockPanel();
            var count = new TextBlock { Text = book.NoteCount.ToString(), FontSize = 11.5, VerticalAlignment = VerticalAlignment.Center };
            count.SetResourceReference(TextBlock.ForegroundProperty, "Brush.SidebarMuted");
            DockPanel.SetDock(count, Dock.Right);
            row.Children.Add(count);
            row.Children.Add(new Border { Width = 18, Margin = new Thickness(0, 0, 12, 0), Child = Ui.Dot(book.Color, 10).Also(d => d.HorizontalAlignment = HorizontalAlignment.Center) });
            row.Children.Add(new TextBlock { Text = book.Name, FontSize = 14, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis });
            var b = new Button { Style = (Style)FindResource("SidebarButton"), Content = row, HorizontalContentAlignment = HorizontalAlignment.Stretch, Padding = new Thickness(14, 8, 14, 8), ToolTip = $"{book.Sections.Length} sections" };
            System.Windows.Automation.AutomationProperties.SetName(b, book.Name);
            b.Click += (_, _) => OpenNotebook(book);
            NotebookList.Children.Add(b);
        }
    }

    // ----- Sidebar buttons -----

    private void NewNote_Click(object sender, RoutedEventArgs e) => Toaster.Soon("New notes");
    private void NewNotebook_Click(object sender, RoutedEventArgs e) => Toaster.Soon("New notebooks");
    private void Search_Click(object sender, MouseButtonEventArgs e) => Toaster.Soon("Search");
    private void Settings_Click(object sender, RoutedEventArgs e) => GoTo("settings");
    private void Theme_Click(object sender, RoutedEventArgs e) => App.SetThemePreference(App.IsDark ? AppTheme.Light : AppTheme.Dark);

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.N: Toaster.Soon("New notes"); e.Handled = true; break;
                case Key.F: Toaster.Soon("Search"); e.Handled = true; break;
                case Key.OemComma: GoTo("settings"); e.Handled = true; break;
            }
        }
        base.OnPreviewKeyDown(e);
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        UpdateThemeButton();
        SystemTheme.ApplyTitleBar(this, App.IsDark);
    }

    private void UpdateThemeButton()
    {
        // Sun when dark (click for light), moon when light (click for dark).
        ThemeGlyph.Text = App.IsDark ? Ic.Sun : Ic.Moon;
        ThemeLabel.Text = App.IsDark ? "Light mode" : "Dark mode";
        ThemeButton.ToolTip = App.IsDark ? "Switch to light mode" : "Switch to dark mode";
    }

    // ----- Toast -----

    public void ShowToast(string message)
    {
        ToastText.Text = message;
        Toast.Visibility = Visibility.Visible;
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        Toast.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(160)));
        ToastShift.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(200)) { EasingFunction = ease });
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private void HideToast()
    {
        var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(220));
        fade.Completed += (_, _) => { if (Toast.Opacity == 0) Toast.Visibility = Visibility.Collapsed; };
        Toast.BeginAnimation(OpacityProperty, fade);
    }

    public void BringToFront()
    {
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Show();
        Activate();
    }

    // ----- Window placement -----

    private void RestorePlacement()
    {
        var s = App.Settings;
        if (s.WindowWidth is double w && s.WindowHeight is double h && s.WindowLeft is double l && s.WindowTop is double t)
        {
            // Only restore if it's still on screen
            if (l + 100 < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth && t >= SystemParameters.VirtualScreenTop - 10 &&
                l + w > SystemParameters.VirtualScreenLeft + 100 && t < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 100)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = l; Top = t; Width = Math.Max(w, MinWidth); Height = Math.Max(h, MinHeight);
            }
        }
        if (s.WindowMaximized) WindowState = WindowState.Maximized;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        var s = App.Settings;
        var r = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
        s.WindowLeft = r.Left; s.WindowTop = r.Top; s.WindowWidth = r.Width; s.WindowHeight = r.Height;
        s.WindowMaximized = WindowState == WindowState.Maximized;
        s.Save();
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        App.ThemeChanged -= OnThemeChanged;
        base.OnClosed(e);
        Application.Current.Shutdown();
    }
}

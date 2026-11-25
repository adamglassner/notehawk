using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NoteHawk.Models;
using NoteHawk.Services;

namespace NoteHawk.Views;

/// <summary>
/// The main notes screen: OneNote-style section tabs across the top, an Evernote-style list of notes on the left
/// and the page on the right. Shows sample notebooks; browsing works, editing is coming soon.
/// </summary>
public sealed class NotesView : UserControl
{
    public Notebook Book { get; private set; }
    public Section Section { get; private set; }
    public Note? Current { get; private set; }

    private readonly StackPanel _tabs = new() { Orientation = Orientation.Horizontal };
    private readonly StackPanel _list = new();
    private readonly TextBlock _listTitle = new() { Style = Ui.S("CardTitle"), TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly TextBlock _listCount = Ui.Text("", muted: true, size: 12.5);
    private readonly ContentControl _page = new() { Focusable = false };
    private readonly TextBlock _crumbs = Ui.Text("", muted: true, size: 12.5);
    private readonly TextBlock _status = Ui.Text("", muted: true, size: 11.5);
    private readonly ScrollViewer _pageScroller = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Focusable = false };

    public NotesView(Notebook? book = null)
    {
        Book = book ?? SampleData.Notebooks[1];
        Section = Book.Sections[0];

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.Children.Add(BuildTabBar().At(0, 0));

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(340) });
        body.ColumnDefinitions.Add(new ColumnDefinition());
        body.Children.Add(BuildListPane().At(0));
        body.Children.Add(BuildEditor().At(1));
        root.Children.Add(body.At(0, 1));

        Content = root;
        ShowBook(Book);
    }

    // ===== Section tabs (OneNote) =====

    private UIElement BuildTabBar()
    {
        var bar = new Border { BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(20, 12, 20, 0) };
        bar.SetResourceReference(Border.BackgroundProperty, "Brush.Bg");
        bar.SetResourceReference(Border.BorderBrushProperty, "Brush.Border");
        var dock = new DockPanel();

        var right = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        right.Children.Add(Ui.SearchBox("Search this notebook…", 260).Also(s => s.Margin = new Thickness(0, 0, 8, 0)));
        right.Children.Add(Ui.ToolButton("Notebook settings", Ic.More, "Notebook options"));
        DockPanel.SetDock(right, Dock.Right);
        dock.Children.Add(right);

        var notebookName = new Button { Style = Ui.S("BaseButton"), Padding = new Thickness(8, 6, 10, 6), Margin = new Thickness(0, 0, 14, 8), Tag = "Switching notebooks here" };
        notebookName.Click += (s, _) => Toaster.Soon(s);
        dock.Children.Add(notebookName.Also(b => DockPanel.SetDock(b, Dock.Left)));
        _notebookButton = notebookName;

        dock.Children.Add(new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Focusable = false, Content = _tabs, VerticalAlignment = VerticalAlignment.Bottom,
        });
        bar.Child = dock;
        return bar;
    }

    private Button? _notebookButton;

    private void BuildTabs()
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(Ui.Dot(Book.Color, 12).Also(d => d.Margin = new Thickness(0, 0, 9, 0)));
        row.Children.Add(Ui.Text(Book.Name, size: 16, weight: FontWeights.Bold).Also(t => t.FontFamily = (FontFamily)FindResource("DisplayFont")));
        row.Children.Add(Ui.Icon(Ic.ChevronDown, 12, "Brush.TextMuted").Also(i => i.Margin = new Thickness(8, 2, 0, 0)));
        _notebookButton!.Content = row;

        _tabs.Children.Clear();
        foreach (var section in Book.Sections)
            _tabs.Children.Add(SectionTab(section));

        var add = new Button { Style = Ui.S("BaseButton"), Padding = new Thickness(12, 8, 12, 8), Tag = "Adding sections", ToolTip = "Add section", VerticalAlignment = VerticalAlignment.Bottom };
        add.Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { Ui.Icon(Ic.Add, 13, "Brush.TextMuted"), Ui.Text("Add section", muted: true, size: 13).Also(t => t.Margin = new Thickness(6, 0, 0, 0)) } };
        add.Click += (s, _) => Toaster.Soon(s);
        _tabs.Children.Add(add);
    }

    private UIElement SectionTab(Section section)
    {
        bool selected = section == Section;
        var tab = new Border
        {
            CornerRadius = new CornerRadius(10, 10, 0, 0),
            BorderThickness = new Thickness(1, 1, 1, 0),
            Margin = new Thickness(0, 0, 4, -1),
            Cursor = Cursors.Hand,
        };
        tab.SetResourceReference(Border.BorderBrushProperty, selected ? "Brush.Border" : "Brush.Bg");
        tab.SetResourceReference(Border.BackgroundProperty, selected ? "Brush.Surface" : "Brush.Bg");

        var stack = new StackPanel();
        stack.Children.Add(new Border { Height = 3, CornerRadius = new CornerRadius(2), Margin = new Thickness(10, 0, 10, 0), Background = Ui.Hex(section.Color), Opacity = selected ? 1 : 0.55 });
        var label = Ui.Text(section.Name, muted: !selected, size: 13.5, weight: selected ? FontWeights.SemiBold : FontWeights.Normal);
        label.Margin = new Thickness(16, 8, 16, 10);
        stack.Children.Add(label);
        tab.Child = stack;
        tab.MouseEnter += (_, _) => { if (section != Section) tab.SetResourceReference(Border.BackgroundProperty, "Brush.SurfaceAlt"); };
        tab.MouseLeave += (_, _) => { if (section != Section) tab.SetResourceReference(Border.BackgroundProperty, "Brush.Bg"); };
        tab.MouseLeftButtonUp += (_, _) => ShowSection(section);
        System.Windows.Automation.AutomationProperties.SetName(tab, section.Name);
        return tab;
    }

    // ===== Note list (Evernote) =====

    private UIElement BuildListPane()
    {
        var pane = new Border { BorderThickness = new Thickness(0, 0, 1, 0) };
        pane.SetResourceReference(Border.BackgroundProperty, "Brush.Surface");
        pane.SetResourceReference(Border.BorderBrushProperty, "Brush.Border");
        var dock = new DockPanel();

        var head = new DockPanel { Margin = new Thickness(20, 18, 14, 12) };
        var tools = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top };
        tools.Children.Add(Ui.ToolButton("Sorting notes", Ic.Sort, "Sort"));
        tools.Children.Add(Ui.ToolButton("Filtering notes", Ic.Filter, "Filter"));
        tools.Children.Add(Ui.ToolButton("List and card views", Ic.Grid, "Change view"));
        DockPanel.SetDock(tools, Dock.Right);
        head.Children.Add(tools);
        var titles = new StackPanel();
        titles.Children.Add(_listTitle);
        titles.Children.Add(_listCount.Also(t => t.Margin = new Thickness(0, 2, 0, 0)));
        head.Children.Add(titles);
        DockPanel.SetDock(head, Dock.Top);
        dock.Children.Add(head);

        var add = Ui.SoonButton("New pages", Ic.NoteAdd, "New page");
        add.HorizontalAlignment = HorizontalAlignment.Stretch;
        add.Margin = new Thickness(14, 0, 14, 10);
        DockPanel.SetDock(add, Dock.Top);
        dock.Children.Add(add);

        dock.Children.Add(new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Focusable = false, Content = _list, Padding = new Thickness(10, 0, 10, 12) });
        pane.Child = dock;
        return pane;
    }

    private void BuildList()
    {
        _listTitle.Text = Section.Name;
        _listCount.Text = Section.Notes.Length == 1 ? "1 page" : $"{Section.Notes.Length} pages";
        _list.Children.Clear();
        foreach (var note in Section.Notes.OrderByDescending(n => n.Pinned).ThenByDescending(n => n.Edited))
            _list.Children.Add(NoteCard(note));
    }

    private UIElement NoteCard(Note note)
    {
        bool selected = note == Current;
        var card = new Border { CornerRadius = new CornerRadius(12), Padding = new Thickness(14, 12, 14, 12), Margin = new Thickness(0, 0, 0, 4), Cursor = Cursors.Hand, BorderThickness = new Thickness(1) };
        card.SetResourceReference(Border.BackgroundProperty, selected ? "Brush.AccentSoft" : "Brush.Surface");
        card.SetResourceReference(Border.BorderBrushProperty, selected ? "Brush.AccentSoft" : "Brush.Surface");

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var text = new StackPanel();
        var titleRow = new DockPanel();
        if (note.Pinned) titleRow.Children.Add(Ui.Icon(Ic.Pin, 12, "Brush.Accent", filled: true).Also(i => { i.Margin = new Thickness(0, 0, 6, 0); DockPanel.SetDock(i, Dock.Left); }));
        titleRow.Children.Add(Ui.Text(note.Title, size: 14, weight: FontWeights.SemiBold));
        text.Children.Add(titleRow);
        var snippet = Ui.Wrap(note.Snippet, muted: true, size: 12.5, lineHeight: 18);
        snippet.MaxHeight = 36;
        snippet.TextTrimming = TextTrimming.CharacterEllipsis;
        snippet.Margin = new Thickness(0, 4, 0, 8);
        text.Children.Add(snippet);
        var foot = new StackPanel { Orientation = Orientation.Horizontal };
        foot.Children.Add(Ui.Text(SampleData.Friendly(note.Edited), muted: true, size: 11.5));
        if (note.Tags.Length > 0) foot.Children.Add(Ui.Chip(note.Tags[0], Ic.Tag).Also(c => c.Margin = new Thickness(10, 0, 0, 0)));
        text.Children.Add(foot);
        grid.Children.Add(text.At(0));

        if (note.HasImage)
        {
            var thumb = new Border { Width = 54, Height = 54, CornerRadius = new CornerRadius(10), Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Top };
            thumb.SetResourceReference(Border.BackgroundProperty, "Brush.Skeleton");
            thumb.Child = Ui.Icon(Ic.Image, 18, "Brush.TextMuted").Also(i => i.HorizontalAlignment = HorizontalAlignment.Center);
            grid.Children.Add(thumb.At(1));
        }
        card.Child = grid;
        card.MouseEnter += (_, _) => { if (note != Current) card.SetResourceReference(Border.BackgroundProperty, "Brush.SurfaceAlt"); };
        card.MouseLeave += (_, _) => { if (note != Current) card.SetResourceReference(Border.BackgroundProperty, "Brush.Surface"); };
        card.MouseLeftButtonUp += (_, _) => ShowNote(note);
        System.Windows.Automation.AutomationProperties.SetName(card, note.Title);
        return card;
    }

    // ===== Editor =====

    private UIElement BuildEditor()
    {
        var dock = new DockPanel();
        dock.SetResourceReference(Panel.BackgroundProperty, "Brush.Paper");

        // Top: breadcrumb + note actions
        var top = new DockPanel { Margin = new Thickness(24, 14, 20, 8) };
        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        actions.Children.Add(Ui.Pill("PREVIEW").Also(p => { p.Margin = new Thickness(0, 0, 10, 0); p.ToolTip = "NoteHawk is a preview: browsing sample notes works, editing is coming soon."; }));
        actions.Children.Add(Ui.ToolButton("Page history", Ic.History, "Page history"));
        actions.Children.Add(Ui.ToolButton("Pinning notes", Ic.Pin, "Pin to Home"));
        actions.Children.Add(Ui.ToolButton("Favorites", Ic.Star, "Add to favorites"));
        actions.Children.Add(Ui.SoonButton("Sharing", Ic.Share, "Share", "PrimaryButton").Also(b => { b.Margin = new Thickness(8, 0, 6, 0); b.Padding = new Thickness(14, 7, 14, 7); }));
        actions.Children.Add(Ui.ToolButton("More note options", Ic.More, "More"));
        DockPanel.SetDock(actions, Dock.Right);
        top.Children.Add(actions);
        top.Children.Add(_crumbs);
        DockPanel.SetDock(top, Dock.Top);
        dock.Children.Add(top);

        var format = BuildFormatBar();
        DockPanel.SetDock(format, Dock.Top);
        dock.Children.Add(format);

        // Bottom status line
        var status = new Border { BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(24, 7, 24, 7) };
        status.SetResourceReference(Border.BorderBrushProperty, "Brush.Border");
        var statusRow = new DockPanel();
        var saved = new StackPanel { Orientation = Orientation.Horizontal };
        saved.Children.Add(Ui.Icon(Ic.CloudDone, 13, "Brush.TextMuted").Also(i => i.Margin = new Thickness(0, 0, 6, 0)));
        saved.Children.Add(Ui.Text("Sample note · not saved", muted: true, size: 11.5));
        DockPanel.SetDock(saved, Dock.Right);
        statusRow.Children.Add(saved);
        statusRow.Children.Add(_status);
        status.Child = statusRow;
        DockPanel.SetDock(status, Dock.Bottom);
        dock.Children.Add(status);

        _pageScroller.Content = _page;
        dock.Children.Add(_pageScroller);
        return dock;
    }

    private UIElement BuildFormatBar()
    {
        var bar = new Border { CornerRadius = new CornerRadius(12), BorderThickness = new Thickness(1), Padding = new Thickness(6, 3, 6, 3), Margin = new Thickness(20, 0, 20, 6) };
        bar.SetResourceReference(Border.BackgroundProperty, "Brush.Surface");
        bar.SetResourceReference(Border.BorderBrushProperty, "Brush.Border");
        var row = new WrapPanel();
        row.Children.Add(Ui.ToolButton("Undo", Ic.Undo, "Undo (Ctrl+Z)"));
        row.Children.Add(Ui.ToolButton("Redo", Ic.Redo, "Redo (Ctrl+Y)"));
        row.Children.Add(Ui.Separator());
        row.Children.Add(FakeCombo("Paragraph styles", "Normal text", 120));
        row.Children.Add(FakeCombo("Fonts", "Segoe UI", 110));
        row.Children.Add(FakeCombo("Font sizes", "14", 54));
        row.Children.Add(Ui.Separator());
        row.Children.Add(Ui.ToolButton("Bold", Ic.Bold, "Bold (Ctrl+B)"));
        row.Children.Add(Ui.ToolButton("Italic", Ic.Italic, "Italic (Ctrl+I)"));
        row.Children.Add(Ui.ToolButton("Underline", Ic.Underline, "Underline (Ctrl+U)"));
        row.Children.Add(Ui.ToolButton("Strikethrough", Ic.Strike, "Strikethrough"));
        row.Children.Add(Ui.ToolButton("Text color", Ic.TextColor, "Text color"));
        row.Children.Add(Ui.ToolButton("Highlighting", Ic.Highlight, "Highlight"));
        row.Children.Add(Ui.Separator());
        row.Children.Add(Ui.ToolButton("Bulleted lists", Ic.Bullets, "Bulleted list"));
        row.Children.Add(Ui.ToolButton("Numbered lists", Ic.Numbers, "Numbered list"));
        row.Children.Add(Ui.ToolButton("Checklists", Ic.Checked, "Checklist"));
        row.Children.Add(Ui.ToolButton("Indenting", Ic.Indent, "Indent"));
        row.Children.Add(Ui.Separator());
        row.Children.Add(Ui.ToolButton("Links", Ic.Link, "Insert link (Ctrl+K)"));
        row.Children.Add(Ui.ToolButton("Tables", Ic.Table, "Insert table"));
        row.Children.Add(Ui.ToolButton("Images", Ic.Image, "Insert image"));
        row.Children.Add(Ui.ToolButton("Attachments", Ic.Attach, "Attach a file"));
        row.Children.Add(Ui.ToolButton("Code blocks", Ic.Code, "Code block"));
        row.Children.Add(Ui.Separator());
        row.Children.Add(Ui.ToolButton("Drawing and ink", Ic.Ink, "Draw"));
        row.Children.Add(Ui.ToolButton("Audio recording", Ic.Mic, "Record audio"));
        row.Children.Add(Ui.ToolButton("Reminders", Ic.Alert, "Add reminder"));
        bar.Child = row;
        return bar;
    }

    private static Button FakeCombo(string feature, string value, double width)
    {
        var b = new Button { Style = Ui.S("BaseButton"), Tag = feature, Width = width, Padding = new Thickness(10, 6, 8, 6), MinHeight = 32, HorizontalContentAlignment = HorizontalAlignment.Stretch, ToolTip = feature, Margin = new Thickness(1, 0, 1, 0) };
        var dock = new DockPanel();
        dock.Children.Add(Ui.Icon(Ic.ChevronDown, 10, "Brush.TextMuted").Also(i => DockPanel.SetDock(i, Dock.Right)));
        dock.Children.Add(Ui.Text(value, size: 13));
        b.Content = dock;
        b.Click += (s, _) => Toaster.Soon(s);
        return b;
    }

    // ===== State =====

    public void ShowBook(Notebook book)
    {
        Book = book;
        Section = book.Sections[0];
        BuildTabs();
        ShowSection(Section);
    }

    public void ShowSection(Section section)
    {
        Section = section;
        BuildTabs();
        Current = section.Notes.OrderByDescending(n => n.Pinned).ThenByDescending(n => n.Edited).FirstOrDefault();
        BuildList();
        RenderPage();
    }

    public void ShowNote(Note note)
    {
        Current = note;
        BuildList();
        RenderPage();
    }

    private void RenderPage()
    {
        _crumbs.Text = $"{Book.Name}  ›  {Section.Name}";
        _pageScroller.ScrollToTop();
        if (Current is not Note note)
        {
            _page.Content = Ui.Text("No pages in this section yet.", muted: true).Also(t => { t.HorizontalAlignment = HorizontalAlignment.Center; t.Margin = new Thickness(0, 80, 0, 0); });
            _status.Text = "";
            return;
        }
        int words = note.Body.Sum(b => b.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length) + note.Title.Split(' ').Length;
        _status.Text = $"Edited {SampleData.Friendly(note.Edited)}  ·  {words} words";

        var canvas = new StackPanel { MaxWidth = 760, Margin = new Thickness(56, 26, 56, 60) };
        var title = new TextBlock { Text = note.Title, FontSize = 32, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap };
        title.SetResourceReference(TextBlock.FontFamilyProperty, "DisplayFont");
        title.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Text");
        title.MouseLeftButtonDown += (_, _) => Toaster.Soon("Editing notes");
        canvas.Children.Add(title);

        // OneNote-style date line, then notebook and tags
        var meta = new WrapPanel { Margin = new Thickness(0, 8, 0, 4) };
        meta.Children.Add(Ui.Text(note.Edited.ToString("dddd, MMMM d, yyyy   h:mm tt"), muted: true, size: 12.5).Also(t => t.Margin = new Thickness(0, 0, 14, 6)));
        var bookChip = Ui.Chip(Book.Name, Ic.Notebook);
        bookChip.Margin = new Thickness(0, 0, 6, 6);
        meta.Children.Add(bookChip);
        foreach (var tag in note.Tags) meta.Children.Add(Ui.Chip(tag, Ic.Tag).Also(c => c.Margin = new Thickness(0, 0, 6, 6)));
        var addTag = new Button { Style = Ui.S("LinkButton"), Content = "+ Tag", Tag = "Tagging notes", Margin = new Thickness(0, -4, 0, 2) };
        addTag.Click += (s, _) => Toaster.Soon(s);
        meta.Children.Add(addTag);
        canvas.Children.Add(meta);
        canvas.Children.Add(Ui.Separator(vertical: false).Also(s => s.Margin = new Thickness(0, 6, 0, 18)));

        foreach (var block in note.Body) canvas.Children.Add(RenderBlock(block));

        var hint = new Border { CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 28, 0, 0), Cursor = Cursors.IBeam };
        hint.SetResourceReference(Border.BorderBrushProperty, "Brush.Border");
        var hintRow = new StackPanel { Orientation = Orientation.Horizontal };
        hintRow.Children.Add(Ui.Icon(Ic.Compose, 14, "Brush.TextMuted").Also(i => i.Margin = new Thickness(0, 0, 8, 0)));
        hintRow.Children.Add(Ui.Text("Click to keep writing… (editing is coming soon)", muted: true));
        hint.Child = hintRow;
        hint.MouseLeftButtonUp += (_, _) => Toaster.Soon("Editing notes");
        canvas.Children.Add(hint);

        _page.Content = canvas;
    }

    private static UIElement RenderBlock(Block block)
    {
        switch (block.Kind)
        {
            case BlockKind.Heading:
                var h = Ui.Wrap(block.Text, size: 19);
                h.FontWeight = FontWeights.Bold;
                h.SetResourceReference(TextBlock.FontFamilyProperty, "DisplayFont");
                h.Margin = new Thickness(0, 14, 0, 6);
                return h;

            case BlockKind.Bullet:
                var b = new DockPanel { Margin = new Thickness(4, 3, 0, 3) };
                b.Children.Add(new Border { Width = 6, Height = 6, CornerRadius = new CornerRadius(3), Margin = new Thickness(0, 9, 12, 0), VerticalAlignment = VerticalAlignment.Top }
                    .Also(d => { d.SetResourceReference(Border.BackgroundProperty, "Brush.Accent"); DockPanel.SetDock(d, Dock.Left); }));
                b.Children.Add(Ui.Wrap(block.Text, size: 15, lineHeight: 24));
                return b;

            case BlockKind.Todo:
            case BlockKind.Done:
                bool done = block.Kind == BlockKind.Done;
                var t = new DockPanel { Margin = new Thickness(0, 3, 0, 3), Cursor = Cursors.Hand };
                t.Children.Add(Ui.Icon(done ? Ic.Checked : Ic.Unchecked, 18, done ? "Brush.Accent" : "Brush.TextMuted", filled: done)
                    .Also(i => { i.Margin = new Thickness(0, 2, 10, 0); i.VerticalAlignment = VerticalAlignment.Top; DockPanel.SetDock(i, Dock.Left); }));
                var tt = Ui.Wrap(block.Text, muted: done, size: 15, lineHeight: 24);
                if (done) tt.TextDecorations = TextDecorations.Strikethrough;
                t.Children.Add(tt);
                t.MouseLeftButtonUp += (_, _) => Toaster.Soon("Checking off tasks");
                return t;

            case BlockKind.Quote:
                var q = new Border { CornerRadius = new CornerRadius(10), Padding = new Thickness(16, 12, 16, 12), Margin = new Thickness(0, 12, 0, 6) };
                q.SetResourceReference(Border.BackgroundProperty, "Brush.Marker");
                var qr = new DockPanel();
                qr.Children.Add(Ui.Icon(Ic.Idea, 16, "Brush.Amber", filled: true).Also(i => { i.Margin = new Thickness(0, 2, 10, 0); i.VerticalAlignment = VerticalAlignment.Top; DockPanel.SetDock(i, Dock.Left); }));
                qr.Children.Add(Ui.Wrap(block.Text, size: 14.5, lineHeight: 22));
                q.Child = qr;
                return q;

            case BlockKind.Image:
                var img = new StackPanel { Margin = new Thickness(0, 12, 0, 8) };
                var frame = new Border { Height = 190, CornerRadius = new CornerRadius(12) };
                frame.SetResourceReference(Border.BackgroundProperty, "Brush.Skeleton");
                var inner = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                inner.Children.Add(Ui.Icon(Ic.Image, 30, "Brush.TextMuted").Also(i => i.HorizontalAlignment = HorizontalAlignment.Center));
                inner.Children.Add(Ui.Text("Images in notes are coming soon", muted: true, size: 12.5).Also(x => x.Margin = new Thickness(0, 8, 0, 0)));
                frame.Child = inner;
                img.Children.Add(frame);
                img.Children.Add(Ui.Text(block.Text, muted: true, size: 12).Also(x => { x.HorizontalAlignment = HorizontalAlignment.Center; x.Margin = new Thickness(0, 6, 0, 0); }));
                return img;

            case BlockKind.Table:
                var wrap = new StackPanel { Margin = new Thickness(0, 12, 0, 8) };
                wrap.Children.Add(Ui.Text(block.Text, size: 13, weight: FontWeights.SemiBold).Also(x => x.Margin = new Thickness(0, 0, 0, 6)));
                var grid = new Grid();
                var rnd = new Random(block.Text.Length);
                for (int c = 0; c < 3; c++) grid.ColumnDefinitions.Add(new ColumnDefinition());
                for (int r = 0; r < 4; r++)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    for (int c = 0; c < 3; c++)
                    {
                        var cell = new Border { BorderThickness = new Thickness(c == 0 ? 1 : 0, r == 0 ? 1 : 0, 1, 1), Padding = new Thickness(12, 10, 12, 10) };
                        cell.SetResourceReference(Border.BorderBrushProperty, "Brush.Border");
                        if (r == 0) cell.SetResourceReference(Border.BackgroundProperty, "Brush.SurfaceAlt");
                        cell.Child = Ui.Skeleton(rnd.Next(40, 120), r == 0 ? 8 : 9, new Thickness(0));
                        grid.Children.Add(cell.At(c, r));
                    }
                }
                wrap.Children.Add(new Border { CornerRadius = new CornerRadius(8), ClipToBounds = true, Child = grid });
                return wrap;

            default:
                return Ui.Wrap(block.Text, size: 15, lineHeight: 25).Also(p => p.Margin = new Thickness(0, 2, 0, 8));
        }
    }
}

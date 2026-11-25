using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using NoteHawk.Models;
using NoteHawk.Services;

namespace NoteHawk.Views;

/// <summary>Evernote-style Home: greeting with quick actions, recent notes, scratch pad, tasks, pinned notes,
/// notebooks and tags. Uses the sample notebooks; actions are coming soon.</summary>
public sealed class HomeView : UserControl
{
    public HomeView()
    {
        var root = new StackPanel();
        root.Children.Add(Hero());

        // Recent notes
        root.Children.Add(SectionHeader("Recent notes", "All notes", () => MainWindow.Instance?.GoTo("notes")));
        var recent = new UniformGrid { Columns = 4, Margin = new Thickness(0, 0, -14, 22) };
        foreach (var (book, section, note) in SampleData.AllNotes.OrderByDescending(x => x.Note.Edited).Take(4))
            recent.Children.Add(NoteTile(book, section, note).Also(t => t.Margin = new Thickness(0, 0, 14, 0)));
        root.Children.Add(recent);

        // Scratch pad + tasks
        var mid = new Grid { Margin = new Thickness(0, 0, 0, 18) };
        mid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
        mid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        mid.ColumnDefinitions.Add(new ColumnDefinition());
        mid.Children.Add(ScratchPad().At(0));
        mid.Children.Add(Tasks().At(2));
        root.Children.Add(mid);

        // Pinned + notebooks + tags
        var lower = new Grid();
        lower.ColumnDefinitions.Add(new ColumnDefinition());
        lower.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        lower.ColumnDefinitions.Add(new ColumnDefinition());
        lower.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        lower.ColumnDefinitions.Add(new ColumnDefinition());
        lower.Children.Add(Pinned().At(0));
        lower.Children.Add(Notebooks().At(2));
        lower.Children.Add(Tags().At(4));
        root.Children.Add(lower);

        Content = root;
    }

    private static UIElement Hero()
    {
        var hero = new Border { CornerRadius = new CornerRadius(18), Padding = new Thickness(30, 26, 30, 26), Margin = new Thickness(0, 0, 0, 26) };
        hero.SetResourceReference(Border.BackgroundProperty, "Brush.Hero");
        var dock = new DockPanel();

        var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        actions.Children.Add(HeroAction(Ic.NoteAdd, "New note", "New notes"));
        actions.Children.Add(HeroAction(Ic.Task, "New task", "Tasks"));
        actions.Children.Add(HeroAction(Ic.Globe, "Clip web page", "The web clipper"));
        actions.Children.Add(HeroAction(Ic.Mic, "Record audio", "Audio notes"));
        DockPanel.SetDock(actions, Dock.Right);
        dock.Children.Add(actions);

        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        int hour = DateTime.Now.Hour;
        string greeting = hour < 12 ? "Good morning" : hour < 17 ? "Good afternoon" : "Good evening";
        var title = new TextBlock { Text = greeting, FontSize = 30, FontWeight = FontWeights.Bold };
        title.SetResourceReference(TextBlock.FontFamilyProperty, "DisplayFont");
        title.SetResourceReference(TextBlock.ForegroundProperty, "Brush.HeroText");
        text.Children.Add(title);
        text.Children.Add(Ui.Text($"{DateTime.Now:dddd, MMMM d}  ·  {SampleData.AllNotes.Count()} sample notes in {SampleData.Notebooks.Length} notebooks", size: 14, brushKey: "Brush.HeroMuted").Also(t => t.Margin = new Thickness(0, 6, 0, 0)));
        dock.Children.Add(text);
        hero.Child = dock;
        return hero;
    }

    private static Button HeroAction(string glyph, string label, string feature)
    {
        var b = new Button { Style = Ui.S("BaseButton"), Tag = feature, Width = 104, Padding = new Thickness(8, 12, 8, 10), Margin = new Thickness(8, 0, 0, 0) };
        b.SetResourceReference(BackgroundProperty, "Brush.SidebarCard");
        var sp = new StackPanel();
        sp.Children.Add(Ui.Icon(glyph, 22, "Brush.Highlight").Also(i => i.HorizontalAlignment = HorizontalAlignment.Center));
        sp.Children.Add(Ui.Text(label, size: 12.5, brushKey: "Brush.HeroText").Also(t => { t.HorizontalAlignment = HorizontalAlignment.Center; t.Margin = new Thickness(0, 7, 0, 0); }));
        b.Content = sp;
        b.Click += (s, _) => Toaster.Soon(s);
        System.Windows.Automation.AutomationProperties.SetName(b, label);
        return b;
    }

    private static DockPanel SectionHeader(string title, string? link, Action? onLink)
    {
        var dock = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
        if (link != null)
        {
            var b = new Button { Style = Ui.S("LinkButton"), Content = link };
            b.Click += (_, _) => onLink?.Invoke();
            DockPanel.SetDock(b, Dock.Right);
            dock.Children.Add(b);
        }
        dock.Children.Add(new TextBlock { Text = title, Style = Ui.S("CardTitle"), VerticalAlignment = VerticalAlignment.Center });
        return dock;
    }

    private static UIElement NoteTile(Notebook book, Section section, Note note)
    {
        var sp = new StackPanel();
        var top = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
        top.Children.Add(Ui.Dot(section.Color, 8).Also(d => { d.Margin = new Thickness(0, 0, 7, 0); DockPanel.SetDock(d, Dock.Left); }));
        top.Children.Add(Ui.Text($"{book.Name} › {section.Name}", muted: true, size: 11.5));
        sp.Children.Add(top);
        sp.Children.Add(Ui.Text(note.Title, size: 15, weight: FontWeights.SemiBold));
        var snip = Ui.Wrap(note.Snippet, muted: true, size: 12.5, lineHeight: 18);
        snip.Height = 54;
        snip.TextTrimming = TextTrimming.CharacterEllipsis;
        snip.Margin = new Thickness(0, 6, 0, 10);
        sp.Children.Add(snip);
        sp.Children.Add(Ui.Text(SampleData.Friendly(note.Edited), muted: true, size: 11.5));

        var button = new Button { Style = Ui.S("CardButton"), Content = new Border { Style = Ui.S("Card"), Padding = new Thickness(18, 16, 18, 16), Child = sp } };
        button.Click += (_, _) => MainWindow.Instance?.OpenNote(book, section, note);
        System.Windows.Automation.AutomationProperties.SetName(button, note.Title);
        return button;
    }

    private static UIElement ScratchPad()
    {
        var sp = new StackPanel();
        sp.Children.Add(Ui.CardHeader(Ic.Compose, "Scratch pad", "COMING SOON", Ui.ToolButton("Turning the scratch pad into a note", Ic.NoteAdd, "Save as note")));
        var pad = new Border { CornerRadius = new CornerRadius(10), Padding = new Thickness(16, 14, 16, 14), MinHeight = 170, Cursor = Cursors.IBeam };
        pad.SetResourceReference(Border.BackgroundProperty, "Brush.Marker");
        pad.Child = Ui.Wrap("Jot something down. Anything you type here is kept until you turn it into a note.", muted: true, size: 14, lineHeight: 22);
        pad.MouseLeftButtonUp += (_, _) => Toaster.Soon("The scratch pad");
        sp.Children.Add(pad);
        return Ui.Card(sp);
    }

    private static UIElement Tasks()
    {
        var sp = new StackPanel();
        sp.Children.Add(Ui.CardHeader(Ic.Task, "My tasks", "PREVIEW", new Button { Style = Ui.S("LinkButton"), Content = "View all" }
            .Also(b => b.Click += (_, _) => MainWindow.Instance?.GoTo("tasks"))));
        var todos = SampleData.AllNotes
            .SelectMany(x => x.Note.Body.Where(b => b.Kind is BlockKind.Todo).Select(b => (x.Note, b.Text)))
            .Take(5);
        foreach (var (note, text) in todos)
        {
            var row = new DockPanel { Margin = new Thickness(0, 4, 0, 4), Cursor = Cursors.Hand };
            row.Children.Add(Ui.Icon(Ic.Unchecked, 17, "Brush.TextMuted").Also(i => { i.Margin = new Thickness(0, 0, 10, 0); DockPanel.SetDock(i, Dock.Left); }));
            row.Children.Add(Ui.Text(note.Title, muted: true, size: 11.5).Also(t => { t.Margin = new Thickness(10, 0, 0, 0); DockPanel.SetDock(t, Dock.Right); }));
            row.Children.Add(Ui.Text(text));
            row.MouseLeftButtonUp += (_, _) => Toaster.Soon("Checking off tasks");
            sp.Children.Add(row);
        }
        return Ui.Card(sp);
    }

    private static UIElement Pinned()
    {
        var sp = new StackPanel();
        sp.Children.Add(Ui.CardHeader(Ic.Pin, "Pinned"));
        foreach (var (book, section, note) in SampleData.AllNotes.Where(x => x.Note.Pinned))
        {
            var b = new Button { Style = Ui.S("BaseButton"), HorizontalContentAlignment = HorizontalAlignment.Stretch, Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(-10, 0, -10, 0) };
            var row = new DockPanel();
            row.Children.Add(Ui.Icon(Ic.Note, 15, "Brush.Accent").Also(i => { i.Margin = new Thickness(0, 0, 10, 0); DockPanel.SetDock(i, Dock.Left); }));
            row.Children.Add(Ui.Text(SampleData.Friendly(note.Edited), muted: true, size: 11.5).Also(t => DockPanel.SetDock(t, Dock.Right)));
            row.Children.Add(Ui.Text(note.Title));
            b.Content = row;
            b.Click += (_, _) => MainWindow.Instance?.OpenNote(book, section, note);
            sp.Children.Add(b);
        }
        return Ui.Card(sp);
    }

    private static UIElement Notebooks()
    {
        var sp = new StackPanel();
        sp.Children.Add(Ui.CardHeader(Ic.Notebook, "Notebooks", null, Ui.ToolButton("New notebooks", Ic.BookAdd, "New notebook")));
        foreach (var book in SampleData.Notebooks)
        {
            var b = new Button { Style = Ui.S("BaseButton"), HorizontalContentAlignment = HorizontalAlignment.Stretch, Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(-10, 0, -10, 0) };
            var row = new DockPanel();
            row.Children.Add(Ui.Dot(book.Color, 10).Also(d => { d.Margin = new Thickness(0, 0, 12, 0); DockPanel.SetDock(d, Dock.Left); }));
            row.Children.Add(Ui.Text($"{book.Sections.Length} sections · {book.NoteCount} pages", muted: true, size: 11.5).Also(t => DockPanel.SetDock(t, Dock.Right)));
            row.Children.Add(Ui.Text(book.Name));
            b.Content = row;
            b.Click += (_, _) => MainWindow.Instance?.OpenNotebook(book);
            sp.Children.Add(b);
        }
        return Ui.Card(sp);
    }

    private static UIElement Tags()
    {
        var sp = new StackPanel();
        sp.Children.Add(Ui.CardHeader(Ic.Tag, "Tags", "COMING SOON"));
        var wrap = new WrapPanel();
        foreach (var tag in SampleData.AllTags)
        {
            var chip = Ui.Chip(tag, Ic.Tag);
            chip.Margin = new Thickness(0, 0, 6, 6);
            chip.Cursor = Cursors.Hand;
            chip.MouseLeftButtonUp += (_, _) => Toaster.Soon("Browsing by tag");
            wrap.Children.Add(chip);
        }
        sp.Children.Add(wrap);
        return Ui.Card(sp);
    }
}

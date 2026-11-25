using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using NoteHawk.Models;

namespace NoteHawk.Views;

/// <summary>Generic page for any section that isn't built yet: header, action bar, summary tiles, a skeleton table
/// and the list of planned features.</summary>
public sealed class ComingSoonView : UserControl
{
    private static readonly string[] DefaultColumns = ["Name", "Notebook", "Edited"];

    public ModuleInfo Module { get; }

    public ComingSoonView(ModuleInfo module)
    {
        Module = module;
        var root = new StackPanel();

        // Title
        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 22) };
        var tile = new Border { Width = 56, Height = 56, CornerRadius = new CornerRadius(14), Margin = new Thickness(0, 0, 16, 0) };
        tile.SetResourceReference(Border.BackgroundProperty, "Brush.AccentSoft");
        tile.Child = Ui.Icon(module.Glyph, 24, "Brush.Accent").Also(i => i.HorizontalAlignment = HorizontalAlignment.Center);
        header.Children.Add(tile);
        var titles = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
        titleRow.Children.Add(new TextBlock { Text = module.Title, Style = Ui.S("PageTitle") });
        titleRow.Children.Add(Ui.Pill().Also(p => p.Margin = new Thickness(12, 0, 0, 0)));
        titles.Children.Add(titleRow);
        titles.Children.Add(new TextBlock { Text = module.Tagline, Style = Ui.S("Muted"), FontSize = 14, Margin = new Thickness(0, 4, 0, 0) });
        header.Children.Add(titles);
        root.Children.Add(header);

        // Action bar
        var bar = new DockPanel { Margin = new Thickness(0, 0, 0, 18) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        buttons.Children.Add(Ui.SoonButton("Filters", Ic.Filter, "Filters").Also(b => b.Margin = new Thickness(0, 0, 8, 0)));
        buttons.Children.Add(Ui.SoonButton("Sorting", Ic.Sort, "Sort").Also(b => b.Margin = new Thickness(0, 0, 8, 0)));
        buttons.Children.Add(Ui.SoonButton(module.PrimaryAction, Ic.Add, module.PrimaryAction, "PrimaryButton"));
        DockPanel.SetDock(buttons, Dock.Right);
        bar.Children.Add(buttons);
        bar.Children.Add(Ui.SearchBox($"Search {module.Title.ToLowerInvariant()}…", 340).Also(s => s.HorizontalAlignment = HorizontalAlignment.Left));
        root.Children.Add(bar);

        // Summary tiles
        if (module.Kpis.Length > 0)
        {
            var tiles = new UniformGrid { Rows = 1, Margin = new Thickness(0, 0, -14, 18) };
            foreach (var k in module.Kpis)
            {
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock { Text = k, Style = Ui.S("Muted"), FontSize = 13 });
                sp.Children.Add(new TextBlock { Text = "—", Style = Ui.S("BigNumber") }.Also(t => t.SetResourceReference(TextBlock.ForegroundProperty, "Brush.TextMuted")));
                tiles.Children.Add(Ui.Card(sp).Also(c => c.Margin = new Thickness(0, 0, 14, 0)));
            }
            root.Children.Add(tiles);
        }

        // Skeleton table with "nothing here yet"
        var table = new Grid { MinHeight = 260 };
        table.Children.Add(SkeletonTable(module.Columns.Length > 0 ? module.Columns : DefaultColumns, module.Key));
        var empty = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        empty.Children.Add(Ui.Icon(module.Glyph, 26, "Brush.Accent").Also(i => i.HorizontalAlignment = HorizontalAlignment.Center));
        empty.Children.Add(Ui.Text("Nothing here yet", size: 15, weight: FontWeights.SemiBold).Also(t => { t.HorizontalAlignment = HorizontalAlignment.Center; t.Margin = new Thickness(0, 8, 0, 2); }));
        empty.Children.Add(Ui.Text($"{module.Title} is coming soon to NoteHawk.", muted: true).Also(t => t.HorizontalAlignment = HorizontalAlignment.Center));
        table.Children.Add(Ui.Card(empty, new Thickness(28, 20, 28, 20)).Also(c => { c.HorizontalAlignment = HorizontalAlignment.Center; c.VerticalAlignment = VerticalAlignment.Center; c.Margin = new Thickness(0, 40, 0, 0); }));
        root.Children.Add(new Border { Style = Ui.S("Card"), Padding = new Thickness(0), Margin = new Thickness(0, 0, 0, 18), ClipToBounds = true, Child = table });

        // About + planned features
        var lower = new Grid();
        lower.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        lower.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        lower.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
        var about = new StackPanel();
        about.Children.Add(new TextBlock { Text = "ABOUT THIS SECTION", Style = Ui.S("SectionLabel") });
        about.Children.Add(Ui.Wrap(module.Description, lineHeight: 21));
        lower.Children.Add(Ui.Card(about).At(0));
        var feats = new StackPanel();
        feats.Children.Add(new TextBlock { Text = "PLANNED FEATURES", Style = Ui.S("SectionLabel") });
        var grid = new UniformGrid { Columns = 2 };
        foreach (var f in module.Features)
        {
            var row = new DockPanel { Margin = new Thickness(0, 4, 14, 4) };
            row.Children.Add(Ui.Icon(Ic.Check, 11, "Brush.Accent").Also(i => { i.Margin = new Thickness(0, 3, 9, 0); i.VerticalAlignment = VerticalAlignment.Top; DockPanel.SetDock(i, Dock.Left); }));
            row.Children.Add(Ui.Wrap(f));
            grid.Children.Add(row);
        }
        feats.Children.Add(grid);
        lower.Children.Add(Ui.Card(feats).At(2));
        root.Children.Add(lower);

        Content = root;
    }

    private static Grid SkeletonTable(string[] columns, string seed)
    {
        var random = new Random(seed.Length * 31 + columns.Length);
        var grid = new Grid { Opacity = 0.9 };
        foreach (var _ in columns) grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var header = new Border();
        header.SetResourceReference(Border.BackgroundProperty, "Brush.SurfaceAlt");
        Grid.SetColumnSpan(header, columns.Length);
        grid.Children.Add(header);
        for (int c = 0; c < columns.Length; c++)
        {
            var text = Ui.Text(columns[c].ToUpperInvariant(), muted: true, size: 11, weight: FontWeights.SemiBold);
            text.Margin = new Thickness(20, 12, 8, 12);
            grid.Children.Add(text.At(c));
        }
        for (int r = 1; r <= 6; r++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var line = new Border { BorderThickness = new Thickness(0, 1, 0, 0) };
            line.SetResourceReference(Border.BorderBrushProperty, "Brush.Border");
            Grid.SetColumnSpan(line, columns.Length);
            grid.Children.Add(line.At(0, r));
            for (int c = 0; c < columns.Length; c++)
                grid.Children.Add(Ui.Skeleton(random.Next(36, 110), 9, new Thickness(20, 16, 8, 16)).At(c, r));
        }
        return grid;
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NoteHawk.Models;
using NoteHawk.Services;

namespace NoteHawk.Views;

/// <summary>Small builders for UI created from code. All colors follow the theme through resource references.</summary>
public static class Ui
{
    public static Style S(string key) => (Style)Application.Current.FindResource(key);

    public static TextBlock Text(string text, bool muted = false, double size = 13.5, FontWeight? weight = null, string? brushKey = null)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = size,
            FontWeight = weight ?? FontWeights.Normal,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
        };
        tb.SetResourceReference(TextBlock.ForegroundProperty, brushKey ?? (muted ? "Brush.TextMuted" : "Brush.Text"));
        return tb;
    }

    public static TextBlock Wrap(string text, bool muted = false, double size = 13.5, double lineHeight = 0)
    {
        var tb = Text(text, muted, size);
        tb.TextWrapping = TextWrapping.Wrap;
        tb.TextTrimming = TextTrimming.None;
        if (lineHeight > 0) tb.LineHeight = lineHeight;
        return tb;
    }

    public static TextBlock Icon(string glyph, double size = 16, string? brushKey = null, bool filled = false)
    {
        var tb = new TextBlock { Text = glyph, FontSize = size, Style = S("Icon") };
        if (filled) tb.SetResourceReference(TextBlock.FontFamilyProperty, "IconFontFilled");
        if (brushKey != null) tb.SetResourceReference(TextBlock.ForegroundProperty, brushKey);
        return tb;
    }

    public static Brush Hex(string hex) => (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;

    public static Border Dot(string hex, double size = 10) => new()
    {
        Width = size, Height = size, CornerRadius = new CornerRadius(size / 2),
        Background = Hex(hex), VerticalAlignment = VerticalAlignment.Center,
    };

    /// <summary>The little "COMING SOON" / "PREVIEW" pill.</summary>
    public static Border Pill(string text = "COMING SOON") => new Border
    {
        Style = S("SoonPill"),
        Child = new TextBlock { Text = text, Style = S("PillText") },
    };

    /// <summary>A soft rounded label, used for tags.</summary>
    public static Border Chip(string text, string? glyph = null)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        if (glyph != null) row.Children.Add(Icon(glyph, 11, "Brush.TextMuted").Also(i => i.Margin = new Thickness(0, 0, 5, 0)));
        row.Children.Add(Text(text, size: 12));
        var chip = new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(9, 3, 10, 3),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Child = row,
        };
        chip.SetResourceReference(Border.BackgroundProperty, "Brush.SurfaceAlt");
        return chip;
    }

    /// <summary>A button that only says "&lt;feature&gt; is coming soon" when clicked.</summary>
    public static Button SoonButton(string feature, string? glyph, string? label = null, string style = "GhostButton", string? tooltip = null)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        if (glyph != null) row.Children.Add(Icon(glyph, 15).Also(i => { if (label != null) i.Margin = new Thickness(0, 0, 7, 0); }));
        if (label != null) row.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        var b = new Button { Style = S(style), Content = row, Tag = feature, ToolTip = tooltip ?? feature };
        System.Windows.Automation.AutomationProperties.SetName(b, label ?? feature);
        b.Click += (s, _) => Toaster.Soon(s);
        return b;
    }

    /// <summary>A small square toolbar button (formatting bar, list header).</summary>
    public static Button ToolButton(string feature, string glyph, string? tooltip = null)
    {
        var b = SoonButton(feature, glyph, null, "BaseButton", tooltip);
        b.Padding = new Thickness(7);
        b.MinHeight = 32;
        b.MinWidth = 32;
        b.SetResourceReference(Control.ForegroundProperty, "Brush.Text");
        return b;
    }

    public static Border Separator(bool vertical = true)
    {
        var s = vertical
            ? new Border { Width = 1, Margin = new Thickness(6, 6, 6, 6) }
            : new Border { Height = 1, Margin = new Thickness(0, 10, 0, 10) };
        s.SetResourceReference(Border.BackgroundProperty, "Brush.Border");
        return s;
    }

    public static Border Card(UIElement child, Thickness? padding = null)
    {
        var c = new Border { Style = S("Card"), Child = child };
        if (padding is Thickness p) c.Padding = p;
        return c;
    }

    /// <summary>Card heading with an optional pill and something on the right.</summary>
    public static DockPanel CardHeader(string glyph, string title, string? pill = null, UIElement? right = null)
    {
        var dock = new DockPanel { Margin = new Thickness(0, 0, 0, 14) };
        if (right != null) { DockPanel.SetDock(right, Dock.Right); dock.Children.Add(right); }
        var left = new StackPanel { Orientation = Orientation.Horizontal };
        left.Children.Add(Icon(glyph, 16, "Brush.Accent").Also(i => i.Margin = new Thickness(0, 0, 9, 0)));
        left.Children.Add(new TextBlock { Text = title, Style = S("CardTitle"), VerticalAlignment = VerticalAlignment.Center });
        if (pill != null) left.Children.Add(Pill(pill).Also(p => p.Margin = new Thickness(10, 0, 0, 0)));
        dock.Children.Add(left);
        return dock;
    }

    /// <summary>A grey placeholder bar, the "skeleton" look for content that isn't built yet.</summary>
    public static Border Skeleton(double width, double height = 9, Thickness? margin = null)
    {
        var b = new Border
        {
            Width = width, Height = height, CornerRadius = new CornerRadius(height / 2),
            HorizontalAlignment = HorizontalAlignment.Left, Margin = margin ?? new Thickness(0, 4, 0, 4),
        };
        b.SetResourceReference(Border.BackgroundProperty, "Brush.Skeleton");
        return b;
    }

    public static T At<T>(this T element, int column, int row = 0) where T : UIElement
    {
        Grid.SetColumn(element, column);
        Grid.SetRow(element, row);
        return element;
    }

    public static T Also<T>(this T element, Action<T> tweak)
    {
        tweak(element);
        return element;
    }

    /// <summary>A fake (read-only) search box that says search is coming soon when clicked.</summary>
    public static Grid SearchBox(string placeholder, double width = double.NaN)
    {
        var grid = new Grid { Width = width, Cursor = System.Windows.Input.Cursors.Hand };
        var box = new Border { CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1), Padding = new Thickness(34, 8, 10, 8) };
        box.SetResourceReference(Border.BackgroundProperty, "Brush.Surface");
        box.SetResourceReference(Border.BorderBrushProperty, "Brush.Border");
        box.Child = Text(placeholder, muted: true);
        grid.Children.Add(box);
        grid.Children.Add(Icon(Ic.Search, 15, "Brush.TextMuted").Also(i => { i.HorizontalAlignment = HorizontalAlignment.Left; i.Margin = new Thickness(12, 0, 0, 0); }));
        grid.MouseLeftButtonDown += (_, e) => { e.Handled = true; Toaster.Soon("Search"); };
        System.Windows.Automation.AutomationProperties.SetName(grid, "Search");
        return grid;
    }
}

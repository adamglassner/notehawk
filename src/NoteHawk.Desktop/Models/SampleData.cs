namespace NoteHawk.Models;

/// <summary>What kind of block a line in a sample note is.</summary>
public enum BlockKind { Paragraph, Heading, Bullet, Todo, Done, Quote, Image, Table }

public sealed record Block(BlockKind Kind, string Text = "");

public sealed record Note(string Title, string Snippet, DateTime Edited, string[] Tags, Block[] Body, bool Pinned = false, bool HasImage = false);

public sealed record Section(string Name, string Color, Note[] Notes);

public sealed record Notebook(string Name, string Color, Section[] Sections)
{
    public int NoteCount => Sections.Sum(s => s.Notes.Length);
}

/// <summary>
/// Sample notebooks shown while NoteHawk is a shell, so the layout can be judged with realistic content.
/// Nothing is saved; this all goes away once notes are real.
/// </summary>
public static class SampleData
{
    private static DateTime Ago(double hours) => DateTime.Now.AddHours(-hours);

    public static readonly Notebook[] Notebooks =
    [
        new("Personal", "#7C5CFF",
        [
            new("Journal", "#7C5CFF",
            [
                new("Thanksgiving prep", "Turkey goes in at 11 AM. Pies Wednesday night, rolls Thursday morning…", Ago(2), ["family", "holidays"],
                [
                    new(BlockKind.Paragraph, "Everyone's coming at 2 PM. Turkey goes in at 11 AM, so the oven is busy all morning."),
                    new(BlockKind.Heading, "Wednesday night"),
                    new(BlockKind.Done, "Pumpkin pie"),
                    new(BlockKind.Done, "Apple crumble"),
                    new(BlockKind.Todo, "Brine the turkey"),
                    new(BlockKind.Heading, "Thursday"),
                    new(BlockKind.Todo, "Rolls in at 1:15 PM"),
                    new(BlockKind.Todo, "Set the table"),
                    new(BlockKind.Quote, "Ask Mom for the stuffing recipe again. Write it down this time!"),
                ], Pinned: true),
                new("Weekend ideas", "Hike the river trail if it's dry. Otherwise the museum…", Ago(30), ["ideas"],
                [
                    new(BlockKind.Paragraph, "If the weather holds, hike the river trail. Otherwise the science museum."),
                    new(BlockKind.Bullet, "Pack lunch"),
                    new(BlockKind.Bullet, "Charge the camera"),
                ]),
            ]),
            new("Recipes", "#F2A93B",
            [
                new("Grandma's stuffing", "Cornbread, celery, onion, sage and lots of butter…", Ago(70), ["recipes", "family"],
                [
                    new(BlockKind.Heading, "Ingredients"),
                    new(BlockKind.Bullet, "1 pan cornbread, crumbled"),
                    new(BlockKind.Bullet, "2 stalks celery, 1 onion"),
                    new(BlockKind.Bullet, "Fresh sage, butter, broth"),
                    new(BlockKind.Image, "Photo of the recipe card"),
                ], HasImage: true),
            ]),
        ]),
        new("Bytehawk Studio", "#3FA7D6",
        [
            new("Product ideas", "#3FA7D6",
            [
                new("NoteHawk roadmap", "Shell first, then the editor, then sync through NoteHawk Server…", Ago(0.5), ["notehawk", "roadmap"],
                [
                    new(BlockKind.Paragraph, "NoteHawk is the notes app in the Bytehawk suite: Evernote-style lists and search, OneNote-style notebooks, sections and pages."),
                    new(BlockKind.Heading, "Milestones"),
                    new(BlockKind.Done, "Shell with every feature marked coming soon"),
                    new(BlockKind.Todo, "Rich text editor (formatting, lists, checklists, tables)"),
                    new(BlockKind.Todo, "Notebooks, sections and pages saved on this PC"),
                    new(BlockKind.Todo, "Search, including text inside images and PDFs"),
                    new(BlockKind.Todo, "NoteHawk Server for sync and sharing"),
                    new(BlockKind.Table, "Feature comparison"),
                    new(BlockKind.Quote, "Keep it fast. Opening a note should feel instant."),
                ], Pinned: true),
                new("Launcher wishlist", "Search box, app categories, update all button…", Ago(26), ["launcher"],
                [
                    new(BlockKind.Bullet, "Search across apps"),
                    new(BlockKind.Bullet, "Update all button"),
                ]),
            ]),
            new("Meeting notes", "#2EB67D",
            [
                new("Weekly planning", "Ship NoteHawk shell, check Waylo call quality, plan WriteHawk…", Ago(20), ["meetings"],
                [
                    new(BlockKind.Heading, "Agenda"),
                    new(BlockKind.Bullet, "NoteHawk shell"),
                    new(BlockKind.Bullet, "Waylo call quality"),
                    new(BlockKind.Bullet, "WriteHawk planning"),
                    new(BlockKind.Heading, "Action items"),
                    new(BlockKind.Todo, "Draft WriteHawk feature list"),
                ]),
            ]),
        ]),
        new("Home", "#2EB67D",
        [
            new("House", "#2EB67D",
            [
                new("Network notes", "UniFi switch ports, Wi-Fi names, the old router for retro handhelds…", Ago(120), ["home", "network"],
                [
                    new(BlockKind.Paragraph, "Where everything plugs in, and which Wi-Fi network each device uses."),
                    new(BlockKind.Table, "Switch ports"),
                ]),
                new("Paint colors", "Living room: warm white. Office: deep blue accent wall…", Ago(400), ["home"],
                [
                    new(BlockKind.Bullet, "Living room: warm white"),
                    new(BlockKind.Bullet, "Office: deep blue accent wall"),
                    new(BlockKind.Image, "Swatches"),
                ], HasImage: true),
            ]),
        ]),
    ];

    public static IEnumerable<(Notebook Book, Section Section, Note Note)> AllNotes =>
        from b in Notebooks from s in b.Sections from n in s.Notes select (b, s, n);

    public static string[] AllTags => AllNotes.SelectMany(x => x.Note.Tags).Distinct().OrderBy(t => t).ToArray();

    public static string Friendly(DateTime when)
    {
        var span = DateTime.Now - when;
        if (span.TotalMinutes < 60) return $"{Math.Max(1, (int)span.TotalMinutes)} min ago";
        if (when.Date == DateTime.Today) return when.ToString("h:mm tt");
        if (when.Date == DateTime.Today.AddDays(-1)) return "Yesterday";
        if (span.TotalDays < 7) return when.ToString("dddd");
        return when.ToString("MMM d");
    }
}

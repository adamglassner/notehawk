namespace NoteHawk.Models;

/// <summary>
/// Every section of NoteHawk. Home and Notes have their own preview pages; everything else uses the generic
/// ComingSoonView built from this data. Add a module here and it appears in the sidebar automatically.
/// </summary>
public static class ModuleCatalog
{
    public static readonly ModuleInfo Home = new()
    {
        Key = "home", Title = "Home", Glyph = Ic.Home, IsComingSoon = false,
        Tagline = "Recent notes, your scratch pad, tasks and pinned notes in one place.",
    };

    public static readonly ModuleInfo Notes = new()
    {
        Key = "notes", Title = "Notes", Glyph = Ic.Note, IsComingSoon = false,
        Tagline = "Notebooks, sections and pages.",
    };

    public static readonly IReadOnlyList<ModuleInfo> Sidebar =
    [
        Home,
        Notes,
        new()
        {
            Key = "notebooks", Title = "Notebooks", Glyph = Ic.Notebook,
            Tagline = "Organize notes into notebooks, sections and section groups.",
            Description = "Notebooks hold sections, and sections hold pages, like a binder with tabbed dividers. Group notebooks into stacks, give each one a color, and move pages between sections by dragging them.",
            PrimaryAction = "New notebook",
            Kpis = ["Notebooks", "Sections", "Pages", "Shared notebooks"],
            Columns = ["Notebook", "Sections", "Pages", "Last edited", "Shared with"],
            Features =
            [
                "Notebooks, sections and pages",
                "Section groups and notebook stacks",
                "Notebook and section colors",
                "Drag pages between sections",
                "Subpages (indent a page under another)",
                "Default notebook for quick notes",
                "Export a notebook to PDF or Markdown",
                "Password-protect a section",
            ],
        },
        new()
        {
            Key = "tasks", Title = "Tasks", Glyph = Ic.Task,
            Tagline = "To-dos inside your notes, with due dates and reminders.",
            Description = "Turn any line in a note into a task. See every open task from every notebook here, sorted by due date, with reminders so nothing slips.",
            PrimaryAction = "New task",
            Kpis = ["Due today", "Overdue", "This week", "Completed"],
            Columns = ["Task", "Note", "Due", "Priority", "Status"],
            Features =
            [
                "Tasks inside notes",
                "Due dates and reminders",
                "Flags and priorities",
                "Recurring tasks",
                "Filter by notebook or tag",
                "Desktop notifications",
            ],
        },
        new()
        {
            Key = "calendar", Title = "Calendar", Glyph = Ic.Calendar,
            Tagline = "Notes and tasks on a calendar, plus meeting notes.",
            Description = "See tasks and reminders by day, week or month. Start meeting notes from an event with its title, time and attendees already filled in.",
            PrimaryAction = "New meeting note",
            Kpis = ["Today", "This week", "Reminders", "Meeting notes"],
            Columns = ["Date", "Title", "Type", "Notebook"],
            Features =
            [
                "Day, week and month views",
                "Tasks and reminders by date",
                "Meeting notes from events",
                "Daily journal page",
            ],
        },
        new()
        {
            Key = "tags", Title = "Tags", Glyph = Ic.Tag,
            Tagline = "Find notes across notebooks with tags.",
            Description = "Tag notes and pages, then browse or filter by tag across every notebook. Nested tags keep large collections tidy.",
            PrimaryAction = "New tag",
            Kpis = ["Tags", "Tagged notes", "Untagged notes", "Most used"],
            Columns = ["Tag", "Notes", "Parent tag", "Last used"],
            Features =
            [
                "Tags on any note or page",
                "Nested tags",
                "Tag colors",
                "Rename and merge tags",
                "Filter by several tags at once",
                "OneNote-style tags (To do, Important, Question)",
            ],
        },
        new()
        {
            Key = "shared", Title = "Shared", Glyph = Ic.Shared,
            Tagline = "Notes and notebooks shared with you, and by you.",
            Description = "Share a note or a whole notebook with family or coworkers through a NoteHawk Server you host. Choose who can view and who can edit.",
            PrimaryAction = "Share a notebook",
            Kpis = ["Shared with me", "Shared by me", "People", "Pending invites"],
            Columns = ["Name", "Owner", "Permission", "Last edited"],
            Features =
            [
                "Share notes and notebooks",
                "View or edit permissions",
                "See who changed what",
                "Comments on pages",
                "Public read-only links",
                "Works through your own NoteHawk Server",
            ],
        },
        new()
        {
            Key = "templates", Title = "Templates", Glyph = Ic.Template,
            Tagline = "Start pages from ready-made layouts.",
            Description = "Pick a template for meeting notes, project plans, journals, recipes and more, or save any page as your own template.",
            PrimaryAction = "New template",
            Kpis = ["Templates", "My templates", "Used this month"],
            Columns = ["Template", "Category", "Created by", "Last used"],
            Features =
            [
                "Built-in templates",
                "Save a page as a template",
                "Default template per section",
                "Template gallery with previews",
            ],
        },
        new()
        {
            Key = "trash", Title = "Trash", Glyph = Ic.Delete,
            Tagline = "Deleted notes stay here for 30 days.",
            Description = "Restore a deleted note, page or section, or empty the trash for good. Items are removed permanently after 30 days.",
            PrimaryAction = "Empty trash",
            Kpis = ["Items", "Oldest", "Space used"],
            Columns = ["Name", "Notebook", "Deleted", "Deletes in"],
            Features =
            [
                "Restore notes, pages and sections",
                "Automatic cleanup after 30 days",
                "Empty trash",
                "Page version history",
            ],
        },
    ];

    public static readonly ModuleInfo Settings = new()
    {
        Key = "settings", Title = "Settings", Glyph = Ic.Settings,
        Tagline = "Appearance, sync, security and import.",
        Description = "Choose light or dark mode, connect to your own NoteHawk Server, lock NoteHawk with a PIN or password, and bring in notes from Evernote and OneNote.",
        PrimaryAction = "Save",
        Columns = ["Setting", "Value"],
        Features =
        [
            "Light, dark or match Windows",
            "NoteHawk Server sync (self-hosted)",
            "PIN or password lock with encrypted notes",
            "Import from Evernote (.enex)",
            "Import from OneNote",
            "Backups and export",
            "Default font and page style",
            "Keyboard shortcuts",
        ],
    };

    public static IEnumerable<ModuleInfo> All => Sidebar.Append(Settings);

    public static ModuleInfo? Find(string key) => All.FirstOrDefault(m => m.Key == key);
}

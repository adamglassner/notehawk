using System.IO;
using System.Text.Json;

namespace NoteHawk.Services;

public static class AppTheme
{
    public const string System = "System";
    public const string Light = "Light";
    public const string Dark = "Dark";
}

/// <summary>Per-user preferences saved to %AppData%\NoteHawk\settings.json.</summary>
public sealed class AppSettings
{
    public string Theme { get; set; } = AppTheme.System;

    // Window position and size, restored on the next start.
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    public bool WindowMaximized { get; set; }

    public static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NoteHawk");
    private static string FilePath => Path.Combine(Root, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch
        {
            // Corrupt or unreadable settings fall back to defaults.
        }
        return new AppSettings();
    }

    public void Save()
    {
        if (SelfTest.IsRunning || Screenshots.IsRunning) return; // automated runs never touch real settings
        try
        {
            Directory.CreateDirectory(Root);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Settings are a convenience; never crash over them.
        }
    }
}

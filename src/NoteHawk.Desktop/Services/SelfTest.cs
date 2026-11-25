using System.IO;
using System.Windows;
using System.Windows.Threading;
using NoteHawk.Models;
using NoteHawk.Views;

namespace NoteHawk.Services;

/// <summary>
/// <c>NoteHawk.exe --self-test &lt;file&gt;</c>: opens every page in light and dark mode, every sample notebook, section
/// and note, writes PASS/FAIL lines to the file and exits with the number of failures. Used by CI.
/// </summary>
public static class SelfTest
{
    public static string? OutputFile { get; set; }
    public static bool IsRunning => OutputFile != null;

    private static readonly List<string> Lines = [];
    private static int _failures;

    public static void RecordUnhandled(Exception ex) => Fail("unhandled", ex);

    private static void Pass(string what) => Lines.Add($"PASS  {what}");

    private static void Fail(string what, Exception ex)
    {
        _failures++;
        Lines.Add($"FAIL  {what}: {ex.GetType().Name}: {ex.Message}");
    }

    private static async Task Check(string what, Func<Task> action)
    {
        try { await action(); Pass(what); }
        catch (Exception ex) { Fail(what, ex); }
    }

    public static Task Idle() => Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task;

    public static async Task<int> RunAsync(MainWindow window)
    {
        Lines.Add($"NoteHawk {App.VersionText} self-test, {DateTime.Now:yyyy-MM-dd h:mm tt}");
        foreach (var mode in new[] { AppTheme.Light, AppTheme.Dark })
        {
            App.ApplyTheme(mode);
            foreach (var module in ModuleCatalog.All)
            {
                await Check($"{mode}: {module.Title}", async () =>
                {
                    window.GoTo(module.Key, force: true);
                    await Idle();
                    if (window.CurrentPage == null) throw new InvalidOperationException("no page shown");
                });
            }
            foreach (var book in SampleData.Notebooks)
            {
                await Check($"{mode}: notebook {book.Name}", async () =>
                {
                    window.OpenNotebook(book);
                    await Idle();
                    var view = window.CurrentPage as NotesView ?? throw new InvalidOperationException("Notes screen not shown");
                    if (view.Book != book) throw new InvalidOperationException("wrong notebook");
                    foreach (var section in book.Sections)
                    {
                        view.ShowSection(section);
                        foreach (var note in section.Notes) { view.ShowNote(note); await Idle(); }
                    }
                });
            }
            await Check($"{mode}: open a note from Home", async () =>
            {
                var (b, s, n) = SampleData.AllNotes.First(x => x.Note.Pinned);
                window.OpenNote(b, s, n);
                await Idle();
                if ((window.CurrentPage as NotesView)?.Current != n) throw new InvalidOperationException("note not opened");
            });
            await Check($"{mode}: toast", async () => { Toaster.Soon("Self-test"); await Idle(); });
        }
        Lines.Add(_failures == 0 ? "ALL PASSED" : $"{_failures} FAILED");
        try { File.WriteAllLines(OutputFile!, Lines); } catch { /* nothing else we can do */ }
        return _failures;
    }
}

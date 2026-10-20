using System.Diagnostics;
using System.Text;
namespace CasterDesk.Core;
public sealed class Countdown
{
    private readonly Func<double> now;
    private double remaining, started;
    public bool Running { get; private set; }
    public bool Finished { get; private set; }
    public Countdown(Func<double>? clock = null) => now = clock ?? (() => (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency);
    public double Remaining => Math.Max(0, remaining - (Running ? now() - started : 0));
    public void Reset(double seconds) { remaining = seconds; Running = false; Finished = false; }
    public void Start() { if (!Running && Remaining > 0) { started = now(); Running = true; Finished = false; } }
    public void Pause() { remaining = Remaining; Running = false; }
    public void Add(double seconds) { remaining = Remaining + seconds; started = now(); Finished = false; }
    public void Tick() { if (Running && Remaining <= 0) { remaining = 0; Running = false; Finished = true; } }
}

public sealed class Settings
{
    public string Language { get; set; } = "pt-BR";
    public bool DarkMode { get; set; } = true;
    public string? VisualTheme { get; set; }
    public bool SiegeTheme { get; set; } = true;
    public bool SpecMode { get; set; }
    public bool QuickRounds { get; set; } = true;
    public decimal Initial { get; set; } = 10;
    public decimal[] Presets { get; set; } = [2, 3, 4];
    public string Format { get; set; } = "mm:ss";
    public string Message { get; set; } = L.T("TEMPO ENCERRADO");
    public string Output { get; set; } = Path.Combine(AppPaths.TextDirectory, "timer.txt");
}

public sealed class Talent
{
    public string Name { get; set; } = "";
    public string Handle { get; set; } = "";
}

public sealed class CastingSettings
{
    public string Folder { get; set; } = AppPaths.TextDirectory;
    public Talent[] People { get; set; } = [new(), new(), new(), new()];
}

public static class CastingFiles
{
    public static readonly string[] Keys = ["narrador", "comentarista", "convidado1", "convidado2"];
    public static string Handle(string value) => string.IsNullOrWhiteSpace(value.Trim().TrimStart('@')) ? "" : "@" + value.Trim().TrimStart('@');
    // External OBS folders are supported. Prepare every file before replacing any,
    // then roll back already-published files if one destination is unavailable.
    public static void Write(string folder, Talent[] people, string? settingsPath = null, string? settingsJson = null)
    {
        if (people.Length != Keys.Length) throw new InvalidDataException("Four participants are required.");
        if ((settingsPath == null) != (settingsJson == null)) throw new ArgumentException("Settings path and contents must be supplied together.");
        var files = new List<(string Path, string Text)>();
        for (int i = 0; i < Keys.Length; i++) {
            files.Add((Path.GetFullPath(Path.Combine(folder, Keys[i] + ".txt")), people[i].Name.Trim()));
            files.Add((Path.GetFullPath(Path.Combine(folder, "@" + Keys[i] + ".txt")), Handle(people[i].Handle)));
        }
        if (settingsPath != null) files.Add((Path.GetFullPath(settingsPath), settingsJson!));
        if (files.Select(f => f.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() != files.Count) throw new InvalidDataException("Duplicate TXT destination.");
        lock (FileBatch.Gate) {
            var prepared = new List<(string Target, string New, string? Old)>();
            var applied = new List<(string Target, string New, string? Old)>();
            bool retain = false;
            try {
                foreach (var file in files) {
                    Directory.CreateDirectory(Path.GetDirectoryName(file.Path)!);
                    string temporary = file.Path + ".casterdesk-" + Guid.NewGuid().ToString("N");
                    var entry = (Target: file.Path, New: temporary + ".new", Old: File.Exists(file.Path) ? temporary + ".old" : null);
                    prepared.Add(entry);
                    File.WriteAllText(entry.New, file.Text, new UTF8Encoding(false));
                    if (entry.Old != null) File.Copy(entry.Target, entry.Old);
                }
                foreach (var entry in prepared) {
                    File.Move(entry.New, entry.Target, true);
                    applied.Add(entry);
                }
            } catch (Exception writeError) {
                var failures = new List<Exception>();
                foreach (var entry in applied.AsEnumerable().Reverse()) {
                    try {
                        if (entry.Old == null) File.Delete(entry.Target);
                        else File.Move(entry.Old, entry.Target, true);
                    } catch (Exception rollbackError) { failures.Add(rollbackError); }
                }
                if (failures.Count > 0) {
                    retain = true;
                    throw new AggregateException("TXT update failed; recovery copies were preserved beside the destinations.", new[] { writeError }.Concat(failures));
                }
                throw;
            } finally {
                if (!retain) foreach (var entry in prepared) foreach (var path in new[] { entry.New, entry.Old })
                    if (path != null) try { File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }
    }
}

using System.Diagnostics;
using System.Text;
namespace CronoLive;
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
    public static void Write(string folder, Talent[] people)
    {
        Directory.CreateDirectory(folder);
        for (int i = 0; i < Keys.Length; i++)
        {
            File.WriteAllText(Path.Combine(folder, Keys[i] + ".txt"), people[i].Name.Trim(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(folder, "@" + Keys[i] + ".txt"), Handle(people[i].Handle), new UTF8Encoding(false));
        }
    }
}

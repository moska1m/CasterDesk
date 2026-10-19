using System.Text.Json;

namespace CasterDesk.Desktop;

public sealed class MatchTableSettings
{
    public bool Enabled { get; set; }
    public string OutputPath { get; set; } = MatchTable.DefaultPath;
}

// Snapshot on the UI thread, serialize disk writes in order. Switching matches
// cannot discard a queued final snapshot of the previous game.
public sealed class MatchTableSync
{
    readonly MatchSession session;
    readonly MatchTableSettings settings;
    Task pending = Task.CompletedTask;
    string previous = "";
    bool stopped;
    public bool Enabled => settings.Enabled;
    public string OutputPath => settings.OutputPath;
    public string Status { get; private set; } = "Atualização automática desligada.";
    public event Action? Changed;
    public MatchTableSync(MatchSession session)
    {
        this.session = session;
        settings = ConfigurationStore.Read<MatchTableSettings>("match-table-settings.json");
        if (string.IsNullOrWhiteSpace(settings.OutputPath)) settings.OutputPath = MatchTable.DefaultPath;
        session.Changed += Capture;
        Capture();
    }
    public async Task ConfigureAsync(bool enabled, string path)
    {
        path = Path.GetFullPath(path.Trim());
        if (!string.Equals(Path.GetExtension(path), ".xlsx", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Escolha um arquivo .xlsx.");
        if (enabled) _ = MatchTable.Inputs(session.Current, session.Catalog);
        await pending;
        var updated = new MatchTableSettings { Enabled = enabled, OutputPath = path };
        MatchFiles.AtomicText(AppPaths.ConfigFile("match-table-settings.json"), JsonSerializer.Serialize(updated));
        settings.Enabled = enabled; settings.OutputPath = path; previous = "";
        if (enabled) Capture(); else { Status = "Atualização automática desligada. As cópias existentes foram mantidas."; Changed?.Invoke(); }
    }
    void Capture() => Queue(false);
    public Task UpdateAsync() { Queue(true); return pending; }
    void Queue(bool force)
    {
        if (stopped || (!settings.Enabled && !force)) return;
        string json = JsonSerializer.Serialize(session.Current, MatchStore.Options);
        if (!force && json == previous) return;
        previous = json;
        var snapshot = JsonSerializer.Deserialize<SiegeMatch>(json, MatchStore.Options)!;
        string path = settings.OutputPath;
        Status = "Atualizando tabela…"; Changed?.Invoke();
        pending = pending.ContinueWith(_ => {
            string message;
            try { MatchTable.Save(snapshot, session.Catalog, path); message = "Tabela atualizada · " + DateTime.Now.ToString("HH:mm:ss"); }
            catch (Exception ex) { message = ex.Message; AppLog.Write("MATCH TABLE: " + ex); }
            Dispatcher.UIThread.Post(() => { Status = message; Changed?.Invoke(); });
        }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
    }
    public Task FlushAsync() => pending;
    public Task StopAsync() { stopped = true; session.Changed -= Capture; return pending; }
}

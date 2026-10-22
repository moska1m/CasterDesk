using System.Net.Http;
using System.Text.Json;

namespace CasterDesk.Desktop;

public sealed class GoogleTableSettings
{
    public bool Enabled { get; set; }
    public string Link { get; set; } = "";
}

public sealed class GoogleTableSync
{
    readonly MatchSession session;
    readonly Func<string> archiveSource;
    readonly GoogleTableSettings settings;
    readonly GoogleSheetsTable sheets;
    readonly CancellationTokenSource lifetime = new();
    readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(3) };
    Task archives = Task.CompletedTask, upload = Task.CompletedTask;
    SiegeMatch? latest;
    string previous = "";
    long revision, sent;
    DateTimeOffset retryAfter;
    int failures;
    bool stopped;
    public GoogleTableAuth Auth { get; }
    public bool Enabled => settings.Enabled;
    public string Link => settings.Link;
    public string Status { get; private set; } = "Google Sheets desligado.";
    public event Action? Changed;
    public GoogleTableSync(MatchSession session, Func<string> archiveSource)
    {
        this.session = session; this.archiveSource = archiveSource;
        settings = ConfigurationStore.Read<GoogleTableSettings>("google-table-settings.json");
        var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(20) };
        Auth = new GoogleTableAuth(http); sheets = new GoogleSheetsTable(http, Auth.AccessTokenAsync);
        session.Changed += Capture;
        timer.Tick += (_, _) => TryUpload(); timer.Start(); Capture();
    }
    public async Task ConfigureAsync(bool enabled, string link)
    {
        SheetsTarget? target = null;
        if (enabled) {
            _ = MatchTable.Inputs(session.Current, session.Catalog);
            if (!Auth.Connected) throw new InvalidOperationException("Conecte a conta Google antes de ativar.");
            target = await sheets.VerifyAsync(link, lifetime.Token);
        }
        // Drain an in-flight request before changing its destination.
        await upload;
        // Pin the selected numeric tab ID so future renames cannot switch tabs.
        link = target == null ? link.Trim() : GoogleSheetsTable.TargetLink(target);
        MatchFiles.AtomicText(AppPaths.ConfigFile("google-table-settings.json"), JsonSerializer.Serialize(new GoogleTableSettings { Enabled = enabled, Link = link.Trim() }));
        settings.Enabled = enabled; settings.Link = link.Trim(); previous = ""; retryAfter = default; failures = 0;
        if (enabled) { Capture(); TryUpload(); }
        else { Status = "Google Sheets desligado. As cópias locais foram mantidas."; Changed?.Invoke(); }
    }
    public async Task<IReadOnlyList<SheetsTarget>> ListSheetsAsync(string link, CancellationToken cancellation = default)
    {
        if (!Auth.Connected) throw new InvalidOperationException("Conecte a conta Google antes de carregar as abas.");
        using var pending = CancellationTokenSource.CreateLinkedTokenSource(cancellation, lifetime.Token);
        return await sheets.ListAsync(link, pending.Token);
    }
    void Capture()
    {
        if (stopped || !Enabled) return;
        string json = JsonSerializer.Serialize(session.Current, MatchStore.Options);
        if (json == previous) return;
        previous = json; latest = JsonSerializer.Deserialize<SiegeMatch>(json, MatchStore.Options)!; revision++;
        var snapshot = latest; string source = archiveSource();
        // Local archives are ordered independently of network latency. A lost
        // connection cannot discard the final state of a previous match.
        archives = archives.ContinueWith(_ => {
            try { MatchTable.Save(snapshot, session.Catalog, source, false); }
            catch (Exception ex) { Dispatcher.UIThread.Post(() => { Status = "Cópia local: " + ex.Message; Changed?.Invoke(); }); }
        }, TaskScheduler.Default);
    }
    void TryUpload(bool force = false)
    {
        if (stopped || !Enabled || latest == null || !upload.IsCompleted || (!force && (sent == revision || DateTimeOffset.UtcNow < retryAfter))) return;
        var snapshot = latest; long version = revision; string destination = Link;
        Status = "Atualizando Google Sheets…"; Changed?.Invoke();
        upload = Send();
        async Task Send() {
            try {
                await sheets.UpdateAsync(destination, snapshot, session.Catalog, lifetime.Token);
                sent = version; failures = 0; retryAfter = default;
                Status = "Google Sheets atualizado · " + DateTime.Now.ToString("HH:mm:ss");
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { return; }
            catch (Exception ex) {
                failures++; retryAfter = DateTimeOffset.UtcNow.AddSeconds(Math.Min(60, 2 * Math.Pow(2, Math.Min(failures, 5))));
                Status = "Não sincronizado: " + ex.Message + " Os dados da partida continuam no PC.";
            }
            Changed?.Invoke();
        }
    }
    public async Task UpdateAsync() { previous = ""; Capture(); TryUpload(true); await upload; }
    public Task FlushArchivesAsync() => archives;
    public async Task StopAsync()
    {
        if (stopped) return;
        stopped = true; timer.Stop(); session.Changed -= Capture; lifetime.Cancel();
        await upload; await archives;
    }
}

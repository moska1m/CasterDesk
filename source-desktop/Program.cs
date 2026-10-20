using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;

namespace CasterDesk.Desktop;

internal static class Program
{
    internal static string[] InitialArguments { get; private set; } = [];
    internal static bool Headless { get; private set; }
    internal static void ApplyInitialLanguage(string[] args)
    {
        if (File.Exists(AppPaths.ConfigFile("settings.json"))) return;
        int index = Array.IndexOf(args, "--language");
        if (index < 0 || index + 1 >= args.Length || !L.Languages.Contains(args[index + 1])) return;
        L.Set(args[index + 1]);
        MatchFiles.AtomicText(AppPaths.ConfigFile("settings.json"), System.Text.Json.JsonSerializer.Serialize(new Settings { Language = L.Current }));
    }
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains("--casterdesk-check")) {
            Headless = true;
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CASTERDESK_DATA_DIR")))
                throw new InvalidOperationException("Headless checks require isolated data.");
            AppBuilder.Configure<App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
            SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
            if(args.Contains("--window-colors-only")){WindowColorChecks.Run();}else if(args.Contains("--visual-only")){L.Set("pt-BR");VisualSettingsChecks.Run();}else if(args.Contains("--updates-only")){L.Set("pt-BR");UpdateUiChecks.Run();}else if(args.Contains("--table-only")){L.Set("pt-BR");TableSyncChecks.Run();}else if(args.Contains("--audit-fixes-only")){L.Set("pt-BR");AuditFixChecks.Run();}else if(args.Contains("--spec-only")){L.Set("pt-BR");SpecChecks.Run();}else if(args.Contains("--interaction-only")){L.Set("pt-BR");InteractionChecks.Run();}else if(args.Contains("--art-only")){L.Set("pt-BR");ArtChecks.Run();}else Checks.Run(); return;
        }
        InitialArguments = args;
        string key = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(AppPaths.DataDirectory).ToUpperInvariant())));
        using var instance = new Mutex(false, "Local\\CasterDesk-" + key);
        bool acquired;
        try { acquired = instance.WaitOne(0); } catch (AbandonedMutexException) { acquired = true; }
        if (!acquired) { AppLog.Write("An instance already owns this data folder."); return; }
        try { AppBuilder.Configure<App>().UseWin32().UseSkia().UseHarfBuzz().StartWithClassicDesktopLifetime(args); }
        finally { instance.ReleaseMutex(); }
    }
}

public sealed class App : Application
{
    public override void Initialize() => Ui.ApplyTheme(this);
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            AppLog.Install(); L.LoadPreference();
            var waiting = new StackPanel { Margin = new Thickness(24) };
            var status = Ui.Text("Preparando os dados do CasterDesk…"); waiting.Children.Add(status);
            waiting.Children.Add(new ProgressBar { IsIndeterminate = true });
            var startup = new Window { Title = "CasterDesk", Width = 520, Height = 180, Background = Ui.Canvas, Content = waiting };
            bool working = true; startup.Closing += (_, e) => { if (working) e.Cancel = true; };
            startup.Opened += async (_, _) => {
                try {
                    string? notice = null;
                    await Task.Run(() => { FileBatch.Recover(); DataBackup.ProcessPending(text => notice = text); });
                    L.LoadPreference(); Program.ApplyInitialLanguage(Program.InitialArguments); var shell = new StudioShell();
                    if (notice != null) shell.Notify(notice);
                    desktop.MainWindow = shell; shell.Show(); working = false; startup.Close();
                } catch (Exception ex) {
                    AppLog.Write("CASTERDESK STARTUP: " + ex); working = false;
                    status.Text = L.T("Não foi possível iniciar o CasterDesk.\n") + ex.Message;
                    waiting.Children.Add(Ui.Button("Fechar", () => startup.Close()));
                }
            };
            desktop.MainWindow = startup;
        }
        base.OnFrameworkInitializationCompleted();
    }
}

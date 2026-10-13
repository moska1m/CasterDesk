using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;

namespace CasterDesk.Avalonia;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains("--avalonia-check")) {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CASTERDESK_DATA_DIR")))
                throw new InvalidOperationException("Headless checks require isolated data.");
            AppBuilder.Configure<App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
            SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
            Checks.Run(); return;
        }
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
            AppLog.Install(); L.LoadPreference(); FileBatch.Recover();
            desktop.MainWindow = new StudioShell();
        }
        base.OnFrameworkInitializationCompleted();
    }
}

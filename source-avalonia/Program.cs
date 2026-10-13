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
        try { AppBuilder.Configure<App>().UsePlatformDetect().StartWithClassicDesktopLifetime(args); }
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

// Stage one: standalone image studio. The existing installed release remains usable.
public sealed class StudioShell : Window
{
    readonly StudioView studio;
    public StudioShell()
    {
        Title = "CasterDesk • Avalonia • Central de imagens"; Width = 1440; Height = 900; MinWidth = 980; MinHeight = 640;
        FontFamily = new FontFamily("Segoe UI"); FontSize = 14; Background = Ui.Canvas;
        var session = new MatchSession();
        studio = new StudioView(() => session.Matches, session.Catalog, session.ApplyArtworkVeto);
        var layout = new Grid { RowDefinitions = new("Auto,*,Auto") }; Content = layout;
        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 32, Margin = new Thickness(20, 16) };
        header.Children.Add(Ui.Text("CASTERDESK", true));
        var active = Ui.Text("Central de imagens", true); active.Foreground = Brush.Parse("#FF4335"); header.Children.Add(active);
        layout.Children.Add(header); layout.Children.Add(studio); Grid.SetRow(studio, 1);
        var footer = Ui.Text("CasterDesk • Avalonia — etapa 1"); footer.Margin = new Thickness(20, 4, 20, 10);
        layout.Children.Add(footer); Grid.SetRow(footer, 2);
        studio.Refresh();
        Closing += (_, e) => { if (studio.Exporting || !studio.Flush()) e.Cancel = true; };
        Closed += (_, _) => studio.Stop();
    }
}

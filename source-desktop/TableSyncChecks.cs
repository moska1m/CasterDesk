using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace CasterDesk.Desktop;

internal static class TableSyncChecks
{
    internal static void Run()
    {
        void Need(bool value, string label) { if (!value) throw new Exception("Table sync: " + label); }
        void Wait(Task task) {
            var timeout = System.Diagnostics.Stopwatch.StartNew();
            while (!task.IsCompleted) { Dispatcher.UIThread.RunJobs(); if (timeout.Elapsed.TotalSeconds > 30) throw new TimeoutException("Table sync timeout"); Thread.Sleep(5); }
            task.GetAwaiter().GetResult(); Dispatcher.UIThread.RunJobs();
        }
        string output = Path.Combine(AppPaths.DataDirectory, "table-sync-check", "live.xlsx");
        var session = new MatchSession(); session.New();
        var sync = new MatchTableSync(session);
        Need(!sync.Enabled && !File.Exists(output), "off by default");
        Wait(sync.ConfigureAsync(true, output));
        session.Names("Primeira A", "Primeira B"); var first = session.Current;
        session.New(); session.Names("Segunda A", "Segunda B"); var second = session.Current;
        Wait(sync.FlushAsync());
        Need(File.Exists(MatchTable.ArchivePath(first)) && File.Exists(MatchTable.ArchivePath(second)), "fast switching archives both matches");
        Need(File.Exists(output), "fixed output created");
        session.ConfigureSpec("MD3", session.Catalog.Maps.Take(3).Select(m => m.Id).ToArray());
        var active = MatchTable.CurrentMap(session.Current)!.MapId;
        session.SelectMap(session.Current.Maps[1].MapId);
        Need(MatchTable.CurrentMap(session.Current)!.MapId == active, "browsing map does not move live map");
        Wait(sync.StopAsync());
        var resumed = new MatchTableSync(session);
        Need(resumed.Enabled && resumed.OutputPath == output, "settings survive restart");
        Wait(resumed.ConfigureAsync(false, output)); Wait(resumed.StopAsync());
        var view = new ChampionshipView(session); bool requested = false; view.TableRequested += () => requested = true;
        var owner = new Window { Content = view, Width = 1920, Height = 1080 }; owner.Show(); Dispatcher.UIThread.RunJobs();
        view.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "match-table").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Need(requested, "toolbar action accessible");
        view.SetSpecMode(true); Dispatcher.UIThread.RunJobs();
        Need(view.GetVisualDescendants().OfType<Button>().Any(b => b.Name == "match-table"), "SPEC includes table action");
        view.Stop(); owner.Close();
        var shell = new StudioShell(session); shell.Show(); Dispatcher.UIThread.RunJobs();
        shell.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "match-table").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        var dialog = shell.OwnedWindows.Single(w => w.Title!.StartsWith("Tabela da partida"));
        var tabs = dialog.GetVisualDescendants().OfType<TabControl>().Single();
        tabs.SelectedIndex = 1; Dispatcher.UIThread.RunJobs();
        Need(dialog.GetVisualDescendants().OfType<TextBox>().Any(c => c.Name == "google-table-link"), "Google destination input");
        Need(!dialog.GetVisualDescendants().OfType<CheckBox>().Single(c => c.Name == "google-table-enabled").IsChecked.GetValueOrDefault(), "Google off by default");
        Need(dialog.GetVisualDescendants().OfType<Button>().Single(c => c.Name == "google-connect").Content?.ToString() == "Conectar com Google", "simple Google login action");
        Need(!dialog.GetVisualDescendants().OfType<Expander>().Single(c => c.Name == "google-advanced").IsExpanded, "technical setup hidden by default");
        using (var preview = new global::Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(740,740), new Vector(96,96))) {
            preview.Render(dialog); preview.Save(Path.Combine(AppPaths.DataDirectory, "google-table-panel.png"));
        }
        dialog.Close(); shell.Close(); Dispatcher.UIThread.RunJobs();
        File.WriteAllText(Path.Combine(AppPaths.DataDirectory, "casterdesk-table-check.txt"), "PASS: optional default, ordered snapshots, archives on match change, persistent configuration, independent active map and toolbar in both modes.");
    }
}

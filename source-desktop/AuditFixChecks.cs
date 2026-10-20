using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System.Security.Cryptography;
using System.Text.Json;

namespace CasterDesk.Desktop;

// Runs only from the isolated headless checks, never from the application startup.
internal static class AuditFixChecks
{
    internal static void Run()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CASTERDESK_DATA_DIR")))
            throw new InvalidOperationException("Audit checks require isolated data.");
        int count = 0;
        void Need(bool value, string label) { count++; if (!value) throw new Exception("Audit fixes: " + label); }
        void Pump() => Dispatcher.UIThread.RunJobs();
        void Until(Func<bool> done) {
            var timeout = System.Diagnostics.Stopwatch.StartNew();
            while (!done()) { Pump(); if (timeout.Elapsed > TimeSpan.FromSeconds(30)) throw new TimeoutException("Audit checks timed out."); Thread.Sleep(5); }
            Pump();
        }
        T Find<T>(Control view, string name) where T : Control => view.GetVisualDescendants().OfType<T>().Single(x => x.Name == name);
        string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        string root = Path.Combine(AppPaths.DataDirectory, "audit-fix-checks"); Directory.CreateDirectory(root);
        var catalog = SiegeCatalog.Load();
        count += AuditIntegrityChecks.Run();
        Need(TextsView.OutputFolder("bad\0path") == "", "invalid timer output cannot break state snapshots");
        var roundSession = new MatchSession(catalog); roundSession.New();
        roundSession.ConfigureSpec("MD1", [catalog.Maps[0].Id]); roundSession.Side(roundSession.Map!.MapId, false, 0);
        var roundView = new ChampionshipView(roundSession); roundView.SetSpecMode(true);
        var roundWindow = new Window { Content = roundView, Width = 1280, Height = 800 };
        roundWindow.Show(); Pump();
        try {
            var entry = Find<Grid>(roundView, "round-entry-panel");
            var siteButton = Find<Button>(roundView, "round-site-0");
            var winnerButton = Find<Button>(roundView, "round-winner-0");
            var confirm = Find<Button>(roundView, "confirm-round");
            Need(!confirm.IsEnabled, "round confirmation waits for selections");
            siteButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump();
            winnerButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump();
            Need(confirm.IsEnabled, "SPEC allows confirmation without ending");
            var endingButton = Find<Button>(roundView, "round-ending-defuser_completed");
            endingButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump();
            Need(ReferenceEquals(entry, Find<Grid>(roundView, "round-entry-panel")) && ReferenceEquals(siteButton, Find<Button>(roundView, "round-site-0")), "selection paints existing controls without rebuilding panels");
            Find<Button>(roundView, "round-winner-1").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump();
            Need(!endingButton.IsEnabled && !endingButton.Classes.Contains("primary"), "changing winner clears incompatible ending");
            confirm.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump();
            Need(roundSession.Map!.Rounds.Count == 1 && roundSession.Map.Rounds[0].Winner == 1 && roundSession.Map.Rounds[0].Ending == "", "round submission uses current selection and optional ending");
        } finally { roundWindow.Close(); }
        var pageBitmap = new System.Drawing.Bitmap(8, 8);
        using (var draw = System.Drawing.Graphics.FromImage(pageBitmap)) draw.Clear(System.Drawing.Color.Coral);
        using (var page = new ResultPage("audit-preview", pageBitmap)) {
            string exported = MatchFiles.ExportRendered(roundSession.Current, new ImageBrand(), ArtKind.Announcement, [page]);
            using var loaded = new System.Drawing.Bitmap(Path.Combine(exported, "Imagens", "Horizontal", "Anuncios", "audit-preview.png"));
            Need(loaded.Width == 8 && loaded.Height == 8 && loaded.GetPixel(4, 4).ToArgb() == pageBitmap.GetPixel(4, 4).ToArgb(), "export uses rendered page pixels without rendering or resizing again");
            Need(pageBitmap.GetPixel(4, 4).ToArgb() == System.Drawing.Color.Coral.ToArgb(), "export leaves preview page owned by caller");
        }
        var match = new SiegeMatch { DirectMaps = true, Maps = [new MapRecord { MapId = catalog.Maps[0].Id }] };
        var map = match.Maps[0]; MatchStore.Save(match);
        MapStatistics Data(int? top = null) => new() {
            Status = "partial", TopTeam = top, ConfirmedAt = DateTimeOffset.Now,
            Players = [new PlayerStatistic { Team = 0, Name = "Audit Alpha", Kills = 9 }, new PlayerStatistic { Team = 1, Name = "Audit Beta", Kills = 8 }]
        };
        using (var screenshot = new System.Drawing.Bitmap(24, 16)) {
            using (var draw = System.Drawing.Graphics.FromImage(screenshot)) draw.Clear(System.Drawing.Color.Teal);
            MatchFiles.SaveStatistics(match, map, Data(), screenshot);
        }
        string print = map.Statistics!.Screenshot;
        string printPath = MatchFiles.Resolve(match, print)!;
        string beforeHash = Hash(printPath); DateTime beforeWrite = File.GetLastWriteTimeUtc(printPath);
        int printCount = Directory.GetFiles(Path.GetDirectoryName(printPath)!, "*.png").Length;
        var reopened = MatchStore.Read(Path.Combine(MatchStore.Folder, match.Id + ".json"), catalog);
        Need(reopened.Maps[0].Statistics!.TopTeam == null, "legacy unknown top team persists without assuming blue");
        void SaveWindow(StatsWindow window) {
            window.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, L.T("Confirmar estatísticas"))).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Until(() => !window.IsVisible);
        }
        var stats = new StatsWindow(match, map); stats.Show(); Pump();
        Need(Find<ComboBox>(stats, "stats-top-team").SelectedIndex == -1, "reopening legacy print requires an explicit orientation");
        Need(Find<TextBox>(stats, "stats-player-0-0").Text == "Audit Alpha", "opening the saved print preserves reviewed names without OCR");
        SaveWindow(stats);
        Need(map.Statistics!.TopTeam == null && map.Statistics.Players.Single(p => p.Name == "Audit Alpha").Team == 0, "manual save preserves unknown orientation and teams");
        Need(map.Statistics.Screenshot == print && Hash(printPath) == beforeHash && File.GetLastWriteTimeUtc(printPath) == beforeWrite, "unchanged screenshot reference and bytes are reused");
        Need(Directory.GetFiles(Path.GetDirectoryName(printPath)!, "*.png").Length == printCount, "reopening and saving creates no duplicate screenshot");
        stats = new StatsWindow(match, map); stats.Show(); Pump();
        Find<ComboBox>(stats, "stats-top-team").SelectedIndex = 1; Pump(); SaveWindow(stats);
        Need(map.Statistics!.TopTeam == 1 && map.Statistics.Players.Single(p => p.Name == "Audit Alpha").Team == 0, "choosing previously unknown top team does not invert reviewed teams");
        stats = new StatsWindow(match, map); stats.Show(); Pump();
        Need(Find<ComboBox>(stats, "stats-top-team").SelectedIndex == 1, "known top team is restored on reopen");
        Find<ComboBox>(stats, "stats-top-team").SelectedIndex = 0; Pump(); SaveWindow(stats);
        Need(map.Statistics!.TopTeam == 0 && map.Statistics.Players.Single(p => p.Name == "Audit Alpha").Team == 1, "changing known orientation swaps player teams once");
        bool invalidTop = false; try { StatisticsData.Validate(Data(2)); } catch (InvalidDataException) { invalidTop = true; }
        Need(invalidTop, "invalid top team is rejected");
        var replacement = Data(0); MatchFiles.SaveStatistics(match, map, replacement, null, screenshotChanged: false);
        Need(replacement.Screenshot == print && Directory.GetFiles(Path.GetDirectoryName(printPath)!, "*.png").Length == printCount, "direct no-new-snapshot save keeps the archived print");

        string castingConfig = AppPaths.ConfigFile("casting-settings.json");
        string? previousConfig = File.Exists(castingConfig) ? File.ReadAllText(castingConfig) : null;
        try {
            foreach (int length in new[] { 0, 1, 3, 8 }) {
                var settings = JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(new Settings { Presets = Enumerable.Range(1, length).Select(n => (decimal)n).ToArray(), Output = Path.Combine(root, "timer-" + length + ".txt") }))!;
                int saves = 0; var texts = new TextsView(settings, () => saves++);
                var window = new Window { Content = texts, Width = 1100, Height = 800 }; window.Show(); Pump();
                try {
                    Need(texts.GetVisualDescendants().OfType<TextBox>().Count(t => t.Name?.StartsWith("timer-preset-") == true) == length, "preset controls match persisted array length " + length);
                    var values = Enumerable.Range(1, length).Select(n => n + 0.5).ToArray();
                    texts.ConfigureTimer(90, "mm:ss", "AUDIT END", values); Pump();
                    Need(settings.Presets.SequenceEqual(values.Select(v => (decimal)v)) && saves == 1, "OBS configuration updates every supported preset " + length);
                    Need(Enumerable.Range(0, length).All(i => Find<TextBox>(texts, "timer-preset-" + i).Text == settings.Presets[i].ToString()), "preset edits remain visible " + length);
                } finally { texts.Stop(); window.Close(); }
            }
        } finally {
            if (previousConfig == null) { if (File.Exists(castingConfig)) File.Delete(castingConfig); }
            else File.WriteAllText(castingConfig, previousConfig);
        }

        string txtFolder = Path.Combine(root, "casting"); Directory.CreateDirectory(txtFolder);
        var people = Enumerable.Range(0, 4).Select(n => new Talent { Name = "New Person " + n, Handle = "new_" + n }).ToArray();
        string[] paths = CastingFiles.Keys.SelectMany(key => new[] { Path.Combine(txtFolder, key + ".txt"), Path.Combine(txtFolder, "@" + key + ".txt") }).ToArray();
        foreach (string path in paths) File.WriteAllText(path, "previous:" + Path.GetFileName(path));
        string cfg = Path.Combine(root, "casting-transaction.json"); File.WriteAllText(cfg, "previous config");
        bool lockedFailure = false;
        using (var locked = new FileStream(cfg, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            try { CastingFiles.Write(txtFolder, people, cfg, "new config"); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { lockedFailure = true; }
        }
        Need(lockedFailure, "locked final config rejects partial TXT publication");
        Need(paths.All(path => File.ReadAllText(path) == "previous:" + Path.GetFileName(path)), "all eight TXT files roll back after final config failure");
        Need(File.ReadAllText(cfg) == "previous config", "failed TXT transaction preserves the saved configuration");
        Need(!Directory.GetFiles(root, "*.casterdesk-*", SearchOption.AllDirectories).Any(), "successful rollback cleans temporary transaction files");
        CastingFiles.Write(txtFolder, people, cfg, "new config");
        Need(File.ReadAllText(cfg) == "new config" && File.ReadAllText(paths[0]) == "New Person 0" && File.ReadAllText(paths[7]) == "@new_3", "unlocked retry publishes TXT files and configuration together");
        File.WriteAllText(Path.Combine(AppPaths.DataDirectory, "casterdesk-audit-fix-check.txt"), $"PASS: {count} audit fix checks: protected statistics/sides, stable round controls, preview export, screenshot orientation/reference, dynamic presets and TXT/config rollback. Headless only.");
    }
}

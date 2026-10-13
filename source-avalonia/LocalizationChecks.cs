using Avalonia.VisualTree;
using Avalonia.Media.Imaging;
using System.Text.RegularExpressions;

namespace CasterDesk.Avalonia;

internal static class LocalizationChecks
{
    static int count;
    static void Need(bool condition, string message) { count++; if (!condition) throw new Exception("Localization: " + message); }
    static string Slots(string value) => string.Join(",", Regex.Matches(value, @"(?<!\{)\{(\d+)(?:[^{}]*)\}(?!\})").Select(m => m.Groups[1].Value).Order());
    public static void Run()
    {
        Need(L.Entries.All(e => e.Value.Length == 2 && e.Value.All(v => !string.IsNullOrWhiteSpace(v))), "All entries have EN and ES values");
        Need(L.Entries.All(e => e.Value.All(v => Slots(v) == Slots(e.Key))), "Translation placeholders match source");
        string settingsFile = AppPaths.ConfigFile("settings.json"), preservedSettings = Path.Combine(AppPaths.BackupDirectory, "initial-language-settings.json");
        string settingsText = File.ReadAllText(settingsFile);
        Program.ApplyInitialLanguage(["--language", "es"]); Need(File.ReadAllText(settingsFile) == settingsText, "Installer language does not overwrite existing preferences");
        File.Move(settingsFile, preservedSettings, true);
        try { Program.ApplyInitialLanguage(["--language", "es"]); Need(ConfigurationStore.Read<Settings>("settings.json").Language == "es", "First installation language applied"); }
        finally { File.Move(preservedSettings, settingsFile, true); L.Set("pt-BR"); }
        string file = Path.Combine(AppPaths.BackupDirectory, "language-match.json");
        var custom = new SiegeMatch { TeamA = "Banco", TeamB = "Clube" }; MatchStore.Atomic(file, custom);
        var session = new MatchSession(); session.AttachImported(custom);
        try {
            foreach (string language in L.Languages) {
                L.Set(language);
                var imported = MatchStore.Read(file, session.Catalog);
                Need(imported.TeamA == "Banco" && imported.TeamB == "Clube", "Custom names retained after import in " + language);
                var defaultMatch = new SiegeMatch(); Need(defaultMatch.TeamA == L.T("Equipe A") && defaultMatch.TeamB == L.T("Equipe B"), "Default names follow recipient language");
                var view = new ChampionshipView(session); var owner = new Window { Width = 1280, Height = 800, Content = view, Background = Ui.Canvas };
                owner.Show(); Dispatcher.UIThread.RunJobs();
                var coin = view.GetVisualDescendants().OfType<ComboBox>().Single(b => b.Name == "setup-coin");
                Need(coin.Items.Cast<string>().Skip(1).SequenceEqual(new[] { "Banco", "Clube" }), "Custom names not translated in coin selector");
                Need(view.GetVisualDescendants().OfType<Button>().Any(b => Equals(b.Content, L.T("Configurar lobby"))), "Localized lobby navigation");
                owner.Content = LobbyView.Create(); Dispatcher.UIThread.RunJobs();
                Need(((Control)owner.Content).GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == L.T("03   RODADAS")), "Localized lobby heading");
                Need(((Control)owner.Content).GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text!.StartsWith(L.T("Número de rodadas"))), "Localized lobby instructions");
                ((Control)owner.Content).Measure(new Size(1280, 800)); ((Control)owner.Content).Arrange(new Rect(0, 0, 1280, 800)); Dispatcher.UIThread.RunJobs();
                using (var image = new RenderTargetBitmap(new PixelSize(1280, 800), new Vector(96, 96))) {
                    image.Render((Control)owner.Content); image.Save(Path.Combine(AppPaths.DataDirectory, "avalonia-lobby-" + language + ".png"));
                }
                view.Stop(); owner.Close();
                var stats = new StatsWindow(custom, new MapRecord()); stats.Show(); Dispatcher.UIThread.RunJobs();
                var top = stats.GetVisualDescendants().OfType<ComboBox>().Single(b => b.Name == "stats-top-team");
                Need(top.Items.Cast<string>().SequenceEqual(new[] { "Banco", "Clube" }), "Custom names retained in statistics selector");
                Need(stats.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Banco"), "Custom statistics team heading retained");
                stats.Close(); Dispatcher.UIThread.RunJobs();
                Need(Ui.ActiveQuestion == null && !stats.IsVisible, "Opening statistics alone does not mark edits");
            }
            var edited = new StatsWindow(custom, new MapRecord()); edited.Show(); Dispatcher.UIThread.RunJobs();
            edited.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "stats-player-0-0").Text = "Player edited manually"; Dispatcher.UIThread.RunJobs(); edited.Close(); Dispatcher.UIThread.RunJobs();
            Need(Ui.ActiveQuestion != null && edited.IsVisible, "Real manual edits still require discard confirmation");
            Ui.ActiveQuestion!.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "ask-1").RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs();
            Need(!edited.IsVisible, "Explicit discard closes edited statistics");
        } finally { L.Set("pt-BR"); }
        File.WriteAllText(Path.Combine(AppPaths.DataDirectory, "avalonia-localization-check.txt"), $"PASS: {count} checks: locale entries/placeholders, default and custom team names, cross-language import, lobby and statistics selectors. PT/EN/ES headless only; native regional terminology review remains pending.");
    }
}

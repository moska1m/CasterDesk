using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace CasterDesk.Avalonia;

internal static class ChampionshipChecks
{
    static int checks;
    static void Need(bool value, string message) { checks++; if (!value) throw new Exception("Championship: " + message); }
    static T Find<T>(Control root, string name) where T : Control => root.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);
    static void Click(Button button) { Need(button.IsEnabled, "Action disabled: " + button.Name); button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs(); }
    static void Pump() => Dispatcher.UIThread.RunJobs();
    static void TextButton(Control root, string text) => Click(root.GetVisualDescendants().OfType<Button>().First(b => Equals(b.Content, L.T(text))));

    public static void Run()
    {
        var session = new MatchSession(); session.New();
        var view = new ChampionshipView(session);
        var owner = new Window { Content = view, Background = Ui.Canvas, Width = 1280, Height = 800 };
        owner.Show(); Pump();
        Need(!Find<Button>(view, "veto-0").IsEnabled, "Veto must wait for setup");
        Need(!view.GetVisualDescendants().OfType<Button>().Any(b => Equals(b.Content, L.T("Sortear coin flip"))), "Coin flip is recorded externally, never drawn by the app");
        Find<TextBox>(view, "team-a").Text = "Team North"; Find<TextBox>(view, "team-b").Text = "Team South"; Pump(); view.FlushNames(); Pump();
        Need(session.Current.TeamA == "Team North" && session.Current.TeamB == "Team South", "Team edits persisted");
        Find<ComboBox>(view, "setup-coin").SelectedIndex = 1; Pump();
        Need(!view.VetoReady, "First ban still required");
        Find<ComboBox>(view, "setup-first").SelectedIndex = 1; Pump();
        Need(view.VetoReady && Find<Button>(view, "veto-0").IsEnabled && !Find<Button>(view, "veto-1").IsEnabled, "Progressive veto sequence");
        for (int step = 0; step < 8; step++) {
            Click(Find<Button>(view, "veto-" + step)); Pump();
            Need(view.ActiveDialog != null, "Map selection opens");
            Click(Find<Button>(view.ActiveDialog!, "map-choice-" + session.Catalog.Maps[step].Id)); Pump();
            Need(session.Current.Veto[step] == session.Catalog.Maps[step].Id, "Map choice saved from UI");
        }
        var map = session.Map!; Need(view.VetoComplete && !view.BansReady, "Bans wait for initial side");
        Find<ComboBox>(view, "side-initial-" + map.MapId).SelectedIndex = 1; Pump();
        Need(view.BansReady && view.ActiveDialog != null, "Initial side opens first ban");

        void FinishBans()
        {
            int count = 0;
            while (view.ActiveDialog != null) {
                if (++count > 6) throw new Exception("Ban chain did not end");
                var pending = session.PendingBans(); Need(pending.Length > 0, "Dialog corresponds to pending ban");
                string role = session.Map!.BanRole(pending[0]);
                var ordered = OperatorOrder.Sort(session.Catalog.Operators, role).ToArray();
                var actual = view.ActiveDialog.GetVisualDescendants().OfType<Button>().Where(b => b.Name?.StartsWith("operator-") == true).ToArray();
                Need(actual.Select(b => b.Name![9..]).SequenceEqual(ordered.Select(o => o.Id)), "Operator release order");
                var grid = view.ActiveDialog.GetVisualDescendants().OfType<UniformGrid>().Single(); Need(grid.Columns == 11, "Eleven operator columns");
                string id = actual.First(b => b.IsEnabled).Name![9..]; int slot = pending[0];
                Click(actual.First(b => b.IsEnabled)); Pump(); Need(session.Map.Bans[slot] == id, "Ban stored and next popup automatic");
            }
        }
        FinishBans(); Need(session.PendingBans().Length == 0 && view.RoundConfirm != null, "Quick rounds unlocked after bans");
        foreach (var size in new[] { new Size(980, 640), new Size(1280, 800), new Size(1920, 1080) }) {
            owner.Width = size.Width; owner.Height = size.Height; owner.Measure(size); owner.Arrange(new Rect(size)); Pump(); view.Refresh(); Pump();
            view.Measure(size); view.Arrange(new Rect(size));
            var action = view.RoundConfirm!; var position = action.TranslatePoint(default, view)!.Value;
            Need(action.Bounds.Height > 0 && position.Y >= 0 && position.Y + action.Bounds.Height <= size.Height, "Confirm round visible at " + size);
            var frame = action.GetVisualAncestors().OfType<Border>().First(b => b.Child is Grid g && g.Children.OfType<TextBlock>().Any(t => t.Text == L.T("Round a round")));
            var inside = action.TranslatePoint(default, frame)!.Value;
            Need(inside.Y >= 0 && inside.Y + action.Bounds.Height <= frame.Bounds.Height, "Confirm round cannot overlap footer");
            var history = Find<Grid>(view, "round-history-panel");
            Need(frame.TranslatePoint(default, view)!.Value.X < history.TranslatePoint(default, view)!.Value.X, "Round entry stays left of history");
            Need(frame.TranslatePoint(default, view)!.Value.X >= Find<Border>(view, "bans-panel").TranslatePoint(default, view)!.Value.X + Find<Border>(view, "bans-panel").Bounds.Width, "Bans stay left; both round controls and history stay in the right block");
            var cards = Enumerable.Range(0, 12).Select(n => Find<Button>(view, "ban-slot-" + n)).ToArray();
            Need(cards.Select(c => c.Bounds.Height).Distinct().Count() == 1, "All ban cards share the same height");
            Need(Enumerable.Range(0, 12).Select(n => Find<Image>(view, "ban-image-" + n).Height).Distinct().Count() == 1, "Role symbols and operators share the same image frame");
            var banArea = Find<Grid>(view, "ban-groups");
            Need(cards.All(c => c.TranslatePoint(default, banArea)!.Value.Y + c.Bounds.Height <= banArea.Bounds.Height + 1), "All twelve bans fit without scrolling at " + size + "; available " + banArea.Bounds.Height + "; bottom " + cards.Max(c => c.TranslatePoint(default, banArea)!.Value.Y + c.Bounds.Height));
            var title = view.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == L.T("Confronto"));
            var prior = title.TranslatePoint(default, view)!.Value; Find<ScrollViewer>(view, "setup-scroll").Offset = new Vector(0, 50); Pump();
            Need(title.TranslatePoint(default, view)!.Value == prior, "Confronto title remains fixed when fields scroll");
            Find<ScrollViewer>(view, "setup-scroll").Offset = default; Pump();
            Snapshot(view, size, "championship-active");
        }
        var compactShell = new StudioShell(session) { Width = 980, Height = 640 }; compactShell.Show(); Pump();
        compactShell.Championship.Refresh(); Pump(); Snapshot((Control)compactShell.Content!, new Size(980, 640), "shell-active");
        var compactAction = compactShell.Championship.RoundConfirm!;
        Need(compactAction.Bounds.Height > 0 && compactAction.TranslatePoint(default, compactShell.Championship)!.Value.Y + compactAction.Bounds.Height <= compactShell.Championship.Bounds.Height, "Full shell compact confirmation visible");
        Need(Find<ScrollViewer>(compactShell.Championship, "round-entry-scroll").Bounds.Height >= 32, "Compact shell retains a usable round form viewport");
        compactShell.Close();
        for (int round = 0; round < 7; round++) {
            int targetSite = Array.FindIndex(session.Catalog.Maps.Single(m => m.Id == map.MapId).Sites, s => !map.SiteBlocked(s, round + 1));
            Click(Find<Button>(view, "round-site-" + targetSite)); Click(Find<Button>(view, "round-winner-0"));
            Click(Find<Button>(view, "round-ending-elimination"));
            Click(Find<Button>(view, "confirm-round")); Pump(); FinishBans();
            Need(session.Map!.Rounds.Count == round + 1, "One round saved per UI confirmation");
        }
        Need(session.Map!.Score().A == 7 && session.Map.Score().B == 0, "UI score 7-0");
        TextButton(view, "Confirmar vencedor"); Need(Ui.ActiveQuestion != null, "Confirm result question"); Click(Find<Button>(Ui.ActiveQuestion!, "ask-1")); Pump();
        Need(session.Map.Confirmed && session.Current.Complete, "Confirmed result retained");
        Click(Find<Button>(view, "history-round-0")); Need(view.ActiveDialog == null && session.Map.Rounds.Count == 7, "Confirmed rounds cannot be edited silently");
        foreach (var size in new[] { new Size(980, 640), new Size(1280, 800), new Size(1920, 1080) }) {
            owner.Width = size.Width; owner.Height = size.Height; owner.Measure(size); owner.Arrange(new Rect(size)); Pump(); view.Refresh(); Pump();
            view.Measure(size); view.Arrange(new Rect(size)); Snapshot(view, size, "championship-complete");
        }
        TextButton(view, "Revisar resultado"); Need(view.ActiveDialog != null, "Review opens");
        Find<TextBox>(view.ActiveDialog!, "review-reason").Text = "Correct broadcast record"; TextButton(view.ActiveDialog!, "Abrir revisão"); Pump();
        Need(!session.Map.Confirmed && Directory.GetFiles(Path.Combine(MatchFiles.DirectoryFor(session.Current), "Revisoes")).Length == 1, "Audited review preserves previous result");
        Click(Find<Button>(view, "history-round-0")); Need(view.ActiveDialog != null, "Round editor opens after review");
        Click(Find<Button>(view.ActiveDialog!, "review-confirm")); Pump(); Need(view.ActiveDialog == null && session.Map.Rounds.Count == 7, "Round editor saves without deleting subsequent rounds");
        for (int format = 0; format < 3; format++) {
            session.New(); Pump(); Find<ComboBox>(view, "setup-format").SelectedIndex = format; Pump();
            Need(session.Current.Format == new[] { "MD1", "MD3", "MD5" }[format], "Series format selector");
        }
        session.Setup("MD3", 0, 0, false); for (int step = 0; step < 5; step++) session.Veto(step, session.Catalog.Maps[step].Id);
        Pump(); Need(session.Current.Maps.Count == 1 && !view.BansReady && Find<ComboBox>(view, "side-initial-" + session.Map!.MapId).IsEnabled, "Picked map side unlocks before veto finishes");
        TextButton(view, "Configurar lobby"); Pump();
        Need(view.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == L.T("03   RODADAS")), "Lobby guidance available");
        Click(Find<Button>(view, "lobby-section-2")); Pump();
        Need(view.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == L.T("Número de rodadas")), "Lobby section selection displays setting rows");
        var completed = new SiegeMatch { TeamA = "Team North", TeamB = "Team South", CoinWinner = 0, FirstBan = 0, Veto = session.Catalog.Maps.Take(8).Select(m => m.Id).ToArray() };
        completed.SyncMaps(session.Catalog); var finished = completed.Maps[0]; finished.InitialAttack = 0; finished.OtAttack = 0;
        var sites = session.Catalog.Maps.Single(m => m.Id == finished.MapId).Sites;
        for(int n=0;n<15;n++) finished.Rounds.Add(new RoundRecord { Winner=n%2, Site=sites[n%4], Ending="elimination" });
        for(int slot=0;slot<12;slot++) finished.Bans[slot] = session.Catalog.Operators.First(o => o.Role==finished.BanRole(slot) && !finished.Bans.Skip(slot/6*6).Take(6).Contains(o.Id)).Id;
        finished.ConfirmedAt=DateTimeOffset.Now;
        var completeSession=new MatchSession();completeSession.AttachImported(completed);var completeView=new ChampionshipView(completeSession);
        var completeOwner=new Window { Content=completeView,Background=Ui.Canvas,Width=1920,Height=1080 };completeOwner.Show();Pump();completeView.Refresh();Pump();
        Need(Enumerable.Range(0,12).All(n => !Find<Button>(completeView,"ban-slot-"+n).IsEnabled), "Confirmed bans remain protected");
        Need(Enumerable.Range(0,12).All(n => Find<Button>(completeView,"ban-slot-"+n).GetVisualDescendants().OfType<Border>().Any(b => Equals(b.Background,Find<Button>(completeView,"ban-slot-"+n).Background))), "Team backgrounds retained on confirmed bans");
        Need(Enumerable.Range(0,12).All(n => !Find<Button>(completeView,"ban-slot-"+n).GetVisualAncestors().OfType<ScrollViewer>().Any()), "All twelve assigned bans require no scrolling");
        Snapshot(completeView,new Size(1920,1080),"championship-all-bans");completeView.Stop();completeOwner.Close();
        view.Stop(); owner.Content = null; owner.Close();
        var shell = new StudioShell(session); shell.Show(); Pump();
        Need(shell.MenuWidth == 0, "Closed menu reserves no width"); shell.ToggleMenu(); Pump(); Need(shell.MenuWidth > 0 && shell.MenuWidth <= 300, "Menu inside available width");
        TextButton(shell, "Modo clássico"); Pump(); Need(!shell.Championship.QuickMode, "Classic registration option applies");
        TextButton(shell, "Modo rápido"); Pump(); Need(shell.Championship.QuickMode, "Quick registration option applies");
        TextButton(shell, "Claro"); Pump(); Need(!Ui.Dark, "Light theme option applies"); TextButton(shell, "Escuro"); Pump(); Need(Ui.Dark, "Dark theme option applies");
        shell.ToggleMenu(); Need(shell.MenuWidth == 0, "Menu closes compactly"); shell.Navigate(1); Pump();
        Need(shell.GetVisualDescendants().OfType<StudioView>().Any(), "Navigation to shared studio");
        shell.Navigate(0); shell.Width = 1280; shell.Height = 800; Pump(); Snapshot((Control)shell.Content!, new Size(1280, 800), "shell"); shell.Close();
        File.WriteAllText(Path.Combine(AppPaths.DataDirectory, "avalonia-championship-check.txt"), $"PASS: {checks} UI checks: progressive setup, map picks, release-order operator grid, automatic ban chaining, rounds, confirmation, protected edits, review, MD formats, lobby, menu and appearance. Headless backend only.");
    }

    static void Snapshot(Control view, Size size, string name)
    {
        view.Measure(size); view.Arrange(new Rect(size));
        using var image = new RenderTargetBitmap(new PixelSize((int)size.Width, (int)size.Height), new Vector(96, 96));
        image.Render(view); image.Save(Path.Combine(AppPaths.DataDirectory, $"{name}-{size.Width}.png"));
    }
}

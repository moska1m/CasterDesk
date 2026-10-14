namespace CasterDesk.Avalonia;

public sealed partial class ChampionshipView
{
    Control Rounds()
    {
        RoundConfirm = null; var map = Session.Map;
        if (map == null) return Ui.Text("Escolha um mapa primeiro.");
        var panel = new Grid { Name = "round-entry-panel", RowDefinitions = new("*,Auto") };
        bool editable = !map.Confirmed && !Session.Current.Complete && map.Score().Winner < 0;
        bool ready = BansReady && Session.PendingBans().Length == 0 && (map.Rounds.Count < 12 || map.OtAttack >= 0);
        if (editable && ready) {
            var form = QuickMode ? QuickRound(map) : CompactRound(map);
            Add(panel, new ScrollViewer { Name = "round-entry-scroll", Content = form, MaxHeight = compact ? 190 : 285 }, 0);
            RoundConfirm = Action("Confirmar round", async () => {
                Session.Round(site, winner, ending); site = ending = ""; winner = -1; Refresh(); await OpenNextBan();
            }, true);
            RoundConfirm.Name = "confirm-round";
            RoundConfirm.IsEnabled = !string.IsNullOrEmpty(site) && winner >= 0 && !string.IsNullOrEmpty(ending);
            Add(panel, RoundConfirm, 0, 1);
        } else if (editable) {
            panel.RowDefinitions[0].Height = GridLength.Auto;
            string message = !VetoComplete ? "Conclua o veto de mapas para registrar rounds."
                : map.InitialAttack < 0 ? "Defina os lados antes de escolher os operadores."
                : map.Rounds.Count >= 12 && map.OtAttack < 0 ? "Overtime sem lado definido." : "Conclua os bans de operadores para registrar o próximo round.";
            var hint = new StackPanel(); hint.Children.Add(Ui.Text(message));
            var next = Action("Banir próximo", OpenNextBan); next.IsEnabled = BansReady && Session.PendingBans().Length > 0; hint.Children.Add(next); Add(panel, hint, 0);
        } else panel.RowDefinitions[0].Height = new GridLength(0);
        return panel;
    }

    Control QuickRound(MapRecord map)
    {
        int round = map.Rounds.Count + 1, attacker = map.Attacker(round);
        var form = new Grid { RowDefinitions = new("Auto,Auto"), ColumnDefinitions = new("*,*"), VerticalAlignment = VerticalAlignment.Top };
        var heading = Ui.RawText($"{L.T("Round")} {round:00} · {Session.Current.Team(attacker)} {L.T("Ataque")} / {Session.Current.Team(1 - attacker)} {L.T("Defesa")}"); heading.FontSize = 12; form.Children.Add(heading); SetColumnSpan(heading, 2);
        var sites = new UniformGrid { Columns = 2, VerticalAlignment = VerticalAlignment.Top };
        foreach (string bombsite in Session.Catalog.Maps.Single(m => m.Id == map.MapId).Sites) {
            var button = Ui.Button(BombsiteNames.Display(map.MapId, bombsite), () => { site = bombsite; Refresh(); }, site == bombsite);
            button.Content = new TextBlock { Text = BombsiteNames.Display(map.MapId, bombsite), TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, FontSize = 12 };
            button.Name = "round-site-" + Array.IndexOf(Session.Catalog.Maps.Single(m => m.Id == map.MapId).Sites, bombsite);
            button.IsEnabled = !map.SiteBlocked(bombsite, round); button.Margin = new Thickness(3); button.FontSize = 12;
            if (site == bombsite) button.BorderBrush = Brushes.White;
            sites.Children.Add(button);
        }
        Add(form, sites, 0, 1);
        var decisions = new StackPanel();
        var teams = new UniformGrid { Columns = 2 };
        for (int team = 0; team < 2; team++) {
            int selected = team;
            var button = Ui.Button(Session.Current.Team(team) + " · " + L.T(team == attacker ? "Ataque" : "Defesa"), () => {
                winner = selected; if (!RoundEnding.Allowed(ending, winner, attacker)) ending = ""; Refresh();
            });
            button.Name = "round-winner-" + team; button.Margin = new Thickness(3);
            button.Content = new StackPanel { Children = {
                new TextBlock { Text = L.T(team == attacker ? "Ataque" : "Defesa"), FontSize = 11, TextAlignment = TextAlignment.Center },
                new TextBlock { Text = Session.Current.Team(team), FontSize = 12, TextAlignment = TextAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis }
            } };
            button.Background = Ui.Team(team, winner == team); button.Foreground = Brushes.White;
            button.BorderThickness = new Thickness(winner == team ? 2 : 0); button.BorderBrush = Brushes.White; teams.Children.Add(button);
        }
        decisions.Children.Add(teams);
        var ends = new UniformGrid { Columns = 2 };
        foreach (string code in RoundEnding.Codes.Skip(1)) {
            var button = Ui.Button(RoundEnding.Label(code), () => { ending = code; Refresh(); }, ending == code);
            button.Content = new TextBlock { Text = RoundEnding.Label(code), TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, FontSize = 11 }; button.Padding = new Thickness(4);
            button.Name = "round-ending-" + code; button.Margin = new Thickness(3); button.FontSize = 12;
            button.IsEnabled = winner >= 0 && RoundEnding.Allowed(code, winner, attacker); ends.Children.Add(button);
        }
        decisions.Children.Add(ends); Add(form, decisions, 1, 1); return form;
    }

    Control CompactRound(MapRecord map)
    {
        var form = new StackPanel();
        var sites = Ui.Combo(); sites.ItemsSource = Session.Catalog.Maps.Single(m => m.Id == map.MapId).Sites
            .Where(s => !map.SiteBlocked(s, map.Rounds.Count + 1)).Select(s => new Choice(s, BombsiteNames.Display(map.MapId, s))).ToArray();
        sites.SelectedItem = sites.Items.OfType<Choice>().FirstOrDefault(c => c.Id == site);
        var teams = Ui.RawCombo(L.T("Selecionar…"), Session.Current.TeamA, Session.Current.TeamB); teams.SelectedIndex = winner + 1;
        var ends = Ui.Combo(); ends.ItemsSource = RoundEnding.Codes.Where(c => c == "" || winner >= 0 && RoundEnding.Allowed(c, winner, map.Attacker(map.Rounds.Count + 1))).Select(c => new Choice(c, RoundEnding.Label(c))).ToArray();
        ends.SelectedItem = ends.Items.OfType<Choice>().FirstOrDefault(c => c.Id == ending);
        sites.SelectionChanged += (_, _) => { if (sites.SelectedItem is Choice c && c.Id != site) { site = c.Id; Refresh(); } };
        teams.SelectionChanged += (_, _) => { if (teams.SelectedIndex - 1 != winner) { winner = teams.SelectedIndex - 1; ending = ""; Refresh(); } };
        ends.SelectionChanged += (_, _) => { if (ends.SelectedItem is Choice c && c.Id != ending) { ending = c.Id; Refresh(); } };
        form.Children.Add(Ui.Text("Bomb site")); form.Children.Add(sites); form.Children.Add(Ui.Text("Vencedor")); form.Children.Add(teams); form.Children.Add(Ui.Text("Finalização")); form.Children.Add(ends); return form;
    }

    Control History(MapRecord map)
    {
        var panel = new StackPanel { Spacing = 3 };
        string[] labels = ["Round", "Ataque", "Defesa", "Bomb site", "Vencedor", "Placar", "Finalização", ""];
        var heading = HistoryLine(labels); heading.Background = Ui.Panel; panel.Children.Add(heading);
        int a = 0, b = 0;
        for (int index = 0; index < map.Rounds.Count; index++) {
            int row = index; var record = map.Rounds[index]; if (record.Winner == 0) a++; else b++;
            var line = HistoryLine([ (index + 1).ToString(), Session.Current.Team(map.Attacker(index + 1)), Session.Current.Team(1 - map.Attacker(index + 1)),
                ShortSite(map.MapId, record.Site), Session.Current.Team(record.Winner), $"{a}–{b}", RoundEnding.Label(record.Ending), "›" ]);
            var button = Action("", () => EditRound(row)); button.Content = line; button.Name = "history-round-" + index;
            button.HorizontalContentAlignment = HorizontalAlignment.Stretch; button.Background = Ui.Team(record.Winner, false); button.Foreground = Brushes.White; button.Margin = new Thickness(0); button.Padding = new Thickness(2);
            button.MinHeight = 0; button.Height = 28;
            ToolTip.SetTip(button, L.T("Clique para corrigir um round.")); panel.Children.Add(button);
        }
        if (map.Rounds.Count == 0) panel.Children.Add(Ui.Text("Nenhum round registrado."));
        return new ScrollViewer { Content = panel, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    }
    static Grid HistoryLine(string[] values)
    {
        var line = Columns(".55*,1.15*,1.15*,1.9*,1.15*,.6*,1.5*,.2*");
        for (int n = 0; n < values.Length; n++) {
            var text = new TextBlock { Text = n < 7 && values[0] == "Round" ? L.T(values[n]) : values[n], FontSize = 11,
                TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(3, 3) };
            ToolTip.SetTip(text, text.Text);
            Add(line, text, n);
        }
        return line;
    }

    internal static string ShortSite(string mapId, string site)
    {
        string text = BombsiteNames.Display(mapId, site);
        foreach (var pair in new[] { ("Escritório Comandante", "Comandante"), ("Escritório do Comandante", "Comandante"), ("Salão de Jogos", "Jogos"), ("Sala de Jogos", "Jogos"), ("Dormitório Principal", "Dormitório"), ("Master Bedroom", "Bedroom"), ("Game Room", "Games"), ("Commander Office", "Commander"), ("Dormitorio principal", "Dormitorio"), ("Sala de juegos", "Juegos") })
            text = text.Replace(pair.Item1, pair.Item2, StringComparison.OrdinalIgnoreCase);
        return text.Replace(" - ", " / ");
    }
}

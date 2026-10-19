namespace CasterDesk.Avalonia;

public sealed partial class ChampionshipView
{
    ScrollViewer? historyScroll;
    string historyMap = "";
    int historyCount;
    bool historyPendingFollow;
    Control Rounds()
    {
        RoundConfirm = null; var map = Session.Map;
        if (map == null) return Ui.Text("Escolha um mapa primeiro.");
        var panel = new Grid { Name = "round-entry-panel", RowDefinitions = new(SpecMode?"Auto,Auto":"*,Auto") };
        bool editable = !map.Confirmed && !Session.Current.Complete && map.Score().Winner < 0;
        bool ready = (!SpecMode || SpecConfigurationReady) && BansReady && (SpecMode || Session.PendingBans().Length == 0) && (map.Rounds.Count < 12 || map.OtAttack >= 0);
        if (editable && ready) {
            var form = (SpecMode || QuickMode) ? QuickRound(map) : CompactRound(map);
            Add(panel, new ScrollViewer { Name = "round-entry-scroll", Content = form, MaxHeight = SpecMode ? 310 : compact ? 190 : 285 }, 0);
            RoundConfirm = Action("Confirmar round", async () => {
                Session.Round(site, winner, ending); if(!SpecMode && !UseObsForBans)await OpenNextBan();
            }, true);
            RoundConfirm.Name = "confirm-round";
            RoundConfirm.IsEnabled = !string.IsNullOrEmpty(site) && winner >= 0 && (SpecMode || !string.IsNullOrEmpty(ending));
            if(SpecMode){RoundConfirm.Margin=new Thickness(0,8,0,0);RoundConfirm.Background=RoundConfirm.IsEnabled?Brush.Parse("#25834C"):Brush.Parse("#485869");}
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
        var form = new Grid { RowDefinitions = new("Auto"), ColumnDefinitions = new(SpecMode ? "*" : "*,*"), VerticalAlignment = VerticalAlignment.Top };
        var siteButtons = new List<(Button Button, string Site)>();
        var teamButtons = new List<(Button Button, int Team)>();
        var endingButtons = new List<(Button Button, string Code)>();
        void PaintSelection()
        {
            foreach (var item in siteButtons) {
                bool selected = site == item.Site; item.Button.Classes.Set("primary", selected);
                item.Button.BorderBrush = Brush.Parse("#FF4335"); item.Button.BorderThickness = new Thickness(selected ? 2 : 0);
            }
            foreach (var item in teamButtons) {
                bool selected = winner == item.Team; item.Button.Background = Ui.Team(item.Team, selected);
                item.Button.BorderThickness = new Thickness(selected ? 2 : 0);
            }
            foreach (var item in endingButtons) {
                bool selected = ending == item.Code; item.Button.Classes.Set("primary", selected);
                item.Button.BorderBrush = Brush.Parse("#FF4335"); item.Button.BorderThickness = new Thickness(selected ? 2 : 0);
                item.Button.IsEnabled = winner >= 0 && RoundEnding.Allowed(item.Code, winner, attacker);
            }
            if (RoundConfirm != null) {
                RoundConfirm.IsEnabled = !string.IsNullOrEmpty(site) && winner >= 0 && (SpecMode || !string.IsNullOrEmpty(ending));
                if (SpecMode) RoundConfirm.Background = RoundConfirm.IsEnabled ? Brush.Parse("#25834C") : Brush.Parse("#485869");
            }
        }
        var sites = new UniformGrid { Columns = 2, VerticalAlignment = VerticalAlignment.Top };
        foreach (string bombsite in Session.Catalog.Maps.Single(m => m.Id == map.MapId).Sites) {
            var button = Ui.Button(BombsiteNames.Display(map.MapId, bombsite), () => { site = bombsite; PaintSelection(); }, site == bombsite);
            button.Content = new TextBlock { Text = BombsiteNames.Display(map.MapId, bombsite), TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, FontSize = 12 };
            button.Name = "round-site-" + Array.IndexOf(Session.Catalog.Maps.Single(m => m.Id == map.MapId).Sites, bombsite);
            button.IsEnabled = !map.SiteBlocked(bombsite, round); button.Margin = new Thickness(3); button.FontSize = 12;
            button.VerticalContentAlignment=VerticalAlignment.Center;button.HorizontalContentAlignment=HorizontalAlignment.Center;button.Height = SpecMode ? 46 : 54; button.Padding = new Thickness(4);
            if (site == bombsite) {button.BorderBrush=Brush.Parse("#FF4335");button.BorderThickness=new Thickness(2);}
            siteButtons.Add((button, bombsite)); sites.Children.Add(button);
        }
        Add(form, sites, 0);
        var decisions = new StackPanel();
        var teams = new UniformGrid { Columns = 2 };
        for (int team = 0; team < 2; team++) {
            int selected = team;
            var button = Ui.Button(Session.Current.Team(team) + " · " + L.T(team == attacker ? "Ataque" : "Defesa"), () => {
                winner = selected; if (!RoundEnding.Allowed(ending, winner, attacker)) ending = ""; PaintSelection();
            });
            button.Name = "round-winner-" + team; button.Margin = new Thickness(3);
            button.Content = new TextBlock { Text = SpecMode ? SpecTeam(team) : Session.Current.Team(team), FontSize = 12, TextAlignment = TextAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            button.Height = 34; button.Padding = new Thickness(4); ToolTip.SetTip(button, Session.Current.Team(team) + " · " + L.Role(team == attacker ? "ATQ" : "DEF"));
            button.Background = Ui.Team(team, winner == team); button.Foreground = Brushes.White;
            button.BorderThickness = new Thickness(winner == team ? 2 : 0); button.BorderBrush = Brush.Parse("#FF4335");button.VerticalContentAlignment=VerticalAlignment.Center; teamButtons.Add((button,team)); teams.Children.Add(button);
        }
        decisions.Children.Add(teams);
        var ends = new UniformGrid { Columns = SpecMode ? 4 : 2 };
        foreach (string code in RoundEnding.Codes.Skip(1)) {
            var button = Ui.Button(RoundEnding.Label(code), () => { ending = code; PaintSelection(); }, ending == code);
            button.Content = new TextBlock { Text = RoundEnding.Label(code), TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, FontSize = 11 }; button.Padding = new Thickness(4);
            if(ending==code){button.BorderBrush=Brush.Parse("#FF4335");button.BorderThickness=new Thickness(2);}
            button.VerticalContentAlignment=VerticalAlignment.Center;button.Name = "round-ending-" + code; button.Margin = new Thickness(3); button.FontSize = 12;
            button.Height = 34;
            button.IsEnabled = winner >= 0 && RoundEnding.Allowed(code, winner, attacker); endingButtons.Add((button,code)); ends.Children.Add(button);
        }
        if(SpecMode){var label=Ui.Text("Finalização · opcional");label.FontSize=11;label.Margin=new Thickness(3,3);decisions.Children.Add(label);}
        decisions.Children.Add(ends); if(SpecMode){form.RowDefinitions=new("Auto,Auto");Add(form,decisions,0,1);}else Add(form, decisions, 1); return form;
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
        string context = Session.Current.Id + ":" + map.MapId;
        bool follow = context != historyMap || map.Rounds.Count > historyCount || historyPendingFollow;
        historyPendingFollow = follow;
        Vector offset = historyScroll?.Offset ?? default;
        historyMap = context; historyCount = map.Rounds.Count;
        var scroll = new ScrollViewer { Name = "round-history-scroll", Content = panel, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        historyScroll = scroll;
        void RestorePosition(object? sender, EventArgs args) {
            if (scroll.Viewport.Height <= 0 || scroll.Extent.Height <= 0) return;
            scroll.LayoutUpdated -= RestorePosition;
            if (historyScroll != scroll) return;
            if (follow) { scroll.ScrollToEnd(); historyPendingFollow = false; } else scroll.Offset = offset;
        }
        scroll.LayoutUpdated += RestorePosition;
        return scroll;
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

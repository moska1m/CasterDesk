namespace CasterDesk.Avalonia;

public sealed partial class ChampionshipView : Grid
{
    public MatchSession Session { get; }
    public bool Processing { get; private set; }
    public event Action<SiegeMatch, MapRecord>? MapConfirmed;
    readonly ContentControl body = new();
    readonly TextBlock notice = Ui.Text("");
    readonly DispatcherTimer namesTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    readonly DispatcherTimer resizeTimer = new() { Interval = TimeSpan.FromMilliseconds(120) };
    TextBox teamA = new(), teamB = new();
    bool building, namesDirty, lobby, stopped;
    bool compact = true;
    public bool QuickMode { get; private set; } = true;
    string site = "", ending = "", roundContext = "";
    int winner = -1;
    internal Button? RoundConfirm { get; private set; }
    internal bool VetoReady => !string.IsNullOrWhiteSpace(Session.Current.TeamA) && !string.IsNullOrWhiteSpace(Session.Current.TeamB)
        && Session.Current.CoinWinner >= 0 && Session.Current.FirstBan >= 0;
    internal bool VetoComplete => Session.Current.Veto.All(v => !string.IsNullOrEmpty(v));
    internal bool BansReady => VetoComplete && Session.Map?.InitialAttack >= 0;

    public ChampionshipView(MatchSession? session = null)
    {
        Session = session ?? new MatchSession();
        Margin = new Thickness(10); Background = Ui.Canvas; RowDefinitions = new("Auto,*,Auto");
        var tabs = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        tabs.Children.Add(Action("Partida", () => { lobby = false; Refresh(); }));
        tabs.Children.Add(Action("Configurar lobby", () => { lobby = true; Refresh(); }));
        Children.Add(tabs); Children.Add(body); SetRow(body, 1);
        notice.FontSize = 12; notice.Margin = new Thickness(4); Children.Add(notice); SetRow(notice, 2);
        Session.Changed += Refresh; Session.MapConfirmed += Confirmed;
        namesTimer.Tick += (_, _) => { namesTimer.Stop(); try { FlushNames(); } catch (Exception ex) { notice.Text = ex.Message; } };
        SizeChanged += (_, _) => { resizeTimer.Stop(); resizeTimer.Start(); };
        resizeTimer.Tick += (_, _) => { resizeTimer.Stop(); Refresh(); };
        Refresh(); if (Session.Warnings.Count > 0) notice.Text = string.Join(" · ", Session.Warnings);
    }

    void Confirmed(SiegeMatch match, MapRecord map) => MapConfirmed?.Invoke(match, map);
    public void Stop() { stopped = true; namesTimer.Stop(); resizeTimer.Stop(); Session.Changed -= Refresh; Session.MapConfirmed -= Confirmed; }
    public void SetQuickMode(bool value) { QuickMode = value; Refresh(); }
    public void FlushNames()
    {
        if (!namesDirty) return;
        string a = teamA.Text ?? "", b = teamB.Text ?? "";
        Session.Names(a, b); namesDirty = false; namesTimer.Stop(); Refresh();
    }
    async Task Run(Func<Task> action)
    {
        if (Processing) return;
        try { FlushNames(); await action(); if (!stopped) notice.Text = L.T("Salvo automaticamente • ") + DateTime.Now.ToString("HH:mm:ss"); }
        catch (Exception ex) { if (!stopped) { notice.Text = ex.Message; Refresh(); } }
    }
    Button Action(string label, System.Action action, bool primary = false) => Action(label, () => { action(); return Task.CompletedTask; }, primary);
    Button Action(string label, Func<Task> action, bool primary = false) => Ui.Button(label, () => _ = Run(action), primary);
    static Grid Columns(string widths) => new() { ColumnDefinitions = new(widths) };
    static void Add(Grid grid, Control child, int column, int row = 0) { grid.Children.Add(child); SetColumn(child, column); SetRow(child, row); }

    internal void Refresh()
    {
        if (building || namesDirty || stopped) return;
        compact = Bounds.Height < 800;
        string context = Session.Current.Id + ":" + Session.Map?.MapId + ":" + Session.Map?.Rounds.Count;
        if (context != roundContext) { site = ending = ""; winner = -1; roundContext = context; }
        building = true;
        try { body.Content = lobby ? LobbyView.Create() : Match(); }
        finally { building = false; }
    }

    Control Match()
    {
        var match = Session.Current; var map = Session.Map;
        var layout = new Grid { RowDefinitions = new("Auto,Auto,Auto,*,Auto"), ColumnDefinitions = new("44*,56*") };
        var toolbar = new WrapPanel(); toolbar.Children.Add(Action("Nova partida", Session.New, true));
        var saved = Ui.Combo(); saved.Width = 370; saved.ItemsSource = Session.Matches.ToArray(); saved.SelectedItem = match;
        saved.SelectionChanged += (_, _) => { if (!building && saved.SelectedItem is SiegeMatch picked && picked.Id != Session.Current.Id) _ = Run(() => { Session.Select(picked.Id); return Task.CompletedTask; }); };
        toolbar.Children.Add(saved); Add(layout, toolbar, 0); SetColumnSpan(toolbar, 2);
        var left = new StackPanel();
        if (compact && match.Maps.Count > 0) left.Children.Add(new Expander { Header = L.T("Confronto"), Content = Setup(match), HorizontalAlignment = HorizontalAlignment.Stretch });
        else left.Children.Add(Ui.Card("Confronto", Setup(match)));
        bool shortReady = Bounds.Height < 650 && BansReady;
        if (shortReady) left.Children.Add(new Expander { Header = L.T("Mapas da partida • lados da equipe indicada"), Content = Sides(match), HorizontalAlignment = HorizontalAlignment.Stretch });
        else left.Children.Add(Ui.Card("Mapas da partida • lados da equipe indicada", Sides(match)));
        Add(layout, new ScrollViewer { Content = left, MaxHeight = Bounds.Height < 700 ? 190 : compact ? 260 : 340 }, 0, 1);
        Add(layout, shortReady
            ? new Expander { Header = L.T("Mapas • picks e bans"), Content = Veto(match), HorizontalAlignment = HorizontalAlignment.Stretch }
            : Ui.Card("Mapas • picks e bans", Veto(match)), 1, 1);
        var score = map?.Score(); var bar = new Grid();
        var selectedMap = Ui.Combo(); selectedMap.Width = 190; selectedMap.HorizontalAlignment = HorizontalAlignment.Left;
        selectedMap.ItemsSource = match.Maps.Select(m => new Choice(m.MapId, Session.Catalog.Maps.Single(c => c.Id == m.MapId).DisplayName)).ToArray();
        selectedMap.SelectedIndex = map == null ? -1 : match.Maps.IndexOf(map);
        selectedMap.SelectionChanged += (_, _) => { if (!building && selectedMap.SelectedItem is Choice item && Session.Map?.MapId != item.Id) _ = Run(() => { Session.SelectMap(item.Id); return Task.CompletedTask; }); };
        Add(bar, selectedMap, 0);
        var scoreText = Ui.Text($"{match.TeamA}   {score?.A ?? 0} × {score?.B ?? 0}   {match.TeamB}", true);
        scoreText.Name = "match-score"; scoreText.TextAlignment = TextAlignment.Center; scoreText.HorizontalAlignment = HorizontalAlignment.Center;
        scoreText.MaxWidth = Math.Max(160, Bounds.Width * .56 - 400); scoreText.Margin = new Thickness(8); Add(bar, scoreText, 0);
        Add(layout, bar, 1, 2);
        Add(layout, Ui.Card("Bans de operadores", Bans()), 0, 3);
        Add(layout, Ui.Card("Round a round", Rounds()), 1, 3);
        var actions = new WrapPanel();
        actions.Children.Add(Action("Banir próximo", OpenNextBan));
        actions.Children.Add(Action("Resumo para remake", () => Ui.CopyText(Session.Summary())));
        actions.Children.Add(Action("Salvar agora", () => MatchStore.Save(Session.Current)));
        actions.Children.Add(Action("Arquivos da partida", Files));
        var confirm = Action("Confirmar vencedor", async () => { if (await Ui.Ask(this, "Confirmar resultado e bloquear os rounds deste mapa?") == 1) Session.Confirm(); }, true);
        confirm.IsEnabled = map != null && !map.Confirmed && map.Score().Winner >= 0; actions.Children.Add(confirm);
        var review = Action("Revisar resultado", Review); review.IsEnabled = map?.Confirmed == true; actions.Children.Add(review);
        Add(layout, actions, 0, 4); SetColumnSpan(actions, 2);
        return layout;
    }

    Control Setup(SiegeMatch match)
    {
        var panel = new StackPanel(); var brand = MatchFiles.ReadBrand(match);
        for (int n = 0; n < 2; n++) {
            int team = n; var line = Columns("Auto,*,Auto");
            string? path = MatchFiles.Resolve(match, n == 0 ? brand.TeamALogo : brand.TeamBLogo);
            var logo = new Image { Width = 36, Height = 36, Stretch = Stretch.Uniform, Margin = new Thickness(0, 0, 8, 6) };
            try { logo.Source = Ui.FileImage(path ?? Path.Combine(AppContext.BaseDirectory, "assets", "promo", "neutral-white.png")); } catch (Exception ex) { AppLog.Write(ex.ToString()); }
            var input = new TextBox { Name = n == 0 ? "team-a" : "team-b", Text = match.Team(n), PlaceholderText = L.T(n == 0 ? "Equipe A:" : "Equipe B:"), MaxLength = 80, Margin = new Thickness(0, 0, 8, 6), IsEnabled = !match.HasConfirmed };
            input.TextChanged += (_, _) => { if (!building && TopLevel.GetTopLevel(input) != null && input.Text != match.Team(team)) { namesDirty = true; namesTimer.Stop(); namesTimer.Start(); } };
            if (team == 0) teamA = input; else teamB = input;
            Add(line, logo, 0); Add(line, input, 1); Add(line, Action("Logo", () => Logo(team)), 2); panel.Children.Add(line);
        }
        var settings = Columns("*,*,*");
        var format = Ui.Combo(L.FormatName("MD1"), L.FormatName("MD3"), L.FormatName("MD5")); format.Name = "setup-format";
        format.SelectedIndex = match.Format == "MD5" ? 2 : match.Format == "MD3" ? 1 : 0;
        var coin = Ui.RawCombo(L.T("Coin flip:"), match.TeamA, match.TeamB); coin.Name = "setup-coin"; coin.SelectedIndex = match.CoinWinner + 1;
        var first = Ui.RawCombo(L.T("Primeiro ban:"), match.TeamA, match.TeamB); first.Name = "setup-first"; first.SelectedIndex = match.FirstBan + 1;
        void Field(int column, string label, Control control) { var field = new StackPanel(); var heading = Ui.Text(label); heading.FontSize = 12; heading.Margin = new Thickness(0, 0, 0, 3); field.Children.Add(heading); field.Children.Add(control); Add(settings, field, column); }
        Field(0, "Formato:", format); Field(1, "Coin flip:", coin); Field(2, "Primeiro ban:", first); settings.IsEnabled = !match.HasConfirmed;
        async Task Configure()
        {
            if (building || TopLevel.GetTopLevel(settings) == null || Session.Current.Id != match.Id) return;
            string value = format.SelectedIndex == 2 ? "MD5" : format.SelectedIndex == 1 ? "MD3" : "MD1";
            int c = coin.SelectedIndex - 1, f = first.SelectedIndex - 1;
            if (value == Session.Current.Format && c == Session.Current.CoinWinner && f == Session.Current.FirstBan) return;
            bool reset = match.Veto.Any(v => !string.IsNullOrEmpty(v)) && (value != match.Format || f != match.FirstBan);
            if (reset && await Ui.Ask(this, "Alterar o formato ou primeiro ban limpa o veto e os registros desta partida. Continuar?") != 1) { Refresh(); return; }
            Session.Setup(value, c, f, reset);
        }
        format.SelectionChanged += (_, _) => _ = Run(Configure); coin.SelectionChanged += (_, _) => _ = Run(Configure); first.SelectionChanged += (_, _) => _ = Run(Configure);
        panel.Children.Add(settings);
        var toss = Action("Sortear coin flip", () => Session.Setup(match.Format, Random.Shared.Next(2), match.FirstBan, false)); toss.IsEnabled = !match.HasConfirmed; panel.Children.Add(toss);
        return panel;
    }

    Control Sides(SiegeMatch match)
    {
        var panel = new StackPanel();
        var headings = Columns("2*,2*,2*"); Add(headings, Ui.Text("Mapa"), 0); Add(headings, Ui.Text("Lado inicial"), 1); Add(headings, Ui.Text("OT"), 2); panel.Children.Add(headings);
        foreach (var record in match.Maps) {
            var line = Columns("2*,2*,2*"); var label = Ui.Text(Session.Catalog.Maps.Single(m => m.Id == record.MapId).DisplayName); label.VerticalAlignment = VerticalAlignment.Center; label.Margin = new Thickness(0, 0, 8, 10); Add(line, label, 0);
            var initial = Ui.RawCombo(L.T("Selecionar…"), match.TeamA + " · " + L.Role("ATQ"), match.TeamB + " · " + L.Role("ATQ")); initial.SelectedIndex = record.InitialAttack + 1;
            initial.Name = "side-initial-" + record.MapId;
            var ot = Ui.RawCombo(L.T("Selecionar…"), match.TeamA + " · " + L.Role("ATQ"), match.TeamB + " · " + L.Role("ATQ")); ot.SelectedIndex = record.OtAttack + 1;
            initial.IsEnabled = ot.IsEnabled = !record.Confirmed && !match.Complete; Add(line, initial, 1); Add(line, ot, 2);
            initial.SelectionChanged += (_, _) => _ = Run(async () => {
                int selected = initial.SelectedIndex - 1; if (building || TopLevel.GetTopLevel(initial) == null || selected == record.InitialAttack) return;
                if (record.HasData && await Ui.Ask(this, "Alterar o lado inicial recalcula ataque/defesa dos rounds e limpa os bans deste mapa, pois mudam os tipos permitidos. Continuar?") != 1) { Refresh(); return; }
                Session.Side(record.MapId, false, selected, true); await OpenNextBan();
            });
            ot.SelectionChanged += (_, _) => { int selected = ot.SelectedIndex - 1; if (!building && TopLevel.GetTopLevel(ot) != null && selected != record.OtAttack) _ = Run(() => { Session.Side(record.MapId, true, selected); return Task.CompletedTask; }); };
            panel.Children.Add(line);
        }
        if (match.Maps.Count == 0) panel.Children.Add(Ui.Text("Escolha os mapas no veto para configurar os lados."));
        return panel;
    }

    Control Veto(SiegeMatch match)
    {
        var grid = new UniformGrid { Columns = 3 };
        int next = Array.FindIndex(match.Veto, string.IsNullOrEmpty);
        for (int i = 0; i < 9; i++) {
            int step = i; string? id = i < 8 ? match.Veto[i] : VetoComplete ? Session.Catalog.Maps.Single(m => !match.Veto.Contains(m.Id)).Id : null;
            var map = Session.Catalog.Maps.FirstOrDefault(m => m.Id == id);
            string label = i == 8 ? "09 · DECIDER" : $"{i + 1:00} · {match.Action(i)} · {match.Team(match.FirstBan < 0 ? 0 : match.Actor(i))}";
            var button = Action("", () => ChooseMap(step)); button.Name = "veto-" + step;
            button.Height = Bounds.Height < 650 ? 38 : compact ? 62 : 88; button.Margin = new Thickness(3); button.Padding = new Thickness(0);
            button.HorizontalContentAlignment = HorizontalAlignment.Stretch; button.VerticalContentAlignment = VerticalAlignment.Stretch; button.Content = MapCard(map, label, i < 8 ? match.Action(i) : "PICK");
            button.IsEnabled = i < 8 && VetoReady && !match.Complete && (next < 0 || i <= next);
            ToolTip.SetTip(button, L.T("Defina coin flip e primeiro ban antes do veto.")); grid.Children.Add(button);
        }
        return grid;
    }

    Control MapCard(SiegeMap? map, string label, string action)
    {
        var grid = new Grid { ClipToBounds = true };
        if (map != null) try { grid.Children.Add(new Image { Source = Ui.FileImage(SiegeCatalog.Asset(map.Image)), Stretch = Stretch.UniformToFill }); } catch (Exception ex) { AppLog.Write(ex.ToString()); }
        grid.Children.Add(new Border { Background = Brush.Parse(action == "BAN" ? "#B5410F15" : "#A00A2D1E") });
        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = Brushes.White, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap });
        text.Children.Add(new TextBlock { Text = map?.DisplayName ?? L.T("Selecionar…"), FontSize = 14, Foreground = Brushes.White, FontWeight = FontWeight.Bold, TextAlignment = TextAlignment.Center });
        grid.Children.Add(text); return grid;
    }

    Control Bans()
    {
        var map = Session.Map;
        if (!VetoComplete) return Ui.Text("Conclua o veto de mapas para escolher os operadores.");
        if (!BansReady || map == null) return Ui.Text("Defina os lados antes de escolher os operadores.");
        var panel = new StackPanel();
        for (int half = 0; half < 2; half++) {
            int attacker = half == 0 ? map.InitialAttack : 1 - map.InitialAttack;
            var heading = Columns("Auto,*");
            if (map.ActiveBanHalf(Math.Min(15, map.Rounds.Count + 1)) == half) Add(heading, new Border { Background = Brush.Parse("#247348"), Padding = new Thickness(8, 3), Child = new TextBlock { Text = L.T("ATIVOS"), Foreground = Brushes.White, FontSize = 11 } }, 0);
            var text = Ui.RawText($"{L.T("Round")} {half * 6 + 1}–{half * 6 + 6} · {Session.Current.Team(attacker)} {L.Role("ATQ")} / {Session.Current.Team(1 - attacker)} {L.Role("DEF")}"); text.TextAlignment = TextAlignment.Center; text.FontSize = 12; Add(heading, text, 1); panel.Children.Add(heading);
            var grid = new UniformGrid { Columns = 6 };
            for (int slot = half * 6; slot < half * 6 + 6; slot++) {
                int field = slot; var op = Session.Catalog.Operators.FirstOrDefault(o => o.Id == map.Bans[slot]);
                var content = new StackPanel();
                string file = op == null ? StudioRenderer.Asset(map.BanRole(slot) == "DEF" ? "Icon_Defender.png" : "Icon_Attacker.png") : SiegeCatalog.Asset(op.Id + "-portrait.png");
                try { content.Children.Add(new Image { Source = Ui.FileImage(file, compact && op != null), Height = op == null ? 50 : compact ? 76 : 180, Stretch = op == null ? Stretch.Uniform : Stretch.UniformToFill }); } catch (Exception ex) { AppLog.Write(ex.ToString()); }
                content.Children.Add(new TextBlock { Text = op?.Name ?? L.T(map.BanStageReached(slot) ? "Selecionar…" : "Bloqueado"), FontSize = op?.Name.Length > 10 ? 10 : 11, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap });
                content.Children.Add(new TextBlock { Text = Session.Current.Team(MapRecord.BanTeam(slot)) + "\n" + L.T("Round") + " " + (MapRecord.BanThreshold(slot) + 1), FontSize = 10, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap });
                var button = Action("", async () => { if (!map.Confirmed || await Ui.Ask(this, "Corrigir o ban de um mapa confirmado?") == 1) await PickBan(field); });
                button.Name = "ban-slot-" + slot; button.Padding = new Thickness(3); button.Margin = new Thickness(3); button.Content = content;
                button.IsEnabled = map.BanStageReached(slot) && !map.Confirmed; button.BorderThickness = new Thickness(0, 0, 0, 3); button.BorderBrush = Ui.Team(MapRecord.BanTeam(slot)); grid.Children.Add(button);
            }
            panel.Children.Add(grid);
        }
        return new ScrollViewer { Content = panel };
    }

    sealed record Choice(string Id, string Name) { public override string ToString() => Name; }
}

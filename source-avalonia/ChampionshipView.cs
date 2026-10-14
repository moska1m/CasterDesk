namespace CasterDesk.Avalonia;

public sealed partial class ChampionshipView : Grid
{
    public MatchSession Session { get; }
    public bool Processing { get; private set; }
    public event Action<SiegeMatch, MapRecord>? MapConfirmed;
    readonly ContentControl body = new();
    readonly TextBlock notice = Ui.Text("");
    readonly Button[] sectionTabs = new Button[2];
    readonly ContentControl matchToolbar = new();
    int lobbySection;
    string lobbyBackground = "";
    readonly DispatcherTimer namesTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    readonly DispatcherTimer resizeTimer = new() { Interval = TimeSpan.FromMilliseconds(120) };
    TextBox teamA = new(), teamB = new();
    bool building, namesDirty, lobby, stopped;
    bool compact = true;
    public bool QuickMode { get; private set; } = true;
    public bool SpecMode { get; private set; }
    public event Action<bool>? SpecModeChanged;
    public void SetSpecMode(bool value) { FlushNames(); SpecMode = value; SpecModeChanged?.Invoke(value); specEditing = -1; specDraftKey = ""; Refresh(); }
    string site = "", ending = "", roundContext = "";
    int winner = -1;
    internal Button? RoundConfirm { get; private set; }
    internal bool VetoReady => !string.IsNullOrWhiteSpace(Session.Current.TeamA) && !string.IsNullOrWhiteSpace(Session.Current.TeamB)
        && Session.Current.CoinWinner >= 0 && Session.Current.FirstBan >= 0;
    internal bool VetoComplete => Session.Current.DirectMaps || Session.Current.Veto.All(v => !string.IsNullOrEmpty(v));
    internal bool BansReady => VetoComplete && Session.Map?.InitialAttack >= 0;
    internal bool UseObsForBans { get; set; }
    bool obsConnected;
    Border? obsTrack, obsKnob;
    internal void SetObsConnected(bool value) { obsConnected = value; PaintObsMode(); }
    void PaintObsMode() {
        if (obsTrack == null || obsKnob == null) return;
        obsTrack.Background = Brush.Parse(UseObsForBans && obsConnected ? "#25834C" : "#82363C");
        obsKnob.HorizontalAlignment = UseObsForBans ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    }
    internal Action<bool>? ObsBansModeChanged { get; set; }
    internal bool DockReady => !stopped && !building && !namesDirty && !Processing && ActiveDialog == null && Ui.ActiveQuestion == null;

    public ChampionshipView(MatchSession? session = null)
    {
        Session = session ?? new MatchSession();
        Margin = new Thickness(10); Background = Ui.Canvas; RowDefinitions = new("Auto,*,Auto");
        var tabs = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 0 };
        sectionTabs[0] = Action("Partida", () => { lobby = false; Refresh(); });
        sectionTabs[1] = Action("Configurar lobby", () => { var pool = Session.Catalog.Maps.Where(m=>m.Id!=lobbyBackground).ToArray(); lobbyBackground=pool[Random.Shared.Next(pool.Length)].Id; lobby = true; Refresh(); });
        foreach (var tab in sectionTabs) {
            tab.Padding = new Thickness(20, 10); tab.Margin = new Thickness(0); tabs.Children.Add(tab);
            tab.PointerEntered += (_, _) => tab.Background = Brush.Parse("#FF4335");
            tab.PointerExited += (_, _) => tab.Background = tab == sectionTabs[lobby ? 1 : 0] ? Ui.Panel : Brushes.Transparent;
        }
        var navigation = new Grid { ColumnDefinitions = new("*,Auto") }; navigation.Children.Add(tabs);
        navigation.Children.Add(matchToolbar); SetColumn(matchToolbar,1);
        Children.Add(navigation); Children.Add(body); SetRow(body, 1);
        notice.FontSize = 12; notice.Margin = new Thickness(4); Children.Add(notice); SetRow(notice, 2);
        Session.Changed += Refresh; Session.MapConfirmed += Confirmed;
        namesTimer.Tick += (_, _) => { namesTimer.Stop(); try { FlushNames(); } catch (Exception ex) { notice.Text = ex.Message; } };
        SizeChanged += (_, _) => { resizeTimer.Stop(); resizeTimer.Start(); };
        resizeTimer.Tick += (_, _) => { resizeTimer.Stop(); Refresh(); };
        KeyDown += (_, e) => SpecShortcut(e);
        Refresh(); if (Session.Warnings.Count > 0) notice.Text = string.Join(" · ", Session.Warnings);
    }

    void Confirmed(SiegeMatch match, MapRecord map) => MapConfirmed?.Invoke(match, map);
    public void Stop() { stopped = true; namesTimer.Stop(); resizeTimer.Stop(); Session.Changed -= Refresh; Session.MapConfirmed -= Confirmed; }
    public void SetQuickMode(bool value) { QuickMode = value; Refresh(); }
    public void FlushNames()
    {
        if (!namesDirty) return;
        string a = teamA.Text ?? "", b = teamB.Text ?? "";
        if(SpecMode){if(Session.Current.TeamAIsDefault && a==L.T("Equipe Azul"))a="";if(Session.Current.TeamBIsDefault && b==L.T("Equipe Laranja"))b="";}
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
        if (context != roundContext) { specEditing=-1; site = ending = ""; winner = -1; roundContext = context; }
        building = true;
        try {
            sectionTabs[0].Content=L.T(SpecMode?"Partida · SPEC":"Partida");
            for (int n = 0; n < sectionTabs.Length; n++) { bool active = lobby == (n == 1); sectionTabs[n].BorderThickness = new Thickness(0, 0, 0, 3); sectionTabs[n].BorderBrush = active ? Brush.Parse("#FF4335") : Brushes.Transparent; sectionTabs[n].Background = active ? Ui.Panel : Brushes.Transparent; }
            matchToolbar.IsVisible = !lobby;
            body.Content = lobby ? LobbyView.Create(lobbySection, index => lobbySection = index, lobbyBackground) : SpecMode ? SpecMatch() : Match();
        }
        finally { building = false; }
    }

    Control Match()
    {
        var match = Session.Current; var map = Session.Map;
        var layout = new Grid { Name = "match-layout", RowDefinitions = new("0,3*,2*,Auto"), ColumnDefinitions = new("40*,60*") };
        layout.RowDefinitions[2].MaxHeight = compact ? 300 : 350;
        if (Bounds.Height < 650 && BansReady) layout.RowDefinitions[1].Height = new GridLength(88);
        var toolbar = new StackPanel { Name = "match-toolbar", Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var newMatch = Action("Nova partida", Session.New, true); newMatch.Name = "new-match"; newMatch.Height = 32; newMatch.Margin = new Thickness(0); toolbar.Children.Add(newMatch);
        var saved = Ui.Combo(); saved.Width = 370; saved.ItemsSource = Session.Matches.ToArray(); saved.SelectedItem = match;
        saved.SelectionChanged += (_, _) => { if (!building && saved.SelectedItem is SiegeMatch picked && picked.Id != Session.Current.Id) _ = Run(() => { Session.Select(picked.Id); return Task.CompletedTask; }); };
        saved.Name = "match-selector"; saved.Width = Math.Min(370, Math.Max(250, Bounds.Width * .25));
        saved.Height = 32; saved.MinHeight = 0; saved.Margin = new Thickness(0);
        toolbar.Children.Add(saved); matchToolbar.Content = toolbar;
        var left = new Grid { RowDefinitions = new("1.6*,*") };
        left.Children.Add(Ui.Card("Confronto", new ScrollViewer { Name = "setup-scroll", Content = Setup(match) }));
        Add(left, Ui.Card("Mapas da partida • lados da equipe indicada", new ScrollViewer { Content = Sides(match) }), 0, 1);
        Add(layout, left, 0, 1);
        Add(layout, Ui.Card("Mapas • picks e bans", Veto(match)), 1, 1);
        var score = map?.Score();
        var selectedMap = Ui.Combo(); selectedMap.Width = 190; selectedMap.HorizontalAlignment = HorizontalAlignment.Left;
        selectedMap.ItemsSource = match.Maps.Select(m => new Choice(m.MapId, Session.Catalog.Maps.Single(c => c.Id == m.MapId).DisplayName)).ToArray();
        selectedMap.SelectedIndex = map == null ? -1 : match.Maps.IndexOf(map);
        selectedMap.SelectionChanged += (_, _) => { if (!building && selectedMap.SelectedItem is Choice item && Session.Map?.MapId != item.Id) _ = Run(() => { Session.SelectMap(item.Id); return Task.CompletedTask; }); };
        var bansPanel = Ui.Card("Bans de operadores", Bans());
        var bansBody = (Grid)bansPanel.Child!; selectedMap.Width = 180; selectedMap.Margin = new Thickness(0); selectedMap.HorizontalAlignment = HorizontalAlignment.Right;
        bansBody.Children.Add(selectedMap);
        bansBody.Children.Remove(selectedMap);
        var title=bansBody.Children.OfType<TextBlock>().First();bansBody.Children.Remove(title);
        var banHeader=Columns("*,Auto,Auto");banHeader.Background=Ui.Header;banHeader.Margin=new Thickness(-10,-10,-10,2);banHeader.MinHeight=30;
        title.Background=Brushes.Transparent;title.Margin=new Thickness(0);Add(banHeader,title,0);selectedMap.Width=compact?140:180;selectedMap.Height=30;selectedMap.MinHeight=0;selectedMap.Margin=new Thickness(0,0,6,0);Add(banHeader,selectedMap,2);
        obsKnob = new Border { Width = 16, Height = 16, CornerRadius = new CornerRadius(8), Background = Brushes.White, Margin = new Thickness(3), VerticalAlignment = VerticalAlignment.Center };
        obsTrack = new Border { Width = 42, Height = 22, CornerRadius = new CornerRadius(11), Child = obsKnob };
        var obsMode=new ToggleButton {Name="use-obs-bans",Content=obsTrack,IsChecked=UseObsForBans,Width=42,Height=22,Padding=new Thickness(0),Margin=new Thickness(4,0,8,0)};
        obsMode.Template = new global::Avalonia.Controls.Templates.FuncControlTemplate<ToggleButton>((control, _) => new global::Avalonia.Controls.Presenters.ContentPresenter { Content = control.Content });
        ToolTip.SetTip(obsMode,L.T("Usar plugin do OBS"));
        global::Avalonia.Automation.AutomationProperties.SetName(obsMode,L.T("Plugin OBS"));
        obsMode.IsCheckedChanged+=(_,_)=>{bool value=obsMode.IsChecked==true;try{ObsBansModeChanged?.Invoke(value);UseObsForBans=value;PaintObsMode();}catch(Exception ex){notice.Text=ex.Message;obsMode.IsChecked=UseObsForBans;}};
        var obsLine = new StackPanel { Name = "obs-mode-line", Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        obsLine.Children.Add(new TextBlock { Text = L.T("Plugin OBS").ToUpperInvariant(), FontSize = 12, FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center }); obsLine.Children.Add(obsMode); PaintObsMode();
        Add(banHeader,obsLine,1);bansBody.Children.Add(banHeader);
        bansPanel.Name = "bans-panel"; Add(layout, bansPanel, 0, 2);
        var scoreText = Ui.Text($"{match.TeamA}   {score?.A ?? 0} × {score?.B ?? 0}   {match.TeamB}", true);
        scoreText.Name = "match-score"; scoreText.TextAlignment = TextAlignment.Center; scoreText.HorizontalAlignment = HorizontalAlignment.Center;
        scoreText.MaxWidth = Math.Max(200, Bounds.Width * .58); scoreText.Margin = new Thickness(4);
        var roundArea = new Grid { Name = "round-area", ColumnDefinitions = new("44*,56*") };
        var roundCard = Ui.Card("Round a round", Rounds());
        roundCard.Name = "round-entry-card";
        var roundBody = (Grid)roundCard.Child!;
        var roundTitle = roundBody.Children.OfType<TextBlock>().First(); roundBody.Children.Remove(roundTitle);
        var roundHeader = Columns("Auto,*");roundHeader.Background=Ui.Header;roundHeader.Margin=new Thickness(-10,-10,-10,8);
        roundTitle.Margin=new Thickness(0);roundTitle.Background=Brushes.Transparent; Add(roundHeader, roundTitle, 0);
        if (map?.InitialAttack >= 0) {
            int number = map.Rounds.Count + 1, attacker = map.Attacker(number);
            var info = Ui.RawText($"{L.T("Round")} {number:00} · {match.Team(attacker)} {L.Role("ATQ")} / {match.Team(1-attacker)} {L.Role("DEF")}");
            info.Name = "round-header-context"; info.FontSize = 11; info.TextAlignment = TextAlignment.Right;
            info.Text=info.Text?.ToUpperInvariant();info.FontWeight=FontWeight.Bold;
            info.Margin = new Thickness(8, 4, 6, 4); ToolTip.SetTip(info, info.Text); Add(roundHeader, info, 1);
        }
        roundBody.Children.Add(roundHeader); Add(roundArea, roundCard, 0);
        var history = new Grid { Name = "round-history-panel", RowDefinitions = new("Auto,*,Auto") }; history.Children.Add(scoreText);
        Add(history, Ui.Card("Histórico de rounds", map == null ? Ui.Text("Escolha um mapa primeiro.") : History(map)), 0, 1);
        Add(roundArea, history, 1); Add(layout, roundArea, 1, 2);
        var actions = new WrapPanel();
        var nextBan = Action("Banir próximo", OpenNextBan);nextBan.Name="next-ban";actions.Children.Add(nextBan);
        var remake = Action("Resumo para remake", ShowRemake); remake.Name = "remake-summary"; actions.Children.Add(remake);
        actions.Children.Add(Action("Salvar agora", () => MatchStore.Save(Session.Current)));
        actions.Children.Add(Action("Arquivos da partida", Files));
        var confirm = Action("Confirmar vencedor", async () => { if (await Ui.Ask(this, "Confirmar resultado e bloquear os rounds deste mapa?", winnerConfirmation: true) == 1) Session.Confirm(); }, true);
        confirm.Name = "confirm-winner"; confirm.IsEnabled = map != null && !map.Confirmed && map.Score().Winner >= 0;
        confirm.Background = confirm.IsEnabled ? Brush.Parse("#25834C") : Brush.Parse("#334354");
        confirm.Opacity = confirm.IsEnabled ? 1 : .5; Add(history, confirm, 0, 2);
        var review = Action("Revisar resultado", Review); review.IsEnabled = map?.Confirmed == true; actions.Children.Add(review);
        Add(layout, actions, 0, 3); SetColumnSpan(actions, 2);
        return layout;
    }

    Control Setup(SiegeMatch match)
    {
        var panel = new StackPanel(); var brand = MatchFiles.ReadBrand(match);
        for (int n = 0; n < 2; n++) {
            int team = n; var line = Columns("Auto,*,Auto");
            string? path = MatchFiles.Resolve(match, n == 0 ? brand.TeamALogo : brand.TeamBLogo);
            var logo = new Image { Width = 36, Height = 36, Stretch = Stretch.Uniform, Margin = new Thickness(0, 0, 8, 6) };
            try { logo.Source = Ui.FileImage(path ?? Path.Combine(AppContext.BaseDirectory, "assets", "promo", "neutral-white.png"), thumbnail: 144); } catch (Exception ex) { AppLog.Write(ex.ToString()); }
            var input = new TextBox { Name = n == 0 ? "team-a" : "team-b", Text = match.Team(n), PlaceholderText = L.T(n == 0 ? "Equipe A:" : "Equipe B:"), MaxLength = 80, Margin = new Thickness(0, 0, 8, 6), IsEnabled = !match.HasConfirmed };
            input.TextChanged += (_, _) => { if (!building && TopLevel.GetTopLevel(input) != null && input.Text != match.Team(team)) { namesDirty = true; namesTimer.Stop(); namesTimer.Start(); } };
            if (team == 0) teamA = input; else teamB = input;
            Add(line, logo, 0); Add(line, input, 1); Add(line, Action("Logo", () => Logo(team)), 2); panel.Children.Add(line);
        }
        var settings = Columns("*,*,*");
        var format = Ui.Combo(L.FormatName("MD1"), L.FormatName("MD3"), L.FormatName("MD5")); format.Name = "setup-format";
        format.SelectedIndex = match.Format == "MD5" ? 2 : match.Format == "MD3" ? 1 : 0;
        var coin = Ui.RawCombo(L.T("Selecionar…"), match.TeamA, match.TeamB); coin.Name = "setup-coin"; coin.SelectedIndex = match.CoinWinner + 1;
        var first = Ui.RawCombo(L.T("Selecionar…"), match.TeamA, match.TeamB); first.Name = "setup-first"; first.SelectedIndex = match.FirstBan + 1;
        void Field(int column, string label, Control control) { var field = new StackPanel(); var heading = Ui.Text(label); heading.FontSize = 12; heading.Height = 32; heading.Margin = new Thickness(0, 0, 0, 3); field.Children.Add(heading); field.Children.Add(control); Add(settings, field, column); }
        Field(0, "Formato:", format); Field(1, "Vencedor do coin flip", coin); Field(2, "Primeiro ban:", first); settings.IsEnabled = !match.HasConfirmed && !match.DirectMaps;
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
                Session.Side(record.MapId, false, selected, true);
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
        if (match.DirectMaps) {
            foreach (var record in match.Maps) {
                string id = record.MapId; var mapCard = Action("", () => { Session.SelectMap(id); });
                mapCard.Content = MapCard(Session.Catalog.Maps.Single(m => m.Id == id), "", "PICK");
                mapCard.HorizontalContentAlignment = HorizontalAlignment.Stretch; mapCard.VerticalContentAlignment = VerticalAlignment.Stretch;
                mapCard.Margin = new Thickness(2); mapCard.Padding = new Thickness(0); grid.Children.Add(mapCard);
            }
            return grid;
        }
        int next = Array.FindIndex(match.Veto, string.IsNullOrEmpty);
        for (int i = 0; i < 9; i++) {
            int step = i; string? id = i < 8 ? match.Veto[i] : VetoComplete ? Session.Catalog.Maps.Single(m => !match.Veto.Contains(m.Id)).Id : null;
            var map = Session.Catalog.Maps.FirstOrDefault(m => m.Id == id);
            string label = i == 8 ? "09 · DECIDER" : $"{i + 1:00} · {match.Action(i)} · {match.Team(match.FirstBan < 0 ? 0 : match.Actor(i))}";
            var button = Action("", () => ChooseMap(step)); button.Name = "veto-" + step;
            button.MinHeight = 0; button.Margin = new Thickness(2); button.Padding = new Thickness(0); button.VerticalAlignment = VerticalAlignment.Stretch;
            button.HorizontalContentAlignment = HorizontalAlignment.Stretch; button.VerticalContentAlignment = VerticalAlignment.Stretch; button.Content = MapCard(map, label, map == null ? "" : i < 8 ? match.Action(i) : "PICK");
            button.IsEnabled = i < 8 && VetoReady && !match.Complete && (next < 0 || i <= next);
            ToolTip.SetTip(button, L.T("Defina coin flip e primeiro ban antes do veto.")); grid.Children.Add(button);
        }
        return grid;
    }

    Control MapCard(SiegeMap? map, string label, string action)
    {
        var grid = new Grid { ClipToBounds = true };
        if (map != null) try { grid.Children.Add(new Image { Source = Ui.FileImage(SiegeCatalog.Asset(map.Image), grayscale: action == "USED"), Stretch = Stretch.UniformToFill, Opacity = action == "USED" ? .45 : 1 }); } catch (Exception ex) { AppLog.Write(ex.ToString()); }
        grid.Children.Add(new Border { Background = Brush.Parse(action == "BAN" ? "#B5410F15" : action == "PICK" ? "#A00A2D1E" : "#50101820") });
        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        if (!string.IsNullOrEmpty(label)) text.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = Brushes.White, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap });
        text.Children.Add(new TextBlock { Text = map?.DisplayName ?? L.T("Selecionar…"), FontSize = string.IsNullOrEmpty(label) ? 20 : 14, Foreground = Brushes.White, FontWeight = FontWeight.Bold, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap });
        grid.Children.Add(text); return grid;
    }

    Control Bans()
    {
        var map = Session.Map;
        if (!VetoComplete) return Ui.Text("Conclua o veto de mapas para escolher os operadores.");
        if (!BansReady || map == null) return Ui.Text("Defina os lados antes de escolher os operadores.");
        var panel = new Grid { Name = "ban-groups", RowDefinitions = new("*,*") };
        for (int half = 0; half < 2; half++) {
            var group = new Grid { Margin = new Thickness(2, half == 0 ? 0 : 6, 2, 0), RowDefinitions = new("24,*"), ClipToBounds = true };
            int attacker = half == 0 ? map.InitialAttack : 1 - map.InitialAttack;
            var heading = Columns("Auto,*");
            var text = Ui.RawText($"{L.T("Round")} {half * 6 + 1}–{half * 6 + 6} · {Session.Current.Team(attacker)} {L.Role("ATQ")} / {Session.Current.Team(1 - attacker)} {L.Role("DEF")}"); text.TextAlignment = TextAlignment.Center; text.FontSize = 12; text.TextWrapping = TextWrapping.NoWrap; text.TextTrimming = TextTrimming.CharacterEllipsis; ToolTip.SetTip(text, text.Text); Add(heading, text, 1); group.Children.Add(heading);
            group.Children.Remove(heading);
            var titleBand = new Border { Name = "ban-half-title-" + half, Background = Ui.Header, Padding = new Thickness(4, 2), Child = heading };
            text.Text=text.Text?.ToUpperInvariant();text.FontWeight=FontWeight.Bold;text.Margin=new Thickness(0);text.VerticalAlignment=VerticalAlignment.Center;
            group.Children.Add(titleBand);
            var grid = new UniformGrid { Columns = 6, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 3, 0, 0) };
            foreach (int slot in map.BanOrder(half)) {
                int field = slot; var op = Session.Catalog.Operators.FirstOrDefault(o => o.Id == map.Bans[slot]);
                var content = new Grid { RowDefinitions = new("*,Auto") };
                string file = op == null ? StudioRenderer.Asset(map.BanRole(slot) == "DEF" ? "Icon_Defender.png" : "Icon_Attacker.png") : SiegeCatalog.Asset(op.Id + "-portrait.png");
                try {
                    var portrait = new Grid { Name = "ban-portrait-" + slot, Height = Bounds.Height < 650 ? 24 : compact ? 26 : 72 };
                    portrait.Children.Add(new Image { Name = "ban-image-" + slot, Source = op != null && !compact ? Ui.FileImage(file, thumbnail: 192) : Ui.FileImage(file, op != null), Height = op == null ? portrait.Height * .55 : portrait.Height, Stretch = op == null || !compact ? Stretch.Uniform : Stretch.UniformToFill, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
                    if (op != null) portrait.Children.Add(new Image { Source = Ui.FileImage(SiegeCatalog.Asset(op.Icon)), Height = compact ? 16 : 20, Width = compact ? 16 : 20, VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Center });
                    content.Children.Add(portrait);
                } catch (Exception ex) { AppLog.Write(ex.ToString()); }
                var caption = new StackPanel(); caption.Children.Add(new TextBlock { Text = op?.Name ?? L.T(map.BanStageReached(slot) ? "Selecionar…" : "Bloqueado"), FontSize = compact ? 9 : op?.Name.Length > 10 ? 10 : 11, FontWeight = FontWeight.Bold, TextAlignment = TextAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis });
                caption.Children.Add(new TextBlock { Text = Session.Current.Team(MapRecord.BanTeam(slot)), FontSize = compact ? 9 : 10, TextAlignment = TextAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis });
                Add(content,new Border { Background = Brush.Parse("#D00C1521"), Padding = new Thickness(2,1), Child = caption },0,1);
                var button = Action("", async () => { if (!map.Confirmed || await Ui.Ask(this, "Corrigir o ban de um mapa confirmado?") == 1) await PickBan(field); });
                button.Name = "ban-slot-" + slot; button.Padding = new Thickness(0); button.Margin = new Thickness(3); button.Content = content;
                ToolTip.SetTip(button, (op?.Name ?? L.Role(map.BanRole(slot))) + " · " + Session.Current.Team(MapRecord.BanTeam(slot)) + " · " + L.T("Round") + " " + (MapRecord.BanThreshold(slot) + 1));
                button.Height = Bounds.Height < 650 ? 62 : compact ? 66 : 108; button.MaxWidth = compact ? 52 : 82; button.Background = Ui.Team(MapRecord.BanTeam(slot), false); button.Foreground = Brushes.White;
                button.Template = new global::Avalonia.Controls.Templates.FuncControlTemplate<Button>((control, _) => new Border { Background = control.Background, BorderBrush = control.BorderBrush, BorderThickness = control.BorderThickness, Padding = control.Padding, Child = new global::Avalonia.Controls.Presenters.ContentPresenter { Content = control.Content, HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Center } });
                button.IsEnabled = map.BanStageReached(slot) && !map.Confirmed; button.BorderThickness = new Thickness(0, 0, 0, 3); button.BorderBrush = Ui.Team(MapRecord.BanTeam(slot)); grid.Children.Add(button);
            }
            Add(group, grid, 0, 1); Add(panel, group, 0, half);
        }
        return panel;
    }

    sealed record Choice(string Id, string Name) { public override string ToString() => Name; }
}

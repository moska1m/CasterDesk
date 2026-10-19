namespace CasterDesk.Avalonia;

public sealed partial class ChampionshipView
{
    internal Window? ActiveDialog { get; private set; }
    internal Window RemakeDialog()
    {
        var layout = new Grid { Margin = new Thickness(16), RowDefinitions = new("Auto,*,Auto") };
        var choice = Ui.Combo(); choice.Name = "remake-map";
        choice.ItemsSource = Session.Current.Maps.Select(m => new Choice(m.MapId, Session.Catalog.Maps.Single(c => c.Id == m.MapId).DisplayName)).ToArray();
        choice.SelectedIndex = Session.Map == null ? -1 : Session.Current.Maps.IndexOf(Session.Map);
        layout.Children.Add(choice);
        var text = new TextBox { Name = "remake-text", IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, FontSize = 15, Margin = new Thickness(0, 10), TextAlignment = TextAlignment.Center };
        Add(layout, text, 0, 1);
        void Update() { text.Text = choice.SelectedItem is Choice c ? Session.Summary(c.Id, SpecMode) : L.T("Escolha um mapa primeiro."); }
        choice.SelectionChanged += (_, _) => Update(); Update();
        var dialog = Ui.Dialog(this, "Resumo para remake", layout, 700, 560);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        buttons.Children.Add(Ui.Button("Copiar resumo", () => Ui.CopyText(text.Text ?? ""), true));
        buttons.Children.Add(Ui.Button("Fechar", () => dialog.Close())); Add(layout, buttons, 0, 2);
        Session.Changed += Update; dialog.Closed += (_, _) => Session.Changed -= Update;
        return dialog;
    }
    async Task ShowRemake() => await Show<bool>(RemakeDialog());
    async Task<T> Show<T>(Window dialog)
    {
        ActiveDialog = dialog;
        try { return await dialog.ShowDialog<T>(Ui.Owner(this)); }
        finally { if (ActiveDialog == dialog) ActiveDialog = null; }
    }
    async Task ChooseMap(int step)
    {
        if (!VetoReady || step >= 8) return;
        var match = Session.Current; var grid = new UniformGrid { Columns = 3, Margin = new Thickness(12) };
        var dialog = Ui.Dialog(this, "Escolher mapa", grid, 1000, 540);
        foreach (var map in Session.Catalog.Maps) {
            var button = Ui.Button("", async () => {
                try {
                    bool replace = false;
                    if (match.Maps.Any(m => m.HasPreservedData)) replace = await Ui.Ask(dialog, "Esta troca muda um mapa com bans, rounds ou estatísticas registrados. Limpar apenas os registros do mapa substituído?") == 1;
                    Session.Veto(step, map.Id, replace); dialog.Close(true);
                } catch (Exception ex) { notice.Text = ex.Message; }
            });
            bool available = !match.Veto.Where((_, i) => i != step).Contains(map.Id);
            button.Name = "map-choice-" + map.Id; button.Content = MapCard(map, "", available ? "" : "USED"); button.Margin = new Thickness(5);
            button.IsEnabled = available; grid.Children.Add(button);
        }
        await Show<bool>(dialog);
    }

    async Task OpenNextBan()
    {
        if (!BansReady || Session.Map?.Confirmed == true) return;
        var pending = Session.PendingBans(); if (pending.Length > 0) await PickBan(pending[0]);
    }

    internal Window OperatorDialog(int slot)
    {
        var map = Session.Map ?? throw new InvalidOperationException(L.T("Escolha um mapa primeiro."));
        var body = new Grid { Margin = new Thickness(12), RowDefinitions = new("Auto,*") };
        var search = new TextBox { PlaceholderText = L.T("Selecionar…"), Margin = new Thickness(0, 0, 0, 10) };
        body.Children.Add(search);
        var grid = new UniformGrid { Columns = 7 }; Add(body, grid, 0, 1);
        var dialog = Ui.Dialog(this, (SpecMode ? SpecTeam(MapRecord.BanTeam(slot)) : Session.Current.Team(MapRecord.BanTeam(slot))) + " · " + L.Role(map.BanRole(slot)), body, 820, 620);
        void Fill()
        {
            grid.Children.Clear();
            var noBan=Ui.Button("",()=>dialog.Close(MapRecord.NoBan(map.BanRole(slot))));noBan.Name="ban-none-choice";noBan.Margin=new Thickness(3);noBan.Padding=new Thickness(4);noBan.Height=72;
            noBan.Background=Brush.Parse("#334354");noBan.Foreground=Brushes.White;
            var noBanCard=new StackPanel();noBanCard.Children.Add(new Image{Source=Ui.FileImage(StudioRenderer.Asset(map.BanRole(slot)=="DEF"?"Icon_Defender.png":"Icon_Attacker.png"),thumbnail:96),Height=36,Stretch=Stretch.Uniform});
            noBanCard.Children.Add(new TextBlock { Text=L.T("Sem banimento"),FontSize=10,TextAlignment=TextAlignment.Center,TextWrapping=TextWrapping.Wrap });
            noBan.Content=noBanCard;ToolTip.SetTip(noBan,L.T("Sem banimento"));
            foreach (var op in OperatorOrder.Sort(Session.Catalog.Operators, map.BanRole(slot)).Where(o => o.Name.Contains(search.Text ?? "", StringComparison.OrdinalIgnoreCase))) {
                var button = Ui.Button("", () => dialog.Close(op.Id)); button.Name = "operator-" + op.Id; button.Margin = new Thickness(3); button.Padding = new Thickness(4);button.Height=72;
                button.Background=Brush.Parse("#334354");button.Foreground=Brushes.White;
                var card = new StackPanel();
                try { card.Children.Add(new Image { Source = Ui.FileImage(SiegeCatalog.Asset(op.Icon),thumbnail:96), Height = 36, Stretch = Stretch.Uniform }); } catch (Exception ex) { AppLog.Write(ex.ToString()); }
                card.Children.Add(new TextBlock { Text = op.Name, FontSize = 10, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap });
                button.Content = card; ToolTip.SetTip(button, op.Name);
                button.IsEnabled = !Enumerable.Range(slot / 6 * 6, 6).Any(i => i != slot && map.Bans[i] == op.Id); grid.Children.Add(button);
            }
            grid.Children.Add(noBan);
        }
        search.TextChanged += (_, _) => Fill(); Fill(); return dialog;
    }
    async Task PickBan(int slot)
    {
        string? id = Session.Map?.MapId;
        while (Session.Map?.MapId == id && id != null && BansReady) {
            string? chosen = await Show<string?>(OperatorDialog(slot));
            if (chosen == null) return;
            Session.Ban(slot, chosen);
            if (Session.Map?.Confirmed != false) return;
            var pending = Session.PendingBans(); if (pending.Length == 0) return; slot = pending[0];
        }
    }

    internal Window RoundEditor(int index)
    {
        var map = Session.Map!;
        if (map.Confirmed) throw new InvalidOperationException(L.T("Mapa confirmado. Abra uma revisão para corrigir os dados."));
        var panel = new StackPanel { Margin = new Thickness(18) };
        var sites = Ui.Combo(); sites.ItemsSource = Session.Catalog.Maps.Single(m => m.Id == map.MapId).Sites.Select(s => new Choice(s, BombsiteNames.Display(map.MapId, s))).ToArray();
        sites.SelectedItem = sites.Items.OfType<Choice>().FirstOrDefault(c => c.Id == map.Rounds[index].Site);
        var teams = Ui.RawCombo(Session.Current.TeamA, Session.Current.TeamB); teams.SelectedIndex = map.Rounds[index].Winner;
        var ends = Ui.Combo(); ends.ItemsSource = RoundEnding.Codes.Select(c => new Choice(c, RoundEnding.Label(c))).ToArray(); ends.SelectedItem = ends.Items.OfType<Choice>().FirstOrDefault(c => c.Id == map.Rounds[index].Ending);
        panel.Children.Add(Ui.Text("Bomb site")); panel.Children.Add(sites); panel.Children.Add(Ui.Text("Vencedor")); panel.Children.Add(teams); panel.Children.Add(Ui.Text("Finalização")); panel.Children.Add(ends);
        var trim = new CheckBox { Content = L.T("Permitir remover rounds posteriores se o mapa terminar antes"), Margin = new Thickness(0, 8), Name = "review-trim" }; panel.Children.Add(trim);
        var error = Ui.Text(""); panel.Children.Add(error);
        var dialog = Ui.Dialog(this, "Corrigir round", panel, 600, 460);
        var save = Ui.Button("Confirmar", () => {
            try { Session.Round((sites.SelectedItem as Choice)?.Id ?? "", teams.SelectedIndex, (ends.SelectedItem as Choice)?.Id ?? "", index, trim.IsChecked == true); dialog.Close(true); }
            catch (Exception ex) { error.Text = ex.Message; }
        }, true); save.Name = "review-confirm"; panel.Children.Add(save); return dialog;
    }
    async Task EditRound(int index) => await Show<bool>(RoundEditor(index));
    async Task Review()
    {
        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Children.Add(Ui.Text("Informe o motivo da revisão. O resultado anterior será preservado no histórico da partida."));
        var reason = new TextBox { Name = "review-reason" }; panel.Children.Add(reason); var error = Ui.Text(""); panel.Children.Add(error);
        var dialog = Ui.Dialog(this, "Revisar resultado", panel, 600, 300);
        panel.Children.Add(Ui.Button("Abrir revisão", () => { try { Session.Review(reason.Text ?? ""); dialog.Close(true); } catch (Exception ex) { error.Text = ex.Message; } }, true));
        await Show<bool>(dialog);
    }

    async Task Logo(int team)
    {
        var match = Session.Current; string? path = await Ui.PickImage(this); if (path == null || match.Id != Session.Current.Id) return;
        var brand = MatchFiles.ReadBrand(match);
        string resource = await Task.Run(() => MatchFiles.Resource(match, team == 0 ? "logo-a" : "logo-b", path));
        if (team == 0) brand.TeamALogo = resource; else brand.TeamBLogo = resource;
        MatchFiles.SaveBrand(match, brand); Refresh();
    }

    async Task FileOperation(bool export, bool package)
    {
        string extension = package ? "casterdesk" : "json";
        string? file = await Ui.PickFile(this, package ? "Pacote CasterDesk" : "Partida JSON", extension, export, MatchStore.ExportFileName(Session.Current).Replace(".json", "." + extension));
        if (file == null) return;
        Processing = true; IsEnabled = false; notice.Text = L.T("Processando pacote da partida…");
        try {
            var match = Session.Current; var existing = Session.Matches.ToArray();
            if (export) await Task.Run(() => { if (package) MatchPackage.Export(match, file); else MatchStore.Atomic(file, match); });
            else if (package) { var imported = await Task.Run(() => MatchPackage.Import(file, Session.Catalog, existing)); Session.AttachImported(imported); }
            else { var imported = await Task.Run(() => MatchStore.Read(file, Session.Catalog)); if (MatchStore.IsDuplicate(existing, imported, MatchStore.Folder)) throw new InvalidOperationException(L.T("Esta partida já existe no programa.")); await Task.Run(() => MatchStore.Save(imported)); Session.AttachImported(imported); }
        } finally { Processing = false; IsEnabled = true; }
    }
    async Task Files()
    {
        var panel = new StackPanel { Margin = new Thickness(18) }; var dialog = Ui.Dialog(this, "Arquivos da partida", panel, 480, 330);
        foreach (var choice in new[] { (false, false, "Importar partida JSON"), (true, false, "Exportar partida JSON"), (false, true, "Importar pacote completo"), (true, true, "Exportar pacote completo") })
            panel.Children.Add(Ui.Button(choice.Item3, () => dialog.Close((choice.Item1, choice.Item2))));
        var selected = await Show<(bool Export, bool Package)?>(dialog); if (selected.HasValue) await FileOperation(selected.Value.Export, selected.Value.Package);
    }
}

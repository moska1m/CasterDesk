using Avalonia.Input;
using Avalonia.VisualTree;

namespace CasterDesk.Avalonia;

public sealed partial class ChampionshipView
{
    ScrollViewer? specScroll;
    string specHistoryKey=""; int specHistoryCount;
    int specEditing = -1, specEditWinner;
    string specEditSite = "", specDraftKey = "", specFormat = "MD1";
    string[] specMaps = [];
    string SpecTeam(int team) => team == 0 && Session.Current.TeamAIsDefault ? L.T("Equipe Azul") : team == 1 && Session.Current.TeamBIsDefault ? L.T("Equipe Laranja") : Session.Current.Team(team);
    bool SpecConfigurationReady => specFormat == Session.Current.Format && specMaps.SequenceEqual(Session.Current.Maps.Select(m=>m.MapId));
    Control SpecMatch()
    {
        var match = Session.Current; var map = Session.Map;
        string draftKey = match.Id + ":" + match.Format + ":" + string.Join(",", match.Maps.Select(m => m.MapId));
        if (draftKey != specDraftKey) { specDraftKey = draftKey; specFormat = match.Format; specMaps = new string[match.Format == "MD5" ? 5 : match.Format == "MD3" ? 3 : 1]; for (int i = 0; i < Math.Min(specMaps.Length, match.Maps.Count); i++) specMaps[i] = match.Maps[i].MapId; specEditing = -1; }
        bool wide = Bounds.Width >= 1500;
        var toolbar = new StackPanel { Name = "spec-match-toolbar", Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var create = Action("Nova partida", Session.New, true); create.Name = "new-match"; create.IsVisible = wide; create.VerticalAlignment = VerticalAlignment.Center; create.MinHeight = 0; create.Height = 32; create.Margin = new Thickness(0); toolbar.Children.Add(create);
        var selected = Ui.Combo(); selected.Name = "match-selector"; selected.IsVisible = wide; selected.VerticalAlignment = VerticalAlignment.Center; selected.Width = Math.Min(370, Math.Max(250, Bounds.Width * .25)); selected.Height = 32; selected.MinHeight=0; selected.Margin=new Thickness(0); selected.ItemsSource = Session.Matches.Select(m=>new Choice(m.Id,$"{m.Created:dd/MM HH:mm} · {(m.TeamAIsDefault?L.T("Equipe Azul"):m.TeamA)} × {(m.TeamBIsDefault?L.T("Equipe Laranja"):m.TeamB)} · {L.T(m.Complete?"Concluída":"Aberta")}")).ToArray(); selected.SelectedItem = selected.Items.OfType<Choice>().First(c=>c.Id==match.Id);
        selected.SelectionChanged += (_, _) => { if (!building && selected.SelectedItem is Choice m && m.Id != Session.Current.Id) _ = Run(() => { Session.Select(m.Id); return Task.CompletedTask; }); };
        toolbar.Children.Add(selected); foreach(bool export in new[]{false,true}){bool operation=export;var b=Action(export?"Exportar":"Importar",()=>FileOperation(operation,false));b.IsVisible=wide;b.VerticalAlignment=VerticalAlignment.Center;b.MinHeight=0;b.Height=32;b.Margin=new Thickness(3,0,0,0);toolbar.Children.Add(b);} matchToolbar.Content = toolbar;
        var root = new Grid { Name = "spec-layout", RowDefinitions = new("Auto,*"), ColumnDefinitions = new("40*,60*") };
        var top = Columns("*,110,*"); top.MaxWidth=840;top.HorizontalAlignment=HorizontalAlignment.Stretch; top.Margin = new Thickness(10, 0);
        for (int t = 0; t < 2; t++) {
            int team = t;
            var input = new TextBox { Name = "spec-team-" + t, Text = SpecTeam(t), IsReadOnly = true, Height=36, MinWidth=0, VerticalContentAlignment=VerticalAlignment.Center, FontSize = wide ? 20 : 16, FontWeight = FontWeight.Bold, Foreground = Ui.Team(t), TextAlignment = TextAlignment.Center, MaxLength = 80, Margin = new Thickness(0) };
            ToolTip.SetTip(input, L.T("Clique no nome para editar."));
            input.AddHandler(PointerPressedEvent, (_, _) => { if (!match.HasConfirmed) { namesDirty = true; namesTimer.Stop(); input.IsReadOnly = false; input.SelectAll(); } }, global::Avalonia.Interactivity.RoutingStrategies.Tunnel, handledEventsToo: true);
            input.TextChanged += (_, _) => { if (!building && !input.IsReadOnly && input.Text != SpecTeam(team)) { namesDirty = true; } };
            input.LostFocus += (_, _) => { input.IsReadOnly = true; _ = Run(() => { FlushNames(); return Task.CompletedTask; }); };
            input.KeyDown += (_, e) => { if (e.Key == Key.Enter) { input.IsReadOnly = true; _=Run(()=>{FlushNames();return Task.CompletedTask;}); e.Handled = true; } };
            if (t == 0) teamA = input; else teamB = input; Add(top, input, t == 0 ? 0 : 2);
        }
        var score=map?.Score(); var versus=Ui.RawText($"{score?.A ?? 0} VS {score?.B ?? 0}");versus.Name="spec-score";versus.FontSize=22;versus.FontWeight=FontWeight.Bold;versus.TextAlignment=TextAlignment.Center;versus.VerticalAlignment=VerticalAlignment.Center;versus.Margin=new Thickness(0);Add(top,versus,1); specMatchHeader.Content=top;
        var setup = new Grid { ColumnDefinitions = new("90,1.1*,1.1*,1.3*,1.3*"), Margin = new Thickness(4, 0, 4, 8) };
        var format = Ui.RawCombo(L.FormatName("MD1"), L.FormatName("MD3"), L.FormatName("MD5")); format.Name = "spec-format"; format.Width = 90; format.SelectedIndex = specFormat == "MD5" ? 2 : specFormat == "MD3" ? 1 : 0;
        format.SelectionChanged += (_, _) => { if (building) return; string f = format.SelectedIndex == 2 ? "MD5" : format.SelectedIndex == 1 ? "MD3" : "MD1"; if (match.Maps.Any(m => m.HasData || m.Confirmed)) { notice.Text = L.T("Esta troca muda um mapa com bans ou rounds registrados."); Refresh(); return; } specFormat = f; specMaps = new string[f == "MD5" ? 5 : f == "MD3" ? 3 : 1]; Refresh(); };
        Add(setup, SpecField("Formato:", format), 0);
        var mapFields = new UniformGrid { Columns = specMaps.Length };
        for (int i = 0; i < specMaps.Length; i++) {
            int slot = i; var choice = Ui.Combo(); choice.Name = "spec-map-" + i;
            choice.ItemsSource = Session.Catalog.Maps.Select(m => new Choice(m.Id, m.DisplayName)).ToArray(); choice.SelectedItem = choice.Items.OfType<Choice>().FirstOrDefault(c => c.Id == specMaps[i]);
            choice.SelectionChanged += (_, _) => { if (building || choice.SelectedItem is not Choice c) return; _ = Run(() => { var previous = specMaps[slot]; specMaps[slot] = c.Id; try { if (specMaps.Distinct().Count() != specMaps.Length && specMaps.Count(x => !string.IsNullOrEmpty(x)) == specMaps.Length) throw new InvalidOperationException(L.T("Mapas repetidos ou desconhecidos.")); if (specMaps.All(x => !string.IsNullOrEmpty(x))) { if (!match.Maps.Select(m => m.MapId).SequenceEqual(specMaps) || match.Format != specFormat) Session.ConfigureSpec(specFormat, specMaps); } if(specMaps.All(x=>!string.IsNullOrEmpty(x))) Refresh(); } catch { specMaps[slot] = previous; throw; } return Task.CompletedTask; }); };
            mapFields.Children.Add(SpecField(L.T("Mapa") + " " + (i + 1), choice));
        }
        if(specMaps.Length==1){Add(setup,mapFields,1);SetColumnSpan(mapFields,2);}
        else {
            mapFields.Columns=1;mapFields.Width=320;
            var choose=Ui.Button(L.T("Selecionar mapas")+" ("+specMaps.Length+")",()=>{});choose.Name="spec-series-maps";choose.Height=32;choose.MinHeight=0;choose.Margin=new Thickness(0);choose.Flyout=new Flyout{Content=mapFields};Add(setup,SpecField("Mapas da série",choose),1);
            var active=Ui.Combo();active.Name="spec-active-map";active.ItemsSource=match.Maps.Select(m=>new Choice(m.MapId,Session.Catalog.Maps.Single(c=>c.Id==m.MapId).DisplayName)).ToArray();active.SelectedItem=active.Items.OfType<Choice>().FirstOrDefault(c=>c.Id==map?.MapId);active.IsEnabled=match.Maps.Count>0;
            active.SelectionChanged+=(_,_)=>{if(!building&&active.SelectedItem is Choice c)_=Run(()=>{Session.SelectMap(c.Id);return Task.CompletedTask;});};Add(setup,SpecField("Mapa ativo",active),2);
        }
        foreach (bool ot in new[] { false, true }) {
            var side = Ui.RawCombo(L.T("Selecionar…"), SpecTeam(0) + " · " + L.Role("ATQ"), SpecTeam(1) + " · " + L.Role("ATQ")); side.Name = ot ? "spec-ot" : "spec-side"; side.SelectedIndex = map == null ? 0 : (ot ? map.OtAttack : map.InitialAttack) + 1; side.IsEnabled = map != null && !map.Confirmed;
            side.SelectionChanged += (_, _) => { if (!building && map != null && side.SelectedIndex >= 0) _ = Run(() => { Session.SpecSide(map.MapId, ot, side.SelectedIndex - 1); return Task.CompletedTask; }); };
            var field = SpecField(ot ? "Ataque na prorrogação" : "Ataque inicial", side);
            if (!ot && map?.InitialAttack >= 0) field.Children.Add(Ui.RawText(L.T("Defesa inicial") + ": " + SpecTeam(1 - map.InitialAttack)));
            Add(setup,field,ot?4:3);
        }

        Add(root, setup, 0, 0); SetColumnSpan(setup, 2);
        var left = new Grid { RowDefinitions = new("Auto,Auto,Auto"), VerticalAlignment=VerticalAlignment.Top };
        var entry = Ui.Card("Registrar round", Rounds()); entry.Name = "spec-round-entry"; left.Children.Add(entry);
        if (map != null) {
            var round = Ui.RawText($"{L.T("Round")} {map.Rounds.Count + 1:00} · {(map.InitialAttack < 0 ? L.T("Defina os lados antes de escolher os operadores.") : SpecTeam(map.Attacker(map.Rounds.Count + 1)) + " " + L.Role("ATQ") + " / " + SpecTeam(1-map.Attacker(map.Rounds.Count + 1)) + " " + L.Role("DEF"))}");
            var title=((Grid)entry.Child!).Children.OfType<TextBlock>().First();title.Text=(L.T("Registrar round")+" · "+round.Text).ToUpperInvariant();title.FontSize=12;title.TextWrapping=TextWrapping.Wrap;
        }
        Add(left, Ui.Card("Bans de operadores", SpecBans()), 0, 1);
        var tools = new WrapPanel(); var remake = Action("Resumo para remake", ShowRemake); remake.Name = "remake-summary"; tools.Children.Add(remake); tools.Children.Add(Action("Arquivos da partida", Files)); tools.Children.Add(Action("Salvar agora", () => MatchStore.Save(Session.Current))); Add(left, tools, 0, 2); Add(root, new ScrollViewer { Name="spec-left-scroll",Content=left,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled }, 0, 1);
        var right = new Grid { RowDefinitions = new("*,Auto") };
        var history=Ui.Card("Histórico de rounds", SpecHistory()); history.Name="spec-history-card"; Add(right, history, 0, 0);
        var confirm = Action("Confirmar vencedor", async () => { if (await Ui.Ask(this, "Confirmar resultado e bloquear os rounds deste mapa?", winnerConfirmation: true) == 1) Session.Confirm(); }, true); confirm.Name = "confirm-winner"; confirm.IsEnabled = map != null && !map.Confirmed && map.Score().Winner >= 0; Add(right, confirm, 0, 1); Add(root, right, 1, 1);
        return root;
    }
    static StackPanel SpecField(string title, Control control) { var field = new StackPanel { Margin = new Thickness(3) }; var text = Ui.Text(title); text.FontSize = 12; field.Children.Add(text); field.Children.Add(control); return field; }
    Control SpecBans()
    {
        var map = Session.Map; if (map == null || map.InitialAttack < 0) return Ui.Text("Defina os lados antes de escolher os operadores.");
        var rows = new StackPanel();
        for (int t = 0; t < 2; t++) {
            var row = new Grid { ColumnDefinitions = new("100,*"), Margin = new Thickness(0, 3) }; var label = Ui.RawText(SpecTeam(t)); label.Foreground = Ui.Team(t); label.FontSize = 12; row.Children.Add(label);
            var cards = new UniformGrid { Columns = 6 };
            foreach (int slot in Enumerable.Range(0, 12).Where(i => MapRecord.BanTeam(i) == t)) {
                int field = slot; var op = Session.Catalog.Operators.FirstOrDefault(o => o.Id == map.Bans[slot]);
                var content = new StackPanel(); var file = op == null ? StudioRenderer.Asset(map.BanRole(slot) == "DEF" ? "Icon_Defender.png" : "Icon_Attacker.png") : SiegeCatalog.Asset(op.Icon);
                content.Children.Add(new Image { Source = Ui.FileImage(file), Height = 26, Stretch = Stretch.Uniform }); content.Children.Add(new TextBlock { Text = op?.Name ?? L.Role(map.BanRole(slot)), FontSize = 10, TextAlignment = TextAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis });
                var card = Action("", () => PickBan(field)); card.Content = content; card.Height = 64; card.Margin = new Thickness(2); card.Background = Ui.Team(t); card.IsEnabled = map.BanStageReached(slot) && !map.Confirmed; cards.Children.Add(card);
            }
            Add(row, cards, 1); rows.Children.Add(row);
        }
        return rows;
    }
    Control SpecHistory()
    {
        var root = new Grid { RowDefinitions = new("Auto,*") }; var controls = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var undo = Action("Desfazer", () => { specEditing = -1; Session.Undo(); }); undo.Name = "spec-undo"; undo.IsEnabled = Session.CanUndo; var redo = Action("Refazer", () => { specEditing = -1; Session.Redo(); }); redo.Name = "spec-redo"; redo.IsEnabled = Session.CanRedo; controls.Children.Add(undo); controls.Children.Add(redo); root.Children.Add(controls);
        var panel = new StackPanel { Spacing = 3 }; var map = Session.Map;
        if (map == null) panel.Children.Add(Ui.Text("Escolha um mapa primeiro."));
        else {
            var headings=Columns("48,1.2*,2*,60,38");string[] labels=["Round","Vencedor","Bomb site","Placar",""];for(int i=0;i<labels.Length;i++){var text=Ui.Text(labels[i]);text.FontSize=11;text.FontWeight=FontWeight.Bold;text.Margin=new Thickness(4,2);Add(headings,text,i);}panel.Children.Add(headings);
            int a = 0, b = 0;
            for (int i = 0; i < map.Rounds.Count; i++) {
                int row = i; var r = map.Rounds[i]; if (r.Winner == 0) a++; else b++;
                var line = Columns("48,1.2*,2*,60,38"); line.Margin = new Thickness(4, 1); Add(line, Ui.RawText($"{i+1:00}"), 0); Add(line, Ui.RawText(SpecTeam(r.Winner)), 1); Add(line, Ui.RawText(ShortSite(map.MapId, r.Site)), 2); Add(line, Ui.RawText($"{a}–{b}"), 3);
                var edit = Action("✎", () => { specEditing = row; specEditWinner = r.Winner; specEditSite = r.Site; Refresh(); }); edit.Name = "spec-edit-" + i; edit.IsEnabled = !map.Confirmed; ToolTip.SetTip(edit, L.T("Corrigir round")); edit.Height=30; edit.MinHeight=0; edit.Margin=new Thickness(0); edit.Padding=new Thickness(4); Add(line, edit, 4);
                foreach(var text in line.Children.OfType<TextBlock>()){text.Margin=new Thickness(4,0);text.FontSize=12;text.VerticalAlignment=VerticalAlignment.Center;text.TextWrapping=TextWrapping.NoWrap;text.TextTrimming=TextTrimming.CharacterEllipsis;ToolTip.SetTip(text,text.Text);}
                panel.Children.Add(new Border { Name = "spec-history-" + i, Child = line, Background = Ui.Panel, BorderBrush = specEditing == i ? Brushes.White : Ui.Team(r.Winner), BorderThickness = specEditing == i ? new Thickness(1) : new Thickness(3,0,0,0) });
                if (specEditing == i) panel.Children.Add(SpecEditor(row));
            }
        }
        string key=Session.Current.Id+":"+map?.MapId;bool follow=key!=specHistoryKey||(map?.Rounds.Count??0)>specHistoryCount;var offset=specScroll?.Offset??default;specHistoryKey=key;specHistoryCount=map?.Rounds.Count??0;
        var scroll = new ScrollViewer { Name = "spec-history-scroll", Content = panel };specScroll=scroll;
        void Restore(object? sender,EventArgs args){if(scroll.Viewport.Height<=0)return;scroll.LayoutUpdated-=Restore;if(specScroll!=scroll)return;if(follow&&specEditing<0)scroll.ScrollToEnd();else scroll.Offset=offset;}scroll.LayoutUpdated+=Restore;
        Add(root, scroll, 0, 1); return root;
    }
    Control SpecEditor(int index)
    {
        var panel = new StackPanel { Name = "spec-inline-editor", Margin = new Thickness(6) }; var teams = new UniformGrid { Columns = 2 };
        for (int i = 0; i < 2; i++) { int team = i; var b = Action(SpecTeam(i), () => { specEditWinner = team; Refresh(); }); b.Content=SpecTeam(i); b.Name = "spec-edit-winner-" + i; b.Background = Ui.Team(i); b.BorderBrush = Brushes.White; b.BorderThickness = new Thickness(specEditWinner == i ? 2 : 0); teams.Children.Add(b); } panel.Children.Add(teams);
        var sites = new UniformGrid { Columns = 2 }; foreach (var s in Session.Catalog.Maps.Single(m => m.Id == Session.Map!.MapId).Sites) { string id = s; var b = Action(ShortSite(Session.Map!.MapId, s), () => { specEditSite = id; Refresh(); }, specEditSite == s); b.Name = "spec-edit-site-" + Array.IndexOf(Session.Catalog.Maps.Single(m => m.Id == Session.Map!.MapId).Sites, s); sites.Children.Add(b); } panel.Children.Add(sites);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        var save = Action("Salvar", () => { Session.CorrectRound(index, specEditSite, specEditWinner); specEditing = -1; Refresh(); }, true); save.Name = "spec-edit-save"; actions.Children.Add(save); actions.Children.Add(Action("Cancelar", () => { specEditing = -1; Refresh(); }));
        var remove = Action("Excluir round", async () => { if (await Ui.Ask(this, "Excluir este round? Os seguintes serão renumerados e os placares recalculados.") == 1) { Session.DeleteRound(index); specEditing = -1; Refresh(); } }); remove.Name = "spec-edit-delete"; actions.Children.Add(remove); panel.Children.Add(actions);
        return new Border { Background = Ui.Header, Child = panel };
    }
    internal void SpecShortcut(KeyEventArgs e)
    {
        if (!SpecMode || lobby || ActiveDialog != null || Ui.ActiveQuestion != null || !e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;
        if (e.Source is Control control && (control is TextBox || control.GetVisualAncestors().OfType<TextBox>().Any())) return;
        if (e.Key != Key.Z && e.Key != Key.Y) return;
        e.Handled = true; _ = Run(() => { specEditing = -1; if (e.Key == Key.Z) Session.Undo(); else Session.Redo(); return Task.CompletedTask; });
    }
}

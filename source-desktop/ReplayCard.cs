namespace CasterDesk.Desktop;

internal sealed class ReplayCard : ToggleButton
{
    public string Folder { get; }
    public ReplayDocument? Header { get; private set; }
    public int Group { get; set; }
    readonly Border frame;
    readonly TextBlock map, teams, detail, relation, check;
    public ReplayCard(string folder, int count)
    {
        Folder = folder; Name = "replay-card"; Height = 68; Margin = new Thickness(4); Padding = new Thickness(0);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        var body = new Grid { RowDefinitions = new("Auto,*,Auto") };
        var heading = new Grid { ColumnDefinitions = new("*,18") };
        var file = Label(Path.GetFileName(folder), 10); file.TextWrapping = TextWrapping.NoWrap; file.TextTrimming = TextTrimming.CharacterEllipsis; file.Opacity = .75;
        check = Label("○", 13); heading.Children.Add(file); heading.Children.Add(check); Grid.SetColumn(check, 1); body.Children.Add(heading);
        map = Label("Identificando…", 12, true); map.Name = "replay-folder-title"; map.TextWrapping=TextWrapping.NoWrap;map.TextTrimming=TextTrimming.CharacterEllipsis;map.VerticalAlignment=VerticalAlignment.Center;body.Children.Add(map); Grid.SetRow(map, 1);
        teams = Label("", 11);
        var bottom=new Grid { ColumnDefinitions=new("Auto,*") };
        detail = Label(Count(count), 9); detail.Opacity = .8; bottom.Children.Add(detail);
        relation = Label("", 9, true); relation.Foreground = Brush.Parse("#78E6AF");relation.TextWrapping=TextWrapping.NoWrap;relation.TextTrimming=TextTrimming.CharacterEllipsis;relation.TextAlignment=TextAlignment.Right;bottom.Children.Add(relation);Grid.SetColumn(relation,1);body.Children.Add(bottom); Grid.SetRow(bottom, 2);
        frame = new Border { Child = body, Padding = new Thickness(8, 6), CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(1) };
        Content = frame; Template = new global::Avalonia.Controls.Templates.FuncControlTemplate<ToggleButton>((control, _) => new global::Avalonia.Controls.Presenters.ContentPresenter { Content = control.Content });
        IsCheckedChanged += (_, _) => Paint(); Paint(); ToolTip.SetTip(this, folder);
    }
    static TextBlock Label(string value, double size, bool bold = false) => new() { Text = value, FontSize = size, FontWeight = bold ? FontWeight.Bold : FontWeight.Normal, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.White, Margin = new Thickness(0, 1) };
    static string Count(int count)=>count+ (count==1?" arquivo":" arquivos");
    public void Identify(ReplayFolderPreview result, SiegeCatalog catalog, int count)
    {
        Header = result.Header;
        if (Header is {} h) {
            string mapName = catalog.Maps.FirstOrDefault(m => m.Id == ReplayImport.MapId(h.Map.Name))?.DisplayName ?? h.Map.Name;
            teams.Text = string.Join(" × ", h.Teams.Select(t => string.IsNullOrWhiteSpace(t.Name) ? "Equipe não informada" : t.Name.Trim()));
            map.Text = mapName + " — " + teams.Text;
            detail.Text = h.Timestamp.ToLocalTime().ToString("dd/MM HH:mm");
        } else { map.Text = "Não identificado"; teams.Text = result.Error; }
        global::Avalonia.Automation.AutomationProperties.SetName(this, Path.GetFileName(Folder) + " · " + map.Text + (Header==null?" · "+teams.Text:""));
        ToolTip.SetTip(this, Folder + Environment.NewLine + map.Text + (Header==null?" · "+teams.Text:"") + Environment.NewLine + detail.Text+" · "+Count(count));
    }
    public void SetRelation(int group, bool sameSession)
    {
        Group = group; relation.Text = group == 0 ? "" : $"GRUPO {group:00} · " + (sameSession ? "MESMA SESSÃO" : "POSSÍVEL CONTINUAÇÃO");
        Paint();
    }
    void Paint()
    {
        bool selected = IsChecked == true;
        frame.Background = Brush.Parse(selected ? "#264F73" : Group > 0 ? "#203D36" : "#233448");
        frame.BorderBrush = Brush.Parse(selected ? "#7CC9FF" : Group > 0 ? "#4EBA85" : "#526479");
        frame.BorderThickness = new Thickness(selected ? 2 : 1); check.Text = selected ? "✓" : "○";
    }
}

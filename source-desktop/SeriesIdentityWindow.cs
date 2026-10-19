namespace CasterDesk.Desktop;

// Explicit cross-map identity review. The working copy is never committed by opening/cancelling.
internal sealed class SeriesIdentityWindow : Window
{
    readonly SiegeMatch match, draft;
    readonly SiegeCatalog catalog;
    readonly List<(PlayerStatistic Player, ComboBox Choice)> rows=[];
    readonly TextBlock status=Ui.Text("");
    internal SeriesIdentityWindow(SiegeMatch match,SiegeCatalog catalog)
    {
        this.match=match;this.catalog=catalog;
        draft=System.Text.Json.JsonSerializer.Deserialize<SiegeMatch>(System.Text.Json.JsonSerializer.Serialize(match))!;
        Title=L.T("Conciliar jogadores da série");Width=850;Height=740;MinWidth=650;Background=Ui.Canvas;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var root=new Grid{Margin=new Thickness(18),RowDefinitions=new("Auto,*,Auto")};Content=root;
        root.Children.Add(Ui.Text("Há nomes incompatíveis entre os mapas. Selecione o mesmo jogador para as variações de nome. Nada será mesclado automaticamente."));
        var list=new StackPanel{Spacing=4};var scroll=new ScrollViewer{Content=list};root.Children.Add(scroll);Grid.SetRow(scroll,1);
        foreach(var map in draft.Maps.Where(m=>m.Statistics?.ConfirmedAt!=null)) {
            list.Children.Add(Ui.RawText(catalog.Maps.Single(m=>m.Id==map.MapId).DisplayName,true));
            foreach(var p in map.Statistics!.Players) {
                var grid=new Grid{ColumnDefinitions=new("*,*")};grid.Children.Add(Ui.RawText(match.Team(p.Team)+" · "+p.Name));
                var known=draft.Maps.SelectMany(m=>m.Statistics?.Players??[]).Where(k=>k.Team==p.Team)
                    .GroupBy(k=>IdentityKey(k)).Select(g=>new Identity(g.Key,g.First().Name)).ToArray();
                var box=Ui.Combo();box.ItemsSource=known;box.SelectedItem=known.First(k=>k.Id==IdentityKey(p));grid.Children.Add(box);Grid.SetColumn(box,1);list.Children.Add(grid);rows.Add((p,box));
            }
        }
        var footer=new StackPanel();footer.Children.Add(status);var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};
        var save=Ui.Button("Salvar conciliação",Save,true);save.Name="series-reconcile-save";buttons.Children.Add(save);buttons.Children.Add(Ui.Button("Cancelar",()=>Close(false)));footer.Children.Add(buttons);root.Children.Add(footer);Grid.SetRow(footer,2);
    }
    static string IdentityKey(PlayerStatistic p)=>p.PlayerId!=""?p.PlayerId:"name:"+p.Name.Trim().ToUpperInvariant();
    void Save()
    {
        try {
            foreach(var (player,box) in rows) {
                var id=(Identity)box.SelectedItem!;player.PlayerId=id.Id.StartsWith("name:")?"":id.Id;player.Name=id.Name;
            }
            foreach(var map in draft.Maps)if(map.Statistics!=null)StatisticsData.Validate(map.Statistics);
            string issue=StatisticsData.SeriesIssue(draft);if(issue!="")throw new InvalidDataException(issue);
            MatchStore.Validate(draft,catalog);
            lock(FileBatch.Gate) {
                using var batch=new FileBatch();
                batch.Text(Path.Combine(MatchFiles.Ensure(match),"Estatisticas","antes-conciliacao-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")+".json"),System.Text.Json.JsonSerializer.Serialize(match,MatchStore.Options));
                MatchStore.Stage(batch,draft);batch.Commit();
            }
            for(int i=0;i<match.Maps.Count;i++)match.Maps[i].Statistics=draft.Maps[i].Statistics;
            Close(true);
        }catch(Exception ex){status.Text=ex.Message;}
    }
    sealed record Identity(string Id,string Name){public override string ToString()=>Name;}
}

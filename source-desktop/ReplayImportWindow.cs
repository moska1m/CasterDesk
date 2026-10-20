using Avalonia.Platform.Storage;

namespace CasterDesk.Desktop;

public sealed record ReplaySelection(ReplayImport Import, MapStatistics Statistics);

public sealed class ReplayImportWindow : Window
{
    readonly SiegeMatch match;
    readonly MapRecord map;
    readonly TextBox path = new() { PlaceholderText = "Pasta MatchReplay do Siege" };
    readonly UniformGrid folders = new() { Columns = 3, VerticalAlignment = VerticalAlignment.Top };
    readonly StackPanel sessions = new() { Spacing = 6 };
    readonly TabControl steps = new();
    readonly TextBlock pageLabel = new();
    readonly Button previousPage, nextPage, related;
    readonly TextBox report = new() { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
    readonly TextBlock state = Ui.Text("Selecione somente as pastas da partida. Nenhum arquivo será enviado pela internet.");
    readonly List<ReplayCard> choices = [];
    int page;
    readonly List<ComboBox> transitions = [];
    readonly CancellationTokenSource cancellation = new();
    CancellationTokenSource? scanCancellation;
    readonly Button load, apply;
    ReplayImport? preview;
    MapStatistics? data;
    bool busy, closed;
    internal Task Listing { get; private set; } = Task.CompletedTask;
    public ReplayImportWindow(SiegeMatch match, MapRecord map, string? directory = null)
    {
        this.match = match; this.map = map;
        Title = "Importar replays · experimental"; Width = 1060; Height = 620; MinWidth = 800; MinHeight = 500;
        Background = Ui.Canvas; FontFamily = new FontFamily("Segoe UI"); WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var grid = new Grid { Margin = new Thickness(12), RowDefinitions = new("Auto,Auto,*,Auto") };
        Content = grid;
        var header = new StackPanel { Spacing = 5 };
        header.Children.Add(Ui.Text("Marque os replays do mapa. Cards verdes indicam trechos relacionados; confira antes de somar."));
        var location = new Grid { ColumnDefinitions = new("*,Auto,Auto") }; location.Children.Add(path);
        var browse = Ui.Button("Procurar", () => _ = Browse()); location.Children.Add(browse); Grid.SetColumn(browse, 1);
        var scan = Ui.Button("Listar pastas", () => Listing = Scan()); location.Children.Add(scan); Grid.SetColumn(scan, 2);
        header.Children.Add(location); grid.Children.Add(header);
        grid.Children.Add(state); Grid.SetRow(state, 1);
        var selection = new Grid { RowDefinitions = new("*,Auto") };
        selection.Children.Add(new ScrollViewer { Content = folders, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        var paging = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center };
        previousPage = Ui.Button("Anterior", () => { page--; ShowPage(); }); nextPage = Ui.Button("Próxima", () => { page++; ShowPage(); });
        related = Ui.Button("Selecionar relacionados", () => { var groups = choices.Where(c => c.IsChecked == true && c.Group > 0).Select(c => c.Group).ToHashSet(); foreach (var c in choices.Where(c => groups.Contains(c.Group))) c.IsChecked = true; });
        related.Name = "replay-select-related"; related.IsEnabled = false;
        paging.Children.Add(previousPage); paging.Children.Add(pageLabel); paging.Children.Add(nextPage); paging.Children.Add(related);
        selection.Children.Add(paging); Grid.SetRow(paging, 1);
        var right = new Grid { RowDefinitions = new("Auto,*"), Margin = new Thickness(0,8,0,8) };
        right.Children.Add(sessions); right.Children.Add(report); Grid.SetRow(report,1);
        steps.Items.Add(new TabItem { Header = "REPLAYS", Content = selection, FontSize=14 }); steps.Items.Add(new TabItem { Header = "CONFERÊNCIA", Content = right, FontSize=14 });
        grid.Children.Add(steps); Grid.SetRow(steps, 2);
        var actions = new WrapPanel();
        load = Ui.Button("Ler selecionados", () => _ = Load()); actions.Children.Add(load);
        apply = Ui.Button("Aplicar à conferência", Apply, true); apply.IsEnabled = false; actions.Children.Add(apply);
        actions.Children.Add(Ui.Button("Cancelar", () => Close())); grid.Children.Add(actions); Grid.SetRow(actions,3);
        SizeChanged += (_, _) => folders.Columns = Bounds.Width < 960 ? 2 : 3;
        path.Text = directory ?? ReplayImport.DefaultDirectory(); Listing = Scan();
        Closed += (_, _) => { closed = true; cancellation.Cancel(); if (!busy) { preview?.Dispose(); cancellation.Dispose(); } };
    }
    async Task Browse()
    {
        if (busy) return;
        var selected = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Pasta MatchReplay ou pasta de uma partida", AllowMultiple = false });
        if (closed) return;
        if (selected.FirstOrDefault()?.TryGetLocalPath() is string directory) { path.Text = directory; await (Listing = Scan()); }
    }
    async Task Scan()
    {
        if (busy || closed) return;
        scanCancellation?.Cancel();
        using var scan = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token);
        scanCancellation = scan;
        var token = scan.Token;
        try {
            string directory = Path.GetFullPath(path.Text ?? "");
            InvalidatePreview();choices.Clear();folders.Children.Clear();related.IsEnabled=false;
            state.Text = "Procurando replays…";
            load.IsEnabled = false;
            var candidates = await Task.Run(() => {
                if (!Directory.Exists(directory)) throw new IOException("Informe a pasta MatchReplay da instalação do Siege.");
                if ((File.GetAttributes(directory)&FileAttributes.ReparsePoint)!=0) throw new IOException("Selecione uma pasta local, sem atalhos.");
                var paths = Directory.GetFiles(directory,"*.rec").Length > 0 ? new[] { directory } : Directory.GetDirectories(directory).Where(d => (File.GetAttributes(d)&FileAttributes.ReparsePoint)==0 && Directory.GetFiles(d,"*.rec").Length>0).OrderDescending().Take(100).ToArray();
                return paths.Select(p => (Path: p, Count: Directory.GetFiles(p, "*.rec").Length)).ToArray();
            }, token);
            token.ThrowIfCancellationRequested();
            choices.Clear(); folders.Children.Clear();
            var labels = new List<(ReplayCard Card, int Count)>();
            foreach (var item in candidates) {
                var card = new ReplayCard(item.Path, item.Count); choices.Add(card); labels.Add((card,item.Count));
                card.IsCheckedChanged += (_, _) => { if(!busy)InvalidatePreview();related.IsEnabled = !busy&&choices.Any(c => c.IsChecked == true && c.Group > 0); };
            }
            page = 0; steps.SelectedIndex = 0; ShowPage();
            ReplayImport.RememberDirectory(directory);
            load.IsEnabled = true;
            var catalog = SiegeCatalog.Load();
            int identified = 0;
            foreach (var item in labels) {
                state.Text = $"Identificando replays · {++identified}/{labels.Count}. Você já pode marcar as pastas.";
                var result = await Task.Run(() => ReplayImport.PreviewFolderAsync(item.Card.Folder, token), token);
                token.ThrowIfCancellationRequested();
                item.Card.Identify(result, catalog, item.Count);
            }
            GroupCards(); ShowPage();
            state.Text = $"{choices.Count} pastas encontradas. Confira mapa, equipes e data antes de selecionar os trechos.";
        } catch (OperationCanceledException) { }
        catch (Exception ex) { if (!closed && !token.IsCancellationRequested) state.Text = ex.Message; }
        finally { if (ReferenceEquals(scanCancellation, scan)) { scanCancellation = null; if (!closed && !busy) load.IsEnabled = true; } }
    }
    void InvalidatePreview()
    {
        preview?.Dispose();preview=null;data=null;apply.IsEnabled=false;sessions.Children.Clear();transitions.Clear();report.Text="";
    }
    void ShowPage()
    {
        int pages = Math.Max(1, (choices.Count + 11) / 12); page = Math.Clamp(page, 0, pages - 1);
        folders.Children.Clear(); foreach (var card in choices.Skip(page * 12).Take(12)) folders.Children.Add(card);
        previousPage.IsVisible = nextPage.IsVisible = pages > 1; previousPage.IsEnabled = page > 0; nextPage.IsEnabled = page < pages - 1;
        pageLabel.Text = pages > 1 ? $"{page + 1}/{pages}" : "Até 12 replays por página"; pageLabel.VerticalAlignment = VerticalAlignment.Center;
    }
    void GroupCards()
    {
        var parents = Enumerable.Range(0, choices.Count).ToArray();
        int Root(int i) { while (parents[i] != i) i = parents[i]; return i; }
        for (int i = 0; i < choices.Count; i++) for (int j = i + 1; j < choices.Count; j++)
            if (choices[i].Header is {} a && choices[j].Header is {} b && ReplayImport.ContinuationHint(a, b) != "") parents[Root(j)] = Root(i);
        var groups = Enumerable.Range(0, choices.Count).GroupBy(Root).Select(g => g.Select(i => choices[i]).ToArray()).OrderByDescending(g => g.Max(c => c.Header?.Timestamp ?? default)).ToArray();
        choices.Clear(); int group = 0;
        foreach (var items in groups) {
            int id = items.Length > 1 ? ++group : 0; bool same = items.Select(c => c.Header?.MatchID).Distinct().Count() == 1;
            foreach (var card in items.OrderBy(c => c.Header?.Timestamp)) { card.SetRelation(id, same); choices.Add(card); }
        }
        related.IsEnabled = choices.Any(c => c.IsChecked == true && c.Group > 0);
    }
    async Task Load()
    {
        if (busy) return;
        var selected = choices.Where(c => c.IsChecked == true).Select(c => c.Folder).ToArray();
        if (selected.Length == 0) { state.Text = "Selecione ao menos uma pasta."; return; }
        scanCancellation?.Cancel();
        busy = true; load.IsEnabled = apply.IsEnabled = folders.IsEnabled = path.IsEnabled = false;
        related.IsEnabled=false;
        preview?.Dispose(); preview = null; data = null; sessions.Children.Clear(); transitions.Clear();
        steps.SelectedIndex = 1;
        try {
            var progress = new Progress<string>(text => { if (!closed) state.Text = text; });
            preview = await Task.Run(() => ReplayImport.ReadAsync(selected, match, map, progress, cancellation.Token));
            if (closed) return;
            for (int i=0;i<preview.Sessions.Count;i++) {
                sessions.Children.Add(Ui.RawText("Sessão "+(i+1)+": "+preview.Sessions[i]));
                if (i==0) continue;
                var mode = Ui.Combo("Selecionar tipo de retorno…", "Retorno: já inclui acumulados anteriores", "Novo lobby: somar ao trecho anterior");
                mode.SelectedIndex=0; transitions.Add(mode); sessions.Children.Add(mode); mode.SelectionChanged += (_,_) => RefreshPreview();
            }
            RefreshPreview();
        } catch (OperationCanceledException) { }
        catch (Exception ex) { if (!closed) state.Text = ex.Message; }
        finally { busy=false; if (closed) { preview?.Dispose(); cancellation.Dispose(); } else { load.IsEnabled = folders.IsEnabled = path.IsEnabled = true;related.IsEnabled=choices.Any(c=>c.IsChecked==true&&c.Group>0); } }
    }
    void RefreshPreview()
    {
        apply.IsEnabled=false; data=null;
        if (preview==null) return;
        if (transitions.Any(t => t.SelectedIndex<1)) { report.Text="Classifique cada sessão: usar os acumulados restaurados ou somar um lobby novo. A confirmação é obrigatória para evitar duplicação."; return; }
        try {
            data=preview.Consolidate(match,map,transitions.Select(t=>t.SelectedIndex==2).ToArray()); report.Text=preview.Report;
            state.Text="Confira os totais e diferenças. Os rounds e bans salvos não serão substituídos nesta versão experimental."; apply.IsEnabled=true;
        } catch (Exception ex) { report.Text=ex.Message; }
    }
    void Apply()
    {
        if (busy || preview==null || data==null) return;
        var result = new ReplaySelection(preview,data); preview=null; Close(result);
    }
}

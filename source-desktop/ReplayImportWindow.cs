using Avalonia.Platform.Storage;

namespace CasterDesk.Desktop;

public sealed record ReplaySelection(ReplayImport Import, MapStatistics Statistics);

public sealed class ReplayImportWindow : Window
{
    readonly SiegeMatch match;
    readonly MapRecord map;
    readonly TextBox path = new() { PlaceholderText = "Pasta MatchReplay do Siege" };
    readonly StackPanel folders = new() { Spacing = 5 }, sessions = new() { Spacing = 6 };
    readonly TextBox report = new() { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
    readonly TextBlock state = Ui.Text("Selecione somente as pastas da partida. Nenhum arquivo será enviado pela internet.");
    readonly List<(CheckBox Box, string Path)> choices = [];
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
        Title = "Importar replays · experimental"; Width = 1060; Height = 760; MinWidth = 800; MinHeight = 580;
        Background = Ui.Canvas; FontFamily = new FontFamily("Segoe UI"); WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var grid = new Grid { Margin = new Thickness(16), RowDefinitions = new("Auto,Auto,*,Auto"), ColumnDefinitions = new("2*,3*") };
        Content = grid;
        var header = new StackPanel { Spacing = 5 };
        header.Children.Add(Ui.Text("Selecione os trechos do mapa. Retorno ao mesmo lobby é agrupado pelo ID da sessão. Lobbies diferentes precisam de classificação."));
        var location = new Grid { ColumnDefinitions = new("*,Auto,Auto") }; location.Children.Add(path);
        var browse = Ui.Button("Procurar", () => _ = Browse()); location.Children.Add(browse); Grid.SetColumn(browse, 1);
        var scan = Ui.Button("Listar pastas", () => Listing = Scan()); location.Children.Add(scan); Grid.SetColumn(scan, 2);
        header.Children.Add(location); grid.Children.Add(header); Grid.SetColumnSpan(header, 2);
        grid.Children.Add(state); Grid.SetRow(state, 1); Grid.SetColumnSpan(state, 2);
        var left = new ScrollViewer { Content = folders, Margin = new Thickness(0, 8, 12, 8) }; grid.Children.Add(left); Grid.SetRow(left, 2);
        var right = new Grid { RowDefinitions = new("Auto,*"), Margin = new Thickness(0,8,0,8) };
        right.Children.Add(sessions); right.Children.Add(report); Grid.SetRow(report,1); grid.Children.Add(right); Grid.SetRow(right,2); Grid.SetColumn(right,1);
        var actions = new WrapPanel();
        load = Ui.Button("Ler selecionados", () => _ = Load()); actions.Children.Add(load);
        apply = Ui.Button("Aplicar à conferência", Apply, true); apply.IsEnabled = false; actions.Children.Add(apply);
        actions.Children.Add(Ui.Button("Cancelar", () => Close())); grid.Children.Add(actions); Grid.SetRow(actions,3); Grid.SetColumnSpan(actions,2);
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
            var labels = new List<(string Path, TextBlock Title, TextBlock Detail, int Count)>();
            foreach (var item in candidates) {
                var title = Ui.RawText("Identificando mapa e equipes…", true); title.Name = "replay-folder-title"; title.TextWrapping = TextWrapping.Wrap;
                var detail = Ui.RawText(Path.GetFileName(item.Path)+" · "+item.Count+" arquivos"); detail.FontSize = 11; detail.Opacity = .75; detail.TextWrapping = TextWrapping.Wrap;
                var content = new StackPanel { Spacing = 3, Children = { title, detail } };
                var box = new CheckBox { Content = content, IsChecked = false, HorizontalContentAlignment = HorizontalAlignment.Stretch, Padding = new Thickness(4, 6) };
                choices.Add((box,item.Path)); folders.Children.Add(box); labels.Add((item.Path,title,detail,item.Count));
            }
            ReplayImport.RememberDirectory(directory);
            load.IsEnabled = true;
            var catalog = SiegeCatalog.Load();
            int identified = 0;
            foreach (var item in labels) {
                state.Text = $"Identificando replays · {++identified}/{labels.Count}. Você já pode marcar as pastas.";
                var result = await Task.Run(() => ReplayImport.PreviewFolderAsync(item.Path, token), token);
                token.ThrowIfCancellationRequested();
                if (result.Header is {} header) {
                    string mapName = catalog.Maps.FirstOrDefault(m => m.Id == ReplayImport.MapId(header.Map.Name))?.DisplayName ?? header.Map.Name;
                    var teams = header.Teams.Select(t => string.IsNullOrWhiteSpace(t.Name) ? "Equipe não informada" : t.Name.Trim());
                    item.Title.Text = mapName + " · " + string.Join(" × ", teams);
                    item.Detail.Text = header.Timestamp.ToLocalTime().ToString("dd/MM/yyyy HH:mm") + " · " + item.Count + " arquivos" + Environment.NewLine + Path.GetFileName(item.Path);
                } else item.Title.Text = result.Error;
            }
            state.Text = $"{choices.Count} pastas encontradas. Confira mapa, equipes e data antes de selecionar os trechos.";
        } catch (OperationCanceledException) { }
        catch (Exception ex) { if (!closed && !token.IsCancellationRequested) state.Text = ex.Message; }
        finally { if (ReferenceEquals(scanCancellation, scan)) { scanCancellation = null; if (!closed && !busy) load.IsEnabled = true; } }
    }
    async Task Load()
    {
        if (busy) return;
        var selected = choices.Where(c => c.Box.IsChecked == true).Select(c => c.Path).ToArray();
        if (selected.Length == 0) { state.Text = "Selecione ao menos uma pasta."; return; }
        scanCancellation?.Cancel();
        busy = true; load.IsEnabled = apply.IsEnabled = folders.IsEnabled = path.IsEnabled = false;
        preview?.Dispose(); preview = null; data = null; sessions.Children.Clear(); transitions.Clear();
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
        finally { busy=false; if (closed) { preview?.Dispose(); cancellation.Dispose(); } else { load.IsEnabled = folders.IsEnabled = path.IsEnabled = true; } }
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

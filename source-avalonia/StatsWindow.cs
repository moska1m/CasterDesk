using Avalonia.Input;

namespace CasterDesk.Avalonia;

public sealed class StatsWindow : Window
{
    readonly SiegeMatch match;
    readonly MapRecord map;
    readonly StackPanel entries = new() { Spacing = 8 };
    readonly List<Entry> rows = [];
    readonly List<(TextBlock Heading, List<Entry> Rows)> groups = [];
    readonly Image image = new() { Stretch = Stretch.Uniform };
    readonly TextBlock status = Ui.Text("E = eliminações · M = mortes · A = assistências. Dados lidos precisam de revisão.");
    readonly ComboBox top, coverage = Ui.Combo("Estatísticas completas", "Estatísticas parciais / remake", "Sem print / sem estatísticas");
    readonly Button confirm;
    readonly CancellationTokenSource lifetime = new();
    CancellationTokenSource? reading;
    System.Drawing.Bitmap? original;
    global::Avalonia.Media.Imaging.Bitmap? thumbnail;
    bool closed, busy, edited, imageChanged;
    bool saving;
    bool allowClose, askingClose;
    int generation;

    public StatsWindow(SiegeMatch match, MapRecord map)
    {
        this.match = match; this.map = map;
        Title = L.T("Conferir estatísticas"); Width = 1280; Height = 780; MinWidth = 900; MinHeight = 580;
        WindowStartupLocation = WindowStartupLocation.CenterOwner; Background = Ui.Canvas; FontFamily = new FontFamily("Segoe UI"); FontSize = 14;
        var body = new Grid { Margin = new Thickness(18), RowDefinitions = new("Auto,*,Auto"), ColumnDefinitions = new("*,*") };
        Content = body;
        var actions = new WrapPanel();
        actions.Children.Add(Ui.Button("Abrir print", () => _ = Open()));
        actions.Children.Add(Ui.Button("Colar print · Ctrl+V", () => _ = Paste()));
        actions.Children.Add(Ui.Button("Ler novamente", () => _ = Read()));
        actions.Children.Add(Ui.Button("Conciliar jogadores", () => _ = Reconcile()));
        body.Children.Add(actions);
        top = Ui.RawCombo(match.TeamA, match.TeamB);
        top.PlaceholderText = L.T("Selecionar…");
        top.SelectedIndex = map.Statistics?.TopTeam ?? -1;
        top.Name = "stats-top-team";
        var choices = new StackPanel { Margin = new Thickness(12, 0, 0, 8) };
        choices.Children.Add(Ui.Text("Equipe no topo do print:")); choices.Children.Add(top); choices.Children.Add(coverage);
        body.Children.Add(choices); Grid.SetColumn(choices, 1);
        coverage.SelectedIndex = map.Statistics?.Status switch { "partial" => 1, "missing" => 2, _ => 0 };
        var frame = new Border { Background = Brush.Parse("#0B1017"), Padding = new Thickness(12), Margin = new Thickness(0, 0, 16, 12), Child = image };
        body.Children.Add(frame); Grid.SetRow(frame, 1);
        var scroll = new ScrollViewer { Content = entries, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        body.Children.Add(scroll); Grid.SetColumn(scroll, 1); Grid.SetRow(scroll, 1);
        var footer = new Grid { ColumnDefinitions = new("*,Auto") };
        footer.Children.Add(status);
        confirm = Ui.Button("Confirmar estatísticas", () => _ = Save(), true); footer.Children.Add(confirm); Grid.SetColumn(confirm, 1);
        body.Children.Add(footer); Grid.SetRow(footer, 2); Grid.SetColumnSpan(footer, 2);
        Fill(map.Statistics?.Players ?? []);
        int lastTop = top.SelectedIndex, lastCoverage = coverage.SelectedIndex;
        top.SelectionChanged += (_, _) => {
            if (top.SelectedIndex == lastTop) return;
            int previousTop = lastTop;
            lastTop = top.SelectedIndex;
            if (lastTop < 0) return;
            // Choosing an unknown legacy orientation must not invert already reviewed player teams.
            if (previousTop >= 0) {
                foreach (var row in rows) { row.Team = 1 - row.Team; row.TeamLabel.Text = match.Team(row.Team); row.PlayerId = ""; }
                foreach (var group in groups) if (group.Rows.Count > 0) group.Heading.Text = match.Team(group.Rows[0].Team);
            }
            edited = true;
        };
        coverage.SelectionChanged += (_, _) => { if (coverage.SelectedIndex != lastCoverage) { lastCoverage = coverage.SelectedIndex; edited = true; } };
        KeyDown += (_, e) => { if (e.Key == Key.V && e.KeyModifiers.HasFlag(KeyModifiers.Control)) { e.Handled = true; _ = Paste(); } };
        string? file = MatchFiles.Resolve(match, map.Statistics?.Screenshot);
        if (file != null) { using var saved = System.Drawing.Image.FromFile(file); ReplaceImage(saved); }
        Closing += (_, e) => {
            if (allowClose) return;
            if (busy) { e.Cancel = true; reading?.Cancel(); return; }
            if (edited) { e.Cancel = true; if (!askingClose) _ = ConfirmClose(); }
        };
        Closed += (_, _) => { closed = true; generation++; reading?.Cancel(); lifetime.Cancel(); original?.Dispose(); thumbnail?.Dispose(); };
    }

    void Fill(IReadOnlyList<PlayerStatistic> data)
    {
        entries.Children.Clear(); rows.Clear(); groups.Clear();
        for (int team = 0; team < 2; team++) {
            var heading = Ui.RawText(match.Team(team), true); entries.Children.Add(heading);
            var groupRows = new List<Entry>(); groups.Add((heading, groupRows));
            var group = data.Where(p => p.Team == team).ToArray();
            for (int n = 0; n < Math.Max(5, group.Length); n++) {
                var p = group.ElementAtOrDefault(n);
                var row = new Entry { Team = team, PlayerId = p?.PlayerId ?? "" };
                row.Name.Name = $"stats-player-{team}-{n}";
                row.TeamLabel.Text = match.Team(team);
                row.Name.Text = p?.Name ?? ""; row.Kills.Text = p?.Kills?.ToString() ?? "";
                row.Deaths.Text = p?.Deaths?.ToString() ?? ""; row.Assists.Text = p?.Assists?.ToString() ?? "";
                var grid = new Grid { ColumnDefinitions = new("3*,*,*,*"), Margin = new Thickness(0, 0, 0, 4) };
                TextBox[] inputs = [row.Name, row.Kills, row.Deaths, row.Assists]; string[] labels = ["Jogador", "E", "M", "A"];
                for (int i = 0; i < inputs.Length; i++) {
                    var column = new StackPanel { Margin = new Thickness(3) }; column.Children.Add(Ui.Text(labels[i])); column.Children.Add(inputs[i]);
                    grid.Children.Add(column); Grid.SetColumn(column, i);
                    var input = inputs[i]; string? lastValue = input.Text;
                    input.TextChanged += (_, _) => { if (input.Text != lastValue) { lastValue = input.Text; edited = true; } };
                }
                entries.Children.Add(grid); rows.Add(row); groupRows.Add(row);
            }
        }
        edited = false;
    }

    async Task Open()
    {
        if (saving) return;
        try {
            var path = await Ui.PickImage(this); if (path == null || closed) return;
            if (new FileInfo(path).Length > 40_000_000) throw new InvalidDataException(L.T("Use uma imagem com até 40 MB."));
            using var decoded = await Task.Run(() => { using var source = System.Drawing.Image.FromFile(path); return new System.Drawing.Bitmap(source); });
            await SetImage(decoded);
        } catch (Exception ex) { if (!closed) status.Text = ex.Message; }
    }

    async Task Paste()
    {
        if (saving) return;
        try {
            using var decoded = Ui.ClipboardImage();
            if (decoded == null) { status.Text = L.T("Copie uma imagem antes de colar."); return; }
            await SetImage(decoded);
        } catch (Exception ex) { if (!closed) status.Text = ex.Message; }
    }

    void ReplaceImage(System.Drawing.Image source)
    {
        original?.Dispose(); original = new System.Drawing.Bitmap(source);
        image.Source = null; thumbnail?.Dispose(); thumbnail = Ui.Bitmap(original); image.Source = thumbnail;
    }

    async Task SetImage(System.Drawing.Image source)
    {
        if ((long)source.Width * source.Height > 50_000_000) throw new InvalidDataException(L.T("A imagem é muito grande."));
        if (edited && await Ui.Ask(this, "Substituir os dados editados pela leitura de um novo print?") != 1) return;
        if (closed) return;
        reading?.Cancel(); generation++; busy = false; confirm.IsEnabled = true;
        ReplaceImage(source); imageChanged = true; top.SelectedIndex = -1; coverage.SelectedIndex = 0; edited = true;
        status.Text = L.T("Equipe no topo do print:") + " " + L.T("Selecionar…") + " · " + L.T("Ler novamente");
    }

    async Task Read()
    {
        if (original == null || closed || saving) return;
        if (top.SelectedIndex < 0) {
            status.Text = L.T("Equipe no topo do print:") + " " + L.T("Selecionar…");
            top.Focus(); return;
        }
        if (edited && await Ui.Ask(this, "Substituir os dados editados por uma nova leitura?") != 1) return;
        reading?.Cancel(); var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token); reading = cancellation;
        int request = ++generation, topTeam = top.SelectedIndex; edited = false; busy = true; confirm.IsEnabled = false;
        status.Text = L.T("Lendo print… Confira os nomes e os números ao terminar.");
        string file = Path.Combine(Path.GetTempPath(), "casterdesk-avalonia-ocr-" + Guid.NewGuid().ToString("N") + ".png");
        using var snapshot = new System.Drawing.Bitmap(original);
        try {
            await Task.Run(() => {
                double scale = Math.Min(1, 2500.0 / Math.Max(snapshot.Width, snapshot.Height));
                using var reduced = new System.Drawing.Bitmap(snapshot, Math.Max(1, (int)(snapshot.Width * scale)), Math.Max(1, (int)(snapshot.Height * scale)));
                reduced.Save(file, System.Drawing.Imaging.ImageFormat.Png);
            }, cancellation.Token);
            var document = await StatisticsData.ReadImage(file, cancellation.Token);
            if (closed || request != generation) return;
            if (edited) { status.Text = L.T("Leitura descartada para preservar sua edição manual."); return; }
            var data = StatisticsData.Parse(document); if (topTeam == 1) foreach (var p in data) p.Team = 1 - p.Team;
            Fill(data);
            bool complete = data.Count == 10 && data.All(p => p.Kills.HasValue && p.Deaths.HasValue && p.Assists.HasValue && !string.IsNullOrWhiteSpace(p.Name));
            coverage.SelectedIndex = complete ? 0 : 1;
            status.Text = L.T(complete ? "Leitura concluída. Confira equipe, nome e E/M/A de cada jogador." : "Não foi possível identificar dez jogadores. Preencha os campos ou marque como parcial.");
        } catch (OperationCanceledException) { }
        catch (Exception ex) { if (!closed && request == generation) status.Text = ex.Message; }
        finally {
            cancellation.Dispose();
            try { if (File.Exists(file)) File.Delete(file); } catch (IOException ex) { AppLog.Write(ex.ToString()); }
            if (request == generation) { reading = null; busy = false; confirm.IsEnabled = true; }
        }
    }

    async Task ConfirmClose()
    {
        askingClose = true;
        try { if (await Ui.Ask(this, "Descartar as alterações não salvas?") == 1) { allowClose = true; Close(false); } }
        finally { askingClose = false; }
    }

    async Task Save()
    {
        if (busy) return;
        try {
            var data = new MapStatistics { Status = coverage.SelectedIndex switch { 1 => "partial", 2 => "missing", _ => "complete" }, ConfirmedAt = DateTimeOffset.Now, TopTeam = top.SelectedIndex >= 0 ? top.SelectedIndex : null };
            int? Number(string? text) => string.IsNullOrWhiteSpace(text) ? null : int.TryParse(text, out int n) ? n : throw new InvalidDataException(L.T("Use números inteiros nos campos E/M/A."));
            if (data.Status != "missing") foreach (var row in rows.Where(r => !string.IsNullOrWhiteSpace(r.Name.Text)))
                data.Players.Add(new() { PlayerId = row.PlayerId, Team = row.Team, Name = row.Name.Text!.Trim(), Kills = Number(row.Kills.Text), Deaths = Number(row.Deaths.Text), Assists = Number(row.Assists.Text) });
            StatisticsData.Validate(data);
            busy = saving = true; confirm.IsEnabled = false; entries.IsEnabled = top.IsEnabled = coverage.IsEnabled = false;
            using var snapshot = original == null || !imageChanged ? null : new System.Drawing.Bitmap(original);
            await Task.Run(() => MatchFiles.SaveStatistics(match, map, data, snapshot, imageChanged));
            allowClose = true; Close(true);
        } catch (Exception ex) { status.Text = ex.Message; }
        finally { busy = saving = false; confirm.IsEnabled = true; entries.IsEnabled = top.IsEnabled = coverage.IsEnabled = true; }
    }

    async Task Reconcile()
    {
        var content = new StackPanel { Margin = new Thickness(18) };
        content.Children.Add(Ui.Text("Associe variações do nome ao mesmo jogador da série. Nenhuma associação aproximada é feita automaticamente."));
        var choices = new List<(Entry Row, ComboBox Box)>();
        foreach (var row in rows.Where(r => !string.IsNullOrWhiteSpace(r.Name.Text))) {
            content.Children.Add(Ui.Text(match.Team(row.Team) + " · " + row.Name.Text));
            var known = match.Maps.SelectMany(m => m.Statistics?.Players ?? []).Where(p => p.Team == row.Team && p.PlayerId != "").GroupBy(p => p.PlayerId).Select(g => g.First());
            var items = new List<Identity> { new("", L.T("Novo jogador")) }; items.AddRange(known.Select(p => new Identity(p.PlayerId, p.Name)));
            var box = Ui.Combo(); box.ItemsSource = items; box.SelectedItem = items.FirstOrDefault(i => i.Id == row.PlayerId) ?? items[0]; content.Children.Add(box); choices.Add((row, box));
        }
        var dialog = new Window { Title = L.T("Conciliar jogadores"), Width = 550, Height = 650, Background = Ui.Canvas, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        content.Children.Add(Ui.Button("Confirmar", () => { foreach (var item in choices) item.Row.PlayerId = (item.Box.SelectedItem as Identity)?.Id ?? ""; edited = true; dialog.Close(true); }, true));
        dialog.Content = new ScrollViewer { Content = content }; await dialog.ShowDialog<bool>(this);
    }

    sealed record Identity(string Id, string Name) { public override string ToString() => Name; }
    sealed class Entry {
        public int Team; public string PlayerId = "";
        public readonly TextBlock TeamLabel = Ui.Text("");
        public readonly TextBox Name = new(), Kills = new(), Deaths = new(), Assists = new();
    }
}

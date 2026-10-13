using System.Text.Json;

namespace CasterDesk.Avalonia;

internal sealed class BackupWindow : Window
{
    readonly Func<Task> prepare;
    readonly System.Action resume;
    readonly StackPanel actions = new(), review = new();
    readonly TextBlock status = Ui.Text("");
    readonly ProgressBar progress = new() { IsIndeterminate = true, IsVisible = false, Height = 4 };
    string? copy, stage;
    BackupManifest? manifest;
    bool scheduled;
    bool confirming;
    internal bool Busy { get; private set; }
    internal Task Operation { get; private set; } = Task.CompletedTask;
    internal int ConflictCount { get; private set; }
    internal string Status => status.Text ?? "";

    public BackupWindow(Control owner, Func<Task> prepare, System.Action resume)
    {
        this.prepare = prepare; this.resume = resume;
        var sizing = Ui.Dialog(owner, "Backup e restauração", new StackPanel(), 760, 610);
        Title = sizing.Title; Width = sizing.Width; Height = sizing.Height; Background = Ui.Canvas;
        WindowStartupLocation = WindowStartupLocation.CenterOwner; FontFamily = new FontFamily("Segoe UI"); FontSize = 14;
        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 6 }; Content = new ScrollViewer { Content = panel, Background = Ui.Canvas };
        panel.Children.Add(Ui.Text("Backup e restauração", true)); panel.Children.Add(Ui.Text("Pasta de dados"));
        panel.Children.Add(new TextBox { Text = AppPaths.DataDirectory, IsReadOnly = true });
        panel.Children.Add(Ui.Text("O backup inclui partidas, artes, prints, textos e configurações dentro da pasta CasterDesk. Arquivos em caminhos externos e backups anteriores não são incluídos."));
        panel.Children.Add(Ui.Button("Copiar caminho", () => { try { Ui.CopyText(AppPaths.DataDirectory); } catch (Exception ex) { status.Text = ex.Message; } }));
        var create = Ui.Button("Criar backup", async () => {
            try { if (await Ui.PickFile(this, "Criar backup", "zip", true, $"CasterDesk_Backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.zip") is string path) Operation = Create(path); }
            catch (Exception ex) { status.Text = ex.Message; }
        }, true); create.Name = "backup-create"; actions.Children.Add(create);
        var restore = Ui.Button("Restaurar backup", async () => {
            try { if (await Ui.PickFile(this, "Restaurar backup", "zip") is string path) Operation = Verify(path); }
            catch (Exception ex) { status.Text = ex.Message; }
        }); restore.Name = "backup-restore"; actions.Children.Add(restore);
        panel.Children.Add(actions); panel.Children.Add(progress); panel.Children.Add(status); panel.Children.Add(review);
        panel.Children.Add(Ui.Button("Fechar", () => Close(scheduled)));
        Closing += async (_, e) => {
            if (Busy || confirming) { e.Cancel = true; return; }
            if (!scheduled && (stage != null || copy != null)) {
                e.Cancel = true; Busy = true; progress.IsVisible = true;
                await Task.Run(Cleanup); Busy = false; progress.IsVisible = false; Close(false);
            }
        };
    }

    async Task Work(Func<Task> action)
    {
        if (Busy) return;
        Busy = true; actions.IsEnabled = review.IsEnabled = false; progress.IsVisible = true;
        try { await prepare(); await action(); }
        catch (Exception ex) { status.Text = ex.Message; }
        finally { resume(); Busy = false; actions.IsEnabled = review.IsEnabled = true; progress.IsVisible = false; }
    }
    internal Task Create(string destination) => Work(async () => {
        await Task.Run(Cleanup); review.Children.Clear();
        var updates = new Progress<BackupProgress>(p => { if (Busy) status.Text = L.F($"Backup: {p.Files} arquivos • {p.Bytes / 1048576.0:0.0} MB"); });
        await Task.Run(() => DataBackup.Create(AppPaths.DataDirectory, destination, p => ((IProgress<BackupProgress>)updates).Report(p)));
        status.Text = L.T("Backup criado. Arquivos salvos fora da pasta do CasterDesk não estão incluídos. Guarde uma cópia em outro disco.") + "\n" + destination;
    });
    internal Task Verify(string archive) => Work(async () => {
        await Task.Run(Cleanup); review.Children.Clear(); manifest = null; ConflictCount = 0;
        if (File.Exists(Path.Combine(AppPaths.DataDirectory, "pending-restore.json"))) throw new InvalidOperationException(L.T("Já existe uma restauração agendada. Feche e abra o CasterDesk para concluí-la."));
        copy = Path.Combine(AppPaths.BackupDirectory, "restaurar-" + Guid.NewGuid().ToString("N") + ".zip");
        stage = Path.Combine(AppPaths.BackupDirectory, "verificacao-" + Guid.NewGuid().ToString("N"));
        status.Text = L.T("Verificando integridade do backup…");
        try {
            var verified = await Task.Run(() => { File.Copy(archive, copy); var result = DataBackup.Extract(copy, stage); return (Manifest: result, Conflicts: DataBackup.Conflicts(AppPaths.DataDirectory, result)); });
            manifest = verified.Manifest; ConflictCount = verified.Conflicts;
        } catch { await Task.Run(Cleanup); throw; }
        status.Text = L.F($"Backup verificado: {manifest.Files.Count} arquivos. {ConflictCount} arquivos existentes têm conteúdo diferente.");
        review.Children.Add(Ui.Text("Escolha como tratar os arquivos existentes. Uma cópia dos dados atuais será criada antes da restauração. A restauração acontece na próxima abertura do programa."));
        AddChoice("Manter arquivos atuais", "backup-keep", false);
        AddChoice("Usar versões do backup", "backup-replace", true);
        var cancel = Ui.Button("Cancelar", () => { Operation = Work(async () => { await Task.Run(Cleanup); review.Children.Clear(); status.Text = L.T("Restauração cancelada. Nenhum dado foi alterado."); }); }); cancel.Name = "backup-cancel"; review.Children.Add(cancel);
    });
    void AddChoice(string label, string name, bool replace) {
        var button = Ui.Button(label, () => { if (!Busy) Operation = Confirm(replace); }, !replace); button.Name = name; review.Children.Add(button);
    }
    async Task Confirm(bool replace)
    {
        if (manifest == null || copy == null || confirming) return;
        confirming = true; review.IsEnabled = actions.IsEnabled = false;
        int answer;
        try { answer = await Ui.Ask(this, "Agendar a restauração e fechar o CasterDesk? Ela será aplicada na próxima abertura. Uma cópia dos dados atuais será guardada em Backups."); }
        finally { confirming = false; review.IsEnabled = actions.IsEnabled = true; }
        if (answer != 1) return;
        await Work(async () => {
            await Task.Run(() => {
                if (File.Exists(Path.Combine(AppPaths.DataDirectory, "pending-restore.json"))) throw new InvalidOperationException(L.T("Já existe uma restauração agendada. Feche e abra o CasterDesk para concluí-la."));
                if (stage != null) DataBackup.RemoveStage(stage);
                MatchFiles.AtomicText(Path.Combine(AppPaths.DataDirectory, "pending-restore.json"), JsonSerializer.Serialize(new { Archive = copy, Replace = replace }));
            });
            stage = null; scheduled = true;
            status.Text = L.T("Restauração agendada. Ela será aplicada na próxima abertura do CasterDesk.");
        });
        if (scheduled) Close(true);
    }
    void Cleanup()
    {
        try {
            if (stage != null) DataBackup.RemoveStage(stage);
            if (copy != null && !scheduled) { string checkedPath = DataBackup.SafePath(AppPaths.BackupDirectory, Path.GetFileName(copy)); if (File.Exists(checkedPath)) File.Delete(checkedPath); }
        } catch (Exception ex) { AppLog.Write("Backup preview cleanup: " + ex); }
        finally { stage = copy = null; manifest = null; }
    }
}

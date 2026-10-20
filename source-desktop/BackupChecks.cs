using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Media.Imaging;
using System.Text.Json;

namespace CasterDesk.Desktop;

internal static class BackupChecks
{
    static int count;
    static void Need(bool value, string message) { count++; if (!value) throw new Exception("Backup UI: " + message); }
    static void Wait(Task task) {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        while (!task.IsCompleted) { if (timer.Elapsed.TotalSeconds > 45) throw new TimeoutException("Backup UI timeout"); Dispatcher.UIThread.RunJobs(); Thread.Sleep(5); }
        task.GetAwaiter().GetResult(); Dispatcher.UIThread.RunJobs();
    }
    static void Click(Control root, string name) {
        var button = root.GetVisualDescendants().OfType<Button>().Single(b => b.Name == name);
        Need(button.IsEnabled, "Enabled " + name); button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs();
    }
    public static void Run() {
        var owner = new Window { Width = 980, Height = 640 }; owner.Show();
        int preparations = 0, resumes = 0;
        BackupWindow Open() { var dialog = new BackupWindow(owner, () => { preparations++; return Task.CompletedTask; }, () => resumes++); dialog.Show(); Dispatcher.UIThread.RunJobs(); return dialog; }
        string file = Path.Combine(AppPaths.TextDirectory, "backup-ui.txt"); File.WriteAllText(file, "archived");
        string archive = Path.Combine(AppPaths.BackupDirectory, "UI-original.zip");
        var view = Open(); Wait(view.Create(archive));
        Need(File.Exists(archive) && !view.Busy && preparations == resumes, "Backup created with work lock released");
        File.WriteAllText(file, "current");
        Wait(view.Verify(archive)); Need(view.ConflictCount >= 1 && !view.Busy, "Conflicts shown before restore");
        Need(!File.Exists(Path.Combine(AppPaths.DataDirectory, "pending-restore.json")) && File.ReadAllText(file) == "current", "Verification does not alter data");
        using (var image = new RenderTargetBitmap(new PixelSize(760, 610), new Vector(96, 96))) {
            ((Control)view.Content!).Measure(new Size(760, 610)); ((Control)view.Content).Arrange(new Rect(0, 0, 760, 610));
            Dispatcher.UIThread.RunJobs();
            image.Render((Control)view.Content); image.Save(Path.Combine(AppPaths.DataDirectory, "casterdesk-backup-review.png"));
        }
        Click(view, "backup-keep"); Need(Ui.ActiveQuestion != null, "Final confirmation required"); Click(Ui.ActiveQuestion!, "ask-0"); Wait(view.Operation);
        Need(!File.Exists(Path.Combine(AppPaths.DataDirectory, "pending-restore.json")), "Rejected confirmation does not schedule");
        Click(view, "backup-cancel"); Wait(view.Operation);
        Need(Directory.GetDirectories(AppPaths.BackupDirectory, "verificacao-*").Length == 0 && File.Exists(archive), "Preview cancelled without removing original archive");
        Wait(view.Verify(archive));
        // The archive selected by the user may change after verification. Restore uses the verified private copy.
        File.WriteAllText(archive, "original changed after verification");
        Click(view, "backup-keep"); Click(Ui.ActiveQuestion!, "ask-1"); Wait(view.Operation);
        string pending = Path.Combine(AppPaths.DataDirectory, "pending-restore.json");
        using (var request = JsonDocument.Parse(File.ReadAllText(pending))) Need(!request.RootElement.GetProperty("Replace").GetBoolean(), "Keep-current policy scheduled");
        Need(File.ReadAllText(file) == "current" && !view.IsVisible, "Scheduling leaves data intact and closes dialog");
        string? notification = null; Wait(Task.Run(() => DataBackup.ProcessPending(text => notification = text)));
        Need(!File.Exists(pending) && File.ReadAllText(file) == "current" && notification != null, "Startup restore preserves current files");
        Need(Directory.GetFiles(AppPaths.BackupDirectory, "Antes-da-restauracao_*.zip").Length == 1, "Recovery ZIP created");
        Wait(Task.Run(() => DataBackup.Create(AppPaths.DataDirectory, archive)));
        File.WriteAllText(file, "newer"); view = Open(); Wait(view.Verify(archive)); Click(view, "backup-replace"); Click(Ui.ActiveQuestion!, "ask-1"); Wait(view.Operation);
        using (var request = JsonDocument.Parse(File.ReadAllText(pending))) Need(request.RootElement.GetProperty("Replace").GetBoolean(), "Replace policy explicit");
        Wait(Task.Run(() => DataBackup.ProcessPending())); Need(File.ReadAllText(file) == "current", "Replace policy restores backup version");
        Need(Directory.GetFiles(AppPaths.BackupDirectory, "Antes-da-restauracao_*.zip").Length == 2, "Every restore has recovery ZIP");
        string bad = Path.Combine(AppPaths.BackupDirectory, "not-backup.zip"); File.WriteAllText(bad, "not ZIP"); view = Open(); Wait(view.Verify(bad));
        Need(!view.Busy && !File.Exists(pending) && File.ReadAllText(file) == "current", "Invalid archive rejected without data changes");
        Need(Directory.GetDirectories(AppPaths.BackupDirectory, "verificacao-*").Length == 0, "Failed verification cleans staging");
        view.Close(); owner.Close();
        var shell = new StudioShell(); shell.Show(); Dispatcher.UIThread.RunJobs(); shell.ToggleMenu(); Dispatcher.UIThread.RunJobs();
        shell.ToggleMenu();var optionTask=shell.OpenOptions();Dispatcher.UIThread.RunJobs();var options=shell.OwnedWindows.Single(w=>w.Name=="options-window");
        var tabs=options.GetVisualDescendants().OfType<TabControl>().Single(t=>t.Name=="settings-tabs");tabs.SelectedIndex=4;Dispatcher.UIThread.RunJobs();
        Need(options.GetVisualDescendants().OfType<Button>().Any(b=>b.Name=="backup-menu"),"Backup entry in options");
        tabs.SelectedIndex=0;Dispatcher.UIThread.RunJobs();var language=options.GetVisualDescendants().OfType<ComboBox>().Single(c=>c.Name=="settings-language");language.SelectedIndex=2;Dispatcher.UIThread.RunJobs();
        Need(ConfigurationStore.Read<Settings>("settings.json").Language=="es" && L.Current=="es","Language saved and applied immediately");
        language.SelectedIndex=0;Dispatcher.UIThread.RunJobs();options.Close();Wait(optionTask);
        shell.Texts.Stop(); Wait(shell.Texts.Writes); shell.Close(); Dispatcher.UIThread.RunJobs();
        Need(preparations == resumes, "All backup operations resume live output");
        foreach (string languageCode in new[] { "en", "es" }) {
            L.Set(languageCode);
            Need(new[] { "Backup e restauração", "Manter arquivos atuais", "Usar versões do backup", "Verificando integridade do backup…", "Preparando os dados do CasterDesk…" }.All(text => L.T(text) != text), "New backup labels translated into " + languageCode);
        }
        L.Set("pt-BR");
        File.WriteAllText(Path.Combine(AppPaths.DataDirectory, "casterdesk-backup-check.txt"), $"PASS: {count} UI/restore checks: creation, verification, conflicts, explicit policies, cancel, immutable copy, pending restore, recovery copy, invalid archive and language menu. Headless only.");
    }
}

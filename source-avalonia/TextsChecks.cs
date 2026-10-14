using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Media.Imaging;
using System.Text.Json;

namespace CasterDesk.Avalonia;

internal static class TextsChecks
{
    static int count;
    static void Need(bool value, string message) { count++; if (!value) throw new Exception("Texts: " + message); }
    static T Find<T>(Control view, string name) where T : Control => view.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);
    static void Click(Control view, string name) { Find<Button>(view, name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs(); }
    static void Wait(Task task) {
        var timeout = System.Diagnostics.Stopwatch.StartNew();
        while (!task.IsCompleted) { if (timeout.Elapsed.TotalSeconds > 15) throw new TimeoutException(); Dispatcher.UIThread.RunJobs(); Thread.Sleep(5); }
        task.GetAwaiter().GetResult(); Dispatcher.UIThread.RunJobs();
    }
    static void Flush(TextsView view) { Dispatcher.UIThread.RunJobs(); Wait(view.Writes); }
    public static void Run() {
        double time = 0;
        var settings = new Settings { Initial = 1, Output = Path.Combine(AppPaths.TextDirectory, "test-timer.txt") };
        var view = new TextsView(settings, () => MatchFiles.AtomicText(AppPaths.ConfigFile("settings.json"), JsonSerializer.Serialize(settings)), new Countdown(() => time));
        Need(!File.Exists(settings.Output), "Construction does not overwrite output");
        var window = new Window { Content = view, Width = 1280, Height = 800 }; window.Show(); Dispatcher.UIThread.RunJobs();
        view.StartLive(); Flush(view); Need(File.ReadAllText(settings.Output) == "01:00", "Initial output");
        Click(view, "timer-start"); time = 12; view.Tick(); Flush(view);
        Need(view.Running && File.ReadAllText(settings.Output) == "00:48", "Running countdown");
        Click(view, "timer-pause"); time += 100; view.Tick(); Need(!view.Running && view.Remaining == 48, "Pause retains remaining time");
        Find<TextBox>(view, "timer-preset-0").Text = "1"; Click(view, "timer-add-0"); Flush(view);
        Need(view.Remaining == 108 && !view.Running, "Add while paused");
        var header = new HeaderTimer(view); var headerWindow = new Window { Content = header, Width = 480, Height = 80 }; headerWindow.Show(); Dispatcher.UIThread.RunJobs();
        Click(header,"header-timer-start"); time += 1; view.Tick();
        Need(view.Running && Find<TextBlock>(header,"header-timer-value").Text == "01:47", "Header starts the same countdown and updates while counting");
        Click(header,"header-timer-subtract"); Flush(view);
        Need(view.Remaining == 47 && view.Running, "Header subtracts one minute without pausing");
        Click(header,"header-timer-pause"); Click(header,"header-timer-add");
        Need(view.Remaining == 107 && !view.Running, "Header pause and addition synchronize main timer");
        view.AdjustClock(-1000); Need(view.Remaining == 0, "Subtract never produces negative remaining time");
        view.AdjustClock(108); header.Stop(); headerWindow.Close();
        Click(view, "timer-start"); time += 2; view.Tick();
        Click(view, "timer-add-0"); Flush(view); Need(view.Running && view.Remaining == 166, "Add without pausing");
        string priorOutput = File.ReadAllText(settings.Output); view.WritesSuspended = true; time += 2; view.Tick(); Flush(view);
        Need(view.Running && File.ReadAllText(settings.Output) == priorOutput, "Backup suspension preserves TXT while clock continues");
        view.WritesSuspended = false; Flush(view); Need(File.ReadAllText(settings.Output) != priorOutput, "Latest output written when backup releases suspension");
        Find<TextBox>(view, "timer-initial").Text = "00:00:03"; Click(view, "timer-reset"); Flush(view);
        Find<ComboBox>(view, "timer-format").SelectedIndex = 1; Flush(view);
        Need(File.ReadAllText(settings.Output) == "00:00:03", "Hour format");
        Find<TextBox>(view, "timer-message").Text = "FINAL"; Click(view, "timer-start"); time += 4; view.Tick(); Flush(view);
        Need(!view.Running && File.ReadAllText(settings.Output) == "FINAL", "Finished message");
        Find<TextBox>(view, "timer-message").Text = "FIM"; Flush(view);
        Need(File.ReadAllText(settings.Output) == "FIM", "TXT shortened without leftover bytes");
        Find<TextBox>(view, "timer-initial").Text = "bad duration"; double previous = view.Remaining; Click(view, "timer-start");
        Need(!view.Running && view.Remaining == previous, "Invalid duration does not reset clock");
        Find<TextBox>(view, "timer-initial").Text = "24:00:00"; Click(view, "timer-reset"); Flush(view);
        Need(view.Remaining == 86400, "24 hour limit");
        string second = Path.Combine(AppPaths.TextDirectory, "second-timer.txt"); Find<TextBox>(view, "timer-output").Text = second; Flush(view);
        Need(File.ReadAllText(second) == "24:00:00", "Changed output path");
        Find<TextBox>(view, "timer-output").Text = AppPaths.TextDirectory; Flush(view);
        Need(Find<TextBlock>(view, "timer-status").Text!.StartsWith(L.T("Não foi possível gravar o TXT: ")), "Write failure reported");
        Need(File.ReadAllText(second) == "24:00:00", "Failed destination preserves previous file");
        Find<TextBox>(view, "timer-output").Text = second; Flush(view);
        Find<TextBox>(view, "casting-name-0").Text = "  José Caster  "; Find<TextBox>(view, "casting-handle-0").Text = "@@caster";
        Click(view, "casting-apply"); Wait(view.Applying);
        Need(File.ReadAllText(Path.Combine(AppPaths.TextDirectory, "narrador.txt")) == "José Caster", "Caster TXT unicode and trimmed");
        Need(File.ReadAllText(Path.Combine(AppPaths.TextDirectory, "@narrador.txt")) == "@caster", "Handle normalized");
        Need(!File.ReadAllBytes(Path.Combine(AppPaths.TextDirectory, "narrador.txt")).Take(3).SequenceEqual(new byte[] { 239, 187, 191 }), "UTF8 without BOM");
        Need(CastingFiles.Keys.All(k => File.Exists(Path.Combine(AppPaths.TextDirectory, k + ".txt")) && File.Exists(Path.Combine(AppPaths.TextDirectory, "@" + k + ".txt"))), "Eight output files");
        view.Save(true); var saved = ConfigurationStore.Read<Settings>("settings.json");
        Need(ConfigurationStore.Read<CastingSettings>("casting-settings.json").People[0].Name == "  José Caster  ", "Participant configuration retained");
        Need(saved.Initial == 1440 && saved.Output == second && saved.Format == "hh:mm:ss", "Timer settings saved");
        Find<TextBox>(view, "timer-initial").Text = "invalid"; view.Save(); Need(settings.Initial == 1440, "Invalid input keeps saved duration");
        foreach (var size in new[] { new Size(980, 640), new Size(1280, 800) }) {
            window.Width = size.Width; window.Height = size.Height; window.Measure(size); window.Arrange(new Rect(size)); Dispatcher.UIThread.RunJobs();
            Need(Find<Button>(view, "timer-start").Bounds.Width > 0, "Timer controls laid out");
            using var image = new RenderTargetBitmap(new PixelSize((int)size.Width, (int)size.Height), new Vector(96, 96)); image.Render(view);
            image.Save(Path.Combine(AppPaths.DataDirectory, $"avalonia-texts-{size.Width}.png"));
        }
        view.Stop(); Flush(view); window.Close();
        // Recovery preserves the bad file before loading defaults.
        string corrupt = AppPaths.ConfigFile("test-invalid.json"); File.WriteAllText(corrupt, "{not JSON");
        ConfigurationStore.Read<Settings>("test-invalid.json");
        Need(File.ReadAllText(corrupt) == "{not JSON" && Directory.GetFiles(Path.GetDirectoryName(corrupt)!, "test-invalid.json.invalid-*").Length == 1, "Malformed settings preserved");
        var invalid = new Settings { Language = "unknown", Initial = -1, Presets = [-2, 2, 2000], Format = "bad" }; ConfigurationStore.Normalize(invalid);
        Need(invalid.Language == "pt-BR" && invalid.Initial == 0 && invalid.Presets.SequenceEqual(new decimal[] { 2 }) && invalid.Format == "mm:ss", "Configuration normalization");
        var shell = new StudioShell(); shell.Show(); Dispatcher.UIThread.RunJobs(); shell.Navigate(2); Dispatcher.UIThread.RunJobs();
        Need(shell.GetVisualDescendants().OfType<TextsView>().Any(), "Top navigation to texts");
        Click(shell.Texts, "timer-start"); shell.Navigate(0); Need(shell.Texts.Running, "Navigation keeps countdown running");
        Find<TextBox>(shell.Texts, "casting-name-1").Text = "Close safely";
        shell.Navigate(2); Dispatcher.UIThread.RunJobs(); Click(shell.Texts, "casting-apply"); shell.Close();
        Wait(shell.Texts.Applying); Wait(shell.Texts.Writes); Dispatcher.UIThread.RunJobs();
        Need(!shell.IsVisible && File.ReadAllText(Path.Combine(AppPaths.TextDirectory, "comentarista.txt")) == "Close safely", "Closing waits for pending export");
        File.WriteAllText(Path.Combine(AppPaths.DataDirectory, "avalonia-texts-check.txt"), $"PASS: {count} checks for countdown, output, casting, persistence, configuration recovery and navigation. Headless only.");
    }
}

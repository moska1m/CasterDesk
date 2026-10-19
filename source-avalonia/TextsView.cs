using System.Text;
using System.Text.Json;
using Avalonia.Platform.Storage;

namespace CasterDesk.Avalonia;

public sealed class TextsView : Grid
{
    readonly Settings settings;
    readonly CastingSettings casting;
    readonly System.Action saveSettings;
    readonly Countdown clock;
    readonly DispatcherTimer pulse = new() { Interval = TimeSpan.FromMilliseconds(250) };
    readonly TextBox[] names = new TextBox[4], handles = new TextBox[4];
    readonly TextBox folder = new(), initial = new(), message = new(), output = new();
    readonly ComboBox format = Ui.Combo("mm:ss", "hh:mm:ss");
    readonly TextBlock readout = Ui.Text(""), status = Ui.Text(""), castingStatus = Ui.Text(""), clockState = Ui.Text("");
    readonly List<Border> cards = [];
    readonly List<TextBox> presetInputs = [];
    bool durationEdited, live, stopped;
    bool writesSuspended;
    public bool WritesSuspended { get => writesSuspended; set { writesSuspended = value; if (!value) Update(); } }
    string? lastPath, lastText;
    long retry;
    (string Path, string Text)? pending;
    Task writing = Task.CompletedTask;
    internal Task Writes => writing;
    internal Task Applying { get; private set; } = Task.CompletedTask;
    public bool Busy => !writing.IsCompleted || !Applying.IsCompleted;
    public bool Running => clock.Running;
    internal double Remaining => clock.Remaining;
    double InitialSeconds { get { try { return Parse(initial.Text ?? ""); } catch (InvalidDataException) { return (double)settings.Initial * 60; } } }
    internal object TimerOptions => new { initial = InitialSeconds, format = format.SelectedIndex == 1 ? "hh:mm:ss" : "mm:ss", message = message.Text ?? "", presets = settings.Presets, output = output.Text ?? "", folder = OutputFolder(output.Text ?? ""), readout = readout.Text, status = clockState.Text };
    internal void ResetClock() { clock.Reset(Parse(initial.Text ?? "")); durationEdited = false; Update(); }
    internal void ConfigureTimer(double seconds, string outputFormat, string finalMessage, double[] presets) {
        if (!double.IsFinite(seconds) || seconds < 0 || seconds > 86400 || outputFormat is not ("mm:ss" or "hh:mm:ss") || finalMessage.Length > 500 || presets.Length != settings.Presets.Length || presets.Any(p => !double.IsFinite(p) || p <= 0 || p > 1440)) throw new InvalidDataException(L.T("Tempo inválido. Use hh:mm:ss."));
        bool changed = InitialSeconds != seconds;
        initial.Text = DurationText(seconds); durationEdited = changed || durationEdited;
        format.SelectedIndex = outputFormat == "hh:mm:ss" ? 1 : 0; message.Text = finalMessage;
        for (int i = 0; i < presets.Length; i++) {
            settings.Presets[i] = (decimal)presets[i];
            presetInputs[i].Text = settings.Presets[i].ToString();
        }
        Save(true); Update();
    }
    internal event Action? ClockChanged;
    internal void PauseClock() { clock.Pause(); Update(); }
    internal void StartFromHeader() { try { Start(); } catch(Exception ex) { status.Text = ex.Message; } }
    internal void AdjustClock(double seconds) { clock.Add(Math.Max(-clock.Remaining, seconds)); clock.Tick(); Update(); }
    internal void SetClock(double seconds) {
        if(!double.IsFinite(seconds)||seconds<0||seconds>86400)throw new InvalidDataException(L.T("Tempo inválido. Use hh:mm:ss."));
        bool running=clock.Running; initial.Text=DurationText(seconds); clock.Reset(seconds); if(running)clock.Start(); durationEdited=false; Update();
    }

    public TextsView(Settings settings, System.Action saveSettings, Countdown? clock = null)
    {
        this.settings = settings; this.saveSettings = saveSettings; this.clock = clock ?? new Countdown();
        ConfigurationStore.Normalize(settings);
        casting = ConfigurationStore.Read<CastingSettings>("casting-settings.json"); ConfigurationStore.Normalize(casting);
        Background = Ui.Canvas; Margin = new Thickness(10); ColumnDefinitions = new("*,*");
        var left = new StackPanel(); var right = new StackPanel();
        Children.Add(new ScrollViewer { Content = left });
        var rightScroll = new ScrollViewer { Content = right }; Children.Add(rightScroll); SetColumn(rightScroll, 1);
        var people = new UniformGrid { Columns = 2 };
        string[] labels = ["NARRADOR / CASTER", "COMENTARISTA", "CONVIDADO 1", "CONVIDADO 2"];
        for (int n = 0; n < 4; n++) {
            var person = new StackPanel(); person.Children.Add(Ui.Text("Nome"));
            names[n] = Input("casting-name-" + n, casting.People[n].Name); person.Children.Add(names[n]);
            person.Children.Add(Ui.Text("@")); handles[n] = Input("casting-handle-" + n, casting.People[n].Handle); person.Children.Add(handles[n]);
            person.Children.Add(Ui.Text(CastingFiles.Keys[n] + ".txt / @" + CastingFiles.Keys[n] + ".txt"));
            people.Children.Add(Card(labels[n], person));
        }
        left.Children.Add(people);
        var files = new StackPanel(); folder.Name = "casting-folder"; folder.Text = casting.Folder; files.Children.Add(folder);
        files.Children.Add(Ui.Button("Escolher pasta", async () => {
            try { var paths = await Ui.Owner(this).StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = L.T("Escolher pasta"), AllowMultiple = false });
                if (paths.FirstOrDefault()?.TryGetLocalPath() is string path) folder.Text = path;
            } catch (Exception ex) { castingStatus.Text = ex.Message; }
        }));
        var apply = Ui.Button("Aplicar no OBS", () => { if (Applying.IsCompleted) Applying = ApplyCasting(); }, true); apply.Name = "casting-apply";
        files.Children.Add(apply); files.Children.Add(castingStatus); left.Children.Add(Card("PASTA DOS OITO ARQUIVOS TXT", files));

        var timer = new StackPanel(); timer.Children.Add(Ui.Text("Tempo inicial:")); initial.Name = "timer-initial";
        initial.Text = DurationText((double)settings.Initial * 60); timer.Children.Add(initial);
        timer.Children.Add(Ui.Text("Formato de saída:")); format.Name = "timer-format"; format.SelectedIndex = settings.Format == "hh:mm:ss" ? 1 : 0; timer.Children.Add(format);
        timer.Children.Add(Ui.Text("Mensagem final:")); message.Name = "timer-message"; message.Text = settings.Message; timer.Children.Add(message);
        readout.Name = "timer-readout"; readout.FontSize = 40; readout.TextAlignment = TextAlignment.Center; timer.Children.Add(readout); timer.Children.Add(clockState);
        var actions = new WrapPanel();
        actions.Children.Add(Command("Iniciar / Retomar", "timer-start", Start, true));
        actions.Children.Add(Command("Aplicar tempo", "timer-apply-time", ()=>SetClock(Parse(initial.Text??"")), true));
        actions.Children.Add(Command("Pausar", "timer-pause", () => { this.clock.Pause(); Update(); }));
        actions.Children.Add(Command("Resetar tempo", "timer-reset", () => { this.clock.Reset(Parse(initial.Text ?? "")); durationEdited = false; Update(); })); timer.Children.Add(actions);
        timer.Children.Add(Ui.Text("Acrescentar tempo sem pausar", true));
        var additions = new WrapPanel();
        for (int n = 0; n < settings.Presets.Length; n++) {
            int index = n; var field = Input("timer-preset-" + n, settings.Presets[n].ToString()); field.Width = 65;
            presetInputs.Add(field);
            var button = Command($"+ {settings.Presets[n]} min", "timer-add-" + n, () => {
                if (!decimal.TryParse(field.Text, out decimal minutes) || minutes <= 0 || minutes > 1440) throw new InvalidDataException(L.T("Use um valor de minutos entre 0 e 1440."));
                settings.Presets[index] = minutes; this.clock.Add((double)minutes * 60); Update();
            });
            field.TextChanged += (_, _) => { if (decimal.TryParse(field.Text, out decimal minutes) && minutes > 0 && minutes <= 1440) { settings.Presets[index] = minutes; button.Content = $"+ {minutes} min"; } };
            additions.Children.Add(button); additions.Children.Add(field);
        }
        timer.Children.Add(additions); right.Children.Add(Card("CRONÔMETRO", timer));
        var target = new StackPanel(); output.Name = "timer-output"; output.Text = settings.Output; target.Children.Add(output);
        target.Children.Add(Ui.Button("Escolher TXT", async () => { try { if (await Ui.PickFile(this, "Escolher TXT", "txt", true, "timer.txt") is string path) output.Text = path; } catch (Exception ex) { status.Text = ex.Message; } }));
        target.Children.Add(Command("Copiar caminho", "timer-copy", () => Ui.CopyText(OutputFolder(output.Text ?? "")))); target.Children.Add(status);
        status.Name = "timer-status"; right.Children.Add(Card("Arquivo de saída • OBS", target));
        this.clock.Reset((double)settings.Initial * 60); Update();
        initial.TextChanged += (_, _) => durationEdited = true;
        format.SelectionChanged += (_, _) => Update(); message.TextChanged += (_, _) => Update();
        output.TextChanged += (_, _) => { retry = 0; lastPath = null; Update(); };
        pulse.Tick += (_, _) => Tick();
    }

    static TextBox Input(string name, string value) => new() { Name = name, Text = value, Margin = new Thickness(0, 0, 6, 8) };
    internal static string OutputFolder(string path) {
        try { return string.IsNullOrWhiteSpace(path) ? "" : Path.GetDirectoryName(Path.GetFullPath(path)) ?? ""; }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) { return ""; }
    }
    Border Card(string title, Control content) { var card = Ui.Card(title, content); cards.Add(card); return card; }
    public void RefreshColors() { Background = Ui.Canvas; foreach (var card in cards) { card.Background = Ui.Panel; if(card.Child is Grid body)foreach(var title in body.Children.OfType<TextBlock>())title.Background=Ui.Header; } }
    Button Command(string label, string name, System.Action action, bool primary = false) {
        var button = Ui.Button(label, () => { try { action(); } catch (Exception ex) { status.Text = ex.Message; } }, primary); button.Name = name; return button;
    }
    internal static string DurationText(double seconds) { int total = (int)Math.Round(seconds); return $"{total / 3600:00}:{total / 60 % 60:00}:{total % 60:00}"; }
    internal static double Parse(string value) {
        if (value == "24:00:00") return 86400;
        var shortTime=System.Text.RegularExpressions.Regex.Match(value,@"^(\d{1,4}):([0-5]\d)$");
        if(shortTime.Success){double seconds=int.Parse(shortTime.Groups[1].Value)*60+int.Parse(shortTime.Groups[2].Value);if(seconds<=86400)return seconds;}
        if (!TimeSpan.TryParseExact(value, @"hh\:mm\:ss", null, out var duration)) throw new InvalidDataException(L.T("Tempo inválido. Use hh:mm:ss."));
        return duration.TotalSeconds;
    }
    internal void Start() { if (durationEdited) { clock.Reset(Parse(initial.Text ?? "")); durationEdited = false; } clock.Start(); Update(); }
    internal void Tick() { clock.Tick(); Update(); }
    public void StartLive() { if (stopped) return; live = true; pulse.Start(); Update(); }
    public void Stop() { stopped = true; live = false; pulse.Stop(); }
    void Update() {
        int seconds = (int)Math.Ceiling(clock.Remaining);
        string text = clock.Finished ? message.Text ?? "" : format.SelectedIndex == 1 ? DurationText(seconds) : $"{seconds / 60:00}:{seconds % 60:00}";
        readout.Text = text; clockState.Text = L.T(clock.Running ? "● Em andamento" : clock.Finished ? "Contagem encerrada" : "Pausado / pronto");
        ClockChanged?.Invoke();
        string path = output.Text ?? "";
        if (!live || stopped || writesSuspended || (path == lastPath && text == lastText) || Environment.TickCount64 < retry) return;
        pending = (path, text); if (writing.IsCompleted) writing = WritePending();
    }
    async Task WritePending() {
        while (pending is { } request) {
            pending = null;
            try {
                await Task.Run(() => { lock (FileBatch.Gate) {
                    using var file = new FileStream(request.Path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                    byte[] bytes = new UTF8Encoding(false).GetBytes(request.Text); file.Write(bytes); file.SetLength(bytes.Length); file.Flush();
                } });
                lastPath = request.Path; lastText = request.Text; retry = 0;
                status.Text = L.T("TXT atualizado • ") + DateTime.Now.ToString("HH:mm:ss");
            } catch (Exception ex) { retry = Environment.TickCount64 + 1000; status.Text = L.T("Não foi possível gravar o TXT: ") + ex.Message; pending = null; }
        }
    }
    CastingSettings CaptureCasting() => new() { Folder = folder.Text ?? "", People = Enumerable.Range(0, 4).Select(n => new Talent { Name = names[n].Text ?? "", Handle = handles[n].Text ?? "" }).ToArray() };
    async Task ApplyCasting() {
        if (!Applying.IsCompleted) return;
        var snapshot = CaptureCasting();
        try {
            await Task.Run(() => { CastingFiles.Write(snapshot.Folder, snapshot.People, AppPaths.ConfigFile("casting-settings.json"), JsonSerializer.Serialize(snapshot)); });
            castingStatus.Text = L.T("TXT atualizado • ") + DateTime.Now.ToString("HH:mm:ss");
        } catch (Exception ex) { castingStatus.Text = ex.Message; }
    }
    public void Save(bool validateTimer = false) {
        if (!Applying.IsCompleted) throw new InvalidOperationException(L.T("Salvando arquivos…"));
        decimal duration = settings.Initial;
        try { duration = (decimal)Parse(initial.Text ?? "") / 60; }
        catch (InvalidDataException) { if (validateTimer) throw; status.Text = L.T("Tempo inválido: mantida a duração salva anteriormente."); }
        MatchFiles.AtomicText(AppPaths.ConfigFile("casting-settings.json"), JsonSerializer.Serialize(CaptureCasting()));
        settings.Initial = duration; settings.Format = format.SelectedIndex == 1 ? "hh:mm:ss" : "mm:ss";
        settings.Message = message.Text ?? ""; settings.Output = output.Text ?? ""; saveSettings();
    }
}

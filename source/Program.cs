using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace CronoLive;

public sealed class Countdown
{
    private readonly Func<double> now;
    private double remaining, started;
    public bool Running { get; private set; }
    public bool Finished { get; private set; }
    public Countdown(Func<double>? clock = null) => now = clock ?? (() => (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency);
    public double Remaining => Math.Max(0, remaining - (Running ? now() - started : 0));
    public void Reset(double seconds) { remaining = seconds; Running = false; Finished = false; }
    public void Start() { if (!Running && Remaining > 0) { started = now(); Running = true; Finished = false; } }
    public void Pause() { remaining = Remaining; Running = false; }
    public void Add(double seconds) { remaining = Remaining + seconds; started = now(); Finished = false; }
    public void Tick() { if (Running && Remaining <= 0) { remaining = 0; Running = false; Finished = true; } }
}

public sealed class Settings
{
    public bool DarkMode { get; set; } = true;
    public bool SiegeTheme { get; set; } = true;
    public decimal Initial { get; set; } = 10;
    public decimal[] Presets { get; set; } = [2, 3, 4];
    public string Format { get; set; } = "mm:ss";
    public string Message { get; set; } = "TEMPO ENCERRADO";
    public string Output { get; set; } = Path.Combine(AppPaths.DataDirectory, "timer.txt");
}

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Contains("--self-test"))
        {
            double t = 0;
            var c = new Countdown(() => t);
            c.Reset(60); c.Start(); t = 12.5; c.Add(120);
            Check(c.Running && c.Remaining == 167.5, "addition while running");
            t = 20; c.Pause(); t = 100; Check(c.Remaining == 160, "pause");
            c.Start(); t = 260; c.Tick(); Check(c.Finished && !c.Running && c.Remaining == 0, "finish");
            c.Add(60); Check(!c.Running && !c.Finished && c.Remaining == 60, "add after finish");
            Check(MainForm.Format(3661, "mm:ss") == "61:01", "total minutes");
            Check(MainForm.Format(3661, "hh:mm:ss") == "01:01:01", "hours");
            Check(DurationInput.TrySeconds("01:02:03", out int duration) && duration == 3723, "duration hours minutes seconds");
            Check(!DurationInput.TrySeconds("00:60:00", out _) && !DurationInput.TrySeconds("00:00:60", out _), "invalid minutes and seconds");
            Check(DurationInput.TrySeconds("24:00:00", out duration) && duration == 86400, "24 hour duration");
            c.Reset(600); c.Start(); t += 12; c.Reset(600);
            Check(!c.Running && !c.Finished && c.Remaining == 600, "reset to initial paused");
            string testFolder = Path.Combine(Path.GetTempPath(), "CronoLive-test-" + Guid.NewGuid().ToString("N"));
            Talent[] people = [new() { Name = "João", Handle = "moska1m" }, new(), new(), new()];
            CastingFiles.Write(testFolder, people);
            Check(Directory.GetFiles(testFolder).Length == 8, "eight casting files");
            Check(File.ReadAllText(Path.Combine(testFolder, "narrador.txt")) == "João", "accented name");
            Check(File.ReadAllText(Path.Combine(testFolder, "@narrador.txt")) == "@moska1m", "handle prefix");
            Check(CastingFiles.Handle(" @moska1m ") == "@moska1m", "no duplicate prefix");
            people[0] = new(); CastingFiles.Write(testFolder, people);
            Check(new FileInfo(Path.Combine(testFolder, "narrador.txt")).Length == 0 && new FileInfo(Path.Combine(testFolder, "@narrador.txt")).Length == 0, "clear unused participant");
            File.WriteAllText(Path.Combine(AppPaths.DataDirectory, "test-result.txt"), "15 verificações aprovadas.");
            return;
        }
        ApplicationConfiguration.Initialize();
        if(args.Contains("--preview-about"))
        {
            foreach(bool dark in new[]{true,false})
            {
                AppTheme.Dark=dark;using var dialog=new AboutDialog();dialog.Show();Application.DoEvents();
                using var image=new Bitmap(dialog.Width,dialog.Height);dialog.DrawToBitmap(image,new Rectangle(Point.Empty,dialog.Size));
                image.Save(Path.Combine(AppPaths.DataDirectory,$"about-{dark}.png"));dialog.Close();
            }
            return;
        }
        if(args.Contains("--championship-test")) { ChampionshipTests.Run(); return; }
        if(args.Contains("--preview-championship"))
        {
            using var form=new Form {Text="CronoLive",ClientSize=new Size(1240,900)};
            form.Controls.Add(new ChampionshipPanel(true));form.Show();Application.DoEvents();
            using var bitmap=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bitmap,new Rectangle(0,0,form.Width,form.Height));
            bitmap.Save(Path.Combine(AppPaths.DataDirectory,"championship-preview.png"));return;
        }
        if(args.Contains("--preview-app"))
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);using var form=new MainForm();form.StopPreviewTimer();form.Show();Application.DoEvents();
            var tabs=form.Controls.OfType<Panel>().SelectMany(p=>p.Controls.OfType<TabControl>()).Single();
            var size=form.Size;
            for(int theme=0;theme<4;theme++) {
            form.SetAppearance(theme<2,theme%2==0);
            for(int i=0;i<tabs.TabCount;i++) {
                tabs.SelectedIndex=i;Application.DoEvents();if(form.Size!=size)throw new Exception("Tab changed window size");
                var inner=tabs.TabPages[0].Controls.OfType<ChampionshipPanel>().Single().Controls.OfType<TabControl>().Single();
                int count=i==0?inner.TabCount:1;
                for(int j=0;j<count;j++){if(i==0)inner.SelectedIndex=j;Application.DoEvents();if(i==0 && j==2)inner.TabPages[2].Controls.OfType<ImageExportPanel>().Single().WaitForPreview();using var image=new Bitmap(form.Width,form.Height);form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(Path.Combine(AppPaths.DataDirectory,$"app-preview-{theme}-{i}-{j}.png"));}
            }}
            return;
        }
        if(args.Contains("--theme-restart-test"))
        {
            string path=Path.Combine(AppPaths.DataDirectory,"settings.json");string? original=File.Exists(path)?File.ReadAllText(path):null;
            try {
                using(var form=new MainForm()){
                    form.Show();form.SetAppearance(true,true);
                    var bar=form.Controls.OfType<NavigationBar>().Single();var menu=bar.SettingsButton!.ContextMenuStrip!;
                    ((ToolStripMenuItem)menu.Items[6]).PerformClick();
                    if(!AppTheme.Dark)throw new Exception("Theme changed before restart");
                    var saved=JsonSerializer.Deserialize<Settings>(File.ReadAllText(path))!;if(saved.DarkMode)throw new Exception("Theme preference not saved");form.Close();
                }
                using var restarted=new MainForm();if(AppTheme.Dark)throw new Exception("Theme not applied at restart");
                File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"theme-restart-test.txt"),"Preferência salva sem mudar a tela; novo tema aplicado na reabertura.");
            }finally{if(original is not null)File.WriteAllText(path,original);else File.Delete(path);}return;
        }
        if(args.Contains("--timer-ui-test"))
        {
            using var form=new MainForm();using var check=new System.Windows.Forms.Timer{Interval=3200};
            form.Shown+=(_,_)=>{form.VerifyTimerStart();check.Start();};
            check.Tick+=(_,_)=>{check.Stop();form.VerifyTimerEnd();form.Close();};Application.Run(form);
            File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"timer-ui-test.txt"),"Timer parado sem polling; contagem de 2s finalizada, TXT final gravado e polling encerrado.");return;
        }
        if(args.Contains("--perf-test"))
        {
            AppTheme.Dark=true;AppTheme.Siege=true;
            using var form=new Form{ClientSize=new Size(1240,900)};var panel=new ChampionshipPanel(true);form.Controls.Add(panel);form.Show();Application.DoEvents();
            File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"performance.txt"),panel.MeasureRefresh());return;
        }
        if(args.Contains("--theme-smoke"))
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            using var form=new MainForm();
            var tabs=form.Controls.OfType<Panel>().SelectMany(p=>p.Controls.OfType<TabControl>()).Single();
            var inner=tabs.TabPages[0].Controls.OfType<ChampionshipPanel>().Single().Controls.OfType<TabControl>().Single();
            var gear=form.Controls.OfType<NavigationBar>().Single().SettingsButton!;
            using var timer=new System.Windows.Forms.Timer {Interval=600};int step=0;
            timer.Tick+=(_,_)=>{if(step==8){timer.Stop();form.Close();return;}int mode=step/2;form.SetAppearance(mode<2,mode%2==0);tabs.SelectedIndex=step%2;inner.SelectedIndex=2;if(step==0){gear.PerformClick();gear.ContextMenuStrip!.Close();}step++;};
            timer.Start();Application.Run(form);
            if(step!=8)throw new Exception("Theme navigation failed");
            File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"theme-smoke.txt"),"4 temas, abas e engrenagem verificados com o cronômetro ativo.");return;
        }
        if(args.Contains("--export-test")){ExportTests.Run();return;}
        AppLog.Install();
        try{Application.Run(new MainForm());}
        catch(Exception ex){AppLog.Write("STARTUP ERROR "+ex);MessageBox.Show("Não foi possível abrir o CasterDesk. Consulte a pasta Logs dos dados do aplicativo.\n"+ex.Message,"CasterDesk");}
    }
    static void Check(bool condition, string name) { if (!condition) throw new Exception(name); }
}

public sealed class MainForm : Form
{
    readonly Countdown clock = new();
    readonly string settingsPath = Path.Combine(AppPaths.DataDirectory, "settings.json");
    Settings settings = new();
    readonly TextBox preview = new() { ReadOnly = true, TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(220, 232, 244), Width = 300 };
    readonly Label status = new() { AutoSize = true };
    readonly Label writeStatus = new() { AutoSize = true, MaximumSize = new Size(660, 0) };
    readonly DurationInput initial = new();
    readonly ComboBox format = new ThemeCombo() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 125 };
    readonly TextBox finalMessage = new() { Width = 420, TextAlign = HorizontalAlignment.Center };
    readonly TextBox output = new() { Width = 530, ReadOnly = true };
    readonly System.Windows.Forms.Timer pulse = new() { Interval = 1000 };
    string? lastText;
    long retryAfter;
    public MainForm()
    {
        Text = "CasterDesk • 0.1.0-beta.2";
        var area=Screen.PrimaryScreen!.WorkingArea;
        Size = new Size(Math.Min(1280,area.Width),Math.Min(990,area.Height)); MinimumSize = new Size(1140,760);
        StartPosition=FormStartPosition.CenterScreen;WindowState=FormWindowState.Maximized;
        BackColor = SystemColors.Control; ForeColor = SystemColors.ControlText;
        Font = new Font("Segoe UI", 9); AutoScaleMode = AutoScaleMode.Dpi;
        try { settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsPath)) ?? new(); } catch { }
        AppTheme.Dark=settings.DarkMode;AppTheme.Siege=settings.SiegeTheme;
        var tabs = new ThemeTabs { Dock = DockStyle.Fill, Padding = new Point(12, 5) };
        var casting = new CastingPanel();
        var championship=new ChampionshipPanel();
        var championshipPage=new TabPage("Campeonato"){BackColor=Color.White};
        championshipPage.Controls.Add(championship);tabs.TabPages.Add(championshipPage);
        var textsPage=new TabPage("Textos para OBS"){BackColor=Color.White,Padding=new Padding(8),AutoScroll=true};
        tabs.TabPages.Add(textsPage);
        var split=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,MinimumSize=new Size(1090,500)};
        split.ColumnStyles.Add(new(SizeType.Percent,50));split.ColumnStyles.Add(new(SizeType.Percent,50));
        var castingGroup=new ThemeGroup {Text="CASTING",Dock=DockStyle.Fill,Padding=new Padding(8)};
        castingGroup.Controls.Add(casting);split.Controls.Add(castingGroup,0,0);
        var page=new ThemeGroup {Text="CRONÔMETRO",Dock=DockStyle.Fill,Padding=new Padding(8)};
        split.Controls.Add(page,1,0);textsPage.Controls.Add(split);
        var footer = new StatusStrip();
        var footerLabel = new ToolStripStatusLabel("CasterDesk • Ferramentas para community casters");
        footer.Items.Add(footerLabel);
        var host = new Panel { Dock = DockStyle.Fill, Padding = Padding.Empty };
        host.Controls.Add(tabs); Controls.Add(host); Controls.Add(footer);
        var themeBar=new NavigationBar(tabs,true);Controls.Add(themeBar);
        var gear=themeBar.SettingsButton!;
        var menu=new ContextMenuStrip();gear.ContextMenuStrip=menu;
        var siege=new ToolStripMenuItem("Siege");var simple=new ToolStripMenuItem("Simplificado");
        var dark=new ToolStripMenuItem("Escuro");var light=new ToolStripMenuItem("Claro");
        menu.Items.Add(new ToolStripMenuItem("TEMÁTICA") {Enabled=false});menu.Items.AddRange([siege,simple,new ToolStripSeparator(),new ToolStripMenuItem("APARÊNCIA"){Enabled=false},dark,light]);
        menu.Items.Add(new ToolStripSeparator());menu.Items.Add(new ToolStripMenuItem("Aplicar na próxima abertura"){Enabled=false});
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Sobre o CasterDesk",null,(_,_)=>{using var dialog=new AboutDialog();dialog.ShowDialog(this);}));
        void ChangeTheme(bool style,bool isDark)
        {
            settings.SiegeTheme=style;settings.DarkMode=isDark;Save();
            footerLabel.Text=(style!=AppTheme.Siege || isDark!=AppTheme.Dark)?"Tema salvo • será aplicado na próxima abertura do CasterDesk.":"CasterDesk • Ferramentas para community casters";
        }
        siege.Click+=(_,_)=>ChangeTheme(true,settings.DarkMode);simple.Click+=(_,_)=>ChangeTheme(false,settings.DarkMode);
        dark.Click+=(_,_)=>ChangeTheme(settings.SiegeTheme,true);light.Click+=(_,_)=>ChangeTheme(settings.SiegeTheme,false);
        gear.Click+=(_,_)=>{siege.Checked=settings.SiegeTheme;simple.Checked=!settings.SiegeTheme;dark.Checked=settings.DarkMode;light.Checked=!settings.DarkMode;menu.BackColor=AppTheme.Surface;menu.ForeColor=AppTheme.Text;menu.Show(gear,new Point(gear.Width-menu.PreferredSize.Width,gear.Height));};
        FormClosed+=(_,_)=>menu.Dispose();
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, Height=400, ColumnCount = 1, RowCount = 3 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 168));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        page.Controls.Add(layout);
        var countdown = new ThemeGroup { Text = "Contagem regressiva", Dock = DockStyle.Fill };
        var additions = new ThemeGroup { Text = "Acrescentar tempo sem pausar", Dock = DockStyle.Fill };
        var fileGroup = new ThemeGroup { Text = "Arquivo de saída • OBS", Dock = DockStyle.Fill };
        layout.Controls.Add(countdown, 0, 0); layout.Controls.Add(additions, 0, 1); layout.Controls.Add(fileGroup, 0, 2);
        void Place(Control parent, Control child, int x, int y, int width) { child.Location = new Point((int)(x * 0.83), y); child.Width = (int)(width * 0.83); parent.Controls.Add(child); }
        void Caption(Control parent, string text, int x, int y, int width) => Place(parent, new Label { Text = text, AutoSize = false, Height = 23, TextAlign = ContentAlignment.MiddleLeft }, x, y, width);
        initial.Value = Math.Clamp(decimal.Round(settings.Initial * 60), initial.Minimum, initial.Maximum);
        Caption(countdown, "Tempo inicial:", 12, 24, 135);
        Place(countdown, initial, 150, 24, 160);
        Place(countdown, Button("Resetar tempo", () => { if (clock.Running && MessageBox.Show(this, "Substituir a contagem atual e deixá-la pausada?", "Resetar tempo", MessageBoxButtons.YesNo) != DialogResult.Yes) return; clock.Reset((double)initial.Value); RefreshOutput(); }), 320, 22, 140);
        format.Items.AddRange(["mm:ss", "hh:mm:ss"]); format.SelectedItem = settings.Format; if (format.SelectedIndex < 0) format.SelectedIndex = 0;
        Caption(countdown, "Formato de saída:", 12, 53, 135); Place(countdown, format, 150, 53, 160);
        Place(countdown, status, 330, 56, 280);
        Caption(countdown, "Saída ao vivo:", 12, 82, 135); Place(countdown, preview, 150, 82, 460);
        finalMessage.Text = settings.Message;
        Caption(countdown, "Mensagem final:", 12, 111, 135); Place(countdown, finalMessage, 150, 111, 460);
        Place(countdown, Button("Iniciar / Retomar", () => { clock.Start(); RefreshOutput(); }), 150, 136, 220);
        Place(countdown, Button("Pausar", () => { clock.Pause(); RefreshOutput(); }), 380, 136, 230);
        var presetInputs = new List<NumericUpDown>();
        for (int i = 0; i < 3; i++)
        {
            var n = Number(settings.Presets.Length > i ? Math.Clamp(settings.Presets[i], 0.01m, 1440m) : i + 2, 1440);
            presetInputs.Add(n);
            var b = Button($"+ {n.Value:0.##} min", () => { clock.Add((double)n.Value * 60); RefreshOutput(); });
            n.ValueChanged += (_, _) => { b.Text = $"+ {n.Value:0.##} min"; settings.Presets = presetInputs.Select(p => p.Value).ToArray(); };
            int x = 150 + i * 156;
            Place(additions, b, x, 24, 148); Place(additions, n, x, 57, 100);
            Caption(additions, "min", x + 105, 57, 40);
        }
        Caption(additions, "Adicionar agora:", 12, 25, 135);
        Caption(additions, "Valor dos botões:", 12, 57, 135);
        output.Text = settings.Output;
        Place(fileGroup, output, 12, 22, 450);
        Place(fileGroup, Button("Escolher TXT", ChooseOutput), 472, 20, 138);
        Place(fileGroup, Button("Copiar caminho", () => Clipboard.SetText(output.Text)), 472, 49, 138);
        Place(fileGroup, writeStatus, 12, 53, 450);
        writeStatus.MaximumSize = new Size(450, 0);
        format.SelectedIndexChanged += (_, _) => RefreshOutput(); finalMessage.TextChanged += (_, _) => RefreshOutput();
        clock.Reset((double)initial.Value); RefreshOutput();
        AppTheme.Apply(this);
        pulse.Tick += (_, _) => { clock.Tick(); RefreshOutput(); };
        FormClosing += (_, e) => { if (clock.Running && MessageBox.Show(this, "Fechar e interromper a contagem? O TXT manterá o último texto.", "Sair", MessageBoxButtons.YesNo) != DialogResult.Yes) { e.Cancel = true; return; } Save(); casting.SaveDraft(); championship.FlushNames(); };
    }
    internal void VerifyTimerStart(){if(pulse.Enabled)throw new Exception("Idle timer is polling");clock.Reset(2);clock.Start();RefreshOutput();if(!pulse.Enabled)throw new Exception("Timer did not start");}
    internal void VerifyTimerEnd(){if(!clock.Finished || pulse.Enabled || File.ReadAllText(output.Text)!=finalMessage.Text)throw new Exception("Timer completion failed");}
    internal void StopPreviewTimer()=>pulse.Stop();
    internal void SetAppearance(bool siege,bool dark)
    {
        settings.SiegeTheme=siege;settings.DarkMode=dark;AppTheme.Siege=siege;AppTheme.Dark=dark;
        foreach(Form open in Application.OpenForms)AppTheme.Apply(open);
    }
    protected override void Dispose(bool disposing){if(disposing)pulse.Dispose();base.Dispose(disposing);}
    static NumericUpDown Number(decimal value, decimal max) => new() { Minimum = 0.01m, Maximum = max, DecimalPlaces = 2, Value = value, Width = 105 };
    static FlowLayoutPanel Row(params Control[] controls) { var p = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 7, 0, 0) }; p.Controls.AddRange(controls); return p; }
    static Button Button(string text, Action action, int width = 165) { var b = new ThemeButton { Text = text, Width = width, Height = 25, UseVisualStyleBackColor = true }; b.Click += (_, _) => action(); return b; }
    public static string Format(int seconds, string format) => format == "hh:mm:ss" ? $"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}" : $"{seconds / 60:00}:{seconds % 60:00}";
    void RefreshOutput()
    {
        try {
        string text = clock.Finished ? finalMessage.Text : Format((int)Math.Ceiling(clock.Remaining), format.Text);
        if(preview.Text!=text)preview.Text=text;
        string state=clock.Running ? "● Em andamento" : clock.Finished ? "Contagem encerrada" : "Pausado / pronto";
        if(status.Text!=state)status.Text=state;
        if (text == lastText || Environment.TickCount64 < retryAfter) return;
        try
        {
            using (var file = new FileStream(output.Text, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(text);
                file.Write(bytes); file.SetLength(bytes.Length); file.Flush();
            }
            lastText = text; retryAfter=0; writeStatus.Text = "TXT atualizado • " + DateTime.Now.ToString("HH:mm:ss.fff"); writeStatus.ForeColor = AppTheme.Success;
        }
        catch (Exception ex) { retryAfter = Environment.TickCount64 + 1000; writeStatus.Text = "Não foi possível gravar o TXT: " + ex.Message; writeStatus.ForeColor = AppTheme.Error; }
            }finally{SchedulePulse();}
    }
    void SchedulePulse()
    {
        if(clock.Running){double fraction=clock.Remaining%1; pulse.Interval=Math.Clamp((int)Math.Ceiling((fraction<.001?1:fraction)*1000)+1,15,1001);pulse.Start();}
        else if(retryAfter>0){pulse.Interval=1000;pulse.Start();}
        else pulse.Stop();
    }

    void ChooseOutput()
    {
        using var dialog = new SaveFileDialog { Filter = "Arquivo de texto (*.txt)|*.txt", FileName = "timer.txt", OverwritePrompt = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        output.Text = dialog.FileName; lastText = null; retryAfter = 0; RefreshOutput();
    }
    void Save()
    {
        settings.Initial = initial.Value / 60m; settings.Format = format.Text; settings.Message = finalMessage.Text; settings.Output = output.Text;
        try { File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true })); }
        catch (Exception ex) { MessageBox.Show(this, "Não foi possível salvar as preferências: " + ex.Message); }
    }
}





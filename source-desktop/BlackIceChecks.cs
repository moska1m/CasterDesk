using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace CasterDesk.Desktop;

internal static class BlackIceChecks
{
    static int count;
    static void Need(bool condition, string message) { count++; if (!condition) throw new Exception("Ice review: " + message); }
    static T Find<T>(Control root, string name) where T : Control => root.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);
    static void Scene(Window owner)
    {
        owner.UpdateLayout(); Dispatcher.UIThread.RunJobs(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(2);
        using var frame = owner.CaptureRenderedFrame(); Dispatcher.UIThread.RunJobs();
    }
    static void Capture(Window owner, string name)
    {
        Scene(owner);
        using var image = new RenderTargetBitmap(new PixelSize((int)owner.Width, (int)owner.Height));
        image.Render(owner); image.Save(Path.Combine(AppPaths.DataDirectory, name + ".png"));
    }
    static Point Center(Control control, Window owner) => control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), owner)!.Value;
    static double Luminance(string file,Rect region)
    {
        using var image=new System.Drawing.Bitmap(Path.Combine(AppPaths.DataDirectory,file+".png"));
        double total=0;int pixels=0;
        for(int y=Math.Max(0,(int)region.Y);y<Math.Min(image.Height,(int)region.Bottom);y++)
            for(int x=Math.Max(0,(int)region.X);x<Math.Min(image.Width,(int)region.Right);x++) {
                var color=image.GetPixel(x,y);total+=color.R*.2126+color.G*.7152+color.B*.0722;pixels++;
            }
        return total/Math.Max(1,pixels);
    }
    static void Centered(Button button)
    {
        var text=button.GetVisualDescendants().OfType<TextBlock>().First(t=>!string.IsNullOrEmpty(t.Text));
        var position=text.TranslatePoint(default,button)!.Value;
        var lines=text.TextLayout.TextLines;
        double left=lines.Min(l=>l.Start),right=lines.Max(l=>l.Start+l.Width);
        double x=position.X+text.Padding.Left+(left+right)/2;
        double y=position.Y+text.Padding.Top+text.TextLayout.Height/2;
        Need(Math.Abs(x-button.Bounds.Width/2)<1.25,"visible text horizontally centered: "+text.Text);
        Need(Math.Abs(y-button.Bounds.Height/2)<1.25,"visible text vertically centered: "+text.Text);
    }
    internal static void Run()
    {
        Need(Program.BlackIcePreview, "review requires reserved preview flag");
        MatchFiles.AtomicText(AppPaths.ConfigFile("settings.json"), System.Text.Json.JsonSerializer.Serialize(new Settings { Language = "pt-BR", VisualTheme = "standard" }));
        var session = new MatchSession(); session.Configure("NA ORG", "FAZE", "MD3", 0, 0);
        for (int step = 0; step < 8; step++) session.Veto(step, session.Catalog.Maps[step].Id);
        var map = session.Map!; session.Side(map.MapId, false, 0); session.Side(map.MapId, true, 1);
        void Bans() {
            foreach (int slot in session.PendingBans()) {
                var choice = session.Catalog.Operators.First(o => o.Role == map.BanRole(slot) && !map.Bans.Contains(o.Id));
                session.Ban(slot, choice.Id);
            }
        }
        Bans();
        foreach (int winner in new[] { 0, 1, 0, 1, 0, 1, 0 }) {
            Bans();
            string site = session.Catalog.Maps.Single(m => m.Id == map.MapId).Sites.First(s => !map.SiteBlocked(s, map.Rounds.Count + 1));
            session.Round(site, winner, "");
        }
        var shell = new StudioShell(session) { WindowState = WindowState.Normal, Width = 1280, Height = 800 };
        shell.Show(); Scene(shell);
        try {
            Need(Ui.BlackIce && BlackIceTheme.HasTexture, "hidden visual loads its local material");
            Need(shell.GetVisualDescendants().OfType<Control>().Any(c=>c.Name=="ice-window-material"),"continuous structural ice behind workspace");
            Need(Find<Grid>(shell,"brand-plaque").GetVisualDescendants().OfType<IceSurface>().Any(),"brand has its own raised plaque");
            Need(ConfigurationStore.Read<Settings>("settings.json").VisualTheme == "standard", "review does not persist the hidden theme");
            foreach (var size in new[] { new Size(980, 640), new Size(1280, 800), new Size(1920, 1080) }) {
                shell.Width = size.Width; shell.Height = size.Height; Scene(shell); shell.Championship.Refresh(); Scene(shell);
                var entry = Find<TextBox>(shell, "team-a");
                Need(entry.Bounds.Width > 50 && entry.IsEffectivelyVisible, "team editor survives carved panel at " + size);
                var action = shell.Championship.RoundConfirm!; var position = action.TranslatePoint(default, shell)!.Value;
                Need(action.Bounds.Height > 0 && position.Y >= 0 && position.Y + action.Bounds.Height < size.Height, "round confirmation stays inside window at " + size);
                var card = Find<Border>(shell, "match-setup-card");
                foreach(var button in shell.GetVisualDescendants().OfType<Button>().Where(b=>b.Name?.StartsWith("round-winner-")==true||b.Name?.StartsWith("round-ending-")==true))Centered(button);
                Need(card.GetVisualDescendants().OfType<IceSurface>().All(c => !c.IsHitTestVisible), "carved decoration never intercepts input");
                Need(Find<TextBlock>(shell, "header-timer-value").Foreground?.ToString() == Brushes.White.ToString(), "timer readable against dark ice");
                Need(entry.Background?.ToString()==Application.Current!.Resources["CasterDeskField"]?.ToString()&&entry.Foreground?.ToString()==Ui.Ink.ToString(),"name field uses material face and readable theme ink");
                Need(shell.GetVisualDescendants().OfType<IceCard>().All(c=>c.GetVisualDescendants().OfType<IceSurface>().Any()),"all panels receive carved material");
                for (int slot = 0; slot < 12; slot++) {
                    var ban = Find<Button>(shell, "ban-slot-" + slot);
                    Need(ban.Background?.ToString() == Ui.Team(MapRecord.BanTeam(slot), false).ToString(), "team colors retained on ban " + slot);
                    Need(ban.GetVisualDescendants().OfType<Border>().First().Background?.ToString() == ban.Background?.ToString(), "rendered team color retained");
                }
                Capture(shell, $"ice-{(int)size.Width}x{(int)size.Height}");
            }
            shell.Width = 1280; shell.Height = 800; Scene(shell);Thread.Sleep(150);Scene(shell);
            var input = Find<TextBox>(shell, "team-a"); var point = Center(input, shell);
            shell.MouseMove(point); shell.MouseDown(point, MouseButton.Left); shell.MouseUp(point, MouseButton.Left); Scene(shell);
            input=Find<TextBox>(shell,"team-a");
            Need(input.IsKeyboardFocusWithin, "real pointer reaches current team editor through decorative layer");
            input.Text = "NA ORG TESTE"; Scene(shell); shell.Championship.FlushNames(); Scene(shell);
            Need(session.Current.TeamA == "NA ORG TESTE", "team edits still save through carved panel");
            session.Names("NA ORG", "FAZE"); Scene(shell);
            var gear = Find<Button>(shell, "settings-menu"); point = Center(gear, shell);
            Capture(shell, "ice-controls-normal"); shell.MouseMove(point); Scene(shell); Need(gear.IsPointerOver, "button hover reaches control"); Capture(shell, "ice-controls-hover");
            shell.MouseDown(point, MouseButton.Left); Scene(shell); Need(gear.IsPressed, "button pressed state reaches control"); Capture(shell, "ice-controls-pressed");
            var gearAt=gear.TranslatePoint(default,shell)!.Value;var gearArea=new Rect(gearAt,gear.Bounds.Size).Deflate(3);
            Need(Luminance("ice-controls-pressed",gearArea)<Luminance("ice-controls-normal",gearArea)*.8,"pressed glass is visibly darker than normal");
            shell.MouseUp(point, MouseButton.Left); Scene(shell);
            Need(Find<Border>(shell, "menu-scrim").IsVisible && shell.Championship.IsEffectivelyEnabled, "compact menu click works without disabling workspace");
            Capture(shell, "ice-menu"); shell.ToggleMenu(); Scene(shell);
            var opened = shell.OpenOptions(); Scene(shell); var surface = shell.ActiveSurface!;
            var tabs = Find<TabControl>(surface, "settings-tabs"); tabs.SelectedIndex = 2; Scene(shell);
            var themes = Find<ComboBox>(surface, "settings-theme");
            foreach(var button in surface.GetVisualDescendants().OfType<Button>().Where(b=>b.Classes.Contains("ice-tab")))Centered(button);
            Need(themes.Items.Count == 3 && !themes.Items.Cast<object>().Any(t => t.ToString()!.Contains("Ice", StringComparison.OrdinalIgnoreCase)), "secret theme absent from ordinary theme picker");
            Need(!shell.Championship.IsEffectivelyEnabled, "options still blocks background input");
            themes.IsDropDownOpen = true; Scene(shell); Capture(shell, "ice-options"); themes.IsDropDownOpen = false;
            for(int index=0;index<6;index++){tabs.SelectedIndex=index;Scene(shell);Capture(shell,"ice-options-tab-"+index);}
            surface.Close(); Scene(shell); Need(opened.IsCompleted && shell.ActiveSurface == null, "single close releases modal");
            foreach(string caption in new[]{"caption-minimize","caption-close"}) {
                var button=Find<Button>(shell,caption);shell.MouseMove(Center(button,shell));Scene(shell);
                Need(button.GetVisualDescendants().OfType<IceSurface>().Any(),"caption has the same material treatment: "+caption);
                if(caption=="caption-close")Need(button.Background?.ToString()==Brush.Parse("#D92C35").ToString(),"close retains red hover beneath glass");
                Capture(shell,"ice-"+caption+"-hover");
            }
            shell.MouseMove(new Point(10,110));
            shell.Navigate(1);var render=shell.GetVisualDescendants().OfType<StudioView>().Single().SettlePreview();
            var until=System.Diagnostics.Stopwatch.StartNew();while(!render.IsCompleted&&until.Elapsed<TimeSpan.FromSeconds(30)){Dispatcher.UIThread.RunJobs();Thread.Sleep(10);}if(!render.IsCompleted)throw new TimeoutException("Studio review render timed out");render.GetAwaiter().GetResult();Capture(shell,"ice-studio");
            shell.Navigate(2);Scene(shell);Capture(shell,"ice-texts");shell.Navigate(0);Scene(shell);
            foreach (string theme in new[] { "light", "standard", "dark", "black-ice" }) {
                Ui.Repaint(theme, shell); shell.Championship.Refresh(); Scene(shell);
                Need(shell.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("app-button")).All(b => b.Classes.Contains("black-ice-button") == Ui.BlackIce), "theme switch removes or restores material templates: " + theme);
                Need(Find<TextBlock>(shell, "header-timer-value").Foreground?.ToString() == BlackIceTheme.HeaderInk.ToString(), "header text follows theme switch: " + theme);
                Need(Find<StackPanel>(shell,"main-navigation").Children.OfType<Button>().Skip(1).All(b=>b.Background?.ToString()==Brushes.Transparent.ToString()),"semantic transparent faces survive palette changes: "+theme);
            }
            File.WriteAllText(Path.Combine(AppPaths.DataDirectory, "ice-check.txt"), $"PASS: {count} checks. Hidden preview, isolated preferences, continuous frame, all cards, frosted inputs, darker pressed glass, centered round-button/tab text, six settings pages, three workspaces, caption hover, three viewport sizes, real pointer focus/menu, team colors, modal blocking and live palette changes. Headless only; native Windows move/minimize remain untested.");
        }
        finally { shell.Close(); Dispatcher.UIThread.RunJobs(); }
    }
}

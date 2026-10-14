using Avalonia.Input;
using System.Text.Json;

namespace CasterDesk.Avalonia;

public sealed class StudioShell : Window
{
    readonly Settings settings;
    internal readonly ChampionshipView Championship;
    readonly StudioView studio;
    internal readonly TextsView Texts;
    readonly ContentControl page = new();
    readonly StackPanel drawer = new();
    readonly Border drawerPanel;
    readonly Button gear;
    readonly Button[] tabs = new Button[3];
    readonly TextBlock footer = Ui.Text("CasterDesk • Ferramentas para community casters");
    int selected;
    bool menuOpen;
    bool backupOpen;

    public StudioShell(MatchSession? session = null)
    {
        settings = ConfigurationStore.Read<Settings>("settings.json"); ConfigurationStore.Normalize(settings); Ui.Theme(settings.DarkMode);
        Title = "CasterDesk • " + BuildInfo.Version; Width = 1440; Height = 900; MinWidth = 980; MinHeight = 640;
        FontFamily = new FontFamily("Segoe UI"); FontSize = 14; Background = Ui.Canvas;
        Icon = new WindowIcon(Path.Combine(AppContext.BaseDirectory, "assets", "branding", "CasterDesk.ico"));
        Championship = new ChampionshipView(session); Championship.SetQuickMode(settings.QuickRounds);
        studio = new StudioView(() => Championship.Session.Matches, Championship.Session.Catalog, Championship.Session.ApplyArtworkVeto);
        Texts = new TextsView(settings, SaveSettings);
        Opened += (_, _) => Texts.StartLive();
        Championship.MapConfirmed += async (match, map) => {
            try { await new StatsWindow(match, map).ShowDialog<bool>(this); }
            catch (Exception ex) { footer.Text = ex.Message; }
        };
        var layout = new Grid { RowDefinitions = new("Auto,*,Auto") }; Content = layout;
        var header = new Grid { ColumnDefinitions = new("Auto,*,Auto,Auto"), Background = Brush.Parse("#0A1118") };
        var brand = BrandMark.Create(true);
        brand.Margin = new Thickness(20, 8, 26, 8); brand.VerticalAlignment = VerticalAlignment.Center;
        header.Children.Add(brand);
        gear = Ui.Button("", ToggleMenu); gear.Name = "settings-menu"; gear.Content = new GearIcon { Width = 34, Height = 34 };
        gear.Width = 50; gear.Padding = new Thickness(6); gear.Margin = new Thickness(12, 5); gear.BorderThickness = new Thickness(1);
        gear.BorderBrush = Brush.Parse("#677078"); gear.Background = Brushes.Transparent;
        global::Avalonia.Automation.AutomationProperties.SetName(gear, L.T("Configurações e sobre"));
        gear.PointerEntered += (_, _) => PaintGear(true); gear.PointerExited += (_, _) => PaintGear(menuOpen);
        header.Children.Add(gear); Grid.SetColumn(gear,3);
        var timer = new HeaderTimer(Texts); header.Children.Add(timer); Grid.SetColumn(timer,2);
        var nav = new StackPanel { Name = "main-navigation", Orientation = Orientation.Horizontal };
        string[] labels = ["CAMPEONATO", "Central de imagens", "TEXTOS PARA OBS"];
        for (int n = 0; n < labels.Length; n++) {
            int index = n; var tab = Ui.Button(labels[n], () => Navigate(index)); tab.Margin = new Thickness(0); tab.Padding = new Thickness(20, 17);
            tab.Foreground = Brushes.White; tab.BorderThickness = new Thickness(0, 0, 0, 4); tabs[n] = tab; nav.Children.Add(tab);
        }
        header.Children.Add(nav); Grid.SetColumn(nav,1); layout.Children.Add(header);
        void FitHeader() {
            bool narrow = Bounds.Width < 1200; timer.Compact(Bounds.Width < 1550);
            brand.Width = narrow ? 140 : 240; brand.Height = brand.Width * 132 / 558;
            brand.Margin = new Thickness(narrow ? 8 : 20,8,narrow ? 8 : 26,8);
            foreach(var tab in tabs) { tab.Padding = new Thickness(narrow ? 8 : 16,17); tab.FontSize = narrow ? 11 : 14; }
        }
        SizeChanged += (_, _) => FitHeader(); FitHeader();
        layout.Children.Add(page); Grid.SetRow(page, 1);
        drawerPanel = new Border { Background = Ui.Panel, Padding = new Thickness(14, 8), Child = new ScrollViewer { Content = drawer }, ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, IsVisible = false, Width = 300 };
        layout.Children.Add(drawerPanel); Grid.SetRow(drawerPanel, 1);
        footer.FontSize = 12; footer.Margin = new Thickness(18, 4); layout.Children.Add(footer); Grid.SetRow(footer, 2);
        KeyDown += (_, e) => { if (e.Key == Key.Escape && menuOpen) { ToggleMenu(); e.Handled = true; } };
        Navigate(0);
        if (ConfigurationStore.RecoveryNotice.Length > 0) footer.Text = ConfigurationStore.RecoveryNotice;
        bool closing = false, waitingToClose = false;
        Closing += async (_, e) => {
            if (closing) return;
            if (waitingToClose) { e.Cancel = true; return; }
            if (backupOpen || studio.Exporting || Championship.Processing) { e.Cancel = true; footer.Text = L.T("Salvando arquivos…"); return; }
            try {
                Championship.FlushNames(); if (!studio.Flush()) { e.Cancel = true; return; }
                if (Texts.Busy) {
                    e.Cancel = true; waitingToClose = true;
                    await Texts.Applying; Texts.Save(); Texts.Stop(); await Texts.Writes; closing = true; Close();
                } else Texts.Save();
            }
            catch (Exception ex) { e.Cancel = true; footer.Text = ex.Message; }
            finally { waitingToClose = false; }
        };
        Closed += (_, _) => { timer.Stop(); Championship.Stop(); studio.Stop(); Texts.Stop(); };
    }

    internal void Navigate(int index)
    {
        if (backupOpen || studio.Exporting || Championship.Processing) return;
        try {
            Championship.FlushNames(); if (!studio.Flush()) return;
            if (selected == 2) Texts.Save();
            selected = index; page.Content = index == 0 ? Championship : index == 1 ? studio : Texts;
            if (index == 1) studio.Refresh(Championship.Session.Current.Id);
            for (int n = 0; n < tabs.Length; n++) { tabs[n].BorderBrush = n == index ? Brush.Parse("#FF4335") : Brushes.Transparent; tabs[n].Background = n == index ? Brush.Parse("#1C2B39") : Brushes.Transparent; }
            if (menuOpen) ToggleMenu();
        } catch (Exception ex) { footer.Text = ex.Message; }
    }

    void SaveSettings() => MatchFiles.AtomicText(AppPaths.ConfigFile("settings.json"), JsonSerializer.Serialize(settings));
    internal void ToggleMenu()
    {
        menuOpen = !menuOpen; drawerPanel.IsVisible = menuOpen; drawerPanel.Width = Math.Min(300, Bounds.Width * .32);
        drawerPanel.MaxHeight = Math.Max(150, Bounds.Height - 100);
        PaintGear(menuOpen); if (menuOpen) Menu();
    }
    void PaintGear(bool active) => gear.Background = active ? new LinearGradientBrush {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        GradientStops = [new GradientStop(Color.Parse("#481C20"), 0), new GradientStop(Color.Parse("#FF4335"), 1)] } : Brushes.Transparent;

    void Menu()
    {
        drawer.Children.Clear(); drawer.Children.Add(Ui.Text("Registro de rounds", true));
        void Option(StackPanel parent, string label, bool active, System.Action change)
        {
            var button = Ui.Button(label, () => {
                bool oldDark = settings.DarkMode, oldQuick = settings.QuickRounds; string oldLanguage = settings.Language;
                try { Championship.FlushNames(); if (!studio.Flush()) return; Texts.Save(); change(); SaveSettings(); Menu(); }
                catch (Exception ex) { settings.Language = oldLanguage; settings.QuickRounds = oldQuick; Appearance(oldDark); Championship.SetQuickMode(oldQuick); Menu(); footer.Text = ex.Message; }
            }, active);
            button.BorderThickness = new Thickness(0, 0, 0, active ? 3 : 0); button.BorderBrush = Brush.Parse("#FF4335"); parent.Children.Add(button);
        }
        Option(drawer, "Modo rápido", settings.QuickRounds, () => { settings.QuickRounds = true; Championship.SetQuickMode(true); });
        Option(drawer, "Modo clássico", !settings.QuickRounds, () => { settings.QuickRounds = false; Championship.SetQuickMode(false); });
        var appearance = new StackPanel();
        Option(appearance, "Escuro", settings.DarkMode, () => Appearance(true)); Option(appearance, "Claro", !settings.DarkMode, () => Appearance(false));
        drawer.Children.Add(new Expander { Header = L.T("Aparência") + " · Siege", Content = appearance, HorizontalAlignment = HorizontalAlignment.Stretch, IsExpanded = true });
        var languages = new StackPanel();
        foreach (var language in new[] { ("pt-BR", "Português"), ("en", "English"), ("es", "Español") }) {
            string code = language.Item1;
            Option(languages, language.Item2, settings.Language == code, () => { settings.Language = code; footer.Text = L.T("O idioma será aplicado na próxima abertura do CasterDesk."); });
        }
        drawer.Children.Add(new Expander { Header = L.T("Idioma"), Content = languages, HorizontalAlignment = HorizontalAlignment.Stretch, IsExpanded = true });
        var backups = Ui.Button("Backup e restauração", () => _ = OpenBackup()); backups.Name = "backup-menu"; drawer.Children.Add(backups);
        drawer.Children.Add(Ui.Button("Sobre", async () => {
            var panel = new StackPanel { Margin = new Thickness(20) }; panel.Children.Add(BrandMark.Create(Ui.Dark)); panel.Children.Add(Ui.Text(BuildInfo.Version));
            panel.Children.Add(Ui.Text("Ferramentas para community casters de Rainbow Six Siege."));
            var dialog = Ui.Dialog(this, "Sobre", panel, 540, 300); panel.Children.Add(Ui.Button("Fechar", () => dialog.Close())); await dialog.ShowDialog(this);
        }));
    }
    void Appearance(bool dark)
    {
        settings.DarkMode = dark; Ui.Theme(dark); Background = Ui.Canvas; drawerPanel.Background = Ui.Panel;
        Championship.Refresh(); studio.Background = Ui.Canvas; Texts.RefreshColors();
    }
    internal double MenuWidth => menuOpen ? drawerPanel.Width : 0;
    internal void Notify(string text) => footer.Text = text;
    internal async Task OpenBackup()
    {
        if (backupOpen || studio.Exporting || Championship.Processing) return;
        if (menuOpen) ToggleMenu();
        backupOpen = true;
        try {
            var dialog = new BackupWindow(this, async () => {
                Championship.FlushNames(); if (!studio.Flush()) throw new InvalidOperationException(L.T("Salvando arquivos…"));
                await Texts.Applying; Texts.Save(); Texts.WritesSuspended = true; await Texts.Writes;
            }, () => Texts.WritesSuspended = false);
            bool restore = await dialog.ShowDialog<bool>(this);
            backupOpen = false;
            if (restore) { Notify(L.T("Restauração agendada. Ela será aplicada na próxima abertura do CasterDesk.")); Close(); }
        } catch (Exception ex) { footer.Text = ex.Message; }
        finally { backupOpen = false; Texts.WritesSuspended = false; }
    }

    sealed class GearIcon : Control
    {
        public override void Render(DrawingContext context)
        {
            using var transform = context.PushTransform(Matrix.CreateScale(Bounds.Width / 48, Bounds.Height / 48));
            void Gear(double x, double y, double radius)
            {
                var geometry = new StreamGeometry();
                using (var path = geometry.Open()) { path.SetFillRule(FillRule.EvenOdd);
                    for (int i = 0; i < 32; i++) {
                        double angle = (i + .5) * Math.PI * 2 / 32, r = radius * (i % 4 is 1 or 2 ? 1 : .73);
                        var p = new Point(x + Math.Cos(angle) * r, y + Math.Sin(angle) * r);
                        if (i == 0) path.BeginFigure(p, true); else path.LineTo(p);
                    }
                    path.EndFigure(true);
                    for (int i = 0; i < 24; i++) {
                        double angle = i * Math.PI * 2 / 24; var p = new Point(x + Math.Cos(angle) * radius * .36, y + Math.Sin(angle) * radius * .36);
                        if (i == 0) path.BeginFigure(p, true); else path.LineTo(p);
                    }
                    path.EndFigure(true);
                }
                context.DrawGeometry(Brushes.White, null, geometry);
            }
            Gear(15, 15, 8.1); Gear(32, 31, 12.5);
        }
    }
}

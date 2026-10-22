using Avalonia.Input;
using Avalonia.VisualTree;
using System.Text.Json;

namespace CasterDesk.Desktop;

public sealed partial class StudioShell : Window
{
    readonly Settings settings;
    readonly MatchTableSync matchTable;
    readonly GoogleTableSync googleTable;
    internal readonly ChampionshipView Championship;
    readonly StudioView studio;
    internal readonly TextsView Texts;
    readonly ContentControl page = new();
    readonly StackPanel drawer = new();
    readonly Border drawerPanel;
    readonly Button gear;
    readonly Button[] tabs = new Button[3];
    readonly ObsDockSettings obsSettings;
    readonly ObsDockServer obsDock;
    readonly LocalControlSettings localSettings;
    readonly LocalControlServer localControl;
    readonly DispatcherTimer obsPresence = new() { Interval = TimeSpan.FromSeconds(1) };
    readonly TextBlock footer = Ui.Text("");
    int selected;
    bool menuOpen;
    int microphoneClicks;
    bool backupOpen;
    readonly Border menuShade = new() { Background = Brush.Parse("#98000000"), IsVisible = false, Name = "menu-scrim" };
    ModalSurface? optionsWindow;
    TextBlock? menuConnection;
    Grid? mainHeader;

    public StudioShell(MatchSession? session = null, UpdateController? updater = null)
    {
        updates=updater??new UpdateController();
        settings = ConfigurationStore.Read<Settings>("settings.json"); ConfigurationStore.Normalize(settings); Ui.Theme(Program.BlackIcePreview ? "black-ice" : settings.VisualTheme!);
        Title = "CasterDesk • " + BuildInfo.Version; Width = 1440; Height = 900; MinWidth = 980; MinHeight = 640;
        WindowState = WindowState.Maximized;
        FontFamily = new FontFamily("Segoe UI"); FontSize = 14; Background = Ui.Canvas;
        Icon = new WindowIcon(Path.Combine(AppContext.BaseDirectory, "assets", "branding", "CasterDesk.ico"));
        Championship = new ChampionshipView(session); Championship.SetQuickMode(settings.QuickRounds);
        Championship.SetSpecMode(settings.SpecMode);
        matchTable = new MatchTableSync(Championship.Session);
        googleTable = new GoogleTableSync(Championship.Session, () => matchTable.OutputPath);
        Championship.TableRequested += () => _ = OpenMatchTable();
        studio = new StudioView(() => Championship.Session.Matches, Championship.Session.Catalog, Championship.Session.ApplyArtworkVeto);
        Texts = new TextsView(settings, SaveSettings);
        obsSettings = ConfigurationStore.Read<ObsDockSettings>("obs-connection.json");
        obsSettings.ConfigurePlugin(File.Exists(Path.Combine(AppContext.BaseDirectory,"obs-plugin.optin")));
        obsDock = new ObsDockServer(Championship,Texts,()=>ActiveSurface==null&&!backupOpen&&!studio.Exporting&&!OwnedWindows.Any(w=>w.IsVisible),obsSettings);
        localSettings=ConfigurationStore.Read<LocalControlSettings>("local-control.json");
        localControl=new LocalControlServer(Championship,obsDock,localSettings);
        Championship.UseObsForBans=obsSettings.RouteBansToOBS;
        Championship.ObsBansModeChanged=value=>{bool old=obsSettings.RouteBansToOBS;try{obsSettings.RouteBansToOBS=value;SaveObsSettings();}catch{obsSettings.RouteBansToOBS=old;throw;}};
        Championship.Refresh();
        obsPresence.Tick+=(_,_)=>{Championship.SetObsConnected(obsDock.HasClient);RefreshMenuConnection();};
        Opened+=(_,_)=>obsPresence.Start();
        Opened += async (_, _) => { Texts.StartLive(); if(obsSettings.Enabled)try{await obsDock.StartAsync();SaveObsSettings();}catch(Exception ex){AppLog.Write("OBS connection: "+ex);Notify(ex.Message);} };
        Opened += async (_,_)=>{if(localSettings.Enabled)try{await localControl.StartAsync();}catch(Exception ex){AppLog.Write("Local control startup: "+ex.GetType().Name);Notify(L.T("Não foi possível ativar o controle local. Confira a conexão nas opções."));}};
        Championship.MapConfirmed += async (match, map) => {
            try { await new StatsWindow(match, map).ShowDialog<bool>(this); }
            catch (Exception ex) { footer.Text = ex.Message; }
        };
        var layout = new Grid { RowDefinitions = new("Auto,*,Auto") }; Content = layout;surfaceLayout=layout;
        var header = new IceHeader { ColumnDefinitions = new("Auto,*,Auto,Auto"), Background = Ui.MenuSurface }; mainHeader=header;
        header.Classes.Add("ice-main-header");
        var brand = BrandMark.Create(Ui.Dark);
        brand.Margin = new Thickness(20, 8, 26, 8); brand.VerticalAlignment = VerticalAlignment.Center;
        var brandPlaque=new IcePlaque(IceSurfaceKind.Brand){Name="brand-plaque"};brandPlaque.Children.Add(brand);header.Children.Add(brandPlaque);
        gear = Ui.Button("", ToggleMenu); gear.Name = "settings-menu"; gear.Content = new GearIcon { Name="settings-gear-icon", Width = 34, Height = 34 };
        gear.Width = 50; gear.Padding = new Thickness(6); gear.Margin = new Thickness(12, 5); gear.BorderThickness = new Thickness(1);
        gear.BorderBrush = Brush.Parse("#677078"); gear.Background = Brushes.Transparent;
        global::Avalonia.Automation.AutomationProperties.SetName(gear, L.T("Configurações e sobre"));
        gear.PointerEntered += (_, _) => PaintGear(true); gear.PointerExited += (_, _) => PaintGear(menuOpen);
        header.Children.Add(gear); Grid.SetColumn(gear,3);
        var timer = new HeaderTimer(Texts);timer.Name="header-timer";timer.IsVisible=!Championship.SpecMode;Championship.SpecModeChanged+=value=>timer.IsVisible=!value; header.Children.Add(timer); Grid.SetColumn(timer,2);
        var nav = new StackPanel { Name = "main-navigation", Orientation = Orientation.Horizontal };
        string[] labels = ["CAMPEONATO", "GERADOR DE IMAGENS", "TEXTOS PARA OBS"];
        for (int n = 0; n < labels.Length; n++) {
            int index = n; var tab = Ui.Button(labels[n], () => Navigate(index)); tab.Margin = new Thickness(0); tab.Padding = new Thickness(20, 17);
            tab.Classes.Add("ice-tab");
            tab.Foreground = BlackIceTheme.HeaderInk; tab.BorderThickness = new Thickness(0, 0, 0, 4); tabs[n] = tab; nav.Children.Add(tab);
        }
        header.Children.Add(nav); Grid.SetColumn(nav,1); layout.Children.Add(header);
        void FitHeader() {
            bool narrow = Bounds.Width < 1200; timer.Compact(Bounds.Width < 1550);
            brand.Width = narrow ? 140 : 240; brand.Height = brand.Width * 132 / 558;
            brand.Margin = new Thickness(narrow ? 8 : 20,8,narrow ? 8 : 26,8);
            foreach(var tab in tabs) { tab.Padding = new Thickness(narrow ? 8 : 16,17); tab.FontSize = narrow ? 11 : 14; }
        }
        SizeChanged += (_, _) => FitHeader(); FitHeader();PaintGear(false);
        layout.Children.Add(page); Grid.SetRow(page, 1);
        var drawerIce=new IcePlaque(IceSurfaceKind.Menu);drawerIce.Children.Add(new ScrollViewer { Content=drawer,Margin=new Thickness(14) });
        drawerPanel = new Border { Background = Ui.MenuSurface, BorderBrush=Ui.FrameLine, BorderThickness=new Thickness(1), Margin=new Thickness(0,0,12,0), Child = drawerIce, ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, IsVisible = false, Width = 300 };
        layout.Children.Add(menuShade); Grid.SetRowSpan(menuShade,3);
        menuShade.PointerPressed+=(_,e)=>{if(menuOpen)ToggleMenu();e.Handled=true;};
        layout.Children.Add(drawerPanel); Grid.SetRow(drawerPanel, 1);
        footer.FontSize = 12; footer.Margin = new Thickness(18, 4); footer.IsVisible = false;
        footer.PropertyChanged += (_, e) => { if (e.Property == TextBlock.TextProperty) footer.IsVisible = !string.IsNullOrWhiteSpace(footer.Text); };
        layout.Children.Add(footer); Grid.SetRow(footer, 2);
        InitializeUpdates(layout);
        KeyDown += (_, e) => {
            if(ActiveSurface!=null){if(e.Key==Key.Escape){_ = CloseSurface();e.Handled=true;}return;}
            if(!menuOpen)return;
            if(e.Key==Key.Escape){ToggleMenu();e.Handled=true;}
            else if(e.Key==Key.Tab){
                var choices=drawer.Children.OfType<Button>().ToArray();
                int current=Array.FindIndex(choices,b=>b.IsFocused);
                choices[(current+(e.KeyModifiers.HasFlag(KeyModifiers.Shift)?choices.Length-1:1)+choices.Length)%choices.Length].Focus();e.Handled=true;
            }
        };
        Navigate(0);
        if (ConfigurationStore.RecoveryNotice.Length > 0) footer.Text = ConfigurationStore.RecoveryNotice;
        bool closing = false, waitingToClose = false;
        Closing += async (_, e) => {
            if (closing) return;
            if(ActiveSurface!=null){e.Cancel=true;if(await CloseSurface())Close();return;}
            if (waitingToClose) { e.Cancel = true; return; }
            if (backupOpen || studio.Exporting || Championship.Processing) { e.Cancel = true; footer.Text = L.T("Salvando arquivos…"); return; }
            // Keep the UI dispatcher alive until saves and HTTP hosts have stopped.
            // An async Closed handler can lose its continuation when the last window closes.
            e.Cancel = true; waitingToClose = true;
            try {
                Championship.FlushNames(); if (!studio.Flush()) return;
                await Texts.Applying; Texts.Save(); Texts.Stop(); await Texts.Writes;
                await matchTable.StopAsync(); await googleTable.StopAsync();
                obsPresence.Stop();timer.Stop();Championship.Stop();studio.Stop();
                await localControl.DisposeAsync();await obsDock.DisposeAsync();
                closing = true; Close();
            }
            catch (Exception ex) { e.Cancel = true; footer.Text = ex.Message; }
            finally { waitingToClose = false; }
        };
    }

    internal void Navigate(int index)
    {
        if (ActiveSurface!=null || backupOpen || studio.Exporting || Championship.Processing) return;
        try {
            Championship.FlushNames(); if (!studio.Flush()) return;
            if (selected == 2) Texts.Save();
            selected = index; page.Content = index == 0 ? Championship : index == 1 ? studio : Texts;
            if (index == 1) studio.Refresh(Championship.Session.Current.Id);
            for (int n = 0; n < tabs.Length; n++) { tabs[n].Classes.Set("ice-selected",n==index);tabs[n].BorderBrush = n == index ? Ui.Accent : Brushes.Transparent; tabs[n].Background = n == index ? Ui.BlackIce ? Brush.Parse("#17658C") : Ui.Panel : Brushes.Transparent; }
            if (menuOpen) ToggleMenu();
        } catch (Exception ex) { footer.Text = ex.Message; }
    }

    void SaveSettings() => MatchFiles.AtomicText(AppPaths.ConfigFile("settings.json"), JsonSerializer.Serialize(settings));
    internal void ToggleMenu()
    {
        if(ActiveSurface!=null)return;
        microphoneClicks=0;
        menuOpen = !menuOpen; drawerPanel.IsVisible = menuOpen; menuShade.IsVisible = menuOpen;menuShade.Background=Brushes.Transparent; drawerPanel.Width = Math.Min(300, Bounds.Width * .32);
        drawerPanel.MaxHeight = Math.Max(150, Bounds.Height - 100);
        page.IsEnabled=true;
        menuShade.ZIndex=100;drawerPanel.ZIndex=101;
        PaintGear(menuOpen); if (menuOpen) { Menu(); drawer.Children.OfType<Button>().FirstOrDefault()?.Focus(); } else gear.Focus();
    }
    void PaintGear(bool active) => gear.Background=active?MenuHighlight():Ui.Dark?Brushes.Transparent:Brush.Parse("#49657D");

    void Menu()
    {
        drawer.Children.Clear();
        void Item(string title, Action action, string name) {
            var button=Ui.Button(title,action);button.Name=name;button.Height=56;button.FontSize=19;button.FontWeight=FontWeight.Bold;
            button.HorizontalContentAlignment=HorizontalAlignment.Left;button.Padding=new Thickness(16,6);button.Margin=new Thickness(0,0,0,8);
            button.Background=Brushes.Transparent;button.Foreground=BlackIceTheme.HeaderInk;button.BorderBrush=Ui.FrameLine;button.BorderThickness=new Thickness(1);
            void Highlight(bool on) { button.Background=on?MenuHighlight():Brushes.Transparent;button.BorderBrush=on?Ui.Accent:Ui.FrameLine;button.BorderThickness=new Thickness(1,1,1,on?4:1); }
            button.PointerEntered+=(_,_)=>Highlight(true);button.PointerExited+=(_,_)=>Highlight(false);
            button.GotFocus+=(_,_)=>Highlight(true);button.LostFocus+=(_,_)=>Highlight(false);drawer.Children.Add(button);
        }
        Item("OPÇÕES",()=>_ = OpenOptions(),"options-menu");
        Item("ATUALIZAÇÕES",()=>_ = OpenUpdates(),"updates-menu");
        Item("CRÉDITOS",()=>_ = OpenCredits(),"credits-menu");
        var label=Ui.Text("STATUS DO PLUGIN OBS",true);label.FontSize=12;label.Foreground=BlackIceTheme.HeaderInk;drawer.Children.Add(label);
        menuConnection=Ui.RawText("");menuConnection.FontSize=16;menuConnection.FontWeight=FontWeight.Bold;drawer.Children.Add(menuConnection);RefreshMenuConnection();
        drawer.Children.Add(new Border { Height=1,Background=Ui.FrameLine,Margin=new Thickness(0,10) });
        var identity=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        var mark=BrandMark.Create(Ui.Dark);mark.Width=130;mark.Height=31;
        var brandArea=new Grid{Width=130,Height=31};brandArea.Children.Add(mark);
        // An unadvertised hit region over the existing mark, without button
        // semantics, cursor, hover decoration or a hint in the ordinary UI.
        var microphone=new Border{Name="menu-brand-microphone",Width=28,Height=31,Background=Brushes.Transparent,HorizontalAlignment=HorizontalAlignment.Left};
        bool microphonePressed=false;
        microphone.PointerPressed+=(_,e)=>microphonePressed=e.GetCurrentPoint(microphone).Properties.IsLeftButtonPressed;
        microphone.PointerReleased+=(_,e)=>{
            bool clicked=microphonePressed&&e.InitialPressMouseButton==MouseButton.Left&&new Rect(microphone.Bounds.Size).Contains(e.GetPosition(microphone));
            microphonePressed=false;
            if(!clicked||settings.BlackIceUnlocked)return;
            if(++microphoneClicks==6)UnlockBlackIce();
        };
        brandArea.Children.Add(microphone);identity.Children.Add(brandArea);
        var version=Ui.RawText(BuildInfo.Version);version.Foreground=BlackIceTheme.HeaderInk;version.FontSize=11;version.VerticalAlignment=VerticalAlignment.Center;version.Margin=new Thickness(0);identity.Children.Add(version);drawer.Children.Add(identity);
    }
    void UnlockBlackIce()
    {
        string previous=settings.VisualTheme??"standard";
        try {
            settings.BlackIceUnlocked=true;Appearance("black-ice");SaveSettings();
        } catch(Exception ex) {
            settings.BlackIceUnlocked=false;Appearance(previous);microphoneClicks=0;Notify(ex.Message);
        }
    }
    IBrush MenuHighlight()=>new LinearGradientBrush {
        StartPoint=new RelativePoint(0,0,RelativeUnit.Relative),EndPoint=new RelativePoint(1,1,RelativeUnit.Relative),
        GradientStops=[new GradientStop(Color.Parse(Ui.BlackIce?"#173C51":Ui.Dark?"#182535":"#D5E7F2"),0),new GradientStop(Color.Parse(Ui.BlackIce?"#245D78":Ui.Dark?"#622731":"#9FCBE1"),.65),new GradientStop(Color.Parse(Ui.BlackIce?"#1687B4":Ui.Dark?"#E83E36":"#288CBA"),1)] };
    void RefreshMenuConnection() { if(menuConnection==null)return;menuConnection.Text="●  "+L.T(obsDock.HasClient?"Conectado":"Desconectado").ToUpperInvariant();menuConnection.Foreground=Brush.Parse(obsDock.HasClient?(Ui.Dark?"#29DF83":"#087B46"):(Ui.Dark?"#FF645B":"#B42629")); }
    async Task OpenCredits() {
        if(menuOpen)ToggleMenu();
        var panel=new StackPanel{Margin=new Thickness(24)};panel.Children.Add(BrandMark.Create(Ui.Dark));panel.Children.Add(Ui.RawText(BuildInfo.Version));
        panel.Children.Add(Ui.Text("Ferramentas para community casters de Rainbow Six Siege."));
        panel.Children.Add(Ui.Text("Projeto independente. Rainbow Six Siege e suas marcas pertencem à Ubisoft. Consulte os avisos de terceiros distribuídos com o aplicativo."));
        var dialog=new ModalSurface("Créditos",panel,650,440){Name="credits-surface"};panel.Children.Add(Ui.Button("Fechar",()=>dialog.Close()));await ShowSurface(dialog);
    }
    void SaveObsSettings()=>MatchFiles.AtomicText(AppPaths.ConfigFile("obs-connection.json"),JsonSerializer.Serialize(obsSettings));
    Control CreateObsPanel()
    {
        var panel=new StackPanel { Margin=new Thickness(20) };
        panel.Children.Add(Ui.Text("Controle o timer e os bans em um painel do OBS.",true));
        panel.Children.Add(Ui.Text(obsSettings.PluginConfigured?"O plugin do OBS encontra a conexão automaticamente. Abra o CasterDesk e selecione Painéis → CasterDesk no OBS.":"Ative a conexão e copie o endereço para um painel de navegador personalizado do OBS. O CasterDesk precisa permanecer aberto."));
        var state=Ui.Text(obsDock.Running?"Conectado":"Desconectado");panel.Children.Add(state);
        var connect=Ui.Button("Ativar conexão",async()=>{
            try{await obsDock.StartAsync();obsSettings.Enabled=true;SaveObsSettings();state.Text=L.T("Conectado");}
            catch(Exception ex){state.Text=ex.Message;}
        });connect.Name="obs-enable";panel.Children.Add(connect);
        panel.Children.Add(Ui.Button("Copiar endereço do painel",()=>{if(obsDock.Running){Ui.CopyText(obsDock.PanelUrl);state.Text=L.T("Endereço copiado.");}}));
        panel.Children.Add(Ui.Button("Desativar conexão",async()=>{try{await obsDock.DisposeAsync();obsSettings.Enabled=false;SaveObsSettings();state.Text=L.T("Desconectado");}catch(Exception ex){state.Text=ex.Message;}}));
        return panel;
    }
    void Appearance(bool dark) => Appearance(dark?"standard":"light");
    void Appearance(string theme,params Control[] extra)
    {
        Ui.Repaint(theme,new Control[]{this,Championship,studio,Texts}.Concat(OwnedWindows).Concat(extra).ToArray());
        settings.VisualTheme=Ui.ThemeName;settings.DarkMode=Ui.Dark;Background=Ui.Canvas;drawerPanel.Background=Ui.MenuSurface;
        if(mainHeader!=null)mainHeader.Background=Ui.MenuSurface;
        foreach(var tab in tabs)tab.Foreground=BlackIceTheme.HeaderInk;
        Championship.Refresh();studio.Background=Ui.Canvas;Texts.RefreshColors();
        for(int i=0;i<tabs.Length;i++){tabs[i].Classes.Set("ice-selected",i==selected);tabs[i].Background=i==selected?Ui.BlackIce?Brush.Parse("#17658C"):Ui.Panel:Brushes.Transparent;tabs[i].BorderBrush=i==selected?Ui.Accent:Brushes.Transparent;}
        PaintGear(menuOpen || gear.IsPointerOver);
        if(menuOpen)Menu();
    }
    internal double MenuWidth => menuOpen ? drawerPanel.Width : 0;
    internal void Notify(string text) => footer.Text = text;
    BackupWindow CreateBackupPanel()=>new BackupWindow(this,async()=>{
        backupOpen=true;
        Championship.FlushNames();if(!studio.Flush())throw new InvalidOperationException(L.T("Salvando arquivos…"));
        await googleTable.FlushArchivesAsync();await matchTable.FlushAsync();await Texts.Applying;Texts.Save();Texts.WritesSuspended=true;await Texts.Writes;
    },()=>{Texts.WritesSuspended=false;backupOpen=false;});
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

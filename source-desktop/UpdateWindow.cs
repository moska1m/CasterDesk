using System.Diagnostics;

namespace CasterDesk.Desktop;

public sealed partial class StudioShell
{
    readonly UpdateController updates;
    readonly DispatcherTimer updateTimer = new() { Interval=TimeSpan.FromSeconds(3) };
    readonly Border updateBanner = new() { Name="update-banner",IsVisible=false,Padding=new Thickness(10,4),Margin=new Thickness(10,0,10,4) };
    readonly TextBlock updateBannerText = Ui.RawText("");
    Window? updateWindow;
    bool updateInitial=true;
    string dismissedUpdate="";
    bool installingUpdate;
    bool startupUpdateShown;

    void InitializeUpdates(Grid layout)
    {
        var row=new Grid{ColumnDefinitions=new("*,Auto,Auto")};
        updateBannerText.VerticalAlignment=VerticalAlignment.Center;updateBannerText.TextWrapping=TextWrapping.Wrap;updateBannerText.Foreground=Brushes.White;row.Children.Add(updateBannerText);
        var show=Ui.Button("Ver atualização",()=>_ = OpenUpdates(false));show.Name="show-update";show.Margin=new Thickness(8,0,0,0);row.Children.Add(show);Grid.SetColumn(show,1);
        var later=Ui.Button("Depois",()=>{dismissedUpdate=updates.Available?.Version??"";RefreshUpdateBanner();});later.Margin=new Thickness(8,0,0,0);row.Children.Add(later);Grid.SetColumn(later,2);
        updateBanner.Child=row;
        layout.Children.Remove(footer);
        var bottom=new StackPanel();bottom.Children.Add(updateBanner);bottom.Children.Add(footer);layout.Children.Add(bottom);Grid.SetRow(bottom,2);
        updates.Changed+=RefreshUpdateBanner;
        updateTimer.Tick+=async(_,_)=>{
            if(updateInitial){updateInitial=false;updateTimer.Interval=TimeSpan.FromMinutes(15);}
            await updates.CheckAsync(false);
            await ShowStartupUpdate();
        };
        Opened+=(_,_)=>{if(!Program.Headless)updateTimer.Start();};
        Closed+=(_,_)=>{updateTimer.Stop();updates.Dispose();};
    }
    internal async Task ShowStartupUpdate()
    {
        var item=updates.Available;
        if(startupUpdateShown||!updates.Preferences.Automatic||item==null||updates.Checking||updates.Downloading||backupOpen||studio.Exporting||Championship.Processing||OwnedWindows.Any(w=>w.IsVisible)||!IsVisible)return;
        startupUpdateShown=true;
        var content=new Grid { Margin=new Thickness(20),RowDefinitions=new("Auto,Auto,*,Auto,Auto") };
        var title=Ui.RawText((item.Important?"ATUALIZAÇÃO IMPORTANTE":"NOVA ATUALIZAÇÃO")+" · "+item.Version,true);title.FontSize=22;content.Children.Add(title);
        var version=Ui.RawText("Instalada: "+BuildInfo.Version+(item.Beta?" · Esta versão é uma beta em teste.":""));content.Children.Add(version);Grid.SetRow(version,1);
        var notes=Ui.RawText(Summary(item.Notes));notes.Name="startup-update-notes";notes.FontSize=15;
        var scroll=new ScrollViewer { Content=notes,Margin=new Thickness(0,12),HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled };content.Children.Add(scroll);Grid.SetRow(scroll,2);
        var hint=Ui.Text("Você pode atualizar agora ou continuar usando o programa e fazer isso depois.");hint.FontSize=12;content.Children.Add(hint);Grid.SetRow(hint,3);
        var buttons=new StackPanel { Orientation=Orientation.Horizontal,Spacing=10,HorizontalAlignment=HorizontalAlignment.Center };
        var dialog=Ui.Dialog(this,"Novidades do CasterDesk",content,640,540);dialog.Name="startup-update-notice";
        var show=Ui.Button("Ver versões e atualizar",()=>dialog.Close(true),true);show.Name="startup-update-open";buttons.Children.Add(show);
        var later=Ui.Button("Depois",()=>dialog.Close(false));later.Name="startup-update-later";buttons.Children.Add(later);
        content.Children.Add(buttons);Grid.SetRow(buttons,4);
        if(await dialog.ShowDialog<bool>(this)) { updates.Select(item.Version);await OpenUpdates(false); }
    }
    static string Summary(string notes)
    {
        string text=string.Join(Environment.NewLine,notes.Split('\n').Where(l=>!l.TrimStart().StartsWith('#')));
        text=System.Text.RegularExpressions.Regex.Replace(text,@"\[([^\]]+)\]\([^)]+\)","$1").Replace("**","").Replace("`","");
        return text.Length>6000?text[..6000]+"…":text.Trim();
    }
    void RefreshUpdateBanner()
    {
        var item=updates.Available;
        updateBanner.IsVisible=item!=null&&item.Version!=dismissedUpdate;
        if(item==null)return;
        updateBanner.Background=Brush.Parse(item.Important?"#71313A":"#25475E");
        updateBannerText.Text=(item.Important?"ATUALIZAÇÃO IMPORTANTE":"NOVA VERSÃO")+" · CasterDesk "+item.Version;
        updateBannerText.FontWeight=item.Important?FontWeight.Bold:FontWeight.Normal;
    }
    internal async Task OpenUpdates(bool check=true)
    {
        startupUpdateShown=true;
        if(updateWindow!=null){updateWindow.Activate();return;}
        if(menuOpen)ToggleMenu();
        var panel=new StackPanel{Margin=new Thickness(20),Spacing=10};
        panel.Children.Add(Ui.Text("ATUALIZAÇÕES DO CASTERDESK",true));
        panel.Children.Add(Ui.RawText("Versão instalada: "+BuildInfo.Version));
        var automatic=new CheckBox{Content="Verificar automaticamente ao abrir e a cada 6 horas",IsChecked=updates.Preferences.Automatic};panel.Children.Add(automatic);
        var betas=new CheckBox{Content="Mostrar betas",Name="show-betas",IsChecked=updates.Preferences.IncludeBetas};panel.Children.Add(betas);
        panel.Children.Add(Ui.Text("Betas incluem recursos em teste."));
        var columns=new Grid{ColumnDefinitions=new("*,125,95"),Background=Ui.Header,Margin=new Thickness(0,0,0,-10)};
        foreach(var (label,column) in new[]{("VERSÃO",0),("LANÇADA EM",1),("TIPO",2)}) {
            var text=Ui.RawText(label,true);text.FontSize=12;text.Margin=new Thickness(10,6);columns.Children.Add(text);Grid.SetColumn(text,column);
        }
        panel.Children.Add(columns);
        var versions=new ListBox{Name="update-versions",Height=190};panel.Children.Add(versions);
        bool refreshing=false;
        UpdateManifest[] displayed=[];
        var title=Ui.RawText("");title.FontSize=20;title.FontWeight=FontWeight.Bold;title.TextWrapping=TextWrapping.Wrap;panel.Children.Add(title);
        var notes=Ui.RawText("");notes.TextWrapping=TextWrapping.Wrap;panel.Children.Add(notes);
        var progress=new ProgressBar{Minimum=0,Maximum=100,IsVisible=false};panel.Children.Add(progress);
        var status=Ui.RawText(updates.Status);status.TextWrapping=TextWrapping.Wrap;panel.Children.Add(status);
        using var cancellation=new CancellationTokenSource();
        var checkButton=Ui.Button("Verificar agora",async()=>{
            try{updates.Configure(automatic.IsChecked==true,betas.IsChecked==true);await updates.CheckAsync(true);}
            catch(Exception ex){status.Text=ex.Message;}
        });checkButton.Name="check-updates";panel.Children.Add(checkButton);
        var download=Ui.Button("Baixar atualização",()=>_ = updates.DownloadAsync(cancellation.Token),true);download.Name="download-update";panel.Children.Add(download);
        Button? install=null;
        install=Ui.Button("Instalar e fechar o CasterDesk",async()=>{
            if(updates.Downloaded==null||updates.Selected==null||installingUpdate)return;
            if(backupOpen||studio.Exporting||Championship.Processing||OwnedWindows.Any(w=>w!=updateWindow&&w.IsVisible)) {status.Text="Conclua a operação atual antes de instalar.";return;}
            installingUpdate=true;install!.IsEnabled=false;
            try {
                Championship.FlushNames();if(!studio.Flush())throw new InvalidOperationException("Aguarde o salvamento das imagens.");
                await Texts.Applying;Texts.Save();await Texts.Writes;await matchTable.FlushAsync();await googleTable.FlushArchivesAsync();
                await UpdateClient.VerifyFileAsync(updates.Downloaded,updates.Selected,cancellation.Token);
                var start=new ProcessStartInfo(updates.Downloaded){UseShellExecute=true};
                start.ArgumentList.Add("/UPDATEAPP");start.ArgumentList.Add("/APPWAITPID="+Environment.ProcessId);
                start.ArgumentList.Add("/LANG="+(L.Current=="es"?"spanish":L.Current=="en"?"english":"brazilianportuguese"));
                _ = Process.Start(start)??throw new IOException("Não foi possível abrir o instalador.");
                updateWindow!.Close();Close();
            } catch(Exception ex){status.Text=ex.Message;installingUpdate=false;install.IsEnabled=true;}
        },true);install.Name="install-update";panel.Children.Add(install);
        panel.Children.Add(Ui.Text("Instale quando puder encerrar a sessão. O instalador será aberto para concluir a atualização. Partidas e configurações permanecem salvas. O painel OBS só é reinstalado se você selecionar essa opção."));
        var close=Ui.Button("Continuar usando / depois",()=>updateWindow?.Close());panel.Children.Add(close);
        void Refresh() {
            refreshing=true;
            var visible=updates.Versions;
            if(!displayed.SequenceEqual(visible)) {
                displayed=visible;
                versions.ItemsSource=visible.Select(v=>{
                    var row=new Grid{ColumnDefinitions=new("*,125,95")};
                    var version=Ui.RawText(v.Version+(v.Version==BuildInfo.Version?" · instalada":""));version.FontWeight=FontWeight.SemiBold;version.Margin=new Thickness(4,6);row.Children.Add(version);
                    var date=Ui.RawText(v.PublishedAt.ToLocalTime().ToString("dd/MM/yyyy"));date.Margin=new Thickness(4,6);row.Children.Add(date);Grid.SetColumn(date,1);
                    var badge=Ui.RawText(v.Beta?"BETA":"RELEASE");badge.FontWeight=FontWeight.Bold;badge.FontSize=12;badge.Margin=new Thickness(4,6);badge.Foreground=Brush.Parse(v.Beta?"#E7AD52":"#59BE90");row.Children.Add(badge);Grid.SetColumn(badge,2);
                    return new ListBoxItem{Content=row,Tag=v.Version,Padding=new Thickness(6,0),HorizontalContentAlignment=HorizontalAlignment.Stretch};
                }).ToArray();
            }
            var item=updates.Selected;
            versions.SelectedIndex=Array.FindIndex(displayed,v=>v.Version==item?.Version);
            title.Text=item==null?"Escolha uma versão":(item.Important?"Atualização importante · ":"CasterDesk ")+item.Version;
            title.Foreground=item?.Important==true?Brush.Parse(Ui.Dark?"#FF887A":"#B63126"):Ui.Dark?Brushes.White:Brushes.Black;
            notes.Text=item?.Notes??"";
            status.Text=item!=null&&!UpdateController.IsNewer(item)?(item.Version==BuildInfo.Version?"Esta versão já está instalada.":"Versão anterior à instalada. Consulte as notas; a atualização mantém sua versão mais recente."):updates.Status;
            progress.IsVisible=updates.Downloading;progress.Value=updates.Progress*100;
            download.IsVisible=item!=null&&UpdateController.IsNewer(item)&&updates.Downloaded==null;download.IsEnabled=!updates.Downloading&&!updates.Checking;
            install.IsVisible=updates.Downloaded!=null;install.IsEnabled=!installingUpdate;
            checkButton.IsEnabled=!updates.Checking&&!updates.Downloading;betas.IsEnabled=checkButton.IsEnabled;automatic.IsEnabled=checkButton.IsEnabled;versions.IsEnabled=checkButton.IsEnabled;
            close.Content=updates.Downloading?"Cancelar download / depois":"Continuar usando / depois";
            refreshing=false;
        }
        versions.SelectionChanged+=(_,_)=>{if(!refreshing&&versions.SelectedItem is ListBoxItem row&&row.Tag is string version)updates.Select(version);};
        void Configure(){if(refreshing)return;try{updates.Configure(automatic.IsChecked==true,betas.IsChecked==true);}catch(Exception ex){status.Text=ex.Message;}}
        betas.IsCheckedChanged+=(_,_)=>Configure();automatic.IsCheckedChanged+=(_,_)=>Configure();
        updates.Changed+=Refresh;Refresh();
        updateWindow=Ui.Dialog(this,"Atualizações",new ScrollViewer{Content=panel},720,850);
        try {var dialogTask=updateWindow.ShowDialog(this);if(check)await updates.CheckAsync(true);await dialogTask;}
        finally {cancellation.Cancel();updates.Changed-=Refresh;updateWindow=null;}
    }
}

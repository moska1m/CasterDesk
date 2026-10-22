using Avalonia.Styling;
namespace CasterDesk.Desktop;
public sealed partial class StudioShell
{
    internal async Task OpenOptions()
    {
        if(optionsWindow!=null){optionsWindow.Focus();return;}
        if(ActiveSurface!=null)return;
        if(menuOpen)ToggleMenu();
        var root=new Grid{RowDefinitions=new("Auto,*"),Margin=new Thickness(20)};
        var tabs=new TabControl{Name="settings-tabs",IsVisible=false};root.Children.Add(tabs);
        var pages=new ContentControl{Name="settings-page"};root.Children.Add(pages);Grid.SetRow(pages,1);
        var pageContents=new List<Control>();
        void Present(){pages.Content=tabs.SelectedIndex>=0&&tabs.SelectedIndex<pageContents.Count?pageContents[tabs.SelectedIndex]:null;}
        var navigation=new UniformGrid{Rows=1};var navigationIce=new IcePlaque(IceSurfaceKind.Toolbar);navigationIce.Children.Add(navigation);root.Children.Add(navigationIce);
        var tabButtons=new List<Button>();
        void PaintTabs(){for(int i=0;i<tabButtons.Count;i++){var button=tabButtons[i];button.Classes.Set("ice-selected",i==tabs.SelectedIndex);button.Foreground=Ui.BlackIce?Brushes.White:Ui.Ink;button.Background=i==tabs.SelectedIndex?Ui.BlackIce?Brush.Parse("#17658C"):Ui.Panel:Brushes.Transparent;button.BorderBrush=i==tabs.SelectedIndex?Ui.Accent:Ui.FrameLine;button.BorderThickness=new Thickness(0,0,0,i==tabs.SelectedIndex?3:1);}}
        tabs.SelectionChanged+=(_,e)=>{if(e.Source==tabs){Present();PaintTabs();}};
        StackPanel Tab(string title) {
            int index=tabs.Items.Count;var button=Ui.Button(title,()=>tabs.SelectedIndex=index);button.Classes.Add("ice-tab");button.Margin=new Thickness(0);button.Padding=new Thickness(10,12);button.FontSize=14;button.FontWeight=FontWeight.Bold;tabButtons.Add(button);navigation.Children.Add(button);
            var panel=new StackPanel{Spacing=10,Margin=new Thickness(8,18)};
            pageContents.Add(new ScrollViewer{Content=panel,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});tabs.Items.Add(new TabItem{Header=L.T(title),Name="settings-tab-"+tabs.Items.Count});return panel;
        }
        void Row(StackPanel panel,string label,Control control) {
            var row=new Grid{ColumnDefinitions=new("*,*"),Background=Ui.Panel,Margin=new Thickness(0,0,0,4)};
            var caption=Ui.Text(label);caption.VerticalAlignment=VerticalAlignment.Center;caption.Margin=new Thickness(14,8);
            control.Margin=new Thickness(8);control.VerticalAlignment=VerticalAlignment.Center;row.Children.Add(caption);row.Children.Add(control);Grid.SetColumn(control,1);panel.Children.Add(row);
        }
        bool loading=true;
        void dialogTitle(){if(optionsWindow!=null){optionsWindow.Title=L.T("Opções");Title="CasterDesk • "+L.T("Opções");}}
        void Change(Action change) {
            if(loading)return;
            try { Championship.FlushNames();if(!studio.Flush())return;Texts.Save();change();SaveSettings(); }
            catch(Exception ex){Notify(ex.Message);}
        }
        var general=Tab("GERAL");general.Children.Add(Ui.Text("PREFERÊNCIAS",true));
        var language=Ui.RawCombo("Português (Brasil)","English","Español");language.Name="settings-language";language.SelectedIndex=Array.IndexOf(L.Languages,settings.Language);
        language.SelectionChanged+=(_,_)=>Change(()=>{
            if(language.SelectedIndex<0)return;
            string previous=settings.Language;string next=L.Languages[language.SelectedIndex];
            settings.Language=next;
            try{SaveSettings();}catch{settings.Language=previous;throw;}
            L.Set(next);loading=true;
            try{Ui.Relocalize();Championship.Refresh();studio.Refresh(Championship.Session.Current.Id);}finally{loading=false;}
            dialogTitle();
            string[] titles=["GERAL","PARTIDA","APARÊNCIA","TABELA","BACKUP","OBS"];
            for(int i=0;i<tabs.Items.Count;i++)((TabItem)tabs.Items[i]!).Header=L.T(titles[i]);
            RefreshMenuConnection();
        });Row(general,"Idioma",language);
        general.Children.Add(Ui.Text("As preferências são aplicadas imediatamente e salvas automaticamente."));
        general.Children.Add(CreateLocalControlPanel(out var cleanupLocal));
        var match=Tab("PARTIDA");match.Children.Add(Ui.Text("Tela da partida",true));
        var mode=Ui.Combo("Modo Caster","Modo SPEC");mode.Name="settings-mode";mode.SelectedIndex=settings.SpecMode?1:0;
        mode.SelectionChanged+=(_,_)=>Change(()=>{if(mode.SelectedIndex<0)return;bool old=settings.SpecMode;try{settings.SpecMode=mode.SelectedIndex==1;Championship.SetSpecMode(settings.SpecMode);SaveSettings();}catch{settings.SpecMode=old;Championship.SetSpecMode(old);throw;}});Row(match,"Tela da partida",mode);
        var rounds=Ui.Combo("Modo rápido","Modo clássico");rounds.Name="settings-rounds";rounds.SelectedIndex=settings.QuickRounds?0:1;
        rounds.SelectionChanged+=(_,_)=>Change(()=>{if(rounds.SelectedIndex<0)return;settings.QuickRounds=rounds.SelectedIndex==0;Championship.SetQuickMode(settings.QuickRounds);});Row(match,"Registro de rounds",rounds);
        var appearance=Tab("APARÊNCIA");appearance.Children.Add(Ui.Text("TEMA DO CASTERDESK",true));
        string[] themes=settings.BlackIceUnlocked?["standard","dark","light","black-ice"]:["standard","dark","light"];
        var theme=Ui.Combo(settings.BlackIceUnlocked?["Padrão","Escuro","Claro","Black Ice"]:["Padrão","Escuro","Claro"]);theme.Name="settings-theme";theme.SelectedIndex=Array.IndexOf(themes,settings.VisualTheme);
        if(Program.BlackIcePreview&&Ui.BlackIce&&!settings.BlackIceUnlocked){theme.SelectedIndex=-1;theme.PlaceholderText="Black Ice · prévia interna";}
        theme.SelectionChanged+=(_,_)=>Change(()=>{if(theme.SelectedIndex<0)return;string previous=settings.VisualTheme!;try{Appearance(themes[theme.SelectedIndex],pageContents.ToArray());PaintTabs();SaveSettings();}catch{Appearance(previous,pageContents.ToArray());throw;}});Row(appearance,"Tema",theme);
        appearance.Children.Add(Ui.Text("O tema altera somente o aplicativo. O painel do OBS mantém a aparência do OBS."));
        var table=Tab("TABELA");table.Children.Add(CreateTablePanel(out var cleanupTable));
        var backupPanel=CreateBackupPanel();var backup=Tab("BACKUP");backup.Children.Add(backupPanel.DetachContent());
        var obs=Tab("OBS");obs.Children.Add(CreateObsPanel());
        var dialog=new ModalSurface("Opções",root,940,810){Name="options-window"};optionsWindow=dialog;
        dialog.BeforeClose=async()=>{if(!await backupPanel.FinishEmbedded())return false;cleanupTable();cleanupLocal();return true;};
        backupPanel.EmbeddedClose=scheduled=>{dialog.Close();if(scheduled)_ = CloseAfterRestore();};
        async Task CloseAfterRestore(){await dialog.Completion;Notify(L.T("Restauração agendada. Ela será aplicada na próxima abertura do CasterDesk."));Close();}
        tabs.SelectedIndex=0;Present();PaintTabs();loading=false;
        try{await ShowSurface(dialog);}finally{optionsWindow=null;}
    }
}

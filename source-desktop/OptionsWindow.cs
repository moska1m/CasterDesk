using Avalonia.Styling;
namespace CasterDesk.Desktop;
public sealed partial class StudioShell
{
    internal async Task OpenOptions()
    {
        if(optionsWindow!=null){optionsWindow.Activate();return;}
        if(menuOpen)ToggleMenu();
        var root=new Grid{RowDefinitions=new("Auto,*,Auto"),Margin=new Thickness(20)};
        var tabs=new TabControl{Name="settings-tabs"};root.Children.Add(tabs);Grid.SetRow(tabs,1);
        tabs.Template=new global::Avalonia.Controls.Templates.FuncControlTemplate<TabControl>((control,scope)=>{
            var host=new global::Avalonia.Controls.Presenters.ContentPresenter{Name="PART_SelectedContentHost"};scope.Register("PART_SelectedContentHost",host);
            void Present(){host.Content=control.SelectedItem is TabItem item?item.Content:null;}
            control.PropertyChanged+=(_,e)=>{if(e.Property==TabControl.SelectedIndexProperty)Present();};Present();return host;
        });
        var navigation=new UniformGrid{Rows=1};root.Children.Add(navigation);
        var tabButtons=new List<Button>();
        void PaintTabs(){for(int i=0;i<tabButtons.Count;i++){var button=tabButtons[i];button.Foreground=Ui.Ink;button.Background=i==tabs.SelectedIndex?Ui.Panel:Brushes.Transparent;button.BorderBrush=i==tabs.SelectedIndex?Brush.Parse("#FF4335"):Ui.FrameLine;button.BorderThickness=new Thickness(0,0,0,i==tabs.SelectedIndex?3:1);}}
        tabs.SelectionChanged+=(_,e)=>{if(e.Source==tabs)PaintTabs();};
        StackPanel Tab(string title) {
            int index=tabs.Items.Count;var button=Ui.Button(title,()=>tabs.SelectedIndex=index);button.Margin=new Thickness(0);button.Padding=new Thickness(10,12);button.FontSize=14;button.FontWeight=FontWeight.Bold;tabButtons.Add(button);navigation.Children.Add(button);
            var panel=new StackPanel{Spacing=10,Margin=new Thickness(8,18)};
            tabs.Items.Add(new TabItem{Header=L.T(title),Name="settings-tab-"+tabs.Items.Count,Content=new ScrollViewer{Content=panel,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled}});return panel;
        }
        void Row(StackPanel panel,string label,Control control) {
            var row=new Grid{ColumnDefinitions=new("*,*"),Background=Ui.Panel,Margin=new Thickness(0,0,0,4)};
            var caption=Ui.Text(label);caption.VerticalAlignment=VerticalAlignment.Center;caption.Margin=new Thickness(14,8);
            control.Margin=new Thickness(8);control.VerticalAlignment=VerticalAlignment.Center;row.Children.Add(caption);row.Children.Add(control);Grid.SetColumn(control,1);panel.Children.Add(row);
        }
        bool loading=true;
        void dialogTitle(){if(optionsWindow!=null)optionsWindow.Title=L.T("Opções");}
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
        var match=Tab("PARTIDA");match.Children.Add(Ui.Text("Tela da partida",true));
        var mode=Ui.Combo("Modo Caster","Modo SPEC");mode.Name="settings-mode";mode.SelectedIndex=settings.SpecMode?1:0;
        mode.SelectionChanged+=(_,_)=>Change(()=>{if(mode.SelectedIndex<0)return;bool old=settings.SpecMode;try{settings.SpecMode=mode.SelectedIndex==1;Championship.SetSpecMode(settings.SpecMode);SaveSettings();}catch{settings.SpecMode=old;Championship.SetSpecMode(old);throw;}});Row(match,"Tela da partida",mode);
        var rounds=Ui.Combo("Modo rápido","Modo clássico");rounds.Name="settings-rounds";rounds.SelectedIndex=settings.QuickRounds?0:1;
        rounds.SelectionChanged+=(_,_)=>Change(()=>{if(rounds.SelectedIndex<0)return;settings.QuickRounds=rounds.SelectedIndex==0;Championship.SetQuickMode(settings.QuickRounds);});Row(match,"Registro de rounds",rounds);
        var appearance=Tab("APARÊNCIA");appearance.Children.Add(Ui.Text("TEMA DO CASTERDESK",true));
        var theme=Ui.Combo("Padrão","Escuro","Claro");theme.Name="settings-theme";string[] themes=["standard","dark","light"];theme.SelectedIndex=Array.IndexOf(themes,settings.VisualTheme);
        theme.SelectionChanged+=(_,_)=>Change(()=>{if(theme.SelectedIndex<0)return;string previous=settings.VisualTheme!;try{Appearance(themes[theme.SelectedIndex]);PaintTabs();SaveSettings();}catch{Appearance(previous);throw;}});Row(appearance,"Tema",theme);
        appearance.Children.Add(Ui.Text("O tema altera somente o aplicativo. O painel do OBS mantém a aparência do OBS."));
        var table=Tab("TABELA");table.Children.Add(Ui.Text("Tabela da partida",true));table.Children.Add(Ui.Text("Configure o arquivo Excel local e a conexão com o Google Sheets para a partida."));table.Children.Add(Ui.Button("Tabela da partida",()=>{optionsWindow?.Close();_ = OpenMatchTable();}));
        var backup=Tab("BACKUP");backup.Children.Add(Ui.Text("Backup e restauração",true));backup.Children.Add(Ui.Text("Crie uma cópia dos dados ou restaure um backup do CasterDesk."));var backupButton=Ui.Button("Backup e restauração",()=>{optionsWindow?.Close();_ = OpenBackup();});backupButton.Name="backup-menu";backup.Children.Add(backupButton);
        var obs=Tab("OBS");obs.Children.Add(Ui.Text("Painel OBS",true));obs.Children.Add(Ui.Text("Configure a conexão do painel com o OBS."));var obsButton=Ui.Button("Painel OBS",()=>{optionsWindow?.Close();_ = OpenObsPanel();});obsButton.Name="obs-menu";obs.Children.Add(obsButton);
        var close=Ui.Button("Fechar",()=>optionsWindow?.Close());close.HorizontalAlignment=HorizontalAlignment.Right;close.MinWidth=120;root.Children.Add(close);Grid.SetRow(close,2);
        var dialog=Ui.Dialog(this,"Opções",root,900,570);dialog.MinWidth=700;dialog.MinHeight=430;dialog.Name="options-window";optionsWindow=dialog;
        tabs.SelectedIndex=0;PaintTabs();loading=false;
        try{await dialog.ShowDialog(this);}finally{optionsWindow=null;}
    }
}

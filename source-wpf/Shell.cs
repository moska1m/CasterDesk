using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using CasterDesk.Core;

namespace CasterDesk.Wpf;
public sealed class Shell : Window
{
 readonly Settings settings;readonly ChampionshipView championship;
 readonly StudioView studio;readonly TextsView texts;
 readonly ContentControl page=new();readonly StackPanel drawer=new();readonly ColumnDefinition drawerWidth=new(){Width=new GridLength(0)};
 readonly Button menuButton;
 readonly TextBlock footer=Ui.Text("CasterDesk • "+BuildInfo.Version);readonly Button[] tabs=new Button[3];
 bool busy,restarting;int selected;
 public Shell()
 {
  Title="CasterDesk • "+BuildInfo.Version;Width=1440;Height=900;MinWidth=1050;MinHeight=680;WindowStartupLocation=WindowStartupLocation.CenterScreen;
  settings=Configuration.Read<Settings>("settings.json");Configuration.Normalize(settings);
  Theme(settings.DarkMode);
  SetResourceReference(BackgroundProperty,"CanvasBrush");TextOptions.SetTextFormattingMode(this,TextFormattingMode.Display);UseLayoutRounding=true;SnapsToDevicePixels=true;
  championship=new ChampionshipView();championship.SetQuickMode(settings.QuickRounds);
  studio=new StudioView(()=>championship.Matches,SiegeCatalog.Load(),championship.ApplyArtworkVeto);texts=new TextsView(settings);
  championship.MapConfirmed+=(match,map)=>{new StatsWindow(match,map){Owner=this}.ShowDialog();};
  var layout=new Grid();Content=layout;layout.RowDefinitions.Add(new(){Height=GridLength.Auto});layout.RowDefinitions.Add(new());layout.RowDefinitions.Add(new(){Height=GridLength.Auto});layout.ColumnDefinitions.Add(new());layout.ColumnDefinitions.Add(drawerWidth);
  var bar=new DockPanel{LastChildFill=false,Background=new SolidColorBrush(Color.FromRgb(10,17,24)),Margin=new Thickness(0,0,0,1)};layout.Children.Add(bar);Grid.SetColumnSpan(bar,2);
  var logo=new TextBlock{Text="CASTERDESK",Foreground=Brushes.White,FontSize=23,FontWeight=FontWeights.Bold,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(22,0,26,0)};bar.Children.Add(logo);
  string[] labels={"CAMPEONATO","TEXTOS PARA OBS","Central de imagens"};for(int n=0;n<3;n++){int index=n;tabs[n]=Ui.Button(labels[n],()=>Navigate(index));tabs[n].Margin=new Thickness(0);tabs[n].Padding=new Thickness(22,20,22,18);tabs[n].BorderThickness=new Thickness(0,0,0,4);bar.Children.Add(tabs[n]);}
  var menu=Ui.Button("",ToggleMenu);menuButton=menu;System.Windows.Automation.AutomationProperties.SetName(menu,L.T("Configurações e sobre"));menu.Content=Ui.Gear();menu.SetResourceReference(StyleProperty,"SettingsButton");menu.ToolTip=L.T("Configurações e sobre");menu.Width=52;menu.Padding=new Thickness(5);menu.Margin=new Thickness(10,6,12,6);DockPanel.SetDock(menu,Dock.Right);bar.Children.Add(menu);
  PreviewKeyDown+=(_,e)=>{if(e.Key==System.Windows.Input.Key.Escape&&drawerWidth.Width.Value>0){drawerWidth.Width=new GridLength(0);menuButton.Tag="";e.Handled=true;}};
  layout.Children.Add(page);Grid.SetRow(page,1);
  var menuScroll=new ScrollViewer{Content=drawer,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Margin=new Thickness(16,6,16,0)};layout.Children.Add(menuScroll);Grid.SetRow(menuScroll,1);Grid.SetColumn(menuScroll,1);
  footer.FontSize=12;footer.Margin=new Thickness(18,5,18,8);footer.SetResourceReference(TextBlock.ForegroundProperty,"MutedBrush");layout.Children.Add(footer);Grid.SetRow(footer,2);Grid.SetColumnSpan(footer,2);
  Navigate(0);Loaded+=(_,_)=>{WindowFit.Clamp(this);texts.StartLive();};
  if(Configuration.RecoveryNotice.Length>0)footer.Text=Configuration.RecoveryNotice;else if(MatchFiles.RecoveryNotice.Length>0)footer.Text=MatchFiles.RecoveryNotice;
  Closing+=(_,e)=>
  {
   if(busy||studio.Exporting||championship.Processing){e.Cancel=true;footer.Text=L.T("Salvando arquivos…");return;}
   if(!restarting&&texts.Running&&MessageBox.Show(this,L.T("Fechar e interromper a contagem? O TXT manterá o último texto."),L.T("Sair"),MessageBoxButton.YesNo)!=MessageBoxResult.Yes){e.Cancel=true;return;}
   try{Flush();}catch(Exception ex){e.Cancel=true;MessageBox.Show(this,ex.Message,"CasterDesk");}
  };
  Closed+=(_,_)=>{texts.Stop();studio.Stop();championship.Stop();};
 }
 void Navigate(int n)
 {
  if(studio.Exporting||busy||championship.Processing)return;
  try{championship.FlushNames();if(!studio.Flush())return;selected=n;page.Content=n switch{0=>championship,1=>texts,_=>studio};if(n==2)studio.Refresh(championship.CurrentMatchId);for(int i=0;i<tabs.Length;i++){tabs[i].BorderBrush=i==n?new SolidColorBrush(Color.FromRgb(231,72,69)):Brushes.Transparent;tabs[i].Background=i==n?new SolidColorBrush(Color.FromRgb(28,43,57)):Brushes.Transparent;tabs[i].Foreground=Brushes.White;}drawerWidth.Width=new GridLength(0);menuButton.Tag="";}
  catch(Exception ex){MessageBox.Show(this,ex.Message,"CasterDesk");}
 }
 void ToggleMenu(){if(drawerWidth.Width.Value>0){drawerWidth.Width=new GridLength(0);menuButton.Tag="";return;}drawerWidth.Width=new GridLength(290);menuButton.Tag="open";Menu();}
 internal void CheckPage(int index,bool menu=false){Navigate(index);if(menu)ToggleMenu();}
 internal Task CheckPreview()=>studio.Render();
 void Menu()
 {
  drawer.Children.Clear();drawer.Children.Add(Ui.Text("Configurações",true));
  drawer.Children.Add(Ui.Text("Registro de rounds",true));drawer.Children.Add(Ui.Button("Modo rápido",()=>{settings.QuickRounds=true;championship.SetQuickMode(true);SaveSettings();Menu();},settings.QuickRounds));drawer.Children.Add(Ui.Button("Lista",()=>{settings.QuickRounds=false;championship.SetQuickMode(false);SaveSettings();Menu();},!settings.QuickRounds));
  drawer.Children.Add(Ui.Text("Aparência",true));drawer.Children.Add(Ui.Button("Escuro",()=>SetTheme(true),settings.DarkMode));drawer.Children.Add(Ui.Button("Claro",()=>SetTheme(false),!settings.DarkMode));
  drawer.Children.Add(Ui.Text("Idioma",true));var language=Ui.Combo("Português","English","Español");language.SelectedIndex=Array.IndexOf(L.Languages,settings.Language);drawer.Children.Add(language);language.SelectionChanged+=(_,_)=>{settings.Language=L.Languages[Math.Max(0,language.SelectedIndex)];SaveSettings();footer.Text=L.T("Idioma salvo. Feche e reabra o CasterDesk para aplicar.");};
  drawer.Children.Add(Ui.Text("Arquivos",true));drawer.Children.Add(Ui.Button("Criar backup",()=>_=Backup()));drawer.Children.Add(Ui.Button("Restaurar backup",()=>_=Restore()));drawer.Children.Add(Ui.Button("Abrir pasta da partida",()=>{var match=championship.Matches.FirstOrDefault(m=>m.Id==championship.CurrentMatchId);if(match!=null)Ui.OpenFolder(MatchFiles.Ensure(match));}));drawer.Children.Add(Ui.Button("Abrir pasta de dados",()=>Ui.OpenFolder(AppPaths.DataDirectory)));drawer.Children.Add(Ui.Button("Sobre",About));
 }
 void SetTheme(bool dark){settings.DarkMode=dark;Theme(dark);SaveSettings();Menu();}
 static void Theme(bool dark)
 {var pairs=dark?new[]{("CanvasBrush","#101923"),("PanelBrush","#1B2B3B"),("InputBrush","#122131"),("TextBrush","#F0F4F8"),("MutedBrush","#ABBACA"),("LineBrush","#40566C")}:new[]{("CanvasBrush","#EAF0F5"),("PanelBrush","#FFFFFF"),("InputBrush","#F1F5F8"),("TextBrush","#182C3F"),("MutedBrush","#50667B"),("LineBrush","#A0B4C6")};foreach(var p in pairs)Application.Current.Resources[p.Item1]=new SolidColorBrush((Color)ColorConverter.ConvertFromString(p.Item2));}
 void SaveSettings(){try{MatchFiles.AtomicText(AppPaths.ConfigFile("settings.json"),JsonSerializer.Serialize(settings));}catch(Exception ex){MessageBox.Show(this,ex.Message,"CasterDesk");}}
 void Flush(){championship.FlushNames();if(!studio.Flush())throw new IOException(L.T("Não foi possível salvar as preferências: "));texts.Save(false);MatchFiles.AtomicText(AppPaths.ConfigFile("settings.json"),JsonSerializer.Serialize(settings));}
 async Task Backup()
 {
  if(busy||studio.Exporting||championship.Processing)return;var d=new SaveFileDialog{Filter="Backup CasterDesk|*.zip",InitialDirectory=AppPaths.BackupDirectory,FileName=$"CasterDesk_Backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.zip"};if(d.ShowDialog(this)!=true)return;
  try{Flush();busy=true;texts.WritesSuspended=true;IsEnabled=false;var progress=new Progress<BackupProgress>(p=>footer.Text=L.F($"Backup: {p.Files} arquivos • {p.Bytes/1048576.0:0.0} MB"));await Task.Run(()=>DataBackup.Create(AppPaths.DataDirectory,d.FileName,p=>((IProgress<BackupProgress>)progress).Report(p)));footer.Text=L.T("Backup concluído");MessageBox.Show(this,L.T("Backup criado. Arquivos salvos fora da pasta do CasterDesk não estão incluídos. Guarde uma cópia em outro disco."),"CasterDesk");}catch(Exception ex){MessageBox.Show(this,ex.Message,"CasterDesk");}finally{busy=false;texts.WritesSuspended=false;IsEnabled=true;}
 }
 async Task Restore()
 {
  if(busy||studio.Exporting||championship.Processing)return;var d=new OpenFileDialog{Filter="Backup CasterDesk|*.zip",InitialDirectory=AppPaths.BackupDirectory};if(d.ShowDialog(this)!=true)return;
  string stage=Path.Combine(AppPaths.BackupDirectory,"verificacao-"+Guid.NewGuid().ToString("N"));
  try
  {
   busy=true;IsEnabled=false;var manifest=await Task.Run(()=>DataBackup.Extract(d.FileName,stage));int conflicts=DataBackup.Conflicts(AppPaths.DataDirectory,manifest);IsEnabled=true;
   bool replace=false;if(conflicts>0){var choice=MessageBox.Show(this,L.F($"Backup verificado: {manifest.Files.Count} arquivos.\n{conflicts} arquivos existentes têm conteúdo diferente (incluindo partidas e configurações).\n\nSim: usar as versões do backup.\nNão: manter as versões atuais e adicionar apenas arquivos ausentes.\nCancelar: não restaurar.\n\nUma cópia completa dos dados atuais será criada antes da restauração."),L.T("Conflitos no backup"),MessageBoxButton.YesNoCancel);if(choice==MessageBoxResult.Cancel)return;replace=choice==MessageBoxResult.Yes;}
   if(MessageBox.Show(this,L.F($"Restaurar {manifest.Files.Count} arquivos? O CasterDesk será reiniciado. Uma cópia de segurança será guardada em Backups.\n\nConfira depois os caminhos externos usados no OBS."),L.T("Restaurar backup"),MessageBoxButton.OKCancel)!=MessageBoxResult.OK)return;
   Flush();string copy=Path.Combine(AppPaths.BackupDirectory,"restaurar-"+Guid.NewGuid().ToString("N")+".zip");File.Copy(d.FileName,copy);MatchFiles.AtomicText(Path.Combine(AppPaths.DataDirectory,"pending-restore.json"),JsonSerializer.Serialize(new{Archive=copy,Replace=replace}));busy=false;restarting=true;Close();if(!IsVisible)Process.Start(new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=true});
  }
  catch(Exception ex){MessageBox.Show(this,ex.Message,"CasterDesk");}finally{busy=false;IsEnabled=true;try{if(Directory.Exists(stage))DataBackup.RemoveStage(stage);}catch(Exception ex){AppLog.Write("Restore staging cleanup: "+ex.Message);}}
 }
 void About(){var panel=new StackPanel{Margin=new Thickness(24)};panel.Children.Add(Ui.Text("CASTERDESK",true));panel.Children.Add(Ui.Text(Title));panel.Children.Add(Ui.Text("Ferramentas para community casters de Rainbow Six Siege. Não é um produto oficial, patrocinado ou aprovado pela Ubisoft."));panel.Children.Add(Ui.Button("Abrir pasta de dados",()=>Ui.OpenFolder(AppPaths.DataDirectory)));var dialog=new Window{Title=L.T("Sobre"),Owner=this,Width=550,Height=300,Content=panel,WindowStartupLocation=WindowStartupLocation.CenterOwner};dialog.SetResourceReference(BackgroundProperty,"CanvasBrush");dialog.ShowDialog();}
}

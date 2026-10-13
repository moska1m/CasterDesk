using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using CronoLive;

namespace CasterDesk.Wpf;
public sealed class TextsView : Grid
{
 readonly Settings settings;readonly CastingSettings casting;
 readonly TextBox[] names=new TextBox[4],handles=new TextBox[4];
 readonly TextBox folder=new(),initial=new(),message=new(),output=new();
 readonly ComboBox format=Ui.Combo("mm:ss","hh:mm:ss");readonly TextBlock readout=Ui.Text(""),status=Ui.Text(""),castingStatus=Ui.Text(""),clockState=Ui.Text("");
 readonly Countdown clock=new();readonly DispatcherTimer pulse=new(){Interval=TimeSpan.FromMilliseconds(250)};
 string? last;long retry;bool durationEdited;
 public bool WritesSuspended {get;set;}
 public TextsView(Settings settings)
 {
  this.settings=settings;Configuration.Normalize(settings);casting=Configuration.Read<CastingSettings>("casting-settings.json");Configuration.Normalize(casting);
  Margin=new Thickness(20);ColumnDefinitions.Add(new());ColumnDefinitions.Add(new());
  var left=new StackPanel();var right=new StackPanel();Children.Add(new ScrollViewer{Content=left,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});var rightScroll=new ScrollViewer{Content=right,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Children.Add(rightScroll);SetColumn(rightScroll,1);
  string[] labels={"NARRADOR / CASTER","COMENTARISTA","CONVIDADO 1","CONVIDADO 2"};var people=new Grid();people.ColumnDefinitions.Add(new());people.ColumnDefinitions.Add(new());people.RowDefinitions.Add(new(){Height=GridLength.Auto});people.RowDefinitions.Add(new(){Height=GridLength.Auto});left.Children.Add(people);
  for(int n=0;n<4;n++){var p=new StackPanel();p.Children.Add(Ui.Text(labels[n],true));p.Children.Add(Ui.Text("Nome"));names[n]=new TextBox{Text=casting.People[n].Name,Margin=new Thickness(0,0,0,10)};p.Children.Add(names[n]);p.Children.Add(Ui.Text("@"));handles[n]=new TextBox{Text=casting.People[n].Handle};p.Children.Add(handles[n]);var card=Ui.Panel(p);people.Children.Add(card);SetColumn(card,n%2);SetRow(card,n/2);}
  var files=new StackPanel();files.Children.Add(Ui.Text("PASTA DOS OITO ARQUIVOS TXT",true));folder.Text=casting.Folder;files.Children.Add(folder);files.Children.Add(Ui.Button("Escolher pasta",()=>{var d=new OpenFolderDialog{InitialDirectory=Directory.Exists(folder.Text)?folder.Text:AppPaths.TextDirectory};if(d.ShowDialog(Window.GetWindow(this))==true)folder.Text=d.FolderName;}));files.Children.Add(Ui.Button("Aplicar no OBS",()=>{try{SaveCasting();CastingFiles.Write(folder.Text,casting.People);castingStatus.Text=L.T("TXT atualizado • ")+DateTime.Now.ToString("HH:mm:ss");}catch(Exception ex){castingStatus.Text=ex.Message;}},true));files.Children.Add(castingStatus);left.Children.Add(Ui.Panel(files));
  var timer=new StackPanel();timer.Children.Add(Ui.Text("CRONÔMETRO",true));timer.Children.Add(Ui.Text("Tempo inicial:"));initial.Text=DurationText((double)settings.Initial*60);timer.Children.Add(initial);timer.Children.Add(Ui.Text("Formato de saída:"));format.SelectedIndex=settings.Format=="hh:mm:ss"?1:0;timer.Children.Add(format);timer.Children.Add(Ui.Text("Mensagem final:"));message.Text=settings.Message;timer.Children.Add(message);readout.FontSize=46;readout.Margin=new Thickness(0,18,0,12);timer.Children.Add(readout);timer.Children.Add(clockState);
  var actions=new WrapPanel();actions.Children.Add(Ui.Button("Iniciar / Retomar",()=>{try{Start();}catch(Exception ex){status.Text=ex.Message;}},true));actions.Children.Add(Ui.Button("Pausar",()=>{clock.Pause();Update();}));actions.Children.Add(Ui.Button("Resetar tempo",()=>{try{clock.Reset(Parse(initial.Text));durationEdited=false;Update();}catch(Exception ex){status.Text=ex.Message;}}));timer.Children.Add(actions);
  timer.Children.Add(Ui.Text("Acrescentar tempo sem pausar",true));
  var additions=new WrapPanel();for(int n=0;n<settings.Presets.Length;n++){int index=n;var field=new TextBox{Text=settings.Presets[n].ToString(),Width=65,Margin=new Thickness(4)};var button=Ui.Button($"+ {settings.Presets[n]} min",()=>{if(!decimal.TryParse(field.Text,out decimal value)||value<=0||value>1440){status.Text=L.T("Use um valor de minutos entre 0 e 1440.");return;}settings.Presets[index]=value;clock.Add((double)value*60);Update();});field.TextChanged+=(_,_)=>{if(decimal.TryParse(field.Text,out decimal value)&&value>0&&value<=1440){settings.Presets[index]=value;button.Content=$"+ {value} min";}};additions.Children.Add(button);additions.Children.Add(field);}timer.Children.Add(additions);
  right.Children.Add(Ui.Panel(timer));var target=new StackPanel();target.Children.Add(Ui.Text("Arquivo de saída • OBS",true));output.Text=settings.Output;target.Children.Add(output);target.Children.Add(Ui.Button("Escolher TXT",()=>{var d=new SaveFileDialog{Filter="TXT|*.txt",FileName="timer.txt"};if(d.ShowDialog(Window.GetWindow(this))==true){output.Text=d.FileName;last=null;Update();}}));target.Children.Add(Ui.Button("Copiar caminho",()=>Clipboard.SetText(output.Text)));target.Children.Add(status);right.Children.Add(Ui.Panel(target));
  clock.Reset((double)settings.Initial*60);Update(false);pulse.Tick+=(_,_)=>{clock.Tick();Update();};format.SelectionChanged+=(_,_)=>Update();message.TextChanged+=(_,_)=>{if(clock.Finished)Update();};
  // Construction never writes a TXT: only the normal loaded view starts live output.
  Loaded+=(_,_)=>{pulse.Start();Update();};
  initial.TextChanged+=(_,_)=>durationEdited=true;
  output.TextChanged+=(_,_)=>{last=null;retry=0;if(IsLoaded)Update();};
 }
 internal static string DurationText(double seconds){int total=(int)Math.Round(seconds);return $"{total/3600:00}:{total/60%60:00}:{total%60:00}";}
 internal void Start(){if(durationEdited){clock.Reset(Parse(initial.Text));durationEdited=false;}clock.Start();Update(IsLoaded);}
 internal void CheckDuration(string text)=>initial.Text=text;
 internal double Remaining=>clock.Remaining;
 internal static double Parse(string value){if(value=="24:00:00")return 86400;if(!TimeSpan.TryParseExact(value,@"hh\:mm\:ss",null,out var duration))throw new InvalidDataException(L.T("Tempo inválido. Use hh:mm:ss."));return duration.TotalSeconds;}
 void Update(bool write=true)
 {
  int seconds=(int)Math.Ceiling(clock.Remaining);string text=clock.Finished?message.Text:format.SelectedIndex==1?$"{seconds/3600:00}:{seconds/60%60:00}:{seconds%60:00}":$"{seconds/60:00}:{seconds%60:00}";
  readout.Text=text;clockState.Text=L.T(clock.Running?"● Em andamento":clock.Finished?"Contagem encerrada":"Pausado / pronto");if(!write||WritesSuspended||text==last||Environment.TickCount64<retry)return;
  try{lock(FileBatch.Gate){using var file=new FileStream(output.Text,FileMode.OpenOrCreate,FileAccess.Write,FileShare.ReadWrite);byte[] bytes=new UTF8Encoding(false).GetBytes(text);file.Write(bytes);file.SetLength(bytes.Length);file.Flush();}last=text;retry=0;status.Text=L.T("TXT atualizado • ")+DateTime.Now.ToString("HH:mm:ss");}
  catch(Exception ex){retry=Environment.TickCount64+1000;status.Text=L.T("Não foi possível gravar o TXT: ")+ex.Message;}
 }
 public void StartLive(){pulse.Start();Update();}
 public void Save(bool validateTimer=true)
 {
  SaveCasting();try{settings.Initial=(decimal)Parse(initial.Text)/60;}catch(InvalidDataException){if(validateTimer)throw;status.Text=L.T("Tempo inválido: mantida a duração salva anteriormente.");}settings.Format=format.SelectedIndex==1?"hh:mm:ss":"mm:ss";settings.Message=message.Text;settings.Output=output.Text;
  SaveCasting();
 }
 void SaveCasting()
 {
  casting.Folder=folder.Text;for(int n=0;n<4;n++){casting.People[n].Name=names[n].Text;casting.People[n].Handle=handles[n].Text;}
  MatchFiles.AtomicText(AppPaths.ConfigFile("casting-settings.json"),JsonSerializer.Serialize(casting));
 }
 public bool Running=>clock.Running;
 public void Stop()=>pulse.Stop();
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using CronoLive;

namespace CasterDesk.Wpf;
public sealed class StatsWindow : Window
{
 readonly SiegeMatch match;readonly MapRecord map;
 readonly DataGrid table=new(){AutoGenerateColumns=false,CanUserAddRows=false,CanUserDeleteRows=false,HeadersVisibility=DataGridHeadersVisibility.Column};
 readonly ObservableCollection<Row> rows=new();readonly Image image=new(){Stretch=Stretch.Uniform};
 readonly ComboBox top,coverage=Ui.Combo("Estatísticas completas","Estatísticas parciais / remake","Sem print / sem estatísticas");
 readonly TextBlock status=Ui.Text("E = eliminações · M = mortes · A = assistências. Dados lidos precisam de revisão.");
 readonly CancellationTokenSource lifetime=new();readonly Button confirm;CancellationTokenSource? reading;
 System.Drawing.Bitmap? original;bool busy,closed,edited;int generation;
 public StatsWindow(SiegeMatch match,MapRecord map)
 {
  this.match=match;this.map=map;Title=L.T("Conferir estatísticas");Width=1280;Height=780;MinWidth=920;MinHeight=580;WindowStartupLocation=WindowStartupLocation.CenterOwner;SetResourceReference(BackgroundProperty,"CanvasBrush");
  var body=new Grid{Margin=new Thickness(20)};Content=body;body.RowDefinitions.Add(new(){Height=GridLength.Auto});body.RowDefinitions.Add(new());body.RowDefinitions.Add(new(){Height=GridLength.Auto});body.ColumnDefinitions.Add(new(){Width=new GridLength(1.1,GridUnitType.Star)});body.ColumnDefinitions.Add(new());
  var actions=new WrapPanel();actions.Children.Add(Ui.Button("Abrir print",Open));actions.Children.Add(Ui.Button("Colar print · Ctrl+V",Paste));actions.Children.Add(Ui.Button("Ler novamente",()=>_=Read()));actions.Children.Add(Ui.Button("Conciliar jogadores",Reconcile));body.Children.Add(actions);
  var choices=new StackPanel{Margin=new Thickness(16,0,0,12)};top=Ui.Combo();top.Items.Add(match.TeamA);top.Items.Add(match.TeamB);top.SelectedIndex=0;choices.Children.Add(Ui.Text("Equipe no topo do print:"));choices.Children.Add(top);choices.Children.Add(coverage);body.Children.Add(choices);Grid.SetColumn(choices,1);coverage.SelectedIndex=map.Statistics?.Status switch{"partial"=>1,"missing"=>2,_=>0};
  var frame=new Border{Background=new SolidColorBrush(Color.FromRgb(11,16,23)),Padding=new Thickness(12),Margin=new Thickness(0,0,16,12),Child=image};body.Children.Add(frame);Grid.SetRow(frame,1);body.Children.Add(table);Grid.SetColumn(table,1);Grid.SetRow(table,1);table.Margin=new Thickness(0,0,0,12);table.ItemsSource=rows;
  var centered=new Style(typeof(TextBlock));centered.Setters.Add(new Setter(TextBlock.TextAlignmentProperty,TextAlignment.Center));centered.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center));
  foreach(var pair in new[]{("Equipe","TeamName"),("Jogador","Name"),("E","Kills"),("M","Deaths"),("A","Assists")})table.Columns.Add(new DataGridTextColumn{Header=L.T(pair.Item1),Binding=new Binding(pair.Item2){UpdateSourceTrigger=UpdateSourceTrigger.LostFocus},IsReadOnly=pair.Item2=="TeamName",Width=new DataGridLength(pair.Item2=="Name"?2:1,DataGridLengthUnitType.Star),ElementStyle=centered});
  table.SizeChanged+=(_,_)=>{double width=Math.Max(460,table.ActualWidth-22);double[] weights={.25,.36,.13,.13,.13};for(int n=0;n<5;n++)table.Columns[n].Width=new DataGridLength(width*weights[n]);};
  var bottom=new DockPanel();confirm=Ui.Button("Confirmar estatísticas",Save,true);DockPanel.SetDock(confirm,Dock.Right);bottom.Children.Add(confirm);bottom.Children.Add(Ui.Button("Verificar OCR",()=>_=Diagnose()));bottom.Children.Add(status);body.Children.Add(bottom);Grid.SetRow(bottom,2);Grid.SetColumnSpan(bottom,2);
  Fill(map.Statistics?.Players??new List<PlayerStatistic>());
  table.CellEditEnding+=(_,_)=>edited=true;
  top.SelectionChanged+=(_,_)=>{foreach(var row in rows){row.Team=1-row.Team;row.TeamName=match.Team(row.Team);row.PlayerId="";}edited=true;table.Items.Refresh();};
  PreviewKeyDown+=(_,e)=>{if(e.Key==Key.V&&Keyboard.Modifiers.HasFlag(ModifierKeys.Control)&&Clipboard.ContainsImage()){e.Handled=true;Paste();}};
  string? file=MatchFiles.Resolve(match,map.Statistics?.Screenshot);if(file!=null){using var bitmap=System.Drawing.Image.FromFile(file);original=new System.Drawing.Bitmap(bitmap);image.Source=Ui.Bitmap(original);}
  Loaded+=(_,_)=>WindowFit.Clamp(this);
  Closed+=(_,_)=>{closed=true;generation++;reading?.Cancel();lifetime.Cancel();original?.Dispose();};
 }
 void Fill(IReadOnlyList<PlayerStatistic> data)
 {rows.Clear();for(int team=0;team<2;team++){var group=data.Where(p=>p.Team==team).ToList();for(int n=0;n<Math.Max(5,group.Count);n++){var p=group.ElementAtOrDefault(n);rows.Add(new Row{PlayerId=p?.PlayerId??"",Team=team,TeamName=match.Team(team),Name=p?.Name??"",Kills=p?.Kills?.ToString()??"",Deaths=p?.Deaths?.ToString()??"",Assists=p?.Assists?.ToString()??""});}}edited=false;}
 async Task Diagnose(){try{status.Text=L.T("Verificando OCR…");status.Text=await StatisticsData.Diagnose(lifetime.Token);}catch(OperationCanceledException){}catch(Exception ex){if(!closed)status.Text=ex.Message;}}
 void Open()
 {var d=new OpenFileDialog{Filter=L.T("Imagem PNG ou JPEG|*.png;*.jpg;*.jpeg")};if(d.ShowDialog(this)!=true)return;try{if(new FileInfo(d.FileName).Length>40_000_000)throw new InvalidDataException(L.T("Use uma imagem com até 40 MB."));using var im=System.Drawing.Image.FromFile(d.FileName);SetImage(im);}catch(Exception ex){status.Text=ex.Message;}}
 void Paste()
 {try{var source=Clipboard.GetImage();if(source==null){status.Text=L.T("Copie uma imagem antes de colar.");return;}if((long)source.PixelWidth*source.PixelHeight>50_000_000)throw new InvalidDataException(L.T("A imagem é muito grande."));var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(source));using var stream=new MemoryStream();encoder.Save(stream);stream.Position=0;using var im=System.Drawing.Image.FromStream(stream);SetImage(im);}catch(Exception ex){status.Text=ex.Message;}}
 void SetImage(System.Drawing.Image im)
 {if((long)im.Width*im.Height>50_000_000)throw new InvalidDataException(L.T("A imagem é muito grande."));if(edited&&MessageBox.Show(this,L.T("Substituir os dados editados pela leitura de um novo print?"),Title,MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;edited=false;generation++;reading?.Cancel();original?.Dispose();original=new System.Drawing.Bitmap(im);image.Source=Ui.Bitmap(original);coverage.SelectedIndex=0;_=Read();}
 async Task Read()
 {
  if(original==null)return;if(edited&&MessageBox.Show(this,L.T("Substituir os dados editados por uma nova leitura?"),Title,MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;
  reading?.Cancel();var cancellation=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);reading=cancellation;int request=++generation;int topTeam=top.SelectedIndex;busy=true;confirm.IsEnabled=false;status.Text=L.T("Lendo print… Confira os nomes e os números ao terminar.");string file=Path.Combine(Path.GetTempPath(),"casterdesk-wpf-ocr-"+Guid.NewGuid().ToString("N")+".png");edited=false;
  try
  {
   float scale=Math.Min(1,2500f/Math.Max(original.Width,original.Height));using(var bitmap=new System.Drawing.Bitmap(original,Math.Max(1,(int)(original.Width*scale)),Math.Max(1,(int)(original.Height*scale))))bitmap.Save(file,System.Drawing.Imaging.ImageFormat.Png);
   var doc=await StatisticsData.ReadImage(file,cancellation.Token);if(closed||request!=generation)return;if(edited){status.Text=L.T("Leitura descartada para preservar sua edição manual.");return;}var data=StatisticsData.Parse(doc);if(topTeam==1)foreach(var p in data)p.Team=1-p.Team;Fill(data);bool complete=data.Count==10&&data.All(p=>p.Kills.HasValue&&p.Deaths.HasValue&&p.Assists.HasValue&&!string.IsNullOrWhiteSpace(p.Name));coverage.SelectedIndex=complete?0:1;status.Text=L.T(complete?"Leitura concluída. Confira equipe, nome e E/M/A de cada jogador.":"Não foi possível identificar dez jogadores. Preencha os campos ou marque como parcial.");
  }
  catch(OperationCanceledException){if(!closed&&request==generation)status.Text=L.T("Leitura cancelada. Os campos podem ser preenchidos manualmente.");}
  catch(Exception ex){if(!closed&&request==generation)status.Text=ex.Message;}
  finally{cancellation.Dispose();if(File.Exists(file))File.Delete(file);if(request==generation){reading=null;busy=false;confirm.IsEnabled=true;}}
 }
 void Save()
 {
  if(busy)return;
  try
  {
   table.CommitEdit(DataGridEditingUnit.Cell,true);table.CommitEdit(DataGridEditingUnit.Row,true);
   var data=new MapStatistics{Status=coverage.SelectedIndex switch{1=>"partial",2=>"missing",_=>"complete"},ConfirmedAt=DateTimeOffset.Now};
   int? Number(string s)=>string.IsNullOrWhiteSpace(s)?null:int.TryParse(s,out int n)?n:throw new InvalidDataException(L.T("Use números inteiros nos campos E/M/A."));
   if(data.Status!="missing")foreach(var r in rows.Where(r=>!string.IsNullOrWhiteSpace(r.Name)))data.Players.Add(new(){PlayerId=r.PlayerId,Team=r.Team,Name=r.Name.Trim(),Kills=Number(r.Kills),Deaths=Number(r.Deaths),Assists=Number(r.Assists)});
   StatisticsData.Validate(data);
   MatchFiles.SaveStatistics(match,map,data,original);DialogResult=true;
  }
  catch(Exception ex){status.Text=ex.Message;}
 }
 void Reconcile()
 {
  table.CommitEdit(DataGridEditingUnit.Cell,true);table.CommitEdit(DataGridEditingUnit.Row,true);
  var panel=new StackPanel{Margin=new Thickness(18)};panel.Children.Add(Ui.Text("Associe variações do nome ao mesmo jogador da série. Nenhuma associação aproximada é feita automaticamente."));
  var choices=new List<(Row Row,ComboBox Box)>();
  foreach(var row in rows.Where(r=>!string.IsNullOrWhiteSpace(r.Name)))
  {
   panel.Children.Add(Ui.Text(match.Team(row.Team)+" · "+row.Name));var box=Ui.Combo();var known=match.Maps.SelectMany(m=>m.Statistics?.Players??new List<PlayerStatistic>()).Where(p=>p.Team==row.Team&&p.PlayerId!="").GroupBy(p=>p.PlayerId).Select(g=>g.First()).ToList();
   box.Items.Add(new IdentityChoice("",L.T("Novo jogador")));foreach(var player in known)box.Items.Add(new IdentityChoice(player.PlayerId,player.Name));box.SelectedItem=box.Items.OfType<IdentityChoice>().FirstOrDefault(c=>c.Id==row.PlayerId)??box.Items[0];panel.Children.Add(box);choices.Add((row,box));
  }
  var dialog=new Window{Title=L.T("Conciliar jogadores"),Owner=this,Width=550,Height=Math.Min(650,SystemParameters.WorkArea.Height),WindowStartupLocation=WindowStartupLocation.CenterOwner};dialog.SetResourceReference(BackgroundProperty,"CanvasBrush");
  panel.Children.Add(Ui.Button("Confirmar",()=>{foreach(var choice in choices)choice.Row.PlayerId=(choice.Box.SelectedItem as IdentityChoice)?.Id??"";edited=true;dialog.DialogResult=true;},true));dialog.Content=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};dialog.ShowDialog();
 }
 sealed record IdentityChoice(string Id,string Name){public override string ToString()=>Name;}
 public sealed class Row{public string PlayerId{get;set;}="";public int Team{get;set;}public string TeamName{get;set;}="";public string Name{get;set;}="";public string Kills{get;set;}="";public string Deaths{get;set;}="";public string Assists{get;set;}="";}
}

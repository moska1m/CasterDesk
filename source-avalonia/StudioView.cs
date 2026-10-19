using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;





using CronoLive;

namespace CasterDesk.Avalonia;
// The view owns only presentation. Native art rendering and match storage remain shared.
public sealed class StudioView : Grid
{
 readonly Func<IReadOnlyList<SiegeMatch>> list;
 readonly SiegeCatalog catalog;
 readonly ComboBox matches=Ui.Combo(),aspect=Ui.Combo(),maps=Ui.Combo(),mode=Ui.Combo("Com fundo","Transparente · OBS"),mvp=Ui.Combo();
 readonly TabControl kind=new(){Margin=new Thickness(0)};
 readonly ImagePreferences preferences=ConfigurationStore.Read<ImagePreferences>("image-preferences.json");
 int[] formats={1,2,1,1,1};readonly Dictionary<string,int[]> rememberedFormats=new();readonly Dictionary<(string,ArtKind),string> rememberedMaps=new();readonly SemaphoreSlim renderGate=new(1,1);
 readonly StackPanel vetoOptions=new();readonly Func<string,ImageBrand,bool>? applyVeto;
 readonly TextBox eventName=new(){MaxLength=80};
 readonly Grid channelRow=new(){ColumnDefinitions=new("Auto,*")};
 readonly TextBox channel=new(){MaxLength=120,PlaceholderText="seucanal"};readonly TextBlock channelTitle=Ui.Text("CANAL DA TRANSMISSÃO",true),channelHint=Ui.Text("twitch.tv/");
 readonly Image preview=new(){Stretch=Stretch.Uniform};
 readonly StackPanel options=new(){Margin=new Thickness(0,0,18,0)},footer=new();
 readonly TextBlock status=Ui.Text(""),background=Ui.Text(""),stats=Ui.Text(""),mapTitle=Ui.Text("MAPA",true),mvpTitle=Ui.Text("MVP",true),modeTitle=Ui.Text("PRÉVIA",true);
 readonly Button generate,statsEdit,copy,reconcile;
 readonly TextBox accent=new(){Name="art-accent",MaxLength=7};
 readonly ComboBox palette=Ui.Combo("Vermelho","Azul","Branco");
 readonly DispatcherTimer debounce=new(){Interval=TimeSpan.FromMilliseconds(400)};
 ImageBrand brand=new();SiegeMatch? owner;
 List<ResultPage> rendered=new();bool loading,exporting,applying,closed,brandDirty,channelPreferenceDirty;int generation;
 string renderedSnapshot="";
 Dictionary<string,global::Avalonia.Media.Imaging.Bitmap> previewPages=new();
 public SiegeMatch? Selected=>matches.SelectedItem as SiegeMatch;
 readonly ArtKind[] categories=[ArtKind.Announcement,ArtKind.MapVeto,ArtKind.MapStats,ArtKind.SeriesStats];
 ArtKind Kind=>categories[Math.Clamp(kind.SelectedIndex,0,categories.Length-1)];
 ExportAspect Aspect=>(ExportAspect)Math.Max(0,aspect.SelectedIndex);
 MapRecord? Map=>Selected?.Maps.ElementAtOrDefault(maps.SelectedIndex);
 public StudioView(Func<IReadOnlyList<SiegeMatch>> list,SiegeCatalog catalog,Func<string,ImageBrand,bool>? applyVeto=null)
 {
  this.list=list;this.catalog=catalog;this.applyVeto=applyVeto;Margin=new Thickness(18);Background=Ui.Canvas;
  RowDefinitions.Add(new(){Height=GridLength.Auto});RowDefinitions.Add(new(){Height=GridLength.Auto});RowDefinitions.Add(new());
  ColumnDefinitions.Add(new(){Width=new GridLength(370)});ColumnDefinitions.Add(new());
  var navigation=new Grid{ColumnDefinitions=new("*,Auto"),Margin=new Thickness(0,0,0,8)};Children.Add(navigation);SetColumnSpan(navigation,2);
  var tabs=new StackPanel{Orientation=Orientation.Horizontal};navigation.Children.Add(tabs);
  string[] labels=["Anúncio","Map veto","Estatísticas por mapa","Estatísticas da série"];
  var buttons=new List<Button>();
  for(int i=0;i<labels.Length;i++) {
   int index=i;kind.Items.Add(new TabItem{Header=L.T(labels[i])});
   var button=Ui.Button(labels[i],()=>{if(!exporting&&!applying)kind.SelectedIndex=index;});button.Name="art-tab-"+i;button.Margin=new Thickness(0);button.Padding=new Thickness(20,10);buttons.Add(button);tabs.Children.Add(button);
  }
  void PaintTabs(){for(int i=0;i<buttons.Count;i++){buttons[i].Background=i==kind.SelectedIndex?Ui.Panel:Brushes.Transparent;buttons[i].BorderThickness=new Thickness(0,0,0,3);buttons[i].BorderBrush=i==kind.SelectedIndex?Brush.Parse("#FF4335"):Brushes.Transparent;}}
  kind.PropertyChanged+=(_,e)=>{if(e.Property==IsEnabledProperty)tabs.IsEnabled=kind.IsEnabled;};
  kind.SelectedIndex=0;PaintTabs();kind.SelectionChanged+=(_,_)=>PaintTabs();
  matches.Name="art-match-selector";matches.Width=300;matches.Height=32;matches.MinHeight=0;matches.Margin=new Thickness(8,0,0,0);matches.VerticalAlignment=VerticalAlignment.Center;navigation.Children.Add(matches);SetColumn(matches,1);
  // The hidden selector retains category state; visible buttons use the same tab treatment as Championship.
  kind.IsVisible=false;Children.Add(kind);SetRow(kind,1);SetColumnSpan(kind,2);
  var sidebar=new Grid{Margin=new Thickness(0,0,18,0)};sidebar.RowDefinitions.Add(new());sidebar.RowDefinitions.Add(new(){Height=GridLength.Auto});SetRow(sidebar,2);Children.Add(sidebar);
  sidebar.Children.Add(new ScrollViewer{Name="art-options-scroll",Content=options,VerticalScrollBarVisibility=ScrollBarVisibility.Visible,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Margin=new Thickness(0,0,4,0)});sidebar.Children.Add(footer);SetRow(footer,1);
  void Label(string s)=>options.Children.Add(Ui.Text(s,true));
  void Action(string s,Action a)=>options.Children.Add(Ui.Button(s,a));
  Label("FORMATO DA IMAGEM");options.Children.Add(aspect);options.Children.Add(vetoOptions);
  options.Children.Add(mapTitle);options.Children.Add(maps);statsEdit=Ui.Button("Conferir estatísticas / colar print",EditStatistics);options.Children.Add(statsEdit);options.Children.Add(stats);
  reconcile=Ui.Button("Conciliar jogadores da série",()=>_=ReconcileSeries());options.Children.Add(reconcile);
  Label("CAMPEONATO (OPCIONAL)");options.Children.Add(eventName);
  options.Children.Add(channelTitle);channelHint.Margin=new Thickness(0,5,6,0);channelRow.Children.Add(channelHint);channelRow.Children.Add(channel);SetColumn(channel,1);options.Children.Add(channelRow);channel.Name="art-channel-nick";
  Label("LOGOS DAS EQUIPES");Action("Selecionar logo da equipe A",()=>Resource("logo-a"));Action("Selecionar logo da equipe B",()=>Resource("logo-b"));Action("Usar logos neutras",()=>{brand.TeamALogo=brand.TeamBLogo="";Changed();});
  Label("FUNDO DA ARTE");Action("Carregar fundo",()=>Resource("fundo"));Action("Restaurar fundo padrão",()=>{brand.Style(Kind).Background="";Changed();});options.Children.Add(background);
  Label("COR DE DESTAQUE");
  palette.Name="art-palette";palette.SelectedIndex=-1;options.Children.Add(palette);
  palette.SelectionChanged+=(_,_)=>{if(!loading&&palette.SelectedIndex>=0){brand.Style(Kind).Accent=new[]{"#FF3927","#4199BB","#FFFFFF"}[palette.SelectedIndex];Changed();}};
  options.Children.Add(accent);Action("Aplicar cor",()=>{if(!System.Text.RegularExpressions.Regex.IsMatch(accent.Text??"","^#[0-9A-Fa-f]{6}$")){status.Text=L.T("Use uma cor no formato #RRGGBB.");return;}brand.Style(Kind).Accent=accent.Text!;Changed();});
  options.Children.Add(mvpTitle);options.Children.Add(mvp);options.Children.Add(modeTitle);options.Children.Add(mode);
  generate=Ui.Button("Gerar arquivos",()=>_=(Kind==ArtKind.MapVeto&&brand.ArtVeto!=null?ApplyCorrections():Export()),true);generate.IsEnabled=false;footer.Children.Add(generate);footer.Children.Add(Ui.Button("Abrir pasta da partida",()=>{if(Selected!=null)Ui.OpenFolder(MatchFiles.Ensure(Selected));}));copy=Ui.Button("Copiar caminho para OBS",()=>{try{Ui.CopyText(MatchFiles.ObsVeto);status.Text=L.T("Caminho copiado. Configure uma fonte de imagem no OBS com este arquivo.");}catch(Exception ex){status.Text=ex.Message;}});footer.Children.Add(copy);footer.Children.Add(status);status.FontSize=12;status.MaxHeight=70;
  var frame=new Border{Background=new SolidColorBrush(Color.FromRgb(11,16,23)),Padding=new Thickness(14),Child=preview};SetRow(frame,2);SetColumn(frame,1);Children.Add(frame);
  matches.SelectionChanged+=(_,e)=>{if(e.Source!=matches)return;if(!loading)LoadSelection();};kind.SelectionChanged+=(_,e)=>{if(e.Source!=kind)return;if(!loading){Configure();_=Render();}};aspect.SelectionChanged+=(_,e)=>{if(e.Source!=aspect)return;if(!loading){formats[(int)Kind]=aspect.SelectedIndex;_=Render();}};maps.SelectionChanged+=(_,e)=>{if(e.Source!=maps)return;if(!loading){if(Selected!=null&&Map!=null)rememberedMaps[(Selected.Id,Kind)]=Map.MapId;Configure();_=Render();}};mode.SelectionChanged+=(_,_)=>ShowPreview();
  eventName.TextChanged+=(_,_)=>{if(!loading){brand.EventName=eventName.Text??"";Changed();}};mvp.SelectionChanged+=(_,_)=>{if(!loading){var value=(mvp.SelectedItem as Choice)?.Key??"";if(value==brand.Mvp)return;brand.Mvp=value;Changed();}};
  channel.LostFocus+=(_,_)=>{channel.Text=ChannelNick(channel.Text??"");Flush();};
  channel.TextChanged+=(_,_)=>{if(!loading){brand.BroadcastChannel=BroadcastChannel.Normalize(ChannelNick(channel.Text??""));preferences.ChannelNick=ChannelNick(channel.Text??"");channelPreferenceDirty=true;Changed();}};
  debounce.Tick+=(_,_)=>{debounce.Stop();if(Flush())_=Render();};SizeChanged+=(_,_)=>{matches.Width=Bounds.Width<1200?240:300;ColumnDefinitions[0].Width=new GridLength(Bounds.Width<1100?320:370);};Configure();
 }
 public void Refresh(string? preferred=null)
 {
  if(!Flush())return;
  string? id=Selected?.Id??preferred;loading=true;matches.ItemsSource=list();matches.SelectedItem=list().FirstOrDefault(m=>m.Id==id)??list().FirstOrDefault();loading=false;LoadSelection();
 }
 void LoadSelection()
 {
  if(!Flush()){loading=true;matches.SelectedItem=owner;loading=false;return;}
  loading=true;owner=Selected;if(owner!=null){if(!rememberedFormats.TryGetValue(owner.Id,out var stored))rememberedFormats[owner.Id]=stored=new[]{1,2,1,1,1};formats=stored;}brand=owner==null?new():MatchFiles.ReadBrand(owner);brandDirty=false;
  if(!string.IsNullOrEmpty(brand.Background)){brand.Style(ArtKind.SeriesStats).Background=brand.Background;brand.Background="";brandDirty=true;}
  eventName.Text=brand.EventName;channel.Text=string.IsNullOrWhiteSpace(brand.BroadcastChannel)?preferences.ChannelNick??"":ChannelNick(brand.BroadcastChannel);if(string.IsNullOrWhiteSpace(brand.BroadcastChannel)&&!string.IsNullOrWhiteSpace(channel.Text)){brand.BroadcastChannel=BroadcastChannel.Normalize(channel.Text);brandDirty=true;}if(string.IsNullOrWhiteSpace(preferences.ChannelNick)&&brand.BroadcastChannel.Contains("twitch.tv/",StringComparison.OrdinalIgnoreCase)){preferences.ChannelNick=ChannelNick(brand.BroadcastChannel);channelPreferenceDirty=true;}maps.ItemsSource=owner?.Maps.Select(m=>catalog.Maps.Single(c=>c.Id==m.MapId).DisplayName).ToList();maps.SelectedIndex=owner?.Maps.Count>0?0:-1;loading=false;BuildVeto();Configure();_=Render();
 }
 void Configure()
 {
  loading=true;
  try
  {
   if(Selected!=null){string? savedMap=rememberedMaps.GetValueOrDefault((Selected.Id,Kind));int index=Selected.Maps.FindIndex(m=>m.MapId==savedMap);maps.SelectedIndex=Selected.Maps.Count==0?-1:Math.Max(0,index);}bool veto=Kind==ArtKind.MapVeto,hasStats=Kind is ArtKind.MapStats or ArtKind.SeriesStats or ArtKind.Results;
   if(aspect.Items.Count!=(Kind==ArtKind.Announcement?2:3))aspect.ItemsSource=new[]{"Stories · 1080 × 1920","Feed · 1080 × 1350","Horizontal · 1920 × 1080"}.Take(Kind==ArtKind.Announcement?2:3).Select(L.T).ToArray();aspect.IsEnabled=!veto;aspect.SelectedIndex=veto?2:Math.Min(formats[(int)Kind],aspect.Items.Count-1);vetoOptions.IsVisible=veto;
   foreach(Control control in new Control[]{channelRow,channel,channelTitle,channelHint})control.IsVisible=Kind==ArtKind.Announcement;
   foreach(Control control in new Control[]{maps,mapTitle,statsEdit,stats})control.IsVisible=hasStats;
   foreach(Control control in new Control[]{mode,modeTitle,copy})control.IsVisible=veto;
   foreach(Control control in new Control[]{mvp,mvpTitle})control.IsVisible=Kind is ArtKind.MapStats or ArtKind.SeriesStats;
   reconcile.IsVisible=Kind==ArtKind.SeriesStats;reconcile.IsEnabled=Selected!=null;accent.Text=brand.Style(Kind).Accent;palette.SelectedIndex=Array.FindIndex(new[]{"#FF3927","#4199BB","#FFFFFF"},c=>string.Equals(c,accent.Text,StringComparison.OrdinalIgnoreCase));
   statsEdit.IsEnabled=Map!=null;stats.Text=Map?.Statistics?.ConfirmedAt==null?L.T("Estatísticas ainda não conferidas."):Map.Statistics.Status=="complete"?L.T("Estatísticas completas e conferidas."):L.T("Estatísticas parciais ou indisponíveis.");
   if(Selected!=null&&Kind==ArtKind.SeriesStats&&StatisticsData.SeriesIssue(Selected) is string issue&&issue!="")stats.Text=issue;
   var choices=new List<Choice>{new("",L.T("MVP automático")),new("none",L.T("Sem destaque MVP"))};
   if(Selected!=null){var summary=Kind==ArtKind.MapStats&&Map!=null?StatisticsData.ForMap(Map):StatisticsData.ForSeries(Selected);ToolTip.SetTip(stats,L.F($"Cobertura: {summary.MapsIncluded}/{summary.MapsPlayed} mapas conferidos.")+Environment.NewLine+string.Join(Environment.NewLine,summary.Players.Select(p=>$"{p.Name}: E {p.KillMaps}, M {p.DeathMaps}, A {p.AssistMaps}")));choices.AddRange(summary.Players.Select(p=>new Choice(p.Key,Selected.Team(p.Team)+" · "+p.Name)));}
   string suggested=Selected==null?"":StatisticsData.SuggestMvp(Selected,Kind==ArtKind.MapStats?Map:null)?.Key??"";
   mvp.ItemsSource=choices;mvp.SelectedItem=choices.FirstOrDefault(c=>c.Key==(string.IsNullOrEmpty(brand.Mvp)?suggested:brand.Mvp))??choices[0];background.Text=L.T(string.IsNullOrEmpty(brand.BackgroundFor(Kind))?"Fundo padrão":"Fundo personalizado. Ajustado ao formato; confira o recorte na prévia.");
  }
  finally{loading=false;}
 }
 async void Resource(string type)
 {
  if(Selected==null)return;
  var ownerMatch=Selected;var resourceKind=Kind;
  try{var path=await Ui.PickImage(this);if(path==null||Selected?.Id!=ownerMatch.Id)return;string resource=await Task.Run(()=>MatchFiles.Resource(ownerMatch,type=="fundo"?"fundo-"+resourceKind:type,path));if(Selected?.Id!=ownerMatch.Id)return;if(type=="fundo")brand.Style(resourceKind).Background=resource;else if(type=="logo-a")brand.TeamALogo=resource;else brand.TeamBLogo=resource;Changed();}catch(Exception ex){status.Text=ex.Message;}
 }
 void BuildVeto()
 {
  vetoOptions.Children.Clear();if(Selected==null)return;
  vetoOptions.Children.Add(Ui.Text("CORREÇÕES DO VETO",true));vetoOptions.Children.Add(Ui.Text("As escolhas abaixo alteram a arte. Ao aplicar, você pode atualizar também a partida."));
  var source=brand.ArtVeto??Selected.Veto;
  if(source.Length!=8||source.Any(string.IsNullOrEmpty)){vetoOptions.Children.Add(Ui.Text("Conclua o veto da partida antes de corrigir a arte."));return;}
  for(int i=0;i<8;i++)
  {
   int slot=i;vetoOptions.Children.Add(Ui.Text($"{i+1:00} · {Selected.Action(i)} · {Selected.Team(Selected.Actor(i))}"));
   var choice=Ui.Combo();choice.ItemsSource=catalog.Maps;choice.ItemTemplate=new global::Avalonia.Controls.Templates.FuncDataTemplate<SiegeMap>((m,_)=>Ui.Text(m?.DisplayName??""));choice.SelectedItem=catalog.Maps.FirstOrDefault(m=>m.Id==source[i]);vetoOptions.Children.Add(choice);
   choice.SelectionChanged+=(_,_)=>{if(choice.SelectedItem is not SiegeMap picked||Selected==null)return;var edited=(brand.ArtVeto??Selected.Veto).ToArray();int other=Array.IndexOf(edited,picked.Id);if(other>=0)edited[other]=edited[slot];edited[slot]=picked.Id;brand.ArtVeto=edited;brand.ArtFormat=Selected.Format;Changed();BuildVeto();};
  }
  try
  {
   var artwork=MatchFiles.ArtworkMatch(Selected,catalog,brand);
   foreach(var map in artwork.Maps)
   {
    vetoOptions.Children.Add(Ui.Text(catalog.Maps.Single(m=>m.Id==map.MapId).DisplayName+" · "+L.T("Ataque inicial")));
    var side=Ui.RawCombo(L.T("Selecionar…"),Selected.TeamA,Selected.TeamB);side.SelectedIndex=map.InitialAttack+1;vetoOptions.Children.Add(side);
    side.SelectionChanged+=(_,_)=>{brand.ArtVeto??=Selected.Veto.ToArray();brand.ArtFormat=Selected.Format;brand.ArtSides[map.MapId]=side.SelectedIndex-1;Changed();};
   }
  }
  catch(Exception ex){vetoOptions.Children.Add(Ui.Text(ex.Message));}
  vetoOptions.Children.Add(Ui.Button("Aplicar correções e gerar",()=>_=ApplyCorrections(),true));
  vetoOptions.Children.Add(Ui.Button("Usar veto da partida",()=>{brand.ArtVeto=null;brand.ArtSides.Clear();brand.ArtFormat="";Changed();BuildVeto();}));
 }
 async Task ApplyCorrections()
 {
  if(Selected==null||exporting||applying)return;
  applying=true;kind.IsEnabled=options.IsEnabled=matches.IsEnabled=generate.IsEnabled=false;
  try
  {
   MatchFiles.ArtworkMatch(Selected,catalog,brand);
   if(brand.ArtVeto!=null)
   {
    var answer=await Ui.Ask(this,"Atualizar também o veto da partida selecionada?\n\nSim: salvar na partida e na arte.\nNão: corrigir somente a arte.\nCancelar: continuar editando.");
    if(answer<0)return;
    if(answer==1){if(applyVeto==null||!applyVeto(Selected.Id,brand))return;brand.ArtVeto=null;brand.ArtSides.Clear();brand.ArtFormat="";brandDirty=true;BuildVeto();}
   }
   if(!Flush())return;await Render();await Export();
  }
  catch(Exception ex){status.Text=ex.Message;}
  finally{applying=false;kind.IsEnabled=options.IsEnabled=matches.IsEnabled=true;generate.IsEnabled=rendered.Count>0;}
 }
 void Changed(){brandDirty=true;Clear();Configure();debounce.Stop();debounce.Start();}
 public bool Flush()
 {debounce.Stop();if(owner==null||!brandDirty&&!channelPreferenceDirty)return true;try{if(brandDirty)MatchFiles.SaveBrand(owner,brand);if(channelPreferenceDirty)MatchFiles.AtomicText(AppPaths.ConfigFile("image-preferences.json"),JsonSerializer.Serialize(preferences));brandDirty=channelPreferenceDirty=false;return true;}catch(Exception ex){status.Text=ex.Message;return false;}}
 void Clear(){generation++;renderedSnapshot="";preview.Source=null;foreach(var bitmap in previewPages.Values)bitmap.Dispose();previewPages.Clear();foreach(var p in rendered)p.Dispose();rendered.Clear();generate.IsEnabled=false;}
 static string SnapshotKey(SiegeMatch match,ImageBrand brand,ArtKind kind,ExportAspect aspect,MapRecord? map)=>JsonSerializer.Serialize(new{match,brand,kind,aspect,mapId=map?.MapId});
 Task? currentRender;
 public Task Render()=>currentRender=RenderCore();
 internal async Task SettlePreview()
 {Task? previous;do{previous=currentRender;if(previous!=null)await previous;await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);if(debounce.IsEnabled){if(!Flush())throw new IOException(CheckStatus);await Render();}}while(previous!=currentRender);}
 async Task RenderCore()
 {
  Clear();if(Selected==null||closed)return;int request=generation;status.Text=L.T("Preparando prévia… Você pode continuar navegando.");
  var match=JsonSerializer.Deserialize<SiegeMatch>(JsonSerializer.Serialize(Selected))!;var b=JsonSerializer.Deserialize<ImageBrand>(JsonSerializer.Serialize(brand))!;var k=Kind;var a=Aspect;var map=match.Maps.ElementAtOrDefault(maps.SelectedIndex);
  string? requirement=k switch
  {
   ArtKind.MapVeto when match.Veto.Any(string.IsNullOrWhiteSpace)||match.Maps.Any(m=>m.InitialAttack<0)=>"Conclua o veto e defina os lados dos mapas antes de gerar.",
   ArtKind.MapStats when map?.Statistics?.ConfirmedAt==null||map.Statistics.Players.Count==0=>"Confira as estatísticas deste mapa antes de gerar.",
   ArtKind.SeriesStats when StatisticsData.SeriesIssue(match)!=""=>StatisticsData.SeriesIssue(match),
   ArtKind.SeriesStats when !match.Maps.Any(m=>m.Statistics?.ConfirmedAt!=null&&m.Statistics.Players.Count>0)=>"Confira as estatísticas de pelo menos um mapa antes de gerar.",
   _ when k!=ArtKind.Announcement&&k!=ArtKind.MapVeto&&!match.Maps.Any(m=>m.Rounds.Count>0)=>"Registre pelo menos um round antes de gerar a imagem.",
   _=>null
  };
  if(requirement!=null){status.Text=L.T(requirement);return;}
  try{await renderGate.WaitAsync();List<ResultPage> pages;try{if(closed||request!=generation)return;pages=await Task.Run(()=>StudioRenderer.Render(k==ArtKind.MapVeto?MatchFiles.ArtworkMatch(match,catalog,b):match,catalog,b,k,a,k==ArtKind.MapStats?map:null));}finally{renderGate.Release();}if(closed||request!=generation){foreach(var p in pages)p.Dispose();return;}Dictionary<string,global::Avalonia.Media.Imaging.Bitmap> thumbnails;try{thumbnails=await Task.Run(()=>pages.ToDictionary(p=>p.Name,p=>Ui.Bitmap(p.Image)));}catch{foreach(var p in pages)p.Dispose();throw;}if(closed||request!=generation){foreach(var p in pages)p.Dispose();foreach(var bitmap in thumbnails.Values)bitmap.Dispose();return;}rendered=pages;renderedSnapshot=SnapshotKey(match,b,k,a,k==ArtKind.MapStats?map:null);previewPages=thumbnails;ShowPreview();generate.IsEnabled=!exporting;status.Text=L.T(k==ArtKind.MapVeto?"Ao gerar: arquivo transparente fixo no OBS + duas cópias na pasta da partida.":"Prévia pronta. Os arquivos serão salvos na pasta desta partida.");}
  catch(Exception ex){if(!closed&&request==generation)status.Text=ex.Message;}
 }
 void ShowPreview(){var page=rendered.FirstOrDefault(p=>Kind!=ArtKind.MapVeto||p.Name==(mode.SelectedIndex==1?"map-veto-obs":"map-veto"));preview.Source=page==null?null:previewPages.GetValueOrDefault(page.Name);}
 async Task Export()
 {
  if(Selected==null||rendered.Count==0||exporting||!Flush())return;
  exporting=true;kind.IsEnabled=options.IsEnabled=matches.IsEnabled=generate.IsEnabled=false;
  try{
   if(renderedSnapshot!=SnapshotKey(Selected,brand,Kind,Aspect,Kind==ArtKind.MapStats?Map:null)){await Render();if(Selected==null||rendered.Count==0)return;}
   var match=JsonSerializer.Deserialize<SiegeMatch>(JsonSerializer.Serialize(Selected))!;var b=JsonSerializer.Deserialize<ImageBrand>(JsonSerializer.Serialize(brand))!;var k=Kind;
   var pages=rendered.ToArray();status.Text=L.T("Salvando arquivos…");
   await Task.Run(()=>MatchFiles.ExportRendered(match,b,k,pages));status.Text=L.T("Arquivos salvos. Use Abrir pasta da partida para acessar.");
  }
  catch(Exception ex){status.Text=ex.Message;}finally{exporting=false;kind.IsEnabled=options.IsEnabled=matches.IsEnabled=true;generate.IsEnabled=rendered.Count>0;}
 }
 async Task ReconcileSeries()
 {
  if(Selected==null||!Flush())return;
  if(await new SeriesIdentityWindow(Selected,catalog).ShowDialog<bool>(Ui.Owner(this))){Configure();await Render();}
 }
 async void EditStatistics(){if(Selected==null||Map==null)return;try{await new StatsWindow(Selected,Map).ShowDialog<bool>(Ui.Owner(this));Configure();_=Render();}catch(Exception ex){status.Text=ex.Message;}}
 public bool Exporting=>exporting||applying;
 public void Stop(){closed=true;debounce.Stop();Clear();}
 internal bool GenerateVisible=>generate.IsVisible;
 internal async Task CheckCategory(int index){kind.SelectedIndex=index;await Render();await SettlePreview();}
 internal int FormatCount=>aspect.Items.Count;
 internal string CheckStatus=>status.Text??"";
 internal bool PreviewReady=>preview.Source!=null&&rendered.Count>0;
 internal Rect GenerateBounds=>new(generate.TranslatePoint(default,this)??default,generate.Bounds.Size);
 internal static string ChannelNick(string value) {
  value=value.Trim();foreach(var prefix in new[]{"https://","http://"})if(value.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))value=value[prefix.Length..];
  if(value.StartsWith("www.",StringComparison.OrdinalIgnoreCase))value=value[4..];
  if(value.StartsWith("twitch.tv/",StringComparison.OrdinalIgnoreCase))value=value[10..];
  return value.Trim().TrimStart('@').Trim('/');
 }
 public sealed class ImagePreferences { public string ChannelNick{get;set;}=""; }
 sealed record Choice(string Key,string Name){public override string ToString()=>Name;}
}

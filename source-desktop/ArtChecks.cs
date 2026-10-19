using System.Text.Json;
using Avalonia.VisualTree;
using Avalonia.Interactivity;
namespace CasterDesk.Desktop;
internal static class ArtChecks
{
 internal static void Run()
 {
  int count=0;void Need(bool ok,string why){count++;if(!ok)throw new Exception("Art: "+why);}
  var catalog=SiegeCatalog.Load();var match=new SiegeMatch{Format="MD3",DirectMaps=true};
  for(int i=0;i<2;i++) {
   var map=new MapRecord{MapId=catalog.Maps[i].Id,Picker=0,InitialAttack=0,OtAttack=0,ConfirmedAt=DateTimeOffset.Now};
   map.Rounds=Enumerable.Range(0,7).Select(_=>new RoundRecord{Winner=0,Ending="elimination",Site=catalog.Maps[i].Sites[0]}).ToList();
   map.Statistics=new(){ConfirmedAt=DateTimeOffset.Now,Players=Enumerable.Range(0,10).Select(n=>new PlayerStatistic{PlayerId="id"+n,Team=n/5,Name="Player"+n,Kills=n+1,Deaths=2,Assists=3}).ToList()};match.Maps.Add(map);
  }
  match.Maps.Add(new MapRecord{MapId=catalog.Maps[2].Id,Picker=0,InitialAttack=0});
  Need(StatisticsData.SeriesIssue(match)=="","same roster accepted");
  match.Maps[1].Statistics!.Players[0].PlayerId="variant";match.Maps[1].Statistics!.Players[0].Name="P1ayer0";
  Need(StatisticsData.ForSeries(match).Players.Count==11&&StatisticsData.SeriesIssue(match)!="","OCR variation detected without auto merge");
  bool blocked=false;try{var pages=StudioRenderer.Render(match,catalog,new(),ArtKind.SeriesStats,ExportAspect.Feed);foreach(var page in pages)page.Dispose();}catch(InvalidOperationException){blocked=true;}
  Need(blocked,"direct rendering cannot publish unresolved names");
  MatchStore.Save(match);
  var owner=new Window();owner.Show();var window=new SeriesIdentityWindow(match,catalog);window.Show(owner);Dispatcher.UIThread.RunJobs();
  foreach(var box in window.GetVisualDescendants().OfType<ComboBox>())if(box.SelectedItem?.ToString()=="P1ayer0")box.SelectedItem=box.Items.Cast<object>().First(i=>i.ToString()=="Player0");
  var save=window.GetVisualDescendants().OfType<Button>().Single(b=>b.Name=="series-reconcile-save");save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs();
  Need(StatisticsData.SeriesIssue(match)==""&&StatisticsData.ForSeries(match).Players.Count==10,"explicit reconciliation fixes roster");
  Need(StatisticsData.ForSeries(match).Players.Single(p=>p.Name=="Player0").Kills==2,"maps summed once after reconciliation");
  Need(Directory.GetFiles(Path.Combine(MatchFiles.DirectoryFor(match),"Estatisticas"),"antes-conciliacao-*.json").Length==1,"pre-reconciliation snapshot preserved");
  var restored=MatchStore.Read(Path.Combine(MatchFiles.DirectoryFor(match),"partida.json"),catalog);Need(StatisticsData.SeriesIssue(restored)=="","correction survives save/reload");
  owner.Close();
  var brand=new ImageBrand();brand.Style(ArtKind.SeriesStats).Accent="#4199BB";brand.Style(ArtKind.Announcement).Accent="#FFFFFF";brand.Style(ArtKind.MapStats).Background="separate.png";
  var roundtrip=JsonSerializer.Deserialize<ImageBrand>(JsonSerializer.Serialize(brand))!;
  Need(roundtrip.AccentFor(ArtKind.SeriesStats).ToArgb()==System.Drawing.Color.FromArgb(0x41,0x99,0xBB).ToArgb(),"series accent persists");
  Need(roundtrip.AccentFor(ArtKind.Announcement).ToArgb()==System.Drawing.Color.White.ToArgb(),"announcement accent independent");
  Need(roundtrip.BackgroundFor(ArtKind.SeriesStats)==""&&roundtrip.BackgroundFor(ArtKind.MapStats)=="separate.png","background scoped per category");
  brand.Style(ArtKind.SeriesStats).Accent="invalid";Need(brand.AccentFor(ArtKind.SeriesStats).R==255,"invalid accent safely falls back");
  // Same glyphs, very different nickname lengths: fixed font must keep the same ink height.
  using(var image=new System.Drawing.Bitmap(300,120)) {
   using(var g=System.Drawing.Graphics.FromImage(image)){g.Clear(System.Drawing.Color.Black);StudioRenderer.PlayerText(g,"AAAA",new(0,0,180,50),25);StudioRenderer.PlayerText(g,new string('A',70),new(0,60,180,50),25);}
   int Height(int top){var ys=Enumerable.Range(top,50).Where(y=>Enumerable.Range(0,180).Any(x=>image.GetPixel(x,y).R>50)).ToArray();return ys.Length==0?0:ys.Last()-ys.First()+1;}
   Need(Height(0)>0&&Height(0)==Height(60),"long nickname trims instead of shrinking");
  }
  foreach(var aspect in Enum.GetValues<ExportAspect>()) {
   brand.Style(ArtKind.SeriesStats).Accent="#4199BB";
   var pages=StudioRenderer.Render(match,catalog,brand,ArtKind.SeriesStats,aspect);
   try{pages[0].Image.Save(Path.Combine(AppPaths.DataDirectory,"series-fixed-"+aspect+".png"));Need(pages.Count==1,"one combined series art "+aspect);}finally{foreach(var p in pages)p.Dispose();}
  }
  string? repro=Environment.GetEnvironmentVariable("CASTERDESK_ART_REPRO");
  if(!string.IsNullOrEmpty(repro)) {
   var real=MatchStore.Read(repro,catalog);
   Need(StatisticsData.ForSeries(real).Players.Count==12&&StatisticsData.SeriesIssue(real)!="","NA ORG repro detects twelve identities without mutating input");
   foreach(var p in real.Maps.SelectMany(m=>m.Statistics?.Players??[])) {
    string? canonical=p.Name=="Lendal."?"Lenda1.":p.Name=="lt kds.FaZe"?"kds.FaZe":null;
    if(canonical!=null){var target=real.Maps.SelectMany(m=>m.Statistics?.Players??[]).First(k=>k.Team==p.Team&&k.Name==canonical);p.Name=target.Name;p.PlayerId=target.PlayerId;}
   }
   Need(StatisticsData.ForSeries(real).Players.Count==10&&StatisticsData.SeriesIssue(real)=="","diagnostic copy reconciles the two reported variants");
   Need(StatisticsData.ForSeries(real).Players.Single(p=>p.Name=="kds.FaZe").Kills==19,"NA ORG repro sums KDS 9 + 10 once");
   Need(StatisticsData.ForSeries(real).Players.Single(p=>p.Name=="Lenda1.").Kills==9,"NA ORG repro sums Lenda 3 + 6 once");
   string resource=Path.Combine(Path.GetDirectoryName(repro)!,"Recursos");
   Directory.CreateDirectory(Path.Combine(MatchFiles.Ensure(real),"Recursos"));
   foreach(var file in Directory.GetFiles(resource))File.Copy(file,Path.Combine(MatchFiles.DirectoryFor(real),"Recursos",Path.GetFileName(file)),true);
   var realBrand=JsonSerializer.Deserialize<ImageBrand>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(repro)!,"arte.json")))!;
   realBrand.Style(ArtKind.SeriesStats).Background=realBrand.Background;realBrand.Background="";realBrand.Style(ArtKind.SeriesStats).Accent="#4199BB";
   foreach(var aspect in Enum.GetValues<ExportAspect>()) {
    var pages=StudioRenderer.Render(real,catalog,realBrand,ArtKind.SeriesStats,aspect);
    try{pages[0].Image.Save(Path.Combine(AppPaths.DataDirectory,"simulacao-naorg-"+aspect+".png"));}finally{foreach(var page in pages)page.Dispose();}
   }
  }
  var studio=new StudioView(()=>new[]{match},catalog);var studioOwner=new Window{Content=studio};studioOwner.Show();studio.Refresh(match.Id);
  void Wait(Task task){while(!task.IsCompleted){Dispatcher.UIThread.RunJobs();Thread.Sleep(5);}task.GetAwaiter().GetResult();}
  Wait(studio.CheckCategory(3));
  ComboBox Palette()=>studio.GetVisualDescendants().OfType<ComboBox>().Single(c=>c.Name=="art-palette");
  Palette().SelectedIndex=1;Need(studio.Flush(),"series color saved");Wait(studio.SettlePreview());
  Wait(studio.CheckCategory(0));Need(Palette().SelectedIndex==0,"switching category restores its independent palette");
  Palette().SelectedIndex=1;Need(studio.Flush(),"same preset applies in another category");Wait(studio.SettlePreview());
  var prefs=MatchFiles.ReadBrand(match);Need(prefs.AccentFor(ArtKind.SeriesStats)==prefs.AccentFor(ArtKind.Announcement),"preset selection not stuck from previous tab");
  Wait(studio.Render());Need(studio.PreviewReady,"export test has a full-resolution preview: "+studio.CheckStatus);
  var renderedField=typeof(StudioView).GetField("rendered",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
  var exportMethod=typeof(StudioView).GetMethod("Export",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
  ResultPage CurrentPage()=>((List<ResultPage>)renderedField.GetValue(studio)!)[0];
  var previewPage=CurrentPage();Wait((Task)exportMethod.Invoke(studio,null)!);
  Need(ReferenceEquals(previewPage,CurrentPage())&&File.Exists(Path.Combine(MatchFiles.DirectoryFor(match),"Imagens",previewPage.Name+".png")),"unchanged preview exports without rendering again");
  match.TeamA="Updated export team";Wait((Task)exportMethod.Invoke(studio,null)!);
  Need(!ReferenceEquals(previewPage,CurrentPage())&&MatchStore.Read(Path.Combine(MatchFiles.DirectoryFor(match),"partida.json"),catalog).TeamA==match.TeamA,"changed match invalidates preview before export");
  studio.Stop();studioOwner.Close();
  File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"casterdesk-art-check.txt"),$"PASS: {count} art checks: identity conflict guard, explicit UI reconciliation, backups, persistence, KDA sums, per-category backgrounds/colors, fixed nickname font and three export formats. Headless only.");
 }
}

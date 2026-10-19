using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CasterDesk.Core;

namespace CasterDesk.Wpf;
internal static class Checks
{
 public static void Run()
 {
  var diagnosis=StatisticsData.Diagnose(System.Threading.CancellationToken.None);try{Wait(diagnosis);File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"ocr-diagnostic.txt"),diagnosis.Result);}catch(Exception ex){File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"ocr-diagnostic.txt"),"Manual entry remains available. "+ex.Message);}MatchInterchangeTests.Run();
  StabilityChecks.Run();
  BackupTests.Run();
  var match=new SiegeMatch{TeamA="BLACK DRAGON",TeamB="TEAM LIQUID",CoinWinner=0,FirstBan=0};var catalog=SiegeCatalog.Load();
  match.Veto=catalog.Maps.Take(8).Select(m=>m.Id).ToArray();match.SyncMaps(catalog);var map=match.Maps[0];map.InitialAttack=0;map.OtAttack=1;
  for(int n=0;n<7;n++)map.Rounds.Add(new(){Winner=1,Site=catalog.Maps.Single(m=>m.Id==map.MapId).Sites[n%4],Ending="elimination"});map.ConfirmedAt=DateTimeOffset.Now;
  var players=new List<PlayerStatistic>();for(int team=0;team<2;team++)for(int n=0;n<5;n++)players.Add(new(){Team=team,Name=$"Player{team}{n}",Kills=5+n,Deaths=7-n,Assists=n});map.Statistics=new(){Status="complete",Players=players,ConfirmedAt=DateTimeOffset.Now};
  MatchStore.Validate(match,catalog);MatchStore.Save(match);var saved=MatchStore.Read(Path.Combine(MatchStore.Folder,match.Id+".json"),catalog);if(saved.Maps[0].Statistics?.Players.Count!=10)throw new Exception("Statistics round trip");
  var promoBrand=new ImageBrand{EventName="CHALLENGER SERIES",BroadcastChannel="twitch.tv/casterdesk_demo"};MatchFiles.SaveBrand(match,promoBrand);if(MatchFiles.ReadBrand(match).BroadcastChannel!=promoBrand.BroadcastChannel)throw new Exception("Broadcast channel not persisted");
  var studio=new StudioView(()=>new[]{match},catalog);
  studio.Refresh(match.Id);Wait(studio.Render());if(!studio.PreviewReady)throw new Exception("Native preview failed");
  if(studio.FormatCount!=2)throw new Exception("Announcement must offer only feed and story");
  // No Window.Show, handles, installed app or real data are used by this check.
  foreach(var size in new[]{new Size(1050,680),new Size(1280,800),new Size(1920,1080)}){Render(studio,size,$"wpf-studio-{size.Width}.png");if(studio.GenerateBounds.Bottom>size.Height||studio.GenerateBounds.Top<0)throw new Exception("Generate action clipped");}
  for(int category=1;category<5;category++){Wait(studio.CheckCategory(category));if(!studio.PreviewReady)throw new Exception($"Category {category} preview failed");Render(studio,new Size(1280,800),$"wpf-category-{category}.png");}studio.Stop();
  string originalMatch=System.Text.Json.JsonSerializer.Serialize(match);var edited=new ImageBrand{ArtVeto=match.Veto.Reverse().ToArray(),ArtFormat=match.Format};var artwork=MatchFiles.ArtworkMatch(match,catalog,edited);foreach(var item in artwork.Maps)edited.ArtSides[item.MapId]=1;
  artwork=MatchFiles.ArtworkMatch(match,catalog,edited);if(artwork.Maps.Any(m=>m.InitialAttack!=1)||System.Text.Json.JsonSerializer.Serialize(match)!=originalMatch)throw new Exception("Artwork correction mutated the match");
  MatchFiles.SaveBrand(match,edited);if(MatchFiles.ReadBrand(match).ArtVeto?.SequenceEqual(edited.ArtVeto)!=true)throw new Exception("Artwork correction not persisted");
  MatchFiles.Export(match,catalog,edited,ArtKind.MapVeto,ExportAspect.Horizontal,null);if(System.Text.Json.JsonSerializer.Serialize(match)!=originalMatch)throw new Exception("Veto export mutated the match");
  edited.ArtVeto[1]=edited.ArtVeto[0];bool duplicateRejected=false;try{MatchFiles.ArtworkMatch(match,catalog,edited);}catch(InvalidDataException){duplicateRejected=true;}if(!duplicateRejected)throw new Exception("Duplicate artwork map accepted");MatchFiles.SaveBrand(match,new());
  var clean=new SiegeMatch{CoinWinner=0,FirstBan=0,Veto=catalog.Maps.Take(8).Select(m=>m.Id).ToArray()};clean.SyncMaps(catalog);MatchStore.Save(clean);
  var championship=new MatchSession();
  
  {
   var correction=new ImageBrand{ArtFormat=clean.Format,ArtVeto=clean.Veto.Reverse().ToArray()};foreach(var item in MatchFiles.ArtworkMatch(clean,catalog,correction).Maps)correction.ArtSides[item.MapId]=0;
   championship.ApplyArtworkVeto(clean.Id,correction);var stored=MatchStore.Read(Path.Combine(MatchStore.Folder,clean.Id+".json"),catalog);if(!stored.Veto.SequenceEqual(correction.ArtVeto))throw new Exception("Explicit match correction not saved");
   var target=championship.Matches.Single(m=>m.Id==clean.Id);string before=System.Text.Json.JsonSerializer.Serialize(target);correction.ArtVeto=clean.Veto.ToArray();bool failed=false;
   using(var file=new FileStream(Path.Combine(MatchStore.Folder,clean.Id+".json"),FileMode.Open,FileAccess.Read,FileShare.None)){try{championship.ApplyArtworkVeto(clean.Id,correction);}catch(IOException){failed=true;}}
   if(!failed||System.Text.Json.JsonSerializer.Serialize(target)!=before)throw new Exception("Failed match correction did not roll back");
   bool protectedResult=false;try{championship.ApplyArtworkVeto(match.Id,new ImageBrand{ArtFormat=match.Format,ArtVeto=match.Veto.Reverse().ToArray()});}catch(InvalidOperationException){protectedResult=true;}if(!protectedResult)throw new Exception("Recorded match allowed destructive veto correction");
  }
  var nativeChampionship=new ChampionshipView();nativeChampionship.Session.Select(match.Id);foreach(var size in new[]{new Size(1050,580),new Size(1280,700),new Size(1920,980)}){nativeChampionship.CheckLayout(size);Render(nativeChampionship,size,$"wpf-championship-{size.Width}.png");}nativeChampionship.Stop();
  var activeChampionship=new ChampionshipView();activeChampionship.Session.New();activeChampionship.Session.Setup("MD1",0,0,false);for(int n=0;n<8;n++)activeChampionship.Session.Veto(n,catalog.Maps[n].Id);activeChampionship.Session.Side(activeChampionship.Session.Map!.MapId,false,0);foreach(var size in new[]{new Size(1050,580),new Size(1280,700),new Size(1920,980)}){activeChampionship.CheckLayout(size);Render(activeChampionship,size,$"wpf-active-championship-{size.Width}.png");var bounds=activeChampionship.RoundConfirmBounds;if(bounds==null||bounds.Value.Bottom>size.Height||bounds.Value.Top<0)throw new Exception("Confirm round action clipped");}activeChampionship.Stop();
  var stats=new StatsWindow(match,map);Render((FrameworkElement)stats.Content,new Size(1240,720),"wpf-statistics.png");stats.Close();
  var texts=new TextsView(new Settings());Render(texts,new Size(1280,800),"wpf-texts.png");texts.CheckDuration("00:03:00");texts.Start();if(texts.Remaining<179||texts.Remaining>180)throw new Exception("Edited timer duration ignored");texts.CheckDuration("invalid");bool invalidStart=false;try{texts.Start();}catch(InvalidDataException){invalidStart=true;}if(!invalidStart)throw new Exception("Invalid timer starts");texts.Stop();
  if(TextsView.DurationText(86400)!="24:00:00")throw new Exception("24 hour timer wrapped");
  var malformed=new CastingSettings{People=null!};Configuration.Normalize(malformed);if(malformed.People.Length!=4||malformed.People.Any(p=>p==null))throw new Exception("Casting config normalization");var timerConfig=new Settings{Presets=null!,Output=null!};Configuration.Normalize(timerConfig);if(timerConfig.Presets.Length!=3||string.IsNullOrEmpty(timerConfig.Output))throw new Exception("Timer config normalization");
  string badConfig=AppPaths.ConfigFile("check-invalid.json");File.WriteAllText(badConfig,"{bad");Configuration.Read<Settings>("check-invalid.json");if(!Directory.GetFiles(Path.GetDirectoryName(badConfig)!,"check-invalid.json.invalid-*").Any())throw new Exception("Invalid configuration was not preserved");
  var shell=new Shell();shell.CheckPage(2,true);Wait(shell.CheckPreview());Render((FrameworkElement)shell.Content,new Size(1440,900),"wpf-shell-menu.png");shell.Close();
  if(TextsView.Parse("00:10:00")!=600)throw new Exception("Timer duration");
  bool rejected=false;try{TextsView.Parse("00:60:00");}catch(InvalidDataException){rejected=true;}if(!rejected)throw new Exception("Invalid duration accepted");
  using var original=StudioRenderer.Render(match,catalog,new(),ArtKind.Announcement,ExportAspect.Feed)[0];if(original.Image.Width!=1080||original.Image.Height!=1350)throw new Exception("Announcement aspect changed");
  MatchFiles.AtomicImage(Path.Combine(AppPaths.DataDirectory,"announcement-without-channel.png"),original.Image);
  using var channel=StudioRenderer.Render(match,catalog,new(){BroadcastChannel="twitch.tv/casterdesk_demo"},ArtKind.Announcement,ExportAspect.Feed)[0];MatchFiles.AtomicImage(Path.Combine(AppPaths.DataDirectory,"announcement-channel.png"),channel.Image);
  string archive=MatchFiles.Export(match,catalog,new(),ArtKind.MapVeto,ExportAspect.Horizontal,null);if(!File.Exists(MatchFiles.ObsVeto)||!File.Exists(Path.Combine(archive,"Imagens","map-veto.png")))throw new Exception("Veto archive missing");
  File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"wpf-check.txt"),"PASS: five artwork tabs; native layouts 1050/1280/1920; announcement feed/story and channel persistence; artwork veto isolation, persistence, duplicate rejection and export; explicit match correction, recorded-match protection and rollback after locked-file failure; statistics; edited and invalid timer duration; 24 hours; configuration normalization and recovery; native announcement geometry; veto archive and OBS fixed path. No window shown.");
 }
 static void Wait(System.Threading.Tasks.Task task)
 {var frame=new DispatcherFrame();bool timeout=false;var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(30)};timer.Tick+=(_,_)=>{timeout=true;frame.Continue=false;};timer.Start();task.ContinueWith(_=>Application.Current.Dispatcher.BeginInvoke(()=>frame.Continue=false));Dispatcher.PushFrame(frame);timer.Stop();if(timeout)throw new Exception("Native preview timeout");task.GetAwaiter().GetResult();}
 static void Render(FrameworkElement element,Size size,string name)
 {element.Measure(size);element.Arrange(new Rect(size));element.UpdateLayout();var image=new RenderTargetBitmap((int)size.Width,(int)size.Height,96,96,PixelFormats.Pbgra32);var background=new DrawingVisual();using(var context=background.RenderOpen())context.DrawRectangle((Brush)Application.Current.FindResource("CanvasBrush"),null,new Rect(size));image.Render(background);image.Render(element);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using var output=File.Create(Path.Combine(AppPaths.DataDirectory,name));encoder.Save(output);}
}

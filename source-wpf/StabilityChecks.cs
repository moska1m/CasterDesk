using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using CasterDesk.Core;

namespace CasterDesk.Wpf;
internal static class StabilityChecks
{
 public static void Run()
 {
  int checks=0;
  void Need(bool value,string message){checks++;if(!value)throw new Exception(message);}
  string root=Path.Combine(AppPaths.DataDirectory,"transactions-fixture");Directory.CreateDirectory(root);
  string a=Path.Combine(root,"a.txt"),b=Path.Combine(root,"b.txt");File.WriteAllText(a,"previous-a");File.WriteAllText(b,"previous-b");
  using(var batch=new FileBatch()){batch.Text(a,"new-a");batch.Text(b,"new-b");batch.Commit();}
  Need(File.ReadAllText(a)=="new-a"&&File.ReadAllText(b)=="new-b","Batch commit");
  using(var locked=new FileStream(b,FileMode.Open,FileAccess.Read,FileShare.Read))
  using(var batch=new FileBatch())
  {
   batch.Text(a,"failed-a");batch.Text(b,"failed-b");bool rejected=false;try{batch.Commit();}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){rejected=true;}
   Need(rejected&&File.ReadAllText(a)=="new-a","Failed batch restores earlier writes");
  }
  Need(File.ReadAllText(b)=="new-b","Locked target preserved");
  string folder=Path.Combine(AppPaths.BackupDirectory,"transactions",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
  string old=Path.Combine(folder,"0.old"),prepared=Path.Combine(folder,"0.new");File.WriteAllText(old,"recover-a");File.WriteAllText(a,"interrupted-a");
  File.WriteAllText(Path.Combine(folder,"journal.json"),JsonSerializer.Serialize(new[]{new FileBatch.Entry(a,prepared,old)}));FileBatch.Recover();
  Need(File.ReadAllText(a)=="recover-a"&&!Directory.Exists(folder),"Startup recovery");
  string unrelated=Path.Combine(AppPaths.BackupDirectory,"transactions","keep-me");Directory.CreateDirectory(unrelated);FileBatch.Recover();Need(Directory.Exists(unrelated),"Recovery preserves unrelated folders");
  var session=new MatchSession();var catalog=session.Catalog;
  foreach(string format in new[]{"MD1","MD3","MD5"})
  {
   session.New();session.Setup(format,0,0,false);
   for(int n=0;n<8;n++)session.Veto(n,catalog.Maps[n].Id);
   Need(session.Current.Maps.Count==(format=="MD1"?1:format=="MD3"?3:5),"Series map count");
   var map=session.Current.Maps[0];session.SelectMap(map.MapId);session.Side(map.MapId,false,0);session.Side(map.MapId,true,1);
   var pending=session.PendingBans();Need(pending.Select(i=>map.BanRole(i)).SequenceEqual(new[]{"DEF","ATQ","DEF","ATQ"}),"Alternating opening bans");
   foreach(int slot in pending)session.Ban(slot,catalog.Operators.First(o=>o.Role==map.BanRole(slot)&&!map.Bans.Contains(o.Id)).Id);
   for(int n=0;n<7;n++)session.Round(catalog.Maps.Single(m=>m.Id==map.MapId).Sites[n%4],1,"elimination");
   session.Confirm();Need(session.Map!.Confirmed,"Confirmation");bool blocked=false;try{session.Round("",0,"");}catch(InvalidOperationException){blocked=true;}Need(blocked,"Confirmed map protected");
   session.Review("headless verification");Need(!session.Map.Confirmed&&Directory.GetFiles(Path.Combine(MatchFiles.DirectoryFor(session.Current),"Revisoes")).Length==1,"Audited review");
   string before=JsonSerializer.Serialize(session.Current);
   using(var locked=new FileStream(Path.Combine(MatchStore.Folder,session.Current.Id+".json"),FileMode.Open,FileAccess.Read,FileShare.None))
   {bool failed=false;try{session.Names("Changed","Other");}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){failed=true;}Need(failed&&JsonSerializer.Serialize(session.Current)==before,"Command rollback");}
  }
  var match=session.Current;var current=session.Map!;var previous=current.Statistics;
  using(var locked=new FileStream(Path.Combine(MatchStore.Folder,match.Id+".json"),FileMode.Open,FileAccess.Read,FileShare.None))
  using(var print=new System.Drawing.Bitmap(3,3))
  {bool failed=false;try{MatchFiles.SaveStatistics(match,current,new(){Status="partial"},print);}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){failed=true;}Need(failed&&current.Statistics==previous&&Directory.GetFiles(Path.Combine(MatchFiles.DirectoryFor(match),"Estatisticas")).Length==0,"Statistics and screenshot rollback");}
  foreach(string format in new[]{"MD1","MD3","MD5"})
  {
   session.New();session.Setup(format,0,0,false);for(int n=0;n<8;n++)session.Veto(n,catalog.Maps[n].Id);
   int needed=session.Current.WinsRequired;for(int played=0;played<needed;played++)
   {
    string id=session.Current.Maps[played].MapId;session.SelectMap(id);session.Side(id,false,0);session.Side(id,true,1);var record=session.Map!;
    for(int round=0;round<12;round++){string site=catalog.Maps.Single(m=>m.Id==id).Sites.First(s=>!record.SiteBlocked(s,round+1));session.Round(site,round%2,"elimination");}
    Need(record.Score().A==6&&record.Score().B==6&&record.Attacker(13)==1&&record.Attacker(14)==0,"Overtime sides and 6-6 score");
    session.Round(catalog.Maps.Single(m=>m.Id==id).Sites[0],0,"elimination");session.Round(catalog.Maps.Single(m=>m.Id==id).Sites[0],0,"elimination");session.Confirm();Need(record.Score().A==8&&record.Score().B==6&&record.Confirmed,"8-6 overtime confirmation");
   }
   Need(session.Current.Complete,"Full series completion");
  }
  var packageMatch=new SiegeMatch{TeamA="Package A",TeamB="Package B"};string directory=MatchFiles.Ensure(packageMatch);
  using(var logo=new System.Drawing.Bitmap(4,4))MatchFiles.AtomicImage(Path.Combine(directory,"Recursos","logo-a.png"),logo);
  MatchFiles.SaveBrand(packageMatch,new(){TeamALogo="Recursos/logo-a.png"});string archive=Path.Combine(root,"match.casterdesk");MatchPackage.Export(packageMatch,archive);
  Directory.Move(directory,directory+"-reference");var imported=MatchPackage.Import(archive,catalog,session.Matches);
  Need(imported.Id==packageMatch.Id&&File.Exists(MatchFiles.Resolve(imported,MatchFiles.ReadBrand(imported).TeamALogo)),"Portable match package restores logo");
  bool duplicate=false;try{MatchPackage.Import(archive,catalog,session.Matches);}catch(InvalidDataException){duplicate=true;}Need(duplicate,"Package duplicate rejected");
  var partialMap=new MapRecord{Rounds=[new(){Winner=0}],Statistics=new(){Status="partial",ConfirmedAt=DateTimeOffset.Now,Players=[new(){Team=0,PlayerId="player-one",Name="Original",Kills=3,Deaths=null,Assists=1}]}};
  var otherMap=new MapRecord{Rounds=[new(){Winner=0}],Statistics=new(){Status="partial",ConfirmedAt=DateTimeOffset.Now,Players=[new(){Team=0,PlayerId="player-one",Name="Variant",Kills=2,Deaths=4,Assists=null}]}};
  var summary=StatisticsData.ForSeries(new(){Maps=[partialMap,otherMap]});var player=summary.Players.Single();Need(summary.Partial&&player.Kills==5&&player.Deaths==4&&player.Assists==1&&player.KillMaps==2&&player.DeathMaps==1&&player.AssistMaps==1,"Partial totals preserve field coverage and identity");
  foreach(float scale in new[]{.6f,1f,1.3f})
  {
   var document=new OcrDocument{Width=(int)(1920*scale),Height=(int)(1080*scale)};
   void Word(string text,float x,float y,float width)=>document.Words.Add(new(){Text=text,X=x*scale,Y=y*scale,Width=width*scale,Height=20*scale});
   for(int n=0;n<10;n++){float y=300+n*38;Word("Player"+n,700,y,100);Word("4500",1100,y,45);Word((n+1).ToString(),1200,y,15);Word("4",1282,y,15);Word("2",1364,y,15);Word("99",1446,y,20);}
   var read=StatisticsData.Parse(document);Need(read.Count==10&&read[9].Team==1&&read[9].Name=="Player9"&&read[9].Kills==10&&read[9].Deaths==4&&read[9].Assists==2,"OCR geometry scaling and ping exclusion");
  }
  var samples=new (string Id,int[][] Values)[]{
   ("2bc6678f-53ca-4279-87a8-ba16fd8557be",[[4,9,3],[3,8,2],[6,7,0],[5,7,0],[3,8,2],[15,4,3],[10,4,2],[8,4,2],[2,5,5],[4,4,2]]),
   ("c1a80804-635e-4064-8f81-6bae9ab3e429",[[6,7,0],[4,7,1],[4,7,1],[2,7,0],[2,7,1],[12,4,4],[6,5,6],[6,3,2],[7,1,0],[4,5,2]]),
   ("3607bd42-b5e5-41be-ab88-73be148c9150",[[12,8,2],[10,5,4],[6,7,2],[8,3,0],[2,7,5],[12,7,2],[5,7,2],[6,8,2],[3,9,2],[4,7,2]])};
  int realRows=0,unread=0,mismatches=0;foreach(var sample in samples){var doc=JsonSerializer.Deserialize<OcrDocument>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"test-fixtures","ocr",sample.Id+".json")))!;var read=StatisticsData.Parse(doc);Need(read.Count is 0 or 10,"Ambiguous OCR layout rejected");if(sample.Id.StartsWith("2bc6678f"))Need(read.Count==10,"Clear reference fixture has ten rows");realRows+=read.Count;for(int n=0;n<read.Count;n++){int?[] values={read[n].Kills,read[n].Deaths,read[n].Assists};for(int column=0;column<3;column++){if(values[column]==null)unread++;else if(sample.Id.StartsWith("2bc6678f"))Need(values[column]==sample.Values[n][column],"Clear reference screenshot KDA");else if(values[column]!=sample.Values[n][column])mismatches++;}}}File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"ocr-fixture-check.txt"),$"Three stored OCR results: {realRows}/30 player rows identified; {unread} fields left unread; {mismatches} recognized values differ from the manual reference and require human correction. Missing data is never inferred. Windows OCR itself was not rerun.");
  File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"stability-check.txt"),$"PASS: {checks} transaction, recovery, package, partial statistics and match command checks.");
 }
}

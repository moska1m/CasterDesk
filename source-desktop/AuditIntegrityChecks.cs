using System.Text.Json;
namespace CasterDesk.Desktop;
internal static class AuditIntegrityChecks { internal static int Run() {


if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CASTERDESK_DATA_DIR")))throw new Exception("Isolation required");
L.Set("pt-BR");var cat=SiegeCatalog.Load();var s=new MatchSession(cat);int checks=0;
void Need(bool ok,string text){if(!ok)throw new Exception(text);checks++;}
string State()=>JsonSerializer.Serialize(s.Current,MatchStore.Options);
string Disk()=>File.ReadAllText(Path.Combine(MatchStore.Folder,s.Current.Id+".json"));
void Reject(Action command,string text){var before=State();var file=Disk();bool rejected=false;try{command();}catch(Exception ex)when(ex is InvalidOperationException or InvalidDataException){rejected=true;}Need(rejected,text+" rejected");Need(State()==before,text+" in-memory preserved");Need(Disk()==file,text+" disk preserved");}
void Start(string format="MD1"){s.New();s.ConfigureSpec(format,cat.Maps.Take(format=="MD3"?3:format=="MD5"?5:1).Select(m=>m.Id).ToArray());s.Side(s.Map!.MapId,false,0);}
void Stats()=>MatchFiles.SaveStatistics(s.Current,s.Map!,new MapStatistics{Status="partial",ConfirmedAt=DateTimeOffset.Now,Players=[new PlayerStatistic{Team=0,Name="Preserved",Kills=9}]},null);
Start();s.Side(s.Map!.MapId,true,0);for(int n=0;n<12;n++)s.Round("",n%2,"elimination");s.Round("",0,"defuser_completed");Reject(()=>s.Side(s.Map!.MapId,true,1),"incompatible OT correction");Need(s.Map!.Rounds[12].Ending=="defuser_completed"&&s.Map.OtAttack==0,"OT ending and attacker unchanged");
Start();var id=s.Map!.MapId;s.Ban(0,MapRecord.NoBan(s.Map.BanRole(0)));s.Round("",0,"defuser_completed");Reject(()=>s.Side(id,false,1,true),"incompatible initial-side correction");Need(s.Map!.Bans[0]!=""&&s.Map.Rounds[0].Ending=="defuser_completed","initial correction preserves bans and ending");
Start();id=s.Map!.MapId;s.Ban(0,MapRecord.NoBan(s.Map.BanRole(0)));s.Round("",0,"elimination");s.Side(id,false,1,true);Need(s.Map!.InitialAttack==1&&s.Map.Rounds[0].Ending=="elimination"&&s.Map.Bans.All(string.IsNullOrEmpty),"compatible side correction keeps round and applies explicit ban reset");
Start();Stats();Need(!s.Map!.HasData&&s.Map.HasPreservedData,"statistics protection separate from side validation");Reject(()=>s.ConfigureSpec("MD1",[cat.Maps[1].Id]),"SPEC replacement with statistics");Reject(()=>s.ConfigureSpec("MD3",cat.Maps.Take(3).Select(m=>m.Id).ToArray()),"SPEC format change with statistics");Reject(()=>s.Setup("MD3",0,0,false),"direct setup reset with statistics");s.ConfigureSpec("MD1",[s.Map!.MapId]);Need(s.Map!.Statistics!.Players.Single().Kills==9,"unchanged SPEC configuration retains statistics");
// Statistics without rounds/sides remain valid for early print review.
s.New();s.ConfigureSpec("MD1",[cat.Maps[0].Id]);Stats();MatchStore.Validate(s.Current,cat);Need(s.Map!.InitialAttack==-1&&s.Map.Statistics!=null,"early statistics accepted without initial sides");
s.New();s.Setup("MD3",0,0,false);for(int n=0;n<8;n++)s.Veto(n,cat.Maps[n].Id);s.SelectMap(cat.Maps[4].Id);Stats();Reject(()=>s.Veto(4,cat.Maps[8].Id,false),"Caster veto replacement with statistics");Reject(()=>s.ApplyArtworkVeto(s.Current.Id,new ImageBrand()),"applying artwork veto over statistics");s.Veto(4,cat.Maps[8].Id,true);Need(s.Current.Veto[4]==cat.Maps[8].Id&&s.Current.Maps.Sum(m=>m.Statistics?.Players.Count??0)==0,"explicit destructive replacement remains possible");s.Undo();Need(s.Current.Maps.Single(m=>m.MapId==cat.Maps[4].Id).Statistics!.Players.Single().Kills==9,"undo explicit replacement restores statistics");var reloaded=MatchStore.Read(Path.Combine(MatchStore.Folder,s.Current.Id+".json"),cat);Need(reloaded.Maps.Single(m=>m.MapId==cat.Maps[4].Id).Statistics!.Players.Single().Kills==9,"saved statistics survive reload");
return checks;

} }
using System.IO.Compression;
using System.Text.Json;
using CasterDesk.Core;

static class ReplayChecks
{
    static void Need(bool value,string label){if(!value)throw new Exception("Replay: "+label);}
    public static void Run()
    {
        Need(ReplayImport.MapId("VillaY10")=="villa" && ReplayImport.MapId("BankY10")=="bank","modernized map identifiers");
        Need(ReplayImport.MapId("UnknownMap")=="","do not guess unknown map");
        Need(ReplayImport.SameName(" FAZE ","faze"),"explicit team normalization");
        bool refused=false;try{ReplayImport.ValidateDocument(new());}catch(InvalidDataException){refused=true;}Need(refused,"invalid document refused");
        var stats=new MapStatistics{Source="replay",Status="partial",Players=[new(){Team=0,Name="Player",Kills=null,Deaths=0,Assists=null,UbisoftProfileId=Guid.NewGuid().ToString()}]};
        StatisticsData.Validate(stats);
        Need(stats.Players[0].Kills==null && stats.Players[0].Deaths==0,"missing is distinct from zero");
        stats.ReplayArchive="../outside.zip";refused=false;try{StatisticsData.Validate(stats);}catch(InvalidDataException){refused=true;}Need(refused,"archive traversal refused");
        Console.WriteLine("Replay validation checks passed.");
    }

    // Explicit opt-in integration test: reads local files, writes exclusively to CASTERDESK_DATA_DIR.
    public static async Task RunLocal(string matchPath,string replayRoot)
    {
        byte[] original=File.ReadAllBytes(matchPath);
        var match=MatchStore.Read(matchPath,SiegeCatalog.Load());
        var folders=Directory.GetDirectories(replayRoot,"Match-2026-10-17_*");
        foreach(var map in match.Maps.Where(m=>m.Rounds.Count>0)){
            using var preview=await ReplayImport.ReadAsync(folders,match,map,null,CancellationToken.None);
            var modes=Enumerable.Repeat(true,preview.Sessions.Count-1).ToArray();
            if(modes.Length>0){bool refused=false;try{preview.Consolidate(match,map,[]);}catch(InvalidDataException){refused=true;}Need(refused,"explicit session classification required");}
            var data=preview.Consolidate(match,map,modes);
            Need(data.Players.Count==10 && data.Players.All(p=>p.Kills.HasValue&&p.Deaths.HasValue&&p.Assists.HasValue),"ten cumulative stat rows");
            if(map.MapId=="villa"){
                Need(preview.Sessions.Count==1,"one Mansion session");
                Need(data.Players.Sum(p=>p.ReplayDetails!.Plants)==1 && data.Players.Sum(p=>p.ReplayDetails!.OpeningKills)==9 && data.Players.Sum(p=>p.ReplayDetails!.OpeningDeaths)==9 && data.Players.All(p=>!p.ReplayDetails!.Partial),"Mansion extra events and coverage");
                foreach(var p in data.Players){var saved=map.Statistics!.Players.Single(s=>s.Team==p.Team&&s.Name==p.Name);Need(p.Kills==saved.Kills && p.Deaths==saved.Deaths,"Mansion kills/deaths match saved");Need(p.Assists==(p.Name=="Gomes7.FaZe"?1:saved.Assists),"Mansion assists match reviewed print");}
            }else if(map.MapId=="bank"){
                Need(preview.Sessions.Count==2,"same lobby return grouped; new lobby separate");
                Need(preview.Files.Count==10,"all Bank source files, including interrupted one");
                bool refused=false;try{preview.Consolidate(match,map,[false]);}catch(InvalidDataException){refused=true;}Need(refused,"reset counters cannot be mistaken for restored totals");
                data=preview.Consolidate(match,map,modes);
                Need(data.Players.All(p=>p.ReplayDetails!.Partial && p.ReplayDetails.ObservedRounds==9 && p.ReplayDetails.ExpectedRounds==10),"missing Bank events stay partial despite restored KDA");
            }
            File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"replay-"+map.MapId+".txt"),preview.Report);
            MatchFiles.SaveStatistics(match,map,data,null,true,preview);
            string archive=Path.Combine(MatchFiles.DirectoryFor(match),data.ReplayArchive);
            using(var zip=ZipFile.OpenRead(archive))Need(zip.Entries.Count(e=>e.FullName.EndsWith(".rec",StringComparison.Ordinal))==preview.Files.Count,"source replays archived");
            var read=MatchStore.Read(Path.Combine(MatchFiles.DirectoryFor(match),"partida.json"),SiegeCatalog.Load());
            Need(read.Maps.Single(m=>m.MapId==map.MapId).Statistics!.Players.All(p=>p.UbisoftProfileId!=""),"Ubisoft identities survive persistence");
            Need(read.Maps.Single(m=>m.MapId==map.MapId).Statistics!.Players.All(p=>p.ReplayDetails is not null),"extra stats survive persistence");
            // A failed archive preparation must not replace already confirmed statistics.
            var savedStats=map.Statistics;
            File.WriteAllText(preview.Files[0].Copy,"changed-test-copy");
            bool rejected=false;try{MatchFiles.SaveStatistics(match,map,new(){Source="replay",Status="partial"},null,true,preview);}catch(IOException){rejected=true;}
            Need(rejected&&ReferenceEquals(map.Statistics,savedStats),"archive failure preserves saved statistics");
        }
        Need(File.ReadAllBytes(matchPath).SequenceEqual(original),"original user match untouched");
        Console.WriteLine("Local replay integration passed: cumulative snapshots, explicit remake, archive, persistence, and rollback; original match unchanged.");
    }
}

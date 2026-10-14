using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CronoLive;

public static class MatchInterchangeTests
{
    public static void Run()
    {
        string previous=L.Current;int checks=0;
        void Check(bool value,string message){if(!value)throw new Exception("Match interchange: "+message);checks++;}
        try
        {
            var catalog=SiegeCatalog.Load();
            foreach(string creator in L.Languages)
            {
                L.Set(creator);
                foreach(var map in catalog.Maps)
                {
                    var match=new SiegeMatch{Created=new DateTime(2026,10,13,4,0,0),CoinWinner=0,FirstBan=0,
                        Veto=catalog.Maps.Where(m=>m.Id!=map.Id).Select(m=>m.Id).ToArray()};match.SyncMaps(catalog);
                    var record=match.Maps[0];record.InitialAttack=0;record.OtAttack=1;
                    for(int i=0;i<4;i++)record.Rounds.Add(new(){Winner=i==2?0:1,Site=map.Sites[i],Ending="elimination"});
                    MatchStore.Validate(match,catalog);
                    string json=JsonSerializer.Serialize(match,MatchStore.Options);var root=JsonNode.Parse(json)!;
                    Check(root["Version"]!.GetValue<int>()==2,"schema version");
                    Check(root["TeamA"]!.GetValue<string>()=="Team A"&&root["TeamB"]!.GetValue<string>()=="Team B","English default names");
                    Check(root["Format"]!.GetValue<string>()=="BO1","English match format");
                    foreach(var round in root["Maps"]![0]!["Rounds"]!.AsArray())Check(!string.IsNullOrWhiteSpace(round!["SiteId"]!.GetValue<string>()),"stable site identifier");
                    string path=Path.Combine(AppPaths.DataDirectory,"interchange-"+map.Id+".json");File.WriteAllText(path,json);
                    foreach(string reader in L.Languages)
                    {
                        L.Set(reader);var imported=MatchStore.Read(path,catalog);
                        Check(imported.TeamA==L.T("Equipe Azul")&&imported.TeamB==L.T("Equipe Laranja"),"localized default teams");
                        Check(imported.Maps[0].MapId==map.Id&&imported.Maps[0].Rounds.Select(r=>r.Site).SequenceEqual(map.Sites),"map and sites preserved");
                        Check(imported.Maps[0].SiteBlocked(map.Sites[0],3),"site cooldown survives migration");
                        Check(JsonSerializer.Serialize(imported,MatchStore.Options)==json,"same file vocabulary in every language");
                    }
                    // Old files had no flags or IDs and stored their creator's translated labels.
                    var legacy=JsonNode.Parse(json)!.AsObject();legacy["Version"]=1;legacy.Remove("TeamAIsDefault");legacy.Remove("TeamBIsDefault");
                    legacy["TeamA"]="Equipo A";legacy["TeamB"]="Equipe B";legacy["Format"]="MD1";
                    var legacyMap=legacy["Maps"]![0]!.AsObject();legacyMap["MapId"]=map.Name;
                    for(int i=0;i<4;i++){var round=legacyMap["Rounds"]![i]!.AsObject();round.Remove("SiteId");round["Site"]=L.Entries[map.Sites[i]][1];}
                    for(int i=0;i<8;i++){var item=catalog.Maps.Single(m=>m.Id==match.Veto[i]);legacy["Veto"]![i]=L.Entries.TryGetValue(item.Name,out var names)?names[0]:item.Name;}
                    File.WriteAllText(path,legacy.ToJsonString());
                    foreach(string reader in L.Languages){L.Set(reader);var imported=MatchStore.Read(path,catalog);Check(imported.TeamAIsDefault&&imported.TeamBIsDefault&&imported.Maps[0].Rounds.Select(r=>r.Site).SequenceEqual(map.Sites),"legacy multilingual import");}
                }
            }
            var custom=new SiegeMatch{TeamA="Team A",TeamB="FaZe Clan"};
            string customJson=JsonSerializer.Serialize(custom);
            foreach(string reader in L.Languages){L.Set(reader);var imported=JsonSerializer.Deserialize<SiegeMatch>(customJson)!;Check(!imported.TeamAIsDefault&&imported.TeamA=="Team A"&&imported.TeamB=="FaZe Clan","explicit custom names preserved, even if identical to a default label");}
            var blank=new SiegeMatch{TeamA="",TeamB=" "};Check(blank.TeamAIsDefault&&blank.TeamBIsDefault,"clearing team name restores default");
            var invalid=new SiegeMatch{CoinWinner=0,FirstBan=0,Veto=catalog.Maps.Take(8).Select(m=>m.Id).ToArray()};invalid.SyncMaps(catalog);invalid.Maps[0].InitialAttack=0;invalid.Maps[0].Rounds.Add(new(){Site=catalog.Maps.Last().Sites[0],Winner=0});
            var invalidNode=JsonNode.Parse(JsonSerializer.Serialize(invalid))!;invalidNode["Maps"]![0]!["Rounds"]![0]!["SiteId"]="unknown-site";
            bool rejected=false;try{JsonSerializer.Deserialize<SiegeMatch>(invalidNode.ToJsonString());}catch(JsonException){rejected=true;}Check(rejected,"unknown site ID rejected rather than guessed");
            File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"match-interchange-check.txt"),$"PASS: {checks} checks; 3 creator languages × 3 reader languages × 9 maps / 36 bombsites; legacy imports; canonical English JSON; default/custom names; cooldown rules; unknown ID rejection.");
        }
        finally{L.Set(previous);}
    }
}

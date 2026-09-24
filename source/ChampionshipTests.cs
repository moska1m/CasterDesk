using System.Text.Json;
namespace CronoLive;
public static class ChampionshipTests
{
    public static void Run()
    {
        int checks=0;
        void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
        var catalog=SiegeCatalog.Load();
        Check(catalog.Maps.Count==9 && catalog.Operators.Count==78,"catalog");
        Check(catalog.Maps.All(m=>File.Exists(SiegeCatalog.Asset(m.Image))) && catalog.Operators.All(o=>File.Exists(SiegeCatalog.Asset(o.Icon))),"assets");
        // Every possible win sequence through round 15: no 7–6 win, finite OT.
        for(int mask=0;mask<32768;mask++)
        {
            var map=new MapRecord();int a=0,b=0;bool ot=false;
            for(int i=0;i<15;i++)
            {
                int team=(mask>>i)&1;map.Rounds.Add(new(){Winner=team});if(team==0)a++;else b++;
                if(a==6 && b==6)ot=true;
                bool done=ot?Math.Max(a,b)==8:Math.Max(a,b)==7;
                Check((map.Score().Winner>=0)==done,"end condition");
                if(done)break;
            }
            Check(map.Score().Winner>=0,"must finish");
        }
        var m=new SiegeMatch(){Format="MD3",FirstBan=0,CoinWinner=1,Veto=catalog.Maps.Take(8).Select(x=>x.Id).ToArray()};m.SyncMaps(catalog);
        Check(m.Maps.Count==3 && m.Maps[0].Picker==0 && m.Maps[1].Picker==1 && m.Maps[2].Picker==1,"md3 picks decider");
        m.Format="MD1";m.Maps.Clear();m.SyncMaps(catalog);Check(m.Maps.Count==1 && m.Maps[0].Picker==1,"md1 final ban chooser");
        var game=m.Maps[0];game.InitialAttack=0;game.OtAttack=1;
        Check(game.Attacker(1)==0 && game.Attacker(6)==0 && game.Attacker(7)==1 && game.Attacker(12)==1 && game.Attacker(13)==1 && game.Attacker(14)==0 && game.Attacker(15)==1,"sides");
        Check(game.ActiveBanHalf(13)==1 && game.ActiveBanHalf(14)==0,"ot bans");
        Check(game.BanRole(0)=="DEF" && game.BanRole(2)=="ATQ" && game.BanRole(6)=="ATQ","ban types");
        Check(MapRecord.BanThreshold(4)==3 && MapRecord.BanThreshold(6)==6 && MapRecord.BanThreshold(10)==9,"ban gates");
        var temp=Path.Combine(Path.GetTempPath(),"CronoLive-match-test-"+Guid.NewGuid().ToString("N"),"match.json");MatchStore.Atomic(temp,m);var loaded=MatchStore.Read(temp,catalog);Check(loaded.Maps[0].OtAttack==1,"json roundtrip");
        Check(loaded.Id==m.Id,"stable match identity");
        m.TeamA="Fnatic";MatchStore.Atomic(temp,m);Check(File.Exists(temp+".bak") && MatchStore.Read(temp+".bak",catalog).TeamA=="Equipe A","backup");
        var duplicate=JsonSerializer.Deserialize<SiegeMatch>(JsonSerializer.Serialize(m))!;duplicate.Veto[1]=duplicate.Veto[0];bool rejected=false;try{MatchStore.Validate(duplicate,catalog);}catch(InvalidDataException){rejected=true;}Check(rejected,"duplicate rejected");
        var confirmation=JsonSerializer.Deserialize<SiegeMatch>(JsonSerializer.Serialize(m))!;
        confirmation.Maps[0].ConfirmedAt=DateTimeOffset.Now;rejected=false;try{MatchStore.Validate(confirmation,catalog);}catch(InvalidDataException){rejected=true;}Check(rejected,"cannot confirm without winner");
        confirmation.Maps[0].Rounds=Enumerable.Range(0,7).Select(_=>new RoundRecord{Winner=0}).ToList();MatchStore.Validate(confirmation,catalog);Check(confirmation.Complete,"confirmed MD1 complete");
        confirmation.Maps[0].ConfirmedAt=null;Check(!confirmation.Complete,"score alone does not confirm");
        var series=new SiegeMatch{Format="MD3",Maps=[new(){Rounds=Enumerable.Range(0,7).Select(_=>new RoundRecord{Winner=0}).ToList(),ConfirmedAt=DateTimeOffset.Now},new(){Rounds=Enumerable.Range(0,7).Select(_=>new RoundRecord{Winner=0}).ToList()}]};
        Check(!series.Complete,"one confirmed map leaves series open");series.Maps[1].ConfirmedAt=DateTimeOffset.Now;Check(series.Complete,"two confirmations end MD3");
        // Build native UI with real assets without modifying the working user's timer.
        using(var window=new Form(){ClientSize=new Size(1240,900)})
        {
            var panel=new ChampionshipPanel(true);window.Controls.Add(panel);window.Show();Application.DoEvents();panel.VerifyRoundEditing();Check(true,"native round editing");window.Close();
        }
        File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"championship-tests.txt"),$"{checks} verificações aprovadas (inclui sequências de placar exaustivas, lados, bans, JSON e construção da tela).");
    }
}

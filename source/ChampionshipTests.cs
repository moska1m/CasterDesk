using System.Text.Json;
namespace CronoLive;
public static class ChampionshipTests
{
    public static void Run()
    {
        int checks=0;
        void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
        var catalog=SiegeCatalog.Load();
        foreach(int initial in new[]{0,1})foreach(int winRound in Enumerable.Range(1,12))
        {
            var gate=new MapRecord{InitialAttack=initial,OtAttack=initial};
            for(int r=1;r<=winRound;r++)gate.Rounds.Add(new(){Winner=r==winRound?1-gate.Attacker(r):gate.Attacker(r),Site=r==winRound?"site":"other"});
            for(int next=winRound+1;next<=15;next++)Check(gate.SiteBlocked("site",next)==(next<=12 && next<=winRound+2 && (winRound-1)/6==(next-1)/6),"Cooldown two rounds, halftime reset and no OT");
            gate.Rounds[^1].Winner=gate.Attacker(winRound);Check(!gate.SiteBlocked("site",winRound+1),"Attack win does not block site");
        }
        Check(catalog.Maps.Count==9 && catalog.Operators.Count==78,"catalog");
        Check(catalog.Maps.All(m=>File.Exists(SiegeCatalog.Asset(m.Image))) && catalog.Operators.All(o=>File.Exists(SiegeCatalog.Asset(o.Icon))),"assets");
        Check(catalog.Operators.All(o=>File.Exists(SiegeCatalog.Asset(o.Id+"-portrait.png"))),"Portraits packaged for all operators");
        using(var body=AppFonts.Body(10))using(var bold=AppFonts.Body(10,FontStyle.Bold))Check(body.FontFamily.Name=="Segoe UI" && bold.FontFamily.Name=="Segoe UI" && bold.Bold,"System font and bold weight restored");
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
        var temp=Path.Combine(AppPaths.DataDirectory,"CronoLive-match-test-"+Guid.NewGuid().ToString("N"),"match.json");MatchStore.Atomic(temp,m);var loaded=MatchStore.Read(temp,catalog);Check(loaded.Maps[0].OtAttack==1,"json roundtrip");
        Check(loaded.Id==m.Id,"stable match identity");
        m.TeamA="Fnatic";MatchStore.Atomic(temp,m);Check(File.Exists(temp+".bak") && MatchStore.Read(temp+".bak",catalog).TeamA=="Equipe A","backup");
        var duplicate=JsonSerializer.Deserialize<SiegeMatch>(JsonSerializer.Serialize(m))!;duplicate.Veto[1]=duplicate.Veto[0];bool rejected=false;try{MatchStore.Validate(duplicate,catalog);}catch(InvalidDataException){rejected=true;}Check(rejected,"duplicate rejected");
        var confirmation=JsonSerializer.Deserialize<SiegeMatch>(JsonSerializer.Serialize(m))!;
        confirmation.Maps[0].ConfirmedAt=DateTimeOffset.Now;rejected=false;try{MatchStore.Validate(confirmation,catalog);}catch(InvalidDataException){rejected=true;}Check(rejected,"cannot confirm without winner");
        confirmation.Maps[0].Rounds=Enumerable.Range(0,7).Select(_=>new RoundRecord{Winner=0}).ToList();MatchStore.Validate(confirmation,catalog);Check(confirmation.Complete,"confirmed MD1 complete");
        confirmation.Maps[0].ConfirmedAt=null;Check(!confirmation.Complete,"score alone does not confirm");
        var series=new SiegeMatch{Format="MD3",Maps=[new(){Rounds=Enumerable.Range(0,7).Select(_=>new RoundRecord{Winner=0}).ToList(),ConfirmedAt=DateTimeOffset.Now},new(){Rounds=Enumerable.Range(0,7).Select(_=>new RoundRecord{Winner=0}).ToList()}]};
        Check(!series.Complete,"one confirmed map leaves series open");series.Maps[1].ConfirmedAt=DateTimeOffset.Now;Check(series.Complete,"two confirmations end MD3");
        foreach(int winner in new[]{0,1})
        foreach(int length in new[]{3,4,5})
        {
            var bo5=new SiegeMatch{Format="MD5"};
            var wins=Enumerable.Repeat(1-winner,length-3).Concat(Enumerable.Repeat(winner,3)).ToArray();
            foreach(int who in wins)
            {
                Check(!bo5.Complete,"MD5 must remain open before third confirmed win");
                var result=new MapRecord{Rounds=Enumerable.Range(0,7).Select(_=>new RoundRecord{Winner=who}).ToList()};
                bo5.Maps.Add(result);Check(!bo5.Complete,"MD5 scores require confirmation");result.ConfirmedAt=DateTimeOffset.Now;
            }
            Check(bo5.Complete && bo5.WinsRequired==3,"MD5 ends at 3-0, 3-1 or 3-2");
        }
        foreach(int first in new[]{0,1})
        {
            var five=new SiegeMatch{Format="MD5",FirstBan=first,CoinWinner=1-first};
            Check(Enumerable.Range(0,8).Select(five.Action).SequenceEqual(new[]{"BAN","BAN","PICK","PICK","PICK","PICK","BAN","BAN"}),"MD5 veto order");
            for(int step=0;step<8;step++)
            {
                five.Veto[step]=catalog.Maps[step].Id;five.SyncMaps(catalog);
                Check(five.Maps.Count==Math.Clamp(step-1,0,4)+(step==7?1:0),"MD5 progressive picks");
                MatchStore.Validate(five,catalog);
            }
            Check(five.Maps.Select(x=>x.Picker).SequenceEqual(new[]{first,1-first,first,1-first,1-first}),"MD5 pick and decider owners");
            var file=Path.Combine(AppPaths.DataDirectory,$"md5-{first}.json");MatchStore.Atomic(file,five);
            var copy=MatchStore.Read(file,catalog);Check(copy.Id==five.Id && copy.Maps.Count==5 && copy.Format=="MD5","MD5 JSON roundtrip");
            var retained=five.Maps[1];five.SyncMaps(catalog);Check(ReferenceEquals(retained,five.Maps[1]),"MD5 sync preserves existing map");
        }
        foreach(var info in catalog.Maps)
        {
            using var gray=new Bitmap(SiegeCatalog.Asset(info.UnavailableImage));
            for(int y=0;y<gray.Height;y+=31)for(int x=0;x<gray.Width;x+=31){var p=gray.GetPixel(x,y);Check(p.R==p.G && p.G==p.B,"Grayscale asset");}
        }
        foreach(int count in new[]{0,2,3,5,6,8,9})
        {
            var stage=new MapRecord{InitialAttack=0,Rounds=Enumerable.Range(0,count).Select(i=>new RoundRecord{Winner=i%2}).ToList()};
            Check(stage.PendingBans(0).Length==(count<3?4:6),"First-half pending stage");
            Check(stage.PendingBans(1).Length==(count<6?0:count<9?4:6),"Second-half pending stage");
        }
        var sevenTwo=new MapRecord{InitialAttack=0,Rounds=new[]{0,0,0,0,0,0,1,1,0}.Select(w=>new RoundRecord{Winner=w}).ToList()};Check(sevenTwo.PendingBans(1).Length==4,"No R10 reminder after 7-2 finish");
        // Build native UI with real assets without modifying the working user's timer.
        using(var window=new Form(){ClientSize=new Size(1240,900)})
        {
            var panel=new ChampionshipPanel(true);window.Controls.Add(panel);window.Show();Application.DoEvents();panel.VerifyRoundEditing();Check(true,"native round editing");panel.VerifyMd5();Check(true,"MD5 native layout and sides");panel.VerifySmallImprovements();Check(true,"Map cards, correction and reminders");panel.VerifyQuickRoundsAndPopups();Check(true,"Quick rounds, endings and central popups");panel.VerifyBombsiteCooldown();Check(true,"Site gates and correction UI");window.Close();
        }
        using(var window=new Form(){ClientSize=new Size(1240,900)}){var panel=new ChampionshipPanel(true);window.Controls.Add(panel);window.Show();Application.DoEvents();panel.VerifyResponsiveDesign();Check(true,"Responsive design, portrait cards and persistent actions");window.Close();}
        File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"championship-tests.txt"),$"{checks} verificações aprovadas (inclui sequências de placar exaustivas, lados, bans, JSON e construção da tela).");
    }
}



namespace CasterDesk.Avalonia;

internal static class CorrectionChecks
{
    internal static void Run()
    {
        int count = 0;
        void Need(bool value, string message) { count++; if(!value)throw new Exception("Corrections: " + message); }
        foreach(int attacker in new[]{0,1}) foreach(int half in new[]{0,1}) {
            var map = new MapRecord { InitialAttack=attacker };
            var order=map.BanOrder(half);
            Need(order.Select(map.BanRole).SequenceEqual(new[]{"DEF","ATQ","DEF","ATQ","DEF","ATQ"}),"All stages alternate without changing slot identity");
            Need(order.Order().SequenceEqual(Enumerable.Range(half*6,6)),"Each responsible team/slot occurs once");
        }
        var match=new SiegeMatch(); var winnerMap=new MapRecord { InitialAttack=0, ConfirmedAt=DateTimeOffset.Now };
        winnerMap.Rounds=Enumerable.Range(0,7).Select(_=>new RoundRecord {Winner=0,Ending="elimination"}).ToList();
        winnerMap.Statistics=new MapStatistics { Status="partial",ConfirmedAt=DateTimeOffset.Now,Players=new(){
            new(){Team=0,Name="Winner A",Kills=12,Deaths=4,Assists=2},new(){Team=0,Name="Winner B",Kills=18,Deaths=7,Assists=0},new(){Team=1,Name="Loser",Kills=25,Deaths=2,Assists=5}} };
        match.Maps.Add(winnerMap);
        Need(StatisticsData.SuggestMvp(match,winnerMap)?.Name=="Winner B","MVP is highest eliminations on winning team, even if losing player has more");
        Need(StatisticsData.SuggestMvp(match)?.Name=="Winner B","Series MVP uses winning team");
        var catalog = SiegeCatalog.Load(); winnerMap.MapId=catalog.Maps[0].Id;
        foreach(var aspect in new[]{ExportAspect.Stories,ExportAspect.Feed,ExportAspect.Horizontal}) {
            var pages=StudioRenderer.Render(match,catalog,new ImageBrand(),ArtKind.MapStats,aspect,winnerMap);
            try { foreach(var page in pages) page.Image.Save(Path.Combine(AppPaths.DataDirectory,$"mvp-order-{aspect}.png"),System.Drawing.Imaging.ImageFormat.Png); }
            finally { foreach(var page in pages) page.Dispose(); }
        }
        winnerMap.Statistics.Players[1].Kills=null;
        Need(StatisticsData.SuggestMvp(match,winnerMap)?.Name=="Winner A","Unknown eliminations are never counted as zero");
        winnerMap.Rounds.Clear(); Need(StatisticsData.SuggestMvp(match,winnerMap)==null,"No automatic MVP before a winner exists");
        Need(BroadcastChannel.Normalize("caster")=="twitch.tv/caster","Bare channel becomes Twitch address");
        Need(BroadcastChannel.Normalize("twitch.tv/")=="","Empty Twitch prefix is not published");
        Need(BroadcastChannel.Normalize("youtube.com/@existing")=="youtube.com/@existing","Existing custom channel preserved");
        Need(ChampionshipView.ShortSite("fortress","Banheiro - Escritório comandante")=="Banheiro / Comandante","History short form only");
        File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"avalonia-corrections-check.txt"),$"PASS: {count} checks for stable ban ordering, winner-only MVP, missing kills, channel defaults and history abbreviations.");
    }
}

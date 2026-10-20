using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CasterDesk.Core;
public sealed class PlayerStatistic
{
    public PlayerReplayDetails? ReplayDetails{get;set;}
    public string PlayerId{get;set;}="";
    public string UbisoftProfileId{get;set;}="";
    public int Team{get;set;}
    public string Name{get;set;}="";
    public int? Kills{get;set;}
    public int? Deaths{get;set;}
    public int? Assists{get;set;}
}
public sealed class PlayerReplayDetails
{
    public int ObservedRounds{get;set;}
    public int ExpectedRounds{get;set;}
    public int ObservedKills{get;set;}
    public int Headshots{get;set;}
    public int OpeningKills{get;set;}
    public int OpeningDeaths{get;set;}
    public int Plants{get;set;}
    public int Defuses{get;set;}
    [System.Text.Json.Serialization.JsonIgnore] public bool Partial=>ObservedRounds<ExpectedRounds;
    [System.Text.Json.Serialization.JsonIgnore] public double? HeadshotPercentage=>ObservedKills>0?100.0*Headshots/ObservedKills:null;
}
public sealed class MapStatistics
{
    public string Source{get;set;}="manual";
    public string ReplayArchive{get;set;}="";
    public string Status{get;set;}="complete";
    public string Screenshot{get;set;}="";
    // Null preserves legacy/manual records without guessing which team was above the other.
    public int? TopTeam{get;set;}
    public DateTimeOffset? ConfirmedAt{get;set;}
    public List<PlayerStatistic> Players{get;set;}=[];
}
public sealed record SummedPlayer(int Team,string Name,int? Kills,int? Deaths,int? Assists,int Maps)
{
    public PlayerReplayDetails? ReplayDetails{get;init;}
    public string PlayerId{get;init;}="";
    public int KillMaps{get;init;}
    public int DeathMaps{get;init;}
    public int AssistMaps{get;init;}
    public string Key=>Team+":"+Name.Trim().ToUpperInvariant();
}
public sealed record StatisticsSummary(List<SummedPlayer> Players,bool Partial,int MapsIncluded,int MapsPlayed);
public static class StatisticsData
{
    public static async Task<string> Diagnose(CancellationToken token)
    {
        string shell=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell","v1.0","powershell.exe");
        var info=new ProcessStartInfo(shell){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=System.Text.Encoding.UTF8};
        foreach(string arg in new[]{"-NoProfile","-NonInteractive","-ExecutionPolicy","Bypass","-File",Path.Combine(AppContext.BaseDirectory,"OcrImage.ps1"),"-Diagnostic"})info.ArgumentList.Add(arg);
        using var process=Process.Start(info)??throw new IOException(L.T("Não foi possível iniciar a leitura do print."));
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(TimeSpan.FromSeconds(20));var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();
        try{await process.WaitForExitAsync(timeout.Token);}catch{try{process.Kill(true);}catch{}throw;}
        string text=await output,detail=await error;if(process.ExitCode!=0)throw new IOException(L.T("Leitura automática indisponível. Você pode preencher os dados manualmente.")+"\n"+detail.Split('\n').FirstOrDefault());
        using var report=JsonDocument.Parse(text);return report.RootElement.GetProperty("Available").GetBoolean()?L.T("OCR disponível. Idiomas: ")+string.Join(", ",report.RootElement.GetProperty("Languages").EnumerateArray().Select(e=>e.GetString())):L.T("Leitura automática indisponível. Você pode preencher os dados manualmente.");
    }
    public static void Validate(MapStatistics data)
    {
        if(data.Source is not ("manual" or "replay") || data.ReplayArchive is null || data.ReplayArchive.Length>200 || Path.IsPathRooted(data.ReplayArchive) || data.ReplayArchive.Split('/','\\').Contains(".."))throw new InvalidDataException("Referência de replay inválida.");
        if(data.Players?.Any(p=>p is not null && (p.UbisoftProfileId is null || (p.UbisoftProfileId!="" && !Guid.TryParse(p.UbisoftProfileId,out _))))==true)throw new InvalidDataException("Perfil de jogador do replay inválido.");
        if(data.Players?.Any(p=>p?.ReplayDetails is {} d && (d.ObservedRounds<0 || d.ExpectedRounds<d.ObservedRounds || d.ExpectedRounds>100 || new[]{d.ObservedKills,d.Headshots,d.OpeningKills,d.OpeningDeaths,d.Plants,d.Defuses}.Any(n=>n<0||n>500) || d.Headshots>d.ObservedKills))==true)throw new InvalidDataException("Estatísticas extras do replay inválidas.");
        if(data.TopTeam is <0 or >1)throw new InvalidDataException(L.T("Estatísticas inválidas."));
        if(data.Status is not ("complete" or "partial" or "missing")||data.Players is null||data.Players.Count>20||data.Screenshot is null||data.Screenshot.Length>200)throw new InvalidDataException(L.T("Estatísticas inválidas."));
        if(!string.IsNullOrEmpty(data.Screenshot)&&(Path.IsPathRooted(data.Screenshot)||data.Screenshot.Split('/','\\').Contains("..")))throw new InvalidDataException(L.T("Caminho de print inválido."));
        if(data.Players.Any(p=>p is null||p.Team is <0 or >1||string.IsNullOrWhiteSpace(p.Name)||p.Name.Length>60||p.Kills is <0 or >99||p.Deaths is <0 or >99||p.Assists is <0 or >99))throw new InvalidDataException(L.T("Confira nomes e valores de eliminações, mortes e assistências."));
        if(data.Players.GroupBy(p=>(p.Team,p.Name.Trim().ToUpperInvariant())).Any(g=>g.Count()>1))throw new InvalidDataException(L.T("Há jogadores repetidos na mesma equipe."));
        if(data.Players.Any(p=>p.PlayerId is null || p.PlayerId.Length>64) || data.Players.Where(p=>p.PlayerId!="").GroupBy(p=>(p.Team,p.PlayerId)).Any(g=>g.Count()>1))throw new InvalidDataException(L.T("Identidade de jogador inválida ou repetida."));
        if(data.Status=="complete"&&(data.Players.Count!=10||data.Players.Count(p=>p.Team==0)!=5||data.Players.Any(p=>p.Kills is null||p.Deaths is null||p.Assists is null)))throw new InvalidDataException(L.T("Estatísticas completas exigem cinco jogadores por equipe e todos os valores."));
        if(data.Status=="missing"&&data.Players.Count!=0)throw new InvalidDataException(L.T("Marque como parcial para guardar os dados disponíveis."));
    }
    public static StatisticsSummary ForMap(MapRecord map)=>Sum([map]);
    public static StatisticsSummary ForSeries(SiegeMatch match)=>Sum(match.Maps.Where(m=>m.Rounds.Count>0).ToList());
    // Render-time guard: OCR variants must be reconciled explicitly, never fuzzily merged.
    public static string SeriesIssue(SiegeMatch match)
    {
        var summary=ForSeries(match);
        var problems=new List<string>();
        for(int team=0;team<2;team++) {
            var players=summary.Players.Where(p=>p.Team==team).ToList();
            if(players.Count>5)problems.Add(match.Team(team)+": "+string.Join(", ",players.Select(p=>p.Name)));
            else if(players.GroupBy(p=>p.Name.Trim().ToUpperInvariant()).Any(g=>g.Count()>1))
                problems.Add(match.Team(team)+": "+string.Join(", ",players.Select(p=>p.Name)));
        }
        return problems.Count==0?"":L.T("Há nomes incompatíveis entre os mapas. Concilie os jogadores antes de gerar as estatísticas da série.")+"\n"+string.Join("\n",problems);
    }
    public static SummedPlayer? SuggestMvp(SiegeMatch match, MapRecord? map = null)
    {
        int winner = map?.Score().Winner ?? (match.Complete ? (match.Maps.Count(m=>m.Confirmed && m.Score().Winner==0)>=match.WinsRequired?0:1) : -1);
        if(winner<0)return null;
        var summary = map == null ? ForSeries(match) : ForMap(map);
        return summary.Players.Where(p=>p.Team==winner && p.Kills.HasValue)
            .OrderByDescending(p=>p.Kills).ThenBy(p=>p.Name,StringComparer.OrdinalIgnoreCase).FirstOrDefault();
    }
    static StatisticsSummary Sum(IReadOnlyList<MapRecord> maps)
    {
        var included=maps.Where(m=>m.Statistics?.ConfirmedAt is not null && m.Statistics.Status!="missing" && m.Statistics.Players.Count>0).ToList();
        int? Known(IEnumerable<int?> values){var known=values.Where(v=>v.HasValue).ToList();return known.Count==0?null:known.Sum(v=>v!.Value);}
        var all=included.SelectMany(m=>m.Statistics!.Players).ToList();string Identity(PlayerStatistic p)=>!string.IsNullOrEmpty(p.PlayerId)?p.PlayerId:all.FirstOrDefault(k=>k.Team==p.Team&&k.PlayerId!=""&&string.Equals(k.Name.Trim(),p.Name.Trim(),StringComparison.OrdinalIgnoreCase))?.PlayerId??p.Name.Trim().ToUpperInvariant();
        var players=all.GroupBy(p=>(p.Team,Identity:Identity(p))).Select(g=>new SummedPlayer(g.Key.Team,g.First().Name.Trim(),Known(g.Select(p=>p.Kills)),Known(g.Select(p=>p.Deaths)),Known(g.Select(p=>p.Assists)),g.Count()){ReplayDetails=CombineReplayDetails(g.Select(p=>p.ReplayDetails)),PlayerId=g.First().PlayerId,KillMaps=g.Count(p=>p.Kills.HasValue),DeathMaps=g.Count(p=>p.Deaths.HasValue),AssistMaps=g.Count(p=>p.Assists.HasValue)}).ToList();
        bool partial=included.Count!=maps.Count||included.Any(m=>m.Statistics!.Status!="complete");
        return new(players,partial,included.Count,maps.Count);
    }
    static PlayerReplayDetails? CombineReplayDetails(IEnumerable<PlayerReplayDetails?> source)
    {
        var items=source.ToArray();if(items.Length==0||items.Any(d=>d is null))return null;
        return new(){ObservedRounds=items.Sum(d=>d!.ObservedRounds),ExpectedRounds=items.Sum(d=>d!.ExpectedRounds),ObservedKills=items.Sum(d=>d!.ObservedKills),Headshots=items.Sum(d=>d!.Headshots),OpeningKills=items.Sum(d=>d!.OpeningKills),OpeningDeaths=items.Sum(d=>d!.OpeningDeaths),Plants=items.Sum(d=>d!.Plants),Defuses=items.Sum(d=>d!.Defuses)};
    }
    public static void Identify(SiegeMatch match,MapStatistics data)
    {
        var known=match.Maps.SelectMany(m=>m.Statistics?.Players??[]).Where(p=>!string.IsNullOrEmpty(p.PlayerId)).ToList();
        foreach(var player in data.Players)
        {
            if(!string.IsNullOrEmpty(player.PlayerId))continue;
            var existing=known.FirstOrDefault(p=>p.Team==player.Team&&((player.UbisoftProfileId!=""&&p.UbisoftProfileId==player.UbisoftProfileId)||string.Equals(p.Name.Trim(),player.Name.Trim(),StringComparison.OrdinalIgnoreCase)));
            player.PlayerId=existing?.PlayerId??Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(match.Id+":"+player.Team+":"+player.Name.Trim().ToUpperInvariant())))[..32].ToLowerInvariant();
        }
    }
    public static async Task<OcrDocument> ReadImage(string file,CancellationToken token)
    {
        string? normalized=null;
        using(var image=Image.FromFile(file))
        {
            if(image.Width<1700)
            {
                float scale=Math.Min(2,2500f/Math.Max(image.Width,image.Height));
                normalized=Path.Combine(Path.GetTempPath(),"casterdesk-ocr-scale-"+Guid.NewGuid().ToString("N")+".png");
                using var bitmap=new Bitmap((int)(image.Width*scale),(int)(image.Height*scale));using(var g=Graphics.FromImage(bitmap)){g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.DrawImage(image,0,0,bitmap.Width,bitmap.Height);}bitmap.Save(normalized,System.Drawing.Imaging.ImageFormat.Png);file=normalized;
            }
        }
        try
        {
        string shell=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell","v1.0","powershell.exe");
        var info=new ProcessStartInfo(shell){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=System.Text.Encoding.UTF8};
        foreach(string arg in new[]{"-NoProfile","-NonInteractive","-ExecutionPolicy","Bypass","-File",Path.Combine(AppContext.BaseDirectory,"OcrImage.ps1"),"-ImagePath",Path.GetFullPath(file)})info.ArgumentList.Add(arg);
        using var process=Process.Start(info)??throw new InvalidOperationException(L.T("Não foi possível iniciar a leitura do print."));
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(TimeSpan.FromSeconds(35));
        var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();
        try{await process.WaitForExitAsync(timeout.Token);}catch{try{process.Kill(true);}catch{}throw;}
        string text=await output,detail=await error;
        if(process.ExitCode!=0)throw new InvalidOperationException(L.T("Leitura automática indisponível. Você pode preencher os dados manualmente.")+"\n"+detail.Split('\n').FirstOrDefault());
        return JsonSerializer.Deserialize<OcrDocument>(text)??throw new InvalidDataException("Invalid OCR output");
        }
        finally{if(normalized is not null&&File.Exists(normalized))File.Delete(normalized);}
    }
    // Anchor player rows by the score column; the next three columns are K, D, A. Ping is ignored.
    // This supports full screenshots and cropped broadcasts without treating other HUD numbers as player statistics.
    public static List<PlayerStatistic> Parse(OcrDocument doc)
    {
        var numbers=doc.Words.Where(w=>Regex.IsMatch(w.Text,"^[0-9oOgGIl]{1,5}$")).Select(w=>new OcrWord{Text=w.Text.Replace('o','0').Replace('O','0').Replace('g','9').Replace('G','9').Replace('I','1').Replace('l','1'),X=w.X,Y=w.Y,Width=w.Width,Height=w.Height}).ToList();
        var anchors=numbers.Where(w=>w.Text.Length>=3&&w.X>doc.Width*.35&&w.X<doc.Width*.85).ToList();
        var column=anchors.Select(w=>anchors.Where(x=>Math.Abs(x.X-w.X)<doc.Width*.018).OrderBy(x=>x.Y).ToList()).Where(x=>x.Count is >=7 and <=10).OrderByDescending(x=>x.Count).FirstOrDefault();
        // Reject ambiguous layouts; incomplete OCR is editable, never silently presented as confirmed.
        if(column is null)return [];
        float scoreX=column.Average(w=>w.X);
        var numericColumns=new List<List<OcrWord>>();
        foreach(var word in numbers.Where(w=>w.X>scoreX+column.Max(x=>x.Width)+doc.Width*.01).OrderBy(w=>w.X))
        {
            var group=numericColumns.FirstOrDefault(c=>Math.Abs(c.Average(x=>x.X)-word.X)<doc.Width*.012);
            if(group is null)numericColumns.Add([word]);else group.Add(word);
        }
        var killsColumn=numericColumns.Where(c=>c.Count>=4).OrderBy(c=>c.Average(x=>x.X)).FirstOrDefault();
        if(killsColumn is null)return [];
        float killsX=killsColumn.Average(x=>x.X),spacing=(killsX-scoreX)*.82f;
        float[] centers=[killsX,killsX+spacing,killsX+spacing*2];
        // A low-resolution score can be missed even when the player name and K column are readable.
        // Recover that row from both pieces of evidence, leaving any unreadable K/D/A value blank.
        var distances=column.Zip(column.Skip(1),(a,b)=>b.Y-a.Y).OrderBy(x=>x).ToArray();float rowGap=distances[distances.Length/2];
        float firstY=column[0].Y-rowGap*.3f,lastY=column[^1].Y+rowGap*1.4f;
        foreach(var word in killsColumn.Where(w=>w.Y>=firstY&&w.Y<=lastY))
        {
            if(column.Any(w=>Math.Abs(w.Y-word.Y)<word.Height))continue;
            if(!doc.Words.Any(w=>w.Text.Length>2&&w.X>scoreX-doc.Width*.32&&w.X+w.Width<scoreX-doc.Width*.015&&Math.Abs(w.Y+w.Height/2-(word.Y+word.Height/2))<word.Height))continue;
            column.Add(new(){X=scoreX,Y=word.Y,Width=column.Max(w=>w.Width),Height=word.Height});
        }
        column=column.OrderBy(w=>w.Y).ToList();if(column.Count!=10)return [];
        var result=new List<PlayerStatistic>();
        for(int i=0;i<column.Count;i++)
        {
            var anchor=column[i];float cy=anchor.Y+anchor.Height/2,height=anchor.Height;
            int? Value(int col){var w=numbers.FirstOrDefault(w=>Math.Abs(w.X-centers[col])<doc.Width*.012 && Math.Abs(w.Y+w.Height/2-cy)<Math.Max(5,height*.7));return w is null||w.Text.Length>2?null:int.Parse(w.Text);}
            var nameWords=doc.Words.Where(w=>w.Text.Length>1 && w.X>scoreX-doc.Width*.32 && w.X+w.Width<scoreX-doc.Width*.015 && Math.Abs(w.Y+w.Height/2-cy)<height).OrderBy(w=>w.X).ToList();
            string name=string.Join(" ",nameWords.Select(w=>w.Text));name=Regex.Replace(name,@"^[^\p{L}\p{N}]+","").Trim();
            result.Add(new(){Team=i<5?0:1,Name=name, Kills=Value(0),Deaths=Value(1),Assists=Value(2)});
        }
        return result;
    }
}
public sealed class OcrDocument {public int Width{get;set;}public int Height{get;set;}public List<OcrWord> Words{get;set;}=[];}
public sealed class OcrWord {public string Text{get;set;}="";public float X{get;set;}public float Y{get;set;}public float Width{get;set;}public float Height{get;set;}}

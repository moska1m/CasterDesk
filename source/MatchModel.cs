using System.Text.Json;
using System.Text.Json.Serialization;

namespace CronoLive;

public sealed class SiegeOperator { public string Id { get; set; } = ""; public string Name { get; set; } = ""; public string Role { get; set; } = ""; public string Icon { get; set; } = ""; public override string ToString() => Name; }
public sealed class SiegeMap { public string Id { get; set; } = ""; public string Name { get; set; } = ""; public string[] Sites { get; set; } = []; public string Image { get; set; } = ""; public override string ToString() => Name; }
public sealed class SiegeCatalog
{
    public List<SiegeOperator> Operators { get; set; } = []; public List<SiegeMap> Maps { get; set; } = [];
    public static string AssetRoot => Path.Combine(AppContext.BaseDirectory, "assets", "siege");
    public static SiegeCatalog Load() => JsonSerializer.Deserialize<SiegeCatalog>(File.ReadAllText(Path.Combine(AssetRoot, "catalog.json")), MatchStore.Options)!;
    public static string Asset(string file) => Path.Combine(AssetRoot, Path.GetFileName(file));
}
public sealed class RoundRecord { public int Winner { get; set; } public string Site { get; set; } = ""; }
public sealed class MapRecord
{
    public string MapId { get; set; } = "";
    public int Picker { get; set; }
    public int InitialAttack { get; set; } = -1;
    public int OtAttack { get; set; } = -1;
    public string[] Bans { get; set; } = new string[12];
    public List<RoundRecord> Rounds { get; set; } = [];
    public DateTimeOffset? ConfirmedAt { get; set; }
    [JsonIgnore] public bool Confirmed => ConfirmedAt.HasValue;
    [JsonIgnore] public bool HasData => Rounds.Count > 0 || Bans.Any(x => !string.IsNullOrEmpty(x));
    public int Attacker(int round) => round <= 6 ? InitialAttack : round <= 12 ? 1 - InitialAttack : (round % 2 == 1 ? OtAttack : 1 - OtAttack);
    public (int A, int B, int Winner, int EndAt) Score()
    {
        int a = 0, b = 0;
        for (int i = 0; i < Rounds.Count; i++)
        {
            if (Rounds[i].Winner == 0) a++; else b++;
            if ((i < 12 && Math.Max(a,b) == 7) || Math.Max(a,b) == 8) return (a,b,a>b?0:1,i+1);
        }
        return (a,b,-1,0);
    }
    public static int BanTeam(int slot) => slot % 6 is 0 or 1 or 4 ? 0 : 1;
    public static int BanThreshold(int slot) => (slot / 6) * 6 + (slot % 6 >= 4 ? 3 : 0);
    public bool BanStageReached(int slot)
    {
        var score=Score();int threshold=BanThreshold(slot);
        return InitialAttack>=0 && Rounds.Count>=threshold && (score.Winner<0 || score.EndAt>threshold);
    }
    public string BanRole(int slot) => BanTeam(slot) == (slot < 6 ? InitialAttack : 1-InitialAttack) ? "DEF" : "ATQ";
    public int ActiveBanHalf(int round) => Attacker(round) == InitialAttack ? 0 : 1;
}
public sealed class SiegeMatch
{
    public int Version { get; set; } = 1;
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime Created { get; set; } = DateTime.Now;
    public string TeamA { get; set; } = "Equipe A"; public string TeamB { get; set; } = "Equipe B";
    public string Format { get; set; } = "MD1";
    public int CoinWinner { get; set; } = -1; public int FirstBan { get; set; } = -1;
    public string[] Veto { get; set; } = new string[8];
    public List<MapRecord> Maps { get; set; } = [];
    public string Team(int index) => index == 0 ? TeamA : TeamB;
    public string Action(int step) => Format == "MD3" && step is 4 or 5 ? "PICK" : "BAN";
    public int Actor(int step) => (FirstBan + step) % 2;
    public void SyncMaps(SiegeCatalog catalog)
    {
        var choices = new List<(string Id,int Picker)>();
        if (Format == "MD3") foreach (int step in new[]{4,5}) if (!string.IsNullOrEmpty(Veto[step])) choices.Add((Veto[step],Actor(step)));
        if (Veto.All(x=>!string.IsNullOrEmpty(x))) choices.Add((catalog.Maps.Single(x=>!Veto.Contains(x.Id)).Id,Actor(7)));
        Maps = choices.Select(c=> Maps.FirstOrDefault(m=>m.MapId==c.Id && m.Picker==c.Picker) ?? new MapRecord {MapId=c.Id,Picker=c.Picker}).ToList();
    }
    [JsonIgnore] public bool HasConfirmed => Maps.Any(m=>m.Confirmed);
    [JsonIgnore] public bool Complete => Maps.Count(m=>m.Confirmed && m.Score().Winner==0) >= (Format=="MD1"?1:2) || Maps.Count(m=>m.Confirmed && m.Score().Winner==1) >= (Format=="MD1"?1:2);
    public override string ToString() => $"{Created:dd/MM HH:mm} • {TeamA} × {TeamB} • {(Complete?"Concluída":"Aberta")}";
}
public static class MatchStore
{
    public static readonly JsonSerializerOptions Options = new() { WriteIndented=true, PropertyNameCaseInsensitive=true };
    public static string Folder => Path.Combine(AppPaths.DataDirectory,"Partidas");
    public static void Atomic(string path, SiegeMatch match)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp=path+".tmp";
        File.WriteAllText(temp,JsonSerializer.Serialize(match,Options));
        if(File.Exists(path)) File.Copy(path,path+".bak",true);
        File.Move(temp,path,true);
    }
    public static void Save(SiegeMatch match) => Atomic(Path.Combine(Folder,match.Id+".json"),match);
    public static SiegeMatch Read(string path, SiegeCatalog catalog)
    {
        if(new FileInfo(path).Length>2_000_000) throw new InvalidDataException("Arquivo de partida muito grande.");
        var m=JsonSerializer.Deserialize<SiegeMatch>(File.ReadAllText(path),Options) ?? throw new InvalidDataException("Partida vazia.");
        Validate(m,catalog); return m;
    }
    public static void Validate(SiegeMatch m,SiegeCatalog c)
    {
        void Need(bool ok,string message) {if(!ok) throw new InvalidDataException(message);}
        Need(m.Version==1,"Versão de partida incompatível.");
        Need(Guid.TryParseExact(m.Id,"N",out _),"Identificador inválido.");
        Need(m.TeamA is not null && m.TeamB is not null && m.TeamA.Length<=12 && m.TeamB.Length<=12,"Nome de equipe deve ter até 12 caracteres.");
        Need(m.Format is "MD1" or "MD3","Formato inválido.");
        Need(m.CoinWinner is >=-1 and <=1 && m.FirstBan is >=-1 and <=1,"Coin flip inválido.");
        Need(m.Veto is {Length:8} && m.Maps is not null,"Estrutura de partida inválida.");
        var used=m.Veto!.Where(x=>!string.IsNullOrEmpty(x)).ToList();
        Need(used.Distinct().Count()==used.Count && used.All(x=>c.Maps.Any(y=>y.Id==x)),"Mapas repetidos ou desconhecidos.");
        Need(used.Count==0 || m.FirstBan>=0 && m.CoinWinner>=0,"Falta definir coin flip.");
        Need(m.Veto.Take(used.Count).All(x=>!string.IsNullOrEmpty(x)),"Veto fora de sequência.");
        var expected=JsonSerializer.Deserialize<SiegeMatch>(JsonSerializer.Serialize(m,Options),Options)!; expected.SyncMaps(c);
        Need(m.Maps!.Count==expected.Maps.Count && m.Maps.Select(x=>(x.MapId,x.Picker)).SequenceEqual(expected.Maps.Select(x=>(x.MapId,x.Picker))),"Mapas não correspondem ao veto.");
        foreach(var map in m.Maps)
        {
            Need(map.InitialAttack is >=-1 and <=1 && map.OtAttack is >=-1 and <=1,"Lados inválidos.");
            Need(map.Rounds is not null && map.Rounds.Count<=15 && map.Bans is {Length:12},"Rounds ou bans inválidos.");
            Need(!map.HasData || map.InitialAttack>=0,"Dados sem lado inicial.");
            Need(map.Rounds!.Count<=12 || map.OtAttack>=0,"Overtime sem lado definido.");
            Need(map.Rounds.All(r=>r is not null && r.Winner is 0 or 1 && (r.Site=="" || c.Maps.Single(x=>x.Id==map.MapId).Sites.Contains(r.Site))),"Round inválido.");
            var score=map.Score(); Need(score.EndAt==0 || score.EndAt==map.Rounds.Count,"Há rounds após o encerramento.");
            Need(!map.Confirmed || score.Winner>=0,"Confirmação sem vencedor definido.");
            for(int i=0;i<12;i++) if(!string.IsNullOrEmpty(map.Bans[i]))
            {
                Need(map.Rounds.Count>=MapRecord.BanThreshold(i),"Ban de etapa futura.");
                Need(c.Operators.Any(o=>o.Id==map.Bans[i] && o.Role==map.BanRole(i)),"Operador incompatível com o lado.");
            }
            foreach(int half in new[]{0,1}) {var bans=map.Bans.Skip(half*6).Take(6).Where(x=>!string.IsNullOrEmpty(x)).ToList(); Need(bans.Distinct().Count()==bans.Count,"Ban duplicado na mesma metade.");}
        }
    }
}

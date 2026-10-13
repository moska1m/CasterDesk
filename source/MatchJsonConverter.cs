using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CronoLive;

// The file vocabulary is independent of the reader's language. The domain keeps
// catalogue values so existing round rules and translated selectors remain compatible.
public sealed class MatchJsonConverter : JsonConverter<SiegeMatch>
{
    sealed class SiteDefinition
    {
        public string MapId{get;set;}="";
        public string SiteId{get;set;}="";
        public string English{get;set;}="";
        public string Legacy{get;set;}="";
    }
    static readonly Lazy<List<SiteDefinition>> sites=new(()=>
    {
        using var stream=typeof(MatchJsonConverter).Assembly.GetManifestResourceStream("CasterDesk.match-sites.json")!;
        return JsonSerializer.Deserialize<List<SiteDefinition>>(stream)!;
    });
    static readonly Lazy<SiegeCatalog> catalog=new(SiegeCatalog.Load);
    static IEnumerable<string> Aliases(string value)
    {
        yield return value;
        if(L.Entries.TryGetValue(value,out var translated))foreach(string text in translated)yield return text;
    }
    static bool Same(string a,string b)=>string.Equals(a.Trim(),b.Trim(),StringComparison.OrdinalIgnoreCase);
    static string MapId(string value)
    {
        var map=catalog.Value.Maps.FirstOrDefault(m=>Same(m.Id,value)||Aliases(m.Name).Any(a=>Same(a,value)));
        return map?.Id??value; // Validation rejects unknown maps; no fuzzy guessing.
    }
    static JsonNode? Field(JsonObject value,string name)=>value.FirstOrDefault(p=>string.Equals(p.Key,name,StringComparison.OrdinalIgnoreCase)).Value;
    static void Set(JsonObject value,string name,JsonNode? content)
    {
        foreach(string key in value.Select(p=>p.Key).Where(k=>string.Equals(k,name,StringComparison.OrdinalIgnoreCase)).ToArray())value.Remove(key);
        value[name]=content;
    }
    static string Text(JsonNode? node)=>node?.GetValue<string>()??"";
    static SiteDefinition? Site(string map,string text,string id="")
    {
        if(string.IsNullOrEmpty(text)&&string.IsNullOrEmpty(id))return null;
        var found=sites.Value.FirstOrDefault(s=>s.MapId==map&&(!string.IsNullOrEmpty(id)?s.SiteId==id:
            Same(s.SiteId,text)||Same(s.English,text)||Aliases(s.Legacy).Any(a=>Same(a,text))||
            (map=="bank"&&Aliases(s.SiteId=="tellers-office-archives"?"Escritório / Arquivos":s.SiteId=="lockers-cctv-room"?"Armários / Vigilância":"").Any(a=>Same(a,text)))));
        return found??throw new JsonException(L.T("Round inválido."));
    }
    static bool DefaultName(string value,int team)=>string.IsNullOrWhiteSpace(value)||
        Aliases(team==0?"Equipe A":"Equipe B").Concat(new[]{team==0?"Equipa A":"Equipa B"}).Any(a=>Same(a,value));

    public override SiegeMatch Read(ref Utf8JsonReader reader,Type typeToConvert,JsonSerializerOptions options)
    {
        var node=JsonNode.Parse(ref reader) as JsonObject??throw new JsonException(L.T("Partida vazia."));
        int version=Field(node,"Version")?.GetValue<int>()??1;
        if(version is not (1 or 2))throw new JsonException(L.T("Versão de partida incompatível."));
        string format=Text(Field(node,"Format"));if(format is "BO1" or "BO3" or "BO5")Set(node,"Format",JsonValue.Create(format.Replace("BO","MD")));
        if(Field(node,"Veto") is JsonArray veto)for(int i=0;i<veto.Count;i++)if(!string.IsNullOrWhiteSpace(Text(veto[i])))veto[i]=MapId(Text(veto[i]));
        if(Field(node,"Maps") is JsonArray maps)foreach(var value in maps)
        {
            if(value is not JsonObject map)throw new JsonException(L.T("Estrutura de partida inválida."));
            string id=MapId(Text(Field(map,"MapId")));Set(map,"MapId",JsonValue.Create(id));
            if(Field(map,"Rounds") is JsonArray rounds)foreach(var item in rounds)
            {
                if(item is not JsonObject round)throw new JsonException(L.T("Round inválido."));
                var site=Site(id,Text(Field(round,"Site")),Text(Field(round,"SiteId")));
                Set(round,"Site",JsonValue.Create(site?.Legacy??""));
            }
        }
        var data=node.Deserialize<Payload>(options)??throw new JsonException(L.T("Partida vazia."));
        return new SiegeMatch{Version=2,Id=data.Id,Created=data.Created,TeamA=data.TeamA,TeamB=data.TeamB,
            TeamAIsDefault=data.TeamAIsDefault??DefaultName(data.TeamA,0),TeamBIsDefault=data.TeamBIsDefault??DefaultName(data.TeamB,1),
            Format=data.Format,CoinWinner=data.CoinWinner,FirstBan=data.FirstBan,Veto=data.Veto,Maps=data.Maps};
    }
    public override void Write(Utf8JsonWriter writer,SiegeMatch value,JsonSerializerOptions options)
    {
        var data=new Payload{Version=value.Version==1?2:value.Version,Id=value.Id,Created=value.Created,
            TeamA=value.StoredTeamA,TeamB=value.StoredTeamB,TeamAIsDefault=value.TeamAIsDefault,TeamBIsDefault=value.TeamBIsDefault,
            Format=value.Format.Replace("MD","BO"),CoinWinner=value.CoinWinner,FirstBan=value.FirstBan,Veto=value.Veto,Maps=value.Maps};
        var node=JsonSerializer.SerializeToNode(data,options)!.AsObject();
        if(node["Maps"] is JsonArray maps)foreach(var valueMap in maps)
        {
            var map=valueMap!.AsObject();string id=Text(map["MapId"]);
            var definition=catalog.Value.Maps.FirstOrDefault(m=>m.Id==id);if(definition!=null)map["MapName"]=L.Entries.TryGetValue(definition.Name,out var names)?names[0]:definition.Name;
            if(map["Rounds"] is JsonArray rounds)foreach(var item in rounds)
            {
                var round=item!.AsObject();var site=Site(id,Text(round["Site"]));
                round["Site"]=site?.English??"";round["SiteId"]=site?.SiteId??"";
            }
        }
        node.WriteTo(writer,options);
    }
    sealed class Payload
    {
        public int Version{get;set;}=1;
        public string Id{get;set;}=Guid.NewGuid().ToString("N");
        public DateTime Created{get;set;}=DateTime.Now;
        public string TeamA{get;set;}="Team A";
        public string TeamB{get;set;}="Team B";
        public bool? TeamAIsDefault{get;set;}
        public bool? TeamBIsDefault{get;set;}
        public string Format{get;set;}="MD1";
        public int CoinWinner{get;set;}=-1;
        public int FirstBan{get;set;}=-1;
        public string[] Veto{get;set;}=new string[8];
        public List<MapRecord> Maps{get;set;}=[];
    }
}

using System.Text.Json;
namespace CronoLive;

// Shared commands do not depend on a window, control or dispatcher.
public sealed class MatchSession
{
    public SiegeCatalog Catalog{get;}
    public List<SiegeMatch> Matches{get;}=[];
    public List<string> Warnings{get;}=[];
    public SiegeMatch Current{get;private set;}=null!;
    public MapRecord? Map{get;private set;}
    record Operation(string Before,string After,string? MapId);
    readonly Dictionary<string,Stack<Operation>> undo=new(),redo=new();
    Stack<Operation> History(Dictionary<string,Stack<Operation>> source) {if(!source.TryGetValue(Current.Id,out var stack))source[Current.Id]=stack=new();return stack;}
    bool RestoreAllowed(string json)
    {
        var restored=JsonSerializer.Deserialize<SiegeMatch>(json,MatchStore.Options)!;
        if(Current.HasConfirmed&&(restored.StoredTeamA!=Current.StoredTeamA||restored.StoredTeamB!=Current.StoredTeamB||restored.Format!=Current.Format))return false;
        return Current.Maps.Where(m=>m.Confirmed).All(m=>restored.Maps.Any(old=>old.MapId==m.MapId&&JsonSerializer.Serialize(old,MatchStore.Options)==JsonSerializer.Serialize(m,MatchStore.Options)));
    }
    public bool CanUndo=>History(undo).TryPeek(out var item)&&JsonSerializer.Serialize(Current,MatchStore.Options)==item.After&&RestoreAllowed(item.Before);
    public bool CanRedo=>History(redo).TryPeek(out var item)&&JsonSerializer.Serialize(Current,MatchStore.Options)==item.Before&&RestoreAllowed(item.After);
    public event Action? Changed;
    public event Action<SiegeMatch,MapRecord>? MapConfirmed;
    public MatchSession(SiegeCatalog? catalog=null)
    {
        Catalog=catalog??SiegeCatalog.Load();Directory.CreateDirectory(MatchStore.Folder);
        foreach(string file in Directory.GetFiles(MatchStore.Folder,"*.json").OrderByDescending(File.GetLastWriteTime))
        {try{Matches.Add(MatchStore.Read(file,Catalog));}catch(Exception ex){try{Matches.Add(MatchStore.Read(file+".bak",Catalog));Warnings.Add(L.T("Partida recuperada da cópia anterior: ")+Path.GetFileName(file));}catch{Warnings.Add(Path.GetFileName(file)+": "+ex.Message);}}}
        if(Matches.Count==0)New();else Select(Matches[0].Id);
    }
    public void Select(string id){Current=Matches.Single(m=>m.Id==id);Map=Current.Maps.FirstOrDefault(m=>!m.Confirmed)??Current.Maps.FirstOrDefault();Changed?.Invoke();}
    public void SelectMap(string id){Map=Current.Maps.Single(m=>m.MapId==id);Changed?.Invoke();}
    public void New(){var next=new SiegeMatch();MatchStore.Save(next);Matches.Insert(0,next);Select(next.Id);}
    void Change(Action action,string? review=null)
    {
        var target=Current;string previous=JsonSerializer.Serialize(target,MatchStore.Options);string? mapId=Map?.MapId;
        try
        {
            action();MatchStore.Validate(target,Catalog);
            if(review==null)MatchStore.Save(target);
            else{using var batch=new FileBatch();MatchStore.Stage(batch,target);batch.Text(Path.Combine(MatchFiles.Ensure(target),"Revisoes",DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")+".json"),JsonSerializer.Serialize(new{At=DateTimeOffset.Now,Reason=review,Before=JsonDocument.Parse(previous).RootElement,After=target},MatchStore.Options));batch.Commit();}
        }
        catch{var old=JsonSerializer.Deserialize<SiegeMatch>(previous)!;Matches[Matches.IndexOf(target)]=old;Current=old;Map=old.Maps.FirstOrDefault(m=>m.MapId==mapId);Changed?.Invoke();throw;}
        {var after=JsonSerializer.Serialize(target,MatchStore.Options);if(previous!=after){History(undo).Push(new(previous,after,mapId));History(redo).Clear();}}
        Map=target.Maps.FirstOrDefault(m=>m.MapId==mapId)??target.Maps.FirstOrDefault(m=>!m.Confirmed)??target.Maps.FirstOrDefault();Changed?.Invoke();
    }
    void Editable(){if(Map==null)throw new InvalidOperationException(L.T("Escolha um mapa primeiro."));if(Map.Confirmed||Current.Complete)throw new InvalidOperationException(L.T("Mapa confirmado. Abra uma revisão para corrigir os dados."));}
    public void Names(string a,string b){if(Current.HasConfirmed)throw new InvalidOperationException(L.T("Mapa confirmado. Abra uma revisão para corrigir os dados."));Change(()=>{if(a!=Current.TeamA)Current.TeamA=a;if(b!=Current.TeamB)Current.TeamB=b;});}
    public void Setup(string format,int coin,int first,bool reset)
    {
        if(Current.HasConfirmed)throw new InvalidOperationException(L.T("Mapa confirmado. Abra uma revisão para corrigir os dados."));
        if(Current.DirectMaps && Current.Maps.Any(m=>m.HasData) && (format!=Current.Format||first!=Current.FirstBan) && !reset)throw new InvalidOperationException(L.T("Esta troca muda um mapa com bans ou rounds registrados."));
        if((format!=Current.Format||first!=Current.FirstBan)&&Current.Veto.Any(v=>!string.IsNullOrEmpty(v))&&!reset)throw new InvalidOperationException(L.T("Esta mudança exige reiniciar o veto."));
        Change(()=>{if(format!=Current.Format||first!=Current.FirstBan){Current.DirectMaps=false;Current.Veto=new string[8];Current.Maps=[];}Current.Format=format;Current.CoinWinner=coin;Current.FirstBan=first;});
    }
    public void Veto(int step,string id,bool replace=false)
    {
        if(Current.Complete||Current.CoinWinner<0||Current.FirstBan<0)throw new InvalidOperationException(L.T("Defina coin flip e primeiro ban antes do veto."));
        int next=Array.FindIndex(Current.Veto,string.IsNullOrEmpty);if(step<0||step>=8||(next>=0&&step>next)||Current.Veto.Where((_,i)=>i!=step).Contains(id))throw new InvalidOperationException(L.T("Seleção inválida. Confira o campo."));
        Change(()=>{var old=Current.Maps.ToList();Current.Veto[step]=id;Current.SyncMaps(Catalog);if(old.Any(m=>!Current.Maps.Contains(m)&&(m.Confirmed||m.HasData&&!replace)))throw new InvalidOperationException(L.T("Esta troca muda um mapa com bans ou rounds registrados."));});
    }
    public void Side(string id,bool overtime,int attacker,bool resetBans=false)
    {
        var map=Current.Maps.Single(m=>m.MapId==id);if(map.Confirmed||Current.Complete)throw new InvalidOperationException(L.T("Mapa confirmado. Abra uma revisão para corrigir os dados."));
        if(map.HasData&&!overtime&&map.InitialAttack!=attacker&&!resetBans)throw new InvalidOperationException(L.T("Alterar lado inicial limpa os bans. Confirme a alteração."));
        Change(()=>{if(overtime)map.OtAttack=attacker;else if(map.InitialAttack!=attacker){map.InitialAttack=attacker;map.Bans=new string[12];}for(int i=0;i<map.Rounds.Count;i++)if(!RoundEnding.Allowed(map.Rounds[i].Ending,map.Rounds[i].Winner,map.Attacker(i+1)))map.Rounds[i].Ending="";});
    }
    public int[] PendingBans()
    {
        if(Map==null||Map.InitialAttack<0)return [];
        int half=Map.ActiveBanHalf(Math.Min(15,Map.Rounds.Count+1));var pending=Map.PendingBans(half);return Map.BanOrder(half).Where(pending.Contains).ToArray();
    }
    public void Ban(int slot,string id)
    {
        if(Map==null||!Map.BanStageReached(slot)||Current.Complete&&Map.Rounds.Count==0)throw new InvalidOperationException(L.T("Ban de etapa futura."));
        if(!Catalog.Operators.Any(o=>o.Id==id&&o.Role==Map.BanRole(slot))||Enumerable.Range(slot/6*6,6).Any(i=>i!=slot&&Map.Bans[i]==id))throw new InvalidOperationException(L.T("Operador incompatível com o lado."));
        Change(()=>Map!.Bans[slot]=id);
    }
    public void Round(string site,int winner,string ending,int index=-1,bool trim=false)
    {
        Editable();var map=Map!;int row=index<0?map.Rounds.Count:index;
        if(row<0||row>map.Rounds.Count||row==map.Rounds.Count&&map.Score().Winner>=0)throw new InvalidOperationException(L.T("Vencedor já definido. Confira os rounds."));
        if(map.SiteBlocked(site,row+1)&&row==map.Rounds.Count)throw new InvalidOperationException(L.T("Bombsite bloqueado por vitória da defesa. Escolha outro local."));
        Change(()=>{var data=new RoundRecord{Winner=winner,Site=site,Ending=ending};if(row==map.Rounds.Count)map.Rounds.Add(data);else map.Rounds[row]=data;var score=map.Score();if(score.EndAt>0&&score.EndAt<map.Rounds.Count){if(!trim)throw new InvalidOperationException(L.T("Esta correção remove rounds posteriores. Confirme a alteração."));map.Rounds.RemoveRange(score.EndAt,map.Rounds.Count-score.EndAt);for(int i=0;i<12;i++)if(MapRecord.BanThreshold(i)>map.Rounds.Count)map.Bans[i]="";}});
    }
    public void ConfigureSpec(string format,string[] ids)
    {
        int count=format=="MD5"?5:format=="MD3"?3:format=="MD1"?1:0;
        if(count==0||ids.Length!=count||ids.Distinct().Count()!=count||ids.Any(id=>!Catalog.Maps.Any(m=>m.Id==id)))throw new InvalidOperationException(L.T("Mapas repetidos ou desconhecidos."));
        if(Current.HasConfirmed||Current.Maps.Any(m=>m.HasData)&&(format!=Current.Format||!Current.Maps.Select(m=>m.MapId).SequenceEqual(ids)))throw new InvalidOperationException(L.T("Esta troca muda um mapa com bans ou rounds registrados."));
        Change(()=>{Current.Format=format;Current.DirectMaps=true;Current.Veto=new string[8];Current.Maps=ids.Select(id=>Current.Maps.FirstOrDefault(m=>m.MapId==id)??new MapRecord{MapId=id,Picker=0}).ToList();});
    }
    public void SpecSide(string id,bool overtime,int attacker)
    {
        var map=Current.Maps.Single(m=>m.MapId==id);
        if(map.Confirmed||Current.Complete)throw new InvalidOperationException(L.T("Mapa confirmado. Abra uma revisão para corrigir os dados."));
        Change(()=>{if(overtime)map.OtAttack=attacker;else map.InitialAttack=attacker;});
    }
    public void CorrectRound(int index,string site,int winner)
    {
        Editable();var map=Map!;if(index<0||index>=map.Rounds.Count)throw new InvalidOperationException(L.T("Round inválido."));
        // The operator must explicitly correct an incompatible ending; never erase it silently.
        var ending=map.Rounds[index].Ending;
        Round(site,winner,ending,index);
    }
    public void DeleteRound(int index)
    {
        Editable();if(index<0||index>=Map!.Rounds.Count)throw new InvalidOperationException(L.T("Round inválido."));
        // Validation can refuse future bans or endings made invalid by renumbering. Rollback keeps every record.
        Change(()=>Map!.Rounds.RemoveAt(index),L.T("Excluir round"));
    }
    public void Undo()=>RestoreOperation(undo,redo,true);
    public void Redo()=>RestoreOperation(redo,undo,false);
    void RestoreOperation(Dictionary<string,Stack<Operation>> source,Dictionary<string,Stack<Operation>> destination,bool backwards)
    {
        var stack=History(source);if(stack.Count==0)return;var item=stack.Peek();
        if(!RestoreAllowed(backwards?item.Before:item.After))throw new InvalidOperationException(L.T("Mapa confirmado. Abra uma revisão para corrigir os dados."));
        if(JsonSerializer.Serialize(Current,MatchStore.Options)!=(backwards?item.After:item.Before))throw new InvalidOperationException(L.T("A partida mudou fora deste histórico. Confira os dados antes de corrigir."));
        var restored=JsonSerializer.Deserialize<SiegeMatch>(backwards?item.Before:item.After,MatchStore.Options)!;MatchStore.Validate(restored,Catalog);MatchStore.Save(restored);
        int at=Matches.IndexOf(Current);Matches[at]=restored;Current=restored;Map=restored.Maps.FirstOrDefault(m=>m.MapId==item.MapId)??restored.Maps.FirstOrDefault();stack.Pop();History(destination).Push(item);Changed?.Invoke();
    }
    public void Confirm(){Editable();if(Map!.Score().Winner<0)throw new InvalidOperationException(L.T("Vencedor ainda não definido."));var id=Map.MapId;Change(()=>Map!.ConfirmedAt=DateTimeOffset.Now);MapConfirmed?.Invoke(Current,Current.Maps.Single(m=>m.MapId==id));}
    public void Review(string reason){if(Map==null||!Map.Confirmed||string.IsNullOrWhiteSpace(reason))throw new InvalidOperationException(L.T("Informe o motivo da revisão."));Change(()=>Map!.ConfirmedAt=null,reason);}
    public bool ApplyArtworkVeto(string id,ImageBrand brand)
    {
        var target=Matches.Single(m=>m.Id==id);if(target.Maps.Any(m=>m.HasData||m.Confirmed))throw new InvalidOperationException(L.T("A partida já tem bans ou rounds registrados. Mantenha esta correção somente na arte."));var copy=MatchFiles.ArtworkMatch(target,Catalog,brand);MatchStore.Validate(copy,Catalog);MatchStore.Save(copy);target.Veto=copy.Veto;target.Maps=copy.Maps;if(Current.Id==id){Current=target;Map=target.Maps.FirstOrDefault();}Changed?.Invoke();return true;
    }
    public void Import(string file){var match=MatchStore.Read(file,Catalog);if(MatchStore.IsDuplicate(Matches,match,MatchStore.Folder))throw new InvalidOperationException(L.T("Esta partida já existe no programa."));MatchStore.Save(match);Matches.Insert(0,match);Select(match.Id);}
    public void AttachImported(SiegeMatch match){Matches.Insert(0,match);Select(match.Id);}
    public string Summary(string? mapId=null,bool specNames=false)
    {
        var lines=new List<string>();string Team(int n)=>specNames && (n==0?Current.TeamAIsDefault:Current.TeamBIsDefault)?L.T(n==0?"Equipe Azul":"Equipe Laranja"):Current.Team(n);lines.Add(Team(0)+" × "+Team(1));
        foreach(var m in Current.Maps.Where(m=>mapId==null||m.MapId==mapId)){
            lines.Add(Catalog.Maps.Single(c=>c.Id==m.MapId).DisplayName);
            lines.Add(L.T("Ataque inicial")+": "+(m.InitialAttack<0?L.T("Não definido"):Team(m.InitialAttack)));
            lines.Add(L.T("Defesa inicial")+": "+(m.InitialAttack<0?L.T("Não definido"):Team(1-m.InitialAttack)));
            lines.Add(L.T("Ataque na prorrogação")+": "+(m.OtAttack<0?L.T("Não definido"):Team(m.OtAttack)));
            for(int half=0;half<2;half++)foreach(int slot in m.BanOrder(half).Where(i=>!string.IsNullOrEmpty(m.Bans[i])))lines.Add($"{L.T("Round")} {half*6+1}–{half*6+6} · {Team(MapRecord.BanTeam(slot))} · {L.Role(m.BanRole(slot))} · {Catalog.Operators.Single(o=>o.Id==m.Bans[slot]).Name}");
            for(int i=0;i<m.Rounds.Count;i++)lines.Add($"{L.T("Round")} {i+1:00} · {BombsiteNames.Display(m.MapId,m.Rounds[i].Site)} · {Team(m.Rounds[i].Winner)}");
        }lines.Add(L.T("Após configurar, confira o mapa."));return string.Join(Environment.NewLine,lines);
    }
}

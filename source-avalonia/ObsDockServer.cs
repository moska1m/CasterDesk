using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CasterDesk.Avalonia;

public sealed class ObsDockSettings
{
    public bool Enabled { get; set; }
    public bool PluginConfigured { get; set; }
    public bool RouteBansToOBS { get; set; }
    public int Port { get; set; } = 43827;
    public string Key { get; set; } = "";
    internal bool ConfigurePlugin(bool installed)
    {
        if(!installed||PluginConfigured)return false;
        Enabled=true;PluginConfigured=true;return true;
    }
}

internal sealed class ObsDockServer : IAsyncDisposable
{
    readonly ChampionshipView championship;
    readonly TextsView texts;
    readonly Func<bool> canControl;
    readonly ObsDockSettings settings;
    WebApplication? app;
    long revision;
    bool stopping;
    bool subscribed = true;
    long lastRead;
    internal bool HasClient => Running && lastRead > 0 && Environment.TickCount64 - lastRead < 5000;
    internal bool Running => app != null && !stopping;
    internal string BaseUrl => $"http://127.0.0.1:{settings.Port}";
    internal string PanelUrl => BaseUrl + "/#key=" + settings.Key;
    internal ObsDockServer(ChampionshipView championship, TextsView texts, Func<bool> canControl, ObsDockSettings settings)
    {
        this.championship=championship; this.texts=texts; this.canControl=canControl; this.settings=settings;
        if(settings.Port is <1024 or >65535)settings.Port=43827;
        if(string.IsNullOrWhiteSpace(settings.Key)||settings.Key.Length!=64||!settings.Key.All(Uri.IsHexDigit))settings.Key=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        championship.Session.Changed+=Changed;
    }
    void Changed() => revision++;
    internal async Task StartAsync()
    {
        if(app!=null)return;
        stopping=false;
        lastRead=0;
        if(!subscribed){championship.Session.Changed+=Changed;subscribed=true;}
        var builder=WebApplication.CreateSlimBuilder(); builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options=>{ options.Listen(IPAddress.Loopback,settings.Port); options.Limits.MaxRequestBodySize=8192; });
        var server=builder.Build();
        server.Use(async (context,next)=>{
            context.Response.Headers.CacheControl="no-store";
            context.Response.Headers["X-Content-Type-Options"]="nosniff";
            context.Response.Headers["Referrer-Policy"]="no-referrer";
            context.Response.Headers["Content-Security-Policy"]="default-src 'self'; img-src 'self' blob:; style-src 'self' 'unsafe-inline'; script-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'";
            string origin=context.Request.Headers.Origin.ToString();
            if(context.Request.Host.Value!=$"127.0.0.1:{settings.Port}" || (origin!="" && origin!=BaseUrl)) { context.Response.StatusCode=403; return; }
            bool protectedPath=context.Request.Path.StartsWithSegments("/api")||context.Request.Path.StartsWithSegments("/icons");
            if(protectedPath && !KeyMatches(context.Request.Headers["X-CasterDesk-Key"].ToString())) { context.Response.StatusCode=401; return; }
            if(stopping){context.Response.StatusCode=503;return;}
            await next(context);
        });
        server.MapGet("/",()=>Results.Content(File.ReadAllText(Asset("dock.html")),"text/html; charset=utf-8"));
        server.MapGet("/dock.js",()=>Results.Content(File.ReadAllText(Asset("dock.js")),"text/javascript; charset=utf-8"));
        server.MapGet("/brand.svg",()=>Results.File(Path.Combine(AppContext.BaseDirectory,"assets","branding","casterdesk-horizontal-white.svg"),"image/svg+xml"));
        server.MapGet("/icons/{id}",(string id)=>{
            if(id is "__def" or "__atq")return Results.File(Path.Combine(AppContext.BaseDirectory,"assets","studio",id=="__def"?"Icon_Defender.png":"Icon_Attacker.png"),"image/png");
            var op=championship.Session.Catalog.Operators.FirstOrDefault(o=>o.Id==id);
            return op==null?Results.NotFound():Results.File(SiegeCatalog.Asset(op.Icon),"image/png");
        });
        server.MapGet("/api/state",async()=>await Dispatcher.UIThread.InvokeAsync(()=>{lastRead=Environment.TickCount64;return Results.Json(State());}));
        server.MapPost("/api/command",async (HttpContext context)=>{
            if(!context.Request.HasJsonContentType())return Results.StatusCode(415);
            DockCommand? command;
            try { command=await context.Request.ReadFromJsonAsync<DockCommand>(context.RequestAborted); }
            catch(JsonException){return Results.BadRequest(new{error="Invalid JSON"});}
            if(command==null)return Results.BadRequest();
            return await Dispatcher.UIThread.InvokeAsync(()=>Execute(command));
        });
        try { await server.StartAsync(); app=server; }
        catch { await server.DisposeAsync(); throw; }
    }
    static string Asset(string name)=>Path.Combine(AppContext.BaseDirectory,"assets","obs",name);
    bool KeyMatches(string key)=>key.Length==settings.Key.Length && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(key),Encoding.UTF8.GetBytes(settings.Key));
    object State()
    {
        var session=championship.Session; var map=session.Map; var match=session.Current;
        int half=map?.ActiveBanHalf(Math.Min(15,map.Rounds.Count+1))??0;
        var pending=championship.BansReady && map?.Confirmed==false ? session.PendingBans() : [];
        string role=pending.Length>0?map!.BanRole(pending[0]):"";
        int number=map?.Rounds.Count+1??1, attacker=map?.InitialAttack>=0?map.Attacker(number):-1;
        bool editable=map!=null&&!map.Confirmed&&!match.Complete;
        return new {
            version=BuildInfo.Version,revision,language=L.Current,controllable=canControl()&&championship.DockReady&&!stopping,
            timer=new{seconds=texts.Remaining,running=texts.Running,options=texts.TimerOptions},
            match=new{id=match.Id,teamA=match.TeamA,teamB=match.TeamB,mapId=map?.MapId,map=map==null?"":session.Catalog.Maps.Single(m=>m.Id==map.MapId).DisplayName,confirmed=map?.Confirmed??false,
                maps=match.Maps.Select(m=>new{id=m.MapId,label=session.Catalog.Maps.Single(c=>c.Id==m.MapId).DisplayName}).ToArray()},
            pending=pending.Length>0?(int?)pending[0]:null,role,
            rounds=new{number,attacker,editable,ready=editable&&championship.BansReady&&pending.Length==0&&map!.Score().Winner<0&&(number<=12||map.OtAttack>=0),
                score=map==null?"":$"{map.Score().A} × {map.Score().B}",
                sites=map==null?[]:session.Catalog.Maps.Single(m=>m.Id==map.MapId).Sites.Select(s=>new{id=s,label=BombsiteNames.Display(map.MapId,s),blocked=map.SiteBlocked(s,number)}).ToArray(),
                endings=RoundEnding.Codes.Skip(1).Select(c=>new{id=c,label=RoundEnding.Label(c),attack=RoundEnding.Allowed(c,0,0),defense=RoundEnding.Allowed(c,1,0)}).ToArray(),
                history=map==null?[]:map.Rounds.Select((r,i)=>new{index=i,site=r.Site,winner=r.Winner,ending=r.Ending,attacker=map.Attacker(i+1)}).ToArray()},
            bans=map==null?[]:map.BanOrder(0).Concat(map.BanOrder(1)).Select(slot=>new{slot,team=MapRecord.BanTeam(slot),role=map.BanRole(slot),id=map.Bans[slot],name=session.Catalog.Operators.FirstOrDefault(o=>o.Id==map.Bans[slot])?.Name??(MapRecord.IsNoBan(map.Bans[slot])?L.T("Sem banimento"):""),unlocked=map.BanStageReached(slot)}).ToArray(),
            remakes=match.Maps.Select(m=>new {
                id=m.MapId,label=session.Catalog.Maps.Single(c=>c.Id==m.MapId).DisplayName,initialAttack=m.InitialAttack,otAttack=m.OtAttack,confirmed=m.Confirmed,
                rounds=m.Rounds.Select((r,i)=>new{index=i,site=BombsiteNames.Display(m.MapId,r.Site),winner=r.Winner}).ToArray(),
                bans=m.BanOrder(0).Concat(m.BanOrder(1)).Where(slot=>!string.IsNullOrEmpty(m.Bans[slot])).Select(slot=>new{slot,half=slot/6,team=MapRecord.BanTeam(slot),role=m.BanRole(slot),id=m.Bans[slot],name=session.Catalog.Operators.FirstOrDefault(o=>o.Id==m.Bans[slot])?.Name??(MapRecord.IsNoBan(m.Bans[slot])?L.T("Sem banimento"):m.Bans[slot])}).ToArray()
            }).ToArray(),
            operators=role==""?[]:OperatorOrder.Sort(session.Catalog.Operators,role).Select(op=>new{id=op.Id,name=op.Name,enabled=!map!.Bans.Skip(half*6).Take(6).Contains(op.Id)}).Concat(new[]{new{id=MapRecord.NoBan(role),name=L.T("Sem banimento"),enabled=true}}).ToArray(),
            labels=new{chooseRemake=L.T("Escolha o mapa para o remake"),undefined=L.T("Não definido"),initialAttack=L.T("Ataque inicial"),initialDefense=L.T("Defesa inicial"),otAttack=L.T("Ataque na prorrogação"),checkMap=L.T("Após configurar, confira o mapa."),partida=L.T("Partida"),history=L.T("Histórico de rounds"),round=L.T("Round"),site=L.T("Bomb site"),winner=L.T("Vencedor"),ending=L.T("Finalização"),reset=L.T("Resetar tempo"),applyTimer=L.T("Aplicar configurações"),copy=L.T("Copiar caminho"),message=L.T("Mensagem final:"),rounds=L.T("Round a round"),newRound=L.T("Novo round"),confirmRound=L.T("Confirmar round"),editRound=L.T("Corrigir round"),roundWait=L.T(map?.Score().Winner>=0?"Vencedor já definido. Confira os rounds.":map?.Rounds.Count>=12&&map.OtAttack<0?"Overtime sem lado definido.":"Conclua os bans de operadores para registrar o próximo round."),timer=L.T("CRONÔMETRO"),bans=L.T("Bans de operadores"),start=L.T("Iniciar / Retomar"),pause=L.T("Pausar"),set=L.T("Aplicar tempo"),select=L.T("Selecionar…"),blocked=L.T("Bloqueado"),wait=L.T(!championship.BansReady?"Conclua o veto e defina os lados na partida.":"Nenhum ban pendente."),connected=L.T("Conectado"),disconnected=L.T("Desconectado"),duration=L.T("Tempo inicial:"),search=L.T("Filtrar operadores"),invalidTime=L.T("Use mm:ss ou hh:mm:ss."),busy=L.T("Conclua a edição aberta no CasterDesk para usar os controles."),team=L.T("Equipe"),def=L.Role("DEF"),atq=L.Role("ATQ")}
        };
    }
    internal sealed record DockCommand(string Action,double Seconds=0,string MatchId="",string MapId="",long Revision=-1,int Slot=-1,string OperatorId="",string Site="",int Winner=-1,string Ending="",int RoundIndex=-1,string TargetMapId="",string Format="mm:ss",string Message="",double[]? Presets=null);
    IResult Execute(DockCommand command)
    {
        lastRead=Environment.TickCount64;
        if(stopping||!canControl()||!championship.DockReady)return Results.Conflict(new{error=L.T("Bloqueado")});
        try {
            if(command.Action=="selectMap") {
                var session=championship.Session;
                if(command.Revision!=revision||command.MatchId!=session.Current.Id||command.MapId!=session.Map?.MapId)return Results.Conflict(new{error=L.T("Partida alterada. Atualize o painel.")});
                if(session.Current.Maps.Count<=1||!session.Current.Maps.Any(m=>m.MapId==command.TargetMapId))return Results.BadRequest();
                session.SelectMap(command.TargetMapId);
            }else if(command.Action=="round") {
                var session=championship.Session;var map=session.Map;
                if(command.Revision!=revision||command.MatchId!=session.Current.Id||command.MapId!=map?.MapId)return Results.Conflict(new{error=L.T("Partida alterada. Atualize o painel.")});
                if(map==null||map.Confirmed||session.Current.Complete||!championship.BansReady||command.RoundIndex < -1||command.RoundIndex>=map.Rounds.Count)return Results.Conflict();
                int number=command.RoundIndex<0?map.Rounds.Count+1:command.RoundIndex+1;
                if(command.RoundIndex<0&&(session.PendingBans().Length>0||number>12&&map.OtAttack<0))return Results.Conflict(new{error=L.T("Conclua os bans de operadores para registrar o próximo round.")});
                if(command.Winner is <0 or >1||!RoundEnding.Codes.Skip(1).Contains(command.Ending)||!RoundEnding.Allowed(command.Ending,command.Winner,map.Attacker(number)))return Results.BadRequest(new{error=L.T("Finalização incompatível com o vencedor.")});
                session.Round(command.Site,command.Winner,command.Ending,command.RoundIndex,false);
            } else if(command.Action=="ban") {
                var session=championship.Session;
                if(command.Revision!=revision||command.MatchId!=session.Current.Id||command.MapId!=session.Map?.MapId)return Results.Conflict(new{error="Partida alterada. Atualize o painel."});
                if(!championship.BansReady||session.Map?.Confirmed!=false||session.Current.Complete||session.PendingBans().FirstOrDefault(-1)!=command.Slot)return Results.Conflict(new{error=L.T("Ban de etapa futura.")});
                session.Ban(command.Slot,command.OperatorId);
            } else switch(command.Action) {
                case "start":texts.Start();break;
                case "pause":texts.PauseClock();break;
                case "adjust": if(!double.IsFinite(command.Seconds)||Math.Abs(command.Seconds)>86400)return Results.BadRequest(); texts.AdjustClock(command.Seconds);break;
                case "set":texts.SetClock(command.Seconds);break;
                case "reset":texts.ResetClock();break;
                case "timerConfig":texts.ConfigureTimer(command.Seconds,command.Format??"",command.Message??"",command.Presets??[]);break;
                default:return Results.BadRequest();
            }
            return Results.Json(State());
        } catch(InvalidDataException ex){return Results.BadRequest(new{error=ex.Message});}
          catch(InvalidOperationException ex){return Results.BadRequest(new{error=ex.Message});}
          catch(IOException ex){AppLog.Write(ex.ToString());return Results.StatusCode(500);}
    }
    public async ValueTask DisposeAsync()
    {
        stopping=true; if(subscribed){championship.Session.Changed-=Changed;subscribed=false;}
        var server=app; app=null;
        if(server!=null){using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(3));try{await server.StopAsync(timeout.Token);}finally{await server.DisposeAsync();}}
    }
}

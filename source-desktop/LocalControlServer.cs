using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CasterDesk.Desktop;

public sealed class LocalControlSettings
{
    public bool Enabled {get;set;}
    public int Port {get;set;}=43828;
}

// A separate LAN listener: never publish the OBS key or enable the OBS timer API.
internal sealed class LocalControlServer : IAsyncDisposable
{
    readonly ChampionshipView championship;
    readonly ObsDockServer bridge;
    readonly LocalControlSettings settings;
    readonly ConcurrentDictionary<string,DateTimeOffset> sessions=new();
    readonly object authGate=new();
    WebApplication? app;
    string invite="";
    long lastRead;
    bool subscribed;
    IPAddress[] addresses=[];
    internal bool Running=>app!=null;
    internal bool HasClient=>Running&&Interlocked.Read(ref lastRead)>0&&Environment.TickCount64-Interlocked.Read(ref lastRead)<6000;
    internal string[] Links=>addresses.Where(a=>!IPAddress.IsLoopback(a)).Select(a=>$"http://{a}:{settings.Port}/#invite={invite}").ToArray();
    internal string TestLink=>$"http://127.0.0.1:{settings.Port}/#invite={invite}";
    internal LocalControlServer(ChampionshipView championship,ObsDockServer bridge,LocalControlSettings settings)
    {this.championship=championship;this.bridge=bridge;this.settings=settings;if(settings.Port is <1024 or >65535)settings.Port=43828;}

    internal static bool PrivateAddress(IPAddress address)
    {
        if(address.AddressFamily!=System.Net.Sockets.AddressFamily.InterNetwork)return false;
        var b=address.GetAddressBytes();return b[0]==10||b[0]==192&&b[1]==168||b[0]==172&&b[1] is >=16 and <=31;
    }
    internal static IPAddress[] NetworkAddresses()=>NetworkInterface.GetAllNetworkInterfaces()
        .Where(n=>n.OperationalStatus==OperationalStatus.Up&&n.NetworkInterfaceType is not (NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel))
        .OrderByDescending(n=>n.GetIPProperties().GatewayAddresses.Count>0)
        .SelectMany(n=>n.GetIPProperties().UnicastAddresses).Select(a=>a.Address).Where(PrivateAddress).Distinct().ToArray();
    static bool Equal(string a,string b)=>a.Length==b.Length&&CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a),Encoding.UTF8.GetBytes(b));
    internal void Revoke(){lock(authGate){invite=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));sessions.Clear();Interlocked.Exchange(ref lastRead,0);}}
    bool Authorized(HttpContext c)=>sessions.TryGetValue(c.Request.Cookies["CasterDeskLocal"]??"",out var expires)&&expires>DateTimeOffset.UtcNow;
    internal async Task StartAsync(IPAddress[]? testAddresses=null)
    {
        if(Running)return;
        addresses=(testAddresses??NetworkAddresses()).Append(IPAddress.Loopback).Distinct().ToArray();
        Revoke();
        var builder=WebApplication.CreateSlimBuilder();builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(o=>{foreach(var ip in addresses)o.Listen(ip,settings.Port);o.Limits.MaxRequestBodySize=3_000_000;});
        builder.Services.AddRateLimiter(o=>{
            o.RejectionStatusCode=429;
            o.GlobalLimiter=PartitionedRateLimiter.Create<HttpContext,string>(c=>RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString()??"",_=>new FixedWindowRateLimiterOptions{PermitLimit=300,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));
            o.AddPolicy("pair",c=>RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString()??"",_=>new FixedWindowRateLimiterOptions{PermitLimit=8,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));
        });
        var server=builder.Build();
        server.Use(async(c,next)=>{
            c.Response.Headers.CacheControl="no-store";c.Response.Headers["Referrer-Policy"]="no-referrer";
            c.Response.Headers["X-Content-Type-Options"]="nosniff";
            c.Response.Headers["Content-Security-Policy"]="default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' blob:; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; object-src 'none'; form-action 'self'";
            if(c.Request.Path.StartsWithSegments("/media/map")||c.Request.Path.StartsWithSegments("/media/icon"))c.Response.Headers.CacheControl="private, max-age=3600";
            var host=c.Request.Host;
            bool validHost=host.Port==settings.Port&&IPAddress.TryParse(host.Host,out var ip)&&addresses.Contains(ip);
            var remote=c.Connection.RemoteIpAddress;
            string origin=c.Request.Headers.Origin.ToString(),expected="http://"+host.Value;
            if(!validHost||remote==null||!IPAddress.IsLoopback(remote)&&!PrivateAddress(remote)||(origin!=""&&origin!=expected)||c.Request.Method=="POST"&&(origin!=expected||c.Request.Headers["X-CasterDesk-Control"]!="1")){c.Response.StatusCode=403;return;}
            if(c.Request.Path.StartsWithSegments("/api")||c.Request.Path.StartsWithSegments("/media")){
                string token=c.Request.Cookies["CasterDeskLocal"]??"";
                if(!sessions.TryGetValue(token,out var expires)||expires<=DateTimeOffset.UtcNow){sessions.TryRemove(token,out _);c.Response.StatusCode=401;return;}
                Interlocked.Exchange(ref lastRead,Environment.TickCount64);
            }
            await next(c);
        });
        server.UseRateLimiter();
        server.MapGet("/",()=>Results.File(Asset("index.html"),"text/html; charset=utf-8"));
        server.MapGet("/control.js",()=>Results.File(Asset("control.js"),"text/javascript; charset=utf-8"));
        server.MapGet("/control.css",()=>Results.File(Asset("control.css"),"text/css; charset=utf-8"));
        server.MapGet("/flow.css",()=>Results.File(Asset("flow.css"),"text/css; charset=utf-8"));
        server.MapGet("/brand.svg",()=>Results.File(Path.Combine(AppContext.BaseDirectory,"assets/branding/casterdesk-horizontal-white.svg"),"image/svg+xml"));
        server.MapPost("/pair",async(HttpContext c)=>{
            if(!c.Request.HasJsonContentType())return Results.StatusCode(415);
            Pair? data;try{data=await c.Request.ReadFromJsonAsync<Pair>(c.RequestAborted);}catch(JsonException){return Results.BadRequest();}
            lock(authGate){
                if(data==null||data.Invite==null||!Equal(data.Invite,invite))return Results.Unauthorized();
                foreach(var s in sessions.Where(s=>s.Value<=DateTimeOffset.UtcNow))sessions.TryRemove(s.Key,out _);
                if(sessions.Count>=16)return Results.StatusCode(429);
                var token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));sessions[token]=DateTimeOffset.UtcNow.AddHours(24);
                // HTTP is limited to the trusted local network; Secure cookies require HTTPS.
                c.Response.Cookies.Append("CasterDeskLocal",token,new CookieOptions{HttpOnly=true,SameSite=SameSiteMode.Strict,Path="/",MaxAge=TimeSpan.FromHours(24),IsEssential=true});
                return Results.Ok();
            }
        }).RequireRateLimiting("pair");
        server.MapGet("/api/state",async Task<IResult> (HttpContext c)=>await Dispatcher.UIThread.InvokeAsync<IResult>(()=>Authorized(c)?Results.Json(bridge.State(true)):Results.Unauthorized()));
        server.MapPost("/api/command",async Task<IResult> (HttpContext c)=>{
            if(!c.Request.HasJsonContentType())return Results.StatusCode(415);
            ObsDockServer.DockCommand? command;try{command=await c.Request.ReadFromJsonAsync<ObsDockServer.DockCommand>(c.RequestAborted);}catch(JsonException){return Results.BadRequest();}
            if(command==null||command.Action is not ("configure" or "veto" or "side" or "round" or "ban" or "selectMap"))return Results.BadRequest();
            return await Dispatcher.UIThread.InvokeAsync(()=>Authorized(c)?bridge.Execute(command,true):Results.Unauthorized());
        });
        server.MapGet("/media/map/{id}",(string id)=>{
            var map=championship.Session.Catalog.Maps.FirstOrDefault(m=>m.Id==id);
            return map==null?Results.NotFound():Results.File(SiegeCatalog.Asset(map.Image),"image/jpeg");
        });
        server.MapGet("/media/icon/{id}",(string id)=>{
            if(id is "__def" or "__atq")return Results.File(StudioRenderer.Asset(id=="__def"?"Icon_Defender.png":"Icon_Attacker.png"),"image/png");
            var op=championship.Session.Catalog.Operators.FirstOrDefault(o=>o.Id==id);
            return op==null?Results.NotFound():Results.File(SiegeCatalog.Asset(op.Icon),"image/png");
        });
        server.MapGet("/media/logo/{team:int}",async(int team)=>await Dispatcher.UIThread.InvokeAsync(()=>{
            if(team is <0 or >1)return Results.NotFound();var match=championship.Session.Current;var brand=MatchFiles.ReadBrand(match);
            var path=MatchFiles.Resolve(match,team==0?brand.TeamALogo:brand.TeamBLogo);return path==null?Results.NotFound():Results.File(path,"image/png");
        }));
        server.MapPost("/api/logo/{team:int}",async(int team,HttpContext c)=>{
            if(team is <0 or >1||c.Request.ContentType!="image/png"&&c.Request.ContentType!="image/jpeg")return Results.BadRequest();
            using var data=new MemoryStream();await c.Request.Body.CopyToAsync(data,c.RequestAborted);if(data.Length>2_000_000)return Results.StatusCode(413);
            return await Dispatcher.UIThread.InvokeAsync(()=>{
                if(!Authorized(c))return Results.Unauthorized();
                var state=JsonSerializer.SerializeToElement(bridge.State(true));
                if(!state.GetProperty("controllable").GetBoolean()||!state.GetProperty("setup").GetProperty("editable").GetBoolean()||c.Request.Headers["X-Match-Id"]!=championship.Session.Current.Id||c.Request.Headers["X-Revision"]!=state.GetProperty("revision").GetInt64().ToString())return Results.Conflict();
                string temp=Path.Combine(Path.GetTempPath(),"casterdesk-logo-"+Guid.NewGuid().ToString("N")+".img");
                try{
                    using(var image=System.Drawing.Image.FromStream(new MemoryStream(data.ToArray()),false,true))if((long)image.Width*image.Height>16_000_000)return Results.BadRequest();
                    File.WriteAllBytes(temp,data.ToArray());var match=championship.Session.Current;var brand=MatchFiles.ReadBrand(match);
                    var file=MatchFiles.Resource(match,team==0?"logo-a":"logo-b",temp);if(team==0)brand.TeamALogo=file;else brand.TeamBLogo=file;
                    MatchFiles.SaveBrand(match,brand);bridge.Changed();championship.Refresh();return Results.Json(bridge.State(true));
                }catch(Exception e) when(e is ArgumentException or InvalidDataException or System.Runtime.InteropServices.ExternalException){return Results.BadRequest(new{error="Use uma imagem PNG ou JPG válida, com até 2 MB."});}
                finally{if(File.Exists(temp))File.Delete(temp);}
            });
        });
        try{await server.StartAsync();app=server;championship.LocalControlActive=true;championship.Session.Changed+=bridge.Changed;subscribed=true;}
        catch{await server.DisposeAsync();throw;}
    }
    static string Asset(string name)=>Path.Combine(AppContext.BaseDirectory,"assets/control",name);
    sealed record Pair(string Invite);
    public async ValueTask DisposeAsync(){var server=app;app=null;championship.LocalControlActive=false;Revoke();if(subscribed){championship.Session.Changed-=bridge.Changed;subscribed=false;}if(server!=null){using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(3));try{await server.StopAsync(timeout.Token);}finally{await server.DisposeAsync();}}}
}

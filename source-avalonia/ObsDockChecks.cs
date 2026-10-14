using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace CasterDesk.Avalonia;

internal static class ObsDockChecks
{
    static int checks;
    static void Need(bool value,string message){checks++;if(!value)throw new Exception("OBS dock: "+message);}
    static T Wait<T>(Task<T> task){var clock=System.Diagnostics.Stopwatch.StartNew();while(!task.IsCompleted){if(clock.Elapsed.TotalSeconds>20)throw new TimeoutException();Dispatcher.UIThread.RunJobs();Thread.Sleep(5);}return task.GetAwaiter().GetResult();}
    static void Wait(Task task)=>Wait(Finish(task));
    static async Task<bool> Finish(Task task){await task;return true;}
    public static void Run()
    {
        var pluginSettings=new ObsDockSettings();Need(!pluginSettings.ConfigurePlugin(false)&&!pluginSettings.Enabled,"No installer opt-in leaves connection disabled");
        Need(pluginSettings.ConfigurePlugin(true)&&pluginSettings.Enabled&&pluginSettings.PluginConfigured,"Plugin installation enables connection once");
        pluginSettings.Enabled=false;Need(!pluginSettings.ConfigurePlugin(true)&&!pluginSettings.Enabled,"Manual disable survives restart and upgrades");
        var probe=new TcpListener(IPAddress.Loopback,0);probe.Start();int port=((IPEndPoint)probe.LocalEndpoint).Port;probe.Stop();
        var session=new MatchSession();session.New();var view=new ChampionshipView(session);
        var texts=new TextsView(new Settings {Initial=1,Output=Path.Combine(AppPaths.TextDirectory,"obs-check.txt")},()=>{});
        var window=new Window {Content=view,Width=1280,Height=800};window.Show();Dispatcher.UIThread.RunJobs();
        bool allowed=true;var server=new ObsDockServer(view,texts,()=>allowed,new ObsDockSettings {Port=port});
        using var client=new HttpClient {BaseAddress=new Uri(server.BaseUrl),Timeout=TimeSpan.FromSeconds(10)};
        Wait(server.StartAsync());Need(server.Running,"Server starts on loopback without native OBS");
        HttpResponseMessage Send(string path,string? key=null,object? body=null,string? host=null,string? origin=null){
            var request=new HttpRequestMessage(body==null?HttpMethod.Get:HttpMethod.Post,path);
            if(key!=null)request.Headers.Add("X-CasterDesk-Key",key);
            if(host!=null)request.Headers.Host=host;if(origin!=null)request.Headers.Add("Origin",origin);
            if(body!=null)request.Content=JsonContent.Create(body);
            return Wait(client.SendAsync(request));
        }
        string key=new Uri(server.PanelUrl).Fragment[5..];
        using(var r=Send("/api/state"))Need(r.StatusCode==HttpStatusCode.Unauthorized,"No key cannot read match state");
        using(var r=Send("/api/state","wrong"))Need(r.StatusCode==HttpStatusCode.Unauthorized,"Invalid key rejected");
        using(var r=Send("/api/state",key,host:"evil.example"))Need(r.StatusCode==HttpStatusCode.Forbidden,"Host protects local API from rebinding");
        using(var r=Send("/api/state",key,origin:"https://evil.example"))Need(r.StatusCode==HttpStatusCode.Forbidden,"Foreign origin rejected");
        using(var r=Send("/")){string html=Wait(r.Content.ReadAsStringAsync());Need(r.IsSuccessStatusCode&&!html.Contains(key)&&!html.Contains(session.Current.Id),"Public dock page contains no key or match data");}
        using(var r=Send("/api/command",key,new{action="set",seconds=90}))Need(r.IsSuccessStatusCode&&texts.Remaining==90,"Panel sets same program timer");
        using(var r=Send("/api/command",key,new{action="start"}))Need(r.IsSuccessStatusCode&&texts.Running,"Panel starts same timer");
        using(var r=Send("/api/command",key,new{action="adjust",seconds=-1000}))Need(r.IsSuccessStatusCode&&texts.Remaining==0&&!texts.Running,"Subtract clamps and ends running timer");
        using(var r=Send("/api/command",key,new{action="set",seconds=100000}))Need(r.StatusCode==HttpStatusCode.BadRequest&&texts.Remaining==0,"Invalid duration preserves timer");
        allowed=false;using(var r=Send("/api/command",key,new{action="set",seconds=60}))Need(r.StatusCode==HttpStatusCode.Conflict&&texts.Remaining==0,"Modal/backup guard blocks commands");allowed=true;
        session.Setup("MD1",0,0,false);for(int n=0;n<8;n++)session.Veto(n,session.Catalog.Maps[n].Id);session.Side(session.Map!.MapId,false,0);
        JsonDocument State(){using var r=Send("/api/state",key);return JsonDocument.Parse(Wait(r.Content.ReadAsStringAsync()));}
        long previous=-1;string mapId=session.Map!.MapId;
        for(int n=0;n<4;n++){
            using var state=State();var data=state.RootElement;string role=data.GetProperty("role").GetString()!;
            Need(role==(n%2==0?"DEF":"ATQ"),"Dock follows alternating next ban");
            int slot=data.GetProperty("pending").GetInt32();long revision=data.GetProperty("revision").GetInt64();
            var op=OperatorOrder.Sort(session.Catalog.Operators,role).First(o=>!session.Map.Bans.Contains(o.Id));
            using(var r=Send("/api/command",key,new{action="ban",matchId=session.Current.Id,mapId,revision,slot,operatorId=op.Id}))Need(r.IsSuccessStatusCode&&session.Map.Bans[slot]==op.Id&&view.ActiveDialog==null,"Dock ban saved without program popup");
            if(n==0){previous=revision;using var r=Send("/api/command",key,new{action="ban",matchId=session.Current.Id,mapId,revision,slot,operatorId=op.Id});Need(r.StatusCode==HttpStatusCode.Conflict,"Stale or duplicated ban request rejected");}
        }
        using(var r=Send("/api/command",key,new{action="ban",matchId=session.Current.Id,mapId,revision=previous,slot=4,operatorId="sentry"}))Need(r.StatusCode==HttpStatusCode.Conflict,"Future stage cannot be opened remotely");
        Wait(server.DisposeAsync().AsTask());Need(!server.Running,"Disable stops connection");Wait(server.StartAsync());
        using(var before=State()){
            long revision=before.RootElement.GetProperty("revision").GetInt64();session.New();using var after=State();
            Need(after.RootElement.GetProperty("revision").GetInt64()>revision,"Restart reconnects match revision notifications");
            using var r=Send("/api/command",key,new{action="ban",matchId=before.RootElement.GetProperty("match").GetProperty("id").GetString(),mapId,revision,slot=4,operatorId="sentry"});Need(r.StatusCode==HttpStatusCode.Conflict,"Changed selected match rejects old commands");
        }
        Wait(server.DisposeAsync().AsTask());view.Stop();texts.Stop();window.Close();Dispatcher.UIThread.RunJobs();
        File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"avalonia-obs-check.txt"),$"PASS: {checks} HTTP integration checks: loopback/key/origin/host, shared timer, protected alternating bans, stale requests, restart and selected match changes. Headless only; OBS not opened.");
    }
}

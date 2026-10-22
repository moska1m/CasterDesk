using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;

namespace CasterDesk.Desktop;

internal static class LocalControlChecks
{
    static int checks;
    static void Need(bool value,string text){checks++;if(!value)throw new Exception("Local control: "+text);}
    static T Wait<T>(Task<T> task){var watch=System.Diagnostics.Stopwatch.StartNew();while(!task.IsCompleted){if(watch.Elapsed.TotalSeconds>25)throw new TimeoutException();Dispatcher.UIThread.RunJobs();Thread.Sleep(5);}return task.GetAwaiter().GetResult();}
    static void Wait(Task task)=>Wait(Complete(task));
    static async Task<bool> Complete(Task task){await task;return true;}
    static int Port(){var p=new TcpListener(IPAddress.Loopback,0);p.Start();int port=((IPEndPoint)p.LocalEndpoint).Port;p.Stop();return port;}
    public static void Run(bool fixture=false)
    {
        var session=new MatchSession();session.New();var view=new ChampionshipView(session);var texts=new TextsView(new Settings{Output=Path.Combine(AppPaths.TextDirectory,"local-check.txt")},()=>{});
        var window=new Window{Content=view,Width=1280,Height=800};window.Show();Dispatcher.UIThread.RunJobs();
        bool allowed=true;var bridge=new ObsDockServer(view,texts,()=>allowed,new ObsDockSettings());var server=new LocalControlServer(view,bridge,new LocalControlSettings{Port=Port()});
        Wait(server.StartAsync([IPAddress.Loopback]));
        if(fixture){
            string info=Path.Combine(AppPaths.DataDirectory,"local-fixture.json"),stop=Path.Combine(AppPaths.DataDirectory,"local-fixture.stop");
            File.WriteAllText(info,JsonSerializer.Serialize(new{url=server.TestLink}));
            var watch=System.Diagnostics.Stopwatch.StartNew();while(!File.Exists(stop)&&watch.Elapsed.TotalMinutes<20){Dispatcher.UIThread.RunJobs();Thread.Sleep(5);}
            Wait(server.DisposeAsync().AsTask());Wait(bridge.DisposeAsync().AsTask());view.Stop();texts.Stop();window.Close();return;
        }
        var url=new Uri(server.TestLink);string origin=url.GetLeftPart(UriPartial.Authority),invite=url.Fragment[8..];
        using var client=new HttpClient(new HttpClientHandler{CookieContainer=new CookieContainer()}){BaseAddress=new Uri(origin),Timeout=TimeSpan.FromSeconds(10)};
        HttpResponseMessage Send(string path,object? body=null,string? host=null,string? foreign=null,bool control=true){var request=new HttpRequestMessage(body==null?HttpMethod.Get:HttpMethod.Post,path);if(body!=null){request.Content=JsonContent.Create(body);request.Headers.Add("Origin",foreign??origin);if(control)request.Headers.Add("X-CasterDesk-Control","1");}if(host!=null)request.Headers.Host=host;return Wait(client.SendAsync(request));}
        JsonElement State(){using var r=Send("/api/state");Need(r.IsSuccessStatusCode,"authenticated state");return JsonDocument.Parse(Wait(r.Content.ReadAsStringAsync())).RootElement.Clone();}
        HttpResponseMessage Cmd(string action,object? extra=null){var d=State();var body=new Dictionary<string,object?>{{"action",action},{"revision",d.GetProperty("revision").GetInt64()},{"matchId",d.GetProperty("match").GetProperty("id").GetString()},{"mapId",d.GetProperty("match").GetProperty("mapId").GetString()??""}};if(extra!=null)foreach(var p in JsonSerializer.SerializeToElement(extra).EnumerateObject())body[p.Name]=p.Value;return Send("/api/command",body);}
        try{
            Need(server.Running&&view.LocalControlActive&&!bridge.Running,"LAN operates without enabling OBS");
            Need(LocalControlServer.PrivateAddress(IPAddress.Parse("192.168.2.5"))&&!LocalControlServer.PrivateAddress(IPAddress.Parse("8.8.8.8")),"only private IPv4 is offered");
            using(var r=Send("/api/state"))Need(r.StatusCode==HttpStatusCode.Unauthorized,"unauthenticated read rejected");
            using(var r=Send("/media/map/bank"))Need(r.StatusCode==HttpStatusCode.Unauthorized,"assets require pairing");
            using(var r=Send("/pair",new{invite="bad"}))Need(r.StatusCode==HttpStatusCode.Unauthorized,"bad invite rejected");
            using(var r=Send("/pair",new{invite},foreign:"https://evil.example"))Need(r.StatusCode==HttpStatusCode.Forbidden,"foreign origin rejected");
            using(var r=Send("/pair",new{invite},control:false))Need(r.StatusCode==HttpStatusCode.Forbidden,"CSRF header required");
            using(var r=Send("/pair",new{invite}))Need(r.IsSuccessStatusCode&&r.Headers.GetValues("Set-Cookie").Any(c=>c.Contains("httponly",StringComparison.OrdinalIgnoreCase)&&c.Contains("samesite=strict",StringComparison.OrdinalIgnoreCase)),"pairing uses HttpOnly SameSite cookie");
            using(var r=Send("/api/state",host:"evil.example"))Need(r.StatusCode==HttpStatusCode.Forbidden,"DNS rebinding rejected");
            using(var r=Cmd("set",new{seconds=10}))Need(r.StatusCode==HttpStatusCode.BadRequest,"timer API absent from LAN");
            using(var r=Cmd("configure",new{teamA="Controle Azul",teamB="Controle Laranja",format="MD3",coin=0,first=1}))Need(r.IsSuccessStatusCode&&session.Current.TeamA=="Controle Azul"&&session.Current.Format=="MD3","configuration shares persisted desktop session");
            using(var r=Cmd("configure",new{teamA="Changed",teamB="",format="bad",coin=0,first=1}))Need(r.StatusCode==HttpStatusCode.BadRequest&&session.Current.TeamA=="Controle Azul","configuration validates atomically");
            {
                var s=State();using var image=new System.Drawing.Bitmap(16,16);using var buffer=new MemoryStream();image.Save(buffer,System.Drawing.Imaging.ImageFormat.Png);
                using var upload=new HttpRequestMessage(HttpMethod.Post,"/api/logo/0");upload.Headers.Add("Origin",origin);upload.Headers.Add("X-CasterDesk-Control","1");upload.Headers.Add("X-Match-Id",session.Current.Id);upload.Headers.Add("X-Revision",s.GetProperty("revision").GetInt64().ToString());
                upload.Content=new ByteArrayContent(buffer.ToArray());upload.Content.Headers.ContentType=new("image/png");
                using var r=Wait(client.SendAsync(upload));Need(r.IsSuccessStatusCode&&MatchFiles.Resolve(session.Current,MatchFiles.ReadBrand(session.Current).TeamALogo)!=null,"logo upload is decoded and saved in match assets");
            }
            var before=State();using(var r=Cmd("veto",new{step=0,targetMapId=session.Catalog.Maps[0].Id}))Need(r.IsSuccessStatusCode,"first veto saved");
            using(var r=Send("/api/command",new{action="veto",matchId=session.Current.Id,mapId="",revision=before.GetProperty("revision").GetInt64(),step=1,targetMapId=session.Catalog.Maps[1].Id}))Need(r.StatusCode==HttpStatusCode.Conflict,"stale veto rejected");
            for(int i=1;i<8;i++)using(var r=Cmd("veto",new{step=i,targetMapId=session.Catalog.Maps[i].Id}))Need(r.IsSuccessStatusCode,"veto order "+i);
            Need(session.Current.Maps.Count==3,"MD3 maps derived from same engine");
            string map=session.Map!.MapId;
            using(var r=Cmd("side",new{targetMapId=map,attacker=0}))Need(r.IsSuccessStatusCode,"initial side");
            using(var r=Cmd("side",new{targetMapId=map,attacker=1,overtime=true}))Need(r.IsSuccessStatusCode&&session.Map.OtAttack==1,"OT side");
            while(session.PendingBans().Length>0){var s=State();int slot=s.GetProperty("pending").GetInt32();using var r=Cmd("ban",new{slot,operatorId=MapRecord.NoBan(s.GetProperty("role").GetString()!)});Need(r.IsSuccessStatusCode,"repeatable no ban");}
            before=State();var round=new{action="round",matchId=session.Current.Id,mapId=map,revision=before.GetProperty("revision").GetInt64(),site=session.Catalog.Maps.Single(m=>m.Id==map).Sites[0],winner=0,ending="elimination"};
            using(var r=Send("/api/command",round))Need(r.IsSuccessStatusCode&&session.Map.Rounds.Count==1&&view.ActiveDialog==null,"round saved without desktop popup");
            using(var r=Send("/api/command",round))Need(r.StatusCode==HttpStatusCode.Conflict&&session.Map.Rounds.Count==1,"duplicate round never appended");
            allowed=false;using(var r=Cmd("round",new{site=round.site,winner=0,ending="elimination"}))Need(r.StatusCode==HttpStatusCode.Conflict,"desktop editing guard");allowed=true;
            using(var r=Cmd("configure",new{teamA="Changed",teamB="Control",format="MD1",coin=0,first=0}))Need(r.StatusCode==HttpStatusCode.BadRequest&&session.Map.Rounds.Count==1&&session.Current.TeamA=="Controle Azul","remote setup cannot erase veto or rounds");
            var resumed=State();Need(resumed.GetProperty("rounds").GetProperty("history").GetArrayLength()==1,"reload retains authenticated history");
            var remake=resumed.GetProperty("remakes").EnumerateArray().Single(m=>m.GetProperty("id").GetString()==map);
            Need(remake.GetProperty("score").GetProperty("a").GetInt32()==1&&remake.GetProperty("score").GetProperty("b").GetInt32()==0,"remake score matches recorded rounds");
            Need(remake.GetProperty("rounds")[0].GetProperty("ending").GetString()=="elimination"&&remake.GetProperty("rounds")[0].GetProperty("attacker").GetInt32()==0,"remake preserves round ending and attacker");
            Need(remake.GetProperty("bans").GetArrayLength()==4&&remake.GetProperty("bans")[0].GetProperty("slot").GetInt32()==session.Map.BanOrder(0)[0],"remake bans retain actual selection order");
            server.Revoke();using(var r=Send("/api/state"))Need(r.StatusCode==HttpStatusCode.Unauthorized,"revocation invalidates existing browser cookie");
            using(var r=Send("/pair",new{invite}))Need(r.StatusCode==HttpStatusCode.Unauthorized,"old QR invalid after rotation");
            string fresh=new Uri(server.TestLink).Fragment[8..];using(var r=Send("/pair",new{invite=fresh}))Need(r.IsSuccessStatusCode,"new QR pairs");
            Wait(server.DisposeAsync().AsTask());Need(!server.Running&&!view.LocalControlActive,"disable stops local routing");Wait(server.StartAsync([IPAddress.Loopback]));using(var r=Send("/api/state"))Need(r.StatusCode==HttpStatusCode.Unauthorized,"PC restart rejects old session");
        }finally{Wait(server.DisposeAsync().AsTask());Wait(bridge.DisposeAsync().AsTask());view.Stop();texts.Stop();window.Close();}
        var shell=new StudioShell(new MatchSession());shell.Show();Dispatcher.UIThread.RunJobs();
        T Field<T>(string name)=>(T)typeof(StudioShell).GetField(name,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(shell)!;
        var shellServer=Field<LocalControlServer>("localControl");
        Field<LocalControlSettings>("localSettings").Port=Port();
        Wait(shellServer.StartAsync([IPAddress.Loopback]));
        int shutdownPort=new Uri(shellServer.TestLink).Port;
        shell.Close();
        var closeWatch=System.Diagnostics.Stopwatch.StartNew();
        while(shell.IsVisible&&closeWatch.Elapsed.TotalSeconds<15){Dispatcher.UIThread.RunJobs();Thread.Sleep(5);}
        Need(!shell.IsVisible&&!shellServer.Running,"window closes only after local HTTP host stops");
        var reclaimed=new TcpListener(IPAddress.Loopback,shutdownPort);
        try{reclaimed.Start();Need(true,"closing releases the local control port");}finally{reclaimed.Stop();}
        File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"casterdesk-local-control-check.txt"),$"PASS: {checks} local control checks: authentication, origin/host, shared setup/veto/sides/rounds/bans, duplicates, reload, revocation, restart and window shutdown. No native window or firewall change.");
    }
}

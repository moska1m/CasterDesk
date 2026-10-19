using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using CasterDesk.Core;

static class GoogleSheetsChecks
{
    sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }
    static HttpResponseMessage Json(object value, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(JsonSerializer.Serialize(value)) };
    static void Need(bool value, string label) { if (!value) throw new Exception("Google Sheets: " + label); }
    public static void Run() => Check().GetAwaiter().GetResult();
    static async Task Check()
    {
        const string id = "test_document_01234567890123456789";
        string link = "https://docs.google.com/spreadsheets/d/" + id + "/edit#gid=42";
        Need(GoogleSheetsTable.ParseLink(link) == (id, 42), "parse target gid");
        foreach (string bad in new[] { "http://docs.google.com/spreadsheets/d/" + id, "https://docs.google.com.evil.test/spreadsheets/d/" + id, "https://example.com/" + id }) {
            bool refused=false;try{GoogleSheetsTable.ParseLink(bad);}catch(InvalidDataException){refused=true;}Need(refused,"reject non-Google link");
        }
        string title = GoogleSheetsTable.SheetTitle, header = "Time1"; int writes = 0; JsonElement payload = default;
        using var http = new HttpClient(new Handler(async request => {
            Need(request.Headers.Authorization?.Scheme == "Bearer", "bearer authorization");
            if (request.Method == HttpMethod.Post) { writes++; payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync()).RootElement.Clone(); return Json(new { totalUpdatedCells = 45 }); }
            if (request.RequestUri!.AbsolutePath.Contains("/values/")) return Json(new { values = new object[][] { new object[] {header,"Time2"}, [],[],[],[],[],new object[]{"Rounds","Vencedor","Formato Vitória"} } });
            return Json(new { sheets = new[] {new { properties = new {sheetId=42,title,gridProperties=new {rowCount=1000,columnCount=26}}}} });
        }));
        var sheets = new GoogleSheetsTable(http, _ => Task.FromResult("fake-test-access"));
        var catalog = SiegeCatalog.Load();var match = new SiegeMatch { TeamA = "=FORMULA()", DirectMaps = true };
        match.Maps.Add(new() { MapId=catalog.Maps[0].Id, InitialAttack=0, Rounds=[new(){Winner=1,Ending="defuser_disabled"}] });
        await sheets.UpdateAsync(link,match,catalog);
        Need(writes==1 && payload.GetProperty("valueInputOption").GetString()=="RAW", "one RAW batch");
        var inputs=MatchTable.Inputs(match,catalog);var data=payload.GetProperty("data");
        Need(data.GetArrayLength()==inputs.Count, "exact input count");
        foreach(var cell in data.EnumerateArray()) { string address=cell.GetProperty("range").GetString()!.Split('!')[1];Need(inputs.ContainsKey(address),"only allowed cells");Need(cell.GetProperty("values")[0][0].ToString()==inputs[address].ToString(),"same Excel projection "+address); }
        header="Wrong";bool rejected=false;try{await sheets.UpdateAsync(link,match,catalog);}catch(InvalidDataException){rejected=true;}Need(rejected&&writes==1,"wrong layout never written");
        header="Time1";title="Wrong";rejected=false;try{await sheets.UpdateAsync(link,match,catalog);}catch(InvalidDataException){rejected=true;}Need(rejected&&writes==1,"wrong tab never written");
        string root=Path.Combine(AppPaths.DataDirectory,"google-auth-test");Directory.CreateDirectory(root);
        string client=Path.Combine(root,"fake-client.json"),store=Path.Combine(root,"auth.bin");
        await File.WriteAllTextAsync(client,JsonSerializer.Serialize(new{installed=new{client_id="fake-test.apps.googleusercontent.com",client_secret="fake-test-secret"}}));
        var bundled = new GoogleTableAuth(new HttpClient(), Path.Combine(root,"fresh-user.bin"), client);
        Need(bundled.Configured && !bundled.Connected, "fresh install loads desktop client without sharing user tokens");
        string expectedChallenge="";int refreshes=0,revokes=0;
        using var tokenHttp=new HttpClient(new Handler(async request=>{
            var form=HttpUtility.ParseQueryString(await request.Content!.ReadAsStringAsync());
            if(request.RequestUri!.AbsolutePath=="/revoke"){revokes++;return Json(new{});}
            if(form["grant_type"]=="authorization_code") {
                string verifier=form["code_verifier"]!;
                string challenge=Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))).TrimEnd('=').Replace('+','-').Replace('/','_');
                Need(challenge==expectedChallenge,"PKCE verifier matches challenge");
                return Json(new{access_token="fake-access",refresh_token="fake-refresh",expires_in=0});
            }
            refreshes++;return Json(new{access_token="fake-refreshed-access",expires_in=3600});
        }));
        var auth=new GoogleTableAuth(tokenHttp,store);await auth.ImportClientAsync(client);Need(auth.Configured&&!auth.Connected,"import does not sign in");
        Task callback=Task.CompletedTask;
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await auth.LoginAsync(url=>{
            var query=HttpUtility.ParseQueryString(new Uri(url).Query);expectedChallenge=query["code_challenge"]!;
            Need(query["prompt"]=="consent select_account","login explicitly offers account choice");
            Need(query["code_challenge_method"]=="S256"&&new Uri(query["redirect_uri"]!).Host=="127.0.0.1","loopback with S256");
            callback=Task.Run(async()=>{
                using var local=new HttpClient();string redirect=query["redirect_uri"]!;
                using var bad=await local.GetAsync(redirect+"?state=wrong&code=fake");Need(bad.StatusCode==HttpStatusCode.BadRequest,"wrong state refused");
                using var good=await local.GetAsync(redirect+"?state="+Uri.EscapeDataString(query["state"]!)+"&code=fake");Need(good.IsSuccessStatusCode,"valid callback");
            });
        },timeout.Token);await callback;
        Need(auth.Connected,"login retains offline token");
        Need(await auth.AccessTokenAsync()=="fake-refreshed-access"&&refreshes==1,"expired access token refreshes");
        string encrypted=Encoding.UTF8.GetString(await File.ReadAllBytesAsync(store));
        Need(!encrypted.Contains("fake-refresh")&&!encrypted.Contains("fake-test-secret"),"credentials protected on disk");
        var restored=new GoogleTableAuth(tokenHttp,store);Need(restored.Connected,"DPAPI store survives restart");
        await restored.DisconnectAsync();Need(!restored.Connected&&revokes==1,"disconnect revokes grant and forgets tokens");
        Console.WriteLine("Google Sheets checks passed: exact RAW ranges, target validation, PKCE/state, encrypted tokens, refresh and revoke. No Google account contacted.");
    }
}

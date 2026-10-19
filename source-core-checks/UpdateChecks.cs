using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using CasterDesk.Core;

static class UpdateChecks
{
    sealed class Handler(Func<HttpRequestMessage,HttpResponseMessage> send):HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>Task.FromResult(send(request));
    }
    static void Need(bool value,string name){if(!value)throw new Exception("Updates: "+name);}
    static async Task Refused(Func<Task> action,string name){bool refused=false;try{await action();}catch(Exception ex) when(ex is InvalidDataException or CryptographicException or OperationCanceledException){refused=true;}Need(refused,name);}
    public static void Run()=>Check().GetAwaiter().GetResult();
    static async Task Check()
    {
        foreach(var(a,b)in new[]{("1.0.9","1.0.8"),("1.0.8","1.0.8-beta.10"),("1.0.8-beta.10","1.0.8-beta.9"),("1.0.8-beta","1.0.8-1"),("1.1.0","1.0.99")})Need(AppVersion.Parse(a).CompareTo(AppVersion.Parse(b))>0,"semantic order "+a);
        Need(AppVersion.Parse("1.0.8+commit").CompareTo(AppVersion.Parse("1.0.8"))==0,"metadata ignored");
        foreach(string bad in new[]{"../1.0.8","1.0.8/evil","1.0.8-beta.01","01.0.8","1.0"})await Refused(()=>Task.FromResult(AppVersion.Parse(bad)),"reject malformed version");
        using var key=RSA.Create(2048);byte[] package=Enumerable.Range(0,1000).Select(i=>(byte)i).ToArray();
        var manifest=new UpdateManifest(1,"release","9.0.0","important","Correção importante de exemplo.",DateTimeOffset.UtcNow,package.Length,Convert.ToHexString(SHA256.HashData(package)));
        byte[] Envelope(object value,RSA? signer=null){byte[] bytes=JsonSerializer.SerializeToUtf8Bytes(value);return JsonSerializer.SerializeToUtf8Bytes(new{Payload=Convert.ToBase64String(bytes),Signature=Convert.ToBase64String((signer??key).SignData(bytes,HashAlgorithmName.SHA256,RSASignaturePadding.Pss))});}
        byte[] Catalog(params UpdateManifest[] versions)=>Envelope(new UpdateCatalog(1,versions));
        byte[] feed=Catalog(manifest);bool badHash=false,untrusted=false,truncated=false;
        using var http=new HttpClient(new Handler(request=>{
            if(untrusted){var response=new HttpResponseMessage(HttpStatusCode.Found);response.Headers.Location=new Uri("https://evil.example/installer.exe");return response;}
            return new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(request.RequestUri!.AbsolutePath.EndsWith(".json")?feed:badHash?new byte[package.Length]:truncated?package[..500]:package)};
        }));
        var client=new UpdateClient(http,key.ExportSubjectPublicKeyInfoPem());
        Need((await client.ListAsync()).Single().Important,"important signed offer");
        var beta=manifest with{Type="beta",Version="9.1.0-beta.10"};
        feed=Catalog(manifest,beta with{Version="9.1.0-beta.9"},beta);
        Need((await client.ListAsync()).Select(v=>v.Version).SequenceEqual(new[]{"9.1.0-beta.10","9.1.0-beta.9","9.0.0"}),"catalog sorted semantically");
        using(var stranger=RSA.Create(2048)){feed=Envelope(new UpdateCatalog(1,[manifest]),stranger);await Refused(()=>client.ListAsync(),"reject foreign signing key");}
        feed=Catalog(manifest,manifest);await Refused(()=>client.ListAsync(),"reject duplicate versions");
        feed=Catalog(manifest with{Type="alpha",Version="9.0.0-alpha.1"});await Refused(()=>client.ListAsync(),"only release and beta supported");
        feed=Catalog(manifest with{Version="9.0.0-beta.1"});await Refused(()=>client.ListAsync(),"reject mislabeled beta");
        feed=Catalog(beta with{Version="9.0.0"});await Refused(()=>client.ListAsync(),"reject mislabeled release");
        feed=Catalog(manifest with{Size=UpdateClient.MaxInstallerSize+1});await Refused(()=>client.ListAsync(),"reject oversize offer");
        feed=Catalog(manifest with{Version="../9.0.0"});await Refused(()=>client.ListAsync(),"reject signed path traversal");
        feed=Catalog(manifest);string root=Path.Combine(AppPaths.DataDirectory,"update-check");
        Need(client.ReadManifest(Envelope(manifest))==manifest,"individual signed manifest");
        var path=await client.DownloadAsync(manifest,directory:root);Need(File.ReadAllBytes(path).SequenceEqual(package),"verified download");
        File.WriteAllBytes(path,new byte[package.Length]);badHash=true;
        await Refused(()=>client.DownloadAsync(manifest,directory:root),"reject tampered cached bytes");
        Need(!Directory.GetFiles(root,"*.partial-*").Any(),"failed partial download removed");
        badHash=false;truncated=true;await Refused(()=>client.DownloadAsync(manifest,directory:root),"reject truncated file");
        truncated=false;untrusted=true;await Refused(()=>client.ListAsync(),"reject external redirect");
        untrusted=false;using(var cancellation=new CancellationTokenSource()){cancellation.Cancel();await Refused(()=>client.DownloadAsync(manifest,token:cancellation.Token,directory:root),"respect cancellation");}
        path=await client.DownloadAsync(manifest,directory:root);await UpdateClient.VerifyFileAsync(path,manifest);
        Console.WriteLine("Update checks passed: signed version catalog, release/beta validation, semantic order, hostile redirects, corrupt/cached/incomplete installers and cancellation. No installer executed.");
    }
}

using System.Net;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CasterDesk.Core;

public sealed record AppVersion(int Major, int Minor, int Patch, string[] Pre) : IComparable<AppVersion>
{
    public static AppVersion Parse(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 128) throw new InvalidDataException("Versão inválida.");
        var m=Regex.Match(value,@"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z.-]+)?$");
        if(!m.Success || !int.TryParse(m.Groups[1].Value,out int a) || !int.TryParse(m.Groups[2].Value,out int b) || !int.TryParse(m.Groups[3].Value,out int c)) throw new InvalidDataException("Versão inválida.");
        string[] pre=m.Groups[4].Success?m.Groups[4].Value.Split('.'):[];
        if(pre.Any(p=>p.Length>1&&p[0]=='0'&&p.All(char.IsDigit)))throw new InvalidDataException("Versão inválida.");
        return new(a,b,c,pre);
    }
    public int CompareTo(AppVersion? other)
    {
        if(other==null)return 1;
        int compare=Major.CompareTo(other.Major);if(compare!=0)return compare;
        compare=Minor.CompareTo(other.Minor);if(compare!=0)return compare;
        compare=Patch.CompareTo(other.Patch);if(compare!=0)return compare;
        if(Pre.Length==0||other.Pre.Length==0)return (Pre.Length==0).CompareTo(other.Pre.Length==0);
        for(int i=0;i<Math.Min(Pre.Length,other.Pre.Length);i++) {
            bool a=Pre[i].All(char.IsDigit),b=other.Pre[i].All(char.IsDigit);
            var av=a?BigInteger.Parse(Pre[i]):BigInteger.Zero;var bv=b?BigInteger.Parse(other.Pre[i]):BigInteger.Zero;
            compare=a&&b?av.CompareTo(bv):a!=b?(a?-1:1):string.CompareOrdinal(Pre[i],other.Pre[i]);if(compare!=0)return compare;
        }
        return Pre.Length.CompareTo(other.Pre.Length);
    }
}

public sealed record UpdateManifest(int Schema,string Type,string Version,string Priority,string Notes,DateTimeOffset PublishedAt,long Size,string Sha256)
{
    public bool Important => Priority=="important";
    public bool Beta => Type=="beta";
    public string FileName => $"CasterDesk-{Version}-win-x64-Setup.exe";
    public Uri InstallerUri => new($"https://github.com/moska1m/CasterDesk/releases/download/v{Version}/{FileName}");
}

public sealed record UpdateCatalog(int Schema,UpdateManifest[] Versions);

public sealed class UpdateClient
{
    public const string FeedRoot="https://github.com/moska1m/CasterDesk/releases/download/update-feed/";
    public const long MaxInstallerSize=1024L*1024*1024;
    public const int MaxCatalogBytes=512_000;
    readonly HttpClient http;
    readonly string publicKey;
    public static string DownloadDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CasterDesk","Updates");
    public UpdateClient(HttpClient http,string? publicKey=null)
    {
        this.http=http;
        this.publicKey=publicKey??File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"assets","update-public-key.pem"));
    }
    public static HttpClient CreateHttpClient()=>new(new HttpClientHandler{AllowAutoRedirect=false}){Timeout=TimeSpan.FromMinutes(15)};
    byte[] VerifyEnvelope(byte[] envelope)
    {
        if(envelope.Length>MaxCatalogBytes)throw new InvalidDataException("Lista de versões muito grande.");
        using var json=JsonDocument.Parse(envelope);
        byte[] bytes=Convert.FromBase64String(json.RootElement.GetProperty("Payload").GetString()??""),signature=Convert.FromBase64String(json.RootElement.GetProperty("Signature").GetString()??"");
        using var rsa=RSA.Create();rsa.ImportFromPem(publicKey);
        if(!rsa.VerifyData(bytes,signature,HashAlgorithmName.SHA256,RSASignaturePadding.Pss))throw new InvalidDataException("A assinatura da atualização não foi reconhecida. Nenhum instalador foi executado.");
        return bytes;
    }
    public static void Validate(UpdateManifest manifest)
    {
        var version=AppVersion.Parse(manifest.Version);
        if(manifest.Schema!=1 || manifest.Type is not ("release" or "beta") || manifest.Priority is not ("normal" or "important") || manifest.Notes is null || manifest.Notes.Length>12000 || manifest.Size is <=0 or >MaxInstallerSize || !Regex.IsMatch(manifest.Sha256??"","^[a-fA-F0-9]{64}$") || (manifest.Beta ? version.Pre.FirstOrDefault()!="beta" : version.Pre.Length>0) || manifest.PublishedAt<DateTimeOffset.UnixEpoch || manifest.PublishedAt>DateTimeOffset.UtcNow.AddDays(1))
            throw new InvalidDataException("Informações de versão inválidas.");
    }
    public UpdateManifest ReadManifest(byte[] envelope)
    {
        var manifest=JsonSerializer.Deserialize<UpdateManifest>(VerifyEnvelope(envelope))??throw new InvalidDataException("Aviso de atualização inválido.");
        Validate(manifest);
        return manifest;
    }
    public UpdateManifest[] ReadCatalog(byte[] envelope)
    {
        var catalog=JsonSerializer.Deserialize<UpdateCatalog>(VerifyEnvelope(envelope))??throw new InvalidDataException("Lista de versões inválida.");
        if(catalog.Schema!=1 || catalog.Versions is null || catalog.Versions.Length>200)throw new InvalidDataException("Lista de versões inválida.");
        var versions=new HashSet<string>(StringComparer.Ordinal);
        foreach(var item in catalog.Versions) {
            if(item==null)throw new InvalidDataException("Versão vazia na lista.");
            Validate(item);
            if(!versions.Add(item.Version))throw new InvalidDataException("Versão duplicada na lista.");
        }
        return catalog.Versions.OrderByDescending(v=>AppVersion.Parse(v.Version)).ToArray();
    }
    static void CheckOrigin(Uri uri,bool redirect)
    {
        bool allowed=uri.Scheme=="https"&&uri.IsDefaultPort&&uri.UserInfo.Length==0&&(
            uri.Host=="github.com"&&uri.AbsolutePath.StartsWith("/moska1m/CasterDesk/releases/download/",StringComparison.Ordinal)
            ||redirect&&(uri.Host is "release-assets.githubusercontent.com" or "objects.githubusercontent.com"));
        if(!allowed)throw new InvalidDataException("O endereço da atualização não pertence à distribuição oficial.");
    }
    async Task<HttpResponseMessage> GetAsync(Uri uri,CancellationToken token)
    {
        for(int redirects=0;redirects<6;redirects++) {
            CheckOrigin(uri,redirects>0);
            using var request=new HttpRequestMessage(HttpMethod.Get,uri);
            request.Headers.UserAgent.ParseAdd("CasterDesk/"+BuildInfo.Version);
            request.Headers.CacheControl=new(){NoCache=true};
            var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);
            if((int)response.StatusCode is 301 or 302 or 303 or 307 or 308) {
                var location=response.Headers.Location;response.Dispose();
                if(location==null)throw new InvalidDataException("Redirecionamento de atualização inválido.");
                uri=location.IsAbsoluteUri?location:new Uri(uri,location);continue;
            }
            return response;
        }
        throw new InvalidDataException("Redirecionamentos demais na atualização.");
    }
    public async Task<UpdateManifest[]> ListAsync(CancellationToken token=default)
    {
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(TimeSpan.FromSeconds(25));
        using var response=await GetAsync(new Uri(FeedRoot+"versions.json"),timeout.Token);
        if(response.StatusCode==HttpStatusCode.NotFound)throw new InvalidOperationException("A lista de versões ainda não foi publicada.");
        response.EnsureSuccessStatusCode();
        if(response.Content.Headers.ContentLength>MaxCatalogBytes)throw new InvalidDataException("Lista de versões muito grande.");
        await using var stream=await response.Content.ReadAsStreamAsync(timeout.Token);using var buffer=new MemoryStream();
        byte[] chunk=new byte[8192];int read;
        while((read=await stream.ReadAsync(chunk,timeout.Token))>0){if(buffer.Length+read>MaxCatalogBytes)throw new InvalidDataException("Lista de versões muito grande.");buffer.Write(chunk,0,read);}
        return ReadCatalog(buffer.ToArray());
    }
    public static async Task VerifyFileAsync(string path,UpdateManifest manifest,CancellationToken token=default)
    {
        if(new FileInfo(path).Length!=manifest.Size)throw new InvalidDataException("O instalador está incompleto. Baixe novamente.");
        await using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,81920,true);
        string actual=Convert.ToHexString(await SHA256.HashDataAsync(stream,token));
        if(!string.Equals(actual,manifest.Sha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("A integridade do instalador não confere. Baixe novamente.");
    }
    public async Task<string> DownloadAsync(UpdateManifest manifest,IProgress<double>? progress=null,CancellationToken token=default,string? directory=null)
    {
        Validate(manifest);
        string root=Path.GetFullPath(directory??DownloadDirectory);Directory.CreateDirectory(root);
        string target=Path.Combine(root,manifest.FileName);
        if(File.Exists(target)) { try{await VerifyFileAsync(target,manifest,token);progress?.Report(1);return target;}catch(InvalidDataException){} }
        using var response=await GetAsync(manifest.InstallerUri,token);response.EnsureSuccessStatusCode();
        if(response.Content.Headers.ContentLength is long size&&size!=manifest.Size)throw new InvalidDataException("Tamanho do download não corresponde à atualização assinada.");
        string temporary=target+".partial-"+Guid.NewGuid().ToString("N");
        try {
            await using(var input=await response.Content.ReadAsStreamAsync(token))
            await using(var output=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None,81920,true)) {
                byte[] buffer=new byte[81920];long total=0;int count;
                while((count=await input.ReadAsync(buffer,token))>0) {
                    total+=count;if(total>manifest.Size)throw new InvalidDataException("Download maior que a atualização assinada.");
                    await output.WriteAsync(buffer.AsMemory(0,count),token);progress?.Report((double)total/manifest.Size);
                }
            }
            await VerifyFileAsync(temporary,manifest,token);File.Move(temporary,target,true);return target;
        } finally {if(File.Exists(temporary))File.Delete(temporary);}
    }
}

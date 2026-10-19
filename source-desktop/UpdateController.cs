using System.Text.Json;

namespace CasterDesk.Desktop;

public sealed class UpdatePreferences
{
    public bool Automatic { get; set; } = true;
    public bool IncludeBetas { get; set; }
    public DateTimeOffset LastCheck { get; set; }
}

public sealed class UpdateController : IDisposable
{
    readonly UpdateClient client;
    readonly CancellationTokenSource lifetime = new();
    UpdateManifest[] catalog=[];
    public UpdatePreferences Preferences { get; }
    public UpdateManifest[] Versions => catalog.Where(v=>Preferences.IncludeBetas||!v.Beta).ToArray();
    public UpdateManifest? Available => Versions.FirstOrDefault(IsNewer);
    public UpdateManifest? Selected { get; private set; }
    public string? Downloaded { get; private set; }
    public bool Checking { get; private set; }
    public bool Downloading { get; private set; }
    public double Progress { get; private set; }
    public string Status { get; private set; } = "Verifique as versões disponíveis do CasterDesk.";
    public event Action? Changed;
    public UpdateController(UpdateClient? client = null)
    {
        this.client = client ?? new UpdateClient(UpdateClient.CreateHttpClient());
        Preferences = ConfigurationStore.Read<UpdatePreferences>("updates.json");
    }
    public static bool IsNewer(UpdateManifest item)=>AppVersion.Parse(item.Version).CompareTo(AppVersion.Parse(BuildInfo.Version))>0;
    public void Select(string version)
    {
        if(Checking||Downloading)return;
        var next=Versions.FirstOrDefault(v=>v.Version==version);
        if(next==Selected)return;
        Selected=next;Downloaded=null;Changed?.Invoke();
    }
    public void Configure(bool automatic,bool includeBetas)
    {
        if(Checking||Downloading)throw new InvalidOperationException("Aguarde a operação de atualização terminar.");
        var next=new UpdatePreferences{Automatic=automatic,IncludeBetas=includeBetas,LastCheck=Preferences.LastCheck};
        MatchFiles.AtomicText(AppPaths.ConfigFile("updates.json"),JsonSerializer.Serialize(next));
        Preferences.Automatic=next.Automatic;Preferences.IncludeBetas=next.IncludeBetas;
        if(Selected==null||!Versions.Contains(Selected)){Selected=Available??Versions.FirstOrDefault();Downloaded=null;}
        Changed?.Invoke();
    }
    public async Task CheckAsync(bool manual)
    {
        if(Checking||Downloading||(!manual&&(!Preferences.Automatic||(catalog.Length>0&&DateTimeOffset.UtcNow-Preferences.LastCheck<TimeSpan.FromHours(6)))))return;
        Checking=true;Status="Verificando versões…";Changed?.Invoke();
        try {
            var next=await client.ListAsync(lifetime.Token);
            catalog=next;
            var selection=Versions.FirstOrDefault(v=>v.Version==Selected?.Version)??Available??Versions.FirstOrDefault();
            if(Selected!=selection)Downloaded=null;
            Selected=selection;Preferences.LastCheck=DateTimeOffset.UtcNow;
            MatchFiles.AtomicText(AppPaths.ConfigFile("updates.json"),JsonSerializer.Serialize(Preferences));
            Status=Available==null?"Você já está na versão mais recente disponível.":Available.Important?"Atualização importante disponível.":"Nova versão disponível.";
        } catch(OperationCanceledException) when(lifetime.IsCancellationRequested) { }
        catch(Exception ex){Status="Não foi possível verificar agora. "+ex.Message;}
        finally {Checking=false;Changed?.Invoke();}
    }
    public async Task DownloadAsync(CancellationToken cancellation)
    {
        var item=Selected;
        if(item==null||!IsNewer(item)||Checking||Downloading)return;
        Downloading=true;Progress=0;Status="Baixando atualização…";Changed?.Invoke();
        try {
            using var combined=CancellationTokenSource.CreateLinkedTokenSource(cancellation,lifetime.Token);
            var progress=new Progress<double>(value=>{Progress=value;Changed?.Invoke();});
            Downloaded=await client.DownloadAsync(item,progress,combined.Token);
            Status="Download conferido. Você pode instalar agora ou continuar usando o programa.";
        } catch(OperationCanceledException){Status="Download cancelado. Você pode tentar novamente.";}
        catch(Exception ex){Status="Não foi possível baixar. "+ex.Message;}
        finally {Downloading=false;Changed?.Invoke();}
    }
    public void Dispose(){lifetime.Cancel();}
}

using System.Drawing.Imaging;
using System.Text.Json;

namespace CronoLive;

// One immutable directory identity keeps OBS paths and screenshot references stable after renaming teams.
public static class MatchFiles
{
    public static string RecoveryNotice{get;private set;}="";
    public static string DirectoryFor(SiegeMatch match)
    {
        if(!Guid.TryParseExact(match.Id,"N",out _))throw new InvalidDataException("Invalid match ID");
        return Path.Combine(MatchStore.Folder,$"{match.Created:yyyy-MM-dd_HH-mm-ss}_{match.Id}");
    }
    public static string Ensure(SiegeMatch match)
    {
        string dir=DirectoryFor(match);Directory.CreateDirectory(dir);
        foreach(string child in new[]{"Imagens","Estatisticas","Recursos"})Directory.CreateDirectory(Path.Combine(dir,child));
        return dir;
    }
    public static void Snapshot(SiegeMatch match)
    {
        lock(FileBatch.Gate){using var batch=new FileBatch();StageSnapshot(batch,match);batch.Commit();}
    }
    public static void StageSnapshot(FileBatch batch,SiegeMatch match)
    {
        string dir=Ensure(match);batch.Text(Path.Combine(dir,"partida.json"),JsonSerializer.Serialize(match,MatchStore.Options));
        batch.Text(Path.Combine(dir,"LEIA-ME.txt"),$"CasterDesk\r\n{match.TeamA} × {match.TeamB}\r\n{match.Created:g} · {match.Format}\r\n\r\npartida.json: dados da partida\r\nImagens: artes geradas\r\nEstatisticas: prints usados na conferência\r\nRecursos: logos e fundos personalizados\r\n");
    }
    public static void SaveStatistics(SiegeMatch match,MapRecord map,MapStatistics data,Image? print,bool screenshotChanged=true)
    {
        StatisticsData.Identify(match,data);StatisticsData.Validate(data);var previous=map.Statistics;
        lock(FileBatch.Gate)
        {
            using var batch=new FileBatch();
            if(data.Status=="missing")data.Screenshot="";
            else if(!screenshotChanged&&previous is not null&&Resolve(match,previous.Screenshot) is not null)data.Screenshot=previous.Screenshot;
            else if(print!=null){data.Screenshot="Estatisticas/"+map.MapId+"-"+Guid.NewGuid().ToString("N")+".png";batch.Add(Path.Combine(Ensure(match),data.Screenshot),p=>OptimizedPng.Save(print,p));}
            map.Statistics=data;try{MatchStore.Stage(batch,match);batch.Commit();}catch{map.Statistics=previous;throw;}
        }
    }
    public static void AtomicText(string path,string contents)
    {
        lock(FileBatch.Gate)
        {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);string tmp=path+".tmp-"+Guid.NewGuid().ToString("N");
        try{File.WriteAllText(tmp,contents);File.Move(tmp,path,true);}finally{if(File.Exists(tmp))File.Delete(tmp);}
        }
    }
    public static void AtomicImage(string path,Image image)
    {
        lock(FileBatch.Gate)
        {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);string tmp=path+".tmp-"+Guid.NewGuid().ToString("N");
        try{OptimizedPng.Save(image,tmp);File.Move(tmp,path,true);}finally{if(File.Exists(tmp))File.Delete(tmp);}
        }
    }
    public static string Resource(SiegeMatch match,string kind,string source)
    {
        if(new FileInfo(source).Length>40_000_000)throw new InvalidDataException(L.T("Use uma imagem com até 40 MB."));
        using var image=Image.FromFile(source);if((long)image.Width*image.Height>50_000_000)throw new InvalidDataException(L.T("A imagem é muito grande."));
        float scale=Math.Min(1,(kind.StartsWith("fundo")?2560f:768f)/Math.Max(image.Width,image.Height));
        using var bitmap=new Bitmap(Math.Max(1,(int)(image.Width*scale)),Math.Max(1,(int)(image.Height*scale)));
        using(var g=Graphics.FromImage(bitmap)){g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.DrawImage(image,0,0,bitmap.Width,bitmap.Height);}
        string relative=Path.Combine("Recursos",kind+".png");AtomicImage(Path.Combine(Ensure(match),relative),bitmap);return relative;
    }
    public static string? Resolve(SiegeMatch match,string? relative)
    {
        if(string.IsNullOrWhiteSpace(relative))return null;
        string root=Path.GetFullPath(DirectoryFor(match))+Path.DirectorySeparatorChar;
        string path=Path.GetFullPath(Path.Combine(root,relative));
        return path.StartsWith(root,StringComparison.OrdinalIgnoreCase)&&File.Exists(path)?path:null;
    }
    public static ImageBrand ReadBrand(SiegeMatch match)
    {
        string file=Path.Combine(DirectoryFor(match),"arte.json");
        try{if(File.Exists(file)){var brand=JsonSerializer.Deserialize<ImageBrand>(File.ReadAllText(file))??throw new InvalidDataException("Empty artwork preferences");brand.ArtSides??=new();brand.EventName??="";brand.BroadcastChannel??="";brand.TeamALogo??="";brand.TeamBLogo??="";brand.Background??="";brand.Mvp??="";brand.ArtFormat??="";return brand;}}
        catch(Exception ex) when(ex is JsonException or InvalidDataException)
        {
            string preserved=file+".invalid-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N");File.Copy(file,preserved);RecoveryNotice=L.T("Preferências da arte recuperadas. O arquivo inválido foi preservado.");AppLog.Write("ART CONFIG RECOVERY "+file+": "+ex.Message);
        }
        {
            try{var legacy=JsonSerializer.Deserialize<Dictionary<string,ExportBrand>>(File.ReadAllText(AppPaths.ConfigFile("export-settings.json")));return new(){EventName=legacy?.GetValueOrDefault(match.Id)?.EventName??""};}catch{return new();}
        }
    }
    public static void SaveBrand(SiegeMatch match,ImageBrand brand)=>AtomicText(Path.Combine(Ensure(match),"arte.json"),JsonSerializer.Serialize(brand,MatchStore.Options));
    public static string ObsVeto=>Path.Combine(AppPaths.Ensure("OBS"),"map-veto.png");
    // Artwork edits live in arte.json; the original match is never mutated by rendering.
    public static SiegeMatch ArtworkMatch(SiegeMatch match,SiegeCatalog catalog,ImageBrand brand)
    {
        var copy=JsonSerializer.Deserialize<SiegeMatch>(JsonSerializer.Serialize(match))!;
        if(brand.ArtVeto is null)return copy;
        if(brand.ArtFormat!=match.Format || brand.ArtVeto.Length!=8 || brand.ArtVeto.Distinct().Count()!=8 || brand.ArtVeto.Any(id=>!catalog.Maps.Any(m=>m.Id==id)))
            throw new InvalidDataException(L.T("Confira as correções do veto da arte ou restaure o veto da partida."));
        copy.Veto=brand.ArtVeto.ToArray();copy.SyncMaps(catalog);
        foreach(var map in copy.Maps)
            if(brand.ArtSides.TryGetValue(map.MapId,out int attacker)){if(attacker is <0 or >1)throw new InvalidDataException(L.T("Confira as correções do veto da arte ou restaure o veto da partida."));map.InitialAttack=attacker;}
        return copy;
    }
    public static string Export(SiegeMatch match,SiegeCatalog catalog,ImageBrand brand,ArtKind kind,ExportAspect aspect,MapRecord? map)
    {
        var artwork=kind==ArtKind.MapVeto?ArtworkMatch(match,catalog,brand):match;
        var pages=StudioRenderer.Render(artwork,catalog,brand,kind,aspect,map);
        try { return ExportRendered(match,brand,kind,pages); }
        finally{foreach(var page in pages)page.Dispose();}
    }
    // Caller owns the full-resolution preview pages. Encoding retains the same
    // file transaction and fixed OBS-path publication order as a fresh render.
    public static string ExportRendered(SiegeMatch match,ImageBrand brand,ArtKind kind,IReadOnlyList<ResultPage> pages)
    {
            if(pages.Count==0||pages.Select(p=>p.Name).Distinct().Count()!=pages.Count||pages.Any(p=>string.IsNullOrWhiteSpace(p.Name)||Path.GetFileName(p.Name)!=p.Name||p.Name.IndexOfAny(Path.GetInvalidFileNameChars())>=0))throw new InvalidDataException("Invalid rendered pages.");
            string dir=Ensure(match);using var batch=new FileBatch();batch.Text(Path.Combine(dir,"arte.json"),JsonSerializer.Serialize(brand,MatchStore.Options));StageSnapshot(batch,match);
            foreach(var page in pages)batch.Add(Path.Combine(dir,"Imagens",page.Name+".png"),p=>OptimizedPng.Save(page.Image,p));
            // Publish the fixed path only after all archive copies succeeded.
            if(kind==ArtKind.MapVeto)batch.Add(ObsVeto,p=>OptimizedPng.Save(pages.Single(page=>page.Name=="map-veto-obs").Image,p));
            batch.Text(Path.Combine(dir,"ultima-exportacao.json"),JsonSerializer.Serialize(new{Generated=DateTimeOffset.Now,Kind=kind.ToString(),Files=pages.Select(p=>"Imagens/"+p.Name+".png").ToArray()},MatchStore.Options));batch.Commit();
            return dir;
    }
}
public sealed class ImageBrand
{
    public string[]? ArtVeto{get;set;}
    public string ArtFormat{get;set;}="";
    public Dictionary<string,int> ArtSides{get;set;}=new();
    public string EventName{get;set;}="";
    public string BroadcastChannel{get;set;}="";
    public DateTime? ScheduledAt{get;set;}
    public string TeamALogo{get;set;}="";
    public string TeamBLogo{get;set;}="";
    public string Background{get;set;}="";
    public Dictionary<string,ArtStyle> ArtStyles{get;set;}=new();
    public ArtStyle Style(ArtKind kind) {
        ArtStyles ??= new();
        string key=(kind==ArtKind.Results?ArtKind.SeriesStats:kind).ToString();
        if(!ArtStyles.TryGetValue(key,out var style)||style is null)ArtStyles[key]=style=new();
        return style;
    }
    // Legacy direct renderer callers remain compatible; the UI migrates the old shared background.
    public string BackgroundFor(ArtKind kind)=>ArtStyles?.GetValueOrDefault((kind==ArtKind.Results?ArtKind.SeriesStats:kind).ToString())?.Background ?? Background;
    public Color AccentFor(ArtKind kind) {
        string hex=Style(kind).Accent??"";
        return System.Text.RegularExpressions.Regex.IsMatch(hex, "^#[0-9A-Fa-f]{6}$")?ColorTranslator.FromHtml(hex):Color.FromArgb(255,57,39);
    }
    public string Mvp{get;set;}="";
}

public sealed class ArtStyle { public string? Background{get;set;} public string Accent{get;set;}="#FF3927"; }

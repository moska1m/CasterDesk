using System.Drawing;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CasterDesk.Core;

public sealed class BrandPackage
{
    public int Schema { get; set; } = 1;
    public string Name { get; set; } = "";
    public string EventName { get; set; } = "";
    public Dictionary<string, ArtStyle> Styles { get; set; } = new();
    static readonly ArtKind[] Kinds = [ArtKind.Announcement, ArtKind.MapVeto, ArtKind.MapStats, ArtKind.SeriesStats];
    static readonly string[] Formats = ["default", "feed", "stories", "horizontal"];

    public static readonly (string File, ArtKind Kind, string Format)[] DesignerFiles = [
        ("anuncio-feed.png",ArtKind.Announcement,"feed"),("anuncio-stories.png",ArtKind.Announcement,"stories"),
        ("map-veto-horizontal.png",ArtKind.MapVeto,"horizontal"),
        ("estatisticas-mapa-feed.png",ArtKind.MapStats,"feed"),("estatisticas-mapa-stories.png",ArtKind.MapStats,"stories"),("estatisticas-mapa-horizontal.png",ArtKind.MapStats,"horizontal"),
        ("estatisticas-serie-feed.png",ArtKind.SeriesStats,"feed"),("estatisticas-serie-stories.png",ArtKind.SeriesStats,"stories"),("estatisticas-serie-horizontal.png",ArtKind.SeriesStats,"horizontal")];
    public static void CreateFromFolder(string folder,string destination,string name,string accent)
    {
        if(!Directory.Exists(folder)||(File.GetAttributes(folder)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Escolha uma pasta local de imagens.");
        var package=new BrandPackage{Name=name,EventName=name};foreach(var kind in Kinds)package.Styles[kind.ToString()]=new(){Accent=accent};Validate(package);
        string temporary=destination+".tmp-"+Guid.NewGuid().ToString("N");int count=0;
        try {
            using(var zip=ZipFile.Open(temporary,ZipArchiveMode.Create)) {
                foreach(var item in DesignerFiles) {
                    string source=Path.Combine(folder,item.File);if(!File.Exists(source))continue;
                    string entry=$"backgrounds/{item.Kind}/{item.Format}.png";AddBackground(zip,entry,source);
                    package.Styles[item.Kind.ToString()].Backgrounds[item.Format]=entry;count++;
                }
                if(count==0)throw new InvalidDataException("Nenhum fundo encontrado com os nomes do modelo. Consulte o guia de pacotes visuais.");
                using var writer=new StreamWriter(zip.CreateEntry("theme.json").Open());writer.Write(JsonSerializer.Serialize(package,MatchStore.Options));
            }
            Complete(temporary,destination);
        }finally{if(File.Exists(temporary))File.Delete(temporary);}
    }

    public static void Export(SiegeMatch match, ImageBrand current, string destination)
    {
        var package = new BrandPackage { Name = current.EventName, EventName = current.EventName };
        string temporary = destination + ".tmp-" + Guid.NewGuid().ToString("N");
        try {
            using (var zip = ZipFile.Open(temporary, ZipArchiveMode.Create)) {
                foreach (var kind in Kinds) {
                    var source = current.ArtStyles?.GetValueOrDefault(kind.ToString()) ?? new();
                    var style = new ArtStyle { Accent = source.Accent };
                    var backgrounds = new Dictionary<string,string>(source.Backgrounds ?? new());
                    if (!string.IsNullOrEmpty(source.Background)) backgrounds["default"] = source.Background;
                    foreach (var (format, relative) in backgrounds) {
                        if (string.IsNullOrWhiteSpace(relative)) { style.Backgrounds[format]=""; continue; }
                        string file = MatchFiles.Resolve(match,relative) ?? throw new IOException("Um fundo personalizado não foi encontrado. Carregue-o novamente antes de exportar.");
                        string entry = $"backgrounds/{kind}/{format}.png";
                        AddBackground(zip,entry,file); style.Backgrounds[format] = entry;
                    }
                    package.Styles[kind.ToString()] = style;
                }
                Validate(package);
                using var writer = new StreamWriter(zip.CreateEntry("theme.json").Open()); writer.Write(JsonSerializer.Serialize(package,MatchStore.Options));
            }
            Complete(temporary,destination);
        } finally { if(File.Exists(temporary))File.Delete(temporary); }
    }
    static void Complete(string temporary,string destination)
    {
        if(new FileInfo(temporary).Length>160_000_000)throw new InvalidDataException("Pacote maior que 160 MB.");
        using(var zip=ZipFile.OpenRead(temporary))if(zip.Entries.Sum(e=>e.Length)>160_000_000||zip.Entries.Any(e=>e.Length>40_000_000))throw new InvalidDataException("As imagens excedem o limite do pacote.");
        File.Move(temporary,destination,true);
    }
    static void AddBackground(ZipArchive zip,string entry,string source)
    {
        var info=new FileInfo(source);
        if((info.Attributes&FileAttributes.ReparsePoint)!=0||info.Length>40_000_000)throw new InvalidDataException("Imagem inválida ou maior que 40 MB: "+info.Name);
        using var image=Image.FromFile(source);if((long)image.Width*image.Height>50_000_000)throw new InvalidDataException("Imagem grande demais: "+info.Name);
        using var bitmap=new Bitmap(image);using var buffer=new MemoryStream();bitmap.Save(buffer,System.Drawing.Imaging.ImageFormat.Png);
        if(buffer.Length>40_000_000)throw new InvalidDataException("Imagem maior que o limite: "+info.Name);
        buffer.Position=0;using var output=zip.CreateEntry(entry,CompressionLevel.Fastest).Open();buffer.CopyTo(output);
    }
    static void Validate(BrandPackage package)
    {
        if(package.Schema!=1 || package.EventName==null || package.EventName.Length>80 || package.Name==null || package.Name.Length>100 || package.Styles==null || package.Styles.Count!=4 || Kinds.Any(k=>!package.Styles.ContainsKey(k.ToString())))throw new InvalidDataException("Pacote visual inválido ou de versão incompatível.");
        foreach(var style in package.Styles.Values) {
            if(style==null || !Regex.IsMatch(style.Accent??"","^#[0-9A-Fa-f]{6}$") || !string.IsNullOrEmpty(style.Background) || style.Backgrounds==null || style.Backgrounds.Count>4 || style.Backgrounds.Any(p=>!Formats.Contains(p.Key)||p.Value==null))throw new InvalidDataException("Confira as cores e os fundos do pacote visual.");
        }
    }
    public static ImageBrand Import(SiegeMatch match,ImageBrand current,string source)
    {
        if(new FileInfo(source).Length>160_000_000)throw new InvalidDataException("Use um pacote visual com até 160 MB.");
        using var zip=ZipFile.OpenRead(source);
        if(zip.Entries.Count is <1 or >65 || zip.Entries.Sum(e=>e.Length)>160_000_000)throw new InvalidDataException("Pacote visual maior que o limite.");
        var entries=new Dictionary<string,ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        foreach(var entry in zip.Entries) {
            string path=entry.FullName;
            if(path.Length>180||path.Contains('\\')||path.Contains(':')||path.StartsWith('/')||path.Split('/').Any(s=>s is "" or "." or "..")||((entry.ExternalAttributes>>16)&0xF000)==0xA000||entry.Length>40_000_000||!entries.TryAdd(path,entry))throw new InvalidDataException("Caminho ou arquivo inválido no pacote visual.");
        }
        if(!entries.TryGetValue("theme.json",out var manifest)||manifest.Length>64_000)throw new InvalidDataException("O pacote precisa conter theme.json.");
        BrandPackage package;
        using(var reader=new StreamReader(manifest.Open()))package=JsonSerializer.Deserialize<BrandPackage>(reader.ReadToEnd(),MatchStore.Options)??throw new InvalidDataException("Pacote vazio.");
        Validate(package);
        var references=package.Styles.Values.SelectMany(s=>s.Backgrounds.Values).Where(p=>p!="").Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if(entries.Keys.Any(p=>p!="theme.json"&&!references.Contains(p,StringComparer.OrdinalIgnoreCase))||references.Any(p=>!p.StartsWith("backgrounds/",StringComparison.Ordinal)||!p.EndsWith(".png",StringComparison.OrdinalIgnoreCase)||!entries.ContainsKey(p)))throw new InvalidDataException("Os fundos declarados não correspondem aos arquivos do pacote.");
        var result=JsonSerializer.Deserialize<ImageBrand>(JsonSerializer.Serialize(current))!;
        result.ArtStyles??=new();result.Background="";
        string folder="Recursos/Campeonatos/"+Guid.NewGuid().ToString("N");
        lock(FileBatch.Gate) {
            using var batch=new FileBatch();
            batch.Text(Path.Combine(MatchFiles.Ensure(match),folder,"anterior.json"),JsonSerializer.Serialize(current,MatchStore.Options));
            foreach(var kind in Kinds) {
                var style=package.Styles[kind.ToString()]; var updated=new ArtStyle { Accent=style.Accent,Background="" };
                foreach(var (format,reference) in style.Backgrounds) {
                    if(reference==""){updated.Backgrounds[format]="";continue;}
                    string relative=$"{folder}/Fundos/{kind}/{format}.png";var entry=entries[reference];
                    batch.Add(Path.Combine(MatchFiles.Ensure(match),relative),target=>{
                        using var stream=entry.Open();using var memory=new MemoryStream();var buffer=new byte[81920];int read;long total=0;
                        while((read=stream.Read(buffer))>0){total+=read;if(total>40_000_000)throw new InvalidDataException("Fundo maior que o limite.");memory.Write(buffer,0,read);}
                        memory.Position=0;using var image=Image.FromStream(memory,true,true);
                        if((long)image.Width*image.Height>50_000_000)throw new InvalidDataException("Dimensões do fundo maiores que o limite.");
                        using var bitmap=new Bitmap(image);OptimizedPng.Save(bitmap,target);
                    });
                    if(format=="default")updated.Background=relative;else updated.Backgrounds[format]=relative;
                }
                result.ArtStyles[kind.ToString()]=updated;
            }
            result.EventName=package.EventName;
            batch.Text(Path.Combine(MatchFiles.Ensure(match),"arte.json"),JsonSerializer.Serialize(result,MatchStore.Options));batch.Commit();
        }
        return result;
    }
}

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace CasterDesk.Core;

public sealed record BackupEntry(string Path,long Length,string Hash);
public sealed record BackupManifest(int Version,string Root,List<BackupEntry> Files);
public sealed record BackupProgress(int Files,long Bytes,string Current);
public static class DataBackup
{
    static string Hash(string path){using var stream=File.OpenRead(path);return Convert.ToHexString(SHA256.HashData(stream));}
    public static string SafePath(string root,string relative)
    {
        if(string.IsNullOrWhiteSpace(relative)||relative.Contains(':')||relative.Contains('\\')||relative.Split('/').Any(p=>p is "" or "." or ".." || p.EndsWith('.') || p.EndsWith(' ')))throw new InvalidDataException(L.T("Caminho inválido no backup."));
        string path=Path.GetFullPath(Path.Combine(root,relative));
        if(!path.StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException(L.T("Caminho fora da pasta do backup."));
        for(string? current=path;current is not null && current.Length>=Path.GetFullPath(root).Length;current=Path.GetDirectoryName(current))
            if((File.Exists(current)||Directory.Exists(current)) && (File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException(L.T("Links não são permitidos no backup."));
        return path;
    }
    static IEnumerable<string> Files(string root)
    {
        foreach(string file in Directory.GetFiles(root))if((File.GetAttributes(file)&FileAttributes.ReparsePoint)==0 && !file.EndsWith(".tmp",StringComparison.OrdinalIgnoreCase)&&!Path.GetFileName(file).Contains(".tmp-",StringComparison.OrdinalIgnoreCase))yield return file;
        foreach(string directory in Directory.GetDirectories(root))
            if((File.GetAttributes(directory)&FileAttributes.ReparsePoint)==0 && !string.Equals(Path.GetFileName(directory),"Backups",StringComparison.OrdinalIgnoreCase))foreach(string file in Files(directory))yield return file;
    }
    public static void Create(string root,string destination,Action<BackupProgress>? progress=null)
    {lock(FileBatch.Gate)CreateSnapshot(root,destination,progress);}
    static void CreateSnapshot(string root,string destination,Action<BackupProgress>? progress)
    {
        string temp=destination+".tmp";
        try
        {
            var entries=new List<BackupEntry>();long totalSize=0;
            using(var zip=ZipFile.Open(temp,ZipArchiveMode.Create))
            {
                foreach(string file in Files(root))
                {
                    if(string.Equals(Path.GetFullPath(file),Path.GetFullPath(destination),StringComparison.OrdinalIgnoreCase)||string.Equals(Path.GetFileName(file),"pending-restore.json",StringComparison.OrdinalIgnoreCase))continue;
                    string relative=Path.GetRelativePath(root,file).Replace('\\','/');
                    var entry=zip.CreateEntry("data/"+relative,CompressionLevel.Fastest);
                    // Hash exactly the bytes written, even for TXT updated by other programs.
                    using var source=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read);
                    using var target=entry.Open();using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                    byte[] buffer=new byte[81920];int read;long size=0;
                    while((read=source.Read(buffer))>0){target.Write(buffer,0,read);hash.AppendData(buffer,0,read);size+=read;totalSize+=read;if(totalSize>8L*1024*1024*1024)throw new InvalidDataException(L.T("Limite de backup: 8 GB."));}
                    entries.Add(new(relative,size,Convert.ToHexString(hash.GetHashAndReset())));
                    progress?.Invoke(new(entries.Count,totalSize,relative));
                    if(entries.Count>49999)throw new InvalidDataException(L.T("Limite de backup: 49.999 arquivos."));
                }
                using var writer=new StreamWriter(zip.CreateEntry("manifest.json").Open());writer.Write(JsonSerializer.Serialize(new BackupManifest(1,root,entries)));
            }
            File.Move(temp,destination,true);
        }
        catch{if(File.Exists(temp))File.Delete(temp);throw;}
    }
    public static BackupManifest Extract(string archive,string staging)
    {
        using var zip=ZipFile.OpenRead(archive);
        if(zip.Entries.Count>50000)throw new InvalidDataException(L.T("Backup tem arquivos demais."));
        var metadata=zip.GetEntry("manifest.json")??throw new InvalidDataException(L.T("Este ZIP não é um backup do CasterDesk."));
        if(metadata.Length>16_000_000)throw new InvalidDataException(L.T("Manifesto muito grande."));
        using var reader=metadata.Open();var manifest=JsonSerializer.Deserialize<BackupManifest>(reader)??throw new InvalidDataException(L.T("Manifesto inválido."));
        if(manifest.Version!=1 || manifest.Files is null || manifest.Files.Count>49999 || manifest.Files.Any(f=>f.Length<0) || manifest.Files.Sum(f=>f.Length)>8L*1024*1024*1024)throw new InvalidDataException(L.T("Versão ou tamanho do backup não suportado (limite de 8 GB)."));
        if(manifest.Files.Select(f=>f.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=manifest.Files.Count || zip.Entries.Select(e=>e.FullName).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=zip.Entries.Count || zip.Entries.Count!=manifest.Files.Count+1)throw new InvalidDataException(L.T("Arquivos duplicados ou não listados no backup."));
        foreach(var file in manifest.Files)
        {
            if(file.Path.StartsWith("Backups/",StringComparison.OrdinalIgnoreCase)||string.Equals(file.Path,"pending-restore.json",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException(L.T("Arquivo reservado."));
            string path=SafePath(staging,file.Path);var entry=zip.GetEntry("data/"+file.Path)??throw new InvalidDataException(L.T("Arquivo ausente."));
            if(entry.Length!=file.Length)throw new InvalidDataException(L.T("Tamanho divergente."));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using(var input=entry.Open())using(var output=new FileStream(path,FileMode.CreateNew)){byte[] buffer=new byte[81920];long total=0;int n;while((n=input.Read(buffer))>0){total+=n;if(total>file.Length)throw new InvalidDataException(L.T("Tamanho excedido."));output.Write(buffer,0,n);}}
            if(Hash(path)!=file.Hash)throw new InvalidDataException(L.T("Backup corrompido: ")+file.Path);
            if(file.Path.EndsWith(".json",StringComparison.OrdinalIgnoreCase))ValidateData(path,file.Path);
        }
        return manifest;
    }
    static void ValidateData(string path,string relative)
    {
        using var parsed=JsonDocument.Parse(File.ReadAllText(path));
        if(relative.StartsWith("Partidas/",StringComparison.OrdinalIgnoreCase)&&(Path.GetFileName(path)=="partida.json"||Guid.TryParseExact(Path.GetFileNameWithoutExtension(path),"N",out _)))MatchStore.Read(path,SiegeCatalog.Load());
        if(string.Equals(relative,"Configuracoes/settings.json",StringComparison.OrdinalIgnoreCase))
        {var settings=JsonSerializer.Deserialize<Settings>(File.ReadAllText(path))??throw new InvalidDataException(L.T("Configuração inválida."));if(settings.Initial<0||settings.Initial>1440||!L.Languages.Contains(settings.Language))throw new InvalidDataException(L.T("Configuração inválida."));}
        if(string.Equals(relative,"Configuracoes/casting-settings.json",StringComparison.OrdinalIgnoreCase))
        {var casting=JsonSerializer.Deserialize<CastingSettings>(File.ReadAllText(path))??throw new InvalidDataException(L.T("Configuração inválida."));if(casting.Folder is null||casting.People is null||casting.People.Length!=4||casting.People.Any(p=>p==null||p.Name==null||p.Handle==null))throw new InvalidDataException(L.T("Configuração inválida."));}
    }
    public static void RemoveStage(string path)
    {
        string root=Path.GetFullPath(AppPaths.BackupDirectory)+Path.DirectorySeparatorChar;string full=Path.GetFullPath(path);string name=Path.GetFileName(full);
        if(!full.StartsWith(root,StringComparison.OrdinalIgnoreCase)||!(name.StartsWith("verificacao-")||name.StartsWith("restauracao-"))||!Guid.TryParseExact(name[(name.IndexOf('-')+1)..],"N",out _))throw new InvalidDataException(L.T("Invalid staging directory"));
        SafePath(AppPaths.BackupDirectory,Path.GetRelativePath(AppPaths.BackupDirectory,full).Replace('\\','/'));if(Directory.Exists(full))Directory.Delete(full,true);
    }
    public static int Conflicts(string root,BackupManifest manifest)=>manifest.Files.Count(f=>File.Exists(SafePath(root,f.Path))&&Hash(SafePath(root,f.Path))!=f.Hash);
    public static void Apply(string root,string staging,BackupManifest manifest,bool replace){lock(FileBatch.Gate)ApplySnapshot(root,staging,manifest,replace);}
    static void ApplySnapshot(string root,string staging,BackupManifest manifest,bool replace)
    {
        // Validate destinations before changing anything; keep a complete recovery ZIP.
        foreach(var f in manifest.Files)SafePath(root,f.Path);
        string backups=Path.Combine(root,"Backups");Directory.CreateDirectory(backups);
        Create(root,Path.Combine(backups,$"Antes-da-restauracao_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.zip"));
        string undo=Path.Combine(staging,".undo-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(undo);
        var written=new List<(string Path,string? Old)>();
        try
        {
            foreach(var file in manifest.Files)
            {
                string target=SafePath(root,file.Path);
                if(File.Exists(target)&&!replace)continue;
                string source=SafePath(staging,file.Path);
                string? oldFile=null;if(File.Exists(target)){oldFile=Path.Combine(undo,written.Count.ToString());File.Copy(target,oldFile);}
                
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                // Rebase paths inside the original data folder; preserve external OBS paths.
                if(file.Path.StartsWith("Configuracoes/") && file.Path.EndsWith(".json"))
                {
                    string text=File.ReadAllText(source);
                    string old=JsonSerializer.Serialize(manifest.Root).Trim('"'),next=JsonSerializer.Serialize(root).Trim('"');
                    text=text.Replace(old+"\\\\",next+"\\\\",StringComparison.OrdinalIgnoreCase);
                    File.WriteAllText(target+".tmp",text);
                }
                else File.Copy(source,target+".tmp",true);
                File.Move(target+".tmp",target,true);written.Add((target,oldFile));
            }
        }
        catch
        {
            foreach(var file in written.AsEnumerable().Reverse()){if(file.Old is null){if(File.Exists(file.Path))File.Delete(file.Path);}else File.Copy(file.Old,file.Path,true);}
            throw;
        }
    }
    public static void ProcessPending(Action<string>? notify=null)
    {
        string pending=Path.Combine(AppPaths.DataDirectory,"pending-restore.json");if(!File.Exists(pending))return;
        try
        {
            using var doc=JsonDocument.Parse(File.ReadAllText(pending));
            string archive=doc.RootElement.GetProperty("Archive").GetString()!;
            string staging=Path.Combine(AppPaths.BackupDirectory,"restauracao-"+Guid.NewGuid().ToString("N"));
            var manifest=Extract(archive,staging);Apply(AppPaths.DataDirectory,staging,manifest,doc.RootElement.GetProperty("Replace").GetBoolean());File.Delete(pending);
            RemoveStage(staging);notify?.Invoke(L.T("Backup restaurado. Confira os caminhos dos TXT no OBS, especialmente se este for outro computador."));
        }
        catch(Exception ex){File.Move(pending,pending+".failed",true);notify?.Invoke(L.T("A restauração não foi concluída. Seus dados e a cópia de segurança foram preservados.\n")+ex.Message);}
    }
}

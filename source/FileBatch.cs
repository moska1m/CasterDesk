using System.Text.Json;
namespace CasterDesk.Core;

// Commits a group as one recoverable operation. Prepared files and rollback copies
// are retained after an interrupted process; startup recovers before loading matches.
public sealed class FileBatch : IDisposable
{
    public static readonly object Gate=new();
    readonly string directory;
    readonly List<Entry> entries=[];
    bool retain;
    public sealed record Entry(string Target,string Prepared,string? Previous);
    public FileBatch(){directory=Path.Combine(AppPaths.BackupDirectory,"transactions",Guid.NewGuid().ToString("N"));DataBackup.SafePath(AppPaths.DataDirectory,Path.GetRelativePath(AppPaths.DataDirectory,directory).Replace('\\','/'));Directory.CreateDirectory(directory);}
    public void Add(string target,Action<string> write)
    {
        target=Path.GetFullPath(target);CheckTarget(target);
        if(entries.Any(e=>string.Equals(e.Target,target,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Duplicate transaction target");
        string prepared=Path.Combine(directory,entries.Count+".new");write(prepared);entries.Add(new(target,prepared,null));
    }
    public void Text(string target,string value)=>Add(target,p=>File.WriteAllText(p,value));
    static void CheckTarget(string target)
    {
        string root=Path.GetFullPath(AppPaths.DataDirectory)+Path.DirectorySeparatorChar;
        if(!target.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Transaction outside data directory");
        DataBackup.SafePath(AppPaths.DataDirectory,Path.GetRelativePath(AppPaths.DataDirectory,target).Replace('\\','/'));
    }
    public void Commit()
    {
        lock(Gate)
        {
            for(int i=0;i<entries.Count;i++)
            {
                var e=entries[i];if(Directory.Exists(e.Target))throw new IOException("A destination is a directory: "+e.Target);
                if(File.Exists(e.Target)){string old=Path.Combine(directory,i+".old");File.Copy(e.Target,old);entries[i]=e with{Previous=old};}
            }
            File.WriteAllText(Path.Combine(directory,"journal.json"),JsonSerializer.Serialize(entries));retain=true;
            var written=new List<Entry>();try
            {
                foreach(var e in entries){Directory.CreateDirectory(Path.GetDirectoryName(e.Target)!);File.Move(e.Prepared,e.Target,true);written.Add(e);}
                File.WriteAllText(Path.Combine(directory,"committed"),"ok");retain=false;
            }
            catch
            {
                Rollback(written);retain=false;throw;
            }
        }
    }
    static void Rollback(IEnumerable<Entry> changes)
    {
        foreach(var e in changes.Reverse())
        {
            CheckTarget(e.Target);
            if(e.Previous!=null)File.Copy(e.Previous,e.Target,true);
            else if(File.Exists(e.Target))File.Delete(e.Target);
        }
    }
    public static void Recover()
    {
        string root=Path.Combine(AppPaths.BackupDirectory,"transactions");if(!Directory.Exists(root))return;
        foreach(string folder in Directory.GetDirectories(root))
        {
            if(!Guid.TryParseExact(Path.GetFileName(folder),"N",out _))continue;
            if((File.GetAttributes(folder)&FileAttributes.ReparsePoint)!=0)throw new IOException("Invalid transaction directory");
            string journal=Path.Combine(folder,"journal.json");
            if(File.Exists(journal)&&!File.Exists(Path.Combine(folder,"committed")))
            {
                var changes=JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(journal))!;
                foreach(var e in changes)if(e.Previous!=null){if(!Path.GetFullPath(e.Previous).StartsWith(Path.GetFullPath(folder)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!File.Exists(e.Previous))throw new IOException("Incomplete recovery journal");DataBackup.SafePath(folder,Path.GetRelativePath(folder,e.Previous).Replace('\\','/'));}
                Rollback(changes);AppLog.Write("Recovered transaction "+Path.GetFileName(folder));
            }
            Directory.Delete(folder,true);
        }
    }
    public void Dispose(){if(!retain&&Directory.Exists(directory))Directory.Delete(directory,true);}
}

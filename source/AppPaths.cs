namespace CasterDesk.Core;

// Installed binaries are read-only; Documents contains portable user data.
public static class AppPaths
{
    public static string DataDirectory { get; } = Resolve();
    public static string ConfigDirectory => Ensure("Configuracoes");
    public static string TextDirectory => Ensure("Textos");
    public static string ImageDirectory => Ensure("Imagens");
    public static string BackupDirectory => Ensure("Backups");
    public static string Ensure(string name) { string path=Path.Combine(DataDirectory,name);Directory.CreateDirectory(path);return path; }
    public static string ConfigFile(string name)=>Path.Combine(ConfigDirectory,name);
    static string Resolve()
    {
        string? custom = Environment.GetEnvironmentVariable("CASTERDESK_DATA_DIR");
        string path = string.IsNullOrWhiteSpace(custom)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CasterDesk")
            : Path.GetFullPath(custom);
        Directory.CreateDirectory(path);
        if(string.IsNullOrWhiteSpace(custom))
        {
            string legacy=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CasterDesk");
            string marker=Path.Combine(path,".migration-v1");
            if(!File.Exists(marker))
            {
                if(Directory.Exists(legacy))CopyMissing(legacy,path);
                File.WriteAllText(marker,"CasterDesk data folder v1");
            }
        }
        foreach(string name in new[]{"Configuracoes","Textos","Imagens","Partidas","Backups"})Directory.CreateDirectory(Path.Combine(path,name));
        ArrangeConfigs(path);
        return path;
    }
    internal static void ArrangeConfigs(string path)
    {
        Directory.CreateDirectory(Path.Combine(path,"Configuracoes"));
        foreach(string file in new[]{"settings.json","casting-settings.json","export-settings.json","promo-settings.json"})
        {
            string old=Path.Combine(path,file),target=Path.Combine(path,"Configuracoes",file);
            if(File.Exists(old)&&!File.Exists(target))File.Copy(old,target);
        }
    }
    internal static void CopyMissing(string source,string destination)
    {
        if((File.GetAttributes(source)&FileAttributes.ReparsePoint)!=0)return;
        Directory.CreateDirectory(destination);
        foreach(string file in Directory.GetFiles(source))
            if((File.GetAttributes(file)&FileAttributes.ReparsePoint)==0 && !File.Exists(Path.Combine(destination,Path.GetFileName(file))))File.Copy(file,Path.Combine(destination,Path.GetFileName(file)));
        foreach(string folder in Directory.GetDirectories(source))
            if(!new[]{"Auth","ReleaseKeys","Updates"}.Contains(Path.GetFileName(folder),StringComparer.OrdinalIgnoreCase))
                CopyMissing(folder,Path.Combine(destination,Path.GetFileName(folder)));
    }
}

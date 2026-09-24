namespace CronoLive;

// Installed binaries are read-only; user data survives upgrades and uninstall.
public static class AppPaths
{
    public static string DataDirectory { get; } = Resolve();
    static string Resolve()
    {
        string? custom = Environment.GetEnvironmentVariable("CASTERDESK_DATA_DIR");
        string path = string.IsNullOrWhiteSpace(custom)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CasterDesk")
            : Path.GetFullPath(custom);
        Directory.CreateDirectory(path);
        return path;
    }
}

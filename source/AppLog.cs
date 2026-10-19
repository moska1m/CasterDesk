namespace CasterDesk.Core;

public static class AppLog
{
    static readonly object gate=new();
    static readonly string file=Path.Combine(AppPaths.DataDirectory,"Logs",$"CasterDesk-{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log");
    public static void Write(string message)
    {
        try{lock(gate){Directory.CreateDirectory(Path.GetDirectoryName(file)!);File.AppendAllText(file,$"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");}}catch{/* Logging must never terminate the application. */}
    }
    public static void Install()
    {
        Write($"START version={BuildInfo.Version} runtime={Environment.Version}");
        AppDomain.CurrentDomain.UnhandledException+=(_,e)=>Write("FATAL "+e.ExceptionObject);
        TaskScheduler.UnobservedTaskException+=(_,e)=>Write("ASYNC ERROR "+e.Exception);
    }
}

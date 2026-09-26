namespace CronoLive;

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
        Write($"START version={typeof(AppLog).Assembly.GetName().Version} runtime={Environment.Version}");
        AppDomain.CurrentDomain.UnhandledException+=(_,e)=>Write("FATAL "+e.ExceptionObject);
        Application.ThreadException+=(_,e)=>
        {
            Write("UI ERROR "+e.Exception);
            MessageBox.Show(L.T("Ocorreu um erro de interface. O diagnóstico foi registrado em:\n")+file+L.T("\n\nReabra o programa para retomar os dados salvos."),L.T("CasterDesk — erro"),MessageBoxButtons.OK,MessageBoxIcon.Error);
        };
        Application.ApplicationExit+=(_,_)=>Write("EXIT");
    }
}

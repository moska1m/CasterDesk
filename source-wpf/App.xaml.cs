using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Threading;
using System.Security.Cryptography;
using System.Text;
using CasterDesk.Core;

namespace CasterDesk.Wpf;
public partial class App : Application
{
 Mutex? instance;
 protected override void OnStartup(StartupEventArgs e)
 {
  base.OnStartup(e);
  AppLog.Install();
  DispatcherUnhandledException+=(_,args)=>{AppLog.Write("WPF FATAL "+args.Exception);args.Handled=true;bool check=Environment.GetCommandLineArgs().Any(a=>a is "--wpf-check" or "--png-check");if(!check)MessageBox.Show(L.T("O CasterDesk encontrou um erro e será encerrado. Reinicie o programa. Os dados já salvos foram preservados; detalhes estão na pasta de logs."),"CasterDesk",MessageBoxButton.OK,MessageBoxImage.Error);Shutdown(1);};
  try
  {
   bool test=e.Args.Contains("--wpf-check")||e.Args.Contains("--png-check");
   if(!test)
   {
    string key=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(AppPaths.DataDirectory).ToUpperInvariant())));
    instance=new Mutex(false,"Local\\CasterDesk-"+key);bool acquired;
    try{acquired=instance.WaitOne(0);}catch(AbandonedMutexException){acquired=true;}
    if(!acquired){MessageBox.Show(L.T("O CasterDesk já está aberto. Use a janela existente para continuar."),"CasterDesk");instance.Dispose();instance=null;Shutdown();return;}
   }
   if(!test){FileBatch.Recover();DataBackup.ProcessPending(text=>MessageBox.Show(text,"CasterDesk"));}
   L.LoadPreference();
   int i=Array.IndexOf(e.Args,"--language");
   if(i>=0&&i+1<e.Args.Length&&L.Languages.Contains(e.Args[i+1])&&!File.Exists(AppPaths.ConfigFile("settings.json")))
   {L.Set(e.Args[i+1]);MatchFiles.AtomicText(AppPaths.ConfigFile("settings.json"),JsonSerializer.Serialize(new Settings{Language=L.Current}));}
   if(test){ShutdownMode=ShutdownMode.OnExplicitShutdown;if(e.Args.Contains("--png-check"))ImageOptimizationTests.Run();else Checks.Run();Shutdown();return;}
   var window=new Shell();MainWindow=window;window.Show();
  }
  catch(Exception ex)
  {
   File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"wpf-error.txt"),ex.ToString());
   if(e.Args.Contains("--wpf-check")||e.Args.Contains("--png-check")){Shutdown(1);return;}
   MessageBox.Show(L.T("Não foi possível iniciar o CasterDesk.\n")+ex.Message,"CasterDesk");Shutdown(1);
  }
 }
 protected override void OnExit(ExitEventArgs e){if(instance!=null){instance.ReleaseMutex();instance.Dispose();}AppLog.Write("WPF EXIT "+e.ApplicationExitCode);base.OnExit(e);}
}

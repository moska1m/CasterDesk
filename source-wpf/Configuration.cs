using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using CronoLive;

namespace CasterDesk.Wpf;
internal static class Configuration
{
 public static string RecoveryNotice{get;private set;}="";
 public static T Read<T>(string name) where T:new()
 {
  string path=AppPaths.ConfigFile(name);if(!File.Exists(path))return new();
  try{return JsonSerializer.Deserialize<T>(File.ReadAllText(path))??throw new InvalidDataException("Empty configuration");}
  catch(Exception ex) when(ex is JsonException or InvalidDataException)
  {
   string copy=path+".invalid-"+Guid.NewGuid().ToString("N");File.Copy(path,copy);
   AppLog.Write($"CONFIG RECOVERY {path}: {ex}");RecoveryNotice=L.T("Uma configuração inválida foi preservada na pasta Configuracoes. Os valores padrão foram carregados.");return new();
  }
 }
 public static void Normalize(Settings value)
 {
  value.Language=L.Languages.Contains(value.Language)?value.Language:"pt-BR";
  value.Initial=Math.Clamp(value.Initial,0,1440);
  value.Presets=(value.Presets??new decimal[]{2,3,4}).Where(p=>p>0&&p<=1440).Take(8).ToArray();
  value.Format=value.Format=="hh:mm:ss"?value.Format:"mm:ss";
  value.Message??="";if(string.IsNullOrWhiteSpace(value.Output))value.Output=Path.Combine(AppPaths.TextDirectory,"timer.txt");
 }
 public static void Normalize(CastingSettings value)
 {
  value.People=Enumerable.Range(0,4).Select(i=>value.People?.ElementAtOrDefault(i)??new Talent()).ToArray();
  foreach(var person in value.People){person.Name??="";person.Handle??="";}
  if(string.IsNullOrWhiteSpace(value.Folder))value.Folder=AppPaths.TextDirectory;
 }
}

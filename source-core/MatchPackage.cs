using System.Text.Json;
namespace CronoLive;

// A portable match package is distinct from a full application backup.
public static class MatchPackage
{
 public static void Export(SiegeMatch match,string destination)
 {
  lock(FileBatch.Gate){MatchFiles.Snapshot(match);DataBackup.Create(MatchFiles.Ensure(match),destination);}
 }
 public static SiegeMatch Import(string archive,SiegeCatalog catalog,IReadOnlyList<SiegeMatch> existing)
 {
  string stage=Path.Combine(AppPaths.BackupDirectory,"verificacao-"+Guid.NewGuid().ToString("N"));
  try
  {
   var manifest=DataBackup.Extract(archive,stage);
   if(!manifest.Files.Any(f=>f.Path=="partida.json"))throw new InvalidDataException(L.T("Este arquivo não contém um pacote de partida."));
   var match=MatchStore.Read(Path.Combine(stage,"partida.json"),catalog);
   if(MatchStore.IsDuplicate(existing,match,MatchStore.Folder))throw new InvalidDataException(L.T("Esta partida já existe no programa."));
   foreach(var file in manifest.Files)
    if(!(file.Path is "partida.json" or "LEIA-ME.txt" or "arte.json" or "ultima-exportacao.json")&&!new[]{"Imagens/","Estatisticas/","Recursos/","Revisoes/"}.Any(p=>file.Path.StartsWith(p,StringComparison.OrdinalIgnoreCase)))throw new InvalidDataException(L.T("Arquivo inesperado no pacote de partida."));
   lock(FileBatch.Gate)
   {
    using var batch=new FileBatch();string target=MatchFiles.DirectoryFor(match);
    foreach(var file in manifest.Files.Where(f=>f.Path is not ("partida.json" or "LEIA-ME.txt")))
    {string source=DataBackup.SafePath(stage,file.Path);batch.Add(DataBackup.SafePath(target,file.Path),p=>File.Copy(source,p));}
    MatchStore.Stage(batch,match);batch.Commit();
   }
   return match;
  }
  finally{if(Directory.Exists(stage))DataBackup.RemoveStage(stage);}
 }
}

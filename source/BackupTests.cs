using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.IO.Compression;

using System.Text.Json;

namespace CronoLive;

public static class BackupTests

{

    public static void Run()

    {

        int count=0;void Need(bool value){count++;if(!value)throw new Exception("Backup check "+count);}

        string root=Path.Combine(AppPaths.DataDirectory,"fixture"),target=Path.Combine(AppPaths.DataDirectory,"restored");Directory.CreateDirectory(root);Directory.CreateDirectory(target);

        Directory.CreateDirectory(Path.Combine(root,"Textos"));File.WriteAllText(Path.Combine(root,"Textos","narração.txt"),"Moska — transmissão");Directory.CreateDirectory(Path.Combine(root,"Partidas"));

        var match=new SiegeMatch{TeamA="Liquid",TeamB="FaZe"};File.WriteAllText(Path.Combine(root,"Partidas",match.Id+".json"),JsonSerializer.Serialize(match));

        Directory.CreateDirectory(Path.Combine(root,"Backups"));File.WriteAllText(Path.Combine(root,"Backups","old.zip"),"excluded");

        string zip=Path.Combine(AppPaths.DataDirectory,"test.zip");DataBackup.Create(root,zip);

        string stage=Path.Combine(AppPaths.DataDirectory,"stage");var manifest=DataBackup.Extract(zip,stage);Need(manifest.Files.Count==2);

        DataBackup.Apply(target,stage,manifest,false);Need(File.ReadAllText(Path.Combine(target,"Textos","narração.txt"))=="Moska — transmissão");Need(DataBackup.Conflicts(target,manifest)==0);

        File.WriteAllText(Path.Combine(target,"Textos","narração.txt"),"current");Need(DataBackup.Conflicts(target,manifest)==1);

        DataBackup.Apply(target,stage,manifest,false);Need(File.ReadAllText(Path.Combine(target,"Textos","narração.txt"))=="current");

        DataBackup.Apply(target,stage,manifest,true);Need(File.ReadAllText(Path.Combine(target,"Textos","narração.txt"))=="Moska — transmissão");Need(Directory.GetFiles(Path.Combine(target,"Backups"),"*.zip").Length==3);

        Need(Directory.GetFiles(Path.Combine(target,"Partidas")).Length==1);

        foreach(string bad in new[]{"../outside","/outside","C:/outside","Backups/../../outside","x\\y","foo./bar"}){try{DataBackup.SafePath(target,bad);throw new Exception("Unsafe path accepted");}catch(InvalidDataException){count++;}}

        string rollback=Path.Combine(AppPaths.DataDirectory,"rollback");Directory.CreateDirectory(Path.Combine(rollback,"Textos"));File.WriteAllText(Path.Combine(rollback,"Textos","narração.txt"),"preserve me");Directory.CreateDirectory(Path.Combine(rollback,"Partidas",match.Id+".json"));

        var ordered=manifest with {Files=manifest.Files.OrderByDescending(f=>f.Path.StartsWith("Textos/")).ToList()};

        try{DataBackup.Apply(rollback,stage,ordered,true);throw new Exception("Expected destination conflict");}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){count++;}

        Need(File.ReadAllText(Path.Combine(rollback,"Textos","narração.txt"))=="preserve me");

        using(var archive=ZipFile.Open(zip,ZipArchiveMode.Update)){archive.GetEntry("data/Textos/narração.txt")!.Delete();using var writer=new StreamWriter(archive.CreateEntry("data/Textos/narração.txt").Open());writer.Write("corruption");}

        try{DataBackup.Extract(zip,Path.Combine(AppPaths.DataDirectory,"bad-stage"));throw new Exception("Corruption accepted");}catch(InvalidDataException){count++;}

        Need(new Settings().QuickRounds);Need(!JsonSerializer.Deserialize<Settings>("{\"QuickRounds\":false}")!.QuickRounds);
        string legacy=Path.Combine(AppPaths.DataDirectory,"legacy"),migrated=Path.Combine(AppPaths.DataDirectory,"migrated");Directory.CreateDirectory(legacy);
        string preferences=JsonSerializer.Serialize(new Settings{Output=Path.Combine(legacy,"timer.txt"),QuickRounds=false});File.WriteAllText(Path.Combine(legacy,"settings.json"),preferences);
        AppPaths.CopyMissing(legacy,migrated);AppPaths.ArrangeConfigs(migrated);
        Need(File.ReadAllText(Path.Combine(migrated,"Configuracoes","settings.json"))==preferences);Need(File.Exists(Path.Combine(legacy,"settings.json")));
        File.WriteAllText(Path.Combine(migrated,"Configuracoes","settings.json"),"{}");AppPaths.CopyMissing(legacy,migrated);AppPaths.ArrangeConfigs(migrated);Need(File.ReadAllText(Path.Combine(migrated,"Configuracoes","settings.json"))=="{}");

        File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"backup-test.txt"),$"{count} verificações aprovadas: integridade, restauração, conflitos, duplicatas, cópia de segurança e caminhos inválidos.");

    }

}


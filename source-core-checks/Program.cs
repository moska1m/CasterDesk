using CasterDesk.Core;

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CASTERDESK_DATA_DIR")))
    throw new InvalidOperationException("Tests require an isolated CASTERDESK_DATA_DIR.");
L.Set("pt-BR");
if(args.Length==3 && args[0]=="--replay-local-check"){await ReplayChecks.RunLocal(args[1],args[2]);return;}
ReplayChecks.Run();
BrandPackageChecks.Run();
if(args.Length==2&&args[0] is "--update-manifest-check" or "--update-catalog-check" or "--update-live-check") {
    using var http=UpdateClient.CreateHttpClient();var client=new UpdateClient(http);
    if(args[0]=="--update-manifest-check")Console.WriteLine("Verified signed manifest: "+client.ReadManifest(File.ReadAllBytes(args[1])).Version);
    else if(args[0]=="--update-catalog-check")Console.WriteLine("Verified signed catalog: "+string.Join(", ",client.ReadCatalog(File.ReadAllBytes(args[1])).Select(v=>v.Version)));
    else {
        var versions=await client.ListAsync();var item=versions.Single(v=>v.Version==args[1]);
        string path=await client.DownloadAsync(item,directory:Path.Combine(AppPaths.DataDirectory,"live-download"));
        await UpdateClient.VerifyFileAsync(path,item);
        Console.WriteLine("Live catalog, pinned signature and installer SHA-256 verified: "+item.Version+" ("+item.Size+" bytes). No installer executed.");
    }
    return;
}
UpdateChecks.Run();
GoogleSheetsChecks.Run();
MatchTableChecks.Run();
MatchInterchangeTests.Run();
CasterDesk.Wpf.StabilityChecks.Run();
BackupTests.Run();
ImageOptimizationTests.Run();
Console.WriteLine("Core checks passed; no application window was created.");

using CronoLive;

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CASTERDESK_DATA_DIR")))
    throw new InvalidOperationException("Tests require an isolated CASTERDESK_DATA_DIR.");
L.Set("pt-BR");
MatchInterchangeTests.Run();
CasterDesk.Wpf.StabilityChecks.Run();
BackupTests.Run();
ImageOptimizationTests.Run();
Console.WriteLine("Core checks passed; no application window was created.");

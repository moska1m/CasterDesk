using Avalonia.Media.Imaging;

namespace CasterDesk.Avalonia;

internal static class Checks
{
    public static void Run()
    {
        L.Set("pt-BR");
        var catalog = SiegeCatalog.Load();
        var match = new SiegeMatch { TeamA = "BLACK DRAGONS", TeamB = "TEAM LIQUID", CoinWinner = 0, FirstBan = 0,
            Veto = catalog.Maps.Take(8).Select(m => m.Id).ToArray() };
        match.SyncMaps(catalog);
        var map = match.Maps[0]; map.InitialAttack = 0; map.OtAttack = 1;
        for (int n = 0; n < 7; n++) map.Rounds.Add(new() { Winner = 1, Site = catalog.Maps.Single(m => m.Id == map.MapId).Sites[n % 4], Ending = "elimination" });
        map.Statistics = new() { Status = "complete", ConfirmedAt = DateTimeOffset.Now };
        for (int team = 0; team < 2; team++) for (int n = 0; n < 5; n++)
            map.Statistics.Players.Add(new() { Team = team, Name = $"Player{team}{n}", Kills = 5 + n, Deaths = 7 - n, Assists = n });
        MatchStore.Save(match);
        var studio = new StudioView(() => new[] { match }, catalog);
        var owner = new Window { Content = studio, Background = Ui.Canvas };
        // The window belongs only to Avalonia.Headless: no native windows, dialogs or real data.
        owner.Show();
        studio.Refresh(match.Id); Wait(studio.Render()); Wait(studio.SettlePreview());
        if (!studio.PreviewReady || studio.FormatCount != 2) throw new Exception($"Announcement preview={studio.PreviewReady}, formats={studio.FormatCount}, selected={studio.Selected?.Id}: {studio.CheckStatus}");
        foreach (var size in new[] { new Size(980, 640), new Size(1280, 800), new Size(1920, 1080) }) {
            owner.Width = size.Width; owner.Height = size.Height;
            owner.Measure(size); owner.Arrange(new Rect(size)); Dispatcher.UIThread.RunJobs();
            studio.Measure(size); studio.Arrange(new Rect(size));
            if (studio.GenerateBounds.Bottom > size.Height || studio.GenerateBounds.Width <= 0) throw new Exception("Generate action clipped.");
            using var image = new RenderTargetBitmap(new PixelSize((int)size.Width, (int)size.Height), new Vector(96, 96));
            image.Render(studio); image.Save(Path.Combine(AppPaths.DataDirectory, $"avalonia-studio-{size.Width}.png"));
        }
        for (int category = 1; category < 4; category++) {
            Wait(studio.CheckCategory(category));
            if (!studio.PreviewReady) throw new Exception($"Category {category} preview failed.");
        }
        studio.Stop(); owner.Content = null; owner.Close();
        ChampionshipChecks.Run();
        BackupChecks.Run();
        LocalizationChecks.Run();
        TextsChecks.Run();
        BrandChecks.Run();
        CorrectionChecks.Run();
        ArtChecks.Run();
        InteractionChecks.Run();
        SpecChecks.Run();
        ObsDockChecks.Run();
        AuditFixChecks.Run();
        File.WriteAllText(Path.Combine(AppPaths.DataDirectory, "avalonia-check.txt"), "PASS: four image categories, announcement feed/story, three layouts, generate bounds and championship UI flow. No desktop window was opened.");
    }

    static void Wait(Task task)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (!task.IsCompleted) {
            if (elapsed.Elapsed > TimeSpan.FromSeconds(45)) throw new TimeoutException("Headless rendering timed out.");
            Dispatcher.UIThread.RunJobs(); Thread.Sleep(5);
        }
        task.GetAwaiter().GetResult();
    }
}

using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace CasterDesk.Desktop;

internal static class ReplayUiChecks
{
    public static void Run()
    {
        string folder=Path.Combine(AppPaths.DataDirectory,"replay-ui-fixture");Directory.CreateDirectory(folder);
        string one=Path.Combine(folder,"Match-test");Directory.CreateDirectory(one);File.WriteAllText(Path.Combine(one,"R01.rec"),"fixture: never parsed");
        var match=new SiegeMatch{TeamA="Equipe Azul",TeamB="Equipe Laranja"};var map=new MapRecord{MapId="bank"};
        var window=new ReplayImportWindow(match,map,folder);window.Show();
        var elapsed=System.Diagnostics.Stopwatch.StartNew();
        while(!window.Listing.IsCompleted){if(elapsed.Elapsed>TimeSpan.FromSeconds(12))throw new Exception("Replay preview did not finish.");Dispatcher.UIThread.RunJobs();Thread.Sleep(5);}
        window.Listing.GetAwaiter().GetResult();Dispatcher.UIThread.RunJobs();
        var title=window.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.Name=="replay-folder-title");
        if(!title.Text!.StartsWith("Não identificado"))throw new Exception("Invalid replay was given invented metadata.");
        title.Text="CAFÉ DOSTOIÉVSKI · EQUIPE AZUL COM NOME LONGO × EQUIPE LARANJA COM NOME LONGO";
        foreach(var size in new[]{new Size(800,600),new Size(1060,760)}){
            window.Width=size.Width;window.Height=size.Height;window.Measure(size);window.Arrange(new Rect(size));Dispatcher.UIThread.RunJobs();
            var position=title.TranslatePoint(default,window)??throw new Exception("Replay label missing.");
            if(position.X+title.Bounds.Width>size.Width*.5 || title.Bounds.Height<30)throw new Exception("Replay metadata did not wrap inside the folder list.");
            using var image=new RenderTargetBitmap(new PixelSize((int)size.Width,(int)size.Height),new Vector(96,96));image.Render(window);
            image.Save(Path.Combine(AppPaths.DataDirectory,$"replay-import-{size.Width}.png"));
        }
        window.Close();
        var stats=new StatsWindow(match,map);stats.Show();stats.Measure(new Size(1280,780));stats.Arrange(new Rect(0,0,1280,780));Dispatcher.UIThread.RunJobs();
        var button=stats.GetVisualDescendants().OfType<Button>().FirstOrDefault(b=>b.Name=="stats-import-replay");
        if(button==null||button.Bounds.Width<=0||!button.IsEnabled)throw new Exception("Replay import action unavailable.");
        stats.Close();
    }
}

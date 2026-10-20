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
        foreach(var size in new[]{new Size(800,600),new Size(1060,760)}){
            window.Width=size.Width;window.Height=size.Height;window.Measure(size);window.Arrange(new Rect(size));Dispatcher.UIThread.RunJobs();
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

using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace CasterDesk.Desktop;

internal static class PortraitChecks
{
    public static void Run()
    {
        var catalog=SiegeCatalog.Load();
        foreach(var op in catalog.Operators) {
            foreach(var (pixels,width,height) in new[]{(60,80,72),(120,160,144),(220,320,288)}) {
                using var image=new System.Drawing.Bitmap(SiegeCatalog.Portrait(op.Id,pixels));
                if(image.Width!=width||image.Height!=height||image.GetPixel(0,0).A!=0)throw new Exception("Portrait dimensions/transparency invalid: "+op.Id);
            }
            if(!File.Exists(Path.Combine(SiegeCatalog.AssetRoot,"operators","originals",op.Id+".png")))throw new Exception("Full-width source missing: "+op.Id);
        }
        int Top(string id){using var image=new System.Drawing.Bitmap(SiegeCatalog.Portrait(id,320));for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)if(image.GetPixel(x,y).A>30)return y;return image.Height;}
        if(Math.Abs(Top("glaz")-Top("azami"))>2||Math.Abs(Top("wamai")-Top("azami"))>2)throw new Exception("Wide weapons or poses must not push the operator head down.");
        var session=new MatchSession(catalog);session.New();session.Names("EQUIPE AZUL","EQUIPE LARANJA");session.ConfigureSpec("MD1",[catalog.Maps[0].Id]);session.Side(session.Map!.MapId,false,0);
        var map=session.Map!;map.Rounds=Enumerable.Range(0,9).Select(i=>new RoundRecord{Site=catalog.Maps[0].Sites[i%4],Winner=i%2,Ending="elimination"}).ToList();
        foreach(int half in new[]{0,1})foreach(string role in new[]{"DEF","ATQ"}) {
            var ids=role=="DEF"?new[]{"wamai","azami","mira"}:new[]{"striker","glaz","brava"};
            var slots=map.BanOrder(half).Where(i=>map.BanRole(i)==role).ToArray();for(int n=0;n<slots.Length;n++)map.Bans[slots[n]]=ids[n];
        }
        var view=new ChampionshipView(session);var window=new Window{Content=view,Background=Ui.Canvas};window.Show();
        foreach(var size in new[]{new Size(980,640),new Size(1280,800),new Size(1920,1080)}) {
            window.Width=size.Width;window.Height=size.Height;window.Measure(size);window.Arrange(new Rect(size));Dispatcher.UIThread.RunJobs();view.Refresh();Dispatcher.UIThread.RunJobs();
            var panel=view.GetVisualDescendants().OfType<Border>().Single(c=>c.Name=="bans-panel");
            var portraits=view.GetVisualDescendants().OfType<Image>().Where(c=>c.Name?.StartsWith("ban-image-")==true).ToArray();
            if(portraits.Length!=12||portraits.Any(i=>i.Margin.Top!=4||i.Source==null))throw new Exception("Portrait headroom/source missing.");
            using var bitmap=new RenderTargetBitmap(new PixelSize(Math.Max(1,(int)panel.Bounds.Width),Math.Max(1,(int)panel.Bounds.Height)));bitmap.Render(panel);bitmap.Save(Path.Combine(AppPaths.DataDirectory,$"operator-cards-{size.Width}.png"));
        }
        view.Stop();window.Close();
    }
}

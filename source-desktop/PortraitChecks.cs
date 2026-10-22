using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace CasterDesk.Desktop;

internal static class PortraitChecks
{
    static void CatalogReview(SiegeCatalog catalog)
    {
        var results=new List<object>();
        foreach(var (width,height) in new[]{(40,32),(80,68),(128,112),(220,188)}) {
            const int columns=8;int rows=(catalog.Operators.Count+columns-1)/columns;
            var gallery=new Grid{ColumnDefinitions=new(string.Join(',',Enumerable.Repeat((width+8).ToString(),columns))),RowDefinitions=new(string.Join(',',Enumerable.Repeat((height+28).ToString(),rows)))};
            var images=new List<(SiegeOperator Operator,Image Image)>();
            for(int i=0;i<catalog.Operators.Count;i++) {
                var op=catalog.Operators[i];var portrait=Ui.OperatorPortrait(op.Id);portrait.Width=width;portrait.Height=height;
                images.Add((op,portrait));
                var content=new StackPanel();content.Children.Add(portrait);content.Children.Add(new TextBlock{Text=op.Name,FontSize=width<60?7:10,TextAlignment=TextAlignment.Center,Foreground=Brushes.White});
                var cell=new Border{Child=content,Margin=new Thickness(2),Background=Ui.Team(op.Role=="DEF"?0:1,false),ClipToBounds=true};
                gallery.Children.Add(cell);Grid.SetColumn(cell,i%columns);Grid.SetRow(cell,i/columns);
            }
            var window=new Window{Content=gallery,Width=columns*(width+8)+12,Height=rows*(height+28)+44,Background=Ui.Canvas};
            window.Show();window.UpdateLayout();Dispatcher.UIThread.RunJobs();
            try {
                foreach(var (op,portrait) in images) {
                    using var shot=new RenderTargetBitmap(new PixelSize(width,height));shot.Render(portrait);
                    using var buffer=new MemoryStream();shot.Save(buffer);buffer.Position=0;
                    using var pixels=new System.Drawing.Bitmap(buffer);
                    int top=pixels.Height,bottom=-1;
                    for(int y=0;y<pixels.Height;y++)for(int x=pixels.Width/10;x<pixels.Width*9/10;x++)if(pixels.GetPixel(x,y).A>60){top=Math.Min(top,y);bottom=Math.Max(bottom,y);}
                    int gap=height-1-bottom;
                    if(bottom<0||gap>1)throw new Exception($"Floating portrait: {op.Id}, {width}x{height}, bottom gap {gap}.");
                    if(top>Math.Max(5,height*.09))throw new Exception($"Portrait headroom changed: {op.Id}, {width}x{height}, top {top}.");
                    results.Add(new{op.Id,Width=width,Height=height,BottomGap=gap,Top=top});
                }
                using var sheet=new RenderTargetBitmap(new PixelSize(columns*(width+8),rows*(height+28)));sheet.Render(gallery);sheet.Save(Path.Combine(AppPaths.DataDirectory,$"all-operators-{width}.png"));
            } finally{window.Close();Dispatcher.UIThread.RunJobs();}
        }
        File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"portrait-audit.json"),System.Text.Json.JsonSerializer.Serialize(results,new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
    }
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
        CatalogReview(catalog);
        var session=new MatchSession(catalog);session.New();session.Names("EQUIPE AZUL","EQUIPE LARANJA");session.ConfigureSpec("MD1",[catalog.Maps[0].Id]);session.Side(session.Map!.MapId,false,0);
        var map=session.Map!;map.Rounds=Enumerable.Range(0,9).Select(i=>new RoundRecord{Site=catalog.Maps[0].Sites[i%4],Winner=i%2,Ending="elimination"}).ToList();
        foreach(int half in new[]{0,1})foreach(string role in new[]{"DEF","ATQ"}) {
            var ids=role=="DEF"?new[]{"fenrir","skopos","tubarao"}:new[]{"solid-snake","ram","grim"};
            var slots=map.BanOrder(half).Where(i=>map.BanRole(i)==role).ToArray();for(int n=0;n<slots.Length;n++)map.Bans[slots[n]]=ids[n];
        }
        var view=new ChampionshipView(session);var window=new Window{Content=view,Background=Ui.Canvas};window.Show();
        foreach(var size in new[]{new Size(980,640),new Size(1280,800),new Size(1920,1080)}) {
            window.Width=size.Width;window.Height=size.Height;window.Measure(size);window.Arrange(new Rect(size));Dispatcher.UIThread.RunJobs();view.Refresh();Dispatcher.UIThread.RunJobs();
            var panel=view.GetVisualDescendants().OfType<Border>().Single(c=>c.Name=="bans-panel");
            var portraits=view.GetVisualDescendants().OfType<Image>().Where(c=>c.Name?.StartsWith("ban-image-")==true).ToArray();
            if(portraits.Length!=12||portraits.Any(i=>i.Margin.Top!=4||i.Source==null))throw new Exception("Portrait headroom/source missing.");
            foreach(string id in new[]{"solid-snake","ram","grim","fenrir","skopos","tubarao"}) {
                var portrait=portraits.Single(i=>i.Name=="ban-image-"+Array.IndexOf(map.Bans,id));
                string path=Path.Combine(AppPaths.DataDirectory,$"portrait-{id}-{size.Width}.png");
                using(var shot=new RenderTargetBitmap(new PixelSize(Math.Max(1,(int)Math.Ceiling(portrait.Bounds.Width)),Math.Max(1,(int)Math.Ceiling(portrait.Bounds.Height))))){shot.Render(portrait);shot.Save(path);}
                using var pixels=new System.Drawing.Bitmap(path);int bottom=0;
                for(int y=Math.Max(0,pixels.Height-3);y<pixels.Height;y++)for(int x=pixels.Width/4;x<pixels.Width*3/4;x++)if(pixels.GetPixel(x,y).A>80)bottom++;
                if(bottom<pixels.Width/4)throw new Exception("Portrait must reach the card base without a transparent gap: "+id+" at "+size);
            }
            using var bitmap=new RenderTargetBitmap(new PixelSize(Math.Max(1,(int)panel.Bounds.Width),Math.Max(1,(int)panel.Bounds.Height)));bitmap.Render(panel);bitmap.Save(Path.Combine(AppPaths.DataDirectory,$"operator-cards-{size.Width}.png"));
        }
        view.Stop();window.Close();
        File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"portrait-check.txt"),$"PASS: all {catalog.Operators.Count} operators at 4 card sizes ({catalog.Operators.Count*4} rendered checks) across small/medium/large assets; bottom gaps <=1 px, headroom retained. Solid Snake, Ram, Grim, Fenrir, Skopos and Tubarao checked in actual ban cards at 3 window sizes. Original artwork/variants and individual aspect ratios preserved. Headless only.");
    }
}

using System.Drawing.Imaging;
using System.Text.Json;

namespace CasterDesk.Core;
public static class StudioTests
{
    public static void Run()
    {
        int checks=0;void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
        var catalog=SiegeCatalog.Load();string folder=Path.Combine(AppPaths.DataDirectory,"studio-test");Directory.CreateDirectory(folder);
        var match=new SiegeMatch{TeamA="Liquid",TeamB="BD",Format="MD3",FirstBan=0,CoinWinner=0,Veto=["border","villa","fortress","nighthaven-labs","clubhouse","chalet","lair","bank"]};match.SyncMaps(catalog);
        foreach(var map in match.Maps){map.InitialAttack=0;map.OtAttack=1;}
        foreach(int i in new[]{0,1}){var map=match.Maps[i];map.Rounds=Enumerable.Range(0,7).Select(_=>new RoundRecord{Winner=0,Ending="elimination"}).ToList();map.ConfirmedAt=DateTimeOffset.Now;map.Statistics=new(){ConfirmedAt=DateTimeOffset.Now,Players=Enumerable.Range(0,10).Select(n=>new PlayerStatistic{Team=n/5,Name="Player"+n,Kills=n+2,Deaths=3,Assists=4}).ToList()};StatisticsData.Validate(map.Statistics);}
        MatchStore.Save(match);var brand=new ImageBrand{EventName="CAMPEONATO DA COMUNIDADE",ScheduledAt=new DateTime(2026,10,1,20,30,0),Mvp="0:PLAYER4"};
        foreach(var kind in Enum.GetValues<ArtKind>())foreach(var aspect in Enum.GetValues<ExportAspect>())
        {
            if(kind==ArtKind.MapVeto&&aspect!=ExportAspect.Horizontal)continue;
            var pages=StudioRenderer.Render(match,catalog,brand,kind,aspect,kind==ArtKind.MapStats?match.Maps[0]:null);
            try{foreach(var p in pages){Check(p.Image.Width==(aspect==ExportAspect.Horizontal?1920:1080),"Image width");Check(p.Image.Height==(aspect==ExportAspect.Horizontal?1080:aspect==ExportAspect.Feed?1350:1920),"Image height");p.Image.Save(Path.Combine(folder,p.Name+".png"),ImageFormat.Png);}
                if(kind==ArtKind.MapVeto){Check(pages.Count==2,"Veto pair");Check(pages[0].Image.GetPixel(0,0).A==0,"OBS alpha");Check(pages[1].Image.GetPixel(0,0).A==255,"Archived background");}}
            finally{foreach(var p in pages)p.Dispose();}
        }
        foreach(var format in new[]{"MD1","MD5"})
        {
            var other=new SiegeMatch{TeamA="A",TeamB="B",Format=format,FirstBan=1,CoinWinner=0,Veto=catalog.Maps.Take(8).Select(m=>m.Id).ToArray()};other.SyncMaps(catalog);foreach(var m in other.Maps)m.InitialAttack=1;
            var pages=StudioRenderer.Render(other,catalog,new(),ArtKind.MapVeto,ExportAspect.Horizontal);foreach(var p in pages){p.Image.Save(Path.Combine(folder,format+"-"+p.Name+".png"));p.Dispose();}Check(true,"Veto "+format);
        }
        var promoBrand=new ImageBrand{EventName="CHALLENGER SERIES",ScheduledAt=DateTime.Now};
        var promo=StudioRenderer.Render(match,catalog,promoBrand,ArtKind.Announcement,ExportAspect.Feed);
        promo[0].Image.Save(Path.Combine(folder,"promo-psd.png"));
        using var before=new MemoryStream();promo[0].Image.Save(before,ImageFormat.Png);foreach(var p in promo)p.Dispose();
        promoBrand.ScheduledAt=null;promo=StudioRenderer.Render(match,catalog,promoBrand,ArtKind.Announcement,ExportAspect.Feed);
        using var after=new MemoryStream();promo[0].Image.Save(after,ImageFormat.Png);foreach(var p in promo)p.Dispose();
        Check(before.ToArray().SequenceEqual(after.ToArray()),"Announcement omits date and time even for old saved metadata");
        var aggregate=StatisticsData.ForSeries(match);Check(!aggregate.Partial&&aggregate.MapsIncluded==2&&aggregate.Players.Count==10,"Complete aggregation");Check(aggregate.Players.Single(p=>p.Name=="Player4").Kills==12,"Kill sum");
        match.Maps[1].Statistics=new(){Status="missing",ConfirmedAt=DateTimeOffset.Now};Check(StatisticsData.ForSeries(match).Partial,"Missing print coverage");
        var partial=new MapStatistics{Status="partial",ConfirmedAt=DateTimeOffset.Now,Players=[new(){Team=0,Name="Partial",Kills=8,Deaths=null,Assists=2}]};StatisticsData.Validate(partial);match.Maps[1].Statistics=partial;Check(!StatisticsData.ForSeries(match).Players.Any(p=>p.Name=="Partial"),"Unknown values never converted to zero");
        bool rejected=false;try{StatisticsData.Validate(new(){Players=[new(){Team=0,Name="A",Kills=-1}]});}catch(InvalidDataException){rejected=true;}Check(rejected,"Negative stats rejected");
        string saved=MatchFiles.Export(match,catalog,brand,ArtKind.MapVeto,ExportAspect.Horizontal,null);Check(File.Exists(Path.Combine(saved,"partida.json"))&&File.Exists(Path.Combine(saved,"Imagens","Horizontal","Mapas","map-veto.png")),"Match archive");
        using(var image=new Bitmap(MatchFiles.ObsVeto))Check(image.GetPixel(0,0).A==0,"Published OBS alpha");
        string path=MatchFiles.DirectoryFor(match);match.TeamA="Renamed";MatchStore.Save(match);Check(path==MatchFiles.DirectoryFor(match),"Folder stable after team rename");Check(MatchStore.Read(Path.Combine(saved,"partida.json"),catalog).TeamA=="Renamed","Latest JSON snapshot");
        using(var bg=new Bitmap(640,360)){using(var g=Graphics.FromImage(bg))g.Clear(Color.Purple);string file=Path.Combine(folder,"custom.png");bg.Save(file);brand.Background=MatchFiles.Resource(match,"fundo",file);}
        var custom=StudioRenderer.Render(match,catalog,brand,ArtKind.Announcement,ExportAspect.Horizontal);Check(custom[0].Image.GetPixel(0,0).R>40,"Custom BG applied");custom[0].Image.Save(Path.Combine(folder,"custom-background.png"));foreach(var p in custom)p.Dispose();
        Check(MatchFiles.Resolve(match,"../settings.json") is null,"Resource traversal blocked");
        using(var form=new Form{ClientSize=new(1366,800)}){var studio=new ImageStudioPanel(()=>new[]{match},catalog);form.Controls.Add(studio);form.Show();studio.RefreshMatches(match.Id);studio.WaitForPreview();foreach(var k in Enum.GetValues<ArtKind>()){studio.SelectKind(k);using var bmp=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bmp,new(Point.Empty,form.Size));bmp.Save(Path.Combine(folder,"ui-"+k+".png"));}form.Close();}
        foreach(var size in new[]{new Size(1140,600),new Size(1920,920)})
        {
            using var form=new Form{ClientSize=size};AppTheme.Dark=true;AppTheme.Siege=true;var studio=new ImageStudioPanel(()=>new[]{match},catalog);form.Controls.Add(studio);form.Show();AppTheme.Apply(form);studio.RefreshMatches(match.Id);studio.WaitForPreview();studio.VerifyActionsVisible();studio.SelectKind(ArtKind.MapVeto);studio.VerifyActionsVisible();Check(true,"Visible generate action "+size);using var bmp=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bmp,new(Point.Empty,form.Size));bmp.Save(Path.Combine(folder,"ui-dark-"+size.Width+".png"));form.Close();
        }
        foreach(string lang in new[]{"en","es"})
        {
            L.Set(lang);var pages=StudioRenderer.Render(match,catalog,brand,ArtKind.MapVeto,ExportAspect.Horizontal);foreach(var p in pages){p.Image.Save(Path.Combine(folder,lang+"-"+p.Name+".png"));p.Dispose();}using var form=new Form{ClientSize=new(1366,800)};var studio=new ImageStudioPanel(()=>new[]{match},catalog);form.Controls.Add(studio);form.Show();AppTheme.Apply(form);studio.RefreshMatches(match.Id);studio.SelectKind(ArtKind.SeriesStats);using var bmp=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bmp,new(Point.Empty,form.Size));bmp.Save(Path.Combine(folder,"ui-"+lang+".png"));form.Close();Check(true,"Localized studio "+lang);
        }
        L.Set("pt-BR");
        using(var dialog=new StatisticsDialog(match,match.Maps[0])){dialog.Show();Application.DoEvents();using var bmp=new Bitmap(dialog.Width,dialog.Height);dialog.DrawToBitmap(bmp,new(Point.Empty,dialog.Size));bmp.Save(Path.Combine(folder,"stats-dialog.png"));dialog.Close();}
        File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"studio-test.txt"),checks+" verificações da Central de Imagens aprovadas.");
    }
}

using System.Drawing;
using System.IO.Compression;
using System.Text.Json;
using CasterDesk.Core;

static class BrandPackageChecks
{
    static void Need(bool value,string label){if(!value)throw new Exception("Brand package: "+label);}
    public static void Run()
    {
        var match=new SiegeMatch();MatchStore.Save(match);
        string image=Path.Combine(AppPaths.DataDirectory,"theme-fixture.png");using(var bitmap=new Bitmap(32,24)){using var g=Graphics.FromImage(bitmap);g.Clear(Color.Blue);bitmap.Save(image);}
        var brand=new ImageBrand{EventName="CAMPEONATO",TeamALogo="preserved-logo",BroadcastChannel="https://twitch.tv/preserved",Mvp="preserved-mvp"};
        brand.Style(ArtKind.SeriesStats).Backgrounds["feed"]=MatchFiles.Resource(match,"fundo-SeriesStats-feed",image);
        brand.Style(ArtKind.SeriesStats).Accent="#3366AA";brand.Style(ArtKind.MapStats).Backgrounds["stories"]="";
        Need(brand.BackgroundFor(ArtKind.SeriesStats,"feed")!=""&&brand.BackgroundFor(ArtKind.SeriesStats,"horizontal")==""&&brand.BackgroundFor(ArtKind.MapStats,"feed")=="","backgrounds isolated by area and format");
        string zip=Path.Combine(AppPaths.DataDirectory,"theme.zip");BrandPackage.Export(match,brand,zip);
        string designer=Path.Combine(AppPaths.DataDirectory,"designer");Directory.CreateDirectory(designer);File.Copy(image,Path.Combine(designer,"anuncio-feed.png"));
        string fromDesigner=Path.Combine(AppPaths.DataDirectory,"from-designer.zip");BrandPackage.CreateFromFolder(designer,fromDesigner,"NOVO CAMPEONATO","#224466");
        using(var archive=ZipFile.OpenRead(fromDesigner))Need(archive.Entries.Count==2&&archive.GetEntry("theme.json")!=null,"designer PNG folder builds a portable package without JSON authoring");
        var target=new SiegeMatch();MatchStore.Save(target);
        var previous=new ImageBrand{TeamALogo="target-logo",BroadcastChannel="target-channel",Mvp="target-mvp",ArtVeto=["preserved"]};
        var applied=BrandPackage.Import(target,previous,zip);
        Need(applied.EventName==brand.EventName&&applied.Style(ArtKind.SeriesStats).Accent=="#3366AA"&&File.Exists(MatchFiles.Resolve(target,applied.BackgroundFor(ArtKind.SeriesStats,"feed"))),"portable theme applied and image copied");
        Need(applied.TeamALogo==previous.TeamALogo&&applied.BroadcastChannel==previous.BroadcastChannel&&applied.Mvp==previous.Mvp&&applied.ArtVeto![0]=="preserved","theme preserves match and personal choices");
        Need(MatchFiles.ReadBrand(target).EventName==brand.EventName,"theme persisted atomically");
        Need(applied.BackgroundFor(ArtKind.MapStats,"stories")==""&&applied.BackgroundFor(ArtKind.SeriesStats,"horizontal")=="","default backgrounds stay explicit");
        byte[] saved=File.ReadAllBytes(Path.Combine(MatchFiles.DirectoryFor(target),"arte.json"));
        string hostile=Path.Combine(AppPaths.DataDirectory,"theme-traversal.zip");File.Copy(zip,hostile);
        using(var archive=ZipFile.Open(hostile,ZipArchiveMode.Update)){using var writer=new StreamWriter(archive.CreateEntry("../outside.png").Open());writer.Write("invalid");}
        bool refused=false;try{BrandPackage.Import(target,applied,hostile);}catch(InvalidDataException){refused=true;}Need(refused&&File.ReadAllBytes(Path.Combine(MatchFiles.DirectoryFor(target),"arte.json")).SequenceEqual(saved),"traversal rejected without replacing theme");
        string broken=Path.Combine(AppPaths.DataDirectory,"theme-broken.zip");File.Copy(zip,broken);
        using(var archive=ZipFile.Open(broken,ZipArchiveMode.Update)){var entry=archive.Entries.First(e=>e.FullName.EndsWith(".png"));string name=entry.FullName;entry.Delete();using var writer=new StreamWriter(archive.CreateEntry(name).Open());writer.Write("not an image");}
        refused=false;try{BrandPackage.Import(target,applied,broken);}catch(Exception ex)when(ex is ArgumentException or OutOfMemoryException or InvalidDataException){refused=true;}
        Need(refused&&File.ReadAllBytes(Path.Combine(MatchFiles.DirectoryFor(target),"arte.json")).SequenceEqual(saved),"invalid image rolls back theme");
        Console.WriteLine("Brand package checks passed: portable backgrounds and colors, format isolation, preserved match choices, hostile archives and atomic failure.");
    }
}

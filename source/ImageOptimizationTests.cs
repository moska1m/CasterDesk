using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace CasterDesk.Core;
internal static class ImageOptimizationTests
{
    internal static void Run()
    {
        int checks=0;var report=new List<object>();string dir=AppPaths.DataDirectory;
        byte[] Pixels(Bitmap image)
        {
            using var normalized=new Bitmap(image.Width,image.Height,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(normalized)){g.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceCopy;g.DrawImageUnscaled(image,0,0);}
            var bits=normalized.LockBits(new(0,0,image.Width,image.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);byte[] bytes=new byte[image.Width*image.Height*4];
            try{for(int y=0;y<image.Height;y++)Marshal.Copy(bits.Scan0+y*bits.Stride,bytes,y*image.Width*4,image.Width*4);}finally{normalized.UnlockBits(bits);}return bytes;
        }
        void Verify(string name,Bitmap image)
        {
            string normal=Path.Combine(dir,name+"-native.png"),optimized=Path.Combine(dir,name+".png");
            var clock=Stopwatch.StartNew();image.Save(normal,ImageFormat.Png);long nativeMs=clock.ElapsedMilliseconds;clock.Restart();MatchFiles.AtomicImage(optimized,image);long optimizedMs=clock.ElapsedMilliseconds;
            using var decoded=new Bitmap(optimized);
            if(decoded.Size!=image.Size||!Pixels(image).SequenceEqual(Pixels(decoded)))throw new Exception("PNG pixels changed: "+name);
            checks++;report.Add(new{name,Before=new FileInfo(normal).Length,After=new FileInfo(optimized).Length,NativeMs=nativeMs,OptimizedMs=optimizedMs});
        }
        foreach(var size in new[]{new Size(1,1),new Size(17,9),new Size(199,113)})
        {
            using var rgba=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppArgb);
            using var rgb=new Bitmap(size.Width,size.Height,PixelFormat.Format24bppRgb);
            using var gray=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppArgb);
            for(int y=0;y<size.Height;y++)for(int x=0;x<size.Width;x++)
            {rgba.SetPixel(x,y,Color.FromArgb((x*13+y*7)%256,(x*5)%256,(y*9)%256,(x+y)%256));rgb.SetPixel(x,y,Color.FromArgb((x*11)%256,(y*7)%256,(x*5+y*3)%256));int v=(x*7+y*5)%256;gray.SetPixel(x,y,Color.FromArgb(v,v,v));}
            Verify("alpha-"+size.Width,rgba);Verify("rgb-"+size.Width,rgb);Verify("gray-"+size.Width,gray);
        }
        var catalog=SiegeCatalog.Load();var match=new SiegeMatch{TeamA="Liquid",TeamB="BD",Format="MD3",FirstBan=0,CoinWinner=0,Veto=["border","villa","fortress","nighthaven-labs","clubhouse","chalet","lair","bank"]};match.SyncMaps(catalog);foreach(var m in match.Maps)m.InitialAttack=0;
        match.Maps[0].Rounds=Enumerable.Range(0,7).Select(_=>new RoundRecord{Winner=0,Ending="elimination"}).ToList();match.Maps[0].ConfirmedAt=DateTimeOffset.Now;
        var brand=new ImageBrand{EventName="CHALLENGER SERIES"};
        foreach(var kind in new[]{ArtKind.Announcement,ArtKind.Results,ArtKind.MapVeto})
        {
            var pages=StudioRenderer.Render(match,catalog,brand,kind,ExportAspect.Horizontal);
            try{foreach(var page in pages)Verify(page.Name,page.Image);}finally{foreach(var page in pages)page.Dispose();}
        }
        File.WriteAllText(Path.Combine(dir,"image-optimization.json"),JsonSerializer.Serialize(report,MatchStore.Options));
        File.WriteAllText(Path.Combine(dir,"image-optimization-test.txt"),checks+" PNGs: mesmos pixels e dimensões; transparência preservada. Tamanho e tempo medidos no relatório.");
    }
}

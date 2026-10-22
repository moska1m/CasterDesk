namespace CasterDesk.Desktop;

// Keep official artwork intact. Trim transparent footer space in presentation,
// preserve the pose's aspect ratio and keep deliberate individual viewports.
internal sealed class FocusedOperatorPortrait(IImage source,Rect region,double offset=0) : IImage
{
    static readonly Dictionary<string,(long Stamp,double Bottom)> footers=new(StringComparer.OrdinalIgnoreCase);
    internal static IImage FitToBase(IImage source,string file)
    {
        long stamp=File.GetLastWriteTimeUtc(file).Ticks;
        if(!footers.TryGetValue(file,out var frame)||frame.Stamp!=stamp) {
            using var image=new System.Drawing.Bitmap(file);
            int bottom=image.Height;
            bool Opaque(int y){for(int x=0;x<image.Width;x++)if(image.GetPixel(x,y).A>30)return true;return false;}
            while(bottom>0&&!Opaque(bottom-1))bottom--;
            if(bottom==0)throw new InvalidDataException("Empty operator portrait: "+Path.GetFileName(file));
            frame=(stamp,(double)bottom/image.Height);footers[file]=frame;
        }
        // Already anchored portraits retain their exact framing and scale.
        return frame.Bottom>=1?source:new FocusedOperatorPortrait(source,new Rect(0,0,1,frame.Bottom));
    }
    public Size Size=>new(320,288);
    public void Draw(DrawingContext context,Rect sourceRect,Rect destRect)
    {
        if(sourceRect.Width<=0||sourceRect.Height<=0||destRect.Width<=0||destRect.Height<=0)return;
        var crop=new Rect(region.X*source.Size.Width,region.Y*source.Size.Height,
            region.Width*source.Size.Width,region.Height*source.Size.Height);
        double sx=destRect.Width/sourceRect.Width,sy=destRect.Height/sourceRect.Height;
        double width=crop.Width*Size.Height/crop.Height;
        var target=new Rect(destRect.X+((Size.Width-width)/2+Size.Width*offset-sourceRect.X)*sx,
            destRect.Y-sourceRect.Y*sy,width*sx,Size.Height*sy);
        using var clip=context.PushClip(destRect);
        context.DrawImage(source,crop,target);
    }
}

using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace CronoLive;

internal static class AppFonts
{
    // Process-private registration serves both GDI (TextRenderer) and GDI+.
    // Keep the collection alive as long as controls and exported images can use it.
    static readonly PrivateFontCollection fonts=new();
    static readonly FontFamily[] families;
    [DllImport("gdi32.dll",CharSet=CharSet.Unicode)]
    static extern int AddFontResourceEx(string file,uint flags,IntPtr reserved);
    static AppFonts()
    {
        foreach(string name in new[]{"Scout-Regular.ttf","Scout-Bold.ttf","ScoutCond-Regular.ttf"})
        {
            string path=Path.Combine(AppContext.BaseDirectory,"assets","fonts",name);
            if(!File.Exists(path))continue;
            try{fonts.AddFontFile(path);AddFontResourceEx(path,0x10,IntPtr.Zero);}catch(Exception ex){AppLog.Write("Font fallback: "+name+" "+ex.Message);}
        }
        families=fonts.Families;
    }
    internal static Font Body(float size,FontStyle style=FontStyle.Regular)=>Create(size,style,false);
    internal static Font Heading(float size,FontStyle style=FontStyle.Bold)=>Create(size,style,true);
    static Font Create(float size,FontStyle style,bool condensed)
    {
        string face=condensed?"ScoutCond-Regular":style.HasFlag(FontStyle.Bold)?"Scout-Bold":"Scout-Regular";
        var family=families.FirstOrDefault(f=>f.Name.Equals(face,StringComparison.OrdinalIgnoreCase));
        if(family is null)return new Font(condensed?"Bahnschrift SemiCondensed":"Segoe UI",size,style);
        return new Font(family,size,family.IsStyleAvailable(style)?style:FontStyle.Regular);
    }
    internal static string[] LoadedFamilies=>families.Select(f=>f.Name).ToArray();
}

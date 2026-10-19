using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace CronoLive;
public enum ArtKind { Announcement,MapVeto,Results,MapStats,SeriesStats }
public static class StudioRenderer
{
    static readonly object gate=new();
    static readonly PrivateFontCollection heading=new(),mono=new(),medium=new();
    static readonly Color Muted=Color.FromArgb(181,193,204),Red=Color.FromArgb(255,67,53);
    static readonly Dictionary<string,string> assetAliases=System.Text.Json.JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"assets","studio","aliases.json")))!;
    public static string Asset(string name)=>Path.Combine(AppContext.BaseDirectory,"assets","studio",assetAliases.GetValueOrDefault(name,name));
    static StudioRenderer()
    {
        heading.AddFontFile(Path.Combine(AppContext.BaseDirectory,"assets","promo","GT-America-Compressed-Bold.otf"));
        mono.AddFontFile(Path.Combine(AppContext.BaseDirectory,"assets","promo","SimplonMono-Bold.otf"));
        medium.AddFontFile(Path.Combine(AppContext.BaseDirectory,"assets","promo","SimplonMono-Medium.otf"));
    }
    public static List<ResultPage> Render(SiegeMatch match,SiegeCatalog catalog,ImageBrand brand,ArtKind kind,ExportAspect aspect,MapRecord? map=null)
    {
        lock(gate)
        {
            if(kind is ArtKind.SeriesStats or ArtKind.Results) {
                string issue=StatisticsData.SeriesIssue(match);
                if(issue!="")throw new InvalidOperationException(issue);
            }
            if(kind==ArtKind.MapVeto)
            {
                if(match.Veto.Any(string.IsNullOrWhiteSpace)||match.Maps.Any(m=>m.InitialAttack<0))throw new InvalidOperationException(L.T("Conclua o veto e defina os lados dos mapas antes de gerar."));
                using var transparent=Veto(match,catalog,brand);
                using var full=Background(match,brand,new(1920,1080),"veto",kind);using(var g=Graphics.FromImage(full))g.DrawImageUnscaled(transparent,0,0);
                return [new("map-veto-obs",new Bitmap(transparent)),new("map-veto",new Bitmap(full))];
            }
            if(kind!=ArtKind.Announcement && !match.Maps.Any(m=>m.Rounds.Count>0))throw new InvalidOperationException(L.T("Registre pelo menos um round antes de gerar a imagem."));
            if(kind==ArtKind.MapStats && (map is null||map.Statistics?.ConfirmedAt is null||map.Statistics.Players.Count==0))throw new InvalidOperationException(L.T("Confira as estatísticas deste mapa antes de gerar."));
            if(kind==ArtKind.SeriesStats && !match.Maps.Any(m=>m.Statistics?.ConfirmedAt is not null && m.Statistics.Players.Count>0))throw new InvalidOperationException(L.T("Confira as estatísticas de pelo menos um mapa antes de gerar."));
            string format=aspect switch{ExportAspect.Horizontal=>"horizontal",ExportAspect.Feed=>"feed",_=>"stories"};
            Size size=aspect==ExportAspect.Horizontal?new(1920,1080):aspect==ExportAspect.Feed?new(1080,1350):new(1080,1920);
            var image=Background(match,brand,size,kind==ArtKind.Announcement?"promo":format,kind);
            try
            {
                using var g=Graphics.FromImage(image);Setup(g);
                if(kind==ArtKind.Announcement)JgArtRenderer.Announcement(g,match,brand,size);
                else JgArtRenderer.Result(g,match,catalog,brand,kind,format,size,kind==ArtKind.MapStats?map:null);
                string name=kind switch{ArtKind.Announcement=>"anuncio",ArtKind.Results=>"resultado-serie",ArtKind.MapStats=>"estatisticas-mapa-"+(match.Maps.IndexOf(map!)+1).ToString("00"),_=>"estatisticas-serie"};
                return [new(name+"-"+format,image)];
            }
            catch{image.Dispose();throw;}
        }
    }
    static Bitmap Background(SiegeMatch match,ImageBrand brand,Size size,string format,ArtKind kind)
    {
        var image=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppArgb);
        using var g=Graphics.FromImage(image);Setup(g);g.Clear(Color.FromArgb(12,18,25));
        string? custom=MatchFiles.Resolve(match,brand.BackgroundFor(kind));
        Photo(g,custom??Asset("background-"+format+".jpg"),new(0,0,size.Width,size.Height));
        if(custom is not null){using var shade=new SolidBrush(Color.FromArgb(105,0,0,0));g.FillRectangle(shade,0,0,size.Width,size.Height);}
        return image;
    }
    static Bitmap Veto(SiegeMatch match,SiegeCatalog catalog,ImageBrand brand)
    {
        var image=new Bitmap(1920,1080,PixelFormat.Format32bppArgb);
        try
        {
            using var g=Graphics.FromImage(image);Setup(g);HeaderVeto(g,match,brand);
            string lang=L.Current switch{"en"=>"EN","es"=>"ES",_=>"PTBR"};
            var order=match.Veto.Concat([catalog.Maps.Single(m=>!match.Veto.Contains(m.Id)).Id]).ToArray();
            for(int i=0;i<9;i++)
            {
                var r=new RectangleF(106+i*192,333,174,620);
                using var clip=Shape(r);var state=g.Save();g.SetClip(clip);
                using var card=Image.FromFile(Asset(order[i]+"-"+lang+".png"));
                bool ban=i<8&&match.Action(i)=="BAN";
                if(ban){using var attrs=new ImageAttributes();attrs.SetColorMatrix(Gray());g.DrawImage(card,Rectangle.Round(r),0,0,card.Width,card.Height,GraphicsUnit.Pixel,attrs);}
                else g.DrawImage(card,r);
                bool actorTop=i%2==1;int actor=i<8?match.Actor(i):match.Maps.Single(m=>m.MapId==order[i]).Picker;
                if(ban)T(g,"BAN",new(r.X+8,r.Y+(actorTop?526:36),r.Width-16,62),52,Muted);
                else
                {
                    var map=match.Maps.Single(m=>m.MapId==order[i]);
                    string icon=map.InitialAttack==map.Picker?"Icon_Attacker.png":"Icon_Defender.png";
                    Img(g,Asset(icon),new(r.X+68,r.Y+(actorTop?535:75),40,40));
                }
                float teamY=r.Y+(actorTop?58:475);
                TeamLogo(g,match,brand,actor,new(r.X+48,teamY,80,80));
                T(g,match.Team(actor).ToUpperInvariant(),new(r.X+6,teamY+85,r.Width-12,34),24,Color.White,false);
                g.Restore(state);
            }
            return image;
        }
        catch{image.Dispose();throw;}
    }
    static void HeaderVeto(Graphics g,SiegeMatch match,ImageBrand brand)
    {
        TeamLogo(g,match,brand,0,new(628,85,154,151));TeamLogo(g,match,brand,1,new(1141,85,154,151));
        T(g,match.TeamA.ToUpperInvariant(),new(250,112,355,95),64,Color.White);
        T(g,match.TeamB.ToUpperInvariant(),new(1315,112,355,95),64,Color.White);
        T(g,"VS",new(820,110,280,95),78,Muted);
        if(!string.IsNullOrWhiteSpace(brand.EventName))T(g,brand.EventName.ToUpperInvariant(),new(500,257,920,40),26,Muted,false);
    }
    static void TeamLogo(Graphics g,SiegeMatch match,ImageBrand brand,int team,RectangleF r)
    {
        using var p=Shape(r);using var shade=new SolidBrush(Color.FromArgb(150,12,20,29));g.FillPath(shade,p);using var pen=new Pen(team==0?ResultRenderer.Blue:ResultRenderer.Orange,2);g.DrawPath(pen,p);
        string file=MatchFiles.Resolve(match,team==0?brand.TeamALogo:brand.TeamBLogo)??Path.Combine(AppContext.BaseDirectory,"assets","promo","neutral-white.png");
        Img(g,file,new(r.X+r.Width*.10f,r.Y+r.Height*.13f,r.Width*.8f,r.Height*.74f));
    }
    static void Setup(Graphics g){g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;g.CompositingQuality=CompositingQuality.HighQuality;}
    static GraphicsPath Shape(RectangleF r)
    {
        float c=Math.Min(12,Math.Min(r.Width,r.Height)*.09f);var p=new GraphicsPath();p.AddPolygon(new PointF[]{new(r.X+c,r.Y),new(r.Right-c,r.Y),new(r.Right,r.Y+c),new(r.Right,r.Bottom-c),new(r.Right-c,r.Bottom),new(r.X+c,r.Bottom),new(r.X,r.Bottom-c),new(r.X,r.Y+c)});return p;
    }
    static void Panel(Graphics g,RectangleF r){using var path=Shape(r);using var b=new SolidBrush(Color.FromArgb(130,10,15,23));g.FillPath(b,path);using var pen=new Pen(Muted,2);g.DrawPath(pen,path);}
    static void T(Graphics g,string text,RectangleF r,float size,Color color,bool title=true,StringAlignment align=StringAlignment.Center)
    {
        using var f=new StringFormat{Alignment=align,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};
        var family=(title?heading:mono).Families[0];
        using var initial=new Font(family,size,FontStyle.Regular,GraphicsUnit.Pixel);
        float measured=g.MeasureString(text,initial).Width;float adjusted=measured>r.Width?Math.Max(12,size*r.Width/measured):size;
        using var font=new Font(family,adjusted,FontStyle.Regular,GraphicsUnit.Pixel);using var brush=new SolidBrush(color);g.DrawString(text,font,brush,r,f);
    }
    // One em size for every nickname in this format. Long names trim, never shrink independently.
    internal static void PlayerText(Graphics g,string text,RectangleF r,float em,bool title=false,StringAlignment align=StringAlignment.Near)
    {
        using var font=new Font((title?heading:medium).Families[0],em,FontStyle.Regular,GraphicsUnit.Pixel);
        using var brush=new SolidBrush(Color.White);
        using var f=new StringFormat{Alignment=align,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap,Trimming=StringTrimming.EllipsisCharacter};
        g.DrawString(text,font,brush,r,f);
    }
    internal static float InkWidth(string text,float height,bool title=true)
    {
        using var path=new GraphicsPath();using var f=new StringFormat(StringFormat.GenericTypographic);
        path.AddString(text,(title?heading:mono).Families[0],(int)FontStyle.Regular,100,PointF.Empty,f);
        var b=path.GetBounds();return b.Height>0?b.Width*height/b.Height:0;
    }
    internal static void InkText(Graphics g,string text,RectangleF r,Color color,bool title=true,StringAlignment align=StringAlignment.Center,bool useMedium=false)
    {
        if(string.IsNullOrWhiteSpace(text)||r.Width<=0||r.Height<=0)return;
        using var path=new GraphicsPath();using var f=new StringFormat(StringFormat.GenericTypographic);
        path.AddString(text,(useMedium?medium:title?heading:mono).Families[0],(int)FontStyle.Regular,100,PointF.Empty,f);
        var b=path.GetBounds();if(b.Width<=0||b.Height<=0)return;
        float s=Math.Min(r.Width/b.Width,r.Height/b.Height);
        float x=align==StringAlignment.Near?r.X:align==StringAlignment.Far?r.Right-b.Width*s:r.X+(r.Width-b.Width*s)/2;
        float y=r.Y+(r.Height-b.Height*s)/2;
        using var matrix=new Matrix(s,0,0,s,x-b.X*s,y-b.Y*s);path.Transform(matrix);
        using var brush=new SolidBrush(color);g.FillPath(brush,path);
    }
    static void Img(Graphics g,string path,RectangleF r){using var image=Image.FromFile(path);float s=Math.Min(r.Width/image.Width,r.Height/image.Height);g.DrawImage(image,r.X+(r.Width-image.Width*s)/2,r.Y+(r.Height-image.Height*s)/2,image.Width*s,image.Height*s);}
    static ColorMatrix Gray()=>new([[.299f,.299f,.299f,0,0],[.587f,.587f,.587f,0,0],[.114f,.114f,.114f,0,0],[0,0,0,1,0],[0,0,0,0,1]]);
    internal static void Photo(Graphics g,string file,RectangleF r,bool gray=false)
    {
        using var im=Image.FromFile(file);float s=Math.Max(r.Width/im.Width,r.Height/im.Height);float sw=r.Width/s,sh=r.Height/s;
        using var attr=new ImageAttributes();if(gray)attr.SetColorMatrix(Gray());
        g.DrawImage(im,Rectangle.Round(r),(im.Width-sw)/2,(im.Height-sh)/2,sw,sh,GraphicsUnit.Pixel,attr);
    }
}

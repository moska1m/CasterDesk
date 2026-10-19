using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Text.Json;

namespace CasterDesk.Core;

public enum ExportAspect { Stories, Feed, Horizontal }
public sealed class ExportBrand { string backgroundStyle="Classic"; public string BackgroundStyle {get=>backgroundStyle;set=>backgroundStyle=value=="JG"?"Community":value;} public string EventName {get;set;}=""; public string LogoFile {get;set;}=""; public string CasterName {get;set;}=""; public string Channel {get;set;}=""; public string Platform {get;set;}="Twitch"; public List<string>? Platforms {get;set;} public Dictionary<string,string> PlatformChannels {get;set;}=new(); public string[] SelectedPlatforms()=> (Platforms??new List<string>{Platform}).Where(p=>p is "Twitch" or "YouTube" or "Kick").Distinct().ToArray(); public string ChannelFor(string p)=>PlatformChannels.TryGetValue(p,out var value)&&!string.IsNullOrWhiteSpace(value)?value.Trim():Channel.Trim(); }
public sealed record ResultPage(string Name, Bitmap Image) : IDisposable {public void Dispose()=>Image.Dispose();}
public static class ResultRenderer
{
    static readonly Color Bg=Color.FromArgb(19,25,31),Panel=Color.FromArgb(27,35,43),Muted=Color.FromArgb(182,194,206),Line=Color.FromArgb(68,82,94);
    public static readonly Color Blue=Color.FromArgb(64,143,179),Orange=Color.FromArgb(255,119,55);
    static readonly Dictionary<Size,Bitmap> backgrounds=[];
    public static List<MapRecord> Played(SiegeMatch match)=>match.Maps.Where(m=>m.Rounds.Count>0).ToList();
    public static List<ResultPage> Render(SiegeMatch match,SiegeCatalog catalog,ExportBrand brand,ExportAspect aspect,MapRecord? only=null)
    {
        var maps=only is null?Played(match):new List<MapRecord>{only};
        if(maps.Count==0)throw new InvalidOperationException(L.T("Registre pelo menos um round antes de gerar a imagem."));
        var pages=new List<ResultPage>();
        try
        {
            if(aspect==ExportAspect.Feed || (aspect==ExportAspect.Stories && maps.Count>3))
            {
                if(only is null)pages.Add(new("01_capa",Draw(match,catalog,brand,maps,aspect,true)));
                for(int i=0;i<maps.Count;i++)pages.Add(new($"{i+2:00}_mapa_{match.Maps.IndexOf(maps[i])+1:00}",Draw(match,catalog,brand,[maps[i]],aspect,false)));
            }
            else if(aspect==ExportAspect.Horizontal && maps.Count>3)
            {
                for(int start=0;start<maps.Count;start+=3)
                    pages.Add(new($"serie_{start/3+1:00}",Draw(match,catalog,brand,maps.Skip(start).Take(3).ToList(),aspect,false)));
            }
            else pages.Add(new(only is null?"serie":$"mapa_{match.Maps.IndexOf(only)+1:00}",Draw(match,catalog,brand,maps,aspect,false)));
            return pages;
        }
        catch {foreach(var page in pages)page.Dispose();throw;}
    }
    static Bitmap Base(Size size)
    {
        if(!backgrounds.TryGetValue(size,out var image))
        {
            image=new Bitmap(size.Width,size.Height);using var g=Graphics.FromImage(image);
            using var bg=new LinearGradientBrush(new Rectangle(Point.Empty,size),Bg,Color.FromArgb(10,16,22),60);g.FillRectangle(bg,new Rectangle(Point.Empty,size));
            using var pen=new Pen(Color.FromArgb(15,130,155,175));for(int x=-size.Height;x<size.Width;x+=90)g.DrawLine(pen,x,0,x+size.Height,size.Height);
            backgrounds[size]=image;
        }
        return new Bitmap(image);
    }
    // Symmetric side columns keep the title centered independently of branding.
    internal static (RectangleF Left, RectangleF Center, RectangleF Right) HeaderBounds(int width,float top)
        => (new(40,top,250,150),new(305,top,width-610,98),new(width-290,top,250,150));
    static Bitmap Draw(SiegeMatch match,SiegeCatalog c,ExportBrand brand,List<MapRecord> maps,ExportAspect aspect,bool cover)
    {
        int w=aspect==ExportAspect.Horizontal?1920:1080,h=aspect==ExportAspect.Horizontal?1080:aspect==ExportAspect.Feed?1350:1920;
        var image=Base(new(w,h));
        if(brand.BackgroundStyle=="Community")
        {
            using var background=Image.FromFile(Path.Combine(AppContext.BaseDirectory,"assets","promo","background.png"));
            using var canvas=Graphics.FromImage(image);
            canvas.InterpolationMode=InterpolationMode.HighQualityBicubic;
            // Preserve the complete frame for every aspect; trim the PSD bleed.
            canvas.DrawImage(background,new Rectangle(0,0,w,h),new Rectangle(4,5,1080,1350),GraphicsUnit.Pixel);
            using var shade=new SolidBrush(Color.FromArgb(125,8,15,22));
            canvas.FillRectangle(shade,0,0,w,h);
        }
        try
        {
            using var g=Graphics.FromImage(image);g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
            float safe=aspect==ExportAspect.Stories?100:30;
            var header=HeaderBounds(w,safe);
            string logo=Path.Combine(AppContext.BaseDirectory,"assets","branding",match.Format=="MD1"?"siege-full.png":"siege-mark.png");
            Img(g,logo,new(header.Left.X+(header.Left.Width-(match.Format=="MD1"?190:76))/2,safe,match.Format=="MD1"?190:76,76));
            if(!string.IsNullOrEmpty(brand.LogoFile))Img(g,brand.LogoFile,new(header.Left.X+(header.Left.Width-190)/2,safe+80,190,70));
            T(g,string.IsNullOrWhiteSpace(brand.EventName)?L.T("RESULTADO DA PARTIDA"):brand.EventName.ToUpperInvariant(),new(header.Center.X,safe,header.Center.Width,50),26,Color.White,true);
            bool series=maps.Count>1||cover;
            string title=series?L.F($"{L.FormatName(match.Format)} · RESULTADO DA SÉRIE"):L.F($"{L.FormatName(match.Format)} · MAPA {match.Maps.IndexOf(maps[0])+1:00} · {c.Maps.Single(m=>m.Id==maps[0].MapId).DisplayName}");
            T(g,title,new(header.Center.X,safe+54,header.Center.Width,44),22,Muted,true);
            int a=series?match.Maps.Count(m=>m.Score().Winner==0):maps[0].Score().A,b=series?match.Maps.Count(m=>m.Score().Winner==1):maps[0].Score().B;
            bool hasCaster=!string.IsNullOrWhiteSpace(brand.CasterName)||!string.IsNullOrWhiteSpace(brand.Channel)||brand.SelectedPlatforms().Any(p=>!string.IsNullOrWhiteSpace(brand.ChannelFor(p)));
            if(hasCaster)Caster(g,brand,header.Right);
            float scoreY=safe+((hasCaster||!string.IsNullOrEmpty(brand.LogoFile))?160:105);float scoreHeight=cover?140:100;
            T(g,match.TeamA.ToUpperInvariant(),new(45,scoreY,w*.33f-45,scoreHeight),w>1100?52:40,Blue,true);
            T(g,$"{a} × {b}",new(w*.33f,scoreY-8,w*.34f,scoreHeight),cover?120:88,a==b?Color.White:a>b?Blue:Orange,true);
            T(g,match.TeamB.ToUpperInvariant(),new(w*.67f,scoreY,w*.33f-45,scoreHeight),w>1100?52:40,Orange,true);
            bool complete=series?match.Complete:maps[0].Confirmed;
            string state=series?L.T("PLACAR EM MAPAS"):L.T("PLACAR EM ROUNDS");
            if(!complete)state+=" · "+(maps.All(m=>m.Score().Winner>=0)?L.T("NÃO CONFIRMADO"):L.T("RESULTADO PARCIAL"));
            T(g,state,new(40,scoreY+scoreHeight,w-80,30),18,complete?Muted:Color.FromArgb(255,201,126),false);
            float top=scoreY+scoreHeight+45,bottom=h-(aspect==ExportAspect.Stories?170:65),height=bottom-top;
            if(cover)
            {
                float cardH=(height-20*(maps.Count-1))/maps.Count;
                for(int i=0;i<maps.Count;i++)CoverMap(g,match,c,maps[i],new(45,top+i*(cardH+20),w-90,cardH));
            }
            else if(aspect==ExportAspect.Horizontal)
            {
                float cardW=(w-80-20*(maps.Count-1))/maps.Count;
                for(int i=0;i<maps.Count;i++)Map(g,match,c,maps[i],new(40+i*(cardW+20),top,cardW,height),maps.Count==1);
            }
            else
            {
                float cardH=(height-20*(maps.Count-1))/maps.Count;
                for(int i=0;i<maps.Count;i++)Map(g,match,c,maps[i],new(40,top+i*(cardH+20),w-80,cardH),maps.Count==1);
            }
            T(g,$"CASTERDESK  •  {match.Created:dd/MM/yyyy}",new(40,bottom+15,w-80,35),20,Muted,true);
            return image;
        }
        catch {image.Dispose();throw;}
    }
    static void CoverMap(Graphics g,SiegeMatch match,SiegeCatalog c,MapRecord map,RectangleF r)
    {
        var info=c.Maps.Single(m=>m.Id==map.MapId);Photo(g,SiegeCatalog.Asset(info.Image),r);
        bool compact=r.Height<250;
        T(g,L.F($"MAPA {match.Maps.IndexOf(map)+1:00} · {info.DisplayName}"),new(r.X+20,r.Y+(compact?8:20),r.Width-40,compact?42:55),compact?32:38,Color.White,true);
        var scoreArea=compact?new RectangleF(r.X+20,r.Y+52,r.Width-40,r.Height-82):new RectangleF(r.X+20,r.Y+r.Height/2-55,r.Width-40,110);
        var s=map.Score();T(g,$"{s.A} × {s.B}",scoreArea,compact?68:102,s.A==s.B?Color.White:s.A>s.B?Blue:Orange,true);
        if(!map.Confirmed)T(g,s.Winner<0?L.T("PARCIAL"):L.T("NÃO CONFIRMADO"),new(r.X+20,r.Bottom-45,r.Width-40,30),18,Muted,true);
    }
    static void Map(Graphics g,SiegeMatch match,SiegeCatalog c,MapRecord map,RectangleF r,bool spacious)
    {
        Fill(g,Panel,r);using var pen=new Pen(Line);g.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height);
        var info=c.Maps.Single(m=>m.Id==map.MapId);float banner=spacious?Math.Min(180,r.Height*.19f):Math.Min(90,r.Height*.19f);
        Photo(g,SiegeCatalog.Asset(info.Image),new(r.X,r.Y,r.Width,banner));
        T(g,L.F($"MAPA {match.Maps.IndexOf(map)+1:00} · {info.DisplayName}"),new(r.X+12,r.Y+8,r.Width-24,banner*.42f),spacious?40:26,Color.White,true);
        var score=map.Score();T(g,$"{match.TeamA}  {score.A} × {score.B}  {match.TeamB}",new(r.X+12,r.Y+banner*.46f,r.Width-24,banner*.45f),spacious?48:30,Color.White,true);
        var body=new RectangleF(r.X+12,r.Y+banner+10,r.Width-24,r.Height-banner-20);
        if(spacious && r.Width<1200)
        {
            float tableH=body.Height*.55f;
            Rounds(g,map,new(body.X,body.Y,body.Width,tableH),match);
            Bans(g,match,c,map,new(body.X,body.Y+tableH+12,body.Width,body.Height-tableH-12),true);
        }
        else if(r.Width>850)
        {
            Rounds(g,map,new(body.X,body.Y,body.Width*.60f,body.Height),match);
            Bans(g,match,c,map,new(body.X+body.Width*.62f,body.Y,body.Width*.38f,body.Height),false);
        }
        else
        {
            float tableH=body.Height*.66f;Rounds(g,map,new(body.X,body.Y,body.Width,tableH),match);
            Bans(g,match,c,map,new(body.X,body.Y+tableH+8,body.Width,body.Height-tableH-8),true);
        }
    }
    static void Rounds(Graphics g,MapRecord map,RectangleF r,SiegeMatch match)
    {
        float titleH=26;T(g,L.T("ROUND A ROUND"),new(r.X,r.Y,r.Width,titleH),20,Muted,true);
        float rowH=RoundHeight(r.Height,map.Rounds.Count);float y=r.Y+titleH;
        float[] widths=[r.Width*.12f,r.Width*.56f,r.Width*.32f];string[] headers=[L.T("ROUND"),L.T("BOMBSITE"),L.T("VENCEDOR")];
        float x=r.X;for(int i=0;i<3;i++){T(g,headers[i],new(x,y,widths[i],rowH),Math.Min(18,rowH*.65f),Muted,true);x+=widths[i];}
        for(int i=0;i<map.Rounds.Count;i++)
        {
            y+=rowH;var round=map.Rounds[i];if(i%2==0)Fill(g,Color.FromArgb(33,43,53),new(r.X,y,r.Width,rowH));
            using var pen=new Pen(Line);g.DrawLine(pen,r.X,y+rowH,r.Right,y+rowH);
            float font=Math.Min(26,rowH*.76f);T(g,$"{i+1:00}",new(r.X,y,widths[0],rowH),font,Color.White,false);
            T(g,string.IsNullOrWhiteSpace(round.Site)?L.T("Não registrado"):L.T(round.Site),new(r.X+widths[0]+3,y,widths[1]-6,rowH),font,Color.White,false);
            T(g,match.Team(round.Winner),new(r.X+widths[0]+widths[1]+7,y,widths[2]-14,rowH),font,round.Winner==0?Blue:Orange,true);
        }
    }
    static void Bans(Graphics g,SiegeMatch match,SiegeCatalog c,MapRecord map,RectangleF r,bool wide)
    {
        T(g,L.T("BANS DE OPERADORES"),new(r.X,r.Y,r.Width,25),18,Muted,true);
        float blockW=wide?(r.Width-16)/2:r.Width;
        float blockH=wide?r.Height-28:(r.Height-36)/2;
        for(int half=0;half<2;half++)
        {
            float x=r.X+(wide?half*(blockW+16):0),top=r.Y+28+(wide?0:half*(blockH+8));
            int attacker=half==0?map.InitialAttack:1-map.InitialAttack;
            if(map.InitialAttack<0)T(g,L.T("LADOS NÃO DEFINIDOS"),new(x,top,blockW,24),17,Muted,true);
            else {T(g,$"{match.TeamA.ToUpperInvariant()} {(attacker==0?L.T("ATACANDO"):L.T("DEFENDENDO"))}",new(x,top,blockW/2,24),16,Blue,true);T(g,$"{match.TeamB.ToUpperInvariant()} {(attacker==1?L.T("ATACANDO"):L.T("DEFENDENDO"))}",new(x+blockW/2,top,blockW/2,24),16,Orange,true);}
            float rowH=(blockH-26)/2;
            for(int role=0;role<2;role++)
            {
                string kind=role==0?"ATQ":"DEF";

                float y=top+26+role*rowH;

                int[] slots=RoleSlots(map,half,kind);
                for(int j=0;j<slots.Length;j++)
                {
                    var op=c.Operators.FirstOrDefault(o=>o.Id==map.Bans[slots[j]]);
                    if(op is null)continue;
                    float cellW=blockW/3,cellX=x+j*cellW;
                    float icon=Math.Min(Math.Min(76,cellW-12),Math.Max(16,rowH-25));
                    Img(g,SiegeCatalog.Asset(op.Icon),new(cellX+(cellW-icon)/2,y+2,icon,icon));
                    T(g,op.Name.ToUpperInvariant(),new(cellX+2,y+3+icon,cellW-4,21),18,Color.White,true);
                }
                if(role==0){using var line=new Pen(Line);g.DrawLine(line,x+8,y+rowH-1,x+blockW-8,y+rowH-1);}
            }
        }
    }
    internal static float RoundHeight(float height,int count)=>Math.Min(44,(height-26)/(Math.Max(10,count)+1));
    static void Caster(Graphics g,ExportBrand brand,RectangleF r)
    {
        var platforms=brand.SelectedPlatforms();
        T(g,brand.CasterName,new(r.X,r.Y,r.Width,35),26,Color.White,true);
        bool shared=platforms.Select(brand.ChannelFor).Distinct().Count()<=1;
        if(shared)
        {
            T(g,platforms.Length>0?brand.ChannelFor(platforms[0]):brand.Channel,new(r.X,r.Y+35,r.Width,30),22,Color.White,true);
            float width=platforms.Length*52;
            for(int i=0;i<platforms.Length;i++)PlatformIcon(g,platforms[i],r.X+(r.Width-width)/2+i*52+5,r.Y+73,1.35f);
        }
        else for(int i=0;i<platforms.Length;i++)
        {
            float y=r.Y+40+i*35;PlatformIcon(g,platforms[i],r.X+8,y,1);
            T(g,brand.ChannelFor(platforms[i]),new(r.X+47,y,r.Width-52,31),20,Color.White,true);
        }
    }
    static void PlatformIcon(Graphics g,string platform,float x,float y,float scale)
    {
        var state=g.Save();g.TranslateTransform(x,y);g.ScaleTransform(scale,scale);
        using var white=new SolidBrush(Color.White);using var dark=new SolidBrush(Bg);
        if(platform=="YouTube")
        {using var shape=new GraphicsPath();shape.AddArc(0,3,8,8,180,90);shape.AddArc(20,3,8,8,270,90);shape.AddArc(20,15,8,8,0,90);shape.AddArc(0,15,8,8,90,90);shape.CloseFigure();g.FillPath(white,shape);g.FillPolygon(dark,[new PointF(11,8),new PointF(11,19),new PointF(20,13.5f)]);}
        else if(platform=="Kick")
        {g.FillRectangle(white,2,2,7,23);g.FillRectangle(white,16,2,9,7);g.FillRectangle(white,9,9,9,9);g.FillRectangle(white,16,18,9,7);}
        else
        {g.FillPolygon(white,[new PointF(2,1),new PointF(27,1),new PointF(27,18),new PointF(19,25),new PointF(13,25),new PointF(8,29),new PointF(8,25),new PointF(2,25)]);g.FillRectangle(dark,6,4,17,14);g.FillRectangle(white,11,7,3,8);g.FillRectangle(white,18,7,3,8);}
        g.Restore(state);
    }
    internal static int[] RoleSlots(MapRecord map,int half,string role)=>Enumerable.Range(half*6,6).Where(slot=>map.BanRole(slot)==role).ToArray();
    static void Fill(Graphics g,Color color,RectangleF rect){using var b=new SolidBrush(color);g.FillRectangle(b,rect);}
    static void T(Graphics g,string text,RectangleF r,float size,Color color,bool bold)
    {
        using var format=new StringFormat {Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.None};
        float selected=size;using(var initial=new Font("Segoe UI",selected,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))
        {
            var measured=g.MeasureString(text,initial,new SizeF(r.Width,10000),format);
            while((measured.Width>r.Width || measured.Height>r.Height) && selected>9){selected-=1;using var smaller=new Font("Segoe UI",selected,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);measured=g.MeasureString(text,smaller,new SizeF(r.Width,10000),format);}
        }
        using var font=new Font("Segoe UI",selected,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);using var brush=new SolidBrush(color);if(selected>=70 && (color==Blue || color==Orange)){using var path=new GraphicsPath();path.AddString(text,font.FontFamily,(int)font.Style,selected,r,format);using var outline=new Pen(Color.FromArgb(210,240,245,250),1.1f){LineJoin=LineJoin.Round};g.DrawPath(outline,path);g.FillPath(brush,path);}else g.DrawString(text,font,brush,r,format);
    }
    static void Img(Graphics g,string file,RectangleF r)
    {
        if(!File.Exists(file))return;using var img=Image.FromFile(file);float scale=Math.Min(r.Width/img.Width,r.Height/img.Height);g.DrawImage(img,r.X+(r.Width-img.Width*scale)/2,r.Y+(r.Height-img.Height*scale)/2,img.Width*scale,img.Height*scale);
    }
    static void Photo(Graphics g,string file,RectangleF r)
    {
        if(File.Exists(file)){using var img=Image.FromFile(file);float scale=Math.Max(r.Width/img.Width,r.Height/img.Height);var state=g.Save();g.SetClip(r);g.DrawImage(img,r.X+(r.Width-img.Width*scale)/2,r.Y+(r.Height-img.Height*scale)/2,img.Width*scale,img.Height*scale);g.Restore(state);}
        Fill(g,Color.FromArgb(155,8,15,22),r);
    }
}


using System.Drawing.Drawing2D;
using System.Text.Json;

namespace CronoLive;

// Layout coordinates are taken from JG's PSDs, in each document's native size.
// A single uniform transform preserves the story PSD's 765 × 1360 proportions.
internal static class JgArtRenderer
{
    internal sealed class Layout
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public Dictionary<string,int[]> Bounds { get; set; }=[];
        public RectangleF R(int index){var b=Bounds[index.ToString()];return new(b[0],b[1],b[2]-b[0],b[3]-b[1]);}
    }
    static Layout Load(string format)=>JsonSerializer.Deserialize<Layout>(File.ReadAllText(StudioRenderer.Asset("layout-"+format+".json")))!;
    static readonly Color Muted=Color.FromArgb(175,186,196),Red=Color.FromArgb(255,57,39);
    static void Layer(Graphics g,string format,int index,RectangleF r)
    {using var image=Image.FromFile(StudioRenderer.Asset($"jg-{format}-{index}.png"));g.DrawImage(image,r);if(format!="promo"&&index is 2 or 13){using var path=Frame(r);using var pen=new Pen(Muted,2);g.DrawPath(pen,path);}}
    static void Text(Graphics g,string text,RectangleF r,bool heading=true,Color? color=null,StringAlignment align=StringAlignment.Center,bool medium=false)
        =>StudioRenderer.InkText(g,text,r,color??Color.White,heading,align,medium);
    static string Logo(SiegeMatch match,ImageBrand brand,int team)=>MatchFiles.Resolve(match,team==0?brand.TeamALogo:brand.TeamBLogo)??Path.Combine(AppContext.BaseDirectory,"assets","promo","neutral-white.png");
    static void ImageFit(Graphics g,string file,RectangleF r)
    {using var image=Image.FromFile(file);float s=Math.Min(r.Width/image.Width,r.Height/image.Height);g.DrawImage(image,r.X+(r.Width-image.Width*s)/2,r.Y+(r.Height-image.Height*s)/2,image.Width*s,image.Height*s);}
    static void Team(Graphics g,Layout layout,string format,int frame,int logo,SiegeMatch match,ImageBrand brand,int team)
    {Layer(g,format,frame,layout.R(frame));ImageFit(g,Logo(match,brand,team),layout.R(logo));}
    static RectangleF Wider(RectangleF r,float width)=>new(r.X+r.Width/2-width/2,r.Y,width,r.Height);
    static void Transform(Graphics g,Layout layout,Size target)
    {float s=Math.Min((float)target.Width/layout.Width,(float)target.Height/layout.Height);g.TranslateTransform((target.Width-layout.Width*s)/2,(target.Height-layout.Height*s)/2);g.ScaleTransform(s,s);}
    internal static void Announcement(Graphics g,SiegeMatch match,ImageBrand brand,Size target)
    {
        var layout=Load("promo");var state=g.Save();Transform(g,layout,target);
        string title=string.IsNullOrWhiteSpace(brand.EventName)?L.T("CAMPEONATO"):brand.EventName.ToUpperInvariant();
        var words=title.Split(' ',StringSplitOptions.RemoveEmptyEntries);
        if(words.Length>1)
        {
            int split=Enumerable.Range(1,words.Length-1).OrderBy(i=>Math.Abs(string.Join(" ",words.Take(i)).Length-string.Join(" ",words.Skip(i)).Length)).First();
            string first=string.Join(" ",words.Take(split)),second=string.Join(" ",words.Skip(split));
            var r=layout.R(12);Text(g,first,new(r.X,r.Y,r.Width,114));Text(g,second,new(r.X,r.Y+140,r.Width,114));
        }
        else Text(g,title,layout.R(12));
        Team(g,layout,"promo",2,3,match,brand,0);Team(g,layout,"promo",6,7,match,brand,1);
        Text(g,match.TeamA.ToUpperInvariant(),Wider(layout.R(4),301),false,Muted);
        Text(g,match.TeamB.ToUpperInvariant(),Wider(layout.R(8),301),false,Muted);
        Text(g,"VS",layout.R(11),false,Muted);
        if(!string.IsNullOrWhiteSpace(brand.BroadcastChannel))
        {
            Text(g,L.T("ASSISTA EM:"),layout.R(9),false,Muted);
            Text(g,brand.BroadcastChannel.Trim().ToUpperInvariant(),layout.R(10),true,Red);
        }
        g.Restore(state);
    }
    internal static void Result(Graphics g,SiegeMatch match,SiegeCatalog catalog,ImageBrand brand,ArtKind kind,string format,Size target,MapRecord? map)
    {
        var layout=Load(format);var state=g.Save();Transform(g,layout,target);
        string title=kind==ArtKind.MapStats?L.T("ESTATÍSTICAS DO MAPA"):L.T("RESULTADO");
        Text(g,title,Wider(layout.R(102),layout.Width*.83f));
        Text(g,L.FormatName(match.Format)+(string.IsNullOrWhiteSpace(brand.EventName)?"":" - "+brand.EventName.ToUpperInvariant()),Wider(layout.R(103),layout.Width*.8f),false,Red);
        Team(g,layout,format,93,94,match,brand,0);Team(g,layout,format,97,98,match,brand,1);
        var left=layout.R(95);var right=layout.R(99);
        Text(g,match.TeamA.ToUpperInvariant(),new(layout.R(91).Left,left.Y,layout.R(93).Left-layout.R(91).Left-18,left.Height));
        Text(g,match.TeamB.ToUpperInvariant(),new(layout.R(97).Right+18,right.Y,layout.R(91).Right-layout.R(97).Right-18,right.Height));
        int a=match.Maps.Count(m=>m.Confirmed&&m.Score().Winner==0),b=match.Maps.Count(m=>m.Confirmed&&m.Score().Winner==1);
        if(map is not null){a=map.Score().A;b=map.Score().B;}
        CompactScore(g,a,b,layout.R(100),false);
        Maps(g,catalog,map is null?match.Maps:new List<MapRecord>{map},layout,format);
        var summary=map is null?StatisticsData.ForSeries(match):StatisticsData.ForMap(map);
        if(summary.Players.Count>0)
        {
            Statistics(g,match,brand,summary,layout,format);
            var best=summary.Players.FirstOrDefault(p=>p.Key==brand.Mvp) ?? (string.IsNullOrEmpty(brand.Mvp)?StatisticsData.SuggestMvp(match,map):null);
            if(best is not null)
            {
                Layer(g,format,2,layout.R(2));Text(g,"MVP",layout.R(3),true,Red);Text(g,best.Name,Wider(layout.R(4),layout.R(2).Right-layout.R(4).Left-35));
                var nameArea=Wider(layout.R(4),layout.R(2).Right-layout.R(4).Left-35);
                var positions=new[]{(7,9),(8,10),(6,11)};
                float start=layout.R(7).Left,end=layout.R(11).Left+38,shift=nameArea.X+nameArea.Width/2-(start+end)/2;
                foreach(var (icon,index,n) in new[]{(7,9,best.Kills),(6,10,best.Deaths),(8,11,best.Assists)})
                {var location=layout.R(positions[index-9].Item1);var ir=layout.R(icon);ir.X=location.X+shift;Layer(g,format,icon,ir);var nr=layout.R(index);Text(g,n?.ToString()??"—",new(nr.X+shift,nr.Y,38,nr.Height),false,null,StringAlignment.Near,true);}
            }
            if(summary.Partial)Text(g,L.T("ESTATÍSTICAS PARCIAIS"),new(layout.R(13).X,layout.R(13).Bottom+3,layout.R(13).Width,15),false,Muted);
        }
        if(!match.Complete&&kind==ArtKind.Results)Text(g,L.T("SÉRIE EM ANDAMENTO"),new(layout.R(78).X,layout.R(78).Bottom+3,layout.R(78).Width,16),false,Muted);
        g.Restore(state);
    }
    internal static void CompactScore(Graphics g,int a,int b,RectangleF r,bool compressed=true)
    {
        float h=r.Height,aw=StudioRenderer.InkWidth(a.ToString(),h,compressed),bw=StudioRenderer.InkWidth(b.ToString(),h,compressed),xh=h*.73f,xw=StudioRenderer.InkWidth("X",xh,compressed);
        float gap=Math.Max(h*.16f,(StudioRenderer.InkWidth($"{a} X {b}",h,compressed)-aw-bw-xw)/2);
        float width=aw+bw+xw+2*gap,scale=Math.Min(1,r.Width/width);
        aw*=scale;bw*=scale;xw*=scale;gap*=scale;h*=scale;xh*=scale;
        float x=r.X+(r.Width-width*scale)/2,y=r.Y+(r.Height-h)/2;
        Text(g,a.ToString(),new(x,y,aw,h),compressed,ResultRenderer.Blue);
        Text(g,"X",new(x+aw+gap,y+h-xh,xw,xh),compressed,Muted);
        Text(g,b.ToString(),new(x+aw+gap+xw+gap,y,bw,h),compressed,ResultRenderer.Orange);
    }
    static void Maps(Graphics g,SiegeCatalog catalog,IReadOnlyList<MapRecord> maps,Layout l,string format)
    {
        var area=l.R(78);if(maps.Count==0)return;
        using var path=Frame(area);var state=g.Save();g.SetClip(path);
        float titleY=l.R(87).Y,scoreY=l.R(88).Y,scoreH=l.R(88).Height;
        for(int i=0;i<maps.Count;i++)
        {
            var map=maps[i];var info=catalog.Maps.Single(m=>m.Id==map.MapId);var r=new RectangleF(area.X+area.Width*i/maps.Count,area.Y,area.Width/maps.Count,area.Height);
            StudioRenderer.Photo(g,SiegeCatalog.Asset(info.Image),r,map.Rounds.Count==0);
            using var shade=new SolidBrush(Color.FromArgb(140,8,14,21));g.FillRectangle(shade,r);
            Text(g,info.DisplayName.ToUpperInvariant(),new(r.X+10,titleY,r.Width-20,l.R(87).Height),false,Muted);
            if(map.Rounds.Count>0){var s=map.Score();CompactScore(g,s.A,s.B,new(r.X+10,scoreY,r.Width-20,scoreH));}
        }
        g.Restore(state);using var pen=new Pen(Muted,2);g.DrawPath(pen,path);
    }
    static GraphicsPath Frame(RectangleF r)
    {var p=new GraphicsPath();float c=10;p.AddPolygon(new PointF[]{new(r.X+c,r.Y),new(r.Right-c,r.Y),new(r.Right,r.Y+c),new(r.Right,r.Bottom-c),new(r.Right-c,r.Bottom),new(r.X+c,r.Bottom),new(r.X,r.Bottom-c),new(r.X,r.Y+c)});return p;}
    static void Statistics(Graphics g,SiegeMatch match,ImageBrand brand,StatisticsSummary summary,Layout l,string format)
    {
        Layer(g,format,13,l.R(13));
        Team(g,l,format,67,68,match,brand,0);Team(g,l,format,70,71,match,brand,1);
        for(int team=0;team<2;team++)
        {
            var rows=summary.Players.Where(p=>p.Team==team).OrderByDescending(p=>p.Kills).ThenBy(p=>p.Deaths).ToList();
            int name=team==0?19:45,k=team==0?18:44,a=team==0?16:42,d=team==0?17:43;
            float first=l.R(name).Y,last=l.R(team==0?39:65).Y,step=(last-first)/Math.Max(4,rows.Count-1);
            for(int i=0;i<rows.Count;i++)
            {
                RectangleF At(int index,float width){var r=l.R(index);return new(r.X+r.Width/2-width/2,first+i*step,width,r.Height);}
                var player=rows[i];var nameRect=l.R(name);Text(g,player.Name,new(nameRect.X,first+i*step,l.R(k).X-nameRect.X-30,nameRect.Height),false,null,StringAlignment.Near,true);
                Text(g,player.Kills?.ToString()??"—",At(k,40),false,null,StringAlignment.Center,true);Text(g,player.Deaths?.ToString()??"—",At(a,40),false,null,StringAlignment.Center,true);Text(g,player.Assists?.ToString()??"—",At(d,40),false,null,StringAlignment.Center,true);
            }
        }
        Text(g,L.T("JOGADOR"),l.R(73),true,Color.FromArgb(114,114,119));
        Layer(g,format,75,l.R(75));Layer(g,format,74,l.R(76));Layer(g,format,76,l.R(74));
    }
}


using System.Drawing.Drawing2D;

namespace CasterDesk.Core;

// Images are owned by the championship cache, never by an individual card.
internal sealed class OperatorBanCard : Button
{
    internal Image? Portrait, OperatorIcon;
    internal string OperatorName="", TeamName="", Stage="", Placeholder="";
    internal Color TeamColor;
    internal bool Pending;
    bool hover;
    static readonly Font NameFont=AppFonts.Heading(12,FontStyle.Bold);
    static readonly Font CompactNameFont=AppFonts.Heading(10,FontStyle.Bold);
    static readonly Font DetailFont=new("Segoe UI",8);
    public OperatorBanCard()
    {
        Dock=DockStyle.Fill;Margin=new Padding(3);Cursor=Cursors.Hand;
        SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.ResizeRedraw,true);
    }
    protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hover=true;Invalidate();}
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hover=false;Invalidate();}
    protected override void OnPaint(PaintEventArgs e)
    {
        if(Width<3 || Height<20)return;
        var g=e.Graphics;g.Clear(BackColor);
        // Keep a standing card even on an ultrawide window. The whole slot remains clickable.
        bool spacious=(FindForm()?.ClientSize.Height??0)>=850*DeviceDpi/96f && Height>=130*DeviceDpi/96f && Width>=100*DeviceDpi/96f;
        int height=Math.Min(Height-1,(int)((spacious?210:140)*DeviceDpi/96f));
        int width=Math.Min(Width-6,Math.Max(42,spacious?Width-6:(int)(height*9f/16)));
        var card=new Rectangle((Width-width)/2,0,width,height);
        using(var fill=new LinearGradientBrush(card,AppTheme.Surface,AppTheme.Field,90f))g.FillRectangle(fill,card);
        if(Portrait is not null)
        {
            float scale=spacious?Math.Min(card.Width/(float)Portrait.Width,(card.Height-35)/(float)Portrait.Height):Math.Max(card.Width/(float)Portrait.Width,card.Height/(Portrait.Height*.7f));
            float sourceWidth=card.Width/scale,sourceHeight=card.Height/scale;
            g.InterpolationMode=InterpolationMode.HighQualityBilinear;
            if(spacious)
            {
                int pw=(int)(Portrait.Width*scale),ph=(int)(Portrait.Height*scale);
                g.DrawImage(Portrait,new Rectangle(card.X+(card.Width-pw)/2,card.Y,pw,ph));
            }
            else g.DrawImage(Portrait,card,new RectangleF((Portrait.Width-sourceWidth)/2,0,sourceWidth,sourceHeight),GraphicsUnit.Pixel);
        }
        bool known=OperatorIcon is not null;
        int footer=Math.Min(56,card.Height/2);
        var foot=new Rectangle(card.X,card.Bottom-footer,card.Width,footer);
        using(var shade=new LinearGradientBrush(foot,Color.FromArgb(200,12,21,33),Color.FromArgb(250,12,21,33),90f))g.FillRectangle(shade,foot);
        if(known)
        {
            int size=Math.Clamp(card.Height/4,22,34);
            g.DrawImage(OperatorIcon!,new Rectangle(card.X+(card.Width-size)/2,Math.Max(3,foot.Top-size+8),size,size));
        }
        else
        {
            var area=new Rectangle(card.X+2,card.Y+5,card.Width-4,Math.Max(18,card.Height-footer-5));
            TextRenderer.DrawText(g,Placeholder,NameFont,area,Enabled?AppTheme.Text:AppTheme.Dark?Color.Silver:Color.DimGray,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);
            if(!Enabled){using var hatch=new HatchBrush(HatchStyle.ForwardDiagonal,Color.FromArgb(30,AppTheme.Text),Color.Transparent);g.FillRectangle(hatch,area);}
        }
        Font nameFont=TextRenderer.MeasureText(g,OperatorName,NameFont,Size.Empty,TextFormatFlags.NoPadding).Width>foot.Width-2?CompactNameFont:NameFont;
        TextRenderer.DrawText(g,OperatorName,nameFont,new Rectangle(foot.X+1,foot.Y+4,foot.Width-2,18),Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(g,TeamName,DetailFont,new Rectangle(foot.X+1,foot.Y+23,foot.Width-2,15),Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.EndEllipsis);
        if(footer>=50)TextRenderer.DrawText(g,Stage,DetailFont,new Rectangle(foot.X+1,foot.Y+38,foot.Width-2,14),Color.FromArgb(196,205,218),TextFormatFlags.HorizontalCenter|TextFormatFlags.EndEllipsis);
        using(var stripe=new SolidBrush(TeamColor))g.FillRectangle(stripe,card.X,card.Bottom-3,card.Width,3);
        using(var edge=new Pen(Pending || Enabled && (hover || Focused)?AppTheme.Interaction:AppTheme.Border,Pending || Enabled && (hover || Focused)?2:1))g.DrawRectangle(edge,card.X,card.Y,card.Width-1,card.Height-1);
        if(Focused)ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(card,-4,-4));
    }
}

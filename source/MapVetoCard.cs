using System.Drawing.Drawing2D;
namespace CasterDesk.Core;

internal sealed class MapVetoCard : Button
{
    internal Image? Thumbnail;
    internal string Caption="", MapName="";
    internal bool Next, Banned;
    bool hover;
    static readonly Font CaptionFont=new("Segoe UI",8);
    static readonly Font MapFont=AppFonts.Heading(13,FontStyle.Bold);
    static readonly Font CompactMapFont=AppFonts.Heading(10,FontStyle.Bold);
    public MapVetoCard(){Dock=DockStyle.Fill;Margin=new Padding(3);Cursor=Cursors.Hand;SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.ResizeRedraw,true);}
    protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hover=true;Invalidate();}
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hover=false;Invalidate();}
    protected override void OnPaint(PaintEventArgs e)
    {
        if(Width<2||Height<2)return;var g=e.Graphics;g.Clear(AppTheme.Field);
        if(Thumbnail is not null)
        {
            float scale=Math.Max(Width/(float)Thumbnail.Width,Height/(float)Thumbnail.Height),w=Width/scale,h=Height/scale;
            g.InterpolationMode=InterpolationMode.HighQualityBilinear;g.DrawImage(Thumbnail,ClientRectangle,new RectangleF((Thumbnail.Width-w)/2,(Thumbnail.Height-h)/2,w,h),GraphicsUnit.Pixel);
            using var shade=new LinearGradientBrush(ClientRectangle,Color.FromArgb(105,Banned?Color.FromArgb(80,22,27):Color.FromArgb(20,60,40)),Color.FromArgb(235,Banned?Color.FromArgb(65,28,33):Color.FromArgb(20,50,38)),90f);g.FillRectangle(shade,ClientRectangle);
        }
        else if(!Enabled){using var hatch=new HatchBrush(HatchStyle.ForwardDiagonal,Color.FromArgb(15,AppTheme.Text),Color.Transparent);g.FillRectangle(hatch,ClientRectangle);}
        if(Enabled && (hover||Focused||Next)){using var glow=new LinearGradientBrush(ClientRectangle,Color.FromArgb(20,AppTheme.Interaction),Color.FromArgb(100,AppTheme.Interaction),90f);g.FillRectangle(glow,ClientRectangle);}
        Color ink=Thumbnail is not null?Color.White:AppTheme.Text;
        int captionHeight=Math.Min(17,Height/2-2),nameHeight=Math.Min(23,Height-captionHeight-4),top=Math.Max(0,(Height-3-captionHeight-nameHeight)/2);
        TextRenderer.DrawText(g,Caption,CaptionFont,new Rectangle(6,top,Width-12,captionHeight),ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(g,MapName,Height<60?CompactMapFont:MapFont,new Rectangle(6,top+captionHeight,Width-12,nameHeight),ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        using var edge=new SolidBrush(Next || Enabled && (hover||Focused)?AppTheme.Interaction:Thumbnail is null?AppTheme.Border:Banned?Color.FromArgb(168,70,70):Color.FromArgb(85,153,112));g.FillRectangle(edge,0,Height-3,Width,3);
        if(Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(3,3,Width-6,Height-6));
    }
}

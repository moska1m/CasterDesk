using System.Drawing.Drawing2D;
namespace CasterDesk.Core;

public sealed class NavigationBar : Panel
{
    readonly TabControl tabs;
    readonly bool primary;
    readonly List<NavigationButton> items=[];
    public Button? SettingsButton {get;}
    static readonly Font BrandFont=AppFonts.Heading(21,FontStyle.Bold);
    public NavigationBar(TabControl tabs,bool primary=false)
    {
        this.tabs=tabs;this.primary=primary;Dock=DockStyle.Top;Height=primary?64:52;DoubleBuffered=true;
        foreach(TabPage page in tabs.TabPages)
        {
            int index=items.Count;var button=new NavigationButton{Text=page.Text.ToUpperInvariant(),AccessibleName=page.Text};
            button.IsCurrent=()=>tabs.SelectedIndex==index;button.Click+=(_,_)=>tabs.SelectedIndex=index;Controls.Add(button);items.Add(button);
        }
        if(primary){var gear=new SettingsGear{AccessibleName=L.T("Configurações e sobre"),Size=new Size(50,50)};SettingsButton=gear;Controls.Add(gear);}
        tabs.SelectedIndexChanged+=(_,_)=>{for(int i=0;i<items.Count;i++){items[i].Active=i==tabs.SelectedIndex;items[i].Invalidate();}};
        for(int i=0;i<items.Count;i++)items[i].Active=i==tabs.SelectedIndex;
    }
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);if(items is null)return;
        int total=items.Sum(b=>b.DesiredWidth);int x=primary?250:12;
        foreach(var item in items){item.SetBounds(x,0,item.DesiredWidth,Height);x+=item.Width;}
        if(SettingsButton is not null)SettingsButton.Location=new Point(Width-SettingsButton.Width-18,(Height-SettingsButton.Height)/2);
    }
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        AppTheme.PaintSurface(e.Graphics,ClientRectangle);
        using var veil=new SolidBrush(Color.FromArgb(AppTheme.Dark?160:20,0,0,0));e.Graphics.FillRectangle(veil,ClientRectangle);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);using var edge=new Pen(AppTheme.Border);e.Graphics.DrawLine(edge,0,Height-1,Width,Height-1);
        if(primary)TextRenderer.DrawText(e.Graphics,"CASTERDESK",BrandFont,new Rectangle(24,0,220,Height),AppTheme.Text,TextFormatFlags.VerticalCenter|TextFormatFlags.Left);
    }
}
sealed class NavigationButton : Button
{
    static readonly Font NavFont=AppFonts.Heading(13,FontStyle.Bold);
    bool hover;
    public bool Active;
    public Func<bool> IsCurrent=()=>false;
    public int DesiredWidth=>Math.Max(170,TextRenderer.MeasureText(Text,NavFont).Width+56);
    public NavigationButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;TabStop=true;}
    protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hover=true;Invalidate();}
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hover=false;Invalidate();}
    protected override void OnPaint(PaintEventArgs e)
    {
        // Paint the same backdrop as the bar without desktop transparency or animation timers.
        AppTheme.PaintSurface(e.Graphics,ClientRectangle);using(var veil=new SolidBrush(Color.FromArgb(AppTheme.Dark?160:20,0,0,0)))e.Graphics.FillRectangle(veil,ClientRectangle);
        var accent=AppTheme.Siege?Color.FromArgb(232,68,76):AppTheme.Accent;
        if(hover){using var glow=new LinearGradientBrush(ClientRectangle,Color.Transparent,Color.FromArgb(65,accent),90);e.Graphics.FillRectangle(glow,ClientRectangle);}
        if(IsCurrent()){using var line=new SolidBrush(accent);int width=(int)(Width*.58);e.Graphics.FillRectangle(line,(Width-width)/2,Height-5,width,4);}
        TextRenderer.DrawText(e.Graphics,Text,NavFont,ClientRectangle,AppTheme.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
        if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(8,8,Width-16,Height-16));
    }
}
sealed class SettingsGear : ThemeButton
{
    bool hover,pressed;
    public bool MenuOpen;
    protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hover=true;Invalidate();}
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hover=false;pressed=false;Invalidate();}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left)pressed=true;Invalidate();}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);pressed=false;Invalidate();}
    protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
    protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;bool active=hover||MenuOpen||Focused;
        g.Clear(Color.FromArgb(18,22,24));
        var bounds=new Rectangle(0,0,Width-1,Height-1);
        if(active)
        {
            using var glow=new LinearGradientBrush(ClientRectangle,Color.FromArgb(24,23,24),pressed?Color.FromArgb(204,42,31):Color.FromArgb(218,57,43),90);
            g.FillRectangle(glow,ClientRectangle);
            using var stripe=new SolidBrush(Color.FromArgb(255,70,53));g.FillRectangle(stripe,0,Height-7,Width,7);
        }
        using(var border=new Pen(active?Color.FromArgb(255,81,65):Color.FromArgb(103,109,112)))g.DrawRectangle(border,bounds);
        g.SmoothingMode=SmoothingMode.AntiAlias;
        // Filled teeth and a cut-out hub stay crisp at any Windows scale.
        void Gear(float x,float y,float radius,int teeth)
        {
            var points=new List<PointF>();
            for(int i=0;i<teeth*4;i++)
            {
                double angle=(i+.5)*Math.PI*2/(teeth*4);
                float r=radius*(i%4 is 1 or 2?1f:.73f);
                points.Add(new(x+(float)Math.Cos(angle)*r,y+(float)Math.Sin(angle)*r));
            }
            using var shape=new GraphicsPath(FillMode.Alternate);shape.AddPolygon(points.ToArray());
            float hole=radius*.36f;shape.AddEllipse(x-hole,y-hole,hole*2,hole*2);
            using var ink=new SolidBrush(Color.White);g.FillPath(ink,shape);
        }
        float sx=Width/50f,sy=Height/50f,scale=Math.Min(sx,sy);
        Gear(15*sx,15*sy,8.1f*scale,8);Gear(32*sx,31*sy,12.5f*scale,8);
    }
}

using System.Drawing.Drawing2D;
namespace CronoLive;

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
        int total=items.Sum(b=>b.DesiredWidth);int x=primary?250:Math.Max(12,(Width-total)/2);
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
    protected override void OnPaint(PaintEventArgs e)
    {
        AppTheme.PaintSurface(e.Graphics,ClientRectangle);using(var veil=new SolidBrush(Color.FromArgb(AppTheme.Dark?160:20,0,0,0)))e.Graphics.FillRectangle(veil,ClientRectangle);
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        void Gear(float x,float y,float r)
        {
            using var pen=new Pen(AppTheme.Text,2);e.Graphics.DrawEllipse(pen,x-r*.66f,y-r*.66f,r*1.32f,r*1.32f);e.Graphics.DrawEllipse(pen,x-r*.23f,y-r*.23f,r*.46f,r*.46f);
            for(int i=0;i<8;i++){double a=i*Math.PI/4; e.Graphics.DrawLine(pen,x+(float)Math.Cos(a)*r*.65f,y+(float)Math.Sin(a)*r*.65f,x+(float)Math.Cos(a)*r,y+(float)Math.Sin(a)*r);}
        }
        Gear(17,14,8);Gear(30,31,12);
        if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(2,2,Width-4,Height-4));
    }
}




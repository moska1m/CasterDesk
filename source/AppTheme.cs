using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;

namespace CronoLive;

public static class AppTheme
{
    public static bool Dark { get; set; }
    public static bool Siege { get; set; } = true;
    public static readonly Font HeadingFont=AppFonts.Heading(10,FontStyle.Bold);
    public static readonly Font TabFont=AppFonts.Heading(11,FontStyle.Regular);
    static readonly Bitmap darkTexture=MakeTexture(true),lightTexture=MakeTexture(false);
    static Bitmap MakeTexture(bool dark)
    {
        var image=new Bitmap(640,500);var random=new Random(17);
        for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)
        {
            double wave=Math.Sin(x*.014+y*.006)*2+Math.Cos(y*.027-x*.01)*1.5+Math.Sin(x*.063+y*.042)*.5;
            int v=(int)(wave+random.NextDouble()*1.6),rise=(int)(12.0*x/640+5.0*y/500);
            image.SetPixel(x,y,dark?Color.FromArgb(29+rise+v,43+rise+v,64+rise+v):Color.FromArgb(230+v-rise/3,239+v-rise/3,248+v-rise/3));
        }
        return image;
    }
    public static Color Border => Siege ? (Dark ? Color.FromArgb(81,105,140) : Color.FromArgb(154,176,200)) : Color.FromArgb(80,89,102);
    public static Color Accent => Color.FromArgb(64,143,179);
    public static Color Interaction => Siege ? Color.FromArgb(231,72,69) : Accent;
    public static void PaintSurface(Graphics g, Rectangle r)
    {
        if(r.Width<=0 || r.Height<=0)return;
        if(Siege){g.DrawImage(Dark?darkTexture:lightTexture,r);return;}
        using var brush=new SolidBrush(Background);g.FillRectangle(brush,r);
    }
    public static Color Background => Siege ? (Dark ? Color.FromArgb(25,38,57) : Color.FromArgb(233,240,248)) : Dark ? Color.FromArgb(24,27,32) : SystemColors.Control;
    public static Color Surface => Siege ? (Dark ? Color.FromArgb(34,49,71) : Color.FromArgb(242,247,252)) : Dark ? Color.FromArgb(34,38,45) : Color.White;
    public static Color Field => Siege ? (Dark ? Color.FromArgb(28,43,63) : Color.White) : Dark ? Color.FromArgb(44,49,58) : SystemColors.Window;
    public static Color Text => Dark ? Color.FromArgb(241,243,245) : Color.Black;
    public static Color Success => Dark ? Color.FromArgb(151,225,173) : Color.DarkGreen;
    public static Color Error => Dark ? Color.FromArgb(255,155,155) : Color.Firebrick;
    sealed class Original { public int Applied=-1; public Color Back, Fore; public Font Font; public Original(Control c){Font=c.Font;Back=c.BackColor;Fore=c.ForeColor; if(Back==Color.FromArgb(24,27,32)||Back==Color.FromArgb(34,38,45)||Back==Color.FromArgb(44,49,58))Back=SystemColors.Control; if(Fore==Color.FromArgb(241,243,245))Fore=Color.Black;} }
    static readonly ConditionalWeakTable<Control,Original> originals=new();
    static readonly ConditionalWeakTable<Control,object> wired=new();
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
    static Color Map(Color c)
    {
        if(!Dark)return c;
        if(c.ToArgb()==Color.FromArgb(64,143,179).ToArgb() || c.ToArgb()==Color.FromArgb(255,119,55).ToArgb())return c;
        if(c.R>240 && c.G>200 && c.G<230 && c.B>200 && c.B<230)return Color.FromArgb(71,41,47);
        if(c.G>225 && c.R<220 && c.B<225)return Color.FromArgb(36,60,49);
        if(c.B>240 && c.R<230)return Color.FromArgb(40,58,76);
        if(c.R<80 && c.B>150)return c;
        return Surface;
    }
    public static void Apply(Control root)
    {
        Capture(root);ApplyTree(root);
    }
    static void ApplyTree(Control root)
    {
        var state=originals.GetValue(root,x=>new Original(x));int mode=(Siege?2:0)+(Dark?1:0);
        if(state.Applied!=mode){root.SuspendLayout();try{ApplyOne(root);state.Applied=mode;}finally{root.ResumeLayout(false);}root.Invalidate();}
        foreach(Control child in root.Controls)ApplyTree(child);
    }
    static void Capture(Control c){originals.GetValue(c,x=>new Original(x));foreach(Control child in c.Controls)Capture(child);}
    static void ApplyOne(Control c)
    {
        if(c is ThemeSwitch)return;
        var old=originals.GetValue(c,x=>new Original(x));
        bool success=c.ForeColor==Color.DarkGreen||c.ForeColor==Color.FromArgb(151,225,173);bool error=c.ForeColor==Color.Firebrick||c.ForeColor==Color.FromArgb(255,155,155);
        c.BackColor=Dark?Map(old.Back):old.Back;
        if(Siege && !Dark && (old.Back==Color.White || old.Back==SystemColors.Control))c.BackColor=Surface;
        c.ForeColor=Dark?(old.Fore==Color.Firebrick?Error:old.Fore==Color.DarkGreen?Success:Text):old.Fore;
        if(success)c.ForeColor=Success;else if(error)c.ForeColor=Error;
        bool neutral=old.Back==Color.White || old.Back==SystemColors.Control || old.Back==Color.FromArgb(24,27,32) || old.Back==Color.FromArgb(34,38,45) || old.Back==Color.Transparent;
        if(c is TabPage || c is UserControl){c.Font=old.Font;c.BackgroundImage=null;c.BackgroundImageLayout=ImageLayout.Stretch;}
        if(neutral && (c is Panel || c is Label || c is GroupBox))c.BackColor=Siege?Surface:(Dark?Surface:old.Back);
        if(c is Panel panel && neutral){panel.BackgroundImage=null;panel.BackgroundImageLayout=ImageLayout.Stretch;}
        
        if(c is ThemeTabs themeTabs){themeTabs.Padding=Siege?new Point(12,7):new Point(8,3);}
        if(c is TextBox tb)tb.BorderStyle=(Dark||Siege)?BorderStyle.FixedSingle:BorderStyle.Fixed3D;
        if(c is Form form){c.BackColor=Background;if(form.IsHandleCreated){int value=Dark?1:0;DwmSetWindowAttribute(form.Handle,20,ref value,4);}else if(!wired.TryGetValue(c,out _)){wired.Add(c,new());form.HandleCreated+=(_,_)=>{int value=Dark?1:0;DwmSetWindowAttribute(form.Handle,20,ref value,4);};}}
        if(c is TextBoxBase || c is UpDownBase || c is ComboBox){c.BackColor=Dark?Field:old.Back;}
        if(c is Button b){b.UseVisualStyleBackColor=!(Dark||Siege);b.FlatStyle=(Dark||Siege)?FlatStyle.Flat:FlatStyle.Standard;b.FlatAppearance.BorderColor=Border;b.BackColor=(Dark||Siege)?Field:old.Back;}
        if(c is TabControl tabs)
        {
            tabs.DrawMode=TabDrawMode.OwnerDrawFixed;
            if(!wired.TryGetValue(c,out _)){wired.Add(c,new());tabs.DrawItem+=(_,e)=>{if(e.Index<0)return;using var brush=new SolidBrush(Dark?(e.Index==tabs.SelectedIndex?Field:Background):SystemColors.Control);e.Graphics.FillRectangle(brush,e.Bounds);TextRenderer.DrawText(e.Graphics,tabs.TabPages[e.Index].Text,tabs.Font,e.Bounds,Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);};}
        }
        if(c is ComboBox combo)
        {
            combo.FlatStyle=(Dark||Siege)?FlatStyle.Flat:FlatStyle.Standard;combo.DrawMode=DrawMode.OwnerDrawFixed;
            if(!wired.TryGetValue(c,out _)){wired.Add(c,new());combo.DrawItem+=(_,e)=>{var selected=(e.State&DrawItemState.Selected)!=0;var bg=selected?Color.FromArgb(64,100,130):Field;using var brush=new SolidBrush(bg);e.Graphics.FillRectangle(brush,e.Bounds);string value=e.Index>=0?(combo.GetItemText(combo.Items[e.Index])??""):combo.Text;TextRenderer.DrawText(e.Graphics,value,combo.Font,e.Bounds,Dark?Text:selected?Color.White:Color.Black,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);e.DrawFocusRectangle();};}
        }
        if(c is DataGridView grid)ApplyGrid(grid);
        if(c is StatusStrip strip){strip.BackColor=Background;foreach(ToolStripItem item in strip.Items)item.ForeColor=Text;}
    }
    public static void ApplyGrid(DataGridView grid)
    {
            grid.EnableHeadersVisualStyles=!(Dark||Siege);grid.BackgroundColor=Surface;grid.GridColor=Border;
            grid.DefaultCellStyle.BackColor=Surface;grid.DefaultCellStyle.ForeColor=Text;grid.DefaultCellStyle.SelectionBackColor=Field;grid.DefaultCellStyle.SelectionForeColor=Text;
            grid.ColumnHeadersDefaultCellStyle.BackColor=Field;grid.ColumnHeadersDefaultCellStyle.ForeColor=Text;
            foreach(DataGridViewRow row in grid.Rows)
            {
                var style=row.DefaultCellStyle;var back=style.BackColor;
                bool team=back==Color.FromArgb(64,143,179)||back==Color.FromArgb(255,119,55);
                if(team){style.ForeColor=style.SelectionForeColor=Color.FromArgb(16,24,32);continue;}
                bool final=back==Color.FromArgb(199,235,204)||back==Color.FromArgb(36,60,49);
                style.BackColor=final?(Dark?Color.FromArgb(36,60,49):Color.FromArgb(199,235,204)):Surface;
                style.ForeColor=final?(Dark?Success:Color.FromArgb(15,65,30)):Text;style.SelectionBackColor=style.BackColor;style.SelectionForeColor=style.ForeColor;
            }
        
    }

}

public sealed class ThemeSwitch : CheckBox
{
    public ThemeSwitch(){AutoSize=false;Size=new Size(170,30);Text=L.T("Modo escuro");AccessibleName=L.T("Alternar modo claro e escuro");SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);Cursor=Cursors.Hand;}
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(AppTheme.Background);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        var track=new Rectangle(1,3,62,24);using var path=new GraphicsPath();path.AddArc(track.X,track.Y,24,24,90,180);path.AddArc(track.Right-24,track.Y,24,24,270,180);path.CloseFigure();
        using var fill=new SolidBrush(Checked?Color.FromArgb(60,84,110):Color.FromArgb(210,215,222));e.Graphics.FillPath(fill,path);
        int x=Checked?4:39;using var knob=new SolidBrush(Checked?Color.FromArgb(231,237,245):Color.White);e.Graphics.FillEllipse(knob,x,5,20,20);
        using var ink=new SolidBrush(Color.FromArgb(34,45,58));
        if(Checked){e.Graphics.FillEllipse(ink,x+5,8,12,13);e.Graphics.FillEllipse(knob,x+9,6,11,12);}
        else {e.Graphics.FillEllipse(ink,x+7,12,6,6);using var pen=new Pen(ink,1.3f);for(int i=0;i<8;i++){double a=i*Math.PI/4;e.Graphics.DrawLine(pen,x+10+(float)Math.Cos(a)*5,15+(float)Math.Sin(a)*5,x+10+(float)Math.Cos(a)*8,15+(float)Math.Sin(a)*8);}}
        TextRenderer.DrawText(e.Graphics,Checked?L.T("Modo escuro"):L.T("Modo claro"),Font,new Rectangle(72,0,95,30),AppTheme.Text,TextFormatFlags.VerticalCenter|TextFormatFlags.Left);
        if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(0,0,Width-1,Height-1),AppTheme.Text,AppTheme.Background);
    }
}

public class ThemeButton : Button
{
    bool hover;
    public ThemeButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);Cursor=Cursors.Hand;}
    protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hover=true;Invalidate();}
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hover=false;Invalidate();}
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal Color? EmphasisFill { get; set; }
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal bool PrimaryAction { get; set; }
    protected override void OnPaint(PaintEventArgs e)
    {
        if(!AppTheme.Dark && !AppTheme.Siege && EmphasisFill is null && !PrimaryAction){base.OnPaint(e);return;}
        if(Width<2 || Height<2)return;
        if(PrimaryAction){using var primary=new LinearGradientBrush(ClientRectangle,Enabled?Color.FromArgb(25,77,111):Color.FromArgb(38,57,72),Enabled?(hover?Color.FromArgb(62,161,205):Color.FromArgb(40,121,166)):Color.FromArgb(50,77,94),90f);e.Graphics.FillRectangle(primary,ClientRectangle);}
        else if(Enabled && EmphasisFill is Color emphasis){using var fill=new LinearGradientBrush(ClientRectangle,ControlPaint.Dark(emphasis,.15f),emphasis,90f);e.Graphics.FillRectangle(fill,ClientRectangle);}
        else if(Enabled && AppTheme.Siege && (hover || Focused)){using var fill=new LinearGradientBrush(ClientRectangle,AppTheme.Dark?Color.FromArgb(96,51,58):Color.FromArgb(186,63,62),Color.FromArgb(217,68,62),90f);e.Graphics.FillRectangle(fill,ClientRectangle);}
        else if(AppTheme.Siege) {using var fill=new LinearGradientBrush(ClientRectangle,AppTheme.Surface,AppTheme.Field,90f);e.Graphics.FillRectangle(fill,ClientRectangle);} else e.Graphics.Clear(AppTheme.Field);
        if(!Enabled && AppTheme.Siege){using var hatch=new HatchBrush(HatchStyle.ForwardDiagonal,Color.FromArgb(22,AppTheme.Text),Color.Transparent);e.Graphics.FillRectangle(hatch,ClientRectangle);}
        using var border=new Pen(Focused?Color.FromArgb(100,175,220):AppTheme.Border);e.Graphics.DrawRectangle(border,0,0,Width-1,Height-1);
        if(Enabled && (hover || EmphasisFill is not null || Focused)){using var accent=new SolidBrush(PrimaryAction?Color.FromArgb(98,193,231):EmphasisFill is Color color?ControlPaint.Light(color):AppTheme.Interaction);e.Graphics.FillRectangle(accent,1,Height-4,Width-2,3);}
        var textBounds=new Rectangle(3,2,Width-6,Height-4);
        Color emphasisText=EmphasisFill is Color tone && (.2126*tone.R+.7152*tone.G+.0722*tone.B)>=125?Color.Black:Color.White;
        if(Image is not null){int x=(Width-Image.Width)/2;e.Graphics.DrawImage(Image,x,6);textBounds=new Rectangle(3,Image.Height+8,Width-6,Math.Max(12,Height-Image.Height-10));}
        TextRenderer.DrawText(e.Graphics,Text,Font,textBounds,PrimaryAction?(Enabled?Color.White:Color.Silver):Enabled && EmphasisFill is not null?emphasisText:Enabled?(AppTheme.Siege && (hover || Focused) && EmphasisFill is null?Color.White:ForeColor):(AppTheme.Dark?Color.FromArgb(189,197,208):Color.FromArgb(98,109,121)),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);
        if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(3,3,Width-6,Height-6));
    }
}
public sealed class ThemeTabs : TabControl
{
    UiUpdate? transition;
    protected override void OnSelecting(TabControlCancelEventArgs e)
    {
        transition?.Dispose();transition=new UiUpdate(this,true);
        try{base.OnSelecting(e);if(e.Cancel){transition.Dispose();transition=null;}else if(e.TabPage is not null)Prepare(e.TabPage);}
        catch{transition?.Dispose();transition=null;throw;}
    }
    static void Prepare(Control c){bool first=!c.IsHandleCreated;if(first)_=c.Handle;foreach(Control child in c.Controls)Prepare(child);if(first)c.PerformLayout();}
    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        try{base.OnSelectedIndexChanged(e);}finally{transition?.Dispose();transition=null;}
    }
    protected override void Dispose(bool disposing){if(disposing){transition?.Dispose();transition=null;}base.Dispose(disposing);}
    public override Rectangle DisplayRectangle=>new Rectangle(0,0,ClientSize.Width,ClientSize.Height);
    public ThemeTabs(){Appearance=TabAppearance.FlatButtons;SizeMode=TabSizeMode.Fixed;ItemSize=new Size(1,1);SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
    protected override void OnPaint(PaintEventArgs e)
    {
        AppTheme.PaintSurface(e.Graphics,ClientRectangle);
        using var border=new Pen(AppTheme.Border);

    }
}
public sealed class ThemeGroup : GroupBox
{
    protected override void OnPaint(PaintEventArgs e)
    {
        if(!AppTheme.Dark && !AppTheme.Siege){base.OnPaint(e);return;}
        if(AppTheme.Siege)
        {
            AppTheme.PaintSurface(e.Graphics,ClientRectangle);
            using var edge=new Pen(AppTheme.Border);
            int h=Math.Max(20,Font.Height+4);
            using var fill=new SolidBrush(Color.FromArgb(AppTheme.Dark?55:22,AppTheme.Dark?Color.White:Color.Black));e.Graphics.FillRectangle(fill,0,0,Width,h);
            e.Graphics.DrawLine(edge,1,h,Width-2,h);
            TextRenderer.DrawText(e.Graphics,Text.ToUpperInvariant(),AppTheme.HeadingFont,new Rectangle(9,1,Width-18,h-1),AppTheme.Text,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
            return;
        }
        e.Graphics.Clear(BackColor);var size=TextRenderer.MeasureText(Text,Font);
        using var border=new Pen(AppTheme.Border);int y=size.Height/2;
        e.Graphics.DrawRectangle(border,0,y,Width-1,Height-y-1);
        using var bg=new SolidBrush(BackColor);e.Graphics.FillRectangle(bg,7,0,size.Width+4,size.Height);
        TextRenderer.DrawText(e.Graphics,Text,Font,new Point(9,0),ForeColor);
    }
}
public sealed class ThemeCombo : ComboBox
{
    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if((!AppTheme.Dark && !AppTheme.Siege) || !IsHandleCreated || (m.Msg!=0xF && m.Msg!=0x317))return;
        using var g=m.Msg==0x317 && m.WParam!=IntPtr.Zero?Graphics.FromHdc(m.WParam):Graphics.FromHwnd(Handle);
        using var bg=new SolidBrush(AppTheme.Field);using var line=new Pen(AppTheme.Border);using var ink=new SolidBrush(Color.FromArgb(189,197,208));
        if(DropDownStyle==ComboBoxStyle.DropDownList)
        {
            g.FillRectangle(bg,ClientRectangle);
            TextRenderer.DrawText(g,Text,Font,new Rectangle(5,1,Math.Max(0,Width-28),Height-2),Enabled?AppTheme.Text:AppTheme.Dark?Color.Silver:Color.DimGray,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        }
        g.FillRectangle(bg,Width-20,1,19,Height-2);g.DrawRectangle(line,0,0,Width-1,Height-1);
        int x=Width-11,y=Height/2;g.FillPolygon(ink,[new Point(x-4,y-2),new Point(x+4,y-2),new Point(x,y+2)]);
    }
}

public sealed class ThemeCaption : Label
{
    protected override void OnPaint(PaintEventArgs e)
    {
        if(!AppTheme.Siege){base.OnPaint(e);return;}
        using var fill=new SolidBrush(AppTheme.Dark?Color.FromArgb(66,80,100):Color.FromArgb(213,223,235));e.Graphics.FillRectangle(fill,ClientRectangle);
        TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(7,0,Math.Max(0,Width-14),Height),AppTheme.Text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
    }
}





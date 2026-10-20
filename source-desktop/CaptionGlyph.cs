namespace CasterDesk.Desktop;
internal sealed class CaptionGlyph(Window window,string kind) : Control
{
    internal bool Hovered;
    public override void Render(DrawingContext context)
    {
        var ink=kind=="close"&&Hovered?Brushes.White:Ui.Ink;
        var pen=new Pen(ink,1);double x=(Bounds.Width-12)/2+.5,y=(Bounds.Height-12)/2+.5;
        if(kind=="close") {context.DrawLine(pen,new Point(x+1,y+1),new Point(x+11,y+11));context.DrawLine(pen,new Point(x+11,y+1),new Point(x+1,y+11));}
        else if(kind=="minimize")context.DrawLine(pen,new Point(x+1,y+6),new Point(x+11,y+6));
        else if(window.WindowState==WindowState.Maximized){context.DrawRectangle(pen,new Rect(x+3,y,8,8));context.DrawRectangle(Hovered?Brush.Parse(Ui.Dark?"#314456":"#CAD6E0"):Ui.MenuSurface,pen,new Rect(x,y+3,8,8));}
        else context.DrawRectangle(pen,new Rect(x+1,y+1,10,10));
    }
}

using System.Drawing.Drawing2D;
namespace CronoLive;

internal sealed class BanHalfHeading : Label
{
    internal bool Active;
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(AppTheme.Surface);
        int inset=Math.Min(76,Width/6);
        if(Active){var badge=new Rectangle(2,4,inset-5,Math.Max(16,Height-8));using var fill=new SolidBrush(Color.FromArgb(36,102,70));e.Graphics.FillRectangle(fill,badge);TextRenderer.DrawText(e.Graphics,L.T("ATIVOS"),Font,badge,Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}
        TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(inset,0,Math.Max(1,Width-inset*2),Height),Text.StartsWith("!")?AppTheme.Error:AppTheme.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
    }
}
internal sealed class ReadOnlySideField : Control
{
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(AppTheme.Field);
        using var pen=new Pen(AppTheme.Border);e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1);
        TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(4,0,Width-8,Height),AppTheme.Text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
    }
}

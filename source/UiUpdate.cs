using System.Runtime.InteropServices;
namespace CasterDesk.Core;
public sealed class BufferedGrid : DataGridView
{
    public BufferedGrid(){DoubleBuffered=true;}
}
public sealed class BufferedTable : TableLayoutPanel
{
    public BufferedTable(){DoubleBuffered=true;}
}
// Suppress intermediate native painting only during a synchronous update.
sealed class UiUpdate : IDisposable
{
    readonly Control root;readonly bool redraw;
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,IntPtr l);
    public UiUpdate(Control root,bool repaint=false){this.root=root;root.SuspendLayout();redraw=repaint && root.IsHandleCreated && root.Visible;if(redraw)SendMessage(root.Handle,0xB,IntPtr.Zero,IntPtr.Zero);}
    public void Dispose(){root.ResumeLayout(true);if(redraw && !root.IsDisposed){SendMessage(root.Handle,0xB,new IntPtr(1),IntPtr.Zero);root.Invalidate(true);}}
}

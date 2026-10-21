using System.Runtime.InteropServices;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Chrome;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace CasterDesk.Desktop;

// An isolated Win32 smoke check: messages target only this process's test window.
public sealed class NativeWindowCheckApp : Application
{
    public override void Initialize()=>Ui.ApplyTheme(this);
    public override void OnFrameworkInitializationCompleted()
    {
        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.ShutdownMode=ShutdownMode.OnExplicitShutdown;
            var shell=new StudioShell(new MatchSession());desktop.MainWindow=shell;
            shell.Opened+=async (_,_)=>{
                int exit=0;
                try {await NativeWindowChecks.Run(shell);}
                catch(Exception ex){exit=1;File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"native-window-check.txt"),"FAIL: "+ex);}
                finally {shell.Close();desktop.Shutdown(exit);}
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}

internal static class NativeWindowChecks
{
    [DllImport("user32.dll")] static extern nint SendMessage(nint window,uint message,nint wParam,nint lParam);
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] static extern bool IsIconic(nint window);
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] static extern bool IsZoomed(nint window);
    [DllImport("user32.dll",EntryPoint="GetWindowLongW")] static extern uint GetWindowLong(nint window,int index);
    static int count;
    static void Need(bool value,string message){count++;if(!value)throw new Exception(message);}
    static async Task Until(Func<bool> ready,string message)
    {
        var deadline=DateTime.UtcNow.AddSeconds(3);
        while(!ready() && DateTime.UtcNow<deadline)await Task.Delay(25);
        Need(ready(),message);
    }
    static nint Coordinates(PixelPoint point)=>(nint)((point.Y&0xffff)<<16|(point.X&0xffff));
    static T Find<T>(Window window,string name) where T:Control=>window.GetVisualDescendants().OfType<T>().Single(x=>x.Name==name);
    internal static async Task Run(StudioShell window)
    {
        await Task.Delay(180);
        nint handle=window.TryGetPlatformHandle()!.Handle;
        uint style=GetWindowLong(handle,-16);
        Need((style&0x00080000)!=0,"Native system menu must remain enabled for taskbar restore");
        Need((style&0x00020000)!=0,"Native minimize support must remain enabled");
        Need((style&0x00010000)!=0,"Native maximize support must remain enabled");
        foreach(string theme in new[]{"standard","light","dark"}) {
            Ui.Repaint(theme,window);
            ChromeOverlayChecks.AssertSingleCaption(window);
            window.WindowState=WindowState.Normal;
            await Until(()=>!IsZoomed(handle)&&!IsIconic(handle),"Restore normal window");
            window.UpdateLayout();
            foreach(var point in new[]{new Point(300,17),new Point(500,26),new Point(18,17)}) {
                nint location=Coordinates(window.PointToScreen(point));
                Need(SendMessage(handle,0x84,0,location)==2,"Title/icon/blank space must hit native caption");
                SendMessage(handle,0xA3,2,location);
                await Until(()=>IsZoomed(handle)&&window.WindowState==WindowState.Maximized,"Native caption double-click maximizes");
                SendMessage(handle,0xA3,2,Coordinates(window.PointToScreen(point)));
                await Until(()=>!IsZoomed(handle)&&window.WindowState==WindowState.Normal,"Native caption double-click restores");
            }
            var minimize=Find<Button>(window,"caption-minimize");
            window.UpdateLayout();await Task.Delay(60);
            using(var shot=new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width,(int)window.Bounds.Height))){shot.Render(ChromeOverlayChecks.Host(window));shot.Save(Path.Combine(AppPaths.DataDirectory,"native-before-minimize.png"));}
            var p=minimize.TranslatePoint(new Point(22,15),window)!.Value;
            nint minimizeLocation=Coordinates(window.PointToScreen(p));
            nint minimizeHit=SendMessage(handle,0x84,0,minimizeLocation);
            var hit=window.GetVisualAt(p);
            string hitPath=hit==null?"null":string.Join(" > ",hit.GetSelfAndVisualAncestors().Select(v=>$"{v.GetType().Name}:{WindowDecorationProperties.GetElementRole(v)}"));
            Need(minimizeHit==8,$"Minimize caption button has native role (actual {minimizeHit}, p={p}, {hitPath})");
            Need(SendMessage(handle,0x84,0,Coordinates(window.PointToScreen(new Point(500,110))))==1,"Application content is outside caption");
            foreach(bool maximized in new[]{false,true}) {
                window.WindowState=maximized?WindowState.Maximized:WindowState.Normal;
                await Until(()=>IsZoomed(handle)==maximized,"Prepare state before minimize");
                minimize.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Until(()=>IsIconic(handle)&&window.WindowState==WindowState.Minimized,"Caption button minimizes");
                SendMessage(handle,0x112,0xF120,0); // SC_RESTORE, as used by the taskbar/system menu.
                await Until(()=>!IsIconic(handle)&&IsZoomed(handle)==maximized,"Taskbar/system restore returns to previous state");
                Need(window.IsVisible,"Restored window remains visible");
            }
        }
        window.WindowState=WindowState.Normal;window.UpdateLayout();
        using(var bitmap=new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width,(int)window.Bounds.Height))){bitmap.Render(ChromeOverlayChecks.Host(window));bitmap.Save(Path.Combine(AppPaths.DataDirectory,"native-window.png"));}
        File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"native-window-check.txt"),$"PASS: {count} native Win32 checks. Caption hit testing and double-click; caption minimize and system/taskbar restore from normal/maximized; three themes.");
    }
}

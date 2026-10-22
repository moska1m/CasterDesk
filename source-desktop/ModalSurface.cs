using Avalonia.Input;
using Avalonia.VisualTree;

namespace CasterDesk.Desktop;

// A modal surface lives in the existing window; it never creates a second OS window.
internal sealed class ModalSurface : Border
{
    readonly TaskCompletionSource<bool> completion=new();
    internal string Title { get; set; }
    internal Task<bool> Completion=>completion.Task;
    internal Func<Task<bool>>? BeforeClose;
    internal Action<bool>? RequestClose;
    internal ModalSurface(string title,Control content,double width,double height)
    {
        Title=L.T(title);Width=width;Height=height;MaxWidth=width;MaxHeight=height;
        Background=Ui.Canvas;BorderBrush=Ui.FrameLine;BorderThickness=new Thickness(1);HorizontalAlignment=HorizontalAlignment.Center;VerticalAlignment=VerticalAlignment.Center;
        var root=new IcePlaque(IceSurfaceKind.Modal){RowDefinitions=new("Auto,*")};
        var heading=Ui.Text(title,true);heading.Margin=new Thickness(20,14);heading.Name="surface-title";
        var header=new IceHeader{ColumnDefinitions=new("*,Auto")};
        header.Children.Add(heading);
        var close=WindowFrame.CloseButton(()=>Close(),"surface-close");
        close.VerticalAlignment=VerticalAlignment.Top;
        header.Children.Add(close);Grid.SetColumn(close,1);
        root.Children.Add(header);root.Children.Add(content);Grid.SetRow(content,1);Child=root;
        KeyboardNavigation.SetTabNavigation(this,KeyboardNavigationMode.Cycle);
    }
    internal void Close(bool result=false)=>RequestClose?.Invoke(result);
    internal void Complete(bool result)=>completion.TrySetResult(result);
}

public sealed partial class StudioShell
{
    internal ModalSurface? ActiveSurface { get; private set; }
    Grid surfaceLayout=null!;
    bool surfaceClosing;
    internal Task<bool> ShowSurface(ModalSurface surface)
    {
        if(ActiveSurface!=null)throw new InvalidOperationException("Conclua o painel atual primeiro.");
        if(menuOpen)ToggleMenu();
        ActiveSurface=surface;surface.RequestClose=result=>_ = CloseSurface(result);
        surface.ZIndex=102;surfaceLayout.Children.Add(surface);Grid.SetRowSpan(surface,3);
        void Fit(){surface.Width=Math.Min(surface.MaxWidth,Math.Max(100,Bounds.Width-32));surface.Height=Math.Min(surface.MaxHeight,Math.Max(100,Bounds.Height-65));}
        Fit();
        EventHandler<SizeChangedEventArgs> resize=(_,_)=>Fit();SizeChanged+=resize;
        surface.Completion.ContinueWith(_=>Dispatcher.UIThread.Post(()=>SizeChanged-=resize));
        menuShade.Background=Brush.Parse("#99000000");menuShade.IsVisible=true;menuShade.ZIndex=100;
        page.IsEnabled=false;if(mainHeader!=null)mainHeader.IsEnabled=false;updateBanner.IsEnabled=false;
        Title="CasterDesk • "+L.T(surface.Title);
        Dispatcher.UIThread.Post(()=>surface.GetVisualDescendants().OfType<Control>().FirstOrDefault(c=>c.Focusable&&c.IsEffectivelyVisible&&c.IsEffectivelyEnabled)?.Focus());
        return surface.Completion;
    }
    internal async Task<bool> CloseSurface(bool result=false)
    {
        if(ActiveSurface is not { } surface)return true;
        if(surfaceClosing)return false;
        surfaceClosing=true;
        try {
            if(surface.BeforeClose!=null&&!await surface.BeforeClose())return false;
            surfaceLayout.Children.Remove(surface);surface.RequestClose=null;ActiveSurface=null;
            menuShade.IsVisible=false;page.IsEnabled=true;if(mainHeader!=null)mainHeader.IsEnabled=true;updateBanner.IsEnabled=true;
            Title="CasterDesk • "+BuildInfo.Version;surface.Complete(result);gear.Focus();return true;
        }catch(Exception ex){Notify(ex.Message);return false;}
        finally{surfaceClosing=false;}
    }
}

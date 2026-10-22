using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Chrome;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace CasterDesk.Desktop;

// The template leaves Window.Content intact, including dialog results and closing guards.
internal static class WindowFrame
{
    internal static Button CloseButton(Action action,string name)
    {
        var glyph=new CaptionGlyph(null,"close"){Width=44,Height=30};
        var button=Ui.Button("",action);button.Name=name;button.Width=44;button.Height=30;button.MinHeight=0;
        button.Margin=new Thickness(0);button.Padding=new Thickness(0);button.BorderThickness=new Thickness(0);button.Background=Brushes.Transparent;
        button.Template=new FuncControlTemplate<Button>((control,names)=>{var layers=new Grid();layers.Children.Add(new IceSurface(IceSurfaceKind.Button,control));layers.Children.Add(glyph);var back=new Border{Child=layers};back.Bind(Border.BackgroundProperty,new Binding("Background"){Source=control});return back;});
        button.PointerEntered+=(_,_)=>{glyph.Hovered=true;button.Background=Brush.Parse("#D92C35");glyph.InvalidateVisual();};
        button.PointerExited+=(_,_)=>{glyph.Hovered=false;button.Background=Brushes.Transparent;glyph.InvalidateVisual();};
        global::Avalonia.Automation.AutomationProperties.SetName(button,L.T("Fechar"));
        ToolTip.SetTip(button,L.T("Fechar"));return button;
    }
    public static void Install(Application app)
    {
        var style = new Style(s => s.OfType<Window>());
        // Keep the native system menu and window state handling; draw the caption ourselves.
        style.Setters.Add(new Setter(Window.WindowDecorationsProperty, WindowDecorations.Full));
        style.Setters.Add(new Setter(Window.ExtendClientAreaToDecorationsHintProperty, true));
        style.Setters.Add(new Setter(Window.ExtendClientAreaTitleBarHeightHintProperty, 32d));
        style.Setters.Add(new Setter(Window.TemplateProperty, new FuncControlTemplate<Window>(Build)));
        app.Styles.Add(style);
        // Avalonia 12 adds managed decorations above Window.Template through
        // TopLevelHost. Our template already draws the complete caption.
        var platformCaption = new Style(s => s.OfType<WindowDrawnDecorations>());
        platformCaption.Setters.Add(new Setter(WindowDrawnDecorations.TemplateProperty, null));
        app.Styles.Add(platformCaption);
    }
    static Control Build(Window window, INameScope scope)
    {
        window.RequestedThemeVariant=Ui.Dark?ThemeVariant.Dark:ThemeVariant.Light;window.Foreground=Ui.Ink;
        window.Classes.Set("black-ice-window",Ui.BlackIce);
        window.CanMinimize=window is StudioShell;window.CanMaximize=window.CanResize;
        var grid = new Grid { RowDefinitions = new("32,*"), Background = Ui.Canvas };
        var bodyIce=new IceSurface(IceSurfaceKind.Canvas){Name="ice-window-material",ZIndex=-1};Grid.SetRowSpan(bodyIce,2);grid.Children.Add(bodyIce);
        var titlebar = new IceHeader { ColumnDefinitions = new("Auto,*,Auto"), Background = Ui.MenuSurface };
        var title = new TextBlock { Margin = new Thickness(12,0), VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
        title.Classes.Add("caption-ink");
        title.Bind(TextBlock.TextProperty, new Binding("Title") { Source = window });
        var symbol=BrandMark.Create(Ui.Dark,16,true);symbol.Margin=new Thickness(10,0,0,0);symbol.VerticalAlignment=VerticalAlignment.Center;titlebar.Children.Add(symbol);
        titlebar.Children.Add(title);Grid.SetColumn(title,1);
        // The full caption, including its empty space and icon, is a drag surface.
        // Caption buttons keep their own input and must never start a move operation.
        titlebar.Name = "window-drag-region";
        WindowDecorationProperties.SetElementRole(titlebar, WindowDecorationsElementRole.TitleBar);
        titlebar.AddHandler(Control.PointerPressedEvent, (_, e) => {
            // Windows owns caption drag/double-click. BeginMoveDrag on the first
            // press starts a modal move loop and consumes the next click.
            if (!Program.Headless) return;
            if (!e.GetCurrentPoint(titlebar).Properties.IsLeftButtonPressed
                || e.Source is Visual source && source.GetSelfAndVisualAncestors().OfType<Button>().Any()) return;
            if (e.ClickCount == 2 && window.CanResize) window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            else window.BeginMoveDrag(e);
            e.Handled = true;
        }, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        WindowDecorationProperties.SetElementRole(buttons, WindowDecorationsElementRole.User);
        void Caption(string kind,string label,Action action,bool visible=true) {
            if(kind=="close"){
                var close=CloseButton(action,"caption-close");
                WindowDecorationProperties.SetElementRole(close,WindowDecorationsElementRole.CloseButton);buttons.Children.Add(close);return;
            }
            var glyph=new CaptionGlyph(window,kind){Width=44,Height=30};
            var b=Ui.Button("",action);b.Name="caption-"+kind;b.Width=44;b.Height=30;b.MinHeight=0;b.Margin=new Thickness(0);b.Padding=new Thickness(0);b.BorderThickness=new Thickness(0);b.Background=Brushes.Transparent;b.IsVisible=visible;
            WindowDecorationProperties.SetElementRole(b, kind switch {
                "minimize"=>WindowDecorationsElementRole.MinimizeButton,
                "maximize"=>WindowDecorationsElementRole.MaximizeButton,
                _=>WindowDecorationsElementRole.CloseButton
            });
            b.Template=new FuncControlTemplate<Button>((control,names)=>{var layers=new Grid();layers.Children.Add(new IceSurface(IceSurfaceKind.Button,control));layers.Children.Add(glyph);var back=new Border{Child=layers};back.Bind(Border.BackgroundProperty,new Binding("Background"){Source=control});return back;});
            b.PointerEntered+=(_,_)=>{glyph.Hovered=true;b.Background=Brush.Parse(kind=="close"?"#D92C35":Ui.Dark?"#314456":"#CAD6E0");glyph.InvalidateVisual();};
            b.PointerExited+=(_,_)=>{glyph.Hovered=false;b.Background=Brushes.Transparent;glyph.InvalidateVisual();};
            window.PropertyChanged+=(_,e)=>{if(e.Property==Window.WindowStateProperty)glyph.InvalidateVisual();};
            global::Avalonia.Automation.AutomationProperties.SetName(b,L.T(label));buttons.Children.Add(b);
        }
        Caption("minimize","Minimizar",()=>window.WindowState=WindowState.Minimized,window is StudioShell);
        Caption("maximize","Maximizar / Restaurar",()=>window.WindowState=window.WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized,window.CanResize);
        Caption("close","Fechar",()=>window.Close());
        titlebar.Children.Add(buttons); Grid.SetColumn(buttons,2); grid.Children.Add(titlebar);
        var presenter = new ContentPresenter { Name = "PART_ContentPresenter" };
        scope.Register("PART_ContentPresenter",presenter);
        presenter.Bind(ContentPresenter.ContentProperty,new Binding("Content") { Source=window });
        WindowDecorationProperties.SetElementRole(presenter, WindowDecorationsElementRole.User);
        grid.Children.Add(presenter); Grid.SetRow(presenter,1);
        var frame = new Border { BorderBrush = Ui.FrameLine, Child = grid };frame.Classes.Add("ice-window-frame");
        var root = new Grid(); root.Children.Add(frame);
        void Grip(WindowEdge edge, HorizontalAlignment h, VerticalAlignment v, bool horizontal) {
            var grip = new Border { Background=Brushes.Transparent, HorizontalAlignment=h, VerticalAlignment=v, Cursor=new Cursor(edge is WindowEdge.North or WindowEdge.South?StandardCursorType.SizeNorthSouth:edge is WindowEdge.West or WindowEdge.East?StandardCursorType.SizeWestEast:edge is WindowEdge.NorthWest or WindowEdge.SouthEast?StandardCursorType.TopLeftCorner:StandardCursorType.TopRightCorner) };
            if(horizontal)grip.Height=5;else grip.Width=5;
            if(h!=HorizontalAlignment.Stretch && v!=VerticalAlignment.Stretch){grip.Width=8;grip.Height=8;}
            WindowDecorationProperties.SetElementRole(grip, edge switch {
                WindowEdge.North=>WindowDecorationsElementRole.ResizeN,WindowEdge.South=>WindowDecorationsElementRole.ResizeS,
                WindowEdge.West=>WindowDecorationsElementRole.ResizeW,WindowEdge.East=>WindowDecorationsElementRole.ResizeE,
                WindowEdge.NorthWest=>WindowDecorationsElementRole.ResizeNW,WindowEdge.NorthEast=>WindowDecorationsElementRole.ResizeNE,
                WindowEdge.SouthWest=>WindowDecorationsElementRole.ResizeSW,_=>WindowDecorationsElementRole.ResizeSE
            });
            void ResizeVisibility()=>grip.IsVisible=window.CanResize && window.WindowState==WindowState.Normal;
            ResizeVisibility();window.PropertyChanged+=(_,e)=>{if(e.Property==Window.WindowStateProperty || e.Property==Window.CanResizeProperty)ResizeVisibility();};
            grip.PointerPressed+=(_,e)=>{if(Program.Headless && window.CanResize && window.WindowState==WindowState.Normal)window.BeginResizeDrag(edge,e);}; root.Children.Add(grip);
        }
        Grip(WindowEdge.North,HorizontalAlignment.Stretch,VerticalAlignment.Top,true);
        Grip(WindowEdge.South,HorizontalAlignment.Stretch,VerticalAlignment.Bottom,true);
        Grip(WindowEdge.West,HorizontalAlignment.Left,VerticalAlignment.Stretch,false);
        Grip(WindowEdge.East,HorizontalAlignment.Right,VerticalAlignment.Stretch,false);
        Grip(WindowEdge.NorthWest,HorizontalAlignment.Left,VerticalAlignment.Top,false);
        Grip(WindowEdge.NorthEast,HorizontalAlignment.Right,VerticalAlignment.Top,false);
        Grip(WindowEdge.SouthWest,HorizontalAlignment.Left,VerticalAlignment.Bottom,false);
        Grip(WindowEdge.SouthEast,HorizontalAlignment.Right,VerticalAlignment.Bottom,false);
        var layers=new VisualLayerManager { Name="PART_VisualLayerManager", Child=root, EnableOverlayLayer=true, EnableAdornerLayer=true, EnableTextSelectorLayer=true };
        scope.Register("PART_VisualLayerManager",layers);return layers;
    }
}


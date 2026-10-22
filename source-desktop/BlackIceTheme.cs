using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media.Imaging;
using Avalonia.Styling;

namespace CasterDesk.Desktop;

// Decorative surfaces never handle input. Window roles and control templates keep
// ownership of dragging, focus, commands and local team/state colors.
internal static class BlackIceTheme
{
    static Lazy<Bitmap?> Material(string name,int width)=>new(() => {
        string path = Path.Combine(AppContext.BaseDirectory, "assets", "themes", "black-ice", name);
        if (!File.Exists(path)) { AppLog.Write("Black Ice surface missing: " + path); return null; }
        using var stream = File.OpenRead(path);
        return Bitmap.DecodeToWidth(stream, width, BitmapInterpolationMode.HighQuality);
    });
    static readonly Lazy<Bitmap?> texture=Material("structure-v3.png",1536),panel=Material("panel-carved-v3.png",1536),glass=Material("glass-v3.png",1024);
    internal static bool HasTexture => texture.Value != null&&panel.Value!=null&&glass.Value!=null;
    internal static IBrush HeaderInk => Ui.BlackIce ? Brushes.White : Ui.Ink;
    static IBrush FieldFace=>glass.Value is {} face?new ImageBrush(face){SourceRect=new RelativeRect(.16,.25,.68,.5,RelativeUnit.Relative),Stretch=Stretch.UniformToFill}:Gradient("#183E54","#0B2B3F");

    // Fluent's interaction resources also need the frosted field face, otherwise
    // focus/hover brings back an unrelated opaque gray field.
    static readonly string[] fieldResources =
        ["TextControlBackground", "TextControlBackgroundPointerOver", "TextControlBackgroundFocused", "TextControlBackgroundDisabled",
         "TextControlForeground", "TextControlForegroundPointerOver", "TextControlForegroundFocused", "TextControlForegroundDisabled",
         "ComboBoxBackground", "ComboBoxBackgroundPointerOver", "ComboBoxBackgroundPressed", "ComboBoxBackgroundDisabled",
         "ComboBoxForeground", "ComboBoxForegroundPointerOver", "ComboBoxForegroundPressed", "ComboBoxForegroundDisabled"];
    internal static void Resources(Application app)
    {
        app.Resources["CasterDeskField"] = Ui.BlackIce ? FieldFace : Ui.Panel;
        app.Resources["CasterDeskIceFramePadding"] = new Thickness(Ui.BlackIce ? 4 : 0);
        app.Resources["CasterDeskIceHeaderMargin"] = Ui.BlackIce ? new Thickness(4, 2, 4, 6) : new Thickness(0);
        foreach (string key in fieldResources) {
            if (!Ui.BlackIce) { app.Resources.Remove(key); continue; }
            app.Resources[key] = key.Contains("Foreground") ? Ui.Ink : FieldFace;
        }
    }

    internal static LinearGradientBrush Gradient(string top, string bottom) => new() {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        GradientStops = [new GradientStop(Color.Parse(top), 0), new GradientStop(Color.Parse(bottom), 1)]
    };

    // Crop at a uniform scale: compressing a whole atlas into a thin header
    // turns fracture faces into horizontal streaks. The rim is shaded separately.
    // The shared bitmap is decoded once; there is no render loop or animation.
    internal static void Surface(DrawingContext context, Rect bounds, double rim = 7)
    {
        if (bounds.Width < 2 || bounds.Height < 2) return;
        if (texture.Value is not { } image) { context.DrawRectangle(Ui.MenuSurface, null, bounds); return; }
        double w = image.Size.Width, h = image.Size.Height;
        double scale = Math.Max(bounds.Width / w, bounds.Height / h);
        double sw = bounds.Width / scale, sh = bounds.Height / scale;
        context.DrawImage(image, new Rect((w - sw) / 2, (h - sh) / 2, sw, sh), bounds);
        double bevel = Math.Min(rim, bounds.Height / 3);
        context.DrawRectangle(Gradient("#9AD5E7ED", "#00C0DCE8"), null, new Rect(bounds.X, bounds.Y, bounds.Width, bevel));
        context.DrawRectangle(Gradient("#00052334", "#90041E2B"), null, new Rect(bounds.X, bounds.Bottom - bevel, bounds.Width, bevel));
    }

    internal static void Grain(DrawingContext context,Rect bounds,double opacity)
    {
        if(texture.Value is not {} image)return;
        using var alpha=context.PushOpacity(opacity);
        double w=image.Size.Width*.42,h=image.Size.Height*.78;
        double scale=Math.Max(bounds.Width/w,bounds.Height/h),sw=bounds.Width/scale,sh=bounds.Height/scale;
        context.DrawImage(image,new Rect((w-sw)/2,image.Size.Height*.1+(h-sh)/2,sw,sh),bounds);
    }

    internal static void Bevel(DrawingContext context,Rect bounds,double radius=6)
    {
        context.DrawRectangle(null,new Pen(Brush.Parse("#B3D6E6EE"),1),bounds.Deflate(.5),radius,radius);
        if(bounds.Width<8||bounds.Height<8)return;
        context.DrawLine(new Pen(Brush.Parse("#A0E1F0F5"),1),new Point(bounds.X+radius,bounds.Y+1.5),new Point(bounds.Right-radius,bounds.Y+1.5));
        context.DrawLine(new Pen(Brush.Parse("#B0072031"),2),new Point(bounds.X+radius,bounds.Bottom-1.5),new Point(bounds.Right-radius,bounds.Bottom-1.5));
    }

    // Baked art contains the frosted chamfer and real recessed contact shading.
    // Preserve those contours with nine slices instead of fading one texture over
    // every component. Source/target caps have independent title-ridge heights.
    static void Slices(DrawingContext context,Bitmap image,Rect source,Rect target,Thickness from,Thickness to,bool center=true)
    {
        double left=Math.Min(to.Left,target.Width/3),right=Math.Min(to.Right,target.Width/3);
        double top=Math.Min(to.Top,target.Height/2),bottom=Math.Min(to.Bottom,target.Height/3);
        double[] sx=[source.X,source.X+from.Left,source.Right-from.Right,source.Right],sy=[source.Y,source.Y+from.Top,source.Bottom-from.Bottom,source.Bottom];
        double[] tx=[target.X,target.X+left,target.Right-right,target.Right],ty=[target.Y,target.Y+top,target.Bottom-bottom,target.Bottom];
        for(int y=0;y<3;y++)for(int x=0;x<3;x++) {
            if(!center&&x==1&&y==1)continue;
            context.DrawImage(image,new Rect(sx[x],sy[y],sx[x+1]-sx[x],sy[y+1]-sy[y]),new Rect(tx[x],ty[y],tx[x+1]-tx[x],ty[y+1]-ty[y]));
        }
    }
    internal static void Structural(DrawingContext context,Rect bounds,double rim=6)
    {
        Surface(context,bounds,0);
        if(texture.Value is {} image)Slices(context,image,new Rect(image.Size),bounds,new Thickness(image.Size.Width*.035,image.Size.Height*.065),new Thickness(rim),false);
    }
    internal static void Carved(DrawingContext context,Rect bounds,double titleHeight,double depth)
    {
        if(panel.Value is not {} image){Structural(context,bounds);return;}
        double w=image.Size.Width,h=image.Size.Height;
        Slices(context,image,new Rect(image.Size),bounds,new Thickness(w*.026,h*.167,w*.026,h*.047),new Thickness(7,titleHeight,7,7));
        var floor=new Rect(7,titleHeight,bounds.Width-14,bounds.Height-titleHeight-7);
        if(floor.Height>0)context.DrawRectangle(Brush.Parse("#400B3049"),null,floor,3,3);
        // Lower cavities sit further inside the mass: their rims have less frost.
        byte tone=(byte)Math.Clamp(depth*65,0,65);
        var tint=new SolidColorBrush(Color.FromArgb(tone,15,53,73));
        context.DrawRectangle(tint,null,new Rect(0,0,7,bounds.Height));
        context.DrawRectangle(tint,null,new Rect(bounds.Width-7,0,7,bounds.Height));
        context.DrawRectangle(tint,null,new Rect(0,bounds.Height-7,bounds.Width,7));
    }
    internal static void Glass(DrawingContext context,Rect bounds)
    {
        if(glass.Value is not {} image){context.DrawRectangle(Gradient("#315F79","#173D55"),null,bounds,5,5);return;}
        double w=image.Size.Width,h=image.Size.Height;
        var source=new Rect(w*.011,h*.045,w*.978,h*.91);
        // Crop the quiet face uniformly. Long utility buttons must not squeeze
        // vertical fractures into watery horizontal stripes.
        var core=new Rect(w*.07,h*.18,w*.86,h*.61);
        double scale=Math.Max(bounds.Width/core.Width,bounds.Height/core.Height),sw=bounds.Width/scale,sh=bounds.Height/scale;
        context.DrawImage(image,new Rect(core.Center.X-sw/2,core.Center.Y-sh/2,sw,sh),bounds);
        Slices(context,image,source,bounds,new Thickness(w*.045,h*.1,w*.045,h*.11),new Thickness(bounds.Height<35?2.5:3),false);
    }

    internal static void Install(Application app)
    {
        var frame=new Style(x=>x.OfType<Border>().Class("ice-window-frame"));
        frame.Setters.Add(new Setter(Border.BorderThicknessProperty,new Thickness(1)));
        frame.Setters.Add(new Setter(Border.PaddingProperty,new global::Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CasterDeskIceFramePadding")));app.Styles.Add(frame);
        var headerMargin=new Style(x=>x.OfType<IceHeader>().Class("ice-main-header"));
        headerMargin.Setters.Add(new Setter(Control.MarginProperty,new global::Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CasterDeskIceHeaderMargin")));app.Styles.Add(headerMargin);
        var choices=new Style(x=>x.OfType<ComboBox>().Class("app-choice"));
        choices.Setters.Add(new Setter(ComboBox.BackgroundProperty,new global::Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CasterDeskField")));
        choices.Setters.Add(new Setter(ComboBox.ForegroundProperty,new global::Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CasterDeskInk")));app.Styles.Add(choices);
        var input=new Style(x=>x.OfType<Window>().Class("black-ice-window").Descendant().OfType<TextBox>());
        input.Setters.Add(new Setter(TextBox.BackgroundProperty,new global::Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CasterDeskField")));
        input.Setters.Add(new Setter(TextBox.ForegroundProperty,Brush.Parse("#F2F7FA")));
        input.Setters.Add(new Setter(TextBox.BorderBrushProperty,Brush.Parse("#80A7BC")));
        input.Setters.Add(new Setter(TextBox.CornerRadiusProperty,new CornerRadius(4)));app.Styles.Add(input);
        var checks=new Style(x=>x.OfType<Window>().Class("black-ice-window").Descendant().OfType<CheckBox>());
        checks.Setters.Add(new Setter(CheckBox.ForegroundProperty,Brush.Parse("#F2F7FA")));app.Styles.Add(checks);
        var title = new Style(x => x.OfType<TextBlock>().Class("ice-card-title"));
        title.Setters.Add(new Setter(TextBlock.BackgroundProperty, new global::Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CasterDeskIceTitleBackground")));
        title.Setters.Add(new Setter(TextBlock.ForegroundProperty, new global::Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CasterDeskIceTitleInk")));
        app.Styles.Add(title);
        foreach(string name in new[]{"app-ink","caption-ink"}) {
            var headerText=new Style(x=>x.OfType<IceHeader>().Class("black-ice-header").Descendant().OfType<TextBlock>().Class(name));
            headerText.Setters.Add(new Setter(TextBlock.ForegroundProperty,Brushes.White));app.Styles.Add(headerText);
        }
        var arrow=new Style(x=>x.OfType<ComboBox>().Class("app-choice").Template().OfType<global::Avalonia.Controls.Shapes.Path>());
        arrow.Setters.Add(new Setter(global::Avalonia.Controls.Shapes.Path.FillProperty,new global::Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CasterDeskInk")));app.Styles.Add(arrow);
        var captionText=new Style(x=>x.OfType<TextBlock>().Class("caption-ink"));
        captionText.Setters.Add(new Setter(TextBlock.ForegroundProperty,new global::Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CasterDeskHeaderInk")));app.Styles.Add(captionText);
        var button = new Style(x => x.OfType<Button>().Class("black-ice-button"));
        button.Setters.Add(new Setter(Button.BackgroundProperty, Brush.Parse("#315E76")));
        button.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));
        button.Setters.Add(new Setter(Button.CornerRadiusProperty, new CornerRadius(5)));
        button.Setters.Add(new Setter(Button.TemplateProperty, new FuncControlTemplate<Button>((owner, scope) => {
            // Keep Fluent's pointer/pressed presenter background from painting
            // over the material behind it. Content and input remain native.
            var presenter = new ContentPresenter { Name = "PART_ContentPresenter", Background=Brushes.Transparent, BorderBrush=Brushes.Transparent };
            scope.Register("PART_ContentPresenter", presenter);
            presenter.Bind(ContentPresenter.ContentProperty, new TemplateBinding(ContentControl.ContentProperty));
            presenter.Bind(ContentPresenter.ContentTemplateProperty, new TemplateBinding(ContentControl.ContentTemplateProperty));
            presenter.Bind(ContentPresenter.HorizontalContentAlignmentProperty, new TemplateBinding(ContentControl.HorizontalContentAlignmentProperty));
            presenter.Bind(ContentPresenter.VerticalContentAlignmentProperty, new TemplateBinding(ContentControl.VerticalContentAlignmentProperty));
            var layers = new Grid();
            layers.Children.Add(new IceSurface(IceSurfaceKind.Button, owner));
            presenter.Bind(ContentPresenter.PaddingProperty, new TemplateBinding(Button.PaddingProperty));
            layers.Children.Add(presenter);
            var face = new Border { Child = layers, CornerRadius = new CornerRadius(5) };
            face.Bind(Border.BackgroundProperty, new TemplateBinding(Button.BackgroundProperty));
            return face;
        })));
        app.Styles.Add(button);
        var primary=new Style(x=>x.OfType<Button>().Class("black-ice-button").Class("primary"));
        primary.Setters.Add(new Setter(Button.BackgroundProperty,Brush.Parse("#0879AC")));app.Styles.Add(primary);
        var disabled = new Style(x => x.OfType<Button>().Class("black-ice-button").Class(":disabled"));
        disabled.Setters.Add(new Setter(Button.OpacityProperty, .45)); app.Styles.Add(disabled);
        var timerText = new Style(x => x.OfType<TextBlock>().Class("header-ink"));
        timerText.Setters.Add(new Setter(TextBlock.ForegroundProperty, new global::Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CasterDeskHeaderInk")));
        app.Styles.Add(timerText);
        var timerButton = new Style(x => x.OfType<Button>().Class("header-ink"));
        timerButton.Setters.Add(new Setter(Button.ForegroundProperty, new global::Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CasterDeskHeaderInk")));
        app.Styles.Add(timerButton);
    }
}

internal enum IceSurfaceKind { Canvas, Header, Card, Button, Brand, Toolbar, Menu, Modal, Well }

internal sealed class IcePlaque : Grid
{
    internal IcePlaque(IceSurfaceKind kind)
    {
        var surface=new IceSurface(kind){ZIndex=-1};
        SetColumnSpan(surface,99);SetRowSpan(surface,99);Children.Add(surface);
    }
}

internal sealed class IceHeader : Grid
{
    internal IceHeader()
    {
        Classes.Set("black-ice-header", Ui.BlackIce);
        var surface = new IceSurface(IceSurfaceKind.Header) { ZIndex = -1 };
        SetColumnSpan(surface, 99); SetRowSpan(surface, 99); Children.Add(surface);
    }
}

internal sealed class IceCard : Border
{
    internal IceCard()
    {
        PropertyChanged += (_, e) => {
            if (e.Property != ChildProperty || Child is not Grid body) return;
            var surface = new IceSurface(IceSurfaceKind.Card) { Margin = new Thickness(-10), ZIndex = -1 };
            Grid.SetRowSpan(surface, 2); body.Children.Insert(0, surface);
        };
    }
}

internal sealed class IceSurface : Control
{
    readonly IceSurfaceKind kind;
    readonly Button? owner;
    internal IceSurface(IceSurfaceKind kind, Button? owner = null)
    {
        this.kind = kind; this.owner = owner; IsHitTestVisible = false;
        if (owner != null) {
            AttachedToVisualTree += (_, _) => owner.PropertyChanged += Changed;
            DetachedFromVisualTree += (_, _) => owner.PropertyChanged -= Changed;
        }
    }
    void Changed(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name is "IsPressed" or "IsPointerOver" or "IsFocused" or "IsEffectivelyEnabled" or "Background") InvalidateVisual();
    }
    public override void Render(DrawingContext context)
    {
        if (!Ui.BlackIce || Bounds.Width < 3 || Bounds.Height < 3) return;
        var bounds = new Rect(Bounds.Size);
        using var clip=context.PushClip(new RoundedRect(bounds,kind==IceSurfaceKind.Button?5:7));
        if (kind == IceSurfaceKind.Canvas) {
            BlackIceTheme.Structural(context,bounds,7);
        } else if (kind is IceSurfaceKind.Header or IceSurfaceKind.Toolbar or IceSurfaceKind.Brand or IceSurfaceKind.Menu) {
            BlackIceTheme.Structural(context,bounds,kind==IceSurfaceKind.Header?4:6);
            if(kind==IceSurfaceKind.Brand) {
                BlackIceTheme.Glass(context,bounds.Deflate(3));
                context.DrawRectangle(Brush.Parse("#50031624"),null,bounds.Deflate(4),5,5);
            } else if(kind==IceSurfaceKind.Menu) {
                context.DrawRectangle(Brush.Parse("#E006202F"),null,bounds.Deflate(6),5,5);
            }
        } else if (kind is IceSurfaceKind.Card or IceSurfaceKind.Modal or IceSurfaceKind.Well) {
            if (bounds.Width < 20 || bounds.Height < 45) return;
            double top=kind==IceSurfaceKind.Modal?49:kind==IceSurfaceKind.Well?7:31;
            var root=TopLevel.GetTopLevel(this);double depth=root==null?0:Math.Clamp((this.TranslatePoint(default,root)?.Y??0)/Math.Max(1,root.Bounds.Height),0,1);
            BlackIceTheme.Carved(context,bounds,top,depth);
        } else if (owner != null) {
            bool pressed = owner.IsPressed, hover = owner.IsPointerOver && owner.IsEffectivelyEnabled;
            bool tab=owner.Classes.Contains("ice-tab"),selected=owner.Classes.Contains("ice-selected");
            bool close=owner.Name is "caption-close" or "surface-close";
            bounds=bounds.Deflate(1);
            if(owner.Name?.StartsWith("history-round-")==true) {
                context.DrawRectangle(BlackIceTheme.Gradient(hover?"#395E9ABB":"#185D94AF","#18051F31"),null,bounds,2,2);
                context.DrawLine(new Pen(Brush.Parse("#3A97C2D8"),1),bounds.BottomLeft,bounds.BottomRight);
                if(pressed)context.DrawRectangle(Brush.Parse("#66021423"),null,bounds,2,2);
                return;
            }
            if(tab&&!selected&&!hover&&!pressed) {
                context.DrawRectangle(BlackIceTheme.Gradient("#125D97B0","#06001121"),null,bounds,5,5);
                context.DrawLine(new Pen(Brush.Parse("#335B93AE"),1),bounds.BottomLeft,bounds.BottomRight);
                return;
            }
            bool team=owner.Name?.StartsWith("ban-slot-")==true||owner.Name?.StartsWith("round-winner-")==true||owner.Name?.StartsWith("veto-")==true;
            if(!team&&!(close&&hover))BlackIceTheme.Glass(context,bounds);
            else using(var faceClip=context.PushClip(new RoundedRect(bounds,5)))BlackIceTheme.Grain(context,bounds,.1);
            if(owner.Classes.Contains("primary")||selected)context.DrawRectangle(BlackIceTheme.Gradient("#A30A8ED3","#95005A92"),null,bounds,5,5);
            if(hover)context.DrawRectangle(BlackIceTheme.Gradient("#307AC2E5","#05001627"),null,bounds,5,5);
            // The face is visibly darker and the upper contact edge sinks on press.
            if(pressed)context.DrawRectangle(Brush.Parse("#99021423"),null,bounds,5,5);
            var edge=owner.IsFocused||selected?Brush.Parse("#8BD6F3"):hover?Brush.Parse("#B1DCEB"):Brush.Parse("#8EB4C8");
            context.DrawRectangle(null,new Pen(edge,1),bounds,5,5);
            context.DrawLine(new Pen(Brush.Parse(pressed?"#D005182A":"#AAD5E9F3"),pressed?2:1),new Point(5,2),new Point(Bounds.Width-5,2));
            if(!pressed)context.DrawLine(new Pen(Brush.Parse("#B0062236"),2),new Point(5,Bounds.Height-2),new Point(Bounds.Width-5,Bounds.Height-2));
            if(selected)context.DrawLine(new Pen(Brush.Parse("#A0DAF3"),2),new Point(5,Bounds.Height-3),new Point(Bounds.Width-5,Bounds.Height-3));
        }
    }
}

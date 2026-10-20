using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Styling;

namespace CasterDesk.Desktop;

internal static partial class Ui
{
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Control,object> paletteBound=new();
    public static bool Dark { get; private set; } = true;
    public static string ThemeName { get; private set; } = "standard";
    public static IBrush Canvas => Brush.Parse(ThemeName == "dark" ? "#0D1824" : Dark ? "#182637" : "#DFE7EF");
    public static IBrush Panel => Brush.Parse(ThemeName == "dark" ? "#152638" : Dark ? "#233448" : "#F1F5F8");
    public static IBrush Header => Brush.Parse(ThemeName == "dark" ? "#243C52" : Dark ? "#354C65" : "#C4D4E2");
    public static IBrush MenuSurface => Brush.Parse(Dark ? "#0C1621" : "#EAF0F5");
    public static IBrush Ink => Brush.Parse(Dark ? "#F2F5F8" : "#172C40");
    public static IBrush Accent => Brush.Parse(Dark ? "#FF4335" : "#187DB0");
    public static IBrush FrameLine => Brush.Parse(Dark ? "#466078" : "#879FB5");
    public static IBrush Team(int team, bool selected = true) => new SolidColorBrush(
        Color.FromArgb(selected ? (byte)255 : (byte)150, team == 0 ? (byte)64 : (byte)255,
            team == 0 ? (byte)143 : (byte)119, team == 0 ? (byte)179 : (byte)55));
    public static void Theme(bool dark) => Theme(dark ? "standard" : "light");
    public static void Theme(string name) {
        ThemeName = name is "dark" or "light" ? name : "standard"; Dark = ThemeName != "light";
        if (Application.Current != null) {
            Application.Current.RequestedThemeVariant = Dark ? ThemeVariant.Dark : ThemeVariant.Light;
            Application.Current.Resources["CasterDeskInk"]=Ink;Application.Current.Resources["CasterDeskPanel"]=Panel;Application.Current.Resources["CasterDeskHeader"]=Header;Application.Current.Resources["CasterDeskFrame"]=FrameLine;
            var resources=Application.Current.Resources;
            resources["CasterDeskAccent"]=Accent;
            resources["ComboBoxDropDownBackground"]=Panel;resources["ComboBoxDropDownBorderBrush"]=FrameLine;
            foreach(string state in new[]{"","PointerOver","Pressed","Selected","SelectedPointerOver","SelectedPressed"}){
                resources["ComboBoxItemForeground"+state]=Ink;
                resources["ComboBoxItemBackground"+state]=state.Length==0?Panel:Header;
                resources["ComboBoxItemBorderBrush"+state]=FrameLine;
            }
        }
    }

    public static Border Card(string title, Control child)
    {
        var body = new Grid { RowDefinitions = new("Auto,*") };
        var label = Text(title, true); label.FontSize = 14; label.Margin = new Thickness(0, 0, 0, 8);
        label.Text = label.Text?.ToUpperInvariant(); label.FontWeight = FontWeight.Bold; label.Background = Header;
        label.Padding = new Thickness(10, 4); label.Margin = new Thickness(-10, -10, -10, 8);
        body.Children.Add(label); body.Children.Add(child); Grid.SetRow(child, 1);
        return new Border { Background = Panel, Padding = new Thickness(10), Margin = new Thickness(4), Child = body, ClipToBounds = true };
    }

    static readonly Dictionary<string, WeakReference<Bitmap>> images = new(StringComparer.OrdinalIgnoreCase);
    public static Bitmap FileImage(string path, bool compactPortrait = false, bool grayscale = false, int thumbnail = 0)
    {
        string key = Path.GetFullPath(path) + File.GetLastWriteTimeUtc(path).Ticks + compactPortrait + grayscale + thumbnail;
        if (images.TryGetValue(key, out var weak) && weak.TryGetTarget(out var saved)) return saved;
        using var source = System.Drawing.Image.FromFile(path);
        Bitmap result;
        if (grayscale || thumbnail > 0) {
            double scale = thumbnail > 0 ? Math.Min(1, (double)thumbnail / Math.Max(source.Width, source.Height)) : 1;
            using var converted = new System.Drawing.Bitmap(Math.Max(1, (int)(source.Width * scale)), Math.Max(1, (int)(source.Height * scale)));
            using var g = System.Drawing.Graphics.FromImage(converted);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            using var attributes = new System.Drawing.Imaging.ImageAttributes();
            if (grayscale) attributes.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix(new float[][] {
                [.299f,.299f,.299f,0,0], [.587f,.587f,.587f,0,0], [.114f,.114f,.114f,0,0], [0,0,0,1,0], [0,0,0,0,1] }));
            g.DrawImage(source, new System.Drawing.Rectangle(0, 0, converted.Width, converted.Height), 0, 0, source.Width, source.Height, System.Drawing.GraphicsUnit.Pixel, attributes);
            result = Bitmap(converted);
        } else if (compactPortrait) {
            using var crop = new System.Drawing.Bitmap(source.Width, Math.Max(1, (int)(source.Height * .48)));
            using (var g = System.Drawing.Graphics.FromImage(crop))
                g.DrawImage(source, new System.Drawing.Rectangle(0, 0, crop.Width, crop.Height), new System.Drawing.Rectangle(0, 0, crop.Width, crop.Height), System.Drawing.GraphicsUnit.Pixel);
            result = Bitmap(crop);
        } else result = Bitmap(source);
        if (images.Count >= 128) images.Clear();
        images[key] = new(result); return result;
    }

    public static Window Dialog(Control owner, string title, Control content, double width = 700, double height = 500)
    {
        var parent = Owner(owner);
        var available = parent.Screens.ScreenFromWindow(parent)?.WorkingArea;
        double scale = parent.RenderScaling;
        var dialog = new Window { Title = L.T(title), Background = Canvas, FontFamily = new FontFamily("Segoe UI"), FontSize = 14,
            Width = Math.Min(width, available?.Width / scale ?? width), Height = Math.Min(height, available?.Height / scale ?? height),
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = content };
        return dialog;
    }

    public static Image OperatorPortrait(string id)
    {
        var image = new Image { Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0,4,0,0) };
        string current = "";
        void Update() {
            double scale = TopLevel.GetTopLevel(image)?.RenderScaling ?? 1;
            string file = SiegeCatalog.Portrait(id, Math.Max(image.Bounds.Width, image.Bounds.Height) * scale);
            if(file==current)return;
            image.Source=FileImage(file);current=file;
        }
        image.Source=FileImage(SiegeCatalog.Portrait(id,160));
        image.SizeChanged+=(_,_)=>Update();image.AttachedToVisualTree+=(_,_)=>Update();
        global::Avalonia.Media.RenderOptions.SetBitmapInterpolationMode(image,BitmapInterpolationMode.HighQuality);
        return image;
    }

    public static async Task<string?> PickFile(Control owner, string title, string extension, bool save = false, string? name = null)
    {
        var type = new FilePickerFileType(title) { Patterns = ["*." + extension] };
        if (save) return (await Owner(owner).StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
            Title = L.T(title), SuggestedFileName = name, DefaultExtension = extension, FileTypeChoices = [type] }))?.TryGetLocalPath();
        return (await Owner(owner).StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = L.T(title), AllowMultiple = false, FileTypeFilter = [type] })).FirstOrDefault()?.TryGetLocalPath();
    }
    public static Button Button(string text, Action action, bool primary = false)
    {
        var button = new Button { Content = L.T(text), Margin = new Thickness(0, 0, 0, 8),
            HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center };
        // Theme defaults belong at style priority. A local color (team, state or warning)
        // must not be overwritten when a resource resolves on attachment/theme change.
        button.Classes.Add("app-button");
        if (primary) button.Classes.Add("primary");
        paletteBound.Add(button,new());
        Track(button, text);
        button.Click += (_, _) => action();
        return button;
    }

    public static TextBlock Text(string text, bool heading = false) { var control = RawText(L.T(text), heading); Track(control,text); return control; }
    public static TextBlock RawText(string text, bool heading = false) {
        var label=new TextBlock {
            Text=text,FontSize=heading?18:14,FontWeight=heading?FontWeight.SemiBold:FontWeight.Normal,
            TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,heading?10:0,0,8) };
        label.Classes.Add("app-ink");paletteBound.Add(label,new());return label;
    }

    public static ComboBox Combo(params string[] choices) { var control = RawCombo(choices.Select(L.T).ToArray()); Track(control,choices); return control; }
    public static ComboBox RawCombo(params string[] choices) => new() {
        ItemsSource=choices,SelectedIndex=choices.Length>0?0:-1,Foreground=Ink,Background=Panel,
        HorizontalAlignment=HorizontalAlignment.Stretch,Margin=new Thickness(0,0,0,10)
    };

    public static Bitmap Bitmap(System.Drawing.Image source)
    {
        double scale = Math.Min(1, 1280.0 / Math.Max(source.Width, source.Height));
        using var reduced = new System.Drawing.Bitmap(Math.Max(1, (int)(source.Width * scale)), Math.Max(1, (int)(source.Height * scale)));
        using (var g = System.Drawing.Graphics.FromImage(reduced)) {
            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(source, 0, 0, reduced.Width, reduced.Height);
        }
        using var stream = new MemoryStream();
        reduced.Save(stream, System.Drawing.Imaging.ImageFormat.Png); stream.Position = 0;
        return new Bitmap(stream);
    }

    public static Window Owner(Control control) => TopLevel.GetTopLevel(control) as Window
        ?? throw new InvalidOperationException("The control must be attached to a window.");

    public static async Task<string?> PickImage(Control control)
    {
        var files = await Owner(control).StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = L.T("Imagem PNG ou JPEG"), AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("PNG / JPEG") { Patterns = ["*.png", "*.jpg", "*.jpeg"] }] });
        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    // Windows clipboard bridge only; it creates no Forms or desktop windows.
    public static System.Drawing.Bitmap? ClipboardImage()
    {
        using var image = System.Windows.Forms.Clipboard.GetImage();
        return image is null ? null : new System.Drawing.Bitmap(image);
    }

    public static void CopyText(string text) => System.Windows.Forms.Clipboard.SetText(text);
    public static void OpenFolder(string path) => System.Diagnostics.Process.Start(
        new System.Diagnostics.ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });

    // Shared confirmation dialog for desktop workflows.
    internal static Window? ActiveQuestion { get; private set; }
    public static async Task<int> Ask(Control owner, string message, bool winnerConfirmation = false)
    {
        var dialog = new Window { Title = "CasterDesk", Width = 540, Height = 260,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = winnerConfirmation ? Brush.Parse("#48242B") : Canvas,
            FontFamily = new FontFamily("Segoe UI") };
        var content = new StackPanel { Margin = new Thickness(20) };
        var question = Text(message);
        if (winnerConfirmation) { question.FontSize = 21; question.TextAlignment = TextAlignment.Center; question.Foreground = Brushes.White; question.Margin = new Thickness(0, 12, 0, 28); }
        content.Children.Add(question);
        int answer = -1;
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12,
            HorizontalAlignment = winnerConfirmation ? HorizontalAlignment.Center : HorizontalAlignment.Left };
        var choices = winnerConfirmation ? new[] { (1, "Confirmar vencedor"), (-1, "Cancelar") } : new[] { (1, "Sim"), (0, "Não"), (-1, "Cancelar") };
        foreach (var choice in choices) {
            int result = choice.Item1;
            var choiceButton = Button(choice.Item2, () => { answer = result; dialog.Close(result); }, result == 1);
            choiceButton.Name = "ask-" + result; actions.Children.Add(choiceButton);
            if (winnerConfirmation) { choiceButton.FontSize = 17; choiceButton.MinHeight = 46; choiceButton.Padding = new Thickness(18, 10); }
        }
        content.Children.Add(actions); dialog.Content = content;
        ActiveQuestion = dialog;
        try { await dialog.ShowDialog<int>(Owner(owner)); }
        finally { if (ActiveQuestion == dialog) ActiveQuestion = null; }
        return answer;
    }

    public static void ApplyTheme(Application app)
    {
        app.Styles.Add(new global::Avalonia.Themes.Fluent.FluentTheme());
        app.RequestedThemeVariant = ThemeVariant.Dark;
        WindowFrame.Install(app);
        Theme(ThemeName);
        var buttonDefaults = new Style(x => x.OfType<Button>().Class("app-button"));
        buttonDefaults.Setters.Add(new Setter(global::Avalonia.Controls.Button.ForegroundProperty, new global::Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CasterDeskInk")));
        buttonDefaults.Setters.Add(new Setter(global::Avalonia.Controls.Button.BackgroundProperty, new global::Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CasterDeskHeader")));
        buttonDefaults.Setters.Add(new Setter(global::Avalonia.Controls.Button.BorderBrushProperty, new global::Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CasterDeskFrame")));
        buttonDefaults.Setters.Add(new Setter(global::Avalonia.Controls.Button.CornerRadiusProperty, new CornerRadius(1)));
        app.Styles.Add(buttonDefaults);
        var textDefaults = new Style(x => x.OfType<TextBlock>().Class("app-ink"));
        textDefaults.Setters.Add(new Setter(TextBlock.ForegroundProperty, new global::Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CasterDeskInk")));
        app.Styles.Add(textDefaults);
        foreach(string state in new[]{"",":selected",":pointerover",":selected:pointerover"}){
            var item=new Style(x=>{var selector=x.OfType<ComboBoxItem>();foreach(var c in state.Split(':',StringSplitOptions.RemoveEmptyEntries))selector=selector.Class(":"+c);return selector;});
            item.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty,new global::Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CasterDeskInk")));
            item.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty,new global::Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension(state.Length==0?"CasterDeskPanel":"CasterDeskHeader")));
            app.Styles.Add(item);
        }
        var primary = new Style(x => x.OfType<Button>().Class("primary"));
        primary.Setters.Add(new Setter(global::Avalonia.Controls.Button.BackgroundProperty, Brush.Parse("#327FA8")));
        primary.Setters.Add(new Setter(global::Avalonia.Controls.Button.ForegroundProperty, Brushes.White));
        app.Styles.Add(primary);
        var hover = new Style(x => x.OfType<Button>().Class("primary").Class(":pointerover"));
        hover.Setters.Add(new Setter(global::Avalonia.Controls.Button.BackgroundProperty, new global::Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CasterDeskAccent")));
        app.Styles.Add(hover);
    }
}

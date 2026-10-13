using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Styling;

namespace CasterDesk.Avalonia;

internal static class Ui
{
    public static bool Dark { get; private set; } = true;
    public static IBrush Canvas => Brush.Parse(Dark ? "#182637" : "#E7EDF3");
    public static IBrush Panel => Brush.Parse(Dark ? "#233448" : "#F6F8FA");
    public static IBrush Team(int team, bool selected = true) => new SolidColorBrush(
        Color.FromArgb(selected ? (byte)255 : (byte)150, team == 0 ? (byte)64 : (byte)255,
            team == 0 ? (byte)143 : (byte)119, team == 0 ? (byte)179 : (byte)55));
    public static void Theme(bool dark) { Dark = dark; if (Application.Current != null) Application.Current.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light; }

    public static Border Card(string title, Control child)
    {
        var body = new Grid { RowDefinitions = new("Auto,*") };
        var label = Text(title, true); label.FontSize = 14; label.Margin = new Thickness(0, 0, 0, 8);
        body.Children.Add(label); body.Children.Add(child); Grid.SetRow(child, 1);
        return new Border { Background = Panel, Padding = new Thickness(10), Margin = new Thickness(4), Child = body };
    }

    static readonly Dictionary<string, WeakReference<Bitmap>> images = new(StringComparer.OrdinalIgnoreCase);
    public static Bitmap FileImage(string path, bool compactPortrait = false)
    {
        string key = Path.GetFullPath(path) + File.GetLastWriteTimeUtc(path).Ticks + compactPortrait;
        if (images.TryGetValue(key, out var weak) && weak.TryGetTarget(out var saved)) return saved;
        using var source = System.Drawing.Image.FromFile(path);
        Bitmap result;
        if (compactPortrait) {
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
        if (primary) button.Classes.Add("primary");
        button.Click += (_, _) => action();
        return button;
    }

    public static TextBlock Text(string text, bool heading = false) => RawText(L.T(text), heading);
    public static TextBlock RawText(string text, bool heading = false) => new() {
        Text = text, FontSize = heading ? 18 : 14,
        FontWeight = heading ? FontWeight.SemiBold : FontWeight.Normal,
        TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, heading ? 10 : 0, 0, 8) };

    public static ComboBox Combo(params string[] choices) => RawCombo(choices.Select(L.T).ToArray());
    public static ComboBox RawCombo(params string[] choices) => new() {
        ItemsSource = choices, SelectedIndex = choices.Length > 0 ? 0 : -1,
        HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 0, 10) };

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

    // Yes / No / Cancel, using an Avalonia dialog rather than WPF or WinForms UI.
    internal static Window? ActiveQuestion { get; private set; }
    public static async Task<int> Ask(Control owner, string message)
    {
        var dialog = new Window { Title = "CasterDesk", Width = 540, Height = 260,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Canvas };
        var content = new StackPanel { Margin = new Thickness(20) };
        content.Children.Add(Text(message));
        int answer = -1;
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        foreach (var choice in new[] { (1, "Sim"), (0, "Não"), (-1, "Cancelar") }) {
            int result = choice.Item1;
            var choiceButton = Button(choice.Item2, () => { answer = result; dialog.Close(result); }, result == 1);
            choiceButton.Name = "ask-" + result; actions.Children.Add(choiceButton);
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
        var primary = new Style(x => x.OfType<Button>().Class("primary"));
        primary.Setters.Add(new Setter(global::Avalonia.Controls.Button.BackgroundProperty, Brush.Parse("#327FA8")));
        primary.Setters.Add(new Setter(global::Avalonia.Controls.Button.ForegroundProperty, Brushes.White));
        app.Styles.Add(primary);
        var hover = new Style(x => x.OfType<Button>().Class("primary").Class(":pointerover"));
        hover.Setters.Add(new Setter(global::Avalonia.Controls.Button.BackgroundProperty, Brush.Parse("#FF4335")));
        app.Styles.Add(hover);
    }
}

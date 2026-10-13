using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Styling;

namespace CasterDesk.Avalonia;

internal static class Ui
{
    public static readonly IBrush Canvas = Brush.Parse("#182637"), Panel = Brush.Parse("#233448");
    public static Button Button(string text, Action action, bool primary = false)
    {
        var button = new Button { Content = L.T(text), Margin = new Thickness(0, 0, 0, 8),
            HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center };
        if (primary) button.Classes.Add("primary");
        button.Click += (_, _) => action();
        return button;
    }

    public static TextBlock Text(string text, bool heading = false) => new() {
        Text = L.T(text), FontSize = heading ? 18 : 14,
        FontWeight = heading ? FontWeight.SemiBold : FontWeight.Normal,
        TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, heading ? 10 : 0, 0, 8) };

    public static ComboBox Combo(params string[] choices) => new() {
        ItemsSource = choices.Select(L.T).ToArray(), SelectedIndex = choices.Length > 0 ? 0 : -1,
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
            actions.Children.Add(Button(choice.Item2, () => { answer = result; dialog.Close(result); }, result == 1));
        }
        content.Children.Add(actions); dialog.Content = content;
        await dialog.ShowDialog<int>(Owner(owner));
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


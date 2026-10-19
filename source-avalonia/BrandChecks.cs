using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace CasterDesk.Avalonia;

internal static class BrandChecks
{
    public static void Run()
    {
        var shell = new StudioShell { Width = 1280, Height = 800 };
        if (shell.WindowState != WindowState.Maximized) throw new Exception("Application must start maximized.");
        shell.Show(); Dispatcher.UIThread.RunJobs();
        Thread.Sleep(150); Dispatcher.UIThread.RunJobs();
        shell.Championship.Refresh(); Dispatcher.UIThread.RunJobs();
        if (shell.Icon == null || shell.FontFamily.Name != "Segoe UI") throw new Exception("Brand icon or unchanged interface font missing.");
        var mark = shell.GetVisualDescendants().OfType<Viewbox>().Single(v => v.Name == "casterdesk-brand");
        if (mark.Width != 240 || mark.Child is not Canvas canvas || canvas.Children.Count != 4)
            throw new Exception("Approved outlined SVG brand missing.");
        foreach (bool white in new[] { true, false }) {
            var preview = new Border { Background = white ? Brushes.Black : Brushes.White, Child = BrandMark.Create(white, 480) };
            preview.Measure(new Size(520, 160)); preview.Arrange(new Rect(0, 0, 520, 160));
            using var bitmap = new RenderTargetBitmap(new PixelSize(520, 160), new Vector(96, 96));
            bitmap.Render(preview); bitmap.Save(Path.Combine(AppPaths.DataDirectory, white ? "brand-white.png" : "brand-black.png"));
        }
        using (var bitmap = new RenderTargetBitmap(new PixelSize(1280, 800), new Vector(96, 96))) {
            bitmap.Render((Control)shell.Content!); bitmap.Save(Path.Combine(AppPaths.DataDirectory, "brand-shell.png"));
        }
        foreach(var size in new[]{new Size(980,640),new Size(1920,1080)}) {
            shell.Width=size.Width; shell.Height=size.Height; shell.Measure(size); shell.Arrange(new Rect(size)); Dispatcher.UIThread.RunJobs();
            Thread.Sleep(150); Dispatcher.UIThread.RunJobs(); shell.Championship.Refresh(); Dispatcher.UIThread.RunJobs();
            var timer=shell.GetVisualDescendants().OfType<HeaderTimer>().Single();
            var nav=shell.GetVisualDescendants().OfType<StackPanel>().Single(c=>c.Name=="main-navigation");
            var gear=shell.GetVisualDescendants().OfType<Button>().Single(c=>c.Name=="settings-menu");
            if(timer.TranslatePoint(default,shell)!.Value.X < nav.TranslatePoint(default,shell)!.Value.X+nav.Bounds.Width || timer.TranslatePoint(default,shell)!.Value.X+timer.Bounds.Width > gear.TranslatePoint(default,shell)!.Value.X)
                throw new Exception("Timer overlaps navigation/settings at " + size);
            using var bitmap=new RenderTargetBitmap(new PixelSize((int)size.Width,(int)size.Height),new Vector(96,96));
            bitmap.Render((Control)shell.Content!); bitmap.Save(Path.Combine(AppPaths.DataDirectory,$"header-shell-{size.Width}.png"));
        }
        shell.Close(); Dispatcher.UIThread.RunJobs();
        File.WriteAllText(Path.Combine(AppPaths.DataDirectory, "avalonia-brand-check.txt"), "PASS: approved SVG paths, window icon, unchanged Segoe UI and black/white brand previews. Headless only.");
    }
}

using System.Reflection;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace CasterDesk.Desktop;

internal static class ChromeOverlayChecks
{
    internal static Control Host(Window window)=>window.GetVisualAncestors().OfType<Control>().Single(c=>c.GetType().Name=="TopLevelHost");
    internal static void AssertSingleCaption(Window window)
    {
        var root=Host(window);
        var buttons=root.GetVisualDescendants().OfType<Button>().ToArray();
        var extra=buttons.Where(b=>b.Name is "PART_MinimizeButton" or "PART_MaximizeButton" or "PART_CloseButton" or "PART_FullScreenButton").ToArray();
        if(extra.Length>0)throw new Exception("Duplicate caption overlay: "+string.Join(", ",extra.Select(b=>b.Name)));
        if(buttons.Count(b=>b.Name is "caption-minimize" or "caption-maximize" or "caption-close")!=3)
            throw new Exception("Custom caption must contain exactly its three buttons.");
    }
    internal static void Run()
    {
        L.Set("pt-BR");Ui.Theme("standard");
        var shell=new StudioShell(new MatchSession()){WindowState=WindowState.Normal,Width=1280,Height=800};shell.Show();Dispatcher.UIThread.RunJobs();
        int count=0;
        try {
            var host=Host(shell);
            // Headless does not request platform chrome. Force the same host layer
            // that Win32 requests for extended captions, without opening a native UI.
            var update=host.GetType().GetMethod("UpdateDrawnDecorations",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)!;
            Type partsType=Nullable.GetUnderlyingType(update.GetParameters()[0].ParameterType)!;
            object titlebar=Enum.Parse(partsType,"TitleBar");
            foreach(string theme in new[]{"standard","light","dark"})foreach(var state in new[]{WindowState.Normal,WindowState.Maximized}) {
                shell.WindowState=state;Ui.Repaint(theme,shell);
                update.Invoke(host,[titlebar,state]);
                shell.UpdateLayout();host.UpdateLayout();Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                using(var image=new RenderTargetBitmap(new PixelSize(1280,800))){image.Render(host);image.Save(Path.Combine(AppPaths.DataDirectory,$"chrome-host-{theme}-{state}.png"));}
                AssertSingleCaption(shell);count++;
            }
            File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"chrome-overlay-check.txt"),$"PASS: {count} full-host caption checks. Three themes, normal/maximized, no duplicate platform overlay. Headless only; no desktop window opened.");
        } finally {shell.Close();Dispatcher.UIThread.RunJobs();Ui.Theme("standard");}
    }
}

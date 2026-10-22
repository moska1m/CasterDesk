using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using System.Text.Json;

namespace CasterDesk.Desktop;

internal static class EasterEggChecks
{
    static int count;
    static void Need(bool condition,string message){count++;if(!condition)throw new Exception("Easter egg: "+message);}
    static void Scene(Window shell){shell.UpdateLayout();Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick(2);using var image=shell.CaptureRenderedFrame();}
    static T Find<T>(Control root,string name)where T:Control=>root.GetVisualDescendants().OfType<T>().Single(c=>c.Name==name);
    static void Click(Window owner,Control control,MouseButton button=MouseButton.Left)
    {
        Scene(owner);var point=control.TranslatePoint(new Point(control.Bounds.Width/2,control.Bounds.Height/2),owner)!.Value;
        owner.MouseMove(point);owner.MouseDown(point,button);owner.MouseUp(point,button);Scene(owner);
    }
    internal static void Run()
    {
        Need(!Program.BlackIcePreview,"unlock tested through ordinary launch, without preview flag");
        string config=AppPaths.ConfigFile("settings.json");string? before=File.Exists(config)?File.ReadAllText(config):null;
        MatchFiles.AtomicText(config,JsonSerializer.Serialize(new Settings{Language="pt-BR",VisualTheme="standard"}));
        var invalid=new Settings{VisualTheme="black-ice"};ConfigurationStore.Normalize(invalid);Need(invalid.VisualTheme=="standard","locked hidden theme is not a selectable saved preference");
        var session=new MatchSession();StudioShell? shell=null;
        StudioShell Open(){var window=new StudioShell(session){WindowState=WindowState.Normal,Width=1280,Height=800};window.Show();Scene(window);return window;}
        ComboBox Options(StudioShell window){_ = window.OpenOptions();Scene(window);Find<TabControl>(window.ActiveSurface!,"settings-tabs").SelectedIndex=2;Scene(window);return Find<ComboBox>(window.ActiveSurface!,"settings-theme");}
        Settings Saved()=>ConfigurationStore.Read<Settings>("settings.json");
        try {
            shell=Open();var themes=Options(shell);Need(themes.Items.Count==3&&!themes.Items.Cast<object>().Any(t=>t.ToString()=="Black Ice"),"fresh settings do not reveal the hidden theme");
            shell.ActiveSurface!.Close();Scene(shell);shell.ToggleMenu();Scene(shell);
            var microphone=Find<Border>(shell,"menu-brand-microphone");Need(!microphone.Focusable&&microphone.Cursor==null,"microphone remains an ordinary non-button region without a hand cursor");
            for(int i=0;i<5;i++)Click(shell,Find<Border>(shell,"menu-brand-microphone"));
            Need(!Saved().BlackIceUnlocked&&Ui.ThemeName=="standard","five left clicks do not unlock");
            Click(shell,Find<Border>(shell,"menu-brand-microphone"),MouseButton.Right);Need(!Saved().BlackIceUnlocked,"right click does not count");
            shell.ToggleMenu();shell.ToggleMenu();Scene(shell);Click(shell,Find<Border>(shell,"menu-brand-microphone"));Need(!Saved().BlackIceUnlocked,"closing the menu resets incomplete sequence");
            for(int i=0;i<5;i++)Click(shell,Find<Border>(shell,"menu-brand-microphone"));
            Need(Saved().BlackIceUnlocked&&Saved().VisualTheme=="black-ice"&&Ui.BlackIce,"six clicks unlock, apply and save immediately");
            Need(shell.ActiveSurface==null&&shell.Championship.IsEffectivelyEnabled,"unlock does not interrupt match with a modal");
            using(var shot=new RenderTargetBitmap(new PixelSize(1280,800))){shot.Render(shell);shot.Save(Path.Combine(AppPaths.DataDirectory,"secret-unlocked.png"));}
            themes=Options(shell);Need(themes.Items.Count==4&&themes.Items[3]?.ToString()=="Black Ice"&&themes.SelectedIndex==3,"unlocked theme joins ordinary appearance choices");
            themes.SelectedIndex=0;Scene(shell);Need(!Ui.BlackIce&&Saved().BlackIceUnlocked&&Saved().VisualTheme=="standard","switching away retains unlock");
            shell.ActiveSurface!.Close();Scene(shell);shell.Close();Dispatcher.UIThread.RunJobs();shell=Open();
            Need(Ui.ThemeName=="standard"&&Saved().BlackIceUnlocked,"restart preserves chosen common theme and unlock");
            themes=Options(shell);Need(themes.Items.Count==4,"unlocked theme remains available after restart");themes.SelectedIndex=3;Scene(shell);
            shell.ActiveSurface!.Close();Scene(shell);shell.Close();Dispatcher.UIThread.RunJobs();shell=Open();
            Need(Ui.BlackIce&&Saved().BlackIceUnlocked,"selected Black Ice survives ordinary restart");
            themes=Options(shell);themes.SelectedIndex=2;Scene(shell);shell.ActiveSurface!.Close();Scene(shell);shell.ToggleMenu();Scene(shell);
            for(int i=0;i<6;i++)Click(shell,Find<Border>(shell,"menu-brand-microphone"));
            Need(Ui.ThemeName=="light"&&Saved().VisualTheme=="light","repeating discovery does not override a later theme choice");
            File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"easter-egg-check.txt"),$"PASS: {count} checks. Hidden before discovery, real mouse click sequence, no button styling, reset/right-click behavior, immediate apply, persistent unlock, all theme choices and restart. Headless only.");
        } finally {
            if(shell?.ActiveSurface!=null){shell.ActiveSurface.Close();Dispatcher.UIThread.RunJobs();}shell?.Close();Dispatcher.UIThread.RunJobs();
            if(before==null)File.Delete(config);else File.WriteAllText(config,before);Ui.Theme("standard");
        }
    }
}

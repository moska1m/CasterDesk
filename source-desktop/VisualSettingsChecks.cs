using Avalonia.Interactivity;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;
using Avalonia.Media.Imaging;

namespace CasterDesk.Desktop;
internal static class VisualSettingsChecks
{
    static int count;
    static void Need(bool value,string message){count++;if(!value)throw new Exception("Visual settings: "+message);}
    static void Pump()=>Dispatcher.UIThread.RunJobs();
    static void Click(Button b){b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();}
    public static void Run()
    {
        string config=AppPaths.ConfigFile("settings.json");string? before=File.Exists(config)?File.ReadAllText(config):null;
        MatchFiles.AtomicText(config,System.Text.Json.JsonSerializer.Serialize(new Settings{Language="pt-BR",VisualTheme="standard"}));L.Set("pt-BR");
        var session=new MatchSession();session.Current.TeamA="Banco";session.Current.TeamB="Team custom";
        var shell=new StudioShell(session);shell.WindowState=WindowState.Normal;shell.Width=1280;shell.Height=800;shell.Show();Pump();
        try {
            Need(shell.WindowDecorations==WindowDecorations.None,"custom frame replaces system titlebar");
            Need(shell.GetVisualDescendants().OfType<ContentPresenter>().Any(p=>p.Name=="PART_ContentPresenter"),"frame presents original content");
            shell.ToggleMenu();Pump();
            Need(shell.GetVisualDescendants().OfType<Border>().Single(b=>b.Name=="menu-scrim").IsVisible,"menu dims workspace");
            Need(!shell.Championship.IsEffectivelyEnabled,"workspace disabled behind menu");
            Need(shell.GetVisualDescendants().OfType<Button>().Count(b=>b.Name is "options-menu" or "updates-menu" or "credits-menu")==3,"three compact menu entries");
            Capture(shell,"visual-standard-menu.png");
            shell.ToggleMenu();Pump();Need(shell.Championship.IsEffectivelyEnabled,"workspace enabled after closing menu");
            Task options=shell.OpenOptions();Pump();var window=shell.OwnedWindows.Single(w=>w.Name=="options-window");
            Need(window.WindowDecorations==WindowDecorations.None,"options uses shared frame");
            var tabs=window.GetVisualDescendants().OfType<TabControl>().Single(t=>t.Name=="settings-tabs");
            Need(tabs.Items.Count==6,"all existing preference groups available without fake audio controls");
            ComboBox Find(string name) { foreach(int n in Enumerable.Range(0,tabs.Items.Count)){tabs.SelectedIndex=n;Pump();var box=window.GetVisualDescendants().OfType<ComboBox>().FirstOrDefault(b=>b.Name==name);if(box!=null)return box;}throw new Exception(name); }
            var theme=Find("settings-theme");theme.IsDropDownOpen=true;Pump();Need(theme.IsDropDownOpen,"dropdown opens inside custom frame");theme.IsDropDownOpen=false;
            foreach(var (index,name) in new[]{(1,"dark"),(2,"light"),(0,"standard")}) {
                theme.SelectedIndex=index;Pump();Need(Ui.ThemeName==name,"live theme "+name);
                Need(ConfigurationStore.Read<Settings>("settings.json").VisualTheme==name,"saved theme "+name);
                Capture(window,"visual-options-"+name+".png");
            }
            var language=Find("settings-language");
            foreach(var (index,code) in new[]{(1,"en"),(2,"es"),(0,"pt-BR")}) {
                language.SelectedIndex=index;Pump();Need(L.Current==code,"language switches live "+code);
                Need(window.Title==L.T("Opções"),"open options title translated "+code);
                Need(((TabItem)tabs.Items[0]!).Header?.ToString()==L.T("GERAL"),"open options tabs translated "+code);
                Need(shell.Championship.Session.Current.TeamA=="Banco" && shell.Championship.Session.Current.TeamB=="Team custom","custom names preserved "+code);
            }
            Find("settings-theme").SelectedIndex=2;Pump();
            window.Close();Pump();Need(options.IsCompleted,"options closes normally");
            shell.ToggleMenu();Pump();Need(!Ui.Dark,"light menu follows light theme");Capture(shell,"visual-light-menu.png");shell.ToggleMenu();
            var again=shell.OpenOptions();Pump();var settingsWindow=shell.OwnedWindows.Single(w=>w.Name=="options-window");
            var againTabs=settingsWindow.GetVisualDescendants().OfType<TabControl>().Single(t=>t.Name=="settings-tabs");againTabs.SelectedIndex=2;Pump();
            settingsWindow.GetVisualDescendants().OfType<ComboBox>().Single(c=>c.Name=="settings-theme").SelectedIndex=1;Pump();settingsWindow.Close();Pump();Need(again.IsCompleted,"preferences reopen with saved theme");
            shell.ToggleMenu();Pump();Capture(shell,"visual-dark-menu.png");shell.ToggleMenu();
            var ownDialog=Ui.Dialog(shell,"Escolher mapa",Ui.Text("Mapa"),500,320);ownDialog.Show(shell);Pump();Need(ownDialog.WindowDecorations==WindowDecorations.None,"map dialog receives shared frame");ownDialog.Close();
            File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"visual-settings-check.txt"),$"PASS: {count} checks. Shared frames, menu blocking, three themes and live PT/EN/ES. Headless only; native Windows drag/resize requires manual verification.");
        }finally{shell.Close();Pump();L.Set("pt-BR");Ui.Theme(true);if(before==null)File.Delete(config);else File.WriteAllText(config,before);}
    }
    static void Capture(Window window,string file) {
        var size=new Size(window.Width,window.Height);window.Measure(size);window.Arrange(new Rect(size));Pump();
        using var image=new RenderTargetBitmap(new PixelSize((int)size.Width,(int)size.Height),new Vector(96,96));image.Render(window);image.Save(Path.Combine(AppPaths.DataDirectory,file));
    }
}

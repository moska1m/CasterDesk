using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace CasterDesk.Desktop;

internal static class WindowColorChecks
{
    static int count;
    static void Need(bool condition,string message){count++;if(!condition)throw new Exception("Window/color regression: "+message);}
    static void Pump()=>Dispatcher.UIThread.RunJobs();
    static void Scene(Window owner){owner.UpdateLayout();Pump();AvaloniaHeadlessPlatform.ForceRenderTimerTick();using var frame=owner.CaptureRenderedFrame();}
    static T Find<T>(Control root,string name) where T:Control=>root.GetVisualDescendants().OfType<T>().Single(c=>c.Name==name);
    internal static void Run()
    {
        L.Set("pt-BR");Ui.Theme("standard");
        var session=new MatchSession();session.New();session.ConfigureSpec("MD1",[session.Catalog.Maps[0].Id]);session.Side(session.Map!.MapId,false,0);
        var map=session.Map!;for(int slot=0;slot<4;slot++)map.Bans[slot]=MapRecord.NoBan(map.BanRole(slot));
        var view=new ChampionshipView(session);var owner=new Window{Title="Window / colors",Content=view,Width=1280,Height=800};owner.Show();Scene(owner);
        try {
            foreach(string theme in new[]{"standard","light","dark","standard"}) {
                Ui.Repaint(theme,owner);view.Refresh();Scene(owner);
                foreach(int slot in Enumerable.Range(0,12)) {
                    var card=Find<Button>(view,"ban-slot-"+slot);string expected=Ui.Team(MapRecord.BanTeam(slot),false).ToString()!;
                    Need(card.Background?.ToString()==expected,$"ban {slot} keeps team color in {theme}: {card.Background} instead of {expected}");
                    Need(card.GetVisualDescendants().OfType<Border>().First().Background?.ToString()==expected,"rendered ban color matches team");
                    Need(card.Foreground?.ToString()==Brushes.White.ToString(),"ban text remains white");
                }
                foreach(int team in new[]{0,1})Need(Find<Button>(view,"round-winner-"+team).Background?.ToString()==Ui.Team(team,false).ToString(),"round winner retains team color");
                var picker=view.OperatorDialog(0);picker.Show(owner);Pump();
                Need(Find<Button>(picker,"ban-none-choice").Background?.ToString()==Brush.Parse("#334354").ToString(),"no-ban picker keeps background");
                Need(picker.GetVisualDescendants().OfType<Button>().Where(b=>b.Name?.StartsWith("operator-")==true).All(b=>b.Background?.ToString()==Brush.Parse("#334354").ToString()),"operator picker keeps background");picker.Close();Pump();
                view.SetSpecMode(true);Scene(owner);
                Need(view.GetVisualDescendants().OfType<Button>().Where(b=>b.Content is StackPanel p&&p.Children.OfType<Image>().Any()).Any(b=>b.Background?.ToString()==Ui.Team(0).ToString()),"SPEC blue ban stays blue");
                Need(view.GetVisualDescendants().OfType<Button>().Where(b=>b.Content is StackPanel p&&p.Children.OfType<Image>().Any()).Any(b=>b.Background?.ToString()==Ui.Team(1).ToString()),"SPEC orange ban stays orange");
                view.SetSpecMode(false);Scene(owner);
                using var bitmap=new RenderTargetBitmap(new PixelSize(1280,800));bitmap.Render(owner);bitmap.Save(Path.Combine(AppPaths.DataDirectory,"colors-"+theme+".png"));
            }
            // Native dragging cannot move a headless window. Double-click verifies caption input routing.
            foreach(var point in new[]{new Point(640,26),new Point(640,8),new Point(18,16)}) {
                owner.WindowState=WindowState.Normal;Scene(owner);owner.MouseMove(point);
                for(int i=0;i<2;i++){owner.MouseDown(point,MouseButton.Left);owner.MouseUp(point,MouseButton.Left);}Pump();
                Need(owner.WindowState==WindowState.Maximized,"double-click blank/icon caption maximizes: "+point);
            }
            File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"window-color-check.txt"),$"PASS: {count} checks. Team and picker colors across themes; caption hit routing. Native move/snap remains a Windows manual check.");
        } finally {view.Stop();owner.Close();Pump();Ui.Theme("standard");}
    }
}

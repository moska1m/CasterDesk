using Avalonia.VisualTree;
using Avalonia.Input;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Interactivity;
namespace CasterDesk.Desktop;
internal static class SpecChecks
{
    static int count;
    static void Need(bool value,string label){count++;if(!value)throw new Exception("SPEC: "+label);}
    static void Click(Control root,string name){Dispatcher.UIThread.RunJobs();root.Measure(new Size(1280,800));root.Arrange(new Rect(0,0,1280,800));Dispatcher.UIThread.RunJobs();var b=root.GetVisualDescendants().OfType<Button>().SingleOrDefault(b=>b.Name==name)??throw new Exception("Missing SPEC button "+name+"; available "+string.Join(",",root.GetVisualDescendants().OfType<Button>().Select(b=>b.Name)));b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs();}
    public static void Run()
    {
        var session=new MatchSession();session.New();var catalog=session.Catalog;var id=catalog.Maps[0].Id;var sites=catalog.Maps[0].Sites;
        var view=new ChampionshipView(session);view.SetSpecMode(true);var owner=new Window{Width=1280,Height=800,Content=view};owner.Show();Dispatcher.UIThread.RunJobs();
        Need(view.GetVisualDescendants().OfType<TextBox>().Single(x=>x.Name=="spec-team-0").Text=="Equipe Azul","blue default shown without mutating canonical default");
        Need(!view.GetVisualDescendants().Any(x=>x is Control c && c.Name=="use-obs-bans"),"no OBS toggle in SPEC");
        var choice=view.GetVisualDescendants().OfType<ComboBox>().Single(x=>x.Name=="spec-map-0");choice.SelectedIndex=0;Dispatcher.UIThread.RunJobs();
        Need(session.Map?.MapId==id&&session.Current.DirectMaps,"direct map selection initializes canonical match");
        Need(session.Current.Veto.All(string.IsNullOrEmpty)&&session.Current.CoinWinner==-1,"SPEC does not fabricate veto or coin flip");
        var side=view.GetVisualDescendants().OfType<ComboBox>().Single(x=>x.Name=="spec-side");side.SelectedIndex=1;Dispatcher.UIThread.RunJobs();
        Need(session.Map!.InitialAttack==0,"initial side uses shared command");
        Click(view,"round-site-0");Click(view,"round-winner-0");Need(view.RoundConfirm!.IsEnabled,"SPEC confirmation accepts absent ending");Click(view,"confirm-round");Need(session.Map!.Rounds[0].Ending=="","absent ending stored as missing, not invented");Need(session.Map!.Rounds.Count==1&&view.ActiveDialog==null,"SPEC registers by buttons without OBS or automatic ban popup");session.Undo();
        Dispatcher.UIThread.RunJobs();view.Measure(new Size(1280,800));view.Arrange(new Rect(0,0,1280,800));Dispatcher.UIThread.RunJobs();
        // Pointer hit testing must use the new scene after Undo replaced the controls.
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        using(var frame=owner.CaptureRenderedFrame()) { }
        var name=view.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="spec-team-0");var point=name.TranslatePoint(new Point(30,16),owner)!.Value;owner.MouseDown(point,MouseButton.Left);owner.MouseUp(point,MouseButton.Left);Dispatcher.UIThread.RunJobs();Need(!name.IsReadOnly,"click unlocks inline name editing");name.Text="SPEC Blue";Dispatcher.UIThread.RunJobs();view.FlushNames();Dispatcher.UIThread.RunJobs();Need(session.Current.TeamA=="SPEC Blue"&&session.Current.TeamBIsDefault,"inline team editing preserves untouched default");session.Names("","");
        session.Round(sites[0],1,"time");var sideBefore=System.Text.Json.JsonSerializer.Serialize(session.Current,MatchStore.Options);bool sideRejected=false;try{session.SpecSide(id,false,1);}catch(InvalidDataException){sideRejected=true;}Need(sideRejected&&System.Text.Json.JsonSerializer.Serialize(session.Current,MatchStore.Options)==sideBefore,"side correction cannot silently erase incompatible ending");session.Undo();
        var winners=new[]{0,1,0,1,1,0,0,1};for(int n=0;n<winners.Length;n++)session.Round(sites[n%sites.Length],winners[n],"elimination");
        Click(view,"spec-edit-2");Click(view,"spec-edit-winner-1");Click(view,"spec-edit-save");
        Need(session.Map!.Rounds.Select(r=>r.Winner).SequenceEqual(new[]{0,1,1,1,1,0,0,1}),"round 3 winner correction preserves others");
        Need(session.Map.Score().A==3&&session.Map.Score().B==5,"eight-round accumulated score recalculated");
        Need(view.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Text=="3–5"),"history score updates through shared records");
        Click(view,"spec-edit-4");Click(view,"spec-edit-site-1");Click(view,"spec-edit-save");
        Need(session.Map.Rounds[4].Site==sites[1]&&session.Map.Score().A==3&&session.Map.Score().B==5,"site-only edit leaves score unchanged");
        var before=System.Text.Json.JsonSerializer.Serialize(session.Current,MatchStore.Options);session.DeleteRound(3);
        Need(session.Map!.Rounds.Count==7&&session.Map.Score().A==3&&session.Map.Score().B==4,"delete intermediate renumbers implicit chronological rounds and score");
        session.Undo();Need(System.Text.Json.JsonSerializer.Serialize(session.Current,MatchStore.Options)==before,"undo restores original deleted record exactly");
        session.Redo();Need(session.Map!.Rounds.Count==7,"redo reapplies deletion");
        view.SpecShortcut(new KeyEventArgs{Key=Key.Z,KeyModifiers=KeyModifiers.Control});Dispatcher.UIThread.RunJobs();Need(session.Map!.Rounds.Count==8,"Ctrl+Z routes to same operation history");
        view.SpecShortcut(new KeyEventArgs{Key=Key.Y,KeyModifiers=KeyModifiers.Control});Dispatcher.UIThread.RunJobs();Need(session.Map!.Rounds.Count==7,"Ctrl+Y reapplies operation");session.Undo();
        Click(view,"spec-edit-3");Click(view,"spec-edit-delete");Need(Ui.ActiveQuestion!=null&&session.Map!.Rounds.Count==8,"delete waits for confirmation");
        Click(Ui.ActiveQuestion!,"ask-1");Need(session.Map!.Rounds.Count==7,"confirmed UI deletion updates shared rounds");session.Undo();
        Click(view,"spec-edit-1");Click(view,"spec-edit-delete");Click(Ui.ActiveQuestion!,"ask-0");Need(session.Map!.Rounds.Count==8,"cancelling deletion preserves data");
        Need(session.Summary(id,true).Contains("Equipe Azul")&&session.Summary(id).Contains("08"),"remake reflects current restored eight rounds");
        var dialog=view.RemakeDialog();dialog.Show(owner);Dispatcher.UIThread.RunJobs();Need(dialog.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="remake-text").Text!.Contains("08"),"SPEC remake is a visible consultation");dialog.Close();
        view.SetSpecMode(false);dialog=view.RemakeDialog();dialog.Show(owner);Dispatcher.UIThread.RunJobs();Need(dialog.GetVisualDescendants().OfType<TextBox>().Any(t=>t.Name=="remake-text"),"Caster remake also opens consultation");dialog.Close();view.SetSpecMode(true);
        var saved=Path.Combine(AppPaths.BackupDirectory,"spec-match.json");MatchStore.Atomic(saved,session.Current);var imported=MatchStore.Read(saved,catalog);Need(imported.DirectMaps&&imported.Maps[0].Rounds.Count==8,"direct-map matches survive JSON import/export");
        bool refused=false;try{session.ConfigureSpec("MD3",catalog.Maps.Take(3).Select(m=>m.Id).ToArray());}catch(InvalidOperationException){refused=true;}Need(refused&&session.Map!.Rounds.Count==8,"series configuration cannot erase existing rounds");
        session.New();session.ConfigureSpec("MD3",catalog.Maps.Take(3).Select(m=>m.Id).ToArray());Need(session.Current.Maps.Count==3,"MD3 direct selection");
        session.Side(session.Map!.MapId,false,0);for(int n=0;n<7;n++)session.Round(sites[n%4],0,"elimination");session.Confirm();bool cannotUndo=false;try{session.Undo();}catch(InvalidOperationException){cannotUndo=true;}Need(cannotUndo,"undo cannot bypass confirmed-map protection");session.SelectMap(catalog.Maps[1].Id);session.Side(session.Map!.MapId,false,1);Need(session.Map.InitialAttack==1,"active MD3 map sides independent");session.Round(catalog.Maps[1].Sites[0],0,"elimination");session.Undo();Need(session.Map!.Rounds.Count==0&&session.Current.Maps[0].Confirmed,"undo current map preserves confirmed prior map");
        session.New();session.ConfigureSpec("MD5",catalog.Maps.Take(5).Select(m=>m.Id).ToArray());Need(session.Current.Maps.Count==5,"MD5 direct selection");
        session.New();session.ConfigureSpec("MD1",[id]);session.Side(id,false,0);for(int n=0;n<8;n++)session.Round(sites[n%sites.Length],n<6?0:1,"elimination");
        before=System.Text.Json.JsonSerializer.Serialize(session.Current,MatchStore.Options);refused=false;try{session.CorrectRound(6,sites[0],0);}catch(InvalidOperationException){refused=true;}
        Need(refused&&before==System.Text.Json.JsonSerializer.Serialize(session.Current,MatchStore.Options),"early end correction preserves later rounds on refusal");
        Click(view,"spec-edit-2");
        foreach(var size in new[]{new Size(980,640),new Size(1280,800),new Size(1920,1080)}){owner.Width=size.Width;owner.Height=size.Height;owner.Measure(size);owner.Arrange(new Rect(size));Dispatcher.UIThread.RunJobs();view.Measure(size);view.Arrange(new Rect(size));Dispatcher.UIThread.RunJobs();view.Refresh();Dispatcher.UIThread.RunJobs();view.Measure(size);view.Arrange(new Rect(size));Dispatcher.UIThread.RunJobs();
            var all=view.GetVisualDescendants().OfType<Control>().ToArray();
            var header=all.Single(c=>c.Name=="spec-match-header");var scoreText=all.OfType<TextBlock>().Single(c=>c.Name=="spec-score");
            Need(scoreText.GetVisualAncestors().Contains(header)&&scoreText.Text=="6 VS 2","score appears once in navigation and uses canonical score");
            var create=all.OfType<Button>().Single(c=>c.Name=="new-match");var selector=all.OfType<ComboBox>().Single(c=>c.Name=="match-selector");
            Need(create.IsVisible==(size.Width>=1500)&&selector.IsVisible==create.IsVisible,"secondary match actions hide and restore with width "+size.Width);
            if(create.IsVisible)Need(Math.Abs(create.TranslatePoint(default,view)!.Value.Y-selector.TranslatePoint(default,view)!.Value.Y)<.1&&create.Bounds.Height==selector.Bounds.Height,"new match and selector share top and height");
            var entry=all.Single(c=>c.Name=="spec-round-entry");var history=all.Single(c=>c.Name=="spec-history-card");
            Need(Math.Abs(entry.TranslatePoint(default,view)!.Value.Y-history.TranslatePoint(default,view)!.Value.Y)<.1,"registration and history headings align");
            Need(scoreText.TranslatePoint(default,view)!.Value.X>=0&&scoreText.TranslatePoint(new Point(scoreText.Bounds.Width,0),view)!.Value.X<=size.Width,"score fits narrow header");
            var spec=view.GetVisualDescendants().OfType<Grid>().Single(g=>g.Name=="spec-layout");Need(spec.Bounds.Width<=size.Width&&spec.Bounds.Height<=size.Height,"SPEC fits viewport "+size.Width);using var image=new RenderTargetBitmap(new PixelSize((int)size.Width,(int)size.Height),new Vector(96,96));image.Render(view);image.Save(Path.Combine(AppPaths.DataDirectory,"spec-"+size.Width+".png"));}
        session.New();Dispatcher.UIThread.RunJobs();var f=view.GetVisualDescendants().OfType<ComboBox>().Single(c=>c.Name=="spec-format");f.SelectedIndex=1;Dispatcher.UIThread.RunJobs();view.Measure(new Size(1280,800));view.Arrange(new Rect(0,0,1280,800));Dispatcher.UIThread.RunJobs();
        var choose=view.GetVisualDescendants().OfType<Button>().Single(b=>b.Name=="spec-series-maps");((Flyout)choose.Flyout!).ShowAt(choose);Dispatcher.UIThread.RunJobs();var content=(Control)((Flyout)choose.Flyout).Content!;
        for(int n=0;n<3;n++){content.GetVisualDescendants().OfType<ComboBox>().Single(c=>c.Name=="spec-map-"+n).SelectedIndex=n;Dispatcher.UIThread.RunJobs();}
        Need(session.Current.Maps.Count==3&&session.Map!.MapId==catalog.Maps[0].Id,"compact series picker saves all maps and keeps first active");
        ((Flyout)choose.Flyout).Hide();view.Stop();owner.Close();var shell=new StudioShell(session);shell.Show();Dispatcher.UIThread.RunJobs();shell.Championship.SetSpecMode(true);Need(!shell.GetVisualDescendants().OfType<HeaderTimer>().Single().IsVisible,"SPEC hides header timer");shell.Championship.SetSpecMode(false);Need(shell.GetVisualDescendants().OfType<HeaderTimer>().Single().IsVisible,"Caster restores header timer");shell.Close();Dispatcher.UIThread.RunJobs();File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"casterdesk-spec-check.txt"),$"PASS: {count} SPEC checks: direct maps MD1/3/5, shared state, inline edits, score propagation, deletion/undo/redo, protected inconsistencies, JSON and remake in both modes. Headless only.");
    }
}

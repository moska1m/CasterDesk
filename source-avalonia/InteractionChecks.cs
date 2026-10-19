using Avalonia.VisualTree;
using Avalonia.Interactivity;
using Avalonia.Input;
using System.Text.Json;
namespace CasterDesk.Avalonia;
internal static class InteractionChecks {
 internal static void Run() {
  int count=0;void Need(bool ok,string label){count++;if(!ok)throw new Exception("Interaction: "+label);}
  void Pump()=>Dispatcher.UIThread.RunJobs();
  void Wait(Task t){while(!t.IsCompleted){Pump();Thread.Sleep(5);}t.GetAwaiter().GetResult();}
  T Find<T>(Control c,string name)where T:Control=>c.GetVisualDescendants().OfType<T>().Single(x=>x.Name==name);
  var session=new MatchSession();session.New();var view=new ChampionshipView(session);var owner=new Window{Content=view,Width=1280,Height=800};owner.Show();Pump();
  var name=Find<TextBox>(view,"team-a");name.Focus();name.Text="NA ";Pump();Thread.Sleep(700);Pump();
  Need(ReferenceEquals(name,Find<TextBox>(view,"team-a"))&&name.IsKeyboardFocusWithin,"slow typing keeps same focused editor");
  name.Text+="ORG";Pump();Need(session.Current.TeamA!="NA ORG","team name not broadcast mid edit");
  name.RaiseEvent(new KeyEventArgs{RoutedEvent=InputElement.KeyDownEvent,Key=Key.Enter});Pump();Need(session.Current.TeamA=="NA ORG","Enter commits full name");
  view.Stop();owner.Close();
  session.New();var ids=session.Catalog.Maps.Take(1).Select(m=>m.Id).ToArray();session.ConfigureSpec("MD1",ids);session.Side(ids[0],false,0);
  for(int n=0;n<10;n++) {
   foreach(int slot in session.PendingBans())session.Ban(slot,MapRecord.NoBan(session.Map!.BanRole(slot)));
   session.Round(session.Catalog.Maps[0].Sites[n%4],n<6?0:1,"elimination");
  }
  foreach(int slot in session.PendingBans())session.Ban(slot,MapRecord.NoBan(session.Map!.BanRole(slot)));
  Need(session.Map!.Bans.All(MapRecord.IsNoBan),"no ban repeatable across twelve slots");MatchStore.Validate(session.Current,session.Catalog);
  var imported=MatchStore.Read(Path.Combine(MatchStore.Folder,session.Current.Id+".json"),session.Catalog);
  Need(imported.Maps[0].Bans.SequenceEqual(session.Map.Bans),"no bans survive JSON save/import");
  Need(session.Summary().Contains(L.T("Sem banimento")),"remake distinguishes explicit no ban");
  bool wrong=false;try{session.Ban(0,MapRecord.NoBan(session.Map.BanRole(0)=="DEF"?"ATQ":"DEF"));}catch(InvalidOperationException){wrong=true;}Need(wrong,"wrong no-ban role rejected");
  double time=0;var texts=new TextsView(new Settings{Output=Path.Combine(AppPaths.TextDirectory,"interaction-timer.txt")},()=>{},new Countdown(()=>time));
  var tw=new Window{Content=texts};tw.Show();Pump();texts.StartFromHeader();time=2;texts.SetClock(120);Need(texts.Running&&texts.Remaining==120,"apply time preserves running");
  texts.PauseClock();texts.SetClock(60);Need(!texts.Running&&texts.Remaining==60,"apply time preserves pause");
  var initial=Find<TextBox>(texts,"timer-initial");initial.Text="00:30";Find<Button>(texts,"timer-apply-time").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Need(texts.Remaining==30&&!texts.Running,"Apply time button replaces paused clock");texts.Stop();tw.Close();
  var match=new SiegeMatch();MatchStore.Save(match);var studio=new StudioView(()=>new[]{match},session.Catalog);var sw=new Window{Content=studio,Width=1280,Height=800};sw.Show();studio.Refresh(match.Id);Wait(studio.SettlePreview());Pump();
  var nick=Find<TextBox>(studio,"art-channel-nick");nick.Text="caster_test";Pump();Need(studio.Flush(),"channel saved");
  Need(MatchFiles.ReadBrand(match).BroadcastChannel=="twitch.tv/caster_test","nickname exports full Twitch channel");
  studio.Stop();sw.Close();var next=new SiegeMatch();var second=new StudioView(()=>new[]{next},session.Catalog);var sw2=new Window{Content=second,Width=1280,Height=800};sw2.Show();second.Refresh(next.Id);Wait(second.SettlePreview());Pump();
  Need(Find<TextBox>(second,"art-channel-nick").Text=="caster_test","channel reused across matches and new view");
  foreach(var size in new[]{new Size(980,640),new Size(1920,1080)}) {
   sw2.Width=size.Width;sw2.Height=size.Height;sw2.Measure(size);sw2.Arrange(new Rect(size));Pump();second.Measure(size);second.Arrange(new Rect(size));Pump();
   var selector=Find<ComboBox>(second,"art-match-selector");Need(selector.TranslatePoint(new Point(selector.Bounds.Width,0),second)!.Value.X<=second.Bounds.Width,"selector fits right edge "+size.Width);
   Need(Find<ScrollViewer>(second,"art-options-scroll").VerticalScrollBarVisibility==global::Avalonia.Controls.Primitives.ScrollBarVisibility.Visible,"scrollbar permanently reserves track");
  }
  var stateTabs=second.Children.OfType<TabControl>().Single();stateTabs.IsEnabled=false;Need(!Find<Button>(second,"art-tab-0").IsEffectivelyEnabled,"visible tabs lock during export");stateTabs.IsEnabled=true;
  second.Stop();sw2.Close();File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"avalonia-interaction-check.txt"),$"PASS: {count} interaction checks: slow names, no-ban repetition/import/role/remake, timer replacement, saved Twitch nickname and image navigation layout. Headless only.");
 }
}

using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Avalonia.Interactivity;
using System.Text;

namespace CasterDesk.Desktop;

internal static class ReplayUiChecks
{
    public static void Run()
    {
        string folder=Path.Combine(AppPaths.DataDirectory,"replay-ui-fixture");Directory.CreateDirectory(folder);
        string lobby=Guid.NewGuid().ToString();
        for(int i=0;i<13;i++) {
            string one=Path.Combine(folder,$"Match-2026-10-19_12-{i:00}-00");Directory.CreateDirectory(one);
            File.WriteAllBytes(Path.Combine(one,"R01.rec"),i==12?Encoding.UTF8.GetBytes("incompatible"):Header(i<2?lobby:Guid.NewGuid().ToString(),i<3?i*2:0,i,i<3?"NA ORG":"EQUIPE "+i,i<3?"FAZE":"ADVERSÁRIO "+i));
        }
        var match=new SiegeMatch{TeamA="Equipe Azul",TeamB="Equipe Laranja"};var map=new MapRecord{MapId="bank"};
        var window=new ReplayImportWindow(match,map,folder);window.Show();
        var elapsed=System.Diagnostics.Stopwatch.StartNew();
        while(!window.Listing.IsCompleted){if(elapsed.Elapsed>TimeSpan.FromSeconds(20))throw new Exception("Replay preview did not finish.");Dispatcher.UIThread.RunJobs();Thread.Sleep(5);}
        window.Listing.GetAwaiter().GetResult();Dispatcher.UIThread.RunJobs();
        ReplayCard[] Cards()=>window.GetVisualDescendants().OfType<ReplayCard>().ToArray();
        if(Cards().Length!=12)throw new Exception("Replay page must show at most twelve cards.");
        var linked=Cards().Where(c=>c.Group>0).ToArray();if(linked.Length!=3)throw new Exception("Related replay fragments not grouped together.");
        linked[0].IsChecked=true;
        window.GetVisualDescendants().OfType<Button>().Single(b=>b.Name=="replay-select-related").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if(linked.Any(c=>c.IsChecked!=true))throw new Exception("Related card selection failed.");
        linked[1].IsChecked=linked[2].IsChecked=false;
        foreach(var size in new[]{new Size(800,600),new Size(1060,620)}){
            window.Width=size.Width;window.Height=size.Height;window.Measure(size);window.Arrange(new Rect(size));Dispatcher.UIThread.RunJobs();
            if(Cards().Any(c=>Math.Abs(c.Bounds.Height-68)>1||c.Bounds.Width<200))throw new Exception("Replay cards lost compact dimensions.");
            using var image=new RenderTargetBitmap(new PixelSize((int)size.Width,(int)size.Height),new Vector(96,96));image.Render(window);image.Save(Path.Combine(AppPaths.DataDirectory,$"replay-cards-{size.Width}.png"));
        }
        var next=window.GetVisualDescendants().OfType<Button>().Single(b=>b.Content?.ToString()=="Próxima");next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs();
        if(Cards().Length!=1||Cards()[0].Header!=null)throw new Exception("Older/unidentified replay must remain accessible on next page.");
        window.GetVisualDescendants().OfType<Button>().Single(b=>b.Content?.ToString()=="Anterior").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs();
        if(Cards().Count(c=>c.IsChecked==true)!=1)throw new Exception("Page change discarded replay selection.");window.Close();
        var stats=new StatsWindow(match,map);stats.Show();stats.Measure(new Size(1280,780));stats.Arrange(new Rect(0,0,1280,780));Dispatcher.UIThread.RunJobs();
        var button=stats.GetVisualDescendants().OfType<Button>().FirstOrDefault(b=>b.Name=="stats-import-replay");if(button==null||button.Bounds.Width<=0||!button.IsEnabled)throw new Exception("Replay import action unavailable.");stats.Close();
    }
    static byte[] Header(string lobby,int round,int minute,string a,string b)
    {
        using var output=new MemoryStream();output.Write(Encoding.ASCII.GetBytes("dissect"));output.Write(new byte[7]);output.WriteByte(1);output.Write(new byte[7]);
        string[][] values=[["version","Y11S3"],["code","99999999"],["datetime",$"2026-10-19-12-{minute:00}-00"],["matchtype","1"],["worldid","355496559878"],["recordingplayerid","1"],["gamemodeid","1"],["roundspermatch","12"],["roundspermatchovertime","3"],["roundnumber",round.ToString()],["overtimeroundnumber","0"],["teamname0",a],["teamname1",b],["id",lobby],["startingteamscore0",(round/2).ToString()],["startingteamscore1",(round-round/2).ToString()],["teamscore0",(round/2).ToString()],["teamscore1",(round-round/2).ToString()]];
        foreach(var pair in values)foreach(string text in pair){byte[] bytes=Encoding.UTF8.GetBytes(text);output.WriteByte((byte)bytes.Length);output.Write(new byte[7]);output.Write(bytes);}output.WriteByte(0);return output.ToArray();
    }
}

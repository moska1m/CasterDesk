using System.Text.Json;
namespace CronoLive;
public static class StatisticsOcrTests
{
    public static void Run(string[] files)
    {
        int tested=0;
        foreach(string file in files)
        {
            if(!File.Exists(file))throw new FileNotFoundException(file);
            var doc=StatisticsData.ReadImage(file,CancellationToken.None).GetAwaiter().GetResult();var players=StatisticsData.Parse(doc);
            string name=Path.GetFileNameWithoutExtension(file);
            File.WriteAllText(Path.Combine(AppPaths.DataDirectory,name+"-ocr.json"),JsonSerializer.Serialize(doc,MatchStore.Options));
            File.WriteAllText(Path.Combine(AppPaths.DataDirectory,name+"-players.json"),JsonSerializer.Serialize(players,MatchStore.Options));
            if(players.Count!=10)throw new Exception("Did not extract 10 players: "+name);
            if(name.Contains("2bc6678f"))
            {
                int[][] expected=[[4,9,3],[3,8,2],[6,7,0],[5,7,0],[3,8,2],[15,4,3],[10,4,2],[8,4,2],[2,5,5],[4,4,2]];
                for(int i=0;i<10;i++)if(players[i].Kills!=expected[i][0]||players[i].Deaths!=expected[i][1]||players[i].Assists!=expected[i][2])throw new Exception("Sample KDA mismatch row "+i);
            }
            tested++;
        }
        var catalog=SiegeCatalog.Load();var match=new SiegeMatch{TeamA="Black Dragons",TeamB="Liquid"};var map=new MapRecord{MapId="clubhouse"};
        using var dialog=new StatisticsDialog(match,map);dialog.Show();Application.DoEvents();dialog.LoadSample(files[0]);using var bmp=new Bitmap(dialog.Width,dialog.Height);dialog.DrawToBitmap(bmp,new(Point.Empty,dialog.Size));bmp.Save(Path.Combine(AppPaths.DataDirectory,"ocr-review.png"));dialog.Close();
        File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"ocr-test.txt"),tested+" prints reais lidos. KDA do print Consulado conferido contra os valores de referência; nomes ainda exigem revisão humana.");
    }
}

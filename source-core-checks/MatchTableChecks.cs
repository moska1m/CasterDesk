using System.IO.Compression;
using System.Xml.Linq;
using CasterDesk.Core;

static class MatchTableChecks
{
    static readonly XNamespace S="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    static void Need(bool value,string message) { if(!value)throw new Exception("Match table: "+message); }
    static XDocument Sheet(byte[] bytes) { using var stream=new MemoryStream(bytes);using var zip=new ZipArchive(stream);using var xml=zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();return XDocument.Load(xml); }
    static string Cell(XDocument doc,string address)=>doc.Descendants(S+"c").Single(c=>(string?)c.Attribute("r")==address).ToString();
    public static void Run()
    {
        var catalog=SiegeCatalog.Load();var match=new SiegeMatch {Format="MD3",DirectMaps=true};
        var first=new MapRecord {MapId=catalog.Maps[0].Id,InitialAttack=0,OtAttack=1};
        first.Rounds.AddRange(new[]{new RoundRecord{Winner=0,Ending="elimination"},new RoundRecord{Winner=1,Ending="defuser_disabled"},new RoundRecord{Winner=0,Ending="defuser_completed"},new RoundRecord{Winner=1,Ending="time"},new RoundRecord{Winner=0}});
        match.Maps.AddRange(new[]{first,new MapRecord{MapId=catalog.Maps[1].Id},new MapRecord{MapId=catalog.Maps[2].Id}});
        var inputs=MatchTable.Inputs(match,catalog);
        Need((string)inputs["A2"]=="Equipe Azul" && (string)inputs["B2"]=="Equipe Laranja","default names");
        Need((string)inputs["B4"]=="Time1" && (string)inputs["B5"]=="Time2","side tokens");
        Need((string)inputs["C9"]=="Desativador" && (string)inputs["C10"]=="Desativador","both defuser endings");
        Need((string)inputs["C11"]=="Tempo" && (string)inputs["C12"]=="","time and optional SPEC ending");
        Need((string)inputs["G4"]=="AO VIVO" && (string)inputs["H4"]=="NÃO JOGADO","live and upcoming maps");
        byte[] template=File.ReadAllBytes(MatchTable.TemplatePath),rendered=MatchTable.Render(template,inputs);
        var original=Sheet(template);var result=Sheet(rendered);
        foreach(string cell in new[]{"C2","D2","G3","H3","I3","C4","D4","C5","D5"}.Concat(Enumerable.Range(8,15).Select(i=>"D"+i)))
            Need(Cell(original,cell)==Cell(result,cell),"protected cell "+cell);
        Need(result.Descendants(S+"dataValidations").Any(),"dropdown validation preserved");
        Need(original.Root!.Element(S+"cols")!.ToString()==result.Root!.Element(S+"cols")!.ToString(),"column dimensions");
        first.Rounds.Clear();first.Rounds.Add(new(){Winner=1});
        var edited=Sheet(MatchTable.Render(rendered,MatchTable.Inputs(match,catalog)));
        Need(Cell(edited,"B8").Contains("Time2")&&!Cell(edited,"B9").Contains("Time"),"edits clear stale rows");
        match.TeamA="=HYPERLINK(\"https://example.com\")";
        var safe=Sheet(MatchTable.Render(template,MatchTable.Inputs(match,catalog)));
        Need(!safe.Descendants(S+"f").Any()&&Cell(safe,"A2").Contains("inlineStr"),"names cannot become formulas");
        first.Rounds=Enumerable.Range(0,7).Select(_=>new RoundRecord{Winner=0}).ToList();first.ConfirmedAt=DateTimeOffset.Now;
        var next=MatchTable.Inputs(match,catalog);
        Need((int)next["E2"]==1 && (string)next["G8"]=="7-0","confirmed series score and map result");
        Need(MatchTable.CurrentMap(match)==match.Maps[1] && (string)next["B8"]=="","next map selected independently of browser");
        string live=Path.Combine(AppPaths.DataDirectory,"artwork-test","current.xlsx");
        MatchTable.Save(match,catalog,live);
        Need(File.Exists(live)&&File.Exists(MatchTable.ArchivePath(match)),"live and match archives");
        string firstArchive=Path.Combine(Path.GetDirectoryName(MatchTable.ArchivePath(match))!,"mapa-"+first.MapId+".xlsx");
        Need(Cell(Sheet(File.ReadAllBytes(firstArchive)),"B8").Contains("Time1"),"previous map breakdown archived");
        var before=File.ReadAllBytes(live);match.Format="MD5";
        bool rejected=false;try{MatchTable.Save(match,catalog,live);}catch(InvalidOperationException){rejected=true;}
        Need(rejected&&before.SequenceEqual(File.ReadAllBytes(live)),"MD5 does not truncate or replace");match.Format="MD3";
        match.Maps[1].InitialAttack=1;match.Maps[1].Rounds.Add(new(){Winner=1});
        using(var locked=new FileStream(live,FileMode.Open,FileAccess.Read,FileShare.Read)) {
            bool failed=false;try{MatchTable.Save(match,catalog,live);}catch(IOException){failed=true;}
            Need(failed,"locked live file reports error");
            Need(Cell(Sheet(File.ReadAllBytes(MatchTable.ArchivePath(match))),"B8").Contains("Time2"),"archive survives live-file lock");
        }
        MatchTable.Save(match,catalog,live);
        Need(Cell(Sheet(File.ReadAllBytes(live)),"B8").Contains("Time2"),"retry updates live file");
        File.WriteAllBytes(Path.Combine(AppPaths.DataDirectory,"artwork-test","preview.xlsx"),File.ReadAllBytes(live));
        Console.WriteLine("Match table checks passed: mapping, protected cells, edits, archives, MD5 guard, file locks and retry.");
    }
}

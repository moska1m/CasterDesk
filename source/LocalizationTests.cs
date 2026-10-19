using System.Text;
using System.Text.Json;
namespace CasterDesk.Core;
public static class LocalizationTests
{
    public static void Run()
    {
        int checks=0;
        void Check(bool value,string name){if(!value)throw new Exception(name);checks++;}
        var catalog=SiegeCatalog.Load();
        foreach(var pair in L.Entries)
        {
            Check(pair.Value.Length==2 && pair.Value.All(s=>!string.IsNullOrWhiteSpace(s)),"missing translation: "+pair.Key);
            if(pair.Key.Contains('{'))foreach(var translated in pair.Value)Check(CompositeFormat.Parse(translated).MinimumArgumentCount==CompositeFormat.Parse(pair.Key).MinimumArgumentCount,"template: "+pair.Key);
        }
        var m=new SiegeMatch{Created=new DateTime(2026,9,24,18,30,0),TeamA="Liquid",TeamB="FaZe",CoinWinner=0,FirstBan=0,Veto=catalog.Maps.Take(8).Select(x=>x.Id).ToArray()};m.SyncMaps(catalog);m.Maps[0].InitialAttack=0;m.Maps[0].OtAttack=1;
        m.Maps[0].Rounds=[new(){Winner=0,Site=catalog.Maps.Last().Sites[0]}];
        string baseline=JsonSerializer.Serialize(m,MatchStore.Options);string id=m.Id;
        foreach(string language in L.Languages)
        {
            L.Set(language);
            Check(MatchStore.ExportFileName(m).StartsWith("2026-09-24_18-30-00_Liquid_vs_FaZe_"),"readable filename");
            Check(!MatchStore.ExportFileName(m).Contains(id),"ID leaked into exported filename");
            Check(MatchStore.ExportFileName(m).EndsWith(L.FileStatus(false)+".json"),"open status");
            var path=Path.Combine(AppPaths.DataDirectory,language+".json");MatchStore.Atomic(path,m);var loaded=MatchStore.Read(path,catalog);
            Check(loaded.Id==id && JsonSerializer.Serialize(loaded,MatchStore.Options)==baseline,"cross-language roundtrip");
            Check(MatchStore.IsDuplicate([m],loaded,Path.Combine(AppPaths.DataDirectory,"empty")),"duplicate match accepted");
            loaded.Id=loaded.Id.ToUpperInvariant();Check(MatchStore.IsDuplicate([m],loaded,Path.Combine(AppPaths.DataDirectory,"empty")),"case insensitive identity");
            Check(L.F($"Mapa {1:00} · {"Não registrado"}").EndsWith("Não registrado"),"translated user argument");
            foreach(var map in catalog.Maps)Check(map.Sites.Select(L.T).Distinct().Count()==map.Sites.Length,"ambiguous translated bombsites");
            using(var form=new Form{ClientSize=new Size(1240,900)})
            {
                var panel=new ChampionshipPanel(true);form.Controls.Add(panel);form.Show();Application.DoEvents();panel.VerifyRoundEditing();
                Check(true,"native selectors, rounds, locks in "+language);form.Close();
            }
            using(var form=new Form{ClientSize=new Size(1240,900)})
            {
                var panel=new ChampionshipPanel(true);form.Controls.Add(panel);form.Show();Application.DoEvents();
                var tabs=panel.Controls.OfType<TabControl>().Single();
                for(int i=0;i<tabs.TabCount;i++){tabs.SelectedIndex=i;Application.DoEvents();using var bmp=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bmp,new Rectangle(Point.Empty,form.Size));bmp.Save(Path.Combine(AppPaths.DataDirectory,$"locale-{language}-{i}.png"));}
                form.Close();
            }
            var completed=JsonSerializer.Deserialize<SiegeMatch>(baseline)!;completed.Maps[0].Rounds=Enumerable.Range(0,7).Select(_=>new RoundRecord{Winner=0}).ToList();completed.Maps[0].ConfirmedAt=DateTimeOffset.Now;
            Check(MatchStore.ExportFileName(completed).EndsWith(L.FileStatus(true)+".json"),"completed filename");
        }
        L.Set("pt-BR");Check(MatchStore.ExportFileName(m)=="2026-09-24_18-30-00_Liquid_vs_FaZe_aberta.json","Portuguese filename");
        m.TeamA="../A:B*?";m.TeamB="<>|";Check(MatchStore.ExportFileName(m).IndexOfAny(Path.GetInvalidFileNameChars())<0,"unsafe filename");
        // Preference changes must be persistent but not live: same behavior as themes.
        string settings=AppPaths.ConfigFile("settings.json");
        File.WriteAllText(settings,JsonSerializer.Serialize(new Settings{Language="pt-BR",Message="MY CUSTOM TEXT"}));
        using(var form=new MainForm())
        {
            form.Show();var menu=form.Controls.OfType<SettingsMenu>().Single();menu.Show();menu.Languages();
            menu.Controls.OfType<FlowLayoutPanel>().Single().Controls.OfType<Button>().Single(b=>b.Text=="English").PerformClick();
            Check(L.Current=="pt-BR","language changed without restart");
            Check(JsonSerializer.Deserialize<Settings>(File.ReadAllText(settings))!.Language=="en","language not saved");form.Close();
        }
        L.LoadPreference();Check(L.Current=="en","language not loaded after restart");
        using(var form=new MainForm())
        {
            form.Show();Application.DoEvents();var menu=form.Controls.OfType<SettingsMenu>().Single();menu.Show();menu.Rounds();
            menu.Controls.OfType<FlowLayoutPanel>().Single().Controls.OfType<Button>().Single(b=>b.Text.EndsWith(L.T("Modo rápido"))).PerformClick();Application.DoEvents();
            Check(JsonSerializer.Deserialize<Settings>(File.ReadAllText(settings))!.QuickRounds,"quick preference not saved");form.Close();
        }
        using(var form=new MainForm()){form.Show();Application.DoEvents();Check(JsonSerializer.Deserialize<Settings>(File.ReadAllText(settings))!.Message=="MY CUSTOM TEXT","custom timer message changed");using var bmp=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bmp,new Rectangle(Point.Empty,form.Size));bmp.Save(Path.Combine(AppPaths.DataDirectory,"locale-main-en.png"));form.Close();}
        L.Set("unknown");Check(L.Current=="pt-BR","fallback");
        File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"localization-tests.txt"),$"{checks} checks passed: templates, three languages, native editing, JSON identity, filename sanitization, restart preference and preserved custom text.");
    }
}

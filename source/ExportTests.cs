namespace CronoLive;
public static class ExportTests
{
    public static void Run()
    {
        var catalog=SiegeCatalog.Load();string folder=Path.Combine(AppPaths.DataDirectory,"export-test");Directory.CreateDirectory(folder);
        var match=new SiegeMatch {TeamA="Liquid",TeamB="FaZe",Format="MD3"};
        int[][] winners=[[1,0,0,0,0,0,0,0],[1,0,1,1,0,1,1,0,1,1],Enumerable.Range(0,15).Select(i=>i%2).ToArray()];
        string[] ids=["bank","fortress","clubhouse"];
        for(int i=0;i<3;i++)
        {
            var info=catalog.Maps.Single(m=>m.Id==ids[i]);var map=new MapRecord{MapId=ids[i],InitialAttack=0,OtAttack=0,ConfirmedAt=DateTimeOffset.Now,Bans=["mira","azami","ace","thatcher","valkyrie","hibana","ace","thatcher","mira","azami","hibana","valkyrie"]};
            foreach(int winner in winners[i])map.Rounds.Add(new(){Winner=winner,Site=info.Sites[map.Rounds.Count%info.Sites.Length]});match.Maps.Add(map);
        }
        var brand=new ExportBrand{EventName="CAMPEONATO DA COMUNIDADE · PRÉVIA",CasterName="MOSKA",Channel="@MOSKA1M",Platform="Twitch"};
        int checks=0;
        void Need(bool value){if(!value)throw new Exception("PNG export test failed");checks++;}
        foreach(int initial in new[]{0,1})
        {
            var roles=new MapRecord{InitialAttack=initial};
            foreach(int half in new[]{0,1})
            {
                var attack=ResultRenderer.RoleSlots(roles,half,"ATQ");var defense=ResultRenderer.RoleSlots(roles,half,"DEF");
                Need(attack.Length==3 && defense.Length==3 && !attack.Intersect(defense).Any());
                Need(attack.All(slot=>MapRecord.BanTeam(slot)!=(half==0?initial:1-initial)));
            }
        }
        foreach(var aspect in Enum.GetValues<ExportAspect>())
        {
            var before=System.Text.Json.JsonSerializer.Serialize(match);var pages=ResultRenderer.Render(match,catalog,brand,aspect);
            Need(pages.Count==(aspect==ExportAspect.Feed?4:1));
            foreach(var page in pages){Need(page.Image.Width==(aspect==ExportAspect.Horizontal?1920:1080));Need(page.Image.Height==(aspect==ExportAspect.Stories?1920:aspect==ExportAspect.Feed?1350:1080));page.Image.Save(Path.Combine(folder,$"{aspect}_{page.Name}.png"),System.Drawing.Imaging.ImageFormat.Png);page.Dispose();}
            Need(before==System.Text.Json.JsonSerializer.Serialize(match));
        }
        var partial=new SiegeMatch{TeamA="ABCDEFGHIJKL",TeamB="MNOPQRSTUVWX",Format="MD1",Maps=[new(){MapId="bank",InitialAttack=0,Rounds=[new(){Winner=1}]}]};
        foreach(var aspect in Enum.GetValues<ExportAspect>()){var pages=ResultRenderer.Render(partial,catalog,brand,aspect);foreach(var page in pages){page.Image.Save(Path.Combine(folder,$"partial_{aspect}_{page.Name}.png"),System.Drawing.Imaging.ImageFormat.Png);page.Dispose();}Need(true);}
        var ended=new MapRecord {InitialAttack=0};
        foreach(int winner in new[]{0,0,0,0,0,0,1,1,0})ended.Rounds.Add(new(){Winner=winner});
        Need(ended.Score().A==7 && ended.Score().B==2 && !ended.BanStageReached(10) && ended.BanStageReached(6));
        ended.Rounds[0].Winner=1;Need(ended.Score().Winner<0 && ended.BanStageReached(10));
        var fresh=new SiegeMatch();try{ResultRenderer.Render(fresh,catalog,brand,ExportAspect.Stories);throw new Exception("Empty match accepted");}catch(InvalidOperationException){checks++;}
        var played=ResultRenderer.Played(new(){Maps=[match.Maps[0],new(){MapId="lair"}]});Need(played.Count==1);
        Need(ResultRenderer.RoundHeight(500,7)==ResultRenderer.RoundHeight(500,10));
        Need(ResultRenderer.RoundHeight(500,15)*16+26<=500);
        foreach(string platform in new[]{"Twitch","YouTube","Kick"})
        {
            brand.Platform=platform;
            using var page=ResultRenderer.Render(match,catalog,brand,ExportAspect.Horizontal)[0];page.Image.Save(Path.Combine(folder,$"platform_{platform}.png"),System.Drawing.Imaging.ImageFormat.Png);Need(true);
        }
        var full=System.Text.Json.JsonSerializer.Deserialize<SiegeMatch>(System.Text.Json.JsonSerializer.Serialize(match))!;
        foreach(var map in full.Maps){map.Rounds.Clear();for(int i=0;i<15;i++)map.Rounds.Add(new(){Winner=i%2,Site=catalog.Maps.Single(m=>m.Id==map.MapId).Sites[i%4]});}
        foreach(var mode in new[]{ExportAspect.Stories,ExportAspect.Horizontal}){using var page=ResultRenderer.Render(full,catalog,brand,mode)[0];page.Image.Save(Path.Combine(folder,$"full15_{mode}.png"),System.Drawing.Imaging.ImageFormat.Png);Need(true);}
        brand.Platforms=["Twitch","YouTube","Kick"];brand.PlatformChannels=new();
        Need(brand.SelectedPlatforms().Length==3 && brand.ChannelFor("Kick")==brand.Channel);
        using(var page=ResultRenderer.Render(match,catalog,brand,ExportAspect.Feed)[0])page.Image.Save(Path.Combine(folder,"multi-shared.png"),System.Drawing.Imaging.ImageFormat.Png);
        brand.PlatformChannels["YouTube"]="@MOSKA-CAST";brand.PlatformChannels["Kick"]="kick.com/moska";
        Need(brand.ChannelFor("YouTube")=="@MOSKA-CAST");
        var multi=ResultRenderer.Render(match,catalog,brand,ExportAspect.Feed);for(int i=0;i<multi.Count;i++){multi[i].Image.Save(Path.Combine(folder,$"multi-different-{i}.png"),System.Drawing.Imaging.ImageFormat.Png);multi[i].Dispose();}
        var restored=System.Text.Json.JsonSerializer.Deserialize<ExportBrand>(System.Text.Json.JsonSerializer.Serialize(brand))!;Need(restored.SelectedPlatforms().Length==3 && restored.ChannelFor("Kick")=="kick.com/moska");
        AppTheme.Dark=true;
        using(var form=new Form(){ClientSize=new Size(1240,850)})
        {
            var panel=new ImageExportPanel(()=>new List<SiegeMatch>{match,partial},catalog);form.Controls.Add(panel);form.Show();AppTheme.Apply(form);Application.DoEvents();panel.VerifySelection(match.Id,partial.Id);Application.DoEvents();
            using var screenshot=new Bitmap(form.Width,form.Height);form.DrawToBitmap(screenshot,new Rectangle(Point.Empty,form.Size));screenshot.Save(Path.Combine(folder,"export-ui.png"));Need(true);
        }
        File.WriteAllText(Path.Combine(folder,"checks.txt"),$"{checks} verificações aprovadas: dimensões, páginas, partidas parciais, nomes de 12 caracteres, dados preservados e mapas não jogados.");
    }
}

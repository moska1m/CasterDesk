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
        foreach(var aspect in Enum.GetValues<ExportAspect>())
        {
            int width=aspect==ExportAspect.Horizontal?1920:1080;
            var header=ResultRenderer.HeaderBounds(width,aspect==ExportAspect.Stories?100:30);
            Need(header.Center.Left+header.Center.Width/2==width/2f);
            Need(header.Left.Left==width-header.Right.Right && header.Left.Width==header.Right.Width);
            Need(header.Left.Right<header.Center.Left && header.Center.Right<header.Right.Left);
        }
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
        brand.BackgroundStyle="JG";
        foreach(var aspect in Enum.GetValues<ExportAspect>())
        {
            var themed=ResultRenderer.Render(match,catalog,brand,aspect);
            var classicBrand=new ExportBrand();
            var classic=ResultRenderer.Render(match,catalog,classicBrand,aspect);
            Need(themed.Count==classic.Count);
            Need(themed[0].Image.GetPixel(5,5)!=classic[0].Image.GetPixel(5,5));
            foreach(var page in themed){page.Image.Save(Path.Combine(folder,$"JG_{aspect}_{page.Name}.png"),System.Drawing.Imaging.ImageFormat.Png);page.Dispose();}
            foreach(var page in classic)page.Dispose();
        }
        brand.BackgroundStyle="Classic";
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
        var md5=new SiegeMatch{Format="MD5",TeamA="Liquid",TeamB="FaZe"};
        foreach(var info in catalog.Maps.Take(5))
        {
            int number=md5.Maps.Count;
            md5.Maps.Add(new MapRecord{MapId=info.Id,InitialAttack=0,OtAttack=1,ConfirmedAt=DateTimeOffset.Now,
                Bans=full.Maps[0].Bans.ToArray(),Rounds=Enumerable.Range(0,15).Select(i=>new RoundRecord{Winner=(i+number)%2,Site=info.Sites[i%info.Sites.Length]}).ToList()});
        }
        foreach(var mode in Enum.GetValues<ExportAspect>())
        {
            var pages=ResultRenderer.Render(md5,catalog,brand,mode);
            Need(pages.Count==(mode==ExportAspect.Horizontal?2:6));
            for(int i=0;i<pages.Count;i++){pages[i].Image.Save(Path.Combine(folder,$"MD5_{mode}_{i:00}.png"));pages[i].Dispose();}
            var single=ResultRenderer.Render(md5,catalog,brand,mode,md5.Maps[4]);Need(single.Count==1);foreach(var page in single)page.Dispose();
        }
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

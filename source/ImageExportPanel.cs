using System.Drawing.Imaging;
using System.Text.Json;

namespace CasterDesk.Core;
public sealed class ImageExportPanel : UserControl
{
    readonly Func<IReadOnlyList<SiegeMatch>> list;readonly SiegeCatalog catalog;
    readonly ComboBox matches=new ThemeCombo(){DropDownStyle=ComboBoxStyle.DropDownList,Width=650},aspect=new ThemeCombo(){DropDownStyle=ComboBoxStyle.DropDownList,Width=330},scope=new ThemeCombo(){DropDownStyle=ComboBoxStyle.DropDownList,Width=330},pages=new ThemeCombo(){DropDownStyle=ComboBoxStyle.DropDownList,Width=240};
    readonly TextBox casterName=new(){Width=330,MaxLength=40,PlaceholderText=L.T("Nome / nick do caster")},channel=new(){Width=330,MaxLength=80,PlaceholderText=L.T("@usuario ou endereço do canal")};
    readonly CheckBox[] platforms=[new(){Text="Twitch",Width=80},new(){Text="YouTube",Width=80},new(){Text="Kick",Width=80}];
    readonly TextBox[] platformChannels=[new(){Width=220,MaxLength=80},new(){Width=220,MaxLength=80},new(){Width=220,MaxLength=80}];
    readonly ComboBox backgroundStyle=new ThemeCombo(){DropDownStyle=ComboBoxStyle.DropDownList,Width=330};
    readonly TextBox eventName=new(){Width=330,MaxLength=80};
    readonly PictureBox preview=new(){Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(19,25,31)},logo=new(){Width=80,Height=60,SizeMode=PictureBoxSizeMode.Zoom};
    readonly Label status=new(){AutoSize=true,MaximumSize=new Size(330,0)},description=new(){AutoSize=true,MaximumSize=new Size(330,0)};
    readonly Button save=new ThemeButton(){Text=L.T("Salvar PNGs"),Width=150,Height=30,Enabled=false},copy=new ThemeButton(){Text=L.T("Copiar imagem"),Width=150,Height=30,Enabled=false};
    readonly Dictionary<string,ExportBrand> brands;
    readonly string settingsFile=AppPaths.ConfigFile("export-settings.json");
    readonly System.Windows.Forms.Timer delay=new(){Interval=450};
    List<ResultPage> rendered=[];bool loading;string? renderKey,selectionKey,pendingKey;
    int generation;CancellationTokenSource? renderCancellation;
    static readonly SemaphoreSlim renderGate=new(1,1);
    internal bool Rendering {get;private set;}
    SiegeMatch? Selected=>matches.SelectedItem as SiegeMatch;
    ExportBrand Brand {get{if(Selected is null)return new();if(!brands.TryGetValue(Selected.Id,out var b))brands[Selected.Id]=b=new();return b;}}
    public ImageExportPanel(Func<IReadOnlyList<SiegeMatch>> list,SiegeCatalog catalog)
    {
        this.list=list;this.catalog=catalog;Dock=DockStyle.Fill;
        try{brands=JsonSerializer.Deserialize<Dictionary<string,ExportBrand>>(File.ReadAllText(settingsFile))??new();}catch{brands=new();}
        var layout=new TableLayoutPanel(){Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,Padding=new Padding(12)};
        layout.ColumnStyles.Add(new(SizeType.Absolute,360));layout.ColumnStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,42));layout.RowStyles.Add(new(SizeType.Percent,100));Controls.Add(layout);
        var top=new FlowLayoutPanel(){Dock=DockStyle.Fill};top.Controls.Add(new Label(){Text=L.T("Partida para exportar:"),AutoSize=true,Margin=new Padding(3,7,8,0)});top.Controls.Add(matches);layout.Controls.Add(top,0,0);layout.SetColumnSpan(top,2);
        var options=new FlowLayoutPanel(){Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};layout.Controls.Add(options,0,1);
        void Label(string s)=>options.Controls.Add(new Label(){Text=s,AutoSize=true,Font=AppTheme.HeadingFont,Margin=new Padding(3,12,3,5)});
        Label(L.T("FORMATO DA IMAGEM"));aspect.Items.AddRange(["Stories · 9:16 · 1080 × 1920","Feed · 4:5 · 1080 × 1350",L.T("Horizontal · 16:9 · 1920 × 1080")]);aspect.SelectedIndex=0;options.Controls.Add(aspect);options.Controls.Add(description);
        Label(L.T("ESTILO DO FUNDO"));backgroundStyle.Items.AddRange([L.T("Clássico"),L.T("Comunidade")]);backgroundStyle.SelectedIndex=0;options.Controls.Add(backgroundStyle);
        Label(L.T("CONTEÚDO"));options.Controls.Add(scope);
        Label(L.T("CAMPEONATO (OPCIONAL)"));options.Controls.Add(eventName);options.Controls.Add(logo);
        var logos=new FlowLayoutPanel(){Width=335,Height=36};var choose=new ThemeButton(){Text=L.T("Selecionar logo"),Width=160};var remove=new ThemeButton(){Text=L.T("Remover logo"),Width=150};logos.Controls.AddRange([choose,remove]);options.Controls.Add(logos);
        Label(L.T("TRANSMISSÃO (OPCIONAL)"));options.Controls.Add(new Label(){Text=L.T("Nome / nick do caster"),AutoSize=true});options.Controls.Add(casterName);options.Controls.Add(new Label(){Text=L.T("@usuário ou endereço do canal"),AutoSize=true});options.Controls.Add(channel);
        options.Controls.Add(new Label(){Text=L.T("Marque as plataformas. Canal vazio usa o @ principal."),AutoSize=true,MaximumSize=new Size(330,0)});
        for(int i=0;i<platforms.Length;i++){var row=new FlowLayoutPanel(){Width=335,Height=30};row.Controls.Add(platforms[i]);row.Controls.Add(platformChannels[i]);options.Controls.Add(row);}
        Label(L.T("PRÉVIA"));options.Controls.Add(pages);var refresh=new ThemeButton(){Text=L.T("Atualizar prévia"),Width=180,Height=30};options.Controls.Add(refresh);
        var actions=new FlowLayoutPanel(){Width=335,Height=40};actions.Controls.AddRange([save,copy]);options.Controls.Add(actions);options.Controls.Add(status);
        layout.Controls.Add(preview,1,1);
        matches.SelectedIndexChanged+=(_,_)=>{if(!loading)LoadSelection();};aspect.SelectedIndexChanged+=(_,_)=>Generate();scope.SelectedIndexChanged+=(_,_)=>{if(!loading)Generate();};pages.SelectedIndexChanged+=(_,_)=>{preview.Image=rendered.ElementAtOrDefault(pages.SelectedIndex)?.Image;};
        backgroundStyle.SelectedIndexChanged+=(_,_)=>{if(loading||Selected is null)return;Brand.BackgroundStyle=backgroundStyle.SelectedIndex==1?"Community":"Classic";SaveBrands();Generate();};
        void CasterChanged(){if(loading||Selected is null)return;Brand.CasterName=casterName.Text;Brand.Channel=channel.Text;Brand.Platforms=platforms.Where(p=>p.Checked).Select(p=>p.Text).ToList();for(int i=0;i<3;i++)Brand.PlatformChannels[platforms[i].Text]=platformChannels[i].Text;ClearPreview();delay.Stop();delay.Start();}
        casterName.TextChanged+=(_,_)=>CasterChanged();channel.TextChanged+=(_,_)=>CasterChanged();foreach(var check in platforms)check.CheckedChanged+=(_,_)=>CasterChanged();foreach(var field in platformChannels)field.TextChanged+=(_,_)=>CasterChanged();
        eventName.TextChanged+=(_,_)=>{if(loading||Selected is null)return;Brand.EventName=eventName.Text;ClearPreview();delay.Stop();delay.Start();};delay.Tick+=(_,_)=>{delay.Stop();SaveBrands();Generate();};
        choose.Click+=(_,_)=>ChooseLogo();remove.Click+=(_,_)=>{Brand.LogoFile="";ShowLogo();SaveBrands();Generate();};refresh.Click+=(_,_)=>{renderKey=null;Generate();};save.Click+=(_,_)=>SaveImages();copy.Click+=(_,_)=>{try{if(preview.Image is not null){Clipboard.SetImage(preview.Image);status.Text=L.T("Imagem copiada.");}}catch(Exception ex){status.Text=L.T("Não foi possível copiar: ")+ex.Message;}};
    }
    public void RefreshMatches(string? preferred=null)
    {
        string? selected=Selected?.Id??preferred;
        var entries=list();
        string key=JsonSerializer.Serialize(entries);
        if(key==selectionKey && Selected is not null)return;
        selectionKey=key;loading=true;matches.BeginUpdate();
        try{matches.Items.Clear();matches.Items.AddRange(entries.Cast<object>().ToArray());matches.SelectedItem=matches.Items.Cast<SiegeMatch>().FirstOrDefault(m=>m.Id==selected);if(matches.SelectedIndex<0&&matches.Items.Count>0)matches.SelectedIndex=0;}
        finally{matches.EndUpdate();loading=false;}
        LoadSelection();
    }
    void LoadSelection()
    {
        delay.Stop();loading=true;backgroundStyle.SelectedIndex=Brand.BackgroundStyle=="Community"?1:0;eventName.Text=Brand.EventName;casterName.Text=Brand.CasterName;channel.Text=Brand.Channel;for(int i=0;i<3;i++){platforms[i].Checked=Brand.SelectedPlatforms().Contains(platforms[i].Text);platformChannels[i].Text=Brand.PlatformChannels.GetValueOrDefault(platforms[i].Text,"");}scope.Items.Clear();scope.Items.Add(L.T("Série · mapas com rounds registrados"));if(Selected is not null)foreach(var map in Selected.Maps)scope.Items.Add(L.F($"Mapa {Selected.Maps.IndexOf(map)+1:00} · {catalog.Maps.Single(m=>m.Id==map.MapId).DisplayName}"));scope.SelectedIndex=0;ShowLogo();loading=false;Generate();
    }
    void ShowLogo(){var old=logo.Image;logo.Image=null;old?.Dispose();try{if(File.Exists(Brand.LogoFile)){using var img=Image.FromFile(Brand.LogoFile);logo.Image=new Bitmap(img);}}catch{status.Text=L.T("Logo indisponível. Selecione a imagem novamente.");}}
    void ChooseLogo()
    {
        if(Selected is null)return;using var dialog=new OpenFileDialog(){Filter=L.T("Imagem PNG ou JPEG|*.png;*.jpg;*.jpeg")};if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        try
        {
            if(new FileInfo(dialog.FileName).Length>20_000_000)throw new InvalidDataException(L.T("Use uma imagem com até 20 MB."));
            using var source=Image.FromFile(dialog.FileName);if((long)source.Width*source.Height>40_000_000)throw new InvalidDataException(L.T("A imagem é muito grande. Use uma logo menor."));
            float scale=Math.Min(1,768f/Math.Max(source.Width,source.Height));using var normalized=new Bitmap(Math.Max(1,(int)(source.Width*scale)),Math.Max(1,(int)(source.Height*scale)));using(var g=Graphics.FromImage(normalized)){g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.DrawImage(source,0,0,normalized.Width,normalized.Height);}
            string folder=Path.Combine(AppPaths.DataDirectory,"Exportacao","Logos");Directory.CreateDirectory(folder);string path=Path.Combine(folder,Selected.Id+".png");normalized.Save(path,ImageFormat.Png);Brand.LogoFile=path;SaveBrands();ShowLogo();Generate();
        }
        catch(Exception ex){status.Text=L.T("Não foi possível usar esta logo: ")+ex.Message;}
    }
    public void FlushPreferences(){delay.Stop();SaveBrands();}
    void SaveBrands(){try{string temp=settingsFile+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(brands,MatchStore.Options));File.Move(temp,settingsFile,true);}catch(Exception ex){status.Text=L.T("Falha ao salvar preferências: ")+ex.Message;}}
    void ClearPreview(){generation++;renderCancellation?.Cancel();pendingKey=null;Rendering=false;renderKey=null;preview.Image=null;foreach(var page in rendered)page.Dispose();rendered.Clear();pages.Items.Clear();save.Enabled=copy.Enabled=false;}
    async void Generate()
    {
        if(loading)return;if(Selected is null){ClearPreview();return;}
        string key=JsonSerializer.Serialize(new{Selected,Brand,Aspect=aspect.SelectedIndex,Scope=scope.SelectedIndex});
        if(key==renderKey && rendered.Count>0 || key==pendingKey)return;
        ClearPreview();int request=generation;
        renderCancellation?.Dispose();renderCancellation=new CancellationTokenSource();var token=renderCancellation.Token;
        pendingKey=key;Rendering=true;status.Text=L.T("Preparando prévia… Você pode continuar navegando.");
        try
        {
            var mode=(ExportAspect)Math.Max(0,aspect.SelectedIndex);description.Text=mode switch{ExportAspect.Stories=>L.T("Até três mapas: resumo vertical. Quatro ou cinco: capa e detalhes por mapa."),ExportAspect.Feed=>L.T("Carrossel: uma capa e uma imagem detalhada por mapa."),_=>L.T("Até três mapas por imagem, lado a lado.")};
            var chosen=scope.SelectedIndex>0?Selected.Maps.ElementAtOrDefault(scope.SelectedIndex-1):null;
            if(chosen is not null && chosen.Rounds.Count==0)throw new InvalidOperationException(L.T("Este mapa ainda não tem rounds registrados."));
            var snapshot=JsonSerializer.Deserialize<SiegeMatch>(JsonSerializer.Serialize(Selected,MatchStore.Options),MatchStore.Options)!;
            var snapshotMap=chosen is null?null:snapshot.Maps.Single(m=>m.MapId==chosen.MapId);
            var brandSnapshot=JsonSerializer.Deserialize<ExportBrand>(JsonSerializer.Serialize(Brand))!;
            await renderGate.WaitAsync(token);
            List<ResultPage> result;
            try{token.ThrowIfCancellationRequested();result=await Task.Run(()=>ResultRenderer.Render(snapshot,catalog,brandSnapshot,mode,snapshotMap));}
            finally{renderGate.Release();}
            if(IsDisposed || Disposing || request!=generation){foreach(var page in result)page.Dispose();return;}
            rendered=result;renderKey=key;pages.Items.AddRange(rendered.Select(p=>(object)p.Name.Replace('_',' ').Replace("mapa",L.T("Mapa")).Replace("serie",L.T("Série")).Replace("capa",L.T("Capa"))).ToArray());pages.SelectedIndex=0;save.Enabled=copy.Enabled=true;status.Text=L.F($"{rendered.Count} imagem(ns) pronta(s). Geração local, sem internet.");
        }
        catch(OperationCanceledException){}
        catch(Exception ex){if(!IsDisposed && request==generation)status.Text=ex.Message;}
        finally{if(request==generation){pendingKey=null;Rendering=false;}}
    }
    void SaveImages()
    {
        if(rendered.Count==0||Selected is null)return;
        using var dialog=new FolderBrowserDialog(){SelectedPath=AppPaths.ImageDirectory,Description=L.T("Escolha onde salvar as imagens")};if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        string Safe(string s)=>string.Concat(s.Select(c=>Path.GetInvalidFileNameChars().Contains(c)?'_':c));
        string directory=Path.Combine(dialog.SelectedPath,$"{Selected.Created:yyyy-MM-dd}_{Safe(Selected.TeamA)}_vs_{Safe(Selected.TeamB)}_{DateTime.Now:HHmmss}_{Guid.NewGuid().ToString("N")[..6]}");
        try{Directory.CreateDirectory(directory);foreach(var p in rendered)p.Image.Save(Path.Combine(directory,p.Name+".png"),ImageFormat.Png);status.Text=L.F($"{rendered.Count} PNG(s) salvo(s) em:\n{directory}");}catch(Exception ex){status.Text=L.T("Falha na exportação: ")+ex.Message;}
    }
    internal void VerifySelection(string firstId,string secondId)
    {
        RefreshMatches(firstId);WaitForPreview();if(Selected?.Id!=firstId)throw new Exception("Export default match");
        matches.SelectedItem=matches.Items.Cast<SiegeMatch>().Single(m=>m.Id==secondId);WaitForPreview();
        var previousImage=preview.Image;RefreshMatches(firstId);if(!ReferenceEquals(previousImage,preview.Image))throw new Exception("Unchanged preview regenerated");if(Selected?.Id!=secondId)throw new Exception("Export selection not preserved");
        matches.SelectedItem=matches.Items.Cast<SiegeMatch>().Single(m=>m.Id==firstId);
        aspect.SelectedIndex=1;WaitForPreview();if(rendered.Count!=4)throw new Exception("Feed page count in UI");
        pages.SelectedIndex=3;if(preview.Image!=rendered[3].Image)throw new Exception("Page navigation");
        aspect.SelectedIndex=0;WaitForPreview();if(rendered.Count!=1)throw new Exception("Stories count in UI");
        aspect.SelectedIndex=1;aspect.SelectedIndex=2;aspect.SelectedIndex=0;WaitForPreview();
        if(rendered.Count!=1 || rendered[0].Image.Size!=new Size(1080,1920))throw new Exception("Stale asynchronous preview published");
        scope.SelectedIndex=1;WaitForPreview();var same=preview.Image;RefreshMatches(firstId);
        if(scope.SelectedIndex!=1 || !ReferenceEquals(same,preview.Image))throw new Exception("Tab return lost preview or selection");

    }
    internal void WaitForPreview()
    {
        var timeout=System.Diagnostics.Stopwatch.StartNew();
        while(Rendering && timeout.ElapsedMilliseconds<15000){Application.DoEvents();Thread.Sleep(10);}
        if(Rendering)throw new Exception("Preview generation timeout");
    }
    protected override void Dispose(bool disposing){if(disposing){delay.Stop();delay.Dispose();SaveBrands();ClearPreview();renderCancellation?.Dispose();logo.Image?.Dispose();}base.Dispose(disposing);}
}

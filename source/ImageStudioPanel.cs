using System.Diagnostics;
using System.Text.Json;

namespace CronoLive;
public sealed class ImageStudioPanel : UserControl
{
    readonly Func<IReadOnlyList<SiegeMatch>> list;readonly SiegeCatalog catalog;
    readonly ComboBox matches=new ThemeCombo{DropDownStyle=ComboBoxStyle.DropDownList},kind=new ThemeCombo{DropDownStyle=ComboBoxStyle.DropDownList,Width=290},aspect=new ThemeCombo{DropDownStyle=ComboBoxStyle.DropDownList,Width=290},maps=new ThemeCombo{DropDownStyle=ComboBoxStyle.DropDownList,Width=290},previewMode=new ThemeCombo{DropDownStyle=ComboBoxStyle.DropDownList,Width=290},mvp=new ThemeCombo{DropDownStyle=ComboBoxStyle.DropDownList,Width=290};
    readonly TextBox eventName=new(){Width=290,MaxLength=80};readonly DateTimePicker scheduled=new(){Width=290,Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy  HH:mm",ShowCheckBox=true};
    readonly PictureBox preview=new(){Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom};
    readonly Label status=new(){AutoSize=true,MaximumSize=new(290,0)},backgroundInfo=new(){AutoSize=true,MaximumSize=new(290,0)},statsInfo=new(){AutoSize=true,MaximumSize=new(290,0)};
    readonly Button generate=new ThemeButton{Text=L.T("Gerar arquivos"),Width=290,Height=42,Enabled=false,PrimaryAction=true};
    readonly FlowLayoutPanel options=new(){Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};
    readonly FlowLayoutPanel actions=new(){Dock=DockStyle.Bottom,FlowDirection=FlowDirection.TopDown,WrapContents=false,Height=170};
    readonly System.Windows.Forms.Timer delay=new(){Interval=400};
    readonly Label dateLabel=new(){Text=L.T("DATA E HORÁRIO"),AutoSize=true},mapLabel=new(){Text=L.T("MAPA"),AutoSize=true},mvpLabel=new(){Text="MVP",AutoSize=true};
    readonly Button statsEdit=new ThemeButton{Text=L.T("Conferir estatísticas / colar print"),Width=290,Height=34};
    readonly Button obsCopy=new ThemeButton{Text=L.T("Copiar caminho para OBS"),Width=290,Height=32};
    ImageBrand brand=new();SiegeMatch? brandMatch;List<ResultPage> rendered=[];bool loading,working,cleaned;int generation;string? selectionKey;
    public SiegeMatch? Selected=>matches.SelectedItem as SiegeMatch;
    ArtKind Kind=>(ArtKind)Math.Max(0,kind.SelectedIndex);
    ExportAspect Aspect=>(ExportAspect)Math.Max(0,aspect.SelectedIndex);
    MapRecord? Map=>Selected?.Maps.ElementAtOrDefault(maps.SelectedIndex);
    public ImageStudioPanel(Func<IReadOnlyList<SiegeMatch>> list,SiegeCatalog catalog)
    {
        this.list=list;this.catalog=catalog;Dock=DockStyle.Fill;Font=AppFonts.Body(10);
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,Padding=new(12)};layout.ColumnStyles.Add(new(SizeType.Absolute,320));layout.ColumnStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,44));layout.RowStyles.Add(new(SizeType.Percent,100));Controls.Add(layout);
        var top=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3};top.ColumnStyles.Add(new(SizeType.Absolute,80));top.ColumnStyles.Add(new(SizeType.Absolute,520));top.ColumnStyles.Add(new(SizeType.Percent,100));top.Controls.Add(new Label{Text=L.T("Partida:"),AutoSize=true,Margin=new(3,8,3,0)},0,0);matches.Dock=DockStyle.Fill;top.Controls.Add(matches,1,0);layout.Controls.Add(top,0,0);layout.SetColumnSpan(top,2);
        var sidebar=new Panel{Dock=DockStyle.Fill};sidebar.Controls.Add(options);sidebar.Controls.Add(actions);layout.Controls.Add(sidebar,0,1);
        var imageHost=new Panel{Dock=DockStyle.Fill,Padding=new(8),BackColor=Color.FromArgb(18,24,32)};imageHost.Controls.Add(preview);layout.Controls.Add(imageHost,1,1);
        void Label(string text)=>options.Controls.Add(new Label{Text=L.T(text),AutoSize=true,Font=AppTheme.HeadingFont,Margin=new(3,14,3,5)});
        Button Button(string text,Action action){var b=new ThemeButton{Text=L.T(text),Width=290,Height=32};b.Click+=(_,_)=>action();options.Controls.Add(b);return b;}
        Label("TIPO DE ARTE");kind.Items.AddRange([L.T("Anúncio da partida"),L.T("Map veto · OBS e arquivo"),L.T("Resultado da série"),L.T("Estatísticas por mapa"),L.T("Estatísticas gerais da série")]);kind.SelectedIndex=0;options.Controls.Add(kind);
        Label("FORMATO DA IMAGEM");aspect.Items.AddRange(["Stories · 1080 × 1920","Feed · 1080 × 1350",L.T("Horizontal · 1920 × 1080")]);aspect.SelectedIndex=1;options.Controls.Add(aspect);
        options.Controls.Add(mapLabel);options.Controls.Add(maps);options.Controls.Add(statsEdit);options.Controls.Add(statsInfo);
        Label("CAMPEONATO (OPCIONAL)");options.Controls.Add(eventName);options.Controls.Add(dateLabel);options.Controls.Add(scheduled);
        Label("LOGOS DAS EQUIPES");Button("Selecionar logo da equipe A",()=>Resource("logo-a"));Button("Selecionar logo da equipe B",()=>Resource("logo-b"));Button("Usar logos neutras",()=>{brand.TeamALogo=brand.TeamBLogo="";Changed();});
        Label("FUNDO DA ARTE");Button("Carregar fundo",()=>Resource("fundo"));Button("Restaurar fundo padrão",()=>{brand.Background="";Changed();});options.Controls.Add(backgroundInfo);
        options.Controls.Add(mvpLabel);options.Controls.Add(mvp);
        Label("PRÉVIA");previewMode.Items.AddRange([L.T("Com fundo"),L.T("Transparente · OBS")]);previewMode.SelectedIndex=0;options.Controls.Add(previewMode);
        actions.Controls.Add(generate);var open=Button("Abrir pasta da partida",()=>{if(Selected is not null)Process.Start(new ProcessStartInfo("explorer.exe",MatchFiles.Ensure(Selected)){UseShellExecute=true});});actions.Controls.Add(open);actions.Controls.Add(obsCopy);status.AutoSize=false;status.Size=new(290,65);status.AutoEllipsis=true;actions.Controls.Add(status);
        matches.SelectedIndexChanged+=(_,_)=>{if(!loading)LoadSelection();};kind.SelectedIndexChanged+=(_,_)=>{if(!loading){Configure();_=Render();}};aspect.SelectedIndexChanged+=(_,_)=>{if(!loading)_=Render();};maps.SelectedIndexChanged+=(_,_)=>{if(!loading){Configure();_=Render();}};previewMode.SelectedIndexChanged+=(_,_)=>ShowPreview();
        eventName.TextChanged+=(_,_)=>{if(!loading){brand.EventName=eventName.Text;Changed();}};scheduled.ValueChanged+=(_,_)=>{if(!loading){brand.ScheduledAt=scheduled.Checked?scheduled.Value:null;Changed();}};
        mvp.SelectedIndexChanged+=(_,_)=>{if(!loading){brand.Mvp=(mvp.SelectedItem as MvpChoice)?.Key??"";Changed();}};
        delay.Tick+=(_,_)=>{delay.Stop();FlushPreferences();_=Render();};generate.Click+=async(_,_)=>await Export();statsEdit.Click+=(_,_)=>EditStatistics();obsCopy.Click+=(_,_)=>{try{Clipboard.SetText(MatchFiles.ObsVeto);status.Text=L.T("Caminho copiado. Configure uma fonte de imagem no OBS com este arquivo.");}catch(Exception ex){status.Text=ex.Message;}};
        Configure();
    }
    public void RefreshMatches(string? preferred=null)
    {
        string? selected=Selected?.Id??preferred;var entries=list();string key=JsonSerializer.Serialize(entries);
        if(selectionKey==key&&Selected is not null)return;FlushPreferences();selectionKey=key;loading=true;
        try{matches.Items.Clear();matches.Items.AddRange(entries.Cast<object>().ToArray());matches.SelectedItem=matches.Items.Cast<SiegeMatch>().FirstOrDefault(m=>m.Id==selected);if(matches.SelectedIndex<0&&matches.Items.Count>0)matches.SelectedIndex=0;}
        finally{loading=false;}LoadSelection();
    }
    void LoadSelection()
    {
        FlushPreferences();delay.Stop();loading=true;brandMatch=Selected;brand=Selected is null?new():MatchFiles.ReadBrand(Selected);eventName.Text=brand.EventName;scheduled.Value=brand.ScheduledAt??DateTime.Now;scheduled.Checked=brand.ScheduledAt.HasValue;
        maps.Items.Clear();if(Selected is not null)maps.Items.AddRange(Selected.Maps.Select(m=>(object)catalog.Maps.Single(c=>c.Id==m.MapId)).ToArray());if(maps.Items.Count>0)maps.SelectedIndex=0;loading=false;Configure();_=Render();
    }
    void Configure()
    {
        bool wasLoading=loading;loading=true;
        try
        {
            aspect.Enabled=Kind!=ArtKind.MapVeto;maps.Visible=mapLabel.Visible=statsEdit.Visible=statsInfo.Visible=Kind is ArtKind.MapStats or ArtKind.SeriesStats or ArtKind.Results;
            if(Kind==ArtKind.MapVeto)aspect.SelectedIndex=2;
            statsEdit.Enabled=Map is not null;dateLabel.Visible=scheduled.Visible=false;
            previewMode.Visible=obsCopy.Visible=Kind==ArtKind.MapVeto;
            actions.Height=Kind==ArtKind.MapVeto?210:170;
            mvp.Visible=mvpLabel.Visible=Kind is ArtKind.MapStats or ArtKind.SeriesStats;
            mvp.Items.Clear();mvp.Items.Add(new MvpChoice("",L.T("Sem destaque MVP")));
            if(Selected is not null)
            {
                var data=Kind==ArtKind.MapStats&&Map is not null?StatisticsData.ForMap(Map):StatisticsData.ForSeries(Selected);
                foreach(var p in data.Players)mvp.Items.Add(new MvpChoice(p.Key,Selected.Team(p.Team)+" · "+p.Name));
                statsInfo.Text=Map?.Statistics?.ConfirmedAt is null?L.T("Estatísticas ainda não conferidas."):Map.Statistics.Status=="complete"?L.T("Estatísticas completas e conferidas."):L.T("Estatísticas parciais ou indisponíveis.");
            }
            mvp.SelectedItem=mvp.Items.Cast<MvpChoice>().FirstOrDefault(p=>p.Key==brand.Mvp)??mvp.Items[0];
            backgroundInfo.Text=string.IsNullOrEmpty(brand.Background)?L.T("Fundo padrão"):L.T("Fundo personalizado. Ajustado ao formato; confira o recorte na prévia.");
        }
        finally{loading=wasLoading;}
    }
    void Resource(string type)
    {
        if(Selected is null)return;using var dialog=new OpenFileDialog{Filter=L.T("Imagem PNG ou JPEG|*.png;*.jpg;*.jpeg")};if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        try{string relative=MatchFiles.Resource(Selected,type,dialog.FileName);if(type=="fundo")brand.Background=relative;else if(type=="logo-a")brand.TeamALogo=relative;else brand.TeamBLogo=relative;Changed();Configure();}
        catch(Exception ex){status.Text=ex.Message;}
    }
    void Changed(){Clear();delay.Stop();delay.Start();Configure();}
    public void FlushPreferences(){delay.Stop();if(brandMatch is not null)try{MatchFiles.SaveBrand(brandMatch,brand);}catch(Exception ex){status.Text=ex.Message;}}
    void Clear(){generation++;preview.Image=null;foreach(var p in rendered)p.Dispose();rendered.Clear();generate.Enabled=false;}
    async Task Render()
    {
        Clear();if(Selected is null)return;int request=generation;working=true;status.Text=L.T("Preparando prévia… Você pode continuar navegando.");
        var snapshot=JsonSerializer.Deserialize<SiegeMatch>(JsonSerializer.Serialize(Selected))!;var copied=JsonSerializer.Deserialize<ImageBrand>(JsonSerializer.Serialize(brand))!;var k=Kind;var a=Aspect;var m=snapshot.Maps.ElementAtOrDefault(maps.SelectedIndex);
        try
        {
            var result=await Task.Run(()=>StudioRenderer.Render(snapshot,catalog,copied,k,a,k==ArtKind.MapStats?m:null));
            if(IsDisposed||request!=generation){foreach(var p in result)p.Dispose();return;}
            rendered=result;ShowPreview();generate.Enabled=true;status.Text=k==ArtKind.MapVeto?L.T("Ao gerar: arquivo transparente fixo no OBS + duas cópias na pasta da partida."):L.T("Prévia pronta. Os arquivos serão salvos na pasta desta partida.");
        }
        catch(Exception ex){if(!IsDisposed&&request==generation)status.Text=ex.Message;}
        finally{if(request==generation)working=false;}
    }
    void ShowPreview(){preview.Image=rendered.FirstOrDefault(p=>Kind!=ArtKind.MapVeto||p.Name==(previewMode.SelectedIndex==1?"map-veto-obs":"map-veto"))?.Image;preview.BackColor=previewMode.SelectedIndex==1&&Kind==ArtKind.MapVeto?Color.FromArgb(75,78,82):Color.FromArgb(18,24,32);}
    async Task Export()
    {
        if(Selected is null||rendered.Count==0)return;FlushPreferences();var selected=JsonSerializer.Deserialize<SiegeMatch>(JsonSerializer.Serialize(Selected))!;var copy=JsonSerializer.Deserialize<ImageBrand>(JsonSerializer.Serialize(brand))!;var k=Kind;var a=Aspect;var map=k==ArtKind.MapStats?selected.Maps.ElementAtOrDefault(maps.SelectedIndex):null;
        generate.Enabled=false;options.Enabled=matches.Enabled=false;status.Text=L.T("Salvando arquivos…");
        try{await Task.Run(()=>MatchFiles.Export(selected,catalog,copy,k,a,map));if(!IsDisposed)status.Text=L.T("Arquivos salvos. Use Abrir pasta da partida para acessar.");}
        catch(Exception ex){if(!IsDisposed)status.Text=ex.Message;}
        finally{if(!IsDisposed){generate.Enabled=rendered.Count>0;options.Enabled=matches.Enabled=true;}}
    }
    void EditStatistics(){if(Selected is null||Map is null)return;using var dialog=new StatisticsDialog(Selected,Map);if(dialog.ShowDialog(this)==DialogResult.OK){Configure();_=Render();}}
    public void PromptStatistics(SiegeMatch match,MapRecord map){using var dialog=new StatisticsDialog(match,map);dialog.ShowDialog(this);selectionKey=null;}
    internal void WaitForPreview(){var watch=Stopwatch.StartNew();while(working&&watch.ElapsedMilliseconds<20000){Application.DoEvents();Thread.Sleep(10);}if(working)throw new Exception("Studio preview timeout");}
    internal void SelectKind(ArtKind value){kind.SelectedIndex=(int)value;WaitForPreview();}
    internal void VerifyActionsVisible(){if(!actions.ClientRectangle.Contains(generate.Bounds)||generate.Parent!=actions||!generate.Visible)throw new Exception("Generate action clipped");}
    protected override void Dispose(bool disposing){if(disposing&&!cleaned){cleaned=true;FlushPreferences();delay.Dispose();Clear();}base.Dispose(disposing);}
    sealed record MvpChoice(string Key,string Name){public override string ToString()=>Name;}
}

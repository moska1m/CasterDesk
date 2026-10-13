using System.Drawing.Imaging;

namespace CronoLive;
public sealed class StatisticsDialog : Form
{
    readonly SiegeMatch match;readonly MapRecord map;
    readonly PictureBox screenshot=new(){Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(16,20,27)};
    readonly DataGridView grid=new BufferedGrid{Dock=DockStyle.Fill,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};
    readonly ComboBox topTeam=new ThemeCombo{DropDownStyle=ComboBoxStyle.DropDownList,Width=250},coverage=new ThemeCombo{DropDownStyle=ComboBoxStyle.DropDownList,Width=210};
    readonly Label status=new(){Dock=DockStyle.Fill,AutoEllipsis=true};
    readonly CancellationTokenSource lifetime=new();
    Bitmap? original;int generation;bool busy,cleaned;
    public StatisticsDialog(SiegeMatch match,MapRecord map)
    {
        this.match=match;this.map=map;
        Text=L.T("Conferir estatísticas")+" · "+SiegeCatalog.Load().Maps.Single(m=>m.Id==map.MapId).DisplayName;
        Font=AppFonts.Body(10);StartPosition=FormStartPosition.CenterParent;MinimumSize=new(950,620);var area=Screen.PrimaryScreen!.WorkingArea;Size=new(Math.Min(1500,area.Width-40),Math.Min(900,area.Height-40));KeyPreview=true;
        var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=3,Padding=new(16)};body.ColumnStyles.Add(new(SizeType.Percent,54));body.ColumnStyles.Add(new(SizeType.Percent,46));body.RowStyles.Add(new(SizeType.Absolute,122));body.RowStyles.Add(new(SizeType.Percent,100));body.RowStyles.Add(new(SizeType.Absolute,78));Controls.Add(body);
        var actions=new FlowLayoutPanel{Dock=DockStyle.Fill};Button Button(string text,Action click){var b=new ThemeButton{Text=L.T(text),AutoSize=false,Width=Math.Max(130,TextRenderer.MeasureText(L.T(text),Font).Width+28),Height=40,Font=AppFonts.Body(10)};b.Click+=(_,_)=>click();return b;}
        actions.Controls.AddRange([Button("Abrir print",OpenImage),Button("Colar print · Ctrl+V",Paste),Button("Ler novamente",()=>{if(original is not null)_=Read();})]);
        actions.Controls.Add(new Label{Text=L.T("Confira os dados ao lado antes de confirmar. O print não é enviado à internet."),AutoSize=true,MaximumSize=new(700,0),Margin=new(6,8,6,0)});body.Controls.Add(actions,0,0);
        var options=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false};options.Controls.Add(new Label{Text=L.T("Equipe no topo do print:"),AutoSize=true,Margin=new(3,0,3,5),Font=AppFonts.Heading(11)});topTeam.Width=390;topTeam.Items.AddRange([match.TeamA,match.TeamB]);topTeam.SelectedIndex=0;options.Controls.Add(topTeam);
        coverage.Width=390;coverage.Items.AddRange([L.T("Estatísticas completas"),L.T("Estatísticas parciais / remake"),L.T("Sem print / sem estatísticas")]);coverage.SelectedIndex=map.Statistics?.Status switch{"partial"=>1,"missing"=>2,_=>0};options.Controls.Add(coverage);body.Controls.Add(options,1,0);
        body.Controls.Add(screenshot,0,1);body.Controls.Add(grid,1,1);
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name=L.T("Equipe"),ReadOnly=true,FillWeight=80});grid.Columns.Add(new DataGridViewTextBoxColumn{Name=L.T("Jogador"),FillWeight=160});
        foreach(string name in new[]{"E","M","A"})grid.Columns.Add(new DataGridViewTextBoxColumn{Name=L.T(name),FillWeight=35});grid.ColumnHeadersHeight=40;grid.RowTemplate.Height=42;grid.DefaultCellStyle.Font=AppFonts.Body(10.5f);grid.ColumnHeadersDefaultCellStyle.Font=AppFonts.Heading(11);
        grid.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleCenter;grid.ColumnHeadersDefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleCenter;
        Fill(map.Statistics?.Players??[]);topTeam.SelectedIndexChanged+=(_,_)=>{for(int i=0;i<grid.Rows.Count;i++){int team=i<5?topTeam.SelectedIndex:1-topTeam.SelectedIndex;grid.Rows[i].Cells[0].Value=match.Team(team);grid.Rows[i].Tag=team;}};
        var footer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};footer.ColumnStyles.Add(new(SizeType.Percent,100));footer.ColumnStyles.Add(new(SizeType.Absolute,220));footer.Controls.Add(status,0,0);
        var confirm=Button("Confirmar estatísticas",Save);((ThemeButton)confirm).PrimaryAction=true;confirm.Dock=DockStyle.Fill;footer.Controls.Add(confirm,1,0);body.Controls.Add(footer,0,2);body.SetColumnSpan(footer,2);
        KeyDown+=(_,e)=>{if(e.Control&&e.KeyCode==Keys.V&&Clipboard.ContainsImage()){e.SuppressKeyPress=true;Paste();}};
        string? file=MatchFiles.Resolve(match,map.Statistics?.Screenshot);if(file is not null){using var im=Image.FromFile(file);original=new Bitmap(im);screenshot.Image=original;}
        status.Text=L.T("E = eliminações · M = mortes · A = assistências. Dados lidos precisam de revisão.");AppTheme.Apply(this);
    }
    void Fill(IReadOnlyList<PlayerStatistic> data)
    {
        grid.Rows.Clear();for(int team=0;team<2;team++)
        {
            var players=data.Where(p=>p.Team==team).Take(5).ToList();
            for(int i=0;i<5;i++){var p=players.ElementAtOrDefault(i);int row=grid.Rows.Add(match.Team(team),p?.Name??"",(object?)p?.Kills??"",(object?)p?.Deaths??"",(object?)p?.Assists??"");grid.Rows[row].Tag=team;grid.Rows[row].DefaultCellStyle.BackColor=team==0?Color.FromArgb(36,70,87):Color.FromArgb(91,54,36);grid.Rows[row].DefaultCellStyle.ForeColor=Color.White;}
        }
    }
    void OpenImage()
    {
        using var dialog=new OpenFileDialog{Filter=L.T("Imagem PNG ou JPEG|*.png;*.jpg;*.jpeg")};if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        try{if(new FileInfo(dialog.FileName).Length>40_000_000)throw new InvalidDataException(L.T("Use uma imagem com até 40 MB."));using var im=Image.FromFile(dialog.FileName);SetImage(im);}catch(Exception ex){status.Text=ex.Message;}
    }
    void Paste()
    {
        try{if(!Clipboard.ContainsImage()){status.Text=L.T("Copie uma imagem antes de colar.");return;}using var im=Clipboard.GetImage();if(im is not null)SetImage(im);}catch(Exception ex){status.Text=ex.Message;}
    }
    void SetImage(Image image)
    {
        if((long)image.Width*image.Height>50_000_000)throw new InvalidDataException(L.T("A imagem é muito grande."));
        generation++;screenshot.Image=null;original?.Dispose();original=new Bitmap(image);screenshot.Image=original;coverage.SelectedIndex=0;_=Read();
    }
    async Task Read()
    {
        if(original is null)return;int request=++generation;busy=true;status.Text=L.T("Lendo print… Confira os nomes e os números ao terminar.");
        string file=Path.Combine(Path.GetTempPath(),"casterdesk-ocr-"+Guid.NewGuid().ToString("N")+".png");
        try
        {
            // OCR supports a bounded image size; keep the archived original separate.
            float scale=Math.Min(1,2500f/Math.Max(original.Width,original.Height));using(var img=new Bitmap(original,(int)(original.Width*scale),(int)(original.Height*scale)))img.Save(file,ImageFormat.Png);
            var doc=await StatisticsData.ReadImage(file,lifetime.Token);if(IsDisposed||request!=generation)return;
            var data=StatisticsData.Parse(doc);if(topTeam.SelectedIndex==1)foreach(var p in data)p.Team=1-p.Team;
            Fill(data);status.Text=data.Count==10?L.T("Leitura concluída. Confira equipe, nome e E/M/A de cada jogador."):L.T("Não foi possível identificar dez jogadores. Preencha os campos ou marque como parcial.");
        }
        catch(OperationCanceledException){if(!IsDisposed&&request==generation)status.Text=L.T("Leitura interrompida. Você pode preencher os dados manualmente.");}
        catch(Exception ex){if(!IsDisposed&&request==generation)status.Text=ex.Message;}
        finally{if(File.Exists(file))File.Delete(file);if(request==generation)busy=false;}
    }
    void Save()
    {
        if(busy){status.Text=L.T("Aguarde a leitura terminar antes de confirmar.");return;}
        try
        {
            grid.EndEdit();var data=new MapStatistics{Status=coverage.SelectedIndex switch{1=>"partial",2=>"missing",_=>"complete"},ConfirmedAt=DateTimeOffset.Now};
            if(data.Status!="missing")foreach(DataGridViewRow row in grid.Rows)
            {
                string name=Convert.ToString(row.Cells[1].Value)?.Trim()??"";if(name.Length==0)continue;
                int? Number(int col){string text=Convert.ToString(row.Cells[col].Value)?.Trim()??"";return text.Length==0?null:int.TryParse(text,out int n)?n:throw new InvalidDataException(L.T("Use números inteiros nos campos E/M/A."));}
                data.Players.Add(new(){Team=(int)row.Tag!,Name=name,Kills=Number(2),Deaths=Number(3),Assists=Number(4)});
            }
            StatisticsData.Validate(data);
            if(original is not null&&data.Status!="missing")
            {
                string relative="Estatisticas/"+map.MapId+"-final.png";MatchFiles.AtomicImage(Path.Combine(MatchFiles.Ensure(match),relative),original);data.Screenshot=relative;
            }
            var old=map.Statistics;map.Statistics=data;try{MatchStore.Save(match);}catch{map.Statistics=old;throw;}
            DialogResult=DialogResult.OK;Close();
        }
        catch(Exception ex){status.Text=ex.Message;}
    }
    internal void LoadSample(string file){using var image=Image.FromFile(file);SetImage(image);var watch=System.Diagnostics.Stopwatch.StartNew();while(busy&&watch.ElapsedMilliseconds<45000){Application.DoEvents();Thread.Sleep(10);}if(busy)throw new Exception("OCR dialog timeout");}
    protected override void Dispose(bool disposing){if(disposing&&!cleaned){cleaned=true;lifetime.Cancel();lifetime.Dispose();screenshot.Image=null;original?.Dispose();}base.Dispose(disposing);}
}

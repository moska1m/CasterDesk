using System.Text;
using System.Text.Json;

namespace CronoLive;

public sealed class ChampionshipPanel : UserControl
{
    readonly SiegeCatalog catalog=SiegeCatalog.Load();
    readonly List<SiegeMatch> matches=[];
    SiegeMatch match=new(); MapRecord? current;
    bool loading, correctBans;
    string vetoKey="",sidesKey="",bansKey="",roundsKey="",savedKey="";
    readonly ComboBox saved=Combo(), format=Combo(), coin=Combo(), first=Combo(), mapSelect=Combo();
    readonly TextBox teamA=new(){MaxLength=12},teamB=new(){MaxLength=12};
    readonly Label score=new(){TextAlign=ContentAlignment.MiddleCenter,Font=new Font("Segoe UI",22,FontStyle.Bold),Dock=DockStyle.Fill};
    readonly Label notice=new(){AutoSize=true,ForeColor=Color.Firebrick,MaximumSize=new Size(1150,0)};
    readonly TableLayoutPanel veto=Table(3,3),sideRows=Table(1,1),bans=Table(6,4);
    readonly DataGridView rounds=new BufferedGrid(){Dock=DockStyle.Fill,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=Color.White,SelectionMode=DataGridViewSelectionMode.CellSelect,EditMode=DataGridViewEditMode.EditOnEnter};
    readonly Dictionary<string,Image> images=[];
    readonly System.Windows.Forms.Timer nameSave=new(){Interval=450};
    readonly ToolTip hints=new();
    bool refreshQueued;
    readonly System.Windows.Forms.Timer deferredRefresh=new(){Interval=50};
    readonly Button confirmWinner=new ThemeButton(){Text="Confirmar vencedor",AutoSize=true,Height=29,Enabled=false};
    static readonly Font SmallFont=new("Segoe UI",8);
    readonly Image missingBan=CreateMissingBan();
    static Image CreateMissingBan()
    {
        var image=new Bitmap(64,64);
        using var g=Graphics.FromImage(image);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var red=new SolidBrush(Color.Firebrick);using var white=new SolidBrush(Color.White);using var font=new Font("Segoe UI",32,FontStyle.Bold);
        g.FillEllipse(red,3,3,58,58);g.DrawString("!",font,white,new RectangleF(3,0,58,62),new StringFormat(){Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center});
        return image;
    }
    public ChampionshipPanel(bool demo=false)
    {
        Dock=DockStyle.Fill;
        deferredRefresh.Tick+=(_,_)=>{deferredRefresh.Stop();RefreshMatch();};
        var tabs=new ThemeTabs(){Dock=DockStyle.Fill}; Controls.Add(tabs);
        var page=new TabPage("Partida"){AutoScroll=true,BackColor=Color.White}; tabs.TabPages.Add(page);
        var canvas=new BufferedTable(){Dock=DockStyle.Top,Height=835,MinimumSize=new Size(1120,835),ColumnCount=2,RowCount=7,Padding=new Padding(8)};
        canvas.ColumnStyles.Add(new(SizeType.Percent,44));canvas.ColumnStyles.Add(new(SizeType.Percent,56));
        foreach(int h in new[]{38,310,50,300,38,36,40})canvas.RowStyles.Add(new(SizeType.Absolute,h));page.Controls.Add(canvas);
        var bar=new FlowLayoutPanel(){Dock=DockStyle.Fill,WrapContents=false}; saved.Width=430;
        bar.Controls.AddRange([Btn("Nova partida",NewMatch),saved]);canvas.Controls.Add(bar,0,0);canvas.SetColumnSpan(bar,2);
        var left=Table(1,2);left.RowStyles.Clear();left.RowStyles.Add(new(SizeType.Absolute,160));left.RowStyles.Add(new(SizeType.Percent,100));canvas.Controls.Add(left,0,1);
        var confrontation=Group("Confronto",left,0,0);var fields=Table(4,4);fields.Padding=new Padding(6);confrontation.Controls.Add(fields);
        fields.ColumnStyles.Clear();foreach(float w in new[]{22,28,22,28})fields.ColumnStyles.Add(new(SizeType.Percent,w));
        fields.Controls.Add(Label("Equipe A:"),0,0);var aField=TeamField(teamA,Color.FromArgb(64,143,179));fields.Controls.Add(aField,1,0);fields.SetColumnSpan(aField,3);
        fields.Controls.Add(Label("Equipe B:"),0,1);var bField=TeamField(teamB,Color.FromArgb(255,119,55));fields.Controls.Add(bField,1,1);fields.SetColumnSpan(bField,3);
        fields.Controls.Add(Label("Formato:"),0,2);fields.Controls.Add(format,1,2);format.Items.AddRange(["MD1","MD3"]);fields.Controls.Add(Label("MD5: futuro"),2,2);fields.SetColumnSpan(fields.GetControlFromPosition(2,2)!,2);
        fields.Controls.Add(Label("Coin flip:"),0,3);fields.Controls.Add(coin,1,3);fields.Controls.Add(Label("Primeiro ban:"),2,3);fields.Controls.Add(first,3,3);
        Group("Mapas da partida • lados da equipe indicada",left,0,1).Controls.Add(sideRows);
        Group("Mapas • picks e bans",canvas,1,1).Controls.Add(veto);
        canvas.Controls.Add(mapSelect,0,2);canvas.Controls.Add(score,1,2);
        var banGroup=Group("Bans de operadores",canvas,0,3);banGroup.Controls.Add(bans);
        bans.RowStyles.Clear();bans.RowStyles.Add(new(SizeType.Absolute,28));bans.RowStyles.Add(new(SizeType.Percent,50));bans.RowStyles.Add(new(SizeType.Absolute,28));bans.RowStyles.Add(new(SizeType.Percent,50));
        Group("Round a round • clique no bomb site ou vencedor",canvas,1,3).Controls.Add(rounds);
        var buttons=new FlowLayoutPanel(){Dock=DockStyle.Fill};buttons.Controls.AddRange([Btn("Corrigir bans",()=>{if(IsLocked){Warn("Mapa confirmado: edição bloqueada.");return;}correctBans=!correctBans;RefreshMatch();}),Btn("Resumo para remake",Summary),Btn("Salvar agora",()=>Persist())]);canvas.Controls.Add(buttons,0,4);
        var finish=new FlowLayoutPanel(){Dock=DockStyle.Fill};finish.Controls.Add(confirmWinner);canvas.Controls.Add(finish,1,4);confirmWinner.Click+=(_,_)=>ConfirmWinner();
        var transfer=new FlowLayoutPanel(){Dock=DockStyle.Fill};transfer.Controls.AddRange([Btn("Importar partida",Import),Btn("Exportar partida",Export)]);canvas.Controls.Add(transfer,0,5);
        canvas.Controls.Add(notice,0,6);canvas.SetColumnSpan(notice,2);
        var guide=new TabPage("Configurar lobby");tabs.TabPages.Add(guide);guide.Controls.Add(new LobbyGuidePanel());
        var exportPage=new TabPage("Exportar imagem");var exporter=new ImageExportPanel(()=>matches,catalog);exportPage.Controls.Add(exporter);tabs.TabPages.Add(exportPage);
        Controls.Add(new NavigationBar(tabs));
        tabs.Selecting+=(_,e)=>{if(e.TabPage==exportPage){FlushNames();exporter.RefreshMatches(match.Id);AppTheme.Apply(exporter);}};
        rounds.Columns.Add(new DataGridViewTextBoxColumn(){Name="Round",ReadOnly=true,FillWeight=45});
        rounds.Columns.Add(new DataGridViewTextBoxColumn(){Name="Ataque",ReadOnly=true,FillWeight=90});rounds.Columns.Add(new DataGridViewTextBoxColumn(){Name="Defesa",ReadOnly=true,FillWeight=90});
        rounds.Columns.Add(new DataGridViewComboBoxColumn(){Name="Bomb site",FillWeight=180,FlatStyle=FlatStyle.Standard});
        rounds.Columns.Add(new DataGridViewComboBoxColumn(){Name="Vencedor",FillWeight=100,FlatStyle=FlatStyle.Standard});
        rounds.Columns.Add(new DataGridViewTextBoxColumn(){Name="Placar",ReadOnly=true,FillWeight=50});
        rounds.CurrentCellDirtyStateChanged+=(_,_)=>{if(rounds.IsCurrentCellDirty)rounds.CommitEdit(DataGridViewDataErrorContexts.Commit);};
        rounds.CellValueChanged+=RoundChanged;
        rounds.DataError+=(_,e)=>{e.ThrowException=false;Warn("Seleção inválida. Confira o campo.");};
        saved.SelectedIndexChanged+=(_,_)=>{if(!loading && saved.SelectedItem is SiegeMatch selected){BeginInvoke((Action)(()=>{if(IsDisposed)return;FlushNames();match=selected;current=null;LoadMatch();}));}};
        teamA.TextChanged+=(_,_)=>NameChanged();teamB.TextChanged+=(_,_)=>NameChanged();
        nameSave.Tick+=(_,_)=>{nameSave.Stop();Persist();RefreshMatch();};
        format.SelectedIndexChanged+=(_,_)=>ChangeSetup("format");coin.SelectedIndexChanged+=(_,_)=>ChangeSetup("coin");first.SelectedIndexChanged+=(_,_)=>ChangeSetup("first");
        mapSelect.SelectedIndexChanged+=(_,_)=>{if(!loading){current=match.Maps.ElementAtOrDefault(mapSelect.SelectedIndex);correctBans=false;RefreshMatch();}};
        Directory.CreateDirectory(MatchStore.Folder);
        var recovered=new List<string>();
        foreach(string path in Directory.GetFiles(MatchStore.Folder,"*.json").OrderByDescending(File.GetLastWriteTimeUtc))
        {
            try{matches.Add(MatchStore.Read(path,catalog));}
            catch{try{matches.Add(MatchStore.Read(path+".bak",catalog));recovered.Add("Recuperada cópia anterior de "+Path.GetFileName(path));}catch{recovered.Add("Não foi possível abrir "+Path.GetFileName(path));}}
        }
        if(matches.Count==0){matches.Add(match);Persist();}else match=matches[0];
        if(demo)
        {
            match=new(){TeamA="Fnatic",TeamB="G2",Format="MD3",CoinWinner=1,FirstBan=0,Veto=["clubhouse","chalet","border","villa","bank","fortress","kafe-dostoyevsky","nighthaven-labs"]};
            match.SyncMaps(catalog);foreach(var m in match.Maps){m.InitialAttack=1;m.OtAttack=0;}
            current=match.Maps[0];current.Bans=["ace","thatcher","mira","azami","hibana","valkyrie","","","","","",""];
            foreach(int winner in new[]{0,1,0,1})current.Rounds.Add(new(){Winner=winner,Site=catalog.Maps.Single(x=>x.Id=="bank").Sites[0]});
            matches.Add(match);
        }
        LoadMatch(); if(recovered.Count>0)Warn(string.Join("; ",recovered));
    }
    static Control TeamField(TextBox name,Color color)
    {
        var row=new BufferedTable(){Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};
        row.ColumnStyles.Add(new(SizeType.Absolute,25));row.ColumnStyles.Add(new(SizeType.Percent,100));
        row.Controls.Add(new Panel(){BackColor=color,Size=new Size(17,17),Margin=new Padding(3,5,3,0),AccessibleName="Cor fixa da equipe"},0,0);
        name.Dock=DockStyle.Fill;row.Controls.Add(name,1,0);return row;
    }
    static ComboBox Combo()=>new ThemeCombo(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill};
    static Label Label(string text)=>new(){Text=text,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};
    static Button Btn(string text,Action action){var b=new ThemeButton(){Text=text,AutoSize=true,Height=29};b.Click+=(_,_)=>action();return b;}
    static TableLayoutPanel Table(int columns,int rows){var t=new BufferedTable(){Dock=DockStyle.Fill,ColumnCount=columns,RowCount=rows};for(int i=0;i<columns;i++)t.ColumnStyles.Add(new(SizeType.Percent,100f/columns));for(int i=0;i<rows;i++)t.RowStyles.Add(new(SizeType.Percent,100f/rows));return t;}
    static GroupBox Group(string title,TableLayoutPanel parent,int col,int row)
    {
        var g=new ThemeGroup(){Text=title.ToUpperInvariant(),Dock=DockStyle.Fill,Padding=new Padding(7),Font=new Font("Segoe UI",9,FontStyle.Bold)};
        g.ControlAdded+=(_,e)=>{if(e.Control is not null)e.Control.Font=new Font("Segoe UI",9,FontStyle.Regular);};
        parent.Controls.Add(g,col,row);return g;
    }
    static void Clear(Control c){foreach(Control child in c.Controls.Cast<Control>().ToArray()){c.Controls.Remove(child);child.Dispose();}}
    void Warn(string message){notice.Text=message;notice.ForeColor=AppTheme.Error;}
    bool Confirm(string message)=>MessageBox.Show(this,message,"Revisar partida",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes;
    bool IsLocked => current?.Confirmed==true || match.Complete;
    void NameChanged(){if(loading || match.HasConfirmed)return;match.TeamA=teamA.Text;match.TeamB=teamB.Text;nameSave.Stop();nameSave.Start();}
    public void FlushNames(){if(!nameSave.Enabled)return;nameSave.Stop();Persist();}
    bool Persist(){try{MatchStore.Save(match);notice.Text="Salvo automaticamente • "+DateTime.Now.ToString("HH:mm:ss");notice.ForeColor=AppTheme.Success;return true;}catch(Exception ex){Warn("Falha ao salvar: "+ex.Message);return false;}}
    void LoadMatch()
    {
        loading=true;saved.Items.Clear();saved.Items.AddRange(matches.Cast<object>().ToArray());saved.SelectedItem=match;teamA.Text=match.TeamA;teamB.Text=match.TeamB;format.SelectedItem=match.Format;loading=false;RefreshMatch();
    }
    void ChangeSetup(string field)
    {
        if(loading || match.HasConfirmed)return;
        if(field=="coin"){match.CoinWinner=coin.SelectedIndex;Persist();RefreshMatch();return;}
        if(match.Veto.Any(x=>!string.IsNullOrEmpty(x)) && !Confirm("Alterar o formato ou primeiro ban limpa o veto e os registros desta partida. Continuar?")){LoadMatch();return;}
        if(field=="format")match.Format=format.Text;else match.FirstBan=first.SelectedIndex;
        match.Veto=new string[8];match.Maps.Clear();current=null;Persist();RefreshMatch();
    }
    Image? Picture(string file)
    {
        try{if(!images.TryGetValue(file,out var img)){using var original=Image.FromFile(SiegeCatalog.Asset(file));images[file]=img=new Bitmap(original);}return img;}catch{return null;}
    }
    void RefreshMatch()
    {
        if(IsDisposed || Disposing)return;
        if(!IsHandleCreated){RefreshMatchNow();return;}
        if(refreshQueued)return;
        refreshQueued=true;
        BeginInvoke((Action)(()=>
        {
            refreshQueued=false;if(IsDisposed || Disposing)return;
            bool OpenDropdown(Control parent)=>parent.Controls.Cast<Control>().Any(c=>c is ComboBox box && box.DroppedDown || OpenDropdown(c));
            if(OpenDropdown(this)){deferredRefresh.Start();return;}
            RefreshMatchNow();
        }));
    }
    void RefreshMatchNow()
    {
        if(IsDisposed)return;
        using var update=new UiUpdate(this);loading=true;
        try {
        string listKey=string.Join("|",matches.Select(m=>m.Id+m.ToString()));
        if(savedKey!=listKey){saved.BeginUpdate();saved.Items.Clear();saved.Items.AddRange(matches.Cast<object>().ToArray());saved.EndUpdate();savedKey=listKey;}
        saved.SelectedItem=match;
        teamA.ReadOnly=teamB.ReadOnly=match.HasConfirmed;format.Enabled=coin.Enabled=first.Enabled=!match.HasConfirmed;
        hints.SetToolTip(saved,"ID da partida: "+match.Id);
        UpdateItems(coin,[match.TeamA,match.TeamB]);UpdateItems(first,[match.TeamA,match.TeamB]);coin.SelectedIndex=match.CoinWinner;first.SelectedIndex=match.FirstBan;
        string vk=JsonSerializer.Serialize(new{match.Id,match.TeamA,match.TeamB,match.Format,match.FirstBan,match.CoinWinner,match.Complete,match.Veto});
        if(vk!=vetoKey){vetoKey=vk;using var vetoUpdate=new UiUpdate(veto,true);Clear(veto);
        int nextStep=Array.FindIndex(match.Veto,string.IsNullOrEmpty);
        for(int i=0;i<9;i++)
        {
            int step=i;string? id=i<8?match.Veto[i]:match.Veto.All(x=>!string.IsNullOrEmpty(x))?catalog.Maps.First(x=>!match.Veto.Contains(x.Id)).Id:null;
            var map=catalog.Maps.FirstOrDefault(x=>x.Id==id);
            var cell=new Panel(){Dock=DockStyle.Fill,BorderStyle=BorderStyle.FixedSingle,Margin=new Padding(3)};
            var image=new PictureBox(){Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,Image=map is null?null:Picture(map.Image),BackColor=map is null?Color.WhiteSmoke:Color.White};
            var label=new Label(){Dock=DockStyle.Bottom,Height=35,TextAlign=ContentAlignment.MiddleCenter,Text=$"{i+1}. {(i==8?"DECIDER":match.Action(i)+" "+(match.FirstBan<0?"—":match.Team(match.Actor(i))))}\n{map?.Name ?? "Selecionar mapa…"}"};
            bool enabled=!match.Complete && i<8 && match.FirstBan>=0 && match.CoinWinner>=0 && (i==0 || !string.IsNullOrEmpty(match.Veto[i-1]));
            bool next=enabled && i==nextStep;
            Color tile=next?Color.FromArgb(215,235,255):map is null?Color.FromArgb(232,232,232):i<8 && match.Action(i)=="BAN"?Color.FromArgb(250,215,215):Color.FromArgb(212,239,216);
            cell.Padding=new Padding(next?3:0);cell.BackColor=next?Color.FromArgb(40,115,190):tile;image.BackColor=tile;label.BackColor=tile;
            label.ForeColor=map is null && !next?SystemColors.GrayText:Color.Black;
            if(next){label.Text=$"{i+1}. PRÓXIMO: {match.Action(i)} {match.Team(match.Actor(i))}\nClique para selecionar";hints.SetToolTip(image,"Continue por aqui");}
            image.Cursor=enabled?Cursors.Hand:Cursors.Default;image.Click+=(_,_)=>{if(enabled)PickMap(step);};label.Click+=(_,_)=>{if(enabled)PickMap(step);};
            cell.Controls.Add(image);cell.Controls.Add(label);veto.Controls.Add(cell,i%3,i/3);
        }
        AppTheme.Apply(veto);}
        string sk=JsonSerializer.Serialize(new{match.Id,match.TeamA,match.TeamB,match.Complete,Maps=match.Maps.Select(m=>new{m.MapId,m.Picker,m.InitialAttack,m.OtAttack,m.Confirmed})});
        if(sk!=sidesKey){sidesKey=sk;using var sideUpdate=new UiUpdate(sideRows,true);Clear(sideRows);sideRows.RowStyles.Clear();sideRows.RowCount=Math.Max(1,match.Maps.Count+1);sideRows.RowStyles.Add(new(SizeType.Absolute,23));for(int i=1;i<sideRows.RowCount;i++)sideRows.RowStyles.Add(new(SizeType.Absolute,30));
        TableLayoutPanel SideLine(){var line=Table(4,1);line.ColumnStyles.Clear();foreach(float width in new[]{25,21,27,27})line.ColumnStyles.Add(new(SizeType.Percent,width));return line;}
        var headings=SideLine();foreach(var pair in new[]{("Mapa",0),("Time",1),("Lado inicial",2),("OT",3)})headings.Controls.Add(Label(pair.Item1),pair.Item2,0);sideRows.Controls.Add(headings,0,0);
        for(int i=0;i<match.Maps.Count;i++)
        {
            var m=match.Maps[i];var row=SideLine();var name=catalog.Maps.Single(x=>x.Id==m.MapId).Name;var initial=Combo();var ot=Combo();
            initial.Items.AddRange([match.Team(1-m.Picker)+": ATQ",match.Team(1-m.Picker)+": DEF"]);ot.Items.AddRange([match.Team(m.Picker)+": ATQ",match.Team(m.Picker)+": DEF"]);
            initial.SelectedIndex=m.InitialAttack<0?-1:m.InitialAttack==1-m.Picker?0:1;ot.SelectedIndex=m.OtAttack<0?-1:m.OtAttack==m.Picker?0:1;
            initial.Enabled=ot.Enabled=!m.Confirmed && !match.Complete;
            var mapField=new TextBox(){Text=name,ReadOnly=true,Dock=DockStyle.Fill};var teamField=new TextBox(){Text=match.Team(m.Picker),ReadOnly=true,Dock=DockStyle.Fill};
            hints.SetToolTip(mapField,name);hints.SetToolTip(teamField,match.Team(m.Picker));
            row.Controls.Add(mapField,0,0);row.Controls.Add(teamField,1,0);row.Controls.Add(initial,2,0);row.Controls.Add(ot,3,0);sideRows.Controls.Add(row,0,i+1);
            initial.SelectedIndexChanged+=(_,_)=>{if(!loading)SetSide(m,false,initial.SelectedIndex==0?1-m.Picker:m.Picker);};ot.SelectedIndexChanged+=(_,_)=>{if(!loading)SetSide(m,true,ot.SelectedIndex==0?m.Picker:1-m.Picker);};
        }
        AppTheme.Apply(sideRows);}
        if(current is null || !match.Maps.Contains(current))current=match.Maps.FirstOrDefault();
        UpdateItems(mapSelect,match.Maps.Select((m,i)=>$"Mapa {i+1} — {catalog.Maps.Single(x=>x.Id==m.MapId).Name}").ToArray());mapSelect.SelectedIndex=current is null?-1:match.Maps.IndexOf(current);
        var s=current?.Score();score.Text=$"{match.TeamA}   {s?.A??0} × {s?.B??0}   {match.TeamB}";
        confirmWinner.Enabled=current is not null && s?.Winner>=0 && !IsLocked;
        confirmWinner.Text=current?.Confirmed==true?"Vencedor confirmado":s?.Winner>=0?"Confirmar vitória: "+match.Team(s.Value.Winner):"Confirmar vencedor";
        string bk=JsonSerializer.Serialize(new{match.Id,match.TeamA,match.TeamB,Locked=IsLocked,correctBans,current?.MapId,current?.InitialAttack,current?.OtAttack,current?.Bans,Reached=Enumerable.Range(0,12).Select(i=>current?.BanStageReached(i)),Half=current is null || current.InitialAttack<0?-1:current.ActiveBanHalf(Math.Min(15,current.Rounds.Count+(current.Score().Winner>=0?0:1)))});
        if(bk!=bansKey){bansKey=bk;using var banUpdate=new UiUpdate(bans,true);RenderBans();AppTheme.Apply(bans);}
        string rk=JsonSerializer.Serialize(new{match.Id,match.TeamA,match.TeamB,Locked=IsLocked,current?.MapId,current?.InitialAttack,current?.OtAttack,current?.Rounds});
        if(rk!=roundsKey){roundsKey=rk;RenderRounds();AppTheme.ApplyGrid(rounds);}
        AppTheme.Apply(this);
        if(IsLocked){notice.Text=match.Complete?"Série concluída. Resultados confirmados e edição bloqueada.":"Vencedor confirmado. Este mapa está bloqueado; você pode continuar nos demais mapas.";notice.ForeColor=AppTheme.Success;return;}
        if(current is not null && current.InitialAttack>=0)
        {
            int pending=Enumerable.Range(0,12).Count(i=>current.BanStageReached(i) && string.IsNullOrEmpty(current.Bans[i]));
            if(pending>0)Warn($"Atenção: {pending} ban(s) pendente(s), marcados com !. "+(s?.Winner>=0?"Confira os rounds e confirme o vencedor.":"Você pode continuar registrando os rounds."));
            else if(s?.Winner>=0)Warn("Vencedor definido. Confira os rounds e confirme o vencedor.");
            if(current.Rounds.Count==12 && s?.Winner<0 && current.OtAttack<0)Warn("Defina o lado do overtime para liberar o round 13. "+(pending>0?$"Há {pending} ban(s) pendente(s).":""));
        }
        else if(current is not null)Warn("Defina o lado inicial para liberar os bans e os rounds deste mapa.");
        }finally{loading=false;}
    }
    void PickMap(int step)
    {
        if(match.Complete)return;
        using var dialog=new Form(){Text=$"{step+1}. {match.Action(step)} — {match.Team(match.Actor(step))}",Size=new Size(720,450),StartPosition=FormStartPosition.CenterParent,MinimizeBox=false,MaximizeBox=false};
        var grid=Table(3,3);dialog.Controls.Add(grid);string? chosen=null;
        int n=0;foreach(var map in catalog.Maps)
        {
            var button=new ThemeButton(){Dock=DockStyle.Fill,Text=map.Name,Image=Picture(map.Image) is Image original?new Bitmap(original,new Size(155,75)):null,TextImageRelation=TextImageRelation.ImageAboveText,Enabled=match.Veto[step]==map.Id || !match.Veto.Contains(map.Id)};
            button.Disposed+=(_,_)=>button.Image?.Dispose();
            button.Click+=(_,_)=>{chosen=map.Id;dialog.DialogResult=DialogResult.OK;};grid.Controls.Add(button,n%3,n/3);n++;
        }
        AppTheme.Apply(dialog);if(dialog.ShowDialog(this)!=DialogResult.OK || chosen is null || chosen==match.Veto[step])return;
        var backup=match.Veto[step];var oldMaps=match.Maps.ToList();match.Veto[step]=chosen;match.SyncMaps(catalog);
        if(oldMaps.Any(m=>m.Confirmed && !match.Maps.Contains(m))){match.Veto[step]=backup;match.Maps=oldMaps;Warn("Esta escolha mudaria um mapa confirmado. Alteração bloqueada.");return;}
        if(oldMaps.Any(m=>m.HasData && !match.Maps.Contains(m)) && !Confirm("Esta troca muda um mapa com bans ou rounds registrados. Limpar apenas os registros do mapa substituído?")){match.Veto[step]=backup;match.Maps=oldMaps;return;}
        Persist();RefreshMatch();
    }
    void SetSide(MapRecord m,bool overtime,int attacker)
    {
        AppLog.Write($"SET SIDE match={match.Id} map={m.MapId} overtime={overtime} attacker={attacker}");
        if(m.Confirmed || match.Complete)return;
        if(m.HasData && !Confirm(overtime?"Recalcular os lados do overtime com esta escolha?":"Alterar o lado inicial recalcula ataque/defesa dos rounds e limpa os bans deste mapa, pois mudam os tipos permitidos. Continuar?")){RefreshMatch();return;}
        if(overtime)m.OtAttack=attacker;else{m.InitialAttack=attacker;m.Bans=new string[12];}Persist();RefreshMatch();
    }
    void RenderBans()
    {
        Clear(bans);
        for(int half=0;half<2;half++)
        {
            int round=current is null?1:Math.Min(15,current.Rounds.Count+(current.Score().Winner>=0?0:1));
            bool active=current is not null && current.InitialAttack>=0 && (round<=12 || current.OtAttack>=0) && current.ActiveBanHalf(round)==half;
            string sides=current is null || current.InitialAttack<0?"Defina o lado inicial":$"{match.Team(half==0?current.InitialAttack:1-current.InitialAttack)} ATQ / {match.Team(half==0?1-current.InitialAttack:current.InitialAttack)} DEF";
            var header=Label($"{(active?"ATIVOS • ":"")}R{(half==0?"1–6":"7–12")} • {sides}{(correctBans?" • Corrigindo":"")}");header.Font=SmallFont;header.BackColor=active?Color.FromArgb(220,235,250):Color.WhiteSmoke;bans.Controls.Add(header,0,half*2);bans.SetColumnSpan(header,6);
            for(int local=0;local<6;local++)
            {
                int slot=half*6+local;var op=catalog.Operators.FirstOrDefault(x=>x.Id==current?.Bans[slot]);bool unlocked=current is not null && (current.BanStageReached(slot) || !string.IsNullOrEmpty(current.Bans[slot]));
                var panel=Table(1,3);panel.Margin=new Padding(2);panel.RowStyles.Clear();panel.RowStyles.Add(new(SizeType.Absolute,43));panel.RowStyles.Add(new(SizeType.Absolute,27));panel.RowStyles.Add(new(SizeType.Percent,100));
                var picture=new PictureBox(){Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,Margin=Padding.Empty,Image=op is null?(unlocked?missingBan:null):Picture(op.Icon)};
                var b=Btn(op?.Name ?? (unlocked?"! Pendente":"Bloqueado"),()=>PickOperator(slot));b.AutoSize=false;b.Height=25;b.Margin=new Padding(0,1,0,1);b.Dock=DockStyle.Fill;b.Font=SmallFont;b.ForeColor=unlocked && op is null?Color.Firebrick:Color.Black;b.Enabled=!IsLocked && unlocked && (op is null || correctBans);
                var caption=Label($"{match.Team(MapRecord.BanTeam(slot))}\nR{MapRecord.BanThreshold(slot)+1}");caption.Dock=DockStyle.Fill;caption.Font=SmallFont;caption.TextAlign=ContentAlignment.TopCenter;caption.Padding=new Padding(0,3,0,0);
                hints.SetToolTip(b,unlocked?"Selecionar ban de "+current!.BanRole(slot):"Libera após o round "+MapRecord.BanThreshold(slot));
                panel.Controls.Add(picture,0,0);panel.Controls.Add(b,0,1);panel.Controls.Add(caption,0,2);panel.BackColor=unlocked?Color.White:Color.FromArgb(235,235,235);bans.Controls.Add(panel,local,half*2+1);
            }
        }
    }
    void PickOperator(int slot)
    {
        if(current is null || IsLocked)return;using var dialog=new Form(){Text=$"{match.Team(MapRecord.BanTeam(slot))} bane {current.BanRole(slot)}",Size=new Size(700,550),StartPosition=FormStartPosition.CenterParent};
        var search=new TextBox(){Dock=DockStyle.Top,PlaceholderText="Buscar operador…"};var flow=new FlowLayoutPanel(){Dock=DockStyle.Fill,AutoScroll=true};dialog.Controls.Add(flow);dialog.Controls.Add(search);string? selected=null;
        void Fill(){Clear(flow);foreach(var op in catalog.Operators.Where(o=>o.Role==current.BanRole(slot) && o.Name.Contains(search.Text,StringComparison.OrdinalIgnoreCase)).OrderBy(o=>o.Name))
        {
            bool used=current.Bans.Skip(slot/6*6).Take(6).Contains(op.Id) && current.Bans[slot]!=op.Id;
            var b=new ThemeButton(){Text=op.Name,Width=100,Height=88,TextImageRelation=TextImageRelation.ImageAboveText,Image=Picture(op.Icon) is Image img?new Bitmap(img,new Size(42,42)):null,Enabled=!used};b.Disposed+=(_,_)=>b.Image?.Dispose();b.Click+=(_,_)=>{selected=op.Id;dialog.DialogResult=DialogResult.OK;};flow.Controls.Add(b);
        }}search.TextChanged+=(_,_)=>Fill();Fill();
        AppTheme.Apply(dialog);if(dialog.ShowDialog(this)==DialogResult.OK){current.Bans[slot]=selected!;Persist();RefreshMatch();}
    }
    void RenderRounds()
    {
        if(current is null){rounds.Rows.Clear();return;}


        var sites=(DataGridViewComboBoxColumn)rounds.Columns[3];UpdateGridItems(sites,new[]{"Selecionar…"}.Concat(catalog.Maps.Single(x=>x.Id==current.MapId).Sites).ToArray());
        var winners=(DataGridViewComboBoxColumn)rounds.Columns[4];UpdateGridItems(winners,["Selecionar…","A • "+match.TeamA,"B • "+match.TeamB]);
        int a=0,b=0;bool ended=current.Score().Winner>=0;
        int count=current.Rounds.Count+(ended?0:1);
        while(rounds.Rows.Count>count)rounds.Rows.RemoveAt(rounds.Rows.Count-1);
        while(rounds.Rows.Count<count)rounds.Rows.Add();
        for(int i=0;i<count;i++)
        {
            var r=current.Rounds.ElementAtOrDefault(i);if(r is not null){if(r.Winner==0)a++;else b++;}
            bool ready=current.InitialAttack>=0 && (i<12 || current.OtAttack>=0);int attacker=ready?current.Attacker(i+1):-1;
            int row=i;object[] values=[i+1,attacker<0?"—":match.Team(attacker),attacker<0?"—":match.Team(1-attacker),string.IsNullOrEmpty(r?.Site)?"Selecionar…":r.Site,r is null?"Selecionar…":(r.Winner==0?"A • ":"B • ")+match.Team(r.Winner),r is null?"—":$"{a}–{b}"];
            for(int col=0;col<values.Length;col++)if(!Equals(rounds.Rows[row].Cells[col].Value,values[col]))rounds.Rows[row].Cells[col].Value=values[col];
            foreach(DataGridViewCell cell in rounds.Rows[row].Cells)if(cell is DataGridViewComboBoxCell combo){combo.FlatStyle=FlatStyle.Flat;combo.DisplayStyle=DataGridViewComboBoxDisplayStyle.DropDownButton;}
            rounds.Rows[row].ReadOnly=!ready || IsLocked;if(!ready)rounds.Rows[row].DefaultCellStyle.BackColor=Color.Gainsboro;else if(r is null)rounds.Rows[row].DefaultCellStyle.BackColor=Color.FromArgb(220,235,250);
            if(r is not null)
            {
                var style=rounds.Rows[row].DefaultCellStyle;
                style.BackColor=r.Winner==0?Color.FromArgb(0x40,0x8f,0xb3):Color.FromArgb(0xff,0x77,0x37);
                style.ForeColor=Color.Black;style.SelectionBackColor=style.BackColor;style.SelectionForeColor=Color.Black;
                foreach(DataGridViewCell cell in rounds.Rows[row].Cells)if(cell is DataGridViewComboBoxCell combo){combo.FlatStyle=FlatStyle.Flat;combo.DisplayStyle=DataGridViewComboBoxDisplayStyle.DropDownButton;}
            }
            if(current.Confirmed && i==current.Rounds.Count-1)
            {
                var style=rounds.Rows[row].DefaultCellStyle;style.BackColor=Color.FromArgb(199,235,204);style.ForeColor=Color.FromArgb(15,65,30);style.SelectionBackColor=Color.FromArgb(174,215,180);style.SelectionForeColor=style.ForeColor;
                foreach(DataGridViewCell cell in rounds.Rows[row].Cells)if(cell is DataGridViewComboBoxCell combo)combo.DisplayStyle=DataGridViewComboBoxDisplayStyle.Nothing;
            }
        }
    }
    static void UpdateItems(ComboBox combo,string[] values)
    {
        if(combo.Items.Cast<object>().Select(x=>x.ToString()).SequenceEqual(values))return;
        combo.BeginUpdate();try{combo.Items.Clear();combo.Items.AddRange(values);}finally{combo.EndUpdate();}
    }
    static void UpdateGridItems(DataGridViewComboBoxColumn column,string[] values)
    {
        if(column.Items.Cast<object>().Select(x=>x.ToString()).SequenceEqual(values))return;
        column.Items.Clear();column.Items.AddRange(values);
    }
    void RoundChanged(object? sender,DataGridViewCellEventArgs e)
    {
        if(loading || current is null || IsLocked || e.RowIndex<0 || e.ColumnIndex<3 || e.ColumnIndex>4)return;
        int row=e.RowIndex;string site=rounds.Rows[row].Cells[3].Value?.ToString()??"Selecionar…";string winner=rounds.Rows[row].Cells[4].Value?.ToString()??"Selecionar…";
        if(row>=current.Rounds.Count && winner=="Selecionar…")return;
        if(winner=="Selecionar…"){Warn("Escolha um vencedor para corrigir o round.");BeginInvoke((Action)RefreshMatch);return;}
        int who=winner.StartsWith("A • ")?0:1;
        var snapshot=JsonSerializer.Deserialize<List<RoundRecord>>(JsonSerializer.Serialize(current.Rounds))!;
        if(row==current.Rounds.Count){if(current.Score().Winner>=0){Warn("Vencedor já definido. Confira os rounds.");return;}current.Rounds.Add(new(){Winner=who,Site=site=="Selecionar…"?"":site});}
        else{current.Rounds[row].Winner=who;current.Rounds[row].Site=site=="Selecionar…"?"":site;}
        var result=current.Score();
        if(result.EndAt>0 && result.EndAt<current.Rounds.Count)
        {
            if(!Confirm("Esta correção encerra o mapa no round "+result.EndAt+". Remover os rounds posteriores?")){current.Rounds=snapshot;BeginInvoke((Action)RefreshMatch);return;}
            current.Rounds.RemoveRange(result.EndAt,current.Rounds.Count-result.EndAt);
            for(int i=0;i<12;i++)if(MapRecord.BanThreshold(i)>current.Rounds.Count)current.Bans[i]="";
        }
        Persist();BeginInvoke((Action)RefreshMatch);
    }
    void NewMatch(){if(!Persist())return;SiegeMatch next;do{next=new();}while(matches.Any(m=>m.Id==next.Id)||File.Exists(Path.Combine(MatchStore.Folder,next.Id+".json")));match=next;matches.Insert(0,match);current=null;correctBans=false;Persist();LoadMatch();}
    void ConfirmWinner()
    {
        if(current is null || IsLocked || current.Score().Winner<0)return;
        int pending=Enumerable.Range(0,12).Count(i=>current.BanStageReached(i) && string.IsNullOrEmpty(current.Bans[i]));
        string message=$"Confirmar que {match.Team(current.Score().Winner)} venceu este mapa?\n\nO mapa ficará bloqueado para edição. A correção administrativa ainda não está disponível.";
        if(pending>0)message+=$"\n\nExistem {pending} bans pendentes. Eles também ficarão bloqueados.";
        if(!Confirm(message))return;
        current.ConfirmedAt=DateTimeOffset.Now;
        if(!Persist()){current.ConfirmedAt=null;return;}
        correctBans=false;RefreshMatch();
    }
    void Export()
    {
        FlushNames();string Safe(string s)=>string.Concat(s.Select(c=>Path.GetInvalidFileNameChars().Contains(c)?'_':c));
        using var dialog=new SaveFileDialog(){Filter="Partida JSON|*.json",FileName=$"{match.Created:yyyy-MM-dd}_{Safe(match.TeamA)}_vs_{Safe(match.TeamB)}_{(match.Complete?"concluida":"aberta")}.json"};
        if(dialog.ShowDialog(this)!=DialogResult.OK)return;try{MatchStore.Atomic(dialog.FileName,match);notice.Text="Partida exportada.";}catch(Exception ex){Warn(ex.Message);}
    }
    void Import()
    {
        using var dialog=new OpenFileDialog(){Filter="Partida JSON|*.json"};if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        try
        {
            var imported=MatchStore.Read(dialog.FileName,catalog);
            if(matches.Any(m=>string.Equals(m.Id,imported.Id,StringComparison.OrdinalIgnoreCase)) || File.Exists(Path.Combine(MatchStore.Folder,imported.Id+".json")))
            {Warn($"Esta partida já existe no programa: {imported.TeamA} × {imported.TeamB}. ID: {imported.Id}. Nenhum registro foi substituído.");return;}
            if(!Persist())return;MatchStore.Save(imported);match=imported;matches.Insert(0,match);current=null;LoadMatch();
        }catch(Exception ex){Warn("Não foi possível importar: "+ex.Message);}
    }
    void Summary()
    {
        if(current is null){Warn("Escolha um mapa primeiro.");return;}
        var s=current.Score();int next=current.Rounds.Count+1;var text=new StringBuilder();text.AppendLine($"{match.TeamA} × {match.TeamB} — {catalog.Maps.Single(x=>x.Id==current.MapId).Name}");text.AppendLine($"Placar: {s.A} × {s.B} • {current.Rounds.Count} rounds concluídos");
        if(s.Winner>=0)text.AppendLine("Mapa encerrado: "+match.Team(s.Winner));else if(current.InitialAttack>=0 && (next<=12 || current.OtAttack>=0))text.AppendLine($"Retomar no Round {next:00}: {match.Team(current.Attacker(next))} ATQ / {match.Team(1-current.Attacker(next))} DEF");else text.AppendLine("Defina os lados antes de retomar.");
        for(int half=0;half<2;half++){text.AppendLine($"\r\nBans da {(half==0?"primeira":"segunda")} metade:");for(int i=half*6;i<half*6+6;i++)text.AppendLine($"{i%6+1:00}. {match.Team(MapRecord.BanTeam(i))}: {catalog.Operators.FirstOrDefault(o=>o.Id==current.Bans[i])?.Name??"Pendente / não realizado"}");}
        text.AppendLine("\r\nHistórico:");for(int i=0;i<current.Rounds.Count;i++){var r=current.Rounds[i];text.AppendLine($"Round {i+1:00}: {match.Team(current.Attacker(i+1))} ATQ • {r.Site} • venceu {match.Team(r.Winner)}");}
        using var dialog=new Form(){Text="Resumo para remake",Size=new Size(720,600),StartPosition=FormStartPosition.CenterParent};var box=new TextBox(){Text=text.ToString(),Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill};var copy=Btn("Copiar resumo",()=>Clipboard.SetText(box.Text));copy.Dock=DockStyle.Bottom;dialog.Controls.Add(box);dialog.Controls.Add(copy);AppTheme.Apply(dialog);dialog.ShowDialog(this);
    }
    protected override void Dispose(bool disposing){if(disposing){deferredRefresh.Dispose();nameSave.Dispose();hints.Dispose();missingBan.Dispose();foreach(var img in images.Values)img.Dispose();}base.Dispose(disposing);}
    internal void VerifyRoundEditing()
    {
        if(current is null || current.Rounds.Count!=4)throw new Exception("Demo not loaded");
        rounds.Rows[4].Cells[3].Value=catalog.Maps.Single(x=>x.Id==current.MapId).Sites[1];
        rounds.Rows[4].Cells[4].Value="A • "+match.TeamA;Application.DoEvents();
        if(current.Rounds.Count!=5 || current.Score().A!=3)throw new Exception("Adding round failed");
        rounds.Rows[4].Cells[4].Value="B • "+match.TeamB;Application.DoEvents();
        if(current.Rounds.Count!=5 || current.Score().B!=3 || current.Score().A!=2)throw new Exception("Correcting round failed");
        if(rounds.Rows[0].DefaultCellStyle.BackColor!=Color.FromArgb(0x40,0x8f,0xb3) || rounds.Rows[4].DefaultCellStyle.BackColor!=Color.FromArgb(0xff,0x77,0x37))throw new Exception("Team colors failed");
        MatchStore.Validate(match,catalog);
        current.Rounds=Enumerable.Range(0,7).Select(_=>new RoundRecord{Winner=0}).ToList();current.ConfirmedAt=DateTimeOffset.Now;RefreshMatch();Application.DoEvents();
        if(confirmWinner.Enabled || rounds.Rows.Cast<DataGridViewRow>().Any(r=>!r.ReadOnly) || !teamA.ReadOnly || format.Enabled)throw new Exception("Confirmation locks failed");
        if(rounds.Rows[6].DefaultCellStyle.BackColor!=Color.FromArgb(199,235,204) || rounds.Rows[5].DefaultCellStyle.BackColor==Color.FromArgb(199,235,204))throw new Exception("Final round highlight failed");
        rounds.Rows[0].Cells[4].Value="B • "+match.TeamB;Application.DoEvents();if(current.Rounds[0].Winner!=0)throw new Exception("Confirmed result changed");
        current=match.Maps[1];RefreshMatch();Application.DoEvents();if(IsLocked)throw new Exception("Unconfirmed map locked too early");
        // Regression: never destroy a native combo while its dropdown is selecting.
        for(int i=0;i<20;i++)
        {
            var line=(TableLayoutPanel)sideRows.GetControlFromPosition(0,2)!;
            var combo=(ComboBox)line.GetControlFromPosition(i%2==0?2:3,0)!;
            combo.Focus();combo.DroppedDown=true;int selected=1-combo.SelectedIndex;combo.SelectedIndex=selected;Application.DoEvents();
            if(combo.IsDisposed)throw new Exception("Destroyed selector during dropdown notification");
            combo.DroppedDown=false;
            for(int tick=0;tick<5;tick++){System.Threading.Thread.Sleep(20);Application.DoEvents();}
            if(current.InitialAttack<0 || current.OtAttack<0)throw new Exception("Side selection not retained");
        }
    }
    internal string MeasureRefresh()
    {
        current=match.Maps[0];RefreshMatchNow();
        IEnumerable<Control> Tree(Control c)=>new[]{c}.Concat(c.Controls.Cast<Control>().SelectMany(Tree));
        var controls=Tree(this).ToHashSet();int layouts=0;foreach(var c in controls)c.Layout+=(_,_)=>layouts++;
        long allocated=GC.GetAllocatedBytesForCurrentThread();var watch=System.Diagnostics.Stopwatch.StartNew();
        for(int i=0;i<20;i++){current.Rounds[0].Site=catalog.Maps.Single(m=>m.Id==current.MapId).Sites[i%2];RefreshMatchNow();}
        watch.Stop();int retained=Tree(this).Count(controls.Contains);
        if(retained!=controls.Count || layouts!=0)throw new Exception("Edição simples recriou controles ou recalculou layout");
        return $"20 alterações de bombsite: {watch.Elapsed.TotalMilliseconds:F0} ms; alocação gerenciada: {(GC.GetAllocatedBytesForCurrentThread()-allocated)/1024} KB; layouts: {layouts}; controles retidos: {retained}/{controls.Count}.";
    }
    const string Guide="GUIA DE LOBBY — referência fornecida para este projeto\r\n\r\n1. Criar online → Criar novas condições de jogo.\r\nDê um nome à playlist e clique em Adicionar uma partida.\r\n\r\n2. Selecione o primeiro mapa, display Pro League e modo Bomba.\r\n\r\n3. Configurações da partida\r\nFase de banimento: Pro League • Tempo: 20\r\nRodadas: 12 • Troca de lados: 6\r\nProrrogação: 3 • Diferença: 2 • Troca no OT: 1\r\nRotação de objetivo: 2 • Rodadas jogadas\r\nFase de escolha: 15\r\nEnergia: 100 • Fogo amigo: 100 • Ferido: 20\r\nFogo amigo na preparação e reverso: desligados\r\nCorrer / inclinar: ligados\r\nDuração da morte: 3 • Replay: desligado\r\nPausa tática: 1 por equipe • Jogadores • 45 segundos\r\nIntervalo de troca de função: ligado • 120 segundos\r\n\r\n4. Modo Bomba — conferir o padrão informado\r\nInstalar: 7 • Desativar: 7 • Detonação: 45\r\nPortador do desativador: ligado\r\nPreparação: 45 • Ação: 180 • Repick: ligado\r\n\r\n5. Duplicar partida e mudar apenas o mapa.\r\nRepita para os nove mapas e Salvar condições de jogo.\r\n\r\nMap pool deste projeto:\r\nBanco, Fronteira, Chalé, Clube, Fortaleza, Café Dostoyevsky,\r\nLaboratórios Nighthaven, Covil e Mansão.\r\n\r\nEste guia é de consulta; o programa não configura o jogo automaticamente.";
}


using System.Text;
using System.Text.Json;

namespace CasterDesk.Core;

public sealed class ChampionshipPanel : UserControl
{
    readonly SiegeCatalog catalog=SiegeCatalog.Load();
    readonly List<SiegeMatch> matches=[];
    SiegeMatch match=new(); MapRecord? current;
    bool loading, correctBans,namesDirty;
    readonly HashSet<string> shownBanReminders=[];
    internal Action<int[]>? BanReminderPreview;
    internal Action<Form,int>? OperatorPickerPreview;
    public event Action<SiegeMatch,MapRecord>? MapConfirmed;
    public IReadOnlyList<SiegeMatch> Matches=>matches;
    public string CurrentMatchId=>match.Id;
    string vetoKey="",sidesKey="",bansKey="",roundsKey="",savedKey="";
    readonly ComboBox saved=Combo(), format=Combo(), coin=Combo(), first=Combo(), mapSelect=Combo();
    readonly TextBox teamA=new(){MaxLength=12,PlaceholderText=L.T("Nome da equipe A")},teamB=new(){MaxLength=12,PlaceholderText=L.T("Nome da equipe B")};
    static readonly Font ScoreFont=AppFonts.Body(24,FontStyle.Bold),CompactScoreFont=AppFonts.Body(18,FontStyle.Bold);
    readonly Label score=new(){TextAlign=ContentAlignment.MiddleCenter,Font=ScoreFont,Dock=DockStyle.Fill,AutoEllipsis=true};
    readonly Label notice=new(){AutoSize=true,ForeColor=Color.Firebrick,MaximumSize=new Size(1150,0)};
    readonly TableLayoutPanel veto=Table(3,3),sideRows=Table(1,1),bans=Table(6,4);
    readonly TableLayoutPanel roundHost=Table(2,1);
    readonly QuickRoundPanel quickRound=new();
    TableLayoutPanel mainCanvas=null!;
    TableLayoutPanel scoreboard=null!;
    TableLayoutPanel leftLayout=null!;
    Panel sidesViewport=null!;
    FlowLayoutPanel actionButtons=null!;
    Button compactSetup=null!;
    bool showCompactSides;
    int visibleBanHalf=-1, previousBanHalf=-1;
    bool adjustingLayout;
    bool quickMode;
    readonly DataGridView rounds=new BufferedGrid(){Dock=DockStyle.Fill,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=Color.White,SelectionMode=DataGridViewSelectionMode.CellSelect,EditMode=DataGridViewEditMode.EditOnEnter};
    readonly Dictionary<string,Image> images=[];
    readonly System.Windows.Forms.Timer nameSave=new(){Interval=450};
    readonly ToolTip hints=new();
    bool refreshQueued;
    readonly System.Windows.Forms.Timer deferredRefresh=new(){Interval=50};
    readonly Button correction=new ThemeButton(){Text=L.T("Corrigir bans"),AutoSize=true,Height=29};
    readonly Button confirmWinner=new ThemeButton(){Text=L.T("Confirmar vencedor"),AutoSize=true,Height=29,Enabled=false};
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
        var page=new TabPage(L.T("Partida")){AutoScroll=false,BackColor=Color.White}; tabs.TabPages.Add(page);
        var canvas=new BufferedTable(){Dock=DockStyle.Fill,ColumnCount=2,RowCount=6,Padding=new Padding(10)};
        mainCanvas=canvas;
        canvas.ColumnStyles.Add(new(SizeType.Percent,44));canvas.ColumnStyles.Add(new(SizeType.Percent,56));
        foreach(int h in new[]{38,320,48,300,42,30})canvas.RowStyles.Add(new(SizeType.Absolute,h));canvas.RowStyles[3].SizeType=SizeType.Percent;canvas.RowStyles[3].Height=100;page.Controls.Add(canvas);
        canvas.SizeChanged+=(_,_)=>AdjustLayout();
        var bar=new FlowLayoutPanel(){Dock=DockStyle.Fill,WrapContents=false}; saved.Width=430;
        var newMatch=Btn(L.T("Nova partida"),NewMatch);((ThemeButton)newMatch).PrimaryAction=true;bar.Controls.AddRange([newMatch,saved]);canvas.Controls.Add(bar,0,0);canvas.SetColumnSpan(bar,2);
        var left=Table(1,2);leftLayout=left;left.RowStyles.Clear();left.RowStyles.Add(new(SizeType.Absolute,155));left.RowStyles.Add(new(SizeType.Percent,100));
        var leftHost=new Panel{Dock=DockStyle.Fill};leftHost.Controls.Add(left);compactSetup=new ThemeButton{Dock=DockStyle.Top,Height=27,Visible=false};compactSetup.Click+=(_,_)=>{showCompactSides=!showCompactSides;AdjustLayout();};leftHost.Controls.Add(compactSetup);canvas.Controls.Add(leftHost,0,1);canvas.SetRowSpan(leftHost,2);
        var confrontation=Group(L.T("Confronto"),left,0,0);var fields=Table(4,4);fields.Padding=new Padding(6);confrontation.Controls.Add(fields);
        fields.ColumnStyles.Clear();foreach(float w in new[]{22,28,22,28})fields.ColumnStyles.Add(new(SizeType.Percent,w));
        fields.Controls.Add(Caption(L.T("Equipe A:")),0,0);var aField=TeamField(teamA,Color.FromArgb(64,143,179),0);fields.Controls.Add(aField,1,0);fields.SetColumnSpan(aField,3);
        fields.Controls.Add(Caption(L.T("Equipe B:")),0,1);var bField=TeamField(teamB,Color.FromArgb(255,119,55),1);fields.Controls.Add(bField,1,1);fields.SetColumnSpan(bField,3);
        fields.Controls.Add(Caption(L.T("Formato:")),0,2);fields.Controls.Add(format,1,2);format.Items.AddRange([L.FormatName("MD1"),L.FormatName("MD3"),L.FormatName("MD5")]);
        fields.Controls.Add(Caption(L.T("Coin flip:")),0,3);fields.Controls.Add(coin,1,3);fields.Controls.Add(Caption(L.T("Primeiro ban:")),2,3);fields.Controls.Add(first,3,3);
        sidesViewport=new Panel{Dock=DockStyle.Fill,AutoScroll=true};sideRows.Dock=DockStyle.Top;
        sidesViewport.Controls.Add(sideRows);Group(L.T("Mapas da partida • lados da equipe indicada"),left,0,1).Controls.Add(sidesViewport);
        Group(L.T("Mapas • picks e bans"),canvas,1,1).Controls.Add(veto);
        scoreboard=Table(3,1);scoreboard.Padding=new Padding(10,0,10,0);scoreboard.ColumnStyles.Clear();scoreboard.ColumnStyles.Add(new(SizeType.Percent,25));scoreboard.ColumnStyles.Add(new(SizeType.Percent,50));scoreboard.ColumnStyles.Add(new(SizeType.Percent,25));mapSelect.Dock=DockStyle.None;mapSelect.Anchor=AnchorStyles.Left;mapSelect.Width=200;scoreboard.Controls.Add(mapSelect,0,0);scoreboard.Controls.Add(score,1,0);canvas.Controls.Add(scoreboard,1,2);
        var banGroup=Group(L.T("Bans de operadores"),canvas,0,3);banGroup.Controls.Add(bans);
        bans.RowStyles.Clear();bans.RowStyles.Add(new(SizeType.Absolute,28));bans.RowStyles.Add(new(SizeType.Percent,50));bans.RowStyles.Add(new(SizeType.Absolute,28));bans.RowStyles.Add(new(SizeType.Percent,50));
        Group(L.T("Round a round • clique no bomb site ou vencedor"),canvas,1,3).Controls.Add(roundHost);
        roundHost.ColumnStyles.Clear();roundHost.ColumnStyles.Add(new(SizeType.Absolute,0));roundHost.ColumnStyles.Add(new(SizeType.Percent,100));
        quickRound.Dock=DockStyle.Fill;quickRound.Visible=false;roundHost.Controls.Add(quickRound,0,0);roundHost.Controls.Add(rounds,1,0);
        quickRound.ConfirmRound+=(site,winner,ending)=>RecordQuickRound(site,winner,ending);
        var footer=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty};canvas.Controls.Add(footer,0,4);canvas.SetColumnSpan(footer,2);
        var buttons=new FlowLayoutPanel(){Dock=DockStyle.Fill,WrapContents=false};actionButtons=buttons;buttons.Controls.AddRange([correction,Btn(L.T("Resumo para remake"),Summary),Btn(L.T("Salvar agora"),()=>Persist()),Btn(L.T("Importar partida"),Import),Btn(L.T("Exportar partida"),Export)]);footer.Controls.Add(buttons);
        correction.Click+=(_,_)=>{if(current is null)return;correctBans=!correctBans;RefreshMatch();};
        var finish=new Panel(){Dock=DockStyle.Right,Width=230};confirmWinner.AutoSize=false;confirmWinner.Dock=DockStyle.Fill;finish.Controls.Add(confirmWinner);footer.Controls.Add(finish);confirmWinner.Click+=(_,_)=>ConfirmWinner();
        notice.AutoSize=false;notice.Dock=DockStyle.Fill;notice.AutoEllipsis=true;notice.MaximumSize=Size.Empty;canvas.Controls.Add(notice,0,5);canvas.SetColumnSpan(notice,2);
        var guide=new TabPage(L.T("Configurar lobby"));tabs.TabPages.Add(guide);guide.Controls.Add(new LobbyGuidePanel());
        Controls.Add(new NavigationBar(tabs));
        rounds.Columns.Add(new DataGridViewTextBoxColumn(){Name=L.T("Round"),ReadOnly=true,FillWeight=45,MinimumWidth=46});
        rounds.Columns.Add(new DataGridViewTextBoxColumn(){Name=L.T("Ataque"),ReadOnly=true,FillWeight=90});rounds.Columns.Add(new DataGridViewTextBoxColumn(){Name=L.T("Defesa"),ReadOnly=true,FillWeight=90});
        rounds.Columns.Add(new DataGridViewComboBoxColumn(){Name="Bomb site",FillWeight=180,FlatStyle=FlatStyle.Standard});
        rounds.Columns.Add(new DataGridViewComboBoxColumn(){Name=L.T("Vencedor"),FillWeight=100,FlatStyle=FlatStyle.Standard});
        rounds.Columns.Add(new DataGridViewTextBoxColumn(){Name=L.T("Placar"),ReadOnly=true,FillWeight=50});
        rounds.Columns.Add(new DataGridViewComboBoxColumn(){Name=L.T("Finalização"),FillWeight=110,FlatStyle=FlatStyle.Standard});
        rounds.CurrentCellDirtyStateChanged+=(_,_)=>{if(rounds.IsCurrentCellDirty)rounds.CommitEdit(DataGridViewDataErrorContexts.Commit);};
        rounds.CellValueChanged+=RoundChanged;
        rounds.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleCenter;rounds.ColumnHeadersDefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleCenter;rounds.CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal;rounds.BorderStyle=BorderStyle.None;rounds.CellPainting+=PaintRoundCell;
        rounds.DataError+=(_,e)=>{e.ThrowException=false;Warn(L.T("Seleção inválida. Confira o campo."));};
        saved.SelectedIndexChanged+=(_,_)=>{if(!loading && saved.SelectedItem is SiegeMatch selected){BeginInvoke((Action)(()=>{if(IsDisposed)return;try{FlushNames();match=selected;current=null;LoadMatch();}catch(Exception ex){Warn(ex.Message);loading=true;saved.SelectedItem=match;loading=false;}}));}};
        teamA.TextChanged+=(_,_)=>NameChanged();teamB.TextChanged+=(_,_)=>NameChanged();
        nameSave.Tick+=(_,_)=>{nameSave.Stop();if(Persist())namesDirty=false;RefreshMatch();};
        format.SelectedIndexChanged+=(_,_)=>ChangeSetup("format");coin.SelectedIndexChanged+=(_,_)=>ChangeSetup("coin");first.SelectedIndexChanged+=(_,_)=>ChangeSetup("first");
        mapSelect.SelectedIndexChanged+=(_,_)=>{if(!loading){current=match.Maps.ElementAtOrDefault(mapSelect.SelectedIndex);correctBans=false;RefreshMatch();}};
        Directory.CreateDirectory(MatchStore.Folder);
        var recovered=new List<string>();
        foreach(string path in Directory.GetFiles(MatchStore.Folder,"*.json").OrderByDescending(File.GetLastWriteTimeUtc))
        {
            try{matches.Add(MatchStore.Read(path,catalog));}
            catch{try{matches.Add(MatchStore.Read(path+".bak",catalog));recovered.Add(L.T("Recuperada cópia anterior de ")+Path.GetFileName(path));}catch{recovered.Add(L.T("Não foi possível abrir ")+Path.GetFileName(path));}}
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
        LoadMatch();AdjustLayout(); if(recovered.Count>0)Warn(string.Join("; ",recovered));
    }
    void PaintRoundCell(object? sender,DataGridViewCellPaintingEventArgs e)
    {
        if(e.Graphics is null || e.RowIndex<0 || e.ColumnIndex<0 || rounds.Columns[e.ColumnIndex] is not DataGridViewComboBoxColumn)return;
        e.Paint(e.ClipBounds,DataGridViewPaintParts.Background|DataGridViewPaintParts.SelectionBackground|DataGridViewPaintParts.Border);
        var bounds=e.CellBounds;bool editable=!rounds[e.ColumnIndex,e.RowIndex].ReadOnly && !rounds.ReadOnly;
        int arrow=editable?22:0;
        Color ink=(e.State&DataGridViewElementStates.Selected)!=0?e.CellStyle!.SelectionForeColor:e.CellStyle!.ForeColor;
        TextRenderer.DrawText(e.Graphics,Convert.ToString(e.FormattedValue)??"",e.CellStyle.Font,new Rectangle(bounds.X+3,bounds.Y,bounds.Width-arrow-6,bounds.Height),ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        if(editable){using var pen=new Pen(ink,1.6f);int x=bounds.Right-12,y=bounds.Y+bounds.Height/2;e.Graphics.DrawLines(pen,new[]{new Point(x-4,y-2),new Point(x,y+2),new Point(x+4,y-2)});}
        e.Handled=true;
    }
    Control TeamField(TextBox name,Color color,int team)
    {
        var row=new BufferedTable(){Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Margin=Padding.Empty};
        row.ColumnStyles.Add(new(SizeType.Absolute,25));row.ColumnStyles.Add(new(SizeType.Percent,100));
        row.Controls.Add(new Panel(){BackColor=color,Size=new Size(17,17),Margin=new Padding(3,5,3,0),AccessibleName=L.T("Cor fixa da equipe")},0,0);
        row.ColumnStyles.Add(new(SizeType.Absolute,58));
        name.Dock=DockStyle.Fill;row.Controls.Add(name,1,0);var logo=Btn(L.T("Logo"),()=>ChooseTeamLogo(team));logo.Dock=DockStyle.Fill;logo.Margin=Padding.Empty;row.Controls.Add(logo,2,0);return row;
    }
    void ChooseTeamLogo(int team)
    {
        using var dialog=new OpenFileDialog{Filter=L.T("Imagem PNG ou JPEG|*.png;*.jpg;*.jpeg")};if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        try{FlushNames();var brand=MatchFiles.ReadBrand(match);var path=MatchFiles.Resource(match,team==0?"logo-a":"logo-b",dialog.FileName);if(team==0)brand.TeamALogo=path;else brand.TeamBLogo=path;MatchFiles.SaveBrand(match,brand);notice.Text=L.T("Logo salva para as artes desta partida.");}
        catch(Exception ex){Warn(ex.Message);}
    }
    public bool ApplyArtworkVeto(string id,ImageBrand brand)
    {
        var target=matches.Single(m=>m.Id==id);var copy=MatchFiles.ArtworkMatch(target,catalog,brand);
        if(target.Maps.Any(m=>m.HasData||m.Confirmed))throw new InvalidOperationException(L.T("A partida já tem bans ou rounds registrados. Mantenha esta correção somente na arte."));
        MatchStore.Validate(copy,catalog);var oldVeto=target.Veto;var oldMaps=target.Maps;
        try{target.Veto=copy.Veto;target.Maps=copy.Maps;MatchStore.Save(target);}
        catch{target.Veto=oldVeto;target.Maps=oldMaps;throw;}
        if(target==match){current=null;LoadMatch();}return true;
    }
    static ComboBox Combo()=>new ThemeCombo(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill};
    static Label Caption(string text)=>new ThemeCaption{Text=text,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft};
    static Label Label(string text)=>new(){Text=text,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};
    static Button Btn(string text,Action action){var b=new ThemeButton(){Text=text,AutoSize=true,Height=29};b.Click+=(_,_)=>action();return b;}
    static TableLayoutPanel Table(int columns,int rows){var t=new BufferedTable(){Dock=DockStyle.Fill,ColumnCount=columns,RowCount=rows};for(int i=0;i<columns;i++)t.ColumnStyles.Add(new(SizeType.Percent,100f/columns));for(int i=0;i<rows;i++)t.RowStyles.Add(new(SizeType.Percent,100f/rows));return t;}
    static GroupBox Group(string title,TableLayoutPanel parent,int col,int row)
    {
        var g=new ThemeGroup(){Text=title.ToUpperInvariant(),Dock=DockStyle.Fill,Padding=new Padding(7),Font=new Font("Segoe UI",9,FontStyle.Bold)};
        g.ControlAdded+=(_,e)=>{if(e.Control is not null)e.Control.Font=AppFonts.Body(10);};
        parent.Controls.Add(g,col,row);return g;
    }
    static void Clear(Control c){foreach(Control child in c.Controls.Cast<Control>().ToArray()){c.Controls.Remove(child);child.Dispose();}}
    void Warn(string message){notice.Text=message;notice.ForeColor=AppTheme.Error;}
    bool Confirm(string message)=>MessageBox.Show(this,message,L.T("Revisar partida"),MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes;
    bool IsLocked => current?.Confirmed==true || match.Complete;
    public void SetQuickMode(bool enabled)
    {
        if(quickMode==enabled)return;quickMode=enabled;
        using var update=new UiUpdate(this);
        rounds.Columns[1].Visible=!enabled;rounds.Columns[2].Visible=!enabled;rounds.Columns[5].Visible=!enabled;
        AdjustLayout();
        roundsKey="";RefreshMatch();
    }
    void AdjustLayout()
    {
        if(adjustingLayout || leftLayout is null || sidesViewport is null || actionButtons is null)return;
        adjustingLayout=true;
        try
        {
            float scale=DeviceDpi/96f;int h=mainCanvas.ClientSize.Height;
            bool small=h<700*scale;
            var scoreFont=small?CompactScoreFont:ScoreFont;if(score.Font!=scoreFont)score.Font=scoreFont;
            float desired=(240+Math.Max(1,match.Maps.Count)*40)*scale;
            float upper=small?Math.Max(170*scale,Math.Min(desired,h-380*scale)):Math.Max(200*scale,Math.Min(desired,h-360*scale));
            if(mainCanvas.RowStyles[1].Height!=upper)mainCanvas.RowStyles[1].Height=upper;
            foreach(var pair in new[]{(0,small?32:38),(2,small?38:48),(4,small?36:42),(5,small?22:30)})if(mainCanvas.RowStyles[pair.Item1].Height!=pair.Item2*scale)mainCanvas.RowStyles[pair.Item1].Height=pair.Item2*scale;
            if(compactSetup.Visible!=small)compactSetup.Visible=small;
            compactSetup.Text=showCompactSides?L.T("Voltar ao confronto"):L.T("Configurar lados dos mapas");
            for(int row=0;row<2;row++)
            {
                bool show=!small || (row==1)==showCompactSides;var group=leftLayout.GetControlFromPosition(0,row)!;
                if(group.Visible!=show)group.Visible=show;
                var rowType=small?(show?SizeType.Percent:SizeType.Absolute):(row==0?SizeType.Absolute:SizeType.Percent);
                float rowHeight=small?(show?100:0):(row==0?155*scale:100);
                if(leftLayout.RowStyles[row].SizeType!=rowType)leftLayout.RowStyles[row].SizeType=rowType;if(leftLayout.RowStyles[row].Height!=rowHeight)leftLayout.RowStyles[row].Height=rowHeight;
            }
            int sideHeight=(int)((23+Math.Max(1,match.Maps.Count)*40)*scale);
            if(sideRows.Height!=sideHeight)sideRows.Height=sideHeight;
            bool entry=quickMode && (current is null || current.Score().Winner<0);
            if(quickRound.Visible!=entry)quickRound.Visible=entry;
            var type=entry?SizeType.Percent:SizeType.Absolute;float width=entry?46:0;
            if(roundHost.ColumnStyles[0].SizeType!=type)roundHost.ColumnStyles[0].SizeType=type;
            if(roundHost.ColumnStyles[0].Width!=width)roundHost.ColumnStyles[0].Width=width;
            float firstWidth=entry?46:25,secondWidth=entry?54:50;if(scoreboard.ColumnStyles[0].Width!=firstWidth)scoreboard.ColumnStyles[0].Width=firstWidth;if(scoreboard.ColumnStyles[1].Width!=secondWidth)scoreboard.ColumnStyles[1].Width=secondWidth;if(scoreboard.ColumnStyles[2].Width!=(entry?0:25))scoreboard.ColumnStyles[2].Width=entry?0:25;
            float other=entry?54:100;if(roundHost.ColumnStyles[1].Width!=other)roundHost.ColumnStyles[1].Width=other;
            int active=current is null || current.InitialAttack<0?0:current.ActiveBanHalf(Math.Min(15,current.Rounds.Count+(current.Score().Winner>=0?0:1)));
            if(previousBanHalf!=active){visibleBanHalf=active;previousBanHalf=active;}
            bool compact=h-upper-158*scale<340*scale;
            for(int half=0;half<2;half++)
            {
                var row=bans.RowStyles[half*2+1];bool show=!compact || half==visibleBanHalf;
                if(bans.GetControlFromPosition(0,half*2) is Label heading && heading.Tag is string caption){string text=caption+(compact?(show?" ▾":" ▸"):"");if(heading.Text!=text)heading.Text=text;}
                var rowType=show?SizeType.Percent:SizeType.Absolute;float rowHeight=show?50:0;
                if(row.SizeType!=rowType)row.SizeType=rowType;if(row.Height!=rowHeight)row.Height=rowHeight;
                for(int slot=0;slot<6;slot++){var card=bans.GetControlFromPosition(slot,half*2+1);if(card is not null && card.Visible!=show)card.Visible=show;}
            }
        }
        finally{adjustingLayout=false;}
    }
    bool RecordQuickRound(string site,int winner,string ending)
    {
        if(current is null || IsLocked || current.InitialAttack<0 || current.Score().Winner>=0 || current.Rounds.Count>=12 && current.OtAttack<0)return false;
        if(winner is not (0 or 1) || !catalog.Maps.Single(m=>m.Id==current.MapId).Sites.Contains(site) || string.IsNullOrEmpty(ending) || !RoundEnding.Allowed(ending,winner,current.Attacker(current.Rounds.Count+1)))return false;
        if(current.SiteBlocked(site,current.Rounds.Count+1)){Warn(L.T("Bombsite bloqueado por vitória da defesa. Escolha outro local."));return false;}
        current.Rounds.Add(new(){Site=site,Winner=winner,Ending=ending});
        if(!Persist()){current.Rounds.RemoveAt(current.Rounds.Count-1);return false;}
        RefreshMatch();QueueBanReminder(current);return true;
    }
    void NameChanged(){if(loading || match.HasConfirmed)return;if(teamA.Text!=match.TeamA)match.TeamA=teamA.Text;if(teamB.Text!=match.TeamB)match.TeamB=teamB.Text;namesDirty=true;nameSave.Stop();nameSave.Start();}
    public void FlushNames(){if(!namesDirty)return;if(!Persist())throw new IOException(L.T("Não foi possível salvar os nomes das equipes. Tente novamente antes de continuar."));namesDirty=false;nameSave.Stop();}
    bool Persist(){try{MatchStore.Save(match);notice.Text=L.T("Salvo automaticamente • ")+DateTime.Now.ToString("HH:mm:ss");notice.ForeColor=AppTheme.Success;return true;}catch(Exception ex){Warn(L.T("Falha ao salvar: ")+ex.Message);return false;}}
    void LoadMatch()
    {
        visibleBanHalf=-1;previousBanHalf=-1;
        correctBans=false;loading=true;saved.Items.Clear();saved.Items.AddRange(matches.Cast<object>().ToArray());saved.SelectedItem=match;teamA.Text=match.TeamA;teamB.Text=match.TeamB;format.SelectedItem=L.FormatName(match.Format);loading=false;RefreshMatch();
    }
    void ChangeSetup(string field)
    {
        if(loading || match.HasConfirmed)return;
        if(field=="coin"){int previous=match.CoinWinner;match.CoinWinner=coin.SelectedIndex;if(!Persist())match.CoinWinner=previous;RefreshMatch();return;}
        if(match.Veto.Any(x=>!string.IsNullOrEmpty(x)) && !Confirm(L.T("Alterar o formato ou primeiro ban limpa o veto e os registros desta partida. Continuar?"))){LoadMatch();return;}
        string oldFormat=match.Format;int oldFirst=match.FirstBan;var oldVeto=match.Veto;var oldMaps=match.Maps;var oldCurrent=current;
        if(field=="format")match.Format=format.SelectedIndex switch { 2 => "MD5", 1 => "MD3", _ => "MD1" };else match.FirstBan=first.SelectedIndex;
        match.Veto=new string[8];match.Maps=new();current=null;if(!Persist()){match.Format=oldFormat;match.FirstBan=oldFirst;match.Veto=oldVeto;match.Maps=oldMaps;current=oldCurrent;}LoadMatch();
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
        hints.SetToolTip(saved,L.T("ID da partida: ")+match.Id);
        UpdateItems(coin,[match.TeamA,match.TeamB]);UpdateItems(first,[match.TeamA,match.TeamB]);coin.SelectedIndex=match.CoinWinner;first.SelectedIndex=match.FirstBan;
        string vk=JsonSerializer.Serialize(new{match.Id,match.TeamA,match.TeamB,match.Format,match.FirstBan,match.CoinWinner,match.Complete,match.Veto});
        if(vk!=vetoKey){vetoKey=vk;using var vetoUpdate=new UiUpdate(veto,true);Clear(veto);
        int nextStep=Array.FindIndex(match.Veto,string.IsNullOrEmpty);
        for(int i=0;i<9;i++)
        {
            int step=i;string? id=i<8?match.Veto[i]:match.Veto.All(x=>!string.IsNullOrEmpty(x))?catalog.Maps.First(x=>!match.Veto.Contains(x.Id)).Id:null;
            var map=catalog.Maps.FirstOrDefault(x=>x.Id==id);
            bool enabled=!match.Complete && i<8 && match.FirstBan>=0 && match.CoinWinner>=0 && (i==0 || !string.IsNullOrEmpty(match.Veto[i-1]));
            bool next=enabled && i==nextStep;
            string caption=$"{i+1:00} · {(i==8?"DECIDER":match.Action(i)+" · "+(match.FirstBan<0?"—":match.Team(match.Actor(i))))}";
            var cell=new MapVetoCard{Thumbnail=map is null?null:Picture(i<8 && match.Action(i)=="BAN"?map.UnavailableImage:map.Image),Caption=caption,MapName=map?.DisplayName.ToUpperInvariant()??(next?L.T("Continue por aqui"):L.T("Selecionar mapa…")),Next=next,Banned=i<8 && match.Action(i)=="BAN",Enabled=enabled,AccessibleName=caption+" · "+(map?.DisplayName??L.T("Selecionar mapa…"))};
            cell.Click+=(_,_)=>PickMap(step);hints.SetToolTip(cell,cell.AccessibleName);veto.Controls.Add(cell,i%3,i/3);
        }
        AppTheme.Apply(veto);}
        string sk=JsonSerializer.Serialize(new{match.Id,match.TeamA,match.TeamB,match.Complete,Maps=match.Maps.Select(m=>new{m.MapId,m.Picker,m.InitialAttack,m.OtAttack,m.Confirmed})});
        if(sk!=sidesKey){sidesKey=sk;using var sideUpdate=new UiUpdate(sideRows,true);Clear(sideRows);sideRows.RowStyles.Clear();sideRows.RowCount=Math.Max(1,match.Maps.Count+1);sideRows.RowStyles.Add(new(SizeType.Absolute,23));for(int i=1;i<sideRows.RowCount;i++)sideRows.RowStyles.Add(new(SizeType.Absolute,40));
        TableLayoutPanel SideLine(){var line=Table(4,1);line.ColumnStyles.Clear();foreach(float width in new[]{25,21,27,27})line.ColumnStyles.Add(new(SizeType.Percent,width));return line;}
        var headings=SideLine();foreach(var pair in new[]{(L.T("Mapa"),0),(L.T("Time"),1),(L.T("Lado inicial"),2),("OT",3)})headings.Controls.Add(Label(pair.Item1),pair.Item2,0);sideRows.Controls.Add(headings,0,0);
        for(int i=0;i<match.Maps.Count;i++)
        {
            var m=match.Maps[i];var row=SideLine();var name=catalog.Maps.Single(x=>x.Id==m.MapId).DisplayName;var initial=Combo();var ot=Combo();
            initial.Items.AddRange([match.Team(1-m.Picker)+L.T(": ATQ"),match.Team(1-m.Picker)+L.T(": DEF")]);ot.Items.AddRange([match.Team(m.Picker)+L.T(": ATQ"),match.Team(m.Picker)+L.T(": DEF")]);
            initial.SelectedIndex=m.InitialAttack<0?-1:m.InitialAttack==1-m.Picker?0:1;ot.SelectedIndex=m.OtAttack<0?-1:m.OtAttack==m.Picker?0:1;
            initial.Enabled=ot.Enabled=!m.Confirmed && !match.Complete;
            var mapField=new ReadOnlySideField(){Text=name,Height=initial.Height};var teamField=new ReadOnlySideField(){Text=match.Team(m.Picker),Height=initial.Height};
            initial.SizeChanged+=(_,_)=>{mapField.Height=teamField.Height=initial.Height;};
            hints.SetToolTip(mapField,name);hints.SetToolTip(teamField,match.Team(m.Picker));
            foreach(Control field in new Control[]{mapField,teamField,initial,ot}){field.Dock=DockStyle.None;field.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;field.Margin=new Padding(3,4,3,3);}row.Controls.Add(mapField,0,0);row.Controls.Add(teamField,1,0);row.Controls.Add(initial,2,0);row.Controls.Add(ot,3,0);sideRows.Controls.Add(row,0,i+1);
            initial.SelectedIndexChanged+=(_,_)=>{if(!loading)SetSide(m,false,initial.SelectedIndex==0?1-m.Picker:m.Picker);};ot.SelectedIndexChanged+=(_,_)=>{if(!loading)SetSide(m,true,ot.SelectedIndex==0?m.Picker:1-m.Picker);};
        }
        AppTheme.Apply(sideRows);}
        if(current is null || !match.Maps.Contains(current))current=match.Maps.FirstOrDefault();
        UpdateItems(mapSelect,match.Maps.Select((m,i)=>L.F($"Mapa {i+1} — {catalog.Maps.Single(x=>x.Id==m.MapId).DisplayName}")).ToArray());mapSelect.SelectedIndex=current is null?-1:match.Maps.IndexOf(current);
        var s=current?.Score();score.Text=$"{match.TeamA}   {s?.A??0} × {s?.B??0}   {match.TeamB}";
        confirmWinner.Enabled=current is not null && s?.Winner>=0 && !IsLocked;
        confirmWinner.Text=current?.Confirmed==true?L.T("Vencedor confirmado"):s?.Winner>=0?L.T("Confirmar vitória: ")+match.Team(s.Value.Winner):L.T("Confirmar vencedor");
        string bk=JsonSerializer.Serialize(new{match.Id,match.TeamA,match.TeamB,Locked=IsLocked,correctBans,current?.MapId,current?.InitialAttack,current?.OtAttack,current?.Bans,Reached=Enumerable.Range(0,12).Select(i=>current?.BanStageReached(i)),Half=current is null || current.InitialAttack<0?-1:current.ActiveBanHalf(Math.Min(15,current.Rounds.Count+(current.Score().Winner>=0?0:1)))});
        if(bk!=bansKey){bansKey=bk;using var banUpdate=new UiUpdate(bans,true);RenderBans();AppTheme.Apply(bans);}
        string rk=JsonSerializer.Serialize(new{match.Id,match.TeamA,match.TeamB,Locked=IsLocked,current?.MapId,current?.InitialAttack,current?.OtAttack,current?.Rounds});
        if(rk!=roundsKey){roundsKey=rk;RenderRounds();AppTheme.ApplyGrid(rounds);}
        if(quickMode)quickRound.UpdateMatch(match,current,catalog,IsLocked);
        AdjustLayout();
        AppTheme.Apply(this);
        correction.Enabled=current is not null && current.InitialAttack>=0 && (!match.Complete || current.Rounds.Count>0);
        correction.Text=correctBans?L.T("Concluir correção"):L.T("Corrigir bans");
        if(correctBans){Warn(L.T("Corrigindo apenas bans de operadores. O resultado permanece protegido."));return;}
        if(IsLocked){notice.Text=match.Complete?L.T("Série concluída. Resultados confirmados e edição bloqueada."):L.T("Vencedor confirmado. Este mapa está bloqueado; você pode continuar nos demais mapas.");notice.ForeColor=AppTheme.Success;return;}
        if(current is not null && current.InitialAttack>=0)
        {
            int pending=Enumerable.Range(0,12).Count(i=>current.BanStageReached(i) && string.IsNullOrEmpty(current.Bans[i]));
            if(pending>0)Warn(L.F($"Atenção: {pending} ban(s) pendente(s), marcados com !. ")+(s?.Winner>=0?L.T("Confira os rounds e confirme o vencedor."):L.T("Você pode continuar registrando os rounds.")));
            else if(s?.Winner>=0)Warn(L.T("Vencedor definido. Confira os rounds e confirme o vencedor."));
            if(current.Rounds.Count==12 && s?.Winner<0 && current.OtAttack<0)Warn(L.T("Defina o lado do overtime para liberar o round 13. ")+(pending>0?L.F($"Há {pending} ban(s) pendente(s)."):""));
        }
        else if(current is not null)Warn(L.T("Defina o lado inicial para liberar os bans e os rounds deste mapa."));
        }finally{loading=false;}
    }
    internal bool MapAvailable(SiegeMap map,int step)=>match.Veto[step]==map.Id || !match.Veto.Contains(map.Id);
    ThemeButton MapChoiceButton(SiegeMap map,int step)
    {
        bool available=MapAvailable(map,step);
        return new ThemeButton{Dock=DockStyle.Fill,Text=map.DisplayName+(available?"":"\n"+L.T("INDISPONÍVEL")),Image=Picture(available?map.Image:map.UnavailableImage) is Image original?new Bitmap(original,new Size(155,75)):null,TextImageRelation=TextImageRelation.ImageAboveText,Enabled=available};
    }
    bool CanEditBan(int slot)=>current is not null && slot is >=0 and <12 && current.BanStageReached(slot) && (!IsLocked || correctBans) && (!match.Complete || current.Rounds.Count>0);
    bool SetOperator(int slot,string selected)
    {
        if(!CanEditBan(slot))return false;
        if(!catalog.Operators.Any(o=>o.Id==selected && o.Role==current!.BanRole(slot)))return false;
        if(Enumerable.Range(slot/6*6,6).Any(i=>i!=slot && current!.Bans[i]==selected))return false;
        string? previous=current!.Bans[slot];current.Bans[slot]=selected;if(!Persist()){current.Bans[slot]=previous;RefreshMatch();return false;}RefreshMatch();return true;
    }
    void PickMap(int step)
    {
        if(match.Complete)return;
        using var dialog=new Form(){Text=$"{step+1}. {match.Action(step)} — {match.Team(match.Actor(step))}",Size=new Size(720,450),StartPosition=FormStartPosition.CenterParent,MinimizeBox=false,MaximizeBox=false};
        var grid=Table(3,3);dialog.Controls.Add(grid);string? chosen=null;
        int n=0;foreach(var map in catalog.Maps)
        {
            var button=MapChoiceButton(map,step);
            button.Disposed+=(_,_)=>button.Image?.Dispose();
            button.Click+=(_,_)=>{chosen=map.Id;dialog.DialogResult=DialogResult.OK;};grid.Controls.Add(button,n%3,n/3);n++;
        }
        AppTheme.Apply(dialog);if(dialog.ShowDialog(this)!=DialogResult.OK || chosen is null || chosen==match.Veto[step])return;
        var backup=match.Veto[step];var oldMaps=match.Maps.ToList();match.Veto[step]=chosen;match.SyncMaps(catalog);
        if(oldMaps.Any(m=>m.Confirmed && !match.Maps.Contains(m))){match.Veto[step]=backup;match.Maps=oldMaps;Warn(L.T("Esta escolha mudaria um mapa confirmado. Alteração bloqueada."));return;}
        if(oldMaps.Any(m=>m.HasData && !match.Maps.Contains(m)) && !Confirm(L.T("Esta troca muda um mapa com bans ou rounds registrados. Limpar apenas os registros do mapa substituído?"))){match.Veto[step]=backup;match.Maps=oldMaps;return;}
        if(!Persist()){match.Veto[step]=backup;match.Maps=oldMaps;}RefreshMatch();
    }
    void SetSide(MapRecord m,bool overtime,int attacker)
    {
        AppLog.Write($"SET SIDE match={match.Id} map={m.MapId} overtime={overtime} attacker={attacker}");
        if(m.Confirmed || match.Complete)return;
        if(m.HasData && !Confirm(overtime?L.T("Recalcular os lados do overtime com esta escolha?"):L.T("Alterar o lado inicial recalcula ataque/defesa dos rounds e limpa os bans deste mapa, pois mudam os tipos permitidos. Continuar?"))){RefreshMatch();return;}
        int oldInitial=m.InitialAttack,oldOt=m.OtAttack;var oldBans=m.Bans.ToArray();var oldEndings=m.Rounds.Select(r=>r.Ending).ToArray();
        if(overtime)m.OtAttack=attacker;else{m.InitialAttack=attacker;m.Bans=new string[12];}
        for(int i=0;i<m.Rounds.Count;i++)if(!RoundEnding.Allowed(m.Rounds[i].Ending,m.Rounds[i].Winner,m.Attacker(i+1)))m.Rounds[i].Ending="";
        if(!Persist()){m.InitialAttack=oldInitial;m.OtAttack=oldOt;m.Bans=oldBans;for(int i=0;i<m.Rounds.Count;i++)m.Rounds[i].Ending=oldEndings[i];}RefreshMatch();
    }
    void RenderBans()
    {
        Clear(bans);
        for(int half=0;half<2;half++)
        {
            int round=current is null?1:Math.Min(15,current.Rounds.Count+(current.Score().Winner>=0?0:1));
            bool active=current is not null && current.InitialAttack>=0 && (round<=12 || current.OtAttack>=0) && current.ActiveBanHalf(round)==half;
            string sides=current is null || current.InitialAttack<0?L.T("Defina o lado inicial"):L.F($"{match.Team(half==0?current.InitialAttack:1-current.InitialAttack)} {L.Role("ATQ")} / {match.Team(half==0?1-current.InitialAttack:current.InitialAttack)} {L.Role("DEF")}");
            var pending=current?.PendingBans(half)??[];
            var header=new BanHalfHeading{Active=active,Text=$"{L.T("Round")} {(half==0?"1–6":"7–12")} • {sides}{(correctBans?L.T(" • Corrigindo"):"")}",Dock=DockStyle.Fill};header.Font=SmallFont;header.BackColor=active?Color.FromArgb(220,235,250):Color.WhiteSmoke;
            if(pending.Length>0)
            {
                int nextRound=MapRecord.BanThreshold(pending.Last())+1;
                header.Text=L.F($"! {pending.Length} BAN(S) PENDENTE(S) • ANTES DO ROUND {nextRound:00}");
                header.ForeColor=Color.Firebrick;header.BackColor=Color.FromArgb(250,215,215);
                hints.SetToolTip(header,L.T("Selecione os operadores nos campos marcados com !. Você pode continuar registrando os rounds."));
            }
            header.Tag=header.Text;int selectedHalf=half;header.Cursor=Cursors.Hand;header.TabStop=true;
            void ShowHalf(){visibleBanHalf=selectedHalf;AdjustLayout();}
            header.Click+=(_,_)=>ShowHalf();header.KeyDown+=(_,e)=>{if(e.KeyCode is Keys.Space or Keys.Enter){ShowHalf();e.Handled=true;}};
            hints.SetToolTip(header,header.Text+"\n"+L.T("Clique para visualizar esta metade."));
            bans.Controls.Add(header,0,half*2);bans.SetColumnSpan(header,6);
            for(int local=0;local<6;local++)
            {
                int slot=BanDisplayOrder(half)[local];var op=catalog.Operators.FirstOrDefault(x=>x.Id==current?.Bans[slot]);bool unlocked=current is not null && current.BanStageReached(slot);
                string name=op?.Name.ToUpperInvariant() ?? (unlocked?L.T("! Pendente"):L.T("Bloqueado"));
                string team=match.Team(MapRecord.BanTeam(slot));
                var card=new OperatorBanCard{Portrait=op is null?null:Picture(op.Id+"-portrait.png"),OperatorIcon=op is null?null:Picture(op.Icon),OperatorName=name,TeamName=team,Stage=$"R{MapRecord.BanThreshold(slot)+1:00}",Placeholder=current is null || current.InitialAttack<0?"—":L.Role(current.BanRole(slot)),Pending=op is null && unlocked,TeamColor=MapRecord.BanTeam(slot)==0?Color.FromArgb(64,143,179):Color.FromArgb(255,119,55),Enabled=CanEditBan(slot) && (op is null || correctBans),AccessibleName=$"{name} • {team} • R{MapRecord.BanThreshold(slot)+1:00}"};
                card.Click+=(_,_)=>PickOperator(slot);
                hints.SetToolTip(card,card.AccessibleName+"\n"+(unlocked?L.T("Selecionar ban de ")+L.Role(current!.BanRole(slot)):L.T("Libera após o round ")+MapRecord.BanThreshold(slot)));
                bans.Controls.Add(card,local,half*2+1);
            }
        }
    }
    int[] BanDisplayOrder(int half)
    {
        int first=half*6;
        if(current is null || current.InitialAttack<0)return Enumerable.Range(first,6).ToArray();
        var initial=Enumerable.Range(first,4).ToArray();
        var defense=initial.Where(i=>current.BanRole(i)=="DEF").ToArray();
        var attack=initial.Where(i=>current.BanRole(i)=="ATQ").ToArray();
        var extra=Enumerable.Range(first+4,2).OrderBy(i=>current.BanRole(i)=="DEF"?0:1);
        return new[]{defense[0],attack[0],defense[1],attack[1]}.Concat(extra).ToArray();
    }
    void PickOperator(int slot)
    {
        if(current is null || !CanEditBan(slot))return;
        bool editing=!string.IsNullOrEmpty(current.Bans[slot]);
        while(current is not null && CanEditBan(slot))
        {
            string role=current.BanRole(slot);
            var available=OperatorOrder.Sort(catalog.Operators,role).ToArray();
            var area=Screen.FromControl(this).WorkingArea;
            int width=Math.Min(1180,area.Width-60),columns=Math.Min(11,Math.Max(5,(width-24)/90));
            int rows=(available.Length+columns-1)/columns;
            int cellHeight=Math.Min(94,Math.Max(58,(area.Height-150)/Math.Max(1,rows)));
            using var dialog=new Form(){Text=L.F($"{match.Team(MapRecord.BanTeam(slot))} bane {L.Role(role)}"),ClientSize=new Size(width,rows*(cellHeight+6)+65),StartPosition=FormStartPosition.CenterParent,MinimizeBox=false,MaximizeBox=false};
            var search=new TextBox(){Dock=DockStyle.Top,PlaceholderText=L.T("Buscar operador…")};
            var flow=new FlowLayoutPanel(){Dock=DockStyle.Fill,Padding=new Padding(6),AutoScroll=false};
            dialog.Controls.Add(flow);dialog.Controls.Add(search);string? selected=null;
            void Fill(){Clear(flow);foreach(var op in available.Where(o=>o.Name.Contains(search.Text,StringComparison.OrdinalIgnoreCase)))
            {
                bool used=current.Bans.Skip(slot/6*6).Take(6).Contains(op.Id) && current.Bans[slot]!=op.Id;
                var b=new ThemeButton(){Text=op.Name,Width=(width-24)/columns-6,Height=cellHeight,Margin=new Padding(3),TextImageRelation=TextImageRelation.ImageAboveText,Image=Picture(op.Icon) is Image img?new Bitmap(img,new Size(cellHeight<80?32:42,cellHeight<80?32:42)):null,Enabled=!used};
                b.Disposed+=(_,_)=>b.Image?.Dispose();b.Click+=(_,_)=>{selected=op.Id;dialog.DialogResult=DialogResult.OK;};flow.Controls.Add(b);
            }}
            search.TextChanged+=(_,_)=>Fill();Fill();AppTheme.Apply(dialog);
            if(OperatorPickerPreview is not null)dialog.Shown+=(_,_)=>OperatorPickerPreview(dialog,slot);
            if(dialog.ShowDialog(this)!=DialogResult.OK || selected is null || !SetOperator(slot,selected))return;
            if(editing || correctBans)return;
            int next=BanDisplayOrder(slot/6).FirstOrDefault(i=>MapRecord.BanThreshold(i)==MapRecord.BanThreshold(slot) && current.BanStageReached(i) && string.IsNullOrEmpty(current.Bans[i]),-1);
            if(next<0)return;slot=next;
        }
    }
    void RenderRounds()
    {
        if(current is null){rounds.Rows.Clear();return;}


        var mapSites=catalog.Maps.Single(x=>x.Id==current.MapId).Sites;
        var sites=(DataGridViewComboBoxColumn)rounds.Columns[3];UpdateGridItems(sites,new[]{L.T("Selecionar…")}.Concat(mapSites.Select(site=>BombsiteNames.Display(current.MapId,site))).ToArray());
        var winners=(DataGridViewComboBoxColumn)rounds.Columns[4];UpdateGridItems(winners,[L.T("Selecionar…"),"A • "+match.TeamA,"B • "+match.TeamB]);
        UpdateGridItems((DataGridViewComboBoxColumn)rounds.Columns[6],RoundEnding.Codes.Select(RoundEnding.Label).ToArray());
        int a=0,b=0;bool ended=current.Score().Winner>=0;
        int count=current.Rounds.Count+(ended || quickMode?0:1);
        while(rounds.Rows.Count>count)rounds.Rows.RemoveAt(rounds.Rows.Count-1);
        while(rounds.Rows.Count<count)rounds.Rows.Add();
        for(int i=0;i<count;i++)
        {
            var r=current.Rounds.ElementAtOrDefault(i);if(r is not null){if(r.Winner==0)a++;else b++;}
            bool ready=current.InitialAttack>=0 && (i<12 || current.OtAttack>=0);int attacker=ready?current.Attacker(i+1):-1;
            var siteCell=(DataGridViewComboBoxCell)rounds.Rows[i].Cells[3];
            string[] choices=new[]{L.T("Selecionar…")}.Concat(mapSites.Where(site=>!current.SiteBlocked(site,i+1) || r?.Site==site).Select(site=>BombsiteNames.Display(current.MapId,site))).ToArray();
            if(!siteCell.Items.Cast<string>().SequenceEqual(choices)){siteCell.Items.Clear();siteCell.Items.AddRange(choices);}
            bool conflict=r is not null && current.SiteBlocked(r.Site,i+1);
            siteCell.ErrorText=conflict?L.T("Bombsite em conflito. Revise este round após a correção do histórico."):"";
            siteCell.ToolTipText=(r is null?"":L.T(r.Site))+(conflict?"\n"+siteCell.ErrorText:"");
            int row=i;object[] values=[i+1,attacker<0?"—":match.Team(attacker),attacker<0?"—":match.Team(1-attacker),string.IsNullOrEmpty(r?.Site)?L.T("Selecionar…"):BombsiteNames.Display(current.MapId,r.Site),r is null?L.T("Selecionar…"):(r.Winner==0?"A • ":"B • ")+match.Team(r.Winner),r is null?"—":$"{a}–{b}",RoundEnding.Label(r?.Ending??"")];
            for(int col=0;col<values.Length;col++)if(!Equals(rounds.Rows[row].Cells[col].Value,values[col]))rounds.Rows[row].Cells[col].Value=values[col];
            rounds.Rows[row].Cells[0].ToolTipText=ready?$"{match.Team(attacker)} {L.Role("ATQ")} / {match.Team(1-attacker)} {L.Role("DEF")}":"";
            rounds.Rows[row].Cells[6].ToolTipText=RoundEnding.Label(r?.Ending??"");
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
        if(loading || current is null || IsLocked || e.RowIndex<0 || e.ColumnIndex is not (3 or 4 or 6))return;
        int row=e.RowIndex;string site=rounds.Rows[row].Cells[3].Value?.ToString()??L.T("Selecionar…");string winner=rounds.Rows[row].Cells[4].Value?.ToString()??L.T("Selecionar…");
        if(row>=current.Rounds.Count && winner==L.T("Selecionar…"))return;
        if(winner==L.T("Selecionar…")){Warn(L.T("Escolha um vencedor para corrigir o round."));BeginInvoke((Action)RefreshMatch);return;}
        if(site!=L.T("Selecionar…"))site=catalog.Maps.Single(x=>x.Id==current.MapId).Sites.FirstOrDefault(x=>BombsiteNames.Display(current.MapId,x)==site)??site;
        if(current.SiteBlocked(site,row+1) && (row>=current.Rounds.Count || current.Rounds[row].Site!=site)){Warn(L.T("Bombsite bloqueado por vitória da defesa. Escolha outro local."));roundsKey="";BeginInvoke((Action)RefreshMatch);return;}
        int who=winner.StartsWith("A • ")?0:1;
        string ending=RoundEnding.Codes.FirstOrDefault(code=>RoundEnding.Label(code)==rounds.Rows[row].Cells[6].Value?.ToString())??"";
        if(!RoundEnding.Allowed(ending,who,current.Attacker(row+1)))
        {
            if(e.ColumnIndex==6){Warn(L.T("Finalização incompatível com o vencedor."));roundsKey="";BeginInvoke((Action)RefreshMatch);return;}
            ending="";
        }
        var snapshot=JsonSerializer.Deserialize<List<RoundRecord>>(JsonSerializer.Serialize(current.Rounds))!;var banSnapshot=current.Bans.ToArray();
        if(row==current.Rounds.Count){if(current.Score().Winner>=0){Warn(L.T("Vencedor já definido. Confira os rounds."));return;}current.Rounds.Add(new(){Winner=who,Site=site==L.T("Selecionar…")?"":site,Ending=ending});}
        else{current.Rounds[row].Winner=who;current.Rounds[row].Site=site==L.T("Selecionar…")?"":site;current.Rounds[row].Ending=ending;}
        var result=current.Score();
        if(result.EndAt>0 && result.EndAt<current.Rounds.Count)
        {
            if(!Confirm(L.T("Esta correção encerra o mapa no round ")+result.EndAt+L.T(". Remover os rounds posteriores?"))){current.Rounds=snapshot;BeginInvoke((Action)RefreshMatch);return;}
            current.Rounds.RemoveRange(result.EndAt,current.Rounds.Count-result.EndAt);
            for(int i=0;i<12;i++)if(MapRecord.BanThreshold(i)>current.Rounds.Count)current.Bans[i]="";
        }
        bool savedRound=Persist();if(!savedRound){current.Rounds=snapshot;current.Bans=banSnapshot;}BeginInvoke((Action)RefreshMatch);
        if(savedRound && current.Rounds.Count>snapshot.Count)QueueBanReminder(current);
    }
    void QueueBanReminder(MapRecord map)
    {
        int threshold=map.Rounds.Count;
        if(threshold is not (3 or 6 or 9) || map.Confirmed || match.Complete || map.Score().Winner>=0)return;
        string matchId=match.Id,key=$"{matchId}/{map.MapId}/{threshold}";
        BeginInvoke((Action)(()=>
        {
            if(IsDisposed || current!=map || match.Id!=matchId || map.Rounds.Count!=threshold || IsLocked || map.Score().Winner>=0)return;
            int[] slots=Enumerable.Range(0,12).Where(i=>MapRecord.BanThreshold(i)==threshold && map.BanStageReached(i) && string.IsNullOrEmpty(map.Bans[i])).ToArray();
            if(slots.Length==0 || !shownBanReminders.Add(key))return;
            if(BanReminderPreview is not null){BanReminderPreview(slots);return;}
            using var dialog=new BanReminderDialog(threshold+1,slots.Select(i=>(match.Team(MapRecord.BanTeam(i)),L.Role(map.BanRole(i)),MapRecord.BanTeam(i))).ToArray());
            if(dialog.ShowDialog(this)!=DialogResult.OK)return;
            foreach(int slot in slots)
            {
                if(IsDisposed || current!=map || IsLocked)break;
                if(!string.IsNullOrEmpty(map.Bans[slot]))continue;
                PickOperator(slot);
                if(string.IsNullOrEmpty(map.Bans[slot]))break;
            }
        }));
    }
    void NewMatch()
    {
        try{FlushNames();if(!Persist())return;SiegeMatch next;do{next=new();}while(matches.Any(m=>m.Id==next.Id)||File.Exists(Path.Combine(MatchStore.Folder,next.Id+".json")));MatchStore.Save(next);match=next;matches.Insert(0,match);current=null;correctBans=false;LoadMatch();}
        catch(Exception ex){Warn(L.T("Falha ao salvar: ")+ex.Message);}
    }
    void ConfirmWinner()
    {
        if(current is null || IsLocked || current.Score().Winner<0)return;
        int pending=Enumerable.Range(0,12).Count(i=>current.BanStageReached(i) && string.IsNullOrEmpty(current.Bans[i]));
        string message=L.F($"Confirmar que {match.Team(current.Score().Winner)} venceu este mapa?\n\nO mapa ficará bloqueado para edição. A correção administrativa ainda não está disponível.");
        if(pending>0)message+=L.F($"\n\nExistem {pending} bans pendentes. Eles também ficarão bloqueados.");
        if(!Confirm(message))return;
        current.ConfirmedAt=DateTimeOffset.Now;
        if(!Persist()){current.ConfirmedAt=null;return;}
        correctBans=false;RefreshMatch();
        MapConfirmed?.Invoke(match,current);
    }
    void Export()
    {
        FlushNames();
        using var dialog=new SaveFileDialog(){InitialDirectory=AppPaths.Ensure("Partidas/Exportadas"),Filter=L.T("Partida JSON|*.json"),FileName=MatchStore.ExportFileName(match)};
        if(dialog.ShowDialog(this)!=DialogResult.OK)return;try{MatchStore.Atomic(dialog.FileName,match);notice.Text=L.T("Partida exportada.");}catch(Exception ex){Warn(ex.Message);}
    }
    void Import()
    {
        using var dialog=new OpenFileDialog(){Filter=L.T("Partida JSON|*.json")};if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        try
        {
            var imported=MatchStore.Read(dialog.FileName,catalog);
            if(MatchStore.IsDuplicate(matches,imported,MatchStore.Folder))
            {Warn(L.F($"Esta partida já existe no programa: {imported.TeamA} × {imported.TeamB}. ID: {imported.Id}. Nenhum registro foi substituído."));return;}
            if(!Persist())return;MatchStore.Save(imported);match=imported;matches.Insert(0,match);current=null;LoadMatch();
        }catch(Exception ex){Warn(L.T("Não foi possível importar: ")+ex.Message);}
    }
    void Summary()
    {
        if(current is null){Warn(L.T("Escolha um mapa primeiro."));return;}
        var s=current.Score();int next=current.Rounds.Count+1;var text=new StringBuilder();text.AppendLine($"{match.TeamA} × {match.TeamB} — {catalog.Maps.Single(x=>x.Id==current.MapId).DisplayName}");text.AppendLine(L.F($"Placar: {s.A} × {s.B} • {current.Rounds.Count} rounds concluídos"));
        if(s.Winner>=0)text.AppendLine(L.T("Mapa encerrado: ")+match.Team(s.Winner));else if(current.InitialAttack>=0 && (next<=12 || current.OtAttack>=0))text.AppendLine(L.F($"Retomar no Round {next:00}: {match.Team(current.Attacker(next))} ATQ / {match.Team(1-current.Attacker(next))} DEF"));else text.AppendLine(L.T("Defina os lados antes de retomar."));
        for(int half=0;half<2;half++){text.AppendLine(L.F($"\r\nBans da {(half==0?L.T("primeira"):L.T("segunda"))} metade:"));for(int i=half*6;i<half*6+6;i++)text.AppendLine($"{i%6+1:00}. {match.Team(MapRecord.BanTeam(i))}: {catalog.Operators.FirstOrDefault(o=>o.Id==current.Bans[i])?.Name??L.T("Pendente / não realizado")}");}
        text.AppendLine(L.T("\r\nHistórico:"));for(int i=0;i<current.Rounds.Count;i++){var r=current.Rounds[i];text.AppendLine(L.F($"Round {i+1:00}: {match.Team(current.Attacker(i+1))} ATQ • {L.T(r.Site)} • venceu {match.Team(r.Winner)}"));}
        using var dialog=new Form(){Text=L.T("Resumo para remake"),Size=new Size(720,600),StartPosition=FormStartPosition.CenterParent};var box=new TextBox(){Text=text.ToString(),Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill};var copy=Btn(L.T("Copiar resumo"),()=>Clipboard.SetText(box.Text));copy.Dock=DockStyle.Bottom;dialog.Controls.Add(box);dialog.Controls.Add(copy);AppTheme.Apply(dialog);dialog.ShowDialog(this);
    }
    protected override void Dispose(bool disposing){if(disposing){deferredRefresh.Dispose();nameSave.Dispose();hints.Dispose();missingBan.Dispose();foreach(var img in images.Values)img.Dispose();}base.Dispose(disposing);}
    internal void VerifyRoundEditing()
    {
        if(current is null || current.Rounds.Count!=4)throw new Exception("Demo not loaded");
        rounds.Rows[4].Cells[3].Value=L.T(catalog.Maps.Single(x=>x.Id==current.MapId).Sites[1]);
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
    internal void VerifyQuickRoundsAndPopups()
    {
        void Need(bool value,string name){if(!value)throw new Exception(name);}
        match=new(){TeamA="Liquid",TeamB="FaZe",CoinWinner=0,FirstBan=0,Veto=catalog.Maps.Take(8).Select(m=>m.Id).ToArray()};
        match.SyncMaps(catalog);current=match.Maps[0];current.InitialAttack=0;current.OtAttack=1;
        matches.Add(match);LoadMatch();SetQuickMode(true);Application.DoEvents();
        var prompts=new List<int[]>();BanReminderPreview=slots=>prompts.Add(slots);shownBanReminders.Clear();
        string site=catalog.Maps.Single(m=>m.Id==current.MapId).Sites[0];
        Need(!RecordQuickRound(site,1,"defuser_completed"),"Invalid quick ending accepted");
        Need(!RecordQuickRound(site,0,""),"Quick mode requires ending");
        Need(!RecordQuickRound("unknown",0,"elimination"),"Unknown quick site");
        IEnumerable<Button> Buttons(Control parent)=>parent.Controls.Cast<Control>().SelectMany(c=>(c is Button b?new[]{b}:Enumerable.Empty<Button>()).Concat(Buttons(c)));
        var buttons=Buttons(quickRound).ToArray();
        buttons.Single(b=>Equals(b.Tag,site)).PerformClick();buttons.Single(b=>Equals(b.Tag,"0")).PerformClick();buttons.Single(b=>Equals(b.Tag,"defuser_completed")).PerformClick();
        buttons.Single(b=>b.Text==L.T("CONFIRMAR ROUND")).PerformClick();buttons.Single(b=>b.Text==L.T("CONFIRMAR ROUND")).PerformClick();Application.DoEvents();
        Need(current.Rounds.Count==1 && current.Rounds[0].Ending=="defuser_completed","Quick UI commit");
        Need(!Buttons(quickRound).Single(b=>b.Text==L.T("CONFIRMAR ROUND")).Enabled,"Quick draft did not reset");
        for(int i=1;i<9;i++){string available=catalog.Maps.Single(m=>m.Id==current.MapId).Sites.First(s=>!current.SiteBlocked(s,current.Rounds.Count+1));Need(RecordQuickRound(available,i%2,"elimination"),"Quick round commit");Application.DoEvents();}
        Need(prompts.Count==3 && prompts.Select(p=>p.Length).SequenceEqual(new[]{2,4,2}),"Popup stages 4/7/10");
        QueueBanReminder(current);Application.DoEvents();Need(prompts.Count==3,"Repeated popup");
        SetQuickMode(false);Application.DoEvents();Need(rounds.Rows.Count==10,"Shared list history");
        rounds.Rows[0].Cells[4].Value="B • "+match.TeamB;Application.DoEvents();Need(current.Rounds[0].Ending=="","Correction must clear incompatible ending");
        rounds.Rows[0].Cells[6].Value=RoundEnding.Label("time");Application.DoEvents();Need(current.Rounds[0].Ending=="time","List ending edit");
        Need(prompts.Count==3,"Correction reopened popup");
        MatchStore.Validate(match,catalog);Need(MatchStore.Read(Path.Combine(MatchStore.Folder,match.Id+".json"),catalog).Maps[0].Rounds[0].Ending=="time","Ending persistence");
        current.Rounds=new[]{0,0,0,0,0,0,1,1}.Select(w=>new RoundRecord{Winner=w,Site=site}).ToList();shownBanReminders.Clear();
        Need(RecordQuickRound(site,0,"elimination"),"7-2 final");Application.DoEvents();Need(prompts.Count==3,"Popup after map ended");
        Need(!RecordQuickRound(site,0,"elimination"),"Round after end accepted");
        current.Rounds=current.Rounds.Take(2).ToList();
        QueueBanReminder(current);Application.DoEvents();Need(prompts.Count==3,"Non-stage popup");
        current.Bans[4]=catalog.Operators.First(o=>o.Role==current.BanRole(4)).Id;current.Bans[5]=catalog.Operators.First(o=>o.Role==current.BanRole(5)).Id;
        Need(RecordQuickRound(site,1,"elimination"),"Recorded stage");Application.DoEvents();Need(prompts.Count==3,"Popup for filled bans");
        current.Rounds=Enumerable.Range(0,12).Select(i=>new RoundRecord{Winner=i%2,Site=site}).ToList();current.OtAttack=-1;
        Need(!RecordQuickRound(site,0,"elimination"),"OT requires sides");current.OtAttack=1;
        Need(!RecordQuickRound(site,0,"defuser_completed"),"OT role mismatch");
        Need(RecordQuickRound(site,1,"defuser_completed"),"OT first attacker");Application.DoEvents();
        Need(RecordQuickRound(site,0,"defuser_completed"),"OT swap attacker");Application.DoEvents();
        Need(RecordQuickRound(site,1,"defuser_completed"),"OT final round");Application.DoEvents();
        Need(current.Score().Winner==1 && current.Rounds.Count==15,"8-7 finish");
        Need(!RecordQuickRound(site,1,"elimination"),"No round 16");Need(prompts.Count==3,"No new OT bans");
        current.Rounds=current.Rounds.Take(3).ToList();
        foreach(bool dark in new[]{true,false})
        {
            AppTheme.Dark=dark;SetQuickMode(true);RefreshMatch();Application.DoEvents();
            using(var picture=new Bitmap(Width,Height)){DrawToBitmap(picture,new Rectangle(Point.Empty,Size));picture.Save(Path.Combine(AppPaths.DataDirectory,$"quick-rounds-{dark}.png"));}
            foreach(int count in new[]{2,4})
            {
                using var dialog=new BanReminderDialog(count==2?4:7,Enumerable.Range(0,count).Select(i=>(i%2==0?"Liquid":"FaZe",L.Role(i%2==0?"DEF":"ATQ"),i%2)).ToArray());
                dialog.Show();Application.DoEvents();
                using var screenshot=new Bitmap(dialog.Width,dialog.Height);dialog.DrawToBitmap(screenshot,new Rectangle(Point.Empty,dialog.Size));screenshot.Save(Path.Combine(AppPaths.DataDirectory,$"popup-{count}-{dark}.png"));dialog.Close();
            }
        }
        BanReminderPreview=null;SetQuickMode(false);
    }
    internal void VerifyMd5()
    {
        match=new SiegeMatch{Format="MD5",CoinWinner=0,FirstBan=0,Veto=catalog.Maps.Take(8).Select(x=>x.Id).ToArray()};
        match.SyncMaps(catalog);matches.Add(match);current=null;LoadMatch();Application.DoEvents();
        if(format.SelectedIndex!=2 || match.Maps.Count!=5 || mapSelect.Items.Count!=5)throw new Exception("MD5 UI format/maps");
        var last=(TableLayoutPanel)sideRows.GetControlFromPosition(0,5)!;
        if(last.Bottom>sideRows.ClientSize.Height)throw new Exception("Fifth map clipped");
        ((ComboBox)last.GetControlFromPosition(2,0)!).SelectedIndex=0;Application.DoEvents();
        last=(TableLayoutPanel)sideRows.GetControlFromPosition(0,5)!;
        ((ComboBox)last.GetControlFromPosition(3,0)!).SelectedIndex=1;Application.DoEvents();
        if(match.Maps[4].InitialAttack!=0 || match.Maps[4].OtAttack!=0)throw new Exception("MD5 decider sides");
        using var snapshot=new Bitmap(Width,Height);DrawToBitmap(snapshot,new Rectangle(Point.Empty,Size));snapshot.Save(Path.Combine(AppPaths.DataDirectory,"md5-ui.png"));
    }
    internal void VerifyBombsiteCooldown()
    {
        void Need(bool value,string why){if(!value)throw new Exception(why);}
        current!.ConfirmedAt=null;current.InitialAttack=0;current.Rounds.Clear();current.Bans=new string[12];BanReminderPreview=_=>{};
        string site=catalog.Maps.Single(m=>m.Id==current.MapId).Sites[0];
        Need(RecordQuickRound(site,1,"elimination"),"Defense win");Application.DoEvents();SetQuickMode(false);Application.DoEvents();
        var choices=(DataGridViewComboBoxCell)rounds.Rows[1].Cells[3];
        Need(!choices.Items.Contains(BombsiteNames.Display(current.MapId,site)),"Blocked site in list dropdown");
        Need(!RecordQuickRound(site,0,"elimination"),"Blocked quick site accepted");SetQuickMode(true);Application.DoEvents();
        IEnumerable<Button> Buttons(Control parent)=>parent.Controls.Cast<Control>().SelectMany(c=>(c is Button b?new[]{b}:Enumerable.Empty<Button>()).Concat(Buttons(c)));
        Need(!Buttons(quickRound).Single(b=>Equals(b.Tag,site)).Enabled,"Blocked site button enabled");
        rounds.Rows[0].Cells[4].Value="A • "+match.TeamA;Application.DoEvents();
        Need(Buttons(quickRound).Single(b=>Equals(b.Tag,site)).Enabled,"Correction failed to release quick site");
        Need(RecordQuickRound(site,1,"elimination"),"Released site not accepted");Application.DoEvents();
        rounds.Rows[0].Cells[4].Value="B • "+match.TeamB;Application.DoEvents();
        Need(current.Rounds.Count==2 && current.Rounds[1].Site==site && !string.IsNullOrEmpty(rounds.Rows[1].Cells[3].ErrorText),"Correction conflict must preserve and flag history");
        Need(roundHost.GetPositionFromControl(quickRound).Column==0 && roundHost.GetPositionFromControl(rounds).Column==1,"Quick/history columns");
        using(var picture=new Bitmap(Width,Height)){DrawToBitmap(picture,new Rectangle(Point.Empty,Size));picture.Save(Path.Combine(AppPaths.DataDirectory,"cooldown-compact.png"));}
        BanReminderPreview=null;
    }
    internal void VerifySmallImprovements()
    {
        void Need(bool ok,string message){if(!ok)throw new Exception(message);}
        foreach(string mode in new[]{"MD1","MD3","MD5"})
        {
            match=new SiegeMatch{Format=mode,CoinWinner=0,FirstBan=0};
            match.Veto[0]=catalog.Maps[0].Id;match.SyncMaps(catalog);
            using var used=MapChoiceButton(catalog.Maps[0],1);
            using var available=MapChoiceButton(catalog.Maps[1],1);
            Need(!used.Enabled && available.Enabled,"Map reuse lock");
            using var gray=new Bitmap(used.Image!);var pixel=gray.GetPixel(50,30);Need(pixel.R==pixel.G && pixel.G==pixel.B,"Unavailable map must load grayscale");
            match.Veto[0]=catalog.Maps[2].Id;Need(MapAvailable(catalog.Maps[0],1),"Corrected veto releases old map");
        }
        match=new SiegeMatch{CoinWinner=0,FirstBan=0,Veto=catalog.Maps.Take(8).Select(m=>m.Id).ToArray()};match.SyncMaps(catalog);matches.Add(match);
        current=match.Maps[0];current.InitialAttack=0;current.OtAttack=1;current.Rounds=Enumerable.Range(0,7).Select(_=>new RoundRecord{Winner=0}).ToList();current.ConfirmedAt=DateTimeOffset.Now;
        LoadMatch();Application.DoEvents();
        var baseline=JsonSerializer.Serialize(new{match.Id,match.Veto,current.MapId,current.Picker,current.InitialAttack,current.OtAttack,current.Rounds,current.ConfirmedAt});
        string defender=catalog.Operators.First(o=>o.Role=="DEF").Id;
        string attacker=catalog.Operators.First(o=>o.Role=="ATQ").Id;
        Need(!SetOperator(0,defender),"Confirmed bans require correction mode");
        correction.PerformClick();Application.DoEvents();
        Need(correctBans && SetOperator(0,defender),"Confirmed ban correction");Application.DoEvents();
        Need(!SetOperator(1,defender),"Duplicate ban rejected");Need(!SetOperator(1,attacker),"Wrong role rejected");Need(!SetOperator(10,attacker),"Future stage rejected after finish");
        Need(baseline==JsonSerializer.Serialize(new{match.Id,match.Veto,current.MapId,current.Picker,current.InitialAttack,current.OtAttack,current.Rounds,current.ConfirmedAt}),"Correction changed result");
        Need(IsLocked && rounds.Rows.Cast<DataGridViewRow>().All(r=>r.ReadOnly) && !confirmWinner.Enabled,"Result unlocked during correction");
        var restored=MatchStore.Read(Path.Combine(MatchStore.Folder,match.Id+".json"),catalog);Need(restored.Complete && restored.Maps[0].Bans[0]==defender,"Correction not persisted");
        correction.PerformClick();Application.DoEvents();Need(!CanEditBan(0),"Correction mode did not close");
        current.Rounds=current.Rounds.Take(3).ToList();current.ConfirmedAt=null;current.Bans=new string[12];
        for(int i=0;i<4;i++)current.Bans[i]=catalog.Operators.Where(o=>o.Role==current.BanRole(i)).Skip(i%2).First().Id;
        RefreshMatch();Application.DoEvents();Need(current.PendingBans(0).Length==2,"Round 4 reminder");
        var banner=(Label)bans.GetControlFromPosition(0,0)!;Need(banner.Text.Contains("04"),"Reminder round missing");
        using(var screenshot=new Bitmap(Width,Height)){DrawToBitmap(screenshot,new Rectangle(Point.Empty,Size));screenshot.Save(Path.Combine(AppPaths.DataDirectory,"ban-reminder.png"));}
        current.Bans[4]=catalog.Operators.Where(o=>o.Role==current.BanRole(4)).Skip(2).First().Id;current.Bans[5]=catalog.Operators.Where(o=>o.Role==current.BanRole(5)).Skip(2).First().Id;RefreshMatch();Application.DoEvents();banner=(Label)bans.GetControlFromPosition(0,0)!;Need(!banner.Text.StartsWith("!"),"Reminder remains after completing bans");
        foreach(bool dark in new[]{false,true})
        {
            AppTheme.Dark=dark;using var dialog=new Form{ClientSize=new Size(720,430)};var grid=Table(3,3);dialog.Controls.Add(grid);
            for(int i=0;i<catalog.Maps.Count;i++){var button=MapChoiceButton(catalog.Maps[i],1);button.Disposed+=(_,_)=>button.Image?.Dispose();grid.Controls.Add(button,i%3,i/3);}
            dialog.Show();AppTheme.Apply(dialog);Application.DoEvents();using var screenshot=new Bitmap(dialog.Width,dialog.Height);dialog.DrawToBitmap(screenshot,new Rectangle(Point.Empty,dialog.Size));screenshot.Save(Path.Combine(AppPaths.DataDirectory,$"map-cards-{dark}.png"));dialog.Close();
        }
    }
    internal void VerifyResponsiveDesign()
    {
        var window=FindForm()!;Size oldSize=window.ClientSize;bool oldDark=AppTheme.Dark;
        foreach(int side in new[]{0,1})
        {
            current=new MapRecord{InitialAttack=side};
            foreach(int half in new[]{0,1})
            {
                var order=BanDisplayOrder(half);
                if(!order.Take(4).Select(current.BanRole).SequenceEqual(new[]{"DEF","ATQ","DEF","ATQ"}) || order.Distinct().Count()!=6)throw new Exception("Ban role order or identity changed");
            }
        }
        void Need(bool value,string message){if(!value)throw new Exception(message);}
        foreach(string mode in new[]{"MD1","MD3","MD5"})
        {
            match=new(){TeamA="Liquid",TeamB="FaZe",Format=mode,CoinWinner=0,FirstBan=0,Veto=catalog.Maps.Take(8).Select(x=>x.Id).ToArray()};match.SyncMaps(catalog);matches.Add(match);
            foreach(var map in match.Maps){map.InitialAttack=0;map.OtAttack=1;}
            current=match.Maps[0];current.Rounds=Enumerable.Range(0,4).Select(i=>new RoundRecord{Winner=i%2,Site=catalog.Maps.Single(m=>m.Id==current.MapId).Sites[i%4],Ending="elimination"}).ToList();
            if(mode=="MD1")
            {
                var visited=new List<string>();
                OperatorPickerPreview=(dialog,slot)=>
                {
                    visited.Add(current.BanRole(slot));
                    var flow=dialog.Controls.OfType<FlowLayoutPanel>().Single();
                    var buttons=flow.Controls.OfType<Button>().ToArray();
                    Need(buttons.All(b=>b.Right<=flow.ClientSize.Width && b.Bottom<=flow.ClientSize.Height),"Operator picker requires scrolling");
                    var expected=OperatorOrder.Sort(catalog.Operators,current.BanRole(slot)).Select(o=>o.Name);
                    Need(buttons.Select(b=>b.Text).SequenceEqual(expected),"Operator grid order differs from Siege");
                    if(visited.Count<=2){using var bmp=new Bitmap(dialog.Width,dialog.Height);dialog.DrawToBitmap(bmp,new Rectangle(Point.Empty,dialog.Size));bmp.Save(Path.Combine(AppPaths.DataDirectory,"operator-picker-"+current.BanRole(slot)+".png"));}
                    buttons.First(b=>b.Enabled).PerformClick();
                };
                LoadMatch();PickOperator(BanDisplayOrder(0)[0]);OperatorPickerPreview=null;
                Need(visited.SequenceEqual(new[]{"DEF","ATQ","DEF","ATQ"}),"Automatic ban sequence wrong");
                Need(current.Bans.Take(4).All(b=>!string.IsNullOrEmpty(b)) && current.Bans.Skip(4).All(string.IsNullOrEmpty),"Ban sequence crossed phase boundary");
                current.Bans=new string[12];
                OperatorPickerPreview=(dialog,slot)=>dialog.Close();PickOperator(BanDisplayOrder(0)[0]);OperatorPickerPreview=null;
                Need(current.Bans.All(string.IsNullOrEmpty),"Cancelled picker recorded a ban");
            }
            for(int slot=0;slot<6;slot++)current.Bans[slot]=catalog.Operators.Where(o=>o.Role==current.BanRole(slot)).Skip(slot).First().Id;
            LoadMatch();SetQuickMode(true);AppTheme.Dark=true;
            foreach(var size in new[]{new Size(1920,920),new Size(1366,640),new Size(1140,600)})
            {
                window.ClientSize=size;Application.DoEvents();RefreshMatchNow();Application.DoEvents();
                var page=(TabPage)mainCanvas.Parent!;
                Need(!page.AutoScroll && mainCanvas.Bottom<=page.ClientSize.Height,"Page must fit without scrolling");
                foreach(Control action in actionButtons.Controls)Need(action.Visible && action.Right<=actionButtons.ClientSize.Width && action.Bottom<=actionButtons.ClientSize.Height,"Action clipped: "+action.Text);
                Need(confirmWinner.Bottom<=confirmWinner.Parent!.ClientSize.Height,"Confirm action clipped");
                Need(rounds.Columns[6].Visible,"Ending column missing");
                if(size.Width==1920){var fields=((TableLayoutPanel)sideRows.GetControlFromPosition(0,1)!).Controls.Cast<Control>().ToArray();Need(fields.Select(f=>f.Height).Distinct().Count()==1 && fields.Select(f=>f.Top).Distinct().Count()==1,"Side fields must share height and baseline: "+string.Join(";",fields.Select(f=>$"{f.GetType().Name}={f.Top}/{f.Height}")));}
                Need(quickRound.Height>=185,"Quick entry too short");
                IEnumerable<Button> QuickButtons(Control c)=>c.Controls.Cast<Control>().SelectMany(child=>(child is Button b?new[]{b}:Enumerable.Empty<Button>()).Concat(QuickButtons(child)));
                Need(QuickButtons(quickRound).All(b=>b.Height>=25),"Quick choice clipped");
                var confirm=QuickButtons(quickRound).Single(b=>b.Text==L.T("CONFIRMAR ROUND"));Need(confirm.Bottom<=quickRound.ClientSize.Height,"Round confirmation below viewport");
                Need(bans.Controls.OfType<OperatorBanCard>().Count()==12,"Ban cards missing");
                Need(bans.Controls.OfType<OperatorBanCard>().Where(c=>c.OperatorIcon is not null).All(c=>c.Portrait is not null),"Portrait missing");
                if(size.Width==1920 || mode=="MD5" && size.Width==1140){using var picture=new Bitmap(Width,Height);DrawToBitmap(picture,new Rectangle(Point.Empty,Size));picture.Save(Path.Combine(AppPaths.DataDirectory,$"design-{mode}-{size.Width}.png"));}
            }
            // At compact heights the inactive half remains reachable via its header.
            visibleBanHalf=1;AdjustLayout();Application.DoEvents();Need(bans.GetControlFromPosition(0,3)!.Visible,"Cannot reveal second ban half");
            current.Rounds=Enumerable.Range(0,7).Select(_=>new RoundRecord{Winner=0}).ToList();RefreshMatchNow();Application.DoEvents();Need(!quickRound.Visible && roundHost.ColumnStyles[0].Width==0,"Finished map must expand history");
        }
        AppTheme.Dark=oldDark;window.ClientSize=oldSize;SetQuickMode(false);RefreshMatchNow();
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
}









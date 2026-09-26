namespace CronoLive;

internal sealed class QuickRoundPanel : UserControl
{
    readonly Label title=new(){Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft};
    readonly TableLayoutPanel sites=new(){Dock=DockStyle.Fill,ColumnCount=2,RowCount=2};
    readonly TableLayoutPanel winners=new(){Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};
    readonly TableLayoutPanel endings=new(){Dock=DockStyle.Fill,ColumnCount=2,RowCount=2};
    readonly Button confirm=new ThemeButton{Text=L.T("CONFIRMAR ROUND"),Dock=DockStyle.Fill,PrimaryAction=true};
    string key="",selectedSite="",selectedEnding="";int selectedWinner=-1,attacker=-1;
    readonly HashSet<string> blockedSites=[];
    readonly ToolTip hints=new(){ShowAlways=true};
    public event Func<string,int,string,bool>? ConfirmRound;
    public QuickRoundPanel()
    {
        Dock=DockStyle.Top;Height=300;
        var viewport=new Panel{Dock=DockStyle.Fill,AutoScroll=true};
        var layout=new TableLayoutPanel{Dock=DockStyle.Top,ColumnCount=1,RowCount=7,Height=340};
        layout.RowStyles.Add(new(SizeType.Absolute,25));foreach(float percent in new[]{38f,24f,38f}){layout.RowStyles.Add(new(SizeType.Absolute,22));layout.RowStyles.Add(new(SizeType.Percent,percent));}
        layout.ColumnStyles.Add(new(SizeType.Percent,100));viewport.Controls.Add(layout);Controls.Add(viewport);
        confirm.Dock=DockStyle.Bottom;confirm.Height=38;Controls.Add(confirm);
        viewport.SizeChanged+=(_,_)=>{int height=Math.Max((int)(340*DeviceDpi/96f),viewport.ClientSize.Height);if(layout.Height!=height)layout.Height=height;};
        foreach(var grid in new[]{sites,winners,endings}){grid.ColumnStyles.Add(new(SizeType.Percent,50));grid.ColumnStyles.Add(new(SizeType.Percent,50));for(int i=0;i<grid.RowCount;i++)grid.RowStyles.Add(new(SizeType.Percent,100f/grid.RowCount));}
        layout.Controls.Add(title,0,0);int section=1;foreach(var pair in new[]{(sites,"Bomb site"),(winners,"Vencedor"),(endings,"Finalização")}){layout.Controls.Add(new ThemeCaption{Text=L.T(pair.Item2).ToUpperInvariant(),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(5,0,0,0)},0,section++);layout.Controls.Add(pair.Item1,0,section++);}
        confirm.Click+=(_,_)=>
        {
            if(!confirm.Enabled)return;confirm.Enabled=false;
            if(ConfirmRound?.Invoke(selectedSite,selectedWinner,selectedEnding)==true){selectedSite="";selectedWinner=-1;selectedEnding="";}
            Highlight();
        };
    }
    static void Clear(Control parent){foreach(Control child in parent.Controls.Cast<Control>().ToArray())child.Dispose();}
    Button Choice(string text,string tag,Action select){var button=new ThemeButton{Text=text,Tag=tag,Dock=DockStyle.Fill,Margin=new Padding(2),AccessibleName=text};button.Click+=(_,_)=>{select();Highlight();};return button;}
    public void UpdateMatch(SiegeMatch match,MapRecord? map,SiegeCatalog catalog,bool locked)
    {
        string next=$"{match.Id}|{match.TeamA}|{match.TeamB}|{map?.MapId}|{System.Text.Json.JsonSerializer.Serialize(map?.Rounds)}|{map?.InitialAttack}|{map?.OtAttack}|{map?.Score().Winner}|{locked}";
        if(next==key)return;key=next;selectedSite="";selectedWinner=-1;selectedEnding="";
        Clear(sites);Clear(winners);Clear(endings);blockedSites.Clear();hints.RemoveAll();
        int round=(map?.Rounds.Count??0)+1;
        bool ready=map is not null && map.InitialAttack>=0 && (round<=12 || map.OtAttack>=0) && map.Score().Winner<0 && !locked;
        title.Text=map is null?L.T("Escolha um mapa primeiro."):map.Score().Winner>=0?L.T("Vencedor já definido. Confira os rounds."):!ready?L.T("Defina os lados antes de retomar."):L.F($"ROUND {round:00} • registro rápido");
        if(!ready){attacker=-1;Highlight();return;}
        attacker=map!.Attacker(round);
        int i=0;foreach(string site in catalog.Maps.Single(m=>m.Id==map.MapId).Sites)
        {
            string chosen=site;bool blocked=map.SiteBlocked(site,round);if(blocked)blockedSites.Add(site);
            var button=Choice((blocked?"× ":"")+BombsiteNames.Display(map.MapId,site),site,()=>selectedSite=chosen);
            hints.SetToolTip(button,L.T(site)+(blocked?"\n"+L.T("Bloqueado: vitória da defesa nos dois rounds anteriores."):""));
            sites.Controls.Add(button,i%2,i/2);i++;
        }
        for(int team=0;team<2;team++){int chosen=team;winners.Controls.Add(Choice(match.Team(team)+"\n"+L.T(team==attacker?"Ataque":"Defesa").ToUpperInvariant(),team.ToString(),()=>{selectedWinner=chosen;if(!RoundEnding.Allowed(selectedEnding,chosen,attacker))selectedEnding="";}),team,0);}
        i=0;foreach(string code in RoundEnding.Codes.Skip(1)){string chosen=code;endings.Controls.Add(Choice(RoundEnding.Label(code),code,()=>selectedEnding=chosen),i%2,i/2);i++;}
        AppTheme.Apply(this);Highlight();
    }
    void Highlight()
    {
        foreach(var grid in new[]{sites,winners,endings})foreach(Button button in grid.Controls)
        {
            string tag=(string)button.Tag!;bool selected=grid==sites?tag==selectedSite:grid==winners?tag==selectedWinner.ToString():tag==selectedEnding;
            button.Enabled=grid==sites?!blockedSites.Contains(tag):grid!=endings || selectedWinner>=0 && RoundEnding.Allowed(tag,selectedWinner,attacker);
            ((ThemeButton)button).EmphasisFill=grid==winners?(selected?(tag=="0"?Color.FromArgb(64,143,179):Color.FromArgb(255,119,55)):(tag=="0"?Color.FromArgb(35,87,111):Color.FromArgb(126,58,27))):selected?(AppTheme.Siege?Color.FromArgb(163,55,54):Color.FromArgb(64,100,130)):null;
            button.ForeColor=grid==winners?(selected?Color.Black:Color.White):selected?Color.White:AppTheme.Text;button.Text=(selected?"✓ ":"")+button.AccessibleName;button.Invalidate();
        }
        confirm.Enabled=selectedSite!="" && selectedWinner>=0 && selectedEnding!="" && attacker>=0;
    }
    protected override void Dispose(bool disposing){if(disposing)hints.Dispose();base.Dispose(disposing);}
}



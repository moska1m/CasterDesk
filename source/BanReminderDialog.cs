namespace CasterDesk.Core;

// A short, themed prompt. Dismissing it never changes match data or clears pending bans.
internal sealed class BanReminderDialog : Form
{
    readonly Font headingFont=new("Bahnschrift SemiCondensed",22,FontStyle.Bold);
    readonly Font cardFont=new("Segoe UI",11,FontStyle.Bold);
    public BanReminderDialog(int round,(string Team,string Role,int TeamIndex)[] slots)
    {
        Text=L.T("Bans pendentes");StartPosition=FormStartPosition.CenterParent;
        FormBorderStyle=FormBorderStyle.FixedDialog;MinimizeBox=false;MaximizeBox=false;ShowInTaskbar=false;
        AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(540,slots.Length>2?430:320);
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(22),ColumnCount=1,RowCount=4};
        layout.ColumnStyles.Add(new(SizeType.Percent,100));
        layout.RowStyles.Add(new(SizeType.Absolute,48));layout.RowStyles.Add(new(SizeType.Absolute,42));
        layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,52));Controls.Add(layout);
        layout.Controls.Add(new Label{Text=L.T("HORA DOS BANS"),Font=headingFont,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,0);
        layout.Controls.Add(new Label{Text=L.F($"Antes do round {round:00} • {slots.Length} ban(s) pendente(s)."),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,1);
        var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=(slots.Length+1)/2};
        grid.ColumnStyles.Add(new(SizeType.Percent,50));grid.ColumnStyles.Add(new(SizeType.Percent,50));
        for(int row=0;row<grid.RowCount;row++)grid.RowStyles.Add(new(SizeType.Percent,100f/grid.RowCount));
        layout.Controls.Add(grid,0,2);
        for(int i=0;i<slots.Length;i++)
        {
            var slot=slots[i];var card=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Margin=new Padding(4),Padding=new Padding(6),BackColor=Color.White};
            card.RowStyles.Add(new(SizeType.Absolute,5));card.RowStyles.Add(new(SizeType.Percent,55));card.RowStyles.Add(new(SizeType.Percent,45));
            card.Controls.Add(new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty,BackColor=slot.TeamIndex==0?Color.FromArgb(64,143,179):Color.FromArgb(255,119,55)},0,0);
            card.Controls.Add(new Label{Text="!  "+slot.Team.ToUpperInvariant(),Font=cardFont,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter},0,1);
            card.Controls.Add(new Label{Text=L.T("Selecionar ban de ")+slot.Role,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter},0,2);
            grid.Controls.Add(card,i%2,i/2);
        }
        var actions=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(0,10,0,0)};
        actions.ColumnStyles.Add(new(SizeType.Percent,50));actions.ColumnStyles.Add(new(SizeType.Percent,50));
        var register=new ThemeButton{Text=L.T("REGISTRAR BANS"),Dock=DockStyle.Fill,DialogResult=DialogResult.OK};
        var later=new ThemeButton{Text=L.T("LEMBRAR DEPOIS"),Dock=DockStyle.Fill,DialogResult=DialogResult.Cancel};
        actions.Controls.Add(register,0,0);actions.Controls.Add(later,1,0);layout.Controls.Add(actions,0,3);
        AcceptButton=register;CancelButton=later;AppTheme.Apply(this);
        register.EmphasisFill=AppTheme.Dark?Color.FromArgb(160,40,57):Color.FromArgb(174,35,51);register.ForeColor=Color.White;
        var stripe=new Panel{Dock=DockStyle.Top,Height=4,BackColor=Color.FromArgb(237,100,117)};Controls.Add(stripe);stripe.BringToFront();
    }
    protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing){headingFont.Dispose();cardFont.Dispose();}}
}

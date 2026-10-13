namespace CronoLive;

// Submenus replace the contents of this panel, never create another window.
public sealed class SettingsMenu : Panel
{
    readonly FlowLayoutPanel items=new(){Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(10)};
    readonly Settings settings;readonly Action<bool> rounds;readonly Action<bool,bool> theme;readonly Action<string> language;readonly Action backup,restore,about;
    public SettingsMenu(Settings settings,Action<bool> rounds,Action<bool,bool> theme,Action<string> language,Action backup,Action restore,Action about)
    {
        this.settings=settings;this.rounds=rounds;this.theme=theme;this.language=language;this.backup=backup;this.restore=restore;this.about=about;
        Width=340;Height=470;BorderStyle=BorderStyle.FixedSingle;Visible=false;Controls.Add(items);
        items.ControlAdded+=(_,_)=>{if(Parent is not null)Fit(Parent);};
        Home();
    }
    void Page(string title,Action? back=null)
    {
        while(items.Controls.Count>0){var c=items.Controls[0];items.Controls.Remove(c);c.Dispose();}
        BackColor=items.BackColor=AppTheme.Surface;
        Add(back is null?L.T("CONFIGURAÇÕES")+"   ×":"‹  "+title,()=>{if(back is null)Hide();else back();});
    }
    void Add(string text,Action action,bool active=false)
    {
        var button=new Button{Text=(active?"✓  ":"")+text,Width=295,Height=44,FlatStyle=FlatStyle.Flat,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(10,0,0,0),BackColor=active?Color.FromArgb(48,112,148):AppTheme.Surface,ForeColor=AppTheme.Text,Font=AppFonts.Body(10,active?FontStyle.Bold:FontStyle.Regular),Margin=new Padding(2,3,2,3)};
        button.FlatAppearance.BorderSize=0;button.FlatAppearance.MouseOverBackColor=Color.FromArgb(175,53,62);button.Click+=(_,_)=>action();items.Controls.Add(button);
    }
    public void Home()
    {
        Page(L.T("CONFIGURAÇÕES"));Add(L.T("Registro de rounds")+"  ›",Rounds);Add(L.T("APARÊNCIA")+"  ›",Appearance);Add(L.T("Idioma")+"  ›",Languages);
        Add(L.T("Backup e restauração")+"  ›",Backups);Add(L.T("Abrir pasta de arquivos"),()=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(AppPaths.DataDirectory){UseShellExecute=true}));Add(L.T("Sobre"),()=>{Hide();about();});
    }
    public void Rounds(){Page(L.T("Registro de rounds"),Home);Add(L.T("Modo rápido"),()=>{rounds(true);Rounds();},settings.QuickRounds);Add(L.T("Lista"),()=>{rounds(false);Rounds();},!settings.QuickRounds);}
    public void Appearance(){Page(L.T("APARÊNCIA"),Home);Add("Siege  ›",()=>Colors(true),settings.SiegeTheme);Add(L.T("Simplificado")+"  ›",()=>Colors(false),!settings.SiegeTheme);}
    public void Colors(bool siege){Page(siege?"Siege":L.T("Simplificado"),Appearance);Add(L.T("Escuro"),()=>{theme(siege,true);Colors(siege);},settings.SiegeTheme==siege&&settings.DarkMode);Add(L.T("Claro"),()=>{theme(siege,false);Colors(siege);},settings.SiegeTheme==siege&&!settings.DarkMode);items.Controls.Add(new Label{Text=L.T("Aplicar na próxima abertura"),Width=285,Height=45,ForeColor=AppTheme.Text});}
    public void Languages(){Page(L.T("Idioma"),Home);foreach(var (code,label) in new[]{("pt-BR","Português (Brasil)"),("en","English"),("es","Español")})Add(label,()=>{language(code);Languages();},settings.Language==code);}
    void Backups(){Page(L.T("Backup e restauração"),Home);Add(L.T("Criar backup…"),()=>{Hide();backup();});Add(L.T("Restaurar backup…"),()=>{Hide();restore();});items.Controls.Add(new Label{Text=L.T("Inclui a pasta do CasterDesk. Arquivos externos e backups anteriores não entram no ZIP."),Width=285,Height=80,ForeColor=AppTheme.Text});}
    public void Toggle(Control owner)
    {
        if(Visible){Hide();return;}Home();Show();BringToFront();Fit(owner);Focus();
    }
    public void Fit(Control owner)
    {
        Width=Math.Min(340,owner.ClientSize.Width);
        int content=items.Padding.Vertical+2+items.Controls.Cast<Control>().Sum(c=>c.Height+c.Margin.Vertical);
        Height=Math.Min(content,Math.Max(80,owner.ClientSize.Height-90));
        Location=new Point(Math.Max(0,owner.ClientSize.Width-Width-12),Math.Min(82,Math.Max(0,owner.ClientSize.Height-Height)));
    }
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData){if(keyData==Keys.Escape){Hide();return true;}return base.ProcessCmdKey(ref msg,keyData);}
}

using System.Drawing.Drawing2D;
namespace CronoLive;

// Read-only reference, independent of match settings.
public sealed class LobbyGuidePanel : UserControl
{
    readonly Panel canvas=new LobbyBackdrop(){Location=Point.Empty};
    readonly List<Control>[] columns=[[],[],[]];
    readonly Label title=new(){Text=L.T("CONFIGURAÇÃO DE LOBBY"),AutoSize=true,Font=AppFonts.Heading(23,FontStyle.Bold)};
    readonly Label subtitle=new(){Text=L.T("Guia de referência para criar sua partida no Siege."),AutoSize=true};
    public LobbyGuidePanel()
    {
        Dock=DockStyle.Fill;AutoScroll=true;DoubleBuffered=true;Font=new Font("Segoe UI",9.5f);Controls.Add(canvas);
        canvas.Controls.Add(title);canvas.Controls.Add(subtitle);
        void Block(int col,string heading,params (string,string)[] rows)
        {
            var card=new Panel(){Height=36+rows.Length*30,Padding=Padding.Empty};
            var label=new Label(){Text=heading,Dock=DockStyle.Top,Height=32,Font=AppFonts.Heading(12,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(8,0,0,0)};
            var table=new TableLayoutPanel(){Dock=DockStyle.Fill,ColumnCount=2,RowCount=rows.Length,Padding=new Padding(0,3,0,0)};
            table.ColumnStyles.Add(new(SizeType.Percent,66));table.ColumnStyles.Add(new(SizeType.Percent,34));
            foreach(var (name,value) in rows)
            {
                int n=table.RowStyles.Count;table.RowStyles.Add(new(SizeType.Absolute,30));
                var caption=new ThemeCaption(){Text=name,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(7,0,0,0),Margin=new Padding(1),AutoEllipsis=false};
                table.Controls.Add(caption,0,n);
                if(value.Length==0)table.SetColumnSpan(caption,2);
                else table.Controls.Add(new Label(){Text=value,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(7,0,0,0),Margin=new Padding(1),BorderStyle=BorderStyle.FixedSingle},1,n);
            }
            card.Controls.Add(table);card.Controls.Add(label);canvas.Controls.Add(card);columns[col].Add(card);
            foreach(Control cell in table.Controls)cell.Paint+=(_,e)=>{using var pen=new Pen(AppTheme.Border);e.Graphics.DrawRectangle(pen,0,0,cell.Width-1,cell.Height-1);};
            label.Paint+=(_,e)=>{using var fill=new SolidBrush(AppTheme.Surface);e.Graphics.FillRectangle(fill,label.ClientRectangle);TextRenderer.DrawText(e.Graphics,label.Text,label.Font,new Rectangle(8,0,label.Width-16,label.Height),AppTheme.Text,TextFormatFlags.VerticalCenter|TextFormatFlags.Left);};
            
            label.Paint+=(_,e)=>{using var pen=new Pen(AppTheme.Border);e.Graphics.DrawLine(pen,0,label.Height-1,label.Width,label.Height-1);};
        }
        Block(0,L.T("01   CRIAR PLAYLIST"),(L.T("Nome"),L.T("Campeonatos")),(L.T("Adicionar uma partida"),""),(L.T("Mapa (exemplo)"),L.T("Clube")),(L.T("Display de navegação"),"Pro League"),(L.T("Modo de jogo"),L.T("Bomba")));
        Block(0,L.T("02   BANIMENTOS"),(L.T("Ativar fase de banimento"),"Pro League"),(L.T("Tempo do banimento"),"20"));
        Block(0,L.T("03   RODADAS"),(L.T("Número de rodadas"),"12"),(L.T("Inverter atacante/defensor"),"6"),(L.T("Rodadas de prorrogação"),"3"),(L.T("Dif. pontos prorrogação"),"2"),(L.T("Inv. papéis prorrogação"),"1"),(L.T("Rotação de objetivo"),"2"),(L.T("Rotação do tipo de objetivo"),L.T("Rodadas jogadas")),(L.T("Tempo da fase de escolha"),"15"));
        Block(1,L.T("04   SAÚDE E DANOS"),(L.T("Energia do agente"),"100"),(L.T("Dano de fogo amigo"),"100"),(L.T("Fogo amigo na preparação"),L.T("Desligado")),(L.T("Fogo amigo reverso"),L.T("Desligado")),(L.T("Ferido"),"20"));
        Block(1,L.T("05   CONTROLE DO PERSONAGEM"),(L.T("Correr"),L.T("Ligado")),(L.T("Inclinar"),L.T("Ligado")));
        Block(1,L.T("06   MORTE"),(L.T("Duração da morte"),"3"),(L.T("Replay da eliminação"),L.T("Desligado")));
        Block(2,L.T("07   INTERRUPÇÃO TÁTICA"),(L.T("Solicitações por equipe"),"1"),(L.T("Permitir solicitações de"),L.T("Jogadores")),(L.T("Duração da interrupção"),"45"),(L.T("Intervalo de troca de função"),L.T("Ligado")),(L.T("Duração do intervalo"),"120"));
        Block(2,L.T("08   MODO BOMBA"),(L.T("Instalar / desativar"),"7 / 7"),(L.T("Detonação"),"45"),(L.T("Portador do desativador"),L.T("Ligado")),(L.T("Preparação / ação"),"45 / 180"),("Repick",L.T("Ligado")));
        Block(2,L.T("09   DUPLICAR E SALVAR"),(L.T("Configure o primeiro mapa."),""),(L.T("Duplique e altere apenas o mapa."),""),(L.T("Repita para os 9 mapas do campeonato."),""),(L.T("Salve as condições de jogo."),""));
        Block(1,L.T("MAP POOL"),(L.T("Banco • Fronteira • Chalé"),""),(L.T("Clube • Fortaleza • Café Dostoyevsky"),""),(L.T("Laboratórios Nighthaven • Covil • Mansão"),""));
        var note=new Label(){Text=L.T("Guia de consulta: configure os valores dentro do jogo. As partidas duplicadas mantêm as configurações."),AutoSize=false,Height=45};
        canvas.Controls.Add(note);
        void LayoutGuide()
        {
            int width=Math.Max(1080,ClientSize.Width-22);canvas.Width=width;
            title.Location=new Point(18,16);subtitle.Location=new Point(20,58);
            int gap=18,cw=(width-4*gap)/3,bottom=0;
            for(int c=0;c<3;c++){int y=100;foreach(var card in columns[c]){card.SetBounds(gap+c*(cw+gap),y,cw,card.Height);y+=card.Height+18;}bottom=Math.Max(bottom,y);}
            note.SetBounds(20,bottom,width-40,45);canvas.Height=bottom+50;AutoScrollMinSize=canvas.Size;
        }
        Resize+=(_,_)=>LayoutGuide();LayoutGuide();
        canvas.Paint+=(_,e)=>{if(AppTheme.Siege){using var pen=new Pen(AppTheme.Border);int gap=18,cw=(canvas.Width-4*gap)/3;for(int i=1;i<3;i++)e.Graphics.DrawLine(pen,i*(cw+gap)+gap/2,90,i*(cw+gap)+gap/2,canvas.Height-55);}};
    }
}

sealed class LobbyBackdrop : Panel
{
    public LobbyBackdrop(){DoubleBuffered=true;}
    protected override void OnPaintBackground(PaintEventArgs e){AppTheme.PaintSurface(e.Graphics,ClientRectangle);}
}

using System.Drawing.Drawing2D;
namespace CronoLive;

// Read-only reference, independent of match settings.
public sealed class LobbyGuidePanel : UserControl
{
    readonly Panel canvas=new LobbyBackdrop(){Location=Point.Empty};
    readonly List<Control>[] columns=[[],[],[]];
    readonly Label title=new(){Text="CONFIGURAÇÃO DE LOBBY",AutoSize=true,Font=new Font("Bahnschrift SemiCondensed",23,FontStyle.Bold)};
    readonly Label subtitle=new(){Text="Guia de referência para criar sua partida no Siege.",AutoSize=true};
    public LobbyGuidePanel()
    {
        Dock=DockStyle.Fill;AutoScroll=true;DoubleBuffered=true;Font=new Font("Segoe UI",9.5f);Controls.Add(canvas);
        canvas.Controls.Add(title);canvas.Controls.Add(subtitle);
        void Block(int col,string heading,params (string,string)[] rows)
        {
            var card=new Panel(){Height=36+rows.Length*30,Padding=Padding.Empty};
            var label=new Label(){Text=heading,Dock=DockStyle.Top,Height=32,Font=new Font("Bahnschrift SemiCondensed",12,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(8,0,0,0)};
            var table=new TableLayoutPanel(){Dock=DockStyle.Fill,ColumnCount=2,RowCount=rows.Length,Padding=new Padding(0,3,0,0)};
            table.ColumnStyles.Add(new(SizeType.Percent,66));table.ColumnStyles.Add(new(SizeType.Percent,34));
            foreach(var (name,value) in rows)
            {
                int n=table.RowStyles.Count;table.RowStyles.Add(new(SizeType.Absolute,30));
                var caption=new Label(){Text=name,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(7,0,0,0),Margin=new Padding(1),AutoEllipsis=false};
                table.Controls.Add(caption,0,n);
                if(value.Length==0)table.SetColumnSpan(caption,2);
                else table.Controls.Add(new Label(){Text=value,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(7,0,0,0),Margin=new Padding(1),BorderStyle=BorderStyle.FixedSingle},1,n);
            }
            card.Controls.Add(table);card.Controls.Add(label);canvas.Controls.Add(card);columns[col].Add(card);
            foreach(Control cell in table.Controls)cell.Paint+=(_,e)=>{using var pen=new Pen(AppTheme.Border);e.Graphics.DrawRectangle(pen,0,0,cell.Width-1,cell.Height-1);};
            label.Paint+=(_,e)=>{using var fill=new SolidBrush(AppTheme.Surface);e.Graphics.FillRectangle(fill,label.ClientRectangle);TextRenderer.DrawText(e.Graphics,label.Text,label.Font,new Rectangle(8,0,label.Width-16,label.Height),AppTheme.Text,TextFormatFlags.VerticalCenter|TextFormatFlags.Left);};
            card.Paint+=(_,e)=>{using var pen=new Pen(AppTheme.Border);e.Graphics.DrawRectangle(pen,0,0,card.Width-1,card.Height-1);};
            label.Paint+=(_,e)=>{using var pen=new Pen(AppTheme.Border);e.Graphics.DrawLine(pen,0,label.Height-1,label.Width,label.Height-1);};
        }
        Block(0,"01   CRIAR PLAYLIST",("Nome","Campeonatos"),("Adicionar uma partida",""),("Mapa (exemplo)","Clube"),("Display de navegação","Pro League"),("Modo de jogo","Bomba"));
        Block(0,"02   BANIMENTOS",("Ativar fase de banimento","Pro League"),("Tempo do banimento","20"));
        Block(0,"03   RODADAS",("Número de rodadas","12"),("Inverter atacante/defensor","6"),("Rodadas de prorrogação","3"),("Dif. pontos prorrogação","2"),("Inv. papéis prorrogação","1"),("Rotação de objetivo","2"),("Rotação do tipo de objetivo","Rodadas jogadas"),("Tempo da fase de escolha","15"));
        Block(1,"04   SAÚDE E DANOS",("Energia do agente","100"),("Dano de fogo amigo","100"),("Fogo amigo na preparação","Desligado"),("Fogo amigo reverso","Desligado"),("Ferido","20"));
        Block(1,"05   CONTROLE DO PERSONAGEM",("Correr","Ligado"),("Inclinar","Ligado"));
        Block(1,"06   MORTE",("Duração da morte","3"),("Replay da eliminação","Desligado"));
        Block(2,"07   INTERRUPÇÃO TÁTICA",("Solicitações por equipe","1"),("Permitir solicitações de","Jogadores"),("Duração da interrupção","45"),("Intervalo de troca de função","Ligado"),("Duração do intervalo","120"));
        Block(2,"08   MODO BOMBA",("Instalar / desativar","7 / 7"),("Detonação","45"),("Portador do desativador","Ligado"),("Preparação / ação","45 / 180"),("Repick","Ligado"));
        Block(2,"09   DUPLICAR E SALVAR",("Configure o primeiro mapa.",""),("Duplique e altere apenas o mapa.",""),("Repita para os 9 mapas do campeonato.",""),("Salve as condições de jogo.",""));
        Block(1,"MAP POOL",("Banco • Fronteira • Chalé",""),("Clube • Fortaleza • Café Dostoyevsky",""),("Laboratórios Nighthaven • Covil • Mansão",""));
        var note=new Label(){Text="Guia de consulta: configure os valores dentro do jogo. As partidas duplicadas mantêm as configurações.",AutoSize=false,Height=45};
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



using CasterDesk.Core;
namespace CasterDesk.Desktop;
internal static class LobbyView
{
 public static Control Create(int selected = 0, Action<int>? selectedChanged = null, string? backgroundMap = null)
 {
  var sections = new List<(string Title, (string Name,string Value)[] Rows)>();
  void Add(string title,(string Name,string Value)[] rows) => sections.Add((title, rows));
  Add("01   CRIAR PLAYLIST",new[]{( "Nome", "Campeonatos" ),( "Adicionar uma partida", "" ),( "Mapa (exemplo)", "Clube" ),( "Display de navegação", "Pro League" ),( "Modo de jogo", "Bomba" )});
  Add("02   BANIMENTOS",new[]{( "Ativar fase de banimento", "Pro League" ),( "Tempo do banimento", "20" )});
  Add("03   RODADAS",new[]{( "Número de rodadas", "12" ),( "Inverter atacante/defensor", "6" ),( "Rodadas de prorrogação", "3" ),( "Dif. pontos prorrogação", "2" ),( "Inv. papéis prorrogação", "1" ),( "Rotação de objetivo", "2" ),( "Rotação do tipo de objetivo", "Rodadas jogadas" ),( "Tempo da fase de escolha", "15" )});
  Add("04   SAÚDE E DANOS",new[]{( "Energia do agente", "100" ),( "Dano de fogo amigo", "100" ),( "Fogo amigo na preparação", "Desligado" ),( "Fogo amigo reverso", "Desligado" ),( "Ferido", "20" )});
  Add("05   CONTROLE DO PERSONAGEM",new[]{( "Correr", "Ligado" ),( "Inclinar", "Ligado" )});
  Add("06   MORTE",new[]{( "Duração da morte", "3" ),( "Replay da eliminação", "Desligado" )});
  Add("07   INTERRUPÇÃO TÁTICA",new[]{( "Solicitações por equipe", "1" ),( "Permitir solicitações de", "Jogadores" ),( "Duração da interrupção", "45" ),( "Intervalo de troca de função", "Ligado" ),( "Duração do intervalo", "120" )});
  Add("08   MODO BOMBA",new[]{( "Instalar / desativar", "7 / 7" ),( "Detonação", "45" ),( "Portador do desativador", "Ligado" ),( "Preparação / ação", "45 / 180" ),( "Repick", "Ligado" )});
  Add("09   DUPLICAR E SALVAR",new[]{( "Configure o primeiro mapa.", "" ),( "Duplique e altere apenas o mapa.", "" ),( "Repita para os 9 mapas do campeonato.", "" ),( "Salve as condições de jogo.", "" )});
  Add("MAP POOL",new[]{( "Banco • Fronteira • Chalé", "" ),( "Clube • Fortaleza • Café Dostoyevsky", "" ),( "Laboratórios Nighthaven • Covil • Mansão", "" )});
  var root = new Grid { Name = "lobby-guide", Background = Ui.Canvas, RowDefinitions = new("Auto,*"), Margin = new Thickness(4) };
  var banner = new Grid { Height = 108 };
  try { var catalog = SiegeCatalog.Load(); var map = catalog.Maps.FirstOrDefault(m => m.Id == backgroundMap) ?? catalog.Maps[Random.Shared.Next(catalog.Maps.Count)]; banner.Children.Add(new Image { Source = Ui.FileImage(SiegeCatalog.Asset(map.Image)), Stretch = Stretch.UniformToFill }); } catch (Exception ex) { AppLog.Write(ex.ToString()); }
  banner.Children.Add(new Border { Background = Brush.Parse("#CE101C29") });
  var titles = new StackPanel { Margin = new Thickness(20, 8) }; titles.Children.Add(Ui.Text("CONFIGURAÇÃO DE LOBBY", true)); titles.Children.Add(Ui.Text("Guia de referência para criar sua partida no Siege.")); banner.Children.Add(titles); root.Children.Add(banner);
  var body = new Grid { ColumnDefinitions = new("280,*"), Margin = new Thickness(0, 8, 0, 0) }; root.Children.Add(body); Grid.SetRow(body, 1);
  var navigation = new StackPanel { Spacing = 2 }; var page = new ContentControl { Name = "lobby-section", Margin = new Thickness(12, 0, 0, 0) };
  body.Children.Add(new ScrollViewer { Content = navigation }); body.Children.Add(page); Grid.SetColumn(page, 1);
  var buttons = new List<Button>();
  void Select(int index) {
   selected = index; selectedChanged?.Invoke(index);
   for(int n=0;n<buttons.Count;n++) { buttons[n].Background=n==index?Ui.Panel:Brushes.Transparent;buttons[n].BorderThickness=new Thickness(3,0,0,0);buttons[n].BorderBrush=n==index?Brush.Parse("#FF4335"):Brushes.Transparent; }
   var section=sections[index]; var content=new Grid { RowDefinitions=new("Auto,*,Auto"), Background=Ui.Panel, Margin=new Thickness(0) };
   var heading=Ui.Text(section.Title,true);heading.Margin=new Thickness(18,12);content.Children.Add(heading);
   var rows=new StackPanel { Spacing=3, Margin=new Thickness(16,0,16,12) };
   foreach(var row in section.Rows) {
    var line=new Grid { ColumnDefinitions=new("2*,*"), MinHeight=44 };
    var caption=Ui.Text(row.Name);caption.Margin=new Thickness(12,8);caption.VerticalAlignment=VerticalAlignment.Center;
    line.Children.Add(new Border { Background=Ui.Canvas,Child=caption });
    if(row.Value=="") Grid.SetColumnSpan(line.Children[0],2);
    else { var value=Ui.Text(row.Value);value.TextAlignment=TextAlignment.Center;value.VerticalAlignment=VerticalAlignment.Center;value.Margin=new Thickness(8);
     var field=new Border { Background=Ui.Canvas,BorderBrush=Brush.Parse("#617083"),BorderThickness=new Thickness(1),Child=value,Margin=new Thickness(3,0,0,0) };line.Children.Add(field);Grid.SetColumn(field,1); }
    rows.Children.Add(line);
   }
   var scroll=new ScrollViewer{Content=rows};content.Children.Add(scroll);Grid.SetRow(scroll,1);
   var note=Ui.Text("Guia de consulta: configure os valores dentro do jogo. As partidas duplicadas mantêm as configurações.");note.FontSize=12;note.Margin=new Thickness(18,8);content.Children.Add(note);Grid.SetRow(note,2);page.Content=content;
  }
  for(int n=0;n<sections.Count;n++) { int index=n;var button=Ui.Button("",()=>Select(index));button.Name="lobby-section-"+n;
   button.Content=new TextBlock{Text=L.T(sections[n].Title),TextWrapping=TextWrapping.Wrap,FontWeight=FontWeight.SemiBold};button.HorizontalContentAlignment=HorizontalAlignment.Left;button.Padding=new Thickness(14,12);button.Margin=new Thickness(0);
   button.PointerEntered+=(_,_)=>button.Background=Brush.Parse("#FF4335");button.PointerExited+=(_,_)=>button.Background=selected==index?Ui.Panel:Brushes.Transparent;
   navigation.Children.Add(button);buttons.Add(button); }
  Select(Math.Clamp(selected,0,sections.Count-1));return root;
 }
}

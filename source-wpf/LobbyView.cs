using System.Windows;
using System.Windows.Controls;
using CronoLive;
namespace CasterDesk.Wpf;
internal static class LobbyView
{
 public static UIElement Create()
 {
  var panel=new WrapPanel();
  void Add(string title,(string Name,string Value)[] rows){var content=new StackPanel{Width=290};content.Children.Add(Ui.Text(title,true));foreach(var row in rows)content.Children.Add(Ui.Text(L.T(row.Name)+ (row.Value==""?"":" · "+L.T(row.Value))));panel.Children.Add(Ui.Panel(content));}
  Add("01   CRIAR PLAYLIST",new[]{( "Nome", "Campeonatos" ),( "Adicionar uma partida", "" ),( "Mapa (exemplo)", "Clube" ),( "Display de navegação", "Pro League" ),( "Modo de jogo", "Bomba" )});
  Add("02   BANIMENTOS",new[]{( "Ativar fase de banimento", "Pro League" ),( "Tempo do banimento", "20" )});
  Add("03   RODADAS",new[]{( "Número de rodadas", "12" ),( "Inverter atacante/defensor", "6" ),( "Rodadas de prorrogação", "3" ),( "Dif. pontos prorrogação", "2" ),( "Inv. papéis prorrogação", "1" ),( "Rotação de objetivo", "2" ),( "Rotação do tipo de objetivo", "Rodadas jogadas" ),( "Tempo da fase de escolha", "15" )});
  Add("04   SAÚDE E DANOS",new[]{( "Energia do agente", "100" ),( "Dano de fogo amigo", "100" ),( "Fogo amigo na preparação", "Desligado" ),( "Fogo amigo reverso", "Desligado" ),( "Ferido", "20" )});
  Add("05   CONTROLE DO PERSONAGEM",new[]{( "Correr", "Ligado" ),( "Inclinar", "Ligado" )});
  Add("06   MORTE",new[]{( "Duração da morte", "3" ),( "Replay da eliminação", "Desligado" )});
  Add("07   INTERRUPÇÃO TÁTICA",new[]{( "Solicitações por equipe", "1" ),( "Permitir solicitações de", "Jogadores" ),( "Duração da interrupção", "45" ),( "Intervalo de troca de função", "Ligado" ),( "Duração do intervalo", "120" )});
  Add("08   MODO BOMBA",new[]{( "Instalar / desativar", "7 / 7" ),( "Detonação", "45" ),( "Portador do desativador", "Ligado" ),( "Preparação / ação", "45 / 180" )});
  Add("09   DUPLICAR E SALVAR",new[]{( "Configure o primeiro mapa.", "" ),( "Duplique e altere apenas o mapa.", "" ),( "Repita para os 9 mapas do campeonato.", "" ),( "Salve as condições de jogo.", "" )});
  Add("MAP POOL",new[]{( "Banco • Fronteira • Chalé", "" ),( "Clube • Fortaleza • Café Dostoyevsky", "" ),( "Laboratórios Nighthaven • Covil • Mansão", "" )});
  return new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
 }
}

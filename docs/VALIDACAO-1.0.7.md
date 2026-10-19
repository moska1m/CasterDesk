# CasterDesk 1.0.7 — bans no OBS

O programa já tinha a seleção revisada, mas o painel OBS ainda usava o seletor antigo e recebia Sem banimento no início. Esta atualização corrige os dois lados da conexão.

- Estado HTTP entrega operadores na ordem do jogo, com Sem banimento no final, tanto para DEF quanto ATQ. A opção continua habilitada e repetível conforme as regras existentes.
- Painel nativo também posiciona Sem banimento por último ao conectar a uma versão anterior do aplicativo.
- Seletor nativo usa os mesmos tamanhos para todas as opções: card de 72 px, ícone de 36×36, texto de 10 px abaixo do ícone, padding de 4 px e tooltip com o nome completo. Mantém três colunas adequadas à largura do dock.
- Cards de etapas futuras usam 50% de opacidade; liberados e registrados usam 100%. Bloqueios de ações continuam valendo.
- Sem banimento registrado mostra DEF/ATQ com o símbolo da função; tooltip, nome acessível, dados e remake preservam o significado explícito. As duas linhas por equipe do painel OBS foram mantidas.

Build local do aplicativo sem avisos ou erros; suíte headless aprovada, incluindo **91 checks HTTP OBS**, oito deles novos para conferir a posição de Sem banimento e a ordem dos operadores nas quatro escolhas iniciais. Os demais grupos da 1.0.6 continuam aprovados.

Painel nativo aprovado em **87 checks Qt sem abrir o OBS**, incluindo ordem antiga do servidor, dimensões iguais, opacidade futura e significado do Sem banimento registrado. Build confirmado no commit `603c361e35364b45a8b39e2afdb0f665c5f2169a`, em branch de compilação separada: https://github.com/moska1m/CasterDesk/actions/runs/39200402699. Não houve atualização de `main` nem publicação de release.

A atualização do painel exige substituir a DLL instalada no OBS. Ao usar o instalador principal, fechar OBS/CasterDesk e marcar **Instalar o painel CasterDesk no OBS**; atualizar apenas o executável do app mantém o painel anterior.

Não foram abertos OBS, aplicativo ou instaladores na área de trabalho. Instalação/UAC e uso visual dentro do OBS real permanecem para teste manual. A versão mantém as correções anteriores e a abertura maximizada.

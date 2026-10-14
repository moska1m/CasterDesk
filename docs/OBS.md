# CasterDesk dentro do OBS

## Plugin opcional — beta.0.12

1. Feche OBS e execute o instalador do CasterDesk.
2. Marque **Instalar o painel CasterDesk no OBS**. O componente OBS pede permissão de administrador; o aplicativo permanece instalado por usuário.
3. Abra CasterDesk e OBS. O plugin registra o painel CasterDesk e descobre a conexão automaticamente. Se estiver oculto, selecione **Painéis → CasterDesk**.

Não é necessário colar endereço. Escolher partida/mapa no CasterDesk atualiza o painel. Fechar CasterDesk desabilita controles; abrir novamente reconecta. Timer e bans continuam usando os mesmos serviços e arquivos do aplicativo. Veto, lados, confirmação final e revisão de mapas confirmados continuam no CasterDesk.

Suporte inicial: OBS Windows x64 31.1+, instalação convencional. O componente fica em ProgramData/obs-studio/plugins/casterdesk-obs. OBS portátil precisa da instalação do plugin no seu próprio diretório. A instalação não altera cenas, perfis ou outros plugins e não encerra OBS/transmissões. Plugin possui entrada própria de desinstalação **CasterDesk — Plugin OBS**.

A conexão é ativada automaticamente uma vez quando a instalação opcional foi concluída. Desativar em engrenagem → Painel OBS prevalece nas aberturas seguintes. Chave privada fica na configuração local; o plugin só acessa 127.0.0.1 e não a registra em logs. Documentos redirecionados ao OneDrive são descobertos pela pasta conhecida do Windows.

## Rounds, bans e mapas

Abas **Partida → Cronômetro**. Partida reúne registro rápido por botões de bombsite, vencedor e finalização, histórico abaixo e bans de operadores em seguida. Os bombsites têm altura uniforme e seleções destacadas. Clique em uma linha do histórico para corrigir; Novo round volta ao registro. A confirmação de uma correção não acrescenta outro round nem autoriza remover rounds posteriores. Mapas confirmados exigem abrir revisão no programa. Docks largos exibem também ataque/defesa no histórico; em docks estreitos essas duas colunas ficam ocultas para preservar a leitura do resultado.

Bans de cada etapa são liberados no próprio painel, na mesma aba dos rounds. Os lados e a prorrogação precisam estar configurados no programa.

No bloco Bans de operadores do CasterDesk, ligue **Plugin OBS** para impedir seletores automáticos depois dos rounds. Essa preferência fica salva e não desativa o cronômetro ou a conexão. Desligado, mantém o fluxo automático no programa. Clicar manualmente em um ban ou em Banir próximo continua funcionando em ambos os modos.

Em MD3/MD5, o seletor de mapa do plugin usa os mapas da partida e atualiza a seleção do programa. Em MD1 não há seletor editável. A seleção não apaga registros.

## Cronômetro e TXT

Controle iniciar/retomar, pausar, definir tempo, retirar/acrescentar um minuto e reiniciar. Configure tempo inicial, formato mm:ss/hh:mm:ss, mensagem final e três acréscimos em minutos; Aplicar configurações salva esses valores sem reiniciar a contagem em andamento. Definir tempo altera imediatamente a contagem; Resetar tempo volta à duração inicial salva. A mensagem final aparece ao encerrar a contagem. Contagem, configurações e TXT são compartilhados com o programa.

Copiar caminho copia somente a pasta que contém o TXT, no painel e no CasterDesk. No seletor de arquivos do OBS, abra essa pasta e escolha o TXT. A seleção de outro arquivo de saída continua na aba Textos para OBS do programa.

## Painel de navegador anterior

A opção manual da beta.0.9 mantém timer/bans, sem a nova aba de rounds: ativar a conexão, copiar endereço completo, fechar o diálogo e colar em Painéis de navegador personalizados. O endereço contém a chave; não o publique. Fonte de navegador na cena não é o painel de controle.

O dock antigo não é apagado automaticamente. Se instalar o plugin, pode remover manualmente o dock de navegador anterior para evitar duplicação.

## Validação

Testes da API, opt-in do instalador e controles Qt são automatizados em dados isolados. OBS nativo e instalação não foram abertos nesta entrega; carregamento real, encaixe e UAC precisam de validação. Não há controle de cenas, acesso entre computadores ou suporte Stream Deck nesta etapa.

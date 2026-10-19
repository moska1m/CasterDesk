# CasterDesk dentro do OBS

## Instalar o painel opcional

1. Feche OBS e execute o instalador do CasterDesk.
2. Marque **Instalar o painel CasterDesk no OBS**. O componente OBS pede permissão de administrador; o aplicativo permanece instalado por usuário.
3. Abra CasterDesk e OBS. O plugin registra o painel CasterDesk e descobre a conexão automaticamente. Se estiver oculto, selecione **Painéis → CasterDesk**.

Não é necessário colar endereço. Escolher partida/mapa no CasterDesk atualiza o painel. Fechar CasterDesk desabilita controles; abrir novamente reconecta. Timer e bans continuam usando os mesmos serviços e arquivos do aplicativo. Veto, lados, confirmação final e revisão de mapas confirmados continuam no CasterDesk.

Suporte inicial: OBS Windows x64 31.1+, instalação convencional. O componente fica em ProgramData/obs-studio/plugins/casterdesk-obs. OBS portátil precisa da instalação do plugin no seu próprio diretório. A instalação não altera cenas, perfis ou outros plugins e não encerra OBS/transmissões. Plugin possui entrada própria de desinstalação **CasterDesk — Plugin OBS**.

A conexão é ativada automaticamente uma vez quando a instalação opcional foi concluída. Desativar em engrenagem → Painel OBS prevalece nas aberturas seguintes. Chave privada fica na configuração local; o plugin só acessa 127.0.0.1 e não a registra em logs. Documentos redirecionados ao OneDrive são descobertos pela pasta conhecida do Windows.

## Rounds, bans e mapas

Abas **PARTIDA → REMAKE → CRONÔMETRO**. PARTIDA reúne registro por botões e bans, com seis cards por equipe em linhas separadas. Ícones, nomes e cores identificam operadores e responsáveis; posições permanecem estáveis nas duas metades. Cliques continuam seguindo DEF → ATQ. O confronto fica centralizado em caixa alta e negrito; mapa abaixo. A conexão aparece no cabeçalho preto, em verde/vermelho.

Bans de cada etapa são liberados no próprio painel, na mesma aba dos rounds. Os lados e a prorrogação precisam estar configurados no programa.

A partir da 1.0.7, Sem banimento fica no final da seleção e usa os mesmos tamanhos dos operadores. A ordem do jogo é preservada; a opção pode ser repetida. Bans futuros aparecem com 50% de opacidade. Atualize o painel pela opção **Instalar o painel CasterDesk no OBS** do instalador, com OBS e CasterDesk fechados.

No bloco Bans de operadores do CasterDesk, ligue **Plugin OBS** para impedir seletores automáticos depois dos rounds. Essa preferência fica salva e não desativa o cronômetro ou a conexão. Desligado, mantém o fluxo automático no programa. Clicar manualmente em um ban ou em Banir próximo continua funcionando em ambos os modos.

Em MD3/MD5, o seletor de mapa do plugin usa os mapas da partida e atualiza a seleção do programa. Em MD1 não há seletor editável. A seleção não apaga registros.

## Guia de remake

REMAKE fica vazio até escolher explicitamente o mapa, inclusive em MD1. Essa escolha é independente do mapa ativo em PARTIDA e nunca altera o registro ou os dados do jogo. Ao mudar de partida, a escolha do guia é limpa.

Após selecionar, confira equipes, ataque/defesa iniciais e atacante da prorrogação. Valores não preenchidos aparecem como Não definido. Bans mostram somente escolhas já registradas, separados por metade e com equipe responsável. Histórico em ordem crescente mostra somente número do round, bombsite e vencedor. O lembrete final pede conferir o mapa após configurar. O guia é para consulta; não configura o jogo automaticamente.

Duplo clique em um round do mapa ativo não confirmado abre sua correção em PARTIDA. Para corrigir outro mapa, primeiro selecione-o em PARTIDA. Novo round volta ao registro. Correções que removeriam dados posteriores permanecem recusadas; mapas confirmados exigem revisão no programa.

## Cronômetro e TXT

Controle iniciar/retomar, pausar, aplicar tempo, retirar/acrescentar um minuto e reiniciar. Configure tempo inicial, formato mm:ss/hh:mm:ss, mensagem final e os acréscimos em minutos salvos no programa (até oito); Aplicar configurações salva esses valores sem reiniciar a contagem em andamento. Aplicar tempo substitui imediatamente a contagem, preservando execução/pausa; Resetar tempo volta à duração inicial salva. A mensagem final aparece ao encerrar a contagem. Contagem, configurações e TXT são compartilhados com o programa.

Copiar caminho copia somente a pasta que contém o TXT, no painel e no CasterDesk. No seletor de arquivos do OBS, abra essa pasta e escolha o TXT. A seleção de outro arquivo de saída continua na aba Textos para OBS do programa.

## Painel de navegador anterior

A opção manual da beta.0.9 mantém timer/bans, sem a nova aba de rounds: ativar a conexão, copiar endereço completo, fechar o diálogo e colar em Painéis de navegador personalizados. O endereço contém a chave; não o publique. Fonte de navegador na cena não é o painel de controle.

O dock antigo não é apagado automaticamente. Se instalar o plugin, pode remover manualmente o dock de navegador anterior para evitar duplicação.

## Validação

Testes da API, opt-in do instalador e controles Qt são automatizados em dados isolados. OBS nativo e instalação não foram abertos nesta entrega; carregamento real, encaixe e UAC precisam de validação. Não há controle de cenas, acesso entre computadores ou suporte Stream Deck nesta etapa.

### Ajustes locais beta.0.14

O seletor do REMAKE permanece no topo do painel alto. O indicador de conexão usa o microfone aprovado verde/vermelho, com o estado abaixo, ao lado da marca em um cabeçalho compacto. Cards de operadores têm ícones e símbolos centralizados. A tela SPEC é uma alternativa no aplicativo, sem toggle do plugin; o painel OBS mantém suas três abas.

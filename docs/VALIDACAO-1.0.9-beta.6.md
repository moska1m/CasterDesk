# Validação local — 1.0.9-beta.6

O teste headless da beta.5 não reproduzia o laço nativo iniciado por `BeginMoveDrag`, nem a restauração pela barra de tarefas. Na execução real, o duplo clique podia ser consumido pelo início do arraste. A moldura sem decorações também removia o menu de sistema nativo da janela.

A janela agora mantém as decorações nativas com a área de conteúdo estendida, desenhando a moldura própria. A barra recebe o papel nativo de título, os botões recebem seus papéis de minimizar/maximizar/fechar e as bordas de redimensionamento ficam inativas quando maximizada. O Windows passa a cuidar do arraste, duplo clique e restauração. Os testes headless mantêm um caminho específico para a simulação.

## Verificação no Windows

`scripts/Test-NativeCaption.ps1` executa `--casterdesk-native-caption-check` com dados isolados. O teste cria sua própria janela e envia mensagens somente a esse HWND; não move o cursor nem controla outras aplicações.

63 verificações nativas passaram nos temas Padrão, Claro e Escuro:

- Estilos nativos de menu do sistema, minimizar e maximizar.
- Identificação do título, ícone e espaço vazio como área de legenda nativa.
- Mensagens de duplo clique maximizam e restauram a janela, verificando estado Avalonia e Win32.
- Identificação do botão de minimizar e do conteúdo principal em suas áreas corretas.
- Ação do botão de minimizar seguida de `SC_RESTORE`, a mesma operação do menu do sistema/barra de tarefas, retorna ao estado anterior normal ou maximizado.

A captura `native-window.png` foi revisada. O teste não simula um clique físico no ícone da barra de tarefas e não valida movimento entre monitores.

O instalador inclui a proteção de pós-instalação contra reinstalação do OBS em `/UPDATEAPP`. O painel nativo permanece o mesmo.

## Pacote e instalação

- Publicação final: `publish-casterdesk-1.0.9-beta.6-afc49e90`.
- Núcleo e suíte completa headless passaram; relatórios em `%TEMP%/CasterDesk-beta6-validation`, incluindo `casterdesk-ui-6bef8917c7f447d4a5e0b5b09f7b4e08`.
- A lógica nativa da moldura passou nas 63 verificações em `%TEMP%/CasterDesk-beta6-native/caption-fa2b64279a9e4df8a7c2676e365e4d80`. Após esse teste, só foram atualizadas as duas verificações headless que exigiam a antiga ausência de decorações. Não foram repetidas janelas de teste durante o trabalho do usuário.
- Instalador, manifesto de arquivos e anúncio assinado foram conferidos. Os dez tamanhos do ícone aprovado estão presentes no EXE e no Setup.
- A atualização local foi aplicada silenciosamente com `/UPDATEAPP` e a tarefa OBS desmarcada. O log confirma `Application update: preserving the installed OBS component and opt-in.`
- Marcador `obs-plugin.optin` e DLL nativa do OBS permaneceram idênticos aos anteriores à instalação, sem reinstalação do painel.
- `CasterDesk.exe`, `CasterDesk.dll` e `CasterDesk.Core.dll` instalados conferem com a publicação final. O CasterDesk foi aberto para uso após a atualização, conforme solicitado.

Logs: `build-config/package-beta6-final2.log` e `build-config/install-beta6.log`. Evidências locais: `build-config/beta6-installed.json`. Não houve envio ao GitHub.

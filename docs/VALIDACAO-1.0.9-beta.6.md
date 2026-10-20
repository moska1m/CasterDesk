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

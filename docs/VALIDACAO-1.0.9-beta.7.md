# Validação local — 1.0.9-beta.7

## Causa e correção

Ao estender o conteúdo para a barra de título, o Avalonia 12 cria `WindowDrawnDecorations` no `TopLevelHost`, acima de `Window.Template`. A moldura própria e essa camada padrão desenhavam os controles ao mesmo tempo. A camada também incluía um botão de tela cheia que não pertence à moldura do CasterDesk.

O estilo global de `WindowDrawnDecorations.Template` fica vazio, pois a moldura própria já desenha a legenda completa. Os estilos nativos, a área de título e os comportamentos de arraste, maximizar, minimizar e restaurar da beta.6 são preservados.

## Regressão

O teste anterior renderizava somente `Window`, sem a camada externa do host. `ChromeOverlayChecks` cria a mesma camada solicitada pelo Win32 no host da janela headless e verifica a árvore inteira. A regressão reproduziu exatamente os quatro botões extras: `PART_FullScreenButton`, `PART_MinimizeButton`, `PART_MaximizeButton` e `PART_CloseButton`.

Após a correção, passaram as seis combinações de Padrão/Claro/Escuro com estado normal/maximizado. As capturas do host completo foram revisadas: somente os três botões próprios permanecem. O teste foi incluído na suíte completa. As capturas do teste nativo também passam a incluir o host e a verificação de legenda única.

Nenhuma janela nativa de teste foi aberta nesta correção. Os testes usam dados isolados. A implantação local preserva o painel do OBS e utiliza o fechamento normal do aplicativo antes de substituí-lo.

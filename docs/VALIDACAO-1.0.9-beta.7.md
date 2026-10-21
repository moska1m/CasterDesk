# Validação local — 1.0.9-beta.7

## Causa e correção

Ao estender o conteúdo para a barra de título, o Avalonia 12 cria `WindowDrawnDecorations` no `TopLevelHost`, acima de `Window.Template`. A moldura própria e essa camada padrão desenhavam os controles ao mesmo tempo. A camada também incluía um botão de tela cheia que não pertence à moldura do CasterDesk.

O estilo global de `WindowDrawnDecorations.Template` fica vazio, pois a moldura própria já desenha a legenda completa. Os estilos nativos, a área de título e os comportamentos de arraste, maximizar, minimizar e restaurar da beta.6 são preservados.

## Regressão

O teste anterior renderizava somente `Window`, sem a camada externa do host. `ChromeOverlayChecks` cria a mesma camada solicitada pelo Win32 no host da janela headless e verifica a árvore inteira. A regressão reproduziu exatamente os quatro botões extras: `PART_FullScreenButton`, `PART_MinimizeButton`, `PART_MaximizeButton` e `PART_CloseButton`.

Após a correção, passaram as seis combinações de Padrão/Claro/Escuro com estado normal/maximizado. As capturas do host completo foram revisadas: somente os três botões próprios permanecem. O teste foi incluído na suíte completa. As capturas do teste nativo também passam a incluir o host e a verificação de legenda única.

Nenhuma janela nativa de teste foi aberta nesta correção. Os testes usam dados isolados. A implantação local preserva o painel do OBS e utiliza o fechamento normal do aplicativo antes de substituí-lo.

## Pacote e instalação

- Publicação: `publish-casterdesk-1.0.9-beta.7-fab8c428`.
- Núcleo e suíte completa de interface passaram na publicação final, em `%TEMP%/CasterDesk-beta7-validation`.
- Instalador, manifesto e anúncio assinado conferem. SHA-256 do Setup: `6466c1f2137cc929ae1b00ed8872d4a40a060dd6d6605bd9e30b02e2921a4730`.
- Todos os dez tamanhos do ícone aprovado estão presentes no aplicativo e no instalador.
- Atualização local aplicada com `/UPDATEAPP`, sem reinstalar o OBS. A DLL do painel e seu marcador de habilitação permaneceram idênticos aos anteriores.
- Executável e DLLs instalados conferem com a publicação testada. O aplicativo foi reaberto para uso.

Logs: `build-config/package-beta7.log` e `build-config/install-beta7.log`. Evidência local: `build-config/beta7-installed.json`. Não houve publicação no GitHub.

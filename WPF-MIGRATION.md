# Estado atual — 0.3.0-beta.0.4

O executável atual usa somente a interface WPF. Campeonato, configuração de lobby, seletores de mapas e operadores e Sobre foram substituídos por controles/janelas nativos. Não há WindowsFormsHost nem referência do aplicativo WPF ao engine legado.

A solução CasterDesk.slnx contém CasterDesk.Core e CasterDesk.Wpf. O núcleo reúne modelos, comandos MatchSession, persistência, recuperação, backup, OCR e renderização. Alguns arquivos compartilhados ainda são vinculados a partir de source; o projeto WinForms permanece no repositório para referência histórica, fora do instalador WPF.

A biblioteca Core usa a referência WindowsDesktop por causa de System.Drawing nos renderizadores. O aplicativo permanece exclusivo para Windows; extração do núcleo não o torna multiplataforma.

Compilação e empacotamento: scripts/Build-Wpf.ps1, com a versão lida de Directory.Build.props. Cada build gera uma pasta de publicação nova, executa os testes isolados e só depois compila o instalador. A assinatura é opcional e exige certificado e servidor de timestamp fornecidos.

Validação interativa do instalador, OBS e troca efetiva de DPI/monitor ainda pendente. Os testes de layout são fora da tela.

---

## Registro da primeira etapa

# CasterDesk 0.3.0-beta.0.1 — primeira etapa WPF

## Implementado

- Janela principal, navegação à esquerda e menu lateral em WPF. O menu ocupa uma coluna da própria janela, sem submenus que atravessam monitores.
- Central de imagens nativa: cinco categorias, prévia assíncrona, geração em segundo plano, logos e fundos personalizados, MVP e arquivo fixo de map veto para OBS. Ações principais permanecem visíveis durante a rolagem.
- Conferência de estatísticas nativa: print ao lado da tabela, abrir arquivo ou Ctrl+V, leitura local, revisão manual, dados parciais/remake e ausência de print.
- Textos de casting e cronômetro em WPF, mantendo os caminhos TXT existentes, oito arquivos e preferências.
- Claro/escuro aplicado imediatamente, fonte Segoe UI, arredondamento de layout e controles vetoriais.

## Compatibilidade e trabalho restante

Campeonato, configuração de lobby, seletores de bans e a janela Sobre ainda usam WinForms integrados. Esta versão **não é uma migração completa para WPF**. A próxima etapa é substituir campeonato e seus diálogos por vistas WPF sobre uma camada de comandos compartilhada, com testes das regras de rounds, veto e bans antes da troca.

O projeto WPF referencia o projeto existente como `CasterDesk.Engine`. Os modelos, persistência, backup, OCR e renderizadores das artes continuam compartilhados. Não foram alteradas posições dos PSDs, regras de partida ou formatos JSON. A separação em uma biblioteca sem dependência de WinForms fica para a próxima etapa.

## Compilar e verificar

`dotnet publish source-wpf/CasterDesk.Wpf.csproj -c Release -r win-x64 --self-contained true -o publish-wpf-0.3.0`

Use uma pasta de testes em `CASTERDESK_DATA_DIR` antes de executar `CasterDesk.exe --wpf-check`. A verificação renderiza as telas fora do monitor, sem `Window.Show`, e usa somente dados isolados. Verifica layouts em 1050, 1280 e 1920 pixels, estatísticas, textos, gravação/leitura de partida, duração, proporções das artes e os dois destinos do map veto.

Instalador: `ISCC.exe installer/CasterDesk-Wpf.iss` (Inno Setup 7 x64). O AppId e `CasterDesk.exe` permanecem iguais para atualizar a instalação anterior. Os dados continuam em Documentos/CasterDesk.

Ainda é necessária validação interativa do campeonato hospedado, mudança de DPI entre monitores, menus por teclado e OCR com prints reais. Nenhuma instalação ou janela de teste foi aberta no computador do usuário durante essa etapa.

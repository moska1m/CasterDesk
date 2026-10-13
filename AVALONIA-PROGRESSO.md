# Avalonia — primeira etapa, 13/10/2026

## Estado conferido

A base encontrada foi **0.3.0-beta.0.4**, commit `7bf6a74`, já registrado localmente antes desta etapa. `ESTABILIZACAO-0.3.0-beta.0.4.md`, `PROGRESSO-ESTABILIZACAO.md` e `WPF-VALIDATION.md` registram a estabilização anterior. Não reconstruí versões antigas nem executei uma migração para WPF.

O núcleo já contém comandos com reversão, JSON canônico/compatibilidade antiga, revisão auditada, transações recuperáveis, estatísticas por campo/identidade, pacotes completos, backup/restauração e renderização das artes baseada nos PSDs. Os relatórios anteriores descrevem verificações WPF; isso **não valida controles Avalonia**.

## Alterações desta etapa

- `source-core` deixou de usar `UseWPF`. Continua dependente do runtime Windows Desktop para System.Drawing/OCR; não contém controles WPF. O projeto antigo foi preservado e a instalação existente não foi substituída.
- `source-avalonia` inicia uma Central de imagens separada, com seletor compartilhado compacto e cinco categorias. Segoe UI na interface; renderizador, fontes, coordenadas e recursos das artes mantidos. Anúncio oferece somente feed/story, campeonato, logos e canal, sem data/horário.
- Apresentação Avalonia da prévia, fundos/logos personalizados, cópia do caminho OBS e geração em segundo plano. Liberação explícita das imagens descartadas; pedidos de prévia antigos não substituem os novos.
- Revisão editorial do veto preserva `arte.json`; diálogo Avalonia pergunta se a correção também deve ser aplicada à partida. O comando compartilhado impede substituir dados de mapas já registrados.
- Conferência de estatísticas em Avalonia: print/dados lado a lado, edição E/M/A, OCR cancelável, proteção de edição manual, opção parcial/remake/sem print, identidade por associação explícita e gravação conjunta print/estatísticas. A confirmação continua obrigatória. Clipboard usa um serviço Windows sem criar controles WinForms.
- `CasterDesk.slnx` aponta para Avalonia/núcleo/verificações; `CasterDesk.Legacy.slnx` preserva a solução anterior. Scripts novos não lançam janelas: a validação de UI seleciona exclusivamente o backend Headless.

**Situação dos novos controles: código preparado, ainda não compilado nem visualmente validado.** Não há instalador ou executável Avalonia entregue. A primeira etapa não inclui uma interface de campeonato de fachada: essa área continuará no programa existente até a adaptação efetiva.

## Evidência executada

Núcleo e executor de verificações compilados com .NET 10: **zero erros e zero avisos**. Execução em dados isolados, sem criar janelas:

- 599 verificações de importação e idiomas: PT/EN/ES, nove mapas/36 bombsites, defaults/personalizados, IDs desconhecidos e bloqueio de bombsites.
- 79 verificações de transações/recuperação, reversão após falha, revisão auditada, alternância DEF/ATQ, MD1/MD3/MD5 completos e prorrogação 8–6, pacotes e estatísticas parciais.
- 22 verificações de backup/restauração, conflitos, integridade, duplicatas e caminhos inválidos.
- 13 PNGs comparados por pixels/dimensões/transparência.

Relatórios: `test-results/avalonia-core-01bcbe0c6a7342e0b51135c7795ae989/`. Total **713 verificações**. OCR: resultados arquivados, não nova leitura de prints pelo Windows; 30 linhas, 25 campos sem leitura e um valor diferente da referência manual. Não elimina a necessidade de confirmação.

O acesso ao NuGet pela execução local falhou por resolução de host. Restore offline confirmou ausência de `Avalonia.Desktop`, `Avalonia.Themes.Fluent` e `Avalonia.Headless` 12.0.0 (NU1100). Portanto **Build-Avalonia/Test-Avalonia não foram aprovados**. A versão dos pacotes segue o exemplo oficial: https://docs.avaloniaui.net/docs/how-to/coded-ui-how-to. Teste em memória: https://docs.avaloniaui.net/docs/testing/setting-up-the-headless-platform.

## Pendências reais

1. Restaurar os pacotes, compilar, corrigir eventuais incompatibilidades e executar/inspecionar as prévias Headless da Central e os fluxos de estatísticas. O roteiro está preparado, não executado.
2. Adaptar campeonato/lobby, fluxo progressivo, rounds/histórico, logos e seletores/cards de bans para Avalonia, reaproveitando `MatchSession`. Conferir layouts, revisão e teclado.
3. Adaptar menu/engrenagens, configurações, cronômetro/TXTs e interface de backup/restauração; conferir traduções de todos os novos controles. Os serviços e testes correspondentes já existem.
4. Validar DPI/monitores, clipboard/OCR real, instalação/atualização e operação com OBS após aviso ao usuário. Não houve abertura do programa, instalador ou OBS nesta etapa.
5. Consolidar empacotamento Avalonia e distribuição. Plugin OBS continua futuro; timeline cancelada; revisão regional do catálogo e diretrizes de uso continuam pendentes conforme relatório anterior.

## Reproduzir

Windows, SDK .NET 10. `scripts/Test-Core.ps1 -Dotnet <dotnet.exe>` executa somente o núcleo. `scripts/Build-Avalonia.ps1 -Dotnet <dotnet.exe>` exige dependências, compilação e teste Headless antes de considerar a etapa validada. `-RestoreConfig` aceita um arquivo NuGet explícito. A execução normal da aplicação não faz parte desses scripts. Não gerar instalador a partir de uma compilação/teste incompleto.

O código e os recursos versionados são preservados no snapshot local. `dist`, `publish*`, dados pessoais e relatórios temporários não entram no Git. Nenhum envio remoto foi realizado nesta etapa.

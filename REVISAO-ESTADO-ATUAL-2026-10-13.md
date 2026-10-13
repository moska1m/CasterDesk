# Revisão do CasterDesk — 13/10/2026

## Escopo e limites

Revisão estática transversal do código atual, projetos .NET, fluxo WPF/WinForms, modelos de partida, persistência, backup, OCR, renderização, localização, scripts de testes, instalador e registros de validação. Método ECC: reconhecimento da arquitetura, impacto entre módulos e auditoria de prontidão com evidência local.

Não foram alterados arquivos do programa, executados novos testes, abertas janelas, instaladas versões ou consultado o GitHub remoto. Este relatório é o único arquivo criado nesta revisão. As validações anteriores são evidência histórica, não uma nova execução. Não se trata de certificação de segurança, desempenho ou licenciamento.

Classificação:

- **C:** comportamento identificável diretamente no código; não significa reproduzido nesta revisão.
- **R:** risco sustentado pelo código, com cenário de falha ainda não reproduzido.
- **M:** melhoria de produto, arquitetura ou manutenção; não é um defeito por si só.
- **P1:** proteger dados e funcionamento básico; corrigir primeiro.
- **P2:** melhorar operação e previsibilidade antes de ampliar a beta.
- **P3:** manutenção, acabamento e evolução.

## O que já está bem encaminhado

- Partidas importadas têm validação de IDs, formatos, veto, lados, operadores, rounds, finalizações e estatísticas.
- Existem salvamentos com arquivo temporário e cópia anterior. O registro rápido de round desfaz a alteração em memória se a gravação falhar.
- O backup confere hashes e tamanhos, limita volume, rejeita caminhos fora do destino e links, e guarda uma cópia anterior à restauração.
- Estatísticas distinguem completos, parciais e ausentes; o OCR é local e depende de confirmação humana.
- O caminho fixo de map veto para OBS é atualizado depois de gravar as cópias de arquivo.
- As artes compartilham renderizadores baseados nas posições dos PSDs. Há testes anteriores de PNG que preservam pixels e transparência.
- A primeira etapa WPF possui navegação, menu, Central, estatísticas e textos nativos, sem exigir uma troca imediata dos modelos de dados.

## 1. Salvamento, recuperação e consistência

| ID | Tipo / prioridade | Situação e melhoria |
|---|---|---|
| D01 | C / P1 | `ChampionshipPanel.FlushNames()` para o temporizador e ignora o retorno de `Persist()`. Troca de partida e fechamento WPF podem continuar após falha de gravação dos nomes. Retornar sucesso/erro, manter estado pendente e impedir a operação dependente quando necessário. |
| D02 | C / P1 | `SetOperator`, `SetSide`, `PickMap`, `ChangeSetup` e a edição de histórico não aplicam a mesma política de rollback do registro rápido. Unificar comandos: validar, salvar e só então assumir sucesso; em falha, restaurar o estado anterior ou mostrar explicitamente alterações pendentes. |
| D03 | R / P1 | A configuração é desserializada sem normalizar seu conteúdo. `People` nulo/curto, elementos nulos ou `Presets` nulo podem quebrar a construção da tela de textos, que ocorre já na abertura do aplicativo. Validar estrutura, faixas, caminhos e idioma antes de montar qualquer tela. |
| D04 | C / P1 | Erros de leitura das preferências e de `arte.json` podem resultar silenciosamente em defaults. Preservar o arquivo problemático e avisar que houve recuperação, em vez de permitir que um salvamento posterior substitua a configuração sem explicação. |
| D05 | R / P1 | Não foi encontrado controle de instância única. Duas instâncias podem escrever os mesmos JSONs, usar o mesmo `.tmp` e atualizar o arquivo do OBS. Impedir uma segunda instância gravadora ou implementar coordenação explícita de acesso. |
| D06 | C / P1 | `StatsWindow.Save()` substitui o print antes de salvar o JSON. Se o JSON falhar, os dados antigos são restaurados, mas o print anterior já pode ter sido substituído. Gravar print com identidade nova e confirmar sua referência junto dos dados; limpar o novo arquivo se a operação falhar. |
| D07 | C / P1 | `MatchStore.Save()` pode reverter o JSON principal se `Snapshot()` falhar, mas o `partida.json` da pasta da partida já pode ter sido atualizado antes da falha no LEIA-ME. Definir uma única fonte oficial e tornar o conjunto consistente por transação ou recuperação explícita. |
| D08 | R / P2 | A exportação salva vários arquivos separadamente. Falha no meio pode deixar um conjunto de PNGs parcialmente renovado. Preparar o lote em staging, registrar um manifesto de geração e apresentar quais arquivos foram concluídos. Preservar a publicação tardia do arquivo fixo do OBS. |

**Evidência:** `source/ChampionshipPanel.cs` (FlushNames, Persist, SetOperator, SetSide, PickMap, ChangeSetup, RoundChanged); `source/MatchModel.cs` (Save, Atomic); `source/MatchFiles.cs` (Snapshot, ReadBrand, Export); `source-wpf/TextsView.cs` (construtor); `source-wpf/StatsWindow.cs` (Save); `source-wpf/Shell.cs` (Closing, Navigate, Flush).

**Verificação necessária:** simular arquivo bloqueado, pasta sem permissão, configuração JSON sintaticamente válida mas estruturalmente inválida, duas instâncias e falha entre cada gravação. Confirmar tanto os arquivos quanto o estado exibido na tela.

## 2. Cronômetro e textos para OBS

| ID | Tipo / prioridade | Situação e melhoria |
|---|---|---|
| T01 | C / P2 | Alterar Tempo inicial e clicar Iniciar não aplica esse novo valor: Iniciar usa o Countdown existente; Resetar é que lê o campo. Definir comportamento claro para iniciar uma contagem nova e para retomar uma contagem pausada. |
| T02 | C / P2 | O caminho TXT é editável, mas a atualização manual não invalida o cache `last`. Com a contagem pausada e texto igual, o novo arquivo pode não receber nada. Validar a mudança de caminho e forçar a primeira escrita no novo destino. |
| T03 | C / P2 | `TextsView.Save()` valida o tempo antes de salvar os participantes. Um tempo inválido impede Aplicar no OBS para casting e pode bloquear o fechamento da janela. Separar a validação e persistência desses dois recursos. |
| T04 | C / P2 | `24:00:00` é aceito pelo parser, mas a inicialização usa o componente `hh` de TimeSpan, que volta a zero em 24 horas. Preservar horas totais, ou limitar a duração de forma coerente no campo e no modelo. |
| T05 | C / P2 | A saída do cronômetro só começa no evento Loaded da tela de textos. Antes da primeira visita à aba, não há atualização por esse fluxo WPF. Definir se o serviço de saída deve iniciar com o programa e testar esse contrato. |
| T06 | M / P3 | Os botões adicionais usam os presets salvos, mas a tela WPF não oferece a edição dos valores que existia na interface anterior. Recuperar a personalização; mostrar caminho válido/inválido, último horário escrito e estado da contagem de maneira consistente. |

**Evidência:** `source-wpf/TextsView.cs`, especialmente construtor, Loaded, Update, Parse e Save; comparação com `source/Program.cs` e `source/CastingPanel.cs`.

**Verificação necessária:** editar duração sem resetar, pausar e mudar caminho por digitação, salvar casting com tempo inválido, usar 24 horas, iniciar sem visitar a aba e manter contagem durante navegação/backup.

## 3. Estatísticas e OCR

| ID | Tipo / prioridade | Situação e melhoria |
|---|---|---|
| S01 | C / P2 | A soma da série exclui completamente um jogador de um mapa quando qualquer valor E/M/A está ausente. Mostrar cobertura por jogador e por campo; definir com clareza como os valores conhecidos entram nos totais parciais, sem transformar ausência em zero. |
| S02 | R / P2 | A identidade do jogador depende de equipe + nome normalizado. Pequenas variações de OCR ou de nickname entre mapas podem criar jogadores diferentes na série. Adotar um elenco com identidade estável e uma etapa de conciliação dos nomes lidos. |
| S03 | C / P2 | O modelo admite até 20 jogadores nas estatísticas, mas a janela apresenta somente cinco por equipe. Dados importados com jogadores adicionais podem ser descartados ao confirmar. Alinhar o limite do modelo à UI ou preservar e permitir revisar os registros adicionais. |
| S04 | M / P2 | Uma nova leitura de OCR pode substituir preenchimentos manuais. Pedir confirmação quando houver edição pendente, oferecer comparação e marcar campos vazios/duvidosos. A heurística atual não fornece uma pontuação de confiança confiável. |
| S05 | R / P2 | Novas leituras invalidam o resultado antigo pelo contador de geração, mas não cancelam imediatamente o trabalho anterior; o token principal só é cancelado ao fechar. Cancelar a leitura substituída e controlar qual pedido pode atualizar equipe, campos e estado de ocupado. |
| S06 | M / P2 | O OCR depende dos componentes e idiomas disponíveis no Windows. Fazer um diagnóstico legível, com entrada manual sempre disponível; ampliar fixtures de prints, resoluções, cortes de transmissão e trocas de ordem das equipes. |

**Evidência:** `source/StatisticsData.cs` (Validate, Sum, ReadImage, Parse), `source/OcrImage.ps1`, `source-wpf/StatsWindow.cs` (Fill, Read, Save).

## 4. Central de imagens, fidelidade e desempenho

| ID | Tipo / prioridade | Situação e melhoria |
|---|---|---|
| A01 | C / P2 | Prévias antigas são descartadas somente depois que a renderização termina. O renderizador tem um lock global; alternar rapidamente as opções pode acumular trabalho inútil. Agrupar mudanças próximas, manter só o pedido mais recente e medir o tempo até a prévia atual. |
| A02 | C / P2 | `ShowPreview()` chama `Ui.Bitmap()`, que codifica PNG e o decodifica na continuação da interface. Preparar uma BitmapSource congelada em segundo plano ou usar conversão direta; reservar a thread de UI para apresentar o resultado. |
| A03 | M / P2 | A prévia é produzida na resolução final e a exportação renderiza novamente. Usar thumbnail adequada ao painel e cache limitado pelo conteúdo, mantendo a renderização final independente e fiel ao PSD. Medir antes de prometer ganho. |
| A04 | M / P2 | Erros e requisitos aparecem principalmente em mensagens depois de tentar renderizar. Mostrar antes o que falta: veto completo, lados, rounds ou estatísticas conferidas. Disponibilizar categorias e ações conforme os dados existentes. |
| A05 | M / P3 | Ao escolher Map veto, o formato vira horizontal e não se restaura automaticamente o formato anterior ao mudar de categoria. Guardar formato e mapa selecionado por categoria/partida. Tornar clara a diferença entre a partida selecionada na Central e a ativa no campeonato. |
| A06 | M / P2 | Exportar/importar JSON transfere dados, mas não leva automaticamente logos, fundos e prints referenciados. Oferecer pacote completo de uma partida, separado da exportação somente de JSON e do backup geral. |
| A07 | M / P2 | A fidelidade visual merece testes de comparação com referências aprovadas em anúncio, resultado e stats, para MD1/MD3/MD5 e os três formatos. Incluir nomes longos, logos extremos, dados incompletos e BG personalizado. Preservar PSDs, posições e requisitos já aprovados. |

**Evidência:** `source-wpf/StudioView.cs`, `source-wpf/Ui.cs` (Bitmap), `source/StudioRenderer.cs` (gate, Render), `source/MatchFiles.cs` (Export, Resource), `source/JgArtRenderer.cs`, importação/exportação em `source/ChampionshipPanel.cs`.

Não foi feito benchmark novo. Há trabalho síncrono e fila potencialmente desperdiçada identificáveis no código, mas não há uma medição nova de travamento ou tempo economizável.

## 5. Interface, fluxo e regras de partidas

| ID | Tipo / prioridade | Situação e melhoria |
|---|---|---|
| U01 | M / P2 | Concluir a migração de campeonato, guia de lobby e seletores de bans. Antes disso, extrair os comandos e regras para uma biblioteca independente; a nova UI não deve depender de um painel WinForms para administrar partidas. |
| U02 | R / P2 | A janela de estatísticas tem tamanho inicial fixo 1280×780. Adaptar à área útil do monitor, escala do Windows e tamanho disponível. Testar também a redução da área do campeonato ao abrir o menu lateral. |
| U03 | M / P2 | Padronizar dimensões, espaçamentos, títulos, avisos, ações primárias, foco e estados bloqueados em WPF e nas telas transitórias. Tornar estados compreensíveis por texto/forma, além de cor e opacidade. |
| U04 | M / P2 | Documentar e testar a sequência de liberação: equipes/setup → veto → lados → bans/rounds. Mostrar por que uma ação está bloqueada. Parte dessas travas já existe; a melhoria é garantir consistência entre todos os caminhos. |
| U05 | M / P2 | Adicionar revisão administrativa de mapa confirmado com confirmação, backup e histórico de alterações. Hoje o bloqueio de resultados é intencional e não há essa revisão; não deve ser removido sem controle. |
| U06 | C / P3 | Nomes de equipe estão limitados a 12 caracteres na UI e no validador. Considerar nome completo + nome curto de exibição, em vez de impor a abreviação ao dado principal; ajustar artes e migração de JSON junto. |
| U07 | M / P3 | Padronizar operação por teclado e rótulos de acessibilidade; manter a engrenagem destacada enquanto o menu está aberto. O template WPF atual reage ao hover/foco, sem um estado específico de menu aberto. |

**Evidência:** `source-wpf/Shell.cs`, `source-wpf/StatsWindow.cs`, `source-wpf/App.xaml`, `source-wpf/CasterDesk.Wpf.csproj`, `source/ChampionshipPanel.cs`, `source/MatchModel.cs`, `WPF-MIGRATION.md`.

Regras de pontuação, cooldown de bombsite, bans por etapa e compatibilidade de finalização já estão implementadas. Nenhuma alteração dessas regras é proposta sem testes e validação do requisito. Não houve verificação online das regras competitivas atuais nesta revisão.

## 6. Backup, restauração e recuperação

| ID | Tipo / prioridade | Situação e melhoria |
|---|---|---|
| B01 | M / P2 | Mostrar progresso, volume, destino e resumo do backup/restauração. Tornar o conteúdo incluído/excluído visível, sobretudo TXT configurado fora de Documentos/CasterDesk. |
| B02 | R / P2 | O backup permite ler arquivos enquanto são escritos; o cronômetro continua executando enquanto a UI fica desabilitada. Criar um snapshot coerente do conjunto de dados, mantendo o relógio em funcionamento mas coordenando suas escritas durante a captura. |
| B03 | C / P2 | Reservas de arquivo usam comparação sem diferenciar caixa para Backups, mas usam igualdade sensível a caixa para `pending-restore.json`. Normalizar esse controle ao comportamento do sistema de arquivos Windows. Não foi reproduzida exploração desse ponto. |
| B04 | M / P2 | Na restauração, validar também a estrutura dos dados do aplicativo, não somente sintaxe JSON/hash. Exibir arquivos incompatíveis e conflitos de forma mais específica antes de aplicar. |
| B05 | C / P3 | Diretórios de verificação/restauração, cópias auxiliares e arquivos temporários podem acumular; o filtro de backup exclui `.tmp`, mas não todos os temporários `.tmp-GUID`. Definir limpeza segura e retenção de backups sem apagar dados de usuário silenciosamente. |

**Evidência:** `source/DataBackup.cs` (Files, Create, Extract, Apply, ProcessPending), `source-wpf/Shell.cs` (Backup, Restore), `source/MatchFiles.cs` (AtomicText, AtomicImage).

Manter a verificação de hashes, validação de caminho, tratamento de conflitos e cópia anterior. Uma política de retenção deve ser configurável, não uma exclusão automática indiscriminada.

## 7. Idiomas e catálogo

| ID | Tipo / prioridade | Situação e melhoria |
|---|---|---|
| L01 | C / P2 | Há mensagens do backup em português fixo e a mensagem pós-desinstalação também é fixa em português. Localizar esses caminhos de erro e instalação, além das telas comuns. |
| L02 | M / P2 | Ampliar os testes de tradução para as telas WPF e mensagens dinâmicas. A amostra de rótulos WPF revisada apresentou cobertura ampla: não há evidência para repetir que toda a interface nova esteja sem tradução. Falta verificar o fluxo completo nos três idiomas e com textos longos. |
| L03 | M / P3 | Centralizar nomes regionais de mapas/bombsites, ordem de operadores e versão do map pool em catálogo revisável. Preservar IDs estáveis no JSON e não alterar partidas históricas ao atualizar o catálogo. Revisão de casters nativos e fontes oficiais permanece necessária. |

**Evidência:** `source/Localization.cs`, `source/translations.json`, `source/DataBackup.cs`, `installer/CasterDesk.iss`, `source/OperatorOrder.cs`, catálogo Siege e modelos de partida.

## 8. Manutenção, diagnóstico, testes e distribuição

| ID | Tipo / prioridade | Situação e melhoria |
|---|---|---|
| E01 | C / P1 | O ponto de entrada WPF não instala `AppLog`, e o tratamento atual cobre a inicialização, não a política de erros da Dispatcher durante o uso. Implementar registro de diagnóstico WPF, mensagens de recuperação e tratamento de tarefas assíncronas sem fingir que qualquer exceção permite continuar com segurança. |
| E02 | M / P2 | Separar modelos, serviços, regras, renderização, UI e testes. `Program.cs` reúne lógica reutilizável, tela e testes; `ChampionshipPanel` reúne comandos, apresentação e verificações. Evitar manter regras duplicadas entre a UI antiga e a nova. |
| E03 | C / P2 | O script principal de testes aponta por padrão para WinForms; o script de build WPF não exige testes antes de gerar instalador. Criar uma verificação de release que inclua os testes relevantes dos dois caminhos durante a transição. |
| E04 | M / P2 | Cobrir ponta a ponta criação, veto, lados, bans, rounds, overtime, correções, confirmação, OCR, exportação e restauração. Incluir múltiplos monitores/DPI, baixa resolução, arquivos bloqueados e instalação sobre versão anterior. Os testes estáticos de layout não provam esses cenários. |
| E05 | C / P2 | Versão, caminho de publicação e nomes de artefato aparecem repetidos no projeto, Shell, scripts e instalador. Usar uma fonte de versão e gerar metadados de release; publicar em diretório limpo para não empacotar sobras. |
| E06 | C / P2 | Há arquivos centrais atuais ainda não rastreados no checkout, incluindo `source-wpf`, `MatchFiles`, `StatisticsData` e Build-Wpf. Registrar o estado validado em commits e tags com notas coerentes. Não foi verificado se há cópia equivalente no GitHub remoto. |
| E07 | M / P2 | O build WPF não oferece o mesmo caminho opcional de assinatura do script antigo. Unificar empacotamento, assinatura quando disponível, hashes, licenças, testes e verificação do conteúdo real do instalador. A documentação existente já registra pendências de autorização de recursos; não se realizou nova avaliação jurídica. |
| E08 | M / P3 | Organizar uma solução com os projetos, documentação de entrada atualizada e formatação legível. Muitos handlers longos em uma linha tornam revisão e manutenção mais difíceis. Consolidar componentes sobrepostos após verificar quais ainda têm consumidores. |

**Evidência:** `source-wpf/App.xaml.cs`, `source/AppLog.cs`, projetos .csproj, `scripts/Test.ps1`, `scripts/Build-Wpf.ps1`, `scripts/Build-Release.ps1`, scripts Inno, `git status`/`git log`, `THIRD-PARTY-NOTICES.md`, `WPF-VALIDATION.md`.

## Prioridade sugerida

1. **Correção de confiabilidade:** D01–D07 e E01; configuração segura, resultado de salvamento explícito, consistência de JSON/prints e proteção contra escrita concorrente.
2. **Regressões de operação:** T01–T05; cronômetro, caminhos TXT e separação de casting/configuração.
3. **Base de verificação:** E03–E06; testes de falhas e fluxo completo, release reproduzível e estado registrado em Git.
4. **Migração WPF e uso diário:** U01–U07; comandos compartilhados antes de substituir as telas finais.
5. **Estatísticas e Central:** S01–S06, A01–A07; identidade/cobertura, OCR e prévias mais eficientes, sem redesenhar as artes aprovadas.
6. **Backup e acabamento:** B01–B05, L01–L03 e manutenção restante.

Integração de controle com OBS, nova marca e novos modelos de artes continuam evoluções de produto. Não são requisitos para corrigir as falhas atuais e não devem deslocar a estabilização dos dados, da interface e do processo de release.

## Conclusão

O projeto possui recursos reais e proteções úteis, mas permanece numa beta híbrida com políticas de erro inconsistentes e validação interativa incompleta. Compilar e gerar PNGs corretamente não basta para afirmar que uma transmissão inteira será confiável. O próximo trabalho deve fechar falhas de salvamento/configuração, recuperar a previsibilidade do cronômetro e estabelecer testes do fluxo real antes de ampliar a migração e a distribuição.

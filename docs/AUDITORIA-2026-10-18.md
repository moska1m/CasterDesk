# Revisão local do CasterDesk 1.0.5 — 18/10/2026

Registro histórico da auditoria antes das correções. Implementação e validação posteriores: [1.0.6](VALIDACAO-1.0.6.md).

Triagem de produção: **68/100 — corrigir as perdas silenciosas antes da próxima distribuição pública**. Nota qualitativa de priorização, não certificação nem medida matemática de qualidade. Há seis falhas reproduzidas, sendo duas de integridade de histórico/dados. A revisão foi delimitada aos caminhos abaixo; não significa que todo o projeto esteja livre de outros problemas.

## Escopo e método

- Estado: commit-base `5464c1c` mais alterações locais 1.0.4/1.0.5, preservadas.
- Skills consultadas: `ecc:production-audit`, `ecc:click-path-audit` e `ecc:benchmark-optimization-loop`.
- Leitura do motor compartilhado, chamadas de UI, persistência, estatísticas/artes, backup/pacotes, cronômetro/TXT, servidor e plugin OBS e empacotamento.
- Reproduções com `CASTERDESK_DATA_DIR` isolado e dados fictícios; testes gráficos usam Avalonia.Headless/Skia. Nenhuma janela nativa do CasterDesk, OBS ou instalador foi aberta.
- Nenhum código de produção corrigido nesta auditoria; nenhum dado real, commit, push ou serviço externo de auditoria utilizado.
- Os 289 checks de campeonato e demais grupos da publicação continuam sendo evidência anterior da 1.0.5, não prova contra os novos casos. Foram construídos pequenos executáveis locais para reproduzir os achados.

## Bugs reproduzidos

Todos foram classificados P2 (impacto relevante, com condições específicas e sem evidência de perda irrecuperável de toda a base). A ordem abaixo representa a prioridade sugerida.

### 1. Substituição de mapa pode descartar estatísticas sem confirmação

**Fonte:** `source/MatchModel.cs:35`, `source-core/MatchSession.cs:61,91`, `source-avalonia/ChampionshipDialogs.cs:40`, `source-avalonia/StudioView.cs:111`.

O mapa pode ter estatísticas salvas sem rounds ou bans. `HasData` ignora essas estatísticas; os guardas do veto e da seleção do SPEC consideram esse mapa vazio. Reproduzido no SPEC e no veto normal MD3: após substituir o mapa, um jogador salvo passa de 1 para 0 nos dados correntes. No SPEC foi verificado também o JSON recarregado, com zero jogadores.

**Correção recomendada:** um critério explícito de dados que precisam ser preservados, incluindo estatísticas/prints, usado por substituição, reset de formato e aplicação do veto da arte. Não alterar `HasData` indiscriminadamente, pois também participa da validação de lados. Exigir confirmação e snapshot recuperável antes de substituir.

Desfazer e cópia anterior podem permitir recuperação, mas dependem de o usuário perceber o descarte.

### 2. Alterar o lado da prorrogação apaga finalizações de rounds

**Fonte:** `source-core/MatchSession.cs:67`; caminho de UI em `source-avalonia/ChampionshipView.cs:248`.

Reprodução: 6–6 após 12 rounds; round 13 vencido pela equipe 0 com `defuser_completed`; trocar o atacante de OT de 0 para 1. O vencedor e o bombsite são mantidos, mas `Ending` passa a vazio e é salvo. O seletor OT chama esse comando diretamente, sem confirmação desse descarte.

**Correção recomendada:** validar as finalizações dependentes antes da alteração; recusar preservando tudo ou abrir uma revisão explícita dos rounds afetados. Não limpar finalizações silenciosamente.

### 3. Equipe no topo do print volta ao padrão após reabrir

**Fonte:** `source-avalonia/StatsWindow.cs:38,142,155`; `source/StatisticsData.cs:15`.

Reprodução headless pela janela real e seu método de salvar: selecionar equipe 1, confirmar e reabrir. Resultado: `BeforeSaveTopTeam=1`, `AfterReopenTopTeam=0`, `PrintStillPresent=true`. A seleção não faz parte do modelo persistido.

**Impacto:** “Ler novamente” captura esse índice padrão e pode atribuir os jogadores às equipes trocadas. A nova leitura ainda exige revisão e confirmação humana; não foi executado OCR nem demonstrada publicação automática de dados errados.

**Correção recomendada:** persistir a equipe do topo junto do print; para arquivos antigos, exigir seleção explícita se ela não for conhecida, evitando assumir A.

### 4. Falha em um TXT deixa participantes parcialmente atualizados

**Fonte:** `source-core/Settings.cs:49–56`; `source-avalonia/TextsView.cs:180`.

Os oito TXT são escritos em sequência. Na reprodução, todos começaram com OLD e `comentarista.txt` foi mantido aberto sem compartilhamento de escrita. Ao aplicar NEW, ocorre `IOException`, mas `narrador.txt` já está NEW enquanto `comentarista.txt` continua OLD.

**Correção recomendada:** preparar todo o conjunto antes de aplicar; realizar troca e rollback recuperáveis, coordenados com `casting-settings.json`. Considerar que os arquivos de saída podem estar fora da raiz de dados aceita pelo FileBatch atual.

Isso afeta a consistência dos overlays de participantes, não apaga a partida.

### 5. Presets legados podem impedir salvar o timer pelo OBS

**Fonte:** `source-core/ConfigurationStore.cs:25`, `obs-plugin/src/panel.cpp:107,114`, `source-avalonia/TextsView.cs:36`.

O normalizador aceita 0–8 presets, o painel nativo envia sempre três e `ConfigureTimer` exige a mesma quantidade do array salvo. Reproduzido com um preset salvo e envio de três: duração/formato válidos são recusados com `InvalidDataException` de tempo inválido. O padrão de três presets não reproduz o problema.

**Correção recomendada:** tornar o contrato consistente e migrar configurações legadas explicitamente, sem truncar dados. Cobrir contagens 0, 1, 3 e 8.

### 6. Reconfirmar estatísticas duplica o print integral

**Fonte:** `source/MatchFiles.cs:36`, `source-avalonia/StatsWindow.cs:185–187`; inclusão recursiva no backup em `source/DataBackup.cs:24,26,38`.

Mesmo ao corrigir apenas um número, a janela entrega novamente a imagem e o salvamento cria outro GUID. Dez salvamentos do mesmo print fictício 640×360 produziram **10 PNGs / 6.918.380 bytes**. Há uma referência corrente; a cópia anterior do JSON pode justificar preservar outro print, mas não explica todas as cópias.

**Correção recomendada:** reutilizar a referência se a imagem não mudou; eliminar apenas recursos comprovadamente órfãos segundo uma política que preserve backups e revisões. Não limpar arquivos indiscriminadamente.

## Desempenho: medido e potencial

### Reconstrução completa da tela — alocação mensurável

`ChampionshipView.Refresh()` recria a árvore da partida e é acionado por seleção de bombsite/equipe/finalização, mudanças do modelo e redimensionamento.

Ensaio isolado: 1920×1080, mapas fictícios com seis rounds, lados definidos e bans explícitos. Duas passagens na ordem MD1/MD3/MD5 e MD5/MD3/MD1. Cada caso executou 45 reconstruções: 15 para aquecimento e 30 medidas; total 180 amostras temporizadas. Inclui medida/layout e processamento de tarefas UI, **não rasterização, FPS nativo ou gravação de partida**.

| Caso | Mediana, passagens 1 / 2 | p95, passagens 1 / 2 | Alocação aproximada por reconstrução* | Descendentes visuais |
|---|---:|---:|---:|---:|
| MD1 | 65,6 / 34,3 ms | 88,2 / 46,6 ms | 7,3 MB | 559 |
| MD3 | 45,1 / 41,4 ms | 53,8 / 49,5 ms | 8,6 MB | 657 |
| MD5 | 51,8 / 48,7 ms | 65,3 / 58,6 ms | 9,9 MB | 755 |

*MB decimais, alocação agregada do trecho de 45 iterações dividida por 45, incluindo aquecimento. Os tempos reportados usam só as 30 amostras posteriores. Variação por aquecimento/JIT e ambiente impede afirmar que MD1 é mais lento que MD5. Memória retida após coleta ficou aproximadamente estável neste ensaio: **não foi confirmado vazamento**.

**Próxima experiência:** manter os blocos não alterados e atualizar apenas seleção, placar e histórico. Comparar com o mesmo harness e inputs antes de aceitar a mudança; não otimizar sem preservar os checks de comportamento e layout.

### Oportunidades ainda sem benchmark comparativo

- A prévia faz render completo e a exportação renderiza novamente (`StudioView.cs:192,201` → `MatchFiles.Export`). Pode ser possível reutilizar as páginas se o snapshot for exatamente o mesmo, mantendo invalidação, descarte e segurança da exportação.
- O painel nativo recria partes de remake a cada polling de 1 segundo (`obs-plugin/src/panel.cpp:202,288,296`), mesmo sem alteração. Avaliar atualização por revisão/estado, sem alegar ganho de CPU ainda não medido.

Nenhuma otimização foi promovida, e nenhum ganho percentual é prometido nesta etapa.

## Riscos não confirmados como bugs de uso comum

- Instalador principal não valida explicitamente o retorno do instalador filho do plugin; opt-in verifica existência de DLL, inclusive potencial versão anterior. Não foi executada instalação nem simulado OBS aberto.
- Helper de caminho TXT lança exceção com NUL; não foi reproduzida uma falha HTTP completa ou entrada comum do usuário.
- `MatchSession.Ban` não chama `Editable`, mas UI e servidor OBS bloqueiam os caminhos atuais para mapas confirmados. Recomendação de defesa adicional, não vulnerabilidade demonstrada.

## Evidências e limites

- Integridade: `work/audit-2026-10-18-integrity/REPORT.md`, `Program.cs` e `Audit.csproj`.
- OBS/TXT: `work/audit-2026-10-18-obs/RELATORIO.md`, `repro.txt`, `Program.cs` e `Audit.csproj`.
- Prints: `work/audit-2026-10-18-art/Program.cs`, `Audit.csproj` e dados fictícios criados pelo harness.
- UI: `work/audit-2026-10-18-ui/top-team.log`; execução `--top-only` salva/reabre de fato a janela headless; não executa OCR.
- Benchmark bruto: `work/audit-2026-10-18-ui/data-repeat/ui-benchmark.json`; implementação em `Program.cs`; contexto técnico no próprio JSON.

Proteções observadas: journal e rollback de arquivos de partida, validação de caminhos/hashes/limites de backup, mutex por pasta de dados, servidor OBS em loopback com chave/Host/Origin e revisão antes de comandos. Isso não equivale a teste de penetração nem prova de todas as falhas de disco/crash.

Não foram testados instalação/desinstalação nativas, renderização Win32 em diferentes GPUs/DPIs, consumo real do OBS, todos os caminhos de todos os botões, OCR ao vivo nem CI remoto. Não foi enviada a base do usuário para análise externa.

## Ordem sugerida de correção

1. Proteger estatísticas ao substituir mapas e impedir limpeza silenciosa das finalizações.
2. Persistir equipe do topo e tornar a escrita de TXT recuperável.
3. Harmonizar presets entre painel e programa.
4. Reutilizar prints e medir otimização incremental da interface e do renderer.

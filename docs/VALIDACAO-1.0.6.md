# CasterDesk 1.0.6 — revisão e correções

Implementa os seis achados reproduzidos na auditoria de 18/10/2026, as otimizações delimitadas abaixo e a abertura maximizada solicitada. As alterações locais 1.0.4 e 1.0.5 foram preservadas.

## Dados e funcionamento

- `HasPreservedData` inclui estatísticas sem alterar a validação original de lados. Substituição de mapa/veto e mudança de formato protegem esses dados. A substituição destrutiva do veto continua exigindo confirmação explícita e pode ser desfeita.
- Trocas de lado incompatíveis com finalizações registradas falham e restauram o estado anterior em memória e no JSON. Não apagam finalizações para contornar a validação.
- Equipe no topo do print é persistida. Arquivos antigos continuam com orientação desconhecida até seleção explícita; escolher essa orientação não inverte equipes já revisadas. Uma nova leitura exige orientação conhecida e confirmação posterior dos dados.
- Reconfirmar estatísticas sem carregar outra imagem mantém a referência, hash e data do print original. Prints antigos não foram excluídos.
- Os oito TXT e `casting-settings.json` são preparados antes da publicação. Falhas de gravação provocam rollback; se o rollback também falhar, cópias de recuperação são preservadas ao lado dos destinos. Isso não equivale a uma transação atômica perante leitores externos ou garantia contra corte de energia.
- Presets do timer no painel nativo refletem a quantidade salva, de zero a oito. Caminhos inválidos de saída não quebram a composição do estado do timer.
- Instalador principal verifica retorno e presença da DLL antes de registrar a instalação opcional do plugin. Falha gera aviso localizado, preservando a instalação do app.
- A janela principal inicia maximizada. Nenhuma configuração de tema ou composição das artes foi redesenhada.

## Otimização e limites das medidas

Seleções de bombsite/equipe/finalização no registro rápido atualizam os controles existentes. A confirmação continua usando `MatchSession.Round`, compartilhando as regras e persistência.

Ensaio isolado Caster MD5, 1920×1080, três cliques por iteração, 15 iterações de aquecimento e 30 medidas: mediana de **186,05 ms para 39,33 ms**, p95 de **226,22 ms para 83,92 ms**. Alocação agregada das 45 iterações: **1.707.077.112 para 82.461.600 bytes**. Inclui seleção, medida/layout e tarefas UI, sem desenho nativo nem gravação de rounds; não mede FPS no Windows. Evidências locais em `work/audit-2026-10-18-ui/selection-before` e `selection-after`, na raiz do workspace.

Exportação reutiliza páginas da prévia em resolução integral somente quando partida, preferências, categoria, formato e mapa coincidem. Mudanças invalidam a prévia antes de exportar. O PNG continua sendo codificado e salvo pelo mecanismo recuperável existente. Remake do plugin evita reconstruções quando seu conteúdo é idêntico; não há medição de redução de CPU para essas duas otimizações.

## Verificação

- Build do aplicativo sem avisos ou erros.
- Suíte headless aprovada: campeonato (289), timer/TXT (35), backup (27), idiomas (33), correções (16), HTTP OBS (83), SPEC (53), além dos grupos de artes/marca e **66 verificações novas da auditoria**.
- Testes de arte verificam reaproveitamento da página e invalidação após alteração da partida. Testes de marca verificam a abertura maximizada.
- Publicação `publish-avalonia-1.0.6-355e3b60` aprovada pelo próprio executável em modo headless: `test-results/avalonia-ui-1b88a100461140d28078717a54e64348`. Núcleo aprovado em `test-results/avalonia-core-e339c14a0b62480b84f209088e3d1e34`.
- Plugin nativo: **83 checks Qt sem abrir OBS**, incluindo presets 0/1/8/3, comandos autenticados, round a round, bans, remake, guarda de edição e reconexão.
- Compilação nativa confirmada no commit `b055204f344050dd81b434c3f5160bb23eeeaf77`, na branch exclusiva `codex/audit-fixes-1.0.6`: https://github.com/moska1m/CasterDesk/actions/runs/39191268436.

Não foram abertos CasterDesk nativo, OBS ou instaladores na área de trabalho. O GitHub foi usado somente para compilar o plugin em branch separada; não houve atualização de `main` nem publicação de release. Instalação/UAC, carregamento no OBS real e comportamento visual do backend Windows ainda dependem de teste manual. O instalador não possui assinatura digital.

## Entrega local

Instalador `dist/CasterDesk-1.0.6-win-x64-Setup.exe`, 73,7 MiB, versão de arquivo 1.0.6.0. Manifesto e SHA-256 gerados ao lado. Todos os **709 arquivos** da publicação foram comparados com os hashes do manifesto; DLL nativa conferida com o pacote recompilado.

SHA-256 do instalador: `311d6396067597306074a51028fca54ec4026485232cd18e5fd90d4d1d43b441`.

SHA-256 da DLL do OBS: `55afab7fcc197050ba31f8802e011ce495e0449c7d1f164e4b2bd69862689124`.

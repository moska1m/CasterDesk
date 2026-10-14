# Avalonia — primeira etapa, 13/10/2026

## Décima entrega — painel local OBS, 14/10/2026, 0.3.0-beta.0.9

Servidor HTTP restrito a 127.0.0.1, com chave persistida, ativação explícita, origem/host validados e comandos executados no dispatcher. Painel de navegador para timer e bans usa os mesmos serviços e regras. Não abre seletor nativo ao banir pelo painel. Confirmações, edição aberta e requisições desatualizadas bloqueiam alterações; não há repetição automática de comandos.

24 checks HTTP reais aprovados, além dos 311 checks anteriores de interface e marca. Edge sem janela validou controles, duração, contexto de ban, bloqueio de edição e larguras 320/400/700px com API de demonstração. Relatório HTTP inicial: test-results/avalonia-ui-4620b0762ef54fbe9d350504f7ebff09. Empacotamento final aprovado: 713 checks do núcleo e 335 de interface/HTTP, além de marca/prévias. Publicação publish-avalonia-0.3.0-beta.0.9-088fbb2c; UI test-results/avalonia-ui-503aeead29604c1b8416bdb19c6a3459; núcleo test-results/avalonia-core-62ef5cec439841d3b10d56b5838fd492. Instalador dist/CasterDesk-0.3.0-beta.0.9-win-x64-Setup.exe, SHA-256 65d8d23da90892f4d20066075d2ad135ba9844667d797e62ce755a3cb3bf58c3. Conferidos 709 arquivos do manifesto e sete resoluções do ícone nos dois EXEs. Sem assinatura digital. OBS/instalador nativos não foram abertos; compatibilidade do navegador do OBS permanece pendente.

GitHub privado atualizado com apresentação, fontes da beta.0.8 e instaladores históricos recuperados. Histórico anterior preservado sem inventar snapshots exatos para versões intermediárias. Esta seção substitui afirmações antigas de que nenhuma integração ou envio existe; seções anteriores documentam cada entrega à época.

## Nona entrega — barra fixa e correções de fluxo, 14/10/2026, 0.3.0-beta.0.8

Timer fixo no cabeçalho com ícone, tempo restante, iniciar/retomar, pausar e −1/+1 minuto. Compartilha o mesmo Countdown, saída TXT e pulso da aba Textos; não cria outro timer. Controles compactam em telas menores, com dicas indicando um minuto. Subtração limita em zero, preserva execução/pausa, e erro de tempo inicial é exibido sem exceção não tratada. Nome exibido, executável, instalador e atalhos passam a CasterDesk; identidade e diretório de atualização anteriores permanecem para não conflitar com a instalação legada.

Nova partida e seletor ficam à direita da linha de Partida/Configurar lobby; retirada a linha extra. Confirmar vencedor fica abaixo do histórico, desabilitado/opaco antes de haver vencedor e verde quando liberado. Histórico tem linhas de 28px, texto de uma linha e dicas; abreviações específicas só na exibição, mantendo IDs/dados e nomes completos na escolha dos bombsites.

No seletor de veto, mapas disponíveis mantêm as cores e mapas usados ficam em escala de cinza, opacos e indisponíveis. Etapas ainda sem mapa ficam neutras; picks concluídos continuam verdes. Selecionar mapas/lados não abre os bans iniciais: o usuário inicia por clique. Encadeamento seguinte preservado. A ordem completa é definida antes de filtrar pendentes, corrigindo DEF/DEF após o primeiro clique: DEF/ATQ, DEF/ATQ e depois DEF/ATQ nas duas metades, sem trocar equipes responsáveis nem índices salvos. Cards verticais, faixa de texto escura, cores fixas e cabeçalho com linha própria evitam sobreposição entre metades. Logos pré-reduzidas com filtro bicúbico, preservando proporção. Fundo do guia do lobby varia a cada abertura da aba e permanece durante a navegação/resize daquela abertura.

Central com coluna de opções 40px mais larga no layout amplo; Twitch é prefixo inicial e nomes sem URL recebem twitch.tv/, mantendo endereços personalizados existentes. MVP automático usa maior número conhecido de eliminações da equipe vencedora, por mapa/série, preservando seleção manual e opção sem destaque. Empates usam nome como desempate estável; sem vencedor não há sugestão. Valores ausentes continuam nulos. PNGs passam a eliminação/morte/assistência nas tabelas e MVP; a faixa do MVP foi centralizada sob seu nome. Restante da composição dos PSDs preservado.

Build e publicação sem erros/avisos; **713 verificações do núcleo e 311 verificações de interface/correções** (201 campeonato, 34 timer/textos, 27 backup, 33 idioma, 16 regras), além dos checks de marca, posição da barra e prévias. Conferidos bans preenchidos, seletor com mapa usado em cinza, cabeçalho em 980/1280/1920 e PNG de MVP em feed; geradas prévias de estatísticas nas três proporções. Sem janela nativa. Publicação `publish-avalonia-0.3.0-beta.0.8-3b103ed4`, UI `test-results/avalonia-ui-c1cc8b8029c941579721fdfbfe32f343/`, núcleo `test-results/avalonia-core-7f0ccad5cedc4a238b4bb7d8843c0a5c/`. Instalador `dist/CasterDesk-0.3.0-beta.0.8-win-x64-Setup.exe`; integridade conferida pelo manifesto e sete resoluções do ícone aprovado confirmadas nos recursos dos dois EXEs. Sem assinatura digital; instalação nativa, DPI/monitores e OCR real continuam pendentes. OBS não foi aberto e sua integração continua futura. Nenhum envio ao GitHub.

## Oitava entrega — marca aprovada, Preview 0.3.0-beta.0.7

Aplicado o ICO exato do kit Microfone + Rajdhani ao recurso de ícone do executável, ao ícone da janela Avalonia e ao SetupIconFile usado pelos instaladores. Cabeçalho e Sobre usam os SVGs aprovados com letras em contorno, sem alterar geometria, cores ou proporção. Segoe UI permanece nos controles; Rajdhani não foi instalada nem aplicada como fonte global. A renderização usa os paths dos SVGs específicos do kit, sem nova dependência de renderização SVG geral. Licença OFL preservada.

O workspace atual estava vazio; os fontes, scripts e recursos foram copiados da pasta anterior para `outputs/CasterDesk-beta`, sem alterar a cópia anterior. O kit foi preservado em `outputs/CasterDesk-Logo-Microfone-Rajdhani`. Build e testes Headless da interface passaram sem erros/avisos, incluindo ícone da janela, fonte preservada e prévias branca/preta da marca. Não houve abertura nativa, instalação ou envio ao GitHub.

Instalador 0.3.0-beta.0.7 gerado, ainda sem assinatura digital ou teste de instalação nativa. As sete resoluções do ICO aprovado foram encontradas **byte a byte** nos recursos PE do executável e do instalador, com `scripts/Test-BrandIcons.py` (leitura de arquivos, sem execução). Publicação `publish-avalonia-0.3.0-beta.0.7-531043aa`; testes UI `test-results/avalonia-ui-3d25275bc37d443e83d9c5f1bb7aeadd/`; núcleo `test-results/avalonia-core-953bdf56c6254816b37cda3e138f96e3/`. Passaram as 713 verificações do núcleo, as 279 de interface e a conferência adicional da marca. SVGs e ícone publicados foram conferidos, além da integridade dos arquivos pelo manifesto. A seleção de ícone nos atalhos e na barra de tarefas continua dependendo da validação nativa; o recurso incorporado já foi confirmado.

## Sétima entrega — layout de partida e lobby, Preview 0.3.0-beta.0.6

O bloco superior recebe mais altura (proporção 3:2, com limite de altura no bloco inferior). Bans permanecem embaixo à esquerda; registro rápido e histórico ficam lado a lado dentro do bloco direito, com placar acima do histórico. Os 12 cards têm tamanhos iguais entre operadores e símbolos de lados, fundo da equipe responsável e duas fileiras sem rolagem. Cards compactam conforme a altura disponível. Cabeçalhos permanecem fora da rolagem dos campos; em telas menores os campos de configuração ainda podem exigir rolagem interna.

Removido somente o sorteio de coin flip: seletores registram o vencedor externo e quem inicia os bans. Regras, dados e fluxo progressivo foram preservados. Lobby agora tem menu de dez seções, imagem competitiva de fundo e linhas de opção/valor, incluindo repick; seção escolhida permanece ativa. Rótulos usam o catálogo de tradução, com novas chaves EN/ES para vencedor do coin flip e histórico. Central de imagens e composições das artes não foram alteradas.

Compilação sem erros/avisos; **279 verificações Headless** (189 campeonato/menu, 30 textos, 27 backup, 33 idioma), incluindo layout em 980×640, 1280×800 e 1920×1080, os 12 bans preenchidos e proteção de mapas confirmados. Prévias inspecionadas em memória. Nenhuma janela nativa, instalador ou OBS foi aberto; testes de DPI/monitores e operação nativa continuam pendentes. O instalador desta entrega substitui o artefato 0.6 intermediário anterior ao último ajuste de altura.

Empacotamento concluído com **713 verificações do núcleo** e nova execução das 279 verificações no executável publicado. Publicação `publish-avalonia-0.3.0-beta.0.6-ee7ba64c`; evidências `test-results/avalonia-ui-ff3ef737529d4f1386e3d6876130a077/` e `test-results/avalonia-core-d66623f82ee34bb8ac3deb67737cf9c2/`. Instalador `dist/CasterDesk-Avalonia-0.3.0-beta.0.6-win-x64-Setup.exe`, autossuficiente x64, sem assinatura digital; SHA-256 `92f92982038ede860ca1c879306d9ba5be94242e440b53c47e98f0c1e2da74b0`. Hash do instalador e de cada arquivo publicado conferidos com o manifesto. Instalação nativa continua não testada. Snapshot local, sem envio ao GitHub.

## Sexta entrega — publicação e instalador Avalonia Preview

Gerado `dist/CasterDesk-Avalonia-0.3.0-beta.0.5-win-x64-Setup.exe`, **64 MiB**, versão de arquivo 0.3.0.5. SHA-256: `93e452a2c7e726e46fb20e9bf3ce8f4fb18337ba50112b6c91455276e4b2406f`. Há arquivos `.sha256` e `.manifest.json` ao lado do instalador. O manifesto lista 558 arquivos da publicação com tamanho/hash; não é uma assinatura digital. Authenticode: **NotSigned**.

Publicação autossuficiente Windows x64, com runtimes .NET/Windows Desktop 10.0.11, Avalonia, Skia/HarfBuzz, recursos competitivos, artes e OCR. Receita reproduzível em `scripts/Package-Avalonia.ps1`, usando Inno Setup 7 x64. O compilador antigo 32 bits disponível no workspace falhou no carregador do Windows; a ferramenta 64 bits já existente compilou com sucesso. Não foi instalado um compilador no PC.

O pacote usa identidade/diretório de instalação separados (`CasterDesk-Avalonia` / CasterDesk Avalonia Preview), preservando a instalação anterior. Os dados continuam em Documentos/CasterDesk e o mutex compartilhado impede abrir duas instâncias sobre a mesma pasta. Não há execução automática do programa ao finalizar o instalador. A desinstalação desta prévia mantém os dados; a escolha de excluir dados e o texto de consentimento específico continuam pendentes, sem texto legal novo aprovado nesta entrega.

A escolha de idioma do instalador configura o primeiro uso por `--language`; não substitui preferências existentes. Dois testes verificam essas condições. O executável **publicado** foi executado somente com `--avalonia-check`, dados isolados e backend Headless: **254 verificações de interface/fluxos + 713 do núcleo**, sem erros/avisos de compilação. O script aguarda o término do apphost Windows com `Start-Process -Wait -WindowStyle Hidden`, em vez de verificar os relatórios antes de ele terminar.

Evidência: publicação `publish-avalonia-0.3.0-beta.0.5-fbe16f1a`, UI `test-results/avalonia-ui-42aa7a556a004a91b8a811b4b1292499/`, núcleo `test-results/avalonia-core-4e33a19f6d3b48158050ebe9e976c9cf/`. Conferidos SHA do instalador, presença dos arquivos do manifesto, runtimeconfig e versão PE. O instalador **não foi executado**: instalação/desinstalação, arranque normal, DPI/monitores, seletores, clipboard/OCR e OBS ainda exigem validação interativa. Não foi aberto programa nativo, instalador ou OBS na área de trabalho. Nenhum envio ao GitHub.

Pendências reais: testes nativos e revisão regional de termos; fluxo de consentimento e escolha de remoção dos dados; assinatura/distribuição. A geração do instalador mencionada como pendente nos registros anteriores foi atendida, mas isso não certifica uma versão final pronta para distribuição ampla.

## Quinta entrega — revisão de idioma e nomes personalizados

Revisados os chamadores de tradução das telas Avalonia, lobby, seletores e diálogos, usando o catálogo JSON existente. Acrescentadas **sete chaves EN/ES**: cancelamento, bombsite, duplicata de partida, dica de correção de round, filtro de imagens, descarte de edição e aplicação editorial do veto. As traduções existentes não foram substituídas em bloco. Placeholders de todas as entradas são comparados com a chave fonte nos testes; isso verifica estrutura, não certifica qualidade linguística regional.

`Ui.RawCombo`/`RawText` separam dados do usuário de rótulos traduzíveis. Coin flip, primeiro ban, lados, revisão de rounds, correção editorial e estatísticas preservam nomes como **Banco** e **Clube**, sem convertê-los para os nomes dos mapas em EN/ES. Nomes padrão continuam vindo do modelo no idioma atual. IDs de mapas, JSON e convenções de importação não mudaram. Cabeçalhos de round/bans usam a tradução de Round; a dica do histórico agora descreve corretamente um clique.

Os testes também reproduziram um problema em estatísticas: eventos de inicialização adiados marcavam a tela como editada e podiam trocar a associação de equipe. Os handlers agora comparam o valor efetivo anterior; abrir/fechar sem edição não pede descarte, enquanto uma edição manual continua exigindo confirmação.

**Zero erros e zero avisos.** A suíte Headless passou em **252 verificações de interface/fluxos** (164 campeonato/menu, 30 textos, 27 backup, 31 idioma/estatísticas), além das prévias da Central. As 713 verificações do núcleo passaram novamente. Evidência: `test-results/avalonia-ui-d9626ef94df94ae0885bd030e7da7417/` e `test-results/avalonia-core-6fa10a20842347659dcf8b6577eda143/`. Lobby renderizado em PT/EN/ES; prévia ES em 1280×800 inspecionada. Testados importação entre idiomas, defaults, nomes personalizados nos controles e avisos de edição real.

Pendências: revisão linguística por falante nativo e confirmação de termos regionais/oficiais de mapas/bombsites; inspeção visual de todos os estados e mensagens dinâmicas; testes reais de OCR, clipboard, arquivos, DPI/monitores e inicialização/restauração; preparar e validar instalador Avalonia. Esta entrega não declara uma auditoria integral de tradução nem validação nativa concluída. Sem abertura do programa/OBS/instalador e sem envio ao GitHub.

## Quarta entrega — backup, restauração e idioma

O menu Avalonia agora inclui **Backup e restauração** e seleção de Português/English/Español, persistida e aplicada na próxima abertura. Os novos rótulos de backup/inicialização têm traduções EN/ES; isso não equivale à revisão completa das traduções existentes.

Backup usa `DataBackup.Create` em segundo plano, após salvar edições e suspender somente a escrita de TXT. O relógio continua contando; ao terminar, a saída recebe o valor atual. O diálogo modal protege contra alterações simultâneas. Inclui a pasta de dados do CasterDesk; caminhos externos e arquivos de backups anteriores ficam excluídos, conforme o serviço existente.

Restauração copia o ZIP selecionado para uma área privada, valida integridade/estrutura e mostra quantos arquivos têm conteúdo diferente. Oferece manter os atuais/adicionar ausentes ou usar versões do backup; cancelamento e confirmação final são explícitos. A política escolhida é agendada sem alterar partidas carregadas. Após confirmação, o programa fecha e a restauração ocorre na próxima abertura; não há relançamento automático. Antes de aplicar, o serviço cria o ZIP `Antes-da-restauracao_*`. A cópia privada impede que uma mudança posterior no ZIP original altere o conteúdo já verificado. Limpeza de prévias e processamento pendente rodam em segundo plano.

Inicialização normal agora aguarda recuperação de transações e `ProcessPending` antes de carregar os dados da interface, com tela de preparação. Recarrega o idioma após a restauração. Falhas de inicialização são registradas e exibidas; o diálogo de restauração rejeita arquivos inválidos sem agendar aplicação.

**Zero erros e zero avisos.** Testes Headless: **164 campeonato/menu + 30 textos/configuração + 27 backup/restauração/idioma = 221 verificações**, além das prévias da Central. Exercitados: criação, conflitos, cancelamento, confirmação, políticas manter/substituir, aplicação pendente em dados isolados, ZIP preventivo, cópia verificada mesmo após alteração do original e rejeição de ZIP inválido. O núcleo passou novamente em **713 verificações**. Evidência: `test-results/avalonia-ui-8a85e5e24a4b4678ba0d6fedcd320415/` e `test-results/avalonia-core-586c1551213e4f03ba72783ec1366062/`. Prévia do diálogo inspecionada em 760×610.

Limitações: abertura/reabertura nativa e tela de preparação ainda não foram testadas na área de trabalho; `ProcessPending` foi exercitado diretamente no backend de testes. Seletores nativos, OCR/clipboard, DPI/monitores e OBS seguem para validação real após aviso ao usuário. Ainda falta revisar a tradução integral da interface e preparar/validar o instalador Avalonia. Nenhuma janela nativa, instalador ou OBS foi aberto e nenhum código foi enviado ao GitHub.

As pendências de telas de backup/restauração e escolha de idioma citadas abaixo são históricas e foram atendidas nesta quarta entrega.

## Terceira entrega — textos OBS e cronômetro

A navegação superior inclui **Textos para OBS**. A tela Avalonia reaproveita `Countdown`, `CastingFiles` e os modelos de configuração: quatro participantes, nome e @, oito arquivos TXT, pasta de saída, contagem regressiva, pausa/retomada/reset, acréscimos editáveis, formatos mm:ss/hh:mm:ss, mensagem final e caminho copiável. Trocar de aba mantém a contagem. Escolha de pasta/arquivo usa o provedor nativo Avalonia; não foi exercitada em janela real.

Gravação dos TXT roda em segundo plano, com uma fila que mantém a atualização mais recente, serialização e aviso de falha. A escrita trunca conteúdo antigo e usa UTF-8 sem BOM, mantendo o caminho configurado no OBS. Construir a tela não sobrescreve arquivos; a saída ao vivo começa ao abrir a janela. O fechamento aguarda exportações pendentes. Configurações e participantes são salvos ao sair da aba/fechar; duração inválida mantém a última duração válida. O serviço de casting anterior continua responsável pelos oito TXT: não foi transformado numa transação única de oito arquivos nesta etapa.

**Zero erros e zero avisos.** Passaram novamente os testes da Central e os **164 testes de campeonato/menu**, mais **28 verificações de textos/cronômetro/configuração**: relógio controlado, pausa, retomada, acréscimo sem pausa, término, limite de 24h, formatos, troca/falha de destino, conteúdo truncado, UTF-8, participantes persistidos, fechamento durante exportação e recuperação de JSON inválido preservando o original. Dados isolados: `test-results/avalonia-ui-aa56705e94d640fb98fe42470a28d04f/`. Prévias da tela inspecionadas em 980×640 e 1280×800; áreas usam rolagem quando necessário. Não foram repetidas as 713 verificações do núcleo, pois os serviços do núcleo não mudaram nesta entrega.

Pendências atuais: telas de backup/restauração, configurações de idioma e revisão de tradução; validação real de clipboard/OCR, seletores de arquivo, DPI/monitores e integração OBS; empacotamento/instalador Avalonia. Testes Headless não comprovam o comportamento dos seletores nativos ou do OBS. Nenhuma janela nativa foi aberta e nenhum envio remoto foi realizado.

As pendências de cronômetro/TXTs e testes específicos do carregador de configurações mencionadas nas entregas abaixo são históricas e foram atendidas nesta terceira entrega.

## Segunda entrega — campeonato e navegação

Campeonato e instruções de lobby agora têm controles Avalonia ligados ao mesmo `MatchSession` da base estabilizada. Inclui nomes/logos, configuração progressiva, nove mapas competitivos, lados por mapa, cards responsivos, seleção por ícones em grade de 11 colunas na ordem do jogo e abertura automática do próximo ban. A responsabilidade das equipes e a alternância DEF/ATQ vêm do núcleo existente.

Registro rápido é o padrão: bombsites, ataque/defesa com cores fixas, finalização compatível, confirmação e histórico centralizado. Resultados confirmados exigem revisão auditada antes de edição. Importação/exportação de JSON e pacotes usa os serviços existentes. O menu fica sobre a área da janela, sem reservar largura quando fechado, com opção ativa destacada, temas claro/escuro e engrenagens reativas. Segoe UI permanece na interface; fontes e composição das artes foram preservadas.

Validação final: **zero erros e zero avisos**, **164 verificações de controles Avalonia** e **713 verificações do núcleo**. Os controles testados percorrem configuração, oito escolhas de veto, bans automáticos, sete rounds, confirmação, bloqueio de edição e revisão auditada; também verificam seletores MD1/MD3/MD5, lobby, navegação e menu. Séries completas MD3/MD5 e prorrogação são verificadas pelo núcleo, não por uma série completa de cliques Avalonia.

Evidência: `test-results/avalonia-ui-31fd854315dd4562839f8bff79235cd9/` e `test-results/avalonia-core-6c1d42aba5874fdf96e561345d2e1029/`. Prévias em 980×640, 1280×800 e 1920×1080; inspeção final de campeonato em 1280 e da janela completa em 980. Em altura reduzida, configuração concluída é recolhida e formulários usam rolagem. Testes executados exclusivamente com dados isolados e backend Headless, sem abrir programa/OBS na área de trabalho.

Pendências atuais:

1. Adaptar telas de cronômetro/TXTs, backup/restauração e demais configurações/idioma. Serviços preservados; tradução integral e revisão regional continuam pendentes.
2. Testar diálogos nativos de arquivos, clipboard/Ctrl+V, OCR real e revisão de estatísticas com o usuário. A nova cópia do carregador de configurações ainda não tem testes específicos de recuperação de JSON inválido.
3. Conferir operação real em DPI/monitores, teclado e modo clássico. O modo clássico foi selecionado nos testes, mas não percorreu uma partida completa pela interface.
4. Preparar e validar distribuição/instalador Avalonia depois dessas entregas. Não foi gerado instalador nem enviado código ao GitHub nesta fase.

Os registros abaixo são históricos; as pendências de campeonato/menu ali foram parcialmente resolvidas nesta segunda entrega. Não significam que toda a migração foi concluída.

## Atualização após restauração das dependências

O usuário restaurou os pacotes pelo PowerShell. A interface Avalonia agora compila com **zero erros e zero avisos**. Foi corrigida uma colisão entre o método Ui.Button e o tipo Button. A verificação de prévias passou a aguardar também pedidos adiados pelo debounce, preservando a rejeição de resultados antigos.

Nesta entrega Windows, o projeto referencia Avalonia.Win32, Skia e HarfBuzz em vez do pacote agregado Desktop. Isso removeu do grafo o suporte X11/D-Bus e a dependência Tmds.DBus.Protocol 0.90.3 sinalizada pelo NU1903. Não houve supressão do aviso. Referência do aviso: https://github.com/advisories/GHSA-xrw6-gwf8-vvr9.

**Test-Avalonia aprovado**, dados isolados em `test-results/avalonia-ui-aaff4aa51c7444b2b750c79affeebf18/`: cinco categorias com prévia, anúncio limitado a feed/story e layouts em 980×640, 1280×800 e 1920×1080, com ação Gerar dentro dos limites. Prévia 1280 inspecionada visualmente. As janelas de teste pertencem exclusivamente ao backend Headless em memória; nenhuma janela nativa foi aberta na área de trabalho.

Ainda faltam testes interativos de arquivos/clipboard/OCR, diálogos de estatísticas e atualização de veto, outras categorias visualmente inspecionadas e as demais áreas do programa. Nenhum instalador Avalonia foi gerado. O bloqueio de dependências descrito abaixo é histórico e foi resolvido.

## Registro da preparação inicial (histórico)

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

# 1.1.0

- Controle pelo celular na rede local por QR code, com veto, bans e rounds.
- Aba Remake com lados, placar, bans por metade e histórico colorido.
- Menu e opções reorganizados, temas e troca imediata de idioma.
- Retratos ajustados e pacotes visuais para o gerador de imagens.
- Importação experimental e arquivamento de replays com prévia de mapa e equipes.
- Aviso de atualização com novidades e correções no encerramento e na moldura.

# 1.0.9-beta.11

- Removido o botão Fechar duplicado das opções.
- Encerramento aguarda salvamentos e conexões locais antes de fechar a janela, evitando bloquear o instalador.

# 1.0.9-beta.10

- Botão Fechar no cabeçalho dos painéis internos.
- Remake com apenas ataque inicial e ataque no OT.
- Bans do remake em quadradinhos separados por metade.
- Histórico na ordem round, bombsite e vencedor, sem finalização.

# 1.0.9-beta.9

- Veto de mapas em janela sobre o painel do celular.
- Rolagem automática até os bans liberados e retorno ao registro do round.
- Aba Remake com mapa, placar, lados, bans por ícones e histórico.
- Cores das equipes reforçadas no histórico de rounds.
- Instalador deixa de sugerir os diretórios antigos com nomes de frameworks.

# 1.0.9-beta.8

- Controle local pelo navegador com QR, link e encerramento dos acessos.
- Equipes, logos, veto, lados e OT sincronizados com o PC.
- Round a round, histórico por cores e bans com ícones.
- Reconexão automática e proteção contra registros duplicados.
- Preparação opcional da rede privada no instalador.

# 1.0.9-beta.7

- Menu compacto com Opções, Atualizações, Créditos e status do OBS.
- Preferências em abas, com Tabela, Backup e OBS integrados à janela principal.
- Temas Padrão, Escuro e Claro, com troca de idioma aplicada imediatamente.
- Correções nos cliques ao fundo, no contraste das listas e nas cores dos operadores.
- Moldura própria com correções de arraste, maximização, restauração e botões duplicados.
- Atualizações do aplicativo preservam o painel do OBS instalado.

# 1.0.8

- Verificador gratuito pelo GitHub Releases, com consulta automática e manual, avisos de prioridade e notas de publicação.
- Lista com versão, data e tipo RELEASE/BETA; Mostrar betas desligado por padrão, sem seleção de canais ou alfas.
- Download com progresso e verificação de assinatura, tamanho e SHA-256. Instalação por ação do usuário após salvar a sessão.
- Publicador com catálogo assinado, preparado antes da publicação e atualizado somente após o instalador estar disponível.
- Tokens de login, chaves de publicação e cache de downloads excluídos da migração dos dados portáveis.

## Tabela da partida (experimental)

- Excel local opcional com arquivo fixo atualizado e cópias separadas por partida e mapa.
- Google Sheets com o mesmo mapeamento, atualização automática e recuperação após falhas de conexão.
- Login Google integrado ao instalador; cada usuário autoriza a própria conta, sem importar JSON.
- Tabela compatível com MD1 e MD3, preservando as células de preenchimento manual.
- Interface, mensagens, modelos, artes, projetos e scripts padronizados com a identidade CasterDesk.
- Namespace do núcleo padronizado; formatos de dados e configurações existentes preservados.
- Histórico interno de desenvolvimento retirado do pacote instalado; changelog passa a acompanhar a distribuição.
- Dependências, créditos legais e licenças dos recursos de terceiros preservados.

# 1.0.7

- Painel OBS com Sem banimento no final da seleção, mantendo a ordem dos operadores do jogo.
- Operadores e Sem banimento com os mesmos cards de 72 px, ícones de 36 px e texto abaixo do ícone.
- Bans futuros no OBS com 50% de opacidade; liberados e registrados permanecem a 100%.
- Sem banimento registrado usa o símbolo de sua função, mantendo o significado explícito no tooltip, acessibilidade e remake.

# 1.0.6

- Abertura maximizada por padrão.
- Trocas de mapa, formato e veto da arte protegem também estatísticas já salvas, mesmo sem rounds ou bans.
- Alterações de lado incompatíveis são recusadas sem apagar finalizações; dados e placares anteriores são preservados.
- Equipe no topo do print persistida. Prints antigos sem essa informação exigem seleção explícita para nova leitura.
- Correções de estatísticas reutilizam o print arquivado quando a imagem não mudou.
- Aplicação dos oito TXT e sua configuração prepara todos os arquivos antes de publicar e desfaz alterações se uma gravação falhar.
- Plugin OBS respeita a quantidade de atalhos de tempo salva (0–8), sem limitar a três; remake evita reconstruções quando o conteúdo não muda.
- Seleções rápidas de round atualizam os controles existentes. Exportação reutiliza a prévia em resolução integral quando os dados não mudaram.
- Caminho inválido de saída do timer não interrompe a consulta de estado. Instalador verifica o resultado da instalação opcional do painel OBS.

# 1.0.5

- Sem banimento com o mesmo tamanho, ícone e fonte das opções de operadores.
- Seleção de operadores em sete colunas, na ordem existente do jogo, com Sem banimento no final da grade.
- Cards vazios usam os símbolos DEF/ATQ; o estado Sem banimento permanece explícito no tooltip, acessibilidade, dados e remake.
- Bans de etapas futuras ficam a 50% de opacidade; liberados e já registrados ficam a 100%, preservando o bloqueio de ações.
- Placar incorporado à barra de título do Histórico de rounds no modo Caster, eliminando a linha separada acima do painel.

# 1.0.4

- Rodapé de marca e linha permanente de salvamento removidos; avisos de erro e recuperação continuam visíveis quando necessários.
- Confronto com altura limitada e espaço restante destinado aos lados dos mapas, favorecendo MD3 e MD5.
- Bans divididos em duas metades lado a lado: DEF acima, ATQ abaixo e separador antes do terceiro ban. Cores e responsáveis preservados.
- Miniaturas proporcionais com redução e interpolação de qualidade, sem recorte forçado de rostos ou símbolos em janelas menores.
- Removido o achatamento da configuração superior em janelas pequenas; cabeçalho dos bans permanece em uma linha.

# 1.0.3

- Canal Twitch salvo para reutilizar em novas partidas, com prefixo fixo e campo apenas para o nick.
- Nomes das equipes confirmados ao sair dos campos ou pressionar Enter, preservando digitação lenta.
- Sem banimento selecionável e repetível por função; preservado em JSON, SPEC, remake e painel OBS. Não é o mesmo que um ban pendente.
- Gerador de imagens com navegação padronizada, seletor à direita, opções mais largas e faixa reservada para rolagem.
- Aplicar tempo substitui o restante mantendo execução/pausa, no app e pelo comando existente do OBS; aceita mm:ss ou hh:mm:ss.
- Blocos reposicionáveis continuam futuros. Arquivos com novos registros Sem banimento exigem esta versão ou posterior.

# 1.0.2

- Central de imagens reúne resultado e estatísticas da série em uma única aba.
- Geração da série bloqueada quando variações de nomes produzem mais de cinco identidades por equipe; conciliação explícita por mapa com backup antes de salvar. Não há mesclagem aproximada nem descarte silencioso.
- Nomes dos jogadores com fonte fixa por formato, truncando nomes longos sem reduzir sua fonte. MVP alinhado ao grupo E/M/A centralizado.
- Fundo e cor de destaque independentes por categoria; cores prontas e hexadecimal personalizado. Fundo compartilhado antigo preservado nas estatísticas da série.
- OBS e partida original preservados.

# 1.0.1

- SPEC: equipes editáveis e placar no centro da barra das abas; placar duplicado removido do histórico.
- Histórico e registro de rounds com cabeçalhos alinhados.
- Nova partida e seletor com altura e alinhamento iguais; ações secundárias ocultas em janelas estreitas e restauradas ao ampliar.
- Plugin e tema do OBS preservados.

# 0.3.0-beta.0.13

- OBS: abas PARTIDA → REMAKE → CRONÔMETRO, com histórico removido de PARTIDA.
- REMAKE exige escolher o mapa e mostra lados, bans já realizados por metade e rounds/bombsite/vencedor, sem alterar a partida ativa.
- Seis cards de ícones por equipe em duas linhas; ordem dos cliques e responsáveis preservados.
- Indicador verde/vermelho no cabeçalho, confronto centralizado em negrito e caixa alta e mapa abaixo.
- Campos do cronômetro centralizados; serviços e controles preservados.
- Seletor do programa mostra cada nome de mapa uma única vez, maior, completo e sem abreviação.
- Faixas de cabeçalho em caixa alta/negrito, botão Plugin OBS em uma linha sem On/Off e indicador de cliente conectado sem alterar a preferência de popups.
- Nova partida/seletor alinhados; cards mostram mais corpo em espaço amplo, símbolos menores e legenda sem margem lateral interna. Prévias redimensionadas com suavização; recursos originais preservados.
- Apresentação centrada em CasterDesk e relatório PROGRESSO.md; entregas locais prioritárias, sem publicação GitHub a cada ajuste.
- Central de imagens preservada para uma etapa posterior.

# 0.3.0-beta.0.12

- OBS: Partida reúne registro por botões, histórico clicável e bans; Cronômetro em aba separada.
- Cronômetro com mensagem final, formato, duração inicial, três acréscimos configuráveis e reset sincronizados com o aplicativo.
- Copiar caminho do timer copia a pasta do TXT, mantendo o arquivo de saída.
- No programa, histórico permanece à direita; round/lados no cabeçalho e bombsites de altura uniforme.
- Novos rounds acompanham o final do histórico; demais atualizações preservam a posição de leitura.
- Confirmação do vencedor com somente confirmar/cancelar, texto maior e alerta vermelho escuro centralizado.
- Selo ATIVOS removido e configurações fecham ao clicar fora.
- Composições de imagem, cronômetro/TXT e regras de partida preservados; sem execução de OBS/instaladores na área de trabalho.

# 0.3.0-beta.0.11

- Plugin com abas Round a round → Bans de operadores → Cronômetro e marca centralizada.
- Registrar e corrigir rounds com bombsites completos, equipe vencedora e finalização compatível.
- Bans intermediários e após troca de lados disponíveis no OBS; o painel acompanha as etapas e retorna aos rounds.
- Botão Plugin OBS no bloco de bans salva a preferência de não abrir seletores automaticamente no programa. Ban manual continua disponível.
- Seleção de mapas MD3/MD5 sincronizada com o programa; MD1 fica fixo.
- Proteção contra comandos duplicados, seleção desatualizada e correções que removeriam dados sem confirmação.
- Ajustes visuais gerais e rolagem do histórico ficaram registrados para a próxima atualização.

# 0.3.0-beta.0.10

- Plugin nativo opcional OBS para timer e bans, com descoberta local e reconexão automática.
- Instalação opcional integrada; sem colar URL. O componente OBS pede administrador e exige OBS fechado.
- Ativação inicial automática ao optar pelo plugin, preservando desativação manual posterior.
- Sem alterações de cenas, perfis ou outros plugins. Painel de navegador anterior permanece compatível.
- Verificações nativas Qt sem janela e API isolada; carregamento/instalação no OBS real permanecem pendentes.

# 0.3.0-beta.0.9

- Painel local beta para controlar timer e bans em um dock de navegador do OBS.
- Ativação explícita, chave local, proteção de origem e de alterações concorrentes.
- Mesmo cronômetro/TXT e comandos persistidos; ordem de operadores e DEF/ATQ preservadas.
- Apresentação do GitHub com marca, funcionalidades, prévias e histórico recuperado.
- 713 checks do núcleo, 311 de interface e 24 HTTP; teste adicional do painel em navegador sem janela. OBS nativo ainda não testado.

# 0.3.0-beta.0.8

- Marca aprovada aplicada, mantendo Segoe UI na interface.
- Timer fixo e sincronizado, nova partida/seletor na linha superior e confirmação verde abaixo do histórico.
- Sequência DEF/ATQ corrigida, início manual dos bans, mapas indisponíveis em cinza e cards sem sobreposição.
- Histórico compacto; Central mais confortável, Twitch como prefixo inicial e MVP da equipe vencedora.
- PNGs na ordem eliminação/morte/assistência, com valores do MVP centralizados sob o nome.
- Nome do programa, instalador e atalhos: CasterDesk. 713 checks do núcleo e 311 de interface/correções.

# 0.3.0-beta.0.7

- Microfone monocromático + CasterDesk unido em Rajdhani; ícone e SVGs aprovados aplicados.
- Instalador preservado; não há snapshot exato separado dos fontes desta entrega.

# 0.3.0-beta.0.6

- Layout de partida compacto, coin flip somente registrado, cabeçalhos fixos, cores de equipe nos bans e lobby em seções.

# 0.3.0-beta.0.5

- Aplicativo CasterDesk: Central, campeonato/lobby, timer/TXTs, backup/restauração e idioma.
- Publicação autossuficiente Windows x64 e correções de nomes personalizados e associação de equipe nas estatísticas.

# 0.3.0-beta.0.4

- Base estabilizada anterior com salvamento recuperável, JSON estruturado, revisão, pacotes e backup.
- Referência técnica preservada para os serviços compartilhados da nova interface.

# 0.3.0-beta.0.3

- JSON de partida versão 2 com base em inglês: equipes padrão explícitas, BO1/BO3/BO5, IDs de mapas existentes e 36 IDs fixos de bombsites com nomes-base legíveis.
- Equipes padrão acompanham o idioma de quem importa; nomes personalizados são preservados, inclusive quando iguais ao texto de um padrão.
- Importação compatível com JSON versão 1 e nomes conhecidos de mapas/bombsites em PT/EN/ES. Novos arquivos exigem um leitor com suporte ao formato 2.
- 599 verificações de portabilidade entre idiomas integradas aos testes da versão. Detalhes em MATCH-JSON-FORMAT.md.

# 0.3.0-beta.0.2

- Central de imagens organizada em cinco abas, com partida compartilhada e formato lembrado por categoria; anúncio em feed e story.
- Canal da transmissão editável no anúncio, nas posições e fontes originais do modelo de arte. Data e horário permanecem ausentes.
- Seleção de logos junto dos nomes das equipes; artes reutilizam os recursos da partida.
- Veto da arte separado do veto registrado. Ao aplicar ou gerar uma correção, opção de atualizar também a partida; atualização real bloqueada quando já existem bans/rounds.
- Falhas de salvamento revertem mudanças em veto, lados, bans e histórico. Nomes pendentes impedem sair/trocar de partida se a gravação falhar; nova partida só entra na lista após ser salva.
- Cronômetro aplica o tempo editado ao iniciar, invalida o cache ao trocar caminho e preserva a exibição de 24 horas. Casting pode ser aplicado independentemente de um tempo inválido.
- Normalização de configurações antigas, preservação de JSON inválido, logs do aplicativo e bloqueio de segunda instância por pasta de dados.
- Validação da interface e regressão de PNG exigidas pelo script de empacotamento. Migração completa do campeonato e demais pontos da revisão continuam planejados.

# 0.3.0-beta.0.1

- Renovação da interface: janela, navegação, menu, Central de imagens, conferência de estatísticas e textos/cronômetro nativos.
- Menu contido na janela; ações de geração fixas; prévias em segundo plano e controles com tamanho consistente.
- Dados, regras e renderizadores das artes preservados durante a renovação da interface.

# 0.2.0-beta.0.3

- Alinhamento dos campos de lados e do placar sobre o histórico; conteúdo da tabela centralizado e seletores com chevrons.
- Cabeçalhos dos bans centralizados, Round por extenso, selo ATIVOS verde; Nova partida em azul.
- Imagens dos mapas aguardam referências do usuário.

# 0.2.0-beta.0.2

- Reverte Scout para Segoe UI e Bahnschrift SemiCondensed após feedback de legibilidade; fontes privadas retiradas da distribuição.
- Mantém cards compactos e retratos. Registro rápido separado em Bomb site, Vencedor e Finalização; equipes coloridas antes da seleção e botão Confirmar round em azul.
- Seletor de mapa compacto junto do placar; configuração de lados ganha altura e campos alinhados.
- Consolida no GitHub o histórico acumulado e arquiva os instaladores preservados das betas anteriores, identificando a ausência de snapshots exatos intermediários.

# 0.2.0-beta.0.1

- Reformulação visual do tema Siege: títulos Scout, campos integrados, destaque vermelho em botões, estados bloqueados hachurados e sublinhado curto centralizado nas abas. Controles compartilhados nas demais páginas.
- Página de partida ajustada à janela, sem rolagem global; ações de corrigir bans, remake, salvar, importar/exportar e confirmar resultado na mesma faixa inferior.
- Configuração superior compacta em janelas baixas, com alternância entre confronto e lados. Metades dos bans alternáveis pelos cabeçalhos quando necessário. Histórico mantém rolagem local.
- Cards verticais de bans com retrato aproximado, ícone, nome e equipe; altura limitada e fonte ajustada para nomes longos. Seletores continuam por ícones. Todos os 78 retratos incluídos localmente.
- Cards do veto com imagem preenchendo o fundo; bans usam os arquivos cinza existentes, picks continuam coloridos.
- Registro rápido junto do histórico, com finalização em texto. Mapas encerrados expandem o histórico e ocultam o formulário de novo round.
- Fontes Scout fornecidas pelo usuário carregadas de forma privada pelo processo. Novos ícones de finalização adiados a pedido do usuário.
# 0.1.0-beta.8

- Bombsite vencido pela defesa bloqueia os dois rounds seguintes no tempo regular. Vitória do ataque não bloqueia; troca de lados após round 06 zera; overtime livre.
- Regra compartilhada pelos modos Lista e Rápido. Correções recalculam disponibilidade e sinalizam conflitos posteriores sem apagar registros. Partidas antigas/importadas preservam dados e exibem conflitos para revisão.
- Registro rápido à esquerda e histórico compacto à direita na mesma área; menor altura que a beta.7. Ataque/defesa ficam na dica do round no modo compacto e continuam como colunas na Lista.
- Banco: rótulos curtos Escritório / Arquivos e Armários / Vigilância, com nome completo nas dicas. Valores originais permanecem no JSON e nos PNGs. Demais apelidos dependem da revisão do caster.
- Símbolos específicos da timeline seguem pendentes; importação/exportação não são prioridade de evolução nesta etapa.

# 0.1.0-beta.7

- Aviso central nas novas etapas de bans após os rounds 03, 06 e 09, apenas quando o mapa continua e há operadores pendentes. Registrar abre os seletores; Lembrar depois mantém a pendência sem impedir rounds. Correções não repetem o aviso na mesma sessão.
- Engrenagem → Registro de rounds → Modo rápido ou Lista, com preferência salva e aplicação imediata.
- Modo rápido por botões: bombsite, vencedor, finalização e confirmação. Histórico compartilhado com a lista e disponível para correções.
- Finalização na lista e no JSON: eliminação, defuser concluído, defuser desativado ou tempo esgotado. Dados antigos mostram Não informado. Combinações incompatíveis com o lado vencedor são recusadas; correções limpam finalizações incompatíveis.
- Símbolos específicos, finalização no PNG e trava de bombsite continuam no backlog.

# 0.1.0-beta.6

- Cards de mapas indisponíveis em preto e branco, carregados de arquivos locais pré-gerados. Histórico mantém picks coloridos e bans em preto e branco.
- Correção explícita de bans após confirmar mapa/série; resultados e lados continuam protegidos e etapas futuras não são liberadas.
- Lembretes junto aos campos de bans, nas etapas antes dos rounds 1, 4, 7 e 10; avisos desaparecem ao completar o registro.
- Modo rápido, trava de bombsite e estatísticas permanecem no backlog.

# 0.1.0-beta.5

- MD5: ban, ban, pick, pick, pick, pick, ban, ban e decider; vitória com três mapas confirmados.
- Cinco mapas na configuração de lados, salvamento e importação/exportação JSON.
- PNG: séries com quatro/cinco mapas geram capa e páginas por mapa em Stories/feed; horizontal usa até três mapas por página.
- Capa compacta evita sobreposição entre nome do mapa e placar.

# 0.1.0-beta.4

- Cabeçalhos dos PNGs centralizados na imagem em Stories, feed e horizontal.
- Logo e identificação do caster em colunas laterais simétricas.

# Histórico

## 1.0.0 — 14/10/2026

- Layout SPEC aprovado: nomes compactos com VS, formato/seleção da série/mapa ativo/lados em uma linha, sem timer no cabeçalho; finalização opcional, confirmação verde e bans maiores junto ao registro.
- Equipe Azul/Equipe Laranja como defaults em PT/EN/ES nos dois modos, preservando IDs e nomes personalizados/arquivos antigos.
- OBS: seleção em vermelho, confirmação habilitada em verde, títulos em caixa alta nas faixas e microfone de conexão menor com cores vivas.
- Instalador com logo, condições de uso e ciência de direitos em três idiomas, aceite obrigatório na interface, progresso identificado e abertura opcional ao finalizar. MIT e GPL preservadas; não concede licença de terceiros.

## 0.3.0-beta.0.14 — 14/10/2026

- Tela SPEC selecionável pelo menu, compartilhando a partida: nomes editáveis no topo, formato MD1/MD3/MD5, mapas escolhidos diretamente, lados inicial/OT e importação/exportação.
- Registro rápido por botões, histórico lateral, edição inline de vencedor/bombsite, exclusão confirmada e desfazer/refazer durante a sessão (Ctrl+Z/Ctrl+Y). Correções incompatíveis são recusadas com dados preservados; mapas confirmados continuam protegidos.
- Resumo para remake abre consulta com mapa, lados, bans e rounds nos dois modos, com cópia do texto.
- Menor espaço acima dos bans iniciais e separação da segunda metade; no OBS, seletor de remake no topo, cabeçalho compacto com microfone verde/vermelho e ícones centralizados nos cards.
- Entrega local; branch de compilação nativa usada apenas para gerar a DLL necessária, sem nova publicação na principal ou release.

## 0.1.0-beta.3

- Português, inglês e espanhol na engrenagem, aplicados na próxima abertura.
- Traduções locais da interface, avisos, guia, resumo e PNGs; instalador nos três idiomas.
- Mapas e bombsites traduzidos apenas para exibição, preservando o JSON existente.
- Nome do JSON exportado com data e hora da criação, equipes e situação; ID interno preservado e duplicatas recusadas.
- Nova partida mantida; nenhum botão de reset de partida adicionado.
- Testes de idioma, persistência, modelos de texto e edição de rounds.

## 0.1.0-beta.2

- Instalador e tradução pt-BR com codificação UTF-8 explícita para preservar acentos e cedilha.

- Sobre o CasterDesk na engrenagem: autoria, versão, créditos de recursos e aviso de uso interno.
- Processo opcional de assinatura do executável e instalador, com timestamp e verificação; certificado ainda não configurado.
- Documentação do uso interno e retorno do usuário sobre instalação/desinstalação da beta.1.

## 0.1.0-beta.1

Primeira beta privada: campeonato MD1/MD3, bans, round a round, histórico e JSON, resumo para remake, guia do lobby, cronômetro e casting TXT, exportação PNG e temas.

Inclui instalador por usuário, atalho opcional, desinstalador e runtime .NET. Os dados passam a ficar separados do executável. Esta beta precisa de validação em outro computador antes de ser considerada estável.

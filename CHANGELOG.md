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

- Linha Avalonia: Central, campeonato/lobby, timer/TXTs, backup/restauração e idioma.
- Publicação autossuficiente Windows x64 e correções de nomes personalizados e associação de equipe nas estatísticas.

# 0.3.0-beta.0.4

- Base estabilizada anterior com salvamento recuperável, JSON estruturado, revisão, pacotes e backup.
- Referência técnica preservada para os serviços compartilhados da nova interface.

# 0.3.0-beta.0.3

- JSON de partida versão 2 com base em inglês: equipes padrão explícitas, BO1/BO3/BO5, IDs de mapas existentes e 36 IDs fixos de bombsites com nomes-base legíveis.
- Equipes padrão acompanham o idioma de quem importa; nomes personalizados são preservados, inclusive quando iguais ao texto de um padrão.
- Importação compatível com JSON versão 1 e nomes conhecidos de mapas/bombsites em PT/EN/ES. Novos arquivos exigem um leitor com suporte ao formato 2.
- 599 verificações de portabilidade entre idiomas integradas ao teste WPF. Detalhes em MATCH-JSON-FORMAT.md.

# 0.3.0-beta.0.2

- Central de imagens organizada em cinco abas, com partida compartilhada e formato lembrado por categoria; anúncio em feed e story.
- Canal da transmissão editável no anúncio, nas posições e fontes originais do PSD do JG. Data e horário permanecem ausentes.
- Seleção de logos junto dos nomes das equipes; artes reutilizam os recursos da partida.
- Veto da arte separado do veto registrado. Ao aplicar ou gerar uma correção, opção de atualizar também a partida; atualização real bloqueada quando já existem bans/rounds.
- Falhas de salvamento revertem mudanças em veto, lados, bans e histórico. Nomes pendentes impedem sair/trocar de partida se a gravação falhar; nova partida só entra na lista após ser salva.
- Cronômetro aplica o tempo editado ao iniciar, invalida o cache ao trocar caminho e preserva a exibição de 24 horas. Casting pode ser aplicado independentemente de um tempo inválido.
- Normalização de configurações antigas, preservação de JSON inválido, logs WPF e bloqueio de segunda instância por pasta de dados.
- Validação WPF e regressão de PNG exigidas pelo script de empacotamento. Migração completa do campeonato e demais pontos da revisão continuam planejados.

# 0.3.0-beta.0.1

- Primeira etapa WPF: janela, navegação, menu, Central de imagens, conferência de estatísticas e textos/cronômetro nativos.
- Menu contido na janela; ações de geração fixas; prévias em segundo plano e controles com tamanho consistente.
- Campeonato e seus diálogos ainda integrados em WinForms. Dados, regras e renderizadores das artes preservados. Detalhes em WPF-MIGRATION.md.

# 0.2.0-beta.0.3

- Alinhamento dos campos de lados e do placar sobre o histórico; conteúdo da tabela centralizado e seletores com chevrons.
- Cabeçalhos dos bans centralizados, Round por extenso, selo ATIVOS verde; Nova partida em azul.
- Imagens dos mapas aguardam referências do usuário/JG.

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
- Base C#/WinForms mantida. Modo rápido, trava de bombsite e estatísticas permanecem no backlog.

# 0.1.0-beta.5

- MD5: ban, ban, pick, pick, pick, pick, ban, ban e decider; vitória com três mapas confirmados.
- Cinco mapas na configuração de lados, salvamento e importação/exportação JSON.
- PNG: séries com quatro/cinco mapas geram capa e páginas por mapa em Stories/feed; horizontal usa até três mapas por página.
- Capa compacta evita sobreposição entre nome do mapa e placar.

# 0.1.0-beta.4

- Cabeçalhos dos PNGs centralizados na imagem em Stories, feed e horizontal.
- Logo e identificação do caster em colunas laterais simétricas.

# Histórico

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




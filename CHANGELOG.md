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




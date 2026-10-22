# Validação — 1.0.9-beta.10

Correção de apresentação no painel local. Nenhum registro da partida ou protocolo de comando foi alterado.

- Lados do Remake: duas linhas, **Ataque inicial** e **Ataque no OT**, com o nome da equipe atacante. Sem repetição de quem defende.
- Bans do Remake: dois grupos de seis quadradinhos, separados por metade, com ícone, nome, equipe e número na ordem real de escolha. Espaços não registrados ficam apagados e não são identificados como “sem ban”. Em celulares estreitos os grupos ficam um abaixo do outro para manter os nomes legíveis.
- Histórico das duas telas: **round → bombsite → vencedor**, com as cores das equipes. Finalização e placar foram retirados somente da linha de apresentação. A finalização continua armazenada e carregada ao corrigir um round.
- Botão Fechar no cabeçalho fixo dos painéis internos, independente da rolagem. A verificação de interface aciona esse botão e confere o retorno dos controles da partida, preservando a proteção de fechamento durante restauração de backup.
- 63 verificações de navegador contra o servidor real: inclui os três ajustes, veto em modal, reconexão, resposta perdida sem duplicação, rolagem entre rounds e bans, seleção independente do mapa para remake e layouts de 320/390/700 pixels.

Instalador de teste sem assinatura, sem publicação no GitHub. A conferência final da leitura e do toque continua dependendo do aparelho físico. Nenhuma janela nativa, OBS ou instalador foi aberto pelos testes.

# 0.3.0-beta.0.4 — estabilização WPF

- Campeonato, lobby, mapas, operadores e Sobre em WPF, com comandos separados da interface.
- Salvamento conjunto recuperável para JSON principal, cópia da partida, estatísticas/prints e lote de exportação. Falhas revertem os arquivos já escritos; interrupções são recuperadas na abertura.
- Revisão de mapa confirmado com motivo e registro do resultado anterior. Correções que removem rounds posteriores exigem opção explícita.
- Bans intercalados e seleção em ordem do jogo, com avanço automático. Registro rápido é o padrão.
- Pacote completo .casterdesk com dados e recursos da partida; importação confere integridade e duplicatas. JSON simples e backup geral continuam separados.
- Totais parciais preservam os campos conhecidos e a cobertura por campo. Conciliação explícita de jogadores e preservação de participantes extras. OCR cancelável protege edição manual; diagnóstico dos componentes do Windows.
- Prévias menores e congeladas preparadas em segundo plano, cache limitado das imagens da interface; exportações continuam na resolução final e com o layout do PSD.
- Backup coordenado com as gravações, progresso e limpeza dos diretórios de verificação. Cronômetro mantém a contagem durante a cópia.
- Presets editáveis, saída iniciada com o aplicativo e fechamento não bloqueado por duração digitada inválida; a duração salva anteriormente é preservada.
- Traduções adicionadas para novos fluxos e mensagens de backup/desinstalação. Nomes de equipe passam a aceitar 80 caracteres.
- Versão centralizada, solução .NET, publicação limpa, testes obrigatórios, assinatura opcional, hashes e manifesto.

Ainda é uma beta privada. Não foram executados instalador nem validação interativa com OBS/DPI. OCR exige conferência: nas três leituras arquivadas, 25 campos não foram lidos e um valor reconhecido divergiu da referência manual. Não há promessa de leitura automática perfeita.

---

# CasterDesk 0.2.0-beta.0.8

Otimização de imagens sem mudar os layouts, resoluções, cores ou transparência. Compactados 75 PNGs com comparação exata dos pixels. Sete recursos idênticos da Central compartilham o mesmo arquivo. Os assets passam de 27,2 MB para 22 MB, redução aproximada de 19%.

Exportações PNG recebem compressão sem perda e dispensam canais desnecessários. Exemplos horizontais testados ficaram entre 16% e 40% menores. Gravação medida em torno de 0,3 segundo por imagem no PC de desenvolvimento; não representa benchmark de todos os computadores. Processamento por linhas evita uma cópia completa adicional da imagem em memória.

Caminho fixo do OBS e arquivos existentes preservados. Pipeline de preparação dos assets guarda os originais e verifica os pixels antes de substituir um recurso. PSDs e arquivos das partidas não são otimizados retroativamente.

# CasterDesk 0.2.0-beta.0.7

Correção visual da Central de imagens com posições extraídas dos PSDs originais. A promo recupera o campeonato grande no topo, logos centrais, VS e nomes abaixo. Removidos data, horário, canal e o título inventado de próxima partida.

Resultados e estatísticas respeitam a área dos mapas, margens, cabeçalhos, fontes e painéis do material de referência. Story usa escala uniforme do documento original; o placar permanece agrupado em vez de ocupar toda a largura de cada card. Resultados também incluem estatísticas já conferidas, quando disponíveis.

Seletor de partida reduzido. Opções de fundo usam Fundo padrão, Carregar fundo e Restaurar fundo padrão. Conferência de estatísticas disponível junto ao seletor de mapa para resultado e estatísticas da série. Tela de conferência com textos maiores, campos mais legíveis e botão de confirmação no padrão do programa.

# CasterDesk 0.2.0-beta.0.6

Central de imagens em uma aba própria: anúncio, map veto, resultado da série, estatísticas por mapa e da série. Artes com base no material de JG, nos formatos horizontal, feed e stories; map veto horizontal. Campos de canal e Twitch ficam fora desta etapa.

Logos individuais das equipes, símbolo neutro quando não há logo, campeonato e data/horário do anúncio. Permite carregar um BG PNG/JPEG do campeonato e restaurar o fundo padrão. O BG pode ser recortado para preencher o formato escolhido.

Map veto gera automaticamente o PNG transparente fixo para o OBS e duas cópias na pasta da partida: transparente e com fundo. Cada partida passa a ter sua pasta com JSON, imagens, recursos e prints de estatísticas, incluídos no backup completo.

Estatísticas por print: abrir imagem ou colar com Ctrl+V, leitura local pelo Windows, comparação lado a lado e correção antes de confirmar. Permite marcar dados parciais, remake ou ausência de print. Totais da série usam apenas dados confirmados; números desconhecidos não viram zero. MVP é uma escolha manual. Nomes reconhecidos automaticamente exigem atenção na conferência.
# CasterDesk 0.2.0-beta.0.5

Seleção de operadores na ordem dos prints do Siege, com até 11 por linha e janela adaptada à tela. Bans alternados DEF → ATQ → DEF → ATQ, preservando a equipe responsável. Ao escolher um operador, abre o próximo ban pendente da mesma fase; cancelar ou corrigir encerra a sequência.

Cards mostram mais do corpo em janelas grandes e mantêm recorte compacto em janelas menores. Submenus alinhados à esquerda. Imagens competitivas de Clube, Laboratório e Covil atualizadas e redimensionadas.

Revisão dos textos em inglês e espanhol, incluindo opções de backup e Sobre. Idioma escolhido na instalação aplicado na primeira abertura pelo instalador ou pelos atalhos, sem substituir preferências existentes.

Central de imagens, novos templates e leitura de estatísticas continuam na próxima etapa.
# CasterDesk 0.2.0-beta.0.4

Menu em painel dentro da janela, com registro rápido no topo, Aparência → Siege/Simplificado → Claro/Escuro, seleção destacada e Sobre no final.

Dados novos em Documentos/CasterDesk: Configuracoes, Textos, Imagens, Partidas e Backups. A migração copia dados antigos sem apagar a origem e preserva caminhos de TXT já configurados no OBS. PNGs e JSONs sugerem pastas organizadas, mas permitem outro destino.

Backup ZIP da pasta do aplicativo, com manifesto de integridade, restauração com escolha global de conflitos, cópia de segurança anterior e aplicação ao reiniciar. Arquivos externos e a pasta Backups ficam fora. Limite de 8 GB / 49.999 arquivos. Restaurações verificadas ficam em Backups para recuperação.

Modo rápido como padrão quando não há preferência. Fundo Comunidade/JG disponível para PNGs, mantendo Clássico.

Desinstalação preserva dados e informa o local. Exclusão automática de partidas/backups não incluída, pendente de autorização explícita. Termos de uso permanecem rascunho, sem aceite obrigatório nesta beta. Revisão de idiomas e novos desenhos de JG continuam separados.


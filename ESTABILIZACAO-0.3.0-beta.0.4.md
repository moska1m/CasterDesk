# Entrega 0.3.0-beta.0.4 — 13/10/2026

## Implementado e verificado automaticamente

- D01–D08: comandos com reversão, configuração normalizada/preservada, instância única, estatísticas e prints gravados conjuntamente, JSON principal/snapshot e exportações em lote recuperável.
- T01–T06: duração editada, destino TXT atualizado, casting independente, 24 horas, saída iniciada na abertura normal e presets editáveis. Um valor de duração inválido impede iniciar; ao fechar, mantém a duração anteriormente salva.
- S01–S05: totais conhecidos por campo, identidade persistente e conciliação explícita, participantes adicionais preservados, proteção das edições e cancelamento de leituras antigas. Cobertura por jogador/campo disponível no resumo da Central.
- S06: diagnóstico do OCR local e regressões com três resultados de OCR arquivados; não é uma nova leitura das imagens pelo Windows. Manual continua disponível.
- A01–A06: descarte de pedidos antigos, prévia menor/congelada em segundo plano, cache de imagens da UI limitado a 128 entradas, requisitos antecipados, formato/mapa lembrados por partida/categoria durante a sessão e pacote completo .casterdesk.
- U01, U02, U04, U05: campeonato/lobby/seletores WPF, núcleo de comandos independente de controles, dimensionamento pela área útil do monitor e revisão auditada. Edição que antecipa o fim do mapa não remove rounds posteriores sem autorização explícita.
- U03/U07: histórico em colunas dimensionadas, confirmação visível nos layouts testados, ações de arquivos agrupadas, foco visual e menu marcado como aberto; ainda é necessária auditoria com teclado/leitor de tela.
- U06: nomes completos aceitam até 80 caracteres; abreviação separada continua uma melhoria possível. Artes usam o ajuste existente de texto aos limites do PSD.
- B01–B05: progresso de criação, escritas coordenadas, contagem mantida durante backup, reservas case-insensitive, validação semântica e limpeza segura de staging. Backups anteriores são preservados; não foi implantada exclusão automática/retensão configurável.
- L01/L02: novas traduções e mensagens de backup/desinstalação; JSON canônico em inglês e defaults localizados continuam cobertos nos três idiomas.
- E01–E05/E07: logs/falha fatal legível, Core/WPF separados, release com testes obrigatórios e publicação limpa, versão centralizada, hashes/manifesto e assinatura opcional.
- E08: solução CasterDesk.slnx e documentação de entrada atualizada. A organização física dos arquivos compartilhados e a formatação extensa ainda podem melhorar.

## Evidência

Build sem avisos/erros; 599 verificações de intercâmbio/idiomas; 79 verificações de comandos, transações, recuperação, pacote, estatísticas e OCR; 22 verificações de backup; 13 PNGs comparados por pixels, dimensões e transparência. Campeonato ativo/concluído e Central renderizados fora da tela em três tamanhos. MD1/MD3/MD5 concluídos por comandos até vitória da série, com overtime 8–6.

Os anúncios com e sem canal também foram comparados ao raster da beta.0.3: dimensões e pixels iguais. Isso cobre essas referências, não todas as combinações de arte possíveis.

No diagnóstico local, o Windows disponibilizou OCR pt-BR. Nos três resultados arquivados: 30/30 linhas encontradas, 25 campos sem leitura e um valor reconhecido diferente da referência manual. O exemplo claro do Consulado mantém os 30 valores E/M/A esperados. A detecção não substitui conferência humana.

## Trabalho ainda necessário para encerrar a revisão completa

1. E04/A07/U02/L02: validação interativa de instalação/atualização, OBS, mudança real de monitor/DPI, teclado/leitor de tela e casos extremos de nomes/logos/fundos. Nenhuma janela do aplicativo nem o instalador foi aberto nesta entrega.
2. L03: revisão regional do catálogo por casters nativos e conferência das fontes oficiais. Não alterar IDs de partidas antigas.
3. A03/B01/B05: benchmark e cache de prévias por conteúdo, progresso detalhado de restauração e política configurável de retenção. Não há promessa de ganho de desempenho medido nem exclusão automática de backups.
4. E06: registrar o estado validado no Git e verificar a publicação remota, sem reconstruir versões antigas artificialmente.
5. Diretrizes de uso permanecem no rascunho TERMOS-RASCUNHO.md; aceite obrigatório e direitos de distribuição dos recursos exigem definições do responsável. Assinatura do instalador depende de certificado.

A revisão original é histórica. Esta entrega não significa que todos os 50 itens ficaram encerrados nem certifica uma transmissão completa ou lançamento público.

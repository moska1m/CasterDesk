# Entrega atual — 0.3.0-beta.0.4

Estado implementado, evidências e trabalho restante em [ESTABILIZACAO-0.3.0-beta.0.4.md](ESTABILIZACAO-0.3.0-beta.0.4.md). O histórico abaixo registra a etapa anterior.

---

# CasterDesk 0.3.0-beta.0.2 — primeira entrega da estabilização

## Entrega desta etapa

Central de imagens em abas (anúncio, veto, resultado, estatísticas por mapa e da série), com seletor de partida compartilhado. Anúncio oferece feed/story, campeonato e canal. A arte mantém o PSD do JG: coordenadas do layout-promo.json, GT America Compressed Bold no título/canal e Simplon Mono Bold nos nomes/VS/ASSISTA EM. Fontes conferidas nos registros FontSet do PSD original e nas tabelas name dos OTF fornecidos, sem instalar fontes no Windows.

Logos selecionadas no cadastro das equipes são guardadas nos Recursos da partida e reutilizadas nas artes. Permanecem disponíveis na Central, com opção neutra. Os campos de canal não aparecem nas outras categorias.

Correção editorial do veto guardada em arte.json (ArtVeto, ArtFormat, ArtSides), sem alterar o JSON de partida. A prévia acompanha as alterações. Aplicar/Gerar pergunta se a pessoa deseja salvar também na partida; a atualização real exige veto válido e ausência de rounds/bans registrados. Restauração do veto original disponível. São preservados os dois arquivos da pasta e o arquivo transparente fixo para OBS.

Correções de estabilidade: nomes não salvos impedem fechamento/navegação; reversão de alterações quando a persistência falha; criação de partida somente após salvar; tempo editado aplicado ao iniciar; caminho TXT atualizado manualmente invalida o cache; 24 horas não viram zero; casting independente da validação do timer; configurações antigas normalizadas e JSON inválido copiado antes da recuperação; logs WPF; segunda instância bloqueada para a mesma pasta de dados. Prévias obsoletas aguardando processamento são descartadas antes de renderizar.

## Verificação e limites

scripts/Test-Wpf.ps1 usa somente dados isolados e renderização fora da tela. Abrange cinco abas, três tamanhos, correção editorial sem mutação da partida, persistência, duplicatas, atualização real explícita, proteção de partidas registradas, falha de gravação com arquivo bloqueado e reversão, cronômetro e configurações. A regressão PNG verifica pixels/dimensões/transparência. Build-Wpf.ps1 interrompe o empacotamento se esses testes falharem.

Ainda precisa de validação interativa do aplicativo instalado, troca de DPI/monitores, arquivos reais de OCR e operação com OBS. Nenhuma janela do programa nem o instalador foi aberto para testar no computador do usuário. O bloqueio de segunda instância foi implementado; seu aviso interativo não foi exercitado pelos testes.

## Próximas etapas antes da apresentação pública

1. Persistência conjunta dos arquivos de estatísticas/prints e lote de exportação; cópia consistente para backup. Configuração corrompida já é preservada, mas recuperação de referências ausentes precisa de interface própria.
2. OCR: proteger edição manual, cancelar leituras substituídas, preservar campos parciais na soma da série e conciliar jogadores entre mapas/remakes.
3. Completar a separação do núcleo e a migração do campeonato/seletores para WPF. Validar teclado, DPI, acessibilidade e todos os fluxos de partida.
4. Testar instalação/atualização, backup/restauração e idiomas completos; consolidar versão, publicação e histórico no Git. O instalador compilado não equivale a instalação validada.
5. Depois da validação, preparar o roteiro do vídeo com uma partida de demonstração: criar confronto/logos → veto → rounds → conferir print → gerar anúncio/resultado/estatísticas → apontar o arquivo fixo no OBS.

A revisão completa em REVISAO-ESTADO-ATUAL-2026-10-13.md permanece como lista de trabalho. Esta entrega não encerra todos os seus 50 pontos nem constitui declaração de estabilidade para lançamento público.

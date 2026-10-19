# CasterDesk 1.0.4 — 18/10/2026

Layout local: removidos o rodapé de marca e o status permanente de salvamento. Mensagens de erro e recuperação aparecem sob demanda. Confronto usa apenas a altura necessária, até o limite definido; Mapas da partida recebe o restante.

Bans: metades 1–6 e 7–12 lado a lado, três DEF acima e três ATQ abaixo. A terceira coluna fica separada dos bans iniciais. Slots, equipes, cores e sequência de seleção são os mesmos do modelo. Miniaturas reduzidas e proporcionais; alturas ajustadas ao espaço real do painel. SPEC recebe a melhoria de interpolação dos ícones, mantendo sua disposição própria.

Validação:
- Compilação Release sem erros nem avisos.
- Verificações do núcleo aprovadas em `test-results/avalonia-core-cb28840aacf64563b327502371d92d5b`.
- Publicação independente validada exclusivamente com Avalonia.Headless em `test-results/avalonia-ui-fc32e7b435c44ff594fc8dff91ad1881`.
- 250 verificações do fluxo de campeonato, incluindo os 12 bans dentro do painel em 980×640, 1280×800 e 1920×1080, agrupamento por função, separador e preservação das proporções das imagens.
- MD3 e MD5: todos os seletores de lados cabem no painel ampliado em Full HD, sem rolagem. Em janelas menores a área de mapas continua rolável.
- Grupos de testes de textos/cronômetro, backup, idiomas, marca, correções, HTTP do OBS e SPEC aprovados.
- Quadros reais dos testes inspecionados para janela compacta, Full HD, bans e MD5.

Instalador: `dist/CasterDesk-1.0.4-win-x64-Setup.exe`, com SHA-256 e manifesto ao lado. Compilação Inno Setup concluída. Não foram executados instalador, interface nativa ou OBS. Instalação e avaliação humana no desktop permanecem para teste do usuário. Nenhum envio ao GitHub nesta entrega.

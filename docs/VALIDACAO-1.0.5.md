# CasterDesk 1.0.5 — 18/10/2026

Complementa o layout 1.0.4 com seleção de operadores em sete colunas, ordem existente preservada e Sem banimento na última posição. Todos os seletores usam 72 px de altura, ícones de 36 px, padding de 4 px e fonte de 10 px. O fundo dos cards garante contraste para os símbolos brancos nos dois temas.

Cards da partida: vazios mostram DEF/ATQ, sem inferir que um campo vazio representa Sem banimento. O valor explícito de Sem banimento permanece no modelo, importação, resumo para remake, tooltip e nome acessível. Etapas futuras usam 50% de opacidade, liberadas e registradas usam 100%. IsEnabled continua seguindo as regras de desbloqueio e confirmação. O placar do modo Caster fica na própria barra de título do histórico, com tooltip completo e reticências em larguras pequenas. O placar superior do SPEC é preservado.

Verificação final:
- Núcleo aprovado: `test-results/avalonia-core-f27252ce0e8441978de00a089b8bd852`.
- Publicação validada com Avalonia.Headless: `test-results/avalonia-ui-4af3697074564a2ead1d8c8df7fc2e88`.
- 289 verificações de campeonato, incluindo dimensões da opção Sem banimento, ordem e última posição na grade, agrupamento dos bans, opacidade e placar na mesma barra do título em 980×640, 1280×800 e 1920×1080.
- Demais grupos de interface, cronômetro, backup, idiomas, marca, correções, integração HTTP do OBS e SPEC aprovados.
- Imagens dos testes inspecionadas: grade de operadores, painel Full HD e janela compacta.

Compilação e empacotamento locais; nenhum envio ao GitHub. Não foram abertos CasterDesk nativo, instalador ou OBS na área de trabalho. Instalação e aparência no backend nativo continuam dependentes do teste do usuário. O instalador e o manifesto final estão em `dist/`, acompanhados do SHA-256.

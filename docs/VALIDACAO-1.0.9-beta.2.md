# Validação — 1.0.9-beta.2

Instalador Windows x64: `CasterDesk-1.0.9-beta.2-win-x64-Setup.exe`, 134.618.009 bytes.

SHA-256: `a3fbec68814908da646536d84a7cc6ac0286d3f924a72f0cd7f381843928c29a`.

## Verificado

- Compilação e publicação sem erros nem avisos. Testes do núcleo e da interface aprovados com dados isolados.
- Regressões de partidas, SPEC, cronômetro, imagens, tabelas, Google Sheets, atualização, backup e HTTP do painel OBS aprovadas. Os símbolos de ataque/defesa e o ícone da Azami continuam disponíveis na API após reorganizar os arquivos.
- Cards de replay de 68 pixels em 800×600 e 1060×620, doze por página, seleção preservada entre páginas, identificação de trechos relacionados e seleção conjunta. Alterar a seleção ou pasta invalida a conferência anterior.
- Sugestões de continuação verificadas para retorno à mesma sessão e remake; mapas diferentes e partidas começando do zero não são sugeridos como continuação. Somar sessões continua exigindo classificação explícita.
- 78 artes originais e 234 retratos presentes no pacote, nas três dimensões esperadas e com transparência. Folga superior, alinhamento do Glaz e Wamai com Azami e cards em 980×640, 1280×800 e 1920×1080 verificados.
- Microfone transparente nas dez resoluções do ICO, incorporado no executável e no instalador, conferido sem executá-los.
- Enxoval criado a partir de pasta de PNGs e exportado/importado como ZIP. Cores, fundos por área/formato e persistência conferidos; logos, canal, veto e MVP da partida preservados. Pacotes com travessia de caminhos e imagem inválida recusados sem substituir o tema salvo.
- Popup inicial usa as notas verificadas, respeita verificação automática e filtro de betas, permite adiar e não reaparece na mesma sessão. Testado com catálogo de teste assinado; nenhum instalador executado.
- Guia para o designer, licenças, origem e fontes do leitor presentes no pacote. Arquivos da instalação separados em subpastas; não há PNGs achatados na raiz de `assets/siege`.
- Manifesto assinado do atualizador verificado com a chave pública existente. Isso é separado da assinatura Authenticode; o instalador ainda não tem Authenticode.

## Testes de uso ainda necessários

Instalação/atualização interativa, aparência no monitor real e uma partida nova com o Siege gravando os replays. A remoção dos arquivos antigos da instalação lista somente nomes obsoletos conhecidos; não altera a pasta de dados do usuário. Os testes desta entrega não abriram OBS nem instalaram o aplicativo.

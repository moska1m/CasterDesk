# Organização dos recursos

Os arquivos da instalação ficam separados por finalidade:

- `assets/branding`: marca e ícones do aplicativo.
- `assets/siege/maps`: mapas usados na interface.
- `assets/siege/operators/icons`: emblemas dos operadores.
- `assets/siege/operators/originals`: artes amplas oficiais, antes do recorte para os cards.
- `assets/siege/operators/portraits/small`, `medium` e `large`: retratos de 80×72, 160×144 e 320×288, escolhidos conforme o tamanho e a escala da interface.
- `assets/studio/templates`: elementos das composições, separados em feed, stories, horizontal e anúncio.
- `assets/studio/backgrounds`: fundos padrão do gerador.
- `assets/studio/layouts`: posições dos elementos das composições.
- `assets/studio/map-veto`: faixas de mapas para exportação.
- `assets/studio/icons`: símbolos e ícones das estatísticas.

Os retratos preservam a largura do corpo, com folga no topo do card e alinhamento superior. Poses largas usam um trecho maior do tronco para manter a cabeça na mesma linha. O enquadramento do Glaz limita a sobra do cano da arma, sem diminuir seu corpo. Montagne e Blackbeard têm enquadramento lateral específico para reduzir a área do escudo. Clash tem o topo do escudo recortado acima da cabeça; Blitz mantém o enquadramento normal. As artes originais permanecem disponíveis. Origem e recortes: `assets/siege/operators/portraits-manifest.json`.

Nas pastas das partidas, novos fundos ficam em `Recursos/Fundos`, logos em `Recursos/Logos` e pacotes importados em `Recursos/Campeonatos`. Exportações novas usam `Imagens/Feed`, `Imagens/Stories` e `Imagens/Horizontal`, com subpastas por finalidade. Arquivos anteriores continuam válidos; não são movidos na pasta do usuário. O arquivo fixo do veto no OBS conserva seu caminho.

Para criar um enxoval externo, consulte [o guia para o designer](PACOTE-VISUAL.txt).

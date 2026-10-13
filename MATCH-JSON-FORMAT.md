# Partidas portáveis — formato JSON 2

A gravação e exportação usam a mesma base em inglês, independentemente do idioma do criador. A leitura converte os valores conhecidos para o catálogo interno; a interface e as artes usam o idioma do leitor.

- `TeamA`/`TeamB`: nome personalizado intacto ou `Team A`/`Team B` quando padrão.
- `TeamAIsDefault`/`TeamBIsDefault`: indicam se o nome deve acompanhar o idioma. Mesmo um nome personalizado igual a `Team A` é preservado quando a marca é `false`.
- `Format`: `BO1`, `BO3` ou `BO5`; o programa exibe MD/BO conforme idioma.
- `MapId` e veto: IDs estáveis existentes, como `bank` e `clubhouse`.
- `MapName`: nome-base inglês para facilitar leitura humana; a identidade vem do ID.
- `Rounds[].SiteId`: ID estável do bombsite dentro do mapa.
- `Rounds[].Site`: nome-base em inglês para leitura humana. Quando há SiteId, ele determina o bombsite.
- Finalizações continuam com os códigos existentes: `elimination`, `time`, `defuser_completed`, `defuser_disabled`.

Exemplo parcial de campos:

```json
{
  "Version": 2,
  "TeamA": "Team A",
  "TeamAIsDefault": true,
  "TeamB": "FaZe Clan",
  "TeamBIsDefault": false,
  "Format": "BO3",
  "Maps": [{
    "MapId": "bank",
    "MapName": "BANK",
    "Rounds": [{
      "SiteId": "tellers-office-archives",
      "Site": "Tellers' Office - Archives",
      "Winner": 1,
      "Ending": "elimination"
    }]
  }]
}
```

O exemplo é um trecho explicativo, não uma partida completa importável.

## Arquivos anteriores

JSON versão 1 continua aceito. Nomes padrão antigos (`Equipe A/B`, `Team A/B`, `Equipo A/B`) são reconhecidos. Mapas e bombsites conhecidos em português, inglês e espanhol são normalizados, sem tentar adivinhar nomes desconhecidos. Os 36 identificadores de bombsites estão fixados em source/match-sites.json; não dependem do idioma nem da posição de um item na lista.

Nos arquivos antigos não havia marca de nome padrão: um nome personalizado literalmente igual a um desses nomes padrão é indistinguível e será tratado como padrão. Nos novos arquivos a marca explícita elimina essa ambiguidade. Limpar o campo de nome restaura o padrão; alterar só uma equipe não transforma a outra em nome personalizado.

A importação não reescreve o arquivo recebido. Quando a partida é salva/exportada pelo programa atualizado, usa formato 2. Versões antigas do CasterDesk, que aceitam somente formato 1, precisam ser atualizadas para abrir esses arquivos novos.

## Verificação

MatchInterchangeTests integra o teste WPF e verifica 599 condições: três idiomas de criação × três de leitura × nove mapas/36 bombsites, JSON idêntico entre idiomas, nomes personalizados, migração de arquivos antigos e regras de bloqueio de bombsite. IDs desconhecidos são rejeitados. Os testes usam exclusivamente pastas isoladas.

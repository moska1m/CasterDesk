# Validação — 1.0.9-beta.9

## Fluxos do controle local

- 69 verificações HTTP/headless: acesso autenticado, configurações, veto, lados/OT, logos, bans, rounds, duplicatas, bloqueios e revogação. Inclui placar, finalização, atacante e ordem de bans da referência de remake.
- 54 verificações no Edge headless com o servidor real e dados isolados: série MD3, veto em diálogo modal, fechamento por escolha/Escape, mapas usados indisponíveis, reconexão, resposta perdida sem duplicação, cores distintas no histórico e rolagem automática.
- Confirmado: ao entrar em uma nova etapa de bans, as escolhas ficam visíveis automaticamente. Depois do último ban, a página retorna ao registro do round. Consulta sem mudança de etapa não rouba a posição de rolagem.
- Remake: imagem, placar, lados e OT, seis bans registrados por ícones e histórico de três rounds. Consultar um mapa ainda não jogado mostra zero rounds/bans e não muda o mapa ativo da partida.
- Layouts de 320, 390 e 700 pixels nas três abas sem transbordamento horizontal. Diálogo cabe em 320 × 650 e mantém a seleção em área rolável própria.
- Screenshots de conferência: `mobile-remake.png`, `mobile-veto-dialog.png`, `mobile-rounds.png` e `mobile-match.png` na pasta isolada de resultados do navegador.

## Diretório de instalação

O instalador usa CasterDesk como destino sugerido para os nomes legados conhecidos, inclusive CasterDesk-Avalonia, e preserva destinos personalizados diferentes. Não remove nem move os arquivos da instalação anterior. Dados continuam em Documentos/CasterDesk. Bibliotecas de terceiros preservam os nomes técnicos necessários. A compilação do instalador valida a sintaxe; o fluxo de instalação/atualização nativa ainda precisa de teste físico.

## Limites da confirmação

O usuário confirmou o funcionamento local da beta.8 no celular, sem atraso perceptível. Os novos movimentos, o diálogo e a aba Remake da beta.9 foram verificados em navegador automatizado; resta conferir a experiência no aparelho real. Nenhum instalador, OBS ou janela nativa foi aberto por esses testes.

Candidato local sem assinatura. Publicação no GitHub e anúncio no atualizador não fazem parte desta validação.

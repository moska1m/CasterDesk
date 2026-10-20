# Estatísticas por replay — experimental

Disponível para teste local na versão 1.0.9-beta.1. O processamento acontece no computador, sem enviar replays para serviços externos.

## Usar

1. Termine o mapa no Siege e aguarde a gravação dos replays. Se a leitura indicar arquivo em gravação, tente novamente depois de sair do mapa/lobby.
2. No CasterDesk, confirme o mapa ou abra suas estatísticas no Gerador de imagens.
3. Clique em **Importar replays · beta**. Informe a pasta `MatchReplay` da instalação do Siege e clique em **Listar pastas**.
4. Aguarde a identificação de **mapa · equipe × equipe**, com data e pasta abaixo, e marque os trechos correspondentes. A pré-leitura consulta o cabeçalho de um arquivo da pasta; arquivos incompletos ou incompatíveis aparecem como não identificados. Inclua os trechos anteriores e posteriores ao remake. A importação completa valida novamente todos os arquivos: outros mapas são ignorados; equipes incompatíveis interrompem a leitura.
5. Clique em **Ler selecionados**. Retornos ao mesmo ID de sessão são agrupados; para cada sessão diferente, escolha **Retorno: já inclui acumulados anteriores** ou **Novo lobby: somar ao trecho anterior**.
6. Confira cobertura, placar, E/M/A e diferenças em relação aos valores salvos. Clique em **Aplicar à conferência**, revise os campos e finalize em **Confirmar estatísticas**.

Aplicar à conferência ainda não grava a partida. Fechar sem confirmar preserva os registros anteriores. Os replays só são arquivados junto com o salvamento confirmado das estatísticas.

## O que esta beta entrega

- Eliminações, mortes e assistências a partir dos acumulados do placar, com perfil Ubisoft associado ao jogador.
- Headshots, primeiras eliminações, primeiras mortes, plants e desarmes observados nos eventos. Esses dados ficam salvos por jogador e podem ser agregados na série, com cobertura parcial quando faltam rounds. As artes e o modelo de tabela atuais continuam usando E/M/A; as novas composições serão integradas separadamente.
- Classificação explícita de retorno e lobby novo, evitando somar novamente uma base já restaurada.
- Prévia das diferenças, lacunas e finalizações. Prints continuam disponíveis, mas não são obrigatórios para confirmar dados completos do replay.
- Cópia dos replays originais e da leitura em `Replays/<mapa>-<identificador>.zip`, dentro da pasta da partida. Nomes originais dos arquivos são preservados dentro do ZIP.
- Arquivos verificados por hash; arquivamento e estatísticas usam a mesma operação de salvamento recuperável.

## Limites

- A confirmação do mapa abre a conferência de estatísticas; a seleção e leitura dos replays ainda são manuais. Não há monitoramento automático permanente da pasta do Siege nesta beta.
- O ID do lobby não define a série. Reiniciar o lobby entre mapas é normal; a seleção é filtrada pelo mapa e pelas equipes. Remakes no mesmo mapa precisam de revisão dos trechos e acumulados.
- Rounds e bans manuais não são substituídos automaticamente. A prévia mostra dados do replay para consulta, preservando também os bans já registrados se o retorno não os trouxer.
- Lobbies com nomes de equipes diferentes dos cadastrados, escalações alteradas, sobreposição de rounds ou contadores incompatíveis exigem revisão manual.
- Campos ausentes permanecem vazios. Falhas do leitor não produzem estatísticas zeradas. A marca de cobertura completa avalia os acumulados selecionados e o placar; não garante que todos os eventos intermediários estejam disponíveis.
- A finalização é inferida pelo leitor. Plant é um evento; eliminação após plant continua sendo eliminação quando essa for a condição de encerramento. Motivo desconhecido permanece não informado.
- A seleção aceita até 150 arquivos, 200 MB por arquivo e 2 GB no total. Um arquivo mudando durante a leitura é recusado.
- Replays e backups podem ocupar bastante espaço. A pasta do jogo nunca é alterada pelo CasterDesk.

## Validação realizada

Teste local com cópias de 19 arquivos de NA ORG × FaZe, de 17/10/2026: Mansão e Banco, incluindo retorno ao mesmo lobby e um lobby novo. Os 19 arquivos foram lidos; os dez acumulados foram extraídos por arquivo. Mansão conferida com print e estatísticas salvas, com uma assistência digitada incorretamente identificada. A última sessão do Banco confere com o print para os dez jogadores. A consolidação completa do Banco continua exigindo revisão contra os trechos anteriores ao remake.

Também foram testados arquivamento, persistência de identidades, recusa de classificação ausente e preservação dos dados após falha de arquivamento. Teste com uma nova partida ao vivo, arquivo efetivamente em gravação e instalação interativa ainda é necessário.

## Leitor utilizado

Adaptador local baseado em [julio208920/r6-dissect v1.7.0](https://github.com/julio208920/r6-dissect/tree/6602cf2e2d72e645cf2b2a3a801fa953cf21903f), derivado de redraskal/r6-dissect, com saída adicional de acumulados. Licenças, origem e fontes do adaptador acompanham a instalação em `assets/replay`.

# CasterDesk 1.0.7 — mudanças desde a 1.0.1

Esta versão reúne as atualizações locais 1.0.2 a 1.0.7. A última release pública anterior era a 1.0.1; não foram criadas releases intermediárias retroativas.

## Instalar ou atualizar

Baixe **CasterDesk-1.0.7-win-x64-Setup.exe** nesta release, feche CasterDesk e OBS e instale. Se usa o painel no OBS, marque **Instalar o painel CasterDesk no OBS**: atualizar apenas o app não substitui a DLL do painel. O pacote inclui o runtime .NET para Windows x64.

## Imagens e estatísticas — 1.0.2

- Resultado e estatísticas da série reunidos em uma única aba, evitando imagens redundantes.
- Conciliação explícita quando variações de nomes produzem mais de cinco identidades na equipe. A geração é bloqueada até revisão; não há mesclagem aproximada nem descarte silencioso.
- Fonte dos nicks padronizada por formato e MVP alinhado ao grupo de eliminações/mortes/assistências.
- Cor de destaque e fundo independentes por categoria de arte, com cores prontas e hexadecimal.

## Uso diário — 1.0.3

- Nick Twitch salvo para novas partidas, com prefixo twitch.tv/ fixo.
- Nomes das equipes confirmados ao sair do campo ou pressionar Enter, preservando a digitação sem reconstruções durante a edição.
- Sem banimento explícito, selecionável e repetível por função, preservado em dados, SPEC, remake e OBS.
- Gerador de imagens com navegação padronizada, seletor à direita e espaço reservado à rolagem.
- Aplicar tempo substitui o restante do timer mantendo execução/pausa, no app e no comando do OBS.

## Layout e bans — 1.0.4 e 1.0.5

- Rodapé de marca e linha permanente de salvamento removidos, mantendo avisos necessários de erro/recuperação.
- Confronto mais compacto e mais espaço para os lados dos mapas em MD3/MD5.
- Bans no app divididos em metades lado a lado, DEF acima, ATQ abaixo e separação antes do terceiro ban.
- Miniaturas proporcionais para retratos e símbolos em janelas menores.
- Seleção de operadores no app em sete colunas, na ordem do jogo, com Sem banimento no final e tamanho padronizado.
- Bans futuros a 50% de opacidade; liberados e registrados a 100%.
- Placar do modo Caster incorporado ao título do histórico, preservando o placar superior do SPEC.

## Correções e desempenho — 1.0.6

- Aplicativo abre maximizado por padrão.
- Trocas de mapa/formato e aplicação do veto da arte protegem também estatísticas salvas sem rounds ou bans.
- Alterações de lado incompatíveis são recusadas sem apagar finalizações existentes.
- Equipe no topo do print persistida; arquivos antigos sem orientação exigem seleção explícita antes de nova leitura.
- Reconfirmar estatísticas sem trocar a imagem reutiliza o print arquivado.
- Gravação dos oito TXT e configuração com preparação e rollback em caso de falha.
- Plugin OBS respeita a quantidade de presets salva (0–8) e evita reconstruir o remake quando não houve alteração.
- Seleções rápidas de round atualizam os controles existentes; exportação reutiliza a prévia integral quando os dados não mudaram.
- Caminho inválido do timer não quebra a consulta de estado. Instalador verifica o resultado da instalação opcional do plugin.

## Painel OBS atualizado — 1.0.7

- Sem banimento no final da seleção, mantendo a ordem dos operadores do jogo, inclusive ao conectar um app anterior.
- Cards e ícones com as mesmas dimensões para operadores e Sem banimento; texto abaixo do ícone.
- Bans futuros com 50% de opacidade e registrados com símbolo da função quando não houve ban.
- Significado de Sem banimento preservado no tooltip, acessibilidade, dados e remake.

## Validação e limites

Build do app sem avisos ou erros, testes do núcleo e interface headless, 91 verificações HTTP OBS e 87 verificações nativas Qt do painel. As regras, histórico, importação/exportação, backup e SPEC usam o estado compartilhado existente. O teste de clique do SPEC sincroniza o desenho da cena após desfazer antes de enviar eventos de mouse.

Nenhum programa, instalador ou OBS foi aberto na área de trabalho durante esta entrega. Instalação/UAC e uso dentro do OBS real ainda dependem de teste manual; o instalador não possui assinatura digital. Veja os relatórios em docs/VALIDACAO-1.0.4.md até docs/VALIDACAO-1.0.7.md e o registro da auditoria em docs/AUDITORIA-2026-10-18.md.

O código próprio permanece sob MIT. Marca, distribuição oficial e recursos de terceiros seguem os avisos e termos já presentes no projeto. Registros Sem banimento exigem versão 1.0.3 ou posterior.

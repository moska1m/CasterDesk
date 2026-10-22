# Controle pelo celular

1. Instale o CasterDesk. A opção **Preparar o controle pelo celular na rede privada** prepara a regra do Windows com permissão de administrador. Se não autorizar durante a instalação, a mesma preparação fica nas opções.
2. Coloque o PC e o celular na mesma rede. O PC pode usar cabo e o celular Wi-Fi. A rede do Windows deve ser privada; redes de convidados podem isolar os dispositivos.
3. No CasterDesk, abra **Opções → Geral → Controle pelo celular** e clique em **Ativar controle local**.
4. Leia o QR ou use **Copiar link para o celular**. Se houver várias interfaces de rede, selecione o endereço da rede usada pelo celular.
5. Feche as opções no PC para liberar a operação. O CasterDesk continua aberto durante o uso.

O painel tem três abas: **Partida** (equipes, logos PNG/JPG de até 2 MB, formato, coin flip, primeiro ban, veto em janela, lados e OT), **Round a round** (mapa, botões de site/vencedor/finalização, correção pelo histórico e bans por ícones) e **Remake** (consulta por mapa com imagem, placar, lados e OT, bans registrados na ordem de escolha e histórico). Consultar outro mapa na aba Remake não troca o mapa ativo nem altera os registros.

No Remake, os lados mostram somente **Ataque inicial** e **Ataque no OT**, identificando a equipe atacante sem repetir quem defende. Os bans usam quadradinhos por metade, com ícone, nome, equipe e número da escolha; espaços ainda não registrados não são apresentados como “sem ban”. O histórico segue **round → bombsite → vencedor**, sem exibir finalização ou placar por linha. As finalizações continuam armazenadas e disponíveis na correção do round.

Quando uma etapa libera bans, eles passam à frente do histórico e a página rola até a escolha do operador. Concluiu os bans, o histórico volta ao lugar e a página retorna ao registro do próximo round. Sincronizações sem mudança de etapa não reposicionam a tela. A opção de movimento reduzido do dispositivo é respeitada. Nenhum dado de jogador foi acrescentado à configuração.

Alterações que exigem apagar veto, bans ou rounds são recusadas no celular; faça as revisões necessárias no PC. Mapas confirmados permanecem protegidos. O modo SPEC continua usando a configuração direta de mapas do PC.

## Reconexão

- Ao retornar à página, sair do bloqueio de tela ou recuperar a rede, o painel consulta novamente o PC.
- Recarregar preserva a sessão pelo cookie durante sua validade. Não é necessário reler o QR a cada troca de aplicativo.
- Não é possível registrar offline. Os botões são bloqueados até a atualização do estado.
- Cada comando leva identificação da partida/mapa e revisão do estado. Repetições e comandos desatualizados são rejeitados.
- Se a resposta se perder depois de o PC salvar, o painel consulta o estado; não reenvia automaticamente a alteração.
- O navegador pode suspender a página em segundo plano. A conexão é retomada na volta; não depende de manter execução escondida.
- Fechar o PC/programa, desativar ou gerar novo link invalida o acesso. Reabra o link atualizado pelo QR. Mudanças no endereço IP do PC exigem reativar e usar o endereço novo.

## Instalação e alcance

Não exige aplicativo no celular, login Google, internet ou OBS. Não instala serviço em segundo plano nem altera o roteador. Recursos e QR são locais. O firewall libera apenas o programa no perfil privado e na sub-rede local. A regra não é removida ao fechar o programa, mas não há servidor escutando quando o controle está desativado. O botão **Preparar acesso na rede privada** permite reaplicar a regra se necessário.

O instalador sugere a pasta `Programs/CasterDesk` quando a instalação anterior usa um dos nomes legados conhecidos (como `CasterDesk-Avalonia`). Diretórios personalizados diferentes são preservados. A pasta antiga não é apagada automaticamente; partidas e configurações permanecem em Documentos/CasterDesk. Dependências de terceiros mantêm os nomes necessários ao funcionamento.

O convite usa um segredo aleatório de 256 bits no fragmento do link, retirado da barra após o pareamento. A sessão usa cookie HttpOnly/SameSite e expira em até 24 horas. Os segredos não entram nos arquivos de partidas nem nos backups. Há bloqueios de origem/Host, limites de requisições e de tamanho das logos. O HTTP está limitado à rede local de confiança; o controle não oferece acesso remoto nem HTTPS local.

## Validação

`--casterdesk-check --local-control-only` usa dados isolados e Avalonia.Headless. `tests/local-control-browser.cjs` usa a fixture `--casterdesk-check --local-control-fixture` com Edge em modo headless para testar reload, perda/retorno de rede, eventos de retorno à página, resposta perdida e duplicatas.

Esses testes não comprovam leitura do QR em câmera física, Safari/Android real, autorização UAC, instalação nativa ou conectividade no roteador do usuário. Esses pontos precisam do teste com um celular na mesma rede.

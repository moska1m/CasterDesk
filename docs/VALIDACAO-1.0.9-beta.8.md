# Validação — 1.0.9-beta.8

## Concluído

- Build Release e publicação Windows x64 autocontida sem avisos nem erros.
- Suíte do núcleo: replays, pacote visual, atualizador, Sheets, XLSX, intercâmbio, integridade, backups e imagens. Dados isolados em caminho curto para evitar o limite de caminhos do GDI+.
- Suíte Avalonia.Headless da publicação: telas, moldura, opções, idiomas, estatísticas, atualizações, tabelas, SPEC e 94 verificações HTTP do painel OBS.
- 66 verificações específicas do controle local na publicação final: autenticação, origem/Host, veto, lados, OT, no-ban repetido, rounds, duplicatas, upload e persistência da logo, alterações protegidas, revogação e reinício. OBS desativado durante esse teste.
- 17 verificações no Edge headless, usando o servidor real da publicação final: configuração, veto, mapas indisponíveis, lados, bans, rounds, reload com cookie, offline/online, retorno por pageshow/foco/visibilidade e resposta perdida após o PC salvar. Sem reenvio automático nem duplicata. Layouts de 320, 390 e 700 pixels sem transbordamento horizontal.
- Compilação do instalador com os arquivos do painel, QRCoder, licença e preparação do firewall privado. O acesso permanece desligado até a ativação no programa. Atualizações automáticas não executam a preparação de rede nem reinstalam o OBS.

## Pendente de teste físico

- Instalar esta beta e autorizar a preparação de rede no Windows.
- Ler o QR em um celular real e acessar pela rede Wi-Fi do usuário.
- Alternar aplicativos e bloquear/desbloquear a tela em Android/iOS reais.
- Testar redes com isolamento de clientes, VPN, múltiplas interfaces e mudança de IP do PC.

O instalador é um candidato local sem assinatura, sem publicação no GitHub nem anúncio no atualizador. Nenhuma instalação nativa, mudança de firewall ou abertura de OBS/Siege foi executada durante a validação automatizada.

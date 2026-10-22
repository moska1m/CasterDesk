# Validação — 1.0.9-beta.11

O instalador encontrou o processo anterior do CasterDesk ainda ativo depois do fechamento da janela. O Windows exibia a solicitação de fechar aplicativos, sem aplicar o novo executável. O processo residual, sem janela principal, foi encerrado para liberar a tentativa do usuário.

A limpeza assíncrona saiu do evento Closed: o fechamento agora é adiado até concluir os salvamentos e desligar os servidores locais. As proteções de exportação, processamento e restauração permanecem.

O teste de controle local abre uma janela headless, inicia o servidor em loopback, solicita o fechamento, aguarda a janela fechar e verifica que a mesma porta pode ser reutilizada. Não abre interface nativa, não modifica firewall e não executa o instalador.

A instalação e o encerramento no Windows do usuário ainda precisam ser conferidos. O pacote é um candidato local sem assinatura e não foi publicado no GitHub.

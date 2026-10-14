# Painel CasterDesk no OBS

Primeira etapa: controlar o cronômetro e os bans de operadores em um painel encaixável do OBS, compartilhando o estado e os comandos do CasterDesk aberto no mesmo PC. A integração aproveita os Custom Browser Docks disponíveis no obs-browser: https://github.com/obsproject/obs-browser.

Na versão 0.3.0-beta.0.8, a integração existente é por arquivos TXT e PNG. O painel de controle está em desenvolvimento; este documento acompanha sua implementação e validação. Não há um plugin nativo separado ou controle de cenas nesta etapa.

O timer deve continuar contando se o painel fechar/recarregar. Bans seguem o mapa e a partida selecionados, DEF/ATQ, equipe responsável e etapas já liberadas; mapas confirmados permanecem protegidos. Alterações feitas no programa reaparecem no painel. Correções de resultados e salvamento continuam no programa.

O painel é controle para o operador, não uma fonte de navegador adicionada à cena. Conexão apenas local, com endereço e chave próprios; sem acesso pela rede. Stream Deck fica para depois, usando a mesma camada de comandos.

Estado e instruções de configuração serão atualizados após os testes da primeira entrega. Não foi aberto OBS na área de trabalho para esta preparação.

# Painel CasterDesk no OBS

Disponível como beta em 0.3.0-beta.0.9: cronômetro e bans de operadores usam o estado e o salvamento do CasterDesk no mesmo PC. Não exige plugin nativo separado. Usa os [Custom Browser Docks do obs-browser](https://github.com/obsproject/obs-browser).

## Configurar

1. Abra CasterDesk e vá à engrenagem → **Painel OBS**.
2. Clique **Ativar conexão**, depois **Copiar endereço do painel**.
3. Feche esse diálogo para liberar os controles.
4. No OBS, em Docks/Painéis → Custom Browser Docks/Painéis de navegador personalizados, adicione CasterDesk e cole o endereço completo.
5. Encaixe o painel onde preferir. Ele é um controle do operador, não uma fonte para a cena.

O endereço contém uma chave: mantenha-o privado. A conexão aceita somente o próprio computador. A preferência de ativação fica salva; Desativar conexão interrompe o acesso. Fechar CasterDesk desconecta o painel.

## Controles

Timer: iniciar/retomar, pausar, adicionar/remover um minuto e definir mm:ss ou hh:mm:ss. Usa o mesmo cronômetro e TXT da aba Textos; fechar/recarregar o painel não reinicia a contagem.

Bans: segue partida/mapa selecionados, equipe responsável, ordem DEF/ATQ e etapas liberadas. Faça veto e lados no programa; escolha os operadores no painel. Partidas confirmadas, etapas futuras, comandos duplicados e seleção desatualizada são protegidos. Edições/diálogos abertos no programa bloqueiam temporariamente os comandos remotos. Alterações do programa reaparecem no painel.

TXT e PNG continuam configurados nas fontes do OBS como antes. Controle de cenas, Stream Deck e conexão entre computadores ficam fora desta primeira entrega.

## Validação e limites

24 verificações HTTP reais cobrem autenticação, origem/host, timer compartilhado, sequência/salvamento de bans, conflitos, bloqueios e reinício. Teste em Edge sem janela cobre controles, validação, requisições e larguras 320/400/700px com API de demonstração. A interface CasterDesk foi testada em Avalonia.Headless.

**OBS e instalação nativa não foram abertos/testados.** O navegador integrado do OBS ainda precisa ser conferido, incluindo encaixe e recarga do painel. Não equivale a compatibilidade nativa já comprovada.

# CasterDesk dentro do OBS

## Plugin opcional — beta.0.10

1. Feche OBS e execute o instalador do CasterDesk.
2. Marque **Instalar o painel CasterDesk no OBS**. O componente OBS pede permissão de administrador; o aplicativo permanece instalado por usuário.
3. Abra CasterDesk e OBS. O plugin registra o painel CasterDesk e descobre a conexão automaticamente. Se estiver oculto, selecione **Painéis → CasterDesk**.

Não é necessário colar endereço. Escolher partida/mapa no CasterDesk atualiza o painel. Fechar CasterDesk desabilita controles; abrir novamente reconecta. Timer e bans continuam usando os mesmos serviços e arquivos do aplicativo. Veto, lados, confirmação e revisão continuam no CasterDesk.

Suporte inicial: OBS Windows x64 31.1+, instalação convencional. O componente fica em ProgramData/obs-studio/plugins/casterdesk-obs. OBS portátil precisa da instalação do plugin no seu próprio diretório. A instalação não altera cenas, perfis ou outros plugins e não encerra OBS/transmissões. Plugin possui entrada própria de desinstalação **CasterDesk — Plugin OBS**.

A conexão é ativada automaticamente uma vez quando a instalação opcional foi concluída. Desativar em engrenagem → Painel OBS prevalece nas aberturas seguintes. Chave privada fica na configuração local; o plugin só acessa 127.0.0.1 e não a registra em logs. Documentos redirecionados ao OneDrive são descobertos pela pasta conhecida do Windows.

## Painel de navegador anterior

A opção manual da beta.0.9 permanece compatível: ativar a conexão, copiar endereço completo, fechar o diálogo e colar em Painéis de navegador personalizados. O endereço contém a chave; não o publique. Fonte de navegador na cena não é o painel de controle.

O dock antigo não é apagado automaticamente. Se instalar o plugin, pode remover manualmente o dock de navegador anterior para evitar duplicação.

## Validação

Testes da API, opt-in do instalador e controles Qt são automatizados em dados isolados. OBS nativo e instalação não foram abertos nesta entrega; carregamento real, encaixe e UAC precisam de validação. Não há controle de cenas, acesso entre computadores ou suporte Stream Deck nesta etapa.

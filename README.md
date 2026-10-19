![CasterDesk — organize a partida, cuide da transmissão](docs/media/banner.svg)

<p align="center">
  <img alt="Versão 1.0.8" src="https://img.shields.io/badge/vers%C3%A3o-1.0.8-327FA8" />
  <img alt="Windows 10 e 11, x64" src="https://img.shields.io/badge/Windows-10%20%2F%2011%20%C2%B7%20x64-233448" />
  <img alt="Português, inglês e espanhol" src="https://img.shields.io/badge/idiomas-PT%20%C2%B7%20EN%20%C2%B7%20ES-233448" />
</p>

<p align="center">
  <a href="https://github.com/moska1m/CasterDesk/releases/latest">Baixar CasterDesk</a> ·
  <a href="#funcionalidades">Funcionalidades</a> ·
  <a href="docs/OBS.md">Usar no OBS</a> ·
  <a href="CHANGELOG.md">Novidades</a> ·
  <a href="https://github.com/moska1m/CasterDesk/issues">Enviar feedback</a>
</p>

**Mapas, bans, rounds, cronômetro e artes para a sua transmissão de Rainbow Six Siege.** O CasterDesk reúne essas ferramentas em um aplicativo para Windows, com um modo dedicado ao spectator e um painel opcional para OBS Studio.

O projeto nasceu da rotina de transmitir campeonatos da comunidade. A proposta é facilitar o registro das informações e a preparação da transmissão, para que o caster possa dedicar mais atenção ao jogo.

## Funcionalidades

| Recurso | O que você pode fazer |
|---|---|
| **Partidas e séries** | Organizar MD1, MD3 e MD5, realizar o veto de mapas e definir lados iniciais e prorrogação. |
| **Modo SPEC** | Escolher mapas diretamente e registrar rounds em uma interface dedicada ao spectator. Corrigir vencedor e bombsite, excluir registros e desfazer ou refazer alterações. |
| **Bans de operadores** | Acompanhar as etapas de banimento, identificar a equipe responsável e registrar escolhas sem banimento. |
| **Round a round** | Registrar bombsite, vencedor e finalização, com histórico e placar atualizado. No SPEC, a finalização é opcional. |
| **Resumo para remake** | Consultar mapa, lados, bans e vencedores dos rounds para reconstruir a partida. |
| **Estatísticas** | Importar prints, revisar os dados dos jogadores e reunir estatísticas por mapa e série. |
| **Gerador de imagens** | Criar anúncios, artes de veto e estatísticas com logos, fundos e cores personalizáveis. |
| **Integração com OBS** | Usar o painel de controle, o cronômetro e arquivos de texto e imagem na transmissão. |
| **Arquivos e backups** | Preservar partidas e recursos, importar e exportar dados e restaurar backups. |
| **Tabela da partida** | Atualizar um arquivo Excel ou uma planilha Google Sheets. Integração experimental e opcional para MD1 e MD3. |
| **Atualizações** | Consultar versões e baixar atualizações pelo programa, com opção de exibir betas. |

O CasterDesk utiliza as informações registradas pelo operador. Não lê a partida automaticamente nem modifica o jogo.

## Conheça a interface

![Tela de partida do CasterDesk](docs/media/interface.png)

<details>
<summary><strong>Modo SPEC</strong></summary>

![Modo SPEC com registro de rounds e histórico](docs/media/spec.png)

</details>

<details>
<summary><strong>Gerador de imagens e estatísticas</strong></summary>

![Gerador de imagens do CasterDesk](docs/media/central.png)

<p align="center"><img src="docs/media/estatisticas.png" width="420" alt="Exemplo de arte de estatísticas com MVP" /></p>

</details>

*Imagens de demonstração; a aparência pode variar conforme a versão e as configurações.*

## Instalação

1. Baixe o instalador para **Windows 10 ou 11, x64**, em [Releases](https://github.com/moska1m/CasterDesk/releases/latest).
2. Feche o CasterDesk e, se for instalar ou atualizar o painel, o OBS Studio.
3. Execute o instalador. A opção **Instalar o painel CasterDesk no OBS** é opcional.
4. Abra o programa, crie uma partida e informe as equipes. Escolha o modo de trabalho nas configurações.

O instalador inclui os componentes necessários para executar o aplicativo. Português, inglês e espanhol estão disponíveis nas configurações.

Depois da instalação, use **Configurações → Verificar atualizações** para consultar novas versões. As partidas e configurações são preservadas nas atualizações. [Saiba mais](docs/ATUALIZACOES.md).

## CasterDesk no OBS

O painel opcional oferece três abas:

- **PARTIDA:** registro de rounds, seleção de mapas da série e bans de operadores.
- **REMAKE:** informações do mapa escolhido para reconstruir a partida.
- **CRONÔMETRO:** controle do tempo e dos textos do timer.

Com o CasterDesk aberto, a conexão é automática. No OBS, acesse **Painéis → CasterDesk** caso o painel esteja oculto. O plugin atende ao **OBS Studio 31.1 ou mais recente, Windows x64**; instalações portáteis exigem configuração manual.

Textos do casting, cronômetro e imagem de veto também podem ser usados como fontes na transmissão. Consulte o [guia do OBS](docs/OBS.md).

## Dados e integrações

As partidas, artes, prints e backups ficam em **Documentos/CasterDesk**. A desinstalação preserva esses dados. Para transferir recursos junto com as partidas, use os pacotes de exportação ou o backup do programa.

A [tabela local em Excel](docs/TABELA-LOCAL.md) acompanha a partida em andamento e preserva cópias por partida e mapa. A [conexão com Google Sheets](docs/GOOGLE-SHEETS.md) é opcional e envia os campos para a planilha escolhida. Nesta fase de testes, o login Google exige uma conta autorizada. O modelo de tabela atual suporta MD1 e MD3.

Consulte a [política de privacidade](PRIVACY.md) para detalhes sobre dados e conexões.

## Feedback e contribuição

Encontrou um problema ou tem uma sugestão? Abra uma [issue](https://github.com/moska1m/CasterDesk/issues) e informe a versão do CasterDesk, o que aconteceu e, quando possível, como reproduzir. Remova dados pessoais de prints e arquivos antes de publicá-los.

Para contribuir com o código, consulte [CONTRIBUTING.md](CONTRIBUTING.md). O histórico de mudanças está no [changelog](CHANGELOG.md), e os detalhes técnicos e verificações estão em [PROGRESSO.md](PROGRESSO.md).

## Licenças e uso

O código próprio do aplicativo está sob [MIT](LICENSE), e o plugin OBS sob [GPL-2.0-or-later](obs-plugin/LICENSE). Marca, imagens, fontes e outros recursos de terceiros mantêm seus próprios direitos e condições; não estão automaticamente cobertos pela licença do código.

O CasterDesk é um projeto independente, sem afiliação ou endosso da Ubisoft. O uso de imagens e marcas deve respeitar as autorizações dos respectivos titulares e do evento ou canal. Consulte os [termos de uso](docs/TERMOS-PT.txt) e os [avisos de terceiros](THIRD-PARTY-NOTICES.md).

O instalador atual ainda não possui assinatura digital Authenticode. A candidatura à SignPath Foundation foi enviada e aguarda análise. Consulte a [política de assinatura](CODE_SIGNING_POLICY.md).

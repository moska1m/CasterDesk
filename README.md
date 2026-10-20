![CasterDesk — organize a partida, cuide da transmissão](docs/media/banner.svg)

<p align="center">
  <strong>Mapas, bans, rounds e resultados para sua transmissão de Rainbow Six Siege.</strong>
</p>

<p align="center">
  <a href="https://github.com/moska1m/CasterDesk/releases/latest"><strong>Baixar para Windows</strong></a> ·
  <a href="docs/OBS.md">Configurar OBS</a> ·
  <a href="CHANGELOG.md">Novidades</a> ·
  <a href="https://github.com/moska1m/CasterDesk/issues">Feedback</a>
</p>

O **CasterDesk** reúne as ferramentas de organização de partidas e transmissão para community casters. Prepare o confronto, acompanhe o jogo pelo programa ou pelo painel OBS e gere as artes com os resultados.

![Tela de partida do CasterDesk](docs/media/interface.png)

## O que você pode fazer

- **Preparar partidas:** MD1, MD3 e MD5, nomes e logos das equipes, veto de mapas, lados e bans de operadores.
- **Operar pelo OBS:** registrar rounds, fazer bans, selecionar mapas e controlar o cronômetro sem trocar de janela.
- **Acompanhar o jogo:** histórico, placar atualizado e resumo de mapas, lados, bans e rounds para remake.
- **Gerar estatísticas e artes:** importar e revisar prints, reunir dados por mapa e série e personalizar anúncios, vetos e resultados.
- **Preservar os registros:** pastas por partida, importação, exportação e backups.

Também inclui **modo SPEC**, voltado ao registro e à correção de rounds, e integração experimental com [Excel](docs/TABELA-LOCAL.md) e [Google Sheets](docs/GOOGLE-SHEETS.md) para MD1 e MD3. O login Google está restrito às contas autorizadas durante os testes.

<details>
<summary><strong>Mais imagens</strong></summary>

![Gerador de imagens](docs/media/central.png)

<p align="center"><img src="docs/media/estatisticas.png" width="420" alt="Exemplo de arte de estatísticas com MVP" /></p>

![Modo SPEC](docs/media/spec.png)

</details>

*Imagens com dados de demonstração; a aparência pode variar conforme a versão.*

## Instalação

1. Baixe o [instalador](https://github.com/moska1m/CasterDesk/releases/latest) para **Windows 10 ou 11, x64**.
2. Feche o CasterDesk e o OBS antes de atualizar. No instalador, o painel OBS é opcional.
3. Abra o programa e crie sua partida. Para usar o painel, acesse **Painéis → CasterDesk** no OBS.

O painel requer **OBS Studio 31.1 ou mais recente, Windows x64**. Com a conexão habilitada e o CasterDesk aberto, ele se conecta automaticamente. [Guia do OBS](docs/OBS.md).

Disponível em **português, inglês e espanhol**. Novas versões podem ser baixadas em **Configurações → Verificar atualizações**, com betas opcionais. [Guia de atualização](docs/ATUALIZACOES.md).

## Arquivos e suporte

Partidas e recursos ficam em **Documentos/CasterDesk** e são preservados na desinstalação. Use exportação ou backup para transferir também as imagens da partida.

Problemas e sugestões: [abra uma issue](https://github.com/moska1m/CasterDesk/issues) informando a versão e o que aconteceu. Remova dados pessoais dos anexos.

[Contribuir](CONTRIBUTING.md) · [Desenvolvimento](PROGRESSO.md) · [Histórico de versões](CHANGELOG.md)

## Uso, privacidade e licenças

O CasterDesk registra as informações fornecidas pelo operador: **não lê partidas automaticamente nem modifica o jogo**. Os dados são armazenados no computador; integrações opcionais utilizam os serviços descritos na [política de privacidade](PRIVACY.md).

O código próprio do aplicativo está sob [MIT](LICENSE), e o plugin OBS sob [GPL-2.0-or-later](obs-plugin/LICENSE). Marca, imagens, fontes e outros recursos de terceiros mantêm seus próprios direitos e condições; não estão automaticamente cobertos pela licença do código.

O projeto é independente, sem afiliação ou endosso da Ubisoft. O uso de imagens e marcas deve respeitar as autorizações dos titulares e do evento ou canal. Consulte os [termos de uso](docs/TERMOS-PT.txt) e os [avisos de terceiros](THIRD-PARTY-NOTICES.md).

O instalador atual ainda não possui assinatura digital Authenticode. A candidatura à SignPath Foundation foi enviada e aguarda análise. A [política de assinatura](CODE_SIGNING_POLICY.md) descreve o processo e as responsabilidades.

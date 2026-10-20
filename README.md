![CasterDesk — organize a partida, cuide da transmissão](docs/media/banner.svg)

<p align="center">
  <strong>Da preparação dos mapas às estatísticas da série.</strong><br />
  Ferramentas para quem transmite Rainbow Six Siege pela comunidade.
</p>

<p align="center">
  <img alt="Versão 1.0.8" src="https://img.shields.io/badge/vers%C3%A3o-1.0.8-327FA8" />
  <img alt="Windows 10 e 11, x64" src="https://img.shields.io/badge/Windows-10%20%2F%2011%20%C2%B7%20x64-233448" />
  <img alt="Português, inglês e espanhol" src="https://img.shields.io/badge/idiomas-PT%20%C2%B7%20EN%20%C2%B7%20ES-233448" />
</p>

<p align="center">
  <a href="https://github.com/moska1m/CasterDesk/releases/latest"><strong>Baixar para Windows</strong></a> ·
  <a href="#funcionalidades">Conhecer os recursos</a> ·
  <a href="docs/OBS.md">Configurar no OBS</a> ·
  <a href="CHANGELOG.md">Notas das versões</a>
</p>

Transmitir uma partida envolve mais do que narrar: é preciso acompanhar o veto, registrar bans e rounds, controlar intervalos e preparar os resultados. **O CasterDesk reúne essas tarefas em um aplicativo**, com informações compartilhadas entre o programa e seu painel para OBS Studio.

O projeto nasceu dessa rotina e evolui com o uso e o feedback de community casters. A proposta é diminuir o trabalho de organizar informações durante a transmissão e manter os registros disponíveis quando chegar a hora de conferir o resultado ou publicar uma arte.

![Tela de partida do CasterDesk, com preparação de mapas, bans e registro de rounds](docs/media/interface.png)

## Funcionalidades

### Prepare a partida

Monte o confronto com nomes e logos das equipes, escolha **MD1, MD3 ou MD5** e organize o veto de mapas. Defina os lados iniciais e a prorrogação de cada mapa antes de acompanhar os rounds.

Os bans de operadores seguem as etapas da partida, com identificação da equipe responsável, indicação das escolhas disponíveis e opção de **sem banimento**. O registro fica associado ao mapa para consulta posterior.

### Acompanhe a transmissão pelo OBS

O **painel opcional para OBS Studio** permite registrar rounds, selecionar o mapa em andamento, fazer bans e controlar o cronômetro sem alternar para a janela do CasterDesk. As ações usam os mesmos dados do aplicativo.

O cronômetro também pode alimentar uma fonte de texto da transmissão. Nomes e informações do casting são disponibilizados em arquivos TXT, e a arte de veto pode usar um caminho fixo de imagem no OBS.

| Aba do painel | Durante a operação |
|---|---|
| **PARTIDA** | Selecione o mapa da série, registre rounds e acompanhe os bans de operadores. |
| **REMAKE** | Escolha o mapa e consulte lados, bans e vencedores dos rounds para reconstruir a partida. |
| **CRONÔMETRO** | Controle o tempo e configure os textos do timer. |

[Veja como instalar e conectar o painel →](docs/OBS.md)

### Registre os rounds e consulte o histórico

Escolha o bombsite, a equipe vencedora e a finalização do round. O histórico reúne os registros e acompanha a evolução do placar, mantendo as informações organizadas por mapa.

Se houver um remake, abra o **Resumo para remake** no programa ou no painel OBS para consultar o que já foi registrado. O resumo serve de guia para configurar o jogo novamente.

### Transforme os resultados em estatísticas e imagens

Importe prints do placar, revise os nomes e os dados dos jogadores e reúna as estatísticas **por mapa e por série**. A conferência permite corrigir a leitura antes de gerar as imagens; o destaque de MVP pode ser escolhido automaticamente ou ajustado manualmente.

No **Gerador de imagens**, prepare anúncios do confronto, artes de veto e estatísticas. Personalize logos, fundos e cores por categoria para adequar as peças à identidade da transmissão.

<p align="center">
  <img src="docs/media/estatisticas.png" width="420" alt="Exemplo de arte de estatísticas gerada pelo CasterDesk, com jogadores e destaque de MVP" />
</p>

<details>
<summary><strong>Ver o Gerador de imagens</strong></summary>

![Gerador de imagens do CasterDesk](docs/media/central.png)

</details>

### Mantenha os arquivos da partida organizados

Cada partida possui sua pasta de dados e recursos. Salve os registros, preserve as artes e os prints e use a importação, a exportação e os backups para recuperar ou transferir o trabalho.

Os arquivos ficam em **Documentos/CasterDesk**. A desinstalação preserva esses dados. Para levar também as imagens e os demais recursos, utilize um pacote de exportação ou um backup, além do JSON da partida.

## Outras formas de trabalhar

<details>
<summary><strong>Modo SPEC — uma interface dedicada ao registro da partida</strong></summary>

Ativado nas configurações, o modo SPEC apresenta os mesmos dados em uma tela voltada ao spectator, com escolha direta de mapas, lados e registro de rounds. A finalização do round é opcional nesse modo.

O histórico permite corrigir vencedor e bombsite, excluir registros com confirmação e desfazer ou refazer alterações. Os placares são recalculados a partir dos registros.

![Modo SPEC com placar, registro de rounds e histórico](docs/media/spec.png)

</details>

<details>
<summary><strong>Tabela da partida — integração experimental com Excel e Google Sheets</strong></summary>

A integração opcional acompanha a partida em andamento e preenche o modelo de tabela a partir dos registros do CasterDesk.

- **Excel local:** mantém um arquivo atualizado e preserva cópias na pasta da partida e de seus mapas.
- **Google Sheets:** envia os campos para a planilha escolhida, mediante autorização da conta Google.

O modelo atual suporta **MD1 e MD3**. Nesta fase de testes, o login Google exige uma conta autorizada no projeto.

[Configurar Excel](docs/TABELA-LOCAL.md) · [Configurar Google Sheets](docs/GOOGLE-SHEETS.md)

</details>

*As imagens desta página usam dados de demonstração. A aparência pode variar conforme a versão e as configurações.*

## Comece a usar

1. Baixe o instalador em [Releases](https://github.com/moska1m/CasterDesk/releases/latest).
2. Feche o CasterDesk e, caso vá instalar ou atualizar o painel, o OBS Studio.
3. Execute o instalador. Marque **Instalar o painel CasterDesk no OBS** se quiser usar essa integração.
4. Abra o CasterDesk, crie uma partida, informe as equipes e configure os mapas e lados.
5. Para trabalhar pelo OBS, abra **Painéis → CasterDesk**. Com o programa aberto e a conexão habilitada, o painel se conecta automaticamente.

| Requisito | Compatibilidade |
|---|---|
| Aplicativo | Windows 10 ou 11, 64 bits |
| Painel OBS, opcional | OBS Studio 31.1 ou mais recente, Windows x64 |
| Idiomas | Português, inglês e espanhol |

O instalador inclui os componentes necessários para executar o aplicativo. O painel atende à instalação convencional do OBS; instalações portáteis exigem [configuração manual](docs/OBS.md).

### Atualizações

Use **Configurações → Verificar atualizações** para consultar as versões disponíveis e baixar pelo programa. Releases são exibidas por padrão; você pode habilitar a exibição de betas. Partidas e configurações são preservadas ao atualizar.

[Como funcionam as atualizações →](docs/ATUALIZACOES.md)

## Feedback da comunidade

Encontrou um problema ou tem uma sugestão para a rotina de transmissão? [Abra uma issue](https://github.com/moska1m/CasterDesk/issues) com a versão do aplicativo, o que aconteceu e os passos para reproduzir. Prints ajudam; retire dados pessoais antes de publicá-los.

Para contribuir com o projeto, consulte o [guia de contribuição](CONTRIBUTING.md). As mudanças de cada versão estão no [changelog](CHANGELOG.md), e as instruções de desenvolvimento e validação estão em [PROGRESSO.md](PROGRESSO.md).

## Uso, privacidade e licenças

O CasterDesk registra as informações fornecidas pelo operador: **não lê partidas automaticamente nem modifica o jogo**. Os dados são armazenados no computador; integrações opcionais utilizam os serviços descritos na [política de privacidade](PRIVACY.md).

O código próprio do aplicativo está sob [MIT](LICENSE), e o plugin OBS sob [GPL-2.0-or-later](obs-plugin/LICENSE). Marca, imagens, fontes e outros recursos de terceiros mantêm seus próprios direitos e condições; não estão automaticamente cobertos pela licença do código.

O projeto é independente, sem afiliação ou endosso da Ubisoft. O uso de imagens e marcas deve respeitar as autorizações dos titulares e do evento ou canal. Consulte os [termos de uso](docs/TERMOS-PT.txt) e os [avisos de terceiros](THIRD-PARTY-NOTICES.md).

O instalador atual ainda não possui assinatura digital Authenticode. A candidatura à SignPath Foundation foi enviada e aguarda análise. A [política de assinatura](CODE_SIGNING_POLICY.md) descreve o processo e as responsabilidades.

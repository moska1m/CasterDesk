# Recursos de terceiros

## Ubisoft / Rainbow Six Siege

Os ícones e retratos de operadores, imagens dos mapas e logos de Siege pertencem aos respectivos titulares. Foram obtidos em páginas e CDNs oficiais da Ubisoft para o protótipo. Origem dos arquivos: `assets/siege/manifest.json` e `assets/branding/ORIGEM.txt`. Os retratos agora são utilizados nos cards de bans.

Não foi identificada uma licença aberta que autorize genericamente a redistribuição desses arquivos em software. Ser gratuito, comunitário ou ter código aberto não transforma esses recursos em MIT.

Fontes consultadas em 24/09/2026:

- Política de vídeos: https://www.ubisoft.com/legal/export/documents/en-INTL-videopolicy-2.0.pdf
- Anúncio de recursos para streamers: https://www.ubisoft.com/en-us/game/rainbow-six/siege/news-updates/2Y0UFDMHHuk6C3E5R1LzhP
- Fan kits: https://www.ubisoft.com/en-us/games/fan-kits

A política de vídeos trata de obras audiovisuais e contém restrição à distribuição separada de assets. O anúncio de recursos para streaming não confirma, por si só, licença para empacotá-los em um aplicativo. Recursos de personagens licenciados de outras franquias podem ter outros titulares. Revisar autorização ou substituir os recursos antes de publicar/distribuir amplamente. A visibilidade privada não concede direitos adicionais.

## Plataformas e logos enviados

Twitch, YouTube e Kick são marcas de seus respectivos titulares; os símbolos exibidos apenas identificam os canais selecionados. Logos de campeonatos enviados pelo usuário permanecem sob responsabilidade de seus titulares. Não acompanham esta distribuição.

## Componentes

A linha Avalonia inclui Avalonia 12.0.0 (MIT, conforme o metadata do pacote), SkiaSharp e HarfBuzzSharp. Avisos em `assets/licenses/AVALONIA-LICENSE.txt`, `SKIASHARP-LICENSE.txt` e `HARFBUZZSHARP-LICENSE.txt`; os dois últimos foram copiados dos pacotes usados nesta publicação. Os avisos do runtime .NET também acompanham o instalador.

O pacote autocontido inclui componentes Microsoft .NET, sujeitos às licenças e avisos distribuídos pelo próprio runtime (`LICENSE.txt` e `THIRD-PARTY-NOTICES.txt`, quando fornecidos). O instalador é produzido com Inno Setup, de Jordan Russell e Martijn Laan, conforme a licença do compilador. Nenhum desses componentes é desenvolvido ou endossado pelo CasterDesk.

As traduções pt-BR e espanhol do instalador são as distribuídas com Inno Setup 7.1.0, com créditos preservados nos arquivos installer/Languages. A licença correspondente está em assets/licenses/INNO-SETUP-LICENSE.txt.

## Tipografia Scout

Arquivos Scout Regular, Bold e Condensed Regular foram utilizados apenas na 0.2.0-beta.0.1. Removidos do código atual e da distribuição a partir da 0.2.0-beta.0.2; permanecem no histórico daquela versão. A licença MIT do código não cobre estas fontes; nenhuma licença aberta ou autorização adicional de redistribuição é declarada pelo projeto.


## Fundo Comunidade — JG

O fundo de exportação Comunidade foi extraído do PSD Promo_CommunityCasters fornecido pelo usuário, que atribui a arte a JG. A autoria da composição permanece com JG; marcas e elementos de terceiros continuam pertencendo aos respectivos titulares. Não está abrangido pela licença MIT do código.

## Artes e tipografia da Central — JG

As bases GameResult nos formatos horizontal/feed/stories, Mapveto_CommunityCasters e os recortes de mapas/ícones foram fornecidos pelo usuário e atribuídos a JG. Composições e recursos de terceiros não são cobertos pela licença MIT do código.

GT America Compressed Bold e Simplon Mono Bold são usados nas imagens exportadas; a interface do programa mantém suas fontes anteriores. Os arquivos foram fornecidos pelo usuário. O projeto não declara licença aberta para essas fontes.

O símbolo neutro foi fornecido pelo usuário como fallback para equipes sem logo. Logos e BGs personalizados permanecem na pasta de dados do usuário e não são incluídos no instalador.

## Plugin nativo OBS

O componente obs-plugin e o suporte de build baseado em obsproject/obs-plugintemplate (3e7d7ac3b5342cd7d9b88890b9c70b472d1520fc) usam GPL-2.0-or-later, separadamente do aplicativo. Licença em obs-plugin/LICENSE; fontes correspondentes em data/source.zip do plugin instalado. Usa libobs, obs-frontend-api e Qt fornecidos pelo OBS; não substitui esses runtimes.

## Uso previsto e condições da versão 1.0

O mantenedor informou que o uso previsto ocorre em ambientes de community casting e SPEC autorizados pelos organizadores/titulares. Essa informação descreve o contexto de uso, não uma licença aberta dos assets ou uma autorização geral para todo campeonato, canal, fonte ou redistribuição separada. O aceite do instalador não concede direitos de terceiros. Código MIT e plugin GPL preservam suas permissões; a marca e a identificação de distribuição oficial permanecem distintas. Termos: docs/TERMOS-PT.txt, TERMOS-EN.txt e TERMOS-ES.txt.

Diretrizes oficiais consultadas em 14/10/2026: https://www.ubisoft.com/en-us/esports/rainbow-six/siege/news-updates/6fKNUaTIgh6rmOlBHPRslf e https://www.ubisoft.com/en-us/esports/rainbow-six/siege/page/5aVBFvcqShI128b0gRvc5L. A autorização de um torneio e a redistribuição dos arquivos de arte/fontes são questões distintas; nenhuma autorização documental específica adicional foi anexada ao repositório nesta entrega.

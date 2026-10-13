# CasterDesk

Ferramentas para community casters de Rainbow Six Siege. **A linha de desenvolvimento atual é Avalonia.** A versão anterior 0.3.0-beta.0.4 permanece preservada. Central de imagens, campeonato, lobby, menu, cronômetro/TXTs e backup/restauração já compilaram e passaram nos testes Headless. Escolha de idioma está no menu; revisão integral das traduções, validação interativa e instalador continuam pendentes. Estado, evidências e limitações em [AVALONIA-PROGRESSO.md](AVALONIA-PROGRESSO.md).

Central em cinco abas, canal do anúncio conforme PSD do JG, logos no cadastro e correção editorial do veto com opção de atualizar também a partida. Entrega, testes e próximas etapas em [PROGRESSO-ESTABILIZACAO.md](PROGRESSO-ESTABILIZACAO.md).

Partidas usam JSON versão 2 com base em inglês e nomes padrão exibidos no idioma local. JSONs antigos continuam importáveis; novos arquivos exigem a versão atualizada em ambos os computadores. Consulte [MATCH-JSON-FORMAT.md](MATCH-JSON-FORMAT.md).

A base estabilizada anterior usa WPF sobre comandos compartilhados. A Central de imagens e conferência de estatísticas estão sendo adaptadas para Avalonia reutilizando esses serviços. `CasterDesk.slnx` e `scripts/Build-Avalonia.ps1` são as entradas da nova linha; `CasterDesk.Legacy.slnx` preserva a base anterior. A documentação WPF é histórica e não comprova validação Avalonia.

Aplicativo independente para Windows, em português, inglês e espanhol. Não é um produto oficial, patrocinado ou aprovado pela Ubisoft. O programa registra as informações inseridas pelo usuário; não controla o jogo nem o OBS.

## Visual da versão 0.2

Tema Siege com Segoe UI nos textos e Bahnschrift SemiCondensed nos títulos, restauradas após o teste com Scout. Cards compactos com retratos nos bans; a janela de seleção mantém ícones. Botões e cabeçalhos compartilham a nova aparência, com destaque vermelho na interação.

A página de partida se ajusta à janela e mantém as ações inferiores visíveis. Em janelas menores, use **Configurar lados dos mapas / Voltar ao confronto** no bloco superior e clique no cabeçalho de cada metade para alternar os bans. Histórico, lista de lados e escolhas rápidas podem rolar dentro dos próprios blocos em janelas pequenas; Confirmar round permanece fixo. Finalizações continuam em texto; novos símbolos foram adiados.

## Funcionalidades

- Partidas MD1, MD3 e MD5: coin flip, veto de nove mapas, lados iniciais e overtime.
- Bans de operadores por etapa, lembretes visuais e correção explícita após confirmar o resultado, sem desbloquear placar ou lados.
- Mapas usados na seleção ficam indisponíveis com imagens preto e branco pré-geradas; picks permanecem coloridos no histórico.
- Round a round com bombsite, vencedor, placar, finalização e cores fixas das equipes. Modo Lista ou Modo rápido por botões, na engrenagem → Registro de rounds; alternar preserva o histórico salvo.
- Modo rápido com registro à esquerda e histórico à direita. Bombsite vencido pela defesa bloqueia os próximos dois rounds regulares; zera na troca de lados e não se aplica ao overtime. Correções sinalizam conflitos posteriores sem apagar o histórico.
- Aviso central após os rounds 03, 06 e 09 para registrar os novos bans pendentes. Lembrar depois mantém o aviso junto aos campos; não bloqueia rounds nem abre etapa futura de mapa encerrado.
- Confirmação de resultado por mapa; MD3 encerrada com duas vitórias confirmadas e MD5 com três.
- Histórico local, salvamento automático, recuperação de backup e importação/exportação da série inteira em JSON com ID único.
- Resumo para remake e guia de configuração de lobby.
- Cronômetro com adição de tempo durante a contagem e saída TXT para OBS.
- Casting: nomes e perfis de narrador, comentarista e dois convidados em oito arquivos TXT.
- Central de imagens: anúncio, map veto, resultado da série, estatísticas por mapa e estatísticas da série, com logos das equipes e fundo personalizável.
- Temas Siege e simplificado, claros/escuros; mudança aplicada ao reabrir.

## Idiomas e arquivos de partida

Na engrenagem, escolha **Idioma → Português (Brasil), English ou Español**. Feche e reabra para aplicar. A tradução está incluída no aplicativo e funciona sem internet. O idioma do instalador pode ser escolhido separadamente.

As traduções incluem interface, avisos, guia do lobby, resumo para remake e PNGs. Os nomes de equipes, operadores, canais, textos personalizados, caminhos TXT e o formato interno dos JSONs são preservados. Inglês mostra BO1/BO3/BO5; o JSON mantém MD1/MD3/MD5 para compatibilidade. A terminologia dos bombsites em inglês/espanhol está sujeita à revisão por casters nativos; os valores originais permanecem armazenados.

Use **Exportar partida** para gerar um nome legível, por exemplo `2026-09-24_18-30-00_Liquid_vs_FaZe_aberta.json`. A data/hora é a de criação da partida; a situação acompanha o idioma escolhido e só indica conclusão após confirmar as vitórias necessárias. O ID permanece dentro do JSON, sem aparecer no nome exportado. Na pasta interna `Partidas`, os arquivos continuam identificados por ID para evitar colisões. Importar o mesmo ID novamente é recusado, mesmo que o arquivo tenha sido renomeado. Na MD3, o JSON inclui a série inteira.

## Instalar

Execute `CasterDesk-0.2.0-beta.0.8-win-x64-Setup.exe` para instalar ou atualizar. Programa e instalador são de 64 bits e incluem o .NET. Configurações e partidas permanecem em Documentos/CasterDesk. Como alternativa, extraia o ZIP portátil em uma pasta e execute `CasterDesk.exe`; não execute diretamente de dentro do ZIP.

Alvo desta beta: Windows 10/11 x64. Ainda é necessário validar em um segundo computador e em diferentes escalas de tela. O instalador beta não tem assinatura digital própria; o Windows pode pedir confirmação de origem. Não desative as proteções do sistema.

Desinstale pelas configurações de aplicativos do Windows. Dados pessoais são preservados para permitir reinstalação.

## Onde ficam os dados

- Programa: `%LOCALAPPDATA%\Programs\CasterDesk`.
- Dados novos: `Documentos\CasterDesk`, respeitando a localização da pasta Documentos no Windows.
- Dados antigos em `%LOCALAPPDATA%\CasterDesk` são copiados na migração; a origem é preservada.
- Os caminhos TXT podem ser personalizados no programa. No OBS, selecione a fonte de texto e habilite a leitura do arquivo correspondente.

A beta não altera a cópia anterior do CronoLive/CasterDesk. Para transferir partidas da prévia, exporte nelas e importe na beta, ou use `scripts/Migrate-Preview.ps1`. Depois confira os caminhos usados pelo OBS. Para backup completo, use Backup e restauração no menu; exportar apenas o JSON não inclui imagens, logos e preferências.

## Limites da beta

PDF, correção administrativa de mapas confirmados e atualização automática não estão implementados. O bloqueio de resultado evita alterações acidentais na interface, não é uma assinatura criptográfica do JSON. Regras e map pool representam o perfil configurado neste projeto, não uma promessa de atualização automática para todos os campeonatos.

## Compilar e testar

Requer Windows, SDK .NET 10 e Inno Setup 6.2 ou superior.

```powershell
dotnet build source/CronoLive.csproj -c Release
./scripts/Test.ps1
./scripts/Build-Release.ps1 -Iscc 'C:\caminho\ISCC.exe'
```

O nome interno `CronoLive` foi mantido em parte do código por compatibilidade histórica. O executável gerado é `CasterDesk.exe`.

`CASTERDESK_DATA_DIR` permite usar uma pasta isolada para testes. Os testes não devem apontar para dados de produção. Consulte [VALIDATION.md](VALIDATION.md) para o que foi realmente verificado e o roteiro do segundo PC.

## Licença e recursos visuais

Código próprio sob MIT. **Imagens e marcas de terceiros não estão cobertas por essa licença.** A autorização para redistribuir os recursos oficiais no instalador ainda precisa ser confirmada para o compartilhamento do pacote, inclusive interno. O projeto permanece privado, destinado ao uso interno dos community casters. Consulte [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Sobre e assinatura

Na engrenagem, escolha **Sobre** para consultar autoria, versão, créditos e condições de uso. Criado por Jeferson “Moska” Sampaio. Consulte SIGNING.md para o processo de assinatura; esta beta ainda não é assinada.



## Pastas e backup (beta.0.4)

Novas instalações usam Documentos/CasterDesk, respeitando a localização da pasta no Windows. Configuracoes guarda preferências; Textos guarda saídas novas; Imagens é o destino sugerido dos PNGs; Partidas guarda o histórico e Exportadas guarda os JSONs exportados; Backups guarda ZIPs e arquivos de recuperação.

Configurações antigas de TXT permanecem apontando para a pasta anterior, para preservar fontes do OBS. Esses arquivos externos não entram no backup. Para centralizá-los, escolha Textos nos campos de saída e atualize as fontes correspondentes do OBS.

Engrenagem → Backup e restauração permite criar um ZIP ou restaurá-lo. A restauração confere integridade, mostra conflitos e permite manter todos os arquivos atuais ou substituí-los pelas versões do backup. Partidas com o mesmo ID conservam o mesmo arquivo. O programa reinicia para aplicar os dados. Antes disso, cria um backup dos dados atuais. Guarde uma cópia externa ao PC. A desinstalação preserva todos esses dados.

## Central de imagens e estatísticas (beta.0.6)

Escolha a partida na Central de imagens. Anúncios permitem campeonato e logos de cada equipe, seguindo a composição do PSD original. Data, horário e canal não aparecem nesta etapa. Sem logo, utiliza-se o símbolo neutro fornecido. Nesta etapa não há endereço de canal nem Twitch nas artes.

Anúncio, resultado e estatísticas oferecem horizontal (1920×1080), feed (1080×1350) e stories (1080×1920). Map veto usa 1920×1080 e exige veto concluído e lados iniciais definidos. Mapas não jogados aparecem em preto e branco, sem texto de ausência nem placar.

Use a opção de fundo para carregar um PNG/JPEG do campeonato; o programa guarda uma cópia otimizada junto à partida. A imagem é recortada para preencher o formato, por isso confira a prévia. Restaurar o fundo padrão volta à arte de JG.

Ao gerar map veto, o arquivo Documentos/CasterDesk/OBS/map-veto.png é substituído pela seleção atual, sempre com transparência. Configure uma fonte de imagem do OBS para esse caminho uma única vez. A pasta da partida recebe map-veto.png com fundo e map-veto-obs.png transparente.

Cada partida tem uma pasta estável em Partidas, identificada pela data e ID. Ela contém partida.json, arte.json, Imagens, Estatisticas e Recursos. O JSON interno por ID também é preservado para compatibilidade. Renomear equipes não muda o destino dos arquivos. Abrir pasta da partida dá acesso às artes geradas.

Após confirmar cada mapa, pode-se registrar estatísticas. Abra um print ou cole uma imagem com Ctrl+V. A leitura usa o reconhecimento local do Windows, sem enviar o print a um serviço externo. Se indisponível, preencha manualmente. Compare a imagem e os campos E/M/A lado a lado, corrija nomes/números e a equipe do topo e confirme. Sem print, marque ausência; remake e informações incompletas podem ser registrados como parciais.

As estatísticas gerais somam jogadores por equipe e nome: mantenha o mesmo apelido entre mapas. Campos desconhecidos não são convertidos em zero; jogadores com números incompletos ficam fora da arte até completar os dados. Cobertura incompleta é indicada na exportação. MVP é uma seleção manual entre jogadores com estatísticas confirmadas.

O backup ZIP completo inclui pastas de partidas, artes, logos, BGs e prints que estiverem dentro da pasta de dados. Arquivos TXT externos continuam fora do backup.

## Otimização de imagens (beta.0.8)

Os recursos mantêm as mesmas dimensões e pixels. PNGs opacos dispensam o canal alfa; mapas em preto e branco utilizam um único canal. Recursos da Central que são exatamente iguais compartilham um arquivo, com aliases para conservar as referências de idioma e formato.

As novas exportações PNG usam compactação sem perda, sem redução de cores ou resolução. O arquivo transparente do OBS continua no mesmo caminho. A gravação processa as linhas sem criar uma cópia completa adicional do bitmap; o trabalho de exportação continua em segundo plano. A compressão aumenta um pouco o tempo de gravação, em troca de arquivos menores. Arquivos já salvos nas pastas das partidas não são alterados.

Para preparar novos recursos do projeto, scripts/Optimize-Assets.py requer Python, Pillow e NumPy. O script compara os pixels e guarda os originais em work/image-optimization-originals, fora do pacote distribuído. Os PSDs fornecidos permanecem intactos.

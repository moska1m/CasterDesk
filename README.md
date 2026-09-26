# CasterDesk

Ferramentas para community casters de Rainbow Six Siege. **0.2.0-beta.0.1 — beta privada para validação.**

Aplicativo independente para Windows, em português, inglês e espanhol. Não é um produto oficial, patrocinado ou aprovado pela Ubisoft. O programa registra as informações inseridas pelo usuário; não controla o jogo nem o OBS.

## Visual da versão 0.2

Tema Siege com tipografia Scout carregada pelo aplicativo, sem instalação global. Cards compactos com retratos nos bans; a janela de seleção mantém ícones. Botões e cabeçalhos compartilham a nova aparência, com destaque vermelho na interação.

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
- PNG: Stories (1080×1920), feed/carrossel (1080×1350) e horizontal (1920×1080), com identificação do caster e logotipo do campeonato.
- Temas Siege e simplificado, claros/escuros; mudança aplicada ao reabrir.

## Idiomas e arquivos de partida

Na engrenagem, escolha **Idioma → Português (Brasil), English ou Español**. Feche e reabra para aplicar. A tradução está incluída no aplicativo e funciona sem internet. O idioma do instalador pode ser escolhido separadamente.

As traduções incluem interface, avisos, guia do lobby, resumo para remake e PNGs. Os nomes de equipes, operadores, canais, textos personalizados, caminhos TXT e o formato interno dos JSONs são preservados. Inglês mostra BO1/BO3/BO5; o JSON mantém MD1/MD3/MD5 para compatibilidade. A terminologia dos bombsites em inglês/espanhol está sujeita à revisão por casters nativos; os valores originais permanecem armazenados.

Use **Exportar partida** para gerar um nome legível, por exemplo `2026-09-24_18-30-00_Liquid_vs_FaZe_aberta.json`. A data/hora é a de criação da partida; a situação acompanha o idioma escolhido e só indica conclusão após confirmar as vitórias necessárias. O ID permanece dentro do JSON, sem aparecer no nome exportado. Na pasta interna `Partidas`, os arquivos continuam identificados por ID para evitar colisões. Importar o mesmo ID novamente é recusado, mesmo que o arquivo tenha sido renomeado. Na MD3, o JSON inclui a série inteira.

## Instalar

Baixe `CasterDesk-0.2.0-beta.0.1-win-x64-Setup.exe` na release privada, execute e escolha se deseja atalho na área de trabalho. O Menu Iniciar recebe um atalho. O instalador inclui o .NET e não requer instalação separada do runtime.

Alvo desta beta: Windows 10/11 x64. Ainda é necessário validar em um segundo computador e em diferentes escalas de tela. O instalador beta não tem assinatura digital própria; o Windows pode pedir confirmação de origem. Não desative as proteções do sistema.

Desinstale pelas configurações de aplicativos do Windows. Dados pessoais são preservados para permitir reinstalação.

## Onde ficam os dados

- Programa: `%LOCALAPPDATA%\Programs\CasterDesk`.
- Dados: `%LOCALAPPDATA%\CasterDesk` (partidas, configurações, logos enviados, logs e saídas TXT padrão).
- Os caminhos TXT podem ser personalizados no programa. No OBS, selecione a fonte de texto e habilite a leitura do arquivo correspondente.

A beta não altera a cópia anterior do CronoLive/CasterDesk. Para transferir partidas da prévia, exporte nelas e importe na beta, ou use `scripts/Migrate-Preview.ps1`. Depois confira os caminhos usados pelo OBS. Para backup completo, feche o aplicativo e copie a pasta de dados; o JSON da partida não inclui os logos nem todas as preferências.

## Limites da beta

MD5, PDF, correção administrativa de mapas confirmados e atualização automática não estão implementados. O bloqueio de resultado evita alterações acidentais na interface, não é uma assinatura criptográfica do JSON. Regras e map pool representam o perfil configurado neste projeto, não uma promessa de atualização automática para todos os campeonatos.

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

Na engrenagem, escolha **Sobre o CasterDesk** para consultar autoria, versão, créditos e condições de uso. Criado por Jeferson “Moska” Sampaio. Consulte SIGNING.md para o processo de assinatura; esta beta ainda não é assinada.

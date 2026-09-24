# CasterDesk

Ferramentas para community casters de Rainbow Six Siege. **0.1.0-beta.1 — beta privada para validação.**

Aplicativo independente para Windows, em português. Não é um produto oficial, patrocinado ou aprovado pela Ubisoft. O programa registra as informações inseridas pelo usuário; não controla o jogo nem o OBS.

## Funcionalidades

- Partidas MD1 e MD3: coin flip, veto de nove mapas, lados iniciais e overtime.
- Bans de operadores por etapa, alertas de registro pendente e ícones visuais.
- Round a round com bombsite, vencedor, placar e cores fixas das equipes.
- Confirmação de resultado por mapa; MD3 encerrada com duas vitórias confirmadas.
- Histórico local, salvamento automático, recuperação de backup e importação/exportação da série inteira em JSON com ID único.
- Resumo para remake e guia de configuração de lobby.
- Cronômetro com adição de tempo durante a contagem e saída TXT para OBS.
- Casting: nomes e perfis de narrador, comentarista e dois convidados em oito arquivos TXT.
- PNG: Stories (1080×1920), feed/carrossel (1080×1350) e horizontal (1920×1080), com identificação do caster e logotipo do campeonato.
- Temas Siege e simplificado, claros/escuros; mudança aplicada ao reabrir.

## Instalar

Baixe `CasterDesk-0.1.0-beta.1-win-x64-Setup.exe` na release privada, execute e escolha se deseja atalho na área de trabalho. O Menu Iniciar recebe um atalho. O instalador inclui o .NET e não requer instalação separada do runtime.

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

Código próprio sob MIT. **Imagens e marcas de terceiros não estão cobertas por essa licença.** A autorização para redistribuir os recursos oficiais no instalador ainda precisa ser confirmada antes de uma distribuição pública. Esta release permanece privada. Consulte [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

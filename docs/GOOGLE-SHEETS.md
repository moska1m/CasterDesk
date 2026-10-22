# Google Sheets — conexão experimental

Disponível em **Tabela da partida → Google Sheets**, desde a versão 1.0.8-beta.2. A conexão é opcional e vem desligada. O Excel local continua disponível separadamente.

## Uso na edição com login integrado (beta.3)

O instalador preparado com a identidade OAuth do CasterDesk já configura o login. Cada usuário clica em **Conectar com Google**, escolhe sua própria conta e autoriza. Depois cola o link da planilha, usa **Carregar abas** e escolhe a aba de destino. Por fim, marca a sincronização e usa **Verificar e aplicar**. O destino é salvo para as próximas aberturas. Carregar ou selecionar uma aba não altera células nem aplica a sincronização.

O usuário não precisa criar um projeto Google nem importar JSON. Enquanto o projeto estiver em testes, o responsável pelo projeto precisa adicionar previamente o e-mail desse usuário à lista autorizada. A conta também precisa ter acesso de edição à planilha selecionada.

O instalador contém somente a identidade de cliente OAuth para aplicativo de computador, que o Google trata como cliente público. Não contém tokens, senha, sessão ou autorização de qualquer usuário. **Trocar conta Google** pausa a sincronização até a nova conta ser autorizada e a configuração reaplicada. A importação de um cliente próprio continua disponível em **Opções avançadas**.

## Configuração inicial do projeto Google

1. Crie um projeto no Google Cloud e ative a **Google Sheets API**.
2. Configure o Google Auth Platform com o nome CasterDesk, um contato de suporte e público externo em modo de testes.
3. Adicione as contas que vão testar em **Público-alvo → Usuários de teste**.
4. Crie um cliente OAuth do tipo **App para computador**. Baixe seu arquivo JSON.
5. Em uma instalação sem login integrado, importe esse JSON em **Opções avançadas** e clique em **Conectar com Google**. A autorização abre no navegador padrão do Windows, sem preferência por Chrome.
6. Cole o link da planilha de destino. Use **Carregar abas** para escolher a aba, ou inclua o `gid` dela no link. A conta precisa poder editar esse arquivo. Para o primeiro teste, use uma cópia do modelo.
7. Marque **Sincronizar automaticamente** e clique em **Verificar e aplicar**.

Não é necessário configurar Cloudflare, abrir portas do roteador ou usar uma conta de serviço. A autorização usa um retorno temporário em `127.0.0.1`, limitado ao próprio PC.

Em projetos externos que permanecem em testes, o Google pode expirar o refresh token após sete dias. Se a autorização expirar, clique em Conectar conta novamente. Para disponibilizar a integração ao público, será necessário preparar o projeto e cumprir os requisitos de verificação do Google; esta configuração inicial é para os usuários de teste cadastrados.

## O que é atualizado

O destino deve manter os cabeçalhos e posições do modelo original, mas a aba pode ter outro nome, inclusive **Round Breakdown Series Layout** sem o sinal `+`. O programa verifica o identificador da aba, as dimensões mínimas e os cabeçalhos antes de escrever. O nome não substitui a validação da estrutura.

O destino é salvo com o identificador numérico da aba (`gid`). Se ela for renomeada depois, o programa consulta o nome atual antes de escrever e continua usando a mesma aba. Se ela for excluída, o programa recusa a atualização em vez de trocar para outra aba. Para links sem `gid`, a aba com o título original é reconhecida automaticamente; uma planilha com somente uma aba também pode ser usada. Nos outros casos, é necessário escolher a aba explicitamente.

Usa a mesma projeção do Excel local: nomes das equipes, placar confirmado da série, mapas, status dos mapas, resultados, atacante inicial/overtime e os quinze registros de vencedor e finalização. As finalizações de desativador são exportadas como `Desativador`. O modelo continua limitado a MD1 e MD3.

Uma atualização em lote usa `RAW`, preservando nomes e placares como texto. O programa escreve apenas A2:B2, E2:I2, B4:B5, G4:I4, G8:I8 e B8:C22. As células vermelhas, fórmulas, imagens, outras abas e formatação não são alteradas.

O mapa em andamento segue a mesma regra local: primeiro mapa ainda não confirmado; ao encerrar a série, último mapa confirmado. Consultar outro mapa no aplicativo não troca a projeção em andamento.

## Internet, cópias e autorização

As mudanças locais são arquivadas por partida e mapa independentemente do envio pela internet. A sincronização remota envia o estado mais recente, reúne alterações próximas e tenta novamente após falhas com espera progressiva. Não reproduz todos os estados intermediários no Google depois de ficar offline. Ao reabrir, envia o estado atual da partida carregada se a opção estiver habilitada.

A autorização do usuário é protegida com Windows DPAPI, vinculada ao usuário do Windows. Fica em LocalAppData/CasterDesk/Auth, fora dos backups e dos arquivos das partidas. O JSON importado originalmente continua onde o usuário o salvou. O empacotamento integrado inclui apenas os campos de identidade do cliente de computador; nunca copia tokens ou sessões. A configuração de empacotamento fica fora do controle de versão.

**Desconectar conta** desliga a sincronização e revoga a autorização no Google. O login usa PKCE/S256 e valida um estado aleatório no retorno. A permissão solicitada pelo Google permite acesso a planilhas; o aplicativo restringe suas operações ao arquivo e à aba configurados.

## Verificação realizada

Testes isolados cobrem o lote RAW, igualdade com o mapeamento do Excel, seleção por identificador, abas renomeadas, nomes com caracteres especiais, listagem sem escrita e recusa de destinos incompatíveis ou excluídos. Também verificam retorno de login com estado inválido, PKCE, proteção da autorização, renovação e revogação. Esses testes usam respostas simuladas, sem contatar uma conta Google. O teste real exige login do usuário e uma planilha de destino com acesso de edição.

Referências oficiais: [OAuth para aplicativos de computador](https://developers.google.com/identity/protocols/oauth2/native-app), [atualização de valores em lote](https://developers.google.com/workspace/sheets/api/reference/rest/v4/spreadsheets.values/batchUpdate), [expiração de autorizações](https://developers.google.com/identity/protocols/oauth2#expiration).

# Atualizações do CasterDesk

O verificador usa arquivos públicos do GitHub Releases. Não exige servidor pago, login GitHub ou cobrança recorrente. O cliente não consulta a API REST a cada verificação: lê o catálogo publicado em `releases/download/update-feed/versions.json`.

## No programa

Abra **Configurações → Verificar atualizações**. A lista mostra versão, data e tipo. Por padrão, só releases aparecem; **Mostrar betas** inclui as versões de teste e permite receber seus avisos. A escolha é salva para as próximas sessões. Versões anteriores podem ter suas notas consultadas; a interface não faz downgrade.

A consulta automática acontece ao abrir e, durante a sessão, a cada seis horas. Se a rede falhar, o aplicativo continua funcionando. Atualizações importantes recebem um aviso destacado. Baixar não instala nem interrompe uma transmissão. A instalação começa somente ao clicar em **Instalar e fechar o CasterDesk**, após salvar os dados. O instalador normal é aberto; o painel OBS não é reinstalado automaticamente.

Instaladores incompletos, adulterados ou anúncios com assinatura desconhecida são recusados. O catálogo é assinado com RSA-PSS/SHA-256; o instalador precisa corresponder ao tamanho e SHA-256 assinados. Isso protege a distribuição e não equivale a uma assinatura Authenticode do executável. Os avisos normais do Windows podem continuar aparecendo.

A primeira instalação desta versão ainda precisa ser feita manualmente: versões antigas não possuem o verificador.

## Publicar uma versão

1. Atualize `Directory.Build.props` e escreva as notas em `docs/releases/`. Use `1.0.9` para release ou `1.0.9-beta.1` para beta.
2. Empacote com `scripts/Package-Desktop.ps1`, informando o compilador Inno Setup, SDK .NET, pacote OBS e, quando aplicável, a identidade OAuth de computador. Use `-UpdatePriority important` para uma correção importante e `-UpdateNotesFile` para incluir as notas.
3. O processo executa os testes, prepara o instalador e o manifesto `dist/update-VERSAO.json`, assinado com a chave local existente.
4. Revise, faça commit e envie os fontes. Execute `scripts/Publish-Update.ps1 -Installer CAMINHO -Target SHA_COMPLETO -NotesFile CAMINHO` para conferir os parâmetros; acrescente `-Publish` para publicar. `-InitializeFeed` só é usado uma vez, na criação da hospedagem.
5. O script prepara a nova lista a partir da anterior, publica o instalador e só então atualiza o catálogo. Uma beta recebe a marca de pré-lançamento no GitHub e não substitui o último release estável.

A chave privada fica fora do repositório e dos backups portáveis, protegida pelo Windows para a conta que publica. **Não gere uma chave nova para cada atualização.** Guarde uma cópia protegida e planeje a transferência para outro computador antes de trocar o ambiente; o aplicativo instalado confia na chave pública original. Nunca envie a chave privada, tokens Google ou credenciais GitHub ao repositório.

O catálogo inclui apenas versões publicadas com manifesto assinado. Releases históricos sem esse manifesto continuam disponíveis na página do GitHub, sem aparecer automaticamente nessa lista.

Referências: [GitHub Releases](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases), [opções de assinatura no Windows](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options).

# Preparação da candidatura SignPath

## Situação

- Repositório público, licença do código próprio MIT, plugin GPL e releases existentes.
- Responsável: moska1m; MFA do GitHub confirmado pelo mantenedor.
- Política de assinatura e privacidade disponíveis no repositório.
- Compilação manual em runner Windows hospedado, sem chave privada do atualizador nem credenciais pessoais.
- **Pendente:** envio da candidatura, avaliação dos recursos de terceiros e aprovação da SignPath. Nenhum certificado foi concedido.

## Compilação verificável

Execute **Actions → Candidato Windows para assinatura → Run workflow → main**. O resultado fica em `casterdesk-signing-candidate`, acompanhado da origem e dos hashes. Os logs ficam num artefato separado.

O fluxo não faz upload de tokens, JSON de conta Google nem chaves de publicação. O candidato inicial não inclui a identidade OAuth nativa embutida; mantém a opção avançada de configuração. Não usa credenciais Google pessoais para testes. Não publica uma release nem gera anúncio válido para o atualizador.

Os fontes do plugin são arquivados pelo Git, excluindo caches, dependências baixadas e arquivos de build. Mudanças locais não commitadas nesse componente impedem o empacotamento, evitando anunciar fontes diferentes do commit verificável.

## Informações para a candidatura

- Projeto: **CasterDesk**.
- Repositório: https://github.com/moska1m/CasterDesk
- Downloads existentes: https://github.com/moska1m/CasterDesk/releases
- Política: https://github.com/moska1m/CasterDesk/blob/main/CODE_SIGNING_POLICY.md
- Privacidade: https://github.com/moska1m/CasterDesk/blob/main/PRIVACY.md
- Responsável/conta GitHub: moska1m.
- Email de contato: o mantenedor deve informar sua preferência no formulário; não é necessário publicar esse endereço nos fontes.

Descrição sugerida:

> CasterDesk é um aplicativo gratuito para Windows voltado a community casters e spectators de Rainbow Six Siege. Reúne registro de rounds, veto e bans de operadores, cronômetro, geração de imagens e um painel opcional para OBS. O código próprio é MIT e o plugin OBS é GPL-2.0-or-later. O aplicativo não controla o jogo nem explora mecanismos de segurança.

Declaração necessária sobre os recursos:

> Além do código aberto, a distribuição atual inclui imagens, artes e fontes de terceiros, documentadas em THIRD-PARTY-NOTICES.md. Não declaramos que esses materiais tenham licença MIT ou outra licença aberta. Solicitamos que a SignPath avalie explicitamente se essa composição é elegível para o programa gratuito e informe qualquer adequação necessária.

## Depois da aprovação

Instale a integração SignPath no repositório indicado, configure o projeto e as definições de artefatos com as identidades efetivamente fornecidas. O token de envio deverá ficar em GitHub Secrets, com permissão mínima de solicitante; a chave privada do certificado fica no serviço.

A integração de assinatura ativa será adicionada com os IDs e políticas reais aprovados, incluindo a assinatura interna dos binários próprios antes de empacotar e a assinatura final do instalador. Não adicionar IDs inventados nem declarar uma assinatura válida antes da verificação.

A assinatura Authenticode altera os bytes: somente depois dela devem ser calculados tamanho/SHA-256 e emitido o manifesto RSA do atualizador. A chave privada existente desse manifesto permanece protegida no computador do mantenedor nesta etapa. Não envie seu arquivo DPAPI ao runner.

Referências: [candidatura](https://signpath.org/apply), [condições](https://signpath.org/terms), [integração GitHub](https://docs.signpath.io/trusted-build-systems/github).

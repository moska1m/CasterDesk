# Code signing policy — CasterDesk

**Estado: preparação da candidatura à SignPath Foundation.** Nenhuma aprovação ou assinatura Authenticode é declarada neste momento. Os instaladores 1.0.8 e anteriores não foram assinados por esse serviço.

## Responsáveis

- Desenvolvimento e revisão: [moska1m](https://github.com/moska1m).
- Aprovação de solicitações de assinatura: [moska1m](https://github.com/moska1m).
- Contribuições externas e mudanças na compilação precisam de revisão do mantenedor antes de entrar na versão que será assinada.
- O mantenedor confirmou autenticação de dois fatores no GitHub. MFA também deverá estar ativa na conta SignPath antes de utilizá-la.

## Processo previsto

Os candidatos são compilados dos fontes deste repositório em runners Windows hospedados pelo GitHub. O fluxo manual [Candidato Windows para assinatura](.github/workflows/signing-candidate.yml) testa o núcleo, a interface e o painel OBS e registra commit, execução e hashes. Ele não publica releases nem acessa a chave privada do atualizador.

Após aprovação e configuração da SignPath, cada solicitação de assinatura deverá receber aprovação manual do responsável. Apenas os binários próprios do projeto serão assinados com a identidade atribuída ao CasterDesk; dependências de terceiros manterão suas identidades e licenças.

A sequência da publicação assinada será: compilar e testar → assinar binários próprios → empacotar → assinar o instalador → conferir assinatura → calcular hashes e assinar o catálogo do atualizador → publicar. O instalador já anunciado nunca será substituído por outro arquivo com bytes diferentes na mesma versão.

A assinatura RSA do catálogo do atualizador e a assinatura Authenticode do Windows são mecanismos separados. A existência do primeiro não significa que o executável tenha o segundo.

## Transparência dos recursos

O código próprio é MIT; o plugin OBS é GPL-2.0-or-later. Imagens, artes e fontes descritas em [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) não são declaradas como cobertas por essas licenças. A candidatura deverá informar esses recursos e solicitar à SignPath uma avaliação expressa da elegibilidade do pacote. Não há declaração de que todos os arquivos sejam open source.

## Privacidade

Consulte [PRIVACY.md](PRIVACY.md), incluindo conexão local OBS, Google Sheets opcional e consulta de atualizações no GitHub.

## Atribuição do serviço

Se a candidatura for aprovada e as assinaturas forem efetivamente emitidas, a distribuição passará a exibir a atribuição exigida: “Free code signing provided by [SignPath.io](https://signpath.io), certificate by [SignPath Foundation](https://signpath.org)”. Essa frase descreve a etapa futura; não representa patrocínio já concedido.

Regras de referência: [SignPath Foundation](https://signpath.org/terms).

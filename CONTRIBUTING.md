# Contribuir com o CasterDesk

## Publicação de releases

- Use somente o número da versão no título, por exemplo `1.0.9` ou `1.1.0-beta.1`, sem o nome do aplicativo ou subtítulo.
- Escreva as notas em português do Brasil, com tópicos curtos sobre funcionalidades, melhorias e correções relevantes para quem usa o programa.
- Inclua instruções de atualização, compatibilidade ou limitações somente quando necessárias para aquela versão.
- Não inclua referências pessoais, conversas de desenvolvimento, decisões abandonadas, detalhes de ferramentas ou relatórios de testes nas notas. Mantenha informações técnicas na documentação apropriada.
- Preserve avisos legais ou de segurança necessários. Diferencie a assinatura do catálogo de atualizações da assinatura digital do instalador.
- Não altere instaladores, anexos, tags ou a classificação beta/estável durante uma revisão apenas editorial.
- Arquivos históricos devem usar o intervalo de versões no título e explicar no texto os limites dos pacotes arquivados. O catálogo técnico do atualizador mantém o título `update-feed`.

## Como contribuir

Feedback de quem transmite é parte do processo de criação. Para relatar um problema, informe a versão, resolução/escala do Windows, passos para reproduzir e o que esperava acontecer. Prints ajudam; remova caminhos pessoais e informações que não deseja compartilhar.

Para traduções, indique idioma, tela e sugestão de termo. Não altere IDs de mapas, bombsites ou equipes para traduzir textos: o formato interno precisa continuar compatível entre idiomas. Nomes personalizados não devem ser traduzidos.

Para mudanças de código, abra uma proposta antes de reformular fluxos ou composições. Preserve comandos compartilhados, salvamento recuperável e confirmação de estatísticas. Rode os testes apropriados em dados isolados; não aponte testes para Documentos/CasterDesk de uso real.

As artes seguem as bases e proporções aprovadas. A interface usa Segoe UI; Rajdhani pertence à marca. Novos recursos precisam ter origem e licença documentadas. A licença MIT do código não autoriza redistribuir assets de terceiros.

Os scripts de teste não devem abrir janelas nativas, instaladores ou OBS automaticamente. Não inclua tokens, arquivos de configuração pessoais, partidas reais, pastas de publicação ou executáveis em commits de código. Instaladores são anexos de Releases, com SHA-256.

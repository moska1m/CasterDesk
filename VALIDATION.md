# Validação da beta 0.1.0-beta.1

## Executado no PC de desenvolvimento

Windows 10 x64; SDK .NET 10.0.303; runtime incluído 10.0.11.

- Publicação autocontida win-x64: compilação aprovada.
- Cronômetro e casting: 15 verificações aprovadas.
- Campeonato: 406.530 verificações aprovadas, incluindo todas as sequências de placar até 15 rounds, lados, bans, persistência JSON e edição na interface nativa.
- Exportação PNG: 44 verificações aprovadas, incluindo formatos, partidas parciais, limites de nomes, preservação de dados e mudança rápida de prévia.
- Tema após reabertura e navegação nos quatro temas: aprovados.
- Cronômetro com encerramento e arquivo TXT: aprovado.
- Microteste de 20 alterações de bombsite: 5 ms, 789 KB de alocação gerenciada, zero recálculos de layout e 299/299 controles preservados. Isso não representa uma medição de consumo geral de CPU/memória.
- Cada teste usa uma pasta de dados própria, fora dos arquivos instalados. Nenhum dado do usuário foi incluído no pacote.
- Migração da prévia: cópia de partidas/configurações, preservação do caminho TXT do OBS, realocação dos logos e recusa de sobrescrever destino existente verificadas com dados fictícios.
- Instalador compilado e extraído para auditoria: conteúdo sem partidas, preferências pessoais ou símbolos de depuração; DLL idêntica à publicação testada. O executável extraído passou novamente nas 406.530 verificações de campeonato usando seu runtime incluído.

## Verificação necessária antes da versão estável

O ambiente de automação impede alterações no registro do Windows. Portanto, não foi validado aqui o ciclo real de instalar/atualizar/desinstalar com registro e atalhos. O script do instalador cria atalho no Menu Iniciar, oferece atalho na área de trabalho e preserva os dados porque estes ficam fora da pasta instalada. Esse comportamento ainda precisa de verificação interativa.

### Roteiro no segundo PC

1. Usar Windows 10/11 x64 sem SDK .NET; instalar com usuário comum.
2. Selecionar atalho da área de trabalho e conferir também o Menu Iniciar.
3. Abrir, criar uma MD1 e uma MD3; testar vetos, lados, bans, placar e confirmação de resultado.
4. Fechar e reabrir; confirmar que os dados permanecem.
5. Exportar/importar JSON em outra instalação e verificar rejeição de ID duplicado.
6. Gerar PNG nos três formatos, com e sem logo, com 7 e 15 rounds.
7. Ligar as fontes TXT no OBS; testar cronômetro, acréscimos e casting.
8. Conferir navegação e legibilidade nas escalas 100%, 125% e 150%; registrar limitações em telas menores.
9. Com backup dos dados, reinstalar sobre a beta e confirmar preservação das partidas e configurações.
10. Desinstalar; confirmar remoção dos atalhos e presença dos dados em `%LOCALAPPDATA%\CasterDesk`. Reinstalar e conferir recuperação.

Registrar versão do Windows, escala/resolução, resultado de cada passo e, se houver falha, os logs da pasta de dados. Compartilhar logs apenas após revisar nomes e caminhos pessoais.

## Antes de tornar público

- Concluir o teste em outro PC e corrigir falhas encontradas.
- Resolver a permissão de redistribuição dos recursos visuais de terceiros ou substituí-los.
- Revisar créditos, regras/map pool, documentação e assinatura do instalador.

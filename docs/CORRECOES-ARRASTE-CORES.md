# Correções preparadas — arraste e cores de operadores

Solicitação de 20/10/2026. Correções locais no código, sem gerar instalador, publicar no GitHub ou substituir a instalação usada na transmissão.

## Causas e ajustes

- O início do arraste estava ligado somente ao TextBlock do título, com uma faixa vertical pequena. O tratamento agora cobre toda a barra de título, inclusive o espaço vazio e o ícone. Os botões de minimizar/maximizar/fechar ficam excluídos dessa área de arraste.
- As cores genéricas dos botões eram bindings locais a recursos de tema. Ao anexar os controles à janela ou mudar o tema, esses bindings sobrescreviam cores específicas de equipe. O teste reproduziu um ban azul `#96408fb3` passando a `#ff354c65`.
- As cores padrão de botões e textos agora são definidas por estilos. Cores específicas de equipe, estados e avisos continuam como valores locais, com prioridade maior. Os recursos de tema continuam atualizando os controles comuns.

## Validação

- Build Release sem avisos ou erros.
- 171 verificações específicas passaram: cores dos 12 bans, escolhas de vencedor, seleção de operadores/sem ban, cores no SPEC e alternância Padrão → Claro → Escuro → Padrão.
- Cliques duplos simulados no espaço vazio, nas faixas superior/inferior da barra e no ícone atingem o tratamento de maximização.
- Suíte completa de interface passou em diretório isolado: `CasterDesk-window-color-regression-ba979ad3ad6a4b9eb97dc72d4993c026` dentro da pasta temporária do Windows.
- Capturas foram revisadas. Os testes usam Avalonia.Headless: o arraste nativo, encaixe do Windows e movimento entre monitores ainda precisam de verificação no Windows real na próxima atualização.

A versão e o instalador beta.4 anteriores permanecem intactos. Empacotar somente depois que o usuário terminar de usar o programa e solicitar a atualização.

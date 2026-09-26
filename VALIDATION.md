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

## Retorno do usuário — 24/09/2026

O usuário informou que instalação e desinstalação da beta.1 funcionaram. Isso complementa a limitação do ambiente de automação acima. O teste em um segundo PC e o ciclo de atualização com preservação de dados seguem pendentes. O projeto será mantido privado para uso interno, sem plano atual de publicação pública.


## Beta 0.1.0-beta.3 — 24/09/2026

- Publicação autocontida win-x64 aprovada.
- Suite existente aprovada: cronômetro/casting (15), campeonato (406.530), PNG (44), temas/reabertura e cronômetro real.
- 687 verificações de localização aprovadas: três idiomas, parâmetros dos textos, edição nativa/seletores, locks, preferência após reabertura, preservação de mensagem personalizada e JSON entre idiomas.
- Nomes de arquivo: data/hora da criação, equipes, situação aberta/concluída em cada idioma, sanitização de caracteres e ausência de ID no nome exportado; ID mantido dentro do JSON e duplicatas recusadas.
- Conferência visual das abas de campeonato em inglês/espanhol e dos quatro temas na aba TXT em espanhol. Corrigidos rótulos cortados no casting e no ajuste dos botões do timer.
- Microteste: 20 edições, 5 ms, 796 KB, zero layouts, 299/299 controles preservados.
- Traduções de termos de bombsite são uma primeira versão e ainda precisam de revisão humana no idioma do jogo.
- Atualização real da instalação beta.2 e teste em segundo PC permanecem pendentes. O pacote continua sem assinatura digital, conforme decisão do usuário.

## Beta.4 — cabeçalhos PNG

- 53 verificações de exportação aprovadas, incluindo centro absoluto e simetria lateral nos três formatos.
- PNGs feed, Stories e horizontal conferidos visualmente.
- Instalador compilado e extraído; DLL corresponde ao binário testado. Instalação no ambiente do usuário ainda não executada.

## Beta.5 — MD5

- Suite completa aprovada: self-test, championship-test, export-test, perf-test, theme-restart-test, timer-ui-test, theme-smoke e localization-test.
- Veto MD5 validado com ambas as equipes iniciando; preservação de mapas e round-trip JSON. Encerramento 3-0, 3-1, 3-2 para ambas as equipes.
- Quinta linha de lados validada na interface, após ajuste de altura. A falha inicial foi uma asserção de layout no teste, corrigida antes do empacotamento.
- Exportação de cinco mapas e seleção individual verificadas; capa Feed conferida visualmente sem sobreposição.
- Relatórios: test-results/2fd6b1d72a3a4cccaffdef5eb29b60e5.

## Beta.6 — cards e bans

Suite completa aprovada: self-test, championship-test, export-test, perf-test, theme-restart-test, timer-ui-test, theme-smoke, localization-test. Relatórios: test-results/1b84af3cf4004353a35e406c5f5a7591.

Verificações novas: nove assets monocromáticos, indisponibilidade e liberação de mapas em MD1/MD3/MD5; correção de bans em série confirmada sem alterar resultado/lados/rounds; rejeição de duplicatas, função errada e etapa futura; persistência JSON; lembretes nos limiares e encerramento 7-2 sem etapa R10. Cards e lembrete conferidos visualmente.

Uso real pelo caster ainda pendente. Modo Rápido, motivo de fim de round e regra de bombsite não fazem parte desta entrega.

## Beta.7 — modo rápido e aviso central (25/09/2026)

- Suite completa aprovada: self-test, campeonato (414872 verificações), PNG, desempenho, reinício de tema, cronômetro, quatro temas e idiomas (718 verificações).
- Modo rápido testado com cliques reais nos botões; clique duplo não duplica. Alternância Lista/Rápido mantém histórico e preferência salva.
- Finalização por lado vencedor validada no modo rápido, na correção pela lista e na leitura de JSON. Correção limpa motivo incompatível. Legado permanece Não informado.
- Avisos centrais nas transições 03→04, 06→07 e 09→10, sem repetição por correção; sem aviso quando já preenchido, em fim 7×2 ou OT. Overtime e bloqueio no 8×7 verificados.
- Avisos de dois/quatro bans conferidos visualmente nos temas claro e escuro. Modo rápido usa área adicional com rolagem em telas menores.
- Microteste: 20 alterações em 5 ms, 880 KB alocados, zero layouts e 306/306 controles retidos. Não representa consumo geral do aplicativo.
- Relatórios: test-results/2489f442feda4a8b9b0f8f4c273182ec.
- Trava de bombsite, símbolos específicos e finalização no PNG permanecem pendentes. Não foi executada instalação sobre os dados reais do usuário.

## Beta.8 — bombsite e layout compacto

- Suite completa aprovada: self-test, campeonato (415101 verificações), PNG, desempenho, reinício de tema, cronômetro, temas e idiomas (726 verificações).
- Cooldown testado com ambas as equipes e vitórias em todos os rounds regulares: dois rounds bloqueados, reset após round 06 e overtime livre. Vitória de ataque não bloqueia.
- Testes nativos: opção removida do seletor da lista, botão indisponível no modo rápido, correção libera opção e conflitos posteriores preservam histórico com sinalização.
- Registro rápido/histórico lado a lado conferidos nos temas claro/escuro. Lista mantém ataque, defesa e placar; modo compacto mostra lados na dica do round.
- Relatórios: test-results/83f9fa5aa524470ca1354377446d00d6.
- Microteste: 20 alterações, 5 ms, 920 KB alocados, zero layouts, 306/306 controles retidos. Não equivale a consumo geral.
- Instalador extraído: DLL confere com a publicação testada; SHA256 gerado. Não instalado sobre os dados reais.
- Importação/exportação mantidas, sem prioridade de evolução por decisão do usuário. Apelidos dos demais bombsites e símbolos específicos ainda dependem de revisão/referência.

## 0.2.0-beta.0.1 — visual e Scout (26/09/2026)

- Suite completa aprovada: 415105 verificações de campeonato, 59 de PNG, 729 de idiomas, 15 de base, cronômetro, reinício de tema e quatro temas.
- Relatórios: test-results/5cd6faf83b714e678ad9e15ecf9cdc21.
- MD1/MD3/MD5 verificados em áreas lógicas de 1920×920, 1366×640 e 1140×600. A página não rola; ações e confirmação permanecem acessíveis. Janelas pequenas usam alternância de configuração/metades e rolagem local nas opções do registro rápido.
- Scout Regular/Bold/Condensed carregadas privadamente; 78 retratos presentes. Cards de até 140 px lógicos, recorte de rosto, nome completo com fonte compacta e seleção por ícones preservada.
- Microteste: 20 alterações de bombsite em 6 ms, 845 KB alocados, zero layouts e 256/256 controles retidos. Não representa o consumo geral do aplicativo.
- Revisão visual dos temas claro/escuro e tamanho compacto. Após a suite, título da janela corrigido e teste de idiomas repetido em work/design02-final-title; contraste do placeholder/rodapé claro ajustado e teste de campeonato repetido em work/design02-final-contrast (pastas relativas à raiz de trabalho).
- Instalador reconstruído, extraído e comparado à DLL publicada; fontes e retratos incluídos, sem dados pessoais ou PDB. Hash SHA256 acompanha o instalador.
- Não instalado sobre dados reais, não publicado no GitHub nesta entrega. Teste pelo usuário em outro PC e outras escalas físicas de DPI ainda pendente. Novos símbolos de finalização adiados por decisão do usuário.

## 0.2.0-beta.0.2 — legibilidade e registro rápido (26/09/2026)

- Scout revertida para Segoe UI/Bahnschrift e removida do pacote atual. Layout e retratos preservados.
- Registro rápido com três divisões, equipes coloridas antes do clique e confirmação azul; seletor de mapa junto ao placar e maior altura para lados.
- Suite completa aprovada em test-results/b1c9e516b101498b881cd38fd87afd40. Após ajustes finais de altura dos botões e fonte do placar compacto, campeonato repetido em work/beta0202-final (relativo à raiz de trabalho).
- Conferência visual em janela larga e compacta, claro e escuro. Instalador comparado à DLL testada, sem fontes Scout no pacote.
- Sem instalação sobre dados reais. Histórico remoto arquiva os instaladores originais das betas 3–8 e identifica explicitamente que seus snapshots de código intermediários não foram preservados.
- Contraste dos botões coloridos conferido novamente no tema claro, com texto calculado pela luminosidade do preenchimento: work/beta0202-contrast.

## 0.2.0-beta.0.3 — alinhamento (26/09/2026)

- Suite completa aprovada em test-results/7e7a727725d04fe38279474fe394c434; inclui edição nativa, placar, JSON, PNG, idiomas, temas e desempenho.
- Nova verificação exige mesma altura e posição vertical dos quatro campos de lados em MD1/MD3/MD5. Diferença de um pixel corrigida com âncora e margem comuns.
- Histórico centralizado com pintura de chevrons sem substituir o editor nativo. Cabeçalhos de bans e botão Nova partida revisados visualmente.
- Ajuste final do centro do placar para histórico expandido/lista retestado em work/beta0203-final, relativo à raiz de trabalho.
- Instalador extraído e comparado ao binário testado. Instalação sobre dados reais não realizada.
- Imagens de mapas ainda aguardam referências do usuário e JG.

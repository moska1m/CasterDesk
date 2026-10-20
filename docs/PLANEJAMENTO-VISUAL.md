# Atualização visual e próximos passos

Implementação local 1.0.9-beta.3: menu compacto, opções em abas, moldura própria compartilhada, temas Padrão/Escuro/Claro e idioma imediato. Preferências aplicam e salvam no momento da escolha. O registro de rounds pertence à aba Partida. Não existe gravador de áudio nesta versão; não foram criados controles fictícios de áudio.

Na beta.4, Tabela, Backup e OBS ficam completos dentro das próprias abas. Opções, Atualizações e Créditos são painéis modais dentro da janela principal, sem outra janela ou barra de título. Somente esses painéis escurecem e bloqueiam o fundo; a engrenagem abre um menu compacto sem escurecimento. O tema claro usa azul nos destaques dos menus e abas; padrão e escuro mantêm vermelho. O fechar permanece vermelho em todos os temas. A identidade e a versão ficam compactas no rodapé do menu. O indicador distingue cliente OBS presente de servidor apenas habilitado.

## Fora deste pacote

- Ícones nos cabeçalhos dos blocos.
- Rever o tema claro posteriormente com inspiração Black Ice: azul-gelo e textura discreta, evitando superfícies quase brancas. Não implementado neste pacote de correções.
- Tema opcional no painel OBS; por enquanto ele continua acompanhando o OBS.
- Tutorial guiado opcional na primeira abertura, repetível pelo menu, com partida demonstrativa isolada, MD1 e três idiomas. MD3 posteriormente.
- Easter egg de seis cliques na área do microfone: identidade pixelada inspirada na Ubisoft, Rabbids e operadores em ilustrações estáticas. Quando desbloqueado, o tema deverá ficar salvo e disponível na lista para escolha a qualquer momento.
- Easter egg dos Créditos: marca fixa, desbloqueio ao rolar até o final e minijogo ao clicar. O jogo ainda não foi escolhido.
- Manutenção própria do leitor de replays se o fork deixar de acompanhar o Siege; continuar validando contribuições externas com replays conhecidos.

## Verificação

Testes automatizados usam diretório isolado e Avalonia.Headless. Validam moldura e conteúdo, abertura de dropdowns, bloqueio da área de trabalho, temas persistidos, troca imediata PT/EN/ES e preservação de nomes personalizados. A suíte existente cobre partidas, SPEC, estatísticas, tabelas, backup, OBS, timer e atualizador.

Arrastar, redimensionar, maximizar, escala de tela e aparência no Windows real precisam de verificação manual. Nenhuma sessão real ou instalação em uso é substituída pelos testes.

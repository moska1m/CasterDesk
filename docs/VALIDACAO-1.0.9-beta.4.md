# Validação local — 1.0.9-beta.4

## Correções verificadas

- Geral apresenta o seletor de idioma já na primeira abertura.
- Opções, Atualizações e Créditos são superfícies na janela principal, sem janela nativa adicional.
- Só as superfícies modais escurecem e bloqueiam o fundo. O menu compacto usa uma camada transparente para fechar sem repassar o clique.
- Cliques reais simulados na lista de idiomas não abrem o seletor de mapas nem alteram a partida ao fundo.
- Tabela (Excel/Sheets), backup/restauração e conexão OBS estão incorporados às abas.
- Paleta legível nas listas abertas durante a troca para claro e nos textos de abas ainda não visitadas.
- Botões vetoriais de minimizar/maximizar/restaurar/fechar; hover vermelho no fechar continua funcionando depois da troca de tema.
- Idioma imediato em PT/EN/ES e nomes personalizados preservados.

Compilação Release sem avisos ou erros. Núcleo e suíte completa de interface passaram no pacote final, incluindo 47 verificações específicas de aparência e superfícies. As capturas dos painéis incorporados e das listas abertas foram revisadas.

Publicação local: `publish-casterdesk-1.0.9-beta.4-2d41240f`.

Log: `build-config/package-beta4-blue.log`.

Testes isolados: `%TEMP%/CasterDesk-beta4-blue-validation`.

O instalador não foi executado nem enviado ao GitHub. Ainda é necessária conferência manual no Windows real, incluindo escala de monitor e manipulação das janelas. A reformulação clara inspirada em Black Ice fica para uma etapa posterior, conforme solicitado.

Destaques azuis no tema claro verificados; vermelho preservado no padrão/escuro e no hover do fechar.
Instalador: `dist/CasterDesk-1.0.9-beta.4-win-x64-Setup.exe`.
SHA-256: `ad6ab3e6df16975506c367ac28af204d493503d026cba076883afdc8971253c9`.
Tamanho: 134626822 bytes.

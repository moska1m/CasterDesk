# Validação 0.3.0-beta.0.4

Use scripts/Build-Wpf.ps1. A execução valida Core e WPF, intercâmbio, comandos/transações, backup, fixtures OCR e PNGs antes de compilar o instalador. Relatórios individuais ficam em test-results/wpf-release-GUID, com dados isolados; diagnóstico e limitações estão em ESTABILIZACAO-0.3.0-beta.0.4.md. Nenhuma janela foi mostrada.

---

# Validação 0.3.0-beta.0.3

Publicação final x64, teste WPF, 599 verificações de intercâmbio entre idiomas e 13 regressões PNG aprovados em `test-results/wpf-release-e74897cc52274f5ca51763808fed259d`. A matriz inclui equipes padrão/personalizadas, os nove mapas e 36 bombsites, JSON idêntico nos três idiomas, importação de versão 1 com textos PT/EN/ES, manutenção do bloqueio por vitória defensiva e rejeição de ID desconhecido. Detalhes do formato em MATCH-JSON-FORMAT.md.

Instalador compilado, 75.537.057 bytes; SHA256 `7d2a8beb2452c80bf29f00df8c779b595b763af3f706423616039d2226ea6aa8`. Não foi instalado ou aberto. JSONs novos requerem leitores com suporte ao formato 2; os antigos continuam aceitos. Os limites de validação interativa da etapa anterior permanecem.

# Validação 0.3.0-beta.0.2

Publicação final x64 e teste headless aprovados em `test-results/wpf-release-c7b26021aae04084adce7474a24917e2`. Inclui cinco abas, feed/story do anúncio, persistência do canal, edição do veto isolada da partida, atualização explícita, bloqueio de alteração destrutiva em partida registrada, reversão após falha com JSON bloqueado, cronômetro editado/inválido/24h e recuperação de configuração. A regressão PNG aprovou 13 casos com os mesmos pixels, dimensões e transparência.

Comparação de pixels do anúncio com/sem canal: alteração contida em `(190,994)–(887,1125)`, exatamente na área original das camadas 9/10 do PSD; o restante da arte é idêntico. Fontes verificadas no PSD e nos OTF: GTAmerica-CompressedBold e SimplonMono-Bold.

Instalador compilado com Inno Setup 7 x64, 75.544.215 bytes. SHA256: `1e562633e0790c2d82587b3c37fe84633465cb2da7a56ca2cc606c59730a9ada`. Instalador não executado, nenhuma janela de teste exibida e dados reais do usuário não usados. Testes interativos e demais etapas continuam pendentes conforme PROGRESSO-ESTABILIZACAO.md.

# Validação da primeira etapa WPF (0.3.0-beta.0.1)

Compilação x64, .NET 10: sem erros ou avisos.

`--wpf-check` executado com `CASTERDESK_DATA_DIR` isolado:

- Prévia real da arte de anúncio e layouts renderizados fora da tela em 1050×680, 1280×800 e 1920×1080; botão Gerar dentro dos limites da janela.
- Janela principal e menu lateral renderizados em 1440×900.
- Conferência de estatísticas e textos/cronômetro renderizados e inspecionados visualmente.
- Partida com dez jogadores gravada e reaberta, preservando estatísticas.
- Duração do cronômetro aceita/rejeitada conforme formato.
- Anúncio preserva 1080×1350 e usa o renderizador existente.
- Map veto salvo na pasta da partida e no caminho transparente fixo do OBS.

`--image-optimization-test`: 13 casos aprovados; dimensões, transparência e pixels preservados na codificação otimizada.

O teste usa `ShutdownMode.OnExplicitShutdown`: fechar uma janela nunca exibida não pode interromper a fila de renderização das verificações seguintes.

O instalador Inno Setup 7 x64 foi compilado. Não foi instalado nem executado no computador do usuário. Nenhuma janela de teste foi exibida. Ainda falta validação interativa do campeonato hospedado, OCR por Ctrl+V com prints reais, troca de DPI entre monitores e instalação/atualização sobre a versão anterior.

Os relatórios e as prévias ficam em `test-results/wpf-*`; a regressão PNG está em `test-results/wpf-png-regression`. Esses diretórios não entram no Git ou no instalador.

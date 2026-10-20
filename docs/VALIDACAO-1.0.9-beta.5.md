# Validação local — 1.0.9-beta.5

## Aplicativo

- Build Release sem avisos ou erros.
- Núcleo e suíte completa de interface passaram na publicação final, em `%TEMP%/CasterDesk-beta5-validation`.
- 171 verificações específicas: cores dos bans, equipes e seleção de operadores nos três temas; encaminhamento dos cliques na barra de título.
- Captura do tema escuro revisada: engrenagem branca e bans com cores azul/laranja.
- Os dez tamanhos do ICO aprovado foram comparados com os recursos do executável e do instalador.

Publicação: `publish-casterdesk-1.0.9-beta.5-5c116a53`.

## Instalação local

Após autorização, o aplicativo foi fechado pela janela e aguardou-se sua saída normal. A instalação foi atualizada em `%LOCALAPPDATA%/Programs/CasterDesk-Avalonia` e o programa foi reaberto. A janela respondeu com título `CasterDesk • 1.0.9-beta.5`; executável e DLL instalados são idênticos aos da publicação testada.

O instalador inicial tentou executar o componente opcional do OBS apesar de `/UPDATEAPP`. A tentativa falhou com código 1, mantendo intacta a DLL já instalada, mas removendo o marcador de habilitação. O marcador foi restaurado antes da abertura do CasterDesk. A DLL instalada foi comparada com `obs-plugin-package/release107/bin/64bit/casterdesk-obs.dll`; ambas têm SHA-256 `4071fe5ededd94951cc36e455b88f3b11a8a74b570b59d50656fba6e0b2f9c97`. O processo do OBS permaneceu aberto.

O instalador foi corrigido para impedir a execução do componente OBS diretamente na etapa de pós-instalação quando `/UPDATEAPP` estiver ativo, além da seleção de tarefas já existente. Esse ajuste do instalador foi recompilado com a mesma publicação do aplicativo; não foi executado novamente na instalação em uso.

Logs: `build-config/package-beta5.log`, `build-config/install-beta5.log` e `build-config/package-beta5-installer.log`. Os metadados em `dist` correspondem ao instalador recompilado.

Os testes de interface são Avalonia.Headless. O arraste nativo, encaixe e movimento entre monitores continuam sujeitos à conferência manual no Windows. Não houve publicação no GitHub.

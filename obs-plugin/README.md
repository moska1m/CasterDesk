# CasterDesk OBS

Plugin nativo Windows x64 para OBS Studio 31.1 ou mais recente. Registra o dock CasterDesk por obs_frontend_add_dock_by_id; não altera cenas, perfis ou docks existentes.

Lê Documentos/CasterDesk/Configuracoes/obs-connection.json pela pasta conhecida do Windows (inclui Documentos redirecionados ao OneDrive). Faz somente requisições autenticadas a 127.0.0.1. Sem URL manual, proxy, CDN ou navegação externa. Atualiza partida/mapa e reconecta ao abrir CasterDesk. Comandos não são repetidos após erro. Timer e bans usam a API existente do aplicativo.

Instalador do programa oferece instalação opcional; o componente OBS requer administrador e OBS fechado. Plugin instalado em ProgramData/obs-studio/plugins/casterdesk-obs/bin/64bit. Instalação portátil do OBS exige colocar a mesma pasta do plugin no diretório próprio da instalação portátil; o instalador automático atende o OBS convencional. Reinicie OBS após instalar. Se o dock estiver oculto, use Painéis → CasterDesk.

Desativar a conexão no CasterDesk é respeitado nas próximas aberturas. Fechar o programa deixa o painel aguardando e desabilita controles. Abra o programa para reconectar; alterações de seleção aparecem automaticamente.

## Build e validação

O build usa o template oficial obsproject/obs-plugintemplate no commit 3e7d7ac3b5342cd7d9b88890b9c70b472d1520fc. CMake, MSVC, Qt e dependências oficiais OBS têm versões/hashes em buildspec.json. Não inclui Qt DLLs no plugin: usa as do OBS.

cmake --preset windows-ci-x64
cmake --build --preset windows-x64 --config RelWithDebInfo

A workflow .github/workflows/obs-plugin.yml compila e executa verificações Qt sem janela com QT_QPA_PLATFORM=minimal, HTTP local, autenticação, timer, contexto de ban, bloqueio e reconexão. Isso não substitui conferir o carregamento e encaixe dentro de OBS real.

Plugin e suporte de build sob GPL-2.0-or-later; LICENSE contém GPL v2. O aplicativo CasterDesk mantém sua licença própria. Fontes correspondentes acompanham o instalador do plugin em data/source.zip.

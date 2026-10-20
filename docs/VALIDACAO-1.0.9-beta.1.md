# Validação — 1.0.9-beta.1

## Entrega local

Instalador: `CasterDesk-1.0.9-beta.1-win-x64-Setup.exe`, 81.074.329 bytes.

SHA-256: `7f13a29d9a21910b957a407579aed424427fde29624213c1f0aac152330029bf`.

Publicação autossuficiente Windows x64 com leitor local de replays, licenças, fontes do adaptador e identidade OAuth nativa já utilizada pelo programa. Nenhum token de conta ou replay real foi incluído. Não foi publicada release nem gerado anúncio de atualização.

## Verificações realizadas

- Compilação do aplicativo sem erros ou avisos; testes do núcleo e da interface publicados aprovados, usando dados isolados e backend headless.
- Regressões de partidas, SPEC, cronômetro, imagens, atualização, tabelas, idiomas, backup e API OBS aprovadas. A instalação e o plugin OBS não foram executados durante os testes.
- Adaptador Go testado para preservar snapshots, distinguir ausência de zero e manter o valor inicial após atualizações.
- Importação local com cópias dos 19 arquivos de NA ORG × FaZe: nove da Mansão e dez do Banco, incluindo arquivo interrompido, retorno à mesma sessão e lobby novo.
- Mansão: eliminações e mortes dos dez jogadores conferem com o registro salvo; assistências conferem com o print revisado, incluindo a divergência manual confirmada de Gomes7.
- Banco: duas sessões agrupadas corretamente a partir de três pastas; retorno à mesma sessão não duplica a base. Classificação de novo lobby é explícita; tratar o reset como acumulados restaurados é recusado.
- Os dez jogadores mantêm suas identidades após salvar/reabrir. Replays e extrações são arquivados na mesma operação recuperável das estatísticas. Alteração da cópia antes do arquivamento é recusada sem substituir estatísticas anteriores.
- Extras da Mansão: um plant, nove primeiras eliminações e nove primeiras mortes nos nove rounds; extras do Banco permanecem parciais, com eventos de nove dos dez rounds, mesmo quando os acumulados cobrem E/M/A.
- Arquivo original da partida permaneceu inalterado. Nenhum replay foi enviado pela internet.
- Telas de importação renderizadas em 800×600 e 1060×760; botão de importação disponível na conferência de estatísticas.

Os testes locais de gravação foram executados em pasta temporária fora do diretório sincronizado, após uma falha de acesso durante uma tentativa de teste sob OneDrive. Não foi alterada a política de gravação dos dados reais do usuário.

## Ainda exige teste de uso

- Instalar/atualizar no ambiente do usuário e realizar uma nova transmissão com o Siege gravando os replays.
- Observar o comportamento de arquivo efetivamente em gravação, retorno ao lobby e reconstrução de lobby durante uma partida nova.
- Conferir as diferenças restantes dos acumulados do Banco contra as sessões anteriores ao remake. A importação nunca assume que valores divergentes estejam corretos só porque foram extraídos.
- Novos patches do Siege, escalações alteradas, outros mapas e nomes de equipes diferentes podem exigir adaptação ou revisão manual.
- Os novos campos ainda não fazem parte das artes nem do modelo de tabela atuais.

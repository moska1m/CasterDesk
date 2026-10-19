# Desenvolvimento e validação do CasterDesk

O aplicativo compartilha as regras de partida entre a interface principal, o modo SPEC e o painel OBS. Alterações devem preservar a compatibilidade dos arquivos de partidas, o salvamento recuperável e as confirmações necessárias para modificar registros.

## Verificações locais

Execute em Windows com o SDK .NET 10:

```powershell
./scripts/Test-Core.ps1
./scripts/Test-Desktop.ps1
```

Os scripts usam dados isolados. Não aponte testes para a pasta de partidas de uso real. As verificações automatizadas abrangem regras de partida, persistência, importação, estatísticas, imagens e interface; seus relatórios ficam em `test-results`.

Para o componente OBS, consulte as instruções de compilação e verificações em [obs-plugin/README.md](obs-plugin/README.md).

## Compilação no GitHub

O fluxo [Signing candidate](.github/workflows/signing-candidate.yml) compila o aplicativo e o plugin, executa verificações e prepara um instalador candidato sem publicar uma release. O [fluxo do plugin OBS](.github/workflows/obs-plugin.yml) verifica o componente nativo separadamente.

A execução [39337304621](https://github.com/moska1m/CasterDesk/actions/runs/39337304621) concluiu a compilação, as verificações automatizadas e o empacotamento do candidato Windows. Esse resultado não significa aprovação da assinatura digital nem substitui o teste interativo de instalação e uso.

## Validação manual

Antes de distribuir uma atualização, confira os fluxos afetados em uso real: instalação e atualização, carregamento do painel no OBS, tamanhos de janela e escalas do Windows, importação de prints e revisão de estatísticas. Um teste automatizado não garante o funcionamento de todos os monitores, configurações ou imagens de entrada.

## Documentação

- [Novidades por versão](CHANGELOG.md)
- [Formato das partidas](MATCH-JSON-FORMAT.md)
- [Contribuição](CONTRIBUTING.md)
- [Política de assinatura](CODE_SIGNING_POLICY.md)
- [Preparação da assinatura digital](docs/SIGNPATH.md)

Relatórios específicos de versões anteriores ficam em `docs/VALIDACAO-*.md`. Documentos de implementações substituídas podem ser consultados pelo histórico do Git; não descrevem a arquitetura atual.

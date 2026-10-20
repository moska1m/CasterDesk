# Privacidade do CasterDesk

O CasterDesk registra partidas, nomes, rounds, estatísticas, imagens e configurações em arquivos no computador. A pasta padrão é Documentos/CasterDesk. Exportar uma imagem, tabela ou pacote cria arquivos para o usuário compartilhar por sua própria iniciativa. Confira seu conteúdo antes de compartilhá-los.

O aplicativo não possui serviço próprio de análise de uso ou envio automático de prints, partidas ou relatórios de erro. Logs locais podem conter caminhos e mensagens de erro; revise-os antes de encaminhar para suporte.

## Replays locais — experimental

Quando o usuário escolhe importar replays do Siege, o CasterDesk lê cópias dos arquivos locais para extrair equipes, perfis Ubisoft dos jogadores, placares, estatísticas e eventos. O processamento é local; os replays não são enviados ao r6tv, GitHub ou a outro serviço de análise. Após confirmação, cópias dos replays e dos dados extraídos ficam na pasta da partida e podem acompanhar seus pacotes e backups. Revise esses arquivos antes de compartilhá-los. A pasta de origem do jogo não é alterada.

## Conexões

- **Painel OBS:** conexão HTTP autenticada na interface local do computador. A configuração contém uma chave de acesso; não a compartilhe. O projeto atual não disponibiliza automaticamente o painel na internet.
- **Atualizações:** por padrão, consulta publicações no GitHub ao abrir e periodicamente. A opção de verificação automática pode ser desativada no menu de atualizações. O servidor recebe os dados normais da conexão, como endereço IP e identificação da versão do aplicativo. O aplicativo não inclui partidas, nomes de equipes, credenciais Google ou prints nessas consultas. Downloads ocorrem por ação do usuário; a instalação exige outra ação explícita.
- **Google Sheets:** recurso opcional, desligado por padrão. Após autenticação e ativação, envia os campos documentados em [GOOGLE-SHEETS.md](docs/GOOGLE-SHEETS.md) à planilha escolhida pelo usuário. A autorização pode ser desconectada no programa ou revogada na conta Google. Tokens são protegidos pelo Windows e armazenados em LocalAppData, fora das pastas portáveis de partidas e backups. Não são enviados ao GitHub ou ao OBS.
- **Excel local:** a opção local grava arquivos XLSX no computador, sem precisar conectar uma conta Google.

## Serviços envolvidos

As conexões opcionais também estão sujeitas às políticas de seus fornecedores: [GitHub](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement) e [Google](https://policies.google.com/privacy).

A SignPath é uma integração de publicação em preparação. Quando configurada, será usada pelo mantenedor para assinar os arquivos de distribuição; não será uma conexão executada pelo aplicativo instalado. Política do fornecedor: [SignPath](https://about.signpath.io/privacy-policy).

## Controle dos arquivos

O usuário pode criar backups, exportar partidas e remover os arquivos locais. A desinstalação preserva os dados de partidas para evitar perda acidental. Remover uma conexão Google não apaga o conteúdo já escrito na planilha; ele permanece sob controle de quem possui acesso a esse documento.

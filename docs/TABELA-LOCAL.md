# Tabela da partida — teste local

Recurso opcional introduzido na versão 1.0.8-beta.1, desligado por padrão. A partir da beta.2, a conexão Google Sheets fica em uma aba separada; veja [GOOGLE-SHEETS.md](GOOGLE-SHEETS.md). Estas instruções tratam apenas do Excel local.

1. Abra **Tabela da partida**, ao lado de Nova partida. Em janelas compactas do SPEC, use a opção de mesmo nome no menu de configurações.
2. Escolha o caminho do arquivo fixo. O padrão é `Documentos/CasterDesk/Tabelas/partida-atual.xlsx`, dentro da pasta de dados efetivamente usada pelo aplicativo.
3. Marque **Atualizar automaticamente** e clique em **Aplicar**. **Atualizar agora** também permite gerar uma cópia sem deixar a automação ligada.
4. Configure o consumidor da tabela para ler esse arquivo fixo. A atualização do consumidor depende do suporte dele a reler arquivos.

O aplicativo salva também `Tabelas/partida.xlsx` e `Tabelas/mapa-ID.xlsx` dentro da pasta de cada partida. As cópias acompanham correções e desfazer/refazer. São arquivos separados por partida e mapa, não um histórico de todas as versões de cada round.

O mapa em andamento é o primeiro ainda não confirmado da série. Consultar outro mapa no aplicativo não troca a tabela ao vivo. Ao encerrar a série, a tabela mantém o último mapa confirmado.

## Modelo da tabela

A única aba exportada é **Round Breakdown + Series Layout**, do modelo de tabela do CasterDesk. Os outros setores da planilha original não são distribuídos.

- Times: A2/B2; placar da série confirmado: E2/F2.
- Mapas: G2:I2; situação: G4:I4; resultados: G8:I8.
- Ataque inicial e overtime: B4/B5, usando `Time1`/`Time2`.
- Vencedores: B8:B22, usando `Time1`/`Time2`.
- Finalizações: C8:C22, usando `Eliminação`, `Desativador`, `Tempo` ou vazio.
- As duas finalizações de desativador viram `Desativador` somente na exportação. Os registros originais permanecem intactos.
- Células vermelhas, cabeçalhos, dimensões e validações são preservados. Os diretórios preenchidos manualmente no arquivo fixo são reaproveitados nas atualizações e cópias.

O modelo suporta MD1/MD3 e 15 rounds. MD5 gera um aviso e não substitui arquivos. A definição de um modelo ampliado fica para outra etapa.

Se o Excel bloquear o arquivo fixo, as cópias da partida continuam salvas. Feche o arquivo e use **Atualizar agora**. Desligar a automação mantém todos os arquivos já criados.

## Validação

Testes isolados verificam mapeamento, células preservadas, limpeza de rounds removidos, nomes tratados como texto, mudança de mapa, cópias de partidas em sequência, persistência da opção, arquivo bloqueado e nova tentativa. O instalador é uma versão de teste local; não foi publicado no GitHub.

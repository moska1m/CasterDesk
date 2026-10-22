# Validação — 1.1.0

Consolida as alterações das betas 1.0.9, incluindo o controle local pelo celular e as correções de encerramento da beta.11. A mudança para 1.1.0 atualiza a versão do aplicativo e do instalador; não acrescenta acesso remoto nem retira o caráter experimental do leitor de replays e das integrações com tabelas.

O usuário confirmou que o controle local funcionou no celular sem atraso perceptível e, após a correção da beta.11, confirmou instalação e fechamento normais. Essa confirmação não representa cobertura de todos os aparelhos ou redes.

A publicação passa pelos testes de núcleo e de interface headless. A validação do navegador usa o servidor local real e Edge headless, incluindo reconexão, duplicatas, perda de resposta, veto em janela, rolagem automática, Remake e layouts de 320/390/700 pixels. O teste de encerramento verifica a liberação da porta após fechar a janela.

O instalador não tem assinatura Authenticode. O manifesto e o catálogo do atualizador são assinados pela chave própria do projeto, conferida pelos clientes. Não houve reinstalação automática do OBS nem execução do instalador estável durante esses testes.

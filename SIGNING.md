# Assinatura digital e SmartScreen

A beta atual continua sem assinatura digital. Um aviso deixar de aparecer no computador de quem já executou o arquivo não garante confiança nos demais computadores ou em versões futuras.

O processo de build aceita opcionalmente SignTool (Windows SDK), CertificateThumbprint e TimestampUrl. É necessário obter um certificado de assinatura de código reconhecido, com chave acessível no armazenamento CurrentUser/My através do provedor da certificadora. Use o servidor RFC 3161 informado por ela. Não coloque chaves, certificados PFX ou senhas no repositório.

Com os três parâmetros, o build assina CasterDesk.exe e CasterDesk.dll antes de empacotar, assina o instalador depois, valida as assinaturas e o timestamp, e só então calcula o SHA256. Uma falha interrompe o processo. Sem os parâmetros, o build informa explicitamente que a beta não está assinada. A assinatura real ainda não foi testada porque não foi contratado/configurado um certificado. Quando houver certificado, integrar também a assinatura do desinstalador pelo SignTool do Inno Setup e validar a instalação completa.

Assinatura identifica o publicador e protege a integridade; não é aprovação do aplicativo pela Microsoft e não garante ausência imediata de alertas. Não alterar o arquivo após assinar. Não desativar SmartScreen ou instalar certificados de teste nos PCs dos usuários como solução de distribuição.

Referências oficiais:
- https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation
- https://learn.microsoft.com/en-us/windows/win32/seccrypto/signtool

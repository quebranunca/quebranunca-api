# Notificações da plataforma

## Separação de responsabilidades

- `PendenciaUsuario` continua sendo a fonte de verdade para ações obrigatórias e suas regras de domínio.
- `NotificacaoUsuario` é a caixa de entrada do usuário, com estado lida/não lida.
- Notificações usam `Origem + ChaveIdempotencia` por usuário. Toda origem deve fornecer uma chave estável.
- Links apontam para o fluxo responsável; ler uma notificação não executa a regra de negócio da origem.

## Módulo de entrega externa

- `IEntregaNotificacaoExternaServico` é o limite entre os fluxos da aplicação e qualquer mecanismo de entrega.
- `SolicitacaoEntregaNotificacaoDto` padroniza origem, chave de idempotência, canal, template, destinatário e dados.
- A implementação atual entrega WhatsApp diretamente pelo WhatsMiau; o projeto não depende da Central de Notificações.
- Para uma integração futura, deve-se criar outro adaptador de `IEntregaNotificacaoExternaServico` e trocar somente o registro na injeção de dependência.
- A caixa interna não envia e-mail, WhatsApp ou SMS. Cada canal externo usa um adaptador separado.
- Estados de entrega externa não substituem `LidaEmUtc`, pois entrega e leitura no aplicativo são conceitos diferentes.

## Configuração do WhatsApp

- `WhatsappConvitesCadastro:Enabled` habilita o envio.
- `Provedor`, `ProvedorBaseUrl`, `ProvedorApiKey` e `ProvedorInstancia` configuram o adaptador direto.
- Em ambiente, use `WHATSMIAU_BASE_URL`, `WHATSMIAU_API_KEY` e `WHATSMIAU_INSTANCE_NAME`.
- O template `qnf.convite.cadastro.v1` é renderizado dentro do módulo, sem expor o provedor ao serviço de convites.

## SMS com Zenvia

- O adaptador `AdaptadorSmsZenviaServico` implementa o canal `Sms` sem acoplar os fluxos da aplicação à Zenvia.
- A integração fica desabilitada por padrão e não gera custo até `Sms:Enabled` ser ativado.
- Configure `ZENVIA_API_TOKEN` e `ZENVIA_SMS_FROM` no ambiente; `ZENVIA_SMS_BASE_URL` é opcional.
- Solicitações aceitas pela API retornam `Aceito`, não `Enviado`; confirmação de entrega deve ser processada futuramente pelo webhook `MESSAGE_STATUS`.
- O contrato espera `Dados["texto"]` e telefone brasileiro com DDD; o adaptador normaliza o destino para o formato `55DDDNÚMERO`.
- Convites no modo `Automático` seguem `WhatsApp → e-mail → SMS` e param assim que um canal envia ou aceita a solicitação.
- A opção somente `WhatsApp` representa recusa do e-mail; em caso de falha, o fallback vai diretamente para SMS.

## Frontend

- O sino usa `/api/notificacoes/resumo` e abre `/app/notificacoes`.
- A contagem atualiza ao abrir, recuperar foco, voltar para a aba, após ações locais e periodicamente.
- `/app/pendencias` permanece responsável pela resolução das tarefas.

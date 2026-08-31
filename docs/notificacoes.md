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
- O mesmo adaptador renderiza `qnf.grupo.presenca.v1` para os pedidos de presença, usando nome do atleta, grupo, data, horário, Arena e link individual.

## Confirmação de presença dos grupos

- Grupos podem ter agenda semanal com dias, horário inicial, horário final e uma `Arena` cadastrada.
- `AgendaPresencaGrupos` processa os grupos no fuso `America/Sao_Paulo`, cria um encontro por grupo/data e uma confirmação por membro.
- Encontro, confirmação e notificação interna usam chaves únicas estáveis. O WhatsApp é reservado atomicamente antes do disparo, evitando concorrência entre instâncias e permitindo retomada segura após uma interrupção.
- A partir de `HoraEnvioLocal` e até o fim do jogo, membros com WhatsApp recebem o template `qnf.grupo.presenca.v1`; falhas reais têm no máximo três tentativas, com intervalo mínimo de uma hora.
- O link público usa `/presenca#<codigo>`. O código fica no fragmento do navegador, não na URL enviada ao servidor, e é transmitido à API somente no corpo das chamadas de consulta e resposta.
- O código individual possui 192 bits aleatórios, não é exibido em logs e deixa de aceitar alterações ao fim do horário do encontro.
- Membros vinculados a usuário ativo também recebem uma notificação interna com o mesmo fluxo de confirmação.
- O painel `/api/grupos/{grupoId}/presencas/painel` exige permissão de gestão do grupo; os endpoints públicos apenas consultam ou registram a resposta correspondente ao código individual.
- Em produção, `AgendaPresencaGrupos:Habilitada` deve estar ativa, `AgendaPresencaGrupos:UrlApp` deve apontar para o frontend e o adaptador de WhatsApp deve estar configurado pelas variáveis do WhatsMiau.

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

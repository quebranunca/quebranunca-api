# Notificações da plataforma

## Separação de responsabilidades

- `PendenciaUsuario` continua sendo a fonte de verdade para ações obrigatórias e suas regras de domínio.
- `NotificacaoUsuario` é a caixa de entrada do usuário, com estado lida/não lida.
- Notificações usam `Origem + ChaveIdempotencia` por usuário. Toda origem deve fornecer uma chave estável.
- Links apontam para o fluxo responsável; ler uma notificação não executa a regra de negócio da origem.

## Compatibilidade com a Central de Notificações

- `Origem` corresponde ao conceito `source` da Central.
- `ChaveIdempotencia` corresponde a `idempotencyKey`.
- A caixa interna não envia e-mail, WhatsApp ou SMS. Esses canais devem usar um adaptador separado.
- Estados de entrega externa não substituem `LidaEmUtc`, pois entrega e leitura no aplicativo são conceitos diferentes.

## Frontend

- O sino usa `/api/notificacoes/resumo` e abre `/app/notificacoes`.
- A contagem atualiza ao abrir, recuperar foco, voltar para a aba, após ações locais e periodicamente.
- `/app/pendencias` permanece responsável pela resolução das tarefas.

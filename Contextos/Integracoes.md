# Integracoes

## Midias

### Cloudinary

* Cloudinary e o provedor oficial de midias.
* Fotos de usuario utilizam Cloudinary.
* Midias de partidas utilizam Cloudinary.
* Midias publicas utilizam Cloudinary.

### Persistencia

* Banco guarda apenas:

  * URL;
  * PublicId;
  * metadados necessarios.

* Banco nao guarda:

  * arquivo local;
  * binario;
  * base64;
  * arquivos no PostgreSQL.

### Upload

* Midias de partidas chegam pela API via `multipart/form-data`.
* Frontend nao envia arquivos diretamente para Cloudinary.
* Backend centraliza validacoes e upload.

## E-mail

### Resend

* Resend e o provedor padrao de e-mail quando configurado.
* Convites podem utilizar Resend.
* Codigo de login pode utilizar Resend.
* Configuracoes de convite e login devem permanecer documentadas separadamente.

### Falhas

* Falha de envio nao invalida:

  * convite criado;
  * cadastro concluido;
  * pendencia resolvida;
  * codigo ja emitido quando aplicavel.

* Falha afeta apenas:

  * rastreabilidade;
  * notificacao;
  * reenvio.

## Mensageria

### WhatsApp

* Twilio/WhatsApp pode entregar o mesmo convite.
* WhatsApp nao substitui regras de convite.
* WhatsApp reutiliza o fluxo principal do backend.

### Futuro

Novos canais devem reutilizar a mesma estrutura de notificacao:

* E-mail
* WhatsApp
* Push
* SMS

Evitar criar regras de negocio especificas por canal.

## Observabilidade

### Application Insights

* Application Insights e opcional.
* Deve ser habilitado apenas quando a connection string estiver configurada.
* Ausencia de telemetria nao pode impedir inicializacao da aplicacao.

### Logs

* Logs nao devem registrar:

  * senha;
  * token;
  * refresh token;
  * codigo operacional;
  * link de convite;
  * dados pessoais sensiveis.

## Resiliencia

* Integracoes externas nao controlam o fluxo principal do produto.
* Falhas externas nao devem invalidar dados internos ja persistidos.
* Reenvios devem ser possiveis sem recriar entidades.
* Integracoes devem ser tratadas como dependencias externas e nao como fonte de verdade.

## Guardrails

* Cloudinary e a fonte oficial das midias.
* Resend e o provedor padrao de e-mail.
* Backend centraliza comunicacao com provedores externos.
* Frontend nao deve depender diretamente de credenciais externas.
* Fluxos de negocio nao devem depender do sucesso imediato de provedores externos.

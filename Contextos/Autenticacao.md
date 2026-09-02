# Autenticacao

## Principios

* Cadastro publico permanece fechado.
* Autenticacao utiliza JWT.
* Login utiliza fluxo de codigo enviado por e-mail.
* Convites de cadastro sao controlados exclusivamente pelo backend.
* Regras oficiais de convite, aceite e perfil criado ficam em `../AGENTS.md` e nos fluxos existentes do domínio.

## Login

* Login ocorre por codigo enviado para o e-mail informado.
* Codigo possui validade limitada.
* Codigo operacional nao deve aparecer em logs.
* Codigo operacional nao deve aparecer em responses.
* Codigo operacional nao deve ser armazenado em logs de erro.

### Development

* Em ambiente `Development` pode existir fallback sem envio de e-mail quando explicitamente configurado.
* O fallback nao deve existir em producao.
* O fallback nao deve ficar habilitado por configuracao padrao.

## JWT

* JWT representa autenticacao da sessao.
* Claims devem refletir informacoes validadas pelo backend.
* Frontend nao define perfil de usuario.
* Frontend nao altera privilegios.

## Convites

* Convites de cadastro sao gerados pelo backend.
* Convites definem o perfil efetivo criado.
* Convites nao podem elevar privilegios apos emissao.
* Convites expirados nao podem ser reutilizados.
* Convites utilizados nao podem ser reutilizados.
* Falha de envio nao invalida convite ja criado.

## Administrador

* O primeiro Administrador e criado por bootstrap operacional.
* Administradores nao sao criados por cadastro publico.
* Administradores nao sao criados por convite comum.
* Administradores podem executar acoes administrativas sem `AtletaId`.
* Validacoes de Administrador e Admin/Organizador devem ficar centralizadas em `AutorizacaoUsuarioServico`.

## Configuracao de E-mail

A configuracao de envio de codigo de login pode utilizar:

* secao `EmailCodigoLogin`; ou
* configuracao compartilhada de `EmailConvitesCadastro`; ou
* `RESEND_API_KEY`;

conforme a infraestrutura ativa.

## Seguranca

* Tokens nao devem aparecer em logs.
* Refresh tokens nao devem aparecer em logs.
* Headers Authorization nao devem aparecer em logs.
* URLs de convite nao devem aparecer em logs.
* Segredos nao devem ficar hardcoded.

## Integracoes Externas

* Falha do provedor de e-mail nao invalida:

  * convite criado;
  * cadastro concluido;
  * pendencia resolvida.

* Fluxos de negocio permanecem consistentes mesmo quando o envio falha.

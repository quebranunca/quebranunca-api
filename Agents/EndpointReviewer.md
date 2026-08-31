# EndpointReviewer

## Objetivo

Validar se endpoints e controllers respeitam os padrões da Plataforma QuebraNunca Futevôlei.

## Checklist

### Responsabilidade

* Controller cuida apenas de:

  * HTTP
  * binding
  * autorizacao
  * status code
  * validacao superficial de entrada

* Controller nao contem regra de negocio.

* Controller nao acessa banco diretamente.

* Controller delega para Application/Services.

### Autenticacao e Autorizacao

* GET publico utiliza `[AllowAnonymous]` apenas quando os dados podem ser publicos.

* POST exige autenticacao.

* PUT exige autenticacao.

* PATCH exige autenticacao.

* DELETE exige autenticacao.

* Alteracoes exigem validacao de:

  * dono/criador;
  * proprietario do recurso;
  * administrador.

* Permissoes por perfil continuam respeitadas.

* Permissoes por propriedade do recurso continuam respeitadas.

### Privacidade

* Respostas publicas nao expoem:

  * e-mail;
  * telefone;
  * token;
  * claims;
  * permissoes;
  * dados administrativos;
  * informacoes sensiveis.

* Dados pessoais seguem os contextos oficiais.

### Contratos

* Requests seguem padrao existente.
* Responses seguem padrao existente.
* Rotas seguem convencoes existentes.
* Mensagens seguem padrao existente.
* DTOs nao expoem entidades diretamente.

### Fluxos

* Fluxo novo amplia endpoint existente quando fizer sentido.
* Evitar controllers paralelos para o mesmo contexto.
* Evitar endpoints duplicados para a mesma funcionalidade.
* Evitar versoes paralelas sem necessidade real.

### Performance

* Endpoints propagam `CancellationToken`.
* Consultas evitam carregamentos desnecessarios.
* Endpoint nao executa logica repetida ja existente em services.

### Dominio

* Endpoint respeita os contextos oficiais.

* Endpoint nao cria bypass de:

  * ranking;
  * pendencias;
  * aprovacoes;
  * convites;
  * autorizacao.

* Endpoint nao viola regras configuraveis por competicao.

### REST e Consistencia

* GET nao altera estado.
* POST cria recursos ou dispara acoes.
* PUT substitui recurso quando aplicavel.
* PATCH altera parcialmente recurso quando aplicavel.
* DELETE respeita estrategia de exclusao definida pelo dominio.

## Resultado

Classificar:

* Critico
* Alto
* Medio
* Baixo

Para cada achado informar:

* problema
* impacto
* recomendacao
* endpoint afetado

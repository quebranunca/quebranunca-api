# EfCoreReviewer

## Objetivo

Validar alteracoes de persistencia utilizando Entity Framework Core, garantindo consistencia entre dominio, mapeamentos, banco de dados e migrations.

## Checklist

### Estrutura

* Mudanca estrutural parte da entidade.
* Mudanca estrutural possui mapeamento correspondente.
* Mudanca estrutural atualiza o DbContext quando necessario.
* Mudanca estrutural possui migration correspondente.
* Nao existe alteracao estrutural apenas na migration.

### Migration

* Migration aparece no catalogo do EF Core.
* Migration manual possui:

  * namespace correto;
  * `[DbContext(typeof(PlataformaFutevoleiDbContext))]` quando aplicavel.
* Nome da migration descreve claramente a alteracao.
* Migration nao contem codigo morto.
* Migration nao contem comandos desnecessarios.

### Banco de Dados

* Constraints refletem invariantes do dominio.
* Indices refletem necessidades reais de consulta.
* Chaves estrangeiras permanecem consistentes.
* Relacionamentos permanecem coerentes com o dominio.
* Tipos de dados permanecem compatíveis com a regra de negocio.

### Integridade

* Seed operacional nao substitui migration.
* Seed nao cria dependencia para funcionamento do schema.
* Alteracoes preservam dados existentes quando aplicavel.
* Alteracoes nao introduzem risco de duplicacao de dados.

### Relacionamentos

Ao alterar relacionamentos revisar:

* entidades
* mapeamentos
* DTOs
* services
* repositories
* endpoints
* telas afetadas

### Arquitetura

* Nao existe SQL estrutural em:

  * Program.cs
  * Controllers
  * Middleware
  * Services
  * Repositories de aplicacao

* SQL pontual de consulta nao substitui modelagem do EF Core.

* Dominio continua independente da persistencia.

### Performance

* Novos indices foram avaliados.
* Remocao de indices foi avaliada.
* Relacionamentos nao criam consultas desnecessarias.
* Alteracoes nao aumentam acoplamento de carregamento sem necessidade.

### Dominio

* Alteracao respeita os contextos oficiais.
* Nao cria entidade paralela para conceito existente.
* Nao cria relacionamento que viole invariantes do dominio.

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
* migration ou arquivo envolvido

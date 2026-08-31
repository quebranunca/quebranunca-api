# Persistencia

## Principios

* EF Core e migrations sao a fonte oficial de evolucao do schema.
* PostgreSQL e o banco oficial do projeto.
* Alteracoes estruturais devem partir do modelo e nao do banco.
* Banco deve refletir o dominio e nao o contrario.

## DbContext

* `PlataformaFutevoleiDbContext` centraliza:

  * DbSets;
  * configuracoes;
  * mapeamentos;
  * relacionamentos.

* Nao criar DbContexts paralelos sem necessidade real.

## Mapeamentos

* Mapeamentos ficam em:

`PlataformaFutevolei.Infraestrutura/Persistencia/Mapeamentos`

* Toda entidade persistida deve possuir configuracao correspondente.
* Constraints e relacionamentos devem refletir invariantes do dominio.
* `Atleta.email` possui unicidade por e-mail normalizado via indice funcional PostgreSQL `ix_atletas_email_normalizado_unico`, criado em migration SQL manual (`20260604170000_AdicionarIndiceUnicoEmailNormalizadoAtleta`). Nao substituir por indice literal em `Email` nem remover em baselines/recriacao de migrations.

## Migrations

* Toda mudanca estrutural deve possuir migration correspondente.
* Migration deve ser gerada a partir das alteracoes do modelo.
* Migration manual deve ser excecao.
* Migration deve aparecer no catalogo do EF Core.

### Producao

* Em Production, `Database:MigrateOnStartup` pode permanecer desabilitado.
* Aplicacao oficial de migrations ocorre via:

`scripts/aplicar-migrations-producao.sh`

### Diagnostico

Quando o banco aparentar estar migrado mas existir erro de schema:

* validar `__EFMigrationsHistory`;
* validar migration aplicada;
* validar estrutura real da tabela;
* validar ambiente correto.

## SQL Estrutural

Nao utilizar:

* `ExecuteSqlRaw`
* `ALTER TABLE`
* `CREATE TABLE`
* `DROP TABLE`
* `CREATE INDEX`
* `DROP INDEX`
* SQL estrutural equivalente

em:

* Program.cs
* startup
* pipeline HTTP
* middleware
* services de aplicacao

Mudancas estruturais devem ocorrer por migrations.

## Dados

* Seed operacional nao substitui migration.
* Seed nao deve criar dependencia para funcionamento do schema.
* Seed deve apenas complementar dados operacionais.

## Legado

* Tabelas historicas podem manter nomenclatura legada.
* Migrations historicas podem manter nomenclatura legada.
* Exemplo: `locais`.

Isso nao autoriza:

* criar novo dominio principal de Local;
* criar novas regras de negocio baseadas em Local;
* criar novas telas baseadas em Local.

Arena continua sendo o dominio oficial.

## Guardrails

* Dominio define o modelo.
* Mapeamentos refletem o dominio.
* Migrations refletem os mapeamentos.
* Banco reflete as migrations.

Fluxo oficial:

Entidade
↓
Mapeamento
↓
DbContext
↓
Migration
↓
Banco

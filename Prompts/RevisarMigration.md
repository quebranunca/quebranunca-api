# RevisarMigration

Revise uma migration EF Core.

1. Confirme entidade, mapeamento Fluent API, DbContext e snapshot.
2. Verifique índices, constraints, nulabilidade e deletes.
3. Procure SQL estrutural fora da migration.
4. Confira se a migration aparece no catálogo do EF.
5. Aponte impacto em dados existentes e ambientes Staging/Production.
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260506120000_GarantirGrupoGeralUnico")]
public partial class GarantirGrupoGeralUnico : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            WITH grupos_geral AS (
                SELECT id,
                       ROW_NUMBER() OVER (ORDER BY data_criacao, id) AS ordem
                FROM grupos
                WHERE lower(btrim(nome)) = 'geral'
            ), grupo_principal AS (
                SELECT id
                FROM grupos_geral
                WHERE ordem = 1
            )
            UPDATE partidas
            SET grupo_id = (SELECT id FROM grupo_principal)
            WHERE grupo_id IN (SELECT id FROM grupos_geral WHERE ordem > 1)
              AND EXISTS (SELECT 1 FROM grupo_principal);
            """);

        migrationBuilder.Sql("""
            WITH grupos_geral AS (
                SELECT id,
                       ROW_NUMBER() OVER (ORDER BY data_criacao, id) AS ordem
                FROM grupos
                WHERE lower(btrim(nome)) = 'geral'
            ), grupo_principal AS (
                SELECT id
                FROM grupos_geral
                WHERE ordem = 1
            ), vinculos_duplicados AS (
                SELECT ga.id,
                       ROW_NUMBER() OVER (PARTITION BY ga.atleta_id ORDER BY ga.data_criacao, ga.id) AS ordem_vinculo
                FROM grupos_atletas ga
                WHERE ga.grupo_id IN (SELECT id FROM grupos_geral)
            )
            DELETE FROM grupos_atletas
            WHERE id IN (SELECT id FROM vinculos_duplicados WHERE ordem_vinculo > 1);
            """);

        migrationBuilder.Sql("""
            WITH grupos_geral AS (
                SELECT id,
                       ROW_NUMBER() OVER (ORDER BY data_criacao, id) AS ordem
                FROM grupos
                WHERE lower(btrim(nome)) = 'geral'
            ), grupo_principal AS (
                SELECT id
                FROM grupos_geral
                WHERE ordem = 1
            )
            UPDATE grupos_atletas
            SET grupo_id = (SELECT id FROM grupo_principal)
            WHERE grupo_id IN (SELECT id FROM grupos_geral WHERE ordem > 1)
              AND EXISTS (SELECT 1 FROM grupo_principal);
            """);

        migrationBuilder.Sql("""
            DELETE FROM grupos
            WHERE id IN (
                SELECT id
                FROM (
                    SELECT id,
                           ROW_NUMBER() OVER (ORDER BY data_criacao, id) AS ordem
                    FROM grupos
                    WHERE lower(btrim(nome)) = 'geral'
                ) grupos_geral
                WHERE ordem > 1
            );
            """);

        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX IF NOT EXISTS ux_grupos_nome_geral_normalizado
            ON grupos ((lower(btrim(nome))))
            WHERE lower(btrim(nome)) = 'geral';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS ux_grupos_nome_geral_normalizado;");
    }
}

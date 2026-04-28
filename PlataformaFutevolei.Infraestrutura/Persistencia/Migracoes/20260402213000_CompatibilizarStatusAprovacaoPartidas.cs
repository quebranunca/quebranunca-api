using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[Migration("20260402213000_CompatibilizarStatusAprovacaoPartidas")]
public partial class CompatibilizarStatusAprovacaoPartidas : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE partidas
            ADD COLUMN IF NOT EXISTS status_aprovacao integer NOT NULL DEFAULT 3;
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS "IX_partidas_status_aprovacao"
            ON partidas (status_aprovacao);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS "IX_partidas_status_aprovacao";
            """);

        migrationBuilder.Sql("""
            ALTER TABLE partidas
            DROP COLUMN IF EXISTS status_aprovacao;
            """);
    }
}

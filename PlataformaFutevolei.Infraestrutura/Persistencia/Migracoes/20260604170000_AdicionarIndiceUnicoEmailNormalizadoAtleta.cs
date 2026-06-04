using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260604170000_AdicionarIndiceUnicoEmailNormalizadoAtleta")]
public partial class AdicionarIndiceUnicoEmailNormalizadoAtleta : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS ix_atletas_email_normalizado_unico
            ON atletas (lower(btrim(email)))
            WHERE email IS NOT NULL AND btrim(email) <> '';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_atletas_email_normalizado_unico;");
    }
}

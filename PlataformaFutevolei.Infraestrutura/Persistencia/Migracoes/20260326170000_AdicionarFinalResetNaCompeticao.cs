using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260326170000_AdicionarFinalResetNaCompeticao")]
public partial class AdicionarFinalResetNaCompeticao : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "possui_final_reset",
            table: "competicoes",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql("""
            UPDATE competicoes
            SET possui_final_reset = CASE
                WHEN tipo IN (1, 2) THEN TRUE
                ELSE FALSE
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "possui_final_reset",
            table: "competicoes");
    }
}

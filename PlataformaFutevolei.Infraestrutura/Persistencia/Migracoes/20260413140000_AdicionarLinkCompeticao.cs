using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

/// <inheritdoc />
[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260413140000_AdicionarLinkCompeticao")]
public partial class AdicionarLinkCompeticao : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "link",
            table: "competicoes",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "link",
            table: "competicoes");
    }
}

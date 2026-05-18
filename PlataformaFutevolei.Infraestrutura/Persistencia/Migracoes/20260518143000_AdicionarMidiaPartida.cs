using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260518143000_AdicionarMidiaPartida")]
public partial class AdicionarMidiaPartida : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "midia_public_id",
            table: "partidas",
            type: "character varying(255)",
            maxLength: 255,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "midia_tipo",
            table: "partidas",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "midia_url",
            table: "partidas",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "midia_public_id",
            table: "partidas");

        migrationBuilder.DropColumn(
            name: "midia_tipo",
            table: "partidas");

        migrationBuilder.DropColumn(
            name: "midia_url",
            table: "partidas");
    }
}

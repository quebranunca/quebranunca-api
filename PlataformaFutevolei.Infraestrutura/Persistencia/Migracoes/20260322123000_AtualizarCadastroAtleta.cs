using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    [DbContext(typeof(PlataformaFutevoleiDbContext))]
    [Migration("20260322123000_AtualizarCadastroAtleta")]
    public partial class AtualizarCadastroAtleta : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cidade",
                table: "atletas");

            migrationBuilder.AddColumn<DateTime>(
                name: "data_nascimento",
                table: "atletas",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "lado",
                table: "atletas",
                type: "integer",
                nullable: false,
                defaultValue: 3);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "data_nascimento",
                table: "atletas");

            migrationBuilder.DropColumn(
                name: "lado",
                table: "atletas");

            migrationBuilder.AddColumn<string>(
                name: "cidade",
                table: "atletas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}

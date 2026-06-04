using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    [DbContext(typeof(PlataformaFutevoleiDbContext))]
    [Migration("20260604153000_AdicionarLocalPrincipalEDiasGrupo")]
    public partial class AdicionarLocalPrincipalEDiasGrupo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "dias_da_semana",
                table: "grupos",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "local_principal",
                table: "grupos",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dias_da_semana",
                table: "grupos");

            migrationBuilder.DropColumn(
                name: "local_principal",
                table: "grupos");
        }
    }
}

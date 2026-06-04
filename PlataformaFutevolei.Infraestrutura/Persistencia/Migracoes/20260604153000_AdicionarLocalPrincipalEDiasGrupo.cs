using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarLocalPrincipalEDiasGrupo : Migration
    {
        /// <inheritdoc />
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

        /// <inheritdoc />
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

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarEstruturaAdministrativaArena : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "possui_bar_restaurante",
                table: "arenas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "possui_cobertura",
                table: "arenas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "possui_ducha",
                table: "arenas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "possui_estacionamento",
                table: "arenas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "possui_iluminacao",
                table: "arenas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "possui_loja",
                table: "arenas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "possui_vestiario",
                table: "arenas",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "possui_bar_restaurante",
                table: "arenas");

            migrationBuilder.DropColumn(
                name: "possui_cobertura",
                table: "arenas");

            migrationBuilder.DropColumn(
                name: "possui_ducha",
                table: "arenas");

            migrationBuilder.DropColumn(
                name: "possui_estacionamento",
                table: "arenas");

            migrationBuilder.DropColumn(
                name: "possui_iluminacao",
                table: "arenas");

            migrationBuilder.DropColumn(
                name: "possui_loja",
                table: "arenas");

            migrationBuilder.DropColumn(
                name: "possui_vestiario",
                table: "arenas");
        }
    }
}

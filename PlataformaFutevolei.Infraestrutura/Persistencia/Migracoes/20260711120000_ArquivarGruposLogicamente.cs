using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    [DbContext(typeof(PlataformaFutevoleiDbContext))]
    [Migration("20260711120000_ArquivarGruposLogicamente")]
    public partial class ArquivarGruposLogicamente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ativo",
                table: "grupos",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "ix_grupos_ativo",
                table: "grupos",
                column: "ativo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_grupos_ativo",
                table: "grupos");

            migrationBuilder.DropColumn(
                name: "ativo",
                table: "grupos");
        }
    }
}

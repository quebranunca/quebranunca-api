using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    [DbContext(typeof(PlataformaFutevoleiDbContext))]
    [Migration("20260323170000_AdicionarFormatoCampeonatoNaCategoria")]
    public partial class AdicionarFormatoCampeonatoNaCategoria : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "formato_campeonato_id",
                table: "categorias_competicao",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_categorias_competicao_formato_campeonato_id",
                table: "categorias_competicao",
                column: "formato_campeonato_id");

            migrationBuilder.AddForeignKey(
                name: "FK_categorias_competicao_formatos_campeonato_formato_campeonato_id",
                table: "categorias_competicao",
                column: "formato_campeonato_id",
                principalTable: "formatos_campeonato",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_categorias_competicao_formatos_campeonato_formato_campeonato_id",
                table: "categorias_competicao");

            migrationBuilder.DropIndex(
                name: "IX_categorias_competicao_formato_campeonato_id",
                table: "categorias_competicao");

            migrationBuilder.DropColumn(
                name: "formato_campeonato_id",
                table: "categorias_competicao");
        }
    }
}

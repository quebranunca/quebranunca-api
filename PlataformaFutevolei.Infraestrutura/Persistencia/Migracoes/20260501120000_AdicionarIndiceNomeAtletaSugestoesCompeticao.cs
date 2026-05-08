using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    [DbContext(typeof(PlataformaFutevoleiDbContext))]
    [Migration("20260501120000_AdicionarIndiceNomeAtletaSugestoesCompeticao")]
    public partial class AdicionarIndiceNomeAtletaSugestoesCompeticao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_atletas_nome",
                table: "atletas",
                column: "nome");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_atletas_nome",
                table: "atletas");
        }
    }
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    [DbContext(typeof(PlataformaFutevoleiDbContext))]
    [Migration("20260413123000_AtualizarConvitesPendentesParaAtleta")]
    public partial class AtualizarConvitesPendentesParaAtleta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE convites_cadastro
                SET perfil_destino = 3
                WHERE perfil_destino = 2
                  AND usado_em_utc IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    [DbContext(typeof(PlataformaFutevoleiDbContext))]
    [Migration("20260323201000_NormalizarOrdemDuplas")]
    public partial class NormalizarOrdemDuplas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                update duplas
                set
                    atleta1_id = atleta2_id,
                    atleta2_id = atleta1_id
                where atleta1_id > atleta2_id;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

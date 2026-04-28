using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    [DbContext(typeof(PlataformaFutevoleiDbContext))]
    [Migration("20260323150000_AdicionarPontuacaoColocacaoRegraCompeticao")]
    public partial class AdicionarPontuacaoColocacaoRegraCompeticao : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "pontos_primeiro_lugar",
                table: "regras_competicao",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "pontos_segundo_lugar",
                table: "regras_competicao",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "pontos_terceiro_lugar",
                table: "regras_competicao",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pontos_primeiro_lugar",
                table: "regras_competicao");

            migrationBuilder.DropColumn(
                name: "pontos_segundo_lugar",
                table: "regras_competicao");

            migrationBuilder.DropColumn(
                name: "pontos_terceiro_lugar",
                table: "regras_competicao");
        }
    }
}

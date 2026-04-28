using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260326123000_AdicionarControleInscricoesCategoria")]
public partial class AdicionarControleInscricoesCategoria : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "inscricoes_encerradas",
            table: "categorias_competicao",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "quantidade_maxima_duplas",
            table: "categorias_competicao",
            type: "integer",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "inscricoes_encerradas",
            table: "categorias_competicao");

        migrationBuilder.DropColumn(
            name: "quantidade_maxima_duplas",
            table: "categorias_competicao");
    }
}

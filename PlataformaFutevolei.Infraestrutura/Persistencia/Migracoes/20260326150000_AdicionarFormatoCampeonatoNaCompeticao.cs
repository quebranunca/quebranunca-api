using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260326150000_AdicionarFormatoCampeonatoNaCompeticao")]
public partial class AdicionarFormatoCampeonatoNaCompeticao : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "formato_campeonato_id",
            table: "competicoes",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_competicoes_formato_campeonato_id",
            table: "competicoes",
            column: "formato_campeonato_id");

        migrationBuilder.AddForeignKey(
            name: "FK_competicoes_formatos_campeonato_formato_campeonato_id",
            table: "competicoes",
            column: "formato_campeonato_id",
            principalTable: "formatos_campeonato",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_competicoes_formatos_campeonato_formato_campeonato_id",
            table: "competicoes");

        migrationBuilder.DropIndex(
            name: "IX_competicoes_formato_campeonato_id",
            table: "competicoes");

        migrationBuilder.DropColumn(
            name: "formato_campeonato_id",
            table: "competicoes");
    }
}

using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes;

[DbContext(typeof(PlataformaFutevoleiDbContext))]
[Migration("20260325120000_AdicionarGruposAtletas")]
public partial class AdicionarGruposAtletas : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "grupos_atletas",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                competicao_id = table.Column<Guid>(type: "uuid", nullable: false),
                atleta_id = table.Column<Guid>(type: "uuid", nullable: false),
                data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_grupos_atletas", x => x.id);
                table.ForeignKey(
                    name: "FK_grupos_atletas_atletas_atleta_id",
                    column: x => x.atleta_id,
                    principalTable: "atletas",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_grupos_atletas_competicoes_competicao_id",
                    column: x => x.competicao_id,
                    principalTable: "competicoes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_grupos_atletas_atleta_id",
            table: "grupos_atletas",
            column: "atleta_id");

        migrationBuilder.CreateIndex(
            name: "IX_grupos_atletas_competicao_id",
            table: "grupos_atletas",
            column: "competicao_id");

        migrationBuilder.CreateIndex(
            name: "IX_grupos_atletas_competicao_id_atleta_id",
            table: "grupos_atletas",
            columns: new[] { "competicao_id", "atleta_id" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "grupos_atletas");
    }
}

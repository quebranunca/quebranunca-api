using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    [DbContext(typeof(PlataformaFutevoleiDbContext))]
    [Migration("20260319120000_AdicionarLigasEPesoRanking")]
    public partial class AdicionarLigasEPesoRanking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ligas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ligas", x => x.id);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "liga_id",
                table: "competicoes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "conta_ranking_liga",
                table: "competicoes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "peso_ranking",
                table: "categorias_competicao",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.Sql("ALTER TABLE partidas DROP CONSTRAINT IF EXISTS ck_partidas_vencedora_valida;");
            migrationBuilder.AddCheckConstraint(
                name: "ck_partidas_vencedora_valida",
                table: "partidas",
                sql: "\"dupla_vencedora_id\" = \"dupla_a_id\" OR \"dupla_vencedora_id\" = \"dupla_b_id\"");
            migrationBuilder.AddCheckConstraint(
                name: "ck_partidas_placar_valido",
                table: "partidas",
                sql: "\"placar_dupla_a\" <> \"placar_dupla_b\" AND GREATEST(\"placar_dupla_a\", \"placar_dupla_b\") >= 18 AND ABS(\"placar_dupla_a\" - \"placar_dupla_b\") >= 2");
            migrationBuilder.AddCheckConstraint(
                name: "ck_partidas_vencedora_coerente_placar",
                table: "partidas",
                sql: "((\"placar_dupla_a\" > \"placar_dupla_b\") AND \"dupla_vencedora_id\" = \"dupla_a_id\") OR ((\"placar_dupla_b\" > \"placar_dupla_a\") AND \"dupla_vencedora_id\" = \"dupla_b_id\")");

            migrationBuilder.CreateIndex(
                name: "IX_partidas_categoria_competicao_id_data_partida",
                table: "partidas",
                columns: new[] { "categoria_competicao_id", "data_partida" });

            migrationBuilder.CreateIndex(
                name: "IX_competicoes_liga_id",
                table: "competicoes",
                column: "liga_id");

            migrationBuilder.CreateIndex(
                name: "IX_ligas_nome",
                table: "ligas",
                column: "nome",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_competicoes_ligas_liga_id",
                table: "competicoes",
                column: "liga_id",
                principalTable: "ligas",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_competicoes_ligas_liga_id",
                table: "competicoes");

            migrationBuilder.DropTable(
                name: "ligas");

            migrationBuilder.DropIndex(
                name: "IX_partidas_categoria_competicao_id_data_partida",
                table: "partidas");

            migrationBuilder.DropIndex(
                name: "IX_competicoes_liga_id",
                table: "competicoes");

            migrationBuilder.DropCheckConstraint(
                name: "ck_partidas_placar_valido",
                table: "partidas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_partidas_vencedora_coerente_placar",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "peso_ranking",
                table: "categorias_competicao");

            migrationBuilder.DropColumn(
                name: "conta_ranking_liga",
                table: "competicoes");

            migrationBuilder.DropColumn(
                name: "liga_id",
                table: "competicoes");
        }
    }
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    [DbContext(typeof(PlataformaFutevoleiDbContext))]
    [Migration("20260321221500_AdicionarRegrasCompeticao")]
    public partial class AdicionarRegrasCompeticao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "regras_competicao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    pontos_minimos_partida = table.Column<int>(type: "integer", nullable: false),
                    diferenca_minima_partida = table.Column<int>(type: "integer", nullable: false),
                    permite_empate = table.Column<bool>(type: "boolean", nullable: false),
                    pontos_vitoria = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    pontos_derrota = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regras_competicao", x => x.id);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "regra_competicao_id",
                table: "competicoes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_regras_competicao_nome",
                table: "regras_competicao",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_competicoes_regra_competicao_id",
                table: "competicoes",
                column: "regra_competicao_id");

            migrationBuilder.AddForeignKey(
                name: "FK_competicoes_regras_competicao_regra_competicao_id",
                table: "competicoes",
                column: "regra_competicao_id",
                principalTable: "regras_competicao",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.DropCheckConstraint(
                name: "ck_partidas_vencedora_valida",
                table: "partidas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_partidas_placar_valido",
                table: "partidas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_partidas_vencedora_coerente_placar",
                table: "partidas");

            migrationBuilder.AlterColumn<Guid>(
                name: "dupla_vencedora_id",
                table: "partidas",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddCheckConstraint(
                name: "ck_partidas_placar_nao_negativo",
                table: "partidas",
                sql: "\"placar_dupla_a\" >= 0 AND \"placar_dupla_b\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_partidas_vencedora_coerente_placar",
                table: "partidas",
                sql: "((\"placar_dupla_a\" = \"placar_dupla_b\") AND \"dupla_vencedora_id\" IS NULL) OR ((\"placar_dupla_a\" > \"placar_dupla_b\") AND \"dupla_vencedora_id\" = \"dupla_a_id\") OR ((\"placar_dupla_b\" > \"placar_dupla_a\") AND \"dupla_vencedora_id\" = \"dupla_b_id\")");

            migrationBuilder.AddCheckConstraint(
                name: "ck_partidas_vencedora_valida",
                table: "partidas",
                sql: "\"dupla_vencedora_id\" IS NULL OR \"dupla_vencedora_id\" = \"dupla_a_id\" OR \"dupla_vencedora_id\" = \"dupla_b_id\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_partidas_placar_nao_negativo",
                table: "partidas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_partidas_vencedora_coerente_placar",
                table: "partidas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_partidas_vencedora_valida",
                table: "partidas");

            migrationBuilder.AlterColumn<Guid>(
                name: "dupla_vencedora_id",
                table: "partidas",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_partidas_placar_valido",
                table: "partidas",
                sql: "\"placar_dupla_a\" <> \"placar_dupla_b\" AND GREATEST(\"placar_dupla_a\", \"placar_dupla_b\") >= 18 AND ABS(\"placar_dupla_a\" - \"placar_dupla_b\") >= 2");

            migrationBuilder.AddCheckConstraint(
                name: "ck_partidas_vencedora_coerente_placar",
                table: "partidas",
                sql: "((\"placar_dupla_a\" > \"placar_dupla_b\") AND \"dupla_vencedora_id\" = \"dupla_a_id\") OR ((\"placar_dupla_b\" > \"placar_dupla_a\") AND \"dupla_vencedora_id\" = \"dupla_b_id\")");

            migrationBuilder.AddCheckConstraint(
                name: "ck_partidas_vencedora_valida",
                table: "partidas",
                sql: "\"dupla_vencedora_id\" = \"dupla_a_id\" OR \"dupla_vencedora_id\" = \"dupla_b_id\"");

            migrationBuilder.DropForeignKey(
                name: "FK_competicoes_regras_competicao_regra_competicao_id",
                table: "competicoes");

            migrationBuilder.DropTable(
                name: "regras_competicao");

            migrationBuilder.DropIndex(
                name: "IX_competicoes_regra_competicao_id",
                table: "competicoes");

            migrationBuilder.DropColumn(
                name: "regra_competicao_id",
                table: "competicoes");
        }
    }
}

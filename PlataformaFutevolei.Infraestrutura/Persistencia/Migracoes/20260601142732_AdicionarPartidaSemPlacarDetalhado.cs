using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarPartidaSemPlacarDetalhado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_partidas_placar_nao_negativo",
                table: "partidas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_partidas_status_e_resultado",
                table: "partidas");

            migrationBuilder.AlterColumn<int>(
                name: "placar_dupla_b",
                table: "partidas",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "placar_dupla_a",
                table: "partidas",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "tipo_registro_resultado",
                table: "partidas",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddCheckConstraint(
                name: "ck_partidas_placar_nao_negativo",
                table: "partidas",
                sql: "(\"placar_dupla_a\" IS NULL OR \"placar_dupla_a\" >= 0) AND (\"placar_dupla_b\" IS NULL OR \"placar_dupla_b\" >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_partidas_status_e_resultado",
                table: "partidas",
                sql: "((\"status\" = 1) AND \"dupla_vencedora_id\" IS NULL) OR ((\"status\" = 2) AND \"dupla_a_id\" IS NOT NULL AND \"dupla_b_id\" IS NOT NULL AND (((\"tipo_registro_resultado\" = 1) AND \"placar_dupla_a\" IS NOT NULL AND \"placar_dupla_b\" IS NOT NULL AND (((\"placar_dupla_a\" = \"placar_dupla_b\") AND \"dupla_vencedora_id\" IS NULL) OR ((\"placar_dupla_a\" > \"placar_dupla_b\") AND \"dupla_vencedora_id\" = \"dupla_a_id\") OR ((\"placar_dupla_b\" > \"placar_dupla_a\") AND \"dupla_vencedora_id\" = \"dupla_b_id\"))) OR ((\"tipo_registro_resultado\" = 2) AND \"placar_dupla_a\" IS NULL AND \"placar_dupla_b\" IS NULL AND \"dupla_vencedora_id\" IS NOT NULL)))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_partidas_placar_nao_negativo",
                table: "partidas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_partidas_status_e_resultado",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "tipo_registro_resultado",
                table: "partidas");

            migrationBuilder.AlterColumn<int>(
                name: "placar_dupla_b",
                table: "partidas",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "placar_dupla_a",
                table: "partidas",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_partidas_placar_nao_negativo",
                table: "partidas",
                sql: "\"placar_dupla_a\" >= 0 AND \"placar_dupla_b\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_partidas_status_e_resultado",
                table: "partidas",
                sql: "((\"status\" = 1) AND \"dupla_vencedora_id\" IS NULL AND \"placar_dupla_a\" = 0 AND \"placar_dupla_b\" = 0) OR ((\"status\" = 2) AND \"dupla_a_id\" IS NOT NULL AND \"dupla_b_id\" IS NOT NULL AND (((\"placar_dupla_a\" = \"placar_dupla_b\") AND \"dupla_vencedora_id\" IS NULL) OR ((\"placar_dupla_a\" > \"placar_dupla_b\") AND \"dupla_vencedora_id\" = \"dupla_a_id\") OR ((\"placar_dupla_b\" > \"placar_dupla_a\") AND \"dupla_vencedora_id\" = \"dupla_b_id\")))");
        }
    }
}

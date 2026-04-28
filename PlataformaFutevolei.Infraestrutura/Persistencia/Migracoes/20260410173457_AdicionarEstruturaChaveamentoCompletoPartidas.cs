using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarEstruturaChaveamentoCompletoPartidas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_partidas_duplas_diferentes",
                table: "partidas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_partidas_status_e_resultado",
                table: "partidas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_partidas_vencedora_valida",
                table: "partidas");

            migrationBuilder.AlterColumn<Guid>(
                name: "dupla_b_id",
                table: "partidas",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "dupla_a_id",
                table: "partidas",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<bool>(
                name: "ativa",
                table: "partidas",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "eh_final",
                table: "partidas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "eh_finalissima",
                table: "partidas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "eh_preliminar",
                table: "partidas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "lado_da_chave",
                table: "partidas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "origem_participante_a_tipo",
                table: "partidas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "origem_participante_b_tipo",
                table: "partidas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "partida_origem_participante_a_id",
                table: "partidas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "partida_origem_participante_b_id",
                table: "partidas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "posicao_na_chave",
                table: "partidas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "proxima_partida_perdedor_id",
                table: "partidas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "proxima_partida_vencedor_id",
                table: "partidas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rodada",
                table: "partidas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "slot_destino_perdedor",
                table: "partidas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "slot_destino_vencedor",
                table: "partidas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_partidas_lado_da_chave",
                table: "partidas",
                column: "lado_da_chave");

            migrationBuilder.CreateIndex(
                name: "IX_partidas_partida_origem_participante_a_id",
                table: "partidas",
                column: "partida_origem_participante_a_id");

            migrationBuilder.CreateIndex(
                name: "IX_partidas_partida_origem_participante_b_id",
                table: "partidas",
                column: "partida_origem_participante_b_id");

            migrationBuilder.CreateIndex(
                name: "IX_partidas_proxima_partida_perdedor_id",
                table: "partidas",
                column: "proxima_partida_perdedor_id");

            migrationBuilder.CreateIndex(
                name: "IX_partidas_proxima_partida_vencedor_id",
                table: "partidas",
                column: "proxima_partida_vencedor_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_partidas_duplas_diferentes",
                table: "partidas",
                sql: "\"dupla_a_id\" IS NULL OR \"dupla_b_id\" IS NULL OR \"dupla_a_id\" <> \"dupla_b_id\"");

            migrationBuilder.AddCheckConstraint(
                name: "ck_partidas_status_e_resultado",
                table: "partidas",
                sql: "((\"status\" = 1) AND \"dupla_vencedora_id\" IS NULL AND \"placar_dupla_a\" = 0 AND \"placar_dupla_b\" = 0) OR ((\"status\" = 2) AND \"dupla_a_id\" IS NOT NULL AND \"dupla_b_id\" IS NOT NULL AND (((\"placar_dupla_a\" = \"placar_dupla_b\") AND \"dupla_vencedora_id\" IS NULL) OR ((\"placar_dupla_a\" > \"placar_dupla_b\") AND \"dupla_vencedora_id\" = \"dupla_a_id\") OR ((\"placar_dupla_b\" > \"placar_dupla_a\") AND \"dupla_vencedora_id\" = \"dupla_b_id\")))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_partidas_vencedora_valida",
                table: "partidas",
                sql: "\"dupla_vencedora_id\" IS NULL OR (\"dupla_a_id\" IS NOT NULL AND \"dupla_b_id\" IS NOT NULL AND (\"dupla_vencedora_id\" = \"dupla_a_id\" OR \"dupla_vencedora_id\" = \"dupla_b_id\"))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_partidas_lado_da_chave",
                table: "partidas");

            migrationBuilder.DropIndex(
                name: "IX_partidas_partida_origem_participante_a_id",
                table: "partidas");

            migrationBuilder.DropIndex(
                name: "IX_partidas_partida_origem_participante_b_id",
                table: "partidas");

            migrationBuilder.DropIndex(
                name: "IX_partidas_proxima_partida_perdedor_id",
                table: "partidas");

            migrationBuilder.DropIndex(
                name: "IX_partidas_proxima_partida_vencedor_id",
                table: "partidas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_partidas_duplas_diferentes",
                table: "partidas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_partidas_status_e_resultado",
                table: "partidas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_partidas_vencedora_valida",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "ativa",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "eh_final",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "eh_finalissima",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "eh_preliminar",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "lado_da_chave",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "origem_participante_a_tipo",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "origem_participante_b_tipo",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "partida_origem_participante_a_id",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "partida_origem_participante_b_id",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "posicao_na_chave",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "proxima_partida_perdedor_id",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "proxima_partida_vencedor_id",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "rodada",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "slot_destino_perdedor",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "slot_destino_vencedor",
                table: "partidas");

            migrationBuilder.AlterColumn<Guid>(
                name: "dupla_b_id",
                table: "partidas",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "dupla_a_id",
                table: "partidas",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_partidas_duplas_diferentes",
                table: "partidas",
                sql: "\"dupla_a_id\" <> \"dupla_b_id\"");

            migrationBuilder.AddCheckConstraint(
                name: "ck_partidas_status_e_resultado",
                table: "partidas",
                sql: "((\"status\" = 1) AND \"dupla_vencedora_id\" IS NULL AND \"placar_dupla_a\" = 0 AND \"placar_dupla_b\" = 0) OR ((\"status\" = 2) AND (((\"placar_dupla_a\" = \"placar_dupla_b\") AND \"dupla_vencedora_id\" IS NULL) OR ((\"placar_dupla_a\" > \"placar_dupla_b\") AND \"dupla_vencedora_id\" = \"dupla_a_id\") OR ((\"placar_dupla_b\" > \"placar_dupla_a\") AND \"dupla_vencedora_id\" = \"dupla_b_id\")))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_partidas_vencedora_valida",
                table: "partidas",
                sql: "\"dupla_vencedora_id\" IS NULL OR \"dupla_vencedora_id\" = \"dupla_a_id\" OR \"dupla_vencedora_id\" = \"dupla_b_id\"");
        }
    }
}

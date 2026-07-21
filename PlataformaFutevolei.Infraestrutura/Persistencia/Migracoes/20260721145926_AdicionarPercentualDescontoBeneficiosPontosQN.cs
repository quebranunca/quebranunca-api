using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarPercentualDescontoBeneficiosPontosQN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "percentual_desconto",
                table: "beneficios_pontuacao",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-4111-8111-111111111111"),
                column: "percentual_desconto",
                value: 10);

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-4222-8222-222222222222"),
                column: "percentual_desconto",
                value: 20);

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-4333-8333-333333333333"),
                column: "percentual_desconto",
                value: null);

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-8444-444444444444"),
                column: "percentual_desconto",
                value: null);

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-4555-8555-555555555555"),
                column: "percentual_desconto",
                value: null);

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-4666-8666-666666666666"),
                column: "percentual_desconto",
                value: null);

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("77777777-7777-4777-8777-777777777777"),
                column: "percentual_desconto",
                value: null);

            migrationBuilder.AddCheckConstraint(
                name: "ck_beneficios_pontuacao_percentual_desconto_valido",
                table: "beneficios_pontuacao",
                sql: "\"percentual_desconto\" IS NULL OR (\"percentual_desconto\" > 0 AND \"percentual_desconto\" <= 30)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_beneficios_pontuacao_percentual_desconto_valido",
                table: "beneficios_pontuacao");

            migrationBuilder.DropColumn(
                name: "percentual_desconto",
                table: "beneficios_pontuacao");
        }
    }
}

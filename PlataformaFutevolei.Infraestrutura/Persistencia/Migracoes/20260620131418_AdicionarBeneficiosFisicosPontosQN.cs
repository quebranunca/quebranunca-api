using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarBeneficiosFisicosPontosQN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-4333-8333-333333333333"),
                column: "ordem",
                value: 4);

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-8444-444444444444"),
                column: "ordem",
                value: 5);

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-4555-8555-555555555555"),
                column: "ordem",
                value: 6);

            migrationBuilder.InsertData(
                table: "beneficios_pontuacao",
                columns: new[] { "id", "ativo", "data_atualizacao", "data_criacao", "descricao", "imagem_url", "ordem", "pontos_necessarios", "quantidade_disponivel", "tipo", "titulo" },
                values: new object[,]
                {
                    { new Guid("66666666-6666-4666-8666-666666666666"), true, new DateTime(2026, 6, 19, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Chaveiro exclusivo QuebraNunca para levar a resenha com voce.", "pontos-qn/beneficio-chaveiro-qn.png", 3, 2000, null, 4, "Chaveiro QuebraNunca" },
                    { new Guid("77777777-7777-4777-8777-777777777777"), true, new DateTime(2026, 6, 19, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Boné trucker QuebraNunca para usar dentro e fora da areia.", "pontos-qn/beneficio-bone-qn.png", 7, 8000, null, 4, "Boné QuebraNunca" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-4666-8666-666666666666"));

            migrationBuilder.DeleteData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("77777777-7777-4777-8777-777777777777"));

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-4333-8333-333333333333"),
                column: "ordem",
                value: 3);

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-8444-444444444444"),
                column: "ordem",
                value: 4);

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-4555-8555-555555555555"),
                column: "ordem",
                value: 5);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AtualizarCatalogoBeneficiosPromocionaisPontosQN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-4111-8111-111111111111"),
                columns: new[] { "descricao", "pontos_necessarios", "titulo" },
                values: new object[] { "Beneficio promocional para campanhas da loja QuebraNunca.", 300, "Cupom 10% OFF" });

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-4222-8222-222222222222"),
                columns: new[] { "descricao", "pontos_necessarios", "titulo" },
                values: new object[] { "Condicao promocional para produtos selecionados da loja QuebraNunca.", 600, "Cupom 20% OFF" });

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-4333-8333-333333333333"),
                column: "titulo",
                value: "Campanha promocional QuebraNunca");

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-4666-8666-666666666666"),
                columns: new[] { "descricao", "pontos_necessarios" },
                values: new object[] { "Brinde fisico QuebraNunca disponivel por campanha.", 700 });

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("77777777-7777-4777-8777-777777777777"),
                columns: new[] { "descricao", "pontos_necessarios" },
                values: new object[] { "Produto especial disponivel em campanhas da comunidade.", 1500 });

            migrationBuilder.Sql("""
                UPDATE beneficios_pontuacao
                SET quantidade_disponivel = COALESCE(quantidade_disponivel, 100)
                WHERE id IN ('22222222-2222-4222-8222-222222222222', '66666666-6666-4666-8666-666666666666');

                UPDATE beneficios_pontuacao
                SET quantidade_disponivel = COALESCE(quantidade_disponivel, 50)
                WHERE id = '77777777-7777-4777-8777-777777777777';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-4111-8111-111111111111"),
                columns: new[] { "descricao", "pontos_necessarios", "titulo" },
                values: new object[] { "Beneficio promocional interno para campanhas QuebraNunca, sujeito a disponibilidade, regras da campanha e validacao.", 500, "Cupom especial QuebraNunca" });

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-4222-8222-222222222222"),
                columns: new[] { "descricao", "pontos_necessarios", "titulo" },
                values: new object[] { "Beneficio promocional interno em produto de campanha QuebraNunca, sujeito a disponibilidade e validacao.", 1000, "Condicao especial em produto QN" });

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-4333-8333-333333333333"),
                column: "titulo",
                value: "Desconto promocional em campanha");

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-4666-8666-666666666666"),
                columns: new[] { "descricao", "pontos_necessarios" },
                values: new object[] { "Chaveiro exclusivo QuebraNunca para levar a resenha com voce.", 2000 });

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("77777777-7777-4777-8777-777777777777"),
                columns: new[] { "descricao", "pontos_necessarios" },
                values: new object[] { "Boné trucker QuebraNunca para usar dentro e fora da areia.", 8000 });

            migrationBuilder.Sql("""
                UPDATE beneficios_pontuacao
                SET quantidade_disponivel = NULL
                WHERE id IN ('22222222-2222-4222-8222-222222222222', '66666666-6666-4666-8666-666666666666')
                  AND quantidade_disponivel = 100;

                UPDATE beneficios_pontuacao
                SET quantidade_disponivel = NULL
                WHERE id = '77777777-7777-4777-8777-777777777777'
                  AND quantidade_disponivel = 50;
                """);
        }
    }
}

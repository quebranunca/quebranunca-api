using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AtualizarEconomiaPontosQN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-4111-8111-111111111111"),
                columns: new[] { "descricao", "pontos_necessarios", "titulo" },
                values: new object[] { "Cupom manual de R$ 5 off para campanhas QuebraNunca. Pode cobrir ate 30% do pedido e nao inclui frete, salvo campanha especifica.", 500, "R$ 5 off na loja" });

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-4222-8222-222222222222"),
                columns: new[] { "descricao", "pontos_necessarios", "tipo", "titulo" },
                values: new object[] { "Cupom manual de R$ 10 off para campanhas QuebraNunca. Pode cobrir ate 30% do pedido e nao inclui frete, salvo campanha especifica.", 1000, 1, "R$ 10 off na loja" });

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-4333-8333-333333333333"),
                columns: new[] { "descricao", "pontos_necessarios", "tipo", "titulo" },
                values: new object[] { "Cupom manual de R$ 20 off para campanhas QuebraNunca. Pode cobrir ate 30% do pedido e nao inclui frete, salvo campanha especifica.", 2000, 1, "R$ 20 off na loja" });

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-8444-444444444444"),
                columns: new[] { "descricao", "pontos_necessarios", "tipo", "titulo" },
                values: new object[] { "Cupom manual de R$ 30 off para campanhas QuebraNunca. Pode cobrir ate 30% do pedido e nao inclui frete, salvo campanha especifica.", 3000, 1, "R$ 30 off na loja" });

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-4555-8555-555555555555"),
                columns: new[] { "descricao", "pontos_necessarios", "tipo", "titulo" },
                values: new object[] { "Cupom manual de R$ 50 off para campanhas QuebraNunca. Pode cobrir ate 30% do pedido e nao inclui frete, salvo campanha especifica.", 5000, 1, "R$ 50 off na loja" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-4111-8111-111111111111"),
                columns: new[] { "descricao", "pontos_necessarios", "titulo" },
                values: new object[] { "Solicite um cupom manual de desconto para usar na loja QuebraNunca.", 600, "10% OFF na loja" });

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-4222-8222-222222222222"),
                columns: new[] { "descricao", "pontos_necessarios", "tipo", "titulo" },
                values: new object[] { "Brinde sujeito a disponibilidade e aprovação manual.", 1200, 2, "Boné QuebraNunca" });

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-4333-8333-333333333333"),
                columns: new[] { "descricao", "pontos_necessarios", "tipo", "titulo" },
                values: new object[] { "Brinde sujeito a disponibilidade e aprovação manual.", 1200, 2, "Garrafa QN" });

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-8444-444444444444"),
                columns: new[] { "descricao", "pontos_necessarios", "tipo", "titulo" },
                values: new object[] { "Produto sujeito a estoque e aprovação manual.", 1800, 4, "Camiseta Drop Especial" });

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-4555-8555-555555555555"),
                columns: new[] { "descricao", "pontos_necessarios", "tipo", "titulo" },
                values: new object[] { "Experiência agendada manualmente pela equipe QuebraNunca.", 2000, 3, "Aula com parceiro" });
        }
    }
}

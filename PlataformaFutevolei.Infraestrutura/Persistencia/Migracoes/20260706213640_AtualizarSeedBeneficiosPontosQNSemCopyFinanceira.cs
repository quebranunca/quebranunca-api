using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AtualizarSeedBeneficiosPontosQNSemCopyFinanceira : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-4111-8111-111111111111"),
                columns: new[] { "descricao", "titulo" },
                values: new object[] { "Beneficio promocional interno para campanhas QuebraNunca, sujeito a disponibilidade, regras da campanha e validacao.", "Cupom especial QuebraNunca" });

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-4222-8222-222222222222"),
                columns: new[] { "descricao", "titulo" },
                values: new object[] { "Beneficio promocional interno em produto de campanha QuebraNunca, sujeito a disponibilidade e validacao.", "Condicao especial em produto QN" });

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-4333-8333-333333333333"),
                columns: new[] { "descricao", "titulo" },
                values: new object[] { "Condicao promocional interna para campanhas QuebraNunca, sujeita a disponibilidade, regras da campanha e validacao.", "Desconto promocional em campanha" });

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-8444-444444444444"),
                columns: new[] { "descricao", "titulo" },
                values: new object[] { "Beneficio interno para participantes da comunidade QuebraNunca, sujeito a disponibilidade e regras da campanha.", "Beneficio promocional da comunidade" });

            migrationBuilder.UpdateData(
                table: "beneficios_pontuacao",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-4555-8555-555555555555"),
                columns: new[] { "descricao", "titulo" },
                values: new object[] { "Condicao promocional interna para campanhas selecionadas QuebraNunca, sujeita a disponibilidade e validacao.", "Condicao especial QuebraNunca" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback nao reintroduz copy financeira legada no catalogo publico.
        }
    }
}

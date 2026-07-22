using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class PermitirSaldoNegativoParaEstornoPontosQN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_pontuacoes_beneficios_atletas_saldo_nao_negativo",
                table: "pontuacoes_beneficios_atletas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_pontuacoes_beneficios_atletas_saldo_nao_negativo",
                table: "pontuacoes_beneficios_atletas",
                sql: "\"saldo_atual\" >= 0");
        }
    }
}
